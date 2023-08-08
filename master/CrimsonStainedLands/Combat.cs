using CrimsonStainedLands.Extensions;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using static CrimsonStainedLands.Magic;
using static System.Net.Mime.MediaTypeNames;

namespace CrimsonStainedLands
{
    /// <summary>
    /// unused
    /// </summary>
    public enum DamageClasses
    {
        None,
        Bash,
        Pierce,
        Slash,
        Fire,
        Cold,
        Lightning,
        Acid,
        Poison,
        Negative,
        Holy,
        Energy,
        Mental,
        Disease,
        Drowning,
        Light,
        Other,
        Harm,
        Charm,
        Sound,
        TrueStrike,
        Earth,
        Wind
    }

    public static partial class Combat
    {
        /// <summary>
        /// Checks if it is safe for a character to engage in combat with another character.
        /// God's protect if not
        /// </summary>
        /// <param name="ch">The character attempting to engage in combat.</param>
        /// <param name="victim">The character being targeted for combat.</param>
        /// <returns>True if it is safe, false otherwise.</returns>
        public static bool CheckIsSafe(Character ch, Character victim)
        {
            // If the victim is a ghost and not already in combat, it is safe
            if (victim.IsAffected(AffectFlags.Ghost) && victim.Fighting == null)
                return true;

            // If the attacker is the same as the victim, it is not safe
            if (ch == victim)
                return false;

            // If the attacker is affected by the Calm flag, they cannot engage in combat
            if (ch.IsAffected(AffectFlags.Calm))
            {
                ch.send("You feel too calm to fight.\n\r");
                StopFighting(ch);
                return true;
            }

            // Check if the attacker and victim are players or non-player characters and if they are not the same
            // Also check if the victim is a pet or has a leader who is a player
            if ((ch != null && victim != null && !ch.IsNPC && !victim.IsNPC && ch != victim) ||
                (!ch.IsNPC && (victim.Flags.ISSET(ActFlags.Pet) || (victim.Leader != null && !victim.Leader.IsNPC))))
            {
                // The gods protect the victim from the attacker
                ch.Act("The gods protect $N from you.", victim, type: ActType.ToChar);
                ch.Act("The gods protect you from $n.", victim, type: ActType.ToVictim);
                ch.Act("The gods protect $N from $n.", victim, type: ActType.ToRoomNotVictim);

                // Stop the attacker from fighting the victim
                if (ch.Fighting == victim)
                {
                    StopFighting(ch);
                    //ch.fighting = null;
                    //if (ch.position == Positions.Fighting) ch.position = Positions.Standing;
                }

                // Stop the victim from fighting the attacker
                if (victim != null && victim.Fighting == ch)
                {
                    victim.Fighting = null;
                    if (victim.Position == Positions.Fighting) victim.Position = Positions.Standing;
                }

                return true; // It is not safe to engage in combat
            }

            return false; // It is safe to engage in combat
        }

