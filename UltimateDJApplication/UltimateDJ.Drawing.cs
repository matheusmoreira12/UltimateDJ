﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

namespace UltimateDJ.Drawing
{
    static class ColorExtension
    {
        /// <summary>Blends the specified colors together.</summary>
        /// <param name="backColor">Color to blend the other color onto.</param>
        /// <param name="color">Color to blend onto the background color.</param>
        /// <param name="amount">How much of <paramref name="color"/> to keep,
        /// “on top of” <paramref name="backColor"/>.</param>
        /// <returns>The blended colors.</returns>
        public static Color Blend(this Color backColor, Color color, double amount = .5)
        {
            byte r = (byte)(color.R * amount + backColor.R * (1 - amount));
            byte g = (byte)(color.G * amount + backColor.G * (1 - amount));
            byte b = (byte)(color.B * amount + backColor.B * (1 - amount));
            byte a = (byte)(color.A * amount + backColor.A * (1 - amount));
            return Color.FromArgb(a, r, g, b);
        }

        public static Color Spectrum(this Color themeColor, Color[] pallete)
        {
            return new Color();
        }
    }
}
