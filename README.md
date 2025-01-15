# PostgreSQL Backup Console

A .NET Core console application that automates [PostgreSQL](https://www.postgresql.org/download/windows/) database backups with configurable settings, logging, and retention policies. Perfect for scheduling regular backups of your PostgreSQL databases.

## Features

- 🗃️ Automated backup of multiple PostgreSQL databases
- 📅 Configurable backup retention period
- 📝 Detailed logging with retention management
- 🔄 Compatible with Windows Task Scheduler
- ☁️ Configurable backup location (supports cloud storage paths)
- 🔒 Secure password handling
- ⚡ Asynchronous operations
- ❌ Robust error handling

## Tested and found to work with:

- PostreSQL 13
- PostreSQL 17

## Prerequisites
- .NET 8.0 or later
- PostgreSQL installed with pg_dump utility available in PATH
- Appropriate PostgreSQL user permissions for backup operations

## Installation

1. Clone the repository or download the latest release
2. Configure the application settings in `appsettings.json`
3. Build the application:

```
dotnet build
```

## Configuration

Update the `appsettings.json` file with your PostgreSQL settings:

```
json
{
  "PostgresSettings": {
    "Host": "localhost",
    "Port": 5432,
    "Username": "postgres",
    "Password": "your_password",
    "Database": "postgres",
    "BackupType": "Full",
    "BackupRetentionDays": 7,
    "BackupPath": "C:\\Your\\Backup\\Path"
  }
}
```

### Configuration Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| Host | PostgreSQL server hostname | localhost |
| Port | PostgreSQL server port | 5432 |
| Username | PostgreSQL user with backup privileges | postgres |
| Password | User password | - |
| Database | Default database (not used for backup selection) | postgres |
| BackupType | Type of backup to perform | Full |
| BackupRetentionDays | Number of days to keep backups | 7 |
| BackupPath | Directory where backups will be stored | - |

## Usage

### Manual Execution

Run the application from the command line:
```
dotnet PostgresBackupConsole.dll
```

### Task Scheduler Setup

1. Open Windows Task Scheduler
2. Create a new Basic Task
3. Set the trigger (schedule) as needed
4. Action: Start a program
5. Program/script: Path to `PostgresBackupConsole.exe`
6. Start in: Application directory path

## Logging

- Logs are stored in the `Logs` directory
- Log files are named `backup_log_YYYYMMDD_HHMMSS.txt`
- Logs older than 30 days are automatically cleaned up
- Each backup operation is logged with timestamp and status

## Backup Files

- Backups are stored in the configured backup path
- File naming format: `DatabaseName_YYYYMMDD_HHMMSS.backup`
- Files use PostgreSQL's custom format (.backup)
- Old backups are automatically removed based on retention days

## Restoring Backups

To restore a backup, use the pg_restore utility:
bash
pg_restore -h localhost -p 5432 -U postgres -d database_name backup_file.backup

## Error Handling

The application includes comprehensive error handling for:
- Database connection issues
- Backup process failures
- File system operations
- Configuration problems

All errors are logged to both console and log file.

## Development

### Project Structure
```
PostgresBackupConsole/
├── Models/
│ └── PostgresSettings.cs
├── Services/
│ ├── IPostgresService.cs
│ └── PostgresService.cs
├── Program.cs
├── appsettings.json
├── README.md
└-- MIT License
```

### Building from Source
```
dotnet restore
dotnet build
dotnet publish -c Release
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Built with .NET 8.0
- Uses Npgsql for PostgreSQL connectivity
- Inspired by the need for simple, reliable database backups for my [Davinci Resolve Project Server](https://www.blackmagicdesign.com/products/davinciresolve/collaboration)

## Support

For issues, questions, or contributions, please create an issue in the GitHub repository.