        /// <summary>
        /// Inflicts damage on a character.
        /// </summary>
        /// <param name="ch">The attacking character.</param>
        /// <param name="victim">The character receiving damage.</param>
        /// <param name="damage">The amount of damage.</param>
        /// <param name="nounDamage">Description of the damage.</param>
        /// <param name="DamageType">Type of damage (default value is WeaponDamageTypes.Bash).</param>
        /// <param name="ownerName">Name of the owner, used for distributing experience.</param>
        /// <param name="weapon">The weapon used (optional).</param>
        /// <param name="show">Indicates if the damage message should be displayed (default is true).</param>
        /// <param name="allowflee">Indicates if the victim can flee after receiving damage (default is true).</param>
        /// <returns>True if the damage was successfully applied, false otherwise.</returns>
        public static bool Damage(Character ch, Character victim, int damage, string nounDamage,
            WeaponDamageTypes DamageType = WeaponDamageTypes.Bash, string ownerName = "", ItemData weapon = null,
            bool show = true, bool allowflee = true)
        {
            // Check if the victim is null, return false if true
            if (victim == null)
                return false;

            // Check if the damage exceeds 500, log a message if true
            if (damage > 500)
                game.log(ch.Name + " did more than 500 damage in one hit.");

            // Perform actions on the attacker (ch) if it exists
            if (ch != null)
            {
                // Check if the attacker and victim are in a safe zone, return false if true
                if (CheckIsSafe(ch, victim))
                    return false;

                // If the victim is not already in combat and the attacker is not the victim,
                // initiate combat and notify other characters in the area
                if (victim.Fighting == null && ch != victim)
                {
                    if (victim.Form != null && victim.Room != null && victim.Room.Area != null)
                    {
                        // Notify other characters in the area of the attack
                        foreach (var person in victim.Room.Area.People)
                        {
                            if (person != victim)
                                person.Act(victim.Form.Yell, victim, null, null, ActType.ToChar);
                        }
                    }
                    else
                        DoActCommunication.DoYell(victim, "Help! " + ch.Display(victim) + " is attacking me!");

                    SetFighting(victim, ch); // Set victim's fighting target to the attacker
                }

                // If the attacker is not already in combat and is not the victim, set the attacker's target to the victim
                if (ch.Fighting == null && ch != victim)
                    SetFighting(ch, victim);

                // Strip various hiding and camouflage effects from the attacker
                if (ch != victim)
                    ch.StripCamouflage();
                ch.StripHidden();
                ch.StripInvis();
                ch.StripSneak();

                // Apply additional effects if the attacker is affected by Burrow
                if (ch != victim && ch.IsAffected(AffectFlags.Burrow))
                {
                    ch.AffectedBy.REMOVEFLAG(AffectFlags.Burrow);
                    damage *= 3; // Triple the damage
                    ch.Act("Your first hit coming out of your burrow surprises $N", victim, type: ActType.ToChar);
                    ch.Act("$n's first hit coming out $s burrow surprises $N", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n's attack surprises you!", victim, type: ActType.ToVictim);
                }

                // Remove the PlayDead effect if the attacker is affected by it
                if (ch != victim && ch.IsAffected(AffectFlags.PlayDead))
                    ch.AffectedBy.REMOVEFLAG(AffectFlags.PlayDead);

                // Reduce damage if the victim is affected by Protection and the attacker and victim have opposing alignments
                if ((victim.IsAffected(AffectFlags.Protection) && ch.Alignment == Alignment.Evil && victim.Alignment == Alignment.Good) ||
                    (victim.IsAffected(AffectFlags.Protection) && ch.Alignment == Alignment.Good && victim.Alignment == Alignment.Evil))
                {
                    damage = (damage * 3 / 4); // Reduce damage by 25%
                }
            } // if ch != null

            // Reset the reset timer for the area if the attacker exists and is in a room within an area
            if (ch != null && ch.Room != null && ch.Room.Area != null)
                ch.Room.Area.Timer = 15;

            // Reduce damage if the victim is affected by Stone Skin
            if (victim.IsAffected(SkillSpell.SkillLookup("stone skin")))
                damage = (damage * 3 / 4); // Reduce damage by 25%

            // Reduce damage based on various shield effects on the victim
            if (victim.IsAffected(AffectFlags.Sanctuary))
                damage /= 2;
            if (victim.IsAffected(AffectFlags.Haven))
                damage -= damage * 3 / 8;
            if (victim.IsAffected(AffectFlags.Watershield))
                damage -= (int)(damage * .15);
            if (victim.IsAffected(AffectFlags.Airshield))
                damage -= (int)(damage * .15);
            if (victim.IsAffected(AffectFlags.Fireshield))
                damage -= (int)(damage * .15);
            if (victim.IsAffected(AffectFlags.Lightningshield))
                damage -= (int)(damage * .15);
            if (victim.IsAffected(AffectFlags.Frostshield))
                damage -= (int)(damage * .15);
            if (victim.IsAffected(AffectFlags.Earthshield))
                damage -= (int)(damage * .15);
            if (victim.IsAffected(AffectFlags.Shield))
                damage -= damage * 3 / 8;
            if (victim.IsAffected(AffectFlags.AnthemOfResistance))
                damage = damage - (damage / 4);
            if (victim.IsAffected(AffectFlags.Retract))
                damage = (int)Math.Ceiling(((float)damage) * .05f);

            // Increase damage if the attacker is affected by Bestial Fury
            if (ch != victim && ch.IsAffected(AffectFlags.BestialFury))
                damage += (int)(damage * .25);

            // Check if the victim is affected by Skin of the Displacer and has a chance to avoid the damage
            if (ch != victim && victim.IsAffected(AffectFlags.SkinOfTheDisplacer) && Utility.Random(1, 8) == 1)
            {
                victim.Act("Your skin shimmers as you avoid $N's {0}.", ch, args: nounDamage);
                victim.Act("$n's skin shimmers as $e avoids $N's {0}.", ch, type: ActType.ToRoomNotVictim, args: nounDamage);
                victim.Act("$n's skin shimmers as $e avoids your {0}.", ch, type: ActType.ToVictim, args: nounDamage);
                return false; // Damage is avoided, return false
            }

            bool immune = false;

            // Check the victim's immunity status against the damage type
            switch (victim.CheckImmune(DamageType))
            {
                case ImmuneStatus.Immune:
                    immune = true;
                    damage = 0; // Damage is completely negated
                    break;
                case ImmuneStatus.Resistant:
                    if (victim.IsNPC)
                        damage -= damage / 2; // Reduce damage by 50% for NPCs
                    else
                        damage -= damage / 2; // Reduce damage by 50% for players
                    break;
                case ImmuneStatus.Vulnerable:
                    damage += damage / 2; // Increase damage by 50%
                    break;
            }

            CheckWeaponImmune(victim, weapon, ref immune, ref damage);

            // Remove hiding and camouflage effects from the victim if damage is greater than 0
            if (damage > 0)
            {
                victim.StripHidden();
                victim.StripInvis();
                victim.StripSneak();
                victim.StripCamouflage();
            }

            // Display the damage message if show is true
            if (show)
                DamageMessage(ch, victim, damage, nounDamage, DamageType, immune);

            // Reduce the victim's hit points by the damage amount
            victim.HitPoints -= damage;

            // Update the attacker (ch) if ownerName is provided and the attacker is the victim or null
            if (!ownerName.ISEMPTY() && (ch == victim || ch == null))
            {
                ch = (from character in Character.Characters where character.Name == ownerName select character).FirstOrDefault();
            }

            // Check if the victim is dead or has reached 0 hit points
            CheckIsDead(ch, victim, damage);

            // If the victim is an NPC and in a fighting position, has a chance to flee
            if ((victim.Position == Positions.Fighting || victim.Position == Positions.Standing) &&
                victim.IsNPC &&
                allowflee &&
                ch != null &&
                ch != victim &&
                victim.Room == ch.Room &&
                damage > 0 &&
                victim.Wait < game.PULSE_VIOLENCE / 2)
            {
                // Check flee conditions and execute fleeing
                if (((victim.Flags.ISSET(ActFlags.Wimpy) && Utility.Random(0, 4) == 0 && victim.HitPoints < victim.MaxHitPoints / 5)) ||
                    (victim.Master != null && victim.Master.Room != victim.Room) && allowflee)
                {
                    Combat.DoFlee(victim, "");
                }
            }

            // If the victim is a player, check if they have a wimpy value and can flee
            if (victim is Player && ch != victim)
            {
                var player = (Player)victim;

                if (player.Wimpy > 0 &&
                    (victim.Position == Positions.Fighting || victim.Position == Positions.Standing) &&
                    damage > 0 &&
                    ch != null &&
                    victim.Room == ch.Room &&
                    allowflee &&
                    victim.HitPoints < player.Wimpy &&
                    victim.Wait < game.PULSE_VIOLENCE / 2)
                {
                    Combat.DoFlee(victim, "");
                }
            }

            return true; // Damage applied successfully
        }

        /// <summary>
        /// Checks if the victim has any immunity or vulnerability based on the weapon material used.
        /// </summary>
        /// <param name="victim">The character receiving damage.</param>
        /// <param name="weapon">The weapon used (optional).</param>
        /// <param name="immune">A reference to a boolean indicating immunity.</param>
        /// <param name="damage">A reference to the damage amount.</param>
        private static void CheckWeaponImmune(Character victim, ItemData weapon, ref bool immune, ref int damage)
        {
            // Define an array of weapon materials to check
            var materials = new WeaponDamageTypes[] { WeaponDamageTypes.Iron, WeaponDamageTypes.Silver, WeaponDamageTypes.Wood, WeaponDamageTypes.Mithril };

            // Iterate over each material and check if the weapon material matches
            foreach (var material in materials)
            {
                if (weapon != null && weapon.Material.StringCmp(material.ToString()))
                {
                    // Check the victim's immunity or vulnerability against the material
                    switch (victim.CheckImmune(material))
                    {
                        case ImmuneStatus.Immune:
                            immune = true;
                            damage = 0; // Damage is completely negated
                            break;
                        case ImmuneStatus.Resistant:
                            if (victim.IsNPC)
                                damage -= damage / 2; // Reduce damage by 50% for NPCs
                            else
                                damage -= damage / 2; // Reduce damage by 50% for players
                            break;
                        case ImmuneStatus.Vulnerable:
                            damage += damage / 2; // Increase damage by 50%
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Inflicts damage on a character using a specific skill.
        /// </summary>
        /// <param name="ch">The attacking character.</param>
        /// <param name="victim">The character receiving damage.</param>
        /// <param name="damage">The amount of damage.</param>
        /// <param name="skill">The skill used for the attack.</param>
        /// <param name="DamageType">Type of damage (default value is WeaponDamageTypes.Bash).</param>
        /// <param name="ownerName">Name of the owner.</param>
        /// <param name="weapon">The weapon used (optional).</param>
        /// <param name="show">Indicates if the damage message should be displayed (default is true).</param>
        /// <returns>True if the damage was successfully applied, false otherwise.</returns>
        public static bool Damage(Character ch, Character victim, int damage, SkillSpell skill,
            WeaponDamageTypes DamageType = WeaponDamageTypes.Bash, string ownerName = "", ItemData weapon = null,
            bool show = true)
        {
            string nounDamage = "attack";

            // If a skill is provided and it has a specific noun for damage, use that noun
            if (skill != null && !string.IsNullOrEmpty(skill.NounDamage))
                nounDamage = skill.NounDamage;

            // Call the original Damage method with the updated nounDamage parameter
            return Damage(ch, victim, damage, nounDamage, DamageType, ownerName, weapon, show);
        }

        /// <summary>
        /// Updates the position of a character based on their hit points.
        /// </summary>
        /// <param name="victim">The character whose position is being updated.</param>
        public static void UpdatePosition(Character victim)
        {
            // Check if the victim's hit points are greater than 0
            if (victim.HitPoints > 0)
            {
                // If the victim's position is at or below Stunned, set it to Standing
                if (victim.Position <= Positions.Stunned)
                    victim.Position = Positions.Standing;
                return; // Exit the method
            }

            // Check if the victim is an NPC and has hit points less than 1
            if (victim.IsNPC && victim.HitPoints < 1)
            {
                victim.Position = Positions.Dead; // Set the position to Dead
                return; // Exit the method
            }

            // Determine the position based on the victim's hit points
            if (victim.HitPoints <= -11)
                victim.Position = Positions.Dead;
            else if (victim.HitPoints <= -6)
                victim.Position = Positions.Mortal;
            else if (victim.HitPoints <= -3)
                victim.Position = Positions.Incapacitated;
            else
                victim.Position = Positions.Stunned;
        }

        /// <summary>
        /// Checks if a character is dead and performs the necessary actions.
        /// </summary>
        /// <param name="ch">The character responsible for the death (optional).</param>
        /// <param name="victim">The character who died.</param>
        /// <param name="damage">The amount of damage that caused the death.</param>
        public static void CheckIsDead(Character ch, Character victim, int damage)
        {
            // Update the position of the victim based on their hit points
            UpdatePosition(victim);

            // Check the victim's position and perform the appropriate actions
            switch (victim.Position)
            {
                case Positions.Mortal:
                    victim.Act("$n is mortally wounded and will die soon, if not aided.", type: ActType.ToRoom);
                    victim.send("You are mortally wounded and will die soon, if not aided.\n\r");
                    break;

                case Positions.Incapacitated:
                    victim.Act("$n is incapacitated and will slowly die, if not aided.", type: ActType.ToRoom);
                    victim.send("You are incapacitated and will slowly die, if not aided.\n\r");
                    break;

                case Positions.Stunned:
                    victim.Act("$n is stunned, but will probably recover.", type: ActType.ToRoom);
                    victim.send("You are stunned, but will probably recover.\n\r");
                    break;

                case Positions.Dead:
                    victim.Act("$n is DEAD!!", null, null, null, ActType.ToRoom);
                    victim.send("You have been KILLED!!\n\r\n\r");
                    break;

                default:
                    if (damage > victim.MaxHitPoints / 4)
                        victim.send("That really did HURT!\n\r");
                    if (victim.HitPoints < victim.MaxHitPoints / 4)
                        victim.send("You sure are BLEEDING!\n\r");
                    break;
            }

            // Perform actions when the victim is dead
            if (victim.Position == Positions.Dead)
            {
                // Stop the fighting and set positions
                if (ch != null)
                {
                    ch.Fighting = null;
                    ch.Position = Positions.Standing;
                }
                victim.Fighting = null;
                //victim.Position = Positions.Dead;

                StopFighting(victim, true);

                if (victim.IsNPC)
                    victim.StopFollowing();

                // Remove affects from the victim
                foreach (var aff in victim.AffectsList.ToArray())
                    victim.AffectFromChar(aff, true);

                // Gain experience for killing an NPC victim
                if (ch != null && victim != null && victim.IsNPC)
                    ch.GroupGainExperience(victim);

                // Perform death cry
                victim.DeathCry();

                // Execute death-related programs for NPCs
                NPCData npcData;
                if (victim.IsNPC && (npcData = (NPCData)victim) != null)
                {
                    foreach (var prog in npcData.Programs)
                    {
                        if (prog.Types.ISSET(Programs.ProgramTypes.SenderDeath))
                        {
                            if (victim.Room != null)
                            {
                                foreach (var other in victim.Room.Characters.OfType<Player>())
                                {
                                    // ch == issamegroup if in same room
                                    if(other.IsSameGroup(ch))
                                    _ = prog.Execute(other, npcData, npcData, null, null, Programs.ProgramTypes.SenderDeath, "");
                                }
                            }
                        }
                    }

                    foreach (var prog in npcData.LuaPrograms)
                        if(prog.ProgramTypes.ISSET(Programs.ProgramTypes.SenderDeath))
                        {
                            if (victim.Room != null)
                            {
                                foreach (var other in victim.Room.Characters.OfType<Player>())
                                {
                                    // ch == issamegroup if in same room
                                    if (other.IsSameGroup(ch))
                                        _ = prog.Execute(other, npcData, null, null, null, null, Programs.ProgramTypes.SenderDeath, "");
                                }
                            }
                        }
                }

                // Execute death-related programs for other characters in the room
                if (victim.Room != null && !victim.IsNPC)
                {
                    foreach (var other in victim.Room.Characters.OfType<NPCData>().ToArray())
                    {
                        Programs.ExecutePrograms(Programs.ProgramTypes.PlayerDeath, other, victim, null, "");
                    }

                    // Execute death-related programs for items in the character's inventory
                    if (ch != null)
                    {
                        foreach (var item in ch.Equipment.Values.Concat(ch.Inventory).ToArray())
                        {
                            if (item == null) continue;
                            Programs.ExecutePrograms(Programs.ProgramTypes.PlayerDeath, ch, item, "");
                        }
                    }

                    // Execute death-related programs for items in the victim's inventory
                    foreach (var item in victim.Equipment.Values.Concat(victim.Inventory).ToArray())
                    {
                        Programs.ExecutePrograms(Programs.ProgramTypes.PlayerDeath, victim, item, "");
                    }
                }

                ItemTemplateData corpseTemplate;
                ItemData newCorpse = null;

                // Create a corpse item and transfer equipment and inventory to it
                if (ItemTemplateData.Templates.TryGetValue(6, out corpseTemplate) && victim.Room != null)
                {
                    newCorpse = new ItemData(corpseTemplate, victim.Room);
                    newCorpse.Name = string.Format(newCorpse.Name, victim.Name);
                    newCorpse.ShortDescription = string.Format(newCorpse.ShortDescription, victim.GetShortDescription(null));
                    newCorpse.LongDescription = string.Format(newCorpse.LongDescription, victim.GetShortDescription(null));
                    newCorpse.Description = string.Format(newCorpse.Description, victim.GetShortDescription(null));
                    newCorpse.ItemType.Clear();
                    newCorpse.ItemType.Add(ItemTypes.Container);
                    newCorpse.Size = victim.Size;

                    if (victim.IsNPC)
                    {
                        newCorpse.ItemType.Add(ItemTypes.NPCCorpse);
                        newCorpse.timer = 3;
                    }
                    else
                    {
                        newCorpse.ItemType.Add(ItemTypes.Corpse);
                        if (newCorpse.wearFlags.Contains(WearFlags.Take))
                            newCorpse.wearFlags.Remove(WearFlags.Take);
                        newCorpse.timer = 10;
                    }

                    if (victim.Form != null)
                        ShapeshiftForm.DoRevert(victim, "");

                    // Transfer equipment to the corpse
                    foreach (var item in new Dictionary<WearSlotIDs, ItemData>(victim.Equipment))
                    {
                        if (item.Value != null)
                        {
                            victim.RemoveEquipment(item.Key, false, true);
                            if (victim.Inventory.Contains(item.Value))
                                victim.Inventory.Remove(item.Value);
                            newCorpse.Contains.Add(item.Value);
                            item.Value.CarriedBy = null;
                            item.Value.Container = newCorpse;
                            if (item.Value.extraFlags.ISSET(ExtraFlags.VisDeath))
                                item.Value.extraFlags.REMOVEFLAG(ExtraFlags.VisDeath);
                            if (item.Value.extraFlags.ISSET(ExtraFlags.RotDeath))
                            {
                                item.Value.timer = 15;
                                item.Value.extraFlags.REMOVEFLAG(ExtraFlags.RotDeath);
                            }
                        }
                    }

                    // Transfer inventory to the corpse
                    foreach (var item in new List<ItemData>(victim.Inventory))
                    {
                        victim.Inventory.Remove(item);
                        newCorpse.Contains.Add(item);
                        item.Container = newCorpse;
                        item.CarriedBy = null;
                        if (item.extraFlags.ISSET(ExtraFlags.VisDeath))
                            item.extraFlags.REMOVEFLAG(ExtraFlags.VisDeath);
                        if (item.extraFlags.ISSET(ExtraFlags.RotDeath))
                        {
                            item.timer = 15;
                            item.extraFlags.REMOVEFLAG(ExtraFlags.RotDeath);
                        }
                    }

                    // Transfer money to the corpse
                    var silver = victim.Silver;
                    var gold = victim.Gold;
                    if (gold > 0 || silver > 0)
                    {
                        var money = Character.CreateMoneyItem(silver, gold);
                        newCorpse.Contains.Add(money);
                    }

                    newCorpse.Alignment = victim.Alignment;
                    newCorpse.Owner = victim.Name;
                    victim.Gold = 0;
                    victim.Silver = 0;
                }

                // Remove the character from the room
                victim.RemoveCharacterFromRoom();

                // Automatic looting and gold collection by the killer
                if (victim.IsNPC && ch != null && (ch.Flags.ISSET(ActFlags.AutoLoot) || ch.Flags.ISSET(ActFlags.AutoGold)) && newCorpse != null)
                {
                    foreach (var item in newCorpse.Contains.ToArray())
                    {
                        if ((item.ItemType.Contains(ItemTypes.Money) && ch.Flags.ISSET(ActFlags.AutoGold)) ||
                            (!item.ItemType.Contains(ItemTypes.Money) && ch.Flags.ISSET(ActFlags.AutoLoot)))
                        {
                            newCorpse.Contains.Remove(item);

                            ch.Act("You get $p from $P.", null, item, newCorpse, ActType.ToChar);
                            ch.Act("$n gets $p from $P.", null, item, newCorpse, ActType.ToRoom);

                            ch.AddInventoryItem(item);
                        }
                    }
                }

                // Automatic sacrifice of the corpse by the killer
                if (ch != null && victim.IsNPC && ch.Flags.ISSET(ActFlags.AutoSac))
                    Character.DoSacrifice(ch, "corpse");

                // Clear killer wait time on kill
                if (ch != null)
                    ch.Wait = 0;

                if (victim is Player)
                {
                    // Handle death-related actions for player characters
                    if (victim.GetRecallRoom() != null)
                        victim.AddCharacterToRoom(victim.GetRecallRoom());
                    victim.Act("$n appears in the room.\n\r", type: ActType.ToRoom);
                    victim.HitPoints = victim.MaxHitPoints / 2;
                    victim.ManaPoints = victim.MaxManaPoints / 2;
                    victim.MovementPoints = victim.MaxMovementPoints / 2;
                    victim.Position = Positions.Resting;
                    victim.ModifiedStats = new PhysicalStats(0, 0, 0, 0, 0, 0);
                    victim.AffectsList.Clear();
                    victim.AffectedBy.Clear();
                    if (victim.Race != null)
                        victim.AffectedBy.AddRange(victim.Race.affects);
                    victim.Hunger = 48;
                    victim.Thirst = 48;
                    victim.Dehydrated = 0;
                    victim.Starving = 0;
                    victim.HitRoll = 0;
                    victim.DamageRoll = 0;

                    // Clear the LastFighting reference from other NPCs
                    foreach (var npc in Character.Characters)
                    {
                        if (npc.LastFighting == victim)
                            npc.LastFighting = null;
                    }

                    // Apply a ghost affect to the player for a short duration
                    var ghostAffect = new AffectData()
                    {
                        duration = 15,
                        displayName = "ghost",
                        endMessage = "\\WYou regain your corporeal form.\\x",
                        endMessageToRoom = "$n regains $s corporeal form."
                    };
                    ghostAffect.flags.SETBIT(AffectFlags.Ghost);
                    victim.AffectToChar(ghostAffect);
                    victim.send("\\RYou become a ghost for a short while.\\x\n\r");
                }
                else
                {
                    // Handle death-related actions for NPCs
                    if (victim is NPCData)
                        ((NPCData)victim).Dispose();
                    victim.Dispose();
                }
            }
            else if (victim != ch && victim.Fighting != null)
            {
                // Set the victim's position to Fighting if they're still in combat
                victim.Position = Positions.Fighting;
            }
        }

        public static void DamageMessage(Character ch, Character victim, int damageAmount, string nounDamage, WeaponDamageTypes DamageType = WeaponDamageTypes.Bash, bool immune = false)
        {
            // Variables for verb singular, verb plural, and punctuation
            string vs;
            string vp;
            string punct;

            // Determine the appropriate verb phrases based on the damage amount
            if (damageAmount == 0)
            {
                vs = "miss";
                vp = "misses";
            }
            else if (damageAmount <= 1)
            {
                vs = "barely scratch";
                vp = "barely scratches";
            }
            else if (damageAmount <= 2)
            {
                vs = "scratch";
                vp = "scratches";
            }
            else if (damageAmount <= 4)
            {
                vs = "graze";
                vp = "grazes";
            }
            else if (damageAmount <= 7)
            {
                vs = "hit";
                vp = "hits";
            }
            else if (damageAmount <= 11)
            {
                vs = "injure";
                vp = "injures";
            }
            else if (damageAmount <= 15)
            {
                vs = "wound";
                vp = "wounds";
            }
            else if (damageAmount <= 20)
            {
                vs = "maul";
                vp = "mauls";
            }
            else if (damageAmount <= 25)
            {
                vs = "decimate";
                vp = "decimates";
            }
            else if (damageAmount <= 30)
            {
                vs = "devastate";
                vp = "devastates";
            }
            else if (damageAmount <= 37)
            {
                vs = "maim";
                vp = "maims";
            }
            else if (damageAmount <= 45)
            {
                vs = "MUTILATE";
                vp = "MUTILATES";
            }
            else if (damageAmount <= 55)
            {
                vs = "EVISCERATE";
                vp = "EVISCERATES";
            }
            else if (damageAmount <= 65)
            {
                vs = "DISMEMBER";
                vp = "DISMEMBERS";
            }
            else if (damageAmount <= 85)
            {
                vs = "MASSACRE";
                vp = "MASSACRES";
            }
            else if (damageAmount <= 100)
            {
                vs = "MANGLE";
                vp = "MANGLES";
            }
            else if (damageAmount <= 135)
            {
                vs = "*** DEMOLISH ***";
                vp = "*** DEMOLISHES ***";
            }
            else if (damageAmount <= 160)
            {
                vs = "*** DEVASTATE ***";
                vp = "*** DEVASTATES ***";
            }
            else if (damageAmount <= 250)
            {
                vs = "=== OBLITERATE ===";
                vp = "=== OBLITERATES ===";
            }
            else if (damageAmount <= 330)
            {
                vs = ">>> ANNIHILATE <<<";
                vp = ">>> ANNIHILATES <<<";
            }
            else if (damageAmount <= 380)
            {
                vs = "<<< ERADICATE >>>";
                vp = "<<< ERADICATES >>>";
            }
            else
            {
                vs = "do UNSPEAKABLE things to";
                vp = "does UNSPEAKABLE things to";
            }

            // Determine the appropriate punctuation based on the damage amount
            punct = (damageAmount <= 33) ? "." : "!";

            // If the damage is immune (no effect)
            if (immune)
            {
                // Check various conditions and display corresponding immunity messages
                if (ch == victim && !nounDamage.ISEMPTY())
                {
                    ch.Act("$n is unaffected by $s own %s.", null, null, null, ActType.ToRoom, nounDamage);
                    ch.Act("Luckily, you are immune to that.");
                }
                else if (!nounDamage.ISEMPTY())
                {
                    ch.Act("$N is unaffected by $n's {0}!", victim, null, null, ActType.ToRoomNotVictim, nounDamage);
                    ch.Act("$N is unaffected by your {0}!", victim, null, null, ActType.ToChar, nounDamage);
                    ch.Act("$n's {0} is powerless against you.", victim, null, null, ActType.ToVictim, nounDamage);
                }
                else if (ch == victim)
                {
                    ch.Act("$n is unaffected by $mself.", null, null, null, ActType.ToRoom);
                    ch.Act("Luckily, you are immune to that.");
                }
                else
                {
                    ch.Act("$N is unaffected by $n!", victim, null, null, ActType.ToRoomNotVictim);
                    ch.Act("$N is unaffected by you!", victim, null, null, ActType.ToChar);
                    ch.Act("$n is powerless against you.", victim, null, null, ActType.ToVictim);
                }
            }

            // If the attacker and victim are different characters and there is a specific damage noun
            if (victim != ch && ch != null && !nounDamage.ISEMPTY())
            {
                // Display damage messages with specific noun to the attacker, victim, and the room
                ch.Act("Your {0} \\R{1}\\x $N{2}", victim, null, null, ActType.ToChar, nounDamage, vp, punct);
                victim.Act("$N's {0} \\R{1}\\x you{2}", ch, null, null, ActType.ToChar, nounDamage, vp, punct);
                ch.Act("$n's {0} \\R{1}\\x $N{2}", victim, null, null, ActType.ToRoomNotVictim, nounDamage, vp, punct);
            }
            // If the attacker is null and there is a specific damage noun
            else if (ch == null && !nounDamage.ISEMPTY())
            {
                // Display damage messages with specific noun to the victim and the room
                victim.Act("Your {0} \\R{1}\\x you{2}", null, null, null, ActType.ToChar, nounDamage, vp, punct);
                victim.Act("$n's {0} \\R{1}\\x them{2}", null, null, null, ActType.ToRoom, nounDamage, vp, punct);
            }
            // If there is a specific damage noun but no attacker
            else if (!nounDamage.ISEMPTY())
            {
                // Display damage messages with specific noun to the attacker (if available) and the room
                ch.Act("Your {0} \\R{1}\\x you{2}", null, null, null, ActType.ToChar, nounDamage, vp, punct);
                ch.Act("$n's {0} \\R{1}\\x them{2}", victim, null, null, ActType.ToRoom, nounDamage, vp, punct);
            }
            // If the attacker and victim are different characters and there is no specific damage noun
            else if (victim != ch && ch != null)
            {
                // Display generic damage messages to the attacker, victim, and the room
                ch.Act("You \\R{0}\\x $N{1}", victim, null, null, ActType.ToChar, vs, punct);
                victim.Act("$N \\R{0}\\x you{1}", ch, null, null, ActType.ToChar, vp, punct);
                ch.Act("$n \\R{0}\\x $N{1}", victim, null, null, ActType.ToRoomNotVictim, vp, punct);
            }
            // If the attacker is null and there is no specific damage noun
            else if (ch == null && nounDamage.ISEMPTY())
            {
                // Display generic damage messages to the victim and the room
                victim.Act("You \\R{0}\\x yourself{1}", null, null, null, ActType.ToChar, vs, punct);
                victim.Act("$n \\R{0}\\x $mself{1}", null, null, null, ActType.ToRoom, vp, punct);
            }
            // If there is no specific damage noun
            else if (nounDamage.ISEMPTY())
            {
                // Display generic damage messages to the attacker and the room
                ch.Act("You \\R{0}\\x yourself{1}", null, null, null, ActType.ToChar, vs, punct);
                ch.Act("$n \\R{0}\\x $mself{1}", victim, null, null, ActType.ToRoom, vp, punct);
            }
        } // end damageMessage

        public static void DoBash(Character ch, string arguments)
        {
            Character victim;
            int dam = 0;
            int skillPercent = 0;
            int count = 0;
            var skill = SkillSpell.SkillLookup("bash");

            //if (!ch.IsNPC && ch.Guild != null && !skill.skillLevel.TryGetValue(ch.Guild.name, out lvl))
            //    lvl = -1;

            if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You fall flat on your face.\n\r");
                ch.WaitState(game.PULSE_VIOLENCE * 1);
                return;
            }
            ItemData shield;
            if (ch.Equipment.TryGetValue(WearSlotIDs.Shield, out shield))
                if (shield != null)
                {
                    ch.send("You must shield bash someone while holding a shield.\n\r");
                    return;
                }
            if ((victim = (ch.GetCharacterFromRoomByName(arguments, ref count)) ?? ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            if (CheckIsSafe(ch, victim)) return;

            if (victim == ch)
                ch.send("You can't bash yourself!.\n\r");
            else if (CheckAcrobatics(ch, victim)) return;
            else if (victim.FindAffect(SkillSpell.SkillLookup("protective shield")) != null)
            {
                ch.WaitState(game.PULSE_VIOLENCE);
                ch.Act("You try to bash $N but fall straight through $M.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n tries to bash $N but falls straight through $M.\n\r", victim, type: ActType.ToRoomNotVictim);
            }
            else if (skillPercent > Utility.NumberPercent())
            {
                dam += Utility.Random(10, (ch.Level) / 2);

                ch.Position = Positions.Fighting;
                ch.Fighting = victim;
                ch.Act("You bash $N and they fall to the ground.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n bashes $N to the ground.\n\r", victim, type: ActType.ToRoomNotVictim);
                victim.send("{0} bashes you to the ground.\n\r", ch.Display(victim));

                Combat.Damage(ch, victim, dam, skill);
                ch.WaitState(game.PULSE_VIOLENCE * 2);
                victim.WaitState(game.PULSE_VIOLENCE * 1);
                ch.CheckImprove(skill, true, 1);
                CheckCheapShot(victim);
                CheckGroundControl(victim);
            }
            else
            {
                ch.WaitState(game.PULSE_VIOLENCE * 1);
                ch.send("You fall flat on your face.\n\r");
                ch.Position = Positions.Sitting;
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoTrip(Character ch, string arguments)
        {
            Character victim = null;
            int dam = 0;
            int skillPercent = 0;
            int count = 0;
            var skill = SkillSpell.SkillLookup("trip");


            //if (!ch.IsNPC && ch.Guild != null && !skill.skillLevel.TryGetValue(ch.Guild.name, out lvl))
            //    lvl = -1;

            //if (!ch.Learned.TryGetValue(skill, out lvl) || lvl <= 1)
            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You stumble and your feet get crossed.\n\r");
            }

            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.Act("You aren't fighting anyone.\n\r");
            }
            else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments, ref count)) == null)
            {
                ch.Act("They aren't here!\n\r");
            }
            else if (CheckIsSafe(ch, victim))
            {

            }
            else if (victim == ch)
            {
                ch.send("You can't trip yourself!.\n\r");
            }

            else if (CheckAcrobatics(ch, victim)) return;

            else if (victim.IsAffected(AffectFlags.Flying))
            {
                ch.send("Their feet aren't on the ground.\n\r");
            }
            else if (skillPercent > Utility.NumberPercent())
            {
                dam += Utility.dice(2, 3, 8);

                //Utility.Random(6, Math.Max(8,(ch.Level) / 5));

                ch.Position = Positions.Fighting;
                ch.Fighting = victim;
                ch.Act("You trip $N and $E falls to the ground.\n\r", victim);
                ch.Act("$n trips $N and $E falls to the ground.\n\r", victim, type: ActType.ToRoomNotVictim);
                victim.Act("$N trips you to the ground.\n\r", ch);

                Combat.Damage(ch, victim, dam, skill);
                ch.WaitState(skill.waitTime);
                victim.WaitState(game.PULSE_VIOLENCE * 1);
                ch.CheckImprove(skill, true, 1);
                CheckCheapShot(victim);
                CheckGroundControl(victim);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("You fail to trip them.\n\r");
                Combat.Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoBerserk(Character ch, string arguments)
        {
            AffectData affect;
            SkillSpell skill = SkillSpell.SkillLookup("berserk");
            int skillPercent;

            //if (!ch.IsNPC && ch.Guild != null && !sn.skillLevel.TryGetValue(ch.Guild.name, out lvl))
            //    lvl = -1;

            //if (!ch.Learned.TryGetValue(sn, out lvl) || lvl <= 1)
            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((affect = ch.FindAffect(skill)) != null)
            {
                ch.send("You are already enraged!\n\r");
                return;
            }

            else if (ch.ManaPoints < 20)
            {
                ch.send("You don't have enough mana to enrage.\n\r");
                return;
            }

            else
            {
                ch.WaitState(skill.waitTime);
                if (skillPercent > Utility.NumberPercent())
                {
                    affect = new AffectData();
                    affect.skillSpell = skill;
                    affect.level = ch.Level;
                    affect.location = ApplyTypes.Damroll;
                    affect.duration = 2;
                    affect.modifier = +5;
                    affect.displayName = "berserk";
                    affect.affectType = AffectTypes.Skill;
                    ch.AffectToChar(affect);

                    affect = new AffectData();
                    affect.skillSpell = skill;
                    affect.level = ch.Level;
                    affect.location = ApplyTypes.Hitroll;
                    affect.duration = 2;
                    affect.modifier = +5;
                    affect.displayName = "berserk";
                    affect.endMessage = "Your rage subsides.\n\r";
                    affect.endMessageToRoom = "$n's rage subsides.\n\r";
                    affect.affectType = AffectTypes.Skill;
                    ch.AffectToChar(affect);

                    int heal = (int)(ch.Level * 2.5) + 5;
                    ch.HitPoints = Math.Min(ch.HitPoints + heal, ch.MaxHitPoints);
                    ch.ManaPoints -= 20;


                    ch.send("You are filled with rage.\n\r");
                    ch.Act("$n becomes filled with rage.\n\r", type: ActType.ToRoom);
                    ch.CheckImprove(skill, true, 1);
                }
                else
                {
                    ch.send("You only manage to turn your face red.\n\r");
                    ch.Act("$n manages to turn their face red.\n\r", type: ActType.ToRoom);
                    ch.CheckImprove(skill, false, 1);
                }
            }
        }

        /// <summary>
        /// Performs multiple hits on a victim during combat.
        /// </summary>
        /// <param name="ch">The character performing the hits.</param>
        /// <param name="victim">The character being hit.</param>
        public static void multiHit(Character ch, Character victim)
        {
            ItemData weapon;
            ItemData tempitem;
            int skill;

            // Check if the attacker is sleeping or in a safe room
            if (ch.Position == Positions.Sleeping)
                return;

            if (CheckIsSafe(ch, victim))
                return;

            // Check if the attacker is attacking themselves
            if (ch == victim)
            {
                StopFighting(ch);
                return;
            }

            // Strip hidden, invisibility, sneak, and camouflage status
            ch.StripHidden();
            ch.StripInvis();
            ch.StripSneak();
            ch.StripCamouflage();

            // Don't perform skills on the first round of combat
            if (ch.Fighting != null && ch.Form == null)
            {
                // Check if the attacker is an NPC and has learned skills
                if (ch.IsNPC && 100 > Utility.NumberPercent() && ch.Wait <= 0 && ch.Learned.Count > 0)
                {
                    bool used = false;
                    int attempts = 0;

                    while (!used && attempts++ < 10)
                    {
                        // Select a random skill from the learned skills
                        var autoskill = ch.Learned.SelectRandom();

                        // Exclude the "recall" skill
                        if (autoskill.Key.name.StringCmp("recall"))
                            continue;

                        // Check if the NPC is in a guild and has offensive casting abilities
                        if (ch.Guild != null && ch.Guild.CastType != Magic.CastType.None)
                        {
                            autoskill = (from sk in ch.Learned where sk.Key.targetType == TargetTypes.targetCharOffensive && sk.Key.minimumPosition == Positions.Fighting select sk).SelectRandom();
                            if (autoskill.Key == null)
                                autoskill = ch.Learned.SelectRandom();
                        }

                        // Check if the skill's percentage is higher than 1
                        if (autoskill.Value.Percentage > 1)
                        {
                            // Check if the skill is a spell and cast it
                            if (autoskill.Key.spellFun != null && autoskill.Key.targetType == TargetTypes.targetCharOffensive && autoskill.Key.minimumPosition == Positions.Fighting)
                            {
                                used = true;
                                Magic.CastCommuneOrSing(ch, "'" + autoskill.Key.name + "'", ch.Guild.CastType);
                            }
                            // Check if the skill is a command and execute it
                            else
                            {
                                foreach (var command in Command.Commands)
                                {
                                    if (command.Name == autoskill.Key.name.Replace(" ", ""))
                                    {
                                        used = true;
                                        command.Action(ch, "");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Set the attacker's fighting target if not already set
            if (ch.Fighting == null)
                SetFighting(ch, victim);

            Programs.ExecutePrograms(Programs.ProgramTypes.RoundCombat, ch, victim, null, "");
            
            var haste = ch.IsAffected(AffectFlags.Haste);
            var slow = ch.IsAffected(AffectFlags.Slow);

            var attackskills = new string[] { "", "second attack", "third attack", "fourth attack", "fifth attack" };

            // Loop through the attack skills and perform the hits
            for (int i = 0; i < attackskills.Length; i++)
            {
                if (ch.Fighting == victim)
                {
                    // Check if the attacker has the skill or the random chance is successful
                    if (attackskills[i].ISEMPTY() || ((skill = ch.GetSkillPercentage(SkillSpell.SkillLookup(attackskills[i]))) > 1 &&
                        (skill + (haste ? 10 : slow ? -20 : 0) + 10) > Utility.NumberPercent()))
                    {
                        if (!attackskills[i].ISEMPTY())
                            ch.CheckImprove(attackskills[i], true, 1);

                        // Check if the attacker has a form or wielded weapon
                        if (ch.Form != null)
                            weapon = null;
                        else
                            ch.Equipment.TryGetValue(WearSlotIDs.Wield, out weapon);

                        // Check if the attacker is an NPC with area attack flag
                        if (ch.IsNPC && ch.Flags.ISSET(ActFlags.AreaAttack) && ch.Room != null)
                        {
                            oneHit(ch, victim, weapon); // Perform hit on the main victim

                            // Perform hit on other characters in the room who are fighting the attacker
                            if (ch.Room != null)
                            {
                                foreach (var fighting in ch.Room.Characters.ToArray())
                                {
                                    if (fighting.Fighting == ch && fighting != victim)
                                    {
                                        oneHit(ch, fighting, weapon);
                                    }
                                }
                            }
                        }
                        // Perform hit on the main victim
                        else if (ch.Room != null)
                        {
                            oneHit(ch, victim, weapon);
                        }

                        // Check if the attacker is still fighting the victim and has the dual wield skill
                        if (ch.Fighting == victim && ch.Room != null && (weapon == null || !weapon.extraFlags.ISSET(ExtraFlags.TwoHands)) &&
                            (skill = ch.GetSkillPercentage(SkillSpell.SkillLookup("dual wield"))) > 1 &&
                            (skill + (haste ? 10 : slow ? -20 : 0)) > Utility.NumberPercent() &&
                            ((!ch.Equipment.TryGetValue(WearSlotIDs.Held, out tempitem) && !ch.Equipment.TryGetValue(WearSlotIDs.Shield, out tempitem)) || ch.Form != null))
                        {
                            // Check if the attacker has a dual wield item
                            if (ch.Form == null)
                                ch.Equipment.TryGetValue(WearSlotIDs.DualWield, out weapon);

                            // Perform hit with dual wield weapon
                            oneHit(ch, victim, weapon, true);
                        }
                    }
                    else
                    {
                        if (!attackskills[i].ISEMPTY())
                            ch.CheckImprove(attackskills[i], false, 1);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs a single hit from one character to another.
        /// </summary>
        /// <param name="ch">The character initiating the hit.</param>
        /// <param name="victim">The character being hit.</param>
        /// <param name="weapon">The weapon used for the hit.</param>
        /// <param name="offhand">Flag indicating if the hit is from the offhand weapon (default: false).</param>
        /// <param name="skill">The skill or spell used for the hit (default: null).</param>
        public static void oneHit(Character ch, Character victim, ItemData weapon, bool offhand = false, SkillSpell skill = null)
        {
            // Check if it is safe to perform the hit
            if (CheckIsSafe(ch, victim))
                return;

            // Check if the victim is valid or in the same room
            if (victim == null || victim.Room != ch.Room)
            {
                ch.Fighting = null;
                ch.Position = Positions.Standing;
                return;
            }

            // Set victim as the target for the character's attack
            if (victim.Fighting == null)
            {
                victim.Fighting = ch;

                // Display a message to the people in the same area as the victim
                if (victim.Form != null && victim.Room != null && victim.Room.Area != null)
                {
                    foreach (var person in victim.Room.Area.People)
                    {
                        if (person != victim)
                            person.Act(victim.Form.Yell, victim, null, null, ActType.ToChar);
                    }
                }
                else
                    DoActCommunication.DoYell(victim, "Help! " + ch.Display(victim) + " is attacking me!");
            }

            // Remove any stealth-related effects from the character
            ch.StripHidden();
            ch.StripInvis();
            ch.StripSneak();
            ch.StripCamouflage();

            int skillLevel = 0;
            int weaponSkillLevel = 0;
            float damage;

            // Check the skill level for the weapon or hand-to-hand combat
            SkillSpell weaponSkill;
            if (weapon != null)
            {
                weaponSkill = SkillSpell.SkillLookup(weapon.WeaponType.ToString());
            }
            else
            {
                weaponSkill = SkillSpell.SkillLookup("hand to hand");
            }

            if ((weaponSkill != null && (weaponSkillLevel = skillLevel = ch.GetSkillPercentage(weaponSkill)) > 0) ||
                (ch.IsNPC && (weaponSkillLevel = skillLevel = 80) == 80))
            {
                // Weapon or hand-to-hand skill check to determine hit or miss
                // Note: The following code is commented out, uncomment if needed
                /*
                if (weaponSkillLevel > utility.number_percent())
                {
                    if (offhand)
                        ch.CheckImprove(SkillSpell.SkillLookup("dual wield"), true, 1);
                    ch.CheckImprove(weaponSkill, true, 1);

                    //damage = ((float)damage * 1.2f);
                }
                else
                {
                    if (offhand)
                        ch.CheckImprove(SkillSpell.SkillLookup("dual wield"), false, 1);
                    ch.CheckImprove(weaponSkill, false, 1);
                    //damage = 0;
                }
                */
            }
            else
                weaponSkillLevel = 50;

            var claws = false;
            AffectData damagemodifier = null;
            string damagenoun = ch.WeaponDamageMessage != null ? ch.WeaponDamageMessage.Message :
                (ch.Race.parts.Contains(PartFlags.Claws) || claws ?
                    "claw" : (ch.Race.parts.Contains(PartFlags.Fangs) ? "bite" : (ch.IsNPC ? "hit" : "punch")));

            if (ch.Form != null)
            {
                damagenoun = ch.Form.DamageType.Message;
                weaponSkillLevel = 100;
            }

            if (skill != null && !skill.NounDamage.ISEMPTY())
            {
                damagenoun = skill.NounDamage;
            }
            else if (weapon == null)
            {
                // Look for an affect that modifies the damage noun
                foreach (var affect in ch.AffectsList.ToArray())
                {
                    if (affect.where == AffectWhere.ToDamageNoun && affect.skillSpell != null)
                    {
                        damagemodifier = affect;
                        damagenoun = affect.skillSpell.NounDamage;
                    }
                }
            }
            else if (weapon != null)
            {
                if (weapon.WeaponDamageType != null)
                    damagenoun = weapon.WeaponDamageType.Message.ToString().ToLower();
                else
                    game.bug("{0} has null weapon damage message on {1}", weapon.Vnum, ch.Name);
            }

            // Check for special conditions that affect the hit
            if (ch.IsAffected(SkillSpell.SkillLookup("feint")))
            {
                ch.Act("\\C$n swings wildly and misses $N.\\x", victim, type: ActType.ToRoomNotVictim);
                ch.Act("\\C$n swings wildly and misses you.\\x", victim, type: ActType.ToVictim);
                ch.Act("\\CYou swing wildly and miss $N.\\x", victim, type: ActType.ToChar);
                ch.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("feint")));
                return;
            }

            if (ch.IsAffected(AffectFlags.Distracted))
            {
                ch.Act("\\C$n swings wildly and misses $N.\\x", victim, type: ActType.ToRoomNotVictim);
                ch.Act("\\C$n swings wildly and misses you.\\x", victim, type: ActType.ToVictim);
                ch.Act("\\CYou swing wildly and miss $N.\\x", victim, type: ActType.ToChar);
                ch.AffectFromChar(ch.FindAffect(AffectFlags.Distracted));
                return;
            }

            if (ch.IsAffected(AffectFlags.GrandNocturne))
            {
                if (Utility.Random(1, 100) <= Utility.Random(80, 90))
                {
                    ch.Act("\\C$n swings wildly and misses $N.\\x", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("\\C$n swings wildly and misses you.\\x", victim, type: ActType.ToVictim);
                    ch.Act("\\CYou swing wildly and miss $N.\\x", victim, type: ActType.ToChar);
                    return;
                }
            }

            // Check for more special conditions
            if (ch.IsAffected(AffectFlags.ZigZagFeint))
            {
                ch.Act("\\C$n swings wildly and misses $N.\\x", victim, type: ActType.ToRoomNotVictim);
                ch.Act("\\C$n swings wildly and misses you.\\x", victim, type: ActType.ToVictim);
                ch.Act("\\CYou swing wildly and miss $N.\\x", victim, type: ActType.ToChar);
                ch.AffectFromChar(ch.FindAffect(AffectFlags.ZigZagFeint));
                return;
            }

            if (ch.IsAffected(SkillSpell.SkillLookup("thrust")))
            {
                ch.Act("$n swings wildly and misses $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n swings wildly and misses you.", victim, type: ActType.ToVictim);
                ch.Act("You swing wildly and miss $N.", victim, type: ActType.ToChar);
                ch.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("thrust")));
                return;
            }

            // Check if the hit is prevented due to distance or defensive actions
            if (CheckDistance(ch, victim, weapon, damagenoun)) return;
            if (CheckDefensiveSpin(ch, victim, weapon, damagenoun)) return;
            if (CheckShieldBlock(ch, victim, weapon, damagenoun)) return;
            if (CheckDodge(ch, victim, weapon, damagenoun)) return;
            if (CheckParry(ch, victim, weapon, damagenoun)) return;

            int victim_ac = 0;

            // Calculate the victim's armor class based on damage type
            if (victim.Form == null)
            {
                switch (weapon != null ? weapon.WeaponDamageType.Type : WeaponDamageTypes.Bash)
                {
                    default: victim_ac += ch.ArmorExotic; break;
                    case WeaponDamageTypes.Bash:
                        victim_ac += ch.ArmorBash;
                        break;
                    case WeaponDamageTypes.Slash:
                        victim_ac += ch.ArmorSlash;
                        break;
                    case WeaponDamageTypes.Pierce:
                        victim_ac += ch.ArmorPierce;
                        break;
                }

                foreach (var equipment in victim.Equipment)
                {
                    if (equipment.Value == null) continue;
                    switch (weapon != null ? weapon.WeaponDamageType.Type : WeaponDamageTypes.Bash)
                    {
                        default: victim_ac += equipment.Value.ArmorExotic; break;
                        case WeaponDamageTypes.Bash:
                            victim_ac += equipment.Value.ArmorBash;
                            break;
                        case WeaponDamageTypes.Slash:
                            victim_ac += equipment.Value.ArmorSlash;
                            break;
                        case WeaponDamageTypes.Pierce:
                            victim_ac += equipment.Value.ArmorPierce;
                            break;
                    }
                }
            }
            else
            {
                switch (weapon != null ? weapon.WeaponDamageType.Type : WeaponDamageTypes.Bash)
                {
                    default: victim_ac += victim.Form.ArmorExotic; break;
                    case WeaponDamageTypes.Bash:
                        victim_ac += victim.Form.ArmorBash;
                        break;
                    case WeaponDamageTypes.Slash:
                        victim_ac += victim.Form.ArmorSlash;
                        break;
                    case WeaponDamageTypes.Pierce:
                        victim_ac += victim.Form.ArmorPierce;
                        break;
                }
            }
            victim_ac += victim.ArmorClass;
            victim_ac += PhysicalStats.DexterityApply[victim.GetCurrentStat(PhysicalStatTypes.Dexterity)].Defensive;
            victim_ac /= 10;

            int thac0;
            int thac0_00 = 20;
            int thac0_32 = -4;

            if (ch.IsNPC)
            {
                thac0_00 = 20;
                thac0_32 = -4;
            }

            if (ch.Guild != null)
            {
                thac0_00 = ch.Guild.THAC0;
                thac0_32 = ch.Guild.THAC032;
            }

            thac0 = Utility.interpolate(ch.Level, thac0_00, thac0_32);

            if (thac0 < 0)
                thac0 = thac0 / 2;

            if (thac0 < -5)
                thac0 = -5 + (thac0 + 5) / 2;

            thac0 -= ch.GetHitRoll * weaponSkillLevel / 100;
            thac0 += 5 * (100 - weaponSkillLevel) / 100;

            if (victim_ac < -15)
                victim_ac = (victim_ac + 15) / 5 - 15;

            if (!ch.CanSee(victim))
                victim_ac -= 4;

            if (victim.Position < Positions.Fighting)
                victim_ac += 4;

            if (victim.Position < Positions.Resting)
                victim_ac += 6;

            var diceroll = Utility.Random(0, 19);
            if (diceroll == 0 || (diceroll != 19 && diceroll < thac0 - victim_ac))
            {
                // Check for special programs attached to the character's equipment
                foreach (var item in ch.Equipment.Values)
                {
                    if (item == null) continue;

                    Programs.ExecutePrograms(Programs.ProgramTypes.OneHitMiss, ch, item, "");
                }

                // Check for special programs attached to the character (NPCs only)
                if (ch is NPCData)
                {
                    Programs.ExecutePrograms(Programs.ProgramTypes.OneHitMiss, ch, victim, null, "");
                }

                damage = 0;
                if (offhand)
                    ch.CheckImprove(SkillSpell.SkillLookup("dual wield"), false, 1);
                ch.CheckImprove(weaponSkill, false, 1);
                if (damagemodifier != null) ch.CheckImprove(damagemodifier.skillSpell, false, 1);

                if (ch.IsAffected(AffectFlags.PlayDead))
                    ch.AffectedBy.REMOVEFLAG(AffectFlags.PlayDead);

                ch.StripCamouflage();

                if (victim.IsAffected(AffectFlags.Burrow))
                {
                    victim.AffectedBy.REMOVEFLAG(AffectFlags.Burrow);
                    victim.Act("$n leaves $s burrow.", type: ActType.ToRoom);
                    victim.Act("You leave your burrow.");
                }
                if (victim.IsAffected(AffectFlags.PlayDead))
                    victim.AffectedBy.REMOVEFLAG(AffectFlags.PlayDead);

                victim.StripCamouflage();

                Damage(ch, victim, (int)damage, damagenoun, ((weapon != null && weapon.WeaponDamageType != null) ? weapon.WeaponDamageType.Type : WeaponDamageTypes.Bash), "", weapon);
                CheckSuckerHit(ch);
                return;
            }

            if (ch.Form != null)
            {
                damage = ch.Form.DamageDice.Roll();
            }
            else if (ch.IsNPC && (weaponSkill == null || weaponSkill.name == "hand to hand"))
            {
                if (ch.DamageDice != null && ch.DamageDice.HasValue)
                    damage = ((NPCData)ch).DamageDice.Roll();
                else
                    damage = Utility.Random(ch.Level / 2, ch.Level * 3 / 2);
            }
            else if (weapon != null)
            {
                damage = weapon.DamageDice.Roll();

                if (ch.GetEquipment(WearSlotIDs.Shield) == null)  /* no shield = more */
                    damage = damage * 11 / 10;

                if (weapon.Level - ch.Level >= 35)
                    damage = (damage * 6) / 10;
                else if (weapon.Level - ch.Level >= 25)
                    damage = (damage * 7) / 10;
                else if (weapon.Level - ch.Level >= 15)
                    damage = (damage * 8) / 10;
                else if (weapon.Level - ch.Level >= 5)
                    damage = (damage * 9) / 10;

                /* sharpness! */
                if (weapon.extraFlags.ISSET(ExtraFlags.Sharp))
                {
                    int percent;

                    if ((percent = Utility.NumberPercent()) <= (weaponSkillLevel / 8))
                        damage = 2 * damage + (damage * 2 * percent / 100);
                }
            }
            else
                damage = Utility.Random(1 + 4 * weaponSkillLevel / 100, 2 * ch.Level / 3 * weaponSkillLevel / 100);

            if (offhand)
                ch.CheckImprove(SkillSpell.SkillLookup("dual wield"), true, 1);
            ch.CheckImprove(weaponSkill, true, 1);

            // channel heat damage modifier
            if (damagemodifier != null)
            {
                ch.CheckImprove(damagemodifier.skillSpell, true, 1);
                damage += damagemodifier.level;
            }

            damage += ch.Level / 8;

            CheckEnhancedDamage(ch, ref damage);
            CheckPreyOnTheWeak(ch, victim, ref damage);

            if (!victim.IsAwake)
                damage *= 2;
            else if (victim.Position < Positions.Fighting)
                damage = damage * 3 / 2;

            damage += ch.GetDamageRoll * Math.Min(100, weaponSkillLevel) / 100;

            int ac = 0;

            if (victim.Form == null)
            {
                foreach (var equipment in victim.Equipment)
                {
                    if (equipment.Value == null) continue;
                    switch (weapon != null ? weapon.WeaponDamageType.Type : WeaponDamageTypes.Bash)
                    {
                        default: ac += equipment.Value.ArmorExotic; break;
                        case WeaponDamageTypes.Bash:
                            ac += equipment.Value.ArmorBash;
                            break;
                        case WeaponDamageTypes.Slash:
                            ac += equipment.Value.ArmorSlash;
                            break;
                        case WeaponDamageTypes.Pierce:
                            ac += equipment.Value.ArmorPierce;
                            break;
                    }

                    // extra chance for shield to block some damage
                    if (equipment.Key == WearSlotIDs.Shield)
                        ac += (int)Math.Round(10f * Math.Max((float)victim.GetSkillPercentage(SkillSpell.SkillLookup("shield block")) / 100f, .2f));

                    //ac *= 10;
                    if ((equipment.Value.ItemType.ISSET(ItemTypes.Armor) || equipment.Value.ItemType.ISSET(ItemTypes.Clothing))
                        && ac > Utility.NumberPercent())
                    {
                        ch.Act("$N's $o blocks some of the damage.", victim, equipment.Value, null, ActType.ToRoomNotVictim);
                        ch.Act("$N's $o blocks some of the damage.", victim, equipment.Value, null, ActType.ToChar);
                        ch.Act("Your $o blocks some of the damage.", victim, equipment.Value, null, ActType.ToVictim);

                        damage = (int)Math.Round((float)damage * Utility.Random(.10f, Math.Max(ac / 100f, .15f)));
                        break;
                    }
                    ac = 0;
                }

                Programs.ExecutePrograms(Programs.ProgramTypes.OneHitHit, ch, victim, null, "");
                // Check for special programs attached to the character's equipment
                foreach (var item in ch.Equipment.Values)
                {
                    if (item == null) continue;

                    Programs.ExecutePrograms(Programs.ProgramTypes.OneHitHit, ch, item, "");
                }

            }

            var damreduc = victim.GetSkillPercentage("damage reduction");
            if (damreduc > 1)
                damage = (int)Math.Round((float)damage * Math.Max((100 - damreduc) / 100f, .15f)); //utility.rand(.10f, Math.Max(damreduc / 100f, .15f)));

            if (ch.IsAffected(AffectFlags.PlayDead))
                ch.AffectedBy.REMOVEFLAG(AffectFlags.PlayDead);

            ch.StripCamouflage();

            if (victim.IsAffected(AffectFlags.Burrow))
            {
                victim.AffectedBy.REMOVEFLAG(AffectFlags.Burrow);
                victim.Act("$n leaves $s burrow.", type: ActType.ToRoom);
                victim.Act("You leave your burrow.");
            }
            if (victim.IsAffected(AffectFlags.PlayDead))
                victim.AffectedBy.REMOVEFLAG(AffectFlags.PlayDead);

            victim.StripCamouflage();

            Damage(ch, victim, (int)damage, damagenoun, ((weapon != null && weapon.WeaponDamageType != null) ?
                weapon.WeaponDamageType.Type : damagemodifier != null && damagemodifier.DamageTypes.Any() ?
                damagemodifier.DamageTypes.First() : WeaponDamageTypes.Bash), "", weapon);

            CheckChargeWeapon(ch, victim, weapon);

            CheckPoison(ch, victim, weapon);

            CheckRabies(ch, victim);

            CheckSeals(ch, victim);

            CheckQuillDefense(ch, victim, weapon);

            CheckMucousDefense(ch, victim, weapon);
        } // end oneHit

        /// <summary>
        /// Checks for enhanced damage bonus and applies it to the damage value.
        /// </summary>
        /// <param name="ch">The character performing the attack.</param>
        /// <param name="damage">The damage value to be modified.</param>
        /// <returns>True if enhanced damage bonus is applied, false otherwise.</returns>
        public static bool CheckEnhancedDamage(Character ch, ref float damage)
        {
            // Retrieve the "enhanced damage" skill
            var enhancedDamageSkill = SkillSpell.SkillLookup("enhanced damage");

            int skillLevel = 0;
            int diceroll = 0;

            // Check if the character has the enhanced damage skill and a skill level greater than 1
            if (damage > 0 && enhancedDamageSkill != null && (skillLevel = ch.GetSkillPercentage(enhancedDamageSkill)) > 1)
            {
                // Roll a random diceroll value
                if (skillLevel > (diceroll = Utility.NumberPercent()))
                {
                    // The character successfully performed enhanced damage
                    ch.CheckImprove(enhancedDamageSkill, true, 1);

                    // Increase the damage by a percentage of the original damage based on the diceroll value
                    damage += (damage * diceroll / 125);

                    // Check for "enhanced damage II" skill
                    if ((skillLevel = ch.GetSkillPercentage("enhanced damage II")) > 1 && skillLevel > (diceroll = Utility.NumberPercent()))
                    {
                        // The character successfully performed enhanced damage II
                        damage += (damage * diceroll / 125);
                        ch.CheckImprove("enhanced damage II", true, 1);
                    }
                    else if (skillLevel > 1)
                    {
                        // The character failed to perform enhanced damage II
                        ch.CheckImprove("enhanced damage II", false, 1);
                    }
                }
                else
                {
                    // The character failed to perform enhanced damage
                    ch.CheckImprove(enhancedDamageSkill, false, 1);
                }

                return true;
            }

            // No enhanced damage bonus applied
            return false;
        }

        private static void CheckPoison(Character ch, Character victim, ItemData weapon)
        {
            AffectData weaponaffect;

            if (weapon != null && (weaponaffect = weapon.FindAffect(AffectFlags.Poison)) != null && Utility.Random(0, 10) == 0 && !victim.IsAffected(AffectFlags.Poison))
            {
                victim.Act("$p poisons $n.", null, weapon, type: ActType.ToRoom);
                victim.Act("$p poisons you.", null, weapon);
                var skPoison = SkillSpell.SkillLookup("poison");
                var affect = new AffectData();
                affect.skillSpell = skPoison;
                affect.displayName = "poison";
                affect.duration = 5;
                affect.flags.SETBIT(AffectFlags.Poison);
                affect.endMessage = "You feel better.";
                affect.endMessageToRoom = "$n looks better.";
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Strength;
                affect.ownerName = ch.Name;
                affect.modifier = -4;
                affect.level = weaponaffect != null ? weaponaffect.level : ch.Level;
                victim.AffectToChar(affect);

            }
        }

        private static void CheckRabies(Character ch, Character victim)
        {
            var skRabies = SkillSpell.SkillLookup("rabies");

            if (ch.IsAffected(AffectFlags.Rabies) && Utility.Random(0, 10) == 0 && !victim.IsAffected(skRabies))
            {
                victim.Act("You give $n rabies.", ch, type: ActType.ToVictim);
                victim.Act("$N gives $n rabies.", ch, type: ActType.ToRoomNotVictim);
                victim.Act("$N gives you rabies.", ch);

                var affect = new AffectData();
                affect.skillSpell = skRabies;
                affect.displayName = skRabies.name;
                affect.duration = 5;
                affect.endMessage = "You feel better.";
                affect.endMessageToRoom = "$n looks better.";
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Strength;
                affect.ownerName = ch.Name;
                affect.modifier = -4;
                affect.level = ch.Level;
                victim.AffectToChar(affect);

            }
        }

        private static void CheckMucousDefense(Character ch, Character victim, ItemData weapon)
        {
            if ((victim.Position == Positions.Fighting || victim.Position == Positions.Standing) &&
                victim.Room == ch.Room &&
                victim.GetSkillPercentage("mucous defense") > 1 &&
                (weapon == null || (weapon.WeaponType != WeaponTypes.Spear && weapon.WeaponType != WeaponTypes.Polearm))
                && Utility.Random(1, 3) == 1)
            {
                if (ch.IsAffected(AffectFlags.Poison))
                {
                    return;
                }
                if (Magic.SavesSpell(ch.Level, victim, WeaponDamageTypes.Poison))
                {
                    victim.Act("$n turns slightly green, but it passes.", type: ActType.ToRoom);
                    victim.send("You feel momentarily ill, but it passes.\n\r");
                    return;
                }

                var affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = SkillSpell.SkillLookup("poison");
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Strength;
                affect.flags.Add(AffectFlags.Poison);
                affect.duration = Utility.Random(5, 10);
                affect.modifier = -3;
                affect.displayName = "Poisoned";
                affect.endMessage = "You feel less sick.\n\r";
                affect.endMessageToRoom = "$n is looking less sick.\n\r";
                affect.affectType = AffectTypes.Skill;

                ch.AffectToChar(affect);
                ch.Act("You feel poisoned from $N's mucous defense.\n\r", victim);
                ch.Act("$n gets poisoned from $N's mucous defense.", victim, null, null, ActType.ToRoom);
            }

        }

        private static void CheckQuillDefense(Character ch, Character victim, ItemData weapon)
        {
            if ((victim.Position == Positions.Fighting || victim.Position == Positions.Standing) &&
                victim.Room == ch.Room &&
                victim.GetSkillPercentage("quill defense") > 1 &&
                (weapon == null || (weapon.WeaponType != WeaponTypes.Spear && weapon.WeaponType != WeaponTypes.Polearm)))
            {
                int quilldamage = Utility.Random(120, 160);
                Damage(victim, ch, quilldamage, "quills", WeaponDamageTypes.Pierce, "", null);
            }
        }

        private static void CheckSeals(Character ch, Character victim)
        {
            AffectData affect;
            var skill = SkillSpell.SkillLookup("seal of righteousness");
            int chance;
            if (ch.Fighting == victim && (affect = ch.FindAffect(skill)) != null && (chance = ch.GetSkillPercentage(skill)) > 1)
            {
                if (chance / 3 > Utility.NumberPercent())
                {
                    var damage = Utility.dice(4, ch.Level / 2, 20);
                    if (victim.Alignment == Alignment.Evil) damage *= 2;

                    ch.CheckImprove(skill, true, 1);
                    Damage(ch, victim, damage, skill, WeaponDamageTypes.Wrath);
                    //Damage(ch, victim, damage, skill, WeaponDamageTypes.Wrath);
                }
                else
                    ch.CheckImprove(skill, false, 1);
            }
            else if ((skill = SkillSpell.SkillLookup("seal of light")) != null && (affect = ch.FindAffect(skill)) != null && (chance = ch.GetSkillPercentage(skill)) > 1)
            {
                if (chance / 3 > Utility.NumberPercent())
                {
                    var heal = Utility.dice(2, ch.Level / 2, ch.Level / 2);
                    ch.CheckImprove(skill, true, 1);
                    ch.HitPoints = Math.Min(ch.HitPoints + heal, ch.MaxHitPoints);
                    ch.Act("$n is healed by $s seal of light.", type: ActType.ToRoom);
                    ch.Act("You are healed {0} hp's from your seal of light.", type: ActType.ToChar, args: heal);
                }
                else
                    ch.CheckImprove(skill, false, 1);
            }
            else if ((skill = SkillSpell.SkillLookup("seal of wisdom")) != null && (affect = ch.FindAffect(skill)) != null && (chance = ch.GetSkillPercentage(skill)) > 1)
            {
                if (chance / 3 > Utility.NumberPercent())
                {
                    var managain = Utility.dice(6, ch.Level / 2);
                    ch.CheckImprove(skill, true, 1);
                    ch.ManaPoints = Math.Min(ch.ManaPoints + managain, ch.MaxManaPoints);
                    ch.Act("$n's mind is restored by $s seal of wisdom.", type: ActType.ToRoom);
                    ch.Act("Your mind is restored by your seal of wisdom.", type: ActType.ToChar);
                }
                else
                    ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoKill(Character ch, string arguments)
        {
            Character victim;
            int count = 0;

            if (ch.IsAffected(AffectFlags.Calm)) { ch.send("You feel too calm to attack.\n\r"); return; }

            if ((victim = ch.GetCharacterFromRoomByName(arguments, ref count)) != null && victim != ch)
            {
                if (CheckIsSafe(ch, victim)) return;
                ItemData weapon;
                ch.Position = Positions.Fighting;
                ch.Fighting = victim;
                if (ch.Form == null)
                    ch.Equipment.TryGetValue(WearSlotIDs.Wield, out weapon);
                else
                    weapon = null;

                Combat.oneHit(ch, victim, weapon);
                ch.WaitState(game.PULSE_VIOLENCE);
            }
            else if (victim == ch)
                ch.send("Suicide is a mortal sin.\n\r");
            else
                ch.send("You don't see them here.\n\r");
        }

        public static void DoFlee(Character ch, string arguments)
        {
            if (ch.Fighting == null)
                ch.send("You aren't fighting anyone!\n\r");
            else
            {
                var exits = new List<ExitData>(from exit in ch.Room.exits where exit != null && exit.destination != null && (!exit.flags.Contains(ExitFlags.Closed) || (!exit.flags.ISSET(ExitFlags.NoPass) && ch.IsAffected(AffectFlags.PassDoor))) && !exit.flags.Contains(ExitFlags.Window) select exit);
                if (exits.Count > 0)
                {
                    // TODO Chance to fail
                    int grip;
                    if (ch.Fighting != null && ch.Fighting.Fighting == ch && (grip = ch.Fighting.GetSkillPercentage("grip")) > 1)
                    {
                        if (grip > Utility.NumberPercent())
                        {
                            ch.Fighting.Act("$n grips you in its strong raptorial arms, preventing you from fleeing.", ch, type: ActType.ToVictim);
                            ch.Fighting.Act("You grip $N in your strong raptorial arms, preventing $M from fleeing.", ch, type: ActType.ToChar);
                            ch.Fighting.Act("$n grips $N in its strong raptorial arms, preventing $M from fleeing.", ch, type: ActType.ToRoomNotVictim);
                            return;
                        }
                    }

                    if (ch.IsAffected(SkillSpell.SkillLookup("secreted filament")) && Utility.Random(0, 1) == 0)
                    {
                        ch.Act("The secreted filament covering you prevents you from fleeing.\n\r");
                        ch.Act("$n tries to flee, but the filament covering $m prevents $m from doing so.", type: ActType.ToRoom);

                        return;
                    }
                    bool cutoff = false;
                    foreach (var fightingch in ch.Room.Characters.ToArray())
                    {
                        if (fightingch.Fighting == ch)
                        {
                            cutoff = Combat.CheckCutoff(fightingch, ch);
                            Combat.CheckPartingBlow(fightingch, ch);
                        }
                    }
                    if (cutoff) return;

                    if (Utility.Random(1, 10) == 1)
                    {
                        ch.Act("PANIC! You couldn't escape!");
                        return;
                    }
                    var exit = exits[Utility.Random(0, exits.Count - 1)];

                    //RoomData WasInRoom = ch.Room;

                    var wasFighting = ch.Fighting;
                    var wasPosition = ch.Position;
                    var wasInRoom = ch.Room;
                    ch.Fighting = null;
                    ch.Position = Positions.Standing;
                    int chance = 0;
                    if ((chance = ch.GetSkillPercentage("rogues awareness") + 20) > 21 && chance > Utility.NumberPercent())
                    {
                        ch.send("You flee " + exit.direction.ToString().ToLower() + ".\n\r");
                        ch.CheckImprove("rogues awareness", true, 1);
                    }
                    else
                    {
                        if (chance > 21)
                        {
                            ch.CheckImprove("rogues awareness", false, 1);
                        }
                        ch.send("You flee from combat.\n\r");
                    }
                    ch.Act("$n flees {0}.\n\r", null, null, null, ActType.ToRoom, exit.direction.ToString().ToLower());
                    ch.moveChar(exit.direction, false, false, false);

                    if (ch.Room == wasInRoom)
                    {
                        ch.Fighting = wasFighting;
                        ch.Position = wasPosition;
                    }
                    else
                    {
                        if (wasFighting != null && wasFighting.Fighting == ch)
                            wasFighting.Fighting = null;

                        foreach (var fightingme in wasInRoom.Characters)
                            if (fightingme.Fighting == ch)
                            {
                                // Find someone else to fight
                                var fightingother = (from other in fightingme.Room.Characters where other.Fighting == fightingme select other).SelectRandom();
                                if (fightingother != null)
                                    fightingme.Fighting = fightingother;
                                else if (fightingme.Position == Positions.Fighting) // No one else to fight? go back to standing
                                {
                                    fightingme.Fighting = null;
                                    fightingme.Position = Positions.Standing;
                                }

                            }
                    }
                    //if (ch.Room != WasInRoom) // flee successful, not a loop exit
                    //{

                    //}

                }
                else
                    ch.send("You don't see anywhere to flee to!\n\r");
            }
        }

        public static void DoKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You better leave the martial arts to fighters.\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = (ch.Level) / 2;
                dam += Utility.Random(0, (ch.Level) / 6);
                dam += Utility.Random(0, (ch.Level) / 6);
                dam += Utility.Random(0, (ch.Level) / 6);
                dam += Utility.Random(0, (ch.Level) / 6);
                dam += Utility.Random(0, (ch.Level) / 6);
                dam += Utility.Random(0, (ch.Level) / 6);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);


                Damage(ch, victim, dam, skill);

                ch.CheckImprove(skill, true, 1);

            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }


        public static void DoDirtKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("dirt kick");
            int chance;
            if ((chance = skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You get your feet dirty.\n\r");
                return;
            }
            int count = 0;
            if ((victim = ch.Fighting) == null && (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments, ref count)) == null) || victim == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            AffectData aff;
            if ((aff = victim.FindAffect(AffectFlags.Blind)) != null)
            {
                ch.send("They are already blinded.\n\r");
                return;
            }
            if (CheckIsSafe(ch, victim)) return;

            ch.WaitState(skill.waitTime);

            /* stats */
            chance += ch.GetCurrentStat(PhysicalStatTypes.Dexterity);
            chance -= 2 * victim.GetCurrentStat(PhysicalStatTypes.Dexterity);

            /* speed  */
            if (ch.Flags.ISSET(ActFlags.Fast) || ch.IsAffected(AffectFlags.Haste))
                chance += 10;
            if (victim.Flags.ISSET(ActFlags.Fast) || victim.IsAffected(AffectFlags.Haste))
                chance -= 30;

            /* level */
            chance += (ch.Level - victim.Level) * 2;

            /* sloppy hack to prevent false zeroes */
            if (chance % 5 == 0)
                chance += 1;

            /* terrain */

            switch (ch.Room.sector)
            {
                case SectorTypes.Inside: chance -= 20; break;
                case SectorTypes.City: chance -= 10; break;
                case SectorTypes.Field: chance += 5; break;
                case SectorTypes.Forest:
                case SectorTypes.Hills:
                    break;
                case SectorTypes.Mountain: chance -= 10; break;
                case SectorTypes.WaterSwim:
                case SectorTypes.WaterNoSwim:
                case SectorTypes.Underwater:
                    chance = 0; break;
                case SectorTypes.Air: chance = 0; break;
                case SectorTypes.Desert: chance += 10; break;
            }

            if (chance == 0)
            {
                ch.send("There isn't any dirt to kick.\n\r");
                return;
            }

            if (chance > Utility.NumberPercent())
            {
                dam = Utility.Random(2, 5);

                victim.Act("$n is blinded by the dirt in $s eyes!", type: ActType.ToRoom);
                ch.Act("$n kicks dirt in your eyes!", victim, type: ActType.ToVictim);

                Damage(ch, victim, dam, skill, WeaponDamageTypes.Blind);//, DAM_BASH, true);

                if (!(victim.ImmuneFlags.ISSET(WeaponDamageTypes.Blind) || (victim.Form != null && victim.Form.ImmuneFlags.ISSET(WeaponDamageTypes.Blind))) || victim.IsAffected(AffectFlags.Deafen))
                {
                    AffectData newAffect = new AffectData();
                    newAffect.flags.Add(AffectFlags.Blind);
                    newAffect.duration = 0;
                    newAffect.displayName = "dirt kick";
                    newAffect.modifier = -4;
                    newAffect.location = ApplyTypes.Hitroll;
                    newAffect.skillSpell = skill;
                    newAffect.endMessage = "You wipe the dirt from your eyes.\n\r";
                    newAffect.endMessageToRoom = "$n wipes the dirt from their eyes.\n\r";
                    newAffect.affectType = AffectTypes.Skill;
                    victim.AffectToChar(newAffect);
                }
                else
                    victim.Act("$n is immune to blindness.", type: ActType.ToRoom);
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                Damage(ch, victim, 0, skill);

                ch.CheckImprove(skill, false, 1);
            }
        }


        public static bool CheckDodge(Character ch, Character victim, ItemData weapon, string damageType)
        {
            float chance;
            int dex, dexa;
            SkillSpell skDodge;
            if (victim == null) return false;

            if (!victim.IsAwake)
                return false;

            skDodge = SkillSpell.SkillLookup("dodge");

            if (skDodge == null) return false;

            var skillDodge = victim.GetSkillPercentage(skDodge);

            if (victim.GetSkillPercentage("tree climb") > 1 && victim.Room.sector == SectorTypes.Forest || victim.Room.sector == SectorTypes.Cave)
            {
                skillDodge = 120;
            }

            chance = (3 * skillDodge / 10);

            dex = victim.GetCurrentStat(PhysicalStatTypes.Dexterity);
            dexa = ch.GetCurrentStat(PhysicalStatTypes.Dexterity);
            if (dex <= 5)
                chance += 0;
            else if (dex <= 10)
                chance += dex / 2;
            else if (dex <= 15)
                chance += (2 * dex / 3);
            else if (dex <= 20)
                chance += (8 * dex / 10);
            else
                chance += dex;
            chance += dex - dexa;
            chance += (ch.Size - victim.Size) * 5;


            if (!victim.CanSee(ch))
                chance *= Utility.Random(6 / 10, 3 / 4);

            if (ch.IsAffected(AffectFlags.Haste)) chance -= 20;
            if (victim.IsAffected(AffectFlags.Haste)) chance += 5;
            if (ch.IsAffected(AffectFlags.Slow)) chance += 5;
            if (victim.IsAffected(AffectFlags.Slow)) chance -= 5;

            if (ch.Form != null) chance = chance * (100 - ch.Form.ParryModifier) / 100;

            if (Utility.NumberPercent() >= chance + victim.Level - ch.Level)
            {
                victim.CheckImprove(skDodge, false, 4);
                return false;
            }

            // check concealed sends its own dodge message
            if (!CheckConcealed(victim, ch, damageType))
            {
                ch.Act("You \\Cdodge\\x $n's {0}.", victim, null, null, ActType.ToVictim, damageType);
                ch.Act("$N \\Cdodges\\x your {0}.", victim, null, null, ActType.ToChar, damageType);
            }
            victim.CheckImprove(skDodge, true, 5);

            CheckOwaza(victim, ch);


            return true;
        } // end check dodge

        public static bool CheckParry(Character ch, Character victim, ItemData weapon, string damageType)
        {
            float chance = 0;
            SkillSpell skParry;
            SkillSpell skWeapon = null;
            ItemData victimWield;
            ItemData victimDualWield;
            SkillSpell skVictimWield = null;
            SkillSpell skVictimDualWield = null;
            SkillSpell skVictimDualWieldSkill = null;

            if (!victim.IsAwake)
                return false;

            skParry = SkillSpell.SkillLookup("parry");
            victim.Equipment.TryGetValue(WearSlotIDs.Wield, out victimWield);
            victim.Equipment.TryGetValue(WearSlotIDs.DualWield, out victimDualWield);

            if (weapon != null) // weapon ch is using to hit victim, skill lowers chance of parry
            {
                skWeapon = SkillSpell.SkillLookup(weapon.WeaponType.ToString());
                chance -= ch.GetSkillPercentage(skWeapon) / 10;

                switch (weapon.WeaponType)
                {
                    default: chance += 15; break;
                    case WeaponTypes.Sword: chance += 5; break;
                    case WeaponTypes.Dagger: chance += 5; break;
                    case WeaponTypes.Spear: chance -= 5; break;
                    case WeaponTypes.Staff: chance -= 5; break;
                    case WeaponTypes.Mace: chance -= 5; break;
                    case WeaponTypes.Axe: chance -= 5; break;
                    case WeaponTypes.Flail: chance += 10; break;
                    case WeaponTypes.Whip: chance += 10; break;
                    case WeaponTypes.Polearm: chance -= 5; break;
                }
            }

            if (victimWield != null)
            {
                skVictimWield = SkillSpell.SkillLookup(victimWield.WeaponType.ToString());
                var skillChance = victim.GetSkillPercentage(skVictimWield);
                chance += skillChance / 10;

                switch (victimWield.WeaponType)
                {
                    default: chance += 15; break;
                    case WeaponTypes.Sword: chance += 10; break;
                    case WeaponTypes.Dagger: chance -= 20; break;
                    case WeaponTypes.Spear: chance += 20; break;
                    case WeaponTypes.Staff: chance += 10; break;
                    case WeaponTypes.Mace: chance -= 20; break;
                    case WeaponTypes.Axe: chance -= 25; break;
                    case WeaponTypes.Flail: chance -= 10; break;
                    case WeaponTypes.Whip: chance -= 10; break;
                    case WeaponTypes.Polearm: chance += 10; break;
                }
            }

            if (victimDualWield != null)
            {
                skVictimDualWield = SkillSpell.SkillLookup(victimDualWield.WeaponType.ToString());
                skVictimDualWieldSkill = SkillSpell.SkillLookup("dual wield");

                var skillChance = victim.GetSkillPercentage(skVictimDualWield); ;
                chance += skillChance / 10;
                skillChance = victim.GetSkillPercentage(skVictimDualWieldSkill);
                chance += skillChance / 10;
                switch (victimDualWield.WeaponType)
                {
                    default: chance += 15; break;
                    case WeaponTypes.Sword: chance += 10; break;
                    case WeaponTypes.Dagger: chance -= 20; break;
                    case WeaponTypes.Spear: chance += 20; break;
                    case WeaponTypes.Staff: chance += 10; break;
                    case WeaponTypes.Mace: chance -= 20; break;
                    case WeaponTypes.Axe: chance -= 25; break;
                    case WeaponTypes.Flail: chance -= 10; break;
                    case WeaponTypes.Whip: chance -= 10; break;
                    case WeaponTypes.Polearm: chance += 10; break;
                }
            }

            if (skParry == null || (!victim.IsNPC && victim.GetSkillPercentage(skParry) == 0)) return false;

            //if (!victim.isNPC)
            chance += victim.GetSkillPercentage(skParry) / 2;
            SkillSpell unarmeddefense = SkillSpell.SkillLookup("unarmed defense");
            int unarmeddefensechance = 0;
            if (skVictimWield == null && skVictimDualWield == null && (unarmeddefensechance = victim.GetSkillPercentage(unarmeddefense)) > 1)
                chance += unarmeddefensechance / 2;

            SkillSpell ironfists = SkillSpell.SkillLookup("ironfists");
            int ironfistschance = 0;
            if (victimWield == null && victimDualWield == null && (ironfistschance = victim.GetSkillPercentage(ironfists)) > 1)
                chance += ironfistschance / 2;

            SkillSpell flourintine = SkillSpell.SkillLookup("flourintine");
            int flourintinechance = 0;
            // must dual wield swords for flourintine
            if (((victimWield != null && victimWield.WeaponType == WeaponTypes.Sword) && (victimDualWield != null && victimDualWield.WeaponType == WeaponTypes.Sword)) && (flourintinechance = victim.GetSkillPercentage(flourintine)) > 1)
            {
                chance += flourintinechance / 5;
            }
            //else
            //    chance /= 5;
            if (ch.IsAffected(AffectFlags.Haste)) chance -= 20;
            if (victim.IsAffected(AffectFlags.Haste)) chance += 5;
            if (ch.IsAffected(AffectFlags.Slow)) chance += 5;
            if (victim.IsAffected(AffectFlags.Slow)) chance -= 5;

            if (ch.Form != null) chance = chance * (100 - ch.Form.ParryModifier) / 100;

            if (Utility.NumberPercent() >= chance + victim.Level - ch.Level)
            {
                if (unarmeddefensechance > 1)
                    victim.CheckImprove(unarmeddefense, false, 2);
                if (ironfistschance > 1)
                    victim.CheckImprove(ironfists, false, 2);
                if (flourintinechance > 1)
                    victim.CheckImprove(flourintine, true, 2);
                victim.CheckImprove(skParry, false, 4);
                return false;
            }

            if (unarmeddefensechance > 1 || ironfistschance > 1)
            {
                ch.Act("You \\Cparry\\x $n's {0} with your bare hands.", victim, null, null, ActType.ToVictim, damageType);
                ch.Act("$N \\Cparries\\x your {0} with $S bare hands.", victim, null, null, ActType.ToChar, damageType);
            }
            else
            {
                ch.Act("You \\Cparry\\x $n's {0}.", victim, null, null, ActType.ToVictim, damageType);
                ch.Act("$N \\Cparries\\x your {0}.", victim, null, null, ActType.ToChar, damageType);
            }

            victim.CheckImprove(skParry, true, 5);
            if (unarmeddefensechance > 1)
                victim.CheckImprove(unarmeddefense, true, 1);
            if (ironfistschance > 1)
                victim.CheckImprove(ironfists, true, 1);
            if (flourintinechance > 1)
                victim.CheckImprove(flourintine, true, 1);
            if ((victimWield != null && victimWield.WeaponType == WeaponTypes.Sword) || (victimDualWield != null && victimDualWield.WeaponType == WeaponTypes.Sword))
            {
                var skRiposte = SkillSpell.SkillLookup("riposte");
                var riposteChance = victim.GetSkillPercentage(skRiposte);

                if (riposteChance > Utility.NumberPercent())
                {
                    victim.Act("You riposte $N's {0}!", ch, null, null, ActType.ToChar, damageType);
                    ch.Act("$N ripostes your {0}!", victim, null, null, ActType.ToChar, damageType);
                    var offhand = !(victimWield != null && victimWield.WeaponType == WeaponTypes.Sword);
                    oneHit(victim, ch, !offhand ? victimWield : victimDualWield, offhand, skRiposte);
                }
            }
            CheckOwaza(victim, ch);

            return true;
        } // end check parry

        public static bool CheckShieldBlock(Character ch, Character victim, ItemData weapon, string damageType)
        {
            float chance;
            int str, stra;
            SkillSpell skShieldBlock;
            if (!victim.IsAwake)
                return false;

            if (victim.Form != null) return false;

            skShieldBlock = SkillSpell.SkillLookup("shield block");
            ItemData shield;
            if (skShieldBlock == null || !victim.Equipment.TryGetValue(WearSlotIDs.Shield, out shield)) return false;

            var skillDodge = !victim.IsNPC ? victim.GetSkillPercentage(skShieldBlock) : 80;
            chance = (3 * skillDodge / 10);

            str = victim.GetCurrentStat(PhysicalStatTypes.Strength);
            stra = ch.GetCurrentStat(PhysicalStatTypes.Strength);
            if (str <= 5)
                chance += 0;
            else if (str <= 10)
                chance += str / 2;
            else if (str <= 15)
                chance += (2 * str / 3);
            else if (str <= 20)
                chance += (8 * str / 10);
            else
                chance += str;
            chance += str - stra;
            chance += (ch.Size - victim.Size) * 5;


            if (!victim.CanSee(ch))
                chance *= Utility.Random(6f / 10f, 3f / 4f);

            if (Utility.NumberPercent() >= chance + victim.Level - ch.Level)
            {
                victim.CheckImprove(skShieldBlock, false, 4);
                return false;
            }

            ch.Act("You \\Cblock\\x $n's {0} with $p.", victim, shield, null, ActType.ToVictim, damageType);
            ch.Act("$N \\Cblocks\\x your {0} with $p.", victim, shield, null, ActType.ToChar, damageType);

            victim.CheckImprove(skShieldBlock, true, 5);

            CheckShieldJab(victim, ch);
            CheckOwaza(victim, ch);

            return true;
        } // end check shield block

        private static void CheckShieldJab(Character ch, Character victim)
        {
            int dam = 0;
            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("shield jab");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                return;
            }

            if (victim.FindAffect(SkillSpell.SkillLookup("protective shield")) != null)
            {
                ch.WaitState(game.PULSE_VIOLENCE);
                ch.Act("You try to shield jab $N but miss $M.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n tries to shield jab $N but miss $M.\n\r", victim, type: ActType.ToRoomNotVictim);
            }
            //else if (Utility.Random(0, 1) == 0) return; //50% chance for shield jab attempt
            else if (skillPercent > Utility.NumberPercent())
            {
                dam += Utility.dice(2, (ch.Level) / 2, (ch.Level) / 4);

                if (ch.Fighting == null)
                {
                    ch.Position = Positions.Fighting;
                    ch.Fighting = victim;
                }
                ch.Act("You take advantage of your block and jab $N with your shield.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n takes advantage of $s block and jabs $N with $s shield.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n jabs you with $s shield after blocking your attack..\n\r", victim, type: ActType.ToVictim);

                Combat.Damage(ch, victim, dam, skill);

                ch.CheckImprove(skill, true, 1);
            }

            else ch.CheckImprove(skill, false, 1);
        }

        public static bool CheckConcealed(Character ch, Character victim, string dodgeDamageNoun)
        {
            int dam = 0;
            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("concealed");
            ItemData wield;
            bool offhand = false;
            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                return false;
            }
            else if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Dagger) &&
                ((offhand = true) && (wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Dagger))
            { // must be wielding a dagger to concealed attack
                return false;
            }
            else if (skillPercent > Utility.NumberPercent())
            {
                dam += Utility.dice(2, (ch.Level) / 2, (ch.Level) / 4);

                if (ch.Fighting == null)
                {
                    ch.Position = Positions.Fighting;
                    ch.Fighting = victim;
                }
                ch.Act("You \\Cdodge\\x $N's {0} and close in for a concealed attack.\n\r", victim, type: ActType.ToChar, args: dodgeDamageNoun);
                ch.Act("$n \\Cdodges\\x $N's {0} and closes in for a concealed attack.\n\r", victim, type: ActType.ToRoomNotVictim, args: dodgeDamageNoun);
                ch.Act("$n \\Cdodges\\x your {0} and closes in for a concealed attack.\n\r", victim, type: ActType.ToVictim, args: dodgeDamageNoun);

                Combat.oneHit(ch, victim, wield, offhand);
                //Combat.Damage(ch, victim, dam, skill);

                ch.CheckImprove(skill, true, 1);
                return true;
            }

            else
            {
                ch.CheckImprove(skill, false, 1);
                return false;
            }
        }

        public static bool CheckCutoff(Character ch, Character victim)
        {
            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("cutoff");
            ItemData wield;

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                return false;
            }
            else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Polearm)
            { // must be wielding a polearm to cut off an opponents escape
                return false;
            }
            else if (skillPercent > Utility.NumberPercent())
            {

                if (ch.Fighting == null)
                {
                    ch.Position = Positions.Fighting;
                    ch.Fighting = victim;
                }
                ch.Act("You \\Ccuts off\\x $N's escape with $p.\n\r", victim, wield, type: ActType.ToChar);
                ch.Act("$n \\Ccuts off\\x $N's escape with $p.\n\r", victim, wield, type: ActType.ToRoomNotVictim);
                ch.Act("$n \\Ccuts off\\x your escape with $p.\n\r", victim, wield, type: ActType.ToVictim);

                ch.CheckImprove(skill, true, 1);
                return true;
            }

            else
            {
                ch.CheckImprove(skill, false, 1);
                return false;
            }
        }

        public static void CheckPartingBlow(Character ch, Character victim)
        {
            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("parting blow");
            ItemData wield;
            bool offhand = false;

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                return;
            }
            else if (skillPercent > Utility.NumberPercent())
            {

                if (ch.Fighting == null)
                {
                    ch.Position = Positions.Fighting;
                    ch.Fighting = victim;
                }
                ch.Act("You get a parting blow as $N attempts to escape.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n gets a parting blow as $N attempts to escape.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n gets a parting blow as you attempt to escape.\n\r", victim, type: ActType.ToVictim);

                wield = ch.GetEquipment(WearSlotIDs.Wield);
                // only hit with main hand
                Combat.oneHit(ch, victim, wield, offhand, skill);

                ch.CheckImprove(skill, true, 1);
                return;
            }

            else
            {
                ch.CheckImprove(skill, false, 1);
                return;
            }
        }

        public static bool CheckDistance(Character ch, Character victim, ItemData weapon, string damageType)
        {
            float chance = 0;
            SkillSpell skDistance;
            SkillSpell skWeapon = null;
            ItemData victimWield;
            SkillSpell skVictimWield = null;


            if (!victim.IsAwake)
                return false;

            skDistance = SkillSpell.SkillLookup("distance");

            victim.Equipment.TryGetValue(WearSlotIDs.Wield, out victimWield);

            if (victimWield == null || victimWield.WeaponType != WeaponTypes.Polearm)
                return false;

            if (weapon != null) // weapon ch is using to hit victim, skill lowers chance of parry
            {
                skWeapon = SkillSpell.SkillLookup(weapon.WeaponType.ToString());
                chance -= ch.GetSkillPercentage(skWeapon) / 10;

                switch (weapon.WeaponType)
                {
                    default: chance += 15; break;
                    case WeaponTypes.Sword: chance += 5; break;
                    case WeaponTypes.Dagger: chance += 5; break;
                    case WeaponTypes.Spear: chance -= 5; break;
                    case WeaponTypes.Staff: chance -= 5; break;
                    case WeaponTypes.Mace: chance -= 5; break;
                    case WeaponTypes.Axe: chance -= 5; break;
                    case WeaponTypes.Flail: chance += 10; break;
                    case WeaponTypes.Whip: chance += 10; break;
                    case WeaponTypes.Polearm: chance -= 5; break;
                }
            }

            if (victimWield != null)
            {
                skVictimWield = SkillSpell.SkillLookup(victimWield.WeaponType.ToString());
                var skillChance = victim.GetSkillPercentage(skVictimWield);
                chance += skillChance / 10;

                switch (victimWield.WeaponType)
                {
                    default: chance += 15; break;
                    case WeaponTypes.Sword: chance += 10; break;
                    case WeaponTypes.Dagger: chance -= 20; break;
                    case WeaponTypes.Spear: chance += 20; break;
                    case WeaponTypes.Staff: chance += 10; break;
                    case WeaponTypes.Mace: chance -= 20; break;
                    case WeaponTypes.Axe: chance -= 25; break;
                    case WeaponTypes.Flail: chance -= 10; break;
                    case WeaponTypes.Whip: chance -= 10; break;
                    case WeaponTypes.Polearm: chance += 10; break;
                }
            }

            if (skDistance == null || (!victim.IsNPC && victim.GetSkillPercentage(skDistance) <= 1)) return false;

            //if (!victim.isNPC)
            chance += victim.GetSkillPercentage(skDistance) / 3;
            chance += victim.GetCurrentStat(PhysicalStatTypes.Strength) / 2;
            chance -= ch.GetCurrentStat(PhysicalStatTypes.Strength) / 4;

            if (victim.Size > ch.Size) chance += 20;
            if (ch.Size > victim.Size) chance -= 10;

            if (Utility.NumberPercent() >= chance + victim.Level - ch.Level)
            {
                victim.CheckImprove(skDistance, false, 4);
                return false;
            }


            ch.Act("You keep $n's {0} at a distance.", victim, null, null, ActType.ToVictim, damageType);
            ch.Act("$N keeps you at a distance.", victim, null, null, ActType.ToChar, damageType);

            victim.CheckImprove(skDistance, true, 5);

            return true;
        } // end check distance

        public static bool CheckDefensiveSpin(Character ch, Character victim, ItemData weapon, string damageType)
        {
            float chance = 0;
            SkillSpell skSpin;
            SkillSpell skWeapon = null;
            ItemData victimWield;
            SkillSpell skVictimWield = null;


            if (!victim.IsAwake)
                return false;


            skSpin = SkillSpell.SkillLookup("defensive spin");


            victim.Equipment.TryGetValue(WearSlotIDs.Wield, out victimWield);

            if (victimWield == null || (victimWield.WeaponType != WeaponTypes.Staff && victimWield.WeaponType != WeaponTypes.Spear))
                return false;

            if (weapon != null) // weapon ch is using to hit victim, skill lowers chance of parry
            {
                skWeapon = SkillSpell.SkillLookup(weapon.WeaponType.ToString());
                chance -= ch.GetSkillPercentage(skWeapon) / 10;

                switch (weapon.WeaponType)
                {
                    default: chance += 15; break;
                    case WeaponTypes.Sword: chance += 5; break;
                    case WeaponTypes.Dagger: chance += 5; break;
                    case WeaponTypes.Spear: chance -= 5; break;
                    case WeaponTypes.Staff: chance -= 5; break;
                    case WeaponTypes.Mace: chance -= 5; break;
                    case WeaponTypes.Axe: chance -= 5; break;
                    case WeaponTypes.Flail: chance += 10; break;
                    case WeaponTypes.Whip: chance += 10; break;
                    case WeaponTypes.Polearm: chance -= 5; break;
                }
            }

            if (victimWield != null)
            {
                skVictimWield = SkillSpell.SkillLookup(victimWield.WeaponType.ToString());
                var skillChance = victim.GetSkillPercentage(skVictimWield);
                chance += skillChance / 10;

                switch (victimWield.WeaponType)
                {
                    default: chance += 15; break;
                    case WeaponTypes.Sword: chance += 10; break;
                    case WeaponTypes.Dagger: chance -= 20; break;
                    case WeaponTypes.Spear: chance += 20; break;
                    case WeaponTypes.Staff: chance += 10; break;
                    case WeaponTypes.Mace: chance -= 20; break;
                    case WeaponTypes.Axe: chance -= 25; break;
                    case WeaponTypes.Flail: chance -= 10; break;
                    case WeaponTypes.Whip: chance -= 10; break;
                    case WeaponTypes.Polearm: chance += 10; break;
                }
            }

            if (skSpin == null || (!victim.IsNPC && victim.GetSkillPercentage(skSpin) <= 1)) return false;

            //if (!victim.isNPC)
            chance += victim.GetSkillPercentage(skSpin) / 3;
            chance += victim.GetCurrentStat(PhysicalStatTypes.Dexterity) / 2;
            chance -= ch.GetCurrentStat(PhysicalStatTypes.Dexterity) / 4;

            if (Utility.NumberPercent() >= chance + victim.Level - ch.Level)
            {
                victim.CheckImprove(skSpin, false, 4);
                return false;
            }


            ch.Act("You keep $n's {0} at bay as you spin away.", victim, null, null, ActType.ToVictim, damageType);
            ch.Act("$N keeps your {0} at bay as they spin away.", victim, null, null, ActType.ToChar, damageType);

            victim.CheckImprove(skSpin, true, 5);

            return true;
        } // end check spin

        public static void CheckAssist(Character ch, Character victim)
        {

            foreach (var rch in ch.Room.Characters.ToArray())
            {
                //if (!rch.isNPC && rch.ghost > 0)
                //    continue;

                if (rch.IsAwake && rch.Fighting == null)
                {

                    /* NPC assisting group (for charm, zombies, elementals..added by Ceran */
                    if (rch.IsNPC && rch.IsSameGroup(ch) && !victim.IsAffected(AffectFlags.Ghost))
                    {
                        multiHit(rch, victim);
                        continue;
                    }


                    /* quick check for ASSIST_PLAYER */
                    if (!ch.IsNPC && rch.IsNPC
                        && !victim.IsAffected(AffectFlags.Ghost)
                        && rch.Flags.ISSET(ActFlags.AssistPlayer)
                        && (rch.Alignment != Alignment.Good || victim.Alignment == Alignment.Evil)
                        && rch.Level + 6 > victim.Level && rch.Position != Positions.Sleeping)
                    {
                        rch.Act("$n screams and attacks!", victim, type: ActType.ToRoom);
                        multiHit(rch, victim);
                        continue;
                    }

                    /* PCs next */
                    if (!rch.IsNPC || rch.IsAffected(AffectFlags.Charm))
                    {
                        ///* First check defending */
                        //if (rch.defending != null)
                        //{
                        //    Character fch;

                        //    fch = ch.fighting;
                        //    if (rch.defending == ch
                        //        && fch != null
                        //        && fch.fighting == ch
                        //        && utility.number_percent() < get_skill(rch, gsn_defend)
                        //        && utility.number_percent() > 40
                        //        && utility.number_percent() < get_skill(rch, gsn_rescue))
                        //    {
                        //        rch.Act("$n leaps to $N's rescue!", ch, type: ActType.ToRoomNotVictim);
                        //        rch.Act("$n leaps to your rescue!", ch, type: ActType.ToVictim);
                        //        rch.Act("You leap to $N's rescue!", ch, type: ActType.ToChar);

                        //        if (CheckIsSafe(rch, fch))
                        //            return;

                        //        rch.WaitState(skill_table[gsn_rescue].beats);
                        //        ch.WaitState(12);
                        //        rch.CheckImprove(gsn_defend, true, 1);

                        //        StopFighting(fch, false);
                        //        StopFighting(ch, false);

                        //        SetFighting(rch, fch);
                        //        SetFightingg(fch, rch);
                        //        //if (fch.isNPC
                        //        //    && (fch.vnum == MOB_VNUM_COVEN
                        //        //    || fch->pIndexData->vnum == MOB_VNUM_ACADIAN
                        //        //    || fch->pIndexData->vnum == MOB_VNUM_PROTECTOR
                        //        //    || fch->pIndexData->vnum == MOB_VNUM_RAVAGER
                        //        //    || fch->pIndexData->vnum == MOB_VNUM_BRIAR
                        //        //    || fch->pIndexData->vnum == MOB_VNUM_PROTECTOR))
                        //        //    set_fighting(ch, fch);

                        //        continue;
                        //    }
                        //}


                        if ((rch.Flags.Contains(ActFlags.AutoAssist)
                            || rch.IsAffected(AffectFlags.Charm))
                            && ch.IsSameGroup(rch)
                            && !victim.IsAffected(AffectFlags.Ghost)
                            && !CheckIsSafe(rch, victim))
                            multiHit(rch, victim);

                        continue;
                    }

                    /* now check the NPC cases */

                    else if (rch.IsNPC) // && ch.IsNPC && !ch.AffectedBy.Contains(AffectFlags.Charm))
                    {
                        if ((rch.Flags.Contains(ActFlags.AssistAll))

                            || (rch.IsSameGroup(ch))
                            || (rch.Race == ch.Race && rch.Flags.Contains(ActFlags.AssistRace) && (rch.Alignment != Alignment.Good || victim.Alignment == Alignment.Evil))
                            || (rch.IsNPC && rch.Flags.Contains(ActFlags.AssistAlign) && rch.Alignment == ch.Alignment)
                            || (ch.IsNPC && ((NPCData)rch).vnum == ((NPCData)ch).vnum && rch.Flags.Contains(ActFlags.AssistVnum)))

                        {
                            Character target = null;

                            if (Utility.Random(0, 8) == 0)
                                continue;

                            target = (from other in ch.Room.Characters where rch.CanSee(other) && other.IsSameGroup(victim) select other).SelectRandom();

                            if (target != null)
                            {
                                rch.Act("$n screams and attacks!", type: ActType.ToRoom);
                                multiHit(rch, target);
                            }
                        }
                    }
                }
            }
        } // end CheckAssist

        public static void StopFighting(Character ch, bool world = false)
        {
            if (world)
            {
                foreach (var other in Character.Characters)
                {
                    if (other.Fighting == ch)
                    {
                        other.Fighting = null;
                        if (other.Position == Positions.Fighting)
                            other.Position = Positions.Standing;
                    }
                }
            }
            //if(ch.fighting != null && ch.fighting.fighting == ch)
            //{
            //    ch.fighting.fighting = null;
            //    if(ch.fighting.position == Positions.Fighting)
            //        ch.fighting.position = Positions.Standing;
            //}

            ch.Fighting = null;
            if (ch.Position == Positions.Fighting) ch.Position = Positions.Standing;

        } // end StopFighting

        public static void SetFighting(Character ch, Character victim)
        {

            if (ch.Fighting != null)
            {
                //game.bug("Set_fighting: already fighting");
                return;
            }

            //if (IS_AFFECTED(ch, AFF_SLEEP))
            //    affect_strip(ch, gsn_sleep);

            //if (is_affected(victim, gsn_trip_wire))
            //{
            //    send_to_char("You lost your trip wire in the frey.\n\r", victim);
            //    affect_strip(victim, gsn_trip_wire);
            //}
            //if (is_affected(ch, gsn_trip_wire))
            //{
            //    send_to_char("You lost your trip wire in the frey.\n\r", ch);
            //    affect_strip(ch, gsn_trip_wire);
            //}

            if (ch == null || victim == null || ch.Room != victim.Room)
                return;

            ch.Fighting = victim;
            ch.Position = Positions.Fighting;

            if (victim.Fighting == null)
            {
                victim.Fighting = ch;
                victim.Position = Positions.Fighting;
            }

            if (victim.Following == ch)
            {
                victim.StopFollowing();
            }
            //if (is_centurion(victim))
            //{
            //    sprintf(buf, "empire Help! %s is attacking me!", PERS(ch, victim));
            //    do_ccb(victim, buf);
            //}

            return;
        } // End SetFighting

        public static SkillSpell GetWeaponSkill(ItemData weapon)
        {
            if (weapon != null && weapon.ItemType.Contains(ItemTypes.Weapon))
            {
                return SkillSpell.SkillLookup(weapon.WeaponType.ToString());
            }
            else
                return SkillSpell.SkillLookup("hand to hand");
        }

        static void Disarm(Character ch, Character victim)
        {
            ItemData obj;

            if ((obj = victim.GetEquipment(WearSlotIDs.Wield)) == null && (obj = victim.GetEquipment(WearSlotIDs.DualWield)) == null)
            {
                ch.send("They don't seem to be wielding a weapon.\n\r");
                return;
            }
            if (ch.IsAffected(AffectFlags.Blind))

            {
                ch.send("You can't see the person to disarm them!\n\r");
                return;
            }
            if (obj.extraFlags.ISSET(ExtraFlags.NoRemove) || obj.extraFlags.ISSET(ExtraFlags.NoDisarm) || victim.IsAffected(SkillSpell.SkillLookup("spiderhands")))
            {
                ch.Act("$S weapon won't budge!", victim, type: ActType.ToChar);
                ch.Act("$n tries to disarm you, but your weapon won't budge!", victim, type: ActType.ToVictim);
                ch.Act("$n tries to disarm $N, but fails.", victim, type: ActType.ToRoomNotVictim);
                return;
            }

            ch.Act("$n DISARMS you and sends your weapon flying!", victim, type: ActType.ToVictim);
            ch.Act("You disarm $N!", victim, type: ActType.ToChar);
            ch.Act("$n disarms $N!", victim, type: ActType.ToRoomNotVictim);

            //victim.Equipment[victim.GetEquipmentWearSlot(obj).id] = null;
            victim.RemoveEquipment(obj, false, false);
            if (victim.Inventory.Contains(obj))
                victim.Inventory.Remove(obj);
            if (obj.extraFlags.ISSET(ExtraFlags.NoDrop) || obj.extraFlags.ISSET(ExtraFlags.Inventory))
            {
                victim.AddInventoryItem(obj);

            }
            else
            {
                victim.Room.items.Insert(0, obj);
                obj.Room = victim.Room;
                obj.CarriedBy = null;
                if (victim.IsNPC && victim.Wait == 0 && victim.CanSee(obj))
                {
                    if (victim.GetItem(obj, null))
                        victim.wearItem(obj);

                }
            }
            return;
        }

        public static void DoDisarm(Character ch, string argument)
        {
            Character victim = null;
            ItemData obj = null;
            ItemData wield = null;

            int chance, hth, ch_weapon, vict_weapon, ch_vict_weapon;
            SkillSpell skill = SkillSpell.SkillLookup("disarm");

            hth = 0;

            if (skill == null || (chance = ch.GetSkillPercentage("disarm")) <= 1)
            {
                ch.send("You don't know how to disarm opponents.\n\r");
                return;
            }

            if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null
                && ((hth = ch.GetSkillPercentage("hand to hand")) == 0)) || ch.IsNPC)
            {
                ch.send("You must wield a weapon to disarm.\n\r");
                return;
            }

            if (ch.IsAffected(AffectFlags.Blind))
            {
                ch.Act("You can't see the person to disarm them!", type: ActType.ToChar);
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            if ((obj = victim.GetEquipment(WearSlotIDs.Wield)) == null && (obj = victim.GetEquipment(WearSlotIDs.DualWield)) == null)
            {
                ch.send("Your opponent is not wielding a weapon.\n\r");
                return;
            }

            /* find weapon skills */
            ch_weapon = ch.GetSkillPercentage(GetWeaponSkill(wield));
            vict_weapon = victim.GetSkillPercentage(GetWeaponSkill(obj));
            ch_vict_weapon = ch.GetSkillPercentage(GetWeaponSkill(obj));

            /* skill */
            if (wield == null)
                chance = (chance + hth) / 2;
            else
                chance = (chance + ch_weapon) / 2;

            //chance += (ch_vict_weapon / 2 - vict_weapon) / 2;

            /* dex vs. strength */
            //chance += ch.GetCurrentStat(PhysicalStatTypes.Dexterity);
            //chance -= 2 * ch.GetCurrentStat(PhysicalStatTypes.Strength);

            /* level */
            chance += (ch.Level - victim.Level);

            /* and now the attack */
            if (Utility.NumberPercent() < chance)
            {
                ch.WaitState(skill.waitTime);
                Disarm(ch, victim);
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("You fail to disarm $N.", victim, type: ActType.ToChar);
                ch.Act("$n tries to disarm you, but fails.", victim, type: ActType.ToVictim);
                ch.Act("$n tries to disarm $N, but fails.", victim, type: ActType.ToRoomNotVictim);
                ch.CheckImprove(skill, false, 1);
            }

            return;
        }


        public static void DoStab(Character ch, string argument)
        {
            Character victim = null;
            ItemData obj;
            int chance = 0;
            int dam;

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

            SkillSpell skill = SkillSpell.SkillLookup("stab");
            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
            }

            else if (argument.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("But you aren't fighting anyone.\n\r");
            }
            else if (!argument.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(argument)) == null)
            {
                ch.send("They aren't here.\n\r");
            }
            else if (victim == ch)
            {
                ch.send("You don't want to stab yourself.\n\r");
            }
            else if (((obj = ch.GetEquipment(WearSlotIDs.Wield)) == null || obj.WeaponType != WeaponTypes.Dagger)
                && ((obj = ch.GetEquipment(WearSlotIDs.DualWield)) == null || obj.WeaponType != WeaponTypes.Dagger))
            {
                ch.send("You must wield a dagger to stab someone.\n\r");
                return;
            }
            else if (chance > Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);
                ch.Act("You unleash with a viscious stab on $N.", victim, type: ActType.ToChar);
                ch.Act("$n unleashes with a viscious stab.", victim, type: ActType.ToVictim);
                ch.Act("$n unleashes with a viscious stab on $N.", victim, type: ActType.ToRoomNotVictim);
                ch.CheckImprove(skill, true, 1);


                var level = ch.Level;
                if (ch.IsNPC) level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level] / 2, dam_each[level] * 2);
                Damage(ch, victim, dam, skill, obj.WeaponDamageType.Type);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("You try to unleash a viscious stab on $N but fail.", victim, type: ActType.ToChar);
                ch.Act("$n tries to unleash a viscious stab but fails.", victim, type: ActType.ToVictim);
                ch.Act("$n tries to unleash a viscious stab on $N but fails.", victim, type: ActType.ToRoomNotVictim);
                ch.CheckImprove(skill, false, 1);
                Damage(ch, victim, 0, skill, obj.WeaponDamageType.Type);
            }
        } // end stab

