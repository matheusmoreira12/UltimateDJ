using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace UltimateDJ.Fonts
{
    static class PrivateFontCollectionExtension
    {
        public static FontFamily ByName(this PrivateFontCollection collection, string name)
        {
            var family = collection.Families.FirstOrDefault(e => e.Name.ToLower() == name.ToLower());
            return family == null ? FontFamily.GenericSansSerif : family;
        }
    }

    static class Fonts
    {
        public static PrivateFontCollection Collection;
        private static unsafe void addFontToCollection(byte[] buffer)
        {
            fixed (byte* ptr = buffer)
                Collection.AddMemoryFont(new IntPtr(ptr), buffer.Length);
        }
        static Fonts()
        {
            Collection = new PrivateFontCollection();
            addFontToCollection(UltimateDJApplication.Properties.Resources.Arial_Rounded_MT_Bold_Regular);
            addFontToCollection(UltimateDJApplication.Properties.Resources.PRIMETIME);
        }
    }
}
