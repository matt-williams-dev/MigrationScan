using System.Web;

namespace LegacyLibrary
{
    public static class HtmlEncoder
    {
        public static string Encode(string raw) => HttpUtility.HtmlEncode(raw);
    }
}