        public static void DoKnife(Character ch, string argument)
        {
            Character victim;
            ItemData obj;
            int chance;
            int dam;

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

            var skill = SkillSpell.SkillLookup("knife");

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to knife.\n\r");
            }
            else if (argument.ISEMPTY())
            {
                ch.send("Knife whom?\n\r");
            }
            else if (ch.Fighting != null)
            {
                ch.send("No way! You're still fighting!\n\r");
            }

            else if ((victim = ch.GetCharacterFromRoomByName(argument)) == null)
            {
                ch.send("They aren't here.\n\r");
            }

            else if (victim == ch)
            {
                ch.send("Bah, you can't knife yourself.\n\r");
            }
            else if (((obj = ch.GetEquipment(WearSlotIDs.Wield)) == null || obj.WeaponType != WeaponTypes.Dagger)
                            && ((obj = ch.GetEquipment(WearSlotIDs.DualWield)) == null || obj.WeaponType != WeaponTypes.Dagger))
            {
                ch.send("You must wield a dagger to knife someone.\n\r");
            }
            else if (chance > Utility.NumberPercent())
            {
                var level = ch.Level;
                var roll = obj.DamageDice.Roll() + ch.DamageRoll;

                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level] + roll, dam_each[level] * 3 / 2 + roll);

