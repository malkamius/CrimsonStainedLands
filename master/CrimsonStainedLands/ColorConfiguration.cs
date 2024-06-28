using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    public static class ColorConfiguration
    {
        /// <summary>
        /// Apply to a DoCommand to prevent the mud from 
        /// escaping input color from its arguments for 
        /// mortals.
        /// </summary>
        public class NoEscapeColor : Attribute
        {

        }

        public enum Keys
        {
            Communication_Tell,
            Communication_Whisper,
            Communication_Say,
            Communication_Yell,
            Communication_GroupTell,
            Communication_Newbie,
            Room_Name,
            Room_Description,
            Room_Exits,
            Room_Exits_Door,
            Holylight_VNum,
            Combat_Damage,
            Reset,
            
        }
        public static Dictionary<Keys, string> DefaultColors = new Dictionary<Keys, string>()
        {
            { Keys.Communication_Tell, "{C" },
            { Keys.Communication_Whisper, "{r" },
            { Keys.Communication_Say, "{Y" },
            { Keys.Communication_Yell, "{R" },
            { Keys.Communication_GroupTell, "{M" },
            { Keys.Room_Name, "{c" },
            { Keys.Room_Description, "{x" },
            { Keys.Room_Exits, "{G" },
            { Keys.Room_Exits_Door, "{W" },
            { Keys.Holylight_VNum, "{C" },
            { Keys.Combat_Damage, "{R" },
            { Keys.Reset, "{x" }
        };

        public static string ColorString(Keys key) => "{=" + key.ToString() + "}";

    }

    public partial class Character
    {
        public Dictionary<ColorConfiguration.Keys,string> ColorConfigurations = new Dictionary<ColorConfiguration.Keys, string>();
        
        public string GetColor(ColorConfiguration.Keys key)
        {
            if (ColorConfigurations.TryGetValue(key, out var color))
                return color;
            else if (ColorConfiguration.DefaultColors.TryGetValue(key, out var defaultColor))
            {
                return defaultColor;
            }
            else
                return "";
        }
    }
}
