using System.Configuration;

namespace Inner
{
    // If Outer wrongly absorbed this nested-project file, it would raise MIG5001.
    public static class Config
    {
        public static string Value => ConfigurationManager.AppSettings["Key"];
    }
}
