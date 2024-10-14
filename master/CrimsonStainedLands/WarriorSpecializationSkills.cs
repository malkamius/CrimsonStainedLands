using CrimsonStainedLands.Extensions;
using CrimsonStainedLands.World;
using System;
using System.Linq;

namespace CrimsonStainedLands
{
    public static partial class Combat
    {
        public partial class WarriorSpecializationSkills
        {
            public static void DoWhirl(Character ch, string arguments)
            {
                Character victim = null;
                SkillSpell skill = SkillSpell.SkillLookup("whirl");
                int whirlPercent;
                ItemData weapon;
                if ((whirlPercent = ch.GetSkillPercentage(skill)) <= 1)
                {
                    if (!ch.CheckSocials("whirl", arguments))
                        ch.Act("You don't know how to do that.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                }
                else if (((weapon = ch.GetEquipment(WearSlotIDs.Wield)) == null || weapon.WeaponType != WeaponTypes.Axe) &&
                    ((weapon = ch.GetEquipment(WearSlotIDs.DualWield)) == null || weapon.WeaponType != WeaponTypes.Axe))
                {
                    ch.Act("You must be wielding an axe to whirl it at someone.");
                }
                else
                {
                    float chance = 0;
                    chance += ch.TotalWeight / 25;
                    chance -= victim.TotalWeight / 20;
                    chance += (ch.Size - victim.Size) * 20;
                    chance -= victim.GetCurrentStat(PhysicalStatTypes.Dexterity);
                    chance += ch.GetCurrentStat(PhysicalStatTypes.Strength) / 3;
                    chance += ch.GetCurrentStat(PhysicalStatTypes.Dexterity) / 2;

                    chance = chance * (whirlPercent / 100);

                    ch.WaitState(skill.waitTime);

                    if (chance > Utility.NumberPercent())
                    {

                        if (!victim.IsAffected(skill))
                        {
                            var whirlAffect = new AffectData()
                            {
                                skillSpell = skill,
                                displayName = "whirl",
                                duration = 5,
                                modifier = -5,
                                location = ApplyTypes.Strength,
                                affectType = AffectTypes.Skill,
                                level = ch.Level,
                            };

                            victim.AffectToChar(whirlAffect);

                            whirlAffect.location = ApplyTypes.Dexterity;
                            whirlAffect.endMessage = "You recover from your injury, feeling stronger and more agile.";
                            whirlAffect.endMessageToRoom = "$n recovers from $s injury, looking stronger and more agile.";
                            victim.AffectToChar(whirlAffect);
                        }

                        ch.Act("You whirl $p at $N.", victim, weapon);
                        ch.Act("$n whirls $p at you.", victim, weapon, type: ActType.ToVictim);
                        ch.Act("$n whirls $p at $N.", victim, weapon, type: ActType.ToRoomNotVictim);

                        float damage = weapon.DamageDice.Roll() + ch.DamageRoll;

                        CheckEnhancedDamage(ch, ref damage);

                        ch.CheckImprove(skill, true, 1);

                        Combat.Damage(ch, victim, (int)damage, skill, WeaponDamageTypes.Slice);
                    }
                    else
                    {
                        ch.Act("You whirl $p at $N but fail to make contact.", victim, weapon);
                        ch.Act("$n whirls $p at you but fails to make contact.", victim, weapon, type: ActType.ToVictim);
                        ch.Act("$n whirls $p at $N but fails to make contact.", victim, weapon, type: ActType.ToRoomNotVictim);

                        ch.CheckImprove(skill, false, 1);

                        Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Slice);
                    }
                }
            } // end DoWhirl

            public static void DoBoneshatter(Character ch, string arguments)
            {
                Character victim = null;
                SkillSpell skill = SkillSpell.SkillLookup("boneshatter");
                int skillPercent;
                ItemData weapon;
                if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                }
                else if (((weapon = ch.GetEquipment(WearSlotIDs.Wield)) == null || weapon.WeaponType != WeaponTypes.Mace) &&
                   ((weapon = ch.GetEquipment(WearSlotIDs.DualWield)) == null || weapon.WeaponType != WeaponTypes.Mace))
                {
                    ch.Act("You must be wielding a mace to shatter someone's bones.");
                }
                else
                {
                    ch.WaitState(skill.waitTime);

                    float chance = skillPercent + 20;

                    if (chance > Utility.NumberPercent())
                    {

                        if (!victim.IsAffected(skill))
                        {
                            var boneshatterAffect = new AffectData()
                            {
                                skillSpell = skill,
                                displayName = "boneshatter",
                                duration = 5,
                                modifier = -10,
                                location = ApplyTypes.Strength,
                                affectType = AffectTypes.Skill,
                                level = ch.Level,
                            };

                            boneshatterAffect.endMessage = "Your bones feel better.";

                            victim.AffectToChar(boneshatterAffect);
                        }

                        ch.Act("You shatter $N's bones with $p.", victim, weapon);
                        ch.Act("$n shatters your bones with $p!", victim, weapon, type: ActType.ToVictim);
                        ch.Act("$n shatters $N's bones with $p!", victim, weapon, type: ActType.ToRoomNotVictim);

                        float damage = weapon.DamageDice.Roll() + ch.DamageRoll;

                        CheckEnhancedDamage(ch, ref damage);

                        ch.CheckImprove(skill, true, 1);

                        Combat.Damage(ch, victim, (int)damage, skill, WeaponDamageTypes.Slice);
                    }
                    else
                    {
                        ch.Act("You attempt to shatter $N's bones with $p.", victim, weapon);
                        ch.Act("$n attempts to shatter your bones with $p!", victim, weapon, type: ActType.ToVictim);
                        ch.Act("$n attempts to shatter $N's bones with $p!", victim, weapon, type: ActType.ToRoomNotVictim);

                        ch.CheckImprove(skill, false, 1);

                        Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Slice);
                    }
                }
            } // end DoBoneshatter

