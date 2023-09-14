using CrimsonStainedLands.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace CrimsonStainedLands
{
    public class DoActItem
    {
        public static void DoQuaf(Character character, string arguments)
        {
            string itemName = "";
            ItemData potion;
            int count = 0;
            arguments = arguments.OneArgument(ref itemName);

            if (itemName.ISEMPTY())
            {
                character.send("Quaf what?\n\r");
            }
            else if ((potion = character.GetItemInventory(itemName, ref count)) == null)
            {
                character.send("You don't have that.\n\r");
            }
            else if (!potion.ItemType.ISSET(ItemTypes.Potion))
            {
                character.send("You can only quaf potions.\n\r");
            }
            else if (character.Fighting != null)
            {
                character.send("You are too busy fighting to quaf anything.\n\r");
            }
            else
            {
                character.Act("$n quaffs $p.", null, potion, null, ActType.ToRoom);
                character.Act("You quaff $p.", null, potion, null, ActType.ToChar);
                character.WaitState(Game.PULSE_VIOLENCE);

                foreach (var spell in potion.Spells)
                {
                    Magic.ItemCastSpell(Magic.CastType.Cast, spell.Spell, spell.Level, character, character, null, null);
                }

                character.Inventory.Remove(potion);
                potion.CarriedBy = null;
            }
        } // End DoQuaf

        public static void DoRecite(Character character, string arguments)
        {
            string itemName = "";
            string victimName = "";
            ItemData scroll;
            ItemData targetItem;
            int count = 0;
            Character victim;

            arguments = arguments.OneArgument(ref itemName);
            arguments = arguments.OneArgument(ref victimName);

            if (itemName.ISEMPTY())
            {
                character.send("What scroll would you like to recite?\n\r");
                return;
            }
            else if ((scroll = character.GetItemInventory(itemName, ref count)) == null)
            {
                character.send("You aren't carrying that scroll.\n\r");
                return;
            }
            else if (!scroll.ItemType.ISSET(ItemTypes.Scroll))
            {
                character.send("You can only recite magical scrolls.\n\r");
                return;
            }
            else if (character.Level < scroll.Level)
            {
                character.send("This scroll is too complex for you to comprehend.\n\r");
                return;
            }

            else if ((targetItem = character.GetItemHere(victimName)) != null)
            {
                character.Act("$n recites $p at $P.", null, scroll, targetItem, ActType.ToRoom);
                character.Act("You recite $p at $P.", null, scroll, targetItem, ActType.ToChar);
                character.WaitState(Game.PULSE_VIOLENCE);

                if (Utility.NumberPercent() >= 20 + character.GetSkillPercentage("scrolls") * 4 / 5)
                {
                    character.send("You mispronounce a syllable.\n\r");
                    character.CheckImprove("scrolls", false, 2);

                }
                else
                {
                    foreach (var spell in scroll.Spells)
                    {
                        Magic.ItemCastSpell(Magic.CastType.Cast, spell.Spell, spell.Level, character, null, targetItem, null);
                    }
                    character.CheckImprove("scrolls", true, 2);
                }

                character.Inventory.Remove(scroll);

            }
            else if ((!victimName.ISEMPTY() && (victim = character.GetCharacterFromRoomByName(victimName, ref count)) == null) || (victim = character.Fighting) != null)
            {
                character.Act("$n recites $p at $N.", victim, scroll, null, ActType.ToRoom);
                character.Act("You recite $p at $N.", victim, scroll, null, ActType.ToChar);
                character.WaitState(Game.PULSE_VIOLENCE);

                if (Utility.NumberPercent() >= 20 + character.GetSkillPercentage("scrolls") * 4 / 5)
                {
                    character.send("You mispronounce a syllable.\n\r");
                    character.CheckImprove("scrolls", false, 2);

                }
                else
                {
                    foreach (var spell in scroll.Spells)
                    {
                        Magic.ItemCastSpell(Magic.CastType.Cast, spell.Spell, spell.Level, character, victim, null, null);
                    }
                    character.CheckImprove("scrolls", true, 2);
                }
                character.Inventory.Remove(scroll);
            }
            else if (!victimName.ISEMPTY())
            {
                character.send("You don't see them here.\n\r");
                return;
            }
            else
            {
                character.Act("$n recites $p at $mself.", null, scroll, null, ActType.ToRoom);
                character.Act("You recite $p at yourself.", null, scroll, null, ActType.ToChar);
                character.WaitState(Game.PULSE_VIOLENCE);
                if (Utility.NumberPercent() >= 20 + character.GetSkillPercentage("scrolls") * 4 / 5)
                {
                    character.send("You mispronounce a syllable.\n\r");
                    character.CheckImprove("scrolls", false, 2);

                }
                else
                {
                    foreach (var spell in scroll.Spells)
                    {
                        Magic.ItemCastSpell(Magic.CastType.Cast, spell.Spell, spell.Level, character, character, null, null);
                    }
                    character.CheckImprove("scrolls", true, 2);
                }
                character.Inventory.Remove(scroll);
                scroll.CarriedBy = null;
            }
        } // End DoRecite

        public static void DoZap(Character character, string arguments)
        {
            ItemData wand = null;
            int count = 0;
            string argument = "";
            Character victim = null;
            ItemData zapTarget = null;
            arguments = arguments.OneArgument(ref argument);

            if (argument.ISEMPTY())
            {
                character.send("Who do you want to zap?\n\r");
                return;
            }
            else if (character.Equipment.TryGetValue(WearSlotIDs.Held, out wand) == false)
            {
                character.send("You aren't holding a wand.\n\r");
                return;
            }
            else if (!wand.ItemType.ISSET(ItemTypes.Wand))
            {
                character.send("You can only zap with wands.\n\r");
                return;
            }
            //else if (character.Fighting != null)
            //{
            //    character.send("You are too busy fighting to zap anyone.\n\r");
            //}
            else if ((victim = character.GetCharacterFromRoomByName(argument, ref count)) == null && (zapTarget = character.GetItemInventory(argument, ref count)) == null)
            {
                character.send("You don't see them or that here.\n\r");
                return;
            }
            else if (victim != null)
            {
                if (victim != character)
                {
                    character.Act("$n zaps $N with $p.", victim, wand, null, ActType.ToRoomNotVictim);
                    character.Act("$n zaps you with $p.", victim, wand, null, ActType.ToVictim);
                    character.Act("You zap $N with $p.", victim, wand, null, ActType.ToChar);
                }
                else
                {
                    character.Act("$n zaps $mself with $p.", victim, wand, null, ActType.ToRoomNotVictim);
                    character.Act("You zap yourself with $p.", victim, wand, null, ActType.ToChar);
                }
                character.WaitState(Game.PULSE_VIOLENCE);

                if (character.Level < wand.Level || Utility.NumberPercent() >= 20 + character.GetSkillPercentage("wands") * 4 / 5)
                {
                    character.Act("Your efforts with $p produce only smoke and sparks.", null, wand);
                    character.Act("$n's efforts with $p produce only smoke and sparks.", null, wand, type: ActType.ToRoom);
                    character.CheckImprove("wands", false, 2);

                }
                else
                {
                    foreach (var spell in wand.Spells)
                    {
                        Magic.ItemCastSpell(Magic.CastType.Cast, spell.Spell, spell.Level, character, victim, wand, null);
                    }
                    character.CheckImprove("wands", true, 2);
                }
            }

            else if (zapTarget != null)
            {

                character.Act("$n zaps $P with $p.", victim, wand, zapTarget, ActType.ToRoom);
                character.Act("You zap $P with $p.", victim, wand, zapTarget, ActType.ToChar);
                character.WaitState(Game.PULSE_VIOLENCE);
                if (character.Level < wand.Level || Utility.NumberPercent() >= 20 + character.GetSkillPercentage("wands") * 4 / 5)
                {
                    character.Act("Your efforts with $p produce only smoke and sparks.", null, wand);
                    character.Act("$n's efforts with $p produce only smoke and sparks.", null, wand, type: ActType.ToRoom);
                    character.CheckImprove("wands", false, 2);

                }
                else
                {
                    foreach (var spell in wand.Spells)
                    {
                        Magic.ItemCastSpell(Magic.CastType.Cast, spell.Spell, spell.Level, character, null, zapTarget, null);
                    }
                    character.CheckImprove("wands", true, 2);
                }
            }

            wand.Charges--;
            if (wand.Charges <= 0)
            {
                character.Act("$n's $p explodes into fragments.", victim, wand, null, ActType.ToRoom);
                character.Act("Your $p explodes into fragments.", victim, wand, null, ActType.ToChar);
                character.Equipment[WearSlotIDs.Held].CarriedBy = null;
                character.Equipment.Remove(WearSlotIDs.Held);
            }
        } // End DoZap

        public static void DoBrandish(Character character, string arguments)
        {
            ItemData staff = null;

            if ((staff = character.GetEquipment(WearSlotIDs.Held)) == null)
            {
                character.send("You aren't using a staff.\n\r");
                return;
            }
            else if (!staff.ItemType.ISSET(ItemTypes.Staff))
            {
                character.send("You can only brandish a staff.\n\r");
                return;
            }
            else if (character.Fighting != null)
            {
                character.send("You are too busy fighting to brandish your staff.\n\r");
                return;
            }
            else
            {
                character.Act("$n brandishes $p.", null, staff, null, ActType.ToRoom);
                character.Act("You brandish $p.", null, staff, null, ActType.ToChar);
                character.WaitState(Game.PULSE_VIOLENCE);
                if (Utility.NumberPercent() >= 20 + character.GetSkillPercentage("talismans") * 4 / 5)
                {
                    character.Act("You fail to invoke $p.\n\r", null, staff);
                    character.Act("... and nothing happens.", type: ActType.ToRoom);
                    character.CheckImprove("talismans", false, 2);

                }
                else
                {
                    foreach (var spell in staff.Spells)
                    {
                        Magic.ItemCastSpell(Magic.CastType.Cast, spell.Spell, spell.Level, character, character, staff, null);
                    }
                    character.CheckImprove("talismans", true, 2);
                }
            }


            staff.Charges--;
            if (staff.Charges <= 0)
            {
                character.Act("$n's $p disingtates in their hands.", null, staff, null, ActType.ToRoom);
                character.Act("Your $p disinitgrates in your hands.", null, staff, null, ActType.ToChar);
                character.Equipment[WearSlotIDs.Held].CarriedBy = null;
                character.Equipment.Remove(WearSlotIDs.Held);
            }
        } // End DoBrandish


        public static void DoPut(Character ch, string arguments)
        {
            string itemName = "";
            string containerName = "";
            arguments = arguments.OneArgument(ref itemName);
            arguments = arguments.OneArgument(ref containerName);
            int count = 0;
            ItemData item = null;
            if (ch.Form != null)
            {
                ch.send("You aren't carrying anything.\n\r");
                return;
            }
            if (!string.IsNullOrEmpty(containerName))
            {
                var container = ch.GetItemHere(containerName, ref count);
                count = 0;
                //var item = ch.GetItemInventory(itemName, ref count);

                if (container != null && container.ItemType.Contains(ItemTypes.Container))
                {

                    if ("all".StringCmp(itemName))
                    {
                        foreach (var containeditem in ch.Inventory.ToArray())
                            if (containeditem != container)
                                if (!Character.PutItem(ch, containeditem, container))
                                    return;
                    }
                    else if (itemName.Length > "all.".Length && itemName.StartsWith("all.", StringComparison.InvariantCultureIgnoreCase))
                    {
                        itemName = itemName.Substring("all.".Length);
                        foreach (var containeditem in ch.Inventory.ToArray())
                            if (containeditem != container)
                                if (containeditem.Name.IsName(itemName))
                                    if (!Character.PutItem(ch, containeditem, container))
                                        return;
                    }
                    else if ((item = ch.GetItemInventory(itemName, ref count)) != null)
                    {
                        if (item == container)
                        {
                            ch.Act("You can't put $p inside of itself.", null, container, null, ActType.ToChar);
                            return;
                        }

                        Character.PutItem(ch, item, container);
                    }
                    else
                        ch.send("You don't have that.\n\r");


                }
                else if (item != null && container != null)
                {
                    ch.send("That isn't a container.\n\r");
                }
                else if (item == null)
                {
                    ch.send("You don't have {0}.\n\r", itemName);
                }
                else
                    ch.send("You don't see that here.\n\r");
            }
            else ch.send("Put what in what?\n\r");
        }

        public static void DoWear(Character ch, string arguments)
        {
            int count = 0;
            if (ch.Form != null)
            {
                ch.send("You can't wear anything.\n\r");
                return;
            }
            if (arguments.ISEMPTY())
            {
                ch.send("Wear what?\n\r");
                return;
            }
            else if (arguments.StringCmp("all"))
            {
                bool woreanything = false;
                foreach (var allitem in ch.Inventory.ToArray())
                    if (ch.wearItem(allitem, true, false))
                        woreanything = true;

                if (!woreanything)
                    ch.send("You wore nothing.\n\r");
                return;
            }

            var item = ch.GetItemInventory(arguments, ref count);

            if (item != null)
            {
                ch.wearItem(item);
            }
            else
                ch.send("You aren't carrying that.\n\r");
        }

        public static void DoWield(Character ch, string arguments)
        {
            int count = 0;
            var item = ch.GetItemInventory(arguments, ref count);

            if (item != null && item.ItemType.ISSET(ItemTypes.Weapon))
            {
                ch.wearItem(item);
            }
            else if (item != null)
                ch.send("You can't wield that.\n\r");
            else
                ch.send("You aren't carrying that.\n\r");
        }

        public static void DoRemove(Character ch, string arguments)
        {
            int count = 0;
            ItemData item;
            if (ch.Form != null)
            {
                ch.send("You aren't wearing anything.\n\r");
                return;
            }
            if (arguments.equals("all"))
            {
                foreach (var allitem in ch.Equipment.Values.ToArray())
                {
                    if (!allitem.extraFlags.Contains(ExtraFlags.NoRemove) || allitem.IsAffected(AffectFlags.Greased))
                    {
                        ch.RemoveEquipment(allitem, true);
                    }
                }
            }
            else if ((item = ch.GetItemEquipment(arguments, out WearSlot slot, ref count)) != null &&
                (!item.extraFlags.Contains(ExtraFlags.NoRemove) || item.IsAffected(AffectFlags.Greased)))
            {
                ch.RemoveEquipment(item, true);
            }
            else if (item != null)
                ch.send("You can't remove that.\n\r");
            else
                ch.send("You aren't wearing that.\n\r");

        }

        public static void DoDrop(Character ch, string arguments)
        {
            int count = 0;
            string drop = "";

            arguments.OneArgument(ref drop);

            if (arguments.ISEMPTY())
            {
                ch.send("Drop what?\n\r");
                return;
            }

            if (ch.Form != null)
            {
                ch.send("You aren't carrying anything.\n\r");
                return;
            }

            if (int.TryParse(drop, out var amount))
            {
                arguments = arguments.OneArgument(ref drop);

                if (amount > 0)
                {
                    if (arguments.StringCmp("gold"))
                    {
                        if (amount > ch.Gold)
                        {
                            ch.send("You don't have that much gold.\n\r");
                            return;
                        }
                        else if (amount < 1)
                        {
                            ch.send("You can only drop a positive amount of gold.\n\r");
                            return;
                        }
                        var money = Character.CreateMoneyItem(0, amount);
                        ch.Room.items.Add(money);
                        money.Room = ch.Room;

                        ch.Gold -= amount;

                        ch.Act("You drops $p.", null, money, null, ActType.ToChar, amount);
                        ch.Act("$n drops $p.", null, money, null, ActType.ToRoom);
                        return;
                    }
                    else if (arguments.StringCmp("silver"))
                    {
                        if (amount > ch.Silver)
                        {
                            ch.send("You don't have that much silver.\n\r");
                            return;
                        }
                        else if (amount < 1)
                        {
                            ch.send("You can only drop a positive amount of silver.\n\r");
                            return;
                        }
                        var money = Character.CreateMoneyItem(amount, 0);
                        ch.Room.items.Add(money);
                        money.Room = ch.Room;

                        ch.Silver -= amount;

                        ch.Act("You drops $p.", null, money, null, ActType.ToChar, amount);
                        ch.Act("$n drops $p.", null, money, null, ActType.ToRoom);
                        return;
                    }
                }
                else
                {
                    ch.send("You can't drop a negative amount of coins.\n\r");
                    return;
                }
            }

            ItemData item;

            if (arguments.equals("all"))
            {
                foreach (var allitem in new List<ItemData>(ch.Inventory))
                {
                    if (!allitem.extraFlags.Contains(ExtraFlags.NoDrop) || allitem.IsAffected(AffectFlags.Greased))
                    {
                        ch.Inventory.Remove(allitem);
                        ch.Room.items.Insert(0, allitem);
                        allitem.CarriedBy = null;
                        allitem.Room = ch.Room;
                        ch.send("You drop " + (!allitem.ShortDescription.ISEMPTY() ? allitem.ShortDescription : allitem.Name) + ".\n\r");
                        ch.Act("$n drops $p.\n\r", item: allitem, type: ActType.ToRoom);

                        if (allitem.extraFlags.ISSET(ExtraFlags.MeltDrop))
                        {
                            ch.Act("$p crumbles into dust.", item: allitem, type: ActType.ToRoom);
                            ch.Act("$p crumbles into dust.", item: allitem, type: ActType.ToChar);
                            allitem.Dispose();
                        }
                    }
                }
            }
            else if (!arguments.ISEMPTY() && arguments.StartsWith("all.") && arguments.Length > 4)
            {
                arguments = arguments.Substring(4);
                foreach (var allitem in new List<ItemData>(ch.Inventory))
                {
                    if (allitem.Name.IsName(arguments))
                    {
                        ch.Inventory.Remove(allitem);
                        ch.Room.items.Insert(0, allitem);
                        allitem.CarriedBy = null;
                        allitem.Room = ch.Room;
                        ch.send("You drop " + (!allitem.ShortDescription.ISEMPTY() ? allitem.ShortDescription : allitem.Name) + ".\n\r");
                        ch.Act("$n drops $p.\n\r", item: allitem, type: ActType.ToRoom);

                        if (allitem.extraFlags.ISSET(ExtraFlags.MeltDrop))
                        {
                            ch.Act("$p crumbles into dust.", item: allitem, type: ActType.ToRoom);
                            ch.Act("$p crumbles into dust.", item: allitem, type: ActType.ToChar);
                            allitem.Dispose();
                        }
                    }
                }
            }
            else if ((item = ch.GetItemInventory(arguments, ref count)) != null && (!item.extraFlags.Contains(ExtraFlags.NoDrop) || item.IsAffected(AffectFlags.Greased)))
            {
                ch.Inventory.Remove(item);
                ch.Room.items.Insert(0, item);
                item.CarriedBy = null;
                item.Room = ch.Room;
                ch.send("You drop " + (!item.ShortDescription.ISEMPTY() ? item.ShortDescription : item.Name) + ".\n\r");
                ch.Act("$n drops $p.\n\r", item: item, type: ActType.ToRoom);

                if (item.extraFlags.ISSET(ExtraFlags.MeltDrop))
                {
                    ch.Act("$p crumbles into dust.", item: item, type: ActType.ToRoom);
                    ch.Act("$p crumbles into dust.", item: item, type: ActType.ToChar);
                    item.Dispose();
                }
            }
            else if (item != null)
                ch.send("You can't drop that.\n\r");
            else
                ch.send("You aren't holding that.\n\r");
        }

        public static void DoGive(Character ch, string arguments)
        {
            Character other;
            string itemname = "";
            int count = 0;
            if (ch.Form != null)
            {
                ch.send("You aren't holding anything.\n\r");
                return;
            }
            arguments = arguments.OneArgument(ref itemname);
            if (int.TryParse(itemname, out var amount))
            {
                arguments = arguments.OneArgument(ref itemname);
                other = ch.GetCharacterFromRoomByName(arguments, ref count);

                if (other == ch)
                {
                    ch.send("You can't give yourself anything.\n\r");
                    return;
                }
                else if (other == null)
                {
                    ch.send("You don't see them here.\n\r");
                    return;
                }

                if (itemname.StringCmp("gold"))
                {
                    if (amount > ch.Gold)
                    {
                        ch.send("You don't have that much gold.\n\r");
                        return;
                    }
                    else if (amount < 1)
                    {
                        ch.send("You can only give a positive amount of gold.\n\r");
                        return;
                    }

                    ch.Gold -= amount;
                    other.Gold += amount;
                    ch.Act("You give $N {0} gold.", other, null, null, ActType.ToChar, amount);
                    ch.Act("$n gives you {0} gold.", other, null, null, ActType.ToVictim, amount);
                    ch.Act("$n gives $N some gold.", other, null, null, ActType.ToRoomNotVictim);
                    Programs.ExecutePrograms(Programs.ProgramTypes.Give, ch, other, null, null, amount + " gold");
                    return;
                }
                else if (itemname.StringCmp("silver"))
                {
                    if (amount > ch.Silver)
                    {
                        ch.send("You don't have that much silver.\n\r");
                        return;
                    }
                    else if (amount < 1)
                    {
                        ch.send("You can only give a positive amount of silver.\n\r");
                        return;
                    }

                    ch.Silver -= amount;
                    other.Silver += amount;
                    ch.Act("You give $N {0} silver.", other, null, null, ActType.ToChar, amount);
                    ch.Act("$n gives you {0} silver.", other, null, null, ActType.ToVictim, amount);
                    ch.Act("$n gives $N some silver.", other, null, null, ActType.ToRoomNotVictim);
                    Programs.ExecutePrograms(Programs.ProgramTypes.Give, ch, other, null, null, amount + " silver");
                    return;
                }
                else
                {
                    ch.send("Give gold or silver?\n\r");
                    return;
                }

            }
            var item = ch.GetItemInventory(itemname, ref count);
            count = 0;
            other = ch.GetCharacterFromRoomByName(arguments, ref count);
            if(item == null)
            {
                ch.send("You couldn't find it.\n\r");
            }
            else if (other == ch)
            {
                ch.send("You can't give yourself anything.\n\r");
            }
            else if (other == null)
            {
                ch.send("You don't see them here.\n\r");
            }
            else if (other != null && !other.CanSee(item))
            {
                ch.Act("You wave your hands at $N but they can't see $p.", other, item);
            }
            else if (other != null && item != null &&
                (!item.extraFlags.Contains(ExtraFlags.NoDrop) || item.IsAffected(AffectFlags.Greased)))
            {
                if (other.Carry + 1 > other.MaxCarry)
                {
                    ch.send("They can't carry anymore items.");
                    ch.Act("$n tries to give you $p, but you are carrying too many things.", other, item, type: ActType.ToVictim);
                    return;
                }
                if (other.TotalWeight + item.Weight > other.MaxWeight)
                {
                    ch.send("You can't carry anymore weight.\n\r");
                    ch.Act("$n tries to give you $p, but you are carrying too much weight.", other, item, type: ActType.ToVictim);
                    return;
                }


                ch.Inventory.Remove(item);
                other.Inventory.Insert(0, item);
                item.CarriedBy = other;
                ch.send("You give " + (!item.ShortDescription.ISEMPTY() ? item.ShortDescription : item.Name) + " to " + other.Display(ch) + ".\n\r");
                other.Act("$N gives you $p.\n\r", ch, item, null, ActType.ToChar);
                ch.Act("$n gives $p to $N.\n\r", other, item, type: ActType.ToRoomNotVictim);
                Programs.ExecutePrograms(Programs.ProgramTypes.Give, ch, other, item, null, "");

                if (other.IsNPC)
                {
                    if (other is NPCData)
                    {
                        Programs.ExecutePrograms(Programs.ProgramTypes.Receive, ch, other, item, null, "");
                    }

                    if (other.IsAwake)
                    {
                        other.wearItem(item, true, false);

                        if (other.Inventory.Contains(item) && other.CanSee(ch))
                        {

                            other.Inventory.Remove(item);
                            ch.Inventory.Insert(0, item);
                            item.CarriedBy = ch;

                            other.Act("You give $p to $N.\n\r", ch, item, type: ActType.ToChar);
                            ch.Act("$N gives you $p.\n\r", other, item, null, ActType.ToChar);
                            other.Act("$n gives $p to $N.\n\r", ch, item, type: ActType.ToRoomNotVictim);


                        }
                        else if (other.Inventory.Contains(item))
                        {
                            other.Inventory.Remove(item);
                            item.CarriedBy = null;
                            other.Room.items.Insert(0, item);
                            item.Room = other.Room;

                            other.Act("You drop $p.\n\r", ch, item, type: ActType.ToChar);
                            ch.Act("$N drops $p.\n\r", other, item, null, ActType.ToChar);
                            other.Act("$n drops $p.\n\r", ch, item, type: ActType.ToRoomNotVictim);
                        }
                        return;
                    }
                }

            }
            else if (other != null && item != null)
                ch.send("You can't let go of it!\n\r");
            else if (item == null)
                ch.send("You don't have that.\n\r");
            else
                ch.send("You don't see them here.\n\r");
        }

        public static void DoEquipment(Character ch, string arguments)
        {

            var wearing = ch.GetEquipmentString(ch);
            ch.send(wearing);
        }



        /// <summary>
        /// The code handles the logic for the "get" command, which allows the character to pick up items from the environment or containers.
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="arguments"></param>
        public static void DoGet(Character ch, string arguments)
        {
            string itemName = "";
            string containerName = "";

            // Extract the item name and container name from the arguments
            arguments = arguments.OneArgument(ref itemName);
            arguments = arguments.OneArgument(ref containerName);

            var fAll = itemName.equals("all");

            // Check if the character is in a form that doesn't have hands
            if (ch.Form != null && !ch.Form.Parts.ISSET(PartFlags.Hands))
            {
                ch.send("You can't pick anything up.\n\r");
                return;
            }

            // Handle picking up items from the room or a container
            if (fAll && string.IsNullOrEmpty(containerName))
            {
                // Pick up all items in the room
                foreach (var item in new List<ItemData>(ch.Room.items))
                {
                    ch.GetItem(item);
                }
            }
            else if (!string.IsNullOrEmpty(containerName))
            {
                int count = 0;
                ItemData container = null;

                // Get the specified container item in the character's inventory
                if ((container = ch.GetItemHere(containerName)) != null)
                {
                    // Check if the container is empty
                    if (container.Contains.Count == 0)
                    {
                        ch.Act("$p is empty.", null, container);
                        return;
                    }

                    // Check if the container is closed
                    if (container.extraFlags.Contains(ExtraFlags.Closed))
                    {
                        ch.Act("$p is closed.\n\r", null, container, null, ActType.ToChar);
                        return;
                    }

                    if (container.ItemType.ISSET(ItemTypes.PC_Corpse) && container.Owner != ch.Name)
                    {
                        ch.Act("The gods wouldn't approve of that.");
                        return;
                    }

                    if (fAll)
                    {
                        // Pick up all items from the container
                        foreach (var item in container.Contains.ToArray())
                        {
                            if (item != null && item.wearFlags.Contains(WearFlags.Take))
                            {
                                if (!ch.GetItem(item, container))
                                    return;
                            }
                            else if (item != null)
                            {
                                ch.send("You can't pick that up.\n\r");
                            }
                        }
                    }
                    else if (itemName.StartsWith("all.") && itemName.Length > 4)
                    {
                        // Pick up specific items matching a name pattern from the container
                        itemName = itemName.Substring(4);
                        foreach (var item in container.Contains.ToArray())
                        {
                            if (item != null && item.wearFlags.Contains(WearFlags.Take) && item.Name.IsName(itemName))
                            {
                                if (!ch.GetItem(item, container))
                                    return;
                            }
                            else if (item != null && item.wearFlags.Contains(WearFlags.Take))
                            {
                                continue;
                            }
                            else if (item != null)
                            {
                                ch.send("You can't pick that up.\n\r");
                            }
                        }
                    }
                    else
                    {
                        // Pick up a specific item from the container
                        count = 0;
                        var item = ch.GetItemList(itemName, container.Contains, ref count);
                        if (item != null && item.wearFlags.Contains(WearFlags.Take))
                        {
                            ch.GetItem(item, container);
                            return;
                        }
                        else if (item != null)
                        {
                            ch.send("You can't pick that up.\n\r");
                        }
                        else
                        {
                            ch.send("You don't see that.\n\r");
                        }
                    }
                }
                else
                {
                    ch.send("You don't see that container here.\n\r");
                }
            }
            else if (string.IsNullOrEmpty(containerName))
            {
                if (ch.Room.items.Count == 0)
                {
                    ch.Act("You don't see anything here.");
                    return;
                }

                if (fAll)
                {
                    // Pick up all items in the room
                    foreach (var item in ch.Room.items.ToArray())
                    {
                        if (item != null && item.wearFlags.Contains(WearFlags.Take))
                        {
                            if (!ch.GetItem(item))
                                return;
                        }
                        else if (item != null)
                        {
                            ch.send("You can't pick that up.\n\r");
                        }
                        else
                        {
                            ch.send("You don't see that here.\n\r");
                        }
                    }
                }
                else if (itemName.StartsWith("all.") && itemName.Length > 4)
                {
                    // Pick up specific items matching a name pattern from the room
                    itemName = itemName.Substring(4);
                    foreach (var item in ch.Room.items.ToArray())
                    {
                        if (item != null && item.wearFlags.Contains(WearFlags.Take) && item.Name.IsName(itemName))
                        {
                            if (!ch.GetItem(item))
                                return;
                        }
                        else if (item != null && item.wearFlags.Contains(WearFlags.Take))
                        {
                            continue;
                        }
                        else if (item != null)
                        {
                            ch.send("You can't pick that up.\n\r");
                        }
                    }
                }
                else
                {
                    // Pick up a specific item from the room
                    int count = 0;
                    var item = ch.GetItemRoom(itemName, ref count);

                    if (item != null && item.wearFlags.Contains(WearFlags.Take))
                    {
                        ch.GetItem(item);
                        return;
                    }
                    else if (item != null)
                    {
                        ch.send("You can't pick that up.\n\r");
                    }
                    else
                    {
                        ch.send("You don't see that here.\n\r");
                    }
                }
            }
            else
            {
                // Pick up an item from a container in the character's inventory
                int count = 0;
                var item = ch.GetItemInventory(containerName, ref count);

                if (item != null)
                {
                    foreach (var containedItem in item.Contains)
                    {
                        if (fAll || containedItem.Name.IsName(itemName))
                        {
                            ch.GetItem(containedItem, item);
                            return;
                        }
                    }
                }

                ch.send("You don't see that here.\n\r");
            }
        }

        /// <summary>
        /// Overall, this method retrieves the items carried by the player character, counts their quantities, and displays them in a formatted message.
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="arguments"></param>
        public static void DoInventory(Character ch, string arguments)
        {
            StringBuilder carrying = new StringBuilder();

            var tempItemList = new Dictionary<string, int>();

            carrying.AppendLine("You are carrying:");

            // Check if the character is not in a form and has items in their inventory
            if (ch.Form == null && ch.Inventory.Any())
            {
                // Iterate through each item in the character's inventory
                foreach (var item in ch.Inventory)
                {
                    // Skip items that the character cannot see
                    if (!ch.CanSee(item)) continue;

                    var itemshow = item.DisplayFlags(ch) + item.Display(ch);

                    // Count the quantity of each item
                    if (tempItemList.ContainsKey(itemshow))
                        tempItemList[itemshow] = tempItemList[itemshow] + 1;
                    else
                        tempItemList[itemshow] = 1;
                }

                // Display the carried items and their quantities
                foreach (var itemkvp in tempItemList)
                {
                    carrying.AppendLine("    " + (itemkvp.Value > 1 ? "[" + itemkvp.Value + "] " : "") + itemkvp.Key);
                }
            }
            else
            {
                carrying.AppendLine("    Nothing.");
            }

            using (new Character.Page(ch))
                ch.SendToChar(carrying.ToString());
        }

        public static void DoList(Character ch, string arguments)
        {
            NPCData shopKeeper = null;

            if (ch.Form != null)
            {
                ch.send("They wouldn't be able to understand you.\n\r");
                return;
            }

            foreach (var npc in ch.Room.Characters)
            {
                if (npc.IsNPC && npc.Flags.ISSET(ActFlags.Shopkeeper))
                {
                    shopKeeper = (NPCData)npc;

                    break;
                }
            }

            if (shopKeeper != null)
            {
                using (new Character.Page(ch))
                {
                    ch.send("This shop will purchase the following types of things: ");
                    ch.send("{0}\n\r", string.Join(", ", from type in shopKeeper.BuyTypes select type.ToString().ToLower()));
                    ch.Act("$N will sell you the following goods:", shopKeeper, type: ActType.ToChar);
                    foreach (var item in shopKeeper.Inventory)
                    {
                        ch.send("[{0,3}] {1,7} - {2}\n\r", item.Level, item.Value * shopKeeper.SellProfitPercent / 100, item.DisplayFlags(ch) + item.Display(ch));
                    }
                    if (shopKeeper.PetVNums.Any())
                    {
                        ch.Act("$N will sell you the following pets:", shopKeeper, type: ActType.ToChar);
                        foreach (var vnum in shopKeeper.PetVNums)
                        {
                            if (NPCTemplateData.Templates.TryGetValue(vnum, out var pettemplate))
                            {
                                ch.send("[{0,2}] {1,8} - {2}\n\r", pettemplate.Level, 10 * pettemplate.Level * pettemplate.Level, pettemplate.ShortDescription);
                            }
                        }
                    }

                }
            }
            else
                ch.send("You see no shopkeepers here.\n\r");
        }

        /// <summary>
        /// The method handles the repair functionality between the player character and an NPC shopkeeper.
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="arguments"></param>
        public static void DoRepair(Character ch, string arguments)
        {
            NPCData shopKeeper = null;

            // Check if the player character is in a form
            if (ch.Form != null)
            {
                ch.send("They wouldn't be able to understand you.\n\r");
                return;
            }

            // Search for a shopkeeper NPC character in the same room as the player character
            foreach (var npc in ch.Room.Characters)
            {
                if (npc.IsNPC && npc.Flags.ISSET(ActFlags.Shopkeeper))
                {
                    shopKeeper = (NPCData)npc;
                    break;
                }
            }

            // If a shopkeeper is found, proceed with repairs
            if (shopKeeper != null)
            {
                // Display repairable item types and damaged items the player character is wearing or carrying
                if (arguments.ISEMPTY())
                {
                    using (new Character.Page(ch))
                    {
                        ch.send("This shop will repair the following types of things: ");
                        ch.send("{0}\n\r", string.Join(", ", from type in shopKeeper.BuyTypes select type.ToString().ToLower()));
                        ch.Act("$N will repair for you the following goods:", shopKeeper, type: ActType.ToChar);

                        // Get the damaged items from the player character's equipment and inventory
                        var items = from item in ch.Equipment.Values.Concat(ch.Inventory)
                                    where item.Durability < item.MaxDurability && item.ItemType.Any(itemtype => shopKeeper.BuyTypes.Contains(itemtype))
                                    select item;

                        if (!items.Any())
                        {
                            ch.send("You aren't wearing or carrying any damaged items.\n\r");
                            return;
                        }

                        // Display the damaged items and their repair costs
                        foreach (var item in items)
                        {
                            if (item.Durability < item.MaxDurability)
                            {
                                var value = item.Value * shopKeeper.SellProfitPercent / 100 * (item.Durability / item.MaxDurability);
                                ch.send("[{0,3}] {1,7} - {2}\n\r", item.Level, value, item.DisplayFlags(ch) + item.Display(ch));
                            }
                        }
                    }
                }
                else // Repair a specific item
                {
                    int count = 0;

                    // Get the damaged items from the player character's equipment and inventory
                    var items = from i in ch.Equipment.Values.Concat(ch.Inventory)
                                where i.Durability < i.MaxDurability && i.ItemType.Any(itemtype => shopKeeper.BuyTypes.Contains(itemtype))
                                select i;

                    // Retrieve the specific item to repair
                    var item = ch.GetItemList(arguments, items.ToList(), ref count);

                    if (item == null)
                    {
                        ch.send("You don't have that item.\n\r");
                        return;
                    }

                    // Calculate the repair cost
                    var value = item.Value * shopKeeper.SellProfitPercent / 100 * (item.Durability / item.MaxDurability);
                    var gold = value / 1000;
                    var silver = value % 1000;

                    // Check if the player character has enough coins to cover the repair cost
                    if (ch.Silver + (ch.Gold * 1000) < value)
                    {
                        ch.send("You don't have enough coins.\n\r");
                        return;
                    }
                    else
                    {
                        // Deduct the repair cost from the player character's gold and silver
                        ch.Silver -= silver;
                        ch.Gold -= gold;

                        // Repair the item by setting its durability to the maximum
                        item.Durability = item.MaxDurability;

                        // Send messages to indicate the successful repair
                        ch.Act("$N repairs $p.", shopKeeper, item, type: ActType.ToChar);
                        ch.Act("$N repairs $n's $p.", shopKeeper, item, type: ActType.ToRoomNotVictim);
                        ch.Act("You repair $n's $p.", shopKeeper, item, type: ActType.ToVictim);
                    }
                }
            }
            else
            {
                ch.send("You see no shopkeepers here.\n\r");
            }
        }

        public static void DoValue(Character ch, string arguments)
        {
            if (ch.Form != null)
            {
                ch.send("They wouldn't be able to understand you.\n\r");
                return;
            }

            if (arguments.ISEMPTY())
            {
                ch.send("Sell what?\n\r");
                return;
            }

            NPCData shopKeeper = null;
            foreach (var npc in ch.Room.Characters)
            {
                if (npc.IsNPC && npc.Flags.ISSET(ActFlags.Shopkeeper))
                {
                    shopKeeper = (NPCData)npc;

                    break;
                }
            }
            if (shopKeeper == null)
            {
                ch.send("There is no shopkeeper here.\n\r");
                return;
            }
            int count = 0;
            var item = ch.GetItemInventory(arguments, ref count);

            if (item == null)
            {
                ch.send("You don't seem to have that.\n\r");
            }
            else if (!item.ItemType.Any(itemtype => shopKeeper.BuyTypes.Contains(itemtype)))
            {
                ch.send("This shopkeeper doesn't buy that kind of item.\n\r");
            }
            else
            {
                var amount = item.Value * shopKeeper.SellProfitPercent / 100;


                ch.Act("$N says '\\yI'd say $p is worth about {0} coins.\\x'.", shopKeeper, item, null, ActType.ToChar, amount);
            }

        }
        public static void DoSell(Character ch, string arguments)
        {
            if (ch.Form != null)
            {
                ch.send("They wouldn't be able to understand you.\n\r");
                return;
            }

            if (arguments.ISEMPTY())
            {
                ch.send("Sell what?\n\r");
                return;
            }

            NPCData shopKeeper = null;
            foreach (var npc in ch.Room.Characters)
            {
                if (npc.IsNPC && npc.Flags.ISSET(ActFlags.Shopkeeper))
                {
                    shopKeeper = (NPCData)npc;

                    break;
                }
            }
            if (shopKeeper == null)
            {
                ch.send("There is no shopkeeper here.\n\r");
                return;
            }
            int count = 0;
            var item = ch.GetItemInventory(arguments, ref count);

            if (item == null)
            {
                ch.send("You don't seem to have that.\n\r");
            }
            else if (!item.ItemType.Any(itemtype => shopKeeper.BuyTypes.Contains(itemtype)))
            {
                ch.send("This shopkeeper doesn't buy that kind of item.\n\r");
            }
            else if (shopKeeper.Inventory.Any(invitem => invitem.Template == item.Template))
            {
                ch.Act("$N says '\\yI already have one of those I can't sell!\\x'.", shopKeeper, null, null, ActType.ToChar);
            }
            else
            {
                ch.Inventory.Remove(item);
                item.extraFlags.ADDFLAG(ExtraFlags.Inventory);
                shopKeeper.Inventory.Insert(0, item);
                item.CarriedBy = shopKeeper;
                var amount = item.Value * shopKeeper.SellProfitPercent / 100;
                var gold = amount / 1000;
                var silver = amount % 1000;

                if (gold > 0 && silver > 0)
                {
                    ch.Act("$N hands you {0} gold and {1} silver coins.", shopKeeper, null, null, ActType.ToChar, gold, silver);
                }
                else if (gold > 1)
                    ch.Act("$N hands you {0} gold coins.", shopKeeper, null, null, ActType.ToChar, gold);
                else if (gold > 0)
                    ch.Act("$N hands {0} gold coins to you.", shopKeeper, null, null, ActType.ToChar, gold);
                else if (silver > 1)
                    ch.Act("$N hands {0} silver coins to you.", shopKeeper, null, null, ActType.ToChar, silver);
                else
                    ch.Act("$N hands {0} silver coin to you.", shopKeeper, null, null, ActType.ToChar, silver);

                ch.Act("$n hands $p to $N.", shopKeeper, item, null, ActType.ToRoom);
                ch.Act("$N hands some coins to $n.", shopKeeper, null, null, ActType.ToRoom);
            }

        }
        public static void DoBuy(Character ch, string arguments)
        {
            if (ch.Form != null)
            {
                ch.send("They wouldn't be able to understand you.\n\r");
                return;
            }

            if (arguments.ISEMPTY())
            {
                ch.send("Buy what?\n\r");
            }
            else
            {
                NPCData shopKeeper = null;
                foreach (var npc in ch.Room.Characters)
                {
                    if (npc.IsNPC && npc.Flags.ISSET(ActFlags.Shopkeeper))
                    {
                        shopKeeper = (NPCData)npc;

                        break;
                    }
                }

                if (shopKeeper != null)
                {
                    int count = 0;
                    ItemData item = null;
                    int numberof;
                    string buywhat = "";
                    arguments = arguments.OneArgument(ref buywhat);

                    if (arguments.ISEMPTY() || !int.TryParse(arguments, out numberof))
                        numberof = 1;

                    if (shopKeeper.PetVNums.Any())
                    {
                        foreach (var vnum in shopKeeper.PetVNums)
                        {
                            if (NPCTemplateData.Templates.TryGetValue(vnum, out var pettemplate) && pettemplate.Name.IsName(buywhat))
                            {
                                if (ch.Pet != null)
                                {
                                    ch.send("You already have a pet.\n\r");
                                    return;
                                }

                                if (pettemplate.Level > ch.Level)
                                {
                                    ch.send("Wait till you get a bit older first.\n\r");
                                    return;
                                }
                                long cost = 10 * pettemplate.Level * pettemplate.Level;

                                if ((ch.Gold * 1000) + ch.Silver > cost)
                                {
                                    // check can carry weight/# of items, check that shop has # of items
                                    long silver = 0, gold = 0;

                                    silver = Math.Min(ch.Silver, cost);

                                    if (silver < cost)
                                    {
                                        gold = ((cost - silver + 999) / 1000);
                                        silver = cost - 1000 * gold;
                                    }

                                    ch.Gold -= gold;
                                    ch.Silver -= silver;
                                    if (gold > 0 && silver > 0)
                                    {
                                        ch.Act("You hand {0} gold and {1} silver coins to $N.", shopKeeper, null, null, ActType.ToChar, gold, silver);
                                    }
                                    else if (gold > 1)
                                        ch.Act("You hand {0} gold coins to $N.", shopKeeper, null, null, ActType.ToChar, gold);
                                    else if (gold > 0)
                                        ch.Act("You hand {0} gold coins to $N.", shopKeeper, null, null, ActType.ToChar, gold);
                                    else if (silver > 1)
                                        ch.Act("You hand {0} silver coins to $N.", shopKeeper, null, null, ActType.ToChar, silver);
                                    else
                                        ch.Act("You hand {0} silver coin to $N.", shopKeeper, null, null, ActType.ToChar, silver);

                                    if (silver < 0)
                                    {
                                        ch.Act("$N hands you {0} silver back.", shopKeeper, null, null, ActType.ToChar, -silver);
                                    }

                                    ch.Act("$n hands some coins to $N.", shopKeeper, type: ActType.ToRoom);

                                    var pet = new NPCData(pettemplate, ch.Room);
                                    pet.Flags.SETBIT(ActFlags.AutoAssist);
                                    pet.Following = ch;
                                    pet.Leader = ch;
                                    ch.Pet = pet;
                                    ch.Group.Add(pet);
                                }
                                else
                                    ch.Act("$N says '\\yYou don't have enough coin for that.\\x'\n\r", shopKeeper);

                                return;
                            }
                        }
                    }

                    item = ch.GetItemList(buywhat, shopKeeper.Inventory, ref count);

                    if (item != null && ch.Level < item.Level)
                    {
                        ch.send("Wait till you're a bit older to be able to buy that.\n\r");
                        return;
                    }
                    else if (item != null)
                    {
                        long cost = (item.Value * shopKeeper.SellProfitPercent / 100) * numberof;

                        if (item.extraFlags.ISSET(ExtraFlags.Inventory) && numberof > 1)
                        {
                            ch.send("You can only buy one of those.\n\r");
                        }
                        else
                        {
                            if ((ch.Gold * 1000) + ch.Silver > cost)
                            {
                                // check can carry weight/# of items, check that shop has # of items
                                long silver = 0, gold = 0;

                                silver = Math.Min(ch.Silver, cost);

                                if (silver < cost)
                                {
                                    gold = ((cost - silver + 999) / 1000);
                                    silver = cost - 1000 * gold;
                                }

                                ch.Gold -= gold;
                                ch.Silver -= silver;
                                if (gold > 0 && silver > 0)
                                {
                                    ch.Act("You hand {0} gold and {1} silver coins to $N.", shopKeeper, null, null, ActType.ToChar, gold, silver);
                                }
                                else if (gold > 1)
                                    ch.Act("You hand {0} gold coins to $N.", shopKeeper, null, null, ActType.ToChar, gold);
                                else if (gold > 0)
                                    ch.Act("You hand {0} gold coins to $N.", shopKeeper, null, null, ActType.ToChar, gold);
                                else if (silver > 1)
                                    ch.Act("You hand {0} silver coins to $N.", shopKeeper, null, null, ActType.ToChar, silver);
                                else
                                    ch.Act("You hand {0} silver coin to $N.", shopKeeper, null, null, ActType.ToChar, silver);

                                if (silver < 0)
                                {
                                    ch.Act("$N hands you {0} silver back.", shopKeeper, null, null, ActType.ToChar, -silver);
                                }

                                ch.Act("$n hands some coins to $N.", shopKeeper, type: ActType.ToRoom);

                                if (item.extraFlags.ISSET(ExtraFlags.Inventory))
                                {
                                    shopKeeper.Inventory.Remove(item);
                                    ch.Inventory.Insert(0, item);
                                    item.CarriedBy = ch;
                                    ch.Act("$N hands $p to $n.", shopKeeper, item, type: ActType.ToRoom);
                                    ch.Act("$N hands $p to you.", shopKeeper, item, type: ActType.ToChar);

                                }
                                else
                                {
                                    for (int i = 0; i < numberof; i++)
                                    {
                                        item = new ItemData(item.Template);
                                        ch.Inventory.Insert(0, item);
                                        item.CarriedBy = ch;
                                    }

                                    ch.Act("$N hands {0} $p to $n.", shopKeeper, item, null, ActType.ToRoom, numberof);
                                    ch.Act("$N hands {0} $p to you.", shopKeeper, item, null, ActType.ToChar, numberof);

                                }

                            }
                            else
                                ch.Act("$N says '\\yYou don't have enough coin for that.\\x'\n\r", shopKeeper);
                        }
                    }
                    else
                        ch.Act("$N says '\\yI don't seem to be carrying that.\\x'\n\r", shopKeeper);
                }
                else
                    ch.send("You see no shopkeepers here.\n\r");
            }
        }

        public static void DoCompare(Character ch, string arguments)
        {
            string arg1 = "", arg2 = "";
            ItemData obj1;
            ItemData obj2;
            int value1;
            int value2;
            string msg;
            int count = 0;
            if (ch.Form != null)
            {
                ch.send("You can't do that in your current form.\n\r");
                return;
            }
            arguments = arguments.OneArgument(ref arg1);
            arguments = arguments.OneArgument(ref arg2);
            if (arg1.ISEMPTY())
            {
                ch.send("Compare what to what?\n\r");
                return;
            }

            if ((obj1 = ch.GetItemInventory(arg1, ref count)) == null)
            {
                ch.send("You do not have that item.\n\r");
                return;
            }

            if (arg2.ISEMPTY())
            {
                obj2 = (from slot in Character.WearSlots where obj1.wearFlags.ISSET(slot.flag) && ch.Equipment.ContainsKey(slot.id) && ch.Equipment[slot.id] != null select ch.Equipment[slot.id]).FirstOrDefault();

                if (obj2 == null)
                {
                    ch.send("You aren't wearing anything comparable.\n\r");
                    return;
                }
            }

            else if ((obj2 = ch.GetItemInventory(arg2, ref count)) == null && (obj2 = ch.GetItemEquipment(arg2, ref count)) != null)
            {
                ch.send("You do not have that item.\n\r");
                return;
            }

            msg = "";
            value1 = 0;
            value2 = 0;

            if (obj1 == obj2)
            {
                msg = "You compare $p to itself.  It looks about the same.";
            }
            else if (obj1.ItemType.All(it => obj2.ItemType.ISSET(it)))
            {
                msg = "You can't compare $p and $P.";
            }
            else
            {
                if (obj1.ItemType.ISSET(ItemTypes.Armor))
                {
                    value1 = obj1.ArmorBash + obj1.ArmorExotic + obj1.ArmorBash + obj1.ArmorPierce;
                    value2 = obj2.ArmorBash + obj2.ArmorExotic + obj2.ArmorBash + obj2.ArmorPierce;
                }
                else if (obj1.ItemType.ISSET(ItemTypes.Weapon))
                {
                    value1 = (1 + obj1.DamageDice.DiceCount) * obj1.DamageDice.DiceSides;
                    value2 = (1 + obj2.DamageDice.DiceCount) * obj2.DamageDice.DiceCount;
                }
                else
                {
                    msg = "You can't compare $p and $P.";
                }


            }

            if (msg.ISEMPTY())
            {
                if (value1 == value2) msg = "$p and $P look about the same.";
                else if (value1 > value2) msg = "$p looks better than $P.";
                else msg = "$p looks worse than $P.";
            }

            ch.Act(msg, null, obj1, obj2, ActType.ToChar);
            return;
        }


        public static void DoSacrifice(Character ch, string arguments)
        {
            string itemName = "";
            string containerName = "";
            arguments = arguments.OneArgument(ref itemName);
            arguments = arguments.OneArgument(ref containerName);

            {
                int count = 0;
                var item = ch.GetItemRoom(itemName, ref count);

                if (item != null)
                {

                    if (item.ItemType.Contains(ItemTypes.Fountain) || !item.wearFlags.Contains(WearFlags.Take))
                    {
                        ch.send(string.Format("Are you nuts? You cannot sacrifice {0} to the gods.\n\r", item.ShortDescription));
                        return;
                    }

                    if (item.ItemType.ISSET(ItemTypes.PC_Corpse))
                    {
                        ch.Act("The gods wouldn't approve of that.");
                        return;
                    }

                    foreach (var contained in item.Contains)
                    {
                        ch.Room.items.Insert(0, contained);
                        contained.Room = ch.Room;
                        contained.Container = null;

                        //if (contained.extraFlags.ISSET(ExtraFlags.MeltDrop))
                        //{
                        //    ch.Act("$p crumbles into dust.", item: contained, type: ActType.ToRoom);
                        //    ch.Act("$p crumbles into dust.", item: contained, type: ActType.ToChar);
                        //    contained.Dispose();
                        //}
                    }
                    item.Contains.Clear();

                    ch.Room.items.Remove(item);
                    item.Room = null;
                    item.Dispose();
                    ch.send(string.Format("You sacrifice {0} to the gods.\n\r", item.ShortDescription));
                    ch.Act("$n sacrifices $p to the gods.\n\r", null, item, null, ActType.ToRoom);
                }

                else ch.send("You don't see that here.\n\r");
            }
        }

        public static void DoFill(Character ch, string args)
        {
            string containerName = "";
            args = args.OneArgument(ref containerName);
            ItemData container = null;
            ItemData fountain = null;
            int count = 0;

            // fountain search
            foreach (var item in ch.Room.items)
            {
                if (item.ItemType.Contains(ItemTypes.Fountain))
                {
                    fountain = item;
                    break;
                }
            }

            if (ch.Fighting != null)
            {
                ch.send("You're too busy fighting to fill anything.\n\r");
            }

            else if (fountain == null)
            {
                ch.send("Nothing here to fill from.\n\r");
                return;
            }

            else if (string.IsNullOrEmpty(containerName))
            {
                ch.send("Fill what?\n\r");
            }

            else if ((container = ch.GetItemInventory(containerName, ref count)) == null)
            {
                ch.send("You can't find it.\n\r");
            }

            else if (!container.ItemType.Contains(ItemTypes.DrinkContainer))
            {
                ch.send("{0} can't be filled.\n\r", container.Display(ch));
            }

            else if (container.Charges >= container.MaxCharges)
            {
                ch.send("{0} is already full.\n\r", container.Display(ch));
            }

            else // fill the damn container!
            {
                container.Charges = Math.Max(16, container.MaxCharges);
                container.Liquid = fountain.Liquid;
                ch.send("You fill {0} with {1} from {2}.\n\r", container.Display(ch), fountain.Liquid, fountain.Display(ch));
                ch.Act("$n fills $p with {0} from $P.\n\r", null, container, fountain, ActType.ToRoom, fountain.Liquid);
            }
        }

        public static void DoDrink(Character ch, string args)
        {
            string containerName = "";
            args = args.OneArgument(ref containerName);
            ItemData container = null;
            int count = 0;

            if (string.IsNullOrEmpty(containerName))
            {
                foreach (var item in ch.Room.items)
                {
                    if (item.ItemType.Contains(ItemTypes.Fountain))
                    {
                        container = item;
                    }
                }

                if (container == null)
                {
                    ch.send("Drink what?\n\r");
                    return;
                }
            }
            else
            {
                if (ch.Form != null || (container = ch.GetItemHere(containerName, ref count)) == null)
                {
                    ch.send("You can't find it.\n\r");
                    return;
                }
            }

            if (ch.Fighting != null)
            {
                ch.send("You're too busy fighting to drink anything.\n\r");
                return;
            }



            int amount;
            string liquid;
            int charges;

            if (container.ItemType.Contains(ItemTypes.Fountain))
            {
                liquid = container.Liquid;
                amount = container.Nutrition;
            }

            else if (container.ItemType.Contains(ItemTypes.DrinkContainer))
            {
                liquid = container.Liquid;
                amount = container.Nutrition;
                charges = container.Charges;

                if (charges == 0)
                {
                    ch.send("It's empty.\n\r");
                    return;
                }
                else container.Charges--;

            }
            else
            {
                ch.send("You can't drink from that.\n\r");
                return;
            }
            var hunger = 0;
            var thirst = amount;
            var drunk = 0;

            Liquid liq;
            if (Liquid.Liquids.TryGetValue(liquid.ToLower(), out liq))
            {
                amount = Math.Max(liq.ssize, amount);
                thirst = amount * liq.thirst / 3;
                hunger = amount * liq.full / 3;
                drunk = amount * liq.proof / 36;

            }

            if (ch.Drunk >= 48 && drunk > 0)
            {
                ch.send("You fail to reach your mouth. *Hic*\n\r");
                return;
            }

            //if (amount > 0)
            {


                ch.Act("You drink {0} from $p.\n\r", null, container, args: liquid);
                ch.Act("$n drinks {0} from $p.\n\r", null, container, null, ActType.ToRoom, liquid);

                if (thirst != 0)
                    ch.Thirst = Math.Min(ch.Thirst + thirst, 48);

                if (hunger != 0)
                    ch.Hunger = Math.Min(ch.Hunger + hunger, 48);

                if (ch.Thirst > 0)
                    ch.Dehydrated = 0;

                if (ch.Hunger > 0)
                    ch.Starving = 0;

                if (drunk != 0)
                    ch.Drunk = ch.Drunk + drunk;

                if (!ch.IsNPC && ch.Thirst >= 48 && thirst > 0)
                {
                    ch.send("Your thirst is quenched.\n\r");
                    ch.Dehydrated = 0;
                }

                if (!ch.IsNPC && ch.Hunger >= 48 && hunger > 0)
                {
                    ch.send("You are full.\n\r");
                    ch.Dehydrated = 0;
                    ch.Starving = 0;
                }

                if (!ch.IsNPC && ch.Drunk >= 20)
                {
                    ch.send("You're smashed!\n\r");
                    AffectData affect;
                    var alcoholpoisoning = SkillSpell.SkillLookup("alcohol poisoning");

                    if ((affect = ch.FindAffect(alcoholpoisoning)) != null)
                    {
                        affect.duration = ch.Drunk / 10;
                    }
                    else
                    {
                        affect = new AffectData();
                        affect.displayName = "alcohol poisoning";
                        affect.skillSpell = alcoholpoisoning;
                        affect.location = ApplyTypes.None;
                        affect.where = AffectWhere.ToAffects;
                        affect.level = 1;
                        affect.duration = ch.Drunk / 10;
                        ch.AffectToChar(affect);
                    }
                }
                else if (!ch.IsNPC && ch.Drunk >= 10)
                {
                    ch.send("You feel drunk.\n\r");

                }
                else if (!ch.IsNPC && ch.Drunk >= 2)
                {
                    ch.send("You feel tipsy.\n\r");

                }

                if (container.extraFlags.Contains(ExtraFlags.Poison))
                {

                    var affect = new AffectData();

                    affect.skillSpell = SkillSpell.SkillLookup("poison");
                    affect.location = ApplyTypes.None;
                    affect.flags.SETBIT(AffectFlags.Poison);
                    affect.where = AffectWhere.ToAffects;
                    affect.level = container.Level;
                    affect.displayName = "food poisoning";
                    affect.duration = 2;
                    ch.AffectToChar(affect);
                    ch.send("You choke and gag.\n\r");
                    ch.Act("$n chokes and gags.\n\r", type: ActType.ToRoom);

                }
            }

        }

        public static void DoEat(Character ch, string args)
        {
            string foodName = "";
            args = args.OneArgument(ref foodName);
            ItemData item = null;
            int count = 0;
            var carrionskill = SkillSpell.SkillLookup("carrion feeding");
            int carrionfeeding = ch.GetSkillPercentage(carrionskill);

            if (string.IsNullOrEmpty(foodName))
            {
                ch.send("Eat what?\n\r");
                return;
            }
            else if (ch.Form == null && (item = ch.GetItemInventory(foodName, ref count)) == null)
            {
                ch.send("You don't have that item.\n\r");
                return;
            }
            else if (ch.Form != null && (item = ch.GetItemRoom(foodName, ref count)) == null)
            {
                ch.send("You don't see that here.\n\r");
                return;
            }
            else if (item == null)
            {
                ch.send("You don't have that item.\n\r");
                return;
            }
            else if (ch.Fighting != null)
            {
                ch.send("You are too busy fighting to worry about food\n\r");
                return;
            }
            else if (carrionfeeding > 1 && ch.IsAffected(carrionskill))
            {
                ch.Act("You are not ready to carrion feed yet.");
            }
            else if (!item.ItemType.Contains(ItemTypes.Food) && !item.ItemType.Contains(ItemTypes.Pill) &&
                (carrionfeeding <= 1 ||
                   !(item.ItemType.Contains(ItemTypes.NPCCorpse) || (item.Vnum >= 8 && item.Vnum <= 12))))
            {
                ch.send("That's not edible.\n\r");
                return;
            }
            else if (!ch.IsNPC && !ch.IsImmortal && ch.Hunger > 40 &&
                (carrionfeeding <= 1 ||
                   !(item.ItemType.Contains(ItemTypes.NPCCorpse) || (item.Vnum >= 8 && item.Vnum <= 12))))
            {
                ch.send("You are too full to eat more.\n\r");
                return;
            }
            else
            {

                if (carrionfeeding > 1 && (((item.ItemType.Contains(ItemTypes.NPCCorpse) || (item.Vnum >= 8 && item.Vnum <= 12)))))
                {
                    item.Nutrition = 40;
                    ch.Thirst = 40;
                    ch.Dehydrated = 0;

                    ch.HitPoints += (int)(ch.MaxHitPoints * 0.2);
                    ch.HitPoints = Math.Min(ch.HitPoints, ch.MaxHitPoints);
                    ch.Act("You devour $p ravenously.", null, item, type: ActType.ToChar);
                    ch.Act("$n devours $p ravenously.", null, item, type: ActType.ToRoom);

                    var feeding = new AffectData();
                    feeding.duration = 1;
                    feeding.displayName = "carrion feeding";
                    feeding.endMessage = "You feel ready to carrion feed again.";
                    feeding.skillSpell = carrionskill;
                    ch.AffectToChar(feeding);
                }
                else
                {
                    ch.Act("You eat $p.", null, item, type: ActType.ToChar);
                    ch.Act("$n eats $p.", null, item, type: ActType.ToRoom);

                }
                ch.Hunger += item.Nutrition;
                ch.Starving = 0;
                if (ch.Hunger <= 4)
                    ch.send("You are no longer hungry.\n\r");
                else if (ch.Hunger > 40)
                    ch.send("You are full.\n\r");

                if (item.extraFlags.Contains(ExtraFlags.Heart))
                {
                    ch.send("You feel empowered by eating the heart of your foe.\n\r");
                    ch.HitPoints += 15;
                }
                if (item.extraFlags.Contains(ExtraFlags.Poison) && carrionfeeding <= 1)
                {
                    var affect = new AffectData();
                    affect.displayName = "food poisoning";
                    affect.skillSpell = SkillSpell.SkillLookup("poison");
                    affect.location = ApplyTypes.None;
                    affect.where = AffectWhere.ToAffects;
                    affect.level = item.Level;
                    affect.duration = 2;
                    ch.AffectToChar(affect);
                    ch.send("You choke and gag.\n\r");
                    ch.Act("$n chokes and gags.\n\r", type: ActType.ToRoom);

                }

                if (ch.Inventory.Contains(item))
                {
                    ch.Inventory.Remove(item);
                    item.CarriedBy = null;
                }
                else if (ch.Room.items.Contains(item))
                {
                    ch.Room.items.Remove(item);
                    item.Room = null;
                }
                if (item.Spells.Any())
                    foreach (var spell in item.Spells)
                    {
                        if (spell.Spell != null)
                            Magic.ItemCastSpell(Magic.CastType.Cast, spell.Spell, spell.Level, ch, ch, item, ch.Room);
                    }
            }
        } // end do eat

        public static void DoUse(Character ch, string arguments)
        {
            if (arguments.ISEMPTY())
            {
                ch.send("Use what?\n\r");
                return;
            }

            string itemname = "";

            arguments = arguments.OneArgument(ref itemname);

            var item = ch.GetItemHere(itemname);

            if (item == null)
            {
                ch.send("You don't see that here.\n\r");
                return;
            }

            bool found = false;

            Programs.ExecutePrograms(Programs.ProgramTypes.Use, ch, item, "");

            if (!found)
                ch.send("You can't seem to figure out how to do that.\n\r");
        }

        public static void DoInvoke(Character ch, string arguments)
        {
            if (arguments.ISEMPTY())
            {
                ch.send("Invoke what?\n\r");
                return;
            }

            if (ch.Room != null)
            {
                bool result = false;
                foreach (var item in ch.Equipment.Values.Concat(ch.Inventory).Concat(ch.Room.items).ToArray())
                    Programs.ExecutePrograms(Programs.ProgramTypes.Invoke, ch, item, "");
                if (!result) ch.send("Nothing seems to happen.\n\r");
            }
        }

        public static void DoRequest(Character ch, string arguments)
        {

            if (ch.Alignment != Alignment.Good)
            {
                ch.send("Only those who follow the Light can request items.\n\r");
                return;
            }
            else if (ch.Position == Positions.Fighting)
            {
                ch.send("No way! You are still fighting!\n\r");
                return;
            }
            else if (arguments.ISEMPTY())
            {
                ch.send("Request what from who?\n\r");
                return;
            }

            string npcname = "";
            string itemname = "";
            arguments = arguments.OneArgument(ref itemname);
            arguments = arguments.OneArgument(ref npcname);

            Character npc;
            ItemData item;
            WearSlotIDs slot;
            if (npcname.ISEMPTY() || itemname.ISEMPTY())
            {
                ch.send("Request what from who?\n\r");
                return;
            }
            else if ((npc = ch.GetCharacterFromRoomByName(npcname)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (!npc.IsNPC)
            {
                ch.send("You cannot request from other players.\n\r");
                return;
            }
            else if (npc.Position <= Positions.Sleeping)
            {
                ch.Act("$n must be awake and healthy to do that.\n\r", npc);
                return;
            }
            else if (npc.Alignment != Alignment.Good)
            {
                ch.send("You can only request from beings that follow the Light.\n\r");
                return;
            }
            else if (npc.Flags.ISSET(ActFlags.Shopkeeper))
            {
                DoActCommunication.DoSay(npc, "Come on, you'll have to buy that!");
                return;
            }
            else if (ch.Level < npc.Level - 10)
            {
                DoActCommunication.DoSay(npc, "Come back when you're older.");
                return;
            }
            else if ((item = npc.GetItemInventoryOrEquipment(itemname, out slot)) == null)
            {
                DoActCommunication.DoSay(npc, "I don't have that.");
                return;
            }

            if (slot != WearSlotIDs.None)
            {
                if (item.extraFlags.ISSET(ExtraFlags.NoRemove))
                {
                    DoActCommunication.DoSay(npc, "I can't seem to remove it.");
                    return;
                }
                npc.RemoveEquipment(slot);
            }

            if (item.extraFlags.Contains(ExtraFlags.NoDrop))
            {
                DoActCommunication.DoSay(npc, "I can't seem to let go of it.");
                return;
            }

            if (ch.Carry + 1 > ch.MaxCarry)
            {
                ch.Act("$N tries to give you $p, but you are carrying too many things.", npc, item, type: ActType.ToVictim);
                return;
            }
            if (ch.TotalWeight + item.Weight > ch.MaxWeight)
            {
                ch.Act("$N tries to give you $p, but you are carrying too much weight.", npc, item, type: ActType.ToVictim);
                return;
            }

            npc.Inventory.Remove(item);
            ch.Inventory.Insert(0, item);
            item.CarriedBy = ch;
            ch.Act("$N gives you $p.\n\r", npc, item, null, ActType.ToChar);
            ch.Act("$N gives $p to $n.\n\r", npc, item, type: ActType.ToRoomNotVictim);
        } // end of DoRequest


    } // End Character

}
