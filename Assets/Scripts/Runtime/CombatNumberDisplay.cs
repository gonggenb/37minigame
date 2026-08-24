using System;
using System.Globalization;

namespace WuxiaRoguelite.Runtime
{
    /// <summary>
    /// Converts compact gameplay values into larger presentation values without
    /// changing combat balance, timings, health loss, or progression formulas.
    /// </summary>
    public static class CombatNumberDisplay
    {
        public const float Scale = 1000f;

        public static long ToDisplayValue(float gameplayValue)
        {
            return (long)Math.Round(gameplayValue * Scale, MidpointRounding.AwayFromZero);
        }

        public static string Format(float gameplayValue)
        {
            return ToDisplayValue(gameplayValue).ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string FormatSigned(float gameplayValue)
        {
            long value = ToDisplayValue(gameplayValue);
            string formatted = value.ToString("N0", CultureInfo.InvariantCulture);
            return value > 0L ? "+" + formatted : formatted;
        }
    }
}
