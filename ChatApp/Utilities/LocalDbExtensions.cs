namespace ChatApp.Utilities
{
    public static class LocalDbExtensions
    {
        public static string RetrieveSecureString(string key)
        {
            return SecureStorage.GetAsync(key).Result ?? string.Empty;
        }

        public static void SaveSecureString(string key, string jsonString)
        {
            SecureStorage.SetAsync(key, jsonString).GetAwaiter().GetResult();
        }

        public static void RemoveSecureString(string key)
        {
            SecureStorage.Remove(key);
        }

        public static string RetrievePreferences(string key)
        {
            return Preferences.Get(key, string.Empty);
        }

        public static void SaveJsonPreferences(string key, string jsonString)
        {
            Preferences.Set(key, jsonString);
        }

        public static void RemovePreferences(string key)
        {
            Preferences.Remove(key);
        }
    }
}
