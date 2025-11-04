using System;

namespace SocialBeeAR
{
    public static class DoubleExtensions
    {
        public static string Shortened(this double value)
        {            
            var num = Math.Abs(value);
            var sign = (value < 0) ? "-" : "";

            var unit = "";
            var divisor = 1;
            if (num >= 1000000000)
            {
                divisor = 1000000000;
                unit = "B";
            }
            else if (num >= 1000000)
            {
                divisor = 1000000000;
                unit = "M";
            }
            else if (num > 1000)
            {
                divisor = 1000;
                unit = "K";
            }

            return $"{sign}{(num / divisor):F2}{unit}";
        }

        public static double MeterToFeetValue(this double value) {
            // 1 m = 3.28084 ft
            return value * 3.28084;
        }

        public static double MeterToMile(this double value)
        {
            // 1 m = 0.000621371
            return value * 0.000621371;
        }
        
        public static string MeterToStatsString(this double value)
        {

            var stat = new double();
            var unit = " ft";
            // We want to write in feet first, up to 99.99 ft
            // before we write in miles.
            // We want to maintain a four digit display.
            // 1 m = 3.28084 ft
            // 16.09 m = 52.80 ft = 0.01 mi
            // 30.48 m = 100 ft = 0.1894 mi

            if (value < 30.48)  {
                stat = value.MeterToFeetValue();
            }
            else
            {
                stat = value.MeterToMile();
                unit = " mi";
            }

            return (stat < 1 ? $"{stat:F2}" : stat.Shortened()) + unit;
        }
    }
}
