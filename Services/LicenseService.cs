using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using YTDownloaderPro.Models;

namespace YTDownloaderPro.Services;

public enum LicenseState { None, Trial, Active, Expired }

public class LicenseInfo
{
    public LicenseState State { get; set; } = LicenseState.None;
    public int DaysRemaining { get; set; }
    public string? Key { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class LicenseService
{
    private readonly AppSettings _settings;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private const int TrialDays = 7;

    public string MachineName => Environment.MachineName;
    public string HardwareId => GetOrCreateHardwareId();

    public LicenseService(AppSettings settings)
    {
        _settings = settings;
    }

    private string GetOrCreateHardwareId()
    {
        if (!string.IsNullOrEmpty(_settings.HardwareId))
            return _settings.HardwareId;

        try
        {
            var sb = new StringBuilder();
            using var cpuSearcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (ManagementObject obj in cpuSearcher.Get())
                sb.Append(obj["ProcessorId"]?.ToString() ?? "");

            using var boardSearcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (ManagementObject obj in boardSearcher.Get())
                sb.Append(obj["SerialNumber"]?.ToString() ?? "");

            sb.Append(Environment.MachineName);

            var raw = sb.ToString().Trim();
            if (raw.Length < 8) raw = Environment.MachineName + Environment.ProcessorCount;

            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var hwid = BitConverter.ToString(hash).Replace("-", "").ToUpper();
            hwid = $"{hwid[..8]}-{hwid[8..16]}-{hwid[16..24]}-{hwid[24..32]}";

            _settings.HardwareId = hwid;
            return hwid;
        }
        catch
        {
            if (string.IsNullOrEmpty(_settings.HardwareId))
                _settings.HardwareId = Guid.NewGuid().ToString("N").ToUpper();
            return _settings.HardwareId;
        }
    }

    private bool HasSupabase() =>
        !string.IsNullOrWhiteSpace(_settings.SupabaseUrl) &&
        !string.IsNullOrWhiteSpace(_settings.SupabaseAnonKey) &&
        _settings.SupabaseUrl.StartsWith("https://");

    private void ConfigureHttp()
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("apikey", _settings.SupabaseAnonKey);
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.SupabaseAnonKey}");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public LicenseInfo GetLocalStatus()
    {
        var hwid = HardwareId;
        var pcName = MachineName;

        // Active license
        if (!string.IsNullOrEmpty(_settings.LicenseKey) &&
            _settings.LicenseStatus == "active")
        {
            return new LicenseInfo
            {
                State = LicenseState.Active,
                Key = _settings.LicenseKey,
                MachineName = pcName,
                HardwareId = hwid,
                Message = "License active",
                DaysRemaining = -1
            };
        }

        // Trial
        if (_settings.TrialStartDate.HasValue)
        {
            var elapsed = (DateTime.Now - _settings.TrialStartDate.Value).Days;
            var remaining = TrialDays - elapsed;

            if (remaining > 0)
                return new LicenseInfo
                {
                    State = LicenseState.Trial,
                    DaysRemaining = remaining,
                    MachineName = pcName,
                    HardwareId = hwid,
                    Message = $"Trial: {remaining} day(s) remaining"
                };
            else
                return new LicenseInfo
                {
                    State = LicenseState.Expired,
                    DaysRemaining = 0,
                    MachineName = pcName,
                    HardwareId = hwid,
                    Message = "Trial expired — please activate a license key"
                };
        }

        return new LicenseInfo
        {
            State = LicenseState.None,
            MachineName = pcName,
            HardwareId = hwid,
            Message = "Not activated"
        };
    }

    public void StartTrial()
    {
        if (_settings.TrialStartDate.HasValue) return;
        _settings.TrialStartDate = DateTime.Now;
        _settings.LicenseStatus = "trial";
    }

    public async Task<(bool success, string message)> ActivateKeyAsync(string key)
    {
        key = key.Trim().ToUpper();
        if (string.IsNullOrWhiteSpace(key))
            return (false, "Please enter a license key.");

        // Validate format: YTPRO-XXXX-XXXX-XXXX
        if (!System.Text.RegularExpressions.Regex.IsMatch(key, @"^YTPRO-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}$"))
            return (false, "Invalid key format. Expected: YTPRO-XXXX-XXXX-XXXX");

        // If no Supabase configured, use offline activation
        if (!HasSupabase())
            return ActivateOffline(key);

        try
        {
            ConfigureHttp();
            var baseUrl = _settings.SupabaseUrl.TrimEnd('/');
            var hwid = HardwareId;
            var pcName = MachineName;

            // Check if key exists and is unused or already active on this device
            var checkUrl = $"{baseUrl}/rest/v1/licenses?key=eq.{Uri.EscapeDataString(key)}&select=*";
            var checkResp = await _http.GetAsync(checkUrl);
            if (!checkResp.IsSuccessStatusCode)
                return (false, "Could not reach license server. Check your connection.");

            var checkJson = await checkResp.Content.ReadAsStringAsync();
            var licenses = JsonConvert.DeserializeObject<List<dynamic>>(checkJson);

            if (licenses == null || licenses.Count == 0)
                return (false, "License key not found. Please check the key and try again.");

            dynamic lic = licenses[0];
            string status = lic.status?.ToString() ?? "";
            string? existingHwid = lic.hardware_id?.ToString();

            if (status == "revoked")
                return (false, "This license key has been revoked.");

            if (status == "active")
            {
                // Check if it's already activated on this PC
                if (existingHwid == hwid)
                {
                    // Re-activation of same PC — allow
                    _settings.LicenseKey = key;
                    _settings.LicenseStatus = "active";
                    await LogUsageAsync("reactivation", key);
                    return (true, "License re-activated successfully on this PC.");
                }
                else
                {
                    return (false, "This key is already activated on another PC. Each key can only be used on one PC.");
                }
            }

            if (status == "expired")
                return (false, "This license key has expired.");

            if (status != "unused")
                return (false, $"Invalid key status: {status}");

            // Activate the key
            var patchUrl = $"{baseUrl}/rest/v1/licenses?key=eq.{Uri.EscapeDataString(key)}";
            var patchBody = JsonConvert.SerializeObject(new
            {
                status = "active",
                hardware_id = hwid,
                device_name = pcName,
                activated_at = DateTime.UtcNow.ToString("O"),
                expires_at = DateTime.UtcNow.AddYears(1).ToString("O"),
                last_seen = DateTime.UtcNow.ToString("O"),
                ip_address = await GetPublicIpAsync(),
                os_version = Environment.OSVersion.ToString()
            });

            var patchReq = new HttpRequestMessage(HttpMethod.Patch, patchUrl)
            {
                Content = new StringContent(patchBody, Encoding.UTF8, "application/json")
            };
            patchReq.Headers.Add("Prefer", "return=minimal");

            var patchResp = await _http.SendAsync(patchReq);
            if (!patchResp.IsSuccessStatusCode)
            {
                var err = await patchResp.Content.ReadAsStringAsync();
                return (false, $"Activation failed: {err}");
            }

            _settings.LicenseKey = key;
            _settings.LicenseStatus = "active";

            await LogUsageAsync("activation", key);

            return (true, $"✓ License activated successfully!\nThis PC ({pcName}) is now licensed.");
        }
        catch (HttpRequestException)
        {
            // Fallback: offline mode with basic validation
            return ActivateOffline(key);
        }
        catch (Exception ex)
        {
            return (false, $"Activation error: {ex.Message}");
        }
    }

    private (bool success, string message) ActivateOffline(string key)
    {
        _settings.LicenseKey = key;
        _settings.LicenseStatus = "active";
        return (true, $"✓ License activated (offline mode).\nPC: {MachineName}");
    }

    private async Task LogUsageAsync(string action, string? detail = null)
    {
        if (!HasSupabase()) return;
        try
        {
            var baseUrl = _settings.SupabaseUrl.TrimEnd('/');
            var body = JsonConvert.SerializeObject(new
            {
                hardware_id = HardwareId,
                license_key = _settings.LicenseKey,
                action,
                detail,
                ip_address = await GetPublicIpAsync(),
                created_at = DateTime.UtcNow.ToString("O")
            });
            await _http.PostAsync($"{baseUrl}/rest/v1/usage_logs",
                new StringContent(body, Encoding.UTF8, "application/json"));
        }
        catch { }
    }

    private static async Task<string> GetPublicIpAsync()
    {
        try
        {
            using var tmp = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            return (await tmp.GetStringAsync("https://api.ipify.org")).Trim();
        }
        catch { return "unknown"; }
    }

    public async Task SyncLastSeenAsync()
    {
        if (!HasSupabase() || string.IsNullOrEmpty(_settings.LicenseKey)) return;
        try
        {
            ConfigureHttp();
            var baseUrl = _settings.SupabaseUrl.TrimEnd('/');
            var key = _settings.LicenseKey;
            var body = JsonConvert.SerializeObject(new
            {
                last_seen = DateTime.UtcNow.ToString("O"),
                download_count = 0
            });
            var req = new HttpRequestMessage(HttpMethod.Patch,
                $"{baseUrl}/rest/v1/licenses?key=eq.{Uri.EscapeDataString(key)}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            req.Headers.Add("Prefer", "return=minimal");
            await _http.SendAsync(req);
        }
        catch { }
    }
}
