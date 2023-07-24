using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    public enum SkyStates
    {
        Cloudlesss,
        Cloudy,
        Raining,
        Lightning
    }

    public enum SunlightStates
    {
        Dark,
        Rise,
        Light,
        Set
    }

    internal static class WeatherData
    {
        public static int mmhg;
        public static int change;

        public static SkyStates Sky;
        public static SunlightStates Sunlight =>
            TimeInfo.Hour < 5 || TimeInfo.Hour > 20 ? SunlightStates.Dark :
            (TimeInfo.Hour < 6 ? SunlightStates.Rise :
            (TimeInfo.Hour < 19 ? SunlightStates.Light :
            (TimeInfo.Hour < 20 ? SunlightStates.Set :
            SunlightStates.Dark)));

        internal static void Initialize()
        {
            change = 0;
            mmhg = 960;
            if (TimeInfo.Month >= 7 && TimeInfo.Month <= 12)
                mmhg += Utility.Random(1, 50);
            else
                mmhg += Utility.Random(1, 80);

            if (mmhg <= 980) Sky = SkyStates.Lightning;
            else if (mmhg <= 1000) Sky = SkyStates.Raining;
            else if (mmhg <= 1020) Sky = SkyStates.Cloudy;
            else Sky = SkyStates.Cloudlesss;
        }
    }

    public static class TimeInfo
    {
        public static long Minute => (long)((DateTime.Now - new DateTime(2023, 1, 1)).TotalSeconds / (game.PULSE_TICK / game.PULSE_PER_SECOND) * 60) % 60;

        public static long Hours => (long)((DateTime.Now - new DateTime(2023, 1, 1)).TotalSeconds / (game.PULSE_TICK / game.PULSE_PER_SECOND));

        public static long Hour => Hours % 24;

        public static long Days => Hours / 24;

        public static long Day => Days % 35;

        public static long Months => (long)(Days / 35);

        public static long Month => Months % 17;

        public static long Year => Months / 17;

        public static bool IS_NIGHT => Hour <= 6 || Hour >= 20;

        private static string[] _day_name = new string[]
        {
            "the Moon", "the Bull", "Deception", "Thunder", "Freedom",
            "the Great Gods", "the Sun"
        };

        private static string[] _month_name = new string[]
        {
            "Winter", "the Winter Wolf", "the Frost Giant", "the Old Forces",
            "the Grand Struggle", "the Spring", "Nature", "Futility", "the Dragon",
            "the Sun", "the Heat", "the Battle", "the Dark Shades", "the Shadows",
            "the Long Shadows", "the Ancient Darkness", "the Great Evil"
        };

        public static string DayName => _day_name[(Day + 1) % 7];

        public static string MonthName => _month_name[(Month + 1) % 12];
    }
}
