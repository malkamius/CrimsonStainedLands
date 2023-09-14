using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static CrimsonStainedLands.ShapeshiftForm;

namespace CrimsonStainedLands
{
    public static class Magic
    {
        public enum CastType
        {
            None,
            Cast,
            Commune,
            Sing,
            Invoke
        }

        public static void CastCommuneOrSing(Character ch, string argument, CastType MethodUsed)
        {
            StringBuilder buf = new StringBuilder();
            Character victim = null;
            ItemData item = null;
            Character vo = null;
            string arg1 = null;
            string arg2 = null;
            SkillSpell spell;
            TargetIsType target = TargetIsType.targetNone;

            int mana;
            //if (!(ch is Connection))
            //    return;

            var targetName = argument.OneArgument(ref arg1);
            argument = targetName.OneArgument(ref arg2);
            targetName = arg2;

            if (string.IsNullOrEmpty(arg1))
            {
                if (MethodUsed == CastType.Cast)

                    ch.send("Cast which what where?\n\r");
                else if (MethodUsed == CastType.Commune)
                    ch.send("Pray which prayer of supplication?\n\r");
                else if (MethodUsed == CastType.Sing)
                    ch.send("Sing what?\n\r");
                return;
            }
            if (ch.IsAffected(AffectFlags.Silenced))
            {
                ch.Act("You cannot do that while silenced!\n\r");
                return;
            }
            if ((spell = SkillSpell.FindSpell(ch, arg1)) == null ||
                //spell.spellFun == null || 
                //ch.Level < ch.GetLevelSkillLearnedAt(spell) ||
                ch.GetSkillPercentage(spell) <= 1)
            {
                if (MethodUsed == CastType.Cast)
                    ch.send("You don't know any spells of that name.\n\r");
                else if (MethodUsed == CastType.Commune)
                    ch.send("You don't know any supplications of that name.\n\r");
                else if (MethodUsed == CastType.Sing)
                    ch.send("You don't know any songs of that name.\n\r");
                return;
            }

            if (ch.IsAffected(AffectFlags.Deafen))
            {
                ch.send("You can't concentrate with the ringing in your ears.\n\r");
                return;
            }

            if (!ch.IsImmortal && !ch.IsNPC)
            {
                if (ch.Guild.CastType == CastType.Commune && MethodUsed != CastType.Commune)
                {
                    ch.send("You must commune with the gods for your blessings.\n\r");
                    return;
                }
                else if (ch.Guild.CastType == CastType.Cast && MethodUsed != CastType.Cast)
                {
                    ch.send("You should try casting that as a spell.\n\r");
                    return;
                }
                else if (ch.Guild.CastType == CastType.Sing && MethodUsed != CastType.Sing)
                {
                    ch.send("You should try singing that song.\n\r");
                    return;
                }


                if (MethodUsed == CastType.Cast && !spell.SkillTypes.Contains(SkillSpellTypes.Spell))
                {
                    ch.send("You can't cast that.\n\r");
                    return;
                }

                if (MethodUsed == CastType.Commune && !spell.SkillTypes.Contains(SkillSpellTypes.Commune))
                {
                    ch.send("You can't pray to the gods about that.\n\r");
                    return;
                }

                if (MethodUsed == CastType.Sing && !spell.SkillTypes.Contains(SkillSpellTypes.Song))
                {
                    ch.send("You can't sing that.\n\r");
                    return;
                }
                if (ch.Position < spell.minimumPosition)
                {
                    ch.send("You can't concentrate enough.\n\r");
                    return;
                }
            }
            mana = spell.GetManaCost(ch);

            int count = 0;


            switch (spell.targetType)
            {
                case TargetTypes.targetIgnore:
                    break;
                case TargetTypes.targetCharOffensive:
                    if (string.IsNullOrEmpty(arg2))
                        victim = ch.Fighting;
                    else if ((victim = ch.GetCharacterFromRoomByName(targetName, ref count)) == null)
                    {
                        ch.send("They aren't here.\n\r");
                        return;
                    }

                    if (victim == null)
                    {
                        ch.send("They are not here.\n\r");
                        return;
                    }
                    

                    target = TargetIsType.targetChar;
                    vo = victim;
                    break;
                case TargetTypes.targetCharDefensive:
                    if (string.IsNullOrEmpty(arg2))
                        victim = ch;
                    else if ((victim = ch.GetCharacterFromRoomByName(targetName, ref count)) == null)
                    {
                        ch.send("They aren't here.\n\r");
                        return;
                    }
                    target = TargetIsType.targetChar;
                    vo = victim;
                    break;
                case TargetTypes.targetCharSelf:
                    if (!string.IsNullOrEmpty(arg2) && !targetName.IsName(ch.Name))
                    {
                        ch.send("You cannot cast this spell on another.\n\r");
                        return;
                    }

                    vo = ch;
                    target = TargetIsType.targetChar;
                    break;
                case TargetTypes.targetItemInventory:
                    if (arg2.ISEMPTY())
                    {
                        ch.send("What should the spell be cast upon?\n\r");
                        return;
                    }

                    if ((item = ch.GetItemInventory(targetName, ref count)) == null)
                    {
                        ch.send("You are not carrying that.\n\r");
                        return;
                    }

                    target = TargetIsType.targetItem;
                    break;

                case TargetTypes.targetItemCharOff:
                    if (arg2.ISEMPTY())
                    {
                        if ((victim = ch.Fighting) == null)
                        {
                            ch.send("Cast the spell on whom or what?\n\r");
                            return;
                        }

                        target = TargetIsType.targetChar;
                    }
                    else if ((item = ch.GetItemHere(targetName)) != null)
                    {
                        target = TargetIsType.targetItem;
                    }
                    else if ((item = ch.GetItemHere(argument)) != null)
                    {
                        target = TargetIsType.targetItem;
                    }
                    else if ((victim = ch.GetCharacterFromRoomByName(targetName, ref count)) != null)
                    {
                        target = TargetIsType.targetChar;
                    }

                    if (target == TargetIsType.targetChar) /* check the sanity of the attack */
                    {
                        /*
                        if(is_safe_spell(ch,victim,FALSE) && victim != ch)
                        {
                        send_to_char("Not on that target.\n\r",ch);
                        return;
                        }
                        */
                        //if (IS_AFFECTED(ch, AFF_CHARM) && ch->master == victim)
                        //{
                        //    send_to_char("You can't do that on your own follower.\n\r",
                        //        ch);
                        //    return;
                        //}

                        //if (!IS_NPC(ch))
                        //    check_killer(ch, victim);

                        vo = victim;
                    }

                    else
                    {
                        ch.send("You don't see that here.\n\r");
                        return;
                    }
                    break;

                case TargetTypes.targetItemCharDef:
                    if (arg2.ISEMPTY())
                    {
                        vo = ch;
                        victim = ch;
                        target = TargetIsType.targetChar;
                    }

                    else if ((item = ch.GetItemHere(argument, ref count)) != null)
                    {
                        target = TargetIsType.targetItem;
                    }
                    else if ((item = ch.GetItemHere(arg2, ref count)) != null)
                    {
                        target = TargetIsType.targetItem;
                    }
                    else if ((victim = ch.GetCharacterFromRoomByName(arg2, ref count)) != null)
                    {
                        vo = victim;
                        target = TargetIsType.targetChar;
                    }
                    else
                    {
                        ch.send("You don't see that here.\n\r");
                        return;
                    }
                    break;
                default:
                    ch.send("You can't cast this type of spell yet.\n\r");
                    return;
            }

            if (ch.ManaPoints < mana)
            {
                ch.send("You don't have enough mana.\n\r");
                return;
            }

            if(target == TargetIsType.targetChar && 
                (spell.targetType == TargetTypes.targetCharOffensive || spell.targetType == TargetTypes.targetItemCharOff) && 
                victim != null)
            {
                if (victim.Fighting == null)
                    victim.Fighting = ch;

                if (ch.Fighting == null)
                    ch.Fighting = victim;

                ch.Position = Positions.Fighting;

            }

            if (MethodUsed == CastType.Cast)
            {
                SaySpell(ch, spell);
            }
            else if (MethodUsed == CastType.Sing)
            {
                ch.Act("\\M$n sings \n\r\n\r{0}\\x\n\r\n\r", type: ActType.ToRoom, args: spell.Lyrics);//.Replace("\r", "").Replace("\n", "\n\t"));
                ch.Act("\\MYou sing \n\r\n\r{0}\\x\n\r\n\r", args: spell.Lyrics);//.Replace("\r", "").Replace("\n", "\n\t"));
            }
            else if (MethodUsed == CastType.Commune)
            {
                ch.Act("$n closes their eyes for a moment.", null, null, null, ActType.ToRoom);
                ch.Act("You close your eyes for a moment as you pray to your diety.", null, null, null, ActType.ToChar);
            }
            ch.WaitState(spell.waitTime);

            var chance = ch.GetSkillPercentage(spell) + 10;

            var held = ch.GetEquipment(WearSlotIDs.Held);

            if (MethodUsed == CastType.Sing)
            {
                chance = (chance + ch.GetSkillPercentage("sing") + 10) / 2;



                if (held == null || !held.ItemType.ISSET(ItemTypes.Instrument))
                {
                    chance = (chance + ch.GetSkillPercentage("a cappella") + 10) / 2;
                }
            }

            if (!ch.IsImmortal && Utility.NumberPercent() > chance)
            {
                if (MethodUsed == CastType.Cast)
                    ch.send("You lost your concentration.\n\r");
                else if (MethodUsed == CastType.Sing)
                {
                    ch.send("Your song falls on deaf ears.\n\r");
                    ch.CheckImprove("sing", false, 1);
                    if (held == null || !held.ItemType.ISSET(ItemTypes.Instrument))
                    {
                        ch.CheckImprove("a cappella", false, 1);
                    }
                }
                else if (MethodUsed == CastType.Commune)
                {
                    ch.send("The gods don't seem to hear you.\n\r");
                }
                //checkimprove(ch, spell);
                ch.CheckImprove(spell, false, 1);
                ch.ManaPoints -= mana / 2;
                return;
            }
            else
            {
                ch.ManaPoints -= mana;
                ch.CheckImprove(spell, true, 1);
                if (MethodUsed == CastType.Sing)
                {
                    ch.CheckImprove("sing", true, 1);
                    if (held == null || !held.ItemType.ISSET(ItemTypes.Instrument))
                    {
                        ch.CheckImprove("a cappella", true, 1);
                    }
                }
                if (spell.spellFun != null)
                    spell.spellFun(MethodUsed, spell, ch.Level, ch, vo, item, targetName + " " + argument, target);
            }
        } // End CastCommuneOrSing

        public static void DoCast(Character ch, string argument)
        {
            CastCommuneOrSing(ch, argument, CastType.Cast);
        } // end docast

        public static void DoCommune(Character ch, string argument)
        {
            CastCommuneOrSing(ch, argument, CastType.Commune);
        } // end DoCommune

        public static void ItemCastSpell(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, RoomData room)
        {
            TargetIsType target = TargetIsType.targetNone;
            Character vo = null;
            ItemData vItem = null;
            if (spell is null)
                return;

            if (spell.spellFun is null)
            {
                Game.bug("ObjectCastSpell: bad spell {0}.", spell.name);
                return;
            }

            if (ch.Room.flags.ISSET(RoomFlags.NoMagic))
            {
                ch.Act("$n's spell fizzles.", type: ActType.ToRoom);
                ch.send("Your spell fizzles and dies.\n\r");
                return;
            }


            switch (spell.targetType)
            {
                default:
                    Game.bug("ItemCastSpell: bad target for spell {0}.", spell.name);
                    return;

                case TargetTypes.targetIgnore:
                    vo = null;
                    break;

                case TargetTypes.targetCharOffensive:
                    if (victim == null)
                        victim = ch.Fighting;
                    if (victim == null)
                    {
                        ch.send("You can't do that.\n\r");
                        return;
                    }
                    if (Combat.CheckIsSafe(ch, victim) && ch != victim)
                    {
                        //ch.send("Something isn't right...\n\r");
                        return;
                    }
                    vo = victim;
                    target = TargetIsType.targetChar;
                    break;

                case TargetTypes.targetCharDefensive:
                case TargetTypes.targetCharSelf:
                    if (victim == null)
                        victim = ch;
                    vo = victim;
                    target = TargetIsType.targetChar;
                    break;

                case TargetTypes.targetItemInventory:
                    if (item == null)
                    {
                        ch.send("You can't do that.\n\r");
                        return;
                    }
                    vItem = item;
                    target = TargetIsType.targetItem;
                    break;

                case TargetTypes.targetItemCharOff:
                    if (victim == null && item == null)
                    {
                        if (ch.Fighting != null)
                            victim = ch.Fighting;
                        else
                        {
                            ch.send("You can't do that.\n\r");
                            return;
                        }
                    }

                    if (victim != null)
                    {
                        if (IsSafeSpell(ch, victim, false) && ch != victim)
                        {
                            //ch.send("Something isn't right...\n\r");
                            return;
                        }

                        vo = victim;
                        target = TargetIsType.targetChar;
                    }
                    else
                    {
                        vItem = item;
                        target = TargetIsType.targetItem;
                    }
                    break;


                case TargetTypes.targetItemCharDef:
                    if (victim == null && item == null)
                    {
                        vo = ch;
                        target = TargetIsType.targetChar;
                    }
                    else if (victim != null)
                    {
                        vo = victim;
                        target = TargetIsType.targetChar;
                    }
                    else
                    {
                        vItem = item;
                        target = TargetIsType.targetItem;
                    }

                    break;
            }

            spell.spellFun(Magic.CastType.Cast, spell, level, ch, vo, vItem, null, target);

            if ((spell.targetType == TargetTypes.targetCharOffensive
                || (spell.targetType == TargetTypes.targetItemCharOff && target == TargetIsType.targetChar))
                && victim != ch
                && victim.Master != ch)
            {

                foreach (var vch in ch.Room.Characters)
                {
                    if (victim == vch && victim.Fighting == null)
                    {
                        //Combat.CheckKiller(victim, ch);
                        Combat.multiHit(victim, ch);
                        break;
                    }
                }
            }

            return;
        } // end ItemCastSpell


        public static void SaySpell(Character ch, SkillSpell spell)
        {

            var syllables = new Dictionary<string, string>
            {
                { " ", " " },
                { "ar", "abra" },
                { "au", "kada" },
                { "bless", "fido" },
                { "blind", "nose" },
                { "bur", "mosa" },
                { "cu", "judi" },
                { "de", "oculo" },
                { "en", "unso" },
                { "light", "dies" },
                { "lo", "hi" },
                { "mor", "zak" },
                { "move", "sido" },
                { "ness", "lacri" },
                { "ning", "illa" },
                { "per", "duda" },
                { "ra", "gru" },
                { "fresh", "ima" },
                { "re", "candus" },
                { "son", "sabru" },
                { "tect", "infra" },
                { "tri", "cula" },
                { "ven", "nofo" },
                { "a", "a" },
                { "b", "b" },
                { "c", "q" },
                { "d", "e" },
                { "e", "z" },
                { "f", "y" },
                { "g", "o" },
                { "h", "p" },
                { "i", "u" },
                { "j", "y" },
                { "k", "t" },
                { "l", "r" },
                { "m", "w" },
                { "n", "i" },
                { "o", "a" },
                { "p", "s" },
                { "q", "d" },
                { "r", "f" },
                { "s", "g" },
                { "t", "h" },
                { "u", "j" },
                { "v", "z" },
                { "w", "x" },
                { "x", "n" },
                { "y", "l" },
                { "z", "k" }
            };

            StringBuilder buf = new StringBuilder();

            for (int i = 0; i < spell.name.Length; i++)
            {
                foreach (var syl in syllables)
                {
                    if (spell.name.Substring(i).StringPrefix(syl.Key))
                    {
                        i += syl.Key.Length - 1;
                        buf.Append(syl.Value);
                        break;
                    }
                }
            }

            foreach (var other in ch.Room.Characters)
            {
                if (other != ch)
                {
                    if (other.Guild == ch.Guild)
                        ch.Act("$n utters the words '" + spell.name + "'.\n\r", other, null, null, ActType.ToVictim);
                    else
                        ch.Act("$n utters the words '" + buf.ToString() + "'.\n\r", other, null, null, ActType.ToVictim);
                }
            }
        }

        public static bool IsSafeSpell(Character ch, Character victim, bool area)
        {
            if ((ch.FindAffect(AffectFlags.DuelInProgress, out var chduel) || ch.Master != null && ch.Master.FindAffect(AffectFlags.DuelInProgress, out chduel)) && chduel.ownerName == victim.Name)
                return false;

            if (ch.IsAffected(AffectFlags.Calm))
            {
                ch.send("You feel to calm to fight.\n\r");
                Combat.StopFighting(ch);

                return true;
            }

            if (victim.Room == null || ch.Room == null)
                return true;

            if (victim == ch && area)
                return true;

            if (Combat.CheckIsSafe(ch, victim))
                return false;

            if (victim.Fighting == ch || victim == ch)
                return false;

            //if (IS_IMMORTAL(ch) && ch.level > LEVEL_IMMORTAL && !area)
            //    return false;

            /* killing mobiles */
            if (victim.IsNPC)
            {
                /* safe room? */
                if (victim.Room.flags.ISSET(RoomFlags.Safe))
                    return true;

                if (victim.IsShop)
                    return true;

                /* no killing healers, trainers, etc */
                if (victim.Flags.ISSET(ActFlags.Train)
                    || victim.Flags.ISSET(ActFlags.Practice)
                    || victim.Flags.ISSET(ActFlags.Healer)
                    || victim.Flags.ISSET(ActFlags.Changer))
                    return true;

                if (!ch.IsNPC)
                {
                    /* no pets */
                    if (victim.Flags.ISSET(ActFlags.Pet))
                        return true;

                    /* no charmed creatures unless owner */
                    if (victim.IsAffected(AffectFlags.Charm) && (area || ch != victim.Master))
                        return true;

                    /* legal kill? -- cannot hit mob fighting non-group member */
                    if (victim.Fighting != null && !ch.IsSameGroup(victim.Fighting))
                        return true;
                }
                else
                {
                    /* area effect spells do not hit other mobs */
                    if (area && !victim.IsSameGroup(ch.Fighting))
                        return true;
                }
            }
            /* killing players */
            else
            {
                //if (area && IS_IMMORTAL(victim) && victim.level > LEVEL_IMMORTAL)
                //    return true;

                /* NPC doing the killing */
                if (ch.IsNPC)
                {
                    /* charmed mobs and pets cannot attack players while owned */
                    if (((ch.IsAffected(AffectFlags.Charm) && (ch.Master != null
                        && ch.Master.Fighting != victim))
                        || (ch.Leader != null && ch.Leader.Fighting != victim)))
                        return true;

                    if (ch.Leader != null && !victim.IsNPC)
                        return false;
                    /* safe room? */
                    if (victim.Room.flags.ISSET(RoomFlags.Safe))
                        return true;

                    /* legal kill? -- mobs only hit players grouped with opponent*/
                    if (ch.Fighting != null && !ch.Fighting.IsSameGroup(victim))
                        return true;
                }
                /* player doing the killing */
                else
                {
                    //if (!is_cabal(ch))
                    //    return true;

                    if (ch.Level > victim.Level + 8)
                        return true;
                }
            }
            return false;
        } // end of IsSafeSpell
        public static void SpellArmor(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (victim == ch)
                    ch.send("You already have armor.\n\r");
                else
                    ch.send("They already have armor.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Armor;
                affect.duration = level / 2;
                affect.modifier = -20;
                affect.displayName = "armor";
                affect.endMessage = "The armor surrounding you fades.\n\r";
                affect.endMessageToRoom = "The armor surrounding $n fades.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("You are surrounded by armor.\n\r");
                victim.Act("$n is surrounded by armor.\n\r", type: ActType.ToRoom);
            }
        }
        public static void SpellCureLight(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            int heal = (int)(ch.Level * 1.5) + Utility.Random(ch.Level / 2, ch.Level);

            victim.HitPoints = Math.Min(victim.HitPoints + heal, victim.MaxHitPoints);
            if (victim.Position <= Positions.Stunned)
                Combat.UpdatePosition(victim);
            victim.send("Your wounds are slightly healed.\n\r");
            victim.Act("$n's wounds are slightly healed.\n\r", type: ActType.ToRoom);
        }
        public static void SpellMendWounds(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            int heal = ch.GetDamage(Math.Min(level, 17), 1, 1, 20);
            victim.HitPoints = Math.Min(victim.HitPoints + heal, victim.MaxHitPoints);
            if (victim.Position <= Positions.Stunned)
                Combat.UpdatePosition(victim);
            victim.Act("Your wounds are healed.\n\r", victim);
            victim.Act("$n's wounds are healed.\n\r", type: ActType.ToRoom);
        }
        public static void SpellCureSerious(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            int heal = (int)(ch.Level * 2.5) + Utility.Random(ch.Level / 2, ch.Level);
            victim.HitPoints = Math.Min(victim.HitPoints + heal, victim.MaxHitPoints);
            if (victim.Position <= Positions.Stunned)
                Combat.UpdatePosition(victim);
            victim.send("Your wounds are greatly healed.\n\r");
            victim.Act("$n's wounds are greatly healed.\n\r", type: ActType.ToRoom);
        }

