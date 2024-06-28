using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace CrimsonStainedLands
{
    public class DoActInfo
    {


        public static void DoAffects(Character ch, string arguments)
        {
            StringBuilder affects = new StringBuilder();
            AffectData lastAffect = null;
            foreach (var affect in ch.AffectsList.OrderBy(a => a.duration))
            {
                if (!affect.hidden)
                {
                    //if (lastAffect == null || affect.displayName != lastAffect.displayName)
                    {
                        if (affect.modifier != 0)
                            affects.AppendLine((affect.displayName + ":").PadRight(20) + " " + affect.location.ToString().ToLower().PadLeft(15)
                                + ((affect.modifier > 0 ? "+" : " ") + affect.modifier.ToString()).PadLeft(5) + (" for " + affect.duration + (affect.frequency == Frequency.Tick ? " hours." : " rounds.")).PadRight(25));

                        else
                            affects.AppendLine((affect.displayName + ":").PadRight(41) + (" for " + affect.duration + (affect.frequency == Frequency.Tick ? " hours." : " rounds.")).PadRight(25));
                    }
                    //else
                    //{
                    //    if (affect.modifier != 0)
                    //        affects.AppendLine(" ".PadRight(30) + " " + affect.location.ToString().ToLower().PadRight(8)
                    //            + ((affect.modifier > 0 ? "+" : "") + affect.modifier.ToString()).PadLeft(3) + " " + affect.duration + " hours.");
                    //    else
                    //        affects.AppendLine(" ".PadRight(30) + " " + affect.duration + " hours.");
                    //}
                    lastAffect = affect;
                }
            }
            if (affects.Length == 0)
                affects.AppendLine("Nothing.");
            ch.send("You are currently affected by:\n\r" + affects.ToString());

            ItemData wield = ch.GetEquipment(WearSlotIDs.Wield);
            ItemData offhand = ch.GetEquipment(WearSlotIDs.DualWield);

            if (wield != null && wield.IsAffected(AffectFlags.Poison))
            {
                var aff = wield.FindAffect(AffectFlags.Poison);
                ch.Act("$p is poisoned for {0} hours.", item: wield, args: aff.duration);
            }

            if (offhand != null && offhand.IsAffected(AffectFlags.Poison))
            {
                var aff = offhand.FindAffect(AffectFlags.Poison);
                ch.Act("$p is poisoned for {0} hours.", item: offhand, args: aff.duration);
            }
        }



        public static void DoScore(Character ch, string arguments)
        {
            ch.send("You are {0} the {1} {2}, a {3} at level {4}.\n\r", ch.Name, ch.Sex != Sexes.None ? ch.Sex.ToString() : "sexless", ch.Race.name.ToLower(), ch.Guild.name, ch.Level);
            ch.send("Alignment: {0}, Ethos: {1}.\n\r", ch.Alignment.ToString().ToLower(), ch.Ethos.ToString().ToLower());
            if (ch.PermanentStats != null)
                ch.send("Strength: {0}(+{1}), Wisdom: {2}(+{3}), Intelligence: {4}(+{5}), Dexterity: {6}(+{7}), Constitution: {8}(+{9}), Charisma: {10}(+{11})\n\r",
                    ch.GetCurrentStat(PhysicalStatTypes.Strength), (ch.GetModifiedStatUncapped(PhysicalStatTypes.Strength) >= ch.GetCurrentStat(PhysicalStatTypes.Strength) ? ch.GetModifiedStatUncapped(PhysicalStatTypes.Strength) - ch.GetCurrentStat(PhysicalStatTypes.Strength) : 0),
                    ch.GetCurrentStat(PhysicalStatTypes.Wisdom), (ch.GetModifiedStatUncapped(PhysicalStatTypes.Wisdom) > ch.GetCurrentStat(PhysicalStatTypes.Wisdom) ? ch.GetModifiedStatUncapped(PhysicalStatTypes.Wisdom) - ch.GetCurrentStat(PhysicalStatTypes.Wisdom) : 0),
                    ch.GetCurrentStat(PhysicalStatTypes.Intelligence), (ch.GetModifiedStatUncapped(PhysicalStatTypes.Intelligence) >= ch.GetCurrentStat(PhysicalStatTypes.Intelligence) ? ch.GetModifiedStatUncapped(PhysicalStatTypes.Intelligence) - ch.GetCurrentStat(PhysicalStatTypes.Intelligence) : 0),
                    ch.GetCurrentStat(PhysicalStatTypes.Dexterity), (ch.GetModifiedStatUncapped(PhysicalStatTypes.Dexterity) >= ch.GetCurrentStat(PhysicalStatTypes.Dexterity) ? ch.GetModifiedStatUncapped(PhysicalStatTypes.Dexterity) - ch.GetCurrentStat(PhysicalStatTypes.Dexterity) : 0),
                    ch.GetCurrentStat(PhysicalStatTypes.Constitution), (ch.GetModifiedStatUncapped(PhysicalStatTypes.Constitution) >= ch.GetCurrentStat(PhysicalStatTypes.Constitution) ? ch.GetModifiedStatUncapped(PhysicalStatTypes.Constitution) - ch.GetCurrentStat(PhysicalStatTypes.Constitution) : 0),
                    ch.GetCurrentStat(PhysicalStatTypes.Charisma), (ch.GetModifiedStatUncapped(PhysicalStatTypes.Charisma) >= ch.GetCurrentStat(PhysicalStatTypes.Charisma) ? ch.GetModifiedStatUncapped(PhysicalStatTypes.Charisma) - ch.GetCurrentStat(PhysicalStatTypes.Charisma) : 0));
            var ac = ch.GetArmorClass();
            ch.send("AC Bash {0}, Slash {1}, Pierce {2}, Exotic {3}\n\r", ac.acBash, ac.acSlash, ac.acPierce, ac.acExotic);
            ch.send("Carry #: {0}/{1}, Weight {2}/{3}\n\r", ch.Carry, ch.MaxCarry, ch.TotalWeight, ch.MaxWeight);

            ch.send("Practices: {0}, Trains {1}\n\r", ch.Practices, ch.Trains);
            ch.send("Hitpoints: {0}/{1} Mana: {2}/{3} Movement: {4}/{5}.\n\r", ch.HitPoints, ch.MaxHitPoints, ch.ManaPoints, ch.MaxManaPoints, ch.MovementPoints, ch.MaxMovementPoints);
            ch.send("Damage Roll: {0}, Hit Roll: {1}\n\r", ch.GetDamageRoll, ch.GetHitRoll);

            DoWorth(ch, arguments);
            DoAffects(ch, arguments);
        }

        public static void DoWorth(Character ch, string arguments)
        {
            if (ch.Form != null)
            {
                ch.send(string.Format("You need {0} xp to level({1} of {2})\n\r",
                (ch.XpToLevel * (ch.Level)) - ch.Xp, ch.XpTotal, ch.XpToLevel * (ch.Level)));
            }
            else
                ch.send(string.Format("You have {0} silver, and {1} gold. You need {2} xp to level({3} of {4})\n\r",
                    ch.Silver.ToString(), ch.Gold.ToString(), (ch.XpToLevel * (ch.Level)) - ch.Xp, ch.XpTotal, ch.XpToLevel * (ch.Level)));

        }

        public static void DoConsider(Character ch, string arguments)
        {
            if (arguments.ISEMPTY())
            {
                ch.send("Consider killing whom?\n\r");
                return;
            }
            int count = 0;
            var victim = ch.GetCharacterFromRoomByName(arguments, ref count);
            if (victim == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            var diff = victim.Level - ch.Level;
            string message;
            if (diff <= -10)
                message = "You could kill $N with your little finger.\n\r";
            else if (diff <= -5)
                message = "$N doesn't have a fighting chance.\n\r";
            else if (diff <= -2)
                message = "$N looks like an easy kill.\n\r";
            else if (diff <= 1)
                message = "The perfect match!\n\r";
            else if (diff <= 4)
                message = "A few lucky blows would kill $M.\n\r";
            else if (diff <= 9)
                message = "$N shows you $S razor-sharp teeth.\n\r";
            else
                message = "An ominous, hooded figure waits patiently nearby.\n\r";

            ch.Act(message, victim, null, null, ActType.ToChar);

            if (victim.Alignment == Alignment.Good) message = "$N smiles happily at you.";
            else if (victim.Alignment == Alignment.Evil) message = "$N grins evilly at you.";
            else message = "$N seems indifferent towards you.";

            ch.Act(message, victim, type: ActType.ToChar);
            return;
        }

        [ColorConfiguration.NoEscapeColor]
        public static void DoColor(Character ch, string arguments)
        {
            if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.Color)))
            {
                ch.Flags.ADDFLAG(ActFlags.Color);
                ch.send("\\GColor\\x is \\gON\\x.\n\r");
            }
            else if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.Color)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.Color);
                ch.send("Color is OFF.\n\r");
            }
            else if("reset".StringPrefix(arguments))
            {
                ch.ColorConfigurations.Clear();
                ch.send("Color reset.\n\r");
            }
            else
            {
                arguments = arguments.OneArgumentOut(out var option);

                var configs = from kvp in ColorConfiguration.DefaultColors where !kvp.Key.ToString().Contains("Holylight", StringComparison.InvariantCultureIgnoreCase) || ch.IsImmortal select new { Key = kvp.Key.ToString().Replace("_", "."), Value = kvp.Key, ColorValue = kvp.Value };

                var config = configs.FirstOrDefault(c => c.Key.StringCmp(option));

                if(config != null && Enum.TryParse<ColorConfiguration.Keys>(config.Key.Replace(".", "_"), false, out var configkey))
                {
                    if (arguments.ISEMPTY() || arguments.StringCmp("default"))
                    {
                        ch.ColorConfigurations.Remove(configkey);
                        ch.send($"Color reset to default {config.ColorValue}{XTermColor.EscapeColor(config.ColorValue)}\\x.\n\r");
                    }
                    else
                    {
                        ch.ColorConfigurations[configkey] = arguments;
                        ch.send($"Color configured to {arguments}{XTermColor.EscapeColor(arguments)}\\x.\n\r");
                    }
                    
                }
                else
                {
                    foreach(var kvp in configs)
                    {
                        ch.send($"Syntax: color {kvp.Key} {ch.GetColor(kvp.Value)}{XTermColor.EscapeColor(ch.GetColor(kvp.Value))}\\x\n\r");
                    }
                    ch.send("Syntax: color [on|off]\n\r");
                }

            }
        }

        public static void DoAFK(Character ch, string arguments)
        {
            if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.AFK)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.AFK);
                ch.send("AFK \\rOFF\\x.\n\r");
            }
            else if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.AFK)))
            {
                ch.Flags.SETBIT(ActFlags.AFK);
                ch.send("AFK \\gON\\x.\n\r");
            }
            else
                ch.send("Syntax: AFK [on|off]\n\r");
        }

        public static void DoDamage(Character ch, string arguments)
        {
            if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.DamageOnType)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.DamageOnType);
                ch.send("Damage message based on type now \\rOFF\\x.\n\r");
            }
            else if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.DamageOnType)))
            {
                ch.Flags.SETBIT(ActFlags.DamageOnType);
                ch.send("Damage message based on type now \\gON\\x.\n\r");
            }
            else
                ch.send("Syntax: AFK [on|off]\n\r");
        }


        public static void DoAutosac(Character ch, string arguments)
        {
            if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.AutoSac)))
            {
                ch.Flags.ADDFLAG(ActFlags.AutoSac);
                ch.send("\\GAutosac\\x is \\gON\\x.\n\r");
            }
            else if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.AutoSac)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.AutoSac);
                ch.send("\\GAutosac\\x is \\rOFF\\x.\n\r");
            }
            else
                ch.send("Syntax: Autosac [on|off]\n\r");
        }



        public static void DoAutoloot(Character ch, string arguments)
        {
            if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.AutoLoot)))
            {
                ch.Flags.ADDFLAG(ActFlags.AutoLoot);
                ch.send("\\GAutoLoot\\x is \\gON\\x.\n\r");
            }
            else if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.AutoLoot)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.AutoLoot);
                ch.send("\\GAutoLoot\\x is \\rOFF\\x.\n\r");
            }
            else
                ch.send("Syntax: AutoLoot [on|off]\n\r");
        }

        public static void DoAutogold(Character ch, string arguments)
        {
            if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.AutoGold)))
            {
                ch.Flags.ADDFLAG(ActFlags.AutoGold);
                ch.send("\\GAutoGold\\x is \\gON\\x.\n\r");
            }
            else if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.AutoGold)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.AutoGold);
                ch.send("\\GAutoGold\\x is \\rOFF\\x.\n\r");
            }
            else
                ch.send("Syntax: AutoGold [on|off]\n\r");
        }

        public static void DoAutosplit(Character ch, string arguments)
        {
            if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.AutoSplit)))
            {
                ch.Flags.ADDFLAG(ActFlags.AutoSplit);
                ch.send("\\GAutoSplit\\x is \\gON\\x.\n\r");
            }
            else if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.AutoSplit)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.AutoSplit);
                ch.send("\\GAutoSplit\\x is \\rOFF\\x.\n\r");
            }
            else
                ch.send("Syntax: AutoSplit [on|off]\n\r");
        }

        public static void DoBrief(Character ch, string arguments)
        {
            if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.Brief)))
            {
                ch.Flags.ADDFLAG(ActFlags.Brief);
                ch.send("\\GBrief\\x is \\gON\\x.\n\r");
            }
            else if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.Brief)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.Brief);
                ch.send("Brief is \\rOFF\\x.\n\r");
            }
            else
                ch.send("Syntax: Brief [on|off]\n\r");
        }
        public static void DoAutoassist(Character ch, string arguments)
        {
            if ("on".StringPrefix(arguments) || (arguments.ISEMPTY() && !ch.Flags.ISSET(ActFlags.AutoAssist)))
            {
                ch.Flags.ADDFLAG(ActFlags.AutoAssist);
                ch.send("\\GAutoAssist\\x is \\gON\\x.\n\r");
            }
            else if ("off".StringPrefix(arguments) || (arguments.ISEMPTY() && ch.Flags.ISSET(ActFlags.AutoAssist)))
            {
                ch.Flags.REMOVEFLAG(ActFlags.AutoAssist);
                ch.send("\\GAutoAssist\\x is \\rOFF\\x.\n\r");
            }
            else
                ch.send("Syntax: AutoGold [on|off]\n\r");
        }

        public static void ReadHelp(Character ch, string arguments, bool plain = false)
        {
            StringBuilder output = new StringBuilder();

            arguments.OneArgumentOut(out var firstarg);

            if (!firstarg.ISEMPTY() && firstarg.StringCmp("list"))
            {
                IEnumerable<HelpData> helps = null;
                arguments = arguments.OneArgument();

                if (!arguments.ISEMPTY())
                    helps = from help
                            in HelpData.Helps
                            where help.vnum.ToString().StringPrefix(arguments) || help.keyword.IsName(arguments)
                            orderby help.keyword
                            select help;
                else
                    helps = from help
                            in HelpData.Helps
                            orderby help.keyword
                            select help;

                foreach (var help in helps)
                {
                    if (help.level > ch.Level) continue;
                    output.AppendLine(string.Format("{0,-10} :: {1}", help.vnum, help.keyword).FriendlyWrapText());
                }

            }
            else
            {
                if (arguments.ISEMPTY())
                    arguments = "help";
                int lvl;
                foreach (var help in HelpData.Helps)
                {

                    lvl = (help.level < 0) ? -1 * help.level - 1 : help.level;

                    if (lvl > ch.Level)
                        continue;

                    if (help.keyword.IsName(arguments) || help.vnum.ToString() == arguments)
                    {
                        if (!plain)
                        {
                            output.AppendLine("Keywords: " + help.keyword + " :: Help Entry " + help.vnum);
                            output.Append("\n\r" + new string('-', 80) + "\n\r");
                        }

                        if (help.text.StartsWith("."))
                            output.Append(help.text.Substring(1));
                        else
                            output.Append(help.text.FriendlyWrapText());

                        if (!plain)
                        {
                            output.Append("\n\r" + new string('-', 80) + "\n\r");
                            output.Append(string.Format("Last edited on {0} by {1}.\n\r", help.lastEditedOn, help.lastEditedBy));
                            output.AppendLine();
                        }
                    }


                }
                IEnumerable<Command> commands;
                if ((commands = Command.Commands.Where(com => com.Name.StringPrefix(arguments))).Any())
                {
                    foreach (var command in commands)
                        output.AppendLine(command.Name + " - " + command.Info);
                }

            }
            if (output.Length == 0)
                ch.send("No help on that word.\n\r");
            else
            {
                using (var page = new Character.Page(ch))
                {
                    var outputstring = output.ToString();
                    ch.send(outputstring);
                    if (!outputstring.EndsWith("\n") && !outputstring.EndsWith("\r"))
                        ch.send("\n\r");
                }

            }

        }
        public static void DoHelp(Character ch, string arguments)
        {
            ReadHelp(ch, arguments);
        }

        public static void DoDescription(Character ch, string arguments)
        {
            var command = "";
            if (ch.Description == null)
                ch.Description = "";
            arguments = arguments.OneArgument(ref command);

            if (string.IsNullOrEmpty(command))
            {
                ch.send("Your description is:\n\r");
                ch.send(ch.Description + "\n\r");
            }
            else if (command == "-" && !string.IsNullOrEmpty(ch.Description))
            {
                var newlineIndex = ch.Description.LastIndexOf("\n");
                if (newlineIndex > -1)
                {
                    ch.Description = ch.Description.Substring(0, newlineIndex);
                }
                else
                    ch.Description = "";
                ch.send("Line removed.\n\r");

            }
            else if (command == "+")
            {
                if ((!ch.Description.EndsWith("\n") || !ch.Description.EndsWith("\n\r")) && ch.Description.Length != 0)
                    ch.Description = ch.Description + "\n\r" + arguments;
                else
                    ch.Description = arguments;
                ch.send("Line added.\n\r");
            }
            else
            {
                ch.send("Syntax: description [+|-] @text");
            }

        }
        public static void DoLook(Character ch, string arguments)
        {
            Character other = null;
            ItemData lookitem = null;
            Direction direction = Direction.North;
            String incheck = "";
            String containername;
            ExtraDescription extraDescription = null;
            int count = 0;
            ExitData exit;

            if (ch.Room == null)
            {
                ch.send("You are not in a room.\n\r");
                return;
            }

            if (ch.IsAffected(AffectFlags.Blind))
            {
                ch.send("You can't see anything!\n\r");
                return;
            }

            if (ch.Position < Positions.Sleeping)
            {
                ch.send("You can't see anything but stars!\n\r");
                return;
            }

            if (ch.Position == Positions.Sleeping)
            {
                ch.send("You can't see anything, you're sleeping!\n\r");
                return;
            }

            var IsDark = ch.Room.IsDark;

            if (!ch.IsAffected(AffectFlags.Infrared) && !ch.IsAffected(AffectFlags.DarkVision) && !ch.IsAffected(AffectFlags.NightVision) && IsDark)
            {
                ch.send("It is pitch black ... \n\r");
                return;
            }

            if (arguments.ISEMPTY() || arguments.StringCmp("auto"))
            {
                using (new Character.Page(ch))
                {
                    if (!ch.IsAffected(AffectFlags.DarkVision) && !ch.IsAffected(AffectFlags.Infrared) && !ch.IsAffected(AffectFlags.NightVision) && IsDark)
                    {
                        ch.send("It is pitch black ... \n\r");
                    }
                    else
                    {
                        var desc = (TimeInfo.IS_NIGHT && !ch.Room.NightDescription.ISEMPTY() ? ch.Room.NightDescription : ch.Room.Description);
                        desc = desc.WrapText(firstlinelength: 75);

                        ch.send(ColorConfiguration.ColorString(ColorConfiguration.Keys.Room_Name) + (TimeInfo.IS_NIGHT && !ch.Room.NightName.ISEMPTY() ? ch.Room.NightName : ch.Room.Name) + ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset) + (ch.Flags.ISSET(ActFlags.HolyLight) ? ColorConfiguration.ColorString(ColorConfiguration.Keys.Holylight_VNum) + " [" + ch.Room.Vnum + "]\\x" : "") + "\n\r");
                        if (!ch.Flags.ISSET(ActFlags.Brief) || arguments.ISEMPTY())
                            ch.send("    " + ColorConfiguration.ColorString(ColorConfiguration.Keys.Room_Description) + desc + ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset) + "\n\r\n\r");
                        else
                            ch.send("\n\r");
                        DoExits(ch, "");
                        var tempItemList = new Dictionary<string, int>();
                        foreach (var item in ch.Room.items.Where(i => ch.CanSee(i)))
                        {
                            if ((!item.LongDescription.ISEMPTY() || (TimeInfo.IS_NIGHT && !item.NightLongDescription.ISEMPTY())) && ch.CanSee(item))
                            {
                                var itemshow = item.DisplayFlags(ch) + item.DisplayToRoom(ch);

                                if (tempItemList.ContainsKey(itemshow))
                                    tempItemList[itemshow] = tempItemList[itemshow] + 1;
                                else
                                    tempItemList[itemshow] = 1;
                                //    carrying.AppendLine();
                            }


                        }
                        StringBuilder carrying = new StringBuilder();
                        foreach (var itemkvp in tempItemList)
                        {
                            carrying.AppendLine("    " + (itemkvp.Value > 1 ? "[" + itemkvp.Value + "] " : "") + itemkvp.Key);
                        }
                        ch.send(carrying.ToString());

                    }
                    SkillSpell fog = SkillSpell.SkillLookup("faerie fog");
                    foreach (var person in ch.Room.Characters)
                    {
                        if (person != ch && ch.CanSee(person) && !(person.IsNPC && (person.LongDescription.TOSTRINGTRIM().ISEMPTY() && (!TimeInfo.IS_NIGHT || person.NightLongDescription.ISEMPTY()))))
                        {

                            if (ch.IsAffected(AffectFlags.DarkVision) || ch.IsAffected(AffectFlags.Infrared) || ch.IsAffected(AffectFlags.NightVision) || !IsDark)
                            {


                                ch.send(person.DisplayFlags(ch)); // send separate so act capitalizes first character
                                ch.Act(person.GetLongDescription(ch).Trim() + "\n\r");
                            }
                        }
                    }
                }
            }
            else if (arguments.StringCmp("all"))
            {
                foreach (var alldirection in Enum.GetValues(typeof(Direction)).OfType<Direction>())
                {
                    ch.send("You look " + alldirection.ToString().ToLower() + ".\n\r");
                    ch.Act("$n looks " + alldirection.ToString().ToLower() + ".\n\r", type: ActType.ToRoom);
                    ExitData iexit;
                    if ((iexit = ch.Room.exits[(int)alldirection]) != null && !string.IsNullOrEmpty(iexit.description))
                    {
                        //if(exit.destination != null)
                        //    ch.send(exit.destination.name + " lies in this direction.\n\r");
                        ch.send(iexit.description + "\n\r");
                    }
                    else if (iexit != null && iexit.destination != null)
                        ch.send(((TimeInfo.IS_NIGHT && !iexit.destination.NightName.ISEMPTY() ? iexit.destination.NightName : iexit.destination.Name)) + " lies" + (alldirection == Direction.Up? " above" : (alldirection == Direction.Down? " below" : (" to the " + alldirection.ToString().ToLower()))) + ".\n\r");
                    else
                        ch.send("You don't see anything special that way.\n\r");
                }
            }
            else if (ch.Room != null && ch.Room.GetExit(arguments, out var iexit, ref count)) //  Utility.GetEnumValueStrPrefix<Direction>(arguments, ref direction))
            {
                ch.send("You look " + iexit.direction.ToString().ToLower() + ".\n\r");
                ch.Act("$n looks " + iexit.direction.ToString().ToLower() + ".\n\r", type: ActType.ToRoom);
                //ExitData iexit;
                if (!string.IsNullOrEmpty(iexit.description))
                {
                    //if(exit.destination != null)
                    //    ch.send(exit.destination.name + " lies in this direction.\n\r");
                    ch.send(iexit.description + "\n\r");
                }
                else if (iexit != null && iexit.destination != null)
                    ch.send(((TimeInfo.IS_NIGHT && !iexit.destination.NightName.ISEMPTY() ? iexit.destination.NightName : iexit.destination.Name)) + " lies" + (iexit.direction == Direction.Up ? " above" : (iexit.direction == Direction.Down ? " below" : (" to the " + iexit.direction.ToString().ToLower()))) + ".\n\r");
                else
                    ch.send("You don't see anything special that way.\n\r");
            }

            else if (arguments.StringCmp("self") || ((other = ch.GetCharacterFromRoomByName(arguments, ref count)) != null && ch.CanSee(other)))
            {
                if (arguments.StringCmp("self"))
                    other = ch;
                ch.send("You look at " + other.Display(ch) + ".\n\r");

                if (other.Form != null && !other.Form.Description.ISEMPTY())
                {
                    var regex = new Regex("(?m)^\\s+");
                    ch.send(regex.Replace(other.Form.Description.Trim(), "").WrapText() + "\n\r");
                }
                else if (other.Form == null && !other.Description.ISEMPTY())
                    ch.send(other.Description.WrapText() + "\n\r");
                else
                    ch.send("You see nothing special about them.\n\r");
                ch.send("\n\r");
                string health = "";
                var hp = (float)other.HitPoints / (float)other.MaxHitPoints;
                if (hp == 1)
                    health = "is in perfect health.";
                else if (hp > .8)
                    health = "is covered in small scratches.";
                else if (hp > .7)
                    health = "has some small wounds.";
                else if (hp > .6)
                    health = "has some larger wounds.";
                else if (hp > .5)
                    health = "is bleeding profusely.";
                else if (hp > .4)
                    health = "writhing in agony.";
                else if (hp > 0)
                    health = "convulsing on the ground.";
                else
                    health = "is dead.";
                ch.Act("$N {0}", other, null, null, ActType.ToChar, health);
                //ch.send(other.Display(ch) + " " + health + "\n\r");

                ch.Act("{0}", other, null, null, ActType.ToChar, other.GetEquipmentString(ch));
                Character.CheckPeek(ch, other);
            }
            else if (!string.IsNullOrEmpty((containername = arguments.OneArgument(ref incheck))) && "in".StringPrefix(incheck) && (lookitem = ch.GetItemHere(containername)) != null)
            {
                if (!lookitem.ItemType.Contains(ItemTypes.Container))
                {
                    ch.send("{0} isn't a container.\n\r", lookitem.Display(ch));
                    return;
                }

                if (lookitem.extraFlags.Contains(ExtraFlags.Closed))
                {
                    ch.send("{0} is closed.\n\r", lookitem.Display(ch));
                    return;
                }
                Character.SendItemList(ch, lookitem);
            }
            else if ((extraDescription = ch.GetExtraDescriptionByKeyword(arguments, ref count)) != null)
            {
                ch.send(extraDescription.Description.WrapText() + "\n\r");
            }
            else if ((lookitem = ch.GetItemHere(arguments, ref count)) != null)
            {
                ch.send((!lookitem.Description.ISEMPTY() ? lookitem.Description : lookitem.LongDescription).WrapText() + "\n\r");

                if (lookitem.extraFlags.Contains(ExtraFlags.Closed))
                {
                    ch.send("{0} is closed.\n\r", lookitem.Display(ch));
                }
                else if (lookitem.extraFlags.Contains(ExtraFlags.Closable))
                {
                    ch.send("{0} is open.\n\r", lookitem.Display(ch));
                }

                if (lookitem.CarriedBy == ch)
                {
                    var itemtype = string.Join(" ", lookitem.ItemType);
                    var wearloc = "";
                    foreach (var slot in Character.WearSlots)
                        if (lookitem.wearFlags.ISSET(slot.flag))
                        {
                            wearloc = slot.wearString;
                            break;
                        }
                    ch.Act("$p is {0} worn {1}, made of {2}, and weighs {3} pounds.\n\r", null, lookitem, null, ActType.ToChar, itemtype.ToLower(), wearloc, lookitem.Material, lookitem.totalweight);
                }

                if (lookitem.ItemType.ISSET(ItemTypes.Container) && !lookitem.extraFlags.Contains(ExtraFlags.Closed))
                    Character.SendItemList(ch, lookitem);
            } // end of look item

            //else if ((exit = ch.Room.GetExit(arguments, ref count)) != null)
            //{
            //    ch.send("You look " + exit.direction + ".\n\r");
            //    ch.send(exit.description.Trim() + "\n\r");
            //}
            else
            {
                ch.send("You don't see that here.\n\r");
            }
        }

        public static void DoExits(Character ch, string v)
        {
            var exits = new List<string>();

            foreach (var iexit in ch.Room.exits)
            {
                if (iexit != null && iexit.destination != null)
                {
                    if (iexit.flags.ISSET(ExitFlags.HiddenWhileClosed) && iexit.flags.ISSET(ExitFlags.Closed))
                        continue;
                    if (iexit.flags.ISSET(ExitFlags.Hidden))
                        continue;
                    if (iexit.flags.ISSET(ExitFlags.Window))
                        continue;

                    if (!(ch.IsImmortal || (ch.Level <= iexit.destination.MaxLevel && ch.Level >= iexit.destination.MinLevel)))
                        continue;

                    else if (iexit.flags.ISSET(ExitFlags.Closed) || iexit.flags.ISSET(ExitFlags.Locked))
                    {
                        exits.Add(ColorConfiguration.ColorString(ColorConfiguration.Keys.Room_Exits_Door) + "[" + iexit.direction.ToString().ToLower() + "]" + ColorConfiguration.ColorString(ColorConfiguration.Keys.Room_Exits));
                    }
                    else
                        exits.Add(iexit.direction.ToString().ToLower());
                }
            }

            if (exits.Count == 0) exits.Add("none");
            ch.send(ColorConfiguration.ColorString(ColorConfiguration.Keys.Room_Exits) + "[Exits " + String.Join(" ", exits) + "]" + ColorConfiguration.ColorString(ColorConfiguration.Keys.Reset)  + "\n\r");
        }

        public static void DoScan(Character ch, string arguments)
        {
            Direction direction = Direction.North;
            if (Utility.GetEnumValueStrPrefix<Direction>(arguments, ref direction))
            {
                Character.ScanDirection(ch, direction);
            }
            else if (arguments.ISEMPTY() || "all".StringPrefix(arguments))
            {
                foreach (var alldirection in Enum.GetValues(typeof(Direction)).OfType<Direction>())
                {
                    Character.ScanDirection(ch, alldirection);
                }
            }
            else
                ch.send("You can't scan that.\n\r");
        }



        public static void DoCommands(Character ch, string arguments)
        {
            int count = 0;
            using (new Character.Page(ch))
            {
                ch.send("Current commands:\n\r");
                foreach (var command in Command.Commands)
                {
                    if (ch.Level < command.MinimumLevel)
                        continue;
                    if (command.Skill != null && ch.GetSkillPercentage(command.Skill) == 0)
                        continue;
                    count++;
                    ch.send("{0,15} ", command.Name); // - " + command.info + "\n\r");
                    if (count > 1 && count % 4 == 0)
                        ch.send("\n\r");

                }
                ch.send("\n\r");
            }
        }

        public static void DoSocials(Character ch, string arguments)
        {
            int i = 1;
            using (new Character.Page(ch))
            {
                ch.send("Current socials:\n\r");
                foreach (var social in Social.Socials)
                {
                    ch.send("{0, -15}", social.Name);
                    if (i > 1 && i % 4 == 0)
                        ch.send("\n\r");
                    i++;
                }
                ch.send("\n\r");
            }
        }
        public static void DoSkills(Character ch, string arguments)
        {
            // var skills = from skill in ch.learned where skill.Key.skillType.Contains(SkillSpellTypes.Skill) && (skill.Key.skillLevel.ContainsKey(ch.guild.name) || skill.Value > 0) orderby skill.Key.skillLevel.ContainsKey(ch.guild.name) ? skill.Key.skillLevel[ch.guild.name] : 100 select skill;
            using (new Character.Page(ch))
            {
                var text = new StringBuilder();

                if (ch.Form != null)
                {
                    var dodgeskill = ch.GetSkillPercentage("dodge");
                    var evasionskill = ch.GetSkillPercentage("evasion");
                    var damreductionskill = ch.GetSkillPercentage("damage reduction");

                    text.AppendLine("===== Defensive abilities =====");

                    if (dodgeskill > 90)
                        text.AppendLine("Passive: You have excellent dodge capabilities.");
                    else if (dodgeskill >= 75)
                        text.AppendLine("Passive: You have better than average dodge capabilities.");
                    else if (dodgeskill > 1)
                        text.AppendLine("Passive: You have some dodge capabilities.");

                    if (damreductionskill > 90)
                        text.AppendLine("Passive: You have excellent damage reduction.");
                    else if (damreductionskill >= 75)
                        text.AppendLine("Passive: You have average damage reduction.");
                    else if (damreductionskill > 1)
                        text.AppendLine("Passive: You have some damage reduction.");

                    if (damreductionskill > 90)
                        text.AppendLine("Passive: You have a chance to avoid an enemies attacks.");
                    else if (damreductionskill >= 75)
                        text.AppendLine("Passive: You have a small chance to avoid an enemies attacks.");
                    else if (damreductionskill > 1)
                        text.AppendLine("Passive: You have a slight chance to avoid an enemies attacks.");

                    if (ch.GetSkillPercentage("slow metabolism") > 1)
                        text.AppendLine("Passive: You have a slow metabolism, causing you to heal more every hour.");

                    if (ch.GetSkillPercentage("retract") > 1)
                        text.AppendLine("Retract: You can retract into your shell for defense.");

                    if (ch.GetSkillPercentage("mucous defense") > 1)
                        text.AppendLine("Passive: Enemies who hit you may begin to feel ill from your mucous defense.");

                    if (ch.GetSkillPercentage("zigzag") > 1)
                        text.AppendLine("Zigzag: Fool your enemies into missing their target.");

                    if (ch.GetSkillPercentage("tree climb") > 1)
                        text.AppendLine("Passive: Employ the tree branches to aid dodging, when within forests.");

                    if (ch.GetSkillPercentage("autotomy") > 1)
                        text.AppendLine("Autotomy: Detach tail once a day to take over fighting for you. You can either regen or run.");


                    text.AppendLine("===== Utility abilities =====");

                    if (ch.GetSkillPercentage("intercept") > 1)
                        text.AppendLine("Intercept: You can intercept foes from hitting someone else.");

                    if (ch.GetSkillPercentage("forage") > 1)
                        text.AppendLine("Forage: You know how to forage for food in the wilderness.");

                    if (ch.GetSkillPercentage("scavenge") > 1)
                        text.AppendLine("Scavenge: You know how to scavenge for food and water in just about any setting.");

                    if (ch.GetSkillPercentage("camouflage") > 1)
                        text.AppendLine("Camouflage: You know how to find cover in the wilderness.");

                    if (ch.GetSkillPercentage("burrow") > 1)
                        text.AppendLine("Burrow: Escape combat into a burrow.");

                    if (ch.GetSkillPercentage("play dead") > 1)
                        text.AppendLine("Playdead: Escape combat by playing dead.");

                    if (ch.GetSkillPercentage("lick self") > 1)
                        text.AppendLine("LickSelf: Use your saliva to heal yourself.");

                    if (ch.GetSkillPercentage("grip") > 1)
                        text.AppendLine("Passive: Chance to keep your foes from fleeing.");

                    if (ch.GetSkillPercentage("flight") > 1)
                        text.AppendLine("Flight: Use your wings to fly temporarily.");

                    if (ch.GetSkillPercentage("carrion feeding") > 1)
                        text.AppendLine("Passive: Eat corpses and body parts, even poisonous ones.");

                    if (ch.GetSkillPercentage("snarl") > 1)
                        text.AppendLine("Snarl: You snarl, reducing someones hitroll in half.");

                    if (ch.GetSkillPercentage("awareness") > 1)
                        text.AppendLine("Awareness: You are immune to surprise attacks, such as ambush or charge.");



                    text.AppendLine("===== Offensive abilities =====");

                    if (ch.GetSkillPercentage("quill defense") > 1)
                        text.AppendLine("Passive: Enemies will encounter your quills when striking you.");

                    if (ch.GetSkillPercentage("bite") > 1)
                        text.AppendLine("Bite: You can bite your foes causing damage.");

                    if (ch.GetSkillPercentage("claw") > 1)
                        text.AppendLine("Claw: You can claw at your foes causing damage.");

                    if (ch.GetSkillPercentage("trample") > 1)
                        text.AppendLine("Trample: You can charge at your foes trampling them.");

                    if (ch.GetSkillPercentage("furor") > 1)
                        text.AppendLine("Furor: You can release a series of ferocious attacks.");

                    if (ch.GetSkillPercentage("pinch") > 1)
                        text.AppendLine("Pinch: You can pinch foes with your pincers.");

                    if (ch.GetSkillPercentage("tailswipe") > 1)
                        text.AppendLine("TailSwipe: You can swipe your enemies with your tail.");

                    if (ch.GetSkillPercentage("antler swipe") > 1)
                        text.AppendLine("AntlerSwipe: You can swipe your enemies with your antlers.");

                    if (ch.GetSkillPercentage("jump") > 1)
                        text.AppendLine("Jump: You can jump on your foes, injuring them.");

                    if (ch.GetSkillPercentage("strike") > 1)
                        text.AppendLine("Strike: You can strike your foe.");

                    if (ch.GetSkillPercentage("spit") > 1)
                        text.AppendLine("spit: You can blind your foes with your spit.");

                    if (ch.GetSkillPercentage("hoof strike") > 1)
                        text.AppendLine("Hoofstrike: You can double kick your foes with hoofs.");

                    if (ch.GetSkillPercentage("devour") > 1)
                        text.AppendLine("Devour: You can devour your foes while you fight them.");

                    if (ch.GetSkillPercentage("charge") > 1)
                        text.AppendLine("Charge: Begin combat by charging your foe.");

                    if (ch.GetSkillPercentage("ambush") > 1)
                        text.AppendLine("Ambush: Use your cover to ambush your prey.");

                    if (ch.GetSkillPercentage("impale") > 1)
                        text.AppendLine("Impale: Impale your enemy leaving a lasting wound.");

                    if (ch.GetSkillPercentage("flank") > 1)
                        text.AppendLine("Flank: Circle behind your foe and strike them.");

                    if (ch.GetSkillPercentage("headbutt") > 1)
                        text.AppendLine("HeadButt: Headbutt your enemy hurting them, and yourself.");

                    if (ch.GetSkillPercentage("tusk jab") > 1)
                        text.AppendLine("TuskJab: Jab your enemy with your tusks injuring them.");

                    if (ch.GetSkillPercentage("hoof stomp") > 1)
                        text.AppendLine("HoofStomp: Stomp your enemy with your hooves injuring them.");

                    if (ch.GetSkillPercentage("dive") > 1)
                        text.AppendLine("Dive: Dive on your prey from a ledge or tree.");

                    if (ch.GetSkillPercentage("pounce attack") > 1)
                        text.AppendLine("PounceAttack: Pounce on your prey.");

                    if (ch.GetSkillPercentage("howl") > 1)
                        text.AppendLine("Howl: Unleash a deafening howl at your enemies.");

                    if (ch.GetSkillPercentage("quill spray") > 1)
                        text.AppendLine("Quillspray: Spray quills everywhere.");

                    if (ch.GetSkillPercentage("laugh") > 1)
                        text.AppendLine("Laugh: Empower yourself with a maniacal laugh.");

                    if (ch.GetSkillPercentage("noxious spray") > 1)
                        text.AppendLine("Noxiousspray: Spray noxious chemicals on your enemies.");

                    if (ch.GetSkillPercentage("venom spit") > 1)
                        text.AppendLine("Venomspit: Spit poisonous venom at your foe.");

                    if (ch.GetSkillPercentage("shoot blood") > 1)
                        text.AppendLine("Shootblood: Attempt to blind your foe by shooting blood at their eyes.");

                    if (ch.GetSkillPercentage("gland spray") > 1)
                        text.AppendLine("Glandspray: Spray sulfurous chemicals from your anal glands.");

                    if (ch.GetSkillPercentage("venom strike") > 1)
                        text.AppendLine("Venomstrike: Subdue your enemy with a venomous strike.");

                    if (ch.GetSkillPercentage("secreted filament") > 1)
                        text.AppendLine("Secretedfilament: Cover your enemy with a secreted filament.");

                    if (ch.GetSkillPercentage("acid excrete") > 1)
                        text.AppendLine("Acidexcrete: Direct a spray of acetic acid at your foe.");

                    if (ch.GetSkillPercentage("rip") > 1)
                        text.AppendLine("rip: shred your foe causing them to bleed.");
                    text.AppendLine();
                    text.AppendLine("Enliven: fly, haste, slow, pass door, stone skin.");

                    ch.send(text.ToString());
                    return;
                }

                if (!arguments.ISEMPTY())
                {
                    var skills = (from sk in SkillSpell.Skills.Values where sk.name.StringPrefix(arguments) && sk.SkillTypes.ISSET(SkillSpellTypes.Skill) select sk);

                    if (!skills.Any())
                    {
                        ch.send("You don't know any skills by that name");
                        return;
                    }
                    else
                    {
                        foreach (var skill in skills)
                        {
                            var percent = ch.GetSkillPercentage(skill);
                            var lvl = ch.GetLevelSkillLearnedAt(skill);
                            if (ch.Level < lvl)
                            {
                                ch.send("You haven't learned that skill yet.");
                                return;
                            }

                            ch.send(skill.name + " " + percent + "%\n\r");
                        }
                        return;
                    }
                }
                int lastLevel = 0;
                int column = 0;

                foreach (var skill in from tempskill in SkillSpell.Skills.Values
                                      where tempskill.SkillTypes.Contains(SkillSpellTypes.Skill)
                                      orderby ch.GetLevelSkillLearnedAt(tempskill)
                                      select tempskill) //skills)
                {
                    //ch.Learned.TryGetValue(skill, out int percent);
                    //skill.skillLevel.TryGetValue(ch.Guild.name, out int lvl);
                    var percent = ch.GetSkillPercentage(skill);
                    var lvl = ch.GetLevelSkillLearnedAt(skill);

                    if ((lvl < Game.LEVEL_HERO || ch.IsImmortal) || percent > 1)  //if (lvl > 0 || percent > 1 || (ch.Level > lvl && lvl > 0))
                    {
                        if (lvl != lastLevel)
                        {
                            lastLevel = lvl;
                            column = 0;
                            text.AppendLine();
                            text.Append("Lvl " + lvl + ": ".PadRight(5));
                            text.AppendLine();
                        }

                        text.Append("    " + (skill.name + " " + (ch.Level >= lvl || percent > 1 ? percent + "%" : "N/A")).PadLeft(20).PadRight(25));

                        if (column == 1)
                        {
                            text.AppendLine();
                            column = 0;
                        }
                        else
                            column++;
                    }
                }
                ch.send(text + "\n\r");

            } // using new page(ch)
        }



        public static void DoSpells(Character ch, string arguments)
        {

            if (ch.Form != null || (ch.Guild != null && ch.Guild.CastType != Magic.CastType.Cast))
            {
                ch.send("You don't know any spells.\n\r");
                return;
            }
            Character.ShowSpells(ch, arguments);
        }

        public static void DoSupplications(Character ch, string arguments)
        {

            if (ch.Form != null || (ch.Guild != null && ch.Guild.CastType != Magic.CastType.Commune))
            {
                ch.send("You don't know any supplications.\n\r");
                return;
            }
            Character.ShowSpells(ch, arguments);
        }

        public static void DoSongs(Character ch, string arguments)
        {

            if (ch.Form != null || (ch.Guild != null && ch.Guild.CastType != Magic.CastType.Sing))
            {
                ch.send("You don't know any songs.\n\r");
                return;
            }
            Character.ShowSpells(ch, arguments);
        }

        public static void DoForms(Character ch, string arguments)
        {
            bool any = false;
            int skill;
            var learned = ch.Learned.Keys.ToArray();
            foreach (var form in from f in ShapeshiftForm.Forms orderby Array.IndexOf(learned, f.FormSkill) select f)
            {
                if ((skill = ch.GetSkillPercentage(form.FormSkill)) > 1)
                {

                    ch.send("You are {0} with the {1} form.\n\r", skill == 100 ? "confident" : skill > 85 ? "competent" : "unfamiliar", form.Name);
                    any = true;
                }
            }

            if (!any)
                ch.send("You know no forms.\n\r");

        }



        public static void DoEmote(Character ch, string arguments)
        {
            if (arguments.ISEMPTY())
            {
                ch.send("Emote what?\n\r");
            }
            else
            {
                ch.Act("$n {0}", null, null, null, ActType.ToRoom, arguments);
                ch.Act("$n {0}", null, null, null, ActType.ToChar, arguments);
            }
        }



        public static void DoTime(Character ch, string argument)
        {
            string suf;
            var day = TimeInfo.Day + 1;

            if (day > 4 && day < 20) suf = "th";
            else if (day % 10 == 1) suf = "st";
            else if (day % 10 == 2) suf = "nd";
            else if (day % 10 == 3) suf = "rd";
            else suf = "th";

            ch.send("It is {0} o'clock {1}, Day of {2}, {3}{4} of the Month of {5}.\n\r",
                (TimeInfo.Hour % 12 == 0) ? 12 : TimeInfo.Hour % 12,
                TimeInfo.Hour >= 12 ? "pm" : "am",
                TimeInfo.DayName,
                day, suf,
                TimeInfo.MonthName);

            var runningtime = DateTime.Now - Game.Instance.GameStarted;

            ch.send("Server started at {0}.\n\rThe system time is {1}.\n\rGame has been running for {2} days, {3} hours and {4} minutes.\n\r",
                Game.Instance.GameStarted, DateTime.Now, runningtime.Days, runningtime.Hours, runningtime.Minutes);

            if (ch is Player)
            {
                var player = ch as Player;
                ch.send("Total Time played: {0}\n\r", (player.TotalPlayTime + (DateTime.Now - player.LastSaveTime)));
            }
            return;
        }



        public static void DoWeather(Character ch, string argument)
        {
            string buf;

            var sky_look = new string[]
            {
                "cloudless",
                "cloudy",
                "rainy",
                "lit by flashes of lightning"
            };

            if (!ch.IS_OUTSIDE)
            {
                ch.send("You can't see the weather indoors.\n\r");
                return;
            }

            buf = string.Format("The sky is {0} and {1}.\n\r",
                sky_look[(int)WeatherData.Sky],
                WeatherData.change >= 0 ? "a warm southerly breeze blows" : "a cold northern gust blows");
            ch.send(buf);
            return;
        }

        [ColorConfiguration.NoEscapeColor]
        public static void DoPrompt(Character ch, string argument)
        {
            if (!(ch is Player player)) return;

            ch.send("The default prompt is: <%1%%h %2%%m %3%%mv %W> \n\r");

            if (!argument.ISEMPTY() && (argument.StringCmp("all") || argument.StringCmp("default")))
            {
                player.Prompt = "<%1%%h %2%%m %3%%mv %W> ";
            }
            else if (!argument.ISEMPTY() && argument.StringCmp("?"))
            {
                DoHelp(ch, "prompt");
            }
            else if (!argument.ISEMPTY())
            {
                player.Prompt = argument + (!argument.EndsWith(" ") ? " " : "");
            }

            ch.send("Your prompt is: " + Extensions.XTermColor.EscapeColor(player.Prompt) + "\n\r");
        }

        public static void DoQuests(Character ch, string argument)
        {
            using (new Character.Page(ch))
            {
                ch.send("Current Quests:\n\r");
                if (ch is Player)
                {
                    var player = (Player)ch;

                    foreach (var quest in player.Quests)
                    {
                        if (quest.Status == Quest.QuestStatus.InProgress && quest.Quest != null && quest.Quest.ShowInQuests)
                        {
                            ch.send("lvl {0,3}-{1,-3} {2} :: {3}\n\r", quest.Quest.StartLevel, quest.Quest.EndLevel, quest.Quest.Display, quest.Quest.Description.WrapText());
                        }
                    }

                    ch.send("Failed Quests:\n\r");
                    foreach (var quest in player.Quests)
                    {
                        if (quest.Status == Quest.QuestStatus.Failed && quest.Quest != null && quest.Quest.ShowInQuests)
                        {
                            ch.send("lvl {0,3}-{1,-3} {2} :: {3}\n\r", quest.Quest.StartLevel, quest.Quest.EndLevel, quest.Quest.Display, quest.Quest.Description.WrapText());
                        }
                    }

                    ch.send("Completed Quests:\n\r");
                    foreach (var quest in player.Quests)
                    {
                        if (quest.Status == Quest.QuestStatus.Complete && quest.Quest != null && quest.Quest.ShowInQuests)
                        {
                            ch.send("lvl {0,3}-{1,-3} {2} :: {3}\n\r", quest.Quest.StartLevel, quest.Quest.EndLevel, quest.Quest.Display, quest.Quest.Description.WrapText());
                        }
                    }
                }
            }
        } // doquests

        public static void DoWimpy(Character ch, string arguments)
        {
            if (!(ch is Player)) return;

            var player = (Player)ch;

            if (arguments.ISEMPTY())
            {
                ch.send("Your wimpy is set to {0} hitpoints.\n\r", player.Wimpy);
            }
            else if (int.TryParse(arguments, out player.Wimpy))
            {
                if (player.Wimpy < 0 || player.Wimpy > ch.MaxHitPoints / 2)
                    ch.send("Wimpy must be between 0 and {0}.\n\r", ch.MaxHitPoints / 2);
                else
                {
                    player.Wimpy = Math.Min(Math.Max(0, player.Wimpy), ch.MaxHitPoints / 2);

                    ch.send("Wimpy set to {0} hitpoints.\n\r", player.Wimpy);
                }
            }
            else
                ch.send("Wimpy must be a number between 0 and {0}.\n\r", ch.MaxHitPoints / 2);
        }

        public static void DoToggle(Character ch, string arguments)
        {
            var flags = new [] {
                new {Flag = ActFlags.AFK, Name = "AFK", Description = "Away from keyboard" },
                new {Flag = ActFlags.AutoAssist, Name = "AutoAssist" ,Description = "Assist group members" },
                new {Flag = ActFlags.AutoSac, Name = "AutoSac" ,Description = "Sacrifice corpses" },
                new {Flag = ActFlags.AutoLoot, Name = "AutoLoot" ,Description = "Loot kills" },
                new {Flag = ActFlags.AutoExit, Name = "AutoExit" ,Description = "Show room exits on look" },
                new {Flag = ActFlags.AutoGold, Name = "AutoGold" ,Description = "Automatically loot gold" },
                new {Flag = ActFlags.AutoSplit, Name = "AutoSplit" ,Description = "Split gold looted amongst the group" },
                new {Flag = ActFlags.Color, Name = "Color" ,Description = "ANSI Color" },
                new {Flag = ActFlags.DamageOnType, Name = "Damage", Description = "Damage type specific messages" },
                new {Flag = ActFlags.Brief, Name = "Brief", Description = "Do not show room descriptions on movement" },
                new {Flag = ActFlags.NoSummon, Name = "NoSummon", Description = "Do not allow players to summon you" },
                new {Flag = ActFlags.NoFollow, Name = "NoFollow", Description = "Do not allow followers" },
                new {Flag = ActFlags.NewbieChannel, Name = "Newbie", Description = "Receive newbie channel messages" },
                new {Flag = ActFlags.WizInvis, Name = "WizInvis", Description = "Invisible to lower level players" },
                new {Flag = ActFlags.HolyLight, Name = "HolyLight", Description = "Immortal vision" }
            };
            if (arguments.ISEMPTY())
            {
                foreach (var flag in flags)
                {
                    if ((flag.Flag == ActFlags.HolyLight || flag.Flag == ActFlags.WizInvis) && !ch.IsImmortal) continue;
                    ch.send("{0,-15}: {1,-10} {2,-20}\\x\n\r", flag.Name, ch.Flags.ISSET(flag.Flag) ? "\\gON\\x" : "\\rOFF\\x", flag.Description);
                }
            }
            else
            {
                foreach (var flag in flags)
                {
                    if ((flag.Flag == ActFlags.HolyLight || flag.Flag == ActFlags.WizInvis) && !ch.IsImmortal) continue;

                    if (flag.Name.StringPrefix(arguments))
                    {
                        if (ch.Flags.ISSET(flag.Flag))
                        {
                            ch.Flags.REMOVEFLAG(flag.Flag);
                        }
                        else
                            ch.Flags.SETBIT(flag.Flag);

                        ch.send("{0,-15}: {1,-10} {2,-20}\\x\n\r", flag.Name, ch.Flags.ISSET(flag.Flag) ? "\\gON\\x" : "\\rOFF\\x", flag.Description);
                        return;
                    }
                }
                ch.send("Flag not found.\n\r");
            }
        }
    } // end class
} // end namespace
