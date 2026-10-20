using System;
namespace FrontlineMap.Core
{
    public sealed class ModelSettings
    {
        public readonly TacticalSettings Tactical;
        public readonly int Resolution;
        public readonly double Radius, Interval, Opacity, SupportThreshold, Epsilon, BudgetMilliseconds, MaximumAge, LineWidthPixels;
        public ModelSettings(int resolution, double radius, double interval, double opacity, double supportThreshold, double epsilon, double budgetMilliseconds, double maximumAge, double lineWidthPixels = 3, TacticalSettings tactical = null)
        {
            if (resolution < 8 || resolution > 2048 || !Positive(radius) || radius > 1e9 || !Positive(interval) || !Finite(opacity) || opacity < 0 || opacity > 1 || !Positive(supportThreshold) || !Positive(epsilon) || !Positive(budgetMilliseconds) || budgetMilliseconds > 20 || !Positive(maximumAge) || maximumAge < interval || !Positive(lineWidthPixels) || lineWidthPixels > 20)
                throw new ArgumentOutOfRangeException("settings", "Invalid finite range or age < interval");
            checked
            {
                var cells = resolution * resolution;
                if (cells > 4194304)
                    throw new ArgumentOutOfRangeException("resolution");
            }
            Tactical = tactical ?? new TacticalSettings();
            Resolution = resolution;
            Radius = radius;
            Interval = interval;
            Opacity = opacity;
            SupportThreshold = supportThreshold;
            Epsilon = epsilon;
            BudgetMilliseconds = budgetMilliseconds;
            MaximumAge = maximumAge;
            LineWidthPixels = lineWidthPixels;
        }
        public static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
        public static bool Positive(double v) => Finite(v) && v > 0;
    }
}
