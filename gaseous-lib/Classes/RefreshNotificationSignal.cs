namespace gaseous_server.Classes
{
    /// <summary>
    /// Publishes lightweight timestamp signals that clients can poll to detect when
    /// content or metadata changed and a refresh may be required.
    /// </summary>
    public static class RefreshNotificationSignal
    {
        public const string LastContentChangeSetting = "LastContentChange";
        public const string LastMetadataChangeSetting = "LastMetadataChange";
        public const string LastLibraryChangeSetting = "LastLibraryChange";

        public static void MarkContentChanged()
        {
            DateTime now = DateTime.UtcNow;
            Config.SetSetting<DateTime>(LastContentChangeSetting, now);
            Config.SetSetting<DateTime>(LastLibraryChangeSetting, now);
        }

        public static void MarkMetadataChanged()
        {
            DateTime now = DateTime.UtcNow;
            Config.SetSetting<DateTime>(LastMetadataChangeSetting, now);
            Config.SetSetting<DateTime>(LastLibraryChangeSetting, now);
        }
    }
}