            public static void DoCrossDownParry(Character ch, string arguments)
            {
                Character victim = null;
                SkillSpell skill = SkillSpell.SkillLookup("cross down parry");
                int skillPercent;
                ItemData weapon;
                if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (((weapon = ch.GetEquipment(WearSlotIDs.Wield)) == null || weapon.WeaponType != WeaponTypes.Sword) ||
                   ((weapon = ch.GetEquipment(WearSlotIDs.DualWield)) == null || weapon.WeaponType != WeaponTypes.Sword))
                {
                    ch.Act("You must be dual wielding swords to perform a cross down parry.");
                }
                else
                {
                    ch.WaitState(skill.waitTime);

                    float chance = skillPercent + 20;

                    if (chance > Utility.NumberPercent())
                    {
                        float damage = weapon.DamageDice.Roll() + ch.DamageRoll;

                        CheckEnhancedDamage(ch, ref damage);

                        var bleeding = SkillSpell.SkillLookup("bleeding");
                        if (Utility.Random(1, 4) == 1 && !victim.IsAffected(bleeding))
                        {
                            var bleedaffect = new AffectData()
                            {
                                skillSpell = skill,
                                displayName = "bleeding",
                                duration = 5,
                                modifier = -4,
                                location = ApplyTypes.Strength,
                                affectType = AffectTypes.Skill,
                                level = ch.Level,
                            };

                            bleedaffect.endMessage = "Your nose stops bleeding.";

                            victim.AffectToChar(bleedaffect);
                            damage += Utility.dice(4, 6, 6);
                            ch.Act("You brings both your swords up, crossing them, and kicking $N's nose, causing it to bleed.", victim);
                            ch.Act("$n brings both $s swords up, crossing them, and kicking your nose, causing it to bleed.", victim, type: ActType.ToVictim);
                            ch.Act("$n brings both $s swords up, crossing them, and kicking $N's nose, causing it to bleed.", victim, type: ActType.ToRoomNotVictim);

                        }
                        else
                        {
                            ch.Act("You brings both your swords up, crossing them, and kick $N in the face.", victim);
                            ch.Act("$n brings both $s swords up, crossing them, and kicking your face.", victim, type: ActType.ToVictim);
                            ch.Act("$n brings both $s swords up, crossing them, and kicking $N's face.", victim, type: ActType.ToRoomNotVictim);
                        }

                        ch.CheckImprove(skill, true, 1);

                        Combat.Damage(ch, victim, (int)damage, skill, WeaponDamageTypes.Bash);
                    }
                    else
                    {
                        ch.Act("You attempt to cross your swords and kick $N but fail.", victim);
                        ch.Act("$n attempts to cross $s swords and kick you, but fails!", victim, type: ActType.ToVictim);
                        ch.Act("$n attempts to cross $s swords and kick $N's face, but fails!", victim, type: ActType.ToRoomNotVictim);

                        ch.CheckImprove(skill, false, 1);

                        Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
                    }
                }
            } // end DoBoneshatter

            public static void DoPummel(Character ch, string arguments)
            {
                Character victim = null;
                SkillSpell skill = SkillSpell.SkillLookup("pummel");
                int skillPercent;
                var level = ch.Level;
                float dam;

                var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,  10, 13, 15, 20, 25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };

                if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (ch.GetEquipment(WearSlotIDs.Wield) != null ||
                   ch.GetEquipment(WearSlotIDs.DualWield) != null ||
                   ch.GetEquipment(WearSlotIDs.Held) != null ||
                   ch.GetEquipment(WearSlotIDs.Shield) != null)
                {
                    ch.Act("Your hands must be empty to pummel.");
                }
                else
                {
                    ch.WaitState(skill.waitTime);

                    float chance = skillPercent + 20;

                    if (chance > Utility.NumberPercent())
                    {
                        ch.WaitState(skill.waitTime);

                        if (ch.IsNPC)
                            level = Math.Min(level, 51);
                        level = Math.Min(level, dam_each.Length - 1);
                        level = Math.Max(0, level);

                        ch.Act("You unleash a series of punches, pummeling $N.", victim);
                        ch.Act("$n unleashes a series of punches, pummeling you!", victim, type: ActType.ToVictim);
                        ch.Act("$n unleashes a series of punches, pummeling $N.", victim, type: ActType.ToRoomNotVictim);

                        ch.CheckImprove(skill, true, 1);

                        for (var counter = 0; counter < Utility.Random(1, 6); counter++)
                        {
                            dam = Utility.Random(dam_each[level] / 2, dam_each[level]);

                            Combat.Damage(ch, victim, (int)dam, skill, WeaponDamageTypes.Bash);
                            if (victim.Room != ch.Room || victim.Fighting == null || ch.Fighting == null)
                                break;
                        }
                    }
                    else
                    {
                        ch.Act("You attempt to unleash a series of punches, but can't get close enough.", victim);
                        ch.Act("$n attempts to unleash a series of punches against you, but fails to get close!", victim, type: ActType.ToVictim);
                        ch.Act("$n attempts to unleash a series of punches against $N, but fails to get close!", victim, type: ActType.ToRoomNotVictim);

                        ch.CheckImprove(skill, false, 1);

                        Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
                    }
                }
            } // end DoPummel

            public static void DoBackhand(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("backhand");
                int chance;
                float damage;
                Character victim = null;
                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Mace) ||
                    ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Mace))
                {
                    ch.Act("You must be wielding a mace to backhand your enemy.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    if (chance > Utility.NumberPercent())
                    {
                        ch.Act("$n backhands $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                        ch.Act("$n backhands you with $p.", victim, wield, type: ActType.ToVictim);
                        ch.Act("You backhand $N with $p.", victim, wield, type: ActType.ToChar);

                        damage = wield.DamageDice.Roll() + ch.DamageRoll;

                        CheckEnhancedDamage(ch, ref damage);

                        ch.CheckImprove(skill, true, 1);
                        Combat.Damage(ch, victim, (int)damage, skill, WeaponDamageTypes.Bash);

                    }
                    else
                    {
                        ch.Act("$n attempts to hit $N with a backhand blow.", victim, type: ActType.ToRoomNotVictim);
                        ch.Act("$n tries to hit you with a backhand blow!", victim, type: ActType.ToVictim);
                        ch.Act("You try to hit $N with a backhand blow.", victim, type: ActType.ToChar);

                        ch.CheckImprove(skill, false, 1);
                        Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
                    }
                }
            } // end backhand
            
            public static void DoSting(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("sting");
                int chance;
                int dam;
                Character victim = null;
                chance = ch.GetSkillPercentage(skill) + 20;
                if (ch.Form == null)
                {
                    ItemData wield = null;

                    if (chance <= 21)
                    {
                        ch.send("You don't know how to do that.\n\r");
                        return;
                    }

                    if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Whip) &&
                        ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Whip))
                    {
                        ch.send("You must be wielding a whip to sting your enemy.");
                        return;
                    }



                    if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                    {
                        ch.send("You aren't fighting anyone.\n\r");
                        return;
                    }
                    else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                    {
                        ch.send("You don't see them here.\n\r");
                        return;
                    }

                    ch.WaitState(skill.waitTime);
                    if (chance > Utility.NumberPercent())
                    {
                        ch.Act("$n stings $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                        ch.Act("$n stings you with $p.", victim, wield, type: ActType.ToVictim);
                        ch.Act("You sting $N with $p.", victim, wield, type: ActType.ToChar);

                        dam = wield.DamageDice.Roll() + ch.DamageRoll;
                        ch.CheckImprove(skill, true, 1);
                        Combat.Damage(ch, victim, dam, "stinging lash", WeaponDamageTypes.Pierce);

                    }
                    else
                    {
                        ch.Act("$n attempts to sting $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                        ch.Act("$n tries to sting you with $p!", victim, wield, type: ActType.ToVictim);
                        ch.Act("You try to sting $N with $p.", victim, wield, type: ActType.ToChar);

                        ch.CheckImprove(skill, false, 1);
                        Combat.Damage(ch, victim, 0, "stinging lash", WeaponDamageTypes.Pierce);
                    }
                } // end form == null
                else // form != null
                {
                    ch.WaitState(skill.waitTime);
                    if (chance < 21)
                    {
                        ch.send("You don't know how to do that.\n\r");
                    }
                    else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                    {
                        ch.send("You aren't fighting anyone.\n\r");
                        return;
                    }
                    else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                    {
                        ch.send("You don't see them here.\n\r");
                        return;
                    }
                    else if (chance < Utility.NumberPercent())
                    {
                        ch.Act("You try to sting $N but miss.");
                        ch.Act("$n tries to sting you but misses.", victim, type: ActType.ToVictim);
                        ch.Act("$n tries to sting $N but misses.", victim, type: ActType.ToRoomNotVictim);

                        Combat.Damage(ch, victim, 0, "sting", WeaponDamageTypes.Sting);

                    }
                    else
                    {
                        ch.Act("You strike quickly with your stinger, injecting $N with poison.", victim);
                        ch.Act("$n strikes quickly with $s stinger, injecting you with poison.", victim, type: ActType.ToVictim);
                        ch.Act("$n strikes quickly with $s stinger, injecting $N with poison.", victim, type: ActType.ToRoomNotVictim);

                        var dam_each = new int[]
                        {
                            30,
                            50,
                            80,
                            100
                        };
                        var level = 3 - (int)ch.Form.Tier;

                        dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                        Combat.Damage(ch, victim, dam, "sting", WeaponDamageTypes.Sting);

                        AffectData poison = victim.FindAffect(skill);

                        if (poison != null)
                        {
                            poison.duration = 2 + level * 2;
                            ch.Act("$n's sting .", victim, type: ActType.ToVictim);
                        }
                        else
                        {
                            poison = new AffectData()
                            {
                                ownerName = ch.Name,
                                skillSpell = skill,
                                duration = 2 + level * 2,
                                endMessage = "The poison from your sting ebbs.",
                                endMessageToRoom = "$n's poison from $s sting ebbs.",
                                location = ApplyTypes.Strength,
                                modifier = -4
                            };
                            victim.AffectToChar(poison);
                            ch.Act("Poison flows through your veins from $n's sting.", victim, type: ActType.ToVictim);
                            ch.Act("Poison flows through $N's veins from your sting.", victim, type: ActType.ToChar);
                            ch.Act("Poison flows through $N's veins from $n's sting.", victim, type: ActType.ToRoomNotVictim);
                        }
                    }
                }
                return;
            } // end sting

            public static void DoBludgeon(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("bludgeon");
                int chance;
                int dam;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.send("You don't know how to do that.\n\r");
                    return;
                }

                if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Flail) &&
                    ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Flail))
                {
                    ch.send("You must be wielding a flail to bludgeon your enemy.");
                    return;
                }

                Character victim = null;

                if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.send("You aren't fighting anyone.\n\r");
                    return;
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.send("You don't see them here.\n\r");
                    return;
                }

                ch.WaitState(skill.waitTime);
                if (chance > Utility.NumberPercent())
                {
                    ch.Act("$n bludgeons $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n bludgeons you with $p.", victim, wield, type: ActType.ToVictim);
                    ch.Act("You bludgeons $N with $p.", victim, wield, type: ActType.ToChar);

                    dam = wield.DamageDice.Roll() + ch.DamageRoll;
                    ch.CheckImprove(skill, true, 1);
                    Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

                }
                else
                {
                    ch.Act("$n attempts to bludgeons $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to bludgeon you with $p!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You try to bludgeon $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
                }
                return;
            } // end bludgeon


            public static void DoLegsweep(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("legsweep");
                int chance;
                int dam;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.send("You don't know how to do that.\n\r");
                    return;
                }

                if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || (wield.WeaponType != WeaponTypes.Polearm && wield.WeaponType != WeaponTypes.Staff && wield.WeaponType != WeaponTypes.Spear)) &&
                    ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || (wield.WeaponType != WeaponTypes.Polearm && wield.WeaponType != WeaponTypes.Staff && wield.WeaponType != WeaponTypes.Spear)))
                {
                    ch.send("You must be wielding a spear, staff or polearm to legsweep your enemy.");
                    return;
                }

                Character victim = null;

                if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.send("You aren't fighting anyone.\n\r");
                    return;
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.send("You don't see them here.\n\r");
                    return;
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n legsweeps $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n legsweeps you with $p.", victim, wield, type: ActType.ToVictim);
                    ch.Act("You legsweep $N with $p.", victim, wield, type: ActType.ToChar);
                    victim.WaitState(Game.PULSE_VIOLENCE * 2);
                    dam = Utility.Random(10, ch.Level);
                    ch.CheckImprove(skill, true, 1);
                    Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
                    CheckCheapShot(victim);
                    CheckGroundControl(victim);

                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to legsweep $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to legsweep you with $p!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You try to legsweep $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
                }
                return;
            } // end legsweep

            public static void DoVitalArea(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("vital area");
                int chance;

                Character victim = null;
                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.send("You don't know how to do that.\n\r");
                    return;
                }

                else if (ch.GetEquipment(WearSlotIDs.Wield) != null ||
                    ch.GetEquipment(WearSlotIDs.DualWield) != null)
                {
                    ch.send("You must be using only your hands to vital area an opponent.");
                    return;
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.send("You aren't fighting anyone.\n\r");
                    return;
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.send("You don't see them here.\n\r");
                    return;
                }
                else if (victim.IsAffected(skill))
                {
                    ch.send("You have already struck their vital areas.");
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n strikes $N's vital areas.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n strikes your vital areas.", victim, type: ActType.ToVictim);
                    ch.Act("You strike $N's vital areas.", victim, type: ActType.ToChar);

                    var affect = new AffectData()
                    {
                        skillSpell = skill,
                        displayName = "vital area",
                        duration = 5,
                        modifier = -4,
                        location = ApplyTypes.Strength,
                        affectType = AffectTypes.Skill,
                        level = ch.Level,
                    };
                    victim.AffectToChar(affect);
                    affect.location = ApplyTypes.Dexterity;
                    affect.endMessage = "You don't feel as sore in your vital areas.";
                    victim.AffectToChar(affect);

                    ch.CheckImprove(skill, true, 1);
                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n fails to strike $N's vital areas.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to strike your vital areas, and misses!", victim, type: ActType.ToVictim);
                    ch.Act("You try to strike $N's vital areas and miss.", victim, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                }
                return;
            } // end vital area

            public static void DoDoubleThrust(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("double thrust");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;
                ItemData dualwield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Sword ||
                    (dualwield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || dualwield.WeaponType != WeaponTypes.Sword)
                {
                    ch.Act("You must be dual wielding swords to double thrust.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.Act("$n double thrusts $N with $p and $P.", victim, wield, dualwield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n double thrusts you with $p and $P.", victim, wield, dualwield, type: ActType.ToVictim);
                    ch.Act("You backhand $N with $p and $P.", victim, wield, dualwield, type: ActType.ToChar);

                    ch.CheckImprove(skill, true, 1);

                    damage = wield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    damage *= 2;
                    Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);

                    damage = dualwield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    damage *= 2;
                    Combat.Damage(ch, victim, (int)damage, skill, dualwield.WeaponDamageType.Type);

                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to double thrust $N with $p and $P.", victim, wield, dualwield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to double thrust you with $p and $P!", victim, wield, dualwield, type: ActType.ToVictim);
                    ch.Act("You try to double thrust $N with $p and $P.", victim, wield, dualwield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
                }
            } // end double thrust

            public static void DoJab(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("jab");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Sword) &&
                    ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Sword))
                {
                    ch.Act("You must be wielding a sword to jab.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.Act("$n jabs $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n jabs you with $p.", victim, wield, type: ActType.ToVictim);
                    ch.Act("You jabs $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, true, 1);

                    damage = wield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    damage *= 2;
                    Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);

                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to jab $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to jab you with $p!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You try to jab $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
                }
            } // end jab

            public static void DoChop(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("chop");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Polearm))
                {
                    ch.Act("You must be wielding a polearm to chop.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.Act("$n chops $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n chops you with $p.", victim, wield, type: ActType.ToVictim);
                    ch.Act("You chop $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, true, 1);

                    damage = wield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);

                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to chop $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to chop you with $p!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You try to chop $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
                }
            } // end chop

            public static void DoCrescentStrike(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("crescent strike");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Spear))
                {
                    ch.Act("You must be wielding a spear to crescent strike.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.Act("$n crescent strikes $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n crescent strikes you with $p.", victim, wield, type: ActType.ToVictim);
                    ch.Act("You crescent strike $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, true, 1);

                    damage = wield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);

                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to crescent strike $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to crescent strike you with $p!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You try to crescent strike $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
                }
            } // end crescent strike

            public static void DoOverhead(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("overhead");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Axe))
                {
                    ch.Act("You must be wielding an axe in your main hand to overhead attack.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.Act("$n overhead attacks $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n overhead attacks you with $p.", victim, wield, type: ActType.ToVictim);
                    ch.Act("You overhead attack $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, true, 1);

                    damage = wield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    damage *= 3;
                    Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);
                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to overhead attack $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to overhead attack you with $p!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You try to overhead attack $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
                }
            } // end overhead

            public static void DoDisembowel(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("disembowel");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Axe))
                {
                    ch.Act("You must be wielding an axe in your main hand to attempt this move.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if (victim.HitPoints.Percent(victim.MaxHitPoints) > 30)
                {
                    ch.Act("They aren't hurt enough to attempt disembowelment.");
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.CheckImprove(skill, true, 1);

                    if (Utility.Random(1, 5) == 1)
                    {
                        ch.Act("$n +++DISEMBOWELS+++ $N.", victim, type: ActType.ToRoomNotVictim);
                        ch.Act("$n +++DISEMBOWELS+++ you.", victim, type: ActType.ToVictim);
                        ch.Act("You +++DISEMBOWEL+++ $N.", victim, type: ActType.ToChar);

                        victim.HitPoints = -15;
                        Combat.CheckIsDead(ch, victim, -1);
                    }
                    else
                    {
                        ch.Act("$n attempts a sweeping gut shot!", victim, wield, type: ActType.ToRoomNotVictim);
                        ch.Act("$n attempts a sweeping gut shot!", victim, wield, type: ActType.ToVictim);
                        ch.Act("You attempt a sweeping gut shot!", victim, wield, type: ActType.ToChar);
                        damage = wield.DamageDice.Roll() + ch.DamageRoll;
                        CheckEnhancedDamage(ch, ref damage);
                        damage *= 3;
                        Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);
                    }
                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts a sweeping gut shot, but misses entirely!", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n attempts a sweeping gut shot, but misses entirely!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You attempt a sweeping gut shot, but misses entirely!", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
                }
            } // end disembowel

            public static void DoPincer(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("pincer");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;
                ItemData dualwield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Axe ||
                    (dualwield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || dualwield.WeaponType != WeaponTypes.Axe)
                {
                    ch.Act("You must be dual wielding an axes to attempt this move.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.CheckImprove(skill, true, 1);


                    ch.Act("$n attempts to pincer $N with $s axes!", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n pincers you with $s axes!", victim, type: ActType.ToVictim);
                    ch.Act("You pincer $N with your axes!", victim, type: ActType.ToChar);
                    damage = wield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    damage *= 2;
                    Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);

                    if (!victim.IsAffected(skill))
                    {
                        var affect = new AffectData()
                        {
                            where = AffectWhere.ToAffects,
                            location = ApplyTypes.Strength,
                            modifier = -5,
                            duration = 5,
                            displayName = "pincer",
                        };
                        victim.AffectToChar(affect);
                        affect.endMessage = "You feel better from the pincer.";
                        affect.location = ApplyTypes.Dexterity;
                        victim.AffectToChar(affect);
                    }
                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to pincer $N with $s axes, but misses entirely!", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n attempts to pincer you with $s axes, but misses entirely!", victim, type: ActType.ToVictim);
                    ch.Act("You attempt to pincer $N with your axes!", victim, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, wield.WeaponDamageType.Type);
                }
            } // end pincer

            public static void DoUnderhandStab(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("underhand stab");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Dagger) &&
                    ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Dagger))
                {
                    ch.Act("You must be wielding a dagger to underhand stab $N.", victim);
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.Act("$n underhand stabs $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n underhand stabs you with $p.", victim, wield, type: ActType.ToVictim);
                    ch.Act("You underhand stab $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, true, 1);

                    damage = wield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    damage *= 2;
                    Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);

                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to underhand stab $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to underhand stab you with $p!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You try to underhand stab $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, wield.WeaponDamageType.Type);
                }
            } // end underhand stab

            public static void DoLeverageKick(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("leverage kick");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || (wield.WeaponType != WeaponTypes.Spear && wield.WeaponType != WeaponTypes.Staff)))
                {
                    ch.Act("You must be wielding a spear or staff to leverage kick.");
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.Act("$n leverage kicks $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n leverage kicks you with $p.", victim, wield, type: ActType.ToVictim);
                    ch.Act("You leverage kick $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, true, 1);

                    damage = wield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);

                    victim.WaitState(Game.PULSE_VIOLENCE * 2);
                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to leverage kick $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to leverage kick you with $p!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You try to leverage kick $N with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
                }
            } // end leverage kick

            public static void DoCranial(Character ch, string arguments)
            {
                var skill = SkillSpell.SkillLookup("cranial");
                int chance;
                float damage;
                Character victim = null;

                ItemData wield = null;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.Act("You don't know how to do that.");
                }
                else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
                {
                    ch.Act("You aren't fighting anyone.");
                }
                else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
                {
                    ch.Act("You don't see them here.");
                    return;
                }
                else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Mace)
                {
                    ch.Act("You must be wielding a mace in your main hand to cranial.");
                }
                else if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime);

                    ch.Act("$n strikes $N on the head with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n strikes you on the head with $p.", victim, wield, type: ActType.ToVictim);
                    ch.Act("You strikes $N on the head with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, true, 1);

                    damage = wield.DamageDice.Roll() + ch.DamageRoll;
                    CheckEnhancedDamage(ch, ref damage);
                    Combat.Damage(ch, victim, (int)damage, skill, wield.WeaponDamageType.Type);

                    if (Utility.Random(1, 3) == 1)
                    {
                        if (victim is NPCData && ((NPCData)victim).Protects.Any())
                        {
                            victim.Act("$n resists passing out from the blow to $s head.", type: ActType.ToRoom);
                            victim.Act("You resist passing out from the blow to your head.");
                        }
                        else
                        {
                            victim.Act("$n passes out from the blow to $s head.", type: ActType.ToRoom);
                            victim.Act("You pass out from the blow to your head.");

                            Combat.StopFighting(victim, true);
                            var affect = new AffectData()
                            {
                                displayName = "cranial",
                                skillSpell = skill,
                                duration = 2,
                                affectType = AffectTypes.Skill,
                                where = AffectWhere.ToAffects,

                            };
                            affect.flags.SETBIT(AffectFlags.Sleep);
                            victim.Position = Positions.Sleeping;
                            victim.AffectToChar(affect);
                        }
                    }
                    else
                    {
                        victim.WaitState(Game.PULSE_VIOLENCE);
                    }
                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("$n attempts to strike $N on the head with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to strike you on the head with $p!", victim, wield, type: ActType.ToVictim);
                    ch.Act("You try to strike $N on the head with $p.", victim, wield, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
                }
            } // end cranial

            public static void DoEntrapWeapon(Character ch, string argument)
            {
                Character victim = null;
                ItemData obj = null;
                ItemData wield = null;

                int chance, ch_weapon, vict_weapon, ch_vict_weapon;
                SkillSpell skill = SkillSpell.SkillLookup("entrap weapon");

                if (skill == null || (chance = ch.GetSkillPercentage(skill) + 20) <= 1)
                {
                    ch.send("You don't know how to entrap an opponents weapon.\n\r");
                    return;
                }
                else if ((victim = ch.Fighting) == null)
                {
                    ch.send("You aren't fighting anyone.\n\r");
                    return;
                }
                else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Polearm)
                {
                    ch.send("You must wield a polearm to entrap an opponents weapon.\n\r");
                    return;
                }

                else if (ch.IsAffected(AffectFlags.Blind))
                {
                    ch.Act("You can't see the person to entrap $s weapon!", victim, type: ActType.ToChar);
                    return;
                }
                else if ((obj = victim.GetEquipment(WearSlotIDs.Wield)) == null && (obj = victim.GetEquipment(WearSlotIDs.DualWield)) == null)
                {
                    ch.send("Your opponent is not wielding a weapon.\n\r");
                    return;
                }
                else
                {
                    /* find weapon skills */
                    ch_weapon = ch.GetSkillPercentage(GetWeaponSkill(wield));
                    vict_weapon = victim.GetSkillPercentage(GetWeaponSkill(obj));
                    ch_vict_weapon = ch.GetSkillPercentage(GetWeaponSkill(obj));

                    /* skill */
                    chance = (chance + ch_weapon) / 2;
                    chance = (chance + ch_vict_weapon) / 2;

                    /* level */
                    chance += (ch.Level - victim.Level);

                    /* and now the entrap */
                    if (Utility.NumberPercent() < chance)
                    {
                        ch.WaitState(skill.waitTime);
                        if (obj.extraFlags.ISSET(ExtraFlags.NoRemove) || obj.extraFlags.ISSET(ExtraFlags.NoDisarm) || victim.IsAffected(SkillSpell.SkillLookup("spiderhands")))
                        {
                            ch.Act("$S weapon won't budge!", victim, type: ActType.ToChar);
                            ch.Act("$n tries to entrap your weapon, but it won't budge!", victim, type: ActType.ToVictim);
                            ch.Act("$n tries to entrap $N's weapon, but it won't budge.", victim, type: ActType.ToRoomNotVictim);
                            return;
                        }

                        ch.Act("$n ENTRAPS your weapon!", victim, type: ActType.ToVictim);
                        ch.Act("You entraps $N's weapon!", victim, type: ActType.ToChar);
                        ch.Act("$n entraps $N's weapon!", victim, type: ActType.ToRoomNotVictim);

                        //victim.Equipment[victim.GetEquipmentWearSlot(obj).id] = null;
                        victim.RemoveEquipment(obj, false, false);

                        if (!obj.extraFlags.ISSET(ExtraFlags.NoDrop) && !obj.extraFlags.ISSET(ExtraFlags.Inventory))
                        {
                            if (victim.Inventory.Contains(obj))
                                victim.Inventory.Remove(obj);

                            victim.Room.items.Insert(0, obj);
                            obj.Room = victim.Room;
                            obj.CarriedBy = null;
                            if (victim.IsNPC && victim.Wait == 0 && victim.CanSee(obj))
                            {
                                if (victim.GetItem(obj, null))
                                    victim.wearItem(obj);

                            }
                        }

                        ch.CheckImprove(skill, true, 1);
                    }
                    else
                    {
                        ch.WaitState(skill.waitTime);
                        ch.Act("You fail to entrap $N's weapon.", victim, type: ActType.ToChar);
                        ch.Act("$n tries to entrap your weapon, but fails.", victim, type: ActType.ToVictim);
                        ch.Act("$n tries to entrap $N's weapon, but fails.", victim, type: ActType.ToRoomNotVictim);
                        ch.CheckImprove(skill, false, 1);
                    }
                }
                return;
            }


            public static void DoStripWeapon(Character ch, string argument)
            {
                Character victim = null;
                ItemData obj = null;
                ItemData wield = null;

                int chance, ch_weapon, vict_weapon, ch_vict_weapon;
                SkillSpell skill = SkillSpell.SkillLookup("strip weapon");

                if (skill == null || (chance = ch.GetSkillPercentage(skill) + 20) <= 1)
                {
                    ch.send("You don't know how to strip an opponents weapon.\n\r");
                    return;
                }
                else if ((victim = ch.Fighting) == null)
                {
                    ch.send("You aren't fighting anyone.\n\r");
                    return;
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || (wield.WeaponType != WeaponTypes.Whip && wield.WeaponType != WeaponTypes.Flail))
                    && ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || (wield.WeaponType != WeaponTypes.Whip && wield.WeaponType != WeaponTypes.Flail)))
                {
                    ch.send("You must wield a whip or flail to strip an opponents weapon.\n\r");
                    return;
                }

                else if (ch.IsAffected(AffectFlags.Blind))
                {
                    ch.Act("You can't see $N to strip $s weapon!", victim, type: ActType.ToChar);
                    return;
                }
                else if ((obj = victim.GetEquipment(WearSlotIDs.Wield)) == null && (obj = victim.GetEquipment(WearSlotIDs.DualWield)) == null)
                {
                    ch.send("Your opponent is not wielding a weapon.\n\r");
                    return;
                }
                else
                {
                    /* find weapon skills */
                    ch_weapon = ch.GetSkillPercentage(GetWeaponSkill(wield));
                    vict_weapon = victim.GetSkillPercentage(GetWeaponSkill(obj));
                    ch_vict_weapon = ch.GetSkillPercentage(GetWeaponSkill(obj));

                    /* skill */
                    chance = (chance + ch_weapon) / 2;
                    chance = (chance + ch_vict_weapon) / 2;

                    /* level */
                    chance += (ch.Level - victim.Level);

                    /* and now the entrap */
                    if (Utility.NumberPercent() < chance)
                    {
                        ch.WaitState(skill.waitTime);
                        if (obj.extraFlags.ISSET(ExtraFlags.NoRemove) || obj.extraFlags.ISSET(ExtraFlags.NoDisarm) || victim.IsAffected(SkillSpell.SkillLookup("spiderhands")))
                        {
                            ch.Act("$S weapon won't budge!", victim, type: ActType.ToChar);
                            ch.Act("$n tries to strip your weapon, but it won't budge!", victim, type: ActType.ToVictim);
                            ch.Act("$n tries to strip $N's weapon, but it won't budge.", victim, type: ActType.ToRoomNotVictim);
                            return;
                        }

                        ch.Act("$n STRIPS your weapon!", victim, type: ActType.ToVictim);
                        ch.Act("You strip $N's weapon!", victim, type: ActType.ToChar);
                        ch.Act("$n strips $N's weapon!", victim, type: ActType.ToRoomNotVictim);

                        //victim.Equipment[victim.GetEquipmentWearSlot(obj).id] = null;
                        victim.RemoveEquipment(obj, false, false);

                        if (!obj.extraFlags.ISSET(ExtraFlags.NoDrop) && !obj.extraFlags.ISSET(ExtraFlags.Inventory))
                        {
                            if (victim.Inventory.Contains(obj))
                                victim.Inventory.Remove(obj);

                            victim.Room.items.Insert(0, obj);
                            obj.Room = victim.Room;
                            obj.CarriedBy = null;
                            if (victim.IsNPC && victim.Wait == 0 && victim.CanSee(obj))
                            {
                                if (victim.GetItem(obj, null))
                                    victim.wearItem(obj);

                            }
                        }

                        ch.CheckImprove(skill, true, 1);
                    }
                    else
                    {
                        ch.WaitState(skill.waitTime);
                        ch.Act("You fail to strip $N's weapon.", victim, type: ActType.ToChar);
                        ch.Act("$n tries to strip your weapon, but fails.", victim, type: ActType.ToVictim);
                        ch.Act("$n tries to strip $N's weapon, but fails.", victim, type: ActType.ToRoomNotVictim);
                        ch.CheckImprove(skill, false, 1);
                    }
                }
                return;
            }


            public static void DoHookWeapon(Character ch, string argument)
            {
                Character victim = null;
                ItemData obj = null;
                ItemData wield = null;

                int chance, ch_weapon, vict_weapon, ch_vict_weapon;
                SkillSpell skill = SkillSpell.SkillLookup("hook weapon");

                if (skill == null || (chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.send("You don't know how to hook an opponents weapon.\n\r");
                    return;
                }
                else if ((victim = ch.Fighting) == null)
                {
                    ch.send("You aren't fighting anyone.\n\r");
                    return;
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Axe)
                    && ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Axe))
                {
                    ch.send("You must wield an axe to hook an opponents weapon.\n\r");
                    return;
                }

                else if (ch.IsAffected(AffectFlags.Blind))
                {
                    ch.Act("You can't see $N to hook $s weapon!", victim, type: ActType.ToChar);
                    return;
                }
                else if ((obj = victim.GetEquipment(WearSlotIDs.Wield)) == null && (obj = victim.GetEquipment(WearSlotIDs.DualWield)) == null)
                {
                    ch.send("Your opponent is not wielding a weapon.\n\r");
                    return;
                }
                else
                {
                    /* find weapon skills */
                    ch_weapon = ch.GetSkillPercentage(GetWeaponSkill(wield));
                    vict_weapon = victim.GetSkillPercentage(GetWeaponSkill(obj));
                    ch_vict_weapon = ch.GetSkillPercentage(GetWeaponSkill(obj));

                    /* skill */
                    chance = (chance + ch_weapon) / 2;
                    chance = (chance + ch_vict_weapon) / 2;

                    /* level */
                    chance += (ch.Level - victim.Level);

                    /* and now the entrap */
                    if (Utility.NumberPercent() < chance)
                    {
                        ch.WaitState(skill.waitTime);
                        if (obj.extraFlags.ISSET(ExtraFlags.NoRemove) || obj.extraFlags.ISSET(ExtraFlags.NoDisarm) || victim.IsAffected(SkillSpell.SkillLookup("spiderhands")))
                        {
                            ch.Act("$S weapon won't budge!", victim, type: ActType.ToChar);
                            ch.Act("$n tries to hook your weapon, but it won't budge!", victim, type: ActType.ToVictim);
                            ch.Act("$n tries to hook $N's weapon, but it won't budge.", victim, type: ActType.ToRoomNotVictim);
                            return;
                        }

                        ch.Act("$n HOOKS your weapon!", victim, type: ActType.ToVictim);
                        ch.Act("You hook $N's weapon!", victim, type: ActType.ToChar);
                        ch.Act("$n hooks $N's weapon!", victim, type: ActType.ToRoomNotVictim);

                        //victim.Equipment[victim.GetEquipmentWearSlot(obj).id] = null;
                        victim.RemoveEquipment(obj, false, false);

                        if (!obj.extraFlags.ISSET(ExtraFlags.NoDrop) && !obj.extraFlags.ISSET(ExtraFlags.Inventory))
                        {
                            if (victim.Inventory.Contains(obj))
                                victim.Inventory.Remove(obj);

                            victim.Room.items.Insert(0, obj);
                            obj.Room = victim.Room;
                            obj.CarriedBy = null;
                            if (victim.IsNPC && victim.Wait == 0 && victim.CanSee(obj))
                            {
                                if (victim.GetItem(obj, null))
                                    victim.wearItem(obj);

                            }
                        }

                        ch.CheckImprove(skill, true, 1);
                    }
                    else
                    {
                        ch.WaitState(skill.waitTime);
                        ch.Act("You fail to hook $N's weapon.", victim, type: ActType.ToChar);
                        ch.Act("$n tries to hook your weapon, but fails.", victim, type: ActType.ToVictim);
                        ch.Act("$n tries to hook $N's weapon, but fails.", victim, type: ActType.ToRoomNotVictim);
                        ch.CheckImprove(skill, false, 1);
                    }
                }
                return;
            }


            public static void DoWeaponBreaker(Character ch, string argument)
            {
                Character victim = null;
                ItemData obj = null;
                ItemData wield = null;

                int chance, ch_weapon, vict_weapon, ch_vict_weapon;
                SkillSpell skill = SkillSpell.SkillLookup("weapon breaker");

                if (skill == null || (chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.send("You don't know how to break an opponents weapon like that.\n\r");
                    return;
                }
                else if ((victim = ch.Fighting) == null)
                {
                    ch.send("You aren't fighting anyone.\n\r");
                    return;
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Axe)
                    && ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Axe))
                {
                    ch.send("You must wield an axe to break an opponents weapon.\n\r");
                    return;
                }

                else if (ch.IsAffected(AffectFlags.Blind))
                {
                    ch.Act("You can't see $N to break $s weapon!", victim, type: ActType.ToChar);
                    return;
                }
                else if ((obj = victim.GetEquipment(WearSlotIDs.Wield)) == null && obj.Durability > 0 && 
                    (obj = victim.GetEquipment(WearSlotIDs.DualWield)) == null && obj.Durability > 0)
                {
                    ch.send("Your opponent is not wielding a weapon you can break.\n\r");
                    return;
                }
                else
                {
                    /* find weapon skills */
                    ch_weapon = ch.GetSkillPercentage(GetWeaponSkill(wield));
                    vict_weapon = victim.GetSkillPercentage(GetWeaponSkill(obj));
                    ch_vict_weapon = ch.GetSkillPercentage(GetWeaponSkill(obj));

                    /* skill */
                    chance = (chance + ch_weapon) / 2;
                    chance = (chance + ch_vict_weapon) / 2;

                    /* level */
                    chance += (ch.Level - victim.Level);

                    /* and now the break */
                    if (Utility.NumberPercent() < chance)
                    {
                        ch.WaitState(skill.waitTime);


                        ch.Act("$n breaks your weapon with $s axe!", victim, type: ActType.ToVictim);
                        ch.Act("You break $N's with your axe!", victim, type: ActType.ToChar);
                        ch.Act("$n breaks $N's weapon with $s axe!", victim, type: ActType.ToRoomNotVictim);

                        obj.Durability = 0;

                        ch.CheckImprove(skill, true, 1);
                    }
                    else
                    {
                        ch.WaitState(skill.waitTime);
                        ch.Act("You fail to break $N's weapon.", victim, type: ActType.ToChar);
                        ch.Act("$n tries to break your weapon, but fails.", victim, type: ActType.ToVictim);
                        ch.Act("$n tries to break $N's weapon, but fails.", victim, type: ActType.ToRoomNotVictim);
                        ch.CheckImprove(skill, false, 1);
                    }
                }
                return;
            } // end weapon breaker

            public static void DoDent(Character ch, string argument)
            {
                Character victim = null;
                ItemData wield = null;

                int chance, ch_weapon;
                SkillSpell skill = SkillSpell.SkillLookup("dent");

                if (skill == null || (chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                {
                    ch.send("You don't know how to dent an opponents armor.\n\r");
                    return;
                }
                else if ((victim = ch.Fighting) == null)
                {
                    ch.send("You aren't fighting anyone.\n\r");
                    return;
                }
                else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Mace)
                    && ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Mace))
                {
                    ch.send("You must wield a mace to dent an opponents armor.\n\r");
                    return;
                }
                else if (victim.Equipment.Keys.Count == 0 || victim.Equipment.All(kvp => (kvp.Value == null || !kvp.Value.ItemType.ISSET(ItemTypes.Armor)) 
                && kvp.Value.Durability > 0))
                {
                    ch.send("They aren't wearing any armor for you to dent.\n\r");
                    return;
                }
                else
                {
                    var obj = (from item in victim.Equipment
                               where item.Value != null &&
                                    item.Key != WearSlotIDs.Wield && item.Key != WearSlotIDs.Held && item.Key != WearSlotIDs.DualWield && item.Value.Durability > 0
                                    && item.Value.ItemType.ISSET(ItemTypes.Armor) // can only dent armor
                               select item.Value).SelectRandom();
                    if(obj == null)
                    {
                        ch.send("You couldn't find any armor to dent.\n\r");
                        return;
                    }
                    /* find weapon skills */
                    ch_weapon = ch.GetSkillPercentage(GetWeaponSkill(wield));

                    /* skill */
                    chance = (chance + ch_weapon) / 2;

                    /* level */
                    chance += (ch.Level - victim.Level);

                    /* and now the break */
                    if (Utility.NumberPercent() < chance)
                    {
                        ch.WaitState(skill.waitTime);



                        ch.Act("$n dents and breaks $p with $s mace!", victim, obj, type: ActType.ToVictim);
                        ch.Act("You dent and breaks $N's $p with your mace!", victim, obj, type: ActType.ToChar);
                        ch.Act("$n dents and breaks $N's $p with $s mace!", victim, obj, type: ActType.ToRoomNotVictim);

                        obj.Durability = 0;

                        ch.CheckImprove(skill, true, 1);
                    }
                    else
                    {
                        ch.WaitState(skill.waitTime);
                        ch.Act("You fail to dent and break $N's $p.", victim, obj, type: ActType.ToChar);
                        ch.Act("$n tries to dent and break $p, but fails.", victim, obj, type: ActType.ToVictim);
                        ch.Act("$n tries to dent break $N's $p, but fails.", victim, obj, type: ActType.ToRoomNotVictim);
                        ch.CheckImprove(skill, false, 1);
                    }
                }
                return;
            } // end dent


        } // end warrior specialization skills
    } // end combat
} // end crimsonstainedlands
