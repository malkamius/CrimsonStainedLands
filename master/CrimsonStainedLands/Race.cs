using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{

    public class Race
    {
        public static List<Race> Races = new List<Race>();

        public string name;
        public bool isPCRace;
        public List<ActFlags> act = new List<ActFlags>();
        public List<PartFlags> parts = new List<PartFlags>();
        public List<FormFlags> form = new List<FormFlags>();
        //public List<ActFlags> offense = new List<ActFlags>();
        public List<WeaponDamageTypes> ImmuneFlags = new List<WeaponDamageTypes>();
        public List<WeaponDamageTypes> VulnerableFlags = new List<WeaponDamageTypes>();
        public List<WeaponDamageTypes> ResistFlags = new List<WeaponDamageTypes>();
        public List<AffectFlags> affects = new List<AffectFlags>();
        public PhysicalStats Stats = new PhysicalStats(20, 20, 20, 20, 20, 20);
        public PhysicalStats MaxStats = new PhysicalStats(25, 25, 25, 25, 25, 25);
        public CharacterSize Size = CharacterSize.Medium;
        public bool CanSpeak = false;
        public bool HasCoins = false;
        public static Race GetRace(string raceName)
        {
            foreach (var race in Races)
            {
                if (race.name.ToLower() == raceName.ToLower())
                    return race;
            }
            Game.log("Failed to find race " + raceName);
            return null;
        }

        public static void SaveRaces()
        {
            
            foreach (var race in Races)
            {
                var raceElement = new XElement("Race");
                raceElement.Add(new XElement("Name", race.name));
                raceElement.Add(new XElement("PcRace", race.isPCRace.ToString()));
                raceElement.Add(new XElement("Act", string.Join(" ", race.act)));
                raceElement.Add(new XElement("Aff", string.Join(" ", race.affects)));
                //raceElement.Add(new XElement("Offense", string.Join(" ", race.offense)));
                raceElement.Add(new XElement("Immune", string.Join(" ", from f in race.ImmuneFlags select f.ToString())));
                raceElement.Add(new XElement("Resist", string.Join(" ", from f in race.ResistFlags select f.ToString())));;
                raceElement.Add(new XElement("Vulnerable", string.Join(" ", from f in race.VulnerableFlags select f.ToString())));
                raceElement.Add(new XElement("Form", string.Join(" ", race.form)));
                raceElement.Add(new XElement("Part", string.Join(" ", race.parts)));
                raceElement.Add(race.Stats.Element("Stats"));
                raceElement.Add(race.MaxStats.Element("MaxStats"));
                raceElement.Add(new XElement("Size", race.Size.ToString()));
                raceElement.Add(new XAttribute("CanSpeak", race.CanSpeak));
                raceElement.Add(new XAttribute("HasCoins", race.HasCoins));
                if (!Directory.Exists(Settings.RacesPath))
                    Directory.CreateDirectory(Settings.RacesPath);
                raceElement.Save(Settings.RacesPath + "\\" + race.name + ".xml");
            }
        }

        public Race(string file)
        {
            XElement RaceElement = XElement.Load(file);
            name = RaceElement.GetElement("name").Value;
            if (RaceElement.HasElement("ispcrace"))
                bool.TryParse(RaceElement.GetElement("ispcrace").Value, out isPCRace);

            this.act.AddRange(Utility.LoadFlagList<ActFlags>(RaceElement.GetElementValue("Act")));

            this.parts.AddRange(Utility.LoadFlagList<PartFlags>(RaceElement.GetElementValue("Part")));

            this.form.AddRange(Utility.LoadFlagList<FormFlags>(RaceElement.GetElementValue("Form")));

            this.affects.AddRange(Utility.LoadFlagList<AffectFlags>(RaceElement.GetElementValue("Aff")));

            this.ImmuneFlags.AddRange(Utility.LoadFlagList<WeaponDamageTypes>(RaceElement.GetElementValue("Immune")));
            this.VulnerableFlags.AddRange(Utility.LoadFlagList<WeaponDamageTypes>(RaceElement.GetElementValue("Vulnerable")));
            this.ResistFlags.AddRange(Utility.LoadFlagList<WeaponDamageTypes>(RaceElement.GetElementValue("Resist")));
            this.CanSpeak = RaceElement.GetAttributeValue("CanSpeak", "false") == "true";
            this.HasCoins = RaceElement.GetAttributeValue("HasCoins", "false") == "true";
            if (form.ISSET(FormFlags.sentient))
            {
                CanSpeak = true;
                HasCoins = true;
            }
            //this.offense.AddRange(utility.LoadFlagList<ActFlags>(RaceElement.GetElementValue("Offense")));
            if (RaceElement.HasElement("Stats"))
            {
                Stats = new PhysicalStats(RaceElement.Element("Stats"));
            }

            if (RaceElement.HasElement("MaxStats"))
            {
                MaxStats = new PhysicalStats(RaceElement.Element("MaxStats"));
            }

            Utility.GetEnumValue<CharacterSize>(RaceElement.GetElementValue("Size", "Medium"), ref Size, CharacterSize.Medium);
        }

        public static void LoadRaces()
        {
            Races.Clear();

            var loadedRaces = new List<Race>();
            foreach (var file in Directory.GetFiles(Settings.RacesPath, "*.xml"))
            {
                var race = new Race(file);

                loadedRaces.Add(race);
            }
            Races.Clear();
            Races.AddRange(loadedRaces);
            
        }

        static Race()
        {
            
        }
    }


}
