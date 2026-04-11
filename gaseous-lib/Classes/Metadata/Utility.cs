using System.Data;
using System.Reflection;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata.Utility
{
    public static class MetadataTableBuilder
    {
        public static async Task BuildTableFromType(string databaseName, string prefix, Type type, string overrideId = "", string customColumnIndexes = "")
        {
            // Get the table name from the class name
            string tableName = type.Name;
            if (!string.IsNullOrEmpty(prefix))
            {
                tableName = prefix + "_" + tableName;
            }

            // Ensure the table name is valid for MySQL
            tableName = tableName.Replace(" ", "_").Replace("-", "_").Replace(".", "_");

            // create the database if it does not exist
            string createDatabaseQuery = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`";
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            db.ExecuteNonQuery(createDatabaseQuery);

            // Get the properties of the class
            PropertyInfo[] properties = type.GetProperties();

            // Create the table with the basic structure if it does not exist
            string createTableQuery = $"CREATE TABLE IF NOT EXISTS `{databaseName}`.`{tableName}` (`Id` BIGINT PRIMARY KEY, `dateAdded` DATETIME DEFAULT CURRENT_TIMESTAMP, `lastUpdated` DATETIME DEFAULT CURRENT_TIMESTAMP )";
            if (!string.IsNullOrEmpty(overrideId))
            {
                // If an override ID is provided, use it as the primary key - don't create it now as the field might not exist yet
                createTableQuery = $"CREATE TABLE IF NOT EXISTS `{databaseName}`.`{tableName}` (`dateAdded` DATETIME DEFAULT CURRENT_TIMESTAMP, `lastUpdated` DATETIME DEFAULT CURRENT_TIMESTAMP )";
            }
            db.ExecuteNonQuery(createTableQuery);

            // Loop through each property to add it as a column in the table
            foreach (PropertyInfo property in properties)
            {
                // skip if the property is marked with the Models.NoDatabaseAttribute attribute
                if (property.GetCustomAttribute<Models.NoDatabaseAttribute>() != null)
                {
                    continue;
                }

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

                // if property is a class, check if that class a property named "Id". If it does, this column will be a foreign key. If it does not, this column will be a longtext.
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    PropertyInfo? idProperty = property.PropertyType
                        .GetProperties()
                        .FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
                    if (idProperty != null)
                    {
                        // This is a foreign key reference
                        columnType = "BIGINT"; // Assuming Id is of type long
                    }
                    else
                    {
                        // This is a longtext column
                        columnType = "LONGTEXT";
                    }
                }
                else
                {
                    // Determine the SQL type based on the property type
                    switch (propertyTypeName)
                    {
                        case "String":
                            if (columnName.ToLower() == "description" || columnName.ToLower() == "notes" || columnName.ToLower() == "comments" || columnName.ToLower() == "details" || columnName.ToLower() == "summary" || columnName.ToLower() == "content" || columnName.ToLower() == "text" || columnName.ToLower() == "body" || columnName.ToLower() == "message" || columnName.ToLower() == "info" || columnName.ToLower() == "data" || columnName.ToLower() == "deck" || columnName.ToLower() == "aliases")
                            {
                                columnType = "LONGTEXT"; // Use TEXT for longer strings
                            }
                            else
                            {
                                columnType = "VARCHAR(255)";
                            }
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
                        case "Float":
                        case "Single":
                            columnType = "FLOAT";
                            break;
                        case "IdentityOrValue`1":
                            columnType = "BIGINT";
                            break;
                        case "IdentitiesOrValues`1":
                            columnType = "LONGTEXT";
                            break;
                        case "AgeRestrictionGroupings":
                            columnType = "INT";
                            break;
                    }
                }

                // check if there is a column with the name of the property
                string checkColumnQuery = $"SHOW COLUMNS FROM `{databaseName}`.`{tableName}` LIKE '{columnName}'";
                var result = db.ExecuteCMD(checkColumnQuery);
                if (result.Rows.Count > 0)
                {
                    // Column already exists, check if the type matches
                    string existingType = result.Rows[0]["Type"].ToString();
                    if (existingType.ToLower().Split("(")[0] != columnType.ToLower().Split("(")[0] && existingType != "text" && existingType != "longtext")
                    {
                        // check for BOOLEAN vs TINYINT(1) mismatch
                        if (existingType.ToLower() == "tinyint(1)" && columnType.ToLower() == "boolean")
                        {
                            continue; // consider this a match
                        }

                        // If the type does not match, we cannot change the column type in MySQL without dropping it first
                        Console.WriteLine($"Column '{columnName}' in table '{tableName}' already exists with type '{existingType}', but expected type is '{columnType}'.");
                        string alterColumnQuery = $"ALTER TABLE `{databaseName}`.`{tableName}` MODIFY COLUMN `{columnName}` {columnType}";
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
                string addColumnQuery = $"ALTER TABLE `{databaseName}`.`{tableName}` ADD COLUMN IF NOT EXISTS `{columnName}` {columnType}";
                Console.WriteLine($"Executing query: {addColumnQuery}");
                db.ExecuteNonQuery(addColumnQuery);
            }

            if (!string.IsNullOrEmpty(overrideId))
            {
                // If an override ID is provided, add it as the primary key
                // check if the primary key already exists - if the columns are not the same, we need to drop the primary key first
                string checkPrimaryKeyQuery = $"SHOW KEYS FROM `{databaseName}`.`{tableName}` WHERE Key_name = 'PRIMARY'";
                var primaryKeyResult = db.ExecuteCMD(checkPrimaryKeyQuery);
                string[] overrideIdFields = overrideId.Split(',').Select(f => f.Trim()).ToArray();
                if (primaryKeyResult.Rows.Count > 0)
                {
                    // Primary key already exists, check if the fields match
                    string existingPrimaryKey = "";
                    foreach (DataRow row in primaryKeyResult.Rows)
                    {
                        if (existingPrimaryKey != "")
                        {
                            existingPrimaryKey += ",";
                        }
                        existingPrimaryKey += row["Column_name"].ToString();
                    }
                    string[] existingPrimaryKeyFields = existingPrimaryKey.Split(',').Select(f => f.Trim()).ToArray();
                    if (!overrideIdFields.SequenceEqual(existingPrimaryKeyFields))
                    {
                        // If the primary key fields do not match, we need to drop the primary key first
                        string dropPrimaryKeyQuery = $"ALTER TABLE `{databaseName}`.`{tableName}` DROP PRIMARY KEY";
                        Console.WriteLine($"Executing query: {dropPrimaryKeyQuery}");
                        db.ExecuteNonQuery(dropPrimaryKeyQuery);
                    }
                }

                // Add the override ID as the primary key
                string addPrimaryKeyQuery = $"ALTER TABLE `{databaseName}`.`{tableName}` ADD PRIMARY KEY ({overrideId})";
                Console.WriteLine($"Executing query: {addPrimaryKeyQuery}");
                db.ExecuteNonQuery(addPrimaryKeyQuery);
            }

            // Add custom indexes if provided
            if (!string.IsNullOrEmpty(customColumnIndexes))
            {
                string[] indexes = customColumnIndexes.Split(',');
                foreach (string index in indexes)
                {
                    string trimmedIndex = index.Trim();
                    if (!string.IsNullOrEmpty(trimmedIndex))
                    {
                        string checkIndexQuery = $"SHOW INDEX FROM `{databaseName}`.`{tableName}` WHERE Key_name = 'idx_{trimmedIndex}'";
                        var indexResult = db.ExecuteCMD(checkIndexQuery);
                        if (indexResult.Rows.Count == 0)
                        {
                            string createIndexQuery = $"CREATE INDEX `idx_{trimmedIndex}` ON `{databaseName}`.`{tableName}` (`{trimmedIndex}`)";
                            Console.WriteLine($"Executing query: {createIndexQuery}");
                            db.ExecuteNonQuery(createIndexQuery);
                        }
                    }
                }
            }
        }
    }
}