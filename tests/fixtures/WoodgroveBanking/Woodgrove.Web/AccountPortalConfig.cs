using System.Configuration;

namespace Woodgrove.Web
{
    public static class AccountPortalConfig
    {
        public static string DefaultAccount =>
            ConfigurationManager.AppSettings["DefaultAccount"] ?? "00000000";

        public static string StatementArchivePath =>
            ConfigurationManager.AppSettings["StatementArchivePath"] ?? @"D:\Archive";
    }
}
