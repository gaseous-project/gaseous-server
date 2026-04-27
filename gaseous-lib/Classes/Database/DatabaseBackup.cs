using System;
using System.Diagnostics;

namespace gaseous_server.Classes
{
    /// <summary>
    /// Provides database backup and restore capability using mysqldump.
    /// A backup is taken before any migration steps run. If migration fails,
    /// restore instructions (including the exact restore command) are written
    /// to the log so that support can recover the database.
    /// </summary>
    public static class DatabaseBackup
    {
        /// <summary>
        /// Identifies the database client family used for backup and restore.
        /// </summary>
        private enum BackupProvider
        {
            /// <summary>MariaDB and MySQL compatible command-line tools.</summary>
            MySqlLike,
            /// <summary>PostgreSQL command-line tools.</summary>
            PostgreSql
        }

        /// <summary>
        /// Generates a timestamped backup file path under the library backup directory.
        /// Example: /path/to/library/Backups/gaseous-backup-20260411-140035.sql
        /// </summary>
        public static string GenerateBackupPath()
        {
            string backupDir = Path.Combine(Config.LibraryConfiguration.LibraryRootDirectory, "Backups");
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            return Path.Combine(backupDir, $"gaseous-backup-{timestamp}.sql");
        }

        /// <summary>
        /// Executes a full dump of the configured database to <paramref name="backupFilePath"/>
        /// using the configured database engine's backup tool. Throws
        /// <see cref="DatabaseBackupException"/> if the dump fails so the caller can abort
        /// the migration safely.
        /// </summary>
        /// <param name="backupFilePath">Absolute path where the .sql dump will be written.</param>
        public static void Backup(string backupFilePath)
        {
            Logging.LogKey(Logging.LogType.Information, "process.database", "database.backup_starting",
                null, new[] { backupFilePath });

            var cfg = Config.DatabaseConfiguration;

            var provider = ResolveProvider();
            var psi = BuildBackupProcessStartInfo(provider, backupFilePath);

            using var process = Process.Start(psi)
                ?? throw new DatabaseBackupException($"Failed to start backup process '{psi.FileName}'.");

            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new DatabaseBackupException(
                    $"Backup command '{psi.FileName}' exited with code {process.ExitCode}. stderr: {stderr}");
            }

            var fileInfo = new FileInfo(backupFilePath);
            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                throw new DatabaseBackupException(
                    $"mysqldump completed but backup file is empty or missing: {backupFilePath}");
            }

