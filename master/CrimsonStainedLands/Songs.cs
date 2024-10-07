using CrimsonStainedLands.Extensions;
using CrimsonStainedLands.World;
using System;
using System.Collections.Generic;
using System.Linq;
using static CrimsonStainedLands.Magic;

namespace CrimsonStainedLands
{
    public static class Songs
    {
        public static void InitializeSongSkills()
        {
            //var SongSkillEntry = new SkillSpell("travelers march", SongTravelersMarch, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("charismatic prelude", SongCharismaticPrelude, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("adagio", SongAdagio, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("elven adagio", SongElvenAdagio, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("piercing dissonance", SongPiercingDissonance, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("battaglia", SongBattaglia, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("canticle", SongCanticle, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("languid carol", SongLanguidCarol, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("anthem of resistance", SongAnthemOfResistance, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("riddle of revelation", SongRiddleOfRevelation, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("reveille", SongReveille, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("pastoral of the mind", SongPastoralOfTheMind, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("tranquil serenade", SongTranquilSerenade, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("requiem", SongRequiem, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("elegy of tears", SongElegyOfTears, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("bagatelle of bravado", SongBagatelleOfBravado, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("laborious lament", SongLaboriousLament, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("vibrato", SongVibrato, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("apocalyptic overture", SongApocalypticOverture, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("fantasia of palliation", SongFantasiaOfPalliation, game.PULSE_VIOLENCE);
            //SongSkillEntry = new SkillSpell("dirge of sacrifice", SongDirgeOfSacrifice, game.PULSE_VIOLENCE) { TickFunction=DirgeImmolation};
            //SongSkillEntry = new SkillSpell("grand nocturne", SongGrandNocturne, game.PULSE_VIOLENCE);

        }


        public static void DoSing(Character ch, string argument)
        {
            Magic.CastCommuneOrSing(ch, argument, CastType.Sing);
        } // end DoSing



        public static void SongTravelersMarch(CastType castType, SkillSpell song, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var RefreshAmountByLevel = new SortedList<int, int>
            {
                { 0, 20 },
                { 15, 30 },
                { 25, 50 },
            };

            var RefreshAmount = (from keyvaluepair in RefreshAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (GroupMember.MovementPoints < GroupMember.MaxMovementPoints && !GroupMember.IsAffected(AffectFlags.Deafen))
                {
                    var FuzzyRefreshAmount = Utility.Random(RefreshAmount - 5, RefreshAmount + 5);

                    GroupMember.MovementPoints = Math.Min(GroupMember.MovementPoints + FuzzyRefreshAmount, GroupMember.MaxMovementPoints);

                    GroupMember.Act("You feel able to walk further.\n\r\n\r");
                }
                else
                    GroupMember.Act("Your feet are already fully rested.\n\r\n\r");
            }

        }
        public static void SongAdagio(CastType castType, SkillSpell song, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var HealAmountByLevel = new SortedList<int, int>
            {
                { 0, 20 },
                { 15, 30 },
                { 25, 50 },
                { 30, 70 },
                { 40, 80 },
                { 50, 100 },
            };

            var HealAmount = (from keyvaluepair in HealAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (GroupMember.HitPoints < GroupMember.MaxHitPoints && !GroupMember.IsAffected(AffectFlags.Deafen))
                {
                    var FuzzyRefreshAmount = Utility.Random(HealAmount - 5, HealAmount + 5);

                    GroupMember.HitPoints = Math.Min(GroupMember.HitPoints + FuzzyRefreshAmount, GroupMember.MaxHitPoints);

                    GroupMember.Act("You feel a little better.\n\r\n\r");
                }
                else
                    GroupMember.Act("Your health is already fully restored.\n\r\n\r");
            }
        }
        public static void SongElvenAdagio(CastType castType, SkillSpell song, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var HealAmountByLevel = new SortedList<int, int>
            {
                { 0, 50 },
                { 15, 70 },
                { 25, 100 },
                { 35, 125 },
                { 45, 175 },
                { 50, 225 },
            };

            var HealAmount = (from keyvaluepair in HealAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (GroupMember.HitPoints < GroupMember.MaxHitPoints && !GroupMember.IsAffected(AffectFlags.Deafen))
                {
                    var FuzzyRefreshAmount = Utility.Random(HealAmount - 10, HealAmount + 10);

                    GroupMember.HitPoints = Math.Min(GroupMember.HitPoints + FuzzyRefreshAmount, GroupMember.MaxHitPoints);

                    GroupMember.Act("You feel much better.\n\r\n\r");
                }
                else
                    GroupMember.Act("Your health is already fully restored.\n\r\n\r");
            }
        }

