using System;
namespace FrontlineMap.Core
{
    // Presentation-only RGB mapping; model coverage and classification remain unchanged.
    public sealed class OverlayAppearance
    {
        public readonly string Friendly, Enemy, Balanced, Front;
        private readonly Pixel friendly, enemy, balanced, front;
        public static readonly OverlayAppearance Default = new OverlayAppearance("419BF5", "F05A46", "B182D7", "F0E19B");
        public OverlayAppearance(string friendly, string enemy, string balanced, string front)
        {
            this.friendly = Parse(friendly); this.enemy = Parse(enemy); this.balanced = Parse(balanced); this.front = Parse(front);
            Friendly = Hex(this.friendly); Enemy = Hex(this.enemy); Balanced = Hex(this.balanced); Front = Hex(this.front);
        }
        private static Pixel Parse(string value)
        {
            if (value == null) throw new ArgumentException("Color must be six hexadecimal RGB digits.");
            value = value.Trim(); if (value.StartsWith("#", StringComparison.Ordinal)) value = value.Substring(1);
            if (value.Length != 6 || !uint.TryParse(value, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out var n)) throw new ArgumentException("Color must be six hexadecimal RGB digits.");
            return new Pixel((byte)(n >> 16), (byte)(n >> 8), (byte)n, 255, PixelKind.Unsupported);
        }
        private static string Hex(Pixel p) => p.R.ToString("X2") + p.G.ToString("X2") + p.B.ToString("X2");
        public Pixel Apply(Pixel p)
        {
            Pixel color;
            switch (p.Kind)
            {
                case PixelKind.Friendly: color = friendly; break;
                case PixelKind.Enemy: color = enemy; break;
                case PixelKind.Balanced: color = balanced; break;
                case PixelKind.Front: color = front; break;
                default: return p;
            }
            return new Pixel(color.R, color.G, color.B, p.A, p.Kind);
        }
    }
}