            Logging.LogKey(Logging.LogType.Information, "process.database", "database.backup_complete",
                null, new[] { backupFilePath, fileInfo.Length.ToString() });
        }

        /// <summary>
        /// Writes a clearly formatted, copy-pasteable restore command to the log.
        /// Called immediately after a migration failure so that operators have
        /// everything they need to recover without searching documentation.
        /// </summary>
        /// <param name="backupFilePath">The backup file produced before migration began.</param>
        /// <param name="failedVersion">The schema version that failed.</param>
        /// <param name="failureReason">Short description of what went wrong.</param>
        public static void LogRestoreInstructions(string backupFilePath, int failedVersion, string failureReason)
        {
            var cfg = Config.DatabaseConfiguration;
            var provider = ResolveProvider();
            string restoreCommand = BuildRestoreCommand(provider, backupFilePath);

            Logging.LogKey(Logging.LogType.Critical, "process.database",
                "database.migration_failed_restore_instructions",
                null, new[]
                {
                    failedVersion.ToString(),
                    failureReason,
                    backupFilePath,
                    restoreCommand
                });

            // Also write directly to console so it is always visible regardless of
            // log-level filtering or log-sink failures during critical failure paths.
            Console.Error.WriteLine();
            Console.Error.WriteLine("=============================================================");
            Console.Error.WriteLine(" DATABASE MIGRATION FAILED");
            Console.Error.WriteLine("=============================================================");
            Console.Error.WriteLine($" Failed at schema version : {failedVersion}");
            Console.Error.WriteLine($" Reason                   : {failureReason}");
            Console.Error.WriteLine($" Backup file              : {backupFilePath}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(" To restore your database, run:");
            Console.Error.WriteLine($"   {restoreCommand}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(" Or using the Gaseous CLI:");
            Console.Error.WriteLine($"   gaseous-cli db restore \"{backupFilePath}\"");
            Console.Error.WriteLine("=============================================================");
            Console.Error.WriteLine();
        }

        /// <summary>
        /// Restores a database from a previously created SQL backup file.
        /// Intended for use by the CLI restore command.
        /// </summary>
        /// <param name="backupFilePath">Path to the .sql dump file to restore.</param>
        public static void Restore(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
            {
                throw new FileNotFoundException($"Backup file not found: {backupFilePath}");
            }

            Logging.LogKey(Logging.LogType.Warning, "process.database", "database.restore_starting",
                null, new[] { backupFilePath });

            var provider = ResolveProvider();
            var psi = BuildRestoreProcessStartInfo(provider);

            using var process = Process.Start(psi)
                ?? throw new DatabaseBackupException($"Failed to start restore process '{psi.FileName}'.");

            using (var fileStream = File.OpenRead(backupFilePath))
            {
                fileStream.CopyTo(process.StandardInput.BaseStream);
            }
            process.StandardInput.Close();

            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new DatabaseBackupException(
                    $"Restore command '{psi.FileName}' exited with code {process.ExitCode}. stderr: {stderr}");
            }

            Logging.LogKey(Logging.LogType.Information, "process.database", "database.restore_complete",
                null, new[] { backupFilePath });
        }

        /// <summary>
        /// Chooses the backup provider from the configured database engine.
        /// </summary>
        private static BackupProvider ResolveProvider()
        {
            string engine = (Config.DatabaseConfiguration.DatabaseEngine ?? "mysql")
                .Trim()
                .ToLowerInvariant();

            if (engine == "postgres" || engine == "postgresql" || engine == "pg")
            {
                return BackupProvider.PostgreSql;
            }

            // Treat mysql and mariadb as MySQL protocol compatible engines.
            return BackupProvider.MySqlLike;
        }

        /// <summary>
        /// Builds the process start information used to create a backup.
        /// </summary>
        private static ProcessStartInfo BuildBackupProcessStartInfo(BackupProvider provider, string backupFilePath)
        {
            var cfg = Config.DatabaseConfiguration;

            switch (provider)
            {
                case BackupProvider.PostgreSql:
                    {
                        string command = ResolveRequiredCommand(new[] { "pg_dump" });
                        var psi = new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = $"--host={cfg.HostName} --port={cfg.Port} --username={cfg.UserName} --format=plain --file=\"{backupFilePath}\" {cfg.DatabaseName}",
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        psi.Environment["PGPASSWORD"] = cfg.Password;
                        return psi;
                    }

                default:
                    {
                        // Prefer mariadb-dump when available, then fall back to mysqldump.
                        string command = ResolveRequiredCommand(new[] { "mariadb-dump", "mysqldump" });
                        var psi = new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = $"--host={cfg.HostName} --port={cfg.Port} --user={cfg.UserName} --single-transaction --routines --triggers --events --result-file=\"{backupFilePath}\" {cfg.DatabaseName}",
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        psi.Environment["MYSQL_PWD"] = cfg.Password;
                        return psi;
                    }
            }
        }

        /// <summary>
        /// Builds the process start information used to restore a backup.
        /// </summary>
        private static ProcessStartInfo BuildRestoreProcessStartInfo(BackupProvider provider)
        {
            var cfg = Config.DatabaseConfiguration;

            switch (provider)
            {
                case BackupProvider.PostgreSql:
                    {
                        string command = ResolveRequiredCommand(new[] { "psql" });
                        var psi = new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = $"--host={cfg.HostName} --port={cfg.Port} --username={cfg.UserName} --dbname={cfg.DatabaseName}",
                            RedirectStandardInput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        psi.Environment["PGPASSWORD"] = cfg.Password;
                        return psi;
                    }

                default:
                    {
                        string command = ResolveRequiredCommand(new[] { "mariadb", "mysql" });
                        var psi = new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = $"--host={cfg.HostName} --port={cfg.Port} --user={cfg.UserName} {cfg.DatabaseName}",
                            RedirectStandardInput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        psi.Environment["MYSQL_PWD"] = cfg.Password;
                        return psi;
                    }
            }
        }

        /// <summary>
        /// Builds a shell command string that operators can use to restore a backup manually.
        /// </summary>
        private static string BuildRestoreCommand(BackupProvider provider, string backupFilePath)
        {
            var cfg = Config.DatabaseConfiguration;

            switch (provider)
            {
                case BackupProvider.PostgreSql:
                    return $"PGPASSWORD=<YOUR_PASSWORD> psql --host={cfg.HostName} --port={cfg.Port} --username={cfg.UserName} --dbname={cfg.DatabaseName} < \"{backupFilePath}\"";

                default:
                    return $"MYSQL_PWD=<YOUR_PASSWORD> mariadb --host={cfg.HostName} --port={cfg.Port} --user={cfg.UserName} {cfg.DatabaseName} < \"{backupFilePath}\"  # or mysql";
            }
        }

        /// <summary>
        /// Returns the first available command from the supplied candidate list.
        /// </summary>
        private static string ResolveRequiredCommand(string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (IsCommandAvailable(candidate))
                {
                    return candidate;
                }
            }

            throw new DatabaseBackupException(
                "No suitable backup/restore command was found on PATH. Tried: " +
                string.Join(", ", candidates) +
                ". Install one of these tools or adjust your DatabaseEngine setting.");
        }

        /// <summary>
        /// Checks whether a command is present on the current PATH.
        /// </summary>
        private static bool IsCommandAvailable(string command)
        {
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathEnv)) return false;

            string[] pathEntries = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string dir in pathEntries)
            {
                string fullPath = Path.Join(dir, command);
                if (File.Exists(fullPath)) return true;

                // Windows compatibility for future local builds
                if (OperatingSystem.IsWindows())
                {
                    string exePath = Path.Join(dir, command + ".exe");
                    if (File.Exists(exePath)) return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Represents a backup or restore failure that should stop migration execution.
    /// </summary>
    public class DatabaseBackupException : Exception
    {
        /// <summary>
        /// Creates a backup exception with a descriptive error message.
        /// </summary>
        public DatabaseBackupException(string message) : base(message) { }

        /// <summary>
        /// Creates a backup exception with a descriptive error message and inner exception.
        /// </summary>
        public DatabaseBackupException(string message, Exception inner) : base(message, inner) { }
    }
}
