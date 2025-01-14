using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using PostgresBackupConsole.Models;
using System.Diagnostics;
using System.Text;

namespace PostgresBackupConsole.Services
{
    public class PostgresService : IPostgresService
    {
        private readonly PostgresSettings _settings;
        private readonly ILogger<PostgresService> _logger;

        public PostgresService(IOptions<PostgresSettings> settings, ILogger<PostgresService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<List<string>> GetAllDatabaseNames()
        {
            var databases = new List<string>();

            try
            {
                using var conn = new NpgsqlConnection(_settings.GetConnectionString());
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    @"SELECT datname FROM pg_database 
                  WHERE datistemplate = false 
                  AND datname != 'postgres'
                  ORDER BY datname;", conn);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    databases.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database names");
                throw;
            }

            return databases;
        }

        public async Task<bool> BackupDatabase(string databaseName, string outputPath)
        {
            try
            {
                var backupFile = Path.Combine(outputPath, $"{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.backup");

                using var process = new Process();
                process.StartInfo.FileName = "pg_dump";
                process.StartInfo.Arguments = $"-h {_settings.Host} -p {_settings.Port} -U {_settings.Username} -F c -b -v -f \"{backupFile}\" {databaseName}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.EnvironmentVariables["PGPASSWORD"] = _settings.Password;

                _logger.LogInformation($"Starting backup of database: {databaseName}");
                _logger.LogDebug($"Executing command: pg_dump -h {_settings.Host} -p {_settings.Port} -U {_settings.Username} -F c -b -v -f \"{backupFile}\" {databaseName}");

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                        _logger.LogInformation($"[{databaseName}] {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // Only log as warning if it's an actual error message
                        if (e.Data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                            e.Data.Contains("fatal", StringComparison.OrdinalIgnoreCase) ||
                            e.Data.Contains("failed", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("[{Database}] {Message}", databaseName, e.Data);
                        }
                        // Otherwise log as debug since it's just progress information
                        else
                        {
                            _logger.LogDebug("[{Database}] {Message}", databaseName, e.Data);
                        }
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    process.Kill();
                    _logger.LogError($"❌ Backup operation timed out for database: {databaseName}");
                    return false;
                }

                // Check if backup file exists and has content
                var fileInfo = new FileInfo(backupFile);
                if (!fileInfo.Exists || fileInfo.Length == 0)
                {
                    _logger.LogError($"❌ Backup file for {databaseName} is empty or doesn't exist");
                    return false;
                }

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation($"✓ Successfully backed up database: {databaseName} (Size: {fileInfo.Length / 1024.0:N2} KB)");
                    return true;
                }
                else
                {
                    _logger.LogError($"❌ Backup failed for database: {databaseName}");
                    _logger.LogError($"Exit code: {process.ExitCode}");
                    _logger.LogError($"Error output: {errorBuilder}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error backing up database: {databaseName}");
                return false;
            }
        }

        public void CleanupOldBackups(string backupPath, int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var files = Directory.GetFiles(backupPath, "*.backup");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                        _logger.LogInformation($"Deleted old backup file: {fileInfo.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old backups");
            }
        }
    }
}
