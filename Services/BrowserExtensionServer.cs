using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace YTDownloaderPro.Services;

public class BrowserExtensionServer : IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private const int Port = 9614;

    public event Action<string>? UrlReceived;
    public event Action<string, string>? VideoDetected;

    public bool IsRunning => _isRunning;

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            _isRunning = true;
            _cts = new CancellationTokenSource();
            _ = ListenAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Browser server start failed: {ex.Message}");
        }
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequestAsync(context);
            }
            catch (ObjectDisposedException) { break; }
            catch { }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var response = context.Response;
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (context.Request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 200;
            response.Close();
            return;
        }

        try
        {
            if (context.Request.HttpMethod == "POST" && context.Request.Url?.AbsolutePath == "/download")
            {
                using var reader = new System.IO.StreamReader(context.Request.InputStream);
                var body = await reader.ReadToEndAsync();
                dynamic? payload = JsonConvert.DeserializeObject(body);

                string url = payload?.url?.ToString() ?? string.Empty;
                string title = payload?.title?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(url))
                {
                    UrlReceived?.Invoke(url);
                    VideoDetected?.Invoke(url, title);
                }

                var responseJson = JsonConvert.SerializeObject(new { success = true, message = "Download queued" });
                var bytes = Encoding.UTF8.GetBytes(responseJson);
                response.ContentType = "application/json";
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes);
            }
            else if (context.Request.HttpMethod == "GET" && context.Request.Url?.AbsolutePath == "/status")
            {
                var statusJson = JsonConvert.SerializeObject(new { running = true, version = "2.0.0" });
                var bytes = Encoding.UTF8.GetBytes(statusJson);
                response.ContentType = "application/json";
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes);
            }
            else
            {
                response.StatusCode = 404;
            }
        }
        catch { response.StatusCode = 500; }
        finally { response.Close(); }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        _isRunning = false;
    }

    public void Dispose() => Stop();
}
