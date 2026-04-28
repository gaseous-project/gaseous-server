using System;
using System.Data;

namespace gaseous_server.Classes
{
    /// <summary>
    /// Declares the expected database structural state for a given schema version.
    /// After a migration step completes, the engine runs the checks registered for
    /// that version, logs any failures, and terminates if they are critical.
    ///
    /// HOW TO ADD VALIDATION FOR A NEW MIGRATION
    /// ------------------------------------------
    /// 1. Add a new entry to the _manifest list in BuildManifest(), using the helper
    ///    methods (RequireTable, RequireColumn, RequireIndex, RequireView).
    /// 2. The CI check (check-migration-scripts.yml) will fail the PR if the latest
    ///    SQL migration version has no manifest entry, keeping validation coverage
    ///    always up to date.
    /// </summary>
    public static class DatabaseMigrationManifest
    {
        /// <summary>
        /// Describes a column that should exist as part of a validation check.
        /// </summary>
        public class ColumnSpec
        {
            /// <summary>
            /// Gets the column name that must exist.
            /// </summary>
            public string Name { get; init; } = "";
            /// <summary>
            /// Expected SQL type family (e.g. "varchar", "int", "bigint", "datetime").
            /// Compared case-insensitively against the prefix before any "(" in the
            /// actual column type, so "varchar(255)" matches "varchar".
            /// Leave null to skip type checking.
            /// </summary>
            public string? TypeFamily { get; init; }
        }

        /// <summary>
        /// Defines one structural validation check for a specific schema version.
        /// </summary>
        public class ValidationEntry
        {
            /// <summary>
            /// Gets the schema version this check belongs to.
            /// </summary>
            public int SchemaVersion { get; init; }

            /// <summary>
            /// Gets the human-readable name of the check.
            /// </summary>
            public string CheckName { get; init; } = "";

            /// <summary>
            /// Gets whether a failed check should block startup.
            /// </summary>
            public bool IsCritical { get; init; } = true;

            /// <summary>The name of the table or view to check.</summary>
            public string? Table { get; init; }

            /// <summary>
            /// Gets the view name to validate when the check targets a view.
            /// </summary>
            public string? View { get; init; }

            /// <summary>If set, the named column must exist in Table.</summary>
            public ColumnSpec? Column { get; init; }

            /// <summary>If set, the named index must exist on Table.</summary>
            public string? Index { get; init; }
        }

        private static readonly List<ValidationEntry> _manifest = BuildManifest();

        /// <summary>
        /// Builds the in-memory validation manifest keyed by schema version.
        /// </summary>
        private static List<ValidationEntry> BuildManifest()
        {
            // -----------------------------------------------------------------------
            // Each block corresponds to one migration SQL file version.
            // Add new blocks here when you add a new gaseous-NNNN.sql file.
            // Mark IsCritical = false for checks that are advisory only.
            // -----------------------------------------------------------------------
            return new List<ValidationEntry>
            {
                // --- 1004: GameLibraries table ---
                new() { SchemaVersion = 1004, CheckName = "GameLibraries table exists",
                        Table = "GameLibraries" },
                new() { SchemaVersion = 1004, CheckName = "GameLibraries.DefaultLibrary exists",
                        Table = "GameLibraries", Column = new() { Name = "DefaultLibrary", TypeFamily = "int" } },

                // --- 1016: Settings table has ValueType column ---
                new() { SchemaVersion = 1016, CheckName = "Settings.ValueType exists",
                        Table = "Settings", Column = new() { Name = "ValueType" } },

                // --- 1023: Country and Language lookup tables ---
                new() { SchemaVersion = 1023, CheckName = "Country table exists",   Table = "Country" },
                new() { SchemaVersion = 1023, CheckName = "Language table exists",  Table = "Language" },

                // --- 1027: UserProfiles table ---
                new() { SchemaVersion = 1027, CheckName = "UserProfiles table exists", Table = "UserProfiles" },
                new() { SchemaVersion = 1027, CheckName = "UserProfiles.UserId exists",
                        Table = "UserProfiles", Column = new() { Name = "UserId" } },

                // --- 1031: Core Metadata_ tables ---
                new() { SchemaVersion = 1031, CheckName = "Metadata_Game table exists",     Table = "Metadata_Game" },
                new() { SchemaVersion = 1031, CheckName = "Metadata_Platform table exists", Table = "Metadata_Platform" },

                // --- 1035: Relation_Game_ tables and indexes ---
                new() { SchemaVersion = 1035, CheckName = "Relation_Game_Genres exists",
                        Table = "Relation_Game_Genres" },
                new() { SchemaVersion = 1035, CheckName = "idx_Relation_Genres_composite exists",
                        Table = "Relation_Game_Genres", Index = "idx_Relation_Genres_composite" },

                // --- 1036: Metadata_Game.MetadataSource column ---
                new() { SchemaVersion = 1036, CheckName = "Metadata_Game.MetadataSource exists",
                        Table = "Metadata_Game", Column = new() { Name = "MetadataSource", TypeFamily = "int" } },
                new() { SchemaVersion = 1036, CheckName = "Metadata_AgeRatingContentDescription table exists",
                        Table = "Metadata_AgeRatingContentDescription" },

                // --- 1037: Metadata_GameVideo.VideoId updated ---
                new() { SchemaVersion = 1037, CheckName = "Metadata_GameVideo.VideoId exists",
                        Table = "Metadata_GameVideo", Column = new() { Name = "VideoId", TypeFamily = "varchar" } },

                // --- 1038: MetadataMap.SignatureGameNameThe column ---
                new() { SchemaVersion = 1038, CheckName = "MetadataMap.SignatureGameNameThe exists",
                        Table = "MetadataMap", Column = new() { Name = "SignatureGameNameThe", TypeFamily = "varchar" } },

                // --- 1039: Settings.Setting column becomes varchar(100) ---
                new() { SchemaVersion = 1039, CheckName = "Settings.Setting is varchar",
                        Table = "Settings", Column = new() { Name = "Setting", TypeFamily = "varchar" } }
            };
        }

