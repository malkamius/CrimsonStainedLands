using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static CrimsonStainedLands.OLC;

namespace CrimsonStainedLands
{
    public static class OLC
    {
        public class EditCommand
        {
            public string name;
            public Action<Character, string> action;

        }
        public static List<EditCommand> EditAreaCommands = new List<EditCommand>()
        {
            new EditCommand() { action = DoEditAreaCredits, name = "credits" },
            new EditCommand() { action = DoEditAreaName, name = "name" },
            new EditCommand() { action = DoEditAreaOverRoomVnum, name = "overroomvnum" },
        };

        public static List<EditCommand> EditRoomCommands = new List<EditCommand>()
        {
            new EditCommand() { action = DoEditRoomDescription, name = "description" },
            new EditCommand() { action = DoEditRoomName, name = "name" },
            new EditCommand() { action = DoEditRoomSector, name = "sector" },
            new EditCommand() { action = DoEditRoomFlags, name = "flags" },
            new EditCommand() { action = DoEditRoomExits, name = "exits" },
            new EditCommand() { action = DoEditRoomResets, name = "resets" },
            new EditCommand() { action = DoEditRoomExtraDescriptions, name = "extradescriptions" }
        };
        public static List<EditCommand> EditNPCCommands = new List<EditCommand>()
        {
            new EditCommand() { action = DoEditNPCDescription, name = "description" },
            new EditCommand() { action = DoEditNPCName, name = "name" },
            new EditCommand() { action = DoEditNPCFlags, name = "flags" },
            new EditCommand() { action = DoEditNPCImmuneFlags, name = "immuneflags" },
            new EditCommand() { action = DoEditNPCResistFlags, name = "resistflags" },
            new EditCommand() { action = DoEditNPCVulnerableFlags, name = "vulnerableflags" },
            new EditCommand() { action = DoEditNPCAffectedBy, name = "affectedby" },
            new EditCommand() { action = DoEditNPCLevel, name = "level" },
            new EditCommand() { action = DoEditNPCGold, name = "gold" },
            new EditCommand() { action = DoEditNPCGuild, name = "guild" },
            new EditCommand() { action = DoEditNPCRace, name = "race" },
            new EditCommand() { action = DoEditNPCGender, name = "sex" },
            new EditCommand() { action = DoEditNPCAlignment, name = "alignment" },
            new EditCommand() { action = DoEditNPCHitRoll, name = "hitroll" },
            new EditCommand() { action = DoEditNPCDamageRoll, name = "damageroll" },
            new EditCommand() { action = DoEditNPCLongDescription, name = "longdescription" },
            new EditCommand() { action = DoEditNPCShortDescription, name = "shortdescription" },
            new EditCommand() { action = DoEditNPCDamageDice, name = "damagedice" },
            new EditCommand() { action = DoEditNPCHitPointDice, name = "hitpointdice" },
            new EditCommand() { action = DoEditNPCManaPointDice, name = "manapointdice" },
            new EditCommand() { action = DoEditNPCProtects, name = "protects" },
        };
        public static List<EditCommand> EditItemCommands = new List<EditCommand>()
        {
            new EditCommand() { action = DoEditItemDescription, name = "description" },
            new EditCommand() { action = DoEditItemName, name = "name" },
            new EditCommand() { action = DoEditItemLongDescription, name = "longdescription" },
            new EditCommand() { action = DoEditItemShortDescription, name = "shortdescription" },
            new EditCommand() { action = DoEditItemNightLongDescription, name = "nightlongdescription" },
            new EditCommand() { action = DoEditItemNightShortDescription, name = "nightshortdescription" },
            new EditCommand() { action = DoEditItemExtraFlags, name = "extraflags" },
            new EditCommand() { action = DoEditItemWearFlags, name = "wearflags" },
            new EditCommand() { action = DoEditItemWeaponType, name = "weapontype" },
            new EditCommand() { action = DoEditItemLevel, name = "level" },
            new EditCommand() { action = DoEditItemWeight, name = "weight" },
            new EditCommand() { action = DoEditItemMaxWeight, name = "maxweight" },
            new EditCommand() { action = DoEditItemValue, name = "value" },
            new EditCommand() { action = DoEditItemItemTypes, name = "itemtypes" },
            new EditCommand() { action = DoEditItemDamageDice, name = "damagedice" },
            new EditCommand() { action = DoEditItemDamageMessage, name = "damagemessage" },
            new EditCommand() { action = DoEditItemMaterial, name = "material" },
            new EditCommand() { action = DoEditItemExtraDescriptions, name = "extradescriptions" },
            new EditCommand() { action = DoEditItemLiquid, name = "liquid" },
            new EditCommand() { action = DoEditItemNutrition, name = "nutrition" },
            new EditCommand() { action = DoEditItemMaxCharges, name = "maxcharges" },
            new EditCommand() { action = DoEditItemMaxDurability, name = "maxdurability" },
            new EditCommand() { action = DoEditItemAffects, name = "affects" },
            new EditCommand() { action = DoEditItemSpells, name = "spells" }
        };



        internal static void DoCreate(Character ch, string args)
        {
            string type = "";
            string vnumEndString = "";
            string vnumStartString = "";
            string nameString = "";
            string countString = "";
            string maxCountString = "";
            string ResetTypeString = "";
            int count;
            int maxCount;

            args = args.OneArgument(ref type);

            if ("area".StringPrefix(type))
            {
                AreaData area = new AreaData();
                args = args.OneArgument(ref nameString);
                args = args.OneArgument(ref vnumStartString);
                args = args.OneArgument(ref vnumEndString);

                area.name = nameString;
                area.fileName = "data\\areas\\" + nameString + ".xml";
                if (!int.TryParse(vnumStartString, out area.vnumStart) || !int.TryParse(vnumEndString, out area.vnumEnd))
                {
                    ch.send("Create Area \"name\" vnumstart vnumend");
                    return;
                }

                AreaData.Areas.Add(area);
                ch.EditingArea = area;
                area.saved = false;
                ch.send("OK");
            }
            else if ("reset".StringPrefix(type))
            {
                args = args.OneArgument(ref ResetTypeString);
                args = args.OneArgument(ref vnumStartString);
                args = args.OneArgument(ref vnumEndString);
                args = args.OneArgument(ref countString);
                args = args.OneArgument(ref maxCountString);

                int vnumEnd;
                int vnumStart;
                ResetTypes resetType = ResetTypes.Equip;
                AreaData areaData = ch.EditingArea ?? ch.Room.Area;
                if (!Utility.GetEnumValue<ResetTypes>(ResetTypeString, ref resetType))
                {
                    ch.send("Invalid reset types, valid values: {0}", string.Join(" ", from resetTypes in Enum.GetNames(typeof(ResetTypes)) select resetTypes.ToString()));
                }
                if (!int.TryParse(vnumStartString, out vnumStart))
                {
                    ch.send("Invalid start vnum.\n\r");
                    return;

                }
                if (!int.TryParse(vnumEndString, out vnumEnd))
                {
                    ch.send("Invalid end vnum, using current room vnum.\n\r");
                    vnumEnd = ch.Room.Vnum;
                    //return;
                }
                if (!int.TryParse(countString, out count))
                {
                    count = 1;
                }
                if (!int.TryParse(maxCountString, out maxCount))
                {
                    maxCount = 1;
                }
                //else
                {
                    ResetData reset = new ResetData(areaData, resetType, vnumStart, vnumEnd, count, maxCount);
                    areaData.saved = false;
                    ch.send("OK.\n\r");
                }
            }
            else if ("room".StringPrefix(type))
            {
                if (!int.TryParse(args, out var vnum))
                {
                    ch.send("You must specify a vnum.\n\r");
                }
                else if (ch.EditingArea == null && (ch.EditingArea = ch.Room.Area) == null)
                {
                    ch.send("Area not found.\n\r");
                }
                else if (RoomData.Rooms.ContainsKey(vnum))
                {
                    ch.send("Room with that vnum already exists.\n\r");
                }
                else
                {
                    var room = new RoomData() { Area = ch.EditingArea, Vnum = vnum };
                    ch.EditingArea.rooms.Add(vnum, room);
                    ch.EditingRoom = room;
                    RoomData.Rooms.Add(vnum, room);
                    ch.send("OK.\n\r");
                }
            }
            else if ("npc".StringPrefix(type) || "mobile".StringPrefix(type))
            {
                if (!int.TryParse(args, out var vnum))
                {
                    ch.send("You must specify a vnum.\n\r");
                }
                else if (ch.EditingArea == null && (ch.EditingArea = ch.Room.Area) == null)
                {
                    ch.send("Area not found.\n\r");
                }
                else if (NPCTemplateData.Templates.ContainsKey(vnum))
                {
                    ch.send("NPC with that vnum already exists.\n\r");
                }
                else
                {
                    var npc = new NPCTemplateData(ch.EditingArea, new XElement("NPC", new XElement("VNum", vnum)));
                    ch.EditingNPCTemplate = npc;
                    ch.send("OK.\n\r");
                }
            }
            else if ("item".StringPrefix(type) || "object".StringPrefix(type))
            {
                if (!int.TryParse(args, out var vnum))
                {
                    ch.send("You must specify a vnum.\n\r");
                }
                else if (ch.EditingArea == null && (ch.EditingArea = ch.Room.Area) == null)
                {
                    ch.send("Area not found.\n\r");
                }
                else if (ItemTemplateData.Templates.ContainsKey(vnum))
                {
                    ch.send("Item with that vnum already exists.\n\r");
                }
                else
                {
                    var item = new ItemTemplateData(ch.EditingArea, new XElement("Item", new XElement("VNum", vnum)));
                    ch.EditingItemTemplate = item;
                    ch.send("OK.\n\r");
                }
            }
            else
            {
                ch.send("Syntax: create area [name] [vnumstart] [vnumend]" +
                    "\ncreate room [vnum]" +
                    "\ncreate npc [vnum]" +
                    "\ncreate item [vnum]" +
                    "\n\rOr: create reset [vnum] [desintation] [count] [maxcount] \n\r");

            }
        }

        public static void DoAEdit(Character ch, string args)
        {
            DoEditArea(ch, args);
        }

        public static void DoREdit(Character ch, string args)
        {
            DoEditRoom(ch, args);
        }

        public static void DoMEdit(Character ch, string args)
        {
            DoEditNPC(ch, args);
        }

        public static void DoOEdit(Character ch, string args)
        {
            DoEditItem(ch, args);
        }

        public static void DoEdit(Character ch, string args)
        {
            string type = "";

            args = args.OneArgument(ref type);

            if ("area".StringPrefix(type))
            {
                DoEditArea(ch, args);
                //string nameString = "";
                //ch.EditingArea = null;
                //if (args.ISEMPTY() && ch.Room != null && ch.HasBuilderPermission(ch.Room.Area))
                //{
                //    ch.EditingArea = ch.Room.Area;
                //    ch.send("Editing {0}.\n\r", ch.EditingArea.name);
                //}
                //else if (args.ISEMPTY())
                //{
                //    ch.send("You aren't in an area.\n\r");
                //}
                //else
                //{
                //    args = args.OneArgument(ref nameString);
                //    int vnumStart = 0;
                //    int.TryParse(nameString, out vnumStart);

                //    foreach (var area in AreaData.Areas)
                //    {
                //        if ((((vnumStart != 0 && area.vnumStart == vnumStart) || area.name == nameString) && ch.HasBuilderPermission(area)))
                //        {
                //            ch.EditingArea = area;
                //            ch.send("Editing {0}.\n\r", ch.EditingArea.name);
                //            break;
                //        }
                //    }

                //    if (ch.EditingArea == null)
                //        ch.send("Area not found or permissions not set.\n\r");
                //}
            }
            else if ("room".StringPrefix(type))
            {
                DoEditRoom(ch, args);

            }
            else if ("npc".StringPrefix(type) || "mobile".StringPrefix(type))
            {
                DoEditNPC(ch, args);
            }
            else if ("item".StringPrefix(type) || "object".StringPrefix(type))
            {
                DoEditItem(ch, args);
            }
            else if ("done".StringPrefix(type))
            {
                ch.EditingRoom = null;
                ch.EditingNPCTemplate = null;
                ch.EditingItemTemplate = null;
                ch.EditingArea = null;
                ch.send("OK.\n\r");
            }
            else
            {
                ch.send("Syntax: Edit [area|room|item|object|npc|mobile]\n\r");
            }

        }

