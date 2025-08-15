using System.Reflection;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata.Utility
{
    public static class TableBuilder
    {
        public static void BuildTables()
        {
            BuildTableFromType(typeof(AgeRating));
            BuildTableFromType(typeof(AgeRatingCategory));
            BuildTableFromType(typeof(AgeRatingContentDescriptionV2));
            BuildTableFromType(typeof(AgeRatingOrganization));
            BuildTableFromType(typeof(AlternativeName));
            BuildTableFromType(typeof(Artwork));
            BuildTableFromType(typeof(Character));
            BuildTableFromType(typeof(CharacterGender));
            BuildTableFromType(typeof(CharacterMugShot));
            BuildTableFromType(typeof(CharacterSpecies));
            BuildTableFromType(typeof(Collection));
            BuildTableFromType(typeof(CollectionMembership));
            BuildTableFromType(typeof(CollectionMembershipType));
            BuildTableFromType(typeof(CollectionRelation));
            BuildTableFromType(typeof(CollectionRelationType));
            BuildTableFromType(typeof(CollectionType));
            BuildTableFromType(typeof(Company));
            BuildTableFromType(typeof(CompanyLogo));
            BuildTableFromType(typeof(CompanyStatus));
            BuildTableFromType(typeof(CompanyWebsite));
            BuildTableFromType(typeof(Cover));
            BuildTableFromType(typeof(Event));
            BuildTableFromType(typeof(EventLogo));
            BuildTableFromType(typeof(EventNetwork));
            BuildTableFromType(typeof(ExternalGame));
            BuildTableFromType(typeof(ExternalGameSource));
            BuildTableFromType(typeof(Franchise));
            BuildTableFromType(typeof(Game));
            BuildTableFromType(typeof(GameEngine));
            BuildTableFromType(typeof(GameEngineLogo));
            BuildTableFromType(typeof(GameLocalization));
            BuildTableFromType(typeof(GameMode));
            BuildTableFromType(typeof(GameReleaseFormat));
            BuildTableFromType(typeof(GameStatus));
            BuildTableFromType(typeof(GameTimeToBeat));
            BuildTableFromType(typeof(GameType));
            BuildTableFromType(typeof(GameVersion));
            BuildTableFromType(typeof(GameVersionFeature));
            BuildTableFromType(typeof(GameVersionFeatureValue));
            BuildTableFromType(typeof(GameVideo));
            BuildTableFromType(typeof(Genre));
            BuildTableFromType(typeof(InvolvedCompany));
            BuildTableFromType(typeof(Keyword));
            BuildTableFromType(typeof(Language));
            BuildTableFromType(typeof(LanguageSupport));
            BuildTableFromType(typeof(LanguageSupportType));
            BuildTableFromType(typeof(MultiplayerMode));
            BuildTableFromType(typeof(NetworkType));
            BuildTableFromType(typeof(Platform));
            BuildTableFromType(typeof(PlatformFamily));
            BuildTableFromType(typeof(PlatformLogo));
            BuildTableFromType(typeof(PlatformVersion));
            BuildTableFromType(typeof(PlatformVersionCompany));
            BuildTableFromType(typeof(PlatformVersionReleaseDate));
            BuildTableFromType(typeof(PlatformWebsite));
            BuildTableFromType(typeof(PlayerPerspective));
            BuildTableFromType(typeof(PopularityPrimitive));
            BuildTableFromType(typeof(PopularityType));
            BuildTableFromType(typeof(Region));
            BuildTableFromType(typeof(ReleaseDate));
            BuildTableFromType(typeof(ReleaseDateRegion));
            BuildTableFromType(typeof(ReleaseDateStatus));
            BuildTableFromType(typeof(Screenshot));
            BuildTableFromType(typeof(Theme));
            BuildTableFromType(typeof(Website));
            BuildTableFromType(typeof(WebsiteType));
        }

        /// <summary>
        /// Builds a table from a type definition, or modifies an existing table.
        /// This is used to create or update tables in the database based on the properties of a class.
        /// Updates are limited to adding new columns, as the table structure should not change once created.
        /// If the table already exists, it will only add new columns that are not already present.
        /// This is useful for maintaining a consistent schema across different versions of the application.
        /// The method is generic and can be used with any type that has properties that can be mapped to database columns.
        /// The method does not return any value, but it will throw an exception if there is an error during the table creation or modification process.
        /// </summary>
        /// <param name="type">The type definition of the class for which the table should be built.</param>
        public static void BuildTableFromType(Type type)
        {
            // Get the table name from the class name
            string tableName = type.Name;

            // Start building the SQL command
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            // check rename migration status
            if (Config.ReadSetting<bool>($"RenameMigration_{tableName}", false) == false)
            {
                // rename the table if it exists
                // Check if the table exists
                string checkTableExistsQuery = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'";
                var result = db.ExecuteCMD(checkTableExistsQuery);
                if (Convert.ToInt32(result.Rows[0][0]) > 0)
                {
                    // The table exists, so we will rename it
                    Console.WriteLine($"Table '{tableName}' already exists. Renaming to 'Metadata_{tableName}'...");

                    string renameTableQuery = $"ALTER TABLE `{tableName}` RENAME TO `Metadata_{tableName}`";
                    db.ExecuteNonQuery(renameTableQuery);
                }

                // mark the rename migration as done
                Config.SetSetting($"RenameMigration_{tableName}", true);
            }
            // Update the table name to include the Metadata prefix
            tableName = $"Metadata_{tableName}";

            // Get the properties of the class
            PropertyInfo[] properties = type.GetProperties();

            // Create the table with the basic structure if it does not exist
            string createTableQuery = $"CREATE TABLE IF NOT EXISTS `{tableName}` (`Id` BIGINT PRIMARY KEY, `dateAdded` DATETIME DEFAULT CURRENT_TIMESTAMP, `lastUpdated` DATETIME DEFAULT CURRENT_TIMESTAMP )";
            db.ExecuteNonQuery(createTableQuery);

            // Add the sourceId column if it does not exist
            string addSourceIdQuery = $"ALTER TABLE `{tableName}` ADD COLUMN IF NOT EXISTS `SourceId` INT";
            db.ExecuteNonQuery(addSourceIdQuery);

            // Loop through each property to add it as a column in the table
            foreach (PropertyInfo property in properties)
            {
                // Get the property name and type
                string columnName = property.Name;
                string columnType = "VARCHAR(255)"; // Default type, can be changed based on property type

                // Convert the property type name to a string
                string propertyTypeName = property.PropertyType.Name;
                if (propertyTypeName == "Nullable`1")
                {
                    // If the property is nullable, get the underlying type
                    propertyTypeName = property.PropertyType.GetGenericArguments()[0].Name;
                }

                // Determine the SQL type based on the property type
                switch (propertyTypeName)
                {
                    case "String":
                        columnType = "VARCHAR(255)";
                        break;
                    case "Int32":
                        columnType = "INT";
                        break;
                    case "Int64":
                        columnType = "BIGINT";
                        break;
                    case "Boolean":
                        columnType = "BOOLEAN";
                        break;
                    case "DateTime":
                    case "DateTimeOffset":
                        columnType = "DATETIME";
                        break;
                    case "Double":
                        columnType = "DOUBLE";
                        break;
                    case "IdentityOrValue`1":
                        columnType = "BIGINT";
                        break;
                    case "IdentitiesOrValues`1":
                        columnType = "LONGTEXT";
                        break;
                }

                // check if there is a column with the name of the property
                string checkColumnQuery = $"SHOW COLUMNS FROM `{tableName}` LIKE '{columnName}'";
                var result = db.ExecuteCMD(checkColumnQuery);
                if (result.Rows.Count > 0)
                {
                    // Column already exists, check if the type matches
                    string existingType = result.Rows[0]["Type"].ToString();
                    if (existingType.ToLower().Split("(")[0] != columnType.ToLower().Split("(")[0] && existingType != "text" && existingType != "longtext")
                    {
                        // If the type does not match, we cannot change the column type in MySQL without dropping it first
                        Console.WriteLine($"Column '{columnName}' in table '{tableName}' already exists with type '{existingType}', but expected type is '{columnType}'.");
                        string alterColumnQuery = $"ALTER TABLE `{tableName}` MODIFY COLUMN `{columnName}` {columnType}";
                        Console.WriteLine($"Executing query: {alterColumnQuery}");
                        try
                        {
                            db.ExecuteNonQuery(alterColumnQuery);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error altering column '{columnName}' in table '{tableName}': {ex.Message}");
                        }
                        continue; // Skip this column as we cannot change its type
                    }
                    continue; // Skip this column as it already exists
                }

                // Add the column to the table if it does not already exist
                string addColumnQuery = $"ALTER TABLE `{tableName}` ADD COLUMN IF NOT EXISTS `{columnName}` {columnType}";
                Console.WriteLine($"Executing query: {addColumnQuery}");
                db.ExecuteNonQuery(addColumnQuery);
            }
        }
    }
}