        /// <summary>
        /// Returns all manifest entries for schema versions > <paramref name="fromVersion"/>
        /// and &lt;= <paramref name="toVersion"/>. Used after a batch of migrations to validate
        /// everything that was applied.
        /// </summary>
        public static IEnumerable<ValidationEntry> GetEntriesForRange(int fromVersion, int toVersion)
            => _manifest.Where(e => e.SchemaVersion > fromVersion && e.SchemaVersion <= toVersion);

        /// <summary>
        /// Returns all manifest entries for exactly <paramref name="version"/>.
        /// </summary>
        public static IEnumerable<ValidationEntry> GetEntriesForVersion(int version)
            => _manifest.Where(e => e.SchemaVersion == version);

        /// <summary>
        /// Returns the highest schema version that has at least one manifest entry.
        /// Used by the CI consistency check to verify that the manifest covers the
        /// latest migration SQL file.
        /// </summary>
        public static int MaxManifestVersion
            => _manifest.Count > 0 ? _manifest.Max(e => e.SchemaVersion) : 0;
    }

    /// <summary>
    /// Executes validation entries against the live database and returns a result list.
    /// </summary>
    public static class DatabaseMigrationValidator
    {
        /// <summary>
        /// Represents the outcome of running a single manifest validation check.
        /// </summary>
        public class ValidationResult
        {
            /// <summary>
            /// Gets the display name of the validation check.
            /// </summary>
            public string CheckName { get; init; } = "";

            /// <summary>
            /// Gets the schema version associated with the check.
            /// </summary>
            public int SchemaVersion { get; init; }

            /// <summary>
            /// Gets whether the check passed.
            /// </summary>
            public bool Passed { get; init; }

            /// <summary>
            /// Gets whether a failure should block startup.
            /// </summary>
            public bool IsCritical { get; init; }

            /// <summary>
            /// Gets the failure reason when the check does not pass.
            /// </summary>
            public string? FailureReason { get; init; }
        }

        /// <summary>
        /// Runs all manifest checks for the given schema version range.
        /// Logs each result. Returns false if any critical check failed.
        /// </summary>
        public static bool ValidateRange(int fromVersion, int toVersion)
        {
            var entries = DatabaseMigrationManifest.GetEntriesForRange(fromVersion, toVersion);
            return RunChecks(entries);
        }

        /// <summary>
        /// Runs all manifest checks for a single schema version.
        /// Called from <see cref="Database.InitDB"/> after each migration step.
        /// </summary>
        public static bool ValidateVersion(int version)
        {
            var entries = DatabaseMigrationManifest.GetEntriesForVersion(version);
            return RunChecks(entries);
        }

        /// <summary>
        /// Executes a set of validation entries and records the outcomes in the journal.
        /// </summary>
        private static bool RunChecks(IEnumerable<DatabaseMigrationManifest.ValidationEntry> entries)
        {
            bool allPassed = true;
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string dbName = Config.DatabaseConfiguration.DatabaseName;

            foreach (var entry in entries)
            {
                ValidationResult result = Check(db, dbName, entry);

                if (result.Passed)
                {
                    Logging.LogKey(Logging.LogType.Information, "process.database",
                        "database.validation_check_passed",
                        null, new[] { result.SchemaVersion.ToString(), result.CheckName });
                }
                else
                {
                    var logLevel = result.IsCritical
                        ? Logging.LogType.Critical
                        : Logging.LogType.Warning;

                    Logging.LogKey(logLevel, "process.database",
                        "database.validation_check_failed",
                        null, new[] { result.SchemaVersion.ToString(), result.CheckName, result.FailureReason ?? "" });

                    if (result.IsCritical) allPassed = false;
                }

                // Write journal entry for each check
                long jId = MigrationJournal.Start(
                    entry.SchemaVersion,
                    MigrationJournal.StepType.Validation,
                    entry.CheckName);
                if (result.Passed)
                    MigrationJournal.Complete(jId);
                else
                    MigrationJournal.Fail(jId, result.FailureReason);
            }

            return allPassed;
        }

