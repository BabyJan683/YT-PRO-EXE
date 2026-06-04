using Microsoft.Data.Sqlite;
using YTDownloaderPro.Models;

namespace YTDownloaderPro.Services;

public class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public DatabaseService(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private SqliteConnection GetConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON;";
            cmd.ExecuteNonQuery();
        }
        return _connection;
    }

    private void InitializeDatabase()
    {
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS downloads (
                id TEXT PRIMARY KEY,
                url TEXT NOT NULL,
                title TEXT,
                thumbnail TEXT,
                output_path TEXT,
                file_name TEXT,
                quality TEXT,
                format TEXT,
                total_bytes INTEGER DEFAULT 0,
                downloaded_bytes INTEGER DEFAULT 0,
                status TEXT DEFAULT 'Queued',
                media_type TEXT DEFAULT 'Video',
                progress REAL DEFAULT 0,
                error_message TEXT,
                created_at TEXT,
                completed_at TEXT,
                scheduled_at TEXT,
                threads INTEGER DEFAULT 8,
                is_playlist INTEGER DEFAULT 0,
                playlist_index INTEGER DEFAULT 0,
                playlist_total INTEGER DEFAULT 0,
                uploader TEXT,
                duration TEXT,
                file_size INTEGER DEFAULT 0,
                speed REAL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT
            );

            CREATE TABLE IF NOT EXISTS scheduled_downloads (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                download_id TEXT,
                scheduled_time TEXT,
                is_recurring INTEGER DEFAULT 0,
                recurrence_days TEXT,
                is_active INTEGER DEFAULT 1,
                FOREIGN KEY (download_id) REFERENCES downloads(id)
            );

            CREATE INDEX IF NOT EXISTS idx_downloads_status ON downloads(status);
            CREATE INDEX IF NOT EXISTS idx_downloads_created ON downloads(created_at);
        ";
        cmd.ExecuteNonQuery();
    }

    public void SaveDownload(DownloadItem item)
    {
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO downloads 
            (id, url, title, thumbnail, output_path, file_name, quality, format,
             total_bytes, downloaded_bytes, status, media_type, progress,
             error_message, created_at, completed_at, scheduled_at, threads,
             is_playlist, playlist_index, playlist_total, uploader, duration, file_size, speed)
            VALUES
            (@id, @url, @title, @thumbnail, @output_path, @file_name, @quality, @format,
             @total_bytes, @downloaded_bytes, @status, @media_type, @progress,
             @error_message, @created_at, @completed_at, @scheduled_at, @threads,
             @is_playlist, @playlist_index, @playlist_total, @uploader, @duration, @file_size, @speed)";

        cmd.Parameters.AddWithValue("@id", item.Id);
        cmd.Parameters.AddWithValue("@url", item.Url);
        cmd.Parameters.AddWithValue("@title", item.Title ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@thumbnail", item.Thumbnail ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@output_path", item.OutputPath ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@file_name", item.FileName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@quality", item.Quality ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@format", item.Format ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@total_bytes", item.TotalBytes);
        cmd.Parameters.AddWithValue("@downloaded_bytes", item.DownloadedBytes);
        cmd.Parameters.AddWithValue("@status", item.Status.ToString());
        cmd.Parameters.AddWithValue("@media_type", item.MediaType.ToString());
        cmd.Parameters.AddWithValue("@progress", item.Progress);
        cmd.Parameters.AddWithValue("@error_message", item.ErrorMessage ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@created_at", item.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@completed_at", item.CompletedAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@scheduled_at", item.ScheduledAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@threads", item.Threads);
        cmd.Parameters.AddWithValue("@is_playlist", item.IsPlaylist ? 1 : 0);
        cmd.Parameters.AddWithValue("@playlist_index", item.PlaylistIndex);
        cmd.Parameters.AddWithValue("@playlist_total", item.PlaylistTotal);
        cmd.Parameters.AddWithValue("@uploader", item.Uploader ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@duration", item.Duration ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@file_size", item.FileSize);
        cmd.Parameters.AddWithValue("@speed", item.Speed);
        cmd.ExecuteNonQuery();
    }

    public void UpdateDownloadStatus(string id, DownloadStatus status, double progress = 0,
        long downloadedBytes = 0, double speed = 0, string eta = "", string errorMessage = "")
    {
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE downloads SET 
                status = @status, progress = @progress, 
                downloaded_bytes = @downloaded_bytes, speed = @speed,
                error_message = @error_message,
                completed_at = CASE WHEN @status = 'Completed' THEN @completed_at ELSE completed_at END
            WHERE id = @id";

        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@status", status.ToString());
        cmd.Parameters.AddWithValue("@progress", progress);
        cmd.Parameters.AddWithValue("@downloaded_bytes", downloadedBytes);
        cmd.Parameters.AddWithValue("@speed", speed);
        cmd.Parameters.AddWithValue("@error_message", errorMessage ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@completed_at", status == DownloadStatus.Completed
            ? DateTime.Now.ToString("O") : (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<DownloadItem> GetAllDownloads()
    {
        var result = new List<DownloadItem>();
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM downloads ORDER BY created_at DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadDownloadItem(reader));
        }
        return result;
    }

    public List<DownloadItem> GetDownloadsByStatus(DownloadStatus status)
    {
        var result = new List<DownloadItem>();
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM downloads WHERE status = @status ORDER BY created_at DESC";
        cmd.Parameters.AddWithValue("@status", status.ToString());
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadDownloadItem(reader));
        }
        return result;
    }

    public void DeleteDownload(string id)
    {
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM downloads WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void ClearHistory()
    {
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM downloads WHERE status = 'Completed'";
        cmd.ExecuteNonQuery();
    }

    public (int total, long totalSize, int completed, int failed) GetStats()
    {
        var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                COUNT(*) as total,
                COALESCE(SUM(file_size), 0) as total_size,
                SUM(CASE WHEN status = 'Completed' THEN 1 ELSE 0 END) as completed,
                SUM(CASE WHEN status = 'Failed' THEN 1 ELSE 0 END) as failed
            FROM downloads";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return (
                reader.GetInt32(0),
                reader.GetInt64(1),
                reader.GetInt32(2),
                reader.GetInt32(3)
            );
        }
        return (0, 0, 0, 0);
    }

    private static DownloadItem ReadDownloadItem(SqliteDataReader reader)
    {
        var item = new DownloadItem
        {
            Id = reader.GetString(0),
            Url = reader.GetString(1),
            Title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Thumbnail = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            OutputPath = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            FileName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            Quality = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Format = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            TotalBytes = reader.GetInt64(8),
            DownloadedBytes = reader.GetInt64(9),
            Progress = reader.GetDouble(12),
            ErrorMessage = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
            Threads = reader.GetInt32(17),
            IsPlaylist = reader.GetInt32(18) == 1,
            PlaylistIndex = reader.GetInt32(19),
            PlaylistTotal = reader.GetInt32(20),
            Uploader = reader.IsDBNull(21) ? string.Empty : reader.GetString(21),
            Duration = reader.IsDBNull(22) ? string.Empty : reader.GetString(22),
            FileSize = reader.GetInt64(23),
        };

        if (Enum.TryParse<DownloadStatus>(reader.GetString(10), out var status))
            item.Status = status;
        if (Enum.TryParse<MediaType>(reader.GetString(11), out var mediaType))
            item.MediaType = mediaType;

        if (!reader.IsDBNull(14) && DateTime.TryParse(reader.GetString(14), out var created))
            item.CreatedAt = created;
        if (!reader.IsDBNull(15) && DateTime.TryParse(reader.GetString(15), out var completed))
            item.CompletedAt = completed;
        if (!reader.IsDBNull(16) && DateTime.TryParse(reader.GetString(16), out var scheduled))
            item.ScheduledAt = scheduled;

        return item;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
