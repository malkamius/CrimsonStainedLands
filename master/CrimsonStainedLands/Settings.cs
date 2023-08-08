using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CrimsonStainedLands.Extensions;

namespace CrimsonStainedLands
{
    public class Settings
    {
        public static int Port { get; private set; } = 4000;
        public static string DataPath { get; private set; } = "..\\..\\data";
        public static string AreasPath { get; private set; } = "..\\..\\data\\areas";
        public static string PlayersPath { get; private set; } = "..\\..\\data\\players";
        public static string GuildsPath{ get; private set; } = "..\\..\\data\\guilds";
        public static string RacesPath { get; private set; } = "..\\..\\data\\races";

        static Settings()
        {
            Load();
        }

        public static void Save()
        {
            var settings = new XElement("Settings", 
                new XAttribute("Port", Port), 
                new XAttribute("MaxPlayersOnlineEver", Game.MaxPlayersOnlineEver),
                new XAttribute("DataPath", DataPath),
                new XAttribute("AreasPath", AreasPath),
                new XAttribute("PlayersPath", PlayersPath),
                new XAttribute("GuildsPath", GuildsPath),
                new XAttribute("RacesPath", RacesPath));
            settings.Save("Settings.xml");
        }

        public static void Load()
        {
            if (!System.IO.File.Exists("Settings.xml"))
            {
                Save();
            }
            else
            {
                var settings = XElement.Load("Settings.xml");
                Game.MaxPlayersOnlineEver = settings.GetAttributeValueInt("MaxPlayersOnlineEver", 0);
                Port = settings.GetAttributeValueInt("Port", 4000);
                DataPath = settings.GetAttributeValue("DataPath", "..\\..\\data");
                AreasPath = settings.GetAttributeValue("AreasPath", "..\\..\\data\\areas");
                PlayersPath = settings.GetAttributeValue("PlayersPath", "..\\..\\data\\players");
                GuildsPath = settings.GetAttributeValue("GuildsPath", "..\\..\\data\\guilds");
                RacesPath = settings.GetAttributeValue("RacesPath", "..\\..\\data\\races");
            }
        }
    }
}