        /// <summary>
        /// Evaluates a single validation entry against the live database.
        /// </summary>
        private static ValidationResult Check(Database db, string dbName,
            DatabaseMigrationManifest.ValidationEntry entry)
        {
            try
            {
                // --- Table check ---
                if (entry.Table != null && entry.Column == null && entry.Index == null && entry.View == null)
                {
                    string sql = "SELECT COUNT(*) FROM information_schema.tables " +
                                 "WHERE table_schema = @db AND table_name = @tbl";
                    var p = new Dictionary<string, object> { { "db", dbName }, { "tbl", entry.Table } };
                    DataTable r = db.ExecuteCMD(sql, p);
                    bool exists = Convert.ToInt32(r.Rows[0][0]) > 0;
                    return new ValidationResult
                    {
                        CheckName = entry.CheckName,
                        SchemaVersion = entry.SchemaVersion,
                        IsCritical = entry.IsCritical,
                        Passed = exists,
                        FailureReason = exists ? null : $"Table '{entry.Table}' not found in database '{dbName}'"
                    };
                }

                // --- View check ---
                if (entry.View != null)
                {
                    string sql = "SELECT COUNT(*) FROM information_schema.views " +
                                 "WHERE table_schema = @db AND table_name = @vw";
                    var p = new Dictionary<string, object> { { "db", dbName }, { "vw", entry.View } };
                    DataTable r = db.ExecuteCMD(sql, p);
                    bool exists = Convert.ToInt32(r.Rows[0][0]) > 0;
                    return new ValidationResult
                    {
                        CheckName = entry.CheckName,
                        SchemaVersion = entry.SchemaVersion,
                        IsCritical = entry.IsCritical,
                        Passed = exists,
                        FailureReason = exists ? null : $"View '{entry.View}' not found"
                    };
                }

                // --- Column check ---
                if (entry.Table != null && entry.Column != null)
                {
                    string sql = "SELECT COLUMN_TYPE FROM information_schema.COLUMNS " +
                                 "WHERE TABLE_SCHEMA = @db AND TABLE_NAME = @tbl AND COLUMN_NAME = @col";
                    var p = new Dictionary<string, object>
                    {
                        { "db",  dbName },
                        { "tbl", entry.Table },
                        { "col", entry.Column.Name }
                    };
                    DataTable r = db.ExecuteCMD(sql, p);
                    if (r.Rows.Count == 0)
                    {
                        return new ValidationResult
                        {
                            CheckName = entry.CheckName,
                            SchemaVersion = entry.SchemaVersion,
                            IsCritical = entry.IsCritical,
                            Passed = false,
                            FailureReason = $"Column '{entry.Column.Name}' not found in table '{entry.Table}'"
                        };
                    }

                    if (entry.Column.TypeFamily != null)
                    {
                        string actualType = r.Rows[0]["COLUMN_TYPE"].ToString() ?? "";
                        string actualFamily = actualType.Split('(')[0].Trim().ToLowerInvariant();
                        string expectedFamily = entry.Column.TypeFamily.ToLowerInvariant();
                        if (actualFamily != expectedFamily)
                        {
                            return new ValidationResult
                            {
                                CheckName = entry.CheckName,
                                SchemaVersion = entry.SchemaVersion,
                                IsCritical = entry.IsCritical,
                                Passed = false,
                                FailureReason = $"Column '{entry.Column.Name}' in '{entry.Table}' has type '{actualType}', expected family '{expectedFamily}'"
                            };
                        }
                    }

                    return new ValidationResult
                    {
                        CheckName = entry.CheckName,
                        SchemaVersion = entry.SchemaVersion,
                        IsCritical = entry.IsCritical,
                        Passed = true
                    };
                }

                // --- Index check ---
                if (entry.Table != null && entry.Index != null)
                {
                    string sql = "SELECT COUNT(*) FROM information_schema.STATISTICS " +
                                 "WHERE TABLE_SCHEMA = @db AND TABLE_NAME = @tbl AND INDEX_NAME = @idx";
                    var p = new Dictionary<string, object>
                    {
                        { "db",  dbName },
                        { "tbl", entry.Table },
                        { "idx", entry.Index }
                    };
                    DataTable r = db.ExecuteCMD(sql, p);
                    bool exists = Convert.ToInt32(r.Rows[0][0]) > 0;
                    return new ValidationResult
                    {
                        CheckName = entry.CheckName,
                        SchemaVersion = entry.SchemaVersion,
                        IsCritical = entry.IsCritical,
                        Passed = exists,
                        FailureReason = exists ? null :
                            $"Index '{entry.Index}' not found on table '{entry.Table}'"
                    };
                }

                return new ValidationResult
                {
                    CheckName = entry.CheckName,
                    SchemaVersion = entry.SchemaVersion,
                    IsCritical = entry.IsCritical,
                    Passed = false,
                    FailureReason = "Unrecognised check specification"
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    CheckName = entry.CheckName,
                    SchemaVersion = entry.SchemaVersion,
                    IsCritical = entry.IsCritical,
                    Passed = false,
                    FailureReason = $"Exception during check: {ex.Message}"
                };
            }
        }
    }
}
