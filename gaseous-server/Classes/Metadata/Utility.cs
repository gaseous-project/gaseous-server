using System.Reflection;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;

namespace Classes.Metadata.Utility
{
    public static class TableBuilder
    {
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

            // Get the properties of the class
            PropertyInfo[] properties = type.GetProperties();

            // Start building the SQL command
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            // Create the table with the basic structure if it does not exist
            string createTableQuery = $"CREATE TABLE IF NOT EXISTS `{tableName}` (`Id` BIGINT PRIMARY KEY, `dateAdded` DATETIME DEFAULT CURRENT_TIMESTAMP, `lastUpdated` DATETIME DEFAULT CURRENT_TIMESTAMP )";
            db.ExecuteNonQuery(createTableQuery);

            // Loop through each property to add it as a column in the table
            foreach (PropertyInfo property in properties)
            {
                // Get the property name and type
                string columnName = property.Name;
                string columnType = "VARCHAR(255)"; // Default type, can be changed based on property type

                // check if there is a column with the name of the property
                string checkColumnQuery = $"SHOW COLUMNS FROM `{tableName}` LIKE '{columnName}'";
                var result = db.ExecuteCMD(checkColumnQuery);
                if (result.Rows.Count > 0)
                {
                    // Column already exists, skip adding it
                    continue;
                }

                // Convert the property type name to a string
                string propertyTypeName = property.PropertyType.Name;

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

                // Add the column to the table if it does not already exist
                string addColumnQuery = $"ALTER TABLE `{tableName}` ADD COLUMN IF NOT EXISTS `{columnName}` {columnType}";
                Console.WriteLine($"Executing query: {addColumnQuery}");
                db.ExecuteNonQuery(addColumnQuery);
            }
        }
    }
}