using System;
namespace FrontlineMap.Core
{
    // Observed 0.34.1 map: UI/AdditiveMasked, full UVs, identity texture transform.
    // RawImage retains its standard alpha shader and inherits the native RectMask2D.
    public static class MapSurfaceContract
    {
        public static bool SupportsMaterial(bool isDefault, string shader, double scaleX, double scaleY, double offsetX, double offsetY)
            => (isDefault || string.Equals(shader, "UI/AdditiveMasked", StringComparison.Ordinal))
                && ModelSettings.Finite(scaleX) && ModelSettings.Finite(scaleY)
                && ModelSettings.Finite(offsetX) && ModelSettings.Finite(offsetY)
                && Math.Abs(scaleX - 1) < 1e-5 && Math.Abs(scaleY - 1) < 1e-5
                && Math.Abs(offsetX) < 1e-5 && Math.Abs(offsetY) < 1e-5;
    }
}