        private static void DoEditNPC(Character ch, string arguments)
        {
            string vnumString = "";
            arguments = arguments.OneArgument(ref vnumString);
            int vnum = 0;
            NPCTemplateData npcTemplate = ch.EditingNPCTemplate;
            EditCommand editCommand;
            //string commandStr = "";
            //args = args.OneArgument(ref commandStr);
            if (int.TryParse(vnumString, out vnum))
            {

                if (NPCTemplateData.Templates.TryGetValue(vnum, out npcTemplate))
                {
                    arguments = arguments.OneArgument(ref vnumString);
                    if (!ch.HasBuilderPermission(npcTemplate))
                    {
                        ch.send("Builder permissions not set.\n\r");
                        return;
                    }
                    ch.EditingNPCTemplate = npcTemplate;
                }
                else
                {
                    ch.send("npc vnum not found.\n\r");
                    //npcTemplate = new NPCTemplateData(ch.Room.area, new XElement("NPC"));
                    //ch.send("New NPC Created.\n\r");
                    return;
                }
            }
            else if ("done".StringPrefix(vnumString))
            {
                ch.EditingRoom = null;
                ch.EditingNPCTemplate = null;
                ch.EditingItemTemplate = null;
                ch.EditingArea = null;
                ch.send("OK.\n\r");
                return;
            }
            else if (ch.EditingNPCTemplate != null && (editCommand = EditNPCCommands.FirstOrDefault(c => c.name.StringPrefix(vnumString))) != null)
            {
                if (!ch.HasBuilderPermission(ch.EditingNPCTemplate))
                {
                    ch.send("Builder permissions not set.\n\r");
                    return;
                }
                editCommand.action(ch, arguments);
                return;
            }
            else if (ch.GetCharacterFromRoomByName(vnumString, out var npc) && npc.IsNPC && ((NPCData)npc).template != null)
            {
                if (!ch.HasBuilderPermission(((NPCData)npc).template))
                {
                    ch.send("Builder permissions not set.\n\r");
                    return;
                }
                ch.EditingNPCTemplate = ((NPCData)npc).template;
            }
            else if (npc != null && !npc.IsNPC)
                ch.send("It isn't an npc.\n\r");
            else if (npc != null && ((NPCData)npc).template == null)
                ch.send("That npc doesn't have a template.\n\r");
            else
                ch.send("You don't see them here.\n\r");

            ch.send("Syntax: Edit NPC [vnum] [name|description|level|flags|affectedby|damageroll|hitroll|damagedice|hitpointdice|manapointdice]\n\r");
        }

        private static void DoEditItem(Character ch, string arguments)
        {
            string vnumString = "";
            arguments = arguments.OneArgument(ref vnumString);
            int vnum = 0;
            ItemTemplateData itemTemplate;
            EditCommand editCommand;
            if (int.TryParse(vnumString, out vnum))
            {

                if (ItemTemplateData.Templates.TryGetValue(vnum, out itemTemplate))
                {
                    if (!ch.HasBuilderPermission(itemTemplate))
                    {
                        ch.send("Builder permissions not set.\n\r");
                        return;
                    }
                    ch.EditingItemTemplate = itemTemplate;
                }
                else
                {
                    ch.send("Item with that vnum not found.\n\r");
                    return;
                }
            }
            else if ("done".StringPrefix(vnumString))
            {
                ch.EditingRoom = null;
                ch.EditingNPCTemplate = null;
                ch.EditingItemTemplate = null;
                ch.EditingArea = null;
                ch.send("OK.\n\r");
                return;
            }
            else if (ch.EditingItemTemplate != null && (editCommand = EditItemCommands.FirstOrDefault(c => c.name.StringPrefix(vnumString))) != null)
            {
                if (!ch.HasBuilderPermission(ch.EditingItemTemplate))
                {
                    ch.send("Builder permissions not set.\n\r");
                    return;
                }
                editCommand.action(ch, arguments);
                return;
            }
            else if (ch.GetItemHere(vnumString, out var item) && item.Template != null)
            {
                if (!ch.HasBuilderPermission(item.Template))
                {
                    ch.send("Builder permissions not set.\n\r");
                    return;
                }
                ch.EditingItemTemplate = item.Template;
            }
            else if (item != null && item.Template == null)
                ch.send("That item doesn't have a template.\n\r");
            else
            {
                ch.send("You don't see that here.");
                return;
            }

            ch.send("Syntax: Edit Item [vnum] [name|description|level|extraflags|wearflags|itemtypes|value|nutrition|maxcharges|liquid|material|affects|damagedice|damagemessage|weapontype|weight|maxweight]\n\r");
        }



        public static void DoEditRoom(Character ch, string args)
        {
            string vnumString = "";
            args.OneArgument(ref vnumString);
            int vnum = 0;
            RoomData room;
            if (int.TryParse(vnumString, out vnum))
            {
                args = args.OneArgument(ref vnumString);
                if (!RoomData.Rooms.TryGetValue(vnum, out room))
                {
                    ch.send("room vnum not found.\n\r");
                    return;
                }
                if (!ch.HasBuilderPermission(room))
                {
                    ch.send("Builder permissions not found.\n\r");
                    return;
                }
                ch.EditingRoom = room;
            }
            else if (args.ISEMPTY())
            {
                room = ch.Room;
                ch.EditingRoom = room;
                ch.send("OK, editing room set to current room.\n\r");
                return;
            }
            else if ("done".StringPrefix(args))
            {
                ch.EditingRoom = null;
                ch.EditingNPCTemplate = null;
                ch.EditingItemTemplate = null;
                ch.EditingArea = null;
                ch.send("OK.\n\r");
                return;
            }
            else
            {
                room = ch.EditingRoom ?? ch.Room;
            }

            if (!ch.HasBuilderPermission(room))
            {
                ch.send("Builder permissions not set.\n\r");
                return;
            }
            string commandStr = "";
            args = args.OneArgument(ref commandStr);
            ch.EditingRoom = room;
            foreach (var command in EditRoomCommands)
            {
                if (command.name.StringPrefix(commandStr))
                {
                    command.action(ch, args);
                    return;
                }
            }
            ch.send("Invalid command.\n\r");
        }

        public static void DoEditRoomDescription(Character ch, string args)
        {
            string plusminus = "";
            if (ch.EditingRoom == null)
            {
                ch.send("Edit room not found");
                return;
            }
            string newargs = args.OneArgument(ref plusminus);

            if (plusminus == "-")
            {
                if (ch.EditingRoom.Description.IndexOf("\n") >= 0)
                {
                    //ch.EditingRoom.Description = ch.EditingRoom.Description.Trim().Substring(0, ch.EditingRoom.Description.LastIndexOf('\n') - 1);
                    if (ch.EditingRoom.Description.IndexOf("\n") >= 1)
                        ch.EditingRoom.Description = ch.EditingRoom.Description.Substring(0, ch.EditingRoom.Description.LastIndexOf('\n'));
                    else
                        ch.EditingRoom.Description = "";
                }
                else
                    ch.EditingRoom.Description = "";
                //ch.editRoom.description = ch.editRoom.description.Substring(0, ch.editRoom.description.LastIndexOf('\n', ch.editRoom.description.LastIndexOf('\n')  - 1) - 1);
            }
            else if (plusminus == "+")
            {
                ch.EditingRoom.Description += (!string.IsNullOrEmpty(ch.EditingRoom.Description) && !ch.EditingRoom.Description.EndsWith("\n") && !ch.EditingRoom.Description.EndsWith("\n\r") ? "\n" : "") + newargs + "\n";
                ch.send("OK.\n\r");
            }
            else if ("extradescriptions".StringPrefix(plusminus))
            {
                if ("clear".StringPrefix(newargs))
                {
                    ch.EditingRoom.ExtraDescriptions.Clear();
                    ch.send("OK.\n\r");
                }
            }
            else
            {
                ch.EditingRoom.Description = args + Environment.NewLine;
                ch.send("OK.\n\r");
            }
            ch.EditingRoom.Area.saved = false;

        }

        public static void DoEditRoomName(Character ch, string args)
        {
            if (ch.EditingRoom == null)
            {
                ch.send("Edit room not found\n\r");
                return;
            }

            ch.EditingRoom.Name = args;

            ch.EditingRoom.Area.saved = false;
            ch.send("Done.\n\r");
        }

        public static void DoEditRoomSector(Character ch, string args)
        {
            var sectors = typeof(SectorTypes).GetEnumValues();
            var sectorNames = typeof(SectorTypes).GetEnumNames();

            if (ch.EditingRoom == null)
            {
                ch.send("Edit room not found\n\r");
                return;
            }
            else if (!ch.HasBuilderPermission(ch.EditingRoom))
            {
                ch.send("You don't have permission to edit that room.\n\r");
            }
            else if (Utility.GetEnumValueStrPrefix(args, ref ch.EditingRoom.sector))
            {
                ch.send("Sector changed to " + ch.EditingRoom.sector.ToString() + "\n\r");
                ch.EditingRoom.Area.saved = false;
            }
            else
                ch.send("Valid Sector Types are " + string.Join(", ", sectorNames) + "\n\r");

        }

        public static void DoEditRoomFlags(Character ch, string args)
        {
            if (ch.EditingRoom == null)
            {
                ch.send("Edit room not found\n\r");
                return;
            }
            else if (!ch.HasBuilderPermission(ch.EditingRoom))
            {
                ch.send("You don't have permission to edit that room.\n\r");
            }

            else if (Utility.GetEnumValues(args, ref ch.EditingRoom.flags, true))
            {
                ch.send("Room Flags changed to " + ch.EditingRoom.sector.ToString() + "\n\r");
                ch.EditingRoom.Area.saved = false;
            }
            else
                ch.send("Valid Flags are " + string.Join(", ", Utility.GetEnumValues<RoomFlags>()) + "\n\r");

        }

