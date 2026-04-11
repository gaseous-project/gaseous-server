using System;
using System.Data;

namespace gaseous_server.Classes
{
    /// <summary>
    /// Manages the migration_journal table, which records the lifecycle of every
    /// migration step (pre-upgrade code, SQL script, post-upgrade code, validation).
    /// This replaces settings-based tracking (e.g. RenameMigration_*) with a single,
    /// schema-verifiable source of truth.
    /// </summary>
    public static class MigrationJournal
    {
        /// <summary>
        /// Identifies the category of migration work recorded in the journal.
        /// </summary>
        public enum StepType
        {
            /// <summary>Blocking code that runs before a schema script.</summary>
            PreUpgrade,
            /// <summary>The versioned SQL script for a schema step.</summary>
            SqlScript,
            /// <summary>Code that runs after a schema script completes.</summary>
            PostUpgrade,
            /// <summary>Post-step structural validation checks.</summary>
            Validation,
            /// <summary>Deferred migration work executed in the background.</summary>
            BackgroundTask
        }

        /// <summary>
        /// Represents the lifecycle state of a journal entry.
        /// </summary>
        public enum StepStatus
        {
            /// <summary>The step has started and is still in progress.</summary>
            Started,
            /// <summary>The step completed successfully.</summary>
            Succeeded,
            /// <summary>The step ended with an error.</summary>
            Failed,
            /// <summary>The step was intentionally skipped.</summary>
            Skipped
        }

        /// <summary>
        /// Ensures the migration_journal table exists. Called at the very start of
        /// InitDB, before any migration steps run. Uses a plain CREATE TABLE statement
        /// without IF NOT EXISTS so that it works across database engines; wraps the
        /// call in a try/catch to silently continue when the table already exists.
        /// </summary>
        public static void EnsureTable()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            // Check existence via information_schema rather than relying on IF NOT EXISTS
            string checkSql = "SELECT COUNT(*) FROM information_schema.tables " +
                              "WHERE table_schema = @dbname AND table_name = 'migration_journal'";
            var dbDict = new Dictionary<string, object> { { "dbname", Config.DatabaseConfiguration.DatabaseName } };
            DataTable result = db.ExecuteCMD(checkSql, dbDict);

            if (Convert.ToInt32(result.Rows[0][0]) == 0)
            {
                string createSql = @"
                    CREATE TABLE `migration_journal` (
                        `Id`             BIGINT        NOT NULL AUTO_INCREMENT,
                        `SchemaVersion`  INT           NOT NULL,
                        `StepType`       VARCHAR(32)   NOT NULL,
                        `StepName`       VARCHAR(256)  NOT NULL,
                        `Status`         VARCHAR(32)   NOT NULL,
                        `StartedAt`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        `CompletedAt`    DATETIME      NULL,
                        `ErrorMessage`   TEXT          NULL,
                        PRIMARY KEY (`Id`),
                        INDEX `idx_journal_version_step` (`SchemaVersion`, `StepType`)
                    )";
                db.ExecuteNonQuery(createSql);
                Logging.LogKey(Logging.LogType.Information, "process.database", "database.migration_journal_table_created");
            }
        }

        /// <summary>
        /// Records the start of a migration step and returns the journal row Id so the
        /// caller can later call Complete() or Fail() with the same Id.
        /// </summary>
        public static long Start(int schemaVersion, StepType stepType, string stepName)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = @"
                INSERT INTO migration_journal (SchemaVersion, StepType, StepName, Status, StartedAt)
                VALUES (@ver, @type, @name, @status, UTC_TIMESTAMP());
                SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            var dbDict = new Dictionary<string, object>
            {
                { "ver",    schemaVersion },
                { "type",   stepType.ToString() },
                { "name",   stepName },
                { "status", StepStatus.Started.ToString() }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);
            return Convert.ToInt64(data.Rows[0][0]);
        }

        /// <summary>Marks an in-progress journal entry as succeeded.</summary>
        public static void Complete(long journalId)
        {
            SetFinal(journalId, StepStatus.Succeeded, null);
        }

        /// <summary>Marks an in-progress journal entry as failed, storing the error message.</summary>
        public static void Fail(long journalId, string errorMessage)
        {
            SetFinal(journalId, StepStatus.Failed, errorMessage);
        }

        /// <summary>Marks an in-progress journal entry as skipped (e.g. step no longer applicable).</summary>
        public static void Skip(long journalId, string? reason = null)
        {
            SetFinal(journalId, StepStatus.Skipped, reason);
        }

        private static void SetFinal(long journalId, StepStatus status, string? errorMessage)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = @"
                UPDATE migration_journal
                   SET Status = @status, CompletedAt = UTC_TIMESTAMP(), ErrorMessage = @err
                 WHERE Id = @id";
            var dbDict = new Dictionary<string, object>
            {
                { "status", status.ToString() },
                { "err",    (object?)errorMessage ?? DBNull.Value },
                { "id",     journalId }
            };
            db.ExecuteNonQuery(sql, dbDict);
        }

        /// <summary>
        /// Returns true when a step for the given version and type already completed
        /// successfully. Used to make steps idempotent: check before running, skip if
        /// already succeeded. This replaces Config.ReadSetting("RenameMigration_*").
        /// </summary>
        public static bool AlreadySucceeded(int schemaVersion, StepType stepType, string stepName)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = @"
                SELECT COUNT(*) FROM migration_journal
                 WHERE SchemaVersion = @ver
                   AND StepType = @type
                   AND StepName = @name
                   AND Status = @status";
            var dbDict = new Dictionary<string, object>
            {
                { "ver",    schemaVersion },
                { "type",   stepType.ToString() },
                { "name",   stepName },
                { "status", StepStatus.Succeeded.ToString() }
            };
            DataTable result = db.ExecuteCMD(sql, dbDict);
            return Convert.ToInt32(result.Rows[0][0]) > 0;
        }

        /// <summary>
        /// Returns the most recent journal entries for display in CLI status output.
        /// </summary>
        public static DataTable GetRecentEntries(int limit = 50)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = @"
                SELECT SchemaVersion, StepType, StepName, Status, StartedAt, CompletedAt, ErrorMessage
                  FROM migration_journal
                 ORDER BY Id DESC
                 LIMIT @lim";
            var dbDict = new Dictionary<string, object> { { "lim", limit } };
            return db.ExecuteCMD(sql, dbDict);
        }
    }
}