        public static void SpellCureCritical(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            int heal = (int)(ch.Level * 4) + Utility.Random(ch.Level / 2, ch.Level);
            victim.HitPoints = Math.Min(victim.HitPoints + heal, victim.MaxHitPoints);
            if (victim.Position <= Positions.Stunned)
                Combat.UpdatePosition(victim);
            victim.send("Your wounds are immensely healed.\n\r");
            victim.Act("$n's wounds are immensely healed.\n\r", type: ActType.ToRoom);
        }
        public static void SpellHeal(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            int heal = (int)(ch.Level * 5) + Utility.Random(ch.Level / 2, ch.Level);
            victim.HitPoints = Math.Min(victim.HitPoints + heal, victim.MaxHitPoints);
            if (victim.Position <= Positions.Stunned)
                Combat.UpdatePosition(victim);
            victim.send("Your wounds are healed.\n\r");
            victim.Act("$n's wounds are healed.\n\r", type: ActType.ToRoom);
        }
        public static void SpellRejuvenate(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            int heal = (int)(ch.Level * 6) + Utility.Random(ch.Level / 2, ch.Level);
            victim.HitPoints = Math.Min(victim.HitPoints + heal, victim.MaxHitPoints);
            if (victim.Position <= Positions.Stunned)
                Combat.UpdatePosition(victim);
            victim.send("Your wounds are healed.\n\r");
            victim.Act("$n's wounds are healed.\n\r", type: ActType.ToRoom);
        }
        public static void SpellBless(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                ch.send("They are already blessed.\n\r");
                return;
            }
            else
            {

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 10 + level / 4;
                affect.modifier = Math.Max(3, level / 3);
                affect.displayName = "blessed";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Damroll;
                affect.duration = 10 + level / 4;
                affect.modifier = Math.Max(3, level / 3);
                affect.displayName = "blessed";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Saves;
                affect.duration = 10 + level / 4;
                affect.modifier = -(Math.Max(5, level / 3));
                affect.displayName = "blessed";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);

                affect.endMessage = "The blessing surrounding you fades.\n\r";
                affect.endMessageToRoom = "The blessing surrounding $n fades.\n\r";

