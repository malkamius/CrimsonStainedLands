using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq.Expressions;

namespace CrimsonStainedLands
{
    public enum Direction : int
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
        Up = 4,
        Down = 5
    }

    public enum ExitFlags
    {
        Door,
        Closed,
        Locked,
        Window,
        PickProof,
        NoPass,
        NoBash,
        Hidden,
        HiddenWhileClosed,
        MustUseKeyword,
        NoRandomize,
        NonObvious = Hidden,
    }

    public enum SectorTypes
    {
        Inside = 0,
        City = 1,
        Forest,
        Field,
        Hills,
        Mountain,
        Swamp,
        Desert,
        Cave,
        Underground,
        WaterSwim,
        WaterNoSwim,
        River,
        Ocean,
        Air,
        Road,
        Trail,
        Underwater,
        NoSwim = WaterNoSwim,
        Swim = WaterSwim
    }

    public enum RoomFlags
    {
        Dark = 1,
        NoMob = 2,
        Indoors = 3,
        Cabal = 4,
        Private = 5,
        Safe = 6,
        Solitary = 7,
        PetShop = 8,
        NoRecall,
        ImplementorOnly,
        HeroesOnly,
        NewbiesOnly,
        Law,
        Nowhere,
        NoGate,
        Consecrated,
        NoSummon,
        NoConsecrate,
        NoMagic,
        GlobeDarkness,
        RandomExits
    }

    public class SectorData
    {
        public SectorTypes sector;
        public string name;
        public int movementCost;
        public int movementWait;
        public string display;
    }


    public class RoomData
    {
        public static Dictionary<SectorTypes, SectorData> sectors = new Dictionary<SectorTypes, SectorData> {
            { SectorTypes.Inside, new SectorData() { sector = SectorTypes.Inside, name = "Indoors", movementCost = 1, movementWait = 1, display = "\\winside\\x" } },
            { SectorTypes.Field, new SectorData() { sector = SectorTypes.Field, name = "Plains", movementCost = 2, movementWait = 1, display = "\\yplains\\x" } },
            { SectorTypes.Desert, new SectorData() { sector = SectorTypes.Desert, name = "Desert", movementCost = 2, movementWait = 2, display = "\\Ydesert\\x" } },
            { SectorTypes.City, new SectorData() { sector = SectorTypes.City, name = "City", movementCost = 1, movementWait = 1, display = "\\wcity\\x" } },
            { SectorTypes.Road, new SectorData() { sector = SectorTypes.Road, name = "Road", movementCost = 1, movementWait = 1, display = "\\Wroad\\x" } },
            { SectorTypes.Trail, new SectorData() { sector = SectorTypes.Trail, name = "Trail", movementCost = 2, movementWait = 1, display = "\\Gtrail\\x" } },
            { SectorTypes.Forest, new SectorData() { sector = SectorTypes.Forest, name = "Forest", movementCost = 5, movementWait = 2, display = "\\Gforest\\x" } },
            { SectorTypes.Hills, new SectorData() { sector = SectorTypes.Hills, name = "Hills", movementCost = 3, movementWait = 4, display = "\\Ghills\\x" } },
            { SectorTypes.Cave, new SectorData() { sector = SectorTypes.Cave, name = "Cave", movementCost = 3, movementWait = 3, display = "\\Wcave\\x" } },
            { SectorTypes.Mountain, new SectorData() { sector = SectorTypes.Mountain, name = "Mountain", movementWait = 3, movementCost = 6, display = "\\Wmountain\\x" } },
            { SectorTypes.Swamp, new SectorData() { sector = SectorTypes.Swamp, name = "Swamp", movementWait = 3, movementCost = 3, display = "\\Gswamp\\x" } },
            { SectorTypes.Underground, new SectorData() { sector = SectorTypes.Underground, name = "Underground", movementWait = 2, movementCost = 2, display = "\\Wunderground\\x" } },
            { SectorTypes.Underwater, new SectorData() { sector = SectorTypes.Underwater, name = "Underwater", movementWait = 3, movementCost = 6, display = "\\Bunderwater\\x" } },
            { SectorTypes.WaterSwim, new SectorData() { sector = SectorTypes.WaterSwim, name = "WaterSwim", movementWait = 2, movementCost = 6, display = "\\Bwater\\x" } },
            { SectorTypes.WaterNoSwim, new SectorData() { sector = SectorTypes.WaterNoSwim, name = "WaterNoSwim", movementWait = 3, movementCost = 6, display = "\\Bwater\\x" } },
            { SectorTypes.River, new SectorData() { sector = SectorTypes.River, name = "River", movementWait = 2, movementCost = 6, display = "\\Briver\\x" } },
            { SectorTypes.Ocean, new SectorData() { sector = SectorTypes.Ocean, name = "Ocean", movementWait = 3, movementCost = 10, display = "\\Bocean\\x" } },
            { SectorTypes.Air, new SectorData() { sector = SectorTypes.Ocean, name = "Air", movementWait = 1, movementCost = 10, display = "\\cair\\x" } }
        };

        public List<ExtraDescription> ExtraDescriptions = new List<ExtraDescription>();
        public SectorData GetSectorType(string sectorLookup)
        {
            foreach (var sector in sectors)
                if (sector.Value.name.IsName(sectorLookup, true))
                    return sector.Value;

            Game.log("Sector " + sectorLookup + " not found.");
            return sectors[SectorTypes.Inside];
        }

        /// <summary>
        /// all rooms regardless of area
        /// </summary>
        public static Dictionary<int, RoomData> Rooms = new Dictionary<int, RoomData>();
        public AreaData Area;
        public int Vnum;
        public string Name;
        public string NightName { get; set; } = "";

        public string _Description = "";
        public string _NightDescription = "";

        public ExitData[] exits = new ExitData[6];
        public ExitData[] OriginalExits = new ExitData[6];

        public List<Character> Characters = new List<Character>();
        public List<ItemData> items = new List<ItemData>();
        public List<RoomAffectData> affects = new List<RoomAffectData>();
        public List<RoomFlags> flags = new List<RoomFlags>();
        public SectorTypes sector;
        public GuildData Guild = null;
        public List<Programs.Program<RoomData>> Programs = new List<Programs.Program<RoomData>>();
        public List<NLuaPrograms.NLuaProgram> LuaPrograms = new List<NLuaPrograms.NLuaProgram>();
        //private int light;

        public string Description
        {
            get
            {
                var regex = new Regex("(?m)^\\s+");
                if (_Description.StartsWith(".")) return _Description.Replace("\n\r", "\n").Replace("\r\n", "\n");
                return regex.Replace((_Description ?? ""), "");
            }
            set
            {
                if (value != null) _Description = value.Replace("\n\r", "\n"); else _Description = "";
            }
        }

        public string NightDescription
        {
            get
            {
                var regex = new Regex("(?m)^\\s+");
                if (_NightDescription.StartsWith(".")) return _NightDescription.Replace("\n\r", "\n").Replace("\r\n", "\n");
                return regex.Replace((_NightDescription ?? ""), "");
            }
            set
            {
                if (value != null) _NightDescription = value.Replace("\n\r", "\n"); else _NightDescription = "";
            }
        }

        public RoomData()
        {

        }

        public bool IsWilderness
        {
            get
            {
                return sector == SectorTypes.Trail ||
                    sector == SectorTypes.Field ||
                    sector == SectorTypes.Forest ||
                    sector == SectorTypes.Hills ||
                    sector == SectorTypes.Mountain ||
                    sector == SectorTypes.WaterSwim ||
                    sector == SectorTypes.WaterNoSwim ||
                    sector == SectorTypes.Cave ||
                    sector == SectorTypes.Underground ||
                    sector == SectorTypes.Underwater ||
                    sector == SectorTypes.Ocean ||
                    sector == SectorTypes.River ||
                    sector == SectorTypes.Swamp ||
                    sector == SectorTypes.Desert;
            }
        }

        public RoomData(AreaData area, XElement room)
        {
            Vnum = room.GetElementValueInt("vnum");

            if (Vnum != 0)
            {
                Name = room.GetElementValue("name");
                Description = room.GetElementValue("description").TrimStart();
                NightName = room.GetElementValue("NightName");
                NightDescription = room.GetElementValue("NightDescription").TrimStart();

                if (room.HasElement("Guild"))
                    Guild = GuildData.GuildLookup(room.GetElementValue("Guild"));

                foreach (var exit in room.GetElement("exits").Elements())
                {
                    var direction = exit.GetElementValue("direction");
                    var newExit = new ExitData();
                    Utility.GetEnumValue<Direction>(direction, ref newExit.direction);

                    newExit.destinationVnum = exit.GetElementValueInt("destination");
                    newExit.description = exit.GetElementValue("description");
                    newExit.display = exit.GetElementValue("Display");
                    newExit.keywords = exit.GetElementValue("Keywords");

                    Utility.GetEnumValues<ExitFlags>(exit.GetElementValue("Flags"), ref newExit.flags);

                    //if (newExit.flags.Contains(ExitFlags.Door))
                    //    newExit.flags.Add(ExitFlags.close);
                    //else if (newExit.flags.Contains(ExitFlags.Window))
                    //    newExit.flags.Add(ExitFlags.WindowClosed);

                    newExit.originalFlags.AddRange(newExit.flags);

                    if (exit.HasElement("Keys"))
                        foreach (var key in exit.GetElementValue("Keys").Split(' '))
                        {
                            if (int.TryParse(key, out int keyVnum))
                                newExit.keys.Add(keyVnum);
                        }
                    if (newExit.keys.Count == 0 || newExit.keys.All(key => key == 0 || key == -1))
                    {
                        newExit.flags.REMOVEFLAG(ExitFlags.Locked);
                        newExit.originalFlags.REMOVEFLAG(ExitFlags.Locked);
                    }
                    Utility.GetEnumValue<CharacterSize>(exit.GetElementValue("ExitSize"), ref newExit.ExitSize, CharacterSize.Giant);

                    exits[(int)newExit.direction] = newExit;
                    OriginalExits[(int)newExit.direction] = new ExitData(newExit);
                }



                if (!Utility.GetEnumValue<SectorTypes>(room.GetElementValue("Sector"), ref sector))
                {
                    sector = SectorTypes.Inside;
                }

                Utility.GetEnumValues<RoomFlags>(room.GetElementValue("Flags"), ref flags);

                if (room.HasElement("ExtraDescriptions"))
                {
                    foreach (var EDElement in room.Element("ExtraDescriptions").Elements())
                    {
                        ExtraDescriptions.Add(new ExtraDescription(EDElement.GetElementValue("keyword"), EDElement.GetElementValue("description")));
                    }
                }

                if (room.HasElement("Programs"))
                {
                    var programsElement = room.GetElement("Programs");
                    foreach (var programElement in programsElement.Elements())
                    {
                        if (CrimsonStainedLands.Programs.RoomProgramLookup(programElement.GetAttributeValue("Name"), out var program)) 
                        {
                            Programs.Add(program);
                        }
                        else if (NLuaPrograms.ProgramLookup(programElement.GetAttributeValue("Name"), out var luaprogram))
                        {
                            LuaPrograms.Add(luaprogram);
                        }
                    }
                }

                this.Area = area;

                if (!area.Rooms.ContainsKey(Vnum))
                {
                    area.Rooms.Add(Vnum, this);
                }
                else
                {
                    Game.log("Duplicate room vnum: {0} in {1}", Vnum, area.FileName);
                }
                if (!Rooms.ContainsKey(Vnum))
                {

                    Rooms.Add(Vnum, this);
                }
                else
                    Game.log("Duplicate room vnum: {0} in {1} conflicts with {2}", Vnum, area.FileName, Rooms[Vnum].Area.FileName);

            }
        } // End Constructor(AreaData, XElement)

        static RoomData()
        {

        } // End Static Constructor

        public ExitData GetExit(Direction direction)
        {
            return exits[(int)direction];
        }

        public ExitData GetExit(string keyword)
        {
            int count = 0;
            return GetExit(keyword, ref count);
        }

        public ExitData GetExit(string keyword, ref int count)
        {
            int num = keyword.number_argument(ref keyword);
            Direction direction = Direction.North;

            if (Utility.GetEnumValueStrPrefix<Direction>(keyword, ref direction) && (exits[(int)direction] == null || !exits[(int)direction].flags.ISSET(ExitFlags.MustUseKeyword)) && ++count >= num)
                return exits[(int)direction];

            foreach (var exit in exits)
            {
                if (exit != null && exit.keywords.IsName(keyword) && ++count >= num)
                {
                    return exit;
                }
            }

            return null;
        }

        public List<ResetData> GetResets()
        {
            var result = new List<ResetData>();
            bool inRoom = false;
            if (Area != null)
                foreach (var reset in Area.Resets)
                {
                    if ((reset.resetType == ResetTypes.NPC || reset.resetType == ResetTypes.Item))
                    {
                        if (reset.roomVnum == Vnum)
                        {
                            result.Add(reset);
                            inRoom = true;
                        }
                        else
                            inRoom = false;
                    }
                    else if (reset.resetType != ResetTypes.NPC && reset.resetType != ResetTypes.Item && inRoom)
                    {
                        result.Add(reset);
                    }
                }

            return result;
        }

        public XElement Element
        {
            get
            {
                return new XElement("Room",
                    new XElement("VNum", Vnum),
                    new XElement("Name", Name),

                    !NightName.ISEMPTY() ?
                    new XElement("NightName", NightName) : null,

                    new XElement("Description", Description.TOSTRINGTRIM()),

                    !NightDescription.ISEMPTY() ?
                    new XElement("NightDescription", NightDescription) : null,

                    Guild != null ? Guild.name : null,

                    new XElement("Sector", sector),
                    new XElement("Flags", flags.ToDelimitedString()), //string.Join(" ", from flag in flags select flag.ToString())); ;
                    new XElement("Exits",
                        from exit in OriginalExits
                        where exit != null
                        select new XElement("Exit",
                            new XElement("Direction", exit.direction.ToString()),
                            new XElement("Destination", exit.destination != null ? exit.destination.Vnum : exit.destinationVnum),
                            new XElement("Display", exit.display),
                            new XElement("Keywords", exit.keywords),
                            new XElement("Description", exit.description.TOSTRINGTRIM()),
                            new XElement("Flags", string.Join(" ", from flag in exit.originalFlags select flag.ToString())),
                            new XElement("Keys", string.Join(" ", from key in exit.keys select key)),
                            new XElement("ExitSize", exit.ExitSize)
                            )
                        ),
                    new XElement("ExtraDescriptions",
                        from ED in ExtraDescriptions
                        select new XElement("ExtraDescription",
                            new XElement("Keyword", ED.Keywords),
                            new XElement("Description", ED.Description)
                            )
                        ),
                    (Programs.Any() || LuaPrograms.Any() ? 
                        new XElement("Programs", 
                            (from program in Programs select new XElement("Program", new XAttribute("Name", program.Name))).
                            Concat(from luaprogram in LuaPrograms select new XElement("Program", new XAttribute("Name", luaprogram.Name)))) : null)
                    );
            }
        }

        public bool IsDark
        {
            get
            {
                if (Characters.Any(c => c.Equipment.Values.Any(i => i != null && (i.extraFlags.ISSET(ExtraFlags.Glow) || i.ItemType.ISSET(ItemTypes.Light)))))
                    return false;

                if (flags.ISSET(RoomFlags.Dark))
                    return true;

                if (sector == SectorTypes.Inside
                    || sector == SectorTypes.City)
                    return false;

                if (WeatherData.Sunlight == SunlightStates.Set
                    || WeatherData.Sunlight == SunlightStates.Dark)
                    return true;

                return false;
            }
        }

        public bool IsWater => sector == SectorTypes.WaterSwim || sector == SectorTypes.WaterNoSwim || sector == SectorTypes.River || sector == SectorTypes.Ocean || sector == SectorTypes.Underwater;
    }
    public class ExitData
    {
        public Direction direction;
        public string description = "";
        private string _display;
        public string display
        {
            get { return _display.ISEMPTY() ? "the door " + direction.ToString().ToLower() : _display; }
            set { if (value != "the door " + direction.ToString().ToLower()) _display = value; }
        }
        public RoomData source;
        public RoomData destination;
        public int destinationVnum;
        public List<ExitFlags> flags = new List<ExitFlags>();
        public List<ExitFlags> originalFlags = new List<ExitFlags>();
        public List<int> keys = new List<int>();
        public string keywords;
        public CharacterSize ExitSize = CharacterSize.Giant;

        public ExitData() { }
        public ExitData(ExitData toclone)
        {
            this.direction = toclone.direction;
            this.description = toclone.description;
            this.display = toclone.display;
            this.source = toclone.source;
            this.destination = toclone.destination;
            this.destinationVnum = toclone.destinationVnum;
            this.flags = new List<ExitFlags>(toclone.flags);
            this.originalFlags = new List<ExitFlags>(toclone.originalFlags);
            this.keys = new List<int>(toclone.keys);
            this.keywords = toclone.keywords;
            ExitSize = toclone.ExitSize;
        }
    }
}
