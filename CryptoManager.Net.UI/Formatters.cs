using MudBlazor;
using System.Globalization;

namespace CryptoManager.Net.UI
{
    public class Formatters
    {
        public static decimal? FormatPrice(decimal? value)
        {
            if (value == null || value == 0)
                return null;

            if (value > 9999)
                return Math.Round(value.Value, 2);

            if (value > 999)
                return Math.Round(value.Value, 3);

            if (value > 99)
                return Math.Round(value.Value, 4);

            if (value > 9)
                return Math.Round(value.Value, 5);

            if (value > 0.9m)
                return Math.Round(value.Value, 6);

            if (value > 0.09m)
                return Math.Round(value.Value, 7);

            return Math.Round(value.Value, 8);
        }

        public static decimal FormatQuantity(decimal? value)
        {
            if (value == null || value == 0)
                return 0;

            if (value > 99999)
                return Math.Round(value.Value, 0);

            if (value > 9999)
                return Math.Round(value.Value, 1);

            if (value > 999)
                return Math.Round(value.Value, 2);

            if (value > 99)
                return Math.Round(value.Value, 3);

            if (value > 9)
                return Math.Round(value.Value, 4);

            if (value > 0.9m)
                return Math.Round(value.Value, 5);

            if (value > 0.09m)
                return Math.Round(value.Value, 6);

            return Math.Round(value.Value, 8);
        }

        public static string ToLocalTime(DateTime? time)
        {
            if (time == null)
                return string.Empty;

            return time.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static decimal Normalize(decimal? value)
        {
            return (value ?? 0) / 1.000000000000000000000000000000000m;
        }
    }
}
