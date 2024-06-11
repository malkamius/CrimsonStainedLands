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
        public static int Port { get; set; } = 4000;
        public static int SSLPort { get; set; } = 4001;
        public static int SSHPort { get; set; } = 4002;
        public static string DataPath { get; set; } = "data";
        public static string AreasPath { get; set; } = "data/areas";
        public static string PlayersPath { get; set; } = "data/players";
        public static string NotesPath { get; set; } = "data/";
        public static string GuildsPath{ get; set; } = "data/guilds";
        public static string RacesPath { get; set; } = "data/races";

        static Settings()
        {
            Load();
        }

        public static void Save()
        {
            var settings = new XElement("Settings", 
                new XAttribute("Port", Port),
                new XAttribute("SSLPort", SSLPort),
                new XAttribute("SSHPort", SSHPort),
                new XAttribute("MaxPlayersOnlineEver", Game.MaxPlayersOnlineEver),
                new XAttribute("DataPath", DataPath),
                new XAttribute("AreasPath", AreasPath),
                new XAttribute("PlayersPath", PlayersPath),
                new XAttribute("NotesPath", NotesPath),
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
                SSLPort = settings.GetAttributeValueInt("SSLPort", 4001);
                SSHPort = settings.GetAttributeValueInt("SSHPort", 4002);
                DataPath = settings.GetAttributeValue("DataPath", "data");
                AreasPath = settings.GetAttributeValue("AreasPath", "data/areas");
                PlayersPath = settings.GetAttributeValue("PlayersPath", "data/players");
                NotesPath = settings.GetAttributeValue("NotesPath", "data/");
                GuildsPath = settings.GetAttributeValue("GuildsPath", "data/guilds");
                RacesPath = settings.GetAttributeValue("RacesPath", "data/races");
            }
        }
    }
}