        public static void DoEditRoomExits(Character ch, string args)
        {
            string directionArg = "";
            string command = "";

            args = args.OneArgument(ref directionArg);
            args = args.OneArgument(ref command);

            if (string.IsNullOrEmpty(directionArg))
            {
                ch.send("What direction do you want to edit?\n\r");
                return;
            }
            Direction direction = Direction.North;
            if (!Utility.GetEnumValueStrPrefix<Direction>(directionArg, ref direction))
            {
                ch.send("Invalid direction.\n\r");
                return;
            }

            var exit = ch.EditingRoom.OriginalExits[(int)direction] != null ? ch.EditingRoom.OriginalExits[(int)direction] : (ch.EditingRoom.OriginalExits[(int)direction] = new ExitData() { source = ch.EditingRoom, direction = direction });

            if ("delete".StringPrefix(command))
            {
                if (exit.destination != null)
                    exit.destination.Area.saved = false;
                if (exit != null) ch.EditingRoom.OriginalExits[(int)direction] = null;
                for (int i = 0; i < ch.Room.exits.Length; i++)
                {
                    if (ch.EditingRoom.OriginalExits[i] != null)
                        ch.EditingRoom.exits[i] = new ExitData(ch.EditingRoom.OriginalExits[i]);
                    else ch.EditingRoom.exits[i] = null;
                }
                ch.Room.Area.saved = false;
                ch.send("Exit removed on this side\n\r");
                return;
            }
            else if ("destination".StringPrefix(command) && int.TryParse(args, out var vnum))
            {
                ;
                if (RoomData.Rooms.TryGetValue(vnum, out var room))
                {
                    exit.destination = room;
                    exit.destinationVnum = room.Vnum;
                    for (int i = 0; i < ch.Room.exits.Length; i++)
                    {
                        if (ch.EditingRoom.OriginalExits[i] != null)
                            ch.EditingRoom.exits[i] = new ExitData(ch.EditingRoom.OriginalExits[i]);
                        else ch.EditingRoom.exits[i] = null;
                    }

                    ch.EditingRoom.Area.saved = false;
                    ch.send("OK.\n\r");
                    return;
                }
                else
                {
                    ch.send("Destination vnum does not exist.");
                    return;
                }

            }
            else if ("flags".StringPrefix(command))
            {
                var flags = new List<ExitFlags>();
                Utility.GetEnumValues(args, ref flags);

                exit.flags.Clear();
                exit.originalFlags.Clear();
                exit.flags.AddRange(flags);
                exit.originalFlags.AddRange(flags);
                for (int i = 0; i < ch.Room.exits.Length; i++)
                {
                    if (ch.EditingRoom.OriginalExits[i] != null)
                        ch.EditingRoom.exits[i] = new ExitData(ch.EditingRoom.OriginalExits[i]);
                    else
                        ch.EditingRoom.exits[i] = null;
                }
                ch.EditingRoom.Area.saved = false;
                ch.send("OK.\n\r");
                ch.send("Valid flags are {0}\n", string.Join(" ", Utility.GetEnumValues<ExitFlags>()));
                return;
            }
            else if ("keywords".StringPrefix(command))
            {
                exit.keywords = args;
                for (int i = 0; i < ch.Room.exits.Length; i++)
                {
                    if (ch.EditingRoom.OriginalExits[i] != null)
                        ch.EditingRoom.exits[i] = new ExitData(ch.EditingRoom.OriginalExits[i]);
                    else
                        ch.EditingRoom.exits[i] = null;
                }
                ch.EditingRoom.Area.saved = false;
                ch.send("OK.\n\r");
                return;
            }
            else if ("display".StringPrefix(command))
            {
                exit.display = args;
                for (int i = 0; i < ch.Room.exits.Length; i++)
                {
                    if (ch.EditingRoom.OriginalExits[i] != null)
                        ch.EditingRoom.exits[i] = new ExitData(ch.EditingRoom.OriginalExits[i]);
                    else
                        ch.EditingRoom.exits[i] = null;
                }
                ch.EditingRoom.Area.saved = false;
                ch.send("OK.\n\r");
                return;
            }
            else if ("description".StringPrefix(command))
            {
                exit.description = args;
                for (int i = 0; i < ch.Room.exits.Length; i++)
                {
                    if (ch.EditingRoom.OriginalExits[i] != null)
                        ch.EditingRoom.exits[i] = new ExitData(ch.EditingRoom.OriginalExits[i]);
                    else
                        ch.EditingRoom.exits[i] = null;
                }
                ch.EditingRoom.Area.saved = false;
                ch.send("OK.\n\r");
                return;
            }
            else if ("size".StringPrefix(command))
            {
                var flags = new List<ExitFlags>();
                if (!Utility.GetEnumValue(args, ref exit.ExitSize))
                {
                    ch.send("Valid sizes are {0}.\n\r", string.Join(" ", Utility.GetEnumValues<CharacterSize>()));
                    return;
                }
                else
                {
                    for (int i = 0; i < ch.Room.exits.Length; i++)
                    {
                        if (ch.EditingRoom.OriginalExits[i] != null)
                            ch.EditingRoom.exits[i] = new ExitData(ch.EditingRoom.OriginalExits[i]);
                        else
                            ch.EditingRoom.exits[i] = null;
                    }
                    ch.EditingRoom.Area.saved = false;
                    ch.send("OK.\n\r");
                    return;
                }
            }
            else if ("keys".StringPrefix(command))
            {
                if (args.ISEMPTY())
                {
                    exit.keys.Clear();
                    for (int i = 0; i < ch.Room.exits.Length; i++)
                    {
                        if (ch.EditingRoom.OriginalExits[i] != null)
                            ch.EditingRoom.exits[i] = new ExitData(ch.EditingRoom.OriginalExits[i]);
                        else
                            ch.EditingRoom.exits[i] = null;
                    }
                    ch.EditingRoom.Area.saved = false;
                    ch.send("OK.\n\r");
                    return;
                }
                else
                {
                    var keys = args.Split(' ');
                    var valid = true;
                    foreach (var key in keys)
                    {
                        if (!int.TryParse(key, out var keyvnum))
                            valid = false;
                    }
                    if (valid)
                    {
                        exit.keys.Clear();
                        exit.keys.AddRange(from key in keys select int.Parse(key));
                        for (int i = 0; i < ch.Room.exits.Length; i++)
                        {
                            if (ch.EditingRoom.OriginalExits[i] != null)
                                ch.EditingRoom.exits[i] = new ExitData(ch.EditingRoom.OriginalExits[i]);
                            else
                                ch.EditingRoom.exits[i] = null;
                        }
                        ch.EditingRoom.Area.saved = false;
                        ch.send("OK.\n\r");
                        return;
                    }
                    else
                    {
                        ch.send("Keys must be vnums separated by spaces\n\r");
                        return;
                    }

                }
            }

            ch.send("Edit room exits {direction} [keywords, description, destination, flags, size, keys, delete] {arguments}\n\r");
        }

        public static void DoEditRoomResets(Character ch, string arguments)
        {
            arguments = arguments.OneArgumentOut(out var command);
            var room = ch.EditingRoom;

            if (room == null)
            {
                ch.send("You aren't editing a room.\n\r");
                return;
            }
            else if (!ch.HasBuilderPermission(room))
            {
                ch.send("Builder permissions not set.\n\r");
                return;
            }
            else if ("list".StringPrefix(command))
            {
                var resets = room.GetResets();
                ch.send("Resets for room {0} - {1}\n\r", room.Vnum, room.Name);
                for (int i = 0; i < resets.Count; i++)
                {
                    var reset = resets[i];
                    string SpawnName = string.Empty;
                    if ((reset.resetType == ResetTypes.Equip || reset.resetType == ResetTypes.Give || reset.resetType == ResetTypes.Put || reset.resetType == ResetTypes.Item)
                        && ItemTemplateData.Templates.TryGetValue(reset.spawnVnum, out var itemtemplate))
                    {
                        SpawnName = itemtemplate.Name;
                    }
                    else if (reset.resetType == ResetTypes.NPC && NPCTemplateData.Templates.TryGetValue(reset.spawnVnum, out var npctemplate))
                        SpawnName = npctemplate.Name;

                    if (SpawnName.ISEMPTY())
                        SpawnName = "unknown name";

                    ch.send("[{0,5:D5}]    {1}Type {2}, SpawnVnum {3} - {4}, MaxRoomCount {5}, MaxCount {6}\n\r",
                        i + 1,
                        reset.resetType == ResetTypes.Equip || reset.resetType == ResetTypes.Give || reset.resetType == ResetTypes.Put ? "    " : "",
                        reset.resetType.ToString(), reset.spawnVnum, SpawnName, reset.count, reset.maxCount);
                }
                ch.send("\n\r{0} resets.\n\r", resets.Count);
            }
            else if ("delete".StringPrefix(command))
            {
                var resets = room.GetResets();
                if (int.TryParse(arguments, out var index) && index >= 1 && index <= resets.Count)
                {
                    var reset = resets[index - 1];
                    room.Area.resets.Remove(reset);
                    room.Area.saved = false;
                    ch.send("Reset removed.\n\r");
                }
                else
                    ch.send("You must supply a valid index.\n\r");
            }
            else if ("move".StringPrefix(command))
            {
                var resets = room.GetResets();

                arguments = arguments.OneArgumentOut(out var argStartIndex);

                if (int.TryParse(argStartIndex, out var index) && index >= 1 && index <= resets.Count && int.TryParse(arguments, out var endIndex) && endIndex >= 1 && endIndex <= resets.Count)
                {
                    var reset = resets[index - 1];

                    room.Area.resets.Remove(reset);

                    if (endIndex < resets.Count) // insert before the destination index
                    {
                        var destinationReset = resets[endIndex - 1];
                        room.Area.resets.Insert(resets.IndexOf(destinationReset), reset);
                    }
                    else // insert after the destination index
                    {
                        var destinationReset = resets[endIndex - 1];

                        room.Area.resets.Insert(resets.IndexOf(destinationReset) + 1, reset);
                    }

                    room.Area.saved = false;
                    ch.send("Reset moved.\n\r");
                }
                else
                    ch.send("You must supply a valid start and end index.\n\r");
            }
            else if ("create".StringPrefix(command) || "new".StringPrefix(command))
            {
                var resets = room.GetResets();
                arguments = arguments.OneArgumentOut(out var arg1);
                arguments = arguments.OneArgumentOut(out var arg2);
                arguments = arguments.OneArgumentOut(out var arg3);
                arguments = arguments.OneArgumentOut(out var arg4);

                int spawnvnum = 0, maxroomcount = 0, maxcount = 0;
                if (!int.TryParse(arg1, out var index))
                {
                    index = resets.Count;

                }
                else
                {
                    arg1 = arg2;
                    arg2 = arg3;
                    arg3 = arg4;
                    arg4 = arguments;
                }
                index = index - 1;
                if (index <= 0 && resets.Count > 0)
                {
                    index = room.Area.resets.IndexOf(resets[resets.Count - 1]) + 1;
                }
                else if (index < 0 || resets.Count == 0)
                {
                    index = room.Area.resets.Count;
                }
                else if (index == resets.Count)
                {
                    index = room.Area.resets.IndexOf(resets[index - 1]) + 1;
                }
                else if (index >= resets.Count)
                {
                    ch.send("Index must be less than or equal to the count of resets in the room.\n\r");
                    return;
                }
                else
                    index = room.Area.resets.IndexOf(resets[index]);
                if (index < 0) index = 0;


                if (arg1.ISEMPTY() || !Utility.GetEnumValueStrPrefixOut<ResetTypes>(arg1, out var type))
                {
                    ch.send("You must supply a valid reset type.\n\r");
                    ch.send("Valid reset types are {0}.\n\r", string.Join(", ", from t in Utility.GetEnumValues<ResetTypes>() select t.ToString()));
                    ch.send("redit reset create [{0}] @spawnvnum @roomcount @maxcount\n\r", string.Join("|", from t in Utility.GetEnumValues<ResetTypes>() select t.ToString()));
                    return;
                }
                else if (arg2.ISEMPTY() || !int.TryParse(arg2, out spawnvnum))
                {
                    ch.send("You must supply a valid spawn vnum.\n\r");
                    return;
                }

                if (!arg3.ISEMPTY() && !int.TryParse(arg3, out maxroomcount))
                {
                    ch.send("Max room count must be numeric if supplied.\n\r");
                    return;
                }

                if (!arg4.ISEMPTY() && !int.TryParse(arg4, out maxcount))
                {
                    ch.send("Max count must be numeric if supplied.\n\r");
                    return;
                }

                if (maxroomcount == 0)
                {
                    maxroomcount = resets.Count(r => r.resetType == type && r.spawnVnum == spawnvnum) + 1;
                }

                if (maxcount == 0)
                {
                    maxcount = room.Area.resets.Count(r => r.resetType == type && r.spawnVnum == spawnvnum);
                }

                var reset = new ResetData()
                {
                    count = maxroomcount,
                    maxCount = maxcount,
                    resetType = type,
                    roomVnum = room.Vnum,
                    spawnVnum = spawnvnum,
                    area = room.Area
                };

                room.Area.resets.Insert(index, reset);
                room.Area.saved = false;
            }
            else
                ch.send("redit resets [list|delete @index|move @index @newindex|create {@index} @type]");
        }

