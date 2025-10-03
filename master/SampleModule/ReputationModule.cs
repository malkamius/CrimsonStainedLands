namespace CrimsonStainedLands;

using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using CrimsonStainedLands.Extensions;
using CrimsonStainedLands.World;

public class ReputationModule : CrimsonStainedLands.Module
{
    public class CharacterReputation
    {
        public Dictionary<string, int> CityReputations { get; set; } = new Dictionary<string, int>();
    }
    public static List<string> CityNames { get; set; } = new List<string>() { "Astoria" };

    public static List<Tuple<int, string, string>> ReputationDescriptions = new List<Tuple<int, string, string>> {
        new Tuple<int, string, string>(0, "Unknown", "{r(Unknown){x"),
        new Tuple<int, string, string>(25, "Below Average", "{y(Unliked){x"),
        new Tuple<int, string, string>(50, "Average", "{W(Known){x"),
        new Tuple<int, string, string>(75, "Good", "{G(Liked){x"),
        new Tuple<int, string, string>(100, "Excellent", "{C(Popular){x")
    };

    public static void DoReputation(CrimsonStainedLands.Character character, string arguments)
    {
        var remainingargs = arguments.OneArgumentOut(out var command);

        /// If character level is too low, it will just ignore extra arguments
        if (command.Equals("set", StringComparison.OrdinalIgnoreCase) && character.Level >= 52)
        {
            remainingargs = remainingargs.OneArgumentOut(out var characterName);
            remainingargs = remainingargs.OneArgumentOut(out var cityName);
            _ = remainingargs.OneArgumentOut(out var value);

            if (characterName.ISEMPTY())
            {
                character.send("Syntax: reputation set <character> <city> <value>\r\n");
                character.send("Character name is required.\r\n");
                return;
            }
            else if (cityName.ISEMPTY())
            {
                character.send("Syntax: reputation set <character> <city> <value>\r\n");
                character.send("City name is required.\r\n");
                return;
            }
            else if (value.ISEMPTY())
            {
                character.send("Syntax: reputation set <character> <city> <value>\r\n");
                character.send("Value is required.\r\n");
                return;
            }
            else if (!int.TryParse(value, out int intValue))
            {
                character.send("Syntax: reputation set <character> <city> <value>\r\n");
                character.send("Invalid value for reputation.\r\n");
                return;
            }
            else
            {
                if (CrimsonStainedLands.Character.GetCharacterWorld(characterName) is CrimsonStainedLands.Character target)
                {
                    var reputation = target.GetVariable<CharacterReputation>("CharacterReputation");

                    if (reputation == null)
                    {
                        target.Variables["CharacterReputation"] = reputation = new CharacterReputation();
                    }
                    var foundCityName = CityNames.FirstOrDefault(c => c.Equals(cityName, StringComparison.OrdinalIgnoreCase));
                    if (foundCityName != null)
                    {
                        reputation.CityReputations[foundCityName] = intValue;
                        character.send($"Set {target.Name}'s reputation with {foundCityName} to {intValue}.\r\n");
                    }
                    else
                    {
                        character.send("City with that name not found.\r\n");
                    }
                }
                else
                {
                    character.send("Character not found.\r\n");
                }
            }
            return;
        }
        else
        {
            var reputation = character.GetVariable<CharacterReputation>("CharacterReputation");
            if (reputation != null)
            {
                foreach (var city in CityNames)
                {
                    reputation.CityReputations.TryGetValue(city, out int value);
                    string description = "Unknown";
                    foreach (var repDesc in ReputationDescriptions)
                    {
                        if (value >= repDesc.Item1 || repDesc.Item1 == 0)
                        {
                            description = repDesc.Item2;
                        }
                        else
                        {
                            break;
                        }
                    }

                    character.send($"Your reputation with the the city {city} is {value} ({description}).\r\n");
                }
            }
            else
            {
                character.send("You have no reputation anywhere.\r\n");
            }
        }
    }

    public ReputationModule(string dllPath, System.Reflection.Assembly assembly) : base(dllPath, assembly)
    {
        this.Name = "ReputationModule : SampleModule";
        this.Description = "A sample module for reputation features.";

        CrimsonStainedLands.Command.Commands.Add(new CrimsonStainedLands.Command()
        {
            Name = "reputation",
            Info = "See reputation with cities",
            Action = DoReputation,
            MinimumLevel = 0,
            MinimumPosition = CrimsonStainedLands.Positions.Dead,
            NPCCommand = false,
            Skill = null
        });

        Module.OnDataLoadedEvent += OnDataLoaded;
        Module.Character.LoadingEvent += OnCharacterLoading;
        Module.Character.SerializingEvent += OnCharacterSerializing;
        Module.Character.OnEnterRoomEvent += OnCharacterEnterRoom;

        CrimsonStainedLands.Character.DisplayFlagsStack.Insert(0, DisplayReputationFlag);
    }

    private string DisplayReputationFlag(CrimsonStainedLands.Character character, CrimsonStainedLands.Character viewer)
    {
        var reputation = character.GetVariable<CharacterReputation>("CharacterReputation");
        if (reputation != null)
        {
            if (reputation.CityReputations.TryGetValue(viewer.Room.Area.Name, out int value))
            {
                string description = "Unknown";
                foreach (var repDesc in ReputationDescriptions)
                {
                    if (value >= repDesc.Item1 || repDesc.Item1 == 0)
                    {
                        description = repDesc.Item3;
                    }
                    else
                    {
                        break;
                    }
                }
                return description;
            }
        }
        return "";
    }

    private void OnCharacterEnterRoom(CrimsonStainedLands.Character character, RoomData oldRoom, RoomData newRoom)
    {
        if (newRoom != null && newRoom.Area.GetVariable<bool>("HasReputation"))
        {
            var reputation = character.GetVariable<CharacterReputation>("CharacterReputation");
            if(reputation == null )
            {
                reputation = new CharacterReputation();
                character.Variables["CharacterReputation"] = reputation;
            }
            if (!reputation.CityReputations.ContainsKey(newRoom.Area.Name))
            {
                reputation.CityReputations[newRoom.Area.Name] = 0;
            }
        }
    }

    private void OnCharacterSerializing(CrimsonStainedLands.Character character, XElement element)
    {
        var reputation = character.GetVariable<CharacterReputation>("CharacterReputation");
        if (reputation != null)
        {
            XElement reputationElement = new XElement("reputation");
            foreach (var cityRep in reputation.CityReputations)
            {
                reputationElement.Add(new XElement("city",
                    new XAttribute("name", cityRep.Key),
                    new XAttribute("value", cityRep.Value)));
            }
            element.Add(reputationElement);
        }
    }

    private void OnCharacterLoading(CrimsonStainedLands.Character character, XElement element)
    {
        var reputation = new CharacterReputation();
        var reputationElement = element.Element("reputation");
        if (reputationElement != null)
        {
            foreach (var cityElement in reputationElement.Elements("city"))
            {
                string? cityName = cityElement.Attribute("name")?.Value;
                int cityValue = int.Parse(cityElement.Attribute("value")?.Value ?? "0");
                if(cityName != null)
                {
                    reputation.CityReputations[cityName] = cityValue;
                }
            }
        }
        character.Variables["CharacterReputation"] = reputation;
    }

    private void OnDataLoaded()
    {
        
        foreach (var area in AreaData.Areas)
        {
            if (CityNames.Contains(area.Name))
            {
                area.Variables["HasReputation"] = true;
            }
        }
    }
}