                ch.Act("You step forward quickly and deliver a powerful knife attack at $N.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n step forward quickly and delivers a powerful knife attack at $N..\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n step forward quickly and delivers a powerful knife attack at you .\n\r", victim, type: ActType.ToVictim);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, obj.WeaponDamageType.Type);
            }
            else
            {
                ch.Act("You step forward quickly but fail to deliver a powerful knife attack at $N.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n steps forward quickly but fails to deliver a powerful knife attack at $N.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n steps forward quickly but fails to deliver a powerful knife attack at you.\n\r", victim, type: ActType.ToVictim);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, obj.WeaponDamageType.Type);
            }
            return;
        }


        public static void DoBackstab(Character ch, string argument)
        {
            var dam_each = new int[]
           {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
           };
            var skill = SkillSpell.SkillLookup("backstab");
            int chance;
            int dam;
            var level = ch.Level;
            string arg = "";
            Character victim;
            ItemData obj;

            argument.OneArgument(ref arg);

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to backstab.\n\r");
            }

            else if (arg.ISEMPTY())
            {
                ch.send("Backstab whom?\n\r");
            }

            else if (ch.Fighting != null)
            {
                ch.send("You're facing the wrong end.\n\r");
            }

            else if ((victim = ch.GetCharacterFromRoomByName(arg)) == null)
            {
                ch.send("They aren't here.\n\r");
            }
            else if (victim == ch)
            {
                ch.send("How can you sneak up on yourself?\n\r");
            }

            else if (((obj = ch.GetEquipment(WearSlotIDs.Wield)) == null || obj.WeaponType != WeaponTypes.Dagger)
                 && ((obj = ch.GetEquipment(WearSlotIDs.DualWield)) == null || obj.WeaponType != WeaponTypes.Dagger))
            {
                ch.send("You must wield a dagger to backstab someone.\n\r");
            }

            else if (CheckIsSafe(ch, victim))
            {

            }
            else if (victim.Fighting != null)
            {
                ch.send("That person is moving around too much to backstab.\n\r");
            }
            else if (victim.HitPoints < victim.MaxHitPoints * 5 / 10)
            {
                ch.Act("$N is hurt and suspicious ... you can't sneak up.", victim, type: ActType.ToChar);
            }
            else if (victim.CanSee(ch) && victim.IsAwake)
            {
                ch.Act("You can't backstab someone who can see you.");
            }
            else if (CheckPreventSurpriseAttacks(ch, victim)) return;

            else if (chance > Utility.NumberPercent() || !victim.IsAwake)
            {
                ch.WaitState(skill.waitTime);

                var roll = obj.DamageDice.Roll() + ch.DamageRoll;

                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level] + roll, dam_each[level] * 2 + roll);

                ch.Act("You backstab $N with your dagger.\n\r", victim);
                ch.Act("$n backstabs $N with $s dagger.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n backstabs you with $s dagger.\n\r", victim, type: ActType.ToVictim);

                Damage(ch, victim, dam, skill.NounDamage, obj.WeaponDamageType.Type);
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("You try backstab $N with your dagger but fail.\n\r", victim);
                ch.Act("$n tries to backstab $N with $s dagger but fails.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to backstab you with $s dagger but fails.\n\r", victim, type: ActType.ToVictim);
                ch.CheckImprove(skill, false, 1);
                Damage(ch, victim, 0, skill, WeaponDamageTypes.None);
            }
        } // end backstab
        public static void DoDualBackstab(Character ch, string argument)
        {
            var dam_each = new int[]
           {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
           };

            var skill = SkillSpell.SkillLookup("dual backstab");
            int chance;
            int dam;
            var level = ch.Level;
            Character victim;
            ItemData weapon;
            ItemData offhand;
            weapon = ch.GetEquipment(WearSlotIDs.Wield);
            offhand = ch.GetEquipment(WearSlotIDs.DualWield);


            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to backstab.\n\r");
            }
            else if (argument.ISEMPTY())
            {
                ch.send("Backstab whom?\n\r");
            }
            else if (ch.Fighting != null)
            {
                ch.send("You're facing the wrong end.\n\r");
            }
            else if ((victim = ch.GetCharacterFromRoomByName(argument)) == null)
            {
                ch.send("They aren't here.\n\r");
            }
            else if (victim.CanSee(ch) && victim.IsAwake)
            {
                ch.Act("You can't backstab someone who can see you.");
            }
            else if (victim == ch)
            {
                ch.send("How can you backstab yourself?\n\r");
            }
            else if (weapon == null || weapon.WeaponType != WeaponTypes.Dagger || offhand == null || offhand.WeaponType != WeaponTypes.Dagger)
            {
                ch.send("You must be dual wielding daggers to dual backstab someone.\n\r");
            }
            else if (CheckIsSafe(ch, victim))
            {

            }
            else if (victim.Fighting != null)
            {
                ch.send("That person is moving around too much to dual backstab.\n\r");
            }
            else if (victim.HitPoints < victim.MaxHitPoints * 5 / 10)
            {
                ch.Act("$N is hurt and suspicious ... you can't sneak up.", victim, type: ActType.ToChar);
            }
            else if (CheckPreventSurpriseAttacks(ch, victim)) return;

            else if (Utility.NumberPercent() < chance || !victim.IsAwake)
            {
                ch.WaitState(skill.waitTime);
                ch.Act("You backstab $N with your daggers.\n\r", victim);
                ch.Act("$n backstabs $N with $s daggers.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n backstabs you with $s daggers.\n\r", victim, type: ActType.ToVictim);

                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                var roll = weapon.DamageDice.Roll();
                dam = Utility.Random(dam_each[level] + roll, dam_each[level] * 2 + roll);
                Damage(ch, victim, dam, skill.NounDamage, weapon.WeaponDamageType.Type);

                roll = offhand.DamageDice.Roll();
                dam = Utility.Random(dam_each[level] + roll, dam_each[level] * 2 + roll);
                Damage(ch, victim, dam, skill.NounDamage, offhand.WeaponDamageType.Type);
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("You try double backstab $N with your daggers but fail.\n\r", victim);
                ch.Act("$n tries to double backstab $N with $s daggers but fails.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to double backstab you with $s daggers but fails.\n\r", victim, type: ActType.ToVictim);
                ch.CheckImprove(skill, false, 1);
                Damage(ch, victim, 0, skill, WeaponDamageTypes.None);
            }
        } // end dual backstab

        public static void CheckCheapShot(Character ch, Character victim)
        {
            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };
            var skill = SkillSpell.SkillLookup("cheap shot");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                return;
            }

            int dam;
            var level = ch.Level;

            if (ch.Room != victim.Room || victim.Position != Positions.Fighting)
                return;
            //chance += level / 10;

            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                {
                    level = Math.Min(level, 51);
                }
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                ch.Act("Seizing upon $N's moment of weakness, you brutally kick him while $E's down!", victim, type: ActType.ToChar);
                ch.Act("Seizing upon your moment of weakness, $n brutally kicks you while you're down!", victim, type: ActType.ToVictim);
                ch.Act("Seizing upon $N's moment of weakness, $n brutally kicks him while $E's down!", victim, type: ActType.ToRoomNotVictim);

                if (Utility.NumberPercent() < 26)
                {
                    ch.Act("$N grunts in pain as you land a particularly vicious kick!", victim, type: ActType.ToChar);
                    ch.Act("You grunt in pain as $n lands a particularly vicious kick!", victim, type: ActType.ToVictim);
                    ch.Act("$N grunts in pain as $n lands a particularly vicious kick!", victim, type: ActType.ToRoomNotVictim);
                    dam = dam * 2;
                    victim.WaitState(game.PULSE_VIOLENCE);
                }
                victim.WaitState(game.PULSE_VIOLENCE);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
            }
            else
            {
                ch.send("You were unable to get a cheap shot in.\n\r");
                ch.CheckImprove(skill, false, 1);
                return;
            }
        }

        public static void DoBindWounds(Character ch, string argument)
        {
            AffectData af;
            var skill = SkillSpell.SkillLookup("bind wounds");

            if (ch.GetSkillPercentage(skill) <= 1)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if (ch.IsAffected(skill))
            {
                ch.send("You can't apply more aid yet.\n\r");
                return;
            }
            if (ch.ManaPoints < 15)
            {
                ch.send("You don't have the mana.\n\r");
                return;
            }

            if (Utility.NumberPercent() > ch.GetSkillPercentage(skill))
            {
                ch.send("You fail to focus on your injuries.\n\r");
                ch.Act("$n fails to focus on $s injuries and bind $s wounds.", type: ActType.ToRoom);
                ch.ManaPoints -= 12;
                ch.CheckImprove(skill, false, 3);
                return;
            }

            ch.ManaPoints -= 25;

            ch.Act("$n focuses on $s injuries and binds $s wounds.", type: ActType.ToRoom);
            ch.send("You focus on your injuries and bind your wounds.\n\r");
            ch.send("You feel better.\n\r");

            ch.HitPoints += (int)(ch.MaxHitPoints * 0.2);
            ch.HitPoints = Math.Min(ch.HitPoints, ch.MaxHitPoints);

            if (Utility.NumberPercent() < Math.Max(1, ch.Level / 4) && ch.IsAffected(AffectFlags.Plague))
            {
                ch.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("plague")));
                ch.Act("The sores on $n's body vanish.\n\r", type: ActType.ToRoom);
                ch.send("The sores on your body vanish.\n\r");
            }

            if (Utility.NumberPercent() < Math.Max(1, (ch.Level)) && ch.IsAffected(AffectFlags.Blind))
            {
                ch.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("blindness")));
                ch.send("Your vision returns!\n\r");
            }

            if (Utility.NumberPercent() < Math.Max(1, ch.Level / 2) && ch.IsAffected(AffectFlags.Poison))
            {
                ch.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("poison")));
                ch.send("A warm feeling goes through your body.\n\r");
                ch.Act("$n looks better.", type: ActType.ToRoom);
            }
            ch.CheckImprove(skill, true, 3);

            af = new AffectData();

            af.where = AffectWhere.ToAffects;
            af.skillSpell = skill;
            af.location = 0;
            af.duration = 2;
            af.modifier = 0;
            af.level = ch.Level;
            af.affectType = AffectTypes.Skill;
            af.displayName = "bind wounds";
            af.endMessage = "You feel ready to bind your wounds once more.";
            ch.AffectToChar(af);
            ch.WaitState(skill.waitTime);
            return;
        }

        public static void DoCircleStab(Character ch, string argument)
        {
            string arg = "";
            Character victim;
            ItemData obj;
            int chance;
            int dam;

            var skill = SkillSpell.SkillLookup("circle stab");

            argument.OneArgument(ref arg);

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Circling? What's that?\n\r");
                return;
            }
            if (arg.ISEMPTY())
            {
                victim = ch.Fighting;
                if (victim == null)
                {
                    ch.send("But you aren't fighting anyone.\n\r");
                    return;
                }
            }
            else if ((victim = ch.GetCharacterFromRoomByName(arg)) == null)
            {
                ch.send("They aren't here.\n\r");
                return;
            }
            if (ch.Fighting == null)
            {
                ch.send("You can't circle someone like that.\n\r");
                return;
            }
            foreach (var other in ch.Room.Characters)
            {
                if (other.Fighting == ch)
                {
                    ch.send("Not while you're defending yourself!\n\r");
                    return;
                }
            }
            if (victim == ch)
            {
                ch.send("Huh?\n\r");
                return;
            }
            obj = ch.GetEquipment(WearSlotIDs.Wield);
            if (obj == null || obj.WeaponType != WeaponTypes.Dagger)
            {
                obj = ch.GetEquipment(WearSlotIDs.DualWield);
            }
            if (obj == null || obj.WeaponType != WeaponTypes.Dagger)
            {
                ch.send("You must wield a dagger to circle stab someone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (Utility.NumberPercent() < chance)
            {
                ch.Act("You circle around $N to land a critical strike.", victim, type: ActType.ToChar);
                ch.Act("$n cirlces around you to land a critical strike.", victim, type: ActType.ToVictim);
                ch.Act("$n circles $N to land a critical strike.", victim, type: ActType.ToRoomNotVictim);
                ch.CheckImprove(skill, true, 1);
                dam = obj.DamageDice.Roll();
                dam += 20;

                if (ch.Level <= 15)
                    dam *= 1;
                else if (ch.Level <= 20)
                    dam *= 3 / 2;
                else if (ch.Level < 25)
                    dam *= 2;
                else if (ch.Level < 30)
                    dam *= 7 / 3;
                else if (ch.Level < 40)
                    dam *= 5 / 2;
                else if (ch.Level <= 49)
                    dam *= 7 / 2;
                else if (ch.Level <= 55)
                    dam *= 10 / 3;
                else dam *= 10 / 3;

                //Damage(ch, victim, dam, skill, obj.WeaponDamageType.Type);
                if ((chance = ch.GetSkillPercentage("precision strike") + 20) > 21 && chance > Utility.NumberPercent())
                {
                    switch (Utility.Random(1, 4))
                    {
                        case 1:
                            dam *= 3 / 2;
                            ch.Act("You strike with precision and do extra damage!\n\r");
                            break;
                        case 2:
                            var affect = new AffectData();
                            affect.hidden = true;
                            affect.duration = 2;
                            affect.frequency = Frequency.Violence;
                            affect.flags.SETBIT(AffectFlags.Distracted);
                            victim.AffectToChar(affect);
                            ch.Act("$N is distracted by your precise circle stab\n\r", victim);
                            ch.Act("You are distracted by $n's precise circle stab\n\r", victim, type: ActType.ToVictim);
                            ch.Act("$N is distracteb by $n's precise circle stab\n\r", victim, type: ActType.ToRoomNotVictim);
                            break;
                        case 3:
                            var bleeding = SkillSpell.SkillLookup("bleeding");

                            if (victim.FindAffect(bleeding) == null)
                            {
                                var aff = new AffectData();
                                aff.skillSpell = bleeding;
                                aff.duration = Utility.Random(2, 4);
                                aff.endMessage = "Your bleeding stops.";
                                aff.endMessageToRoom = "$n stops bleeding.";
                                aff.ownerName = ch.Name;
                                aff.level = ch.Level;
                                aff.affectType = AffectTypes.Skill;
                                aff.displayName = "bleeding";
                                aff.where = AffectWhere.ToAffects;
                                victim.AffectToChar(aff);
                                ch.Act("$N receives a deep wound from your precise circle stab\n\r", victim);
                                ch.Act("You are deeply wounded by $n's precise circle stab\n\r", victim, type: ActType.ToVictim);
                                ch.Act("$N is deeply wounded by $n's precise circle stab\n\r", victim, type: ActType.ToRoomNotVictim);
                            }

                            break;
                        case 4:
                            var precisionstrike = SkillSpell.SkillLookup("precision strike");

                            if (victim.FindAffect(precisionstrike) == null)
                            {
                                var aff = new AffectData();
                                aff.skillSpell = precisionstrike;
                                aff.duration = Utility.Random(2, 4);
                                aff.endMessage = "Your strength recovers.";
                                aff.ownerName = ch.Name;
                                aff.level = ch.Level;
                                aff.affectType = AffectTypes.Skill;
                                aff.displayName = "precision strike";
                                aff.modifier = -5;
                                aff.location = ApplyTypes.Strength;
                                aff.where = AffectWhere.ToAffects;
                                victim.AffectToChar(aff);
                                ch.Act("$N is weakened by your precise circle stab\n\r", victim);
                                ch.Act("You are weakened by $n's precise circle stab\n\r", victim, type: ActType.ToVictim);
                                ch.Act("$N is weakened by $n's precise circle stab\n\r", victim, type: ActType.ToRoomNotVictim);
                            }
                            break;

                    }// end switch random number 1-4

                    ch.CheckImprove("precision strike", true, 1);
                }
                else if (chance > 21)
                {
                    ch.CheckImprove("precision strike", false, 1);
                }

                Damage(ch, victim, dam, skill, obj.WeaponDamageType.Type);
            }
            else
            {
                ch.CheckImprove(skill, false, 1);

                Damage(ch, victim, 0, skill, WeaponDamageTypes.None);
            }
            return;
        }

        public static void DoFlurry(Character ch, string argument)
        {
            Character victim;
            int chance = 0, numhits = 0, i = 0, dam = 0;
            ItemData weapon;
            ItemData weapon2;

            var skill = SkillSpell.SkillLookup("flurry");

            if (ch.GetSkillPercentage(skill) <= 1)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            weapon = ch.GetEquipment(WearSlotIDs.Wield);
            if (weapon == null)
            {
                ch.send("You first need to find a weapon to flurry with.\n\r");
                return;
            }

            weapon2 = ch.GetEquipment(WearSlotIDs.DualWield);
            if (weapon2 == null)
            {
                ch.send("You need to find another weapon to flurry with.\n\r");
                return;
            }

            if ((weapon.WeaponType != WeaponTypes.Sword) || (weapon2.WeaponType != WeaponTypes.Sword))
            {
                ch.send("You must be wielding two swords to flurry.\n\r");
                return;
            }

            chance = Utility.NumberPercent();

            var learned = ch.GetSkillPercentage(skill);

            if (chance > learned)
            {
                ch.Act("You attempt to start a flurry, but fail.", victim, type: ActType.ToChar);
                ch.Act("$n flails out wildly with $s swords but blunders.", type: ActType.ToRoom);
                ch.CheckImprove(skill, false, 2);
                ch.WaitState(2 * game.PULSE_VIOLENCE);
                return;
            }



            if ((chance + learned) > 175)
            {
                numhits = 7;
            }
            else if ((chance + learned) > 160)
            {
                numhits = 6;
            }
            else if ((chance + learned) > 145)
            {
                numhits = 5;
            }
            else if ((chance + learned) > 130)
            {
                numhits = 4;
            }
            else if ((chance + learned) > 115)
            {
                numhits = 3;
            }
            else if ((chance + learned) > 100)
            {
                numhits = 2;
            }
            else if ((chance + learned) > 85)
            {
                numhits = 2;
            }
            else
            {
                numhits = 2;
            }

            ch.Act("You begin a wild flurry of attacks!", victim, type: ActType.ToChar);
            ch.Act("$n begins a wild flurry of attacks!", type: ActType.ToRoom);
            for (i = 0; i < numhits; i++)
            {
                if (Utility.NumberPercent() > 80)
                {
                    dam = Utility.dice(45 / 4, 7);
                    Damage(ch, victim, dam, skill, WeaponDamageTypes.None);
                    dam = dam - dam / 5;
                    if (ch.Fighting != victim)
                        break;
                    continue;
                }
                oneHit(ch, victim, weapon, false, skill);
                if (ch.Fighting != victim)
                    break;

            }
            ch.CheckImprove(skill, true, 1);
            ch.WaitState(2 * game.PULSE_VIOLENCE);
            if ((numhits == 2))
            {
                ch.MovementPoints -= 25;
            }
            else if ((numhits == 3))
            {
                ch.MovementPoints -= 50;
            }
            else if ((numhits == 4))
            {
                ch.MovementPoints -= 75;
            }
            else if ((numhits == 5))
            {
                ch.MovementPoints -= 100;
            }
            else if ((numhits == 6))
            {
                ch.MovementPoints -= 125;
            }
            else if ((numhits == 7))
            {
                ch.MovementPoints -= 150;
            }
            else
            {
                ch.MovementPoints -= 25;
            }
            return;
        }

        public static void DoDrum(Character ch, string argument)
        {
            Character victim;
            int chance, numhits, i, dam, learned;
            ItemData weapon;
            ItemData weapon2;

            var skill = SkillSpell.SkillLookup("drum");
            learned = ch.GetSkillPercentage(skill);

            if (learned <= 1)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            weapon = ch.GetEquipment(WearSlotIDs.Wield);
            if (weapon == null)
            {
                ch.send("You first need to find a weapon to drum with.\n\r");
                return;
            }

            weapon2 = ch.GetEquipment(WearSlotIDs.DualWield);
            if (weapon2 == null)
            {
                ch.send("You need to find another weapon to drum with.\n\r");
                return;
            }

            if ((weapon.WeaponType != WeaponTypes.Mace) || (weapon2.WeaponType != WeaponTypes.Mace))
            {
                ch.send("You must be wielding two maces to drum.\n\r");
                return;
            }

            chance = Utility.NumberPercent();

            dam = Utility.dice(51 / 3, 7);

            if (chance > learned)
            {
                ch.Act("You attempt to start drumming with your maces, but fail.", victim, type: ActType.ToChar);
                ch.Act("$n attempts to start drumming with two maces, but fails.", type: ActType.ToRoom);
                ch.CheckImprove(skill, false, 1);
                ch.WaitState(2 * game.PULSE_VIOLENCE);
                return;
            }

            if ((chance + learned) > 175)
            {
                numhits = 4;
            }
            else if ((chance + learned) > 160)
            {
                numhits = 3;
            }
            else if ((chance + learned) > 145)
            {
                numhits = 2;
            }
            else
            {
                numhits = 2;
            }

            ch.Act("$n drums at $N with $s maces.", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n drums at you with $s maces.", victim, type: ActType.ToVictim);
            ch.Act("You drum at $N with your maces.", victim, type: ActType.ToChar);

            for (i = 0; i < numhits; i++)
            {
                if (Utility.NumberPercent() > 95)
                {
                    Damage(ch, victim, 0, skill, WeaponDamageTypes.None);
                    dam = dam - dam / 5;
                    if (ch.Fighting != victim)
                        break;
                    continue;
                }
                oneHit(ch, victim, weapon, false, skill);
                if (ch.Fighting != victim)
                    break;
            }
            Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
            ch.CheckImprove(skill, true, 1);
            ch.WaitState(2 * game.PULSE_VIOLENCE);
            return;
        }


        public static void DoFeint(Character ch, string arguments)
        {
            Character victim;

            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("feint");
            int chance;
            if ((chance = skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to mislead your foes.\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            AffectData aff;
            if ((aff = victim.FindAffect(skill)) != null)
            {
                ch.send("They are already being misled.\n\r");
                return;
            }

            if (CheckIsSafe(ch, victim)) return;

            ch.WaitState(skill.waitTime);

            /* stats */
            chance += ch.GetCurrentStat(PhysicalStatTypes.Dexterity);
            chance -= 2 * victim.GetCurrentStat(PhysicalStatTypes.Dexterity);

            /* speed  */
            if (ch.Flags.ISSET(ActFlags.Fast) || ch.IsAffected(AffectFlags.Haste))
                chance += 10;
            if (victim.Flags.ISSET(ActFlags.Fast) || victim.IsAffected(AffectFlags.Haste))
                chance -= 30;

            /* level */
            chance += (ch.Level - victim.Level) * 2;

            /* sloppy hack to prevent false zeroes */
            if (chance % 5 == 0)
                chance += 1;


            if (chance > Utility.NumberPercent())
            {

                ch.Act("$N is misled by $n's feint!", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n feints your next attack.", victim, type: ActType.ToVictim);
                ch.Act("You distract $N with a cunning feint.", victim, type: ActType.ToChar);

                AffectData newAffect = new AffectData();

                newAffect.duration = 2;
                newAffect.frequency = Frequency.Violence;
                newAffect.displayName = "feint";
                newAffect.modifier = 0;
                newAffect.location = ApplyTypes.None;
                newAffect.skillSpell = skill;
                newAffect.affectType = AffectTypes.Skill;
                victim.AffectToChar(newAffect);

                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.Act("$n fails to mislead $N with $s feint!", type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to distract you, but fails to mislead your next attack.", victim, type: ActType.ToVictim);
                ch.Act("You fail to distract $N from their next attack.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoCrusaderStrike(Character ch, string arguments)
        {
            ItemData weapon;
            var skill = SkillSpell.SkillLookup("crusader strike");
            int chance;
            int dam;
            var level = ch.Level;
            Character victim = null;

            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
            }
            else if (!ch.Equipment.TryGetValue(WearSlotIDs.Wield, out weapon) || weapon == null || !weapon.extraFlags.ISSET(ExtraFlags.TwoHands))
            {
                ch.send("You must be wielding a two-handed weapon to crusader strike someone.\n\r");
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
            }
            else if (victim == ch)
            {
                ch.send("You can't crusaders strike yourself.\n\r");
            }

            else if (chance > Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);
                var roll = weapon.DamageDice.Roll();

                dam = Utility.Random(dam_each[level] * 2 + roll, dam_each[level] * 3 + roll);

                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, weapon.WeaponDamageType.Type);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, weapon.WeaponDamageType.Type);
            }
            return;
        } // end crusader strike

        public static void DoBite(Character ch, string arguments)
        {
            var dam_each = new int[]
            {
                36,
                63,
                78,
                95
            };

            int dam;
            Character victim = null;
            var skill = SkillSpell.SkillLookup("bite");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form == null)
            {
                ch.send("Only animals can bite someone.\n\r");
                return;
            }

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            var level = 3 - (int)ch.Form.Tier;
            chance += 20;
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n ferociously bite $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n ferociously bites you.", victim, type: ActType.ToVictim);
                ch.Act("You ferociously bite $N.", victim, type: ActType.ToChar);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);
                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bite);
            }
            else
            {
                ch.Act("$n snaps to bite $N but fails to make contact.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n snaps at you but fails to make contact.", victim, type: ActType.ToVictim);
                ch.Act("You snap at $N in an attemt to bite them, but fail to make contact.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bite);
            }
            return;
        }
        public static void DoPeck(Character ch, string arguments)
        {
            var dam_each = new int[]
            {
                36,
                63,
                78,
                95
            };

            int dam;
            Character victim = null;
            var skill = SkillSpell.SkillLookup("peck");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form == null)
            {
                ch.send("Only animals can peck someone.\n\r");
                return;
            }

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            var level = 3 - (int)ch.Form.Tier;
            chance += 20;
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n ferociously peck $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n ferociously pecks you.", victim, type: ActType.ToVictim);
                ch.Act("You ferociously pecks $N.", victim, type: ActType.ToChar);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);
                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);
            }
            else
            {
                ch.Act("$n tries to peck $N but fails to make contact.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to peck you but fails to make contact.", victim, type: ActType.ToVictim);
                ch.Act("You try to peck at $N, but fail to make contact.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }
        public static void DoWaspSting(Character ch, string arguments)
        {
            var dam_each = new int[]
            {
                36,
                63,
                78,
                95
            };

            int dam;
            Character victim = null;
            var skill = SkillSpell.SkillLookup("wasp sting");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
            }
            else if (ch.Form == null)
            {
                ch.send("Only animals can sting someone.\n\r");
            }

            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
            }

            var level = 3 - (int)ch.Form.Tier;
            chance += 20;
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n ferociously stings $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n ferociously stings you.", victim, type: ActType.ToVictim);
                ch.Act("You ferociously stings $N.", victim, type: ActType.ToChar);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);
                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Sting);
            }
            else
            {
                ch.Act("$n tries to sting $N but fails to make contact.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to sting you fails to make contact.", victim, type: ActType.ToVictim);
                ch.Act("You try to sting $N, but fail to make contact.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Sting);
            }
            return;
        }

        public static void DoClaw(Character ch, string arguments)
        {

            var dam_each = new int[]
           {
                36,
                63,
                78,
                95
           };
            var skill = SkillSpell.SkillLookup("claw");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form == null)
            {
                ch.send("Only animals can claw someone.\n\r");
                return;
            }
            int dam;
            var level = 3 - (int)ch.Form.Tier;
            Character victim = null;

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            chance += (level * 2);
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n makes a furious attack with their claws at $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n makes a furious attack with their claws at you.", victim, type: ActType.ToVictim);
                ch.Act("You make a furious attack with your claws at $N.", victim, type: ActType.ToChar);

                dam = ch.GetDamage(level, 1, 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);
            }
            else
            {
                ch.Act("$n tries to make a furious attack with their claws at $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to make a furious attack with their claws at you.", victim, type: ActType.ToVictim);
                ch.Act("You try to make a furious attack with your claws at $N.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoFuror(Character ch, string arguments)
        {

            var dam_each = new int[]
           {
                36,
                63,
                78,
                95
           };
            var skill = SkillSpell.SkillLookup("furor");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form == null)
            {
                ch.send("Only animals can ferociously attack someone.\n\r");
                return;
            }

            int dam;
            var level = 3 - (int)ch.Form.Tier;
            Character victim = null;

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            chance += (level * 2);
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n makes a series of ferocious attack, biting and clawing at $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n makes a series of ferocious attacks, biting and clawing at you.", victim, type: ActType.ToVictim);
                ch.Act("You make a series of ferocious attacks, biting and clawing at $N.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, true, 1);

                for (int i = 0; i < 4; i++)
                {
                    dam = ch.GetDamage(level, 1, 2);
                    Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

                    if (ch.Fighting != victim)
                        break;
                }
            }
            else
            {
                ch.Act("$n tries to make a furious attack on $N but fails to get close enough.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to make a furious attack on you but fails to get close enough.", victim, type: ActType.ToVictim);
                ch.Act("You try to make a furious attack on $N but fail to get close enough.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoTrample(Character ch, string arguments)
        {

            var dam_each = new int[]
           {
                36,
                63,
                78,
                95
           };
            var skill = SkillSpell.SkillLookup("trample");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form == null)
            {
                ch.send("Only animals can trample someone.\n\r");
                return;
            }

            int dam;
            var level = 3 - (int)ch.Form.Tier; // ch.Level;
            Character victim = null;

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            chance += (level * 2);// / 10;
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n charges at $N and tramples $M.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n charges at you, and tramples you.", victim, type: ActType.ToVictim);
                ch.Act("You charge at $N, trampling them.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, true, 1);

                for (int i = 0; i < 4; i++)
                {
                    dam = ch.GetDamage(level, 1, 2);

                    Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

                    if (ch.Fighting != victim)
                        break;
                }
            }
            else
            {
                ch.Act("$n tries to charge at $N but fails to get close enough.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to charge you but fails to get close enough.", victim, type: ActType.ToVictim);
                ch.Act("You try to make charge $N but fail to get close enough.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoBerserkersStrike(Character ch, string arguments)
        {
            int dam;
            var level = ch.Level;
            Character victim = null;
            var skill = SkillSpell.SkillLookup("berserkers strike");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            if (victim == ch)
            {
                ch.send("You can't berserkers strike yourself.\n\r");
                return;
            }
            chance += level / 10;
            var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (wield != null)
                {
                    var roll = wield.DamageDice.Roll();
                    dam = ch.GetDamage(level, .5f, 1, roll);

                    Combat.Damage(ch, victim, dam, skill, wield != null ? wield.WeaponDamageType.Type : wield.WeaponDamageType.Type);
                }
                else
                {
                    dam = ch.GetDamage(level, .5f, 1);
                    Combat.Damage(ch, victim, dam, skill, wield != null ? wield.WeaponDamageType.Type : WeaponDamageTypes.Bash);
                }
                ch.CheckImprove(skill, true, 1);

            }
            else
            {
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, wield != null ? wield.WeaponDamageType.Type : WeaponDamageTypes.Bash);
            }
            return;
        }

        public static void DoRisingKick(Character ch, string arguments)
        {
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("rising kick");

            if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You better leave the martial arts to fighters.\n\r");
                return;
            }

            if (ch.Fighting == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            bool improved = false;
            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (victim.Fighting == null || (victim.Fighting != ch && !victim.Fighting.IsSameGroup(ch)))
                    continue;

                if (skillPercent > Utility.NumberPercent())
                {
                    dam = (ch.Level) / 2;
                    dam += Utility.Random(0, (ch.Level) / 6);
                    dam += Utility.Random(0, (ch.Level) / 6);
                    dam += Utility.Random(0, (ch.Level) / 6);
                    dam += Utility.Random(0, (ch.Level) / 6);
                    dam += Utility.Random(0, (ch.Level) / 6);
                    dam += Utility.Random(0, (ch.Level) / 6);
                    dam += Utility.Random(ch.Level / 5, ch.Level / 4);

                    ch.Act("$n performs a rising kick on $N.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n performs a rising kick on you.", victim, type: ActType.ToVictim);
                    ch.Act("You perform a rising kick on $N.", victim, type: ActType.ToChar);
                    Damage(ch, victim, dam, skill);

                    improved = true;
                }
                else
                {
                    ch.Act("$n attempts to perform a rising kick on $N.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n attempts to perform a rising kick on you.", victim, type: ActType.ToVictim);
                    ch.Act("You attempt to perform a rising kick on $N.", victim, type: ActType.ToChar);
                    Damage(ch, victim, 0, skill);
                }
            }

            ch.CheckImprove(skill, improved, 1);
        }

        public static void DoHoofStrike(Character ch, string arguments)
        {
            Character victim = null;
            var dam_each = new int[]
             {
                    50,
                    65,
                    85,
                    105
             };

            int dam;
            int skillPercent = 0;

            if (ch.Form == null)
            {
                ch.send("You don't know how to hoof strike.\n\r");
                return;
            }
            var skill = SkillSpell.SkillLookup("hoof strike");
            if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to hoof strike.\n\r");
                return;
            }
            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                var level = 4 - ((int)ch.Form.Tier);
                dam = dam_each[level];

                ch.Act("$n unleashes a powerful kick with each hoof, striking $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n unleashes a powerful kick with each of $s hoofs, striking you!", victim, type: ActType.ToVictim);
                ch.Act("You unleash a powerful kick with each hoof, striking $N.", victim, type: ActType.ToChar);

                Damage(ch, victim, dam, skill);

                if (ch.Fighting == victim)
                    Damage(ch, victim, dam, skill);
            }
            else
            {
                Damage(ch, victim, 0, skill);
            }
        }

        public static void DoStrike(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("strike");
            int chance;
            Character victim = null;

            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to strike your enemy.\n\r");
                return;
            }
            else if (ch.Form == null)
            {
                ch.send("Only animals can strike their enemies.\n\r");
                return;
            }
            else if ((arguments.ISEMPTY() && (victim = ch.Fighting) == null) || (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null))
            {
                ch.send("Strike who?\n\r");
                return;
            }
            else if (chance < Utility.NumberPercent())
            {
                ch.Act("You attempt to strike $N but miss.", victim);
                ch.Act("$n attempts to strike at $N but misses.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to strike you!", victim, type: ActType.ToVictim);
                Combat.Damage(ch, victim, 0, skill);
                ch.WaitState(skill.waitTime);
            }
            else
            {

                ch.Act("You coils up before striking at $N.", victim);
                ch.Act("$n coils up and strikes at $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n coils up and strikes you!", victim, type: ActType.ToVictim);
                Combat.Damage(ch, victim, Utility.dice(5, 5, 100), skill);
                ch.WaitState(skill.waitTime);
            }
        }

        public static void DoPinch(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("pinch");
            int chance;
            Character victim = null;

            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to pinch your enemy.\n\r");
                return;
            }
            else if (ch.Form == null)
            {
                ch.send("Only animals can pinch their enemies.\n\r");
                return;
            }
            else if ((arguments.ISEMPTY() && (victim = ch.Fighting) == null) || (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null))
            {
                ch.send("Pinch who?\n\r");
                return;
            }
            else if (chance < Utility.NumberPercent())
            {
                ch.Act("You attempt to pinch $N but fail to get close enough.", victim);
                ch.Act("$n attempts to pinch $N but doesn't get close.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to pinch you but doesn't get close!", victim, type: ActType.ToVictim);
                Combat.Damage(ch, victim, 0, skill);
                ch.WaitState(skill.waitTime);
            }
            else
            {

                ch.Act("You fiercely grip $N with your powerful claws, pinching with determination.", victim);
                ch.Act("$n fiercely grips $N with its powerful claws, pinching with determination.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n fiercely grips you with $s powerful claws, pinching with determination!", victim, type: ActType.ToVictim);
                Combat.Damage(ch, victim, Utility.dice(5, 5, 100), skill);
                ch.WaitState(skill.waitTime);
            }
        }

        public static void DoJump(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("jump");
            int chance;
            var dam_each = new int[]
           {
                36,
                63,
                78,
                95
           };
            int dam;
            Character victim = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to jump.\n\r");
                return;
            }
            else if (ch.Form == null)
            {
                ch.send("Only animals can jump.\n\r");
                return;
            }
            else if ((arguments.ISEMPTY() && (victim = ch.Fighting) == null) || (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null))
            {
                ch.send("Jump on who?\n\r");
                return;
            }
            else if (chance < Utility.NumberPercent())
            {
                ch.Act("You attempt to jump on $N's head but miss.", victim);
                ch.Act("$n jumps towards $N but misses.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to jump on your head!", victim, type: ActType.ToVictim);
                Combat.Damage(ch, victim, 0, skill);
                ch.WaitState(skill.waitTime);
            }
            else
            {
                var level = 3 - (int)ch.Form.Tier;

                ch.Act("You jump on $N's head.", victim);
                ch.Act("$n jumps on $N's head.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n jumps on your head!", victim, type: ActType.ToVictim);
                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                Combat.Damage(ch, victim, dam, skill);
                ch.WaitState(skill.waitTime);

                if (victim.FindAffect(skill) == null)
                {
                    var aff = new AffectData();
                    aff.skillSpell = skill;
                    aff.duration = ch.Level / 4;
                    aff.endMessage = "Your head stops throbbing.";
                    aff.endMessageToRoom = "$n's head stops throbbing.";
                    aff.ownerName = ch.Name;
                    aff.level = ch.Level;
                    aff.location = ApplyTypes.Strength;
                    aff.modifier = -4 - ch.Level / 8;
                    aff.affectType = AffectTypes.Skill;
                    aff.displayName = skill.name;
                    aff.where = AffectWhere.ToAffects;

                    victim.AffectToChar(aff);
                }
            }
        }

        public static void DoAntlerSwipe(Character ch, string arguments)
        {
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("antler swipe");

            if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Fighting == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            bool improved = false;

            ch.Act("$n swings $s antlers viciously.", type: ActType.ToRoomNotVictim);
            ch.Act("You swing your antlers viciously.", type: ActType.ToChar);

            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (victim.Fighting == null || (victim.Fighting != ch && !victim.Fighting.IsSameGroup(ch)))
                    continue;

                if (skillPercent > Utility.NumberPercent())
                {
                    Damage(ch, victim, Utility.dice(4, 4, 85), skill);

                    improved = true;
                }
                else
                {
                    Damage(ch, victim, 0, skill);
                }
            }

            ch.CheckImprove(skill, improved, 1);
        }

        public static void DoTailSwipe(Character ch, string arguments)
        {
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("tailswipe");

            if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Fighting == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            bool improved = false;

            ch.Act("$n sweeps $s tail in a wide arc.", type: ActType.ToRoomNotVictim);
            ch.Act("You sweep around with your tail.", type: ActType.ToChar);

            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (victim.Fighting == null || (victim.Fighting != ch && !victim.Fighting.IsSameGroup(ch)))
                    continue;

                if (skillPercent > Utility.NumberPercent())
                {
                    Damage(ch, victim, Utility.dice(4, 4, 30), skill);
                    victim.WaitState(1);
                    improved = true;
                }
                else
                {
                    Damage(ch, victim, 0, skill);
                }
            }

            ch.CheckImprove(skill, improved, 1);
        }

        public static void DoSwipe(Character ch, string arguments)
        {

            var dam_each = new int[]
           {
                36,
                63,
                78,
                95
           };
            var skill = SkillSpell.SkillLookup("swipe");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form == null)
            {
                ch.send("Only animals can swipe someone.\n\r");
                return;
            }
            int dam;
            var level = 3 - (int)ch.Form.Tier;
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

            chance += (level * 2);
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n unleashes a powerful blow on $N, swiping $M with $s claws.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n unleashes a powerful blow on you, swiping you with $s claws.", victim, type: ActType.ToVictim);
                ch.Act("You unleash a powerful blow on $N, swiping $M with your claws.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);
            }
            else
            {
                ch.Act("$n tries to make a furious attack with their claws at $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to make a furious attack with their claws at you.", victim, type: ActType.ToVictim);
                ch.Act("You try to make a furious attack with your claws at $N.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoRip(Character ch, string arguments)
        {

            var dam_each = new int[]
           {
                36,
                63,
                78,
                95
           };
            var skill = SkillSpell.SkillLookup("rip");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form == null)
            {
                ch.send("Only animals can rip someone with their claws.\n\r");
                return;
            }
            int dam;
            var level = 3 - (int)ch.Form.Tier;
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

            chance += (level * 2);
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n rips $N up, leaving behind a vicious wound.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n rips you up, leaving behind a vicious wound.", victim, type: ActType.ToVictim);
                ch.Act("You rip $N up, leaving behind a vicious wound.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

                var bleeding = SkillSpell.SkillLookup("bleeding");

                if (victim.FindAffect(bleeding) == null)
                {
                    var aff = new AffectData();
                    aff.skillSpell = bleeding;
                    aff.duration = Utility.Random(5, 10);
                    aff.endMessage = "Your bleeding stops.";
                    aff.endMessageToRoom = "$n stops bleeding.";
                    aff.ownerName = ch.Name;
                    aff.level = ch.Level;
                    aff.modifier = -4;
                    aff.location = ApplyTypes.Strength;
                    aff.affectType = AffectTypes.Skill;
                    aff.displayName = "bleeding";
                    aff.where = AffectWhere.ToAffects;

                    victim.AffectToChar(aff);
                }
            }
            else
            {
                ch.Act("$n attempts to rip $N up but doesn't connect.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to rip you up!", victim, type: ActType.ToVictim);
                ch.Act("You try to rip $N up but fail to connect.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void AffectBleedingTick(Character ch, AffectData affect)
        {
            Combat.Damage(ch, ch, Math.Max(affect.level / 2, 8), affect.skillSpell, WeaponDamageTypes.Pierce, affect.ownerName);
        }


        public static void DoDevour(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                40,
                70,
                90,
                120
            };
            var skill = SkillSpell.SkillLookup("devour");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form == null)
            {
                ch.send("Only animals can devour someone.\n\r");
                return;
            }
            int dam;
            var level = 3 - (int)ch.Form.Tier;
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

            chance += (level * 2);
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n closes in on $N, and ferociously devours $M.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n closes in on you, and ferociously devours you.", victim, type: ActType.ToVictim);
                ch.Act("You close in on $N, and ferociously devour them.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);
            }
            else
            {
                ch.Act("$n tries to close in on $N, but can't get close enough.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to close in on you, but can't get close enough.", victim, type: ActType.ToVictim);
                ch.Act("You try to close in on $N, but can't get close enough.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoRescue(Character ch, string arguments)
        {
            Character torescue;
            Character victim = null;
            var skill = SkillSpell.SkillLookup("rescue");
            var chance = ch.GetSkillPercentage(skill) + 20;

            if (ch.IsNPC) chance += 80;

            if (chance <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if ((torescue = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            foreach (var other in ch.Room.Characters)
            {
                if (other.Fighting == torescue)
                {
                    victim = other;
                    break;
                }
            }

            if (victim == null)
            {
                ch.send("No one is fighting them.\n\r");
                return;
            }

            ch.WaitState(game.PULSE_VIOLENCE);
            if (chance >= Utility.NumberPercent())
            {

                victim.Fighting = ch;
                ch.Act("$n rescues $N!", torescue, type: ActType.ToRoomNotVictim);
                ch.Act("$n rescues you!", torescue, type: ActType.ToVictim);
                ch.Act("You rescue $N!", torescue, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.Act("$n tries to rescue $N, but fails.", torescue, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to rescue you, but fails.", torescue, type: ActType.ToVictim);
                ch.Act("You try to rescue $N, but fail.", torescue, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoIntercept(Character ch, string arguments)
        {
            Character tointercept = null;
            var skill = SkillSpell.SkillLookup("intercept");
            var chance = ch.GetSkillPercentage(skill);
            if (chance <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (tointercept = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anything.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (tointercept = ch.GetCharacterFromRoomByName(arguments)) == null) || tointercept == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (tointercept.Fighting == ch)
            {
                ch.send("They are already targeting you.\n\r");
                return;
            }
            else if (tointercept.Fighting == null)
            {
                ch.send("They aren't fighting anyone.\n\r");
                return;
            }
            else if (chance >= Utility.NumberPercent())
            {
                ch.WaitState(game.PULSE_VIOLENCE);
                if (tointercept != null && tointercept.Fighting != null)
                {
                    tointercept.Fighting = ch;
                    ch.Act("$n intercepts $N!", tointercept, type: ActType.ToRoomNotVictim);
                    ch.Act("$n intercepts you!", tointercept, type: ActType.ToVictim);
                    ch.Act("You intercept $N!", tointercept, type: ActType.ToChar);
                    return;
                }
            }
            else
            {
                ch.WaitState(game.PULSE_VIOLENCE);
                ch.Act("$n tries to intercept $N, but fails.", tointercept, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to intercept you, but fails.", tointercept, type: ActType.ToVictim);
                ch.Act("You try to intercept $N, but fail.", tointercept, type: ActType.ToChar);
            }
        }


        public static void DoImpale(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("impale");
            int chance;
            int dam;
            var level = ch.Level;
            ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {36, 63, 78, 95};
                level = 3 - (int)ch.Form.Tier;
                //ch.send("Only animals can impale someone.\n\r");
                //return;
            }
            else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Spear)
            {
                ch.send("You must be wearing a spear to impale your enemy.\n\r");
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

            //chance += (level * 2);
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n impales $N, leaving behind a vicious wound.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n impale you, leaving behind a vicious wound.", victim, type: ActType.ToVictim);
                ch.Act("You impale $N, leaving behind a vicious wound.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

                var bleeding = SkillSpell.SkillLookup("bleeding");

                if (victim.FindAffect(bleeding) == null)
                {
                    var aff = new AffectData();
                    aff.skillSpell = bleeding;
                    aff.duration = Utility.Random(5, 10);
                    aff.endMessage = "Your bleeding stops.";
                    aff.endMessageToRoom = "$n stops bleeding.";
                    aff.ownerName = ch.Name;
                    aff.level = ch.Level;
                    aff.modifier = -4;
                    aff.location = ApplyTypes.Strength;
                    aff.affectType = AffectTypes.Skill;
                    aff.displayName = "bleeding";
                    aff.where = AffectWhere.ToAffects;

                    victim.AffectToChar(aff);
                }
            }
            else
            {
                ch.Act("$n attempts to impale $N but doesn't connect.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to impale you!", victim, type: ActType.ToVictim);
                ch.Act("You try to impale $N but fail to connect.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoFlank(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("flank");
            int chance;
            int dam;
            var level = ch.Level;
            //ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {36, 63, 78, 95};
                level = 3 - (int)ch.Form.Tier;
                //ch.send("Only animals can impale someone.\n\r");
                //return;
            }
            //else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Spear)
            //{
            //    ch.send("You must be wearing a spear to impale your enemy.\n\r");
            //    return;
            //}
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

            foreach (var other in ch.Room.Characters)
            {
                if (other.Fighting == ch)
                {
                    ch.send("You are too busy defending yourself.\n\r;");
                    return;
                }
            }
            //chance += (level * 2);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n flank attacks $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n flank attacks you.", victim, type: ActType.ToVictim);
                ch.Act("You flank attack $N.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            }
            else
            {
                ch.Act("$n attempts to flank attack $N but fails.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to flank attack you!", victim, type: ActType.ToVictim);
                ch.Act("You try to flank attack $N but fail.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoAmbush(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("ambush");
            int chance;
            int dam;
            var level = ch.Level;
            //ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }


            if (ch.Form != null)
            {
                dam_each = new int[]
                {36, 63, 78, 95};
                level = 3 - (int)ch.Form.Tier;
                //ch.send("Only animals can impale someone.\n\r");
                //return;
            }

            Character victim = null;

            if (ch.Position == Positions.Fighting)
            {
                ch.send("You're too busy fighting already!\n\r");
                return;
            }

            if (!ch.IsAffected(AffectFlags.Camouflage))
            {
                ch.send("You aren't using any cover to set up for an ambush.\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            if (victim == ch)
            {
                ch.send("You can't ambush yourself.\n\r");
                return;
            }
            //chance += (level * 2);


            ch.WaitState(skill.waitTime);

            if (CheckPreventSurpriseAttacks(ch, victim))
            {
                return;
            }
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                ch.Act("$n ambushes $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n ambushes you.", victim, type: ActType.ToVictim);
                ch.Act("You ambush $N.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level] * 3, dam_each[level] * 4);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);
            }
            else
            {
                ch.Act("$n tries to ambush $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to ambush you but fails!", victim, type: ActType.ToVictim);
                ch.Act("You try to ambush $N but fail.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        private static bool CheckPreventSurpriseAttacks(Character ch, Character victim)
        {
            int awareness;

            if (!victim.IsAwake) return false;
            SetFighting(victim, ch);

            if (CheckAcrobatics(ch, victim)) return true;
            if ((awareness = victim.GetSkillPercentage("awareness") + 20) > 21 && awareness > Utility.NumberPercent())
            {
                ch.Act("They were immune to your surprise attack.");
                victim.CheckImprove("awareness", true, 1);
                return true;
            }
            else if (awareness > 21)
            {
                victim.CheckImprove("awareness", false, 1);
            }
            return false;
        }

        private static bool CheckAcrobatics(Character ch, Character victim)
        {
            int acrobatics;
            if (!victim.IsAwake) return false;
            SetFighting(victim, ch);

            if ((acrobatics = victim.GetSkillPercentage("acrobatics") + 20) > 21 && acrobatics > Utility.NumberPercent())
            {
                ch.Act("They nimbly avoid your detrimental attack.");
                victim.CheckImprove("acrobatics", true, 1);
                return true;
            }
            else if (acrobatics > 21)
            {
                victim.CheckImprove("acrobatics", false, 1);
                return false;
            }
            return false;
        }

        public static void DoCharge(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("charge");
            int chance;
            int dam;
            var level = ch.Level;
            ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {36, 63, 78, 95};
                level = 3 - (int)ch.Form.Tier;
                //ch.send("Only animals can impale someone.\n\r");
                //return;
            }
            else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || (wield.WeaponType != WeaponTypes.Spear && wield.WeaponType != WeaponTypes.Polearm))
            {
                ch.send("You must be wearing a spear or polearm to charge your enemy.\n\r");
                return;
            }
            Character victim = null;

            if (ch.Position == Positions.Fighting)
            {
                ch.send("You're too busy fighting already!\n\r");
                return;
            }


            if (arguments.ISEMPTY() || (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            //chance += (level * 2);

            ch.WaitState(skill.waitTime);

            if (CheckPreventSurpriseAttacks(ch, victim)) return;

            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n charges $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n charges you.", victim, type: ActType.ToVictim);
                ch.Act("You charge $N.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level] * 3, dam_each[level] * 4);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            }
            else
            {
                ch.Act("$n tries to charge $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to charge you but fails!", victim, type: ActType.ToVictim);
                ch.Act("You try to charge $N but fail.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoHeadbutt(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("headbutt");
            int chance;
            int dam;
            var level = ch.Level;
            //ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    25,
                    35,
                    45,
                    60
                };
                level = 3 - (int)ch.Form.Tier;
                //ch.send("Only animals can impale someone.\n\r");
                //return;
            }
            //else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Spear)
            //{
            //    ch.send("You must be wearing a spear to impale your enemy.\n\r");
            //    return;
            //}
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



            //chance += (level * 2);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n headbutts $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n headbutts you.", victim, type: ActType.ToVictim);
                ch.Act("You headbutt $N.", victim, type: ActType.ToChar);
                victim.WaitState(game.PULSE_VIOLENCE * 2);
                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
                Combat.Damage(ch, ch, dam / 2, skill, WeaponDamageTypes.Bash);
            }
            else
            {
                ch.Act("$n attempts to headbutt $N but fails.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to headbutt you!", victim, type: ActType.ToVictim);
                ch.Act("You try to headbutt $N but fail.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
            }
            return;
        }

        public static void DoLickSelf(Character ch, string argument)
        {
            AffectData af;
            var skill = SkillSpell.SkillLookup("lick self");

            if (ch.GetSkillPercentage(skill) + 20 <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if (ch.IsAffected(skill))
            {
                ch.send("You can't benefit from licking your wounds more yet.\n\r");
                return;
            }


            if (Utility.NumberPercent() > ch.GetSkillPercentage(skill))
            {
                ch.send("You lick your wounds but it has no effect.\n\r");
                ch.Act("$n licks $s wounds but doesn't seem to find any relief.", type: ActType.ToRoom);
                ch.CheckImprove(skill, false, 3);
                return;
            }

            ch.Act("$n spews healing saliva and antimicrobial substances to heal itself.", type: ActType.ToRoom);
            ch.send("You spew healing saliva and antimicrobial substances to heal yourself.\n\r");
            ch.send("You feel better.\n\r");

            ch.HitPoints += (int)(ch.MaxHitPoints * 0.2);
            ch.HitPoints = Math.Min(ch.HitPoints, ch.MaxHitPoints);

            if (Utility.NumberPercent() < Math.Max(1, ch.Level / 4) && ch.IsAffected(AffectFlags.Plague))
            {
                ch.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("plague")));
                ch.Act("The sores on $n's body vanish.\n\r", type: ActType.ToRoom);
                ch.send("The sores on your body vanish.\n\r");
            }

            if (Utility.NumberPercent() < Math.Max(1, (ch.Level)) && ch.IsAffected(AffectFlags.Blind))
            {
                ch.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("blindness")));
                ch.send("Your vision returns!\n\r");
            }

            if (Utility.NumberPercent() < Math.Max(1, ch.Level / 2) && ch.IsAffected(AffectFlags.Poison))
            {
                ch.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("poison")));
                ch.send("A warm feeling goes through your body.\n\r");
                ch.Act("$n looks better.", type: ActType.ToRoom);
            }
            ch.CheckImprove(skill, true, 3);

            af = new AffectData();

            af.where = AffectWhere.ToAffects;
            af.skillSpell = skill;
            af.location = 0;
            af.duration = 1;
            af.modifier = 0;
            af.level = ch.Level;
            af.affectType = AffectTypes.Skill;
            af.displayName = "lick self";
            af.endMessage = "You feel ready to lick your wounds once more.";
            ch.AffectToChar(af);
            return;
        }

        public static void DoRetract(Character ch, string argument)
        {
            var skill = SkillSpell.SkillLookup("retract");

            if (ch.GetSkillPercentage(skill) <= 1)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if (ch.IsAffected(AffectFlags.Retract))
            {
                ch.AffectedBy.REMOVEFLAG(AffectFlags.Retract);

                if (ch.Form != null && ch.Form.Name.Contains("tortoise"))
                {
                    ch.Act("$n pulls $s limbs and head out of $s shell.", type: ActType.ToChar);
                    ch.Act("You pull your limbs and head out of your shell.");
                }
                else
                {
                    ch.Act("$n uncurls becoming more vulnerable.", type: ActType.ToChar);
                    ch.Act("You uncurl becoming more vulnerable.");
                }
                return;
            }

            if (ch.Form != null && ch.Form.Name.Contains("tortoise"))
            {
                ch.Act("$n retreats into $s shell for protection.", type: ActType.ToChar);
                ch.Act("You retreat into your shell for protection.");
            }
            else
            {
                ch.Act("$n curls into a ball, becoming less vulnerable.", type: ActType.ToRoom);
                ch.Act("You curl into a ball, becoming less vulnerable.");
            }
            ch.AffectedBy.SETBIT(AffectFlags.Retract);
            return;
        }

        public static void DoTuskJab(Character ch, string arguments)
        {

            var dam_each = new int[]
            {40, 70, 90, 120};
            var skill = SkillSpell.SkillLookup("tusk jab");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            int dam;

            Character victim = null;

            if (ch.Form == null)
            {
                ch.send("Only animals can tusk jab someone.\n\r");
                return;
            }

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            var level = 3 - (int)ch.Form.Tier;
            chance += (level * 2);
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                ch.Act("$n jabs $N with $s tusks.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n jabs you with $s tusks.", victim, type: ActType.ToVictim);
                ch.Act("You jab $N with your tusks.", victim, type: ActType.ToChar);


                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);
            }
            else
            {
                ch.Act("$n attempts to jab $N with $s tusks, but fails to make contact.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to jab you with $s tusks, but fails to make contact.", victim, type: ActType.ToVictim);
                ch.Act("You attempt to jab $N with your tusks, but fail to make contact.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoHoofStomp(Character ch, string arguments)
        {

            var dam_each = new int[]
            {25, 35, 45, 65};
            var skill = SkillSpell.SkillLookup("hoof stomp");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            int dam;

            Character victim = null;

            if (ch.Form == null)
            {
                ch.send("Only animals can hoof stomp someone.\n\r");
                return;
            }

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            var level = 3 - (int)ch.Form.Tier;
            chance += (level * 2);
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                ch.Act("$n stomps $N with $s hooves.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n stomps you with $s hooves.", victim, type: ActType.ToVictim);
                ch.Act("You stomp $N with your hooves.", victim, type: ActType.ToChar);


                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                victim.WaitState(game.PULSE_VIOLENCE * 2);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);
            }
            else
            {
                ch.Act("$n attempts to stomp $N with $s hooves, but fails.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to stomp you with $s hooves, but fails.", victim, type: ActType.ToVictim);
                ch.Act("You attempt to stomp $N with your hooves, but fail.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoDive(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("dive");
            int chance;
            int dam;
            var level = ch.Level;
            //ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    40,
                    60,
                    80,
                    130
                };
                level = 3 - (int)ch.Form.Tier;
                //ch.send("Only animals can impale someone.\n\r");
                //return;
            }
            //else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Spear)
            //{
            //    ch.send("You must be wearing a spear to impale your enemy.\n\r");
            //    return;
            //}
            Character victim = null;

            if (ch.Position == Positions.Fighting)
            {
                ch.send("You're too busy fighting already!\n\r");
                return;
            }

            if (ch.Room.sector != SectorTypes.Cave || ch.Room.sector != SectorTypes.Forest)
            {
                ch.send("You don't see any ledges or trees suitable for diving from.\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            //chance += (level * 2);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n dives on $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n dives on you.", victim, type: ActType.ToVictim);
                ch.Act("You dive on $N.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            }
            else
            {
                ch.Act("$n tries to dive on $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to dive on you, but fails!", victim, type: ActType.ToVictim);
                ch.Act("You try to dive on $N, but fail.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoPounceAttack(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("pounce attack");
            int chance;
            int dam;
            var level = ch.Level;
            //ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    60,
                    90,
                    120,
                    130
                };
                level = 3 - (int)ch.Form.Tier;
                //ch.send("Only animals can impale someone.\n\r");
                //return;
            }
            //else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Spear)
            //{
            //    ch.send("You must be wearing a spear to impale your enemy.\n\r");
            //    return;
            //}
            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You haven't recovered from your last pounce attack yet!\n\r");
                return;
            }


            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            //chance += (level * 2);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n pounces on $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n pounces on you.", victim, type: ActType.ToVictim);
                ch.Act("You pounce on $N.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);
                var affect = new AffectData();
                affect.skillSpell = skill;
                affect.hidden = true;
                affect.frequency = Frequency.Violence;
                affect.duration = 2;
                ch.AffectToChar(affect);
            }
            else
            {
                ch.Act("$n tries to pounce on $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to pounce on you, but fails!", victim, type: ActType.ToVictim);
                ch.Act("You try to pounce on $N, but fail.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoHowl(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("howl");
            int chance;
            int dam;
            var level = ch.Level;
            //ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    60,
                    90,
                    120,
                    130
                };
                level = 3 - (int)ch.Form.Tier;
            }
            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to howl yet!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            //chance += (level * 2);

            ch.WaitState(skill.waitTime);
            //if (chance > utility.number_percent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n lets loose a deafening howl.", victim, type: ActType.ToRoom);

                ch.Act("You let loose a deafening howl.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, true, 1);

                foreach (var other in ch.Room.Characters)
                {
                    if (other != ch && other.Fighting != null && (other.Fighting == ch || other.Fighting.IsSameGroup(ch)))
                    {
                        dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                        Combat.Damage(ch, other, dam, skill, WeaponDamageTypes.Pierce);
                        var deafen = new AffectData();
                        deafen.skillSpell = skill;
                        deafen.level = ch.Level;
                        deafen.endMessage = "The ringing in your ears lessens.";
                        deafen.affectType = AffectTypes.Skill;
                        deafen.where = AffectWhere.ToAffects;
                        deafen.displayName = "deafening howl";
                        deafen.flags.SETBIT(AffectFlags.Deafen);
                        deafen.duration = 3;
                        other.AffectToChar(deafen);
                    }
                }
                var affect = new AffectData();
                affect.skillSpell = skill;
                affect.hidden = true;
                affect.frequency = Frequency.Tick;
                affect.duration = 5;
                ch.AffectToChar(affect);


            }
            return;
        }

        public static void DoQuillSpray(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("quill spray");
            int chance;
            int dam;
            var level = ch.Level;
            //ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    30,
                    40,
                    50,
                    60
                };
                level = 3 - (int)ch.Form.Tier;
                //ch.send("Only animals can impale someone.\n\r");
                //return;
            }
            //else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Spear)
            //{
            //    ch.send("You must be wearing a spear to impale your enemy.\n\r");
            //    return;
            //}
            //Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to spray quills yet!\n\r");
                return;
            }



            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);


            ch.Act("$n sprays quills everywhere.", type: ActType.ToRoom);

            ch.Act("You spray quills everywhere.", type: ActType.ToChar);

            ch.CheckImprove(skill, true, 1);

            foreach (var other in ch.Room.Characters)
            {
                if (other != ch && !other.IsSameGroup(ch))
                {
                    dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                    Combat.Damage(ch, other, dam, skill, WeaponDamageTypes.Pierce);

                }
            }
            var affect = new AffectData();
            affect.skillSpell = skill;
            affect.hidden = true;
            affect.frequency = Frequency.Tick;
            affect.duration = 5;
            affect.endMessage = "You feel ready to spray more quills.";
            ch.AffectToChar(affect);

            return;
        }

        public static void DoChestPound(Character ch, string arguments)
        {
            AffectData affect;
            SkillSpell skill = SkillSpell.SkillLookup("chest pound");
            int skillPercent;

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((affect = ch.FindAffect(skill)) != null)
            {
                ch.send("You are already enraged!\n\r");
                return;
            }
            else
            {
                ch.WaitState(skill.waitTime);
                affect = new AffectData();
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.location = ApplyTypes.Damroll;
                affect.duration = 2;
                affect.modifier = +5;
                affect.displayName = "chest pound";
                affect.affectType = AffectTypes.Skill;
                ch.AffectToChar(affect);

                affect = new AffectData();
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 2;
                affect.modifier = +5;
                affect.displayName = "chest pound";
                affect.endMessage = "Your rage subsides.\n\r";
                affect.endMessageToRoom = "$n's rage subsides.\n\r";
                affect.affectType = AffectTypes.Skill;
                ch.AffectToChar(affect);

                int heal = (int)(ch.Level * 2.5) + 5;
                ch.HitPoints = Math.Min(ch.HitPoints + heal, ch.MaxHitPoints);


                ch.send("You pound your chest, becoming enraged.\n\r");
                ch.Act("$n pounds $s chest, becoming enraged.\n\r", type: ActType.ToRoom);
                ch.CheckImprove(skill, true, 1);
            }
        }

        public static void DoLaugh(Character ch, string arguments)
        {
            AffectData affect;
            SkillSpell skill = SkillSpell.SkillLookup("laugh");
            int skillPercent;

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.CheckSocials("laugh", arguments);
                return;
            }

            if ((affect = ch.FindAffect(skill)) != null)
            {
                ch.send("You are already empowered!\n\r");
                return;
            }
            else
            {
                ch.WaitState(skill.waitTime);

                affect = new AffectData();
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.flags.SETBIT(AffectFlags.Haste);
                affect.duration = 2;
                affect.displayName = "laugh";
                affect.affectType = AffectTypes.Skill;
                ch.AffectToChar(affect);

                affect = new AffectData();
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Damroll;
                affect.duration = 2;
                affect.modifier = Math.Max(3, ch.Level / 3);
                affect.displayName = "laugh";
                affect.affectType = AffectTypes.Skill;
                ch.AffectToChar(affect);

                affect = new AffectData();
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 2;
                affect.modifier = Math.Max(3, ch.Level / 3);
                affect.displayName = "laugh";
                affect.endMessage = "Your empowering laugh subsides.\n\r";
                affect.endMessageToRoom = "$n's empowering laugh subsides.\n\r";
                affect.affectType = AffectTypes.Skill;
                ch.AffectToChar(affect);

                int heal = (int)(ch.Level * 2.5) + 5;
                ch.HitPoints = Math.Min(ch.HitPoints + heal, ch.MaxHitPoints);


                ch.send("You let lose a laugh-like vocalization, invigorating yourself.\n\r");
                ch.Act("$n lets lose a laugh-like vocalization, invigorating $mself.\n\r", type: ActType.ToRoom);
                ch.CheckImprove(skill, true, 1);
            }
        }

        public static void DoNoxiousSpray(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("noxious spray");
            int chance;
            int dam;
            var level = ch.Level;
            var spray = SkillSpell.SkillLookup("noxious poison");
            //ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    30,
                    40,
                    50,
                    60
                };
                level = 3 - (int)ch.Form.Tier;
            }

            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to release a noxious spray yet!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            //chance += (level * 2);

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);


            ch.Act("$n ejects a hot noxious chemical spray from the tip of its abdomen with a popping sound.", victim, type: ActType.ToRoom);
            ch.Act("You eject a hot noxious chemical spray from the tip of its abdomen with a popping sound.", victim, type: ActType.ToChar);

            ch.CheckImprove(skill, true, 1);

            foreach (var other in ch.Room.Characters)
            {
                if (other != ch && other.Fighting != null && (other.Fighting == ch || other.Fighting.IsSameGroup(ch)))
                {
                    dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                    Combat.Damage(ch, other, dam, skill, WeaponDamageTypes.Pierce);

                    if (!other.IsAffected(spray))
                    {
                        var affect = new AffectData();
                        affect.ownerName = ch.Name;
                        affect.skillSpell = spray;
                        affect.level = ch.Level;
                        affect.where = AffectWhere.ToAffects;
                        affect.location = ApplyTypes.Strength;
                        affect.flags.Add(AffectFlags.Poison);
                        affect.duration = 2 + level / 8;
                        affect.modifier = -5;
                        affect.displayName = "Noxious Spray";
                        affect.endMessage = "You feel less sick.\n\r";
                        affect.endMessageToRoom = "$n looks less sick.\n\r";
                        affect.affectType = AffectTypes.Skill;

                        other.AffectToChar(affect);
                        other.send("You feel very sick.\n\r");
                        other.Act("$n looks very ill.", null, null, null, ActType.ToRoom);
                    }
                }
            }
            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = false;
            aff.frequency = Frequency.Tick;
            aff.location = ApplyTypes.AC;
            aff.where = AffectWhere.ToAffects;
            aff.displayName = "noxious spray";
            aff.modifier = 200;
            aff.duration = 5;
            aff.endMessage = "You feel ready to spray noxious chemicals again.";
            ch.AffectToChar(aff);


        }


        public static void DoVenomSpit(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
                5,  6,  7,  8,  9,  11, 14, 16, 21, 26,
                31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
                59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
                66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
                74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
                95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("venom spit");
            var venom = SkillSpell.SkillLookup("venom");
            int chance;
            int dam;
            var level = ch.Level;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    30,
                    50,
                    80,
                    100
                };
                level = 3 - (int)ch.Form.Tier;
            }

            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to spit venom again yet!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            if (Utility.Random(1, 10) <= 6 && !victim.IsAffected(skill))
            {
                if ((victim.ImmuneFlags.ISSET(WeaponDamageTypes.Blind) || (victim.Form != null && victim.Form.ImmuneFlags.ISSET(WeaponDamageTypes.Blind))) && !victim.IsAffected(AffectFlags.Deafen))
                    victim.Act("$n is immune to blindness.", type: ActType.ToRoom);
                else
                {
                    var blindaffect = new AffectData();
                    blindaffect.ownerName = ch.Name;
                    blindaffect.skillSpell = skill;
                    blindaffect.level = ch.Level;
                    blindaffect.where = AffectWhere.ToAffects;
                    blindaffect.location = ApplyTypes.Hitroll;
                    blindaffect.flags.Add(AffectFlags.Blind);
                    blindaffect.duration = 3;
                    blindaffect.modifier = -4;
                    blindaffect.displayName = "Blinded";
                    blindaffect.endMessage = "You can see again.\n\r";
                    blindaffect.endMessageToRoom = "$n recovers their sight.\n\r";
                    blindaffect.affectType = AffectTypes.Skill;

                    victim.AffectToChar(blindaffect);
                    ch.Act("$n spits venom at $N, getting some in $S eyes.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("You spit venom at $N, getting some in $S eyes.", victim, type: ActType.ToChar);
                    victim.Act("You are blinded by $N's venom!\n\r", ch);
                    victim.Act("$n appears to be blinded by $N's venom!", ch, null, null, ActType.ToRoomNotVictim);
                }
            }
            else
            {
                ch.Act("$n spits venom at $N.", victim, type: ActType.ToRoom);
                ch.Act("You spit venom at $N.", victim, type: ActType.ToChar);
            }
            ch.CheckImprove(skill, true, 1);

            dam = Utility.Random(dam_each[level], dam_each[level] * 2);
            Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            if (!victim.IsAffected(venom))
            {
                var affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = venom;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Strength;
                affect.duration = 6;
                affect.modifier = -5;
                affect.displayName = "Venom Poison";
                affect.endMessage = "You feel less sick.\n\r";
                affect.endMessageToRoom = "$n is looking less sick.\n\r";
                affect.affectType = AffectTypes.Skill;

                victim.AffectToChar(affect);
                victim.send("You feel very sick.\n\r");
                victim.Act("$n looks very ill.", null, null, null, ActType.ToRoom);
            }

            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = true;
            aff.frequency = Frequency.Tick;
            aff.where = AffectWhere.ToAffects;
            aff.displayName = "venom spit";
            aff.duration = 6;
            aff.endMessage = "You feel ready to spit venom again.";
            ch.AffectToChar(aff);
        }
        public static void DoShootBlood(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("shoot blood");
            int chance;
            var level = ch.Level;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    30,
                    50,
                    80,
                    100
                };
                level = 3 - (int)ch.Form.Tier;
            }

            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to shoot blood again!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            if (chance > Utility.NumberPercent())
            {
                if ((victim.ImmuneFlags.ISSET(WeaponDamageTypes.Blind) || (victim.Form != null && victim.Form.ImmuneFlags.ISSET(WeaponDamageTypes.Blind))) && !victim.IsAffected(AffectFlags.Deafen))
                    victim.Act("$n is immune to blindness.", type: ActType.ToRoom);
                else
                {
                    ch.Act("$n shoots blood at $N' eyes.", victim, type: ActType.ToRoom);
                    ch.Act("You shoot blood at $N's eyes.", victim, type: ActType.ToChar);
                    var blindaffect = new AffectData();
                    blindaffect.ownerName = ch.Name;
                    blindaffect.skillSpell = skill;
                    blindaffect.level = ch.Level;
                    blindaffect.where = AffectWhere.ToAffects;
                    blindaffect.location = ApplyTypes.Hitroll;
                    blindaffect.flags.Add(AffectFlags.Blind);
                    blindaffect.duration = 3;
                    blindaffect.modifier = -4;
                    blindaffect.displayName = "Blinded";
                    blindaffect.endMessage = "You can see again.\n\r";
                    blindaffect.endMessageToRoom = "$n recovers their sight.\n\r";
                    blindaffect.affectType = AffectTypes.Skill;

                    victim.AffectToChar(blindaffect);
                    victim.send("You are blinded!\n\r");
                    victim.Act("$n appears to be blinded!", null, null, null, ActType.ToRoom);
                }
            }
            else
            {
                ch.Act("$n shoots blood at $N's eyes, but misses.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n shoots blood at your eyes, but misses.", victim, type: ActType.ToVictim);
                ch.Act("You shoot blood at $N's eyes, but miss.", victim, type: ActType.ToChar);
            }

            //dam = utility.rand(dam_each[level], dam_each[level] * 2);
            //Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = true;
            aff.frequency = Frequency.Tick;
            aff.where = AffectWhere.ToAffects;
            aff.displayName = "shoot blood";
            aff.duration = 5;
            aff.endMessage = "You feel ready to shoot blood again.";
            ch.AffectToChar(aff);
        }
        public static void DoZigzag(Character ch, string arguments)
        {
            Character victim;

            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("zigzag");
            int chance;
            if ((chance = skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to mislead your foes.\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            AffectData aff;
            if ((aff = victim.FindAffect(skill)) != null)
            {
                ch.send("They are already being misled.\n\r");
                return;
            }

            if (CheckIsSafe(ch, victim)) return;

            ch.WaitState(skill.waitTime);

            /* stats */
            chance += ch.GetCurrentStat(PhysicalStatTypes.Dexterity);
            chance -= 2 * victim.GetCurrentStat(PhysicalStatTypes.Dexterity);

            /* speed  */
            if (ch.Flags.ISSET(ActFlags.Fast) || ch.IsAffected(AffectFlags.Haste))
                chance += 10;
            if (victim.Flags.ISSET(ActFlags.Fast) || victim.IsAffected(AffectFlags.Haste))
                chance -= 30;

            /* level */
            chance += (ch.Level - victim.Level) * 2;

            /* sloppy hack to prevent false zeroes */
            if (chance % 5 == 0)
                chance += 1;


            if (chance > Utility.NumberPercent())
            {

                ch.Act("$N is misled by $n's zig and zag!", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n zigs and zags and distracts your next attack.", victim, type: ActType.ToVictim);
                ch.Act("You distract $N with a zigzag.", victim, type: ActType.ToChar);

                AffectData newAffect = new AffectData();
                newAffect.flags.SETBIT(AffectFlags.ZigZagFeint);
                newAffect.duration = 2;
                newAffect.frequency = Frequency.Violence;
                newAffect.displayName = "zigzag";
                newAffect.modifier = 0;
                newAffect.location = ApplyTypes.None;
                newAffect.skillSpell = skill;
                newAffect.affectType = AffectTypes.Skill;
                victim.AffectToChar(newAffect);

                AffectData existingAffect = (from a in victim.AffectsList where a.skillSpell == skill && a.flags.Count == 0 select a).FirstOrDefault();

                if (existingAffect != null)
                {
                    victim.AffectApply(existingAffect, true, true);
                    existingAffect.duration = 6;
                    existingAffect.modifier -= 1;
                    victim.AffectApply(existingAffect, false, true);
                }
                else
                {
                    newAffect.frequency = Frequency.Tick;
                    newAffect.flags.Clear();
                    newAffect.duration = 6;
                    newAffect.location = ApplyTypes.Dex;
                    newAffect.modifier = -1;
                    victim.AffectToChar(newAffect);
                }
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.Act("$n fails to mislead $N with $s zigzag!", type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to distract you, but fails to mislead your next attack with a zigzag.", victim, type: ActType.ToVictim);
                ch.Act("You fail to distract $N from their next attack with a zigzag.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoFlight(Character ch, string arguments)
        {
            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("flight");
            int chance;
            if ((chance = skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to fly.\n\r");
                return;
            }

            AffectData aff;
            if (ch.IsAffected(AffectFlags.Flying) || (aff = ch.FindAffect(skill)) != null)
            {
                ch.send("You are already flying.\n\r");
                return;
            }


            ch.WaitState(skill.waitTime);

            //if (chance > utility.number_percent())
            {

                ch.Act("$n leaps into the air and uses $s small wings to fly for a short time.", type: ActType.ToRoom);
                ch.Act("You leap into the air and use your small wings to fly for a short time.", type: ActType.ToChar);

                AffectData newAffect = new AffectData();

                newAffect.duration = 2;
                newAffect.frequency = Frequency.Tick;
                newAffect.displayName = "flight";
                newAffect.modifier = 0;
                newAffect.location = ApplyTypes.None;
                newAffect.skillSpell = skill;
                newAffect.affectType = AffectTypes.Skill;
                newAffect.flags.SETBIT(AffectFlags.Flying);
                ch.AffectToChar(newAffect);

                ch.CheckImprove(skill, true, 1);
            }

        }

        public static void DoGlandSpray(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("gland spray");
            int chance;
            int dam;
            var level = ch.Level;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    30,
                    50,
                    80,
                    100
                };
                level = 3 - (int)ch.Form.Tier;
            }

            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to spray from your glands again!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            if (Utility.Random(1, 10) <= 3)
            {
                if ((victim.ImmuneFlags.ISSET(WeaponDamageTypes.Blind) || (victim.Form != null && victim.Form.ImmuneFlags.ISSET(WeaponDamageTypes.Blind))) && !victim.IsAffected(AffectFlags.Deafen))
                    victim.Act("$n is immune to blindness.", type: ActType.ToRoom);
                else
                {
                    ch.Act("$n sprays sulfurous and pungent chemicals from $s anal glands, getting some in $N's eyes.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n sprays sulfurous and pungent chemicals from $s anal glands, getting some in your eyes.", victim, type: ActType.ToVictim);
                    ch.Act("You spray sulfurous and pungent chemicals from your anal glands, getting some in $N's eyes.", victim, type: ActType.ToChar);
                    var blindaffect = new AffectData();
                    blindaffect.ownerName = ch.Name;
                    blindaffect.skillSpell = skill;
                    blindaffect.level = ch.Level;
                    blindaffect.where = AffectWhere.ToAffects;
                    blindaffect.location = ApplyTypes.Hitroll;
                    blindaffect.flags.Add(AffectFlags.Blind);
                    blindaffect.duration = 2;
                    blindaffect.modifier = -4;
                    blindaffect.displayName = "Blinded";
                    blindaffect.endMessage = "You can see again.\n\r";
                    blindaffect.endMessageToRoom = "$n recovers their sight.\n\r";
                    blindaffect.affectType = AffectTypes.Skill;

                    victim.AffectToChar(blindaffect);
                    victim.send("You are blinded!\n\r");
                    victim.Act("$n appears to be blinded!", null, null, null, ActType.ToRoom);
                }
            }
            else
            {
                ch.Act("$n sprays sulfurous and pungent chemicals from $s anal glands at $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n sprays sulfurous and pungent chemicals from $s anal glands at you.", victim, type: ActType.ToVictim);
                ch.Act("You spray sulfurous and pungent chemicals from your anal glands at $N.", victim, type: ActType.ToChar);
            }
            ch.CheckImprove(skill, true, 1);


            dam = Utility.Random(dam_each[level], dam_each[level] * 2);
            Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            var affect = new AffectData();
            affect.ownerName = ch.Name;
            affect.skillSpell = SkillSpell.SkillLookup("faerie fire");
            affect.level = ch.Level;
            affect.where = AffectWhere.ToAffects;
            affect.location = ApplyTypes.AC;
            affect.flags.Add(AffectFlags.FaerieFire);
            affect.duration = 6;
            affect.modifier = 20;
            affect.displayName = "sprayed";
            affect.endMessage = "Your stench wanes.\n\r";
            affect.endMessageToRoom = "$n's stench wanes.\n\r";
            affect.affectType = AffectTypes.Skill;

            victim.AffectToChar(affect);

            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = true;
            aff.frequency = Frequency.Tick;
            aff.where = AffectWhere.ToAffects;
            aff.displayName = "gland spray";
            aff.duration = 6;
            aff.endMessage = "You feel ready to spray from your stink glands again.";
            ch.AffectToChar(aff);
        }

        public static void DoVenomStrike(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("venom strike");
            var venomstrikepoison = SkillSpell.SkillLookup("poisonous venom strike");
            int chance;
            int dam;
            var level = ch.Level;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    30,
                    50,
                    80,
                    100
                };
                level = 3 - (int)ch.Form.Tier;
            }

            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to venom strike again!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);


            ch.Act("$n strikes quickly, injecting $N with subduing poison.", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n strikes quickly, injecting you with subduing poison.", victim, type: ActType.ToVictim);
            ch.Act("You strike quickly, injecting $N with subduing poison.", victim, type: ActType.ToChar);

            ch.CheckImprove(skill, true, 1);


            dam = Utility.Random(dam_each[level], dam_each[level] * 2);
            Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            if (!ch.IsAffected(venomstrikepoison))
            {
                var affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = venomstrikepoison;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 6;
                affect.modifier = -10;
                affect.displayName = "venom strike";
                affect.endMessage = "You feel less sick.\n\r";
                affect.endMessageToRoom = "$n appears less sick.\n\r";
                affect.affectType = AffectTypes.Skill;

                victim.AffectToChar(affect);
            }

            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = true;
            aff.frequency = Frequency.Violence;
            aff.where = AffectWhere.ToAffects;
            aff.displayName = "venom strike";
            aff.duration = 3;
            aff.endMessage = "You feel ready to venom strike again.";
            ch.AffectToChar(aff);
        }
        public static void DoVenomousSting(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                0,
                5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
                31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
                59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
                66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
                74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
                95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("venomous sting");
            var venomstingpoison = SkillSpell.SkillLookup("poisonous venom strike");
            int chance;
            int dam;
            var level = ch.Level;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    30,
                    50,
                    80,
                    100
                };
                level = 3 - (int)ch.Form.Tier;
            }

            Character victim = null;

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            ch.Act("$n strikes quickly, stinging $N with poison.", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n strikes quickly, stinging you with poison.", victim, type: ActType.ToVictim);
            ch.Act("You strike quickly, stinging $N with poison.", victim, type: ActType.ToChar);

            ch.CheckImprove(skill, true, 1);

            dam = Utility.Random(dam_each[level], dam_each[level] * 2);
            Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Sting);

            if (!ch.IsAffected(venomstingpoison))
            {
                var affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = venomstingpoison;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 6;
                affect.modifier = -10;
                affect.displayName = "venomous sting";
                affect.endMessage = "You feel less sick.\n\r";
                affect.endMessageToRoom = "$n appears less sick.\n\r";
                affect.affectType = AffectTypes.Skill;

                victim.AffectToChar(affect);
            }

            //var aff = new AffectData();
            //aff.skillSpell = skill;
            //aff.hidden = true;
            //aff.frequency = Frequency.Violence;
            //aff.where = AffectWhere.ToAffects;
            //aff.displayName = "venomous sting";
            //aff.duration = 3;
            //aff.endMessage = "You feel ready to venomous sting again.";
            //ch.AffectToChar(aff);
        }


        public static void DoSecreteFilament(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("secreted filament");
            int chance;
            int dam;
            var level = ch.Level;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    30,
                    50,
                    80,
                    100
                };
                level = 3 - (int)ch.Form.Tier;
            }

            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to secrete filament again!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);


            ch.Act("$n sprays $N with a secreted filament.", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n sprays you with a secreted filament.", victim, type: ActType.ToVictim);
            ch.Act("You spray $N with a secreted filament.", victim, type: ActType.ToChar);

            ch.CheckImprove(skill, true, 1);


            dam = Utility.Random(dam_each[level], dam_each[level] * 2);
            Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            if (!ch.IsAffected(AffectFlags.Poison))
            {
                var affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 6;
                affect.modifier = -5;
                affect.displayName = "secreted filament";
                affect.endMessage = "You escape the secreted filament.\n\r";
                affect.endMessageToRoom = "$n escapes the secreted filament.\n\r";
                affect.affectType = AffectTypes.Skill;

                victim.AffectToChar(affect);

                affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.DamageRoll;
                affect.duration = 6;
                affect.modifier = -5;
                affect.displayName = "secreted filament";

                affect.affectType = AffectTypes.Skill;

                victim.AffectToChar(affect);
            }

            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = true;
            aff.frequency = Frequency.Violence;
            aff.where = AffectWhere.ToAffects;
            aff.displayName = "secreted filament";
            aff.duration = 3;
            aff.endMessage = "You feel ready to secrete filament again.";
            ch.AffectToChar(aff);
        }


        public static void DoAcidExcrete(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("acid excrete");
            int chance;
            int dam;
            var level = ch.Level;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    30,
                    50,
                    80,
                    100
                };
                level = 3 - (int)ch.Form.Tier;
            }

            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to excete acid again!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);


            ch.Act("$n directs an acetic stream of acid at $N from $s pedicel.", victim, type: ActType.ToRoomNotVictim);
            ch.Act("$n directs an acetic stream of acid at you from $s pedicel.", victim, type: ActType.ToVictim);
            ch.Act("You direct an acetic stream of acid at $N from your pedicel.", victim, type: ActType.ToChar);

            ch.CheckImprove(skill, true, 1);


            dam = Utility.Random(dam_each[level], dam_each[level] * 2);
            Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Acid);

            bool found = false;

            foreach (var existingaff in victim.AffectsList)
            {
                if (existingaff.skillSpell != skill)
                    continue;

                if (existingaff.location == ApplyTypes.Strength || existingaff.location == ApplyTypes.Dexterity)
                {
                    existingaff.modifier -= 2;
                    existingaff.duration = 6;
                    found = true;
                }
            }

            if (!found)
            {
                var affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Strength;
                affect.duration = 6;
                affect.modifier = -2;
                affect.displayName = "excreted acid";
                affect.endMessage = "The burning lessens.\n\r";
                affect.endMessageToRoom = "$n looks relieved from the excreted acid.\n\r";
                affect.affectType = AffectTypes.Skill;

                victim.AffectToChar(affect);

                affect = new AffectData();
                affect.ownerName = ch.Name;
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Dexterity;
                affect.duration = 6;
                affect.modifier = -2;
                affect.displayName = "excreted acid";

                affect.affectType = AffectTypes.Skill;

                victim.AffectToChar(affect);
            }

            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = true;
            aff.frequency = Frequency.Violence;
            aff.where = AffectWhere.ToAffects;
            aff.displayName = "excreted acid";
            aff.duration = 3;
            aff.endMessage = "You feel ready to excrete acid again.";
            ch.AffectToChar(aff);
        }

        public static void AcidExcreteTick(Character ch, AffectData affect)
        {
            if (affect.hidden)
            {
                var skill = SkillSpell.SkillLookup("acid excrete");
                int chance;
                int dam;
                var level = ch.Level;

                if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
                    return;

                if (ch.Form != null)
                {
                    var dam_each = new int[]
                    {
                    30,
                    50,
                    80,
                    100
                    };
                    level = 3 - (int)ch.Form.Tier;


                    Character victim = null;




                    if ((victim = ch.Fighting) == null)
                    {
                        return;
                    }

                    if (!victim.IsAffected(skill))
                    {
                        ch.send("You aren't ready to excete acid again!\n\r");
                        return;
                    }

                    if (ch.IsNPC)
                        level = Math.Min(level, 51);
                    level = Math.Min(level, dam_each.Length - 1);
                    level = Math.Max(0, level);
                    bool found = false;

                    foreach (var existingaff in victim.AffectsList)
                    {
                        if (existingaff.skillSpell != skill)
                            continue;

                        if (existingaff.location == ApplyTypes.Strength || existingaff.location == ApplyTypes.Dexterity)
                        {
                            existingaff.modifier -= 2;
                            existingaff.duration = 6;
                            found = true;
                        }
                    }


                    if (found)
                    {
                        ch.Act("$n directs an acetic stream of acid at $N from $s pedicel.", victim, type: ActType.ToRoomNotVictim);
                        ch.Act("$n directs an acetic stream of acid at you from $s pedicel.", victim, type: ActType.ToVictim);
                        ch.Act("You direct an acetic stream of acid at $N from your pedicel.", victim, type: ActType.ToChar);
                        dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                        Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Acid);

                    }
                }

            }
        }

        public static void DoHerbs(Character ch, string argument)
        {
            AffectData af;
            var skill = SkillSpell.SkillLookup("herbs");

            if (ch.GetSkillPercentage(skill) <= 1)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if (ch.IsAffected(skill))
            {
                ch.send("You can't search for more herbs yet.\n\r");
                return;
            }

            if (ch.Room == null || !ch.Room.IsWilderness)
            {
                ch.send("You must be in the wilderness to find herbs.\n\r");
                return;
            }

            Character victim;

            if (argument.ISEMPTY())
                victim = ch;
            else if ((victim = ch.GetCharacterFromRoomByName(argument)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            if (Utility.NumberPercent() > ch.GetSkillPercentage(skill))
            {
                ch.send("You fail to find any herbs here.\n\r");
                ch.Act("$n fails to find any herbs.", type: ActType.ToRoom);
                ch.CheckImprove(skill, false, 3);
                return;
            }

            if (victim == ch)
            {
                ch.Act("$n searches for and finds healing herbs.", type: ActType.ToRoom);
                ch.send("You search for and find healing herbs.\n\r");
                ch.send("You feel better.\n\r");
            }
            else
            {
                ch.Act("$n searches for and finds healing herbs and applies them to $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n searches for and finds healing herbs and applies them to you.", victim, type: ActType.ToVictim);
                ch.Act("You search for and find healing herbs and apply them to $N.\n\r", victim);
                victim.send("You feel better.\n\r");
            }
            victim.HitPoints += victim.MaxHitPoints / 4;
            victim.HitPoints = Math.Min(victim.HitPoints, victim.MaxHitPoints);

            if (Utility.NumberPercent() < Math.Max(1, ch.Level / 4) && victim.IsAffected(AffectFlags.Plague))
            {
                victim.AffectFromChar(victim.FindAffect(SkillSpell.SkillLookup("plague")));
                victim.Act("The sores on $n's body vanish.\n\r", type: ActType.ToRoom);
                victim.send("The sores on your body vanish.\n\r");
            }

            if (Utility.NumberPercent() < Math.Max(1, (ch.Level)) && ch.IsAffected(AffectFlags.Blind))
            {
                victim.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("blindness")));
                victim.send("Your vision returns!\n\r");
            }

            if (Utility.NumberPercent() < Math.Max(1, ch.Level / 2) && victim.IsAffected(AffectFlags.Poison))
            {
                victim.AffectFromChar(victim.FindAffect(SkillSpell.SkillLookup("poison")));
                victim.send("A warm feeling goes through your body.\n\r");
                victim.Act("$n looks better.", type: ActType.ToRoom);
            }
            ch.CheckImprove(skill, true, 3);

            af = new AffectData();

            af.where = AffectWhere.ToAffects;
            af.skillSpell = skill;
            af.location = 0;
            af.duration = 2;
            af.modifier = 0;
            af.level = ch.Level;
            af.affectType = AffectTypes.Skill;
            af.displayName = "herbs";
            af.endMessage = "You feel ready to search for herbs once more.";
            ch.AffectToChar(af);
            ch.WaitState(skill.waitTime);
            return;
        }


        public static void DoSerpentStrike(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("serpent strike");
            int chance;
            int dam;
            var level = ch.Level;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            Character victim = null;

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n performs a serpent strike against $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n performs a serpent strike against you.", victim, type: ActType.ToVictim);
                ch.Act("You perform a serpent strike against $N.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);


                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Acid);
            }
            else
            {
                ch.Act("$n attempts to perform a serpent strike against $N, but fails.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to perform a serpent strike against you, but fails.", victim, type: ActType.ToVictim);
                ch.Act("You attempt to perform a serpent strike against $N, but fail.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);


                dam = 0;
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Acid);
            }
        }

        public static void DoWheelKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("wheel kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = (ch.Level) / 2;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);

                Damage(ch, victim, dam, skill);

                ch.CheckImprove(skill, true, 1);

            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoSweepKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("sweep kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = (ch.Level) / 2;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);

                victim.WaitState(game.PULSE_VIOLENCE);

                Damage(ch, victim, dam, skill);
                ch.CheckImprove(skill, true, 1);

            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoSideKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("side kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = (ch.Level) / 2;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);
                dam = dam * 5 / 4; //adding 25% to previous kick

                Damage(ch, victim, dam, skill);
                ch.CheckImprove(skill, true, 1);

            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoScissorsKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("scissors kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = (ch.Level) / 2;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);
                dam = dam * 3 / 2;

                victim.WaitState(game.PULSE_VIOLENCE);

                Damage(ch, victim, dam, skill);
                ch.CheckImprove(skill, true, 1);

            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoAssassinMuleKick(Character ch, string arguments)
        {
            Character victim = null;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("mule kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            foreach (var other in ch.Room.Characters)
            {
                if (other.Fighting == ch && ch.Fighting != other)
                {
                    victim = other;
                    break;
                }
            }

            if (victim == null)
            {
                ch.send("There is no one fighting you from behind or the side.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = (ch.Level) / 2;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);
                dam = dam * 9 / 5;

                Damage(ch, victim, dam, skill);
                ch.CheckImprove(skill, true, 1);

            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoCrescentKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("crescent kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = ch.Level;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);
                dam = dam * 2;


                Damage(ch, victim, dam, skill);
                ch.CheckImprove(skill, true, 1);

            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoAxeKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("axe kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = ch.Level;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);
                dam = dam * 9 / 4;


                Damage(ch, victim, dam, skill);
                ch.CheckImprove(skill, true, 1);

                if (!victim.IsAffected(skill))
                {
                    var affect = new AffectData();
                    affect.displayName = "axe kick";
                    affect.affectType = AffectTypes.Skill;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Strength;
                    affect.duration = 5;
                    affect.modifier = -5;
                    ch.AffectToChar(affect);

                    affect.location = ApplyTypes.Dexterity;
                    affect.endMessage = "Your shoulder feels better.";
                    affect.endMessageToRoom = "$n's shoulder looks better.";
                    ch.AffectToChar(affect);

                }
            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoMountainStormKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("mountain storm kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = ch.Level;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);
                dam = dam * 5 / 2;

                victim.WaitState(game.PULSE_VIOLENCE);

                Damage(ch, victim, dam, skill);
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoDoubleSpinKick(Character ch, string arguments)
        {
            Character victim;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("double spin kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = ch.Level;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);
                dam = dam * 11 / 4;


                Damage(ch, victim, dam, skill);

                dam = ch.Level;
                dam += Utility.Random(ch.Level / 2, ch.Level);
                dam += Utility.Random(ch.Level / 5, ch.Level / 4);
                dam = dam * 11 / 4;

                Damage(ch, victim, dam, skill);

                ch.CheckImprove(skill, true, 1);


            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoRisingPhoenixKick(Character ch, string arguments)
        {
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("rising phoenix kick");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if (ch.Room.Characters.Count <= 1)
            {
                ch.send("There is no one here to fight.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                foreach (var victim in ch.Room.Characters.ToArray())
                {
                    if (victim != ch && !victim.IsSameGroup(ch))
                    {
                        dam = ch.Level;
                        dam += Utility.Random(ch.Level / 2, ch.Level);
                        dam += Utility.Random(ch.Level / 5, ch.Level / 4);
                        dam = dam * 3;

                        victim.WaitState(game.PULSE_VIOLENCE);

                        Damage(ch, victim, dam, skill);
                    }
                }
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.Act("Your attempt to Rising Pheonix Kick failed");
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoCaltraps(Character ch, string arguments)
        {
            Character victim = null;
            int dam;
            int skillPercent = 0;

            var skill = SkillSpell.SkillLookup("caltraps");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            if (victim == null)
            {
                ch.send("You have no victim.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (skillPercent > Utility.NumberPercent())
            {
                dam = Utility.dice(2, 5, 15);

                Damage(ch, victim, dam, skill);

                ch.CheckImprove(skill, true, 1);

                if (!victim.IsAffected(skill))
                {
                    var affect = new AffectData();
                    affect.displayName = "caltraps";
                    affect.affectType = AffectTypes.Skill;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Dexterity;
                    affect.duration = 3;
                    affect.modifier = -3;
                    affect.endMessage = "Your feet feel better.";
                    affect.endMessageToRoom = "$n stops limping.";
                    victim.AffectToChar(affect);

                }
            }
            else
            {
                Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }

        public static void DoPierce(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("pierce");
            int chance;
            int dam;
            var level = ch.Level;
            ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Spear)
            {
                ch.send("You must be wielding a spear to pierce your enemy.");
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
                ch.Act("$n pierces $N, leaving behind a painful injury.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n pierces you, leaving behind an painful injury.", victim, type: ActType.ToVictim);
                ch.Act("You pierce $N, leaving behind a painful injury.", victim, type: ActType.ToChar);

                dam = wield.DamageDice.Roll() + ch.DamageRoll;
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

                if (!victim.IsAffected(skill))
                {
                    var aff = new AffectData();
                    aff.skillSpell = skill;
                    aff.duration = Utility.Random(5, 10);
                    aff.endMessage = "Your injury feels better.";
                    aff.endMessageToRoom = "$n recovers from $s injury.";
                    aff.ownerName = ch.Name;
                    aff.level = ch.Level;
                    aff.modifier = -4;
                    aff.location = ApplyTypes.Strength;
                    aff.affectType = AffectTypes.Skill;
                    aff.displayName = "pierce";
                    aff.where = AffectWhere.ToAffects;

                    victim.AffectToChar(aff);
                }
            }
            else
            {
                ch.Act("$n attempts to pierce $N but doesn't connect.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to pierce you!", victim, type: ActType.ToVictim);
                ch.Act("You try to pierce $N but fail to connect.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoThrust(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("thrust");
            int chance;
            int dam;
            var level = ch.Level;
            ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || (wield.WeaponType != WeaponTypes.Spear && wield.WeaponType != WeaponTypes.Polearm))
            {
                ch.send("You must be wielding a spear or polearm to thrust at your enemy.");
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
                ch.Act("$n thrusts at $N, knocking $M back.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n thrusts at you, knocking you back.", victim, type: ActType.ToVictim);
                ch.Act("You thrust at $N, knocking $M back.", victim, type: ActType.ToChar);

                dam = (wield.DamageDice.Roll() + ch.DamageRoll) * 2;
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

                if (!victim.IsAffected(skill))
                {
                    var aff = new AffectData();
                    aff.skillSpell = skill;
                    aff.duration = 2;
                    //aff.endMessage = "Your injury feels better.";
                    //aff.endMessageToRoom = "$n recovers from $s injury.";
                    aff.ownerName = ch.Name;
                    aff.level = ch.Level;
                    aff.affectType = AffectTypes.Skill;
                    aff.hidden = true;
                    aff.frequency = Frequency.Violence;
                    aff.displayName = "thrust";
                    aff.where = AffectWhere.ToAffects;

                    victim.AffectToChar(aff);
                }
            }
            else
            {
                ch.Act("$n attempts to thrust at $N but doesn't manage to push $M back.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to thrust at you!", victim, type: ActType.ToVictim);
                ch.Act("You try to thrust at $N but fail to knock $M back.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        public static void DoSlice(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("slice");
            int chance;
            int dam;
            var level = ch.Level;
            ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || (wield.WeaponType != WeaponTypes.Polearm && wield.WeaponType != WeaponTypes.Axe))
            {
                ch.send("You must be wielding a polearm or axe to slice your enemy.");
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
                ch.Act("$n slices $N with $p, leaving behind a bleeding wound.", victim, wield, type: ActType.ToRoomNotVictim);
                ch.Act("$n slices you with $p, leaving behind a bleeding wound.", victim, wield, type: ActType.ToVictim);
                ch.Act("You slice $N with $p, leaving behind a bleeding wound.", victim, wield, type: ActType.ToChar);

                dam = wield.DamageDice.Roll() + ch.DamageRoll;
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);
                var skBleed = SkillSpell.SkillLookup("bleeding");

                if (!victim.IsAffected(skBleed))
                {
                    var aff = new AffectData();
                    aff.skillSpell = skBleed;
                    aff.duration = 4;
                    aff.endMessage = "You stop bleeding.";
                    aff.endMessageToRoom = "$n stops bleeding.";
                    aff.ownerName = ch.Name;
                    aff.level = ch.Level;
                    aff.affectType = AffectTypes.Skill;

                    aff.frequency = Frequency.Tick;
                    aff.displayName = "slice";
                    aff.where = AffectWhere.ToAffects;

                    victim.AffectToChar(aff);
                }
            }
            else
            {
                ch.Act("$n attempts to slice $N with $p but doesn't connect.", victim, wield, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to slice you with $p!", victim, wield, type: ActType.ToVictim);
                ch.Act("You try to slice $N with $p but don't connect.", victim, wield, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }

        //public static void DoWhirl(Character ch, string arguments)
        //{
        //    var skill = SkillSpell.SkillLookup("whirl");
        //    int chance;
        //    float dam;
        //    var level = ch.Level;
        //    ItemData wield = null;

        //    if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
        //    {
        //        if (!ch.CheckSocials("whirl", arguments))
        //            ch.send("You don't know how to do that.\n\r");
        //        return;
        //    }

        //    if (((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Axe) &&
        //        ((wield = ch.GetEquipment(WearSlotIDs.DualWield)) == null || wield.WeaponType != WeaponTypes.Axe))
        //    {
        //        //if (!ch.CheckSocials("whirl", arguments))
        //            ch.send("You must be wielding an axe to whirl at your enemy.");
        //        return;
        //    }

        //    Character victim = null;

        //    if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
        //    {
        //        ch.send("You aren't fighting anyone.\n\r");
        //        return;
        //    }
        //    else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
        //    {
        //        ch.send("You don't see them here.\n\r");
        //        return;
        //    }

        //    ch.WaitState(skill.waitTime);
        //    if (chance > Utility.NumberPercent())
        //    {
        //        ch.Act("$n whirls $p at $N, leaving behind a painful injury.", victim, wield, type: ActType.ToRoomNotVictim);
        //        ch.Act("$n whirls $p at you, leaving behind an painful injury.", victim, wield, type: ActType.ToVictim);
        //        ch.Act("You whirl $p at $N, leaving behind a painful injury.", victim, wield, type: ActType.ToChar);

        //        dam = wield.DamageDice.Roll() + ch.DamageRoll;

        //        ch.CheckImprove(skill, true, 1);
        //        Combat.Damage(ch, victim, (int) dam, skill, WeaponDamageTypes.Pierce);

        //        if (!victim.IsAffected(skill))
        //        {
        //            var aff = new AffectData();
        //            aff.skillSpell = skill;
        //            aff.duration = 4;
        //            aff.ownerName = ch.Name;
        //            aff.level = ch.Level;
        //            aff.modifier = -4;
        //            aff.location = ApplyTypes.Strength;
        //            aff.affectType = AffectTypes.Skill;
        //            aff.displayName = "whirl";
        //            aff.where = AffectWhere.ToAffects;

        //            victim.AffectToChar(aff);

        //            aff.location = ApplyTypes.Dexterity;
        //            aff.endMessage = "Your injury feels better.";
        //            aff.endMessageToRoom = "$n recovers from $s injury.";
        //            victim.AffectToChar(aff);
        //        }
        //    }
        //    else
        //    {
        //        ch.Act("$n attempts to whirl $p at $N but doesn't connect.", victim, wield, type: ActType.ToRoomNotVictim);
        //        ch.Act("$n tries to whirl $p at you!", victim, wield, type: ActType.ToVictim);
        //        ch.Act("You try to whirl $p at $N but fail to connect.", victim, wield, type: ActType.ToChar);

        //        ch.CheckImprove(skill, false, 1);
        //        Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
        //    }
        //    return;
        //}

        public static void DoStrangle(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("strangle");
            int chance;
            int dam;
            Character victim = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (arguments.ISEMPTY())
            {
                ch.send("Strangle who?\n\r");
                return;
            }
            else if (ch.Fighting != null)
            {
                ch.send("You're too busy fighting.\n\r");
                return;
            }
            else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (ch == victim)
            {
                ch.send("You can't strangle yourself.\n\r");
            }
            else if (victim is NPCData && ((NPCData)victim).Protects.Any())
            {
                ch.send("You can't sneak up on them.\n\r");
            }
            else if (victim.IsAffected(AffectFlags.Sleep))
            {
                ch.send("They are already asleep.\n\r");
                return;
            }
            else
            {
                ch.WaitState(skill.waitTime);
                if (chance > Utility.NumberPercent())
                {
                    ch.Act("$n strangles $N.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n strangles you.", victim, type: ActType.ToVictim);
                    ch.Act("You strangle $N.", victim, type: ActType.ToChar);

                    StopFighting(victim, true);
                    victim.Position = Positions.Sleeping;
                    var affect = new AffectData();
                    affect.displayName = "strangle";
                    affect.flags.SETBIT(AffectFlags.Sleep);
                    affect.duration = 3;
                    affect.where = AffectWhere.ToAffects;
                    affect.skillSpell = skill;
                    affect.endMessage = "You feel able to wake yourself up.";

                    victim.AffectToChar(affect);

                    ch.CheckImprove(skill, true, 1);
                }
                else
                {
                    ch.Act("$n attempts to strangle $N.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n tries to strangle you!", victim, type: ActType.ToVictim);
                    ch.Act("You try to strangle $N.", victim, type: ActType.ToChar);

                    ch.CheckImprove(skill, false, 1);
                    dam = Utility.Random(10, ch.Level);
                    Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
                }
            }
            return;
        }

        public static void StrangleEnds(Character ch, AffectData affect)
        {
            if (ch.IsNPC)
            {
                Character.DoStand(ch, "");
            }
        }

        public static void DoBlindnessDust(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("blindness dust");
            int chance;


            if ((chance = ch.GetSkillPercentage(skill)) + 10 <= 11)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            ch.Act("$n throws some blinding dust.", type: ActType.ToRoom);
            ch.Act("You throw some blinding dust.", type: ActType.ToChar);

            if (chance <= Utility.NumberPercent())
            {
                ch.CheckImprove(skill, false, 1);
                ch.Act("The blinding dust drifts away harmlessly.");
                ch.Act("The blinding dust drifts away harmlessly.", type: ActType.ToRoom);
                return;
            }

            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (victim.IsSameGroup(ch))
                    continue;
                if (CheckIsSafe(ch, victim))
                    continue;
                if ((victim.ImmuneFlags.ISSET(WeaponDamageTypes.Blind) || (victim.Form != null && victim.Form.ImmuneFlags.ISSET(WeaponDamageTypes.Blind))) && !victim.IsAffected(AffectFlags.Deafen))
                {
                    victim.Act("$n is immune to blindness.", type: ActType.ToRoom);
                    continue;
                }
                if (victim.IsAffected(AffectFlags.Blind))
                {
                    ch.Act("$N is already blind.", victim);
                }

                if (chance > Utility.NumberPercent())
                {
                    var affect = new AffectData();
                    affect.displayName = "blindness dust";
                    affect.duration = 3;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Hitroll;
                    affect.modifier = -4;
                    affect.skillSpell = skill;
                    affect.flags.SETBIT(AffectFlags.Blind);
                    affect.endMessage = "You can see again.";
                    affect.endMessageToRoom = "$n wipes the blinding dust out of $s eyes.";
                    victim.AffectToChar(affect);
                    victim.Act("$n is blinded by dust in their eyes!", type: ActType.ToRoom);
                }
                if (victim.Fighting == null)
                    Combat.multiHit(victim, ch);
            }
            ch.CheckImprove(skill, true, 1);

            return;
        }

        public static void DoPoisonDust(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("poison dust");
            int chance;


            if ((chance = ch.GetSkillPercentage(skill)) + 10 <= 11)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            ch.Act("$n throws some poison dust.", type: ActType.ToRoom);
            ch.Act("You throw some poison dust.", type: ActType.ToChar);

            if (chance <= Utility.NumberPercent())
            {
                ch.CheckImprove(skill, false, 1);
                ch.Act("The poison dust drifts away harmlessly.");
                ch.Act("The poison dust drifts away harmlessly.", type: ActType.ToRoom);
                return;
            }

            var skPoison = SkillSpell.SkillLookup("poison");

            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (victim.IsSameGroup(ch))
                    continue;
                if (CheckIsSafe(ch, victim))
                    continue;
                if (victim.IsAffected(AffectFlags.Poison))
                {
                    ch.Act("$N is already poisoned.", victim);
                }

                if (chance > Utility.NumberPercent())
                {
                    var affect = new AffectData();
                    affect.displayName = "poison dust";
                    affect.duration = 3;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Hitroll;
                    affect.modifier = -4;
                    affect.skillSpell = skPoison;
                    affect.flags.SETBIT(AffectFlags.Poison);
                    affect.endMessage = "You feel better.";
                    affect.endMessageToRoom = "$n recovers from $s poison.";
                    victim.AffectToChar(affect);
                    victim.Act("$n is poisoned by inhaling poison dust!", type: ActType.ToRoom);
                }
                if (victim.Fighting == null)
                    Combat.multiHit(victim, ch);
            }
            ch.CheckImprove(skill, true, 1);

            return;
        }

        public static void DoPoisonDagger(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("poison dagger");
            int chance;


            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (!ItemTemplateData.Templates.TryGetValue(2966, out var ItemTemplate))
            {
                ch.send("You fail.\n\r");
                return;
            }
            ch.WaitState(skill.waitTime);
            ch.Act("$n crafts a poisonous dagger.", type: ActType.ToRoom);
            ch.Act("You craft a poisonous dagger.", type: ActType.ToChar);

            var dagger = new ItemData(ItemTemplate);

            var skPoison = SkillSpell.SkillLookup("poison");

            var affect = new AffectData();
            affect.duration = -1;
            affect.where = AffectWhere.ToWeapon;
            affect.skillSpell = skPoison;
            affect.flags.SETBIT(AffectFlags.Poison);
            dagger.affects.Add(affect);
            dagger.Level = ch.Level;
            dagger.DamageDice = new Dice(2, 6, 11);
            dagger.timer = 15;

            ch.AddInventoryItem(dagger);

            ch.CheckImprove(skill, true, 1);

            return;
        }
        public static void DoThrow(Character ch, string arguments)
        {
            Character victim = null;

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (CheckAcrobatics(ch, victim)) return;

            var skill = SkillSpell.SkillLookup("throw");
            ch.WaitState(skill.waitTime);
            AssassinThrow(ch, victim);
        }

        public static bool AssassinThrow(Character ch, Character victim)
        {
            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };
            var skill = SkillSpell.SkillLookup("throw");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return false;
            }

            int dam;
            var level = ch.Level;

            //chance += level / 10;

            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                ch.Act("$n throws $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n throws you!", victim, type: ActType.ToVictim);
                ch.Act("You throw $N.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);

                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
                victim.WaitState(game.PULSE_VIOLENCE);
                CheckGroundControl(victim);
                CheckCheapShot(victim);
                return true;
            }
            else
            {
                ch.Act("$n attempts to throw $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to throw you!", victim, type: ActType.ToVictim);
                ch.Act("You try to throw $N.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
            }
            return false;
        }

        private static void CheckGroundControl(Character victim)
        {
            if (victim.Room != null && victim.Position == Positions.Fighting)
                foreach (var other in victim.Room.Characters.ToArray())
                {
                    if (other.Fighting == victim)
                    {
                        CheckGroundControl(other, victim);
                    }
                }
        }

        private static void CheckCheapShot(Character victim)
        {
            if (victim.Room != null && victim.Position == Positions.Fighting)
                foreach (var other in victim.Room.Characters.ToArray())
                {
                    if (other.Fighting == victim)
                    {
                        CheckCheapShot(other, victim);
                    }
                }
        }

        private static void CheckGroundControl(Character ch, Character victim)
        {
            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };
            var skill = SkillSpell.SkillLookup("ground control");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                return;
            }

            int dam;
            var level = ch.Level;

            if (ch.Room != victim.Room || victim.Position != Positions.Fighting)
                return;
            //chance += level / 10;

            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                ch.Act("$n manipulates $N while they are on the ground.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n manipulates you while you are on the ground!", victim, type: ActType.ToVictim);
                ch.Act("You manipulate $N while they are on the ground.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);

                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
            }
        }

        public static void DoKotegaeshi(Character ch, string arguments)
        {
            Character victim = null;

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            var skill = SkillSpell.SkillLookup("kotegaeshi");
            ch.WaitState(skill.waitTime);
            DoAssassinKotegaeshi(ch, victim);
        }
        public static bool DoAssassinKotegaeshi(Character ch, Character victim)
        {
            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };
            var skill = SkillSpell.SkillLookup("kotegaeshi");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return false;
            }

            int dam;
            var level = ch.Level;


            //chance += level / 10;

            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                ch.Act("$n breaks $N's wrist with $s kotegaeshi.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n breaks your wrist with $s kotegaeshi!", victim, type: ActType.ToVictim);
                ch.Act("You break $N's wrist with your kotegaeshi.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);

                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
                if (!victim.IsAffected(skill))
                {
                    var affect = new AffectData();
                    affect.skillSpell = skill;
                    affect.displayName = skill.name;
                    affect.duration = 5;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Strength;
                    affect.modifier = -5;
                    affect.endMessage = "Your wrist feels better.";
                    affect.endMessageToRoom = "$n's wrist looks better.";
                    victim.AffectToChar(affect);
                }
                return true;
            }
            else
            {
                ch.Act("$n attempts to break $N's wrists with $s kotegaeshi.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to break your wrist with $s kotegaeshi!", victim, type: ActType.ToVictim);
                ch.Act("You attempt to break $N's wrist with your kotegaeshi.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
            }
            return false;
        }

        public static void DoKansetsuwaza(Character ch, string arguments)
        {
            Character victim = null;

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            var skill = SkillSpell.SkillLookup("kansetsuwaza");
            ch.WaitState(skill.waitTime);
            DoAssassinKansetsuwaza(ch, victim);
        }
        public static bool DoAssassinKansetsuwaza(Character ch, Character victim)
        {
            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };
            var skill = SkillSpell.SkillLookup("kansetsuwaza");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return false;
            }

            int dam;
            var level = ch.Level;

            //chance += level / 10;

            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                ch.Act("$n locks $N's elbow with $s kansetsuwaza.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n locks your elbow with $s kansetsuwaza!", victim, type: ActType.ToVictim);
                ch.Act("You lock $N's elbow with your kansetsuwaza.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);

                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);

                if (!victim.IsAffected(skill))
                {
                    var affect = new AffectData();
                    affect.skillSpell = skill;
                    affect.displayName = skill.name;
                    affect.duration = 5;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Strength;
                    affect.modifier = -4;
                    victim.AffectToChar(affect);

                    affect.location = ApplyTypes.Dexterity;
                    affect.modifier = -4;
                    affect.endMessage = "Your elbow feels better.";
                    affect.endMessageToRoom = "$n's elbow looks better.";
                    victim.AffectToChar(affect);
                }
                return true;
            }
            else
            {
                ch.Act("$n attempts to lock $N's elbow with $s kansetsuwaza.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to lock your elbow with $s kansetsuwaza!", victim, type: ActType.ToVictim);
                ch.Act("You attempt to lock $N's elbow with your kansetsuwaza.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
            }
            return false;
        }

        public static void DoVanish(Character ch, string arguments)
        {

            var skill = SkillSpell.SkillLookup("vanish");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }


            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {

                ch.Act("$n throws down a globe of dust, vanishing from sight.", type: ActType.ToRoom);
                ch.Act("You throw down a globe of dust, vanishing from sight.", type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);
                StopFighting(ch, true);

                if (ch.Room.Area.Rooms.Count > 1)
                {
                    int attempts = 0;
                    var newroom = ch.Room.Area.Rooms.Values.SelectRandom();
                    while ((newroom == null || newroom == ch.Room || newroom.flags.ISSET(RoomFlags.Indoors) || newroom.sector == SectorTypes.Inside) && attempts <= 10)
                    {
                        newroom = ch.Room.Area.Rooms.Values.SelectRandom();
                        attempts++;
                    }
                    if (attempts < 10)
                    {
                        ch.RemoveCharacterFromRoom();
                        ch.AddCharacterToRoom(newroom);
                        //Character.DoLook(ch, "auto");
                    }
                }

            }
            else
            {
                ch.Act("$n throws down a globe of dust, but nothing happens.", type: ActType.ToRoom);
                ch.Act("You throw down a globe of dust, but fail to vanish.", type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);

            }
            return;
        }

        public static void DoPugil(Character ch, string arguments)
        {
            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61
            };
            var skill = SkillSpell.SkillLookup("pugil");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 10) <= 11)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            int dam;
            var level = ch.Level;
            Character victim = null;

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            chance += level / 10;
            var wield = ch.GetEquipment(WearSlotIDs.Wield);

            if (wield == null || wield.WeaponType != WeaponTypes.Staff)
            {
                ch.send("You must use a staff to pugil someone.\n\r");
                ch.send("You must use a staff to pugil someone.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                var roll = wield.DamageDice.Roll();
                dam = Utility.Random(dam_each[level] + roll, dam_each[level] * 2 + roll);

                ch.Act("$n pugils $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                ch.Act("$n pugils you with $p!", victim, wield, type: ActType.ToVictim);
                ch.Act("You pugil $N with $p.", victim, wield, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, wield != null ? wield.WeaponDamageType.Type : WeaponDamageTypes.Bash);
            }
            else
            {
                ch.Act("$n attempts to pugil $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to pugil you with $p!", victim, wield, type: ActType.ToVictim);
                ch.Act("You attempt to pugil $N with $p.", victim, wield, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, wield != null ? wield.WeaponDamageType.Type : WeaponDamageTypes.Bash);
            }
            return;
        }

        public static void DoRoundhouse(Character ch, string arguments)
        {
            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };
            var skill = SkillSpell.SkillLookup("roundhouse");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 10) <= 11)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            int dam;
            var level = ch.Level;
            Character victim = null;

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            chance += level / 10;

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level] / 2, dam_each[level]);

                ch.Act("$n roundhouses $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n roundhouses you!", victim, type: ActType.ToVictim);
                ch.Act("You roundhouse $N.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
            }
            else
            {
                ch.Act("$n attempts to roundhouse $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to roundhouse you!", victim, type: ActType.ToVictim);
                ch.Act("You attempt to roundhouse $N.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
            }
            return;
        }
        public static void DoEndure(Character ch, string arguments)
        {
            AffectData affect;
            SkillSpell skill = SkillSpell.SkillLookup("endure");
            int skillPercent;

            //if (!ch.IsNPC && ch.Guild != null && !sn.skillLevel.TryGetValue(ch.Guild.name, out lvl))
            //    lvl = -1;

            //if (!ch.Learned.TryGetValue(sn, out lvl) || lvl <= 1)
            if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((affect = ch.FindAffect(skill)) != null)
            {
                ch.send("You are already enduring!\n\r");
                return;
            }

            else if (ch.ManaPoints < 20)
            {
                ch.send("You don't have enough mana to endure.\n\r");
                return;
            }

            else
            {
                ch.WaitState(skill.waitTime);
                if (skillPercent > Utility.NumberPercent())
                {
                    affect = new AffectData();
                    affect.skillSpell = skill;
                    affect.level = ch.Level;
                    affect.location = ApplyTypes.SavingSpell;
                    affect.duration = 8;
                    affect.modifier = -20;
                    affect.displayName = skill.name;
                    affect.affectType = AffectTypes.Skill;
                    ch.AffectToChar(affect);

                    affect = new AffectData();
                    affect.skillSpell = skill;
                    affect.level = ch.Level;
                    affect.location = ApplyTypes.AC;
                    affect.duration = 8;
                    affect.modifier = -20;
                    affect.displayName = skill.name;
                    affect.endMessage = "You are no longer enduring.\n\r";
                    //affect.endMessageToRoom = "$n's rage subsides.\n\r";
                    affect.affectType = AffectTypes.Skill;
                    ch.AffectToChar(affect);

                    //int heal = (int)(ch.Level * 2.5) + 5;
                    //ch.HitPoints = Math.Min(ch.HitPoints + heal, ch.MaxHitPoints);
                    ch.ManaPoints -= 20;


                    ch.send("You feel able to persevere better.\n\r");
                    //ch.Act("$n becomes filled with rage.\n\r", type: ActType.ToRoom);
                    ch.CheckImprove(skill, true, 1);
                }
                else
                {
                    ch.send("You try to endure but fail.\n\r");
                    //ch.Act("$n manages to turn their face red.\n\r", type: ActType.ToRoom);
                    ch.CheckImprove(skill, false, 1);
                }
            }
        }
        public static void DoOwaza(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("owaza");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Fighting == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            if (ch.IsAffected(skill))
            {
                var affect = ch.FindAffect(skill);
                ch.AffectFromChar(affect);
            }

            //chance += level / 10;

            ch.WaitState(skill.waitTime);

            if (ch.Fighting.HitPoints.Percent(ch.Fighting.MaxHitPoints) <= 50)
                chance += 20;
            else if (ch.Fighting.HitPoints.Percent(ch.Fighting.MaxHitPoints) <= 60)
                chance += 10;
            else if (ch.Fighting.HitPoints.Percent(ch.Fighting.MaxHitPoints) <= 70)
                chance += 5;
            else if (ch.Fighting.HitPoints.Percent(ch.Fighting.MaxHitPoints) >= 80)
                chance += -5;

            if (chance > Utility.NumberPercent())
            {
                var affect = new AffectData();
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.duration = 1;
                affect.frequency = Frequency.Violence;
                affect.hidden = true;
                affect.displayName = skill.name;
                affect.affectType = AffectTypes.Skill;
                ch.AffectToChar(affect);

                ch.send("You have successfully prepared an owaza attack.\n\r");
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.send("You try to owaza but fail.\n\r");
                ch.Act("$n tries to do somee fancy moves but fails.\n\r", type: ActType.ToRoom);
                ch.CheckImprove(skill, false, 1);
            }
        }
        static void CheckOwaza(Character ch, Character victim)
        {
            var skill = SkillSpell.SkillLookup("owaza");

            if (ch.Fighting == victim && ch.IsAffected(skill))
            {
                var affect = ch.FindAffect(skill);
                ch.AffectFromChar(affect);

                if (DoAssassinKotegaeshi(ch, victim))
                {
                    if (ch.Fighting == victim && DoAssassinKansetsuwaza(ch, victim))
                        if (ch.Fighting == victim) AssassinThrow(ch, victim);
                }
            }
        }
        public static void DoSpit(Character ch, string arguments)
        {

            var dam_each = new int[]
                {
                    50,
                    80,
                    100,
                    120
                };

            if (ch.Form == null)
            {
                if (!ch.CheckSocials("spit", arguments))
                    ch.send("You must be in camel form to spit!\n\r");
                return;
            }

            var level = 3 - (int)ch.Form.Tier;
            var skill = SkillSpell.SkillLookup("spit");
            int chance;
            int dam;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to spit again yet!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n shoots saliva and partially digested food at $N' eyes.", victim, type: ActType.ToRoom);
                ch.Act("You shoot saliva and partially digested food at $N's eyes.", victim, type: ActType.ToChar);

                if (!victim.IsAffected(AffectFlags.Blind))
                {
                    if ((victim.ImmuneFlags.ISSET(WeaponDamageTypes.Blind) || (victim.Form != null && victim.Form.ImmuneFlags.ISSET(WeaponDamageTypes.Blind))) && !victim.IsAffected(AffectFlags.Deafen))
                        victim.Act("$n is immune to blindness.", type: ActType.ToRoom);
                    else
                    {
                        var blindaffect = new AffectData();
                        blindaffect.ownerName = ch.Name;
                        blindaffect.skillSpell = skill;
                        blindaffect.level = ch.Level;
                        blindaffect.where = AffectWhere.ToAffects;
                        blindaffect.location = ApplyTypes.Hitroll;
                        blindaffect.flags.Add(AffectFlags.Blind);
                        blindaffect.duration = 1;
                        blindaffect.modifier = -4;
                        blindaffect.displayName = "Blinded";
                        blindaffect.endMessage = "You can see again.\n\r";
                        blindaffect.endMessageToRoom = "$n recovers their sight.\n\r";
                        blindaffect.affectType = AffectTypes.Skill;

                        victim.AffectToChar(blindaffect);
                        victim.Act("You are blinded by $N's spit of saliva and partially digested food!\n\r", ch);
                        victim.Act("$n appears to be blinded by $N's saliva and partially digested food!", ch, null, null, ActType.ToRoomNotVictim);
                        victim.Act("$n appears to be blinded by your spit of saliva and partially digested food!", ch, null, null, ActType.ToVictim);
                    }
                }
                dam = dam_each[level];
                Combat.Damage(ch, victim, dam, skill);

            }
            else
            {
                ch.Act("$n tries to spit saliva and partially digested food at $N's eyes, but misses.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("You try to spit saliva and partially digested food at $N's eyes, but miss.", victim, type: ActType.ToChar);
                ch.Act("$n tries to spit saliva and partially digested food your eyes, but misses.", victim, type: ActType.ToVictim);
            }

            //dam = utility.rand(dam_each[level], dam_each[level] * 2);
            //Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = true;
            aff.frequency = Frequency.Tick;
            aff.where = AffectWhere.ToAffects;
            aff.displayName = "spit";
            aff.frequency = Frequency.Violence;
            aff.duration = 2;
            aff.endMessage = "You feel ready to spit again.";
            ch.AffectToChar(aff);
        }
        public static void DoSnarl(Character ch, string arguments)
        {

            var dam_each = new int[]
                {
                    25,
                    35,
                    45,
                    55
                };

            if (ch.Form == null)
            {
                if (!ch.CheckSocials("snarl", arguments))
                    ch.send("You must be in form to growl!\n\r");
                return;
            }

            var level = 3 - (int)ch.Form.Tier;
            var skill = SkillSpell.SkillLookup("snarl");
            int chance;
            int dam;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            Character victim = null;
            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to snarl again yet!\n\r");
                return;
            }
            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }
            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n snarls fiercely at $N.", victim, type: ActType.ToRoom);
                ch.Act("You snarl fiercely at $N.", victim, type: ActType.ToChar);
                var aff = new AffectData();
                aff.skillSpell = skill;
                aff.hidden = false;
                aff.location = ApplyTypes.Hitroll;
                aff.displayName = "snarled";
                aff.frequency = Frequency.Violence;
                aff.duration = 2;
                aff.modifier = victim.HitRoll / 2;

                victim.AffectToChar(aff);

                aff.hidden = true;
                aff.location = ApplyTypes.None;
                aff.modifier = 0;
                aff.duration = 4;
                aff.endMessage = "You feel ready to snarl again.";
                ch.AffectToChar(aff);

                victim.Act("Your aim is hindered by $N's snarl!\n\r", ch);
                victim.Act("$n's aim appears to hindered by $N's snarl!", ch, null, null, ActType.ToRoomNotVictim);
                victim.Act("$n's aim appears to be hindered by your snarl!", ch, null, null, ActType.ToVictim);

                dam = dam_each[level];
                Combat.Damage(ch, victim, dam, skill);
            }
            else
            {
                ch.Act("$n tries to snarl at $N, but misses.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("You try to snarl at $N, but miss.", victim, type: ActType.ToChar);
                ch.Act("$n tries to snarl at you, but misses.", victim, type: ActType.ToVictim);
            }
        }
        public static void DoAutotomy(Character ch, string arguments)
        {

            var skill = SkillSpell.SkillLookup("autotomy");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (ch.IsAffected(skill))
            {
                ch.send("Your tail has not fully grown back yet.\n\r");
                return;
            }
            ch.WaitState(skill.waitTime);
            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = true;
            aff.frequency = Frequency.Tick;
            aff.duration = 24;
            aff.endMessage = "Your tail has fully grown back.";
            ch.AffectToChar(aff);

            ch.Act("$n detaches its tail, and steps aside to rest for a bit.", type: ActType.ToRoom);
            ch.Act("You detach your tail and step aside to rest or run.", type: ActType.ToChar);

            NPCTemplateData TailTemplate;
            if (NPCTemplateData.Templates.TryGetValue(19037, out TailTemplate))
            {
                var Tail = new NPCData(TailTemplate, ch.Room);
                Tail.MaxHitPoints = ch.MaxHitPoints;
                Tail.HitPoints = ch.MaxHitPoints;

                var tailaffect = new AffectData();
                tailaffect.hidden = true;
                tailaffect.flags.Add(AffectFlags.SuddenDeath);
                tailaffect.duration = 4;
                tailaffect.endMessageToRoom = "$n finally subsides and dies.\n\r";
                Tail.AffectToChar(tailaffect);

                foreach (var attacker in ch.Room.Characters.ToArray())
                {
                    if (attacker.Fighting == ch) attacker.Fighting = Tail;
                }
            }
            StopFighting(ch, true);

            return;
        }
        public static void DoBarkskin(Character ch, string arguments)
        {
            AffectData affect;
            Character victim = ch;

            var skill = SkillSpell.SkillLookup("barkskin");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if ((affect = victim.FindAffect(skill)) != null)
            {
                ch.send("You are already protected by barkskin.\n\r");
                return;
            }
            else

            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n's skin becomes as hard as treebark.", victim, type: ActType.ToRoom);
                ch.Act("Your skin becomes as hard as treebark.", victim, type: ActType.ToChar);

                affect = new AffectData();
                affect.skillSpell = skill;
                affect.where = AffectWhere.ToAffects;
                affect.location = ApplyTypes.Armor;
                affect.duration = 7;
                affect.modifier = -40;
                affect.displayName = "barkskin";
                affect.endMessage = "The toughness of your barkskin fades.\n\r";
                affect.endMessageToRoom = "The toughness of $n's barkskin fades.\n\r";
                victim.AffectToChar(affect);
                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.Act("$n concentrats a bit with steady breathing, but nothing happens.", victim, type: ActType.ToRoom);
                ch.Act("You try to make your skin as hard as treebark but fail.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
            }

        }
        public static void DoLash(Character ch, string arguments)
        {

            var skill = SkillSpell.SkillLookup("lash");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            var level = ch.Level;
            Character victim = null;

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            chance += level / 10;
            var wield = ch.GetEquipment(WearSlotIDs.Wield);

            if (wield == null || (wield.WeaponType != WeaponTypes.Whip && wield.WeaponType != WeaponTypes.Flail))
            {
                ch.send("You must use a whip or flail to lash someone.\n\r");
                return;
            }
            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                var damage = Utility.dice(2, ch.Level / 2, ch.Level / 2);

                ch.Act("$n lashes $N with $p and pulls $M to the ground.", victim, wield, type: ActType.ToRoomNotVictim);
                ch.Act("$n lashes you with $p and pulls you to the ground!", victim, wield, type: ActType.ToVictim);
                ch.Act("You lash $N with $p and pull $M to the ground.", victim, wield, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, damage, skill, wield != null ? wield.WeaponDamageType.Type : WeaponDamageTypes.Sting);
                victim.WaitState(game.PULSE_VIOLENCE * 2);
                CheckCheapShot(victim);
                CheckGroundControl(victim);
            }
            else
            {
                ch.Act("$n attempts to lash $N with $p.", victim, wield, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to lash you with $p!", victim, wield, type: ActType.ToVictim);
                ch.Act("You attempt to lash $N with $p.", victim, wield, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, wield != null ? wield.WeaponDamageType.Type : WeaponDamageTypes.Sting);
            }
            return;
        }
        public static void DoFashionStaff(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("fashion staff");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (ch.Room.sector == SectorTypes.City || ch.Room.sector == SectorTypes.Inside)
            {
                ch.send("You cannot find a suitable tree branch from which to fashion your staff.\n\r");
                return;
            }
            if (!ItemTemplateData.Templates.TryGetValue(2967, out var ItemTemplate))
            {
                ch.send("You failed.\n\r");
                return;
            }
            ch.WaitState(skill.waitTime);
            ch.Act("$n fashions a ranger staff from a nearby tree branch.", type: ActType.ToRoom);
            ch.Act("You fashion a ranger staff from a nearby tree branch.", type: ActType.ToChar);

            var affmodifiertable = new SortedList<int, int>()
            {
                { 0, 4 },
                { 35, 6 },
                { 42, 8 },
                { 48, 10 },
                { 51, 12 },
                { 56, 14 },

            };
            var staff = new ItemData(ItemTemplate);

            var affect = new AffectData();
            affect.duration = -1;
            affect.where = AffectWhere.ToObject;
            affect.skillSpell = skill;
            affect.location = ApplyTypes.DamageRoll;
            affect.modifier = (from keyvaluepair in affmodifiertable where ch.Level >= keyvaluepair.Key select keyvaluepair.Value).Max();
            staff.affects.Add(new AffectData(affect));
            affect.location = ApplyTypes.Hitroll;
            staff.affects.Add(new AffectData(affect));
            staff.DamageDice.DiceSides = 6;
            staff.DamageDice.DiceCount = 8;
            staff.DamageDice.DiceBonus = ch.Level / 3;
            staff.Level = ch.Level;

            ch.AddInventoryItem(staff);
            ch.CheckImprove(skill, true, 1);
            return;
        }
        public static void DoFashionSpear(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("fashion spear");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (ch.Room.sector == SectorTypes.City || ch.Room.sector == SectorTypes.Inside)
            {
                ch.send("You cannot find a suitable tree branch from which to fashion your spear.\n\r");
                return;
            }
            if (!ItemTemplateData.Templates.TryGetValue(2969, out var ItemTemplate))
            {
                ch.send("You failed.\n\r");
                return;
            }
            ch.WaitState(skill.waitTime);
            ch.Act("$n fashions a ranger spear from a nearby tree branch.", type: ActType.ToRoom);
            ch.Act("You fashion a ranger spear from a nearby tree branch.", type: ActType.ToChar);

            var affmodifiertable = new SortedList<int, int>()
            {
                { 0, 4 },
                { 35, 6 },
                { 42, 8 },
                { 48, 10 },
                { 51, 12 },
                { 56, 14 },

            };
            var spear = new ItemData(ItemTemplate);

            var affect = new AffectData();
            affect.duration = -1;
            affect.where = AffectWhere.ToObject;
            affect.skillSpell = skill;
            affect.location = ApplyTypes.DamageRoll;
            affect.modifier = (from keyvaluepair in affmodifiertable where ch.Level >= keyvaluepair.Key select keyvaluepair.Value).Max();
            spear.affects.Add(new AffectData(affect));
            affect.location = ApplyTypes.Hitroll;
            spear.affects.Add(new AffectData(affect));
            spear.DamageDice.DiceSides = 6;
            spear.DamageDice.DiceCount = 8;
            spear.DamageDice.DiceBonus = ch.Level / 3;
            spear.Level = ch.Level;

            ch.AddInventoryItem(spear);
            ch.CheckImprove(skill, true, 1);
            return;
        }
        public static void DoOwlKinship(Character ch, string arguments)
        {

            var skill = SkillSpell.SkillLookup("owl kinship");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (ch.IsAffected(skill))
            {
                ch.send("You cannot commune with the owls yet.\n\r");
                return;
            }
            if (ch.Pet != null)
            {
                ch.send("You cannot have more than one wild creature assisting you.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = false;
            aff.displayName = skill.name;
            aff.frequency = Frequency.Tick;
            aff.duration = 24;
            aff.endMessage = "You can once again commune with the owls.";
            ch.AffectToChar(aff);

            NPCTemplateData KinshipTemplate;
            if (NPCTemplateData.Templates.TryGetValue(19041, out KinshipTemplate))
            {
                var Pet = new NPCData(KinshipTemplate, ch.Room);
                Pet.MaxHitPoints = ch.MaxHitPoints / 2;
                Pet.HitPoints = ch.MaxHitPoints / 2;
                Pet.HitRoll = ch.HitRoll;
                Pet.DamageRoll = ch.DamageRoll;
                Pet.ArmorClass = ch.ArmorClass - 100;
                Pet.Level = ch.Level;
                ch.Group.Add(Pet);
                ch.Pet = Pet;
                Pet.Following = ch;
                Pet.Leader = ch;

                var damagedicebylevel = new SortedList<int, Dice>()
                {
                    {0, new Dice(3,5,10)},
                    {25, new Dice(4,6,10)},
                    {35, new Dice(4,8,10)},
                    {45, new Dice(4,10,10)},
                    {51, new Dice(4,10,15)},
                    {56, new Dice(4,11,20)},
                };
                Pet.DamageDice = (from keyvaluepair in damagedicebylevel where ch.Level >= keyvaluepair.Key select keyvaluepair.Value).Max();

                ch.Act("With a loud howl, $n calls into the wild, attracting $N to $s side.", Pet, type: ActType.ToRoom);
                ch.Act("You howl into the wild, attracting $N to your side.", Pet, type: ActType.ToChar);

                //foreach (var attacker in ch.Room.Characters.ToArray())
                //{
                //    if (attacker.Fighting == ch)
                //        attacker.Fighting = Pet;
                //}
            }
            return;
        }
        public static void DoWolfKinship(Character ch, string arguments)
        {

            var skill = SkillSpell.SkillLookup("wolf kinship");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (ch.IsAffected(skill))
            {
                ch.send("You cannot commune with the wolves yet.\n\r");
                return;
            }
            if (ch.Pet != null)
            {
                ch.send("You cannot have more than one wild creature assisting you.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = false;
            aff.displayName = skill.name;
            aff.frequency = Frequency.Tick;
            aff.duration = 24;
            aff.endMessage = "You can once again commune with the wolves.";
            ch.AffectToChar(aff);

            NPCTemplateData KinshipTemplate;
            if (NPCTemplateData.Templates.TryGetValue(19038, out KinshipTemplate))
            {
                var Pet = new NPCData(KinshipTemplate, ch.Room);
                Pet.Master = ch;
                Pet.MaxHitPoints = ch.MaxHitPoints / 2;
                Pet.HitPoints = ch.MaxHitPoints / 2;
                Pet.HitRoll = ch.HitRoll;
                Pet.DamageRoll = ch.DamageRoll;
                Pet.ArmorClass = ch.ArmorClass - 100;
                Pet.Level = ch.Level;
                ch.Group.Add(Pet);
                ch.Pet = Pet;
                Pet.Following = ch;
                Pet.Leader = ch;

                var damagedicebylevel = new SortedList<int, Dice>()
                {
                    {0, new Dice(3,5,10)},
                    {25, new Dice(4,6,10)},
                    {35, new Dice(4,8,10)},
                    {45, new Dice(4,10,10)},
                    {51, new Dice(4,10,15)},
                    {56, new Dice(4,11,20)},
                };
                Pet.DamageDice = (from keyvaluepair in damagedicebylevel where ch.Level >= keyvaluepair.Key select keyvaluepair.Value).Max();

                ch.Act("With a loud howl, $n calls into the wild, attracting $N to $s side.", Pet, type: ActType.ToRoom);
                ch.Act("You howl into the wild, attracting $N to your side.", Pet, type: ActType.ToChar);

                //foreach (var attacker in ch.Room.Characters.ToArray())
                //{
                //    if (attacker.Fighting == ch)
                //        attacker.Fighting = Pet;
                //}
            }
            return;
        }
        public static void DoSerpentKinship(Character ch, string arguments)
        {

            var skill = SkillSpell.SkillLookup("serpent kinship");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (ch.IsAffected(skill))
            {
                ch.send("You cannot commune with the serpents yet.\n\r");
                return;
            }
            if (ch.Pet != null)
            {
                ch.send("You cannot have more than one wild creature assisting you.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = false;
            aff.displayName = skill.name;
            aff.frequency = Frequency.Tick;
            aff.duration = 24;
            aff.endMessage = "You can once again commune with the serpents.";
            ch.AffectToChar(aff);

            NPCTemplateData KinshipTemplate;
            if (NPCTemplateData.Templates.TryGetValue(19039, out KinshipTemplate))
            {
                var Pet = new NPCData(KinshipTemplate, ch.Room);
                Pet.Master = ch;
                Pet.MaxHitPoints = ch.MaxHitPoints / 2;
                Pet.HitPoints = ch.MaxHitPoints / 2;
                Pet.HitRoll = ch.HitRoll * 2;
                Pet.DamageRoll = ch.DamageRoll * 2;
                Pet.ArmorClass = ch.ArmorClass - 100;
                Pet.Level = ch.Level;
                ch.Group.Add(Pet);
                ch.Pet = Pet;
                Pet.Following = ch;
                Pet.Leader = ch;

                var damagedicebylevel = new SortedList<int, Dice>()
                {
                    {0, new Dice(3,5,10)},
                    {25, new Dice(4,6,10)},
                    {35, new Dice(4,8,10)},
                    {45, new Dice(4,10,10)},
                    {51, new Dice(4,10,15)},
                    {56, new Dice(4,11,20)},
                };
                Pet.DamageDice = (from keyvaluepair in damagedicebylevel where ch.Level >= keyvaluepair.Key select keyvaluepair.Value).Max();

                ch.Act("With a loud hiss, $n calls into the wild, attracting $N to $s side.", Pet, type: ActType.ToRoom);
                ch.Act("You hiss loudly into the wild, attracting $N to your side.", Pet, type: ActType.ToChar);

                //foreach (var attacker in ch.Room.Characters.ToArray())
                //{
                //    if (attacker.Fighting == ch)
                //        attacker.Fighting = Pet;
                //}
            }
            return;
        }
        public static void DoBearKinship(Character ch, string arguments)
        {

            var skill = SkillSpell.SkillLookup("bear kinship");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (ch.IsAffected(skill))
            {
                ch.send("You cannot commune with the bears yet.\n\r");
                return;
            }
            if (ch.Pet != null)
            {
                ch.send("You cannot have more than one wild creature assisting you.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);
            var aff = new AffectData();
            aff.skillSpell = skill;
            aff.hidden = false;
            aff.displayName = skill.name;
            aff.frequency = Frequency.Tick;
            aff.duration = 24;
            aff.endMessage = "You can once again commune with the bears.";
            ch.AffectToChar(aff);

            NPCTemplateData KinshipTemplate;
            if (NPCTemplateData.Templates.TryGetValue(19040, out KinshipTemplate))
            {
                var Pet = new NPCData(KinshipTemplate, ch.Room);
                Pet.Master = ch;
                Pet.MaxHitPoints = ch.MaxHitPoints * 2;
                Pet.HitPoints = ch.MaxHitPoints * 2;
                Pet.HitRoll = ch.HitRoll / 2;
                Pet.DamageRoll = ch.DamageRoll / 2;
                Pet.ArmorClass = ch.ArmorClass - 500;
                Pet.Level = ch.Level;
                ch.Group.Add(Pet);
                ch.Pet = Pet;
                Pet.Following = ch;
                Pet.Leader = ch;

                var damagedicebylevel = new SortedList<int, Dice>()
                {
                    {0, new Dice(3,5,10)},
                    {25, new Dice(4,6,10)},
                    {35, new Dice(4,8,10)},
                    {45, new Dice(4,10,10)},
                    {51, new Dice(4,10,15)},
                    {56, new Dice(4,11,20)},
                };
                Pet.DamageDice = (from keyvaluepair in damagedicebylevel where ch.Level >= keyvaluepair.Key select keyvaluepair.Value).Max();

                ch.Act("With a loud growl, $n calls into the wild, attracting $N to $s side.", Pet, type: ActType.ToRoom);
                ch.Act("You growl loudly into the wild, attracting $N to your side.", Pet, type: ActType.ToChar);

                //foreach (var attacker in ch.Room.Characters.ToArray())
                //{
                //    if (attacker.Fighting == ch)
                //        attacker.Fighting = Pet;
                //}
            }
            return;
        }
        public static void DoWarCry(Character ch, string arguments)
        {
            AffectData affect;
            SkillSpell skill = SkillSpell.SkillLookup("warcry");
            int skillPercent;

            //if (!ch.IsNPC && ch.Guild != null && !sn.skillLevel.TryGetValue(ch.Guild.name, out lvl))
            //    lvl = -1;

            //if (!ch.Learned.TryGetValue(sn, out lvl) || lvl <= 1)
            if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("Huh?\n\r");
                return;
            }

            if ((affect = ch.FindAffect(skill)) != null)
            {
                ch.send("You are already inspired!\n\r");
                return;
            }
            else if (ch.Position == Positions.Standing && ch.Fighting == null)
            {
                ch.send("You must be fighting to warcry!\n\r");
                return;
            }
            else if (ch.ManaPoints < 20)
            {
                ch.send("You don't have enough mana to warcry.\n\r");
                return;
            }

            else
            {
                ch.WaitState(skill.waitTime);
                if (skillPercent > Utility.NumberPercent())
                {
                    affect = new AffectData();
                    affect.skillSpell = skill;
                    affect.level = ch.Level;
                    affect.location = ApplyTypes.Saves;
                    affect.duration = 10;
                    affect.modifier = -8;
                    affect.displayName = "warcry";
                    affect.affectType = AffectTypes.Skill;
                    ch.AffectToChar(affect);

                    affect = new AffectData();
                    affect.skillSpell = skill;
                    affect.level = ch.Level;
                    affect.location = ApplyTypes.Hitroll;
                    affect.duration = 10;
                    affect.modifier = +8;
                    affect.displayName = "warcry";
                    affect.endMessage = "Your warcry subsides.\n\r";
                    affect.endMessageToRoom = "$n's warcry subsides.\n\r";
                    affect.affectType = AffectTypes.Skill;
                    ch.AffectToChar(affect);

                    ch.ManaPoints -= 20;

                    ch.send("You are inspired by your warcry.\n\r");
                    ch.Act("$n becomes inspired by $s warcry.\n\r", type: ActType.ToRoom);
                    ch.CheckImprove(skill, true, 1);
                }
                else
                {
                    ch.send("You choke up and fail to yell your warcry.\n\r");
                    ch.Act("$n chokes up and fails to yell their warcry.\n\r", type: ActType.ToRoom);
                    ch.CheckImprove(skill, false, 1);
                }
            }
        }
        public static void DoShieldBash(Character ch, string arguments)
        {
            Character victim;
            int dam = 0;
            int skillPercent = 0;
            int count = 0;
            var skill = SkillSpell.SkillLookup("shield bash");

            //if (!ch.IsNPC && ch.Guild != null && !skill.skillLevel.TryGetValue(ch.Guild.name, out lvl))
            //    lvl = -1;

            if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                ch.WaitState(game.PULSE_VIOLENCE * 1);
                return;
            }
            ItemData shield;
            if (!ch.Equipment.TryGetValue(WearSlotIDs.Shield, out shield) || shield == null)
            {
                ch.send("You must be holding a shield to shield bash someone.\n\r");
                return;
            }
            if ((victim = (ch.GetCharacterFromRoomByName(arguments, ref count)) ?? ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            if (CheckIsSafe(ch, victim)) return;

            if (victim == ch)
                ch.send("You can't shield bash yourself!.\n\r");

            else if (CheckAcrobatics(ch, victim)) return;

            else if (victim.FindAffect(SkillSpell.SkillLookup("protective shield")) != null)
            {
                ch.WaitState(game.PULSE_VIOLENCE);
                ch.Act("You try to shield bash $N but miss $M.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n tries to shield bash $N but miss $M.\n\r", victim, type: ActType.ToRoomNotVictim);
            }
            else if (skillPercent > Utility.NumberPercent())
            {
                dam += Utility.Random(10, (ch.Level) / 2);

                ch.Position = Positions.Fighting;
                ch.Fighting = victim;
                ch.Act("You shield bash $N and they fall to the ground.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n shield bashes $N to the ground.\n\r", victim, type: ActType.ToRoomNotVictim);
                victim.send("{0} shield bashes you to the ground.\n\r", ch.Display(victim));

                Combat.Damage(ch, victim, dam, skill);
                ch.WaitState(game.PULSE_VIOLENCE * 2);
                victim.WaitState(game.PULSE_VIOLENCE * 1);
                ch.CheckImprove(skill, true, 1);
                CheckCheapShot(victim);
                CheckGroundControl(victim);
            }
            else
            {
                ch.WaitState(game.PULSE_VIOLENCE * 1);
                ch.send("You failed to shield bash.\n\r");
                ch.CheckImprove(skill, false, 1);
            }
        }
        public static void DoAngelsWing(Character ch, string arguments)
        {
            ItemData shield;
            var skill = SkillSpell.SkillLookup("angels wing");
            int chance;
            int dam;
            var level = ch.Level;
            Character victim = null;

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
            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
            }
            else if (!ch.Equipment.TryGetValue(WearSlotIDs.Shield, out shield) || shield == null)
            {
                ch.send("You must be holding a shield to perform this maneuver.\n\r");
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
            }
            else if (chance > Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);

                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                ch.Act("You swing your shield outward and upward, then make a quick, powerful attack to $N.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n swings $s shield outward and upward, then makes a quick, powerful attack to $N.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n swings $s shield outward and upward, then strikes you with a quick, powerful attack.\n\r", victim, type: ActType.ToVictim);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
            }
            else
            {
                ch.WaitState(skill.waitTime);

                ch.Act("You swing your shield outward and upward, but fail to make a quick, powerful attack to $N.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n swings $s shield outward and upward, but fails to make a quick, powerful attack to $N.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n swings $s shield outward and upward, but fails to strike you .\n\r", victim, type: ActType.ToVictim);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
            }
            return;
        }
        public static void DoPalmSmash(Character ch, string arguments)
        {

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
            var skill = SkillSpell.SkillLookup("palm smash");
            int chance;
            if ((chance = ch.GetSkillPercentage(skill) + 10) <= 11)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            ItemData weapon;
            if (ch.Equipment.TryGetValue(WearSlotIDs.Wield, out weapon) && weapon != null)
            {
                ch.send("You must be bare-handed to perform this maneuver.\n\r");
                return;
            }
            int dam;
            var level = ch.Level;
            Character victim = null;

            if (arguments.ISEMPTY() && ch.Fighting == null)
            {
                ch.send("Who did you want to palm smash?\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("There is no one here to palm smash.\n\r");
                return;
            }

            chance += level / 10;

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                dam = Utility.Random(dam_each[level] * 2, dam_each[level] * 3);

                ch.Act("You smash the palm of your hand forward, making a quick, powerful attack to $N.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n smashes $s palm forward, quickly stricking $N.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n smashes $s palm forward, quickly stricking you with a powerful attack.\n\r", victim, type: ActType.ToVictim);

                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
                victim.WaitState(game.PULSE_VIOLENCE * 1);
                ch.CheckImprove(skill, true, 1);
                CheckCheapShot(ch, victim);
            }
            else
            {
                ch.Act("You try to smash $N with the palm of your hand but miss.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n tries to smash $N with $s palm but misses.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to smash you with $s palm, but misses.\n\r", victim, type: ActType.ToVictim);
                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
            }
            return;
        }
        public static void DoHandFlurry(Character ch, string argument)
        {
            var dam_each = new int[]
           {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
           };

            Character victim;
            var skill = SkillSpell.SkillLookup("hand flurry");

            int chance = 0, numhits = 0, i = 0, dam = 0;
            int learned = 0;

            if ((learned = ch.GetSkillPercentage(skill) + 20) <= 11)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            ItemData weapon;
            if ((ch.Equipment.TryGetValue(WearSlotIDs.Wield, out weapon) && weapon != null) ||
                (ch.Equipment.TryGetValue(WearSlotIDs.Shield, out weapon) && weapon != null) ||
                (ch.Equipment.TryGetValue(WearSlotIDs.Held, out weapon) && weapon != null) ||
                (ch.Equipment.TryGetValue(WearSlotIDs.DualWield, out weapon) && weapon != null))
            {
                ch.send("You must be bare-handed to perform this maneuver.\n\r");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            chance = Utility.NumberPercent();

            if (chance > learned)
            {
                ch.Act("You attempt to start a hand flurry, but fail.", victim, type: ActType.ToChar);
                ch.Act("$n flails out wildly with $s hands but blunders.", type: ActType.ToRoom);
                ch.CheckImprove(skill, false, 2);
                ch.WaitState(skill.waitTime / 2);
                return;
            }

            if ((chance + learned) > 165)
            {
                numhits = 7;
            }
            else if ((chance + learned) > 155)
            {
                numhits = 6;
            }
            else if ((chance + learned) > 145)
            {
                numhits = 5;
            }
            else if ((chance + learned) > 135)
            {
                numhits = 4;
            }
            else if ((chance + learned) > 115)
            {
                numhits = 3;
            }
            else
            {
                numhits = 2;
            }
            ch.Act("You begin a wild flurry of attacks with your palms!", victim, type: ActType.ToChar);
            ch.Act("$n begins a wild flurry of attacks with $s palms!", type: ActType.ToRoom);
            ch.CheckImprove(skill, true, 1);

            var level = ch.Level;

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            for (i = 0; i < numhits; i++)
            {
                dam = Utility.Random(dam_each[level] + 10, dam_each[level] * 2 + 10);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);

                if (ch.Fighting != victim)
                    break;
            }

            ch.WaitState(skill.waitTime);

            return;
        }
        public static void DoMaceFlurry(Character ch, string argument)
        {
            var dam_each = new int[]
           {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
           };

            Character victim;
            var skill = SkillSpell.SkillLookup("mace flurry");
            ItemData wield = null;
            int chance = 0, numhits = 0, i = 0, dam = 0;
            int learned = 0;

            if ((learned = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || (wield.WeaponType != WeaponTypes.Mace))
            {
                ch.send("You must be wielding a mace to do this.");
                return;
            }

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }

            chance = Utility.NumberPercent();

            if (chance > learned)
            {
                ch.Act("You attempt to do something fancy with your mace, but fail.", victim, type: ActType.ToChar);
                ch.Act("$n flails out wildly with $s mace but blunders.", type: ActType.ToRoom);
                ch.CheckImprove(skill, false, 2);
                ch.WaitState(skill.waitTime / 2);
                return;
            }

            if ((chance + learned) > 158)
            {
                numhits = 5;
            }
            else if ((chance + learned) > 148)
            {
                numhits = 4;
            }
            else if ((chance + learned) > 140)
            {
                numhits = 3;
            }
            else
            {
                numhits = 2;
            }
            ch.Act("You begin a wild flurry of attacks with your mace!", victim, type: ActType.ToChar);
            ch.Act("$n begins a wild flurry of attacks with $s mace!", type: ActType.ToRoom);
            ch.CheckImprove(skill, true, 1);

            var level = ch.Level;

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            for (i = 0; i < numhits; i++)
            {
                var roll = wield.DamageDice.Roll();

                dam = Utility.Random(dam_each[level] + roll, dam_each[level] * 2 + roll);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);

                if (ch.Fighting != victim)
                    break;
            }

            ch.WaitState(skill.waitTime);

            return;
        }
        public static void DoLanceCharge(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("lance charge");
            int chance;
            int dam;
            var level = ch.Level;
            ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            else if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || (wield.WeaponType != WeaponTypes.Spear && wield.WeaponType != WeaponTypes.Polearm))
            {
                ch.send("You must be wielding a spear or polearm to lance charge your enemy.\n\r");
                return;
            }
            Character victim = null;

            if (ch.Position == Positions.Fighting)
            {
                ch.send("You're too busy fighting already!\n\r");
                return;
            }

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (CheckPreventSurpriseAttacks(ch, victim)) return;

            if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);


                ch.Act("$n lance charges $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n lance charges you.", victim, type: ActType.ToVictim);
                ch.Act("You lance charge $N.", victim, type: ActType.ToChar);

                dam = Utility.Random(dam_each[level] * 4, dam_each[level] * 6);
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Pierce);

            }
            else
            {
                ch.Act("$n tries to lance charge $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to lance charge you but fails!", victim, type: ActType.ToVictim);
                ch.Act("You try to lance charge $N but fail.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Pierce);
            }
            return;
        }
        public static void DoSabreCharge(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("sabre charge");
            int chance;
            int dam;
            var level = ch.Level;
            ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            Character victim = null;

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Sword)
            {
                ch.send("You must be wielding a sword to sabre charge your enemy.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (CheckPreventSurpriseAttacks(ch, victim)) return;

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n performs a sabre charge against $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n performs a sabre charge against you.", victim, type: ActType.ToVictim);
                ch.Act("You perform a sabre charge against $N.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);

                var roll = wield.DamageDice.Roll();

                dam = Utility.Random(dam_each[level] * 2 + roll, dam_each[level] * 3 + roll);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Acid);
            }
            else
            {
                ch.Act("$n attempts to perform a sabre charge against $N, but fails.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to perform a sabre charge against you, but fails.", victim, type: ActType.ToVictim);
                ch.Act("You attempt to perform a sabre charge against $N, but fail.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);

                dam = 0;
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Acid);
            }

        }
        public static void DoCrushingCharge(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("crushing charge");
            int chance;
            int dam;
            var level = ch.Level;
            ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            Character victim = null;

            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || wield.WeaponType != WeaponTypes.Mace)
            {
                ch.send("You must be wielding a mace to crush charge your enemy.\n\r");
                return;
            }

            ch.WaitState(skill.waitTime);

            if (CheckPreventSurpriseAttacks(ch, victim)) return;

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n performs a crushing charge against $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n performs a crushing charge against you.", victim, type: ActType.ToVictim);
                ch.Act("You perform a crushing charge against $N.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, true, 1);

                dam = Utility.Random(dam_each[level] * 2, dam_each[level] * 3);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Acid);
            }
            else
            {
                ch.Act("$n attempts to perform a crushing charge against $N, but fails.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n attempts to perform a crushing charge against you, but fails.", victim, type: ActType.ToVictim);
                ch.Act("You attempt to perform a crushing charge against $N, but fail.", victim, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);

                dam = 0;
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Acid);
            }
        }
        public static void DoHeadSmash(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("head smash");
            int chance;
            int dam;
            var level = ch.Level;
            ItemData wield = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if ((wield = ch.GetEquipment(WearSlotIDs.Wield)) == null || (wield.WeaponType != WeaponTypes.Staff && wield.WeaponType != WeaponTypes.Polearm))
            {
                ch.send("You must be wielding a staff or polearm to head smash your enemy.");
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
                ch.Act("$n feints an attack at $N, creating an opportunity to smash $S head in.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n feints an attack at you, creating an opportunity to smash your head in.", victim, type: ActType.ToVictim);
                ch.Act("You feint an attack at $N, then take the opportunity to smash $S head in.", victim, type: ActType.ToChar);

                dam = (wield.DamageDice.Roll() + ch.DamageRoll) * 2;
                ch.CheckImprove(skill, true, 1);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Smash);

                if (!victim.IsAffected(skill))
                {
                    var affect = new AffectData();
                    affect.skillSpell = skill;
                    affect.displayName = skill.name;
                    affect.duration = 5;
                    affect.where = AffectWhere.ToAffects;
                    affect.location = ApplyTypes.Strength;
                    affect.modifier = -6;
                    victim.AffectToChar(affect);

                    affect.location = ApplyTypes.Dexterity;
                    affect.modifier = -6;
                    victim.AffectToChar(affect);

                    affect.location = ApplyTypes.AC;
                    affect.modifier = +400;

                    affect.endMessage = "Your head feels better.";
                    affect.endMessageToRoom = "$n's head looks better.";
                    victim.AffectToChar(affect);
                }
            }
            else
            {
                ch.Act("$n attempts to headsmash $N but fails to open a strike opportunity with $s feint.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to smash your head but failed to open a strike opportunity &s initial feint!", victim, type: ActType.ToVictim);
                ch.Act("You try to headsmash $N but your faint fails to open a strike opportunity.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Smash);
            }
            return;
        }
        public static void DoWeaponTrip(Character ch, string arguments)
        {
            Character victim = null;
            int dam = 0;
            int skillPercent = 0;
            int count = 0;
            var skill = SkillSpell.SkillLookup("weapon trip");
            ItemData weapon = null;

            //if (!ch.IsNPC && ch.Guild != null && !skill.skillLevel.TryGetValue(ch.Guild.name, out lvl))
            //    lvl = -1;

            //if (!ch.Learned.TryGetValue(skill, out lvl) || lvl <= 1)
            if ((skillPercent = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.Act("You don't know how to do that.\n\r");
            }

            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.Act("You aren't fighting anyone.\n\r");
            }
            else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments, ref count)) == null)
            {
                ch.Act("They aren't here!\n\r");
            }

            else if (CheckIsSafe(ch, victim))
            {

            }
            else if (victim == ch)
            {
                ch.Act("You can't weapon trip yourself!.\n\r");
            }
            else if ((weapon = ch.GetEquipment(WearSlotIDs.Wield)) == null ||
                !(new WeaponTypes[] { WeaponTypes.Mace, WeaponTypes.Sword, WeaponTypes.Spear }).Contains(weapon.WeaponType))
            {
                ch.Act("You must be wielding a mace, sword, or spear in your main hand to do that.\n\r");
            }

            else if (skillPercent > Utility.NumberPercent())
            {
                dam = weapon.DamageDice.Roll();

                ch.Position = Positions.Fighting;
                ch.Fighting = victim;
                ch.Act("You weapon trip $N and $E falls to the ground.\n\r", victim);
                ch.Act("$n weapon trips $N to the ground.\n\r", victim, type: ActType.ToRoomNotVictim);
                victim.Act("$N weapon trips you to the ground.\n\r", ch);

                Combat.Damage(ch, victim, dam, skill);
                ch.WaitState(skill.waitTime);

                if (!victim.IsAffected(AffectFlags.Flying))
                {
                    victim.WaitState(game.PULSE_VIOLENCE * 2);
                }
                ch.CheckImprove(skill, true, 1);
                CheckCheapShot(victim);
                CheckGroundControl(victim);
            }
            else
            {
                ch.Act("You fail to weapon trip $N to the ground.\n\r", victim);
                ch.Act("$n fails to weapon trip $N to the ground.\n\r", victim, type: ActType.ToRoomNotVictim);
                victim.Act("$N fails to weapon trip you to the ground.\n\r", ch);
                ch.WaitState(skill.waitTime);
                Combat.Damage(ch, victim, 0, skill);
                ch.CheckImprove(skill, false, 1);
            }
        }
        public static void DoSap(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("sap");
            int chance;
            int dam;
            Character victim = null;
            ItemData weapon = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (arguments.ISEMPTY())
            {
                ch.send("Sap who?\n\r");
                return;
            }
            else if (victim == ch)
            {
                ch.Act("You wouldn't want to sap yourself!");
            }
            else if (ch.Fighting != null)
            {
                ch.send("You're too busy fighting.\n\r");
                return;
            }
            else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (victim is NPCData && ((NPCData)victim).Protects.Any())
            {
                ch.send("You can't sneak up on them.\n\r");
            }
            else if (ch == victim)
                ch.send("You can't sap yourself.\n\r");
            else if (victim.IsAffected(AffectFlags.Sleep))
            {
                ch.send("They are already asleep.\n\r");
                return;
            }
            else if ((weapon = ch.GetEquipment(WearSlotIDs.Wield)) == null || ch.GetSkillPercentage(weapon.WeaponType.ToString().ToLower()) <= 1)
            {
                ch.send("You must be wielding a weapon you are familiar with to sap someone.\n\r");
                return;
            }
            else if (Combat.CheckIsSafe(ch, victim))
            {

            }
            else if (chance > Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n saps $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n saps you.", victim, type: ActType.ToVictim);
                ch.Act("You sap $N.", victim, type: ActType.ToChar);

                StopFighting(victim, true);
                victim.Position = Positions.Sleeping;
                var affect = new AffectData();
                affect.displayName = "sap";
                affect.flags.SETBIT(AffectFlags.Sleep);
                affect.duration = 5;
                affect.where = AffectWhere.ToAffects;
                affect.skillSpell = skill;
                affect.endMessage = "You feel able to wake yourself up.";

                victim.AffectToChar(affect);

                ch.CheckImprove(skill, true, 1);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n attempts to sap $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to sap you!", victim, type: ActType.ToVictim);
                ch.Act("You try to sap $N.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, false, 1);
                dam = Utility.Random(10, ch.Level);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
            }
            return;
        }

        public static void DoDisengage(Character ch, String arguments)
        {
            Character victim = ch.Fighting;
            int skillPercent = 0;
            SkillSpell skill = SkillSpell.SkillLookup("disengage");

            if ((victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone!\n\r");
            }
            else if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.Act("You don't know how to do that.");
            }
            else
            {
                int grip;
                if (ch.Fighting != null && ch.Fighting.Fighting == ch && (grip = ch.Fighting.GetSkillPercentage("grip")) > 1)
                {
                    if (grip > Utility.NumberPercent())
                    {
                        ch.Fighting.Act("$n grips you in its strong raptorial arms, preventing you from disengaging.", ch, type: ActType.ToVictim);
                        ch.Fighting.Act("You grip $N in your strong raptorial arms, preventing $M from disengaging.", ch, type: ActType.ToChar);
                        ch.Fighting.Act("$n grips $N in its strong raptorial arms, preventing $M from disengaging.", ch, type: ActType.ToRoomNotVictim);
                        return;
                    }
                }
                if (ch.IsAffected(SkillSpell.SkillLookup("secreted filament")) && Utility.Random(0, 1) == 0)
                {
                    ch.Act("$n tries to disengage, but the filament covering $m prevents $m from doing so.", ch, type: ActType.ToVictim);
                    ch.Act("The secreted filament covering you prevents you from disengaging.\n\r", ch, type: ActType.ToChar);
                    ch.Act("$n tries to disengage, but the filament covering $m prevents $m from doing so.", ch, type: ActType.ToRoomNotVictim);
                    return;
                }

                int chance = skillPercent;

                if (chance > Utility.NumberPercent())
                {
                    ch.WaitState(skill.waitTime / 2);
                    StopFighting(ch, true);
                    ch.Position = Positions.Standing;

                    ch.AffectedBy.SETBIT(AffectFlags.Hide);
                    ch.AffectedBy.SETBIT(AffectFlags.Sneak);
                    ch.Act("$n disengages from $N and fades into the shadows.", victim, type: ActType.ToRoomNotVictim);
                    ch.Act("$n disengages from you and fades into the shadows.", victim, type: ActType.ToVictim);
                    ch.Act("You disengage from $N and fade into the shadows.", victim, type: ActType.ToChar);
                    ch.CheckImprove("disengage", true, 1);
                }
                else
                {
                    ch.WaitState(skill.waitTime);
                    ch.Act("You fail to disengage!\n\r", victim, type: ActType.ToChar);
                    ch.CheckImprove("disengage", false, 1);
                }
            }
        }//end disengage
        public static void DoKidneyShot(Character ch, string arguments)
        {
            Character victim = null;
            SkillSpell skill = SkillSpell.SkillLookup("kidney shot");
            int skillPercent;
            ItemData weapon;
            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
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
            else if (victim.IsAffected(skill))
            {
                ch.Act("$E is still bleeding from your previous kidney shot.\n\r", victim);
            }
            else if (((weapon = ch.GetEquipment(WearSlotIDs.Wield)) == null || weapon.WeaponType != WeaponTypes.Dagger) &&
               ((weapon = ch.GetEquipment(WearSlotIDs.DualWield)) == null || weapon.WeaponType != WeaponTypes.Dagger))
            {
                ch.Act("You must be wielding a dagger to kidney shot.");
            }
            else
            {
                ch.WaitState(skill.waitTime);

                if (skillPercent > Utility.NumberPercent())
                {

                    if (!victim.IsAffected(skill))
                    {
                        var affect = new AffectData()
                        {
                            skillSpell = skill,
                            displayName = "kidney shot",
                            duration = 5,
                            modifier = -5,
                            location = ApplyTypes.Strength,
                            affectType = AffectTypes.Skill,
                            level = ch.Level,
                        };

                        affect.endMessage = "Your kidneys feel better.";
                        victim.AffectToChar(affect);
                    }

                    ch.Act("You stab $N in the kidneys with $p.", victim, weapon);
                    ch.Act("$n stabs you in the kidneys $p!", victim, weapon, type: ActType.ToVictim);
                    ch.Act("$n stabs $N in the kidneys with $p!", victim, weapon, type: ActType.ToRoomNotVictim);

                    float damage = (weapon.DamageDice.Roll() + ch.DamageRoll) * 3 / 2;

                    CheckEnhancedDamage(ch, ref damage);

                    ch.CheckImprove(skill, true, 1);

                    Combat.Damage(ch, victim, (int)damage, skill, WeaponDamageTypes.Pierce);
                }
                else
                {
                    ch.Act("You attempt to stab $N in the kidneys with $p.", victim, weapon);
                    ch.Act("$n attempts to stab your kidneys with $p!", victim, weapon, type: ActType.ToVictim);
                    ch.Act("$n attempts to stab $N in the kidneys with $p!", victim, weapon, type: ActType.ToRoomNotVictim);

                    ch.CheckImprove(skill, false, 1);

                    Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Slice);
                }
            }
        } // end DoKidneyShot
        public static void DoBlindFold(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("blindfold");
            int chance;
            Character victim = null;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if ((victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here\n\r");
                return;
            }
            if (!victim.IsAffected(AffectFlags.Sleep))
            {
                ch.send("They must be sapped or sleeping to do that.\r\n");
                return;
            }
            if (ch.Fighting != null)
            {
                ch.send("You cannot blindfold someone while fighting.\n\r");
                return;
            }
            if (CheckIsSafe(ch, victim))
            {
                return;
            }

            if (victim.IsAffected(skill))
            {
                ch.Act("$N is already blindfolded.", victim);
                return;
            }
            if (chance < Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n tries to wrap a blindfold around $N's head but fails.", victim, type: ActType.ToRoom);
                ch.Act("You try to wrap a blindfold around $N's head but fail.", victim, type: ActType.ToChar);
                ch.Act("Someone tries to wrap something around your head but fails.", victim, type: ActType.ToVictim);
                return;
            }
            ch.WaitState(skill.waitTime);
            var affect = new AffectData();
            affect.displayName = "blindfold";
            affect.duration = 6;
            affect.where = AffectWhere.ToAffects;
            affect.location = ApplyTypes.Hitroll;
            affect.modifier = -4;
            affect.skillSpell = skill;
            if (!(victim.ImmuneFlags.ISSET(WeaponDamageTypes.Blind) || (victim.Form != null && victim.Form.ImmuneFlags.ISSET(WeaponDamageTypes.Blind))) || victim.IsAffected(AffectFlags.Deafen))
                affect.flags.SETBIT(AffectFlags.Blind);
            affect.affectType = AffectTypes.Skill;
            affect.endMessage = "You can see again.";
            affect.endMessageToRoom = "$n removes the blindfold from $s eyes.";
            victim.AffectToChar(affect);
            ch.WaitState(skill.waitTime);
            ch.Act("$n wraps a blindfold around $N's head.", victim, type: ActType.ToRoom);
            ch.Act("You wrap a blindfold around $N's head.", victim, type: ActType.ToChar);
            ch.Act("Someone wraps something around your head.", victim, type: ActType.ToVictim);
            ch.CheckImprove(skill, true, 1);
            return;
        } // end blindfold
        public static void DoEnvenomWeapon(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("envenom weapon");
            int chance;
            ItemData weapon = null;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.Act("You don't know how to do that.\n\r");
            }
            else if (((arguments.ISEMPTY()) && (weapon = ch.GetEquipment(WearSlotIDs.Wield)) == null) || (!arguments.ISEMPTY() && (weapon = ch.GetItemInventoryOrEquipment(arguments, false)) == null))
            {
                ch.Act("Which weapon did you want to envenom?\n\r");
            }
            else if (!weapon.ItemType.ISSET(ItemTypes.Weapon))
            {
                ch.Act("You can only envenom a weapon.\n\r");
            }
            else if (weapon.IsAffected(skill))
            {
                ch.Act("$p is already envenomed.\n\r", item: weapon);
            }
            else if (chance < Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n tries to envenom $s weapon, but fails.", null, type: ActType.ToRoom);
                ch.Act("You try to envenom $p but fail.", item: weapon, type: ActType.ToChar);
                ch.CheckImprove(skill, false, 1);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n successfully applies envenom to $p.", item: weapon, type: ActType.ToRoom);
                ch.Act("You successfully apply envenom to $p.", item: weapon, type: ActType.ToChar);

                var affect = new AffectData();
                affect.duration = 10 + ch.Level / 4;
                affect.level = ch.Level;
                affect.where = AffectWhere.ToWeapon;
                affect.skillSpell = skill;
                affect.flags.SETBIT(AffectFlags.Poison);
                weapon.affects.Add(affect);
                affect.endMessage = "Envenom on $p wears off.\n\r";
                ch.CheckImprove(skill, true, 1);
            }

        } // end envenom weapon
        public static void DoGag(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("gag");
            int chance;
            Character victim = null;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if ((victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here\n\r");
                return;
            }
            if (!victim.IsAffected(AffectFlags.Sleep))
            {
                ch.send("They must be sapped or sleeping to do that.\r\n");
                return;
            }
            if (ch.Fighting != null)
            {
                ch.send("You cannot gag someone while fighting.\n\r");
                return;
            }
            if (CheckIsSafe(ch, victim))
            {
                return;
            }

            if (victim.IsAffected(skill))
            {
                ch.Act("$N is already gagged.", victim);
                return;
            }
            if (chance < Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n tries to gag $N's mouth but fails.", victim, type: ActType.ToRoom);
                ch.Act("You try to gag $N's mouth but fail.", victim, type: ActType.ToChar);
                ch.Act("Someone tries to gag your mouth but fails.", victim, type: ActType.ToVictim);
                ch.CheckImprove(skill, false, 1);
                return;
            }
            ch.WaitState(skill.waitTime);
            var affect = new AffectData();
            affect.displayName = "gag";
            affect.duration = 6;
            affect.where = AffectWhere.ToAffects;
            affect.skillSpell = skill;
            affect.flags.SETBIT(AffectFlags.Silenced);
            affect.affectType = AffectTypes.Skill;
            affect.endMessage = "You can speak again.";
            affect.endMessageToRoom = "$n removes the gag from $s mouth.";
            victim.AffectToChar(affect);
            ch.WaitState(skill.waitTime);
            ch.Act("$n stuffs a gag into $N's mouth.", victim, type: ActType.ToRoom);
            ch.Act("You stuffs a gag into $N's mouth.", victim, type: ActType.ToChar);
            ch.Act("Someone stuffs something into your mouth.", victim, type: ActType.ToVictim);
            ch.CheckImprove(skill, true, 1);
            return;
        } // end gag
        public static void DoBindHands(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("bind hands");
            int chance;
            Character victim = null;
            ItemData item = null;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if ((victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here\n\r");
                return;
            }
            if (!victim.IsAffected(AffectFlags.Sleep))
            {
                ch.send("They must be sapped or sleeping to do that.\r\n");
                return;
            }
            if (ch.Fighting != null)
            {
                ch.send("You cannot bind someones hands while fighting.\n\r");
                return;
            }
            if (CheckIsSafe(ch, victim))
            {
                return;
            }
            if (victim.IsAffected(skill))
            {
                ch.Act("$S hands are already bound.", victim);
                return;
            }
            if (chance < Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n tries to bind $N's hands but fails.", victim, type: ActType.ToRoom);
                ch.Act("You try to bind $N's hands but fail.", victim, type: ActType.ToChar);
                ch.Act("Someone tries to bind your hands but fails.", victim, type: ActType.ToVictim);
                ch.CheckImprove(skill, false, 1);
                return;
            }
            ch.WaitState(skill.waitTime);
            var affect = new AffectData();
            affect.displayName = "bind hands";
            affect.duration = 6;
            affect.where = AffectWhere.ToAffects;
            affect.skillSpell = skill;
            affect.flags.SETBIT(AffectFlags.BindHands);
            affect.affectType = AffectTypes.Skill;
            affect.endMessage = "You can hold things in your hands again.";
            affect.endMessageToRoom = "$n removes the binding from $s hands.";
            victim.AffectToChar(affect);
            ch.WaitState(skill.waitTime);
            ch.Act("$n wraps a binding around $N's hands.", victim, type: ActType.ToRoom);
            ch.Act("You wraps a binding around $N's hands.", victim, type: ActType.ToChar);
            ch.Act("Someone wraps something around your hands, preventing you from holding anything.", victim, type: ActType.ToVictim);

            foreach (var wearslot in new WearSlotIDs[] { WearSlotIDs.Wield, WearSlotIDs.DualWield, WearSlotIDs.Shield, WearSlotIDs.Held })
            {
                if ((item = victim.GetEquipment(wearslot)) != null)
                {
                    victim.RemoveEquipment(item, false, true);
                    //item.CarriedBy = victim;
                }
            }
            ch.CheckImprove(skill, true, 1);
            return;
        } // end bind hands
        public static void DoBindLegs(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("bind legs");
            int chance;
            Character victim = null;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if ((victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here\n\r");
                return;
            }
            if (!victim.IsAffected(AffectFlags.Sleep))
            {
                ch.send("They must be sapped or sleeping to do that.\r\n");
                return;
            }
            if (ch.Fighting != null)
            {
                ch.send("You cannot bind someones legs while fighting.\n\r");
                return;
            }
            if (CheckIsSafe(ch, victim))
            {
                return;
            }
            if (victim.IsAffected(skill))
            {
                ch.Act("$S legs are already bound.", victim);
                return;
            }
            if (chance < Utility.NumberPercent())
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n tries to bind $N's legs but fails.", victim, type: ActType.ToRoom);
                ch.Act("You try to bind $N's legs but fail.", victim, type: ActType.ToChar);
                ch.Act("Someone tries to bind your legs but fails.", victim, type: ActType.ToVictim);
                ch.CheckImprove(skill, false, 1);
                return;
            }
            ch.WaitState(skill.waitTime);
            var affect = new AffectData();
            affect.displayName = "bind legs";
            affect.duration = 6;
            affect.where = AffectWhere.ToAffects;
            affect.skillSpell = skill;
            affect.flags.SETBIT(AffectFlags.BindLegs);
            affect.affectType = AffectTypes.Skill;
            affect.endMessage = "You can use your legs again.";
            affect.endMessageToRoom = "$n removes the binding from $s legs.";
            victim.AffectToChar(affect);
            ch.WaitState(skill.waitTime);
            ch.Act("$n wraps a binding around $N's legs.", victim, type: ActType.ToRoom);
            ch.Act("You wraps a binding around $N's legs.", victim, type: ActType.ToChar);
            ch.Act("Someone wraps something around your legs, preventing you from moving.", victim, type: ActType.ToVictim);


            ch.CheckImprove(skill, true, 1);
            return;
        } // end bind legs
        public static void DoSleepingDisarm(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("sleeping disarm");
            int chance;
            Character victim = null;
            ItemData obj = null;

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
            }

            else if ((victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here\n\r");
            }
            else if (victim.IsAwake)
            {
                ch.send("They must be sapped or sleeping to do that.\r\n");
            }
            else if (ch.Fighting != null)
            {
                ch.send("You cannot sleeping disarm someone while fighting.\n\r");
            }
            else if (CheckIsSafe(ch, victim))
            {

            }
            else if ((obj = victim.GetEquipment(WearSlotIDs.Wield)) == null && (obj = victim.GetEquipment(WearSlotIDs.DualWield)) == null)
            {
                ch.send("Your opponent is not wielding a weapon.\n\r");
            }
            else if (chance < Utility.NumberPercent())
            {
                ch.Act("$n tries to disarm $N while $E sleeps but fails.", victim, type: ActType.ToRoom);
                ch.Act("You try to disarm $N while $E sleeps but fail.", victim, type: ActType.ToChar);
                ch.Act("Someone tries to disarm you in your sleep fails.", victim, type: ActType.ToVictim);
                ch.CheckImprove(skill, false, 1);
            }

            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n disarms $N while $E sleeps.", victim, type: ActType.ToRoom);
                ch.Act("You disarm $N while $E sleeps.", victim, type: ActType.ToChar);
                ch.Act("Someone tries to disarm you while you sleep.", victim, type: ActType.ToVictim);

                victim.RemoveEquipment(obj, false, true);
                //if (victim.Inventory.Contains(obj))
                //obj.CarriedBy = victim;

                ch.CheckImprove(skill, true, 1);

                if (Utility.Random(0, 1) == 1)
                {
                    victim.StripAffect(AffectFlags.Sleep);
                    victim.Position = Positions.Standing;
                    Combat.SetFighting(victim, ch);
                }
            }
        } // end sleeping disarm
        public static void DoPepperDust(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("pepper dust");
            int chance;
            int dam;
            var level = ch.Level;
            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (chance <= Utility.NumberPercent())
            {
                ch.CheckImprove(skill, false, 1);
                ch.Act("The pepper dust drifts away harmlessly.");
                ch.Act("The pepper dust drifts away harmlessly.", type: ActType.ToRoom);
                return;
            }
            ch.Act("$n throws some pepper dust.", type: ActType.ToRoom);
            ch.Act("You throw some pepper dust.", type: ActType.ToChar);
            ch.CheckImprove(skill, true, 1);

            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (victim.IsSameGroup(ch))
                    continue;
                if (CheckIsSafe(ch, victim))
                    continue;

                if (chance > Utility.NumberPercent())
                {
                    if (ch.IsNPC)
                        level = Math.Min(level, 51);
                    level = Math.Min(level, dam_each.Length - 1);
                    level = Math.Max(0, level);

                    dam = Utility.Random(dam_each[level] / 2, dam_each[level]);

                    Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Sting);

                    if (!victim.IsAffected(skill))
                    {
                        if ((victim.ImmuneFlags.ISSET(WeaponDamageTypes.Blind) || (victim.Form != null && victim.Form.ImmuneFlags.ISSET(WeaponDamageTypes.Blind))) && !victim.IsAffected(AffectFlags.Deafen))
                            victim.Act("$n is immune to blindness.", type: ActType.ToRoom);
                        else
                        {
                            var affect = new AffectData();
                            affect.displayName = skill.name;
                            affect.skillSpell = skill;
                            affect.duration = 3;
                            affect.where = AffectWhere.ToAffects;
                            affect.location = ApplyTypes.Hitroll;
                            affect.modifier = -4;
                            affect.skillSpell = skill;
                            victim.Act("$n is blinded by pepper dust in their eyes!", type: ActType.ToRoom);
                            affect.flags.SETBIT(AffectFlags.Blind);
                            victim.AffectToChar(affect);

                            affect.location = ApplyTypes.Dex;
                            affect.modifier = -8;
                            affect.skillSpell = skill;
                            affect.endMessage = "You can see again.";
                            affect.endMessageToRoom = "$n wipes the pepper dust out of $s eyes.";
                            victim.AffectToChar(affect);
                        }
                    }
                    if (victim.Fighting == null)
                        Combat.multiHit(victim, ch);
                }
                ch.CheckImprove(skill, true, 1);
            }
        } // end pepper dust
        public static void DoBlisterAgent(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("blister agent");
            int chance;
            int dam;
            var level = ch.Level;
            var bleeding = SkillSpell.SkillLookup("bleeding");

            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };
            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (chance <= Utility.NumberPercent())
            {
                ch.CheckImprove(skill, false, 1);
                ch.Act("The blister agent drifts away harmlessly.");
                ch.Act("The blister agent drifts away harmlessly.", type: ActType.ToRoom);
                return;
            }
            ch.Act("$n throws some blister agent.", type: ActType.ToRoom);
            ch.Act("You throw some blister agent.", type: ActType.ToChar);
            ch.CheckImprove(skill, true, 1);

            foreach (var victim in ch.Room.Characters)
            {
                if (victim.IsSameGroup(ch))
                    continue;
                if (CheckIsSafe(ch, victim))
                    continue;

                if (chance > Utility.NumberPercent())
                {
                    if (ch.IsNPC)
                        level = Math.Min(level, 51);
                    level = Math.Min(level, dam_each.Length - 1);
                    level = Math.Max(0, level);

                    dam = Utility.Random(dam_each[level] / 2, dam_each[level]);

                    Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Sting);

                    if (!victim.IsAffected(skill))
                    {
                        var affect = new AffectData();
                        affect.displayName = skill.name;
                        affect.skillSpell = skill;
                        affect.level = ch.Level;
                        affect.duration = 6;
                        affect.where = AffectWhere.ToAffects;
                        affect.location = ApplyTypes.Str;
                        affect.modifier = -8;
                        victim.Act("$n is burned by blister agent!", type: ActType.ToRoom);
                        //affect.flags.SETBIT(AffectFlags.Blind);
                        victim.AffectToChar(affect);

                        if (victim.FindAffect(bleeding) == null)
                        {
                            affect.location = ApplyTypes.None;
                            affect.skillSpell = bleeding;
                            victim.AffectToChar(affect);
                        }
                        affect.skillSpell = skill;
                        affect.location = ApplyTypes.Hitroll;
                        affect.modifier = -8;
                        victim.AffectToChar(affect);

                        affect.location = ApplyTypes.AC;
                        affect.modifier = 400;
                        affect.skillSpell = skill;
                        affect.endMessage = "The blister agent surrounding you finally wears off.";
                        affect.endMessageToRoom = "The blister agent surrounding $n wears off.";
                        victim.AffectToChar(affect);
                    }
                    if (victim.Fighting == null)
                        Combat.multiHit(victim, ch);
                }
            }
        } // end blister agent
        private static void CheckSuckerHit(Character victim)
        {
            if (victim.Room != null && victim.Position == Positions.Fighting)
                foreach (var ch in victim.Room.Characters.ToArray())
                {
                    if (ch.Fighting == victim) CheckSuckerHit(ch, victim);
                }
        }
        private static void CheckSuckerHit(Character ch, Character victim)
        {
            int dam = 0;
            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("sucker hit");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                return;
            }
            if (skillPercent > Utility.NumberPercent())
            {
                dam += Utility.dice(2, (ch.Level) / 2, (ch.Level) / 4);

                if (ch.Fighting == null)
                {
                    ch.Position = Positions.Fighting;
                    ch.Fighting = victim;
                }
                ch.Act("You take advantage of $N's hesitation and sucker hits them.\n\r", victim, type: ActType.ToChar);
                ch.Act("$n takes advantage of $s hesitation and sucker hits $N.\n\r", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n takes advantage of your hesitation and sucker hits you.\n\r", victim, type: ActType.ToVictim);

                Combat.Damage(ch, victim, dam, skill);

                ch.CheckImprove(skill, true, 1);
            }

            else ch.CheckImprove(skill, false, 1);
        } // end sucker hit
        public static void DoEarClap(Character ch, string arguments)
        {

            var dam_each = new int[]
            {
                 0,
        5,  6,  7,  8,  9,  11,  14,  16,  21,  26,
        31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
        59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
        66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
        74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
        95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("ear clap");
            int chance;
            int dam;
            var level = ch.Level;
            Character victim = null;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
            }
            else if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.Act("You aren't fighting anybody.\n\r");
            }
            else if (victim.IsAffected(skill))
            {
                ch.Act("$N is already affected by your earl clap!\n\r", victim);
            }
            else if (chance > Utility.NumberPercent())
            {
                if (ch.IsNPC)
                    level = Math.Min(level, 51);
                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                ch.Act("$n deafens $N with a powerful ear clap.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n deafens you with a powerful ear clap.", victim, type: ActType.ToVictim);
                ch.Act("You deafen $N with a powerful ear clap.", victim, type: ActType.ToChar);

                ch.CheckImprove(skill, true, 1);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bash);
                ch.WaitState(skill.waitTime);

                var affect = new AffectData();
                affect.skillSpell = skill;
                affect.level = ch.Level;
                affect.duration = 3;
                affect.endMessage = "The ringing in your ears lessens.";
                affect.affectType = AffectTypes.Skill;
                affect.where = AffectWhere.ToAffects;
                affect.displayName = "ear clap";
                affect.flags.SETBIT(AffectFlags.Deafen);
                victim.AffectToChar(affect);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("$n tries to deafen $N with a powerful ear clap but misses.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to deafen you with a powerful ear clap but misses.", victim, type: ActType.ToVictim);
                ch.Act("You try to deafen $N with a powerful ear clap but miss.", victim, type: ActType.ToChar);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bash);
                ch.CheckImprove(skill, false, 1);
            }
            return;
        } // ear clap

        private static void CheckPreyOnTheWeak(Character ch, Character victim, ref float dam)
        {
            int skillPercent = 0;
            var skill = SkillSpell.SkillLookup("prey on the weak");

            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                return;
            }

            if (skillPercent > Utility.NumberPercent())
            {
                var health = victim.HitPoints.Percent(victim.MaxHitPoints);

                if (health > 90) dam += 0;
                else if (health > 85) dam *= 1.1f;
                else if (health > 80) dam *= 1.2f;
                else if (health > 75) dam *= 1.3f;
                else if (health > 70) dam *= 1.4f;
                else if (health > 65) dam *= 1.5f;
                else if (health > 60) dam *= 1.6f;
                else if (health > 55) dam *= 1.7f;
                else if (health > 50) dam *= 1.8f;
                else if (health > 45) dam *= 1.9f;
                else if (health > 40) dam *= 2.0f;
                else if (health > 35) dam *= 2.1f;
                else if (health > 30) dam *= 2.2f;
                else if (health > 25) dam *= 2.3f;
                else if (health > 20) dam *= 2.4f;
                else dam *= 2.5f;

                ch.CheckImprove(skill, true, 1);
            }
            else ch.CheckImprove(skill, false, 1);
        } // prey on the weak
        public static void DoStenchCloud(Character ch, string arguments)
        {
            var skill = SkillSpell.SkillLookup("stench cloud");
            int chance;
            int dam;
            var level = ch.Level;
            var dam_each = new int[]
            {
                0,
                4,  5,  6,  7,  8,   10,  13,  15,  20,  25,
                30, 35, 40, 45, 50, 55, 55, 55, 56, 57,
                58, 58, 59, 60, 61, 61, 62, 63, 64, 64,
                65, 66, 67, 67, 68, 69, 70, 70, 71, 72,
                73, 73, 74, 75, 76, 76, 77, 78, 79, 79,
                90,110,120,150,170,200,230,500,500,500
            };

            if ((chance = ch.GetSkillPercentage(skill)) + 20 <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (chance <= Utility.NumberPercent())
            {
                ch.CheckImprove(skill, false, 1);
                ch.Act("The stench cloud drifts away harmlessly.");
                ch.Act("The stench cloud drifts away harmlessly.", type: ActType.ToRoom);
                return;
            }
            ch.Act("$n generates a stench cloud.", type: ActType.ToRoom);
            ch.Act("You generate a stench cloud.", type: ActType.ToChar);
            ch.CheckImprove(skill, true, 1);

            foreach (var victim in ch.Room.Characters.ToArray())
            {
                if (victim.IsSameGroup(ch))
                    continue;
                if (CheckIsSafe(ch, victim))
                    continue;

                if (chance > Utility.NumberPercent())
                {
                    if (ch.IsNPC)
                        level = Math.Min(level, 51);
                    level = Math.Min(level, dam_each.Length - 1);
                    level = Math.Max(0, level);

                    dam = Utility.Random(dam_each[level] / 2, dam_each[level]);


                    if (!victim.IsAffected(skill))
                    {
                        Combat.Damage(victim, victim, dam, skill, WeaponDamageTypes.Sting, ch.Name);

                        var affect = new AffectData();
                        affect.displayName = skill.name;
                        affect.skillSpell = skill;
                        affect.duration = 6;
                        affect.where = AffectWhere.ToAffects;
                        affect.location = ApplyTypes.AC;
                        affect.modifier = 100;
                        affect.skillSpell = skill;
                        affect.flags.SETBIT(AffectFlags.Smelly);
                        affect.endMessage = "You stop smelling so bad.";
                        affect.endMessageToRoom = "$n stops smelling so bad.";
                        victim.AffectToChar(affect);
                        victim.Act("$n is covered by a stench cloud!", type: ActType.ToRoom);

                    }
                    //if (victim.Fighting == null)
                    //   Combat.multiHit(victim, ch);
                }

            }
        } // end stench cloud
        public static void DoShiv(Character ch, string arguments)
        {
            Character victim = null;
            SkillSpell skill = SkillSpell.SkillLookup("shiv");
            int skillPercent;
            ItemData weapon;
            if ((skillPercent = ch.GetSkillPercentage(skill) + 20) <= 21)
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
            else if (((weapon = ch.GetEquipment(WearSlotIDs.Wield)) == null || weapon.WeaponType != WeaponTypes.Dagger) &&
               ((weapon = ch.GetEquipment(WearSlotIDs.DualWield)) == null || weapon.WeaponType != WeaponTypes.Dagger))
            {
                ch.Act("You must be wielding a dagger to shiv.");
            }
            else if (skillPercent > Utility.NumberPercent())
            {
                AffectData affect;
                ch.WaitState(skill.waitTime);

                if ((affect = victim.FindAffect(skill)) == null)
                {
                    affect = new AffectData()
                    {
                        skillSpell = skill,
                        displayName = "shiv",
                        duration = 5,
                        modifier = -2,
                        location = ApplyTypes.Strength,
                        affectType = AffectTypes.Skill,
                        level = ch.Level,
                    };

                    affect.endMessage = "Your shiv bleeding subsides.";
                    victim.AffectToChar(affect);
                }
                else
                {
                    victim.AffectApply(affect, true, true);
                    affect.modifier -= 2;
                    affect.duration = 5;
                    victim.AffectApply(affect, false, true);
                }

                ch.Act("You shiv $N in the gut with $p.", victim, weapon);
                ch.Act("$n shivs you in the gut with $p!", victim, weapon, type: ActType.ToVictim);
                ch.Act("$n shivs $N in the gut with $p!", victim, weapon, type: ActType.ToRoomNotVictim);

                float damage = (weapon.DamageDice.Roll() + ch.DamageRoll) * 3;

                CheckEnhancedDamage(ch, ref damage);
                CheckPreyOnTheWeak(ch, victim, ref damage);

                ch.CheckImprove(skill, true, 1);

                Combat.Damage(ch, victim, (int)damage, skill, weapon.WeaponDamageType.Type);
            }
            else
            {
                ch.WaitState(skill.waitTime);
                ch.Act("You attempt to shiv $N in the gut with $p.", victim, weapon);
                ch.Act("$n attempts to shiv your gut with $p!", victim, weapon, type: ActType.ToVictim);
                ch.Act("$n attempts to shiv $N in the gut with $p!", victim, weapon, type: ActType.ToRoomNotVictim);

                ch.CheckImprove(skill, false, 1);
                Combat.Damage(ch, victim, 0, skill, weapon.WeaponDamageType.Type);

            }
        } // end shiv
        public static void DoTalonStrike(Character ch, string arguments)
        {
            var dam_each = new int[]
            {
                 0,
                5,  6,  7,  8,  9,  11, 14, 16, 21, 26,
                31, 36, 41, 46, 51, 56, 56, 56, 57, 58,
                59, 59, 60, 61, 62, 62, 63, 64, 65, 65,
                66, 67, 68, 68, 69, 70, 71, 71, 72, 73,
                74, 74, 75, 76, 77, 77, 78, 79, 80, 80,
                95, 115, 125, 155, 175, 210, 240, 550, 550, 550
            };
            var skill = SkillSpell.SkillLookup("talon strike");
            var venom = SkillSpell.SkillLookup("venom");
            var bleeding = SkillSpell.SkillLookup("bleeding");
            int chance;
            int dam;
            var level = ch.Level;

            if ((chance = ch.GetSkillPercentage(skill) + 20) <= 21)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }
            if (ch.Form != null)
            {
                dam_each = new int[]
                {
                    75,
                    100,
                    150,
                    200
                };
                level = 3 - (int)ch.Form.Tier;
            }

            Character victim = null;

            if (ch.IsAffected(skill))
            {
                ch.send("You aren't ready to spit venom again yet!\n\r");
                return;
            }
            if (!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null)
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            else if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anybody.\n\r");
                return;
            }
            ch.WaitState(skill.waitTime);

            if (ch.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);

            if (!victim.IsAffected(skill))
            {
                var affect = new AffectData();
                affect.skillSpell = bleeding;
                affect.displayName = bleeding.name;
                affect.duration = 5;
                affect.ownerName = ch.Name;
                affect.level = ch.Level;
                affect.affectType = AffectTypes.Skill;
                affect.displayName = "bleeding";
                affect.where = AffectWhere.ToAffects;
                victim.AffectToChar(affect);

                affect.skillSpell = skill;
                affect.location = ApplyTypes.DamageRoll;
                affect.duration = 5;
                affect.modifier = -10;
                victim.AffectToChar(affect);

                affect.skillSpell = skill;
                affect.location = ApplyTypes.Hitroll;
                affect.duration = 5;
                affect.modifier = -10;
                victim.AffectToChar(affect);

                affect.skillSpell = venom;
                affect.displayName = venom.name;
                affect.duration = 5;
                affect.endMessage = "Your wound closes and your poison runs it's course..";
                affect.endMessageToRoom = "$n wounds close and $s poison ends.";
                victim.AffectToChar(affect);

                dam = Utility.Random(dam_each[level], dam_each[level] * 2);
                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Claw);
            }
            else
            {
                ch.Act("They are already wounded by your powerful talon strike!");
            }
        } // end talon strike
        public static void DoPuncturingBite(Character ch, string arguments)
        {
            var dam_each = new int[]
            {
                36,
                63,
                78,
                95
            };
            int dam;
            int healamount;
            Character victim = null;
            var skill = SkillSpell.SkillLookup("puncturing bite");
            var bleeding = SkillSpell.SkillLookup("bleeding");
            int chance;

            if ((chance = ch.GetSkillPercentage(skill)) <= 1)
            {
                ch.send("You don't know how to do that.\n\r");
                return;
            }

            if (ch.Form == null)
            {
                ch.send("Only animals can puncture bite someone.\n\r");
                return;
            }

            if (arguments.ISEMPTY() && (victim = ch.Fighting) == null)
            {
                ch.send("You aren't fighting anyone.\n\r");
                return;
            }
            else if ((!arguments.ISEMPTY() && (victim = ch.GetCharacterFromRoomByName(arguments)) == null) || (arguments.ISEMPTY() && (victim = ch.Fighting) == null))
            {
                ch.send("You don't see them here.\n\r");
                return;
            }
            var level = 3 - (int)ch.Form.Tier;
            chance += 20;
            //var wield = ch.GetEquipment(WearSlotIDs.Wield);

            ch.WaitState(skill.waitTime);
            if (chance > Utility.NumberPercent())
            {
                ch.Act("$n punctures $N's skin with $s sharp teeth and quickly feeds on their exposed blood.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n punctures your skin with $s sharp teeth and quickly feeds on your exposed blood.", victim, type: ActType.ToVictim);
                ch.Act("You puncture $N's skin with your sharp teeth and quickly feed on their expolsed blood.", victim, type: ActType.ToChar);

                level = Math.Min(level, dam_each.Length - 1);
                level = Math.Max(0, level);

                healamount = Utility.Random(dam_each[level], dam_each[level] * 2);
                dam = Utility.Random(dam_each[level], dam_each[level] * 2);

                Combat.Damage(ch, victim, dam, skill, WeaponDamageTypes.Bite);

                ch.HitPoints = Math.Min(ch.HitPoints + healamount, ch.MaxHitPoints);
                ch.Hunger = 48;
                ch.Starving = 0;
                ch.Thirst = 48;
                ch.Dehydrated = 0;

                var affect = new AffectData();
                affect.skillSpell = bleeding;
                affect.displayName = bleeding.name;
                affect.duration = 5;
                affect.ownerName = ch.Name;
                affect.level = ch.Level;
                affect.affectType = AffectTypes.Skill;
                affect.displayName = "bleeding";
                affect.where = AffectWhere.ToAffects;
                victim.AffectToChar(affect);

                affect.skillSpell = skill;
                affect.location = ApplyTypes.Strength;
                affect.modifier = -2;
                affect.duration = 5;
                affect.endMessage = "A bleeding wound puncture finally closes.";
                victim.AffectToChar(affect);
            }
            else
            {
                ch.Act("$n tries to puncture $N's skin with $s sharp teeth but fails to make contact.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n tries to puncture your skin with $s sharp teeth but fails to make contact.", victim, type: ActType.ToVictim);
                ch.Act("You try to puncture $N's skin with your sharp teeth but fail to make contact.", victim, type: ActType.ToChar);
                Combat.Damage(ch, victim, 0, skill, WeaponDamageTypes.Bite);
            }
            return;
        } // end puncture bite
    } // end combat
} // end namespace
