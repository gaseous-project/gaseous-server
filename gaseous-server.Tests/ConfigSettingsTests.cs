using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using gaseous_server.Classes;
using Xunit;

namespace gaseous_server.Tests
{
    public class ConfigSettingsTests
    {
        private static Dictionary<string, object> BuildSettingWriteParameters<T>(string settingName, T value)
        {
            MethodInfo method = typeof(Config).GetMethod("BuildSettingWriteParameters", BindingFlags.NonPublic | BindingFlags.Static)!;
            MethodInfo genericMethod = method.MakeGenericMethod(typeof(T));
            return (Dictionary<string, object>)genericMethod.Invoke(null, new object?[] { settingName, value })!;
        }

        private static object GetStoredSettingValue(DataRow row)
        {
            MethodInfo method = typeof(Config).GetMethod("GetStoredSettingValue", BindingFlags.NonPublic | BindingFlags.Static)!;
            return method.Invoke(null, new object[] { row })!;
        }

        private static T ConvertSettingValue<T>(object value)
        {
            MethodInfo method = typeof(Config).GetMethod("ConvertSettingValue", BindingFlags.NonPublic | BindingFlags.Static)!;
            MethodInfo genericMethod = method.MakeGenericMethod(typeof(T));
            return (T)genericMethod.Invoke(null, new[] { value })!;
        }

        private static DataRow CreateSettingsRow(int valueType, object? value, object? valueDate)
        {
            DataTable table = new DataTable();
            table.Columns.Add("ValueType", typeof(int));
            table.Columns.Add("Value", typeof(object));
            table.Columns.Add("ValueDate", typeof(object));

            DataRow row = table.NewRow();
            row["ValueType"] = valueType;
            row["Value"] = value ?? DBNull.Value;
            row["ValueDate"] = valueDate ?? DBNull.Value;
            table.Rows.Add(row);

            return row;
        }

        [Fact]
        public void BuildSettingWriteParameters_String_StoresInValueColumn()
        {
            Dictionary<string, object> parameters = BuildSettingWriteParameters("ServerLanguage", "en-GB");

            Assert.Equal("ServerLanguage", parameters["SettingName"]);
            Assert.Equal(0, parameters["ValueType"]);
            Assert.Equal("en-GB", parameters["Value"]);
            Assert.Equal(DBNull.Value, parameters["ValueDate"]);
        }

        [Fact]
        public void BuildSettingWriteParameters_Int_StoresInValueColumn()
        {
            Dictionary<string, object> parameters = BuildSettingWriteParameters("MaxPlayers", 4);

            Assert.Equal(0, parameters["ValueType"]);
            Assert.Equal(4, parameters["Value"]);
            Assert.Equal(DBNull.Value, parameters["ValueDate"]);
        }

        [Fact]
        public void BuildSettingWriteParameters_Bool_StoresInValueColumn()
        {
            Dictionary<string, object> parameters = BuildSettingWriteParameters("FeatureEnabled", true);

            Assert.Equal(0, parameters["ValueType"]);
            Assert.True((bool)parameters["Value"]);
            Assert.Equal(DBNull.Value, parameters["ValueDate"]);
        }

        [Fact]
        public void BuildSettingWriteParameters_DateTime_StoresInValueDateColumn()
        {
            DateTime value = new DateTime(2026, 4, 10, 12, 30, 45, DateTimeKind.Utc);
            Dictionary<string, object> parameters = BuildSettingWriteParameters("LastLibraryChange", value);

            Assert.Equal(1, parameters["ValueType"]);
            Assert.Equal(DBNull.Value, parameters["Value"]);
            Assert.Equal(value, parameters["ValueDate"]);
        }

        [Fact]
        public void BuildSettingWriteParameters_NullableDateTimeWithValue_StoresInValueDateColumn()
        {
            DateTime? value = new DateTime(2026, 4, 10, 12, 30, 45, DateTimeKind.Utc);
            Dictionary<string, object> parameters = BuildSettingWriteParameters("LastMetadataChange", value);

            Assert.Equal(1, parameters["ValueType"]);
            Assert.Equal(DBNull.Value, parameters["Value"]);
            Assert.Equal(value, parameters["ValueDate"]);
        }

        [Fact]
        public void BuildSettingWriteParameters_NullableDateTimeWithoutValue_StoresNullValueDate()
        {
            DateTime? value = null;
            Dictionary<string, object> parameters = BuildSettingWriteParameters("LastMetadataChange", value);

            Assert.Equal(1, parameters["ValueType"]);
            Assert.Equal(DBNull.Value, parameters["Value"]);
            Assert.Equal(DBNull.Value, parameters["ValueDate"]);
        }

        [Fact]
        public void GetStoredSettingValue_WhenValueTypeIsDate_ReturnsValueDate()
        {
            DateTime expected = new DateTime(2026, 4, 10, 13, 00, 00, DateTimeKind.Utc);
            DataRow row = CreateSettingsRow(1, "legacy-string-value", expected);

            object storedValue = GetStoredSettingValue(row);

            Assert.Equal(expected, storedValue);
            Assert.Equal(expected, ConvertSettingValue<DateTime?>(storedValue));
        }

        [Fact]
        public void GetStoredSettingValue_WhenValueTypeIsStandard_ReturnsValue()
        {
            DataRow row = CreateSettingsRow(0, "value-from-text-column", new DateTime(2026, 4, 10, 13, 00, 00, DateTimeKind.Utc));

            object storedValue = GetStoredSettingValue(row);

            Assert.Equal("value-from-text-column", storedValue);
            Assert.Equal("value-from-text-column", ConvertSettingValue<string>(storedValue));
        }
    }
}