        public static void SongCharismaticPrelude(CastType castType, SkillSpell song, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var CharismaAmountByLevel = new SortedList<int, int>
            {
                { 0, 4 },
                { 15, 6 },
                { 25, 8 },
            };

            var DurationAmountByLevel = new SortedList<int, int>
            {
                { 0, 5 },
                { 15, 10 },
                { 25, 12 },
            };

            var CharismaAmount = (from keyvaluepair in CharismaAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();
            var Duration = (from keyvaluepair in DurationAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (!GroupMember.IsAffected(song) && !GroupMember.IsAffected(AffectFlags.Deafen))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.Charisma;
                    Affect.modifier = CharismaAmount;
                    Affect.duration = Duration;
                    Affect.endMessage = "You feel less lovely.";
                    Affect.endMessageToRoom = "$n looks less lovely.";

                    GroupMember.AffectToChar(Affect);

                    GroupMember.Act("You feel more lovely.\n\r\n\r");
                }
                else if (!GroupMember.IsAffected(AffectFlags.Deafen))

                {
                    GroupMember.Act("You already feel more lovely.\n\r\n\r");
                }
            }
        }
        public static void SongCanticle(CastType castType, SkillSpell song, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var BlessAmountByLevel = new SortedList<int, int>
            {
                { 0, 4 },
                { 15, 5 },
                { 25, 6 },
                { 35, 8 },
                { 45, 10 },
            };

            var DurationAmountByLevel = new SortedList<int, int>
            {
                { 0, 5 },
                { 15, 10 },
                { 25, 12 },
            };

            var BlessAmount = (from keyvaluepair in BlessAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();
            var Duration = (from keyvaluepair in DurationAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (!GroupMember.IsAffected(song) && !GroupMember.IsAffected(AffectFlags.Deafen))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.Hitroll;
                    Affect.modifier = BlessAmount;
                    Affect.duration = Duration;


                    GroupMember.AffectToChar(Affect);
                    Affect.modifier = -BlessAmount;
                    Affect.location = ApplyTypes.SavingSpell;

                    Affect.endMessage = "You feel less blessed.";
                    Affect.endMessageToRoom = "$n looks less blessed.";

                    GroupMember.AffectToChar(Affect);

                    GroupMember.Act("You feel blessed.\n\r\n\r");
                }
                else if (!GroupMember.IsAffected(AffectFlags.Deafen))

                {
                    GroupMember.Act("You already feel blessed.\n\r\n\r");
                }
            }
        }
        public static void SongPiercingDissonance(CastType castType, SkillSpell song, int level, Character ch, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (!victim.IsSameGroup(ch) && !victim.IsAffected(AffectFlags.Deafen))
                {
                    var damage = Utility.dice(3, level + 2, level);
                    Combat.Damage(ch, victim, damage, song, WeaponDamageTypes.Sound);
                }
            }
        }
        public static void SongBattaglia(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var Victim in bard.Room.Characters.ToArray())
            {
                if (!Victim.IsSameGroup(bard) && Victim.Fighting != null && Victim.Fighting.IsSameGroup(bard)
                    && !Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.Hitroll;
                    Affect.modifier = level / -5;
                    Affect.duration = 2;

                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.DamageRoll;

                    Affect.endMessage = "You feel less frightened.";
                    Affect.endMessageToRoom = "$n looks less frigntened.";

                    Victim.AffectToChar(Affect);

                    bard.Act("You feel frightened by $n's battaglia.\n\r\n\r", Victim, type: ActType.ToVictim);
                    bard.Act("$N appears to be frightened by your battaglia.", Victim, type: ActType.ToChar);
                    bard.Act("$N appears to be frightened by $n's battaglia.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);
                }
                else if (bard.IsSameGroup(Victim) && !Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.Hitroll;
                    Affect.modifier = level / 5;
                    Affect.duration = 2;

                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.DamageRoll;

                    Affect.endMessage = "You feel less frenzied.";
                    Affect.endMessageToRoom = "$n looks less frenzied.";

                    Victim.AffectToChar(Affect);

                    if (bard != Victim)
                    {
                        bard.Act("You feel frenzied by $n's battaglia.\n\r\n\r", Victim, type: ActType.ToVictim);
                        bard.Act("$N appears to be frenzied by your battaglia.", Victim, type: ActType.ToChar);
                        bard.Act("$N appears to be frenzied by $n's battaglia.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);
                    }
                    else
                    {
                        bard.Act("You feel frenzied by your battaglia.\n\r\n\r", Victim, type: ActType.ToVictim);
                        bard.Act("$N appears to be frenzied by $s battaglia.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);
                    }
                }
            }
        }
        public static void SongLanguidCarol(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            var StrengthAmountByLevel = new SortedList<int, int>
            {
                { 0, 4 },
                { 15, 6 },
                { 25, 8 },
                { 35, 10 },
                { 45, 12 },
            };

            var DurationAmountByLevel = new SortedList<int, int>
            {
                { 0, 5 },
                { 15, 10 },
                { 25, 12 },
            };
            var StrengthAmount = (from keyvaluepair in StrengthAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();
            var Duration = (from keyvaluepair in DurationAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var Victim in bard.Room.Characters.ToArray())
            {
                if (!Victim.IsSameGroup(bard) && !Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.Strength;
                    Affect.modifier = -StrengthAmount;
                    Affect.duration = Duration;

                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.DamageRoll;

                    Affect.endMessage = "You feel stronger.";
                    Affect.endMessageToRoom = "$n looks stronger.";

                    Victim.AffectToChar(Affect);

                    bard.Act("You feel weakened by $n's Languid Carol.\n\r\n\r", Victim, type: ActType.ToVictim);
                    bard.Act("$N appears to be weakened by your Languid Carol.", Victim, type: ActType.ToChar);
                    bard.Act("$N appears to be weakened by $n's Languid Carol.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);

                    if (Victim.Fighting == null && Victim.IsAwake)
                    {
                        var Weapon = Victim.GetEquipment(WearSlotIDs.Wield);
                        Combat.oneHit(Victim, bard, Weapon);
                    }
                }
            }
        }
        public static void SongAnthemOfResistance(CastType castType, SkillSpell song, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {

            var DurationAmountByLevel = new SortedList<int, int>
            {
                { 0, 5 },
                { 15, 8 },
                { 25, 10 },
                { 35, 12 },
                { 45, 14 },
            };

            var Duration = (from keyvaluepair in DurationAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (!GroupMember.IsAffected(song) && !GroupMember.IsAffected(AffectFlags.Deafen))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.flags.Add(AffectFlags.AnthemOfResistance);
                    Affect.duration = Duration;
                    Affect.endMessage = "You feel less resistant.";
                    Affect.endMessageToRoom = "$n looks less resistant.";

                    GroupMember.AffectToChar(Affect);

                    GroupMember.Act("You feel more resistant.\n\r\n\r");
                }
                else if (!GroupMember.IsAffected(AffectFlags.Deafen))

                {
                    GroupMember.Act("You already feel more resistant.\n\r\n\r");
                }
            }

        }
        public static void SongRiddleOfRevelation(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {

            var DurationAmountByLevel = new SortedList<int, int>
            {
                { 0, 2 },
                { 15, 4 },
                { 25, 6 },
                { 35, 8 },
                { 45, 10 },
            };
            var Duration = (from keyvaluepair in DurationAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var Victim in bard.Room.Characters.ToArray())
            {
                if (!Victim.IsSameGroup(bard) && !Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.None;
                    Affect.modifier = 0;
                    Affect.duration = Duration;
                    Affect.flags.Add(AffectFlags.FaerieFire);

                    Affect.endMessage = "You are no longer glowing.";
                    Affect.endMessageToRoom = "$n is no longer glowing.";

                    Victim.AffectToChar(Affect);

                    bard.Act("You are revealed by $n's Riddle Of Revelation.\n\r\n\r", Victim, type: ActType.ToVictim);
                    bard.Act("$N becomes revealed by your Riddle Of Revelation.", Victim, type: ActType.ToChar);
                    bard.Act("$N becomes refealed by $n's Riddle of Revelation.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);

                    Victim.StripHidden();
                    Victim.StripInvis();
                    Victim.StripSneak();
                    Victim.StripCamouflage();

                }
            }
        }
        public static void SongReveille(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var victim in bard.Room.Characters.ToArray())
            {
                if ((victim.IsAffected(AffectFlags.Sleep) || victim.IsAffected(SkillSpell.SkillLookup("strangle"))) && !victim.IsAffected(AffectFlags.Deafen))
                {

                    var effect = victim.FindAffect(AffectFlags.Sleep);
                    if (effect != null) victim.AffectFromChar(effect, AffectRemoveReason.Cleansed);

                    effect = victim.FindAffect(SkillSpell.SkillLookup("strangle"));
                    if (effect != null) victim.AffectFromChar(effect, AffectRemoveReason.Cleansed);

                    CharacterDoFunctions.DoStand(victim, "");

                    bard.Act("You are awakened by $n's Reveille.\n\r\n\r", victim, type: ActType.ToVictim);
                    bard.Act("$N is awakened by your Reveille.", victim, type: ActType.ToChar);
                    bard.Act("$N becomes awakened by $n's Reveille.\n\r\n\r", victim, type: ActType.ToRoomNotVictim);
                }
            }

        }
        public static void SongPastoralOfTheMind(CastType castType, SkillSpell song, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var ManaAmountByLevel = new SortedList<int, int>
            {
                { 0, 100 },
                { 15, 100 },
                { 25, 100 },
                { 30, 120 },
                { 35, 140 },
                { 40, 160 },
                { 45, 180 },
                { 50, 200 },
            };

            var ManaAmount = (from keyvaluepair in ManaAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (GroupMember.ManaPoints < GroupMember.MaxManaPoints && !GroupMember.IsAffected(AffectFlags.Deafen))
                {
                    var FuzzyManaAmount = Utility.Random(ManaAmount - 10, ManaAmount + 10);

                    GroupMember.ManaPoints = Math.Min(GroupMember.ManaPoints + FuzzyManaAmount, GroupMember.MaxManaPoints);

                    GroupMember.Act("You feel better able to concentrate.\n\r\n\r");
                }
                else
                    GroupMember.Act("Your ability to concentrate is already accomplished.\n\r\n\r");
            }

        }
        public static void SongTranquilSerenade(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var victim in bard.Room.Characters.ToArray())
            {
                if (!victim.IsAffected(AffectFlags.Deafen))
                {
                    if (victim.Fighting != null) Combat.StopFighting(victim, true);
                    if (!victim.IsAffected(song))
                    {
                        var Affect = new AffectData();

                        Affect.displayName = song.name;
                        Affect.skillSpell = song;
                        Affect.level = level;
                        Affect.duration = 2;
                        Affect.flags.Add(AffectFlags.Calm);

                        Affect.endMessage = "You are no longer calmed.";
                        Affect.endMessageToRoom = "$n is no longer calmed.";

                        victim.AffectToChar(Affect);

                    }

                    bard.Act("You are calmed by $n's Tranquil Serenade.\n\r\n\r", victim, type: ActType.ToVictim);
                    bard.Act("$N is calmed by your Tranquil Serenade.", victim, type: ActType.ToChar);
                    bard.Act("$N becomes calmed by $n's Tranquil Serenade.\n\r\n\r", victim, type: ActType.ToRoomNotVictim);
                }
            }

        }
        public static void SongRequiem(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
                      
            ItemData corpse= null;

            foreach (var Item in bard.Room.items)

            {
                if (Item.ItemType.ISSET(ItemTypes.PC_Corpse)) corpse=item;
            }

            if (corpse == null) { bard.send("There is no player corpse here to sing over.\n\r"); return; }

            foreach (var Victim in bard.Room.Characters.ToArray())
            {
                if (!Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.AC;
                    Affect.modifier = -40;
                    Affect.duration = 100;
                    Affect.flags.Add(AffectFlags.EnhancedFastHealing);

                    Affect.endMessage = "You are no longer inspired.";
                    Affect.endMessageToRoom = "$n is no longer inspired.";

                    Victim.AffectToChar(Affect);

                    bard.Act("You are inspired by $n's song of Requiem over $p.\n\r\n\r", Victim,corpse, type: ActType.ToVictim);
                    bard.Act("$N becomes inspired by your song of Requiem.", Victim, type: ActType.ToChar);
                    bard.Act("$N becomes inspired by $n's song of Requiem over $p.\n\r\n\r", Victim,corpse, type: ActType.ToRoomNotVictim);

                }

            }

        }
        public static void SongElegyOfTears(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var victim in bard.Room.Characters.ToArray())
            {
                if (!victim.IsAffected(AffectFlags.Deafen))
                {
                    if (victim.LastFighting != null)
                    {
                        victim.LastFighting = null;

                        bard.Act("$N is calmed by your Elegy Of Tears.", victim, type: ActType.ToChar);
                        bard.Act("$N becomes calmed by $n's Elegy Of Tears.\n\r\n\r", victim, type: ActType.ToRoomNotVictim);
                    }
                }
            }

        }
        public static void SongBagatelleOfBravado(CastType castType, SkillSpell song, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType target)
        {
            var StatAmountByLevel = new SortedList<int, int>
            {
                { 10, 40 },
                { 20, 50 },
                { 28, 70 },
                { 30, 90 },
                { 32, 110 },
                { 34, 130 },
                { 38, 145 },
                { 41, 155 },
                { 45, 170 },
            };

            var DurationAmountByLevel = new SortedList<int, int>
            {
                { 0, 5 },
                { 15, 5 },
                { 28, 5 },
                { 30, 6 },
                { 32, 8 },
                { 34, 10 },
                { 38, 12 },
                { 41, 14 },
                { 45, 16 },
            };

            var StatAmount = (from keyvaluepair in StatAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();
            var Duration = (from keyvaluepair in DurationAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var GroupMember in ch.GetGroupMembersInRoom())
            {
                if (!GroupMember.IsAffected(song) && !GroupMember.IsAffected(AffectFlags.Deafen))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.Hp;
                    Affect.modifier = StatAmount;
                    Affect.duration = Duration;

                    GroupMember.AffectToChar(Affect);
                    Affect.location = ApplyTypes.Mana;

                    Affect.endMessage = "You feel more vulnerable.";
                    Affect.endMessageToRoom = "$n looks more vulnerable.";

                    GroupMember.AffectToChar(Affect);

                    GroupMember.Act("You feel more confidence.\n\r\n\r");
                }
                else if (!GroupMember.IsAffected(AffectFlags.Deafen))

                {
                    GroupMember.Act("You already feel more confident.\n\r\n\r");
                }
            }
        }
        public static void SongLaboriousLament(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            var DexAmountByLevel = new SortedList<int, int>
            {
                { 0, 5 },
                { 15, 5 },
                { 29, 5 },
                { 32, 6 },
                { 36, 7 },
                { 39, 8 },
                { 42, 9 },
                { 44, 10 },
                { 46, 11 },
                { 50, 12 },
            };

            var DurationAmountByLevel = new SortedList<int, int>
            {
                { 0, 5 },
                { 29, 5 },
                { 33, 6 },
                { 37, 7 },
                { 41, 8 },
                { 45, 10 },
                { 50, 12 },
            };
            var DexAmount = (from keyvaluepair in DexAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();
            var Duration = (from keyvaluepair in DurationAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var Victim in bard.Room.Characters.ToArray())
            {
                if (!Victim.IsSameGroup(bard) && !Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.Strength;
                    Affect.modifier = -DexAmount;
                    Affect.duration = Duration;

                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.Dex;

                    Affect.endMessage = "You feel more coordinated.";
                    Affect.endMessageToRoom = "$n looks more coordinated.";

                    Victim.AffectToChar(Affect);

                    bard.Act("You feel less coordinated by $n's Laborious Lament.\n\r\n\r", Victim, type: ActType.ToVictim);
                    bard.Act("$N appears to be less coordinated by your Laborious Lament.", Victim, type: ActType.ToChar);
                    bard.Act("$N appears to be less coordinated by $n's Laborious Lament.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);

                    if (Victim.Fighting == null && Victim.IsAwake)
                    {
                        var Weapon = Victim.GetEquipment(WearSlotIDs.Wield);
                        Combat.oneHit(Victim, bard, Weapon);
                    }
                }
            }
        }
        public static void SongVibrato(CastType castType, SkillSpell song, int level, Character ch, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (!victim.IsSameGroup(ch) && !victim.IsAffected(AffectFlags.Deafen))
                {
                    var damage = Utility.dice(3, level + 2, level*2);
                    Combat.Damage(ch, victim, damage, song, WeaponDamageTypes.Blast);
                }
            }
        }
        public static void SongApocalypticOverture(CastType castType, SkillSpell song, int level, Character ch, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            var fire = Utility.Random(0, 1) == 1;

            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (!victim.IsSameGroup(ch) && !victim.IsAffected(AffectFlags.Deafen))
                {
                    var damage = Utility.dice(3, level + 10, level * 2);
                    Combat.Damage(ch, victim, damage, fire ? "apocalyptic blaze":"apocalyptic frost", fire ? WeaponDamageTypes.Fire : WeaponDamageTypes.Cold);
                }
            }
        }
        public static void SongFantasiaOfPalliation(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            var DurationAmountByLevel = new SortedList<int, int>
            {
                { 0, 5 },
                { 29, 5 },
                { 33, 6 },
                { 37, 7 },
                { 41, 8 },
                { 45, 10 },
                { 50, 12 },
            };
            var Duration = (from keyvaluepair in DurationAmountByLevel where level >= keyvaluepair.Key select keyvaluepair.Value).Max();

            foreach (var Victim in bard.Room.Characters.ToArray())
            {
                if (!Victim.IsSameGroup(bard) && !Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();
                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.Strength;
                    Affect.modifier = (int)(-Victim.GetCurrentStat(PhysicalStatTypes.Strength)*.1);
                    Affect.duration = Duration;
                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.Dex;
                    Affect.modifier = (int)(-Victim.GetCurrentStat(PhysicalStatTypes.Dexterity) * .1);
                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.Int;
                    Affect.modifier = (int)(-Victim.GetCurrentStat(PhysicalStatTypes.Intelligence) * .1);
                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.Wis;
                    Affect.modifier = (int)(-Victim.GetCurrentStat(PhysicalStatTypes.Wisdom) * .1);
                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.Con;
                    Affect.modifier = (int)(-Victim.GetCurrentStat(PhysicalStatTypes.Constitution) * .1);
                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.Chr;
                    Affect.modifier = (int)(-Victim.GetCurrentStat(PhysicalStatTypes.Charisma) * .1);
                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.Hp;
                    Affect.modifier = (int)(-Victim.MaxHitPoints * .1);
                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.DamageRoll;
                    Affect.modifier = (int)(-Victim.DamageRoll * .1);
                    Victim.AffectToChar(Affect);

                    Affect.location = ApplyTypes.Hitroll;
                    Affect.modifier = (int)(-Victim.HitRoll * .1);

                    Affect.endMessage = "You feel less disheartened.";
                    Affect.endMessageToRoom = "$n looks disheartened.";
                    Victim.AffectToChar(Affect);

                    bard.Act("You feel less disheartened by $n's Fantasia Of Palliation.\n\r\n\r", Victim, type: ActType.ToVictim);
                    bard.Act("$N appears to be less disheartened by your Fantasia Of Palliation.", Victim, type: ActType.ToChar);
                    bard.Act("$N appears to be less disheartened by $n's Fantasia Of Palliation.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);

                    if (Victim.Fighting == null && Victim.IsAwake)
                    {
                        var Weapon = Victim.GetEquipment(WearSlotIDs.Wield);
                        Combat.oneHit(Victim, bard, Weapon);
                    }
                }
            }
        }
        public static void DirgeImmolation(Character victim, AffectData affect)
        {
            var DamAmountByLevel = new SortedList<int, int>
            {
                { 40, 37 },
                { 45, 45 },
                { 50, 55 },
                { 51, 60 },
            };

            var DamAmount = (from keyvaluepair in DamAmountByLevel where affect.level >= keyvaluepair.Key select keyvaluepair.Value).Max();
            Combat.Damage(victim, victim, Utility.Random(DamAmount-5, DamAmount+5),affect.skillSpell, WeaponDamageTypes.Fire, affect.ownerName);
        }
        public static void SongDirgeOfSacrifice(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
           foreach (var Victim in bard.Room.Characters.ToArray())
            {
                if (!Victim.IsSameGroup(bard) && !Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();
                    Affect.ownerName = bard.Name;
                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.location = ApplyTypes.None;
                    Affect.modifier = 0;
                    Affect.duration = Game.PULSE_TICK *2;
                    Affect.frequency= Frequency.Violence;

                    Affect.endMessage = "You stop burning.";
                    Affect.endMessageToRoom = "$n stops burning.";

                    Victim.AffectToChar(Affect);

                    bard.Act("You feel immolated by $n's Dirge Of Sacrifice.\n\r\n\r", Victim, type: ActType.ToVictim);
                    bard.Act("$N appears to be afflicted by immolation from your Dirge Of Sacrifice.", Victim, type: ActType.ToChar);
                    bard.Act("$N appears to be afflicted by immolation from $n's Dirge Of Sacrifice.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);

                    if (Victim.Fighting == null && Victim.IsAwake)
                    {
                        var Weapon = Victim.GetEquipment(WearSlotIDs.Wield);
                        Combat.oneHit(Victim, bard, Weapon);
                    }
                }
            }
        }
        public static void SongGrandNocturne(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var Victim in bard.Room.Characters.ToArray())
            {
                if (!Victim.IsSameGroup(bard)
                    && !Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.modifier = 0;
                    Affect.duration = 2;
                    Affect.flags.Add(AffectFlags.GrandNocturne);

                    Affect.endMessage = "You feel less ineffective.";
                    Affect.endMessageToRoom = "$n looks less ineffective.";

                    Victim.AffectToChar(Affect);

                    bard.Act("You feel ineffective from $n's battaglia.\n\r\n\r", Victim, type: ActType.ToVictim);
                    bard.Act("$N appears to be very ineffective by your battaglia.", Victim, type: ActType.ToChar);
                    bard.Act("$N appears to be ineffective by $n's battaglia.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);
                }
            }
        }

        public static void SongLullaby(CastType castType, SkillSpell song, int level, Character bard, Character NA, ItemData item, string arguments, TargetIsType target)
        {
            foreach (var Victim in bard.Room.Characters.ToArray())
            {
                if (!Victim.IsSameGroup(bard)
                    && !Victim.IsAffected(AffectFlags.Deafen) && !Victim.IsAffected(song))
                {
                    var Affect = new AffectData();

                    Affect.displayName = song.name;
                    Affect.skillSpell = song;
                    Affect.level = level;
                    Affect.where = AffectWhere.ToAffects;
                    Affect.modifier = 0;
                    Affect.duration = 5;
                    Affect.flags.Add(AffectFlags.Sleep);

                    Affect.endMessage = "You feel less sleepy.";
                    Affect.endMessageToRoom = "$n looks less sleepy.";

                    Victim.AffectToChar(Affect);
                    Victim.Position = Positions.Sleeping;
                    bard.Act("You feel sleepy from $n's lullaby.\n\r\n\r", Victim, type: ActType.ToVictim);
                    bard.Act("$N appears to be very sleepy by your lullaby.", Victim, type: ActType.ToChar);
                    bard.Act("$N appears to be very sleepy by $n's lullaby.\n\r\n\r", Victim, type: ActType.ToRoomNotVictim);
                }
            }
        }

    } // end class songs
} // end namespace CrimsonStainedLands
