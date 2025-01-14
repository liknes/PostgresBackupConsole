using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PostgresBackupConsole.Services;

class Program
{
    public static async Task<int> Main(string[] args)  // Return int for exit code
    {
        // Setup file logging with date in filename
        var logPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            $"Logs",  // Create a Logs subdirectory
            $"backup_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        );

        // Ensure logs directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        try
        {
            using var fileStream = new StreamWriter(logPath, true);
            LogMessage("Starting PostgreSQL Backup Process...", fileStream);

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<PostgresSettings>(
                        hostContext.Configuration.GetSection("PostgresSettings"));
                    services.AddScoped<IPostgresService, PostgresService>();
                })
                .Build();

            var postgresService = host.Services.GetRequiredService<IPostgresService>();
            var settings = host.Services.GetRequiredService<IOptions<PostgresSettings>>().Value;

            // Display settings
            LogMessage($"Using settings:", fileStream);
            LogMessage($"Host: {settings.Host}", fileStream);
            LogMessage($"Port: {settings.Port}", fileStream);
            LogMessage($"Username: {settings.Username}", fileStream);
            LogMessage($"Database: {settings.Database}", fileStream);
            LogMessage($"Backup Type: {settings.BackupType}", fileStream);
            LogMessage($"Retention Days: {settings.BackupRetentionDays}", fileStream);

            // Create backup directory
            var backupPath = settings.BackupPath;
            if (string.IsNullOrEmpty(backupPath))
            {
                backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                LogMessage($"Warning: No backup path configured, using default: {backupPath}", fileStream);
            }
            Directory.CreateDirectory(backupPath);
            LogMessage($"Backup directory: {backupPath}", fileStream);

            // Get all databases
            LogMessage("\nRetrieving database list...", fileStream);
            var databases = await postgresService.GetAllDatabaseNames();
            LogMessage($"Found {databases.Count} databases to backup:", fileStream);
            foreach (var db in databases)
            {
                LogMessage($"- {db}", fileStream);
            }

            // Backup each database
            LogMessage("\nStarting backup process...", fileStream);
            foreach (var database in databases)
            {
                LogMessage($"\nBacking up database: {database}", fileStream);
                var success = await postgresService.BackupDatabase(database, backupPath);
                if (success)
                    LogMessage($"✓ Successfully backed up database: {database}", fileStream);
                else
                    LogMessage($"✗ Failed to backup database: {database}", fileStream);
            }

            // Cleanup old backups
            LogMessage("\nCleaning up old backups...", fileStream);
            postgresService.CleanupOldBackups(backupPath, settings.BackupRetentionDays);

            LogMessage("\nBackup process completed successfully!", fileStream);
            CleanupOldLogs(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"), 30);
            return 0; // Success exit code
        }
        catch (Exception ex)
        {
            // Make sure we log errors even if fileStream creation failed
            using var errorStream = new StreamWriter(logPath, true);
            LogMessage($"\nERROR: {ex.Message}", errorStream);
            LogMessage(ex.StackTrace ?? "No stack trace available", errorStream);
            return 1; // Error exit code
        }
    }

    private static void CleanupOldLogs(string logsPath, int retentionDays)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var files = Directory.GetFiles(logsPath, "backup_log_*.txt");

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to cleanup old logs: {ex.Message}");
        }
    }

    private static void LogMessage(string message, StreamWriter fileStream)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logMessage = $"[{timestamp}] {message}";

        // Still write to console for debugging purposes
        Console.WriteLine(logMessage);

        // Write to file with error handling
        try
        {
            fileStream.WriteLine(logMessage);
            fileStream.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to log file: {ex.Message}");
        }
    }
}