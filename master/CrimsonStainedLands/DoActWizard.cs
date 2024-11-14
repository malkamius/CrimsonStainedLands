using CrimsonStainedLands.Extensions;
using CrimsonStainedLands.World;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace CrimsonStainedLands
{
    public class DoActWizard
    {
        public static void DoGoto(Character ch, string arguments)
        {
            Character other;
            int count = 0;
            if (int.TryParse(arguments, out int vnum))
            {
                if (RoomData.Rooms.TryGetValue(vnum, out RoomData room))
                {
                    ch.Act("$n disappears in a puff of smoke.\r\n", type: ActType.ToRoom);
                    ch.RemoveCharacterFromRoom();
                    ch.SendToChar("You goto " + room.Vnum + ".\r\n");
                    ch.AddCharacterToRoom(room);
                    
                    ch.Act("$n appears out of a puff of smoke.\r\n", type: ActType.ToRoom);

                    //Character.DoLook(ch, "auto");
                }
                else
                    ch.SendToChar("Room not found.\r\n");
            }
            else if ((other = ch.GetCharacterFromListByName(Character.Characters, arguments, ref count)) != null && other.Room != null)
            {
                ch.Act("$n disappears in a puff of smoke.\r\n", type: ActType.ToRoom);
                ch.RemoveCharacterFromRoom();
                ch.SendToChar("You goto " + other.Room.Vnum + ".\r\n");
                ch.AddCharacterToRoom(other.Room);
                
                ch.Act("$n appears out of a puff of smoke.\r\n", type: ActType.ToRoom);

                //Character.DoLook(ch, "auto");
            }
            else
                ch.SendToChar("Enter a valid vnum.\r\n");
        }

        public static void DoSetPlayerPassword(Character ch, string arguments)
        {

            arguments = arguments.OneArgumentOut(out var playerName);
            arguments = arguments.OneArgumentOut(out var password);
            arguments = arguments.OneArgumentOut(out var passwordConfirm);

            Player player = null;
            if (ch.IsNPC)
            {
                ch.send("Non player characters can't do that.\r\n");
            }
            else if (!ch.IsImmortal)
            {
                ch.send("Huh?\r\n");
                return;
            }            
            else if (playerName.ISEMPTY() || password.ISEMPTY() || passwordConfirm.ISEMPTY())
            {
                ch.send("Syntax: SetPlayerPassword {Player Name} {New Password} {Confirm New Password}\r\n");
            }
            else if (password != passwordConfirm)
            {
                ch.send("Passwords do not match.\r\n");
            }
            else if ((player = (from connection in Game.Instance.Info.Connections where connection.Name.StringCmp(playerName) select connection).FirstOrDefault()) == null)
            {
                playerName = playerName[0].ToString().ToUpper() + playerName.Substring(1).ToLower();

                var playerpath = Path.Join(Settings.PlayersPath, playerName + ".xml");

                if(System.IO.File.Exists(playerpath)) 
                {
                    player = new Player(playerpath);
                    player.SetPassword(password);
                    player.SaveCharacterFile();
                    ch.send("Password of offline player changed.\r\n");
                }
                else
                {
                    ch.send("Could not find player.\r\n");
                }
            }
            else
            {
                player.SetPassword(password);
                player.SaveCharacterFile();
                ch.send("Password of online player changed.\r\n");
            }
        }

        
        public static void DoRenamePlayer(Character ch, string arguments) 
        {
            Player connectedplayer = null;
            arguments = arguments.OneArgumentOut(out var playername);
            arguments = arguments.OneArgumentOut(out var newplayername);
            if (ch.IsNPC)
            {
                ch.send("Non player characters can't do that.\r\n");
            }
            else if(!ch.IsImmortal) 
            {
                ch.send("Huh?\r\n");
            }
            else if(playername.ISEMPTY() || newplayername.ISEMPTY()) 
            {
                ch.send("Syntax: renameplayer playername newplayername\r\n");
            }
            else if(playername.Length < 3 || newplayername.Length < 3) 
            {
                ch.send("Name must be at least 3 characters in length.\r\n");
            }
            else
            {
                playername = playername[0].ToString().ToUpper() + playername.Substring(1).ToLower();
                newplayername = newplayername[0].ToString().ToUpper() + newplayername.Substring(1).ToLower();
                
                var newplayerpath = Path.Join(Settings.PlayersPath, newplayername + ".xml");
                var oldplayerpath = Path.Join(Settings.PlayersPath, playername + ".xml");

                if(System.IO.File.Exists(newplayerpath))
                {
                    ch.send("A player file with the new player name already exists.\r\n");
                }
                else if(!System.IO.File.Exists(oldplayerpath))
                {
                    ch.send("Could not find the player file with the player name.\r\n");
                }
                else 
                {
                    connectedplayer = (from connection in Game.Instance.Info.Connections where connection.Name.StringCmp(playername) select connection).FirstOrDefault();

                    if(connectedplayer != null) 
                    {
                        connectedplayer.Name = newplayername;
                        connectedplayer.SaveCharacterFile();

                        ch.send("Renamed online player.\r\n");
                    }
                    else 
                    {
                        var player = new Player(oldplayerpath);
                        player.Name = newplayername;
                        player.SaveCharacterFile();
                        
                        ch.send("Renamed offline player.\r\n");
                    }
                    System.IO.File.Move(oldplayerpath, Path.Join(Path.GetDirectoryName(oldplayerpath), Path.GetFileNameWithoutExtension(oldplayerpath) + ".renamed.xml"));
                }
            }
        }

        public static void DoAdvance(Character ch, string arguments)
        {
            string characterName = "";
            int level = 0;
            Player player;
            arguments = arguments.OneArgument(ref characterName);


            if (characterName.ISEMPTY() || !int.TryParse(arguments, out level))
            {
                ch.send("Syntax: Advance {{character} {{level}");
            }
            else if ((player = (from connection in Game.Instance.Info.Connections where connection.Name.StringCmp(characterName) select connection).FirstOrDefault()) == null)
            {
                ch.send("No player with that exact name found.\r\n");
            }
            else if(level > ch.Level)
            {
                ch.send("Level must be between 1 and " + ch.Level + ".\r\n");
            }
            else if (level < 1 || level > Game.MAX_LEVEL)
            {
                ch.send("Level must be between 1 and " + Game.MAX_LEVEL);
            }
            else
            {
                if (level < player.Level)
                {
                    player.Level = level;
                    ch.send("OK.\r\n");
                }
                else if (level > player.Level)
                {
                    while (level - 1 > player.Level)
                    {
                        player.AdvanceLevel(false);
                    }
                    player.AdvanceLevel(true);
                    ch.send("OK.\r\n");
                }
                else
                    ch.send("They are already that level.\r\n");
            }
        }

        public static void DoEnumerate(Character ch, string arguments)
        {

            if (arguments.ISEMPTY())
            {
                ch.send("Syntax: Enumerate [npcs|mobs|areas|rooms|items|objects] [filter]\r\n");
            }
            else
            {

                string type = "";
                arguments = arguments.OneArgument(ref type);

                using (new Character.Page(ch))
                {
                    if ("npcs".StringPrefix(type) || "mobs".StringPrefix(type))
                    {
                        foreach (var npc in Character.Characters.OfType<NPCData>())
                        {
                            if (arguments.ISEMPTY() || npc.Name.IsName(arguments))
                            {
                                ch.send("Vnum " + npc.vnum + " - " + (!npc.ShortDescription.ISEMPTY() ? npc.ShortDescription : npc.Name) + "\r\n");
                            }
                        }
                    }
                    else if ("objects".StringPrefix(type) || "items".StringPrefix(type))
                    {
                        foreach (var item in ItemData.Items)
                        {
                            if (arguments.ISEMPTY() || item.Name.IsName(arguments))
                            {
                                ch.send("Vnum " + item.Template.Vnum + " - " + (!item.ShortDescription.ISEMPTY() ? item.ShortDescription : item.Name) + " {0} {1}\r\n",
                                    (item.CarriedBy != null ? "held by" : (item.Room != null ? "on the ground in" : "contained in")),
                                    (item.CarriedBy != null ? item.CarriedBy.Display(ch) : (item.Room != null ? (TimeInfo.IS_NIGHT && !item.Room.NightName.ISEMPTY() ? item.Room.NightName : item.Room.Name) : (item.Container != null ? item.Container.Display(ch) : ""))));
                            }
                        }
                    }
                    else if ("rooms".StringPrefix(type))
                    {
                        foreach (var room in RoomData.Rooms.Values)
                        {
                            if (arguments.ISEMPTY() || room.Name.IsName(arguments))
                            {
                                ch.send("Vnum " + room.Vnum + " - " + room.Name + "\r\n");
                            }
                        }
                    }
                    else if ("areas".StringPrefix(type))
                    {
                        foreach (var area in AreaData.Areas)
                        {
                            if (arguments.ISEMPTY() || area.Name.IsName(arguments))
                            {
                                ch.send("Vnums (" + area.VNumStart + " - " + area.VNumEnd + ") - " + area.Name + "\r\n");
                            }
                        }
                    }
                    else if ("resets".StringPrefix(type))
                    {
                        NPCTemplateData lastNPC = null;
                        ItemTemplateData lastItem = null;
                        foreach (var reset in ch.Room.Area.Resets)
                        {
                            if (!arguments.StringPrefix("area") && reset.roomVnum != ch.Room.Vnum && !((reset.resetType == ResetTypes.Give || reset.resetType == ResetTypes.Equip) && lastNPC != null) && !(reset.resetType == ResetTypes.Put && lastItem != null))
                            {
                                lastItem = null;
                                lastNPC = null;
                                continue;
                            }
                            switch (reset.resetType)
                            {
                                default:
                                    ch.send("Type: {0} {1} {2} {3}\r\n", reset.resetType.ToString(), reset.roomVnum, reset.spawnVnum, reset.maxCount);
                                    break;
                                case ResetTypes.NPC:
                                    {
                                        RoomData.Rooms.TryGetValue(reset.roomVnum, out var room);
                                        NPCTemplateData.Templates.TryGetValue(reset.spawnVnum, out var template);
                                        ch.send("Type: {0} Room: {1} {2}\n\tNPC {3} {4}\n\tMaxCount: {5}\r\n", reset.resetType.ToString(), reset.roomVnum, room != null ? room.Name : "unknown", reset.spawnVnum, template != null ? (!template.ShortDescription.ISEMPTY() ? template.ShortDescription : template.Name) : "unknown", reset.maxCount);
                                        lastNPC = template;
                                    }
                                    break;
                                case ResetTypes.Item:
                                    {
                                        RoomData.Rooms.TryGetValue(reset.roomVnum, out var room);
                                        ItemTemplateData.Templates.TryGetValue(reset.spawnVnum, out var template);
                                        ch.send("Type: {0} Room: {1} {2}\n\tItem {3} {4}\n\tMaxCount: {5}\r\n", reset.resetType.ToString(), reset.roomVnum, room != null ? room.Name : "unknown", reset.spawnVnum, template != null ? (!template.ShortDescription.ISEMPTY() ? template.ShortDescription : template.Name) : "unknown", reset.maxCount);
                                        lastItem = template;
                                    }
                                    break;
                                case ResetTypes.Equip:
                                case ResetTypes.Give:
                                    if (lastNPC != null)
                                    {
                                        ItemTemplateData.Templates.TryGetValue(reset.spawnVnum, out var template);
                                        ch.send("Type: {0} NPC: {1} {2}\n\tItem {3} {4}\n\tMaxCount: {5}\r\n", reset.resetType.ToString(), lastNPC.Vnum, !lastNPC.ShortDescription.ISEMPTY() ? lastNPC.ShortDescription : lastNPC.Name, reset.spawnVnum, template != null ? (!template.ShortDescription.ISEMPTY() ? template.ShortDescription : template.Name) : "unknown", reset.maxCount);
                                        lastItem = template;
                                    }
                                    break;
                                case ResetTypes.Put:
                                    if (lastItem != null)
                                    {
                                        ItemTemplateData.Templates.TryGetValue(reset.spawnVnum, out var template);
                                        ch.send("Type: {0} Container: {1} {2}\n\tItem {3} {4}\n\tMaxCount: {5}\r\n", reset.resetType.ToString(), lastItem.Vnum, !lastItem.ShortDescription.ISEMPTY() ? lastItem.ShortDescription : lastItem.Name, reset.spawnVnum, template != null ? (!template.ShortDescription.ISEMPTY() ? template.ShortDescription : template.Name) : "unknown", reset.maxCount);
                                        lastItem = template;
                                    }
                                    break;
                            }
                        }
                        return;

                    }

                }
                ch.send("Syntax: Enumerate [npcs|mobs|areas|rooms|items|objects|resets] [filter]\r\n");
            }
        } // end enumerate

        public static void DoAreaConnections(Character ch, string arguments)
        {
            if (ch.Room == null || ch.Room.Area == null)
            {
                ch.send("You aren't in a room or area.\r\n");
                return;
            }

            foreach (var room in ch.Room.Area.Rooms.Values)
            {
                foreach (var exit in room.OriginalExits)
                {
                    if (exit != null && exit.destination != null)
                    {
                        if (exit.destination.Area != room.Area && exit.destination.Area != null)
                        {
                            ch.send("Connection from {0} to {1} in room {2} [{3}] {4} to room {5} [{6}]\r\n",
                                room.Area.Name, exit.destination.Area.Name, room.Name, room.Vnum,
                                exit.direction.ToString(), exit.destination.Name, exit.destination.Vnum);
                        }
                        else if (exit.destination.Area != room.Area)
                            ch.send("Bad exit in {0} [{1}] - {2} to vnum {3} [{4}] without an area\r\n", room.Name, room.Vnum, exit.direction.ToString(), exit.destination.Name, exit.destinationVnum);
                    }
                    else if (exit != null)
                    {
                        ch.send("Bad exit in {0} [{1}] - {2} to bad vnum {3}\r\n", room.Name, room.Vnum, exit.direction.ToString(), exit.destinationVnum);
                    }
                }
            }
        } // end doareaconnections

        public static void DoHolyLight(Character ch, string arguments)
        {
            if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.HolyLight)))
            {
                ch.Flags.ADDFLAG(ActFlags.HolyLight);
                ch.send("\\GHolyLight\\x is \\gON\\x.\r\n");
            }
            else if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.HolyLight)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.HolyLight);
                ch.send("\\GHolyLight\\x is \\rOFF\\x.\r\n");
            }
            else
                ch.send("Syntax: HolyLight [on|off]\r\n");
        }

        public static void DoWizInvis(Character ch, string arguments)
        {
            if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.WizInvis)))
            {
                ch.Flags.ADDFLAG(ActFlags.WizInvis);
                ch.send("\\GWizInvis\\x is \\gON\\x.\r\n");
            }
            else if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.WizInvis)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.WizInvis);
                ch.send("\\GWizInvis\\x is \\rOFF\\x.\r\n");
            }
            else
                ch.send("Syntax: WizInvis [on|off]\r\n");
        }

        public static void DoResetArea(Character ch, string arguments)
        {
            if (ch.Room != null && ch.Room.Area != null)
            {
                ch.Room.Area.ResetArea(true);
                ch.send("Area Reset.\r\n");
            }
            else
                ch.send("You aren't in an area.\r\n");
        }

        private static void PurgeRoom(RoomData room)
        {
            foreach (var item in room.items.ToArray())
            {
                if(!item.extraFlags.ISSET(ExtraFlags.NoPurge))
                    item.Dispose();
            }
            foreach (var npc in room.Characters.ToArray())
            {
                if (npc.IsNPC)
                {
                    ((NPCData)npc).Dispose();
                    npc.Dispose();
                }
            }
        }
        public static void DoPurge(Character ch, string arguments)
        {
            arguments = arguments.OneArgumentOut(out var command);

            if (command.ISEMPTY() || "room".StringPrefix(command))
            {
                PurgeRoom(ch.Room);
                ch.send("OK.\r\n");
            }
            else if ("area".StringPrefix(command))
            {
                AreaData area = null;
                if (arguments.ISEMPTY())
                    area = ch.Room.Area;
                else
                    area = AreaData.Areas.FirstOrDefault(a => a.Name.IsName(arguments, true));

                if (area == null)
                {
                    ch.send("Area not found.\r\n");
                }
                else
                {
                    foreach (var room in ch.Room.Area.Rooms.Values)
                    {
                        PurgeRoom(room);
                    }
                    ch.send("OK.\r\n");
                }
            }
            else
                ch.send("purge room|purge area {\"@areaname\"}\r\n");
        }

        public static void DoShutdown(Character ch, string arguments)
        {
            Game.shutdown();
        }

        public static void DoStat(Character ch, string arguments)
        {
            string command = "";
            int vnum = 0;
            int count = 0;
            RoomData room;
            ItemData item;
            //Character npc;

            if (string.IsNullOrEmpty(arguments))
            {
                arguments = "room";
            }

            arguments = arguments.OneArgument(ref command);

            if ("room".StringPrefix(command))
            {
                vnum = arguments.number_argument(ref arguments);

                if (vnum == 0)
                {
                    vnum = ch.Room.Vnum;
                }

                if (!RoomData.Rooms.TryGetValue(vnum, out room))
                {
                    ch.send("Room not found.\r\n");
                }
                else
                {
                    ch.send("Room details for {0}\r\n", room.Vnum);

                    ch.send("Name: {0}\r\n", room.Name);
                    ch.send("Description: {0}\r\n", room.Description);
                    ch.send("Exits:\r\n");
                    foreach (var exit in room.OriginalExits)
                        if (exit != null)
                            ch.send("\tDirection {0} Vnum {1} Name {2} Flags {3} Keys {4}\r\n", exit.direction.ToString(), exit.destination != null ? exit.destination.Vnum : exit.destinationVnum, exit.destination != null ? exit.destination.Name : "", string.Join(" ", exit.flags), string.Join(" ", exit.keys));
                }
            }
            else if ("object".StringPrefix(command) || "item".StringPrefix(command))
            {
                if (int.TryParse(arguments, out vnum))
                {
                    ItemTemplateData itemTemplate;
                    if (ItemTemplateData.Templates.TryGetValue(vnum, out itemTemplate))
                    {
                        ch.send("Item Template details for {0}\r\n", itemTemplate.Vnum);
                        ch.send("Name {0}\r\n", itemTemplate.Name);
                        ch.send("Short Description {0}\r\n", itemTemplate.ShortDescription);
                        ch.send("Long Description {0}\r\n", itemTemplate.LongDescription);
                        if (!itemTemplate.NightShortDescription.ISEMPTY())
                            ch.send("Night Short Description {0}\r\n", itemTemplate.NightShortDescription);
                        if (!itemTemplate.NightLongDescription.ISEMPTY())
                            ch.send("Night Long Description {0}\r\n", itemTemplate.NightLongDescription);
                        ch.send("Level {0}\r\n", itemTemplate.Level);
                        ch.send("Item Types: {0}\r\n", string.Join(" ", (from flag in itemTemplate.itemTypes.Distinct() select flag.ToString())));

                        if (itemTemplate.WeaponDamageType != null)
                            ch.send("Weapon Damage Type {0}\r\n", itemTemplate.WeaponDamageType.Keyword);
                        ch.send("Weapon Type {0}\r\n", itemTemplate.WeaponType.ToString());

                        ch.send("Damage Dice {0} avg({1})\r\n", itemTemplate.DamageDice.ToString(), itemTemplate.DamageDice.Average);
                        ch.send("Weight {0}, Max Weight {1}\r\n", itemTemplate.Weight, itemTemplate.MaxWeight);
                        ch.send("Cost {0}\r\n", itemTemplate.Value);
                        ch.send("Nutrition {0}, Charges {1}, Max Charges {2}\r\n", itemTemplate.Nutrition, itemTemplate.Charges, itemTemplate.MaxCharges);
                        ch.send("Material {0}, Liquid {1}\r\n", itemTemplate.Material, itemTemplate.Liquid);
                        ch.send("Armor bash {0}, slash {1}, pierce {2}, magic {3}\r\n", itemTemplate.ArmorBash, itemTemplate.ArmorSlash, itemTemplate.ArmorPierce, itemTemplate.ArmorExotic);
                        ch.send("Wear Flags: {0}\r\n", string.Join(" ", (from flag in itemTemplate.wearFlags.Distinct() select flag.ToString())));
                        ch.send("Extra Flags: {0}\r\n", string.Join(" ", (from flag in itemTemplate.extraFlags.Distinct() select flag.ToString())));

                        ch.send("Affects: \n   {0}\r\n", string.Join("\n   ", (from aff in itemTemplate.affects select aff.@where + " - " + aff.location.ToString() + " " + aff.modifier + " - " + string.Join(",", aff.flags) + " - " + aff.duration)));

                    }
                    else
                    {
                        ch.send("Item Template with that vnum not found.\r\n");
                    }
                }
                else
                {
                    item = ch.GetItemHere(arguments);

                    if (item == null)
                    {
                        ch.send("You don't see that here.\r\n");
                    }
                    else
                    {
                        ch.send("Item details for {0}\r\n", item.Vnum);
                        ch.send("Name {0}\r\n", item.Name);
                        ch.send("Short Description {0}\r\n", item.ShortDescription);
                        ch.send("Long Description {0}\r\n", item.LongDescription);
                        ch.send("Level {0}\r\n", item.Level);
                        ch.send("Item Types: {0}\r\n", string.Join(" ", (from flag in item.ItemType.Distinct() select flag.ToString())));

                        if (item.WeaponDamageType != null)
                            ch.send("Weapon Damage Type {0}\r\n", item.WeaponDamageType.Keyword);
                        ch.send("Weapon Type {0}\r\n", item.WeaponType.ToString());

                        ch.send("Damage Dice {0} avg({1})\r\n", item.DamageDice.ToString(), item.DamageDice.Average);
                        ch.send("Weight {0}, Max Weight {1}\r\n", item.Weight, item.MaxWeight);
                        ch.send("Cost {0}\r\n", item.Value);
                        ch.send("Timer {0}\r\n", item.timer);
                        ch.send("Nutrition {0}, Charges {1}, Max Charges {2}\r\n", item.Nutrition, item.Charges, item.MaxCharges);
                        ch.send("Material {0}, Liquid {1}\r\n", item.Material, item.Liquid);
                        ch.send("Armor bash {0}, slash {1}, pierce {2}, magic {3}\r\n", item.ArmorBash, item.ArmorSlash, item.ArmorPierce, item.ArmorExotic);
                        ch.send("Wear Flags: {0}\r\n", string.Join(" ", (from flag in item.wearFlags.Distinct() select flag.ToString())));
                        ch.send("Extra Flags: {0}\r\n", string.Join(" ", (from flag in item.extraFlags.Distinct() select flag.ToString())));

                        ch.send("Affects: \n   {0}\r\n", string.Join("\n   ", (from aff in item.affects select aff.@where + " - " + aff.location.ToString() + " " + aff.modifier + " - " + string.Join(",", aff.flags) + " - " + aff.duration)));


                    }
                }

            } // end stat item
            else if ("mobile".StringPrefix(command) || "npc".StringPrefix(command) || "character".StringPrefix(command) || (!(arguments = command + " " + arguments).ISEMPTY()))
            {
                if (!arguments.ISEMPTY() && int.TryParse(arguments, out vnum))
                {
                    NPCTemplateData npcTemplate;
                    if (NPCTemplateData.Templates.TryGetValue(vnum, out npcTemplate))
                    {
                        ch.send("NPC Template details for {0}\r\n", npcTemplate.Vnum);
                        ch.send("Name {0}\r\n", npcTemplate.Name);
                        ch.send("Short Description {0}\r\n", npcTemplate.ShortDescription);
                        ch.send("Long Description {0}\r\n", npcTemplate.LongDescription);
                        ch.send("Level {0}\r\n", npcTemplate.Level);
                        ch.send("Guild {0}\r\n", npcTemplate.Guild != null? npcTemplate.Guild.name : "none");
                        ch.send("Gold {0}\r\n", npcTemplate.Gold);
                        ch.send("Silver {0}\r\n", npcTemplate.Silver);
                        ch.send("Race {0}\r\n", npcTemplate.Race != null ? npcTemplate.Race.name : "unknown");
                        ch.send("Hitpoint Dice {0} avg ({1})\r\n", npcTemplate.HitPointDice.ToString(), npcTemplate.HitPointDice.Average);
                        ch.send("Manapoint Dice {0} avg ({1})\r\n", npcTemplate.ManaPointDice.ToString(), npcTemplate.ManaPointDice.Average);
                        ch.send("Damage Dice {0} avg ({1})\r\n", npcTemplate.DamageDice.ToString(), npcTemplate.DamageDice.Average);
                        ch.send("HitRoll {0}, DamageRoll {1}\r\n", npcTemplate.HitRoll, npcTemplate.DamageRoll);
                        ch.send("Act Flags: {0}\r\n", string.Join(" ", (from flag in npcTemplate.Flags.Distinct() select flag.ToString())));
                        ch.send("AffectedBy Flags: {0}\r\n", string.Join(" ", (from flag in npcTemplate.AffectedBy.Distinct() select flag.ToString())));
                        ch.send("Alignment {0}\r\n", npcTemplate.Alignment.ToString());
                        ch.send("Armor bash {0}, slash {1}, pierce {2}, magic {3}\r\n", npcTemplate.ArmorBash, npcTemplate.ArmorSlash, npcTemplate.ArmorPierce, npcTemplate.ArmorExotic);
                        ch.send("Skills: {0}\r\n", (string.Join(" ", from sk in npcTemplate.Learned select sk.Key.name)));
                        ch.send("Affects: \n   {0}\r\n", string.Join("\n   ", (from aff in npcTemplate.AffectsList select aff.displayName + ", " + aff.@where + ", " + aff.location.ToString() + " " + aff.modifier + ", " + string.Join(",", aff.flags) + ", " + aff.duration)));
                    }
                    else
                    {
                        ch.send("NPC Template with that vnum not found.\r\n");
                    }
                }

                else
                {
                    arguments = arguments.Trim();
                    var target = ch.GetCharacterFromRoomByName(arguments, ref count);
                    if (target == null) target = ch.GetCharacterFromListByName(Character.Characters, arguments, ref count);
                    if (target == null)
                    {
                        ch.send("You don't see them here or they are not an NPC.\r\n");
                    }
                    else
                    {
                        NPCData npc = null;
                        if (target is NPCData)
                            npc = (NPCData) target;
                        ch.send("NPC details for {0}\r\n", npc?.vnum);
                        ch.send("Name {0}\r\n", target.Name);
                        ch.send("Short Description {0}\r\n", target.ShortDescription);
                        ch.send("Long Description {0}\r\n", target.LongDescription);
                        ch.send("Level {0}\r\n", target.Level);
                        ch.send("Guild {0}\r\n", target.Guild != null? target.Guild.name : "none");
                        ch.send("Gold {0}\r\n", target.Gold);
                        ch.send("Silver {0}\r\n", target.Silver);
                        ch.send("Race {0}\r\n", target.Race != null ? target.Race.name : "unknown");
                        if (npc != null && NPCTemplateData.Templates.TryGetValue(npc.vnum, out var npcTemplate))
                        {
                            ch.send("Hitpoint Dice {0} avg ({1})\r\n", npcTemplate.HitPointDice.ToString(), npcTemplate.HitPointDice.Average);
                            ch.send("Manapoint Dice {0} avg ({1})\r\n", npcTemplate.ManaPointDice.ToString(), npcTemplate.ManaPointDice.Average);
                            ch.send("Damage Dice {0} avg ({1})\r\n", target.DamageDice.ToString(), target.DamageDice.Average);
                        }
                        ch.send("Hitpoints {0}/{1}, Mana {2}/{3}, Moves {4}/{5}\r\n", target.HitPoints, target.MaxHitPoints, target.ManaPoints, target.MaxManaPoints, target.MovementPoints, target.MaxMovementPoints);
                        ch.send("HitRoll {0}, DamageRoll {1}\r\n", target.HitRoll, target.DamageRoll);
                        ch.send("Act Flags: {0}\r\n", string.Join(" ", (from flag in target.Flags.Distinct() select flag.ToString())));
                        ch.send("AffectedBy Flags: {0}\r\n", string.Join(" ", (from flag in target.AffectedBy.Distinct() select flag.ToString())));
                        ch.send("Alignment {0}\r\n", target.Alignment.ToString());
                        var ac = target.GetArmorClass();
                        ch.send($"Armor bash {ac.acBash}, slash {ac.acSlash}, pierce {ac.acPierce}, magic {ac.acExotic}\r\n");
                        if (target.PermanentStats != null)
                            ch.send("Strength: {0}(+{1}), Wisdom: {2}(+{3}), Intelligence: {4}(+{5}), Dexterity: {6}(+{7}), Constitution: {8}(+{9}), Charisma: {10}(+{11})\r\n",
                                target.GetCurrentStat(PhysicalStatTypes.Strength), (target.GetModifiedStatUncapped(PhysicalStatTypes.Strength) >= target.GetCurrentStat(PhysicalStatTypes.Strength) ? target.GetModifiedStatUncapped(PhysicalStatTypes.Strength) - target.GetCurrentStat(PhysicalStatTypes.Strength) : 0),
                                target.GetCurrentStat(PhysicalStatTypes.Wisdom), (target.GetModifiedStatUncapped(PhysicalStatTypes.Wisdom) > target.GetCurrentStat(PhysicalStatTypes.Wisdom) ? target.GetModifiedStatUncapped(PhysicalStatTypes.Wisdom) - target.GetCurrentStat(PhysicalStatTypes.Wisdom) : 0),
                                target.GetCurrentStat(PhysicalStatTypes.Intelligence), (target.GetModifiedStatUncapped(PhysicalStatTypes.Intelligence) >= target.GetCurrentStat(PhysicalStatTypes.Intelligence) ? target.GetModifiedStatUncapped(PhysicalStatTypes.Intelligence) - target.GetCurrentStat(PhysicalStatTypes.Intelligence) : 0),
                                target.GetCurrentStat(PhysicalStatTypes.Dexterity), (target.GetModifiedStatUncapped(PhysicalStatTypes.Dexterity) >= target.GetCurrentStat(PhysicalStatTypes.Dexterity) ? target.GetModifiedStatUncapped(PhysicalStatTypes.Dexterity) - target.GetCurrentStat(PhysicalStatTypes.Dexterity) : 0),
                                target.GetCurrentStat(PhysicalStatTypes.Constitution), (target.GetModifiedStatUncapped(PhysicalStatTypes.Constitution) >= target.GetCurrentStat(PhysicalStatTypes.Constitution) ? target.GetModifiedStatUncapped(PhysicalStatTypes.Constitution) - target.GetCurrentStat(PhysicalStatTypes.Constitution) : 0),
                                target.GetCurrentStat(PhysicalStatTypes.Charisma), (target.GetModifiedStatUncapped(PhysicalStatTypes.Charisma) >= target.GetCurrentStat(PhysicalStatTypes.Charisma) ? target.GetModifiedStatUncapped(PhysicalStatTypes.Charisma) - target.GetCurrentStat(PhysicalStatTypes.Charisma) : 0));

                        ch.send("Skills: {0}\r\n", (string.Join(" ", from sk in target.Learned select sk.Key.name)));
                        ch.send("Affects: \n   {0}\r\n", string.Join("\n   ", (from aff in target.AffectsList select aff.displayName + ", " + aff.@where + ", " + aff.location.ToString() + " " + aff.modifier + ", " + string.Join(",", aff.flags) + ", " + aff.duration)));

                    }
                }
            }
            else
            {
                ch.send("Stat [mobile|object|character] @name.\r\n");
            }
        }
        public static void DoString(Character ch, string arguments)
        {
            string command = "";
            string field = "";
            string search = "";
            int count = 0;

            arguments = arguments.OneArgument(ref command);
            arguments = arguments.OneArgument(ref search);
            arguments = arguments.OneArgument(ref field);

            if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(field) || string.IsNullOrEmpty(arguments) || string.IsNullOrEmpty(search))
            {
                ch.send("String [mobile|object] [name|shortdescription|longdescription] [value]");
                return;
            }
            else if ("mobile".StringPrefix(command) || "npc".StringPrefix(command))
            {
                var npc = ch.GetCharacterFromRoomByName(search, ref count);

                if (npc == null)
                {
                    ch.send("You don't see them here.\r\n");
                }
                else
                {
                    if ("name".StringPrefix(field) && npc.IsNPC)
                    {
                        npc.Name = arguments;
                        ch.send("NPC Name updated.\r\n");
                    }
                    else if ("name".StringPrefix(field))
                        ch.send("You cannot update player names this way.\r\n");
                    else if ("shortdescription".StringPrefix(field))
                    {
                        npc.ShortDescription = arguments;
                        ch.send("NPC Short Description updated.\r\n");
                    }
                    else if ("longdescription".StringPrefix(field))
                    {
                        npc.LongDescription = arguments;
                        ch.send("NPC Long Description updated.\r\n");
                    }
                    else
                        ch.send("Invalid field for NPCs.\r\n");
                }
            }
            else if ("object".StringPrefix(command) || "item".StringPrefix(command))
            {
                var item = ch.GetItemHere(search);

                if (item == null)
                {
                    ch.send("You don't see that here.\r\n");
                }
                else
                {
                    if ("name".StringPrefix(field))
                    {
                        item.Name = arguments;
                        ch.send("Item Name updated.\r\n");
                    }
                    else if ("shortdescription".StringPrefix(field))
                    {
                        item.ShortDescription = arguments;
                        ch.send("Item Short Description updated.\r\n");
                    }
                    else if ("longdescription".StringPrefix(field))
                    {
                        item.LongDescription = arguments;
                        ch.send("Item Long Description updated.\r\n");
                    }
                    else
                        ch.send("Invalid field for objects.\r\n");
                }
            }
            else
                ch.send("String [mobile|object] [name|shortdescription|longdescription] [value]");
        }

        public static void DoSet(Character ch, string arguments)
        {
            string command = "";
            string field = "";
            string search = "";
            string valuestring = "";

            int count = 0;
            int value = 0;
            Character victim;

            arguments = arguments.OneArgument(ref command);
            arguments = arguments.OneArgument(ref search);
            arguments = arguments.OneArgument(ref field);
            arguments = arguments.OneArgument(ref valuestring);

            if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(field))
            {
                ch.send("set [mobile|object|skill|specializations] [level|maxhitpoints|maxmanapoints] [value]");
                return;
            }
            else if ("mobile".StringPrefix(command) || "npc".StringPrefix(command))
            {
                if (string.IsNullOrEmpty(valuestring) || !int.TryParse(valuestring, out value))
                {
                    ch.send("You must provide a value.\r\n");
                    return;
                }
                var npc = ch.GetCharacterFromRoomByName(search, ref count);

                if (npc == null)
                {
                    ch.send("You don't see them here.\r\n");
                }
                else
                {
                    if ("level".StringPrefix(field))
                    {
                        npc.Level = value;
                        ch.send("NPC level updated.\r\n");
                    }
                    else if ("maxhitpoints".StringPrefix(field))
                    {
                        npc.MaxHitPoints = value;
                        npc.HitPoints = value;
                        ch.send("NPC max hitpoints updated.\r\n");
                    }
                    else if ("maxmanapoints".StringPrefix(field))
                    {
                        npc.MaxManaPoints = value;
                        npc.ManaPoints = value;
                        ch.send("NPC max mana updated.\r\n");
                    }
                    else
                        ch.send("Invalid field for NPCs.\r\n");
                }
            }
            else if ("object".StringPrefix(command) || "item".StringPrefix(command))
            {
                if (string.IsNullOrEmpty(valuestring) || !int.TryParse(valuestring, out value))
                {
                    ch.send("You must provide a value.\r\n");
                    return;
                }
                var item = ch.GetItemHere(search);

                if (item == null)
                {
                    ch.send("You don't see that here.\r\n");
                }
                else
                {
                    if ("level".StringPrefix(field) && int.TryParse(valuestring, out var level))
                    {
                        
                        item.Level = level;
                        ch.send("Item level updated.\r\n");
                    }
                    else
                        ch.send("Invalid field for objects.\r\n");

                }
            }
            else if ("specializations".StringPrefix(command))
            {
                if ((victim = Character.GetCharacterWorld(ch, search)) == null)
                {
                    ch.send("You don't see them.\r\n");
                    return;
                }

                if (!(victim is Player))
                {
                    ch.send("You can only grant specializations to players.\r\n");
                    return;
                }
                if (string.IsNullOrEmpty(field) || !int.TryParse(field, out value))
                {
                    ch.send("You must provide a value.\r\n");
                    return;
                }
                var player = (Player)victim;
                player.WeaponSpecializations = value;
            }
            else if ("skill".StringPrefix(command))
            {
                if (string.IsNullOrEmpty(valuestring) || !int.TryParse(valuestring, out value))
                {
                    ch.send("You must provide a value.\r\n");
                    return;
                }
                if ((victim = Character.GetCharacterWorld(ch, search)) == null)
                {
                    ch.send("You don't see them.\r\n");
                    return;
                }
                SkillSpell skill = SkillSpell.SkillLookup(field);
                int percent = 0;
                int level = 60;
                //string percentstring = "";

                //arguments = arguments.oneArgument(ref percentstring);
                if (skill == null)
                {
                    ch.send("Skill not found.\r\n");
                    return;
                }

                if (!int.TryParse(valuestring, out percent))
                {
                    ch.send("Set skill [name] [skill] [0-100]");
                    return;
                }

                if (!int.TryParse(arguments, out level))
                    level = 60;

                var prereqsnotmet = (from l in victim.Learned where l.Key.PrerequisitesMet(victim) == false select l).ToArray();

                victim.LearnSkill(skill, percent, level);

                foreach (var prereqnotmet in prereqsnotmet)
                {
                    if (prereqnotmet.Key.PrerequisitesMet(victim))
                    {
                        victim.send("\\CYou feel a rush of insight into {0}!\\x\r\n", prereqnotmet.Key.name);
                    }
                }

                if (percent < 0)
                    victim.Learned.Remove(skill);
                ch.send("OK.\r\n");

            }
        } // end doset

        public static void DoTransfer(Character ch, string argument)
        {
            string arg1 = "";
            string arg2 = "";
            RoomData location;
            Character victim;

            argument = argument.OneArgument(ref arg1);
            argument = argument.OneArgument(ref arg2);

            if (arg1.ISEMPTY())
            {
                ch.send("Transfer whom (and where)?\r\n");
                return;
            }

            if (arg1.StringCmp("all"))
            {
                foreach (var player in Game.Instance.Info.Connections)
                {
                    if (player.state == Player.ConnectionStates.Playing && player != ch && player.Room != null && ch.CanSee(player))
                    {
                        DoTransfer(ch, player.Name + " " + arg2);
                    }
                }

                return;
            }

            /*
            * Thanks to Grodyn for the optional location parameter.
            */
            if (arg2.ISEMPTY())
            {
                location = ch.Room;
            }
            else
            {
                if ((location = Character.FindLocation(ch, arg2)) == null)
                {
                    ch.send("No such location.\r\n");
                    return;
                }

                //if (!is_room_owner(ch, location) && room_is_private(location)
                //    && get_trust(ch) < MAX_LEVEL)
                //{
                //    send_to_char("That room is private right now.\r\n", ch);
                //    return;
                //}
            }

            if ((victim = Character.GetCharacterWorld(ch, arg1)) == null)
            {
                ch.send("They aren't here.\r\n");
                return;
            }

            if (victim.Room == null)
            {
                ch.send("They are in limbo.\r\n");
                return;
            }

            if (victim.Level >= ch.Level && !victim.IsNPC)
            {
                ch.send("They are too high for you to mess with.\r\n");
                return;
            }

            Combat.StopFighting(victim, true);

            victim.Act("$n disappears in a mushroom cloud.", type: ActType.ToRoom);
            victim.RemoveCharacterFromRoom();
            if (ch != victim)
                ch.Act("$n has transferred you.", victim, null, null, ActType.ToVictim);
            victim.AddCharacterToRoom(location);
            victim.Act("$n arrives from a puff of smoke.", type: ActType.ToRoom);
            
            //Character.DoLook(victim, "auto");
            ch.send("Ok.\r\n");
        }

        public static void DoRestore(Character ch, string args)
        {
            if (!string.IsNullOrEmpty(args) && !args.equals("self"))
            {
                ch.send("Restoration of self is all that is possible right now.\r\n");
            }
            else
            {
                ch.HitPoints = ch.MaxHitPoints;
                ch.ManaPoints = ch.MaxManaPoints;
                ch.MovementPoints = ch.MaxMovementPoints;
                ch.send("You are restored.\r\n");
                ch.Act("$n is restored.\r\n", type: ActType.ToRoom);
            }
        }

        internal static void DoLoad(Character ch, string args)
        {
            string loadtype = "";
            string vnumstring = "";
            int vnum = 0;
            args = args.OneArgument(ref loadtype);
            args = args.OneArgument(ref vnumstring);
            if ("mobile".StringPrefix(loadtype) || "npc".StringPrefix(loadtype))
            {
                if (int.TryParse(vnumstring, out vnum))
                {
                    if (NPCTemplateData.Templates.TryGetValue(vnum, out NPCTemplateData mobt))
                    {
                        NPCData mob = new NPCData(mobt, ch.Room);
                        ch.Act("$N magically appears.\r\n", mob);
                        ch.Act("$N magically appears.\r\n", mob, null, null, ActType.ToRoomNotVictim);
                        var resets = mobt.Area.Resets;

                        if (resets.Any())
                        {
                            var reset = resets.FirstOrDefault(r => r.resetType == ResetTypes.NPC && r.spawnVnum == mobt.Vnum);
                            if (reset != null)
                            {
                                var resetindex = resets.IndexOf(reset);
                                ItemData lastitem = null;
                                if (resetindex + 1 < resets.Count)
                                {
                                    for (int i = resetindex + 1; i < resets.Count; i++)
                                    {
                                        reset = resets[i];
                                        if (reset.resetType != ResetTypes.Give && reset.resetType != ResetTypes.Equip)
                                        {
                                            break;
                                        }
                                        else
                                            reset.execute(ref mob, ref lastitem);
                                    }
                                }
                            }
                        }
                    }
                    else ch.send("mob vnum not found.\r\n");
                }
                else ch.send("You must enter a vnum to load.\r\n");
            }
            else if ("item".StringPrefix(loadtype) || "object".StringPrefix(loadtype))
            {
                if (int.TryParse(vnumstring, out vnum))
                {
                    ItemTemplateData itemt;
                    if (ItemTemplateData.Templates.TryGetValue(vnum, out itemt))
                    {
                        ItemData item = new ItemData(itemt, ch);
                        ch.send(string.Format("{0} magically appears.\r\n", item.Display(ch)));
                        //ch.Act("$p magically appears.\r\n", null, item, null, ActType.ToRoom);
                    }
                    else ch.send("Item vnum not found.\r\n");
                }
                else ch.send("You must enter a vnum to load.\r\n");
            }
        }

        public static void DoSlay(Character ch, string arguments)
        {
            Character victim;
            int count = 0;

            if (arguments.ISEMPTY())
            {
                ch.send("Who would you like to slay?\r\n");
            }
            else if ((victim = ch.GetCharacterFromRoomByName(arguments, ref count)) != null && (victim.IsNPC || victim.Level < ch.Level))
            {
                ch.Act("$n slays $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n slays you.", victim, type: ActType.ToVictim);
                ch.Act("You slay $N.", victim, type: ActType.ToChar);
                victim.HitPoints = -15;
                Combat.CheckIsDead(ch, victim, 15);
            }
            else if (victim != null)
            {
                ch.send("They are too high level.\r\n");
            }
            else
            {
                ch.send("You don't see them here.\r\n");
            }
        }

        public static void DoStartQuest(Character ch, string arguments)
        {
            string arg = "";
            int vnum = 0;

            if (arguments.ISEMPTY())
            {
                ch.send("Syntax: startquest [character] [vnum]");
                return;
            }

            arguments = arguments.OneArgument(ref arg);

            var victim = Character.GetCharacterWorld(ch, arg);
            if (victim == null)
            {
                ch.send("You don't see them.\r\n");
                return;
            }
            else if (arguments.ISEMPTY() || !int.TryParse(arguments, out vnum))
            {
                ch.send("Syntax: startquest [character] [vnum]");
                return;
            }

            Quest quest;
            if ((quest = Quest.GetQuest(vnum)) == null)
            {
                ch.send("Quest not found.\r\n");
                return;
            }

            QuestProgressData.StartQuest(victim, ch.Name, quest);
        }

        public static void DoCompleteQuest(Character ch, string arguments)
        {
            string arg = "";
            int vnum = 0;

            if (arguments.ISEMPTY())
            {
                ch.send("Syntax: completequest [character] [vnum]");
                return;
            }

            arguments = arguments.OneArgument(ref arg);

            var victim = Character.GetCharacterWorld(ch, arg);
            if (victim == null)
            {
                ch.send("You don't see them.\r\n");
                return;
            }
            else if (arguments.ISEMPTY() || !int.TryParse(arguments, out vnum))
            {
                ch.send("Syntax: completequest [character] [vnum]");
                return;
            }

            Quest quest;
            if ((quest = Quest.GetQuest(vnum)) == null)
            {
                ch.send("Quest not found.\r\n");
                return;
            }

            QuestProgressData.CompleteQuest(victim, quest);
        }

        public static void DoFailQuest(Character ch, string arguments)
        {
            string arg = "";
            int vnum = 0;

            if (arguments.ISEMPTY())
            {
                ch.send("Syntax: failquest [character] [vnum]");
                return;
            }

            arguments = arguments.OneArgument(ref arg);

            var victim = Character.GetCharacterWorld(ch, arg);
            if (victim == null)
            {
                ch.send("You don't see them.\r\n");
                return;
            }
            else if (arguments.ISEMPTY() || !int.TryParse(arguments, out vnum))
            {
                ch.send("Syntax: failquest [character] [vnum]");
                return;
            }

            Quest quest;
            if ((quest = Quest.GetQuest(vnum)) == null)
            {
                ch.send("Quest not found.\r\n");
                return;
            }

            QuestProgressData.FailQuest(victim, quest);
        }

        public static void DoDisableQuest(Character ch, string arguments)
        {
            string arg = "";
            int vnum = 0;

            if (arguments.ISEMPTY())
            {
                ch.send("Syntax: disablequest [character] [vnum]");
                return;
            }

            arguments = arguments.OneArgument(ref arg);

            var victim = Character.GetCharacterWorld(ch, arg);
            if (victim == null)
            {
                ch.send("You don't see them.\r\n");
                return;
            }
            else if (arguments.ISEMPTY() || !int.TryParse(arguments, out vnum))
            {
                ch.send("Syntax: disablequest [character] [vnum]");
                return;
            }

            Quest quest;
            if ((quest = Quest.GetQuest(vnum)) == null)
            {
                ch.send("Quest not found.\r\n");
                return;
            }

            QuestProgressData.DisableQuest(victim, quest);
        }

        public static void DoResetQuest(Character ch, string arguments)
        {
            string arg = "";
            int vnum = 0;

            if (arguments.ISEMPTY())
            {
                ch.send("Syntax: resetquest [character] [vnum]");
                return;
            }

            arguments = arguments.OneArgument(ref arg);

            var victim = Character.GetCharacterWorld(ch, arg);
            if (victim == null)
            {
                ch.send("You don't see them.\r\n");
                return;
            }
            else if (arguments.ISEMPTY() || !int.TryParse(arguments, out vnum))
            {
                ch.send("Syntax: resetquest [character] [vnum]");
                return;
            }

            Quest quest;
            if ((quest = Quest.GetQuest(vnum)) == null)
            {
                ch.send("Quest not found.\r\n");
                return;
            }

            QuestProgressData.ResetQuest(victim, quest);
        }

        public static void DoDropQuest(Character ch, string arguments)
        {
            string arg = "";
            int vnum = 0;

            if (arguments.ISEMPTY())
            {
                ch.send("Syntax: dropquest [character] [vnum]");
                return;
            }

            arguments = arguments.OneArgument(ref arg);

            var victim = Character.GetCharacterWorld(ch, arg);
            if (victim == null)
            {
                ch.send("You don't see them.\r\n");
                return;
            }
            else if (arguments.ISEMPTY() || !int.TryParse(arguments, out vnum))
            {
                ch.send("Syntax: dropquest [character] [vnum]");
                return;
            }

            Quest quest;
            if ((quest = Quest.GetQuest(vnum)) == null)
            {
                ch.send("Quest not found.\r\n");
                return;
            }

            QuestProgressData.DropQuest(victim, quest);
        }

        public static void DoReboot(Character ch, string arguments)
        {
            Game.reboot();
        }

        public static void DoImmortal(Character ch, string arguments)
        {
            if (!ch.IsImmortal)
            {
                ch.send("Huh?\r\n");
                return;
            }

            if (arguments.ISEMPTY() || arguments.Trim().ISEMPTY())
            {
                ch.send("Immortal what?\r\n");
                return;
            }

            foreach (var other in Character.Characters)
            {
                if (other != ch && other.IsImmortal)
                    ch.Act("\\WImmortal - $n: {0}\\x\r\n", other, null, null, ActType.ToVictim, arguments);
            }
            Game.log("{0}: '{1}'", ch.Name, arguments);
            ch.Act("\\WImmortal - You: {0}\\x\r\n", null, null, null, ActType.ToChar, arguments);
        }

        public static void DoStripAffects(Character ch, string arguments)
        {
            //if (!ch.IS_IMMORTAL)
            //{
            //    ch.send("Huh?\r\n");
            //    return;
            //}

            foreach (var aff in ch.AffectsList.ToArray())
            {
                ch.AffectFromChar(aff, AffectRemoveReason.Other);
            }

            ch.send("OK.\r\n");
        }

        public static void DoTitle(Character ch, string arguments)
        {
            arguments = arguments.OneArgumentOut(out var charname);
            int count = 0;
            Character target = null;
            if (string.IsNullOrEmpty(charname))
            {
                ch.send("You must specify a player name.\r\n");
            }
            else if ((target = ch.GetCharacterFromRoomByName(charname, ref count)) == null && (target = ch.GetCharacterFromListByName(Character.Characters, charname, ref count)) == null)
            {
                ch.send("Player not found.\r\n");
            }
            else if (target.IsNPC)
            {
                ch.send("Target is an NPC.\r\n");
            }
            else if (target.Guild == null && arguments.ISEMPTY())
            {
                ch.send("Target is not in a guild.\r\n");
            }
            else if (arguments.ISEMPTY())
            {
                target.Title = target.Sex != Sexes.Female ?
                    (target.Guild.Titles.ContainsKey(target.Level) ? "the " + target.Guild.Titles[target.Level].MaleTitle : "") :
                    (target.Guild.Titles.ContainsKey(target.Level) ? "the " + target.Guild.Titles[target.Level].FemaleTitle : "");
                ch.send("Title reset to guild title.\r\n");
            }
            else
            {
                target.Title = arguments.Trim();
                ch.send("Target title set.\r\n");
            }
        }

        public static void DoExtendedTitle(Character ch, string arguments)
        {
            arguments = arguments.OneArgumentOut(out var charname);
            int count = 0;
            Character target = null;
            if (string.IsNullOrEmpty(charname))
            {
                ch.send("You must specify a player name.\r\n");
            }
            else if ((target = ch.GetCharacterFromRoomByName(charname, ref count)) == null && (target = ch.GetCharacterFromListByName(Character.Characters, charname, ref count)) == null)
            {
                ch.send("Player not found.\r\n");
            }
            else if (target.IsNPC)
            {
                ch.send("Target is an NPC.\r\n");
            }
            else if (arguments.ISEMPTY())
            {
                target.ExtendedTitle = string.Empty;
                ch.send("Target extended title cleared.\r\n");
            }
            else
            {
                target.ExtendedTitle = arguments.Trim();
                ch.send("Target extended title set.\r\n");
            }
        }

        /// <summary>
        /// Set the flags of an item or character
        /// </summary>
        /// <param name="ch">Character executing the command</param>
        /// <param name="arguments">Arguments supplied after the command name</param>
        public static void DoFlags(Character ch, string arguments)
        {
            if(arguments.ISEMPTY() || (arguments = arguments.OneArgumentOut(out var targettype)).ISEMPTY() || (arguments = arguments.OneArgumentOut(out var targetname)).ISEMPTY() || (arguments = arguments.OneArgumentOut(out var flagtype)) != arguments)
            {
                ch.send("flags [item|character] @target @flagtype @flags");
                return;
            }

            if("character".StringPrefix(targettype) || "mobile".StringPrefix(targettype) || "npc".StringPrefix(targettype))
            {
                Character target = null;
                int count = 0; 
                if((target = ch.GetCharacterFromRoomByName(targetname, ref count)) == null && (target = ch.GetCharacterFromListByName(Character.Characters, targetname, ref count)) == null)
                {
                    ch.send("Character not found, here or in the world.\r\n");
                    return;
                }
                else if ("affectedby".StringPrefix(flagtype))
                {
                    Utility.GetEnumValues(arguments, ref target.AffectedBy);
                }
                else if ("immune".StringPrefix(flagtype))
                {
                    Utility.GetEnumValues(arguments, ref target.ImmuneFlags);
                }
                else if ("resist".StringPrefix(flagtype))
                {
                    Utility.GetEnumValues(arguments, ref target.ResistFlags);
                }
                else if ("vulnerable".StringPrefix(flagtype))
                {
                    Utility.GetEnumValues(arguments, ref target.VulnerableFlags);
                }

                else if ("flags".StringPrefix(flagtype))
                {
                    Utility.GetEnumValues(arguments, ref target.Flags);
                }
                else
                {
                    ch.send("flag character [affectedby|flags|immune|resist|vulnerable] @flags\r\n");
                    return;
                }
                ch.send("OK.\r\n");
            }
            else if ("item".StringPrefix(targettype) || "object".StringPrefix(targettype))
            {
                ItemData target = null;
                
                if ((target = ch.GetItemHere(targetname)) == null)
                {
                    ch.send("Item not found here.\r\n");
                    return;
                }
                else if ("wearflags".StringPrefix(flagtype))
                {
                    if(!Utility.GetEnumValues(arguments, ref target.wearFlags))
                        ch.send("Failed.\r\n");
                }
                else if ("extraflags".StringPrefix(flagtype))
                {
                    if(!Utility.GetEnumValues(arguments, ref target.extraFlags))
                        ch.send("Failed.\r\n");
                }
                else if("weapontype".StringPrefix(flagtype))
                {
                    if (!Utility.GetEnumValue(arguments, ref target.WeaponType))
                        ch.send("Failed.\r\n");
                }
                else
                {
                    ch.send("flag item [wearflags|extraflags|weapontype] @flags");
                    return;
                }
                ch.send("OK.\r\n");
            }
            else
                ch.send("flags [item|character] @target @flagtype @flags\r\n");
        }

        //[Command.Command("forcetick", "Force a tick update to happen.", Positions.Dead, 0)]
        public static void DoForceTick(Character ch, string arguments)
        {
            Game.Instance.PerformTick();
            ch.send("OK.\r\n");
        }

        public static void DoForce(Character ch, string arguments)
        {
            string name = "";
            string command = "";
            string commandargs = "";
            Character pet;

            arguments = arguments.OneArgument(ref name);
            arguments = arguments.OneArgument(ref command);
            arguments = arguments.OneArgument(ref commandargs);

            if (name.ISEMPTY() || command.ISEMPTY())
            {
                ch.send("Order who to do what?\r\n");
                return;
            }

            if (commandargs.StringCmp("self"))
                commandargs = ch.GetName;

            if (name.StringCmp("all"))
            {
                foreach (var other in Character.Characters.ToArray())
                {
                    if(other.IsNPC || other.Level < ch.Level)
                    other.DoCommand(command + " " + commandargs + (!arguments.ISEMPTY() ? " " + arguments : ""));
                }
                ch.send("OK.\r\n");
            }
            else if ((pet = Character.GetCharacterWorld(ch, name, false)) != null && (pet.IsNPC || pet.Level < ch.Level))
            {
                pet.DoCommand(command + " " + commandargs + (!arguments.ISEMPTY()? " " + arguments:""));
                ch.send("OK.\r\n");
            }
            else
            {
                ch.send("You couldn't force them to do anything.\r\n");
                return;
            }

        } // end do force


        public static void DoSwitch(Character ch, string arguments)
        {
            string name = "";
            Character pet;

            arguments = arguments.OneArgument(ref name);
            
            if(ch is NPCData && ch.SwitchedBy != null)
            {
                var Switched = ch;
                ch = ch.SwitchedBy;
                Switched.SwitchedBy = null;
                ch.Switched = null;
            }

            if (name.ISEMPTY())
            {
                ch.send("Switch to what npc?\r\n");
                return;
            }

            if ((pet = Character.GetCharacterWorld(ch, name, false)) != null && pet.IsNPC)
            {
                if(pet.SwitchedBy != null)
                {
                    pet.SwitchedBy.Switched = null;
                }    
                ch.Switched = pet;
                pet.SwitchedBy = (Player) ch;
                ch.send("OK.\r\n");
            }
            else
            {
                ch.send("You couldn't switch into them.\r\n");
                return;
            }
        } // end do switch

        public static void DoReturn(Character ch, string arguments)
        {

            if (ch is NPCData && ch.SwitchedBy != null)
            {
                var Switched = ch;
                ch = ch.SwitchedBy;
                Switched.SwitchedBy = null;
                ch.Switched = null;
                ch.send("OK.\r\n");
            }
            else
            {
                ch.send("You failed to return.\r\n");
                return;
            }
        } // end do return

        public static void DoSnoop(Character ch, string arguments)
        {
            string name = "";
            Character pet;

            arguments = arguments.OneArgument(ref name);

            if (ch is NPCData && ch.SwitchedBy != null)
            {
                ch = ch.SwitchedBy;
            }

            if (name.ISEMPTY())
            {
                foreach(var player in Game.Instance.Info.Connections)
                {
                    if (player.SnoopedBy == ch) player.SnoopedBy = null;
                }
                ch.send("OK.\r\n");
                return;
            }

            if ((pet = Character.GetCharacterWorld(ch, name, true)) != null && !pet.IsNPC && pet != ch)
            {
                pet.SnoopedBy = (Player) ch;
                ch.send("OK.\r\n");
            }
            //else if(pet == ch)
            //{
            //    DoReturn(ch, "");
            //}
            else
            {
                ch.send("You couldn't snoop them.\r\n");
                return;
            }
        } // end do snoop

        public static void DoTrust(Character ch, string arguments)
        {
            arguments = arguments.OneArgumentOut(out var charactername);
            arguments = arguments.OneArgumentOut(out var truststr);
            Character target = null;
            if (charactername.ISEMPTY() || truststr.ISEMPTY())
            {
                ch.send("Syntax: trust [charactername] [level]\r\n");
                ch.send("Your trust is level {0}.\r\n", ch.Trust);
            }
            else if(!int.TryParse(truststr, out int value) || value < 1 || value > ch.Trust)
            {
                ch.send("Level must be numeric, greater than or equal to 1 and less than or equal to {0}.\r\n", ch.Trust);
            }
            else if((target = Character.GetCharacterWorld(charactername)) == null)
            {
                ch.send("You couldn't find them.\r\n");
            }
            else
            {
                target.Trust = value;
                ch.send("{0}'s trust set to {1}.\r\n", target.Name, value);
            }
        }
    } // End DoActWiz
}
