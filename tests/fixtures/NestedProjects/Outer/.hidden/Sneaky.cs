using System.Configuration;

namespace Outer.Hidden
{
    // Lives in a hidden folder. If Outer scanned hidden directories, this would raise
    // MIG5001. (A non-gitignored dot-directory so the fixture survives in the repo.)
    public static class Sneaky
    {
        public static string Value => ConfigurationManager.AppSettings["Sneaky"];
    }
}
