using System.Configuration;

namespace LegacyWebForms
{
    public static class AppConfig
    {
        public static string Environment =>
            ConfigurationManager.AppSettings["Environment"] ?? "Development";
    }
}
