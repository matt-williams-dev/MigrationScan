using System.Drawing; // MIG4001 — System.Drawing.Common

namespace LegacySyntax
{
    public class Thumbnailer
    {
        public Bitmap Make(int width, int height) => new Bitmap(width, height);
    }
}
