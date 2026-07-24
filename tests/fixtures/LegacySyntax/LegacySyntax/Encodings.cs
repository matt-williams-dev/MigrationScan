using System.Text;

namespace LegacySyntax
{
    public class Encodings
    {
        // MIG8002 — Encoding.Default (ANSI on Framework, UTF-8 on modern .NET).
        public byte[] ToDefaultBytes(string value) => Encoding.Default.GetBytes(value);

        // MIG8003 — code-page encoding needs CodePagesEncodingProvider registration.
        public Encoding Ansi() => Encoding.GetEncoding(1252);

        // Not flagged: a Unicode name is always available.
        public Encoding Utf8() => Encoding.GetEncoding("utf-8");

        // Not flagged: 65001 is UTF-8 by code-page number (always available).
        public Encoding Utf8ByNumber() => Encoding.GetEncoding(65001);
    }
}
