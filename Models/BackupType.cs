namespace PostgresBackupConsole.Models
{
    public enum BackupType
    {
        Full,
        SchemaOnly,
        DataOnly,
        SpecificTables
    }
}
