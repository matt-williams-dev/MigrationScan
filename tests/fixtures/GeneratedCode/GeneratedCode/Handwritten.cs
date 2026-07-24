using Microsoft.Win32;

namespace GeneratedCode
{
    // Hand-written: this Registry use IS flagged (MIG4002).
    public static class Handwritten
    {
        public static object Read() => Registry.CurrentUser.OpenSubKey("SOFTWARE\\Contoso");
    }
}
