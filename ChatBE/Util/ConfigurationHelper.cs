namespace ChatBE.Util
{
    public static class ConfigurationHelper
    {
        private static IConfiguration? _configuration;

        public static void SetConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string GetConfigValue(string key)
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException(
                    "Configuration has not been set. Call SetConfiguration before using GetConfigValue."
                );
            }

            return _configuration[key] ?? "";
        }
    }
}