        private static void DoEditNPCLevel(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            if (!int.TryParse(args.Trim(), out ch.EditingNPCTemplate.Level))
            {
                ch.send("Syntax: edit npc [vnum] level [1-60]\n\r");
                return;
            }

            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCDamageRoll(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            if (!int.TryParse(args.Trim(), out ch.EditingNPCTemplate.DamageRoll))
            {
                ch.send("Syntax: edit npc [vnum] damageroll [number]\n\r");
                return;
            }

            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCHitRoll(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            if (!int.TryParse(args.Trim(), out ch.EditingNPCTemplate.HitRoll))
            {
                ch.send("Syntax: edit npc [vnum] hitroll [number]\n\r");
                return;
            }

            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCProtects(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            var protects = new List<int>();

            foreach (var protectstring in args.Split(' '))
            {
                if (!int.TryParse(protectstring, out var vnum))
                {
                    ch.send("Syntax: edit npc [vnum] protects {vnum vnum vnum...}\n\r");
                    return;
                }
                protects.Add(vnum);
            }
            ch.EditingNPCTemplate.Protects.Clear();
            ch.EditingNPCTemplate.Protects.AddRange(protects);

            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCGold(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            if (!long.TryParse(args.Trim(), out ch.EditingNPCTemplate.Gold))
            {
                ch.send("Syntax: edit npc [vnum] gold [number]\n\r");
                return;
            }

            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCGuild(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }
            GuildData guild = null;
            if ((guild = GuildData.GuildLookup(args)) == null)
            {
                ch.send("Syntax: edit npc [vnum] guild {guild name}\n\r");
                ch.send("Valid guilds are {0}.\n\r", string.Join(", ", from g in GuildData.Guilds select g.name));
                return;
            }

            ch.EditingNPCTemplate.Guild = guild;
            ch.send("OK.\n\r");
            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCRace(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }
            Race race = null;
            if ((race = Race.GetRace(args)) == null)
            {
                ch.send("Syntax: edit npc [vnum] race {race name}\n\r");
                ch.send("Valid races are {0}.\n\r", string.Join(", ", from g in Race.Races select g.name));
                return;
            }

            ch.EditingNPCTemplate.Race = race;
            ch.send("OK.\n\r");
            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCGender(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }
            Sexes sex = Sexes.None;
            if (!Utility.GetEnumValueStrPrefix(args, ref sex))
            {
                ch.send("Syntax: edit npc [vnum] gender {sex}\n\r");
                ch.send("Valid sexes are {0}.\n\r", string.Join(", ", from g in Utility.GetEnumValues<Sexes>() select g.ToString()));
                return;
            }

            ch.EditingNPCTemplate.Sex = sex;
            ch.send("OK.\n\r");
            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCAlignment(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }
            Alignment alignment = Alignment.None;
            if (!Utility.GetEnumValueStrPrefix(args, ref alignment))
            {
                ch.send("Syntax: edit npc [vnum] alignment {alignment}\n\r");
                ch.send("Valid alignments are {0}.\n\r", string.Join(", ", from g in Utility.GetEnumValues<Alignment>() select g.ToString()));
                return;
            }

            ch.EditingNPCTemplate.Alignment = alignment;
            ch.send("OK.\n\r");
            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCFlags(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            if (!Utility.GetEnumValues(args.Trim(), ref ch.EditingNPCTemplate.Flags, true))
            {
                ch.send("Valid flags are {0}.\n\r", string.Join(", ", (from flag in Utility.GetEnumValues<ActFlags>() select flag.ToString())));
            }

            ch.EditingNPCTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditNPCImmuneFlags(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            if (!Utility.GetEnumValues(args.Trim(), ref ch.EditingNPCTemplate.ImmuneFlags, true))
            {
                ch.send("Valid flags are {0}.\n\r", string.Join(", ", (from flag in Utility.GetEnumValues<WeaponDamageTypes>() select flag.ToString())));
            }

            ch.EditingNPCTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditNPCResistFlags(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            if (!Utility.GetEnumValues(args.Trim(), ref ch.EditingNPCTemplate.ResistFlags, true))
            {
                ch.send("Valid flags are {0}.\n\r", string.Join(", ", (from flag in Utility.GetEnumValues<WeaponDamageTypes>() select flag.ToString())));
            }

            ch.EditingNPCTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditNPCVulnerableFlags(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            if (!Utility.GetEnumValues(args.Trim(), ref ch.EditingNPCTemplate.VulnerableFlags, true))
            {
                ch.send("Valid flags are {0}.\n\r", string.Join(", ", (from flag in Utility.GetEnumValues<WeaponDamageTypes>() select flag.ToString())));
            }

            ch.EditingNPCTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditNPCAffectedBy(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            if (!Utility.GetEnumValues(args.Trim(), ref ch.EditingNPCTemplate.AffectedBy, true))
            {
                ch.send("Valid flags are {0}.\n\r", string.Join(", ", (from flag in Utility.GetEnumValues<AffectFlags>() select flag.ToString())));
            }

            ch.EditingNPCTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditNPCName(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            ch.EditingNPCTemplate.Name = args.Trim();

            ch.EditingNPCTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditNPCLongDescription(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            ch.EditingNPCTemplate.LongDescription = args.Trim();

            ch.EditingNPCTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditNPCShortDescription(Character ch, string args)
        {
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            ch.EditingNPCTemplate.ShortDescription = args.Trim();

            ch.EditingNPCTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditNPCDescription(Character ch, string args)
        {
            string plusminus = "";
            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }
            string newargs = args.OneArgument(ref plusminus);

            if (plusminus == "-")
            {
                if (ch.EditingNPCTemplate.Description.IndexOf("\n") >= 0)
                {
                    //ch.EditingNPCTemplate.Description = ch.EditingNPCTemplate.Description.Substring(0, ch.EditingNPCTemplate.Description.Trim().LastIndexOf('\n') - 1);
                    if (ch.EditingNPCTemplate.Description.IndexOf("\n") >= 0)
                        ch.EditingNPCTemplate.Description = ch.EditingNPCTemplate.Description.Substring(0, ch.EditingNPCTemplate.Description.LastIndexOf('\n'));
                    else
                        ch.EditingNPCTemplate.Description = "";
                }
                else
                    ch.EditingNPCTemplate.Description = "";
                //ch.editRoom.description = ch.editRoom.description.Substring(0, ch.editRoom.description.LastIndexOf('\n', ch.editRoom.description.LastIndexOf('\n')  - 1) - 1);
            }
            else if (plusminus == "+")
            {
                ch.EditingNPCTemplate.Description += (!string.IsNullOrEmpty(ch.EditingNPCTemplate.Description) && !ch.EditingNPCTemplate.Description.EndsWith("\n") && !ch.EditingNPCTemplate.Description.EndsWith("\n\r") ? "\n" : "") + newargs + "\n";
            }
            else
            {
                ch.EditingNPCTemplate.Description = args + Environment.NewLine;
            }
            ch.EditingNPCTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditNPCDamageDice(Character ch, string args)
        {
            string damagedicesidesstring = "";
            string damagedicecountstring = "";
            string damagedicebonusstring = "";

            args = args.OneArgument(ref damagedicesidesstring);
            args = args.OneArgument(ref damagedicecountstring);
            args = args.OneArgument(ref damagedicebonusstring);

            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            if (damagedicesidesstring.ISEMPTY() || damagedicecountstring.ISEMPTY() || damagedicebonusstring.ISEMPTY() ||
                !int.TryParse(damagedicesidesstring, out ch.EditingNPCTemplate.DamageDice.DiceSides) ||
                !int.TryParse(damagedicecountstring, out ch.EditingNPCTemplate.DamageDice.DiceCount) ||
                !int.TryParse(damagedicebonusstring, out ch.EditingNPCTemplate.DamageDice.DiceBonus))
            {
                ch.send("Syntax: edit npc [vnum] damagedice [dicesides] [dicecount] [dicebonus]\n\r");
                return;
            }
            else
                ch.send("OK.\n\r");

            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCHitPointDice(Character ch, string args)
        {
            string dicesidesstring = "";
            string dicecountstring = "";
            string dicebonusstring = "";

            args = args.OneArgument(ref dicesidesstring);
            args = args.OneArgument(ref dicecountstring);
            args = args.OneArgument(ref dicebonusstring);

            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            if (dicesidesstring.ISEMPTY() || dicecountstring.ISEMPTY() || dicebonusstring.ISEMPTY() ||
                !int.TryParse(dicesidesstring, out ch.EditingNPCTemplate.HitPointDice.DiceSides) ||
                !int.TryParse(dicecountstring, out ch.EditingNPCTemplate.HitPointDice.DiceCount) ||
                !int.TryParse(dicebonusstring, out ch.EditingNPCTemplate.HitPointDice.DiceBonus))
            {
                ch.send("Syntax: edit npc [vnum] hitpointdice [dicesides] [dicecount] [dicebonus]\n\r");
                return;
            }
            else
                ch.send("OK.\n\r");

            ch.EditingNPCTemplate.Area.saved = false;
        }

        private static void DoEditNPCManaPointDice(Character ch, string args)
        {
            string dicesidesstring = "";
            string dicecountstring = "";
            string dicebonusstring = "";

            args = args.OneArgument(ref dicesidesstring);
            args = args.OneArgument(ref dicecountstring);
            args = args.OneArgument(ref dicebonusstring);

            if (ch.EditingNPCTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            if (dicesidesstring.ISEMPTY() || dicecountstring.ISEMPTY() || dicebonusstring.ISEMPTY() ||
                !int.TryParse(dicesidesstring, out ch.EditingNPCTemplate.ManaPointDice.DiceSides) ||
                !int.TryParse(dicecountstring, out ch.EditingNPCTemplate.ManaPointDice.DiceCount) ||
                !int.TryParse(dicebonusstring, out ch.EditingNPCTemplate.ManaPointDice.DiceBonus))
            {
                ch.send("Syntax: edit npc [vnum] manapointdice [dicesides] [dicecount] [dicebonus]\n\r");
                return;
            }
            else
                ch.send("OK.\n\r");

            ch.EditingNPCTemplate.Area.saved = false;
        }


        private static void DoEditItemLevel(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            if (!int.TryParse(args.Trim(), out ch.EditingItemTemplate.Level))
            {
                ch.send("Syntax: edit item [vnum] level [1-60]\n\r");
                return;
            }
            else
                ch.send("OK.\n\r");

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemWeight(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            if (!float.TryParse(args.Trim(), out ch.EditingItemTemplate.Weight))
            {
                ch.send("Syntax: edit item [vnum] weight [number]\n\r");
                return;
            }
            else
                ch.send("OK.\n\r");

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemMaxWeight(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            if (!float.TryParse(args.Trim(), out ch.EditingItemTemplate.MaxWeight))
            {
                ch.send("Syntax: edit item [vnum] maxweight [number]\n\r");
                return;
            }
            else
                ch.send("OK.\n\r");

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemValue(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            if (!int.TryParse(args.Trim(), out ch.EditingItemTemplate.Value))
            {
                ch.send("Syntax: edit item [vnum] value [number]\n\r");
                return;
            }
            else
                ch.send("OK.\n\r");

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemExtraFlags(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            ch.EditingItemTemplate.extraFlags.Clear();

            if (!Utility.GetEnumValues(args.Trim(), ref ch.EditingItemTemplate.extraFlags, true))
            {
                ch.send("Valid extra flags are {0}.\n\r", string.Join(", ", (from flag in Utility.GetEnumValues<ExtraFlags>() select flag.ToString())));
            }
            else
                ch.send("OK.\n\r");

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemWearFlags(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }
            ch.EditingItemTemplate.wearFlags.Clear();
            if (!Utility.GetEnumValues(args.Trim(), ref ch.EditingItemTemplate.wearFlags, true))
            {
                ch.send("Valid wear flags are {0}.\n\r", string.Join(", ", (from flag in Utility.GetEnumValues<WearFlags>() select flag.ToString())));
            }
            else
                ch.send("OK.\n\r");
            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemWeaponType(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            ch.EditingItemTemplate.WeaponType = WeaponTypes.None;

            if (!Utility.GetEnumValue(args.Trim(), ref ch.EditingItemTemplate.WeaponType, WeaponTypes.None))
            {
                ch.send("Valid weapon types are {0}.\n\r", string.Join(", ", (from flag in Utility.GetEnumValues<WeaponTypes>() select flag.ToString())));
            }
            else
                ch.send("OK.\n\r");

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemItemTypes(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            if (!Utility.GetEnumValues(args.Trim(), ref ch.EditingItemTemplate.itemTypes, true))
            {
                ch.send("Valid item types are {0}.\n\r", string.Join(", ", (from flag in Utility.GetEnumValues<ItemTypes>() select flag.ToString())));
            }

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemDamageMessage(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            foreach (var message in WeaponDamageMessage.WeaponDamageMessages)
            {
                if (message.Keyword.StringPrefix(args))
                {
                    ch.EditingItemTemplate.WeaponDamageType = message;
                    ch.EditingItemTemplate.Area.saved = false;
                    return;
                }
            }

            ch.send("Damage message not found, valid messages are {0}.\n\r", string.Join(", ", (from message in WeaponDamageMessage.WeaponDamageMessages select message.Keyword)));

        }

        private static void DoEditItemDamageDice(Character ch, string args)
        {
            string damagedicesidesstring = "";
            string damagedicecountstring = "";
            string damagedicebonusstring = "";

            args = args.OneArgument(ref damagedicesidesstring);
            args = args.OneArgument(ref damagedicecountstring);
            args = args.OneArgument(ref damagedicebonusstring);

            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            if (damagedicesidesstring.ISEMPTY() || damagedicecountstring.ISEMPTY() || damagedicebonusstring.ISEMPTY() ||
                !int.TryParse(damagedicesidesstring, out ch.EditingItemTemplate.DamageDice.DiceSides) ||
                !int.TryParse(damagedicecountstring, out ch.EditingItemTemplate.DamageDice.DiceCount) ||
                !int.TryParse(damagedicebonusstring, out ch.EditingItemTemplate.DamageDice.DiceBonus))
            {
                ch.send("Syntax: edit item [vnum] damagedice [dicesides] [dicecount] [dicebonus]\n\r");
                return;
            }

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemMaterial(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found");
                return;
            }

            ch.EditingItemTemplate.Material = args.Trim();

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemLiquid(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found");
                return;
            }

            ch.EditingItemTemplate.Liquid = args.Trim();

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemMaxCharges(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found");
                return;
            }

            if (!int.TryParse(args, out ch.EditingItemTemplate.MaxCharges)) ch.send("MaxCharges must be numerical.\n\r");

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemMaxDurability(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found");
                return;
            }

            if (!int.TryParse(args, out ch.EditingItemTemplate.MaxCharges)) ch.send("MaxDurability must be numerical.\n\r");

            ch.EditingItemTemplate.Area.saved = false;
        }

        private static void DoEditItemNutrition(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found");
                return;
            }

            if (!int.TryParse(args, out ch.EditingItemTemplate.Nutrition)) ch.send("Nutrition must be numerical.\n\r");

            ch.EditingItemTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditArmorClass(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found");
                return;
            }
            string ac_bash = "", ac_slash = "", ac_pierce = "", ac_exotic = "";
            args = args.OneArgument(ref ac_bash);
            args = args.OneArgument(ref ac_slash);
            args = args.OneArgument(ref ac_pierce);
            args = args.OneArgument(ref ac_exotic);
            if (ac_bash.ISEMPTY() || ac_slash.ISEMPTY() || ac_pierce.ISEMPTY() || ac_exotic.ISEMPTY() ||
                !int.TryParse(ac_bash, out ch.EditingItemTemplate.ArmorBash) ||
                !int.TryParse(ac_slash, out ch.EditingItemTemplate.ArmorSlash) ||
                !int.TryParse(ac_pierce, out ch.EditingItemTemplate.ArmorPierce) ||
                !int.TryParse(ac_exotic, out ch.EditingItemTemplate.ArmorExotic)) ch.send("Armor class must be numerical and must supply armorclass [bash slash pierce exotic].\n\r");

            ch.EditingItemTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditItemName(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found");
                return;
            }

            ch.EditingItemTemplate.Name = args.Trim();

            ch.EditingItemTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditItemLongDescription(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            ch.EditingItemTemplate.LongDescription = args.Trim();

            ch.EditingItemTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditItemNightLongDescription(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            ch.EditingItemTemplate.NightLongDescription = args.Trim();

            ch.EditingItemTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditItemNightShortDescription(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            ch.EditingItemTemplate.NightShortDescription = args.Trim();

            ch.EditingItemTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }


        private static void DoEditItemShortDescription(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit npc not found\n\r");
                return;
            }

            ch.EditingItemTemplate.ShortDescription = args.Trim();

            ch.EditingItemTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditItemDescription(Character ch, string args)
        {
            string plusminus = "";
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit npc not found");
                return;
            }
            string newargs = args.OneArgument(ref plusminus);

            if (plusminus == "-")
            {
                if (ch.EditingItemTemplate.Description.IndexOf("\n\r") >= 0)
                {
                    ch.EditingItemTemplate.Description = ch.EditingItemTemplate.Description.Substring(0, ch.EditingItemTemplate.Description.LastIndexOf('\n') - 1);
                    if (ch.EditingItemTemplate.Description.IndexOf("\n\r") >= 0)
                        ch.EditingItemTemplate.Description = ch.EditingItemTemplate.Description.Substring(0, ch.EditingItemTemplate.Description.LastIndexOf('\n'));
                    else
                        ch.EditingItemTemplate.Description = "";
                }
                else
                    ch.EditingItemTemplate.Description = "";
                //ch.editRoom.description = ch.editRoom.description.Substring(0, ch.editRoom.description.LastIndexOf('\n', ch.editRoom.description.LastIndexOf('\n')  - 1) - 1);
            }
            else if (plusminus == "+")
            {
                ch.EditingItemTemplate.Description += (!string.IsNullOrEmpty(ch.EditingItemTemplate.Description) && !ch.EditingItemTemplate.Description.Contains('\n') ? "\n" : "") + newargs + "\n";
            }
            else
            {
                ch.EditingItemTemplate.Description = args + Environment.NewLine;
            }
            ch.EditingItemTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditItemSpells(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }
            string arg = "";
            string levelstring = "";
            ch.EditingItemTemplate.spells.Clear();
            while (!args.ISEMPTY())
            {
                args = args.OneArgument(ref levelstring);
                args = args.OneArgument(ref arg);
                int level = 0;
                SkillSpell skill;
                if (!int.TryParse(levelstring, out level) || (skill = SkillSpell.SkillLookup(arg)) == null)
                {
                    ch.send("Syntax: edit item [vnum] spells [spell level] [spell name] [spell level] [spell name].\n Skill/spell {0} not found.\n\r", arg);
                    return;
                }
                ch.EditingItemTemplate.spells.Add(new ItemSpellData(level, skill.name));
            }


            ch.EditingItemTemplate.Area.saved = false;
            ch.send("OK.\n\r");
        }

        private static void DoEditItemAffects(Character ch, string args)
        {
            if (ch.EditingItemTemplate == null)
            {
                ch.send("Edit item not found\n\r");
                return;
            }

            ApplyTypes apply = ApplyTypes.None;

            string applytypestring = "";
            string applyvaluestring = "";
            int applyvalue = 0;
            args = args.OneArgument(ref applytypestring);
            args = args.OneArgument(ref applyvaluestring);
            if (!applytypestring.ISEMPTY() && applytypestring == "-")
            {
                if (ch.EditingItemTemplate.affects.Count > 0)
                {
                    ch.EditingItemTemplate.affects.RemoveAt(ch.EditingItemTemplate.affects.Count - 1);
                    ch.send("OK.\n\r");
                }
                else
                    ch.send("No more affects on that item.\n\r");
                return;
            }
            if (!applytypestring.ISEMPTY() && !applyvaluestring.ISEMPTY() && int.TryParse(applyvaluestring, out applyvalue) && Utility.GetEnumValue(applytypestring, ref apply, ApplyTypes.None))
            {
                foreach (var aff in ch.EditingItemTemplate.affects.ToArray())
                {
                    if (aff.where == AffectWhere.ToObject && aff.location == apply)
                    {
                        if (aff.modifier == 0)
                        {
                            ch.EditingItemTemplate.affects.Remove(aff);
                        }
                        else
                            aff.modifier = applyvalue;
                        return;
                    }
                }

                ch.EditingItemTemplate.affects.Add(new AffectData() { where = AffectWhere.ToObject, location = apply, modifier = applyvalue, duration = -1 });
                ch.EditingItemTemplate.Area.saved = false;
                ch.send("OK.\n\r");
            }
            else
            {
                ch.send("Edit item [vnum] affects [type] [value]\n\r");
                ch.send("Types: {0}", string.Join(", ", Utility.GetEnumValues<ApplyTypes>()));
            }

        }
        public static void DoDig(Character ch, string arguments)
        {
            string directionArg = "";
            string vnumArg = "";
            arguments = arguments.OneArgument(ref directionArg);

            arguments = arguments.OneArgument(ref vnumArg);

            if (string.IsNullOrEmpty(directionArg))
            {
                ch.send("What direction do you want to create a new room in?\n\r");
                return;
            }
            Direction direction = Direction.North;
            if (!Utility.GetEnumValueStrPrefix<Direction>(directionArg, ref direction))
            {
                ch.send("Invalid direction.\n\r");
                return;
            }

            if (!ch.HasBuilderPermission(ch.Room))
            {
                ch.send("Builder permissions not set.\n\r");
                return;
            }

            int vnum = 0;
            var area = ch.EditingArea ?? ch.Room.Area;
            if (int.TryParse(vnumArg, out int result))
                vnum = result;
            else if ("delete".StringPrefix(vnumArg))
            {
                var exit = ch.Room.OriginalExits[(int)direction];
                if (exit != null) ch.Room.OriginalExits[(int)direction] = null;
                for (int i = 0; i < ch.Room.exits.Length; i++)
                {
                    if (ch.Room.OriginalExits[i] != null)
                        ch.Room.exits[i] = new ExitData(ch.Room.OriginalExits[i]);
                    else
                        ch.Room.exits[i] = null;
                }
                ch.send("Exit removed on this side\n\r");
            }
            else if ((vnumArg.ISEMPTY() || vnum == 0) && area != null)
            {
                vnum = area.rooms.Count > 0 ? area.rooms.Max(r => r.Key) + 1 : area.vnumStart;
            }
            else
            {
                ch.send("Invalid vnum.\n\r");
                return;
            }
            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };
            RoomData room;
            if (RoomData.Rooms.TryGetValue(vnum, out room))
            {
                //ch.send("Not yet implemented.\n\r");
            }
            else
            {

                room = new RoomData();
                room.Vnum = vnum;
                if (vnumArg.StringCmp("copy") || arguments.StringCmp("copy"))
                {
                    room.Name = ch.Room.Name;
                    room.Description = ch.Room.Description;
                    room.sector = ch.Room.sector;
                }
                room.Area = area;
                room.Area.rooms.Add(vnum, room);
                RoomData.Rooms.Add(vnum, room);
            }
            if (!ch.HasBuilderPermission(room))
            {
                ch.send("Builder permissions not set.\n\r");
                return;
            }

            room.Area.saved = false;
            ch.Room.Area.saved = false;
            var revDirection = reverseDirections[direction];
            var flags = new List<ExitFlags>();
            Utility.GetEnumValues(arguments, ref flags);
            room.OriginalExits[(int)revDirection] = new ExitData() { destination = ch.Room, destinationVnum = ch.Room.Vnum, direction = revDirection, description = "", flags = new List<ExitFlags>(flags), originalFlags = new List<ExitFlags>(flags) };
            ch.Room.OriginalExits[(int)direction] = new ExitData() { destination = room, direction = direction, description = "", flags = new List<ExitFlags>(flags), originalFlags = new List<ExitFlags>(flags) };
            for (int i = 0; i < ch.Room.exits.Length; i++)
            {
                if (ch.Room.OriginalExits[i] != null)
                    ch.Room.exits[i] = new ExitData(ch.Room.OriginalExits[i]);
                else
                    ch.Room.exits[i] = null;
                if (room.OriginalExits[i] != null)
                    room.exits[i] = new ExitData(room.OriginalExits[i]);
                else room.exits[i] = null;
            }
            ch.moveChar(direction, true, false);
            //ch.RemoveCharacterFromRoom();
            //ch.AddCharacterToRoom(room);
            //Character.DoLook(ch, "auto");
        }

        public static void DoRenumber(Character ch, string arguments)
        {
            string minVnumFromStr = "", maxVnumFromStr = "", vnumToStr = "", vnumToMaxStr = "";
            int minVnumFrom, maxVnumFrom, vnumTo, vnumMaxTo;
            arguments = arguments.OneArgument(ref minVnumFromStr);
            arguments = arguments.OneArgument(ref maxVnumFromStr);
            arguments = arguments.OneArgument(ref vnumToStr);
            arguments = arguments.OneArgument(ref vnumToMaxStr);

            if (
                minVnumFromStr.ISEMPTY() ||
                maxVnumFromStr.ISEMPTY() ||
                vnumToStr.ISEMPTY() ||
                !int.TryParse(minVnumFromStr, out minVnumFrom) ||
                !int.TryParse(maxVnumFromStr, out maxVnumFrom) ||
                !int.TryParse(vnumToStr, out vnumTo) ||
                !int.TryParse(vnumToMaxStr, out vnumMaxTo) ||
                maxVnumFrom <= minVnumFrom ||
                vnumMaxTo < vnumTo ||
                (maxVnumFrom - minVnumFrom) > (vnumMaxTo - vnumTo))
            {
                ch.send("Syntax: Renumber [FromVnumMin] [FromVnumMax] [VnumTo] [VnumToMax]\n\r");
            }
            else
            {
                foreach (var area in AreaData.Areas)
                {
                    if (area.vnumStart == minVnumFrom && area.vnumEnd == maxVnumFrom)
                    {
                        area.vnumStart = vnumTo;
                        area.vnumEnd = vnumMaxTo;// (maxVnumFrom - minVnumFrom) + vnumTo;
                        foreach (var reset in area.resets)
                        {
                            if (reset.roomVnum >= minVnumFrom && reset.roomVnum <= maxVnumFrom)
                                reset.roomVnum = reset.roomVnum - minVnumFrom + vnumTo;

                            if (reset.spawnVnums.ISEMPTY())
                            {
                                if (reset.spawnVnum >= minVnumFrom && reset.spawnVnum <= maxVnumFrom)
                                    reset.spawnVnum = reset.spawnVnum - minVnumFrom + vnumTo;
                            }
                            else
                            {
                                var spawnvnums = from vnum in reset.spawnVnums.Split(' ') select int.Parse(vnum);
                                var newvnums = new List<int>();

                                foreach (var vnum in spawnvnums)
                                {
                                    if (vnum >= minVnumFrom && vnum <= maxVnumFrom)
                                        newvnums.Add(vnum - minVnumFrom + vnumTo);
                                    else
                                        newvnums.Add(vnum);
                                }
                                reset.spawnVnums = string.Join(" ", newvnums);

                            }
                        }
                    }
                }


                for (var vnum = minVnumFrom; vnum <= maxVnumFrom; vnum++)
                {
                    RoomData room;
                    NPCTemplateData npcTemplate;
                    ItemTemplateData itemTemplate;

                    if (RoomData.Rooms.TryGetValue(vnum, out room))
                    {
                        room.Area.saved = false;
                        room.Vnum = room.Vnum - minVnumFrom + vnumTo;
                        RoomData.Rooms.Remove(vnum);
                        RoomData.Rooms.Add(room.Vnum, room);
                    }

                    if (NPCTemplateData.Templates.TryGetValue(vnum, out npcTemplate))
                    {
                        npcTemplate.Area.saved = false;
                        npcTemplate.Vnum = npcTemplate.Vnum - minVnumFrom + vnumTo;

                        NPCTemplateData.Templates.Remove(vnum);
                        NPCTemplateData.Templates.Add(npcTemplate.Vnum, npcTemplate);
                    }

                    if (ItemTemplateData.Templates.TryGetValue(vnum, out itemTemplate))
                    {
                        itemTemplate.Area.saved = false;
                        itemTemplate.Vnum = itemTemplate.Vnum - minVnumFrom + vnumTo;

                        ItemTemplateData.Templates.Remove(vnum);
                        ItemTemplateData.Templates.Add(itemTemplate.Vnum, itemTemplate);
                    }

                    if (Quest.Quests.TryGetValue(vnum, out var quest))
                    {
                        quest.Area.saved = false;
                        quest.Vnum = quest.Vnum - minVnumFrom + vnumTo;

                        Quest.Quests.Remove(vnum);
                        Quest.Quests.Add(quest.Vnum, quest);
                    }

                }

                foreach (var otherroom in RoomData.Rooms)
                {
                    for (int i = 0; i < otherroom.Value.exits.Length; i++)
                    {
                        var exit = otherroom.Value.exits[i];
                        if (exit != null && exit.destination != null && exit.destination.Vnum >= vnumTo && exit.destination.Vnum <= vnumMaxTo)
                        {
                            exit.destinationVnum = exit.destination.Vnum;
                            otherroom.Value.Area.saved = false;
                        }
                        exit = otherroom.Value.OriginalExits[i];
                        if (exit != null && exit.destination != null && exit.destination.Vnum >= vnumTo && exit.destination.Vnum <= vnumMaxTo)
                        {
                            exit.destinationVnum = exit.destination.Vnum;
                        }
                    }
                    foreach (var exit in otherroom.Value.exits)
                    {
                        if (exit != null && exit.destination != null && exit.destination.Vnum >= vnumTo && exit.destination.Vnum <= vnumMaxTo)
                        {
                            exit.destinationVnum = exit.destination.Vnum;
                            otherroom.Value.Area.saved = false;
                        }
                    }
                }

                AreaData.DoASaveWorlds(ch, "");
            }

        }

        public static void DoNextVnum(Character ch, string arguments)
        {
            var area = ch.EditingArea ?? ch.Room.Area;

            ch.send("Next Vnums: Room {0}, NPC {1}, Item {2}\n\r",
                area.rooms.Count > 0 ? area.rooms.Max(r => r.Key) + 1 : area.vnumStart,
                area.NPCTemplates.Count > 0 ? area.NPCTemplates.Max(n => n.Key) + 1 : area.vnumStart,
                area.ItemTemplates.Count > 0 ? area.ItemTemplates.Max(i => i.Key) + 1 : area.vnumStart);
        }

        public static void DoFreeVnumRanges(Character ch, string arguments)
        {
            AreaData last = null;
            using (new Character.Page(ch))
                foreach (var area in AreaData.Areas.OrderBy(a => a.vnumStart))
                {
                    if (last != null)
                        ch.send("Vnum range {0} - {1}  between {2} and {3}\n\r", last.vnumEnd + 1, area.vnumStart - 1, last.name, area.name);

                    last = area;

                }
        }

        public static void DoBuilder(Character ch, string arguments)
        {
            string areaname = "";
            string op = "";
            arguments = arguments.OneArgument(ref areaname);
            arguments = arguments.OneArgument(ref op);
            if (!areaname.ISEMPTY() && !arguments.ISEMPTY() && !op.ISEMPTY())
            {

                var area = AreaData.Areas.FirstOrDefault(a => a.name.IsName(areaname));
                var character = game.Instance.Info.connections.FirstOrDefault(p => p.Name.IsName(arguments, true));


                if (character != null && area != null)
                {
                    if (op == "+")
                        area.builders += (area.builders.Length > 0 ? " " : "") + character.Name;
                    else if (op == "-")
                    {
                        var newbuilders = new StringBuilder();
                        var builders = area.builders;
                        string builder = "";
                        while (!builders.ISEMPTY())
                        {
                            builders = builders.OneArgument(ref builder);
                            if (!builder.ISEMPTY() && !builder.IsName(character.Name, true))
                                newbuilders.Append((newbuilders.Length > 0 ? " " : "") + builder);
                        }
                        area.builders = newbuilders.ToString();
                        ch.send("OK - Builders now " + area.builders + "\n\r");
                    }
                    else
                        ch.send("+ or -\n\r");
                }
                else
                    ch.send("Character or area not found.\n\r");
            }
            else
                ch.send("Builder \"area name\" [+/-] \"character name\"\n\r");
        }

        public static void DoEditRoomExtraDescriptions(Character ch, string arguments)
        {
            arguments = arguments.OneArgumentOut(out var command);
            var room = ch.EditingRoom;

            if (room == null)
            {
                ch.send("You aren't editing a room.\n\r");
                return;
            }
            else if (!ch.HasBuilderPermission(room))
            {
                ch.send("Builder permissions not set.\n\r");
                return;
            }
            else if ("list".StringPrefix(command))
            {
                arguments.OneArgumentOut(out var strindex);
                ExtraDescription ed = null;

                if (!strindex.ISEMPTY() && (int.TryParse(strindex, out var index) || (ed = room.ExtraDescriptions.FirstOrDefault(e => e.Keywords.IsName(strindex))) != null))
                {
                    ch.send("Keywords: {0}\n\rDescription: {1}\n\r", ed.Keywords, ed.Description);
                }
                else if (!strindex.ISEMPTY())
                {
                    ch.send("ExtraDescription not found or index out of range.\n\r");
                }
                else
                {
                    var extradescriptions = room.ExtraDescriptions;
                    ch.send("Extra descriptions for item {0} - {1}\n\r", room.Vnum, room.Name);
                    for (int i = 0; i < extradescriptions.Count; i++)
                    {
                        ed = extradescriptions[i];

                        ch.send("[{0,5:D5}]    Keywords {1}\n\r",
                            i + 1, ed.Keywords);
                    }
                    ch.send("\n\r{0} extra descriptions.\n\r", extradescriptions.Count);
                }
            }
            else if ("delete".StringPrefix(command))
            {
                var eds = room.ExtraDescriptions;
                ExtraDescription ed = null;
                if ((int.TryParse(arguments, out var index) && index >= 1 && index <= eds.Count && (ed = eds[index - 1]) != null) || (ed = room.ExtraDescriptions.FirstOrDefault(e => e.Keywords.IsName(arguments))) != null)
                {
                    room.ExtraDescriptions.Remove(ed);
                    room.Area.saved = false;
                    ch.send("Extra description removed.\n\r");
                }
                else
                    ch.send("You must supply a valid index or keyword.\n\r");
            }
            else if ("move".StringPrefix(command))
            {
                var eds = room.ExtraDescriptions;

                arguments = arguments.OneArgumentOut(out var argStartIndex);

                if (int.TryParse(argStartIndex, out var index) && index >= 1 && index <= eds.Count && int.TryParse(arguments, out var endIndex) && endIndex >= 1 && endIndex <= eds.Count)
                {
                    var ed = eds[index - 1];

                    room.ExtraDescriptions.Remove(ed);

                    if (endIndex < eds.Count) // insert before the destination index
                    {
                        var destinationReset = eds[endIndex - 1];
                        room.ExtraDescriptions.Insert(eds.IndexOf(destinationReset), ed);
                    }
                    else // insert after the destination index
                    {
                        var destinationReset = eds[endIndex - 1];

                        room.ExtraDescriptions.Insert(eds.IndexOf(destinationReset) + 1, ed);
                    }

                    room.Area.saved = false;
                    ch.send("Extra description moved.\n\r");
                }
                else
                    ch.send("You must supply a valid start and end index.\n\r");
            }
            else if ("create".StringPrefix(command) || "new".StringPrefix(command))
            {
                var eds = room.ExtraDescriptions;
                arguments.OneArgumentOut(out var keywords);

                if (!int.TryParse(keywords, out var index))
                {
                    index = eds.Count;
                }
                else
                {
                    arguments = arguments.OneArgument(ref keywords);
                }
                index = index - 1;
                if (index <= 0 && eds.Count > 0)
                {
                    index = room.ExtraDescriptions.IndexOf(eds[eds.Count - 1]) + 1;
                }
                else if (index < 0 || eds.Count == 0)
                {
                    index = room.ExtraDescriptions.Count;
                }
                else if (index == eds.Count)
                {
                    index = room.ExtraDescriptions.IndexOf(eds[index - 1]) + 1;
                }
                else if (index >= eds.Count)
                {
                    ch.send("Index must be less than or equal to the count of resets in the room.\n\r");
                    return;
                }
                else
                    index = room.ExtraDescriptions.IndexOf(eds[index]);
                if (index < 0) index = 0;


                if (keywords.ISEMPTY())
                {
                    ch.send("You must supply keywords.\n\r");
                    return;
                }

                var ed = new ExtraDescription(arguments, "");

                room.ExtraDescriptions.Insert(index, ed);
                ch.send("ExtraDescription added at index {0}.\n\r", index + 1);
                room.Area.saved = false;
            }
            else if ("edit".StringPrefix(command))
            {
                var eds = room.ExtraDescriptions;
                arguments.OneArgumentOut(out var strindex);
                string modifier = "";
                ExtraDescription ed = null;
                if (!int.TryParse(strindex, out var index) && (ed = room.ExtraDescriptions.FirstOrDefault(e => e.Keywords.IsName(strindex))) == null)
                {
                    index = eds.Count;
                }
                else if (ed == null)
                {
                    arguments = arguments.OneArgument(ref strindex);
                    arguments.OneArgument(ref modifier);


                    index = index - 1;
                    if (index <= 0 && eds.Count > 0)
                    {
                        index = room.ExtraDescriptions.IndexOf(eds[eds.Count - 1]) + 1;
                    }
                    else if (index < 0 || eds.Count == 0)
                    {
                        index = room.ExtraDescriptions.Count;
                    }
                    else if (index == eds.Count)
                    {
                        index = room.ExtraDescriptions.IndexOf(eds[index - 1]) + 1;
                    }
                    else if (index >= eds.Count)
                    {
                        ch.send("Index must be less than or equal to the count of resets in the room.\n\r");
                        return;
                    }
                    else
                        index = room.ExtraDescriptions.IndexOf(eds[index]);
                    if (index < 0) index = 0;

                    ed = room.ExtraDescriptions[index];
                }
                else
                {
                    arguments = arguments.OneArgument(ref strindex);
                    arguments.OneArgument(ref modifier);
                }
                if (ed != null)
                {
                    if (modifier == "+")
                    {
                        arguments = arguments.OneArgument(ref modifier).Replace("\n\r", "\n").Replace("\r\n", "\n");
                        ed.Description += ((ed.Description.Length > 0 && !ed.Description.EndsWith("\n")) ? "\n" : "") + arguments + "\n";
                    }
                    else if (modifier == "-")
                    {
                        var newlineindex = ed.Description.LastIndexOf("\n");
                        arguments = arguments.OneArgument(ref modifier).Replace("\n\r", "\n").Replace("\r\n", "\n");
                        ed.Description = newlineindex < 1 ? "" : ed.Description.Substring(0, newlineindex);
                    }
                    else
                        ed.Description = arguments.Replace("\n\r", "\n").Replace("\r\n", "\n") + "\n";
                    ch.send("OK.\n\r");
                }
            }
            else
                ch.send("redit extradescriptions [list|delete @index|move @index @newindex|create @keywords|edit @index {+|-} $text]");
        } // end EditRoomExtraDescriptions

        public static void DoEditItemExtraDescriptions(Character ch, string arguments)
        {
            arguments = arguments.OneArgumentOut(out var command);
            var item = ch.EditingItemTemplate;

            if (item == null)
            {
                ch.send("You aren't editing an item.\n\r");
                return;
            }
            else if (!ch.HasBuilderPermission(item))
            {
                ch.send("Builder permissions not set.\n\r");
                return;
            }
            else if ("list".StringPrefix(command))
            {
                arguments.OneArgumentOut(out var strindex);
                ExtraDescription ed = null;

                if (!strindex.ISEMPTY() && (int.TryParse(strindex, out var index) || (ed = item.ExtraDescriptions.FirstOrDefault(e => e.Keywords.IsName(strindex))) != null))
                {
                    ch.send("Keywords: {0}\n\rDescription: {1}\n\r", ed.Keywords, ed.Description);
                }
                else if (!strindex.ISEMPTY())
                {
                    ch.send("ExtraDescription not found or index out of range.\n\r");
                }
                else
                {
                    var extradescriptions = item.ExtraDescriptions;
                    ch.send("Extra descriptions for item {0} - {1}\n\r", item.Vnum, item.Name);
                    for (int i = 0; i < extradescriptions.Count; i++)
                    {
                        ed = extradescriptions[i];

                        ch.send("[{0,5:D5}]    Keywords {1}\n\r",
                            i + 1, ed.Keywords);
                    }
                    ch.send("\n\r{0} extra descriptions.\n\r", extradescriptions.Count);
                }
            }
            else if ("delete".StringPrefix(command))
            {
                var eds = item.ExtraDescriptions;
                ExtraDescription ed = null;
                if ((int.TryParse(arguments, out var index) && index >= 1 && index <= eds.Count && (ed = eds[index - 1]) != null) || (ed = item.ExtraDescriptions.FirstOrDefault(e => e.Keywords.IsName(arguments))) != null)
                {
                    item.ExtraDescriptions.Remove(ed);
                    item.Area.saved = false;
                    ch.send("Extra description removed.\n\r");
                }
                else
                    ch.send("You must supply a valid index or keyword.\n\r");
            }
            else if ("move".StringPrefix(command))
            {
                var eds = item.ExtraDescriptions;

                arguments = arguments.OneArgumentOut(out var argStartIndex);

                if (int.TryParse(argStartIndex, out var index) && index >= 1 && index <= eds.Count && int.TryParse(arguments, out var endIndex) && endIndex >= 1 && endIndex <= eds.Count)
                {
                    var ed = eds[index - 1];

                    item.ExtraDescriptions.Remove(ed);

                    if (endIndex < eds.Count) // insert before the destination index
                    {
                        var destinationReset = eds[endIndex - 1];
                        item.ExtraDescriptions.Insert(eds.IndexOf(destinationReset), ed);
                    }
                    else // insert after the destination index
                    {
                        var destinationReset = eds[endIndex - 1];

                        item.ExtraDescriptions.Insert(eds.IndexOf(destinationReset) + 1, ed);
                    }

                    item.Area.saved = false;
                    ch.send("Extra description moved.\n\r");
                }
                else
                    ch.send("You must supply a valid start and end index.\n\r");
            }
            else if ("create".StringPrefix(command) || "new".StringPrefix(command))
            {
                var eds = item.ExtraDescriptions;
                arguments.OneArgumentOut(out var keywords);

                if (!int.TryParse(keywords, out var index))
                {
                    index = eds.Count;
                }
                else
                {
                    arguments = arguments.OneArgument(ref keywords);
                }
                index = index - 1;
                if (index <= 0 && eds.Count > 0)
                {
                    index = item.ExtraDescriptions.IndexOf(eds[eds.Count - 1]) + 1;
                }
                else if (index < 0 || eds.Count == 0)
                {
                    index = item.ExtraDescriptions.Count;
                }
                else if (index == eds.Count)
                {
                    index = item.ExtraDescriptions.IndexOf(eds[index - 1]) + 1;
                }
                else if (index >= eds.Count)
                {
                    ch.send("Index must be less than or equal to the count of resets in the room.\n\r");
                    return;
                }
                else
                    index = item.ExtraDescriptions.IndexOf(eds[index]);
                if (index < 0) index = 0;


                if (keywords.ISEMPTY())
                {
                    ch.send("You must supply keywords.\n\r");
                    return;
                }

                var ed = new ExtraDescription(arguments, "");

                item.ExtraDescriptions.Insert(index, ed);
                ch.send("ExtraDescription added at index {0}.\n\r", index + 1);
                item.Area.saved = false;
            }
            else if ("edit".StringPrefix(command))
            {
                var eds = item.ExtraDescriptions;
                arguments.OneArgumentOut(out var strindex);
                string modifier = "";
                ExtraDescription ed = null;
                if (!int.TryParse(strindex, out var index) && (ed = item.ExtraDescriptions.FirstOrDefault(e => e.Keywords.IsName(strindex))) == null)
                {
                    index = eds.Count;
                }
                else if (ed == null)
                {
                    arguments = arguments.OneArgument(ref strindex);
                    arguments.OneArgument(ref modifier);


                    index = index - 1;
                    if (index <= 0 && eds.Count > 0)
                    {
                        index = item.ExtraDescriptions.IndexOf(eds[eds.Count - 1]) + 1;
                    }
                    else if (index < 0 || eds.Count == 0)
                    {
                        index = item.ExtraDescriptions.Count;
                    }
                    else if (index == eds.Count)
                    {
                        index = item.ExtraDescriptions.IndexOf(eds[index - 1]) + 1;
                    }
                    else if (index >= eds.Count)
                    {
                        ch.send("Index must be less than or equal to the count of resets in the room.\n\r");
                        return;
                    }
                    else
                        index = item.ExtraDescriptions.IndexOf(eds[index]);
                    if (index < 0) index = 0;

                    ed = item.ExtraDescriptions[index];
                }
                else
                {
                    arguments = arguments.OneArgument(ref strindex);
                    arguments.OneArgument(ref modifier);
                }
                if (ed != null)
                {
                    if (modifier == "+")
                    {
                        arguments = arguments.OneArgument(ref modifier).Replace("\n\r", "\n").Replace("\r\n", "\n");
                        ed.Description += ((ed.Description.Length > 0 && !ed.Description.EndsWith("\n")) ? "\n" : "") + arguments + "\n";
                    }
                    else if (modifier == "-")
                    {
                        var newlineindex = ed.Description.LastIndexOf("\n");
                        arguments = arguments.OneArgument(ref modifier).Replace("\n\r", "\n").Replace("\r\n", "\n");
                        ed.Description = newlineindex < 1 ? "" : ed.Description.Substring(0, newlineindex);
                    }
                    else
                        ed.Description = arguments.Replace("\n\r", "\n").Replace("\r\n", "\n") + "\n";
                    ch.send("OK.\n\r");
                }
            }
            else
                ch.send("oedit extradescriptions [list|delete @index|move @index @newindex|create @keywords|edit @index {+|-} $text]");
        } // end EditItemExtraDescriptions

        public static void DoEditArea(Character ch, string args)
        {
            string vnumString = "";
            args.OneArgument(ref vnumString);
            int vnum = 0;
            AreaData area = null;
            if (int.TryParse(vnumString, out vnum))
            {
                args = args.OneArgument(ref vnumString);
                if ((area = (from a in AreaData.Areas where a.vnumStart >= vnum && a.vnumEnd <= vnum select a).FirstOrDefault()) == null)
                {
                    ch.send("area vnum not found.\n\r");
                    return;
                }
                else if (!ch.HasBuilderPermission(area))
                {
                    ch.send("Builder permissions not found.\n\r");
                    return;
                }
                else
                    ch.EditingArea = area;
            }
            else if (!vnumString.ISEMPTY() && (area = (from a in AreaData.Areas where a.name.IsName(vnumString) select a).FirstOrDefault()) == null)
            {
                ch.send("Area not found.\n\r");
            }
            //else if (!ch.HasBuilderPermission(area))
            //{
            //    ch.send("Builder permissions not found.\n\r");
            //    return;
            //}
            //else if (args.ISEMPTY())
            //{
            //    area = ch.Room.Area;
            //    ch.EditingArea = area;
            //    ch.send("OK, editing area set to current room's area.\n\r");
            //    return;
            //}
            else if ("done".StringPrefix(args))
            {
                ch.EditingRoom = null;
                ch.EditingNPCTemplate = null;
                ch.EditingItemTemplate = null;
                ch.EditingArea = null;
                ch.send("OK.\n\r");
                return;
            }

            if (area == null)
            {
                //ch.send("Area not found.\n\r");
                //return;
                area = (area ?? ch.EditingArea) ?? ch.Room.Area;
            }
            ch.EditingArea = area;
            if (!ch.HasBuilderPermission(area))
            {
                ch.send("Builder permissions not set.\n\r");
                return;
            }
            string commandStr = "";
            args = args.OneArgument(ref commandStr);
            ch.EditingArea = area;
            foreach (var command in EditAreaCommands)
            {
                if (command.name.StringPrefix(commandStr))
                {
                    command.action(ch, args);
                    return;
                }
            }
            ch.send("Invalid command.\n\r");
        }

        public static void DoEditAreaName(Character ch, string args)
        {
            if (ch.EditingArea == null)
            {
                ch.send("Edit area not found\n\r");
                return;
            }
            else if (!ch.HasBuilderPermission(ch.EditingArea))
            {
                ch.send("You don't have permission to edit that area.\n\r");
            }
            else
            {
                ch.EditingArea.name = args;

                ch.EditingArea.saved = false;
                ch.send("Done.\n\r");
            }
        }

        public static void DoEditAreaCredits(Character ch, string args)
        {
            if (ch.EditingArea == null)
            {
                ch.send("Edit area not found\n\r");
                return;
            }
            else if (!ch.HasBuilderPermission(ch.EditingArea))
            {
                ch.send("You don't have permission to edit that area.\n\r");
            }
            else
            {
                ch.EditingArea.credits = args;

                ch.EditingArea.saved = false;
                ch.send("Done.\n\r");
            }
        }

        public static void DoEditAreaOverRoomVnum(Character ch, string args)
        {
            if (ch.EditingArea == null)
            {
                ch.send("Edit area not found\n\r");
                return;
            }
            else if (!ch.HasBuilderPermission(ch.EditingArea))
            {
                ch.send("You don't have permission to edit that area.\n\r");
            }
            else if (!int.TryParse(args, out int Vnum) || !RoomData.Rooms.TryGetValue(Vnum, out var room))
            {
                ch.send("Room with specified vnum not found.\n\r");
            }
            else
            {
                ch.EditingArea.OverRoomVnum = Vnum;

                ch.EditingArea.saved = false;
                ch.send("Done.\n\r");
            }
        }

        public static void DoHEdit(Character ch, string args)
        {
            DoEditHelp(ch, args);
        }

        public static void DoEditHelp(Character ch, string args)
        {
            string arg1 = "";
            args = args.OneArgument(ref arg1);
            if ("list".StringPrefix(arg1))
            {
                IEnumerable<HelpData> helps = null;

                if (ch.EditingArea != null)
                {
                    helps = ch.EditingArea.Helps;
                }
                else if (!args.ISEMPTY())
                    helps = from help in HelpData.Helps where help.vnum.ToString().StringPrefix(args) || help.keyword.IsName(args) select help;
                else
                    helps = HelpData.Helps;

                foreach(var help in helps)
                {
                    ch.send("{0} :: {1} - {2}\n\r", help.area.name, help.vnum, help.keyword);
                }
            }
            else if ("create".StringPrefix(arg1))
            {

                if (int.TryParse(args, out var vnum))
                {
                    var area = AreaData.Areas.FirstOrDefault(a => vnum >= a.vnumStart && vnum <= a.vnumEnd);
                    if (area != null)
                    {
                        area.Helps.Add(ch.EditingHelp = new HelpData());
                        HelpData.Helps.Add(ch.EditingHelp);
                        ch.EditingHelp.area = area;
                        ch.EditingHelp.vnum = vnum;
                        ch.EditingHelp.keyword = "";
                        ch.EditingHelp.text = "";
                        ch.EditingHelp.lastEditedOn = DateTime.Now;
                        ch.EditingHelp.lastEditedBy = ch.Name;
                        ch.EditingHelp.area.saved = false;
                    }
                    else
                        ch.send("Area with a vnum range containing that vnum not found.\n\r");
                }
                else if (ch.EditingArea != null)
                {
                    if (HelpData.Helps.Any(h => h.keyword.IsName(args)))
                    {
                        ch.send("A help with that keyword already exists.\n\r");
                    }
                    else
                    {
                        var area = ch.EditingArea;
                        area.Helps.Add(ch.EditingHelp = new HelpData());
                        HelpData.Helps.Add(ch.EditingHelp);
                        ch.EditingHelp.area = area;
                        ch.EditingHelp.vnum = Math.Max(area.vnumStart, area.Helps.Any() ? area.Helps.Max(h => h.vnum) + 10 : 1); ;
                        ch.EditingHelp.keyword = "";
                        ch.EditingHelp.text = "";
                        ch.EditingHelp.lastEditedOn = DateTime.Now;
                        ch.EditingHelp.lastEditedBy = ch.Name;
                        ch.EditingHelp.area.saved = false;
                    }
                }
                else
                {
                    var area = AreaData.Areas.FirstOrDefault(a => a.name == "Help");
                    area.Helps.Add(ch.EditingHelp = new HelpData());
                    HelpData.Helps.Add(ch.EditingHelp);
                    ch.EditingHelp.area = area;
                    ch.EditingHelp.vnum = Math.Max(area.vnumStart, area.Helps.Any() ? area.Helps.Max(h => h.vnum) + 10 : 1); ;
                    ch.EditingHelp.keyword = args;
                    ch.EditingHelp.text = "";
                    ch.EditingHelp.lastEditedOn = DateTime.Now;
                    ch.EditingHelp.lastEditedBy = ch.Name;
                    ch.EditingHelp.area.saved = false;
                    ch.send("OK.\n\r");
                }
            }
            else if ("edit".StringPrefix(arg1))
            {

                if (int.TryParse(args, out var vnum))
                {
                    ch.EditingHelp = HelpData.Helps.FirstOrDefault(h => h.vnum == vnum);
                    if (ch.EditingHelp != null)
                        ch.send("Editing help {0} - {1}.\n\r", ch.EditingHelp.vnum, ch.EditingHelp.keyword);
                    else
                        ch.send("Help {0} not found.\n\r", vnum);
                }
                else
                {
                    ch.EditingHelp = HelpData.Helps.FirstOrDefault(h => h.keyword.IsName(args));

                    if (ch.EditingHelp != null)
                        ch.send("Editing help {0} - {1}.\n\r", ch.EditingHelp.vnum, ch.EditingHelp.keyword);
                    else
                        ch.send("Help {0} not found.\n\r", vnum);
                }
            }
            else if ("vnum".StringPrefix(arg1))
            {
                if (ch.EditingHelp == null)
                    ch.send("You aren't editing a help entry.\n\r");
                else if (int.TryParse(args, out var vnum))
                {
                    if (HelpData.Helps.Any(h => h.vnum == vnum && h != ch.EditingHelp))
                    {
                        ch.send("A help with that vnum already exists.\n\r");
                    }
                    else
                    {
                        ch.EditingHelp.vnum = vnum;
                        ch.send("OK.\n\r");
                    }
                }
                else
                    ch.send("Enter a numeric vnum.\n\r");
            }
            else if ("level".StringPrefix(arg1))
            {
                if (ch.EditingHelp == null)
                    ch.send("You aren't editing a help entry.\n\r");
                else if (int.TryParse(args, out var level))
                {
                    ch.EditingHelp.level = level;
                    ch.EditingHelp.area.saved = false;
                    ch.send("OK.\n\r");
                }
                else
                    ch.send("Enter a numeric level.\n\r");
            }
            else if ("keywords".StringPrefix(arg1))
            {
                if (ch.EditingHelp == null)
                    ch.send("You aren't editing a help entry.\n\r");
                else if (args.ISEMPTY())
                    ch.send("Set keywords to what?\n\r");
                else if (HelpData.Helps.Any(h => h.keyword.IsName(args) && h != ch.EditingHelp))
                {
                    ch.send("A help with that keyword already exists.\n\r");
                }
                else
                {
                    ch.EditingHelp.keyword = args;
                    ch.EditingHelp.area.saved = false;
                    ch.send("OK.\n\r");
                }
            }
            else if ("text".StringPrefix(arg1))
            {
                string mod = "";
                args.OneArgument(ref mod);
                if (ch.EditingHelp == null)
                    ch.send("You aren't editing a help entry.\n\r");
                else if (mod == "-")
                {
                    args = args.OneArgument();
                    if (ch.EditingHelp.text.IndexOf("\n\r") >= 0)
                    {
                        ch.EditingHelp.text = ch.EditingHelp.text.Substring(0, ch.EditingHelp.text.LastIndexOf('\n') - 1);
                        if (ch.EditingHelp.text.IndexOf("\n\r") >= 0)
                            ch.EditingHelp.text = ch.EditingHelp.text.Substring(0, ch.EditingHelp.text.LastIndexOf('\n'));
                        else
                            ch.EditingHelp.text = "";
                    }
                    else
                        ch.EditingItemTemplate.Description = "";

                    ch.EditingHelp.area.saved = false;
                    ch.send("OK.\n\r");
                }
                else if (mod == "+")
                {
                    args = args.OneArgument();
                    ch.EditingHelp.text += (!string.IsNullOrEmpty(ch.EditingHelp.text) && !ch.EditingHelp.text.Contains('\n') ? "\n" : "") + args + "\n";
                    ch.EditingHelp.area.saved = false;
                    ch.send("OK.\n\r");
                }
                else
                {
                    ch.EditingHelp.text = args;
                    ch.EditingHelp.area.saved = false;
                    ch.send("OK.\n\r");
                }

            }
            else
            {
                if (ch.EditingHelp == null)
                {
                    ch.send("HEdit [create|list|edit] [vnum|keywords]\n\r");
                }
                else
                {
                    ch.send("HEdit [vnum|keywords|level|text]\n\r");
                }
            }
        } // end DoEditHelp
    } // end DoOLC
} // End namespace
