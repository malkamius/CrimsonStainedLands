using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public enum PartFlags
    {
        Head = 1,
        Arms = 2,
        Legs,
        Heart,
        Brains,
        Guts,
        Hands,
        Feet,
        Fingers,
        Ear,
        Eye,
        LongTongue,
        EyeStalks,
        Tentacles,
        Fins,
        Wings,
        Tail,
        Claws,
        Fangs,
        Horns,
        Scales,
        Tusks,
        Pincers,
        Shell,
        Mandible,
        Bones
    }

    public enum FormFlags
    {
        edible,
        poison,
        magical,
        instant_decay,
        other,
        animal,
        sentient,
        undead,
        construct,
        mist,
        intangible,
        biped,
        centaur,
        insect,
        spider,
        crustacean,
        worm,
        blob,
        mammal,
        bird,
        reptile,
        snake,
        dragon,
        amphibian,
        fish,
        cold_blood,
        skeleton,
        ghoul,
        zombie
    }

    public class PcRace
    {
        public static List<PcRace> PcRaces = new List<PcRace>();

        public string name;
        public List<Alignment> alignments = new List<Alignment>();
        public List<Ethos> ethosChoices = new List<Ethos>();
        public bool isPCRace;
        public List<PartFlags> parts = new List<PartFlags>();
        public PhysicalStats Stats = new PhysicalStats(20,20,20,20,20,20);
        public PhysicalStats MaxStats = new PhysicalStats(25,25,25,25,25,25);
        public CharacterSize Size = CharacterSize.Medium;
        public Race BaseRace;

        public static PcRace GetRace(string raceName, bool StringPrefix = false)
        {
            foreach (var race in PcRaces)
            {
                if (race.name.ToLower() == raceName.ToLower() || (StringPrefix && race.name.StringPrefix(raceName)))
                    return race;
            }
            Game.log("Failed to find race " + raceName);
            return null;
        }

        public static void SaveRaces()
        {
            XElement element = new XElement("Races");

            foreach (var race in PcRaces)
            {
                var racedata = new XElement("RaceData");
                racedata.Add(new XElement("Name", race.name));
                racedata.Add(new XElement("Alignment", string.Join(" ", from alignment in race.alignments select alignment.ToString())));
                racedata.Add(new XElement("Ethos", string.Join(" ", from ethos in race.ethosChoices select ethos.ToString())));
                racedata.Add(new XElement("IsPCRace", race.isPCRace.ToString()));
                racedata.Add(new XElement("Parts", string.Join(" ", race.parts)));
                racedata.Add(new XElement("Size", race.Size.ToString()));
                if (race.Stats != null)
                    racedata.Add(race.Stats.Element("Stats"));
                if (race.MaxStats != null)
                    racedata.Add(race.Stats.Element("MaxStats"));
                element.Add(racedata);
            }
            if (!Directory.Exists(Settings.DataPath))
                Directory.CreateDirectory(Settings.DataPath);
            element.Save(System.IO.Path.Join(Settings.DataPath, "PC_Races.xml"));
        }

        public static void LoadRaces()
        {
            if (File.Exists(System.IO.Path.Join(Settings.DataPath, "PC_Races.xml")))
            {
                XElement Races = XElement.Load(System.IO.Path.Join(Settings.DataPath, "PC_Races.xml"));
                var loadedRaces = new List<PcRace>();

                try
                {
                    foreach (var racedata in Races.Elements())
                    {
                        var race = new PcRace
                        {
                            name = racedata.GetElement("name").Value
                        };

                        if (racedata.HasElement("alignment"))
                        {
                            var alignmentString = racedata.GetElement("alignment").Value;
                            var alignments = alignmentString.Split(' ');
                            Alignment alignmentValue = Alignment.Unknown;
                            foreach (var alignment in alignments)
                                if (Utility.GetEnumValue(alignment, ref alignmentValue))
                                {
                                    race.alignments.Add(alignmentValue);
                                }
                        }
                        else
                            race.alignments.Add(Alignment.Neutral);

                        if (racedata.HasElement("ethos"))
                        {
                            var ethosString = racedata.GetElement("ethos").Value;
                            var ethos = ethosString.Split(' ');
                            CrimsonStainedLands.Ethos ethosValue = CrimsonStainedLands.Ethos.Unknown;
                            foreach (var eachethos in ethos)
                                if (Utility.GetEnumValue(eachethos, ref ethosValue))
                                {
                                    race.ethosChoices.Add(ethosValue);
                                }
                        }
                        else
                            race.ethosChoices.Add(Ethos.Neutral);

                        if (racedata.HasElement("ispcrace"))
                            bool.TryParse(racedata.GetElement("ispcrace").Value, out race.isPCRace);

                        if (racedata.HasElement("parts"))
                        {
                            var parts = racedata.GetElement("Parts").Value.Split(' ');
                            PartFlags partValue = PartFlags.Arms;
                            foreach (var part in parts)
                                if (Utility.GetEnumValue<PartFlags>(part, ref partValue))
                                    race.parts.Add(partValue);
                        }

                        if (racedata.HasElement("Stats"))
                        {
                            race.Stats = new PhysicalStats(racedata.Element("Stats"));
                        }

                        if (racedata.HasElement("MaxStats"))
                        {
                            race.MaxStats = new PhysicalStats(racedata.Element("MaxStats"));
                        }
                        Utility.GetEnumValue<CharacterSize>(racedata.GetElementValue("Size", "Medium"), ref race.Size, CharacterSize.Medium);
                        loadedRaces.Add(race);
                        race.BaseRace = Race.GetRace(race.name);

                        if (race.BaseRace == null) { Game.bug("*** Race not found for PC Race"); }
                    }
                    PcRace.PcRaces.Clear();
                    PcRace.PcRaces.AddRange(loadedRaces);
                    Game.log("Loaded " + loadedRaces.Count + " races.");
                }
                catch (Exception ex)
                {
                    Game.log("Exception in Load Races - " + ex.ToString());
                }
            }
        }

        static PcRace()
        {
            //Races.AddRange(new Race[] {
            //    new Race() { Name = "Elf", Alignments = new List<Alignment>(new Alignment[] { Alignment.Good }), EthosChoices = new List<Ethos>(new Ethos[] { Ethos.Neutral, Ethos.Lawful, Ethos.Chaotic }), isPCRace = true },
            //    new Race() { Name = "Human", Alignments = new List<Alignment>(new Alignment[] { Alignment.Good, Alignment.Neutral, Alignment.Evil }), EthosChoices = new List<Ethos>(new Ethos[] { Ethos.Neutral, Ethos.Lawful, Ethos.Chaotic }) , isPCRace = true},
            //    new Race() { Name = "Dwarf", Alignments = new List<Alignment>(new Alignment[] { Alignment.Good, Alignment.Neutral }), EthosChoices = new List<Ethos>(new Ethos[] { Ethos.Neutral, Ethos.Lawful, Ethos.Chaotic } ), isPCRace = true},
            //    new Race() { Name = "Orc", Alignments = new List<Alignment>(new Alignment[] { Alignment.Evil }), EthosChoices = new List<Ethos>(new Ethos[] { Ethos.Neutral, Ethos.Lawful, Ethos.Chaotic } ), isPCRace = true},
            //});
        }
    }


}
