using PostgresBackupConsole.Models;

public class PostgresSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5432;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = "postgres";
    public int BackupRetentionDays { get; set; } = 7;
    public BackupType BackupType { get; set; } = BackupType.Full;
    public List<string> SpecificTables { get; set; } = new List<string>();
    public string BackupPath { get; set; } = string.Empty;

    public string GetConnectionString()
    {
        return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};";
    }
}
