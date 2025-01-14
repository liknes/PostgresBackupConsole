using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresBackupConsole.Services
{
    public interface IPostgresService
    {
        Task<List<string>> GetAllDatabaseNames();
        Task<bool> BackupDatabase(string databaseName, string outputPath);
        void CleanupOldBackups(string backupPath, int retentionDays);
    }
}