                victim.send("You are surrounded by a blessing.\n\r");
                victim.Act("$n is surrounded by a blessing.", type: ActType.ToRoom);
            }
        }
        public static void SpellPrayer(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;

            ch.Act("$n kneels and prays for a blessing.", type: ActType.ToRoom);

            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (!GroupMember.IsAffected(spell))
                {
                    affect = new AffectData();
                    affect.skillSpell = spell;
                    affect.level = level;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Hitroll;
                    affect.duration = 10 + level / 4;
                    affect.modifier = Math.Max(3, level / 3);
                    affect.displayName = "blessed";
                    affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                    GroupMember.AffectToChar(affect);

                    affect = new AffectData();
                    affect.skillSpell = spell;
                    affect.level = level;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Damroll;
                    affect.duration = 10 + level / 4;
                    affect.modifier = Math.Max(3, level / 3);
                    affect.displayName = "blessed";
                    affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                    GroupMember.AffectToChar(affect);

                    affect = new AffectData();
                    affect.skillSpell = spell;
                    affect.level = level;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Saves;
                    affect.duration = 10 + level / 4;
                    affect.modifier = -(Math.Max(5, level / 3));
                    affect.displayName = "blessed";
                    affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                    GroupMember.AffectToChar(affect);

                    affect.endMessage = "The blessing surrounding you fades.\n\r";
                    affect.endMessageToRoom = "The blessing surrounding $n fades.\n\r";

                    GroupMember.send("You are surrounded by a blessing.\n\r");
                    GroupMember.Act("$n is surrounded by a blessing.", type: ActType.ToRoom);
                }
            }
        }

        public static void SpellBlindness(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch.Fighting;
            if (victim == null)
                ch.send("You aren't fighting anyone.\n\r");
            else if ((affect = victim.FindAffect(spell)) != null)
            {
                ch.send("They are already blind.\n\r");
                return;
            }
            else if ((victim.ImmuneFlags.ISSET(WeaponDamageTypes.Blind) || (victim.Form != null && victim.Form.ImmuneFlags.ISSET(WeaponDamageTypes.Blind))) && !victim.IsAffected(AffectFlags.Deafen))
                victim.Act("$n is immune to blindness.", type: ActType.ToRoom);
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = ch_level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Hitroll;
                affect.flags.Add(AffectFlags.Blind);
                affect.duration = Math.Max(3, ch_level / 3);
                affect.modifier = -4;
                affect.displayName = "Blinded";
                affect.endMessage = "You can see again.\n\r";
                affect.endMessageToRoom = "$n can see again.\n\r";
                affect.affectType = AffectTypes.Malady; //castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("You are blinded.\n\r");
                victim.Act("$n is blinded.\n\r", null, null, null, ActType.ToRoom);
            }
        }

        public static void SpellCreateBread(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            ItemTemplateData template;
            if (ItemTemplateData.Templates.TryGetValue(13, out template))
            {
                var bread = new ItemData(template);
                ch.Room.items.Insert(0, bread);
                bread.Room = ch.Room;
                ch.send("You create a loaf of bread out of thin air.\n\r");
                ch.Act("$n creates a loaf of bread out of thin air.\n\r", type: ActType.ToRoom);
            }
        }

        public static void SpellDetectInvis(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("They can already see the invisible.\n\r");
                else
                    ch.send("You can already see the invisible.\n\r");
                return;
            }
            else
            {

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.DetectInvis);
                affect.duration = 10 + level / 4;
                affect.displayName = "detect invisibility";
                affect.endMessage = "You can no longer see the invisible.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                victim.AffectToChar(affect);


                victim.send("Your eyes tingle.\n\r");
                if (ch != victim)
                    ch.send("Ok.\n\r");
            }
        }
        public static void SpellInfravision(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("They can already see through the darkness.\n\r");
                else
                    ch.send("You can already see through the darkness.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.Infrared);
                affect.duration = 10 + level / 4;
                affect.displayName = "Infravision";
                affect.endMessage = "You can no longer see through the darkness.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                victim.AffectToChar(affect);

                victim.send("Your eyes briefly glow red.\n\r");
                if (ch != victim)
                    ch.send("Ok.\n\r");
            }
        }

        public static void SpellInvis(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            SkillSpell fog = SkillSpell.SkillLookup("faerie fog");
            SkillSpell fire = SkillSpell.SkillLookup("faerie fire");

            if (victim.IsAffected(fog) || victim.IsAffected(fire) || victim.IsAffected(AffectFlags.FaerieFire))
            {
                if (victim != ch)
                    ch.send("They can't become invisible while glowing.\n\r");
                else
                    ch.send("You cannot become invisible while glowing.\n\r");
                return;
            }

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (victim != ch)
                    ch.send("They are already invisible.\n\r");
                else
                    ch.send("You are already invisible.");
                return;
            }
            else
            {
                victim.send("You fade out of existence.\n\r");
                victim.Act("$n fades out of existence.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.Invisible);
                affect.duration = 10 + level / 4;
                affect.displayName = "invisibility";
                affect.endMessage = "You fade into existence.\n\r";
                affect.endMessageToRoom = "$n fades back into existence.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                victim.AffectToChar(affect);
            }
        }

        public static void AffectPlagueTick(Character ch, AffectData affect)
        {
            Combat.Damage(ch, ch, Math.Max(affect.level, 12), affect.skillSpell, WeaponDamageTypes.Disease, affect.ownerName);
        }

        public static void AffectPoisonTick(Character ch, AffectData affect)
        {
            Combat.Damage(ch, ch, Math.Max(affect.level / 2, 8), affect.skillSpell, WeaponDamageTypes.Poison, affect.ownerName);
        }

        public static void AffectAlcoholPoisonTick(Character ch, AffectData affect)
        {
            Combat.Damage(ch, ch, Math.Max(affect.level / 5, 5), affect.skillSpell, WeaponDamageTypes.Poison, affect.ownerName);
        }

        public static void SpellPoison(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch.Fighting;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                ch.send("They are already poisoned.\n\r");
                return;
            }
            else
            {
                if (SavesSpell(ch_level, victim, WeaponDamageTypes.Poison))
                {
                    victim.Act("$n turns slightly green, but it passes.", type: ActType.ToRoom);
                    victim.send("You feel momentarily ill, but it passes.\n\r");
                    return;
                }

                affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = spell;
                affect.level = ch_level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Strength;
                affect.flags.Add(AffectFlags.Poison);
                affect.duration = 3;
                affect.modifier = -3;
                affect.displayName = "Poisoned";
                affect.endMessage = "You feel less sick.\n\r";
                affect.endMessageToRoom = "$n starts looking less sick.\n\r";
                affect.affectType = AffectTypes.Malady; //castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                victim.AffectToChar(affect);
                victim.send("You feel very sick.\n\r");
                victim.Act("$n looks very ill.", null, null, null, ActType.ToRoom);
            }
        }
        public static void SpellWordOfRecall(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;
            RoomData recallroom;
            if ((recallroom = victim.GetRecallRoom()) == null) //!RoomData.rooms.TryGetValue(3001, out recallroom))
            {
                ch.send("You can't seem to find home.\n\r");
                return;
            }
            victim.send("Your vision swirls a moment as you are transported to another place.\n\r");
            victim.Act("$n's disappears.", null, null, null, ActType.ToRoom);

            if (victim.Fighting != null)
                Combat.StopFighting(victim, true);

            ch.MovementPoints /= 2;

            victim.RemoveCharacterFromRoom();
            victim.AddCharacterToRoom(recallroom);

            victim.Act("$n appears in the room.", null, null, null, ActType.ToRoom);
            //Character.DoLook(victim, "auto");
        }
        public static void SpellRemoveCurse(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            if (target == TargetIsType.targetItem)
            {
                if (item != null)
                {
                    if (item.extraFlags.ISSET(ExtraFlags.NoRemove) || item.extraFlags.ISSET(ExtraFlags.NoDrop))
                    {
                        item.extraFlags.REMOVEFLAG(ExtraFlags.NoRemove);
                        item.extraFlags.REMOVEFLAG(ExtraFlags.NoDrop);
                        ch.Act("$p glows white as its curse is lifted.", null, item, null, ActType.ToChar);
                        ch.Act("$p glows white as its curse is lifted.", null, item, null, ActType.ToRoom);
                    }
                    else
                        ch.Act("$p isn't cursed.", null, item, null, ActType.ToChar);
                }
            }
            else
            {
                if (victim != null)
                {
                    int count = 0;
                    string victimname = "";
                    arguments = arguments.OneArgument(ref victimname);
                    if (!arguments.ISEMPTY())
                    {
                        item = victim.GetItemEquipment(arguments, ref count);
                        if (item == null)
                            item = victim.GetItemInventory(arguments, ref count);

                        if (item == null)
                        {
                            ch.send("You can't find it.\n\r");
                            return;
                        }

                        if (item.extraFlags.ISSET(ExtraFlags.NoRemove) || item.extraFlags.ISSET(ExtraFlags.NoDrop))
                        {
                            item.extraFlags.REMOVEFLAG(ExtraFlags.NoRemove);
                            item.extraFlags.REMOVEFLAG(ExtraFlags.NoDrop);
                            ch.Act("$p glows white as its curse is lifted.", null, item, null, ActType.ToChar);
                            ch.Act("$p glows white as its curse is lifted.", null, item, null, ActType.ToRoom);
                            return;
                        }
                        else
                        {
                            ch.Act("$p isn't cursed.", null, item, null, ActType.ToChar);
                            return;
                        }
                    }

                    var curse = victim.FindAffect(AffectFlags.Curse);// (from aff in victim.AffectsList where aff.flags.ISSET(AffectFlags.Curse) select aff).FirstOrDefault();

                    if (curse != null)
                    {
                        victim.AffectFromChar(curse, AffectRemoveReason.Cleansed);
                        ch.Act("You place your hand on $N's head for a moment and a look of relief passes over $M.", victim, null, null, ActType.ToChar);
                        ch.Act("$n places their hand on $N's head for a moment and a look of relief passes over $M.", victim, null, null, ActType.ToRoomNotVictim);
                        ch.Act("$n places their hand on your head for a moment and a feeling of relief passes over you.", victim, null, null, ActType.ToVictim);
                    }
                    else
                        ch.Act("$N isn't cursed in any way you can intervene.", victim, type: ActType.ToChar);
                }
            }
        }

        public static void SpellWrath(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            int dam;

            if (victim.Alignment != Alignment.Evil)
            {
                ch.Act("$N is unaffected by $n's heavenly wrath.", victim, null, null, ActType.ToRoomNotVictim);
                ch.Act("You are unaffected by $n's heavenly wrath.\n\r", victim, null, null, ActType.ToRoom);
                ch.send("The Gods do not enhance your wrath and frown on your actions.\n\r");
                return;
            }
            var level = ch_level;

            if (!ch.IsNPC && victim.Flags.ISSET(ActFlags.Undead))
                level += Utility.Random(2, level / 2);

            dam = Utility.dice(7, level, level + 10);

            if (SavesSpell(ch_level, victim, WeaponDamageTypes.Holy) || SavesSpell(ch_level + 5, victim, WeaponDamageTypes.Holy))
                dam /= 2;

            ch.Act("You call down the wrath of god upon $N.", victim, type: ActType.ToChar);
            ch.Act("$n calls down the wrath of god upon $N.", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n calls down the wrath of god upon you.", victim, type: ActType.ToVictim);
            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Holy);
            return;
        }

        public static void SpellHolyWrath(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch)
                    SpellWrath(castType, spell, ch_level, ch, vict, item, arguments, TargetIsType.targetChar);
            }
        }

        public static void SpellFlamestrike(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            int dam;

            dam = Utility.dice(4, ch_level, ch_level);

            if (SavesSpell(ch_level, victim, WeaponDamageTypes.Fire))
                dam /= 2;
            //damage_old(ch, victim, dam, spell, DAM_FIRE, TRUE);

            //dam = utility.dice(6 + ch_level / 2, 8);
            ch.Act("You call down a pillar of fire upon $N.", victim, type: ActType.ToChar);
            ch.Act("$n calls down a pillar of fire upon $N.", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n calls down a pillar of fire upon you.", victim, type: ActType.ToVictim);
            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Fire);

            //if (number_range(0, 3) != 0)
            //    return;
            //spell_curse(gsn_curse, level, ch, (void*)victim, TARGET_CHAR);
            return;
        }

        public static void SpellSanctuary(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Sanctuary))
            {
                if (victim != ch)
                    ch.send("They are already protected by a white aura.\n\r");
                else
                    ch.send("You are already protected by a white aura.");
                return;
            }
            else
            {
                if (victim.IsAffected(AffectFlags.Haven)) victim.AffectFromChar(victim.FindAffect(AffectFlags.Haven), AffectRemoveReason.WoreOff);
                victim.send("A white aura surrounds you.\n\r");
                victim.Act("A white aura surrounds $n.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.Sanctuary);
                affect.duration = 10 + level / 4;
                affect.displayName = "sanctuary";
                affect.endMessage = "Your white aura fades.\n\r";
                affect.endMessageToRoom = "$n's white aura fades.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);

            }
        }
        public static void SpellShield(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                ch.send("You aren't ready to cast a shield again.\n\r");
                return;
            }
            else if(victim.IsAffected(AffectFlags.Shield))
            {
                if (victim != ch)
                    ch.send("They are already protected by a shield.\n\r");
                else
                    ch.send("You are already protected by a shield.");
                return;
            }
            else
            {
                victim.send("A magical shield surrounds you.\n\r");
                victim.Act("A magical shield surrounds $n.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.displayName = "shield cooldown";
                affect.duration = 24;
                affect.endMessage = "You feel ready to cast shield again.";
                victim.AffectToChar(affect);

                affect.flags.Add(AffectFlags.Shield);
                affect.duration = 12 + level / 6;
                affect.location = ApplyTypes.AC;
                affect.modifier = -30 - level / 5;
                affect.endMessage = "Your magical shield fades.\n\r";
                affect.endMessageToRoom = "$n's magical shield fades.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
            }
        }

        //public static void SpellProtectionGood(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        //{
        //    AffectData affect;
        //    if (victim == null)
        //        victim = ch;

        //    if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.ProtectionGood))
        //    {
        //        if (victim != ch)
        //            ch.send("They are already protected from good.\n\r");
        //        else
        //            ch.send("You are already protected from good.");
        //        return;
        //    }
        //    else
        //    {
        //        victim.send("You are protected from good.\n\r");
        //        victim.Act("$n is protected from good.", type: ActType.ToRoom);
        //        affect = new AffectData();
        //        affect.skillSpell = spell;
        //        affect.level = level;
        //        affect.location = ApplyTypes.None;
        //        affect.where = AffectWhere.ToAffects;
        //        affect.flags.Add(AffectFlags.ProtectionGood);
        //        affect.duration = 10 + level / 4;
        //        affect.displayName = "protection good";
        //        affect.endMessage = "You feel less protected.\n\r";
        //        affect.endMessageToRoom = "$n's protection wanes.\n\r";
        //        affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

        //        victim.AffectToChar(affect);

        //    }
        //}

        //public static void SpellProtectionEvil(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        //{
        //    AffectData affect;
        //    if (victim == null)
        //        victim = ch;

        //    if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.ProtectionEvil))
        //    {
        //        if (victim != ch)
        //            ch.send("They are already protected from evil.\n\r");
        //        else
        //            ch.send("You are already protected from evil.");
        //        return;
        //    }
        //    else
        //    {
        //        victim.send("You feel more protected from evil.\n\r");
        //        victim.Act("$n is protected from evil.", type: ActType.ToRoom);
        //        affect = new AffectData();
        //        affect.skillSpell = spell;
        //        affect.level = level;
        //        affect.location = ApplyTypes.None;
        //        affect.where = AffectWhere.ToAffects;
        //        affect.flags.Add(AffectFlags.ProtectionEvil);
        //        affect.duration = 10 + level / 4;
        //        affect.displayName = "protection evil";
        //        affect.endMessage = "Your feel less protected.\n\r";
        //        affect.endMessageToRoom = "$n's protection wanes.\n\r";
        //        affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

        //        victim.AffectToChar(affect);
        //    }
        //}
        public static void SpellWaterShield(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Watershield))
            {
                if (victim != ch)
                    ch.send("They are already protected by your water shield.\n\r");
                else
                    ch.send("You are already protected by water shield.");
                return;
            }
            else
            {
                victim.send("A magical water shield surrounds you.\n\r");
                victim.Act("A magical water shield surrounds $n.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.displayName = "water shield";
                affect.duration = 24;
                affect.endMessage = "Your magical shield of water fades.\n\r";
                affect.endMessageToRoom = "$n's magical shield of water fades.\n\r";
                affect.endMessage = "You feel ready to cast water shield again.";
                affect.DamageTypes.Add(WeaponDamageTypes.Drowning);
                affect.where = AffectWhere.ToImmune;
                victim.AffectToChar(affect);
            }
        }
        public static void SpellEarthShield(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Earthshield))
            {
                if (victim != ch)
                    ch.send("They are already protected by your earth shield.\n\r");
                else
                    ch.send("You are already protected by earth shield.");
                return;
            }
            else
            {
                victim.send("A magical shield of surrounds you.\n\r");
                victim.Act("A magical shield of earth surrounds $n.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.displayName = "earth shield";
                affect.duration = 24;
                affect.endMessage = "Your magical shield of earth fades.\n\r";
                affect.endMessageToRoom = "$n's magical shield of earth fades.\n\r";
                affect.endMessage = "You feel ready to cast earth shield again.";
                affect.DamageTypes.Add(WeaponDamageTypes.Bash);
                affect.where = AffectWhere.ToImmune;
                victim.AffectToChar(affect);
            }
        }
        public static void SpellAirShield(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Airshield))
            {
                if (victim != ch)
                    ch.send("They are already protected by your air shield.\n\r");
                else
                    ch.send("You are already protected by air shield.");
                return;
            }
            else
            {
                victim.send("A magical shield of air surrounds you.\n\r");
                victim.Act("A magical shield of air surrounds $n.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.displayName = "air shield";
                affect.duration = 24;
                affect.endMessage = "Your magical shield of air fades.\n\r";
                affect.endMessageToRoom = "$n's magical shield of air fades.\n\r";
                affect.endMessage = "You feel ready to cast air shield again.";
                affect.DamageTypes.Add(WeaponDamageTypes.Air);
                affect.where = AffectWhere.ToImmune;
                victim.AffectToChar(affect);
            }
        }
        public static void SpellFireShield(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Fireshield))
            {
                if (victim != ch)
                    ch.send("They are already protected by your fire shield.\n\r");
                else
                    ch.send("You are already protected by fire shield.");
                return;
            }
            else
            {
                victim.send("A magical shield of fire surrounds you.\n\r");
                victim.Act("A magical shield of fire surrounds $n.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.displayName = "fire shield";
                affect.duration = 24;
                affect.endMessage = "Your magical shield of fire fades.\n\r";
                affect.endMessageToRoom = "$n's magical shield of fire fades.\n\r";
                affect.endMessage = "You feel ready to cast fire shield again.";
                affect.DamageTypes.Add(WeaponDamageTypes.Fire);
                affect.where = AffectWhere.ToImmune;
                victim.AffectToChar(affect);
            }
        }
        public static void SpellLightningShield(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Lightningshield))
            {
                if (victim != ch)
                    ch.send("They are already protected by your lightning shield.\n\r");
                else
                    ch.send("You are already protected by lightning shield.");
                return;
            }
            else
            {
                victim.send("A magical shield of lightning crackles around you.\n\r");
                victim.Act("A magical shield of lightning crackles around $n.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.displayName = "lightning shield";
                affect.duration = 24;
                affect.endMessage = "Your magical shield of lightning fades.\n\r";
                affect.endMessageToRoom = "$n's magical shield of lightning fades.\n\r";
                affect.endMessage = "You feel ready to cast lightning shield again.";
                affect.DamageTypes.Add(WeaponDamageTypes.Lightning);
                affect.where = AffectWhere.ToImmune;
                victim.AffectToChar(affect);
            }
        }
        public static void SpellFrostShield(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Frostshield))
            {
                if (victim != ch)
                    ch.send("They are already protected by your frost shield.\n\r");
                else
                    ch.send("You are already protected by frost shield.");
                return;
            }
            else
            {
                victim.send("You sence tingling and numbing in your skin.\n\r");
                victim.Act("A magical shield of frost surrounds $n.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.displayName = "frost shield";
                affect.duration = 24;
                affect.endMessage = "Your magical shield of frost fades.\n\r";
                affect.endMessageToRoom = "$n's magical shield of frost fades.\n\r";
                affect.endMessage = "You feel ready to cast frost shield again.";
                affect.DamageTypes.Add(WeaponDamageTypes.Cold);
                affect.where = AffectWhere.ToImmune;
                victim.AffectToChar(affect);
            }
        }
        public static void SpellCureBlindness(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null) victim = ch;

            var blindness = (from aff in victim.AffectsList where aff.flags.ISSET(AffectFlags.Blind) && (aff.affectType == AffectTypes.Malady || aff.affectType == AffectTypes.Spell || aff.affectType == AffectTypes.Commune) select aff).FirstOrDefault();

            if (blindness != null)
            {
                victim.AffectFromChar(blindness, AffectRemoveReason.Cleansed);

                if (ch != victim)
                {
                    ch.Act("You place your hand over $N's eyes for a moment and the cloudy look filling them goes away.", victim, null, null, ActType.ToChar);
                    ch.Act("$n places their hand over $N's eyes for a moment and the cloudy look filling them goes away.", victim, null, null, ActType.ToRoomNotVictim);
                    ch.Act("$n places their hand over your eyes for a moment.", victim, null, null, ActType.ToVictim);
                }
                else
                    ch.send("OK.\n\r");
            }
            else
                ch.Act("$N isn't blinded in a way you can cure.", victim, type: ActType.ToChar);
        }

        public static void SpellPassDoor(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.PassDoor))
            {
                if (victim != ch)
                    ch.send("They are already out of phase.\n\r");
                else
                    ch.send("You are already out of phase.\n\r");
                return;
            }
            else
            {
                victim.send("You phase in and out of existence.\n\r");
                victim.Act("$n phases in and out of existence.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.PassDoor);
                affect.duration = 5 + level / 3;
                affect.displayName = "pass door";
                affect.endMessage = "Your feel less translucent.\n\r";
                affect.endMessageToRoom = "$n appears less translucent.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                victim.AffectToChar(affect);

            }
        }
        static bool CheckSpellcraft(Character ch, string spell)
        {
            return CheckSpellcraft(ch, SkillSpell.SkillLookup(spell));
        }

        static bool CheckSpellcraft(Character ch, SkillSpell spell)
        {
            int chance;
            ItemData symbol = null;
            //ItemData ring = null;

            if (spell == null) return false;
            chance = ch.GetSkillPercentage("spellcraft");
            if (chance <= 1)
                return false;
            symbol = ch.GetEquipment(WearSlotIDs.Held);
            if (symbol != null && symbol.Vnum == 14739)
                chance += 15;

            chance /= 5;

            int spellcraftII = ch.GetSkillPercentage("spellcraft II");
            if (spellcraftII <= 75)
                chance += 5;
            if (spellcraftII <= 85)
                chance += 6;
            if (spellcraftII <= 95)
                chance += 7;
            if (spellcraftII == 100)
                chance += 8;




            //ring = ch.GetEquipment(WearSlotIDs.LeftFinger);
            //if (ring != null && ring.Vnum == OBJ_VNUM_WIZARDRY_1)
            //    chance += 2;
            //else if (ring != null && ring.Vnum == OBJ_VNUM_WIZARDRY_2)
            //    chance += 5;
            //ring = ch.GetEquipment(WearSlotIDs.RightFinger);
            //if (ring != null && ring.Vnum == OBJ_VNUM_WIZARDRY_1)
            //    chance += 2;
            //else if (ring != NUnullLL && ring.Vnum == OBJ_VNUM_WIZARDRY_2)
            //    chance += 5;

            if (Utility.NumberPercent() > chance)
            {
                ch.CheckImprove("spellcraft", false, 6);
                if (ch.GetSkillPercentage("spellcraft II") != 0)
                {
                    ch.CheckImprove("spellcraft", false, 6);
                }
                return false;

            }

            ch.CheckImprove("spellcraft II", true, 6);

            ch.CheckImprove("spellcraft", true, 6);

            return true;
        }

        public static int SpellcraftDamagDice(int num, int die)
        {
            int dam;
            if (num == 0 || die == 0)
                return 0;

            if (die == 1)
                return num;
            else if (die == 2)
                return (num * 2);
            else if (die == 3)
                return (num * Utility.Random(2, 3));

            dam = (num * die) / 2;
            dam += Utility.dice(num / 2, die);
            return dam;
        }

        public static bool SavesSpell(int level, Character victim, WeaponDamageTypes damageType)
        {
            int save;
            //Character ch;
            //ItemData obj;

            if (victim == null) return false;

            save = 35 + (victim.Level - level) * 5 - victim.SavingThrow;
            if (victim.IsAffected(AffectFlags.Berserk))
                save += victim.Level / 5;

            if (victim.IsNPC)
                save = victim.Level / 4;  /* simulate npc saving throw */

            switch (victim.CheckImmune(damageType))
            {
                case ImmuneStatus.Immune: return true;
                case ImmuneStatus.Resistant: save += 2; break;
                case ImmuneStatus.Vulnerable: save -= 2; break;
            }


            //obj = get_eq_char(victim, WEAR_HOLD);
            //if (obj != NULL
            //    && obj->pIndexData->vnum == 19002
            //    && dam_type == gsn_charm_person)
            //    save += 3;

            //if ((obj = get_eq_char(victim, WEAR_BODY)) != NULL
            //    && obj->pIndexData->vnum == 14005)
            //{
            //    if (dam_type == DAM_NEGATIVE)
            //        level -= 3;
            //    else if (dam_type == DAM_POISON)
            //        level -= 2;
            //    else if (dam_type == DAM_COLD)
            //        level -= 3;
            //    else if (dam_type == DAM_LIGHT)
            //        level += 2;
            //}

            //if (!victim.IsNPC && victim.Guild != null && victim.Guild.fMana)
            //    save = 9 * save / 10;
            save = Utility.URANGE(5, save, 98);
            return Utility.NumberPercent() < save;
        }

        public static void SpellSummon(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {


            if ((victim = Character.GetCharacterWorld(ch, arguments)) == null
                || victim == ch
                || victim.Room == null
                || (!victim.IsNPC && victim.Room.Area != ch.Room.Area)
                || ch.Room.flags.ISSET(RoomFlags.Safe)
                //|| ch->in_room->guild != 0
                //|| victim->in_room->guild != 0
                || (victim.IsNPC && victim.IsAffected(AffectFlags.Charm) && victim.Room.Area != ch.Room.Area)
                || victim.Room.flags.ISSET(RoomFlags.Safe)
                || victim.Room.flags.ISSET(RoomFlags.Private)
                || victim.Room.flags.ISSET(RoomFlags.Solitary)
                || victim.Room.flags.ISSET(RoomFlags.NoRecall)
                || ch.Room.flags.ISSET(RoomFlags.NoRecall)
                || (victim.IsNPC && victim.Flags.ISSET(ActFlags.Aggressive))
                || victim.Level >= (level + 10)
                || (!victim.IsNPC && victim.Level >= Game.LEVEL_IMMORTAL)
                //|| (ch->in_room->vnum == 23610)  /* Enforcer entrance */
                || victim.Fighting != null
                //|| (victim.IsNPC && ISSET(victim->imm_flags, IMM_SUMMON))
                || (victim.IsNPC && victim.Flags.ISSET(ActFlags.Shopkeeper))
                //|| (victim.IsNPC && victim->spec_fun != NULL)
                //|| (!victim.IsNPC && !can_pk(ch, victim) && ISSET(victim->act, PLR_NOSUMMON))
                || (SavesSpell(level, victim, WeaponDamageTypes.Other)))
            {
                ch.send("You failed.\n\r");
                return;
            }

            victim.Act("$n disappears suddenly.", type: ActType.ToRoom);
            victim.RemoveCharacterFromRoom();
            ch.Act("$n has summoned you!", victim, type: ActType.ToVictim);
            victim.AddCharacterToRoom(ch.Room);
            victim.Act("$n arrives suddenly.", type: ActType.ToRoom);
            
            //Character.DoLook(victim, "auto");
            return;
        }
        public static void SpellTeleport(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (ch.Room == null
                || (!ch.IsNPC && ch.Room.flags.ISSET(RoomFlags.NoRecall))
                || (!ch.IsNPC && ch.Fighting != null)
                )
            {
                ch.send("You failed.\n\r");
                return;
            }
            var room = RoomData.Rooms.Values.SelectRandom();

            if (room == null || !(ch.IsImmortal || ch.IsNPC || (ch.Level <= room.MaxLevel && ch.Level >= room.MinLevel)))
            {
                ch.send("You failed.\n\r");
                return;
            }

            ch.Act("$n vanishes!", type: ActType.ToRoom);
            ch.RemoveCharacterFromRoom();
            ch.AddCharacterToRoom(room);
            ch.Act("$n slowly fades into existence.", type: ActType.ToRoom);
            //Character.DoLook(ch, "auto");
            return;
        }
        public static void SpellFireball(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act("The air surrounding you grows into a heat wave!", null, null, null, ActType.ToChar);
            ch.Act("$n makes the air grow into a heat wave!", null, null, null, ActType.ToRoom);

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    var dam = ch.GetDamage(level, 1, 2);

                    if (SavesSpell(level, victim, WeaponDamageTypes.Fire))
                        dam /= 2;

                    if (spellcraft) dam += level;

                    Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Fire);
                    ch.CheckImprove(spell, true, 1);
                }
            }
        } // end fireball
        public static void SpellConeOfCold(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act("As you bring your hands together, a cone of freezing ice shoots out from your fingertips.", null, null, null, ActType.ToChar);
            ch.Act("As $N brings $s hands together, a cone of freezing ice shoots out from $s fingertips.", null, null, null, ActType.ToRoom);

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    var dam = ch.GetDamage(level, 1, 2);

                    if (spellcraft)
                        dam += level;
                    if (SavesSpell(level, victim, WeaponDamageTypes.Cold))
                        dam /= 2;

                    Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Cold);
                    ch.CheckImprove(spell, true, 1);
                }
            }
        } // end cone of cold
        public static void SpellControlledFireball(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act("The air surrounding you grows into a giant heat wave!", null, null, null, ActType.ToChar);
            ch.Act("$n makes the air grow into a giant heat wave!", null, null, null, ActType.ToRoom);

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (((vict.Fighting != null && vict.Fighting.IsSameGroup(ch)) || ch.Fighting == vict) && vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    var dam = ch.GetDamage(level, 1, 2);

                    if (SavesSpell(level, victim, WeaponDamageTypes.Fire))
                        dam /= 2;

                    if (spellcraft) dam += level;

                    Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Fire);
                    ch.CheckImprove(spell, true, 1);
                }
            }
        } // end controlled fireball
        public static void SpellNova(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.Inside));
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act("You emit a soundless scream as waves of heat roll out from your mouth.", null, null, null, ActType.ToChar);
            ch.Act("With hands outstretched and face to the sky, " +
                "$n opens his mouth in a soundless scream as waves of heat roll out from $s.".WrapText(), null, null, null, ActType.ToRoom);

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (((vict.Fighting != null && vict.Fighting.IsSameGroup(ch)) || ch.Fighting == vict) && vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    var dam = ch.GetDamage(level, 1, 2);

                    if (spellcraft)
                        dam += level;
                    if (checkSector)
                        dam *= 2;
                    if (SavesSpell(level, victim, WeaponDamageTypes.Fire))
                        dam /= 2;

                    Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Fire);
                    ch.CheckImprove(spell, true, 1);
                }
            }
        } // end nova
        public static void SpellAvalanche(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.Mountain) || (sector == SectorTypes.Cave) || (sector == SectorTypes.Underground));
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act("You summon an avalanche from the surrounding rocks!", null, null, null, ActType.ToChar);
            ch.Act("$n summons an avalanche from the surrounding rocks!", null, null, null, ActType.ToRoom);

            if (checkSector) ch.Act("You summon powerful avalanche!", null, null, null, ActType.ToChar);

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    var dam = ch.GetDamage(level, .5f, 1);  //dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                    if (SavesSpell(level, victim, WeaponDamageTypes.Bash))
                        dam /= 2;

                    if (checkSector)
                        dam *= 2;

                    if (spellcraft) dam += level;

                    Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Bash);
                    ch.CheckImprove(spell, true, 1);
                }
            }
        } // end avalanche

        public static void SpellMagicMissile(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {

            var dam = ch.GetDamage(level, .5f, 2);  //dam = Utility.Random(dam_each[level] / 2, dam_each[level] * 2);

            if (CheckSpellcraft(ch, spell))
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Energy))
                dam /= 2;
            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Energy);
            return;
        }

        public static void SpellDetectMagic(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("They can already sense magic.\n\r");
                else
                    ch.send("You can already sense magic.\n\r");
                return;
            }
            else
            {

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.DetectMagic);
                affect.duration = 10;
                affect.displayName = "detect magic";
                affect.endMessage = "You can no longer sense magic.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);


                victim.send("Your eyes feel different.\n\r");
                if (ch != victim)
                    ch.send("Ok.\n\r");
            }
        }

        public static void SpellDispelMagic(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null) victim = ch;
            bool stripped = false;
            foreach (var aff in victim.AffectsList.ToArray())
            {
                if (aff.affectType == AffectTypes.Spell || aff.affectType == AffectTypes.Commune)
                {
                    var striplevel = aff.level - ch.Level;
                    var chance = 75;
                    if (striplevel < 0)
                        chance += 25;
                    else if (striplevel > 5)
                        chance -= 25;

                    if (Utility.NumberPercent() < chance)
                    {
                        stripped = true;
                        victim.AffectFromChar(aff, AffectRemoveReason.Cleansed);

                        // remove all of the affect of that type - now done in affectfromchar
                        //foreach (var affother in victim.AffectsList.ToArray())
                        //{
                        //    if (affother != aff && affother.skillSpell == aff.skillSpell)
                        //        victim.AffectFromChar(affother);
                        //}
                    }
                }
            }
            if (stripped)
                ch.send("Ok.\n\r");
            else
                ch.send("You failed.\n\r");
        }

        public static void SpellCureDisease(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null) victim = ch;
            bool stripped = false;

            foreach (var aff in victim.AffectsList.ToArray())
            {
                if ((aff.affectType == AffectTypes.Malady || aff.affectType == AffectTypes.Spell || aff.affectType == AffectTypes.Commune) &&
                    (aff.skillSpell == SkillSpell.SkillLookup("plague") || aff.flags.ISSET(AffectFlags.Plague)))
                {
                    var striplevel = aff.level - ch.Level;
                    var chance = 75;
                    if (striplevel < 0)
                        chance += 25;
                    else if (striplevel > 5)
                        chance -= 25;

                    if (Utility.NumberPercent() < chance)
                    {
                        stripped = true;
                        victim.AffectFromChar(aff, AffectRemoveReason.Cleansed);

                        // remove all of the affect of that type
                        foreach (var affother in victim.AffectsList.ToArray())
                        {
                            if (affother != aff && affother.skillSpell == aff.skillSpell)
                                victim.AffectFromChar(affother, AffectRemoveReason.Cleansed);
                        }
                    }
                    else
                        ch.send("You fail to remove {0}.\n\r", aff.displayName);
                }
            }
            if (stripped)
                ch.send("Ok.\n\r");
            else
                ch.send("You failed.\n\r");
        }

        public static void SpellCurePoison(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null) victim = ch;
            bool stripped = false;

            foreach (var aff in victim.AffectsList.ToArray())
            {
                if ((aff.affectType == AffectTypes.Malady || aff.affectType == AffectTypes.Spell || aff.affectType == AffectTypes.Commune) &&
                    (aff.skillSpell == SkillSpell.SkillLookup("poison") || aff.flags.ISSET(AffectFlags.Poison)))
                {
                    var striplevel = aff.level - ch.Level;
                    var chance = 75;
                    if (striplevel < 0)
                        chance += 25;
                    else if (striplevel > 5)
                        chance -= 25;

                    if (Utility.NumberPercent() < chance)
                    {
                        stripped = true;
                        victim.AffectFromChar(aff, AffectRemoveReason.Cleansed);

                        // remove all of the affect of that type
                        foreach (var affother in victim.AffectsList.ToArray())
                        {
                            if (affother != aff && affother.skillSpell == aff.skillSpell)
                                victim.AffectFromChar(affother, AffectRemoveReason.Cleansed);
                        }
                    }
                    else
                        ch.send("You fail to remove {0}.\n\r", aff.displayName);
                }
            }
            if (stripped)
                ch.send("Ok.\n\r");
            else
                ch.send("You failed.\n\r");
        }

        public static void SpellStoneskin(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("They already have stoneskin.\n\r");
                else
                    ch.send("You already have stoneskin.\n\r");
                return;
            }
            else
            {

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.Armor;
                affect.duration = 10 + level / 4;
                affect.modifier = -30 - ((level / 2) - 10);
                affect.displayName = "stoneskin";
                affect.endMessage = "Your stoneskin fades..\n\r";
                affect.endMessageToRoom = "$n's stoneskin fades.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("Your skin turns to stone.\n\r");
                victim.Act("$n's skin turns to stone.\n\r", type: ActType.ToRoom);
            }
        }
        public static void SpellSheenOfStone(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("They're skin already glistens with a sheen of stone.\n\r");
                else
                    ch.send("Your skin already glistens with a sheen of stone.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.Armor;
                affect.duration = 10 + level / 4;
                affect.modifier = -60 - ((level / 2) - 20);
                affect.displayName = "Sheen of Stone";
                affect.endMessage = "Your sheen of stone fades..\n\r";
                affect.endMessageToRoom = "$n's sheen of stone fades.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("Your skin glistens with a sheen of stone.\n\r");
                victim.Act("$n's skin glistnes with a sheen of stone.\n\r", type: ActType.ToRoom);
            }
        }
        public static void SpellFly(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Flying))
            {
                if (ch != victim)
                    ch.send("They are already flying.\n\r");
                else
                    ch.send("You are already flying.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.None;
                affect.duration = level / 2;
                affect.modifier = 0;
                affect.flags.SETBIT(AffectFlags.Flying);
                affect.displayName = "fly";
                affect.endMessage = "You fall back to the ground.\n\r";
                affect.endMessageToRoom = "$n falls back to the ground.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("Your feet lift off the ground as you begin to fly.\n\r");
                victim.Act("$n's feet lift off the ground as they begin to fly.\n\r", type: ActType.ToRoom);
            }
        }
        public static void SpellWaterBreathing(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.WaterBreathing))
            {
                if (ch != victim)
                    ch.send("They already have gills.\n\r");
                else
                    ch.send("Your gills already give you water breathing..\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.None;
                affect.duration = level / 2;
                affect.modifier = 0;
                affect.flags.SETBIT(AffectFlags.WaterBreathing);
                affect.displayName = "water breathing";
                affect.endMessage = "Your gills disappear, preventing you from breath underwater anymore.\n\r";
                affect.endMessageToRoom = "$n's gills disappear.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("Miniature gills apear on the side of your face.\n\r");
                victim.Act("Miniature gills appear on the side of $n's face.\n\r", type: ActType.ToRoom);
            }
        }

        public static void SpellGiantStrength(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("Their strength is already increased.\n\r");
                else
                    ch.send("You already have the strength of giants.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.Strength;
                affect.where = AffectWhere.ToAffects;
                affect.duration = level / 2;
                affect.modifier = 3 + (level / 7);
                affect.displayName = "giant strength";
                affect.endMessage = "Your giant strength fades..\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("You feel the strength of giants enter you.\n\r");
                victim.Act("$n appears to be stronger.\n\r", type: ActType.ToRoom);
            }
        }

        public static void SpellPlague(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch.Fighting;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Plague))
            {
                ch.send("They are already afflicted with the plague.\n\r");
                return;
            }
            else
            {
                if (SavesSpell(ch_level, victim, WeaponDamageTypes.Poison))
                {
                    victim.Act("$n starts to break out with sores, but recovers.", type: ActType.ToRoom);
                    victim.send("You start to break out with sores, but recover.\n\r");
                    return;
                }

                affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = spell;
                affect.level = ch_level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Strength;
                affect.flags.Add(AffectFlags.Plague);
                affect.duration = Math.Max(3, ch_level / 4);
                affect.modifier = -5;
                affect.displayName = "plague";
                affect.endMessage = "Your sores disappear.\n\r";
                affect.endMessageToRoom = "$n's sores disappear.\n\r";
                affect.affectType = AffectTypes.Malady; // castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                victim.AffectToChar(affect);
                victim.send("You break out in sores.\n\r");
                victim.Act("$n breaks out in sores.", null, null, null, ActType.ToRoom);
            }
        }

        public static void SpellArcaneNuke(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    var damage = Utility.dice(25, 4, 20);

                    ch.Act("$n's arcane nuke hits $N!", vict, null, null, ActType.ToRoomNotVictim);
                    ch.Act("$n's arcane nuke hits you!", vict, null, null, ActType.ToVictim);
                    ch.Act("Your arcane nuke hits $N!", vict, null, null, ActType.ToChar);

                    Combat.Damage(ch, vict, damage, spell, WeaponDamageTypes.Force);
                }
            }
        }

        public static void SpellIdentify(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var buffer = new StringBuilder();
            buffer.Append(new string('-', 80) + "\n\r");
            buffer.AppendFormat("Object {0} can be referred to as '{1}'\n\rIt is of type {2} and level {3}\n\r", item.ShortDescription.TOSTRINGTRIM(), item.Name,
                string.Join(" ", (from flag in item.ItemType.Distinct() select flag.ToString())), item.Level);
            buffer.AppendFormat("It is worth {0} silver and weighs {1} pounds.\n\r", item.Value, item.Weight);

            if (item.ItemType.ISSET(ItemTypes.Weapon))
            {
                if (item.WeaponDamageType != null)
                    buffer.AppendFormat("Damage Type is {0}\n\r", item.WeaponDamageType.Keyword);
                buffer.AppendFormat("Weapon Type is {0} with damage dice of {1} (avg {2})\n\r", item.WeaponType.ToString(), item.DamageDice.ToString(), item.DamageDice.Average);

            }

            if (item.ItemType.ISSET(ItemTypes.Container))
            {
                buffer.AppendFormat("It can hold {0} pounds.", item.MaxWeight);
            }

            if (item.ItemType.ISSET(ItemTypes.Food))
            {
                buffer.AppendFormat("It is edible and provides {0} nutrition.\n\r", item.Nutrition);
            }

            if (item.ItemType.ISSET(ItemTypes.DrinkContainer))
            {
                buffer.AppendFormat("Nutrition {0}, Drinks left {1}, Max Capacity {2}, it is filled with '{3}'\n\r", item.Nutrition, item.Charges, item.MaxCharges, item.Liquid);
            }

            buffer.AppendFormat("It is made out of '{0}'\n\r", item.Material);
            if (item.timer > 0)
                buffer.AppendFormat("It will decay in {0} hours.\n\r", item.timer);

            if (item.ItemType.ISSET(ItemTypes.Armor) || item.ItemType.ISSET(ItemTypes.Clothing))
            {
                buffer.AppendFormat("It provides armor against bash {0}, slash {1}, pierce {2}, magic {3}\n\r", item.ArmorBash, item.ArmorSlash, item.ArmorPierce, item.ArmorExotic);
            }
            buffer.AppendFormat("It can be worn on {0} and has extra flags of {1}.\n\r", string.Join(", ", (from flag in item.wearFlags.Distinct() select flag.ToString())),
                string.Join(", ", (from flag in item.extraFlags.Distinct() select flag.ToString())));

            buffer.AppendFormat("Affects: \n   {0}\n\r", string.Join("\n   ", (from aff in item.affects where aff.@where == AffectWhere.ToObject select aff.location.ToString() + " " + aff.modifier)));

            if (item.ItemType.ISSET(ItemTypes.Staff) || item.ItemType.ISSET(ItemTypes.Wand) || item.ItemType.ISSET(ItemTypes.Scroll) || item.ItemType.ISSET(ItemTypes.Potion))
            {
                buffer.AppendFormat("It contains the following spells:\n\r   {0}", string.Join("\n   ", from itemspell in item.Spells select (itemspell.SpellName + " [lvl " + itemspell.Level + "]")) + "\n\r");
            }

            if (item.ItemType.ISSET(ItemTypes.Staff) || item.ItemType.ISSET(ItemTypes.Wand))
            {
                buffer.AppendFormat("It has {0} of {1} charges left\n\r", item.Charges, item.MaxCharges);
            }

            buffer.Append(new string('-', 80) + "\n\r");
            ch.send(buffer.ToString());

        } // end SpellIdentify

        public static void SpellLocateObject(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            using (new Character.Page(ch))
            {
                foreach (var locate in ItemData.Items)
                {
                    if (arguments.ISEMPTY() || locate.Name.IsName(arguments))
                    {
                        try
                        {
                            ch.send(locate.Display(ch) + " {0} {1}\n\r",
                                (locate.CarriedBy != null ? "held by" : (locate.Room != null ? "on the ground in" : (locate.Container != null ? "contained in" : ""))),
                                (locate.CarriedBy != null ? locate.CarriedBy.Display(ch) : (locate.Room != null ? (TimeInfo.IS_NIGHT && !locate.Room.NightName.ISEMPTY() ? locate.Room.NightName : locate.Room.Name) : (locate.Container != null ? locate.Container.Display(ch) : ""))));
                        }
                        catch (Exception ex)
                        {
                            Game.bug(ex.ToString());
                        }

                    }
                }
            }
        } // end locate object

        public static void SpellEnchantArmor(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (item == null)
            {
                ch.send("You don't see that here.\n\r");
                return;
            }
            else if (!item.ItemType.ISSET(ItemTypes.Armor) && !item.ItemType.ISSET(ItemTypes.Clothing))
            {
                ch.send("That isn't armor.\n\r");
                return;
            }
            else
            {
                // TODO Chance to fail / maybe destroy item?
                var aff = (from ia in item.affects where ia.skillSpell == spell && ia.location == ApplyTypes.AC select ia).FirstOrDefault();

                if (aff != null && aff.modifier >= 50)
                {
                    ch.Act("$p flares blindingly and then fades completely.\n\r", null, item);
                    ch.Act("$p flares blindingly and then fades completely.\n\r", null, item, null, ActType.ToRoom);

                    if (ch.GetEquipmentWearSlot(item) != null)
                    {
                        ch.AffectApply(aff, true);
                    }
                    item.affects.Remove(aff);
                    item.extraFlags.Remove(ExtraFlags.Glow);
                    item.extraFlags.Remove(ExtraFlags.Hum);

                }
                else if (aff != null)
                {
                    if (ch.GetEquipmentWearSlot(item) != null)
                    {
                        ch.AffectApply(aff, true);
                    }
                    aff.modifier += 10;
                    if (ch.GetEquipmentWearSlot(item) != null)
                    {
                        ch.AffectApply(aff);
                    }
                    ch.Act("$p glows blue briefly before fading slightly.\n\r", null, item);
                    ch.Act("$p glows blue briefly before fading slightly.\n\r", null, item, null, ActType.ToRoom);

                }
                else
                {
                    aff = new AffectData();
                    aff.skillSpell = spell;
                    aff.where = AffectWhere.ToObject;
                    aff.location = ApplyTypes.AC;
                    aff.modifier = 10;
                    aff.level = ch_level;
                    aff.duration = -1;
                    item.affects.Add(aff);
                    item.extraFlags.SETBIT(ExtraFlags.Glow);
                    item.extraFlags.SETBIT(ExtraFlags.Hum);
                    if (ch.GetEquipmentWearSlot(item) != null)
                    {
                        ch.AffectApply(aff);
                    }
                    ch.Act("$p glows blue briefly before fading slightly.\n\r", null, item);
                    ch.Act("$p glows blue briefly before fading slightly.\n\r", null, item, null, ActType.ToRoom);

                }
            }
        } // end enchant armor

        public static void SpellEnchantWeapon(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (item == null)
            {
                ch.send("You don't see that here.\n\r");
                return;
            }
            else if (!item.ItemType.ISSET(ItemTypes.Weapon))
            {
                ch.send("That isn't a weapon.\n\r");
                return;
            }
            else
            {
                // TODO Chance to fail / maybe destroy item?
                var aff = (from ia in item.affects where ia.skillSpell == spell && ia.location == ApplyTypes.Damroll select ia).FirstOrDefault();
                var aff2 = (from ia in item.affects where ia.skillSpell == spell && ia.location == ApplyTypes.Hitroll select ia).FirstOrDefault();
                var worn = ch.GetEquipmentWearSlot(item) != null;
                if (aff != null && aff.modifier >= 6)
                {
                    ch.Act("$p flares blindingly and then fades completely.\n\r", null, item);
                    ch.Act("$p flares blindingly and then fades completely.\n\r", null, item, null, ActType.ToRoom);

                    if (worn)
                    {
                        ch.AffectApply(aff, true);
                        ch.AffectApply(aff2, true);
                    }
                    item.affects.Remove(aff);
                    item.affects.Remove(aff2);
                    item.extraFlags.Remove(ExtraFlags.Glow);
                    item.extraFlags.Remove(ExtraFlags.Hum);

                }
                else if (aff != null)
                {
                    if (worn)
                    {
                        ch.AffectApply(aff, true);
                        ch.AffectApply(aff2, true);
                    }
                    aff.modifier += 1;
                    aff2.modifier += 1;
                    if (worn)
                    {
                        ch.AffectApply(aff);
                        ch.AffectApply(aff2);
                    }

                    ch.Act("$p glows blue briefly before fading slightly.\n\r", null, item);
                    ch.Act("$p glows blue briefly before fading slightly.\n\r", null, item, null, ActType.ToRoom);

                }
                else
                {
                    aff = new AffectData();
                    aff.skillSpell = spell;
                    aff.location = ApplyTypes.Damroll;
                    aff.modifier = 1;
                    aff.level = ch_level;
                    aff.duration = -1;
                    aff.where = AffectWhere.ToObject;

                    aff2 = new AffectData();
                    aff2.where = AffectWhere.ToObject;
                    aff2.skillSpell = spell;
                    aff2.location = ApplyTypes.Hitroll;
                    aff2.modifier = 1;
                    aff2.duration = -1;
                    aff2.level = ch_level;

                    item.affects.Add(aff);
                    item.affects.Add(aff2);

                    item.extraFlags.SETBIT(ExtraFlags.Glow);
                    item.extraFlags.SETBIT(ExtraFlags.Hum);
                    if (worn)
                    {
                        ch.AffectApply(aff);
                        ch.AffectApply(aff2);
                    }
                    ch.Act("$p glows blue briefly before fading slightly.\n\r", null, item);
                    ch.Act("$p glows blue briefly before fading slightly.\n\r", null, item, null, ActType.ToRoom);

                }
            }
        } // end enchant weapon

        public static void SpellCurse(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch.Fighting;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Curse))
            {
                ch.send("They are already cursed.\n\r");
                return;
            }
            else
            {
                if (SavesSpell(ch_level, victim, WeaponDamageTypes.Negative))
                {
                    victim.Act("$n turns to look uncfomfortable but recovers.", type: ActType.ToRoom);
                    victim.send("You feel momentarily uncomfortable but recover.\n\r");
                    return;
                }

                affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = spell;
                affect.level = ch_level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.None;
                affect.flags.Add(AffectFlags.Curse);
                affect.duration = ch_level / 4;
                affect.modifier = 0;
                affect.displayName = "Cursed";
                affect.endMessage = "You feel less unclean.\n\r";
                affect.endMessageToRoom = "$n looks less uncomfortable.\n\r";
                affect.affectType = AffectTypes.Malady; //castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                victim.AffectToChar(affect);
                victim.send("You feel unclean.\n\r");
                victim.Act("$n looks very uncomfortable.", null, null, null, ActType.ToRoom);
            }
        } // end curse

        public static void SpellWeaken(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch.Fighting;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Weaken))
            {
                ch.send("They are already weakened.\n\r");
                return;
            }
            else
            {
                if (SavesSpell(ch_level, victim, WeaponDamageTypes.Negative))
                {
                    victim.Act("$n looks weaker for a moment but recovers.", type: ActType.ToRoom);
                    victim.send("You feel weaker for a moment but recover.\n\r");
                    return;
                }

                affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = spell;
                affect.level = ch_level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Strength;
                affect.flags.Add(AffectFlags.Weaken);
                affect.duration = ch_level / 4;
                affect.modifier = -5;
                affect.displayName = "Weakened";
                affect.endMessage = "You feel stronger.\n\r";
                affect.endMessageToRoom = "$n looks like a weight has been lifted off of $m.\n\r";
                affect.affectType = AffectTypes.Malady;  //castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                victim.AffectToChar(affect);
                victim.send("You feel weaker.\n\r");
                victim.Act("$n looks weaker.", null, null, null, ActType.ToRoom);
            }
        } // end weaken

        public static void SpellCreateFood(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            ItemTemplateData template;
            if (ItemTemplateData.Templates.TryGetValue(13, out template))
            {
                var bread = new ItemData(template);
                ch.Room.items.Insert(0, bread);
                bread.Room = ch.Room;
                ch.send("You create a loaf of bread out of thin air.\n\r");
                ch.Act("$n creates a loaf of bread out of thin air.\n\r", type: ActType.ToRoom);
            }
        }

        public static void SpellCreateSpring(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            ItemTemplateData template;
            if (ItemTemplateData.Templates.TryGetValue(22, out template))
            {
                var spring = new ItemData(template);
                ch.Room.items.Insert(0, spring);
                spring.Room = ch.Room;
                spring.timer = 7;
                ch.send("You create a spring of water flowing out of the ground.\n\r");
                ch.Act("$n creates a spring of water flowing out of the ground.\n\r", type: ActType.ToRoom);
            }
        }

        public static void SpellCreateWater(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (item != null && !item.ItemType.ISSET(ItemTypes.DrinkContainer))
            {
                ch.send("That can't hold water.\n\r");
            }

            if (item == null)
            {
                ch.send("Fill what with water?");
            }
            else if (item.Charges != 0 && item.Liquid != "water")
            {
                ch.send("It still has something other than water in it.\n\r");
            }
            else
            {
                item.Liquid = "water";
                item.Charges = Math.Max(16, item.MaxCharges);
                item.Nutrition = Liquid.Liquids["water"].thirst;

                ch.Act("Water flows out of your finger into $p.\n\r", null, item, type: ActType.ToChar);
                ch.Act("Water flows out of $n's finger into $p.\n\r", null, item, type: ActType.ToRoom);
            }
        } // end create water

        public static void SpellFaerieFog(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !vict.IsAffected(spell) && !vict.IsAffected(AffectFlags.FaerieFire)) // && !ch.IsSameGroup(vict))
                {
                    if (vict.FindAffect(spell) != null)
                        continue;

                    ch.Act("$N is revealed by $n's faerie fog.", vict, null, null, ActType.ToRoomNotVictim);
                    ch.Act("$n's faerie fog reveals you.", vict, null, null, ActType.ToVictim);
                    ch.Act("Your faerie fog reveals $N.", vict, null, null, ActType.ToChar);
                    var affect = new AffectData();
                    affect.ownerName = ch.Name;
                    affect.skillSpell = spell;
                    affect.level = ch_level;
                    affect.location = ApplyTypes.None;
                    //affect.flags.Add(AffectFlags.FaerieFire);
                    affect.where = AffectWhere.ToAffects;
                    affect.duration = ch_level / 5;
                    affect.modifier = 0;
                    affect.displayName = "Faerie Fogged";
                    affect.endMessage = "You stop glowing purple.\n\r";
                    affect.endMessageToRoom = "$n stops glowing purple.\n\r";
                    affect.affectType = AffectTypes.Malady; //castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                    vict.AffectToChar(affect);
                    vict.StripHidden();
                    vict.StripInvis();
                    vict.StripSneak();
                    vict.StripCamouflage();
                }
            }
        }

        public static void SpellFaerieFire(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim.FindAffect(spell) != null)
            {
                ch.send("They are already glowing.\n\r");
                return;
            }

            ch.Act("$N is revealed by $n's faerie fire.", victim, null, null, ActType.ToRoomNotVictim);
            ch.Act("$n's faerie fire reveals you.", victim, null, null, ActType.ToRoom);
            ch.Act("Your faerie fire reveals $N.", victim, null, null, ActType.ToChar);
            var affect = new AffectData();
            affect.ownerName = ch.Name;
            affect.skillSpell = spell;
            affect.level = ch_level;
            affect.location = ApplyTypes.Hitroll;
            affect.flags.Add(AffectFlags.FaerieFire);
            affect.duration = ch_level / 4;
            affect.modifier = -4 - ch.Level / 4;
            affect.where = AffectWhere.ToAffects;
            victim.AffectToChar(affect);

            affect.location = ApplyTypes.AC;
            affect.modifier = +200 + ch.Level * 4;
            affect.displayName = "Faerie Fire";
            affect.endMessage = "You stop glowing purple.\n\r";
            affect.endMessageToRoom = "$n stops glowing purple.\n\r";
            affect.affectType = AffectTypes.Malady; //castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
            victim.AffectToChar(affect);
            victim.StripHidden();
            victim.StripInvis();
            victim.StripSneak();
            victim.StripCamouflage();
        }

        public static void SpellHarm(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            int dam;

            dam = ch.GetDamage(level, 1, 2, 10);

            if (SavesSpell(level, victim, WeaponDamageTypes.Force))
                dam = dam / 2;

            var chance = ch.GetSkillPercentage("second attack") + 20;
            var chance2 = ch.GetSkillPercentage("third attack") + 20;

            if (chance2 > 21 && Utility.NumberPercent() < chance2)
            {
                Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Force);
                if (victim.Room == ch.Room)
                    Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Force);
                if (victim.Room == ch.Room)
                    Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Force);
            }
            else if (chance > 21 && Utility.NumberPercent() < chance)
            {
                Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Force);
                if (victim.Room == ch.Room)
                    Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Force);
            }
            else Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Force);
        }

        public static void SpellLightningBolt(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.WaterSwim) || (sector == SectorTypes.Swim) ||
                (sector == SectorTypes.WaterNoSwim) || (sector == SectorTypes.Ocean) ||
                (sector == SectorTypes.River) || (sector == SectorTypes.Underwater));

            var dam = ch.GetDamage(level, .5f, 1);// dam = Utility.Random(dam_each[level] / 2, dam_each[level] * 2);
            ch.Act(checkSector ? "Due to the nearness of water, A HUGE bolt of lightning shoots forth from your hands!" :
                "A bolt of lightning shoots forth from your hands!", null, null, null, ActType.ToChar);
            ch.Act("A bolt of lightning shoots from $n's hands!", null, null, null, ActType.ToRoom);

            if (checkSector)
                dam *= 2;
            if (CheckSpellcraft(ch, spell))
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Lightning))
                dam /= 2;
            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Lightning);
            return;
        }
        public static void SpellForkedLightning(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.WaterSwim) || (sector == SectorTypes.Swim) ||
                (sector == SectorTypes.WaterNoSwim) || (sector == SectorTypes.Ocean) ||
                (sector == SectorTypes.River) || (sector == SectorTypes.Underwater));
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act(checkSector ? "Due to the nearness of water, A HUGE bolt of forked lightning shoots forth from your hands!" :
                "A bolt of forked lightning shoots forth from your hands!", null, null, null, ActType.ToChar);
            ch.Act("A bolt of forked lightning shoots from $n's hands!", null, null, null, ActType.ToRoom);

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (((vict.Fighting != null && vict.Fighting.IsSameGroup(ch)) || ch.Fighting == vict) && vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    var dam = ch.GetDamage(level, 1, 2);

                    if (checkSector)
                        dam *= 2;
                    if (spellcraft)
                        dam += level;
                    if (SavesSpell(level, victim, WeaponDamageTypes.Lightning))
                        dam /= 2;

                    Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Lightning);
                    ch.CheckImprove(spell, true, 1);
                }
            }
        } // end fireball

        public static void SpellAcidBlast(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var dam = ch.GetDamage(level, 1, 2);  //dam = Utility.Random(dam_each[level] * 2, dam_each[level] * 4);

            if (CheckSpellcraft(ch, spell))
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Acid))
                dam /= 2;
            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Acid);
            return;
        }

        public static void SpellColourSpray(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var dam = ch.GetDamage(level, 2, 4); // Utility.Random(dam_each[level] * 2, dam_each[level] * 4);

            if (CheckSpellcraft(ch, spell))
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Light))
                dam /= 2;
            else
                SpellBlindness(castType, SkillSpell.SkillLookup("blindness"), level / 2, ch, victim, item, arguments, target);

            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Light);
            return;
        }

        public static void SpellChillTouch(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var dam = ch.GetDamage(level, .5f, 1);

            if (CheckSpellcraft(ch, spell))
                dam += level;

            if (SavesSpell(level, victim, WeaponDamageTypes.Cold))
                dam /= 2;
            else
            {
                var affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.Strength;
                affect.where = AffectWhere.ToAffects;
                affect.duration = 6;
                affect.modifier = -1;
                affect.displayName = "Chill Touch";
                affect.endMessage = "You feel less chilled.\n\r";
                affect.endMessageToRoom = "$n looks less chilled.\n\r";
                affect.affectType = AffectTypes.Malady; //castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectApply(affect);
                victim.Act("$n turns blue and shivers.\n\r", null, null, null, ActType.ToRoom);
                victim.Act("You turn blue and shiver.\n\r", null, null, null, ActType.ToChar);
            }

            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Cold);
            return;
        }

        //public static void SpellDetectEvil(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        //{
        //    AffectData affect;
        //    if (victim == null)
        //        victim = ch;

        //    if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.DetectEvil))
        //    {
        //        if (victim != ch)
        //            ch.send("They can already sense evil.\n\r");
        //        else
        //            ch.send("You can already sense evil when it is present.");
        //        return;
        //    }
        //    else
        //    {
        //        victim.send("You feel able to sense evil.\n\r");
        //        //victim.Act("$n is protected from evil.", type: ActType.ToRoom);
        //        affect = new AffectData();
        //        affect.skillSpell = spell;
        //        affect.level = level;
        //        affect.location = ApplyTypes.None;
        //        affect.where = AffectWhere.ToAffects;
        //        affect.flags.Add(AffectFlags.DetectEvil);
        //        affect.duration = 10 + level / 4;
        //        affect.displayName = "detect evil";
        //        affect.endMessage = "Your feel less aware of evil.\n\r";
        //        //affect.endMessageToRoom = "$n's protection wanes.\n\r";
        //        affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

        //        victim.AffectToChar(affect);


        //    }
        //}
        //public static void SpellDetectGood(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        //{
        //    AffectData affect;
        //    if (victim == null)
        //        victim = ch;

        //    if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.DetectGood))
        //    {
        //        if (victim != ch)
        //            ch.send("They can already sense good.\n\r");
        //        else
        //            ch.send("You can already sense good when it is present.\n\r");
        //        return;
        //    }
        //    else
        //    {
        //        victim.send("You feel able to sense good.\n\r");
        //        //victim.Act("$n is protected from evil.", type: ActType.ToRoom);
        //        affect = new AffectData();
        //        affect.skillSpell = spell;
        //        affect.level = level;
        //        affect.location = ApplyTypes.None;
        //        affect.where = AffectWhere.ToAffects;
        //        affect.flags.Add(AffectFlags.DetectGood);
        //        affect.duration = 10 + level / 4;
        //        affect.displayName = "detect good";
        //        affect.endMessage = "Your feel less aware of good.\n\r";
        //        //affect.endMessageToRoom = "$n's protection wanes.\n\r";
        //        affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

        //        victim.AffectToChar(affect);
        //    }
        //}
        public static void SpellRefresh(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            if (victim.MovementPoints == victim.MaxMovementPoints)
            {
                if (victim != ch)
                    ch.send("Their feet are already fully rested.\n\r");
                else
                    ch.send("Your feet are already fully rested.");
                return;
            }
            else
            {
                victim.send("You feel able to walk further.\n\r");
                victim.Act("$n looks refreshed.", type: ActType.ToRoom);

                victim.MovementPoints += level; // over refresh :D

            }
        }
        public static void SpellSleep(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
            {
                ch.send("You can't find them.\n\r");
                return;
            }

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Sleep))
            {
                ch.send("They are already forced asleep.\n\r");
                return;
            }
            else
            {
                // Saves ??
                victim.send("You feel very sleepy ..... zzzzzz.\n\r");
                victim.Act("$n goes to sleep.", type: ActType.ToRoom);

                Combat.StopFighting(victim, true);
                victim.Position = Positions.Sleeping;

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.Sleep);
                affect.duration = Math.Max(5, level / 4);
                affect.displayName = "sleep";
                affect.endMessage = "Your feel less tired.\n\r";
                affect.endMessageToRoom = "$n looks less tired.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                victim.AffectToChar(affect);
            }
        }
        public static void SpellCancellation(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null) victim = ch;
            bool stripped = false;
            foreach (var aff in victim.AffectsList.ToArray())
            {
                if (aff.affectType == AffectTypes.Spell || aff.affectType == AffectTypes.Commune)
                {
                    var striplevel = aff.level - ch.Level;
                    var chance = 75;
                    if (striplevel < 0)
                        chance += 25;
                    else if (striplevel > 5)
                        chance -= 25;

                    if (Utility.NumberPercent() < chance)
                    {
                        stripped = true;
                        victim.AffectFromChar(aff, AffectRemoveReason.Cleansed);

                        // remove all of the affect of that type - now done in affectfromchar
                        //foreach (var affother in victim.AffectsList.ToArray())
                        //{
                        //    if (affother != aff && affother.skillSpell == aff.skillSpell)
                        //        victim.AffectFromChar(affother);
                        //}
                    }
                }
            }
            if (stripped)
                ch.send("Ok.\n\r");
            else
                ch.send("You failed.\n\r");
        }
        public static void SpellCauseLight(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            Combat.Damage(ch, victim, Utility.dice(1, 8) + level / 3, spell, WeaponDamageTypes.Force);
            return;
        }

        public static void SpellCauseSerious(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            Combat.Damage(ch, victim, Utility.dice(2, 8) + level / 2, spell, WeaponDamageTypes.Force);
            return;
        }

        public static void SpellCauseCritical(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            Combat.Damage(ch, victim, Utility.dice(3, 8) + level - 6, spell, WeaponDamageTypes.Force);
            return;
        }

        public static void SpellSlow(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(AffectFlags.Slow)) != null)
            {
                if (ch != victim)
                    ch.send("They are already moving more slowly.\n\r");
                else
                    ch.send("Your movements are already slowed.\n\r");
                return;
            }
            else if ((affect = victim.FindAffect(AffectFlags.Haste)) != null)
            {
                victim.AffectFromChar(affect, AffectRemoveReason.WoreOff);
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 5 + level / 4;
                affect.modifier = -4;
                affect.displayName = "slow";
                affect.flags.SETBIT(AffectFlags.Slow);
                affect.endMessage = "Your movement quickens.\n\r";
                affect.endMessageToRoom = "$n is moving more quickly.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("You begin to move more slowly.\n\r");
                victim.Act("$n begins to move more slowly.\n\r", type: ActType.ToRoom);
            }
        }
        public static void SpellHaste(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(AffectFlags.Haste)) != null)
            {
                if (ch != victim)
                    ch.send("They are already moving more quickly.\n\r");
                else
                    ch.send("Your movements is already quickened.\n\r");
                return;
            }
            else if ((affect = victim.FindAffect(AffectFlags.Slow)) != null)
            {
                victim.AffectFromChar(affect, AffectRemoveReason.WoreOff);
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 5 + level / 4;
                affect.modifier = 4 + level / 6;
                affect.displayName = "haste";
                affect.where = AffectWhere.ToAffects;
                affect.flags.SETBIT(AffectFlags.Haste);
                affect.endMessage = "Your movement slows down.\n\r";
                affect.endMessageToRoom = "$n is moving more slowly.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("You begin to move more quickly.\n\r");
                victim.Act("$n begins to move more quickly.\n\r", type: ActType.ToRoom);
            }
        }
        public static void SpellProtectiveShield(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("They are already surrounded by a protective shield.\n\r");
                else
                    ch.send("You are already surrounded by a protective shield.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.None;
                affect.duration = 10 + level / 4;
                affect.modifier = 0;
                affect.displayName = "protective shield";
                affect.endMessage = "Your protective shield fades.\n\r";
                affect.endMessageToRoom = "$n's protective shield fades.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
                victim.send("You are surrounded by a protective shield.\n\r");
                victim.Act("$n is surrounded by a protective shield.\n\r", type: ActType.ToRoom);
            }
        }
        public static void SpellFrenzy(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(AffectFlags.Berserk)) != null)
            {
                if (ch != victim)
                    ch.send("They are already in a frenzied state.\n\r");
                else
                    ch.send("You are already in a frenzied state.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 10 + level / 4;
                affect.modifier = level / 4;
                affect.flags.Add(AffectFlags.Berserk);
                affect.displayName = "frenzy";
                affect.endMessage = "Your frenzy fades.\n\r";
                affect.endMessageToRoom = "$n's frenzied look fades.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Damroll;
                affect.duration = 10 + level / 4;
                affect.modifier = level / 4;
                affect.flags.Add(AffectFlags.Berserk);
                affect.displayName = "frenzy";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);

                victim.send("You enter a frenzied state.\n\r");
                victim.Act("$n enters a frenzied state.\n\r", type: ActType.ToRoom);
            }
        }
        public static void SpellChangeSex(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("They've already been changed.\n\r");
                else
                    ch.send("You've already been changed.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Sex;
                affect.duration = level * 2;
                affect.modifier = 1;
                affect.displayName = "change sex";
                affect.endMessage = "Your sex returns to normal.";
                affect.endMessageToRoom = "$n's sex returns to normal.";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);


                victim.Act("You feel different.", type: ActType.ToChar);
                victim.Act("$n doesn't look like $mself anymore...", type: ActType.ToRoom);
            }
        }
        public static void SpellEnergyDrain(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            int amount;
            int type;
            if (victim == ch)
            {
                ch.send("You can't drain your own life force.\n\r", ch);
                return;
            }
            switch (Utility.Random(0, 3))
            {
                default: type = 1; amount = Utility.dice(level, 3); break;
                case (0):
                case (1):          /* HP */
                    type = 1; amount = Utility.dice(level, 2); break;
                case (2):       /* move */
                    type = 2; amount = Utility.dice(level, 2); break;
                case (3):       /* mana */
                    type = 3; amount = Utility.dice(level, 2); break;
            }
            var amounthp = Utility.dice(level, 2);
            victim.send("You feel an icy hand brush against your soul.\n\r");

            if (SavesSpell(level, victim, WeaponDamageTypes.Negative))
            {
                victim.Act("$n turns pale and shivers briefly.", victim, type: ActType.ToRoom);
                Combat.Damage(ch, victim, amounthp, spell, WeaponDamageTypes.Negative);
                return;
            }
            victim.Act("$n gets a horrible look of pain in $s face and shudders in shock.", type: ActType.ToRoom);

            var affect = new AffectData();
            affect.skillSpell = spell;
            affect.level = level;

            affect.duration = level / 2;
            affect.where = AffectWhere.ToAffects;
            affect.displayName = "energy drain";

            affect.affectType = AffectTypes.Malady;

            switch (type)
            {
                default:
                case (1):
                    ch.Act("You drain $N's vitality with vampiric magic.", victim, type: ActType.ToChar);
                    victim.send("You feel your body being mercilessly drained.\n\r");
                    ch.HitPoints = Utility.URANGE(0, ch.HitPoints + amount / 3, ch.MaxHitPoints);

                    if (!victim.IsAffected(spell))
                    {
                        affect.endMessage = "Your vitality returns.";
                        affect.endMessageToRoom = "$n's vitality returns.";

                        affect.location = ApplyTypes.Constitution;
                        affect.modifier = -3;
                        victim.AffectToChar(affect);
                    }
                    break;
                case (2):
                    ch.send("Your energy draining invigorates you!\n\r");
                    victim.MovementPoints = Utility.URANGE(0, victim.MovementPoints - amount, victim.MaxMovementPoints);
                    ch.MovementPoints = Utility.URANGE(0, ch.MovementPoints + amount / 2, ch.MaxMovementPoints);
                    victim.send("You feel tired and weakened.\n\r", victim);
                    if (!victim.IsAffected(spell))
                    {
                        affect.endMessage = "Your agility returns.";
                        affect.endMessageToRoom = "$n's agility returns.";

                        affect.location = ApplyTypes.Dexterity;
                        affect.modifier = -2;
                        victim.AffectToChar(affect);

                        affect.endMessage = "";
                        affect.endMessageToRoom = "";

                        affect.location = ApplyTypes.Move;
                        affect.modifier = -amount / 2;
                        victim.AffectToChar(affect);
                    }
                    break;
                case (3):
                    victim.ManaPoints = Utility.URANGE(0, victim.ManaPoints - amount, victim.MaxManaPoints);
                    ch.send("Your draining sends warm energy through you!\n\r");

                    ch.ManaPoints = Utility.URANGE(0, ch.ManaPoints + amount / 3, ch.MaxManaPoints);
                    victim.send("You feel part of your mind being savagely drained.\n\r");
                    if (!victim.IsAffected(spell))
                    {
                        affect.endMessage = "Your mental focus returns.";
                        affect.endMessageToRoom = "$n's mental focus returns.";

                        affect.location = ApplyTypes.Intelligence;
                        affect.modifier = -3;
                        victim.AffectToChar(affect);

                        affect.endMessage = "";
                        affect.endMessageToRoom = "";

                        affect.location = ApplyTypes.Wisdom;
                        affect.modifier = -2;
                        victim.AffectToChar(affect);
                    }
                    break;
            }

            Combat.Damage(ch, victim, amount + amounthp, spell, WeaponDamageTypes.Negative);
        }
        public static void SpellGasBreath(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var dam = ch.HitPoints / 9;
            dam += Utility.dice(ch_level, 5);
            if (dam > ch.HitPoints)
                dam = ch.HitPoints;

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    ch.Act("$n's gas breath hits $N!", vict, null, null, ActType.ToRoomNotVictim);
                    ch.Act("$n's gas breath hits you!", vict, null, null, ActType.ToVictim);
                    ch.Act("Your gas breath hits $N!", vict, null, null, ActType.ToChar);

                    if (SavesSpell(ch_level, vict, WeaponDamageTypes.Poison))
                    {
                        Combat.Damage(ch, vict, dam / 2, spell, WeaponDamageTypes.Poison);
                    }
                    else
                    {
                        Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Poison);
                    }
                }
            }
        }
        public static void SpellFireBreath(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var dam = ch.HitPoints / 9;
            dam += Utility.dice(ch_level, 5);
            if (dam > ch.HitPoints)
                dam = ch.HitPoints;

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    ch.Act("$n's fire breath hits $N!", vict, null, null, ActType.ToRoomNotVictim);
                    ch.Act("$n's fire breath hits you!", vict, null, null, ActType.ToVictim);
                    ch.Act("Your fire breath hits $N!", vict, null, null, ActType.ToChar);

                    if (SavesSpell(ch_level, vict, WeaponDamageTypes.Fire))
                    {
                        Combat.Damage(ch, vict, dam / 2, spell, WeaponDamageTypes.Fire);
                    }
                    else
                    {
                        Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Fire);
                    }
                }
            }
        }
        public static void SpellAcidBreath(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var dam = ch.HitPoints / 9;
            dam += Utility.dice(ch_level, 4);
            if (dam > ch.HitPoints)
                dam = ch.HitPoints;

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    ch.Act("$n's acid breath hits $N!", vict, null, null, ActType.ToRoomNotVictim);
                    ch.Act("$n's acid breath hits you!", vict, null, null, ActType.ToVictim);
                    ch.Act("Your acid breath hits $N!", vict, null, null, ActType.ToChar);

                    if (SavesSpell(ch_level, vict, WeaponDamageTypes.Acid))
                    {
                        Combat.Damage(ch, vict, dam / 2, spell, WeaponDamageTypes.Acid);
                    }
                    else
                    {
                        Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Acid);
                    }
                }
            }
        }
        public static void SpellLightningBreath(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var dam = ch.HitPoints / 9;
            dam += Utility.dice(ch_level, 4);
            if (dam > ch.HitPoints)
                dam = ch.HitPoints;

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    ch.Act("$n's lightning breath hits $N!", vict, null, null, ActType.ToRoomNotVictim);
                    ch.Act("$n's lightning breath hits you!", vict, null, null, ActType.ToVictim);
                    ch.Act("Your lightning breath hits $N!", vict, null, null, ActType.ToChar);

                    if (SavesSpell(ch_level, vict, WeaponDamageTypes.Lightning))
                    {
                        Combat.Damage(ch, vict, dam / 2, spell, WeaponDamageTypes.Lightning);
                    }
                    else
                    {
                        Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Lightning);
                    }
                }
            }
        }
        public static void SpellEarthquake(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act("The earth trembles beneath your feet!", null, null, null, ActType.ToChar);
            ch.Act("$n makes the earth tremble and shiver.", null, null, null, ActType.ToRoom);

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    if (vict.IsAffected(AffectFlags.Flying))
                    {
                        Combat.Damage(ch, vict, 0, spell, WeaponDamageTypes.Bash);
                        ch.CheckImprove(spell, false, 1);
                    }
                    else
                    {
                        var dam = ch.GetDamage(ch_level, .5f, 1);

                        if (spellcraft) dam += ch_level;

                        if (SavesSpell(ch_level, victim, WeaponDamageTypes.Bash))
                            dam /= 2;
                        Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Bash);
                        ch.CheckImprove(spell, true, 1);
                    }

                }
            }
        }
        public static void SpellDetectPoison(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("They can already sense poison.\n\r");
                else
                    ch.send("You can already sense poison.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.duration = 10;
                affect.displayName = "detect poison";
                affect.endMessage = "You can no longer sense poison.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);


                victim.send("Your eyes feel different.\n\r");
                if (ch != victim)
                    ch.send("Ok.\n\r");
            }
        }
        public static void SpellSpiderhands(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null)
            {
                if (ch != victim)
                    ch.send("They already have a spiderlike grip.\n\r");
                else
                    ch.send("You already have a spiderlike grip.\n\r");
                return;
            }
            else
            {
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.duration = level / 3;
                affect.displayName = "spiderhands";
                affect.endMessage = "You lose your spiderlike grip.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);


                victim.send("Your grip tightens like a spider on its prey.\n\r");
                if (ch != victim)
                    ch.send("Ok.\n\r");
            }
        }
        public static void SpellSealOfRighteousness(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim != ch)
                victim = ch;

            StripSeals(victim);

            affect = new AffectData();
            affect.skillSpell = spell;
            affect.level = level;
            affect.where = AffectWhere.ToAffects;
            affect.location = ApplyTypes.Hitroll;
            affect.duration = 10 + (level / 4);
            affect.modifier = level / 4;
            affect.displayName = "seal of righteousness";
            affect.endMessage = "Your seal of righteousness breaks.\n\r";
            affect.endMessageToRoom = "$n's seal of righteousness breaks.\n\r";
            affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
            victim.AffectToChar(affect);

            victim.send("You gain the effect of a seal of righteousness.\n\r");
            victim.Act("$n gains the effect of a seal of righteousness.\n\r", type: ActType.ToRoom);

        }
        public static void StripSeals(Character ch)
        {
            foreach (var aff in ch.AffectsList.ToArray())
            {
                if (aff.skillSpell != null && aff.skillSpell.name.StartsWith("seal of"))
                    ch.AffectFromChar(aff, AffectRemoveReason.WoreOff);
            }
        }
        public static void SpellSealOfLight(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim != ch)
                victim = ch;

            StripSeals(victim);

            affect = new AffectData();
            affect.skillSpell = spell;
            affect.level = level;
            affect.where = AffectWhere.ToAffects;
            affect.location = ApplyTypes.Saves;
            affect.duration = 10 + (level / 4);
            affect.modifier = -(level / 4);
            affect.displayName = "seal of light";
            affect.endMessage = "Your seal of light breaks.\n\r";
            affect.endMessageToRoom = "$n's seal of light breaks.\n\r";
            affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
            victim.AffectToChar(affect);

            victim.send("You gain the effect of a seal of light.\n\r");
            victim.Act("$n gains the effect of a seal of light.\n\r", type: ActType.ToRoom);
        }
        public static void SpellSealOfWisdom(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim != ch)
                victim = ch;

            StripSeals(victim);

            affect = new AffectData();
            affect.skillSpell = spell;
            affect.level = level;
            affect.where = AffectWhere.ToAffects;
            affect.location = ApplyTypes.Mana;
            affect.duration = 10 + (level / 4);
            affect.modifier = level / 3;
            affect.displayName = "seal of wisdom";
            affect.endMessage = "Your seal of wisdom breaks.\n\r";
            affect.endMessageToRoom = "$n's seal of wisdom breaks.\n\r";
            affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
            victim.AffectToChar(affect);

            victim.send("You gain the effect of a seal of wisdom.\n\r");
            victim.Act("$n gains the effect of a seal of wisdom.\n\r", type: ActType.ToRoom);
        }
        public static void DoHeal(Character ch, string arguments)
        {
            Character healer = null;
            if (ch == null) return;

            foreach (var npc in ch.Room.Characters)
            {
                if (npc.Flags.ISSET(ActFlags.Healer) || npc.Flags.ISSET(ActFlags.Cleric))
                {
                    healer = npc;
                    break;
                }
            }
            if (healer == null)
            {
                ch.send("There are no healers here.\n\r");
                return;
            }
            if (arguments.ISEMPTY())
            {
                ch.Act("\\Y$N says 'I offer the following spells:'\\x", healer);
                ch.send("  Light       :  Cure light wounds      100 silver\n\r");
                ch.send("  Serious     :  Cure serious wounds    150 silver\n\r");
                ch.send("  Critic      :  Cure critical wounds   250 silver\n\r");
                ch.send("  Heal        :  Healing spell          400 silver\n\r");
                ch.send("  Blind       :  Cure blindness         150 silver\n\r");
                ch.send("  Disease     :  Cure disease           250 silver\n\r");
                ch.send("  Poison      :  Cure poison            250 silver\n\r");
                ch.send("  Cleanse     :  Cleanse                500 silver\n\r");
                ch.send("  Uncurse     :  Remove curse           100 silver\n\r");
                ch.send("  Restoration :  Restoration            150 silver\n\r");
                ch.send("  Refresh     :  Restore movement       150 silver\n\r");
                ch.send("  Mana        :  Restore mana           100 silver\n\r");
                ch.send(" Type heal <type> to be healed.\n\r");
                return;
            }
            int cost;
            string arg = "";
            SkillSpell spell;
            arguments = arguments.OneArgument(ref arg);
            if ("light".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("cure light");
                cost = 100;
            }
            else if ("serious".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("cure serious");
                cost = 150;
            }
            else if ("critical".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("cure critical");
                cost = 250;
            }
            else if ("cleanse".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("cleanse");
                cost = 500;
            }
            else if ("heal".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("heal");
                cost = 400;
            }
            else if ("blindness".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("cure blindness");
                cost = 150;
            }
            else if ("disease".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("cure disease");
                cost = 250;
            }
            else if ("poison".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("cure poison");
                cost = 250;
            }
            else if ("curse".StringPrefix(arg) || "uncurse".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("remove curse");
                cost = 100;
            }
            else if ("restore".StringPrefix(arg) || "restoration".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("restoration");
                cost = 150;
            }
            else if ("mana".StringPrefix(arg) || "energize".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("energize");
                cost = 100;
            }
            else if ("refresh".StringPrefix(arg) || "moves".StringPrefix(arg))
            {
                spell = SkillSpell.SkillLookup("refresh");
                cost = 150;
            }
            else
            {
                ch.Act("\\Y$N says 'Type 'heal' for a list of spells.'\\x",
                    healer);
                return;
            }
            if (cost > (ch.Gold * 1000 + ch.Silver))
            {
                ch.Act("\\Y$N says 'You do not have enough gold for my services.'\\x",
                    healer);
                return;
            }
            ch.WaitState(Game.PULSE_VIOLENCE);

            if (ch.Silver > cost)
                ch.Silver -= cost;
            else
            {
                ch.Silver += 1000 - cost;
                ch.Gold--;
            }

            healer.Act("$n closes $s eyes for a moment and nods at $N.", ch, type: ActType.ToRoomNotVictim);
            healer.Act("$n closes $s eyes for a moment and nods at you.", ch, type: ActType.ToVictim);

            if (spell == null || spell.spellFun == null)
            {
                ch.Act("\\Y$N says 'I don't know how to do that yet.'\\x", healer); return;
            }

            spell.spellFun(CastType.Commune, spell, healer.Level, healer, ch, null, ch.Name + " " + arguments, TargetIsType.targetChar);
        }
        public static void SpellImbueWeapon(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (item == null)
            {
                ch.send("You don't see that here.\n\r");
                return;
            }
            else if (ch.ManaPoints < ch.MaxManaPoints / 2)
            {
                ch.send("You do not have enough mana to cast this spell.\n\r");
                return;
            }
            else if (!item.ItemType.ISSET(ItemTypes.Weapon))
            {
                ch.send("That isn't a weapon.\n\r");
                return;
            }
            else if (ch.GetEquipmentWearSlot(item) != null)
            {
                ch.send("You cannot imbue a weapon you are wielding or holding.\n\r");
                return;
            }
            else
            {
                int vnum = 0;
                if (item.Vnum == 2967) vnum = 2968;
                else if (item.Vnum == 2969) vnum = 2970;
                else
                {
                    ch.send("You do not have an appropriate weapon to imbue.\n\r");
                    return;
                }
                if (!ItemTemplateData.Templates.TryGetValue(vnum, out var ItemTemplate))
                {
                    ch.send("You failed.\n\r");
                    return;
                }
                ch.WaitState(spell.waitTime * 2);
                ch.Act("$n imbues $p with a measure of $s life force.", type: ActType.ToRoom);
                ch.Act("You imbue $p with a measure of your life force.", type: ActType.ToChar);

                var affmodifiertable = new SortedList<int, int>()
            {
                { 0, 4 },
                { 35, 6 },
                { 42, 8 },
                { 48, 10 },
                { 51, 12 },
                { 56, 14 },
            };
                var staffspear = new ItemData(ItemTemplate);

                var affect = new AffectData();
                affect.duration = -1;
                affect.where = AffectWhere.ToObject;
                affect.skillSpell = spell;
                affect.location = ApplyTypes.DamageRoll;
                affect.modifier = (from keyvaluepair in affmodifiertable where ch.Level >= keyvaluepair.Key select keyvaluepair.Value).Max();
                staffspear.affects.Add(new AffectData(affect));
                affect.location = ApplyTypes.Hitroll;
                staffspear.affects.Add(new AffectData(affect));
                staffspear.DamageDice.DiceSides = 6;
                staffspear.DamageDice.DiceCount = 8;
                staffspear.DamageDice.DiceBonus = ch.Level / 4 + 10;
                staffspear.Level = ch.Level;
                item.Dispose();
                ch.AddInventoryItem(staffspear);
                ch.ManaPoints -= ch.MaxManaPoints / 2;
                Combat.Damage(ch, ch, ch.MaxHitPoints / 2, spell, WeaponDamageTypes.Magic);
            }
        } // end enchant weapon
        public static void SpellBlessWeapon(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (item == null)
            {
                ch.send("You don't see that here.\n\r");
                return;
            }
            else if (!item.ItemType.ISSET(ItemTypes.Weapon))
            {
                ch.send("That isn't a weapon.\n\r");
                return;
            }
            else
            {
                var aff = (from ia in item.affects where ia.skillSpell == spell && ia.location == ApplyTypes.Damroll select ia).FirstOrDefault();
                var aff2 = (from ia in item.affects where ia.skillSpell == spell && ia.location == ApplyTypes.Hitroll select ia).FirstOrDefault();
                var worn = ch.GetEquipmentWearSlot(item) != null;
                if (worn)
                {
                    ch.Act("You cannot bless a weapon you are wielding.\n\r");
                    return;
                }

                if (aff != null)
                {
                    item.affects.Remove(aff);
                    item.affects.Remove(aff2);
                }

                aff = new AffectData();
                aff.skillSpell = spell;
                aff.location = ApplyTypes.Damroll;
                aff.modifier = (ch.Level / 10) + 5;
                aff.level = ch_level;
                aff.duration = -1;
                aff.where = AffectWhere.ToObject;

                aff2 = new AffectData();
                aff2.where = AffectWhere.ToObject;
                aff2.skillSpell = spell;
                aff2.location = ApplyTypes.Hitroll;
                aff2.modifier = (ch.Level / 10) + 5;
                aff2.duration = -1;
                aff2.level = ch_level;

                item.affects.Add(aff);
                item.affects.Add(aff2);

                item.extraFlags.SETBIT(ExtraFlags.Glow);
                item.extraFlags.SETBIT(ExtraFlags.Hum);
                item.WeaponDamageType = WeaponDamageMessage.GetWeaponDamageMessage("Wrath");

                if (worn)
                {
                    ch.AffectApply(aff);
                    ch.AffectApply(aff2);
                }
                ch.Act("$p glows a deep violet before fading slightly.\n\r", null, item);
                ch.Act("$p glows a deep violet before fading slightly.\n\r", null, item, null, ActType.ToRoom);
            }
        }
        public static void SpellRemoveTaint(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            if (target == TargetIsType.targetItem)
            {
                if (item != null)
                {
                    if (item.extraFlags.ISSET(ExtraFlags.Evil) || item.extraFlags.ISSET(ExtraFlags.AntiGood))
                    {
                        item.extraFlags.REMOVEFLAG(ExtraFlags.Evil);
                        item.extraFlags.REMOVEFLAG(ExtraFlags.AntiGood);
                        ch.Act("$p glows white as its taint is lifted.", null, item, null, ActType.ToChar);
                        ch.Act("$p glows white as its taint is lifted.", null, item, null, ActType.ToRoom);
                    }
                    else
                        ch.Act("$p isn't tainted.", null, item, null, ActType.ToChar);
                }
            }
            else
            {
                if (victim != null)
                {
                    int count = 0;
                    string victimname = "";
                    arguments = arguments.OneArgument(ref victimname);
                    if (!arguments.ISEMPTY())
                    {
                        item = victim.GetItemEquipment(arguments, ref count);
                        if (item == null)
                            item = victim.GetItemInventory(arguments, ref count);

                        if (item == null)
                        {
                            ch.send("You can't find it.\n\r");
                            return;
                        }

                        if (item.extraFlags.ISSET(ExtraFlags.Evil) || item.extraFlags.ISSET(ExtraFlags.AntiGood))
                        {
                            item.extraFlags.REMOVEFLAG(ExtraFlags.Evil);
                            item.extraFlags.REMOVEFLAG(ExtraFlags.AntiGood);
                            ch.Act("$p glows white as its taint is lifted.", null, item, null, ActType.ToChar);
                            ch.Act("$p glows white as its taint is lifted.", null, item, null, ActType.ToRoom);
                            return;
                        }
                        else
                        {
                            ch.Act("$p isn't tainted.", null, item, null, ActType.ToChar);
                            return;
                        }
                    }
                }
            }
        }
        private static void StripDamageNounModifiers(Character ch)
        {
            foreach (var affect in ch.AffectsList.ToArray())
            {
                if (affect.where == AffectWhere.ToDamageNoun)
                {
                    ch.AffectFromChar(affect, AffectRemoveReason.WoreOff);
                }
            }
        } // end strip damage noun modifier
        public static void SpellChannelHeat(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            StripDamageNounModifiers(ch);
            ch.Act("$n begins channeling heat.", type: ActType.ToRoom);
            ch.Act("You channel heat.");
            var affect = new AffectData();
            affect.where = AffectWhere.ToDamageNoun;
            affect.duration = 10;
            affect.skillSpell = spell;
            affect.level = ch_level;
            affect.displayName = spell.name;
            affect.DamageTypes.Add(WeaponDamageTypes.Fire);
            affect.endMessage = "You are no longer channeling heat.";
            ch.AffectToChar(affect);
        } // end Channel Heat
        public static void SpellFrostFingers(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            StripDamageNounModifiers(ch);
            ch.Act("$n begins channeling frost fingers.", type: ActType.ToRoom);
            ch.Act("You channel frost fingers.");
            var affect = new AffectData();
            affect.where = AffectWhere.ToDamageNoun;
            affect.duration = 10;
            affect.skillSpell = spell;
            affect.level = ch_level;
            affect.displayName = spell.name;
            affect.DamageTypes.Add(WeaponDamageTypes.Cold);
            affect.endMessage = "You are no longer channeling frost.";
            ch.AffectToChar(affect);
        } // end Frost Fingers
        public static void SpellShockingTouch(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            StripDamageNounModifiers(ch);
            ch.Act("A spark seems to jump between $n's eyes.", type: ActType.ToRoom);
            ch.Act("You charge yourself with electricity.");
            var affect = new AffectData();
            affect.where = AffectWhere.ToDamageNoun;
            affect.duration = 10;
            affect.skillSpell = spell;
            affect.level = ch_level;
            affect.displayName = spell.name;
            affect.DamageTypes.Add(WeaponDamageTypes.Lightning);
            affect.endMessage = "Your charge dissipates.";
            ch.AffectToChar(affect);
        } // end Shocking Touch
        public static void SpellChargeWeapon(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            ItemData weapon = null;

            if (((arguments.ISEMPTY()) && (weapon = ch.GetEquipment(WearSlotIDs.Wield)) == null) || (!arguments.ISEMPTY() && (weapon = ch.GetItemInventoryOrEquipment(arguments, false)) == null))
            {
                ch.Act("Which weapon did you want to charge?\n\r");
            }
            else if (!weapon.ItemType.ISSET(ItemTypes.Weapon))
            {
                ch.Act("You can only charge a weapon.\n\r");
            }
            //else if (weapon.IsAffected(spell ))
            //{
            //    ch.Act("$p is already charged.\n\r", item: weapon);
            //}
            else
            {
                ch.Act("Sparks fly between $n eyes then $p crackles with lightning.", item: weapon, type: ActType.ToRoom);
                ch.Act("You successfully charge $p with lightning.", item: weapon, type: ActType.ToChar);

                AffectData affect = weapon.FindAffect(spell);
                if (affect == null)
                {
                    affect = new AffectData();
                    affect.duration = 10;
                    affect.level = ch.Level;
                    affect.where = AffectWhere.ToWeapon;
                    affect.skillSpell = spell;
                    affect.flags.SETBIT(AffectFlags.Lightning);
                    weapon.affects.Add(affect);
                    affect.endMessage = "Charge on $p wears off.\n\r";
                }
                else affect.duration = 10;
            }

        } // end charge weapon
        public static void CheckChargeWeapon(Character ch, Character victim, ItemData weapon)
        {
            AffectData weaponaffect;

            if (weapon != null && (weaponaffect = weapon.FindAffect(AffectFlags.Lightning)) != null && Utility.Random(1, 10) == 1)
            {
                var dam = ch.GetDamage(weaponaffect.level, .5f, 1); //dam = Utility.Random(dam_each[level] / 2, dam_each[level]);
                if (SavesSpell(weaponaffect.level, victim, WeaponDamageTypes.Lightning))
                    dam /= 2;
                ch.Act("As $p strikes $N, its stored chearge is unleashed!\n\r", victim, weapon, type: ActType.ToChar);
                ch.Act("As $n strikes $N with $p, its stored chearge is unleashed!\n\r", victim, weapon, type: ActType.ToRoomNotVictim);
                ch.Act("As $p strikes you, its stored chearge is unleashed!\n\r", victim, weapon, type: ActType.ToVictim);

                Combat.Damage(ch, victim, dam, weaponaffect.skillSpell, WeaponDamageTypes.Lightning);
                return;
            }
        }
        public static void SpellCyclone(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var dam = ch.GetDamage(level, .5f, 1); //dam = Utility.Random(dam_each[level] / 2, dam_each[level]);

            if (spellcraft)
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Air))
                dam /= 2;

            ch.Act("A small cyclone of air begins to swirl around you before striking out at $N!", victim, type: ActType.ToChar);
            ch.Act("A small cyclone of air begins to swirl around $n before striking out at $N!", victim, type: ActType.ToRoomNotVictim);
            ch.Act("A small cyclone of air begins to swirl around $n before striking out at you!", victim, type: ActType.ToVictim);

            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Air);

        } // end cyclone
        public static void SpellIceNeedles(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var chance = ch.GetSkillPercentage(spell) + 20;
            var spellcraft = (CheckSpellcraft(ch, spell));
            var dam = ch.GetDamage(level, 1, 2); //dam = Utility.Random(dam_each[level] / 2, dam_each[level]);

            if (spellcraft)
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Cold))
                dam /= 2;

            ch.Act("A spray of piercing ice needles swirl around before striking $N!", victim, type: ActType.ToChar);
            ch.Act("A spray of piercing ice needles swirl around $n before striking $N!", victim, type: ActType.ToRoomNotVictim);
            ch.Act("A spray of piercing ice needles swirl around $n before striking you!", victim, type: ActType.ToVictim);

            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Cold);
            if (chance > Utility.NumberPercent())
            {
                var affect = new AffectData();
                affect.displayName = spell.name;
                affect.skillSpell = spell;
                affect.duration = 5;
                affect.ownerName = ch.Name;
                affect.level = level;
                affect.location = ApplyTypes.Str;
                affect.modifier = -3;
                if (!victim.IsAffected(spell))
                {
                    victim.Act("$n is pierced by ice needles.\n\r", null, null, null, ActType.ToRoom);
                    victim.Act("You are pierced by ice needles.\n\r", null, null, null, ActType.ToChar);
                }
                else
                {
                    victim.Act("The ice needle wounds of $n deepen.\n\r", null, null, null, ActType.ToRoom);
                    victim.Act("Your ice needle wounds deepen.\n\r", null, null, null, ActType.ToChar);
                }
                victim.AffectToChar(affect);

                affect.location = ApplyTypes.Dex;
                affect.modifier = -3;
                affect.endMessage = "You recover from your ice wounds.";
                affect.endMessageToRoom = "$n recovers from $s ice wounds.";
                victim.AffectToChar(affect);
            }
        } // end iceneedles
        public static void SpellBuffet(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var isFlying = victim.IsAffected(AffectFlags.Flying);
            var dam = ch.GetDamage(level, 1, 2); //dam = Utility.Random(dam_each[level] / 2, dam_each[level]);

            if (spellcraft)
                dam += level;
            if (isFlying)
                dam *= 2;
            if (SavesSpell(level, victim, WeaponDamageTypes.Air))
                dam /= 2;

            ch.Act("You buffet $N with a controlled blast of air!", victim, type: ActType.ToChar);
            ch.Act("$n buffets $N with a controlled blast of air!", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n buffets you with a controlled blast of air!", victim, type: ActType.ToVictim);


            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Air);
            return;
        }
        public static void SpellWallOfFire(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var dam = ch.GetDamage(level, 1.5f, 2.5f);  //dam = Utility.Random(dam_each[level], dam_each[level] * 2);

            if (spellcraft)
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Fire))
                dam /= 2;

            ch.Act("You conjure a precise wall of fire, then throw it at $N.\n\r", victim, type: ActType.ToChar);
            ch.Act("$n conjures a precise wall of fire, then throws it at $N.\n\r", victim, type: ActType.ToRoomNotVictim);

            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Fire);
        } // end wall of fire
        public static void SpellIcicle(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.WaterSwim) || (sector == SectorTypes.Swim) ||
                (sector == SectorTypes.WaterNoSwim) || (sector == SectorTypes.Ocean) ||
                (sector == SectorTypes.River) || (sector == SectorTypes.Underwater));

            var dam = ch.GetDamage(level, .5f, 1); //dam = Utility.Random(dam_each[level], dam_each[level] * 2);

            ch.Act(checkSector ? "Because of the available water nearby, you conjure a greater bolt of ice to throw at $N." :
                "You conjure a magical bolt of ice, then throw it at $N.\n\r", victim, type: ActType.ToChar);
            ch.Act("$n conjures a magical bolt of ice, then throws it at $N.\n\r", victim, type: ActType.ToRoomNotVictim);

            if (spellcraft)
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Cold))
                dam /= 2;
            if (checkSector)
                dam *= 2;


            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Cold);
        } // end icicle
        public static void SpellEngulf(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.WaterSwim) || (sector == SectorTypes.Swim) ||
                (sector == SectorTypes.WaterNoSwim) || (sector == SectorTypes.Ocean) ||
                (sector == SectorTypes.River) || (sector == SectorTypes.Underwater));

            var dam = ch.GetDamage(level, 1, 2);

            ch.Act(checkSector ? "Because of the available water nearby, your engulfing of $N is twice as powerful." :
                "You completely engulf $N in water, drowning them without mercy.\n\r", victim, type: ActType.ToChar);
            ch.Act("$n completely engulfs $N in water, drowning them without mercy.\n\r", victim, type: ActType.ToRoomNotVictim);

            if (spellcraft)
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Drowning))
                dam /= 2;
            if (checkSector)
                dam *= 2;

            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Drowning);
        } // end engulf
        public static void SpellDrown(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.WaterSwim) || (sector == SectorTypes.Swim) ||
                (sector == SectorTypes.WaterNoSwim) || (sector == SectorTypes.Ocean) ||
                (sector == SectorTypes.River) || (sector == SectorTypes.Underwater));

            var dam = ch.GetDamage(level, .5f, 1); //dam = Utility.Random(dam_each[level], dam_each[level] * 2);

            ch.Act(checkSector ? "Because of the available water nearby, you conjure a greater ball of water to drown $N in." :
                "You conjure a magical ball of water to drown $N in.\n\r", victim, type: ActType.ToChar);
            ch.Act("$n conjures a magical ball of water to drown $N in.\n\r", victim, type: ActType.ToRoomNotVictim);

            if (spellcraft)
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Drowning))
                dam /= 2;
            if (checkSector)
                dam *= 2;

            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Drowning);
        } // end drown
        public static void SpellGeyser(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.WaterSwim) || (sector == SectorTypes.Swim) ||
                (sector == SectorTypes.WaterNoSwim) || (sector == SectorTypes.Ocean) ||
                (sector == SectorTypes.River) || (sector == SectorTypes.Underwater));

            var dam = ch.GetDamage(level, 1.5f, 2.5f);

            ch.Act(checkSector ? "Because of the available water nearby, you conjure a greater geyser to drown $N in." :
                "You conjure a geyser of water to drown $N in.\n\r", victim, type: ActType.ToChar);
            ch.Act("$n conjures a geyser of water to drown $N in.\n\r", victim, type: ActType.ToRoomNotVictim);

            if (spellcraft)
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Drowning))
                dam /= 2;
            if (checkSector)
                dam = (int)(dam * 1.5); ;

            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Drowning);
        } // end drown
        public static void SpellImmolation(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));

            foreach (var Victim in ch.Room.Characters.ToArray())
            {
                if (!Victim.IsSameGroup(ch) && !Victim.IsAffected(AffectFlags.Immolation) && !Victim.IsAffected(spell))
                {
                    var Affect = new AffectData();
                    Affect.ownerName = ch.Name;
                    Affect.displayName = spell.name;
                    Affect.skillSpell = spell;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.None;
                    Affect.flags.SETBIT(AffectFlags.Immolation);
                    Affect.modifier = 0;
                    Affect.duration = Game.PULSE_TICK * 2;
                    Affect.frequency = Frequency.Violence;

                    Affect.endMessage = "You stop burning.";
                    Affect.endMessageToRoom = "$n stops burning.";

                    Victim.AffectToChar(Affect);

                    ch.Act("$N starts burning from your immolation.", Victim, type: ActType.ToChar);
                    ch.Act("You start burning from $n's immolation.\n\r\n\r", Victim, type: ActType.ToVictim);
                    ch.Act("$N starts burning from $n's immolation.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);

                    if (Victim.Fighting == null)
                    {
                        var Weapon = Victim.GetEquipment(WearSlotIDs.Wield);
                        Combat.oneHit(Victim, ch, Weapon);
                    }
                }
                var dam = ch.GetDamage(level, .5f, 1); //dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                if (spellcraft)
                    dam += level;
                if (SavesSpell(level, victim, WeaponDamageTypes.Fire))
                    dam /= 2;

                Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Fire);
            }
        } // end immolation
        public static void ImmolationDamage(Character victim, AffectData affect)
        {
            var DamAmountByLevel = new SortedList<int, int>
            {
                { 40, 37 },
                { 45, 45 },
                { 50, 55 },
                { 51, 60 },
            };

            var DamAmount = (from keyvaluepair in DamAmountByLevel where affect.level >= keyvaluepair.Key select keyvaluepair.Value).Max();
            Combat.Damage(victim, victim, Utility.Random(DamAmount - 5, DamAmount + 5), affect.skillSpell, WeaponDamageTypes.Fire, affect.ownerName);
        }
        public static void SpellTsunami(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var sector = ch.Room.sector;
            var checkSector = (sector == SectorTypes.WaterSwim) || (sector == SectorTypes.Swim) ||
                        (sector == SectorTypes.WaterNoSwim) || (sector == SectorTypes.Ocean) ||
                        (sector == SectorTypes.River) || (sector == SectorTypes.Underwater);
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act(checkSector ? "Because of the available water nearby, You conjure a massive ball of water, then throw a mammoth tsunami!\n\r" :
                "You conjure a magical ball of water then throw a tsunami!\n\r", victim, type: ActType.ToChar);
            ch.Act("$n conjures a magical ball of water then throws a tsunami!\n\r", victim, type: ActType.ToRoomNotVictim);

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {

                    var dam = ch.GetDamage(level, .5f, 1); //dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                    if (spellcraft)
                        dam += level;

                    if (SavesSpell(level, victim, WeaponDamageTypes.Drowning))
                        dam /= 2;

                    if (checkSector)
                        dam *= 2;

                    Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Drowning);
                    ch.CheckImprove(spell, true, 1);
                }
            }
        } // end tsunami
        public static void SpellPillarOfTheHeavens(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var sector = ch.Room.sector;
            var checkSector = (sector == SectorTypes.Inside) || (sector == SectorTypes.Underground) ||
                (sector == SectorTypes.Cave);
            var spellcraft = (CheckSpellcraft(ch, spell));
            int chance = 0, numhits = 0, i = 0, dam = 0;
            int learned = 0;

            ch.Act(checkSector ? "Because you are not outside, You conjure a week barrage of lighting bolts.\n\r" :
                "You conjure a barrage of lightning bolts!\n\r", victim, type: ActType.ToChar);
            ch.Act("$n conjures a barrage of lightning bolts!\n\r", victim, type: ActType.ToRoomNotVictim);

            learned = ch.GetSkillPercentage(spell) + 20;

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {
                    chance = Utility.NumberPercent();

                    if ((chance + learned) > 165)
                    {
                        numhits = 4;
                    }
                    else if ((chance + learned) > 155)
                    {
                        numhits = 3;
                    }
                    else if ((chance + learned) > 145)
                    {
                        numhits = 2;
                    }
                    else
                    {
                        numhits = 1;
                    }
                    for (i = 0; i < numhits; i++)
                    {
                        dam = ch.GetDamage(level, 1, 2);

                        if (checkSector)
                            dam /= 2;
                        if (spellcraft)
                            dam += level;
                        if (SavesSpell(level, victim, WeaponDamageTypes.Lightning))
                            dam /= 2;

                        Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Lightning);

                        if (ch.Room != victim.Room)
                            break;
                    }
                    ch.CheckImprove(spell, true, 1);
                }
            }
        } // end tsunami
        public static void SpellIceshards(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            int chance = 0, numhits = 0, i = 0, dam = 0;
            int learned = 0;
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act("You conjure a magical bolt of ice, then throw it at $N.\n\r", victim, type: ActType.ToChar);
            ch.Act("$n conjures a magical bolt of ice, then throws it at $N.\n\r", victim, type: ActType.ToRoomNotVictim);

            learned = ch.GetSkillPercentage(spell) + 20;
            chance = Utility.NumberPercent();

            if ((chance + learned) > 165)
            {
                numhits = 4;
            }
            else if ((chance + learned) > 155)
            {
                numhits = 3;
            }
            else if ((chance + learned) > 145)
            {
                numhits = 2;
            }
            else
            {
                numhits = 1;
            }
            for (i = 0; i < numhits; i++)
            {
                dam = ch.GetDamage(level);
                if (spellcraft)
                    dam += level;
                if (SavesSpell(level, victim, WeaponDamageTypes.Cold))
                    dam /= 2;
                Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Cold);

                if (ch.Room != victim.Room)
                    break;
            }
        } // end iceshards
        public static void SpellWindWall(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));

            ch.Act("The air surrounding you grows and blasts out!", null, null, null, ActType.ToChar);
            ch.Act("$n makes the air grow into a forceful blast!", null, null, null, ActType.ToRoom);

            foreach (var vict in ch.Room.Characters.ToArray())
            {
                if (vict != ch && !ch.IsSameGroup(vict) && !IsSafeSpell(ch, vict, true))
                {

                    var dam = ch.GetDamage(level); //dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                    if (spellcraft)
                        dam += level;
                    if (SavesSpell(level, victim, WeaponDamageTypes.Air))
                        dam /= 2;

                    Combat.Damage(ch, vict, dam, spell, WeaponDamageTypes.Air);
                    ch.CheckImprove(spell, true, 1);
                }
            }
        } // end wind wall
        public static void SpellPebbleToBoulder(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.Mountain) || (sector == SectorTypes.Cave) || (sector == SectorTypes.Underground));
            var chance = ch.GetSkillPercentage(spell) + 20;

            ch.Act("You toss a small pebble at $N. In mid-air the pebble transforms into a huge boulder!\n\r", victim, type: ActType.ToChar);
            ch.Act("$n tosses a small pebble at $N. In mid-air the pebble transforms into a huge boulder!\n\r", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n tosses a small pebble at you. In mid-air the pebble tranforms into a huge boulder!\n\r", victim, type: ActType.ToVictim);

            if (chance > Utility.NumberPercent())
            {
                var dam = ch.GetDamage(level, 1, 2); //dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                if (spellcraft)
                    dam += level;
                if (SavesSpell(level, victim, WeaponDamageTypes.Bash))
                    dam /= 2;
                if (checkSector)
                {
                    victim.WaitState(Game.PULSE_VIOLENCE * 2);
                    dam *= 2;
                }
                else victim.WaitState(Game.PULSE_VIOLENCE);

                Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Bash);
            }
            else
            {
                Combat.Damage(ch, victim, 0, spell, WeaponDamageTypes.Bash);
            }
        } // end stoneshatter
        public static void SpellStoneShatter(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var dam = ch.GetDamage(level, .5f, 1); //dam = Utility.Random(dam_each[level], dam_each[level] * 2);

            if (spellcraft)
                dam += level;
            if (SavesSpell(level, victim, WeaponDamageTypes.Bash))
                dam /= 2;

            ch.Act("You conjure stones from thin air, then throw them at $N.\n\r", victim, type: ActType.ToChar);
            ch.Act("$n conjures stones from thin air, then throws them at $N.\n\r", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n conjures stones from thin air, then throws them you.\n\r", victim, type: ActType.ToVictim);

            Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Bash);
        } // end pebble to boulder
        public static void SpellEarthRipple(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var spellcraft = (CheckSpellcraft(ch, spell));
            var sector = ch.Room.sector;
            var checkSector = ((sector == SectorTypes.Mountain) || (sector == SectorTypes.Cave) || (sector == SectorTypes.Underground));
            string dirArg = null;
            Direction direction = Direction.North;
            string victimname = null;
            arguments = arguments.OneArgument(ref victimname);
            arguments = arguments.OneArgument(ref dirArg);
            if (dirArg.ISEMPTY()) dirArg = victimname;
            ExitData exit = null;

            if (dirArg.ISEMPTY() || !Utility.GetEnumValueStrPrefix(dirArg, ref direction))
            {
                ch.Act("Which direction did you want to force $N in?\n\r", victim);
            }
            else if ((exit = ch.Room.GetExit(direction)) == null || exit.destination == null
                || exit.flags.ISSET(ExitFlags.Closed) || exit.flags.ISSET(ExitFlags.Window) ||
                (!victim.IsImmortal && !victim.IsNPC && (exit.destination.MinLevel > victim.Level || exit.destination.MaxLevel < victim.Level)))
            {
                ch.Act("You can't force $N in that direction.", victim);
            }
            else
            {
                var dam = ch.GetDamage(level, .5f, 1); //dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                if (spellcraft)
                    dam += level;
                if (SavesSpell(level, victim, WeaponDamageTypes.Bash))
                    dam /= 2;
                if (checkSector)
                    dam *= 2;

                ch.Act(checkSector ? "Because of the earth near you, you invoke massive elemental earth beneath $N, and drive them {0}.\n\r" :
                    "You invoke elemental earth beneath $N, and drive them {0}.\n\r", victim, type: ActType.ToChar, args: direction.ToString().ToLower());
                ch.Act("$n invokes elemental earth beneath $N and drives them {0}.\n\r", victim, type: ActType.ToRoomNotVictim, args: direction.ToString().ToLower());
                ch.Act("$n invokes elemental earth beneath you and drives you {0}.\n\r", victim, type: ActType.ToVictim, args: direction.ToString().ToLower());

                Combat.Damage(ch, victim, dam, spell, WeaponDamageTypes.Bash);

                victim.RemoveCharacterFromRoom();
                victim.AddCharacterToRoom(exit.destination);
                victim.Act("$n arrives on a wave of elemental earth.\n\r", type: ActType.ToRoom);
                //Character.DoLook(victim, "auto");

                ch.Act("$n hurls $N {0} with $s earth ripple.", victim, type: ActType.ToRoomNotVictim, args: direction.ToString().ToLower());
            }
        } // end earth ripple
        public static void DoTigerFrenzy(CastType castType, SkillSpell skill, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var affect = new AffectData();

            affect.where = AffectWhere.ToAffects;
            affect.skillSpell = skill;
            affect.location = ApplyTypes.Dexterity;
            affect.duration = ch.Level / 2;
            affect.modifier = +8;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            affect.affectType = AffectTypes.GreaterEnliven;
            ch.AffectToChar(affect);

            affect.location = ApplyTypes.Strength;
            affect.modifier = +8;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            affect.endMessage = "You feel ready to enliven tiger frenzy again.";
            ch.AffectToChar(affect);

            ch.Act("You gain the strength and dexterity of a tiger!");
        } // end tiger frenzy
        public static void DoBestialFury(CastType castType, SkillSpell skill, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var affect = new AffectData();

            affect.where = AffectWhere.ToAffects;
            affect.skillSpell = skill;
            affect.flags.SETBIT(AffectFlags.BestialFury);
            affect.duration = ch.Level / 2;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            affect.affectType = AffectTypes.GreaterEnliven;
            affect.displayName = skill.name;
            affect.endMessage = "You feel ready to enliven bestial fury again.";
            ch.AffectToChar(affect);

            ch.Act("You gain bestial fury!");
        } // end bestial fury
        public static void DoPrimalTenacity(CastType castType, SkillSpell skill, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var affect = new AffectData();

            affect.where = AffectWhere.ToAffects;
            affect.skillSpell = skill;
            affect.location = ApplyTypes.SavingBreath;
            affect.duration = ch.Level / 2;
            affect.modifier = 10 + ch.Level / 4;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            affect.affectType = AffectTypes.GreaterEnliven;
            ch.AffectToChar(affect);

            affect.location = ApplyTypes.SavingPetrification;
            affect.modifier = 10 + ch.Level / 4;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            ch.AffectToChar(affect);

            affect.location = ApplyTypes.Saves;
            affect.modifier = 10 + ch.Level / 4;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            ch.AffectToChar(affect);

            affect.location = ApplyTypes.SavingRod;
            affect.modifier = 10 + ch.Level / 4;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            ch.AffectToChar(affect);

            affect.where = AffectWhere.ToResist;
            affect.DamageTypes.Add(WeaponDamageTypes.Mental);
            affect.DamageTypes.Add(WeaponDamageTypes.Negative);
            affect.level = ch.Level;
            affect.displayName = skill.name;

            affect.endMessage = "You feel ready to enliven primal tenacity again.";
            ch.AffectToChar(affect);

            ch.Act("You gain a greater defense against spells and mental damage.");
        } // end primal tenacity
        public static void DoSkinOfTheDisplacer(CastType castType, SkillSpell skill, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var affect = new AffectData();

            affect.where = AffectWhere.ToAffects;
            affect.skillSpell = skill;
            affect.flags.SETBIT(AffectFlags.SkinOfTheDisplacer);
            affect.duration = ch.Level / 2;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            affect.affectType = AffectTypes.GreaterEnliven;
            affect.displayName = skill.name;
            affect.endMessage = "You feel ready to enliven skin of the displacer again.";
            ch.AffectToChar(affect);

            ch.Act("Your skin shimmers similarly to that of a displacer beast!");
        } // end skin of the displacer
        public static void DoSlynessOfTheFox(CastType castType, SkillSpell skill, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var affect = new AffectData();

            affect.where = AffectWhere.ToAffects;
            affect.skillSpell = skill;
            affect.location = ApplyTypes.Dexterity;
            affect.duration = ch.Level / 2;
            affect.modifier = +8;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            affect.affectType = AffectTypes.GreaterEnliven;
            ch.AffectToChar(affect);

            affect.location = ApplyTypes.Move;
            affect.modifier = 125 + ch.Level;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            ch.AffectToChar(affect);

            affect.location = ApplyTypes.Hitroll;
            affect.modifier = 10 + ch.Level / 4;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            ch.AffectToChar(affect);

            affect.flags.SETBIT(AffectFlags.Sneak);
            affect.level = ch.Level;
            affect.displayName = skill.name;
            affect.endMessage = "You feel ready to enliven slyness of the fox again.";
            ch.AffectToChar(affect);

            ch.Act("You gain the agility and slyness of a fox!");
        } // end slyness of the fox
        public static void DoRecoveryOfTheSnake(CastType castType, SkillSpell skill, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var affect = new AffectData();

            affect.where = AffectWhere.ToAffects;
            affect.skillSpell = skill;
            affect.flags.SETBIT(AffectFlags.Regeneration);
            affect.duration = ch.Level / 2;
            affect.level = ch.Level;
            affect.displayName = skill.name;
            affect.affectType = AffectTypes.GreaterEnliven;
            affect.endMessage = "You feel ready to enliven recovery of the snake again.";
            ch.AffectToChar(affect);

            ch.Act("You feel the regeneration of the snake envelop you!");
        } // end recovery of the snake
        public static void SpellSate(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            if (victim.Hunger > 40)
            {
                if (victim != ch)
                    ch.send("They aready feel full and satisfied.\n\r");
                else
                    ch.send("You already feel full and satisfied.");
                return;
            }
            else
            {
                victim.send("Your hunger is temporarily sated and you feel fully satisfied.\n\r");
                victim.Act("$n looks fully satisfied instead of starving.", type: ActType.ToRoom);

                var affect = new AffectData();
                affect.where = AffectWhere.ToAffects;
                affect.skillSpell = spell;
                affect.flags.SETBIT(AffectFlags.Sated);
                affect.duration = 5 + ch.Level / 5;
                affect.displayName = spell.name;
                affect.endMessage = "You no longer feel sated.";
                victim.AffectToChar(affect);
            }
        } // end sate
        public static void SpellQuench(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
                victim = ch;

            if (victim.Thirst > 40)
            {
                if (victim != ch)
                    ch.send("They are not dehydrated.\n\r");
                else
                    ch.send("You are not dehydrated.");
                return;
            }
            else
            {
                victim.send("Your dehydration is temporarily relieved and you feel fully quenched.\n\r");
                victim.Act("$n looks fully quenched instead of dehydrated.", type: ActType.ToRoom);

                var affect = new AffectData();
                affect.where = AffectWhere.ToAffects;
                affect.skillSpell = spell;
                affect.flags.SETBIT(AffectFlags.Quenched);
                affect.duration = 5 + ch.Level / 5;
                affect.displayName = spell.name;
                affect.endMessage = "You no longer feel quenched.";
                ch.AffectToChar(affect);
            }
        } // end quench
        public static void SpellCureDeafness(CastType castType, SkillSpell spell, int ch_level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null) victim = ch;

            var deaf = (from aff in victim.AffectsList where aff.flags.ISSET(AffectFlags.Deafen) && (aff.affectType == AffectTypes.Spell || aff.affectType == AffectTypes.Commune) select aff).FirstOrDefault();

            if (deaf != null)
            {
                victim.AffectFromChar(deaf, AffectRemoveReason.Cleansed);

                if (ch != victim)
                {
                    ch.Act("You place your hand over $N's ears for a moment and restore $S hearing.", victim, null, null, ActType.ToChar);
                    ch.Act("$n places their hand over $N's ears for a moment and restores $S hearing.", victim, null, null, ActType.ToRoomNotVictim);
                    ch.Act("$n places their hand over your ears for a moment.", victim, null, null, ActType.ToVictim);
                }
                else
                    ch.send("OK.\n\r");
            }
            else
                ch.Act("$N isn't deaf in a way you can cure.", victim, type: ActType.ToChar);
        } // end cure deafness
        public static void SpellAegis(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            int count = 0;
            var types = new string[] { "fire", "cold", "lightning", "mental", "acid", "negative" };

            arguments = arguments.OneArgumentOut(out var targetname);
            arguments = arguments.OneArgumentOut(out var aegistype);

            if (victim == ch || victim == null || (aegistype.ISEMPTY() || types.Any(t => t.StringPrefix(targetname)))) ch.Act("You cannot grant the power of aegis to yourself.");

            else if ((victim = ch.GetCharacterFromRoomByName(targetname, ref count)) == null)
            {
                ch.Act("They aren't here.\n\r");
            }
            else if (aegistype.ISEMPTY())
            {
                ch.Act("Which type of protection did you want to grant $n with?" +
                " {0}", victim, args: string.Join(", ", types));
            }
            else if (!types.Any(t => t.StringPrefix(aegistype)))
            {
                ch.Act("You can't protect them from that.");
            }
            else
            {
                aegistype = types.First(t => t.StringPrefix(aegistype));
                WeaponDamageTypes damageTypes = WeaponDamageTypes.None;
                if (!Utility.GetEnumValue(aegistype, ref damageTypes)) ch.Act("You can't protect them from that.");

                else
                {
                    ch.Act("You lay hands on $N momentarily and protect them with the aegis of {0}.", victim, args: aegistype, type: ActType.ToChar);
                    ch.Act("$n is protected from {0}.", victim, args: aegistype, type: ActType.ToRoom);

                    affect = new AffectData();
                    affect.skillSpell = spell;
                    affect.level = level;
                    affect.location = ApplyTypes.None;
                    affect.where = AffectWhere.ToImmune;
                    affect.DamageTypes.SETBIT(damageTypes);
                    affect.duration = 10 + level / 4;
                    affect.displayName = spell.name + " " + aegistype;
                    affect.endMessage = "Your aegis protection wears off.\n\r";
                    affect.endMessageToRoom = "$n's aegis protection wanes.\n\r";
                    affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;

                    victim.AffectToChar(affect);
                }
            }
        } // end aegis
        public static void SpellGate(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            Character other;
            int count = 0;

            if ((other = ch.GetCharacterFromListByName(Character.Characters, arguments, ref count)) != null && other.Room != null && (ch.IsImmortal || ch.IsNPC || (ch.Level <= other.Room.MaxLevel && ch.Level >= other.Room.MinLevel)))
            {
                ch.Act("$n creates a gate, then steps in, then $n and the gate disappears.\n\r", type: ActType.ToRoom);
                ch.RemoveCharacterFromRoom();
                ch.Act("You gate to $N.", other);
                ch.AddCharacterToRoom(other.Room);
                
                ch.Act("A gate suddenly appears, then $n steps out.", type: ActType.ToRoom);
                //Character.DoLook(ch, "auto");
            }
            else
            {
                ch.Act("Who did you want to gate to?");
            }
        } // end gate
        public static void SpellHaven(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if (victim.IsAffected(AffectFlags.Sanctuary))
            {
                if (victim != ch)
                    ch.send("They are already protected by a white aura.\n\r");
                else
                    ch.send("You are already protected by a white aura.");
            }
            else if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Haven))
            {
                if (victim != ch)
                    ch.send("They are already protected by a haven aura.\n\r");
                else
                    ch.send("You are already protected by a haven aura.");
            }
            else
            {
                victim.send("A haven aura surrounds you.\n\r");
                victim.Act("A haven aura surrounds $n.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.Haven);
                affect.duration = 10 + level / 4;
                affect.displayName = "haven";
                affect.endMessage = "Your haven aura fades.\n\r";
                affect.endMessageToRoom = "$n's haven aura fades.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
            }
        } // end haven
        public static void SpellCalm(CastType castType, SkillSpell spell, int level, Character ch, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            ch.Act("You pray a prayer of calming.", type: ActType.ToChar);
            ch.Act("$n prays a prayer of calming.", type: ActType.ToRoom);

            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (victim.Fighting != null) Combat.StopFighting(victim, true);

                victim.LastFighting = null;

                if (!victim.IsAffected(spell))
                {
                    var Affect = new AffectData();

                    Affect.displayName = spell.name;
                    Affect.skillSpell = spell;
                    Affect.level = level;
                    Affect.duration = 2;
                    Affect.flags.Add(AffectFlags.Calm);

                    Affect.endMessage = "You are no longer calmed.";
                    Affect.endMessageToRoom = "$n is no longer calmed.";

                    victim.AffectToChar(Affect);
                }

            }
        } // end calm
        public static void SpellRestoration(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (GroupMember.HitPoints < GroupMember.MaxHitPoints)
                {
                    SpellCureCritical(castType, spell, level, ch, GroupMember, null, "", TargetIsType.targetChar);
                }
                else
                    GroupMember.Act("Your health is already fully restored.");
            }
        } // end restoration
        public static void SpellSpiritShepherd(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData na, string arguments, TargetIsType target)
        {
            if ((victim = Character.GetCharacterWorld(ch, arguments)) == null
                || victim == ch
                || victim.Room == ch.Room
                || victim.Room == null
                || victim.IsNPC
                )
            {
                ch.send("You failed.\n\r");
                return;
            }

            if (!ch.Room.items.Any(item => item.ItemType.ISSET(ItemTypes.PC_Corpse) && item.Owner.IsName(victim.Name)))
            {
                ch.Act("You don't see $N's corpse here.");
            }
            else
            {
                victim.Act("$n disappears suddenly.", type: ActType.ToRoom);
                victim.RemoveCharacterFromRoom();
                ch.Act("$n has guided you through the ether to your corpse!", victim, type: ActType.ToVictim);
                victim.AddCharacterToRoom(ch.Room);
                victim.Act("$n is guided through the ether to $s corpse.", type: ActType.ToRoom);
                
            }
        } // end spirit shepherd
        public static void SpellTurnUndead(CastType castType, SkillSpell spell, int level, Character ch, Character na, ItemData item, string arguments, TargetIsType target)
        {
            var race = new string[] { "lich", "skeleton", "undead", "ghoul", "spirit", "zombie" };

            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (victim != ch && victim.IsNPC && victim.Race != null && (race.Contains(victim.Race.name) || victim.Flags.ISSET(ActFlags.Undead)))
                {
                    if (victim.Level <= level - 10)
                    {
                        ch.Act("$n's powerful prayer completely disintregates $N.", victim, type: ActType.ToRoomNotVictim);
                        ch.Act("$n's powerful prayer completely disintregrated you.", victim, type: ActType.ToVictim);
                        ch.Act("Your powerful prayer completely disintegrated $N.", victim, type: ActType.ToChar);
                        victim.HitPoints = -15;
                        Combat.CheckIsDead(ch, victim, 15);
                    }
                    else
                    {
                        ch.Act("$n prays a powerful prayer against $N.", victim, type: ActType.ToRoomNotVictim);
                        ch.Act("$n prays a powerful prayer against you.", victim, type: ActType.ToVictim);
                        ch.Act("You pray a powerful prayer against $N.", victim, type: ActType.ToChar);
                        var damage = ch.GetDamage(level, 2, 3, 10);

                        Combat.Damage(ch, victim, damage, spell, WeaponDamageTypes.Wrath);
                    }
                }
            }
        } // end turn undead
        public static void SpellHealingSleep(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
            {
                ch.Act("You can't find them.\n\r");
            }
            else if (victim.Fighting != null || victim.Position == Positions.Fighting)
            {
                ch.Act("$N is still fighting.", victim);
            }
            else if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Sleep))
            {
                ch.Act("$N is already forced asleep.", victim);
            }
            else if (victim.IsSameGroup(ch) && !victim.IsAffected(spell))
            {
                Combat.StopFighting(victim, true);
                victim.Position = Positions.Sleeping;
                victim.send("You fall into a very deep...  deep......... zzzzzz.\n\r");
                victim.Act("$n falls into a deep deep sleep.", type: ActType.ToRoom);

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.Sleep);
                affect.duration = 1;
                affect.displayName = "sleep";

                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
            }
        }
        public static void EndHealingSleep(Character victim, AffectData affect)
        {
            if (affect.duration <= 0)
            {
                victim.HitPoints = victim.MaxHitPoints;
                victim.ManaPoints = victim.MaxManaPoints;
                victim.MovementPoints = victim.MaxMovementPoints;

                foreach (var malady in victim.AffectsList.ToArray())
                {
                    if (malady.affectType == AffectTypes.Malady)
                        victim.AffectFromChar(malady, AffectRemoveReason.Cleansed);

                }
                victim.Act("You wake up, fully healed, free from all maladies and ready to move.");
                victim.Act("$n wakes up fully healed, free from all maladies and ready to move.", type: ActType.ToRoom);
            }
        } // end endhealingsleep
        public static void SpellGroupTeleport(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {

            var newroom = RoomData.Rooms.Values.SelectRandom();

            if (ch.Room == null || (!ch.IsNPC && ch.Room.flags.ISSET(RoomFlags.NoRecall)) || (!ch.IsNPC && ch.Fighting != null))
            {
                ch.Act("You failed.\n\r");
                return;
            }
            else if (newroom == null)
            {
                ch.send("You failed.\n\r");
            }
            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if(GroupMember.IsImmortal || GroupMember.IsNPC || (GroupMember.Level <= newroom.MaxLevel && GroupMember.Level >= newroom.MinLevel))
                GroupMember.Act("$n vanishes!", type: ActType.ToRoom);
                GroupMember.RemoveCharacterFromRoom();
                GroupMember.AddCharacterToRoom(newroom);
                GroupMember.Act("$n slowly fades into existence.", type: ActType.ToRoom);
                //Character.DoLook(GroupMember, "auto");
            }
        } // end group teleport
        public static void SpellMassHealing(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (GroupMember.HitPoints < GroupMember.MaxHitPoints)
                {
                    SpellHeal(castType, spell, level, ch, GroupMember, null, "", TargetIsType.targetChar);

                    if (GroupMember.MovementPoints < GroupMember.MaxMovementPoints)
                    {
                        SpellRefresh(castType, spell, level, ch, GroupMember, null, "", TargetIsType.targetChar);
                    }
                }
                else
                    GroupMember.Act("Your health is already fully restored.");
            }
        } // end mass healing
        public static void SpellCleanse(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            if (victim == null)
            {
                ch.Act("You can't find them.\n\r");
            }
            else if (victim.Fighting != null || victim.Position == Positions.Fighting)
            {
                ch.Act("$N is still fighting.", victim);
            }
            else
            {
                foreach (var malady in victim.AffectsList.ToArray())
                {
                    if (malady.affectType == AffectTypes.Malady)
                        victim.AffectFromChar(malady, AffectRemoveReason.Cleansed);
                }
                victim.Act("You are now free from all maladies.");
                ch.Act("$N is free from all maladies.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("You cleanse $N of all maladies.", victim);
            }
        }  // end cleanse
        public static void SpellKnowAlignment(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.KnowAlignment))
            {
                if (victim != ch)
                    ch.send("They can already sense the alignment of others.\n\r");
                else
                    ch.send("You can already sense the alignment of others.\n\r");
                return;
            }
            else
            {
                victim.send("You feel able to sense the alignment of others.\n\r");

                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.KnowAlignment);
                affect.duration = 10 + level / 3;
                affect.displayName = "know alignment";
                affect.endMessage = "Your feel less aware of others alignment.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
            }
        } // end know alignment
        public static void SpellProtection(CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            AffectData affect;
            if (victim == null)
                victim = ch;

            if ((affect = victim.FindAffect(spell)) != null || victim.IsAffected(AffectFlags.Protection))
            {
                if (victim != ch)
                    ch.send("They already have protection.\n\r");
                else
                    ch.send("You already have protection.");
                return;
            }
            else
            {
                victim.send("You gain protection.\n\r");
                victim.Act("$n gains protection.", type: ActType.ToRoom);
                affect = new AffectData();
                affect.skillSpell = spell;
                affect.level = level;
                affect.location = ApplyTypes.None;
                affect.where = AffectWhere.ToAffects;
                affect.flags.Add(AffectFlags.Protection);
                affect.duration = 10 + level / 4;
                affect.displayName = "protection";
                affect.endMessage = "You feel less protected.\n\r";
                affect.endMessageToRoom = "$n's protection wanes.\n\r";
                affect.affectType = castType == CastType.Cast ? AffectTypes.Spell : AffectTypes.Commune;
                victim.AffectToChar(affect);
            }
        }
    }

} // End Magic
