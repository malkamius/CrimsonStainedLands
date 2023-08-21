using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using global::CrimsonStainedLands.Extensions;

namespace CrimsonStainedLands
{
   
    public class Character : IDisposable
    {
        public static List<Character> Characters = new List<Character>();

        public static WearSlot[] WearSlots = new WearSlot[]
        {
            new WearSlot() { id = WearSlotIDs.LeftFinger, flag = WearFlags.Finger, slot =   "<ring>             ", wearString = "on your finger", wearStringOthers = "on their finger"},
            new WearSlot() { id = WearSlotIDs.RightFinger, flag = WearFlags.Finger, slot =  "<ring>             ", wearString = "on your finger", wearStringOthers = "on their finger"},
            new WearSlot() { id = WearSlotIDs.Neck1, flag = WearFlags.Neck, slot =          "<around neck>      ", wearString = "around your neck", wearStringOthers = "around their neck"},
            new WearSlot() { id = WearSlotIDs.Neck2, flag = WearFlags.Neck, slot =          "<around neck>      ", wearString = "around your neck", wearStringOthers = "around their neck"},
            new WearSlot() { id = WearSlotIDs.Head, flag = WearFlags.Head, slot =           "<on head>          ", wearString = "on your head", wearStringOthers = "on their head"},
            new WearSlot() { id = WearSlotIDs.Chest, flag = WearFlags.Body, slot =          "<on chest>         ", wearString = "on your chest", wearStringOthers = "on their chest"},
            new WearSlot() { id = WearSlotIDs.Arms, flag = WearFlags.Arms, slot =           "<on arms>          ", wearString = "on your arms", wearStringOthers = "on their arms"},
            new WearSlot() { id = WearSlotIDs.Legs, flag = WearFlags.Legs, slot =           "<on legs>          ", wearString = "on your legs", wearStringOthers = "on their legs"},
            new WearSlot() { id = WearSlotIDs.Feet, flag = WearFlags.Feet, slot =           "<on feet>          ", wearString = "on your feet", wearStringOthers = "on their feet"},
            new WearSlot() { id = WearSlotIDs.Hands, flag = WearFlags.Hands, slot =         "<on hands>         ", wearString = "on your hands", wearStringOthers = "on their hands"},
            new WearSlot() { id = WearSlotIDs.Waist, flag = WearFlags.Waist, slot =         "<on waist>         ", wearString = "on your waist", wearStringOthers = "on their waist"},
            new WearSlot() { id = WearSlotIDs.About, flag = WearFlags.About, slot =         "<about body>       ", wearString = "about your body", wearStringOthers = "about their body"},
            new WearSlot() { id = WearSlotIDs.LeftWrist, flag = WearFlags.Wrist, slot =     "<wrists>           ", wearString = "on your wrists", wearStringOthers = "on their wrists"},
            new WearSlot() { id = WearSlotIDs.RightWrist, flag = WearFlags.Wrist, slot =    "<wrists>           ", wearString = "on your wrists", wearStringOthers = "on their wrists"},
            new WearSlot() { id = WearSlotIDs.Wield, flag = WearFlags.Wield, slot =         "<wielded>          ", wearString = "in your main hand", wearStringOthers = "in their main hand"},
            new WearSlot() { id = WearSlotIDs.DualWield, flag = WearFlags.Wield, slot =     "<dual wielding>    ", wearString = "in your offhand", wearStringOthers = "in their offhand"},
            new WearSlot() { id = WearSlotIDs.Held, flag = WearFlags.Hold, slot =           "<offhand>          ", wearString = "in your offhand", wearStringOthers = "in their offhand"},
            new WearSlot() { id = WearSlotIDs.Shield, flag = WearFlags.Shield, slot =       "<offhand>          ", wearString = "in your offhand", wearStringOthers = "in their offhand"},
            new WearSlot() { id = WearSlotIDs.Floating, flag = WearFlags.Float, slot =      "<floating nearby>  ", wearString = "floating near you", wearStringOthers = "floating near them"},
            new WearSlot() { id = WearSlotIDs.Tattoo, flag = WearFlags.Tattoo, slot =       "<tattood>          ", wearString = "on your arm", wearStringOthers = "on their arm"}
        };

        public HashSet<WizardNet.Flags> WiznetFlags = new HashSet<WizardNet.Flags>();

        public string Name { get; set; }

        private string _description = "";
        public string Description
        {
            get
            {
                var regex = new Regex("(?m)^\\s+");
                if (_description.StartsWith("."))
                    return _description.Replace("\n\r", "\n").Replace("\r\n", "\n");
                return regex.Replace(_description.Trim(), "");
            }
            set
            {
                if (value != null) _description = value.Replace("\n\r", "\n").Replace("\r\n", "\n"); else _description = "";
            }
        }
        public string ShortDescription { get; set; } = "";
        public string LongDescription { get; set; } = "";

        public string NightShortDescription { get; set; } = "";
        public string NightLongDescription { get; set; } = "";
        public WeaponDamageMessage WeaponDamageMessage = null;
        public Alignment Alignment;
        public Ethos Ethos;
        private Positions _position;
        public Race Race;
        public PcRace PcRace;
        private Sexes _Sex;

        public RoomData Room;

        public CharacterSize Size = CharacterSize.Medium;
        public string Material;

        public List<ItemData> Inventory = new List<ItemData>();
        public Dictionary<WearSlotIDs, ItemData> Equipment = new Dictionary<WearSlotIDs, ItemData>();
        public List<AffectData> AffectsList = new List<AffectData>();
        public Dictionary<SkillSpell, LearnedSkillSpell> Learned = new Dictionary<SkillSpell, LearnedSkillSpell>();
        public List<AffectFlags> AffectedBy = new List<AffectFlags>();

        public AreaData EditingArea { get; internal set; }
        public RoomData EditingRoom { get; internal set; }
        public NPCTemplateData EditingNPCTemplate { get; internal set; }
        public ItemTemplateData EditingItemTemplate { get; internal set; }
        public HelpData EditingHelp { get; internal set; }
        public Dice DamageDice;

        public int HitPoints;
        public int MaxHitPoints;
        public int MovementPoints;
        public int MaxMovementPoints;
        public int ManaPoints;
        public int MaxManaPoints;
        public int Level;
        public int Xp;
        public int XpTotal;

        public HashSet<WeaponDamageTypes> ImmuneFlags = new HashSet<WeaponDamageTypes>();
        public HashSet<WeaponDamageTypes> ResistFlags = new HashSet<WeaponDamageTypes>();
        public HashSet<WeaponDamageTypes> VulnerableFlags = new HashSet<WeaponDamageTypes>();

        public GuildData Guild;

        public Character LastFighting;
        private Character _fighting;
        public Character Fighting { get { return _fighting; } set { _fighting = value; if (value != null && Leader == null && IsNPC) LastFighting = value; } }
        public Character Following;
        public Character Leader;
        public Character Pet = null;

        public List<Character> Group = new List<Character>();

        public int Wait;
        public int Daze;
        public int HitRoll;
        public int DamageRoll;
        public int ArmorClass;
        public long Silver;
        public long Gold;

        public int Hunger;
        public int Thirst;
        public int Drunk;
        public int Starving;
        public int Dehydrated;

        public int Trains;
        public int Practices;
        public int SavingThrow { get; set; }

        public HashSet<ActFlags> Flags = new HashSet<ActFlags>();
        public PhysicalStats PermanentStats = new PhysicalStats(0, 0, 0, 0, 0, 0);
        public PhysicalStats ModifiedStats = new PhysicalStats(0, 0, 0, 0, 0, 0);

        internal Character Master = null;
        internal bool IsShop = false;

        public int ArmorBash = 0;
        public int ArmorPierce = 0;
        public int ArmorSlash = 0;
        public int ArmorExotic = 0;
        public Positions DefaultPosition;

        public Dictionary<ShapeshiftForm, int> Forms = new Dictionary<ShapeshiftForm, int>();

        public string LastCommand = "";

        public Character SnoopedBy = null;
        public Character SwitchedBy = null;
        public Character Switched = null;


        public Positions Position
        {
            get { return _position; }
            set
            {
                if (value != Positions.Sleeping && IsAffected(SkillSpell.SkillLookup("camp")))
                    AffectFromChar(FindAffect("camp"), AffectRemoveReason.ChangedPosition);
                if (value != Positions.Sleeping && IsAffected(AffectFlags.Sleep))
                    AffectFromChar(FindAffect(AffectFlags.Sleep), AffectRemoveReason.ChangedPosition);
                if (value == Positions.Standing && Room != null)
                {
                    foreach (var other in Room.Characters)
                    {
                        if (other.IsNPC && other.Position != value && other.Leader == this)
                        {
                            CharacterDoFunctions.DoStand(other, "");
                        }
                    }
                }

                if (_position != value)
                {
                    var removeaffects = (from aff in AffectsList where aff.RemoveAndSaveFlags.ISSET(AffectData.StripAndSaveFlags.RemoveOnPositionChange) select aff).ToArray();
                    foreach (var aff in removeaffects)
                        AffectFromChar(aff, AffectRemoveReason.ChangedPosition);
                }
                _position = value;
            }
        }
        public Sexes Sex
        {
            get
            {
                var sexmod = (from aff in AffectsList where aff.location == ApplyTypes.Sex select aff.modifier).Sum();
                sexmod = sexmod % 3;
                switch (Math.Abs(sexmod))
                {
                    default:
                    case 0:
                        return _Sex;
                    case 1: return Sexes.Either;
                    case 2: return _Sex == Sexes.Male ? Sexes.Female : Sexes.Male;
                }
            }
            set
            {
                if (_Sex != value) { _Sex = value; }
            }
        }
        public ShapeshiftForm Form { get; set; }

        public DateTime MapLastDisplayed { get; internal set; }


        public int MaxWeight => WearSlots.Length + Level * 8 + PhysicalStats.StrengthApply[GetCurrentStat(PhysicalStatTypes.Strength)].Carry;
        public int MaxCarry => WearSlots.Length + Level / 5 + PhysicalStats.DexterityApply[GetCurrentStat(PhysicalStatTypes.Dexterity)].Carry;
        public int Carry => Inventory.Count + (from i in Equipment.Values where i != null select 1).Count();

        
        public bool IsImmortal => Level > Game.LEVEL_HERO && !IsNPC;
        public bool IsAwake => Position > Positions.Sleeping;

        public bool IS_OUTSIDE => Room != null && !Room.flags.ISSET(RoomFlags.Indoors) && Room.sector != SectorTypes.Inside;

        public int XpToLevel => (int)(1500f + ((Level - 1) * 1500f * .08f));


        public string GetName
        {
            get
            {
                if (Form != null)
                    return Form.Name;

                return Name;
            }
        }

        public string GetShortDescription(Character onlooker)
        {
            if (onlooker != null && !onlooker.CanSee(this))
            {
                if (!this.IsNPC && this.Level >= Game.LEVEL_IMMORTAL && this.Flags.ISSET(ActFlags.WizInvis))
                    return "An Immortal";
                else
                    return "someone";
            }
            else if (this.Form != null && !Form.ShortDescription.ISEMPTY())
            {
                return this.Form.ShortDescription;
            }
            else if (!ShortDescription.ISEMPTY())
                return ShortDescription;
            else
                return GetName;
        }

        public string GetLongDescription(Character onlooker)
        {
            if (Form != null && !Form.LongDescription.ISEMPTY() && Position == Positions.Standing)
            {
                return Form.LongDescription;
            }
            else if (TimeInfo.IS_NIGHT && !NightLongDescription.ISEMPTY() && (!IsNPC || Position == DefaultPosition))
            {
                return NightLongDescription;
            }
            else if (!LongDescription.ISEMPTY() && (!IsNPC || Position == DefaultPosition))
                return LongDescription;
            else
            {
                string position;
                switch (Position)
                {
                    case Positions.Fighting:
                        if (Fighting != null)
                            position = " is fighting " + (onlooker == Fighting ? " YOU!" : (Fighting.GetShortDescription(onlooker) + "."));
                        else
                            position = " is fighting here.";
                        break;
                    case Positions.Standing:
                        position = " is standing here.";
                        break;
                    case Positions.Resting:
                        position = " is resting here.";
                        break;
                    case Positions.Sitting:
                        position = " is sitting here.";
                        break;
                    case Positions.Sleeping:
                        position = " is sleeping here.";
                        break;
                    case Positions.Incapacitated:
                        position = " is incapacitated here.";
                        break;
                    case Positions.Mortal:
                        position = " is mortally wounded here.";
                        break;
                    case Positions.Dead:
                        position = " is DEAD here.";
                        break;
                    default:
                        position = " is here.";
                        break;
                }

                if (IsAffected(AffectFlags.PlayDead))
                    position = " is DEAD here.";

                return GetShortDescription(onlooker) + position;
            }
        }

        
        public Character()
        {
            if (this is Player)
            {
                Level = 1;
                Xp = 0;
                //Flags.ADDFLAG(ActFlags.Color);
                Flags.ADDFLAG(ActFlags.AutoAssist);
                Flags.ADDFLAG(ActFlags.AutoExit);
                Flags.ADDFLAG(ActFlags.AutoGold);
                Flags.ADDFLAG(ActFlags.AutoSplit);
                Flags.ADDFLAG(ActFlags.AutoLoot);
                Flags.ADDFLAG(ActFlags.AutoSac);
                //xpToLevel is generated off of level
                MaxHitPoints = 100;
                HitPoints = 100;
                MaxManaPoints = 100;
                ManaPoints = 100;
                MaxMovementPoints = 100;
                MovementPoints = 100;
                Silver = 0;
                Gold = 0;
                Hunger = 48;
                Thirst = 48;
                Dehydrated = 0;
                Starving = 0;
                Practices = 5;
                Trains = 3;// ((level - (level % 5)) / 5);
                PermanentStats = new PhysicalStats(20, 20, 20, 20, 20, 20);
                DefaultPosition = Positions.Standing;

                Group.Add(this);
            }

            if (!(this is NPCTemplateData))
                Characters.Add(this);

        }

        public int GetDamageRoll => (Form == null ? DamageRoll : Form.DamageRoll) + PhysicalStats.StrengthApply[GetCurrentStat(PhysicalStatTypes.Strength)].ToDam;
        public int GetHitRoll => (Form == null ? HitRoll : Form.HitRoll) + PhysicalStats.StrengthApply[GetCurrentStat(PhysicalStatTypes.Strength)].ToHit + PhysicalStats.DexterityApply[GetCurrentStat(PhysicalStatTypes.Dexterity)].ToHit;
        public float TotalWeight
        {
            get
            {
                float weight;
                weight = 0;
                foreach (var contained in Inventory) weight += contained.totalweight;
                foreach (var contained in Equipment.Values)
                    if (contained != null) weight += contained.totalweight;
                return weight;
            }
        }

        public int GetLevelSkillLearnedAt(string skillname) => GetLevelSkillLearnedAt(SkillSpell.SkillLookup(skillname));

        public int GetLevelSkillLearnedAt(SkillSpell skill)
        {
            if (skill == null)
                return 60;

            var informskills = new string[] { "control speed", "trance", "meditation", "control phase", "control skin", "control levitation" };
            if (Form != null && !skill.SkillTypes.Contains(SkillSpellTypes.InForm) && !informskills.Contains(skill.name) && skill.spellFun == null)
            {
                return 60;
            }

            //else if (IS_IMMORTAL)
            //    return 1;
            //else if (!skill.skillLevel.ContainsKey(Guild.name))
            //    return 60;
            else if (!skill.PrerequisitesMet(this))
                return 60;
            else if (Learned.ContainsKey(skill) && (Guild == null || !skill.skillLevel.ContainsKey(Guild.name) || Learned[skill].Level < skill.skillLevel[Guild.name]))
                return Learned[skill].Level;
            else if (Guild == null || !skill.skillLevel.ContainsKey(Guild.name))
                return 60;
            else
                return skill.skillLevel[Guild.name];
        }

        public int GetLevelSkillLearnedAtOutOfForm(SkillSpell skill)
        {
            if (skill == null || Guild == null)
                return 60;
            //else if (IS_IMMORTAL)
            //    return 1;
            else if (Learned.ContainsKey(skill) && (!skill.skillLevel.ContainsKey(Guild.name) || Learned[skill].Level < skill.skillLevel[Guild.name]))
                return Learned[skill].Level;
            else if (!skill.skillLevel.ContainsKey(Guild.name))
                return 60;
            else if (!skill.PrerequisitesMet(this))
                return 60;
            else if (Learned.ContainsKey(skill) && Learned[skill].Level < skill.skillLevel[Guild.name])
                return Learned[skill].Level;
            else
                return skill.skillLevel[Guild.name];
        }

        public int GetSkillPercentage(string skillname, bool ignoreLevel = false)
        {
            return GetSkillPercentage(SkillSpell.SkillLookup(skillname), ignoreLevel);
        }

        public int GetSkillPercentage(SkillSpell skill, bool ignoreLevel = false)
        {
            if (Form != null && skill != null)
            {
                var informskills = new string[] { "control speed", "trance", "meditation", "control phase", "control skin", "control levitation" };
                if (Form.FormSkill != skill) // no stack overflow
                {
                    var formskill = GetSkillPercentage(Form.FormSkill);

                    if (Form.Skills.ContainsKey(skill))
                        return (int)(formskill * Form.Skills[skill] / 100);
                    else if (skill.SkillTypes.ISSET(SkillSpellTypes.Form) && Learned.ContainsKey(skill))
                        return Learned[skill].Percentage;
                    else if (informskills.Contains(skill.name) && Learned.ContainsKey(skill))
                        return Learned[skill].Percentage;
                    else if ((informskills.Contains(skill.name) || skill.SkillTypes.ISSET(SkillSpellTypes.Form)) && IsImmortal)
                        return 128;
                    else
                        return 0;
                }
                else if (Form.FormSkill == skill && Learned.ContainsKey(skill))
                    return Learned[skill].Percentage;
                else if (skill.SkillTypes.ISSET(SkillSpellTypes.Form) && Learned.ContainsKey(skill))
                    return Learned[skill].Percentage;
                else if (IsImmortal && !skill.SkillTypes.ISSET(SkillSpellTypes.InForm))
                    return 128;
                else
                    return 0;
            }

            if (skill == null)
                return 0;
            else if (IsImmortal && skill.SkillTypes.Any(type => type != SkillSpellTypes.InForm))
                return 100;
            else if (IsNPC && GetLevelSkillLearnedAt(skill) == 60)
                return 0;
            else if (Learned.ContainsKey(skill) && (ignoreLevel || Level >= GetLevelSkillLearnedAt(skill)) && skill.PrerequisitesMet(this))
                return Learned[skill].Percentage;
            else if (Guild != null && (ignoreLevel || Level >= GetLevelSkillLearnedAt(skill)) && skill.PrerequisitesMet(this))
                return 1;
            else
                return 0;
        }

        public int GetSkillPercentageOutOfForm(string name) => GetSkillPercentageOutOfForm(SkillSpell.SkillLookup(name));
        public int GetSkillPercentageOutOfForm(SkillSpell skill)
        {

            if (skill == null || Guild == null)
                return 0;
            else if (IsImmortal)
                return 100;
            else if (Learned.ContainsKey(skill) && Level >= GetLevelSkillLearnedAt(skill) && skill.PrerequisitesMet(this))
                return Learned[skill].Percentage;
            else if (Guild != null && Level >= GetLevelSkillLearnedAt(skill) && skill.PrerequisitesMet(this))
                return 1;
            else
                return 0;
        }

        public int GetSkillRating(SkillSpell skill)
        {
            int rating;
            if (skill == null || Guild == null)
                return 0;
            else if (IsImmortal)
                return 0;
            else if (Guild != null && skill.rating.TryGetValue(Guild.name, out rating))
                return rating;
            else
                return 0;
        }

        public int GetCurrentStatOutOfForm(PhysicalStatTypes stat)
        {
            if (PcRace != null && PcRace.MaxStats != null)
                return Math.Min(PcRace.MaxStats[stat], Math.Min(25, Math.Max(0, PermanentStats != null && ModifiedStats != null ? PermanentStats[stat] + ModifiedStats[stat] : (IsNPC ? 20 : 3))));
            else
                return Math.Min(25, Math.Max(3, PermanentStats != null && ModifiedStats != null ? PermanentStats[stat] + ModifiedStats[stat] : (IsNPC ? 20 : 3)));
        }

        /// <summary>
        /// Return current stat + modifier, min of 0, max of 25
        /// </summary>
        /// <param name="stat"></param>
        /// <returns></returns>
        public int GetCurrentStat(PhysicalStatTypes stat)
        {
            if (Form != null)
                return Form.Stats[stat];

            if (PcRace != null && PcRace.MaxStats != null)
                return Math.Min(PcRace.MaxStats[stat], Math.Min(25, Math.Max(0, PermanentStats != null && ModifiedStats != null ? PermanentStats[stat] + ModifiedStats[stat] : (IsNPC ? 20 : 3))));
            else
                return Math.Min(25, Math.Max(3, PermanentStats != null && ModifiedStats != null ? PermanentStats[stat] + ModifiedStats[stat] : (IsNPC ? 20 : 3)));
        }

        public int GetModifiedStatUncapped(PhysicalStatTypes stat)
        {
            if (Form != null)
                return Form.Stats[stat];

            return PermanentStats != null && ModifiedStats != null ? PermanentStats[stat] + ModifiedStats[stat] : (IsNPC ? 20 : 3);
        }
        public bool CanSee(Character victim, params GetFlags[] flags)
        {
            if (victim == this) return true;
            return (Flags.ISSET(ActFlags.HolyLight) && (Level >= victim.Level || victim.IsNPC)) ||
                (!IsAffected(AffectFlags.Blind) &&
                (IsAffected(AffectFlags.DetectInvis) || !victim.IsAffected(AffectFlags.Invisible) || victim.IsAffected(AffectFlags.Smelly)) &&
                (IsAffected(AffectFlags.DetectHidden) ||
                    (this.IsAffected(AffectFlags.AcuteVision) && this.Room.IsWilderness) || !victim.IsAffected(AffectFlags.Hide) || victim.IsAffected(AffectFlags.Smelly)) &&
                (IsAffected(AffectFlags.AcuteVision) || !victim.IsAffected(AffectFlags.Camouflage) || victim.IsAffected(AffectFlags.Smelly)) &&
                (!victim.IsAffected(AffectFlags.Burrow)) && (!victim.Flags.ISSET(ActFlags.WizInvis)));
        }

        public bool CanSee(ItemData item)
        {
            return Flags.ISSET(ActFlags.HolyLight) ||
                (!IsAffected(AffectFlags.Blind) && (!item.extraFlags.ISSET(ExtraFlags.VisDeath) || IsImmortal || IsNPC) &&
                (IsAffected(AffectFlags.DetectInvis) || !item.extraFlags.ISSET(ExtraFlags.Invisibility) || IsAffected(AffectFlags.ArcaneVision)));
        }

        public int ExperienceCompute(Character victim, int group_amount, int glevel)
        {
            float xp, base_exp;
            int level_range;
            float mult;

            mult = ((float)(Level) / glevel) * group_amount;
            if (mult >= 1)
            {
                mult = (1 + mult) / 2;
            }
            else
            {
                mult *= mult;
            }
            mult = Utility.URANGE(.25f, mult, 1.1f);

            level_range = victim.Level - Level;

            /* compute the base exp */
            switch (level_range)
            {
                default: base_exp = 0; break;
                case -9: base_exp = 2; break;
                case -8: base_exp = 4; break;
                case -7: base_exp = 7; break;
                case -6: base_exp = 12; break;
                case -5: base_exp = 14; break;
                case -4: base_exp = 25; break;
                case -3: base_exp = 36; break;
                case -2: base_exp = 55; break;
                case -1: base_exp = 70; break;
                case 0: base_exp = 88; break;
                case 1: base_exp = 110; break;
                case 2: base_exp = 131; break;
                case 3: base_exp = 153; break;
                case 4: base_exp = 165; break;
            }

            if (level_range > 4)
                base_exp = 165 + 20 * (level_range - 4);

            if (mult < 1 && level_range > 4)
                base_exp = (4 * base_exp + 165) / 3;

            base_exp *= 3;

            if (victim.Flags.Contains(ActFlags.NoAlign))
                xp = base_exp;

            else if (Alignment == Alignment.Good)
            {
                if (victim.Alignment == Alignment.Evil)
                    xp = (base_exp * 4) / 3;

                else if (victim.Alignment == Alignment.Good)
                    xp = -30;

                else
                    xp = base_exp;
            }
            else if (Alignment == Alignment.Evil) /* for baddies */
            {
                if (victim.Alignment == Alignment.Good)
                    xp = (base_exp * 4) / 3;

                else if (victim.Alignment == Alignment.Evil)
                    xp = base_exp / 2;

                else
                    xp = base_exp;
            }
            else /* neutral */
            {
                xp = base_exp;
            }

            xp = (xp * 2) / 3;

            xp *= mult;
            xp = Utility.Random((int)xp, (int)(xp * 5f / 4f));

            /* adjust for grouping */
            if (group_amount == 2)
                xp = (xp * 5) / 3;
            if (group_amount == 3)
                xp = (xp * 7) / 3;
            if (group_amount > 3)
                xp /= (group_amount - 2);

            return (int)(xp * BonusInfo.ExperienceBonus);
        }

        public void GroupGainExperience(Character victim)
        {
            int xp;
            int members = 0;
            //int group_levels = 0;
            //Character lch;
            if (victim.Room == null) return;
            foreach (var gch in victim.Room.Characters)
            {
                if (IsSameGroup(gch))
                {
                    if (!gch.IsNPC) members++;

                    //group_levels += gch.level;

                    //if (gch.isNPC) group_levels += gch.level;
                }
            }

            if (members == 0)
            {
                members = 1;
            }

            //lch = Leader ?? this;

            foreach (var gch in victim.Room.Characters)
            {

                if (!IsSameGroup(gch) || gch.IsNPC)
                    continue;

                //if (gch.level - lch.level > 8)
                //{
                //    gch.send("You are too high for this group.\n\r");
                //    continue;
                //}

                //if (gch.level - lch.level < -8)
                //{
                //    gch.send("You are too low for this group.\n\r");
                //    continue;
                //}


                xp = gch.ExperienceCompute(victim, members, gch.Level); // group_levels);
                var buf = string.Format("\\CYou receive \\W{0}\\C experience points.\\x\n\r", xp);
                gch.send(buf);
                gch.GainExperience(xp);
            }
        }


        /// <summary>
        /// Apply experience gained, level if threshold reached
        /// </summary>
        /// <param name="gain"></param>
        public void GainExperience(int gain)
        {
            if (IsNPC)
                return;

            /*ch->exp = UMAX( exp_per_level(ch,ch->pcdata->points), ch->exp + gain );*/
            if (Level < Game.LEVEL_HERO)
                Xp += gain;

            if (Xp > XpTotal)
                XpTotal = Xp;

            while (Level < Game.LEVEL_HERO && Xp >=
                XpToLevel * (Level))
            {
                AdvanceLevel();
            }

            return;
        }

        public void AdvanceLevel(bool show = true)
        {
            if (show)
                send("\\gYou raise a level!!  \\x\n\r");
            Level += 1;
            //game.log("{0} gained level {1}", Name, Level);
            WizardNet.Wiznet(WizardNet.Flags.Levels, "{0} gained level {1}", null, null, Name, Level);
            //sprintf(buf, "$N has attained level %d!", ch->level);
            //wiznet(buf, ch, NULL, WIZ_LEVELS, 0, 0);
            GiveAdvanceLevelGains(show);
            if (this is Player)
            {
                if (Guild != null && Guild.Titles.TryGetValue(Level, out var title))
                {
                    if (Sex == Sexes.Female)
                    {
                        this.Title = "the " + title.FemaleTitle;
                    }
                    else
                        this.Title = "the " + title.MaleTitle;

                }
                ((Player)this).SaveCharacterFile();
            }

            if (this is Player)
            {
                foreach (var questprogress in ((Player)this).Quests)
                {
                    if (questprogress.Status == Quest.QuestStatus.InProgress && Level > questprogress.Quest.EndLevel)
                    {
                        QuestProgressData.FailQuest(this, questprogress.Quest);
                    }
                }
            }
        }

        void GiveAdvanceLevelGains(bool show)
        {
            int add_hp;
            int add_mana;
            int add_move;
            int add_prac;

            add_hp = PhysicalStats.ConstitutionApply[GetCurrentStatOutOfForm(PhysicalStatTypes.Constitution)].Hitpoints + (Guild != null ? Utility.Random(
            Guild.HitpointGain,
            Guild.HitpointGainMax) : 3);

            var int_mod = GetCurrentStatOutOfForm(PhysicalStatTypes.Intelligence) - 2;

            //add_mana = utility.rand(7, 10);
            add_mana = (int)Math.Min(1f + Utility.Random((float)int_mod / 2f, int_mod), 16f);
            //if (!str_cmp(guild_table[ch->guild].name, "healer"))
            //    add_mana = UMIN(1 + number_range(int_mod / 2, int_mod), 16);
            //else if (!str_cmp(guild_table[ch->guild].name, "transmuter"))
            //    add_mana = UMIN(1 + number_range(int_mod * 2 / 3, int_mod), 22);
            //else if (!str_cmp(guild_table[ch->guild].name, "necromancer"))
            //    add_mana = UMIN(1 + number_range(int_mod * 2 / 3, int_mod), 20);
            //else if (!str_cmp(guild_table[ch->guild].name, "elementalist"))
            //    add_mana = UMIN(1 + number_range(int_mod * 2 / 3, int_mod), 19);
            //else if (!str_cmp(guild_table[ch->guild].name, "paladin"))
            //    add_mana = UMIN(1 + number_range(int_mod / 3, int_mod), 16);
            //else if (!str_cmp(guild_table[ch->guild].name, "nightwalker"))
            //    add_mana = UMIN(1 + number_range(int_mod / 3, int_mod * 3 / 4), 15);
            //else if (!str_cmp(guild_table[ch->guild].name, "anti-paladin"))
            //    add_mana = UMIN(1 + number_range(int_mod / 3, int_mod * 3 / 4), 15);
            //else add_mana = UMIN(1 + number_range(int_mod / 3, int_mod / 2), 11);

            add_move = Utility.Random(1, (GetCurrentStatOutOfForm(PhysicalStatTypes.Constitution)
                + GetCurrentStatOutOfForm(PhysicalStatTypes.Dexterity)) / 6);
            //add_move = utility.rand(7, 10);

            add_prac = PhysicalStats.WisdomApply[GetCurrentStatOutOfForm(PhysicalStatTypes.Wisdom)].Practice;
            //add_prac = 3;
            if (!Guild.name.StringCmp("warrior"))
            {
                add_hp += Utility.Random(1, 4);
                add_hp = Math.Min(add_hp, 24);
            }
            else if (!Guild.name.StringCmp("paladin"))
            {
                add_hp = Math.Min(add_hp, 17);
            }
            else if (!Guild.name.StringCmp("healer"))
            {
                add_hp = Math.Min(add_hp, 15);
            }
            else if (!Guild.name.StringCmp("mage"))
            {
                add_hp = Math.Min(add_hp, 11);
            }
            else if (!Guild.name.StringCmp("shapeshifter"))
            {
                add_hp = Math.Min(add_hp, 11);
            }
            else
                add_hp = Math.Min(add_hp, 15);

            add_hp = Math.Max(2, add_hp);
            add_mana = Math.Max(2, add_mana);
            add_move = Math.Max(6, add_move);

            MaxHitPoints += add_hp;
            MaxManaPoints += add_mana;
            MaxMovementPoints += add_move;

            // restore on level up
            HitPoints = MaxHitPoints;
            ManaPoints = MaxManaPoints;
            MovementPoints = MaxMovementPoints;

            Practices += add_prac;
            if (Level % 5 == 0)
                Trains += 1;

            if (show)
            {
                send("\\gYou gain {0}/{1} hp, {2}/{3} mana, {4}/{5} move, and {6}/{7} practices.\\x\n\r",
                    add_hp, MaxHitPoints, add_mana, MaxManaPoints,
                    add_move, MaxMovementPoints, add_prac, Practices);

                if (Level % 5 == 0)
                    send("\\YYou gain a train.\\x\n\r");

                if (this is Player && Level % 20 == 0 && Guild.name == "warrior")
                {
                    send("\\YYou gain a weapon specialization.\\x\n\r");
                    ((Player)this).WeaponSpecializations++;
                }

                if (this is Player && this.Guild != null && this.Guild.name == "shapeshifter" && (((Player)this).ShapeFocusMajor == ShapeshiftForm.FormType.None
                    || ((Player)this).ShapeFocusMinor == ShapeshiftForm.FormType.None))
                {
                    send("\\RYou have not chosen both of your shapefocuses yet. Type shapefocus major/minor to set it.\\x\n\r");
                }
            }

            if (this is Player && this.Guild != null && this.Guild.name == "shapeshifter")
                ShapeshiftForm.CheckGainForm(this);
            //if (highest_character < level && level < LEVEL_IMMORTAL) highest_character = level;
            return;
        }

        public bool IsNPC
        {
            get
            {
                return (!(this is Player));
            }
        }

        internal static ItemData CreateMoneyItem(long silver, long gold)
        {
            ItemData obj;

            //OBJ_DATA* obj;

            if (gold < 0 || silver < 0 || (gold == 0 && silver == 0))
            {
                gold = Math.Max(1, gold);
                silver = Math.Max(1, silver);
            }

            if (gold == 0 && silver == 1)
            {
                ItemTemplateData silvertemplate;
                ItemTemplateData.Templates.TryGetValue(1, out silvertemplate);
                obj = new ItemData(silvertemplate);
            }
            else if (gold == 1 && silver == 0)
            {
                ItemTemplateData goldtemplate;
                ItemTemplateData.Templates.TryGetValue(3, out goldtemplate);
                obj = new ItemData(goldtemplate);
            }
            else if (silver == 0)
            {
                ItemTemplateData goldpile;
                ItemTemplateData.Templates.TryGetValue(4, out goldpile);
                obj = new ItemData(goldpile);

                obj.ShortDescription = string.Format(obj.ShortDescription, gold);
                //obj->value[1] = gold;
                //obj->cost = gold;
                //obj->weight = gold / 5;
            }
            else if (gold == 0)
            {
                ItemTemplateData silverpile;
                ItemTemplateData.Templates.TryGetValue(2, out silverpile);
                obj = new ItemData(silverpile);

                obj.ShortDescription = string.Format(obj.ShortDescription, silver);
                //obj->value[0] = silver;
                //obj->cost = silver;
                //obj->weight = silver / 20;
            }

            else
            {
                ItemTemplateData coinpile;
                ItemTemplateData.Templates.TryGetValue(5, out coinpile);
                obj = new ItemData(coinpile);

                obj.ShortDescription = string.Format(obj.ShortDescription, silver, gold);

                //obj = create_object(get_obj_index(OBJ_VNUM_COINS), 0);
                //sprintf(buf, obj->short_descr, silver, gold);
                //free_string(obj->short_descr);
                //obj->short_descr = str_dup(buf);
                //obj->value[0] = silver;
                //obj->value[1] = gold;
                //obj->cost = 100 * gold + silver;
                //obj->weight = gold / 5 + silver / 20;
            }
            obj.Silver = (int)silver;
            obj.Gold = (int)gold;
            obj.Value = (int)(silver + gold * 1000);
            return obj;
        }


        public class Page : IDisposable
        {
            private Character character;
            public Page(Character character)
            {
                this.character = character;
                character.StartPage();
            }
            public void Dispose()
            {
                character.EndPage();
            }
        }

        private StringBuilder PageText = new StringBuilder();
        private bool Paging { get; set; }
        public bool HasPageText { get => PageText.Length > 0; }
        public int ScrollCount = 40;

        public void StartPage()
        {
            PageText.Clear();
            Paging = true;
        }

        public void EndPage()
        {
            Paging = false;
            int index = 0;
            var count = 0;
            var text = PageText.ToString().Replace("\r", "");
            for (index = text.IndexOf("\n", index); count < ScrollCount + 1 && text.Length > index + 2 && index > -1; index = text.IndexOf("\n", index + 1))
                count++;

            if (count > ScrollCount)
                SendPage();
            else
            {
                send(PageText.ToString());
                ClearPage();
            }
        }
        public void SendPage()
        {
            int index = 0;
            int count = 0;
            var text = PageText.ToString();//.Replace("\r", "");
            for (index = text.IndexOf("\n", index); count < ScrollCount && text.Length > index + 1; index = text.IndexOf("\n", index + 1))
                count++;
            if (index >= 0 && index < text.Length)
            {
                PageText.Clear();

                if (text.Length > index + 1)
                    PageText.Append(text.Substring(index + 1, text.Length - index - 1));

                send(text.Substring(0, index + 1));

                if (HasPageText)
                {

                    if (this is Player player)
                    {
                        if (player.state != Player.ConnectionStates.Playing)
                            send("[Hit Enter to Continue]"); // output while playing will display a prompt and this line at time of output

                        player.SittingAtPrompt = true;
                    }
                    else
                        send("[Hit Enter to Continue]");
                }
                else
                    send("\n\r");
            }
            else
            {
                ClearPage();
                send(text + "\n\r");

            }
        }

        public void ClearPage() => PageText.Clear();

        public void send(string data)
        {
            if (this is Player player && player.socket != null)
            {
                if (!Paging)
                    ((Player)this).output.Append(data);
                else
                    PageText.Append(data.Replace("\r", ""));
            }
            if (SwitchedBy != null)
                SwitchedBy.send(data);
            if (SnoopedBy != null)
                SnoopedBy.send(data);
        }

        public void send(string data, params object[] args) => send(string.Format(data, args));

        public void SendToChar(string text, params object[] args) => send(text, args);

        public virtual void SendRaw(string data, bool sendimmediate = true)
        {
            
        }

        public virtual void SendRaw(byte[] data, bool sendimmediate = true)
        {
            
        }

        

        public void RemoveCharacterFromRoom(bool ExecutePrograms = true)
        {
            if (Room != null)
            {
                if(ExecutePrograms)
                Programs.ExecutePrograms(Programs.ProgramTypes.ExitRoom, this, null, Room, "");


                if (IsAffected(AffectFlags.PlayDead))
                {
                    AffectedBy.REMOVEFLAG(AffectFlags.PlayDead);
                    Act("$n stops playing dead.", type: ActType.ToRoom);
                    Act("You stop playing dead.");
                }

                if (IsAffected(AffectFlags.Burrow))
                {
                    AffectedBy.REMOVEFLAG(AffectFlags.Burrow);
                    Act("$n leaves $s burrow.", type: ActType.ToRoom);
                    Act("You leave your burrow.");
                }
                AffectData gentlewalk;
                if ((gentlewalk = (from aff in AffectsList where aff.skillSpell == SkillSpell.SkillLookup("gentle walk") && aff.duration == -1 select aff).FirstOrDefault()) != null)
                {
                    AffectFromChar(gentlewalk, AffectRemoveReason.Other);
                }

                var removeaffects = (from aff in AffectsList where aff.RemoveAndSaveFlags.ISSET(AffectData.StripAndSaveFlags.RemoveOnMove) select aff).ToArray();
                foreach (var aff in removeaffects)
                    AffectFromChar(aff, AffectRemoveReason.Moved);

                if (this is Player && Room.Area.People.Contains(this))
                {
                    Room.Area.People.Remove(this);

                }
                if (Room.Characters.Contains(this))
                    Room.Characters.Remove(this);
                Room = null;
            }
        }

        public void AddCharacterToRoom(RoomData room, bool executeRoomAndNPCProgs = true)
        {
            if (Room != null)
                RemoveCharacterFromRoom();
            Room = room;
            room.Characters.Insert(0, this);

            if (this is Player)
            {
                room.Area.People.Insert(0, this);

                DoActInfo.DoLook(this, "auto");


            }
            if (executeRoomAndNPCProgs)
            {
                Programs.ExecutePrograms(Programs.ProgramTypes.EnterRoom, this, null, Room, "");
            }


        }

        public WearSlot GetEquipmentWearSlot(ItemData item)
        {
            foreach (var slot in WearSlots)
            {
                if (Equipment.ContainsKey(slot.id) && Equipment[slot.id] == item)
                {
                    return slot;
                }
            }
            return null;
        }

        public bool RemoveEquipment(ItemData item, bool show = true, bool overrideNoRemove = false)
        {
            var slot = GetEquipmentWearSlot(item);
            if (slot == null) return false;
            return RemoveEquipment(slot, show, overrideNoRemove);
        }

        public bool RemoveEquipment(WearSlot slot, bool show = true, bool overrideNoRemove = false)
        {
            if (slot == null) return false;
            return RemoveEquipment(slot.id, show, overrideNoRemove);
        }

        public bool RemoveEquipment(WearSlotIDs slotid, bool show = true, bool overrideNoRemove = false)
        {
            if (Equipment.ContainsKey(slotid) && (overrideNoRemove || !Equipment[slotid].extraFlags.Contains(ExtraFlags.NoRemove) ||
                Equipment[slotid].IsAffected(AffectFlags.Greased)))
            {
                var item = Equipment[slotid];
                Equipment.Remove(slotid);
                if (item.Durability != 0) // broken items don't have any affects applied
                    foreach (var aff in item.affects)
                        AffectApply(aff, true);
                if (show)
                {
                    Act("You remove $p.\n\r", null, item, null, ActType.ToChar);
                    Act("$n removes $p.\n\r", null, item, null, ActType.ToRoom);
                }
                AddInventoryItem(item);
                return true;
            }
            else if (Equipment.ContainsKey(slotid) && show)
            {
                Act("You can't remove $p.", null, Equipment[slotid], null, ActType.ToChar);
            }
            return false;
        }

        /// <summary>
        /// Attempts to wear an item for the character.
        /// </summary>
        /// <param name="itemData">The item to wear.</param>
        /// <param name="sendMessage">Specifies whether to send wear messages.</param>
        /// <param name="remove">Specifies whether to remove conflicting items automatically.</param>
        /// <returns>True if the item was successfully worn, false otherwise.</returns>
        public bool wearItem(ItemData itemData, bool sendMessage = true, bool remove = true)
        {
            WearSlot firstSlot = null;
            WearSlot emptySlot = null;
            WearSlot offhandSlotToRemove = null;
            ItemData offhand;
            ItemData wielded;

            string noun = "wear";

            // Check if the item has the UniqueEquip flag and the character is already wearing one
            if (itemData.extraFlags.ISSET(ExtraFlags.UniqueEquip))
            {
                foreach (var other in Equipment)
                {
                    if (other.Value.Vnum == itemData.Vnum)
                    {
                        send("You can only wear one of those at a time.\n\r");
                        return false;
                    }
                }
            }

            // Check if the item has alignment restrictions that conflict with the character's alignment
            if ((itemData.extraFlags.ISSET(ExtraFlags.AntiGood) && Alignment == Alignment.Good) ||
                (itemData.extraFlags.ISSET(ExtraFlags.AntiNeutral) && Alignment == Alignment.Neutral) ||
                (itemData.extraFlags.ISSET(ExtraFlags.AntiEvil) && Alignment == Alignment.Evil))
            {
                Act("You try to wear $p, but it zaps you.", null, itemData, type: ActType.ToChar);
                Act("$n tries to wear $p, but it zaps $m.", null, itemData, type: ActType.ToRoom);
                return false;
            }

            var handslots = new WearSlotIDs[] { WearSlotIDs.Wield, WearSlotIDs.DualWield, WearSlotIDs.Shield, WearSlotIDs.Held };

            wielded = GetEquipment(WearSlotIDs.Wield);

            // Iterate over the available wear slots to find an empty slot or the first suitable slot
            foreach (var slot in WearSlots)
            {
                // Check if the character's hands are bound and the item requires hand slots
                if (itemData.wearFlags.Contains(slot.flag) && handslots.Contains(slot.id) && IsAffected(AffectFlags.BindHands))
                {
                    this.Act("You can't equip that while your hands are bound.\n\r");
                    return false;
                }

                // Check if the item can be dual wielded and the character has the appropriate skill
                if (slot.id == WearSlotIDs.DualWield &&
                    (GetSkillPercentage(SkillSpell.SkillLookup("dual wield")) <= 1 ||
                    itemData.extraFlags.ISSET(ExtraFlags.TwoHands) ||
                    (wielded != null && wielded.extraFlags.ISSET(ExtraFlags.TwoHands))))
                    continue;

                // Check if the item can be worn in the current slot and the slot is empty
                if (itemData.wearFlags.Contains(slot.flag) && !Equipment.ContainsKey(slot.id))
                {
                    emptySlot = slot;
                    break;
                }
                else if (itemData.wearFlags.Contains(slot.flag) && firstSlot == null)
                    firstSlot = slot;
            }

            // Check if a hand slot needs to be emptied for a two-handed item
            if (emptySlot != null && (emptySlot.id == WearSlotIDs.Held || emptySlot.id == WearSlotIDs.DualWield || emptySlot.id == WearSlotIDs.Shield) && Equipment.TryGetValue(WearSlotIDs.Wield, out wielded) && wielded.extraFlags.ISSET(ExtraFlags.TwoHands))
            {
                if (remove)
                {
                    if (!RemoveEquipment(wielded, sendMessage))
                        return false;
                }
                else
                    return false;
            }

            // Check if a two-handed item requires the removal of an offhand item
            if (emptySlot == null && !remove) return false;
            if (itemData.extraFlags.ISSET(ExtraFlags.TwoHands) && (Equipment.TryGetValue(WearSlotIDs.Shield, out offhand) || Equipment.TryGetValue(WearSlotIDs.Held, out offhand) || Equipment.TryGetValue(WearSlotIDs.DualWield, out offhand))
                && (offhandSlotToRemove = GetEquipmentWearSlot(offhand)) != null)
            {
                if (!RemoveEquipment(offhandSlotToRemove, sendMessage))
                {
                    send("You need two hands free for that weapon.\n\r");
                    return false;
                }
            }

            // Check if there is an offhand item that needs to be replaced
            if (
                (
                    (emptySlot != null && (emptySlot.id == WearSlotIDs.DualWield || emptySlot.id == WearSlotIDs.Shield || emptySlot.id == WearSlotIDs.Held))
            ||
                    (firstSlot != null && (firstSlot.id == WearSlotIDs.DualWield || firstSlot.id == WearSlotIDs.Shield || firstSlot.id == WearSlotIDs.Held))
                )
            && (
                    Equipment.TryGetValue(WearSlotIDs.Shield, out offhand) || Equipment.TryGetValue(WearSlotIDs.Held, out offhand) || Equipment.TryGetValue(WearSlotIDs.DualWield, out offhand)
               )
            && (offhandSlotToRemove = GetEquipmentWearSlot(offhand)) != null)
            {
                if (!remove) return false;
                if (RemoveEquipment(offhandSlotToRemove, sendMessage))
                {
                    if (emptySlot == null)
                        emptySlot = firstSlot;
                }
                else
                    return false; // no remove item, should have gotten a message if we are showing messages, no switching out noremove gear in resets
            }
            // Replace an item other than the offhand
            else if (emptySlot == null && firstSlot != null)
            {
                if (RemoveEquipment(firstSlot, sendMessage))
                    emptySlot = firstSlot;
            }

            // Attempt to wear the item in the empty slot
            if (emptySlot != null)
            {
                if (sendMessage)
                {
                    if (emptySlot.id == WearSlotIDs.Wield || emptySlot.id == WearSlotIDs.DualWield)
                    {
                        // Check if the character can wield the item based on their strength
                        if (!IsNPC && itemData.Weight > PhysicalStats.StrengthApply[GetCurrentStat(PhysicalStatTypes.Strength)].Wield)
                        {
                            Act("$p weighs too much for you to wield.", null, itemData);
                            return false;
                        }

                        noun = "wield";
                    }
                    else
                        noun = "wear";

                    // Display wear messages to the character and the room
                    Act("You " + noun + " $p " + emptySlot.wearString + ".\n\r", null, itemData, null, ActType.ToChar);
                    Act("$n " + noun + "s $p " + emptySlot.wearStringOthers + ".\n\r", null, itemData, null, ActType.ToRoom);
                }

                // Add the item to the equipment slot
                Equipment[emptySlot.id] = itemData;

                // Remove the item from the character's inventory
                if (Inventory.Contains(itemData))
                    Inventory.Remove(itemData);
                itemData.CarriedBy = this;

                // Apply the item's affects to the character
                if (itemData.Durability != 0) // Broken items don't apply any affects
                {
                    foreach (var aff in itemData.affects)
                        AffectApply(aff);
                }

                // Execute any wear programs associated with the item
                Programs.ExecutePrograms(Programs.ProgramTypes.Wear, this, itemData, "");

                return true;
            }
            else
            {
                send("You couldn't wear it.\n\r");
                return false;
            }
        }

        /// <summary>
        /// Processes and executes player commands.
        /// </summary>
        /// <param name="arguments">The command arguments.</param>
        public void DoCommand(string arguments)
        {
            if (arguments == "!")
                arguments = LastCommand;
            if (!arguments.ISEMPTY())
                LastCommand = arguments;
            // Check if the character is switched to another entity
            if (Switched != null)
            {
                // Delegate the command execution to the switched entity
                Switched.DoCommand(arguments);
                return;
            }

            string arg = "";

            // Extract the first argument from the command arguments
            arguments = arguments.OneArgument(ref arg);

            if (!string.IsNullOrEmpty(arg))
            {
                // Iterate through all available commands
                foreach (var command in Command.Commands)
                {
                    // Check if the command name matches the extracted argument using string prefix matching
                    if (command.Name.StringPrefix(arg))
                    {
                        // Check the character's position against the minimum position required by the command
                        if (Position < command.MinimumPosition)
                        {
                            // Send an appropriate message based on the character's position
                            switch (Position)
                            {
                                case Positions.Dead:
                                    SendToChar("Lie still; you are DEAD.\n\r");
                                    break;
                                case Positions.Mortal:
                                case Positions.Incapacitated:
                                    SendToChar("You are hurt far too bad for that.\n\r");
                                    break;
                                case Positions.Stunned:
                                    SendToChar("You are too stunned to do that.\n\r");
                                    break;
                                case Positions.Sleeping:
                                    SendToChar("In your dreams or what?\n\r");
                                    break;
                                case Positions.Resting:
                                    SendToChar("Nah... You feel too relaxed...\n\r");
                                    break;
                                case Positions.Sitting:
                                    SendToChar("Better stand up first.\n\r");
                                    break;
                                case Positions.Fighting:
                                    SendToChar("No way! You are still fighting!\n\r");
                                    break;
                            }
                        }
                        else if (Level < command.MinimumLevel)
                        {
                            // Continue to the next command if the character's level is below the minimum level required by the command
                            continue;
                        }
                        else if (command.NPCCommand == false && IsNPC && Leader != null)
                        {
                            // Continue to the next command if the command is not applicable to NPCs with leaders
                            continue;
                        }
                        else if (command.Skill != null && GetSkillPercentage(command.Skill) <= 1)
                        {
                            // Continue to the next command if the character's skill percentage for the command skill is below or equal to 1
                            continue;
                        }
                        else
                        {
                            // Execute the command's action
                            command.Action(this, arguments);
                        }
                        return;
                    }
                }

                // If no matching command is found, check for social commands
                if (!CheckSocials(arg, arguments))
                    send("Huh?\n\r");
            }
            else
            {
                // Send an empty line if no arguments are provided
                send(" ");
            }
        }


        /// <summary>
        /// Checks if the command matches any social commands and performs the corresponding actions.
        /// </summary>
        /// <param name="command">The command to check.</param>
        /// <param name="arguments">The arguments for the command.</param>
        /// <returns>True if a matching social command is found and executed, false otherwise.</returns>
        public bool CheckSocials(string command, string arguments)
        {
            Character victim = null;
            int count = 0;

            // Iterate through all available social commands
            foreach (var social in Social.Socials)
            {
                // Check if the social command name matches the given command using string prefix matching
                if (social.Name.StringPrefix(command))
                {
                    switch (Position)
                    {
                        // Check the character's position against certain positions that restrict social commands
                        case Positions.Dead:
                            SendToChar("Lie still; you are DEAD.\n\r");
                            return true;
                        case Positions.Mortal:
                        case Positions.Incapacitated:
                            SendToChar("You are hurt far too bad for that.\n\r");
                            return true;
                        case Positions.Stunned:
                            SendToChar("You are too stunned to do that.\n\r");
                            return true;
                        case Positions.Sleeping:
                            if (social.Name == "snore")
                                break; // Continue executing the social command
                            SendToChar("In your dreams or what?\n\r");
                            return true;
                    }

                    if (string.IsNullOrEmpty(arguments) && !string.IsNullOrEmpty(social.OthersNoArg) && !string.IsNullOrEmpty(social.CharNoArg))
                    {
                        // Perform the social command actions without any arguments
                        Act(social.OthersNoArg, type: ActType.ToRoom);
                        Act(social.CharNoArg, type: ActType.ToChar);
                    }
                    else if (string.IsNullOrEmpty(arguments) || (victim = GetCharacterFromRoomByName(arguments, ref count)) == null)
                    {
                        // The victim is not found in the room
                        send("They aren't here.\n\r");
                    }
                    else if (victim == this)
                    {
                        // Perform the social command actions targeting oneself
                        Act(social.OthersAuto, victim, type: ActType.ToRoom);
                        Act(social.CharAuto, victim, type: ActType.ToChar);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(social.OthersFound))
                        {
                            // Perform the social command actions with a specific victim found message
                            Act(social.OthersFound, victim, type: ActType.ToRoomNotVictim);
                        }
                        else
                        {
                            // Perform the social command actions without a specific victim found message
                            Act(social.OthersNoArg, type: ActType.ToRoomNotVictim);
                        }

                        if (!string.IsNullOrEmpty(social.CharFound))
                        {
                            // Perform the social command actions with a specific character found message
                            Act(social.CharFound, victim, type: ActType.ToChar);
                        }
                        else
                        {
                            // Perform the social command actions without a specific character found message
                            Act(social.CharNoArg, type: ActType.ToChar);
                        }

                        if (!string.IsNullOrEmpty(social.VictimFound) && victim.Position != Positions.Sleeping)
                        {
                            // Perform the social command actions with a specific victim found message if the victim is not sleeping
                            Act(social.VictimFound, victim, type: ActType.ToVictim);
                        }

                        if (!string.IsNullOrEmpty(social.OthersFound) && !string.IsNullOrEmpty(social.CharFound) && !IsNPC && victim.IsNPC && victim.IsAwake)
                        {
                            // Perform additional social command actions when the character is not an NPC, the victim is an NPC and awake
                            switch (Game.Instance.random.Next(0, 12))
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                case 5:
                                case 6:
                                case 7:
                                case 8:
                                    victim.Act(social.OthersFound, this, type: ActType.ToRoomNotVictim);
                                    victim.Act(social.CharFound, this, type: ActType.ToChar);
                                    victim.Act(social.VictimFound, this, type: ActType.ToVictim);
                                    break;

                                case 9:
                                case 10:
                                case 11:
                                case 12:
                                    victim.Act("$n slaps $N.", this, type: ActType.ToRoomNotVictim);
                                    victim.Act("You slap $N.", this, type: ActType.ToChar);
                                    victim.Act("$n slaps you.", this, type: ActType.ToVictim);
                                    break;
                            }
                        }
                    }
                    return true;
                }
            }

            return false;
        }

        

        /// <summary>
        /// Handles the movement of a character.
        /// </summary>
        /// <param name="direction">The direction of movement.</param>
        /// <param name="follow">Flag indicating whether to follow the character being moved.</param>
        /// <param name="crawl">Flag indicating whether to crawl during movement.</param>
        /// <param name="creep">Flag indicating whether to move stealthily.</param>
        /// <param name="sendWalkMessage">Flag indicating whether to send a walk message.</param>
        /// <param name="first">Flag indicating whether it's the first movement.</param>
        /// <param name="movementCost">The movement cost associated with the movement.</param>
        /// <param name="movementWait">The wait time associated with the movement.</param>
        public void moveChar(Direction direction, bool follow, bool crawl, bool creep = false, bool sendWalkMessage = true, bool first = true, int movementCost = 0, int movementWait = 0)
        {
            // Check if character's legs are bound, preventing movement
            if (IsAffected(AffectFlags.BindLegs))
            {
                this.Act("You can't move while your legs are bound!\n\r");
                return;
            }

            // Check character's position to determine if movement is possible
            if (Position != Positions.Standing)
            {
                if (Position == Positions.Dead)
                {
                    SendToChar("Lie still; you are DEAD.\n\r");
                }
                else if (Position < Positions.Incapacitated)
                {
                    SendToChar("You are hurt far too bad for that.\n\r");
                }
                else if (Position == Positions.Sitting)
                {
                    SendToChar("You are too stunned to do that.\n\r");
                }
                else if (Position < Positions.Resting)
                {
                    SendToChar("Nah... You feel too relaxed...\n\r");
                }
                else if (Position == Positions.Sitting)
                {
                    SendToChar("Better stand up first.\n\r");
                }
                else if (Position == Positions.Fighting)
                {
                    SendToChar("No way! You are still fighting!\n\r");
                }
                else
                {
                    SendToChar("You aren't in the right position?\n\r");
                }
                return;
            }

            // Check if character is currently in combat
            if (Fighting != null)
            {
                SendToChar("No way! You are still fighting!\n\r");
                return;
            }

            var wasInRoom = Room;
            ExitData exit = Room.exits[(int)direction];

            // Check if there is a valid exit in the specified direction
            if (Room != null && exit != null && exit.destination != null)
            {
                var reverseDirections = new string[] { "the south", "the west", "the north", "the east", "below", "above" };
                // Check if the exit is closed or locked, or if character is unable to pass through it
                if ((exit.flags.Contains(ExitFlags.Closed) && (exit.flags.Contains(ExitFlags.NoPass) || !IsAffected(AffectFlags.PassDoor))) ||
                    (exit.flags.Contains(ExitFlags.Locked) && (exit.flags.Contains(ExitFlags.NoPass) || !IsAffected(AffectFlags.PassDoor))) ||
                    (exit.flags.Contains(ExitFlags.Window) && !crawl) ||
                    (!exit.flags.Contains(ExitFlags.Window) && crawl))
                {
                    SendToChar("Alas, you cannot go that way.\n\r");
                    return;
                }

                // Check if character's size is too large to fit through the exit
                if (Size > exit.ExitSize)
                {
                    send("You can't fit.\n\r");
                    return;
                }

                var checkpathfinding = false;
                if (movementCost == 0)
                {
                    checkpathfinding = true;
                    movementCost = RoomData.sectors[wasInRoom.sector].movementCost;
                }
                if (movementWait == 0)
                    movementWait = RoomData.sectors[wasInRoom.sector].movementWait;

                // Check character's movement points for sufficient stamina
                if (!IsNPC && MovementPoints < movementCost)
                {
                    SendToChar("You can barely feel your feet!\n\r");
                    return;
                }

                // Check if character is allowed to enter a room protected by a guild guard
                foreach (var guildguard in Room.Characters.OfType<NPCData>())
                {
                    if (guildguard.Guild != null && guildguard.Guild != Guild && guildguard.Protects.Count > 0 && guildguard.Protects.Contains(exit.destinationVnum) && !this.IsImmortal && !this.IsNPC)
                    {
                        Act("$N steps in your way.", guildguard, type: ActType.ToChar);
                        Act("$N steps in your $n's way.", guildguard, type: ActType.ToRoomNotVictim);
                        DoActCommunication.DoSay(guildguard, "You aren't allowed in there.");
                        return;
                    }
                }

                // Check if flying is required to move through air sectors
                if ((wasInRoom.sector == SectorTypes.Air || exit.destination.sector == SectorTypes.Air) && !IsAffected(AffectFlags.Flying))
                {
                    send("You can't fly.\n\r");
                    return;
                }

                if (creep && (!exit.destination.IsWilderness || exit.destination.IsWater))
                {
                    send("There's no cover there.\n\r");
                    return;
                }

                // Check if movement through water sectors requires swimming or a boat
                if (exit.destination.IsWater &&
                    !IsAffected(AffectFlags.Flying) &&
                    !IsAffected(AffectFlags.Swim) &&
                    !IsAffected(AffectFlags.WaterBreathing) &&
                    !Inventory.Any(b => b.ItemType.ISSET(ItemTypes.Boat)) &&
                    !Equipment.Values.Any(b => b.ItemType.ISSET(ItemTypes.Boat)))
                {
                    send("You need a boat to go there.\n\r");
                    return;
                }

                // Check if movement through underwater sectors requires swimming or water breathing
                if (exit.destination.sector == SectorTypes.Underwater &&
                    !IsAffected(AffectFlags.Swim) &&
                    !IsAffected(AffectFlags.WaterBreathing))
                {
                    send("You need water breathing to go there.\n\r");
                    return;
                }

                // Deduct movement points and apply movement wait
                if (!IsNPC)
                {
                    int chance = 0;
                    if (checkpathfinding && wasInRoom.sector != SectorTypes.City && wasInRoom.sector != SectorTypes.Inside &&
                        (chance = GetSkillPercentage("path finding")) > 1 && chance + 28 >= Utility.NumberPercent())
                    {
                        this.CheckImprove("path finding", true, 1);
                        movementCost /= 2;
                        movementWait /= 2;
                    }
                    else if (checkpathfinding && wasInRoom.sector != SectorTypes.City && wasInRoom.sector != SectorTypes.Inside && chance > 1)
                    {
                        this.CheckImprove("path finding", false, 1);
                    }

                    MovementPoints -= movementCost;
                }
                if (!IsAffected(AffectFlags.FastRunning))
                    WaitState(movementWait);
                else
                    WaitState(movementWait / 3);

                // Strip certain affect flags during movement
                if (AffectedBy.ISSET(AffectFlags.Hide) && !(AffectedBy.ISSET(AffectFlags.Sneak)))
                {
                    StripHidden();
                }
                if (AffectedBy.ISSET(AffectFlags.Camouflage) && !creep)
                {
                    StripCamouflage();
                }
                if (creep && (GetSkillPercentage("creep") + 28) < Utility.NumberPercent())
                {
                    StripCamouflage(creep);
                    this.CheckImprove("creep", false, 1);
                }
                else if (creep)
                {
                    this.CheckImprove("creep", true, 1);
                }

                // Handle cancellation of certain affect flags during movement
                if (IsAffected(AffectFlags.PlayDead))
                {
                    AffectedBy.REMOVEFLAG(AffectFlags.PlayDead);
                    Act("$n stops playing dead.", type: ActType.ToRoom);
                    Act("You stop playing dead.");
                }
                if (IsAffected(AffectFlags.Burrow))
                {
                    AffectedBy.REMOVEFLAG(AffectFlags.Burrow);
                    Act("$n leaves $s burrow.", type: ActType.ToRoom);
                    Act("You leave your burrow.");
                }

                // Handle movement messages
                if (sendWalkMessage && !IsAffected(AffectFlags.Sneak))
                {
                    var quietmovement = this.GetSkillPercentage("quiet movement") + 28;

                    if (quietmovement > 29 && quietmovement > Utility.NumberPercent())
                    {
                        Act("$n leaves.", type: ActType.ToRoom);
                        this.CheckImprove("quiet movement", true, 1);
                    }
                    else
                    {
                        if (quietmovement > 29)
                            this.CheckImprove("quiet movement", false, 1);
                        Act("$n leaves {0}.", type: ActType.ToRoom, args: direction.ToString().ToLower());
                    }
                }

                // Handle track skill for tracking movement
                if (!IsNPC)
                {
                    var trackskill = SkillSpell.SkillLookup("track");
                    var trackAffect = (from aff in Room.affects where aff.skillSpell == trackskill && aff.owner == this select aff).FirstOrDefault();
                    if (trackAffect != null)
                    {
                        trackAffect.Modifier = (int)direction;
                    }
                    else
                    {
                        trackAffect = new RoomAffectData() { Duration = -1, owner = this, skillSpell = trackskill, Modifier = (int)direction };
                        Room.affects.Add(trackAffect);
                    }
                }

                // Move the character to the destination room
                RemoveCharacterFromRoom();
                AddCharacterToRoom(wasInRoom.exits[(int)direction].destination, true);

                // Apply gentle walk affect if applicable
                int gentlewalk = 0;
                var gentleaff = (from a in AffectsList where a.skillSpell == SkillSpell.SkillLookup("gentle walk") && a.duration != -1 select a).FirstOrDefault();
                if (gentleaff != null && (gentlewalk = GetSkillPercentage("gentle walk") + 20) > 21 && gentlewalk > Utility.NumberPercent())
                {
                    var affect = new AffectData();
                    affect.hidden = true;
                    affect.duration = -1;
                    affect.skillSpell = SkillSpell.SkillLookup("gentle walk");
                    CheckImprove(affect.skillSpell, true, 1);
                    AffectToChar(affect);
                }
                else if (gentleaff != null && gentlewalk > 21)
                {
                    CheckImprove("gentle walk", false, 1);
                }

                if (!IsAffected(AffectFlags.Sneak))
                    Act("$n arrives from " + reverseDirections[(int)direction] + ".\n\r", type: ActType.ToRoom);

                /// Executed under AddCharacterToRoom
                // Execute actions upon entering the new room
                //foreach (var npc in Room.Characters.OfType<NPCData>().ToArray())
                //{
                //    var programs = from program in npc.Programs
                //                   where program.Types.ISSET(Programs.ProgramTypes.EnterRoom)
                //                   select program;
                //    foreach (var program in programs)
                //    {
                //        if (program.Execute(this, npc, null, null, null, Programs.ProgramTypes.EnterRoom, ""))
                //            break;
                //    }
                //}

                if (wasInRoom != Room) // Avoid circular follows
                {
                    foreach (var cch in wasInRoom.Characters.ToArray())
                    {
                        if (cch != this && cch.Following == this && (!(cch is Player) || ((Player)cch).socket != null) && cch.IsAwake)
                        {
                            cch.SendToChar("You follow " + Display(this) + " " + direction.ToString().ToLower() + ".\n\r");
                            cch.Act("$n follows $N " + direction.ToString().ToLower() + ".\n\r", this, null, null, ActType.ToRoom);
                            cch.moveChar(direction, follow, crawl, false, sendWalkMessage, false, movementCost, movementWait);
                        }
                        else if (cch != this && cch.Following == this && cch is Player && ((Player)cch).socket == null)
                        {
                            Act("You try to drag $N along, but they've lost their animation.", cch, type: ActType.ToChar);
                            cch.Act("$N tries to drag $n along, but they've lost their animation.", this, type: ActType.ToRoomNotVictim);
                        }
                    }
                }


                //DoLook(this, "auto");

                // Remove track affect upon entering a new room
                if (!IsNPC)
                {
                    var trackskill = SkillSpell.SkillLookup("track");
                    var trackAffect = (from aff in Room.affects where aff.skillSpell == trackskill && aff.owner == this select aff).FirstOrDefault();
                    if (trackAffect != null)
                    {
                        Room.affects.Remove(trackAffect);
                    }
                }

                //if (!IsAffected(AffectFlags.Sneak))
                //    Act("$n arrives from " + reverseDirections[(int)direction] + ".\n\r", type: ActType.ToRoom);

                // Execute aggressive actions for characters in the room
                if (first)
                {
                    //inRoom.EnactAggressiveFlag();
                }
            }
            else
            {
                SendToChar("Alas, you cannot go that way.\n\r");
            }
        }

        public void StripCamouflage(bool creep = false)
        {
            if (AffectedBy.ISSET(AffectFlags.Camouflage))
            {
                AffectData aff;
                while ((aff = FindAffect(AffectFlags.Camouflage)) != null)
                    AffectFromChar(aff, AffectRemoveReason.WoreOff);
                AffectedBy.REMOVEFLAG(AffectFlags.Camouflage);

                if (creep)
                {
                    Act("You try to creep but step out of your cover.", null, null, null, ActType.ToChar);
                    Act("$n tries to creep but steps out of $s cover.", null, null, null, ActType.ToRoom);
                }
                else
                {
                    Act("You step out of your cover.", null, null, null, ActType.ToChar);
                    Act("$n steps out of $s cover.", null, null, null, ActType.ToRoom);
                }
            }
        }

        public void StripHidden()
        {
            if (AffectedBy.ISSET(AffectFlags.Hide))
            {
                var affects = FindAffects(AffectFlags.Hide);

                if (!affects.Any(aff => !aff.endMessage.ISEMPTY()))
                {
                    Act("You step out of the shadows.", null, null, null, ActType.ToChar);
                    Act("$n steps out of the shadows.", null, null, null, ActType.ToRoom);
                }

                foreach (var aff in affects)
                    AffectFromChar(aff, AffectRemoveReason.WoreOff);
                AffectedBy.REMOVEFLAG(AffectFlags.Hide);

            }
        }


        public void StripInvis()
        {
            if (AffectedBy.ISSET(AffectFlags.Invisible))
            {
                var affects = FindAffects(AffectFlags.Invisible);

                if (!affects.Any(aff => !aff.endMessage.ISEMPTY()))
                {
                    Act("You fade into existence.", null, null, null, ActType.ToChar);
                    Act("$n fades into existence.", null, null, null, ActType.ToRoom);
                }

                foreach (var aff in affects)
                    AffectFromChar(aff, AffectRemoveReason.WoreOff);
                AffectedBy.REMOVEFLAG(AffectFlags.Invisible);

            }

            return;
        }

        public void StripSneak()
        {
            if (Race.affects.ISSET(AffectFlags.Sneak))
                return;

            if (AffectedBy.ISSET(AffectFlags.Sneak))
            {
                AffectData aff;
                while ((aff = FindAffect(AffectFlags.Sneak)) != null)
                    AffectFromChar(aff, AffectRemoveReason.WoreOff);
                AffectedBy.REMOVEFLAG(AffectFlags.Sneak);
                Act("You trample around loudly.", null, null, null, ActType.ToChar);
            }

            return;
        }

        

        public ItemData GetEquipment(WearSlotIDs slot)
        {
            if (Form != null) return null;
            Equipment.TryGetValue(slot, out var item);
            return item;
        }


        public string GetEquipmentString(Character toch)
        {
            StringBuilder wearing = new StringBuilder();
            wearing.AppendLine((toch == this || toch == null ? "You are " : Display(toch) + " is ") + "wearing:");
            bool items = false;
            if (Form == null) // && Equipment.Any(e => !e.Value.extraFlags.ISSET(ExtraFlags.VisDeath)))
            {
                foreach (var slot in WearSlots)
                {
                    if (Equipment.TryGetValue(slot.id, out ItemData item) && item != null)
                    {
                        if (item.extraFlags.ISSET(ExtraFlags.VisDeath) && !toch.IsImmortal)
                            continue;
                        items = true;

                        wearing.AppendLine(slot.slot + item.DisplayFlags(toch) + (toch.CanSee(item) ? ((item.Display(toch))) : "something"));
                    }
                }
            }
            if (!items)
                wearing.AppendLine("    Nothing.");
            return wearing.ToString();
        }



        public bool AddInventoryItem(ItemData item, bool atEnd = false)
        {
            List<Character> splitamongst = new List<Character>();
            if (item.ItemType.Contains(ItemTypes.Money))
            {
                Silver += item.Silver;
                Gold += item.Gold;


                if (Flags.ISSET(ActFlags.AutoSplit))
                {
                    foreach (var other in Room.Characters)
                    {
                        if (other != this && !other.IsNPC && other.IsSameGroup(this))
                        {
                            splitamongst.Add(other);
                        }
                    }


                    if (splitamongst.Count > 0)
                    {
                        var share_silver = item.Silver / (splitamongst.Count + 1);
                        var extra_silver = item.Silver % (splitamongst.Count + 1);

                        var share_gold = item.Gold / (splitamongst.Count + 1);
                        var extra_gold = item.Gold % (splitamongst.Count + 1);


                        Silver -= item.Silver;
                        Silver += share_silver + extra_silver;
                        Gold -= item.Gold;
                        Gold += share_gold + extra_gold;


                        if (share_silver > 0)
                        {
                            send("You split {0} silver coins. Your share is {1} silver.\n\r", item.Silver, share_silver + extra_silver);
                        }

                        if (share_gold > 0)
                        {
                            send("You split {0} gold coins. Your share is {1} gold.\n\r", item.Gold, share_gold + extra_gold);

                        }
                        string buf;
                        if (share_gold == 0)
                        {
                            buf = string.Format("$n splits {0} silver coins. Your share is {1} silver.",
                                 item.Silver, share_silver);
                        }
                        else if (share_silver == 0)
                        {
                            buf = string.Format("$n splits {0} gold coins. Your share is {1} gold.",
                                item.Gold, share_gold);
                        }
                        else
                        {
                            buf = string.Format(
                                "$n splits {0} silver and {1} gold coins, giving you {2} silver and {3} gold.\n\r",
                                 item.Silver, item.Gold, share_silver, share_gold);
                        }

                        foreach (var gch in splitamongst)
                        {
                            if (gch != this)
                            {
                                if (gch.Position != Positions.Sleeping)
                                    Act(buf, gch, null, null, ActType.ToVictim);
                                gch.Gold += share_gold;
                                gch.Silver += share_silver;
                            }
                        }
                    }// end if splitamongst.count > 0
                } // end isset autosplit
                return true;

            }
            else
            {
                Inventory.Insert(atEnd ? Inventory.Count : 0, item);
                item.CarriedBy = this;
                return true;
            }
        }




        

        public void NoFollow()
        {
            if (this.Following != null)
            {
                this.send("You stop following " + (this.Following.Display(this)) + ".\n\r");
                if (this.Following.Group.Contains(this))
                    this.Following.Group.Remove(this);

                this.Following = null;
                this.Leader = null;
            }
            this.send("You stop allowing followers.\n\r");

            foreach (var other in Characters.ToArray())
            {
                if (other.Following == this)
                {

                    if (other.Leader == this)
                    {
                        other.Leader = null;
                        if (this.Group.Contains(other)) this.Group.Remove(other);
                        this.Act("$N leaves your group.\n\r", other, type: ActType.ToChar);
                        this.Act("You leave $n's group.\n\r", other, type: ActType.ToVictim);
                    }
                    if (other == this.Pet)
                    {
                        this.Pet = null;
                        other.Act("$n wanders off.", type: ActType.ToRoom);
                        other.RemoveCharacterFromRoom();
                        other.Dispose();
                    }

                    if (other.Leader == this)
                        other.Leader = null;

                    if (other.Following == this)
                    {

                        other.Following = null;
                        this.Act("$N stops following you.\n\r", other, type: ActType.ToChar);
                        this.Act("You stop following $n.\n\r", other, type: ActType.ToVictim);
                    }
                }
            }
        }

        

        public string Display(Character to)
        {

            //if (!to.CanSee(this) && (!this.IsNPC && this.Level >= game.LEVEL_IMMORTAL && this.Flags.ISSET(ActFlags.WizInvis)))
            //{
            //    return "An Immortal";
            //}
            //else if (!to.CanSee(this))
            //{
            //    return "someone";
            //}

            return GetShortDescription(to);
        }

        public bool IsSameGroup(Character bch)
        {
            Character ach = this;
            if (bch == null)
                return false;
            if (bch == ach) return true;
            /*    if ( ( ach->level - bch->level > 8 || ach->level - bch->level < -8 ) && !IS_NPC(ach) )
            return FALSE;*/
            if (Leader != null) ach = Leader;
            if (bch.Leader != null) bch = bch.Leader;
            return ach == bch;
        }

        

        /// <summary>
        /// Return whether a weapon of the given type is being wielded and/or dual wielded
        /// </summary>
        /// <param name="type">Weapon type to be checked for</param>
        /// <param name="OrDualWielding">Can be wielded or dual wielded</param>
        /// <param name="AndDualWielding">Must be wielded and dual wielded</param>
        /// <returns>True or false whether the weapon type is wielded/dual wielded</returns>
        public bool IsWielding(WeaponTypes type, bool OrDualWielding = false, bool AndDualWielding = false)
        {
            ItemData wield;
            ItemData dualWield;

            wield = GetEquipment(WearSlotIDs.Wield);
            dualWield = GetEquipment(WearSlotIDs.DualWield);

            if (type == WeaponTypes.None &&
                wield == null && (!OrDualWielding || dualWield == null) &&
                (!AndDualWielding || (wield == null && dualWield == null)))
                return true;
            else if (type != WeaponTypes.None && !AndDualWielding &&
                ((wield != null && wield.WeaponType == type) ||
                (OrDualWielding && dualWield != null && dualWield.WeaponType == type)))
            {
                return true;
            }
            else if (type != WeaponTypes.None && AndDualWielding && wield != null && dualWield != null &&
                wield.WeaponType == type && dualWield.WeaponType == type)
            {
                return true;
            }
            else
                return false;
        }

        public bool FindAffect(SkillSpell skillSpell, out AffectData affect) => (affect = FindAffect(skillSpell)) != null;

        public AffectData FindAffect(SkillSpell skillSpell) => skillSpell == null ? null : (from aff in AffectsList where aff.skillSpell == skillSpell select aff).FirstOrDefault();

        public AffectData FindAffect(string skillname) => FindAffect(SkillSpell.SkillLookup(skillname));

        public bool FindAffect(AffectFlags flag, out AffectData affect) => (affect = FindAffect(flag)) != null;
        public AffectData FindAffect(AffectFlags flag) => (from aff in AffectsList where aff.flags.ISSET(flag) select aff).FirstOrDefault();

        public AffectData[] FindAffects(AffectFlags flag) => (from aff in AffectsList where aff.flags.ISSET(flag) select aff).ToArray();
        public AffectData[] FindAffects(SkillSpell skillSpell) => skillSpell == null ? new AffectData[] { } : (from aff in AffectsList where aff.skillSpell == skillSpell select aff).ToArray();

        public AffectData[] FindAffects(string skillname) => FindAffects(SkillSpell.SkillLookup(skillname));

        public bool IsAffected(string flag) => IsAffected(SkillSpell.SkillLookup(flag));

        public bool IsAffected(AffectFlags flag)
        {
            if (Form != null && Form.AffectedBy.ISSET(flag)) return true;

            if (AffectedBy.ISSET(flag)) return true;
            foreach (var aff in AffectsList)
                if (aff.flags.Contains(flag))
                    return true;
            return false;
        }

        public bool IsAffected(SkillSpell AffectSkill)
        {
            if (AffectSkill != null)
                foreach (var aff in AffectsList)
                    if (aff.skillSpell == AffectSkill)
                        return true;
            return false;
        }


        public ExtraDescription GetExtraDescriptionByKeyword(string arguments, ref int count)
        {
            if (arguments.ISEMPTY())
                return null;
            int num = arguments.number_argument(ref arguments);

            foreach (var item in Equipment.Values.Where(eq => CanSee(eq)))
                foreach (var ED in item.ExtraDescriptions)
                {
                    if ((arguments.ISEMPTY() || ED.Keywords.IsName(arguments)) && ++count >= num)
                        return ED;
                }

            foreach (var item in Inventory.Where(i => CanSee(i)))
                foreach (var ED in item.ExtraDescriptions)
                {
                    if ((arguments.ISEMPTY() || ED.Keywords.IsName(arguments)) && ++count >= num)
                        return ED;
                }

            foreach (var ED in Room.ExtraDescriptions)
            {
                if ((arguments.ISEMPTY() || ED.Keywords.IsName(arguments)) && ++count >= num)
                    return ED;
            }

            foreach (var item in Room.items)
                foreach (var ED in item.ExtraDescriptions)
                {
                    if ((arguments.ISEMPTY() || ED.Keywords.IsName(arguments)) && ++count >= num)
                        return ED;
                }



            return null;
        }

        public enum GetFlags
        {
            DisallowStringPrefix,
            DisrespectMortalVisibility,
            DisrespectImmortalVisibility,
            PlayersOnly,
            PlayerName
        }

        public bool GetCharacterFromRoomByName(string arguments, out Character person, params GetFlags[] flags) => (person = GetCharacterFromRoomByName(arguments, flags)) != null;
        public bool GetCharacterFromRoomByName(string arguments, ref int count, out Character person, params GetFlags[] flags) => (person = GetCharacterFromRoomByName(arguments, ref count, flags)) != null;

        public Character GetCharacterFromRoomByName(string arguments, params GetFlags[] flags)
        {
            int count = 0;
            return GetCharacterFromRoomByName(arguments, ref count, flags);
        }

        public Character GetCharacterFromRoomByName(string arguments, ref int count, params GetFlags[] flags) =>
            Room != null ? GetCharacterFromListByName(Room.Characters, arguments, ref count, flags) : null;

        public bool GetCharacterFromListByName(IEnumerable<Character> CharacterList, string arguments, out Character victim, params GetFlags[] flags) => (victim = GetCharacterFromListByName(CharacterList, arguments, flags)) != null;

        public Character GetCharacterFromListByName(IEnumerable<Character> CharacterList, string arguments, params GetFlags[] flags)
        {
            int count = 0;
            return GetCharacterFromListByName(CharacterList, arguments, ref count, flags);
        }

        public Character GetCharacterFromListByName(IEnumerable<Character> CharacterList, string arguments, ref int count, params GetFlags[] flags)
        {
            if (arguments.ISEMPTY()) return null;
            if (arguments.StringCmp("self"))
                return this;
            int num = arguments.number_argument(ref arguments);

            bool UnalteredName = flags.ISSET(GetFlags.PlayerName);
            var DisallowPrefix = flags.ISSET(GetFlags.DisallowStringPrefix);

            foreach (Character other in CharacterList)
            {
                if (!CanSee(other, flags)) continue;
                if ((arguments.ISEMPTY() ||
                        ((UnalteredName && other.Name.IsName(arguments, DisallowPrefix)) ||
                        (!UnalteredName && other.GetName.IsName(arguments, DisallowPrefix))) ||
                    (IsImmortal && other.Name.IsName(arguments))) &&
                    ++count >= num)
                    return other;
            }
            return null;
        }

        public bool GetItemHere(string arguments, out ItemData item) => (item = GetItemHere(arguments)) != null;
        public bool GetItemHere(string arguments, ref int count, out ItemData item) => (item = GetItemHere(arguments, ref count)) != null;

        public ItemData GetItemHere(string arguments)
        {
            int count = 0;
            return GetItemHere(arguments, ref count);
        }

        public ItemData GetItemHere(string arguments, ref int count)
        {
            ItemData result = null;


            if ((result = GetItemInventory(arguments, ref count)) != null)
                return result;
            if ((result = GetItemEquipment(arguments, out WearSlot wearSlot, ref count)) != null)
                return result;
            if ((result = GetItemList(arguments, Room.items, ref count)) != null)
                return result;

            return result;
        }


        public ItemData GetItemRoom(string arguments, ref int count)
        {
            if (arguments.ISEMPTY()) return null;
            int num = arguments.number_argument(ref arguments);
            foreach (ItemData item in Room.items)
            {
                if (!CanSee(item)) continue;
                if ((arguments.ISEMPTY() || item.Name.IsName(arguments)) && ++count >= num)
                    return item;
            }
            return null;
        }

        public ItemData GetItemList(string arguments, List<ItemData> items, ref int count)
        {
            if (arguments.ISEMPTY()) return null;
            int num = arguments.number_argument(ref arguments);

            foreach (ItemData item in items)
            {
                if (!CanSee(item))
                    continue;
                if ((arguments.ISEMPTY() || item.Name.IsName(arguments)) && ++count >= num)
                    return item;
            }
            return null;
        }

        public ItemData GetItemInventoryOrEquipment(string arguments, bool InventoryFirst = true)
        {
            WearSlotIDs slot;
            return GetItemInventoryOrEquipment(arguments, out slot, InventoryFirst);
        }
        public ItemData GetItemInventoryOrEquipment(string arguments, out WearSlotIDs slot, bool InventoryFirst = true)
        {
            if (arguments.ISEMPTY())
            {
                slot = WearSlotIDs.None;
                return null;
            }
            int count = 0;
            ItemData item = null;
            if (InventoryFirst)
                item = GetItemInventory(arguments, ref count);
            if (item == null)
            {
                WearSlot wearslot;
                item = GetItemEquipment(arguments, out wearslot, ref count);
                if (wearslot != null)
                    slot = wearslot.id;
                else
                    slot = WearSlotIDs.None;
            }
            else
                slot = WearSlotIDs.None;

            if (item == null && !InventoryFirst)
                item = GetItemInventory(arguments, ref count);
            return item;
        }

        public ItemData GetItemInventory(string arguments)
        {
            int count = 0;
            return GetItemInventory(arguments, ref count);
        }

        public ItemData GetItemInventory(string arguments, ref int count) => Form != null ? null : GetItemList(arguments, Inventory, ref count);

        public ItemData GetItemEquipment(string arguments, ref int count)
        {
            WearSlot slot = null;
            return GetItemEquipment(arguments, out slot, ref count);
        }

        public ItemData GetItemEquipment(string arguments, out WearSlot outslot, ref int count)
        {
            if (arguments.ISEMPTY())
            {
                outslot = null;
                return null;
            }
            if (Form != null)
            {
                outslot = null;
                return null;
            }
            int num = arguments.number_argument(ref arguments);

            foreach (var slot in WearSlots) // consider reverse here?
            {
                if (Equipment.TryGetValue(slot.id, out ItemData item) && (arguments.ISEMPTY() || item.Name.IsName(arguments)) && ++count >= num)
                {
                    outslot = slot;
                    return item;
                }
            }
            outslot = null;
            return null;
        }

        public void Dispose()
        {
            // Remove keys
            foreach (var item in Inventory.ToArray())
            {
                //if (item.ItemType.ISSET(ItemTypes.Key))
                {
                    Inventory.Remove(item);
                    item.Dispose();
                }
            }

            foreach (var item in Equipment.ToArray())
            {
                //if (item.Value.ItemType.ISSET(ItemTypes.Key))
                {
                    Equipment.Remove(item.Key);
                    if (item.Value != null)
                        item.Value.Dispose();
                }
            }
            // end remove keys

            if (Pet != null)
            {

                ((NPCData)Pet).Dispose();
                if (Pet != null) Pet.Dispose();

            }

            //if (Group.Count > 0)
            //{
            //    foreach (var member in Group)
            //    {
            //        if(member.Following == this)
            //        {
            //            member.StopFollowing();
            //        }    
            //    }
            //}

            StopFollowing();

            Combat.StopFighting(this, true);

            foreach (var character in Characters)
            {
                if (character != null && character != this)
                {


                    if (character.Following == this)
                    {
                        character.StopFollowing(); // unfollow, ungroup
                        //character.Following = null;
                        //character.send("You stop following " + Display(character) + ".\n\r");
                    }

                    if (character.Fighting == this)
                    {
                        character.Fighting = null;
                        if (character.Position == Positions.Fighting)
                            character.Position = Positions.Standing;
                        character.send("You stop fighting " + (Display(character)) + ".\n\r");
                    }

                    if (character.LastFighting == this)
                        character.LastFighting = null;
                }
            }

            if (Characters.Contains(this))
                Characters.Remove(this);
            if (this is NPCData && NPCData.NPCs.Contains((NPCData)this))
                NPCData.NPCs.Remove((NPCData)this);

            RemoveCharacterFromRoom();
        }

        public void DeathCry()
        {
            string msg = "You hear $n's death cry."; ;
            int vnum = 0;

            var parts = Race.parts;
            if (Form != null) parts = Form.Parts;
            switch (Utility.Random(0, 7))
            {
                default:
                case 0: msg = "$n hits the ground ... DEAD."; break;
                case 1:
                    msg = "$n splatters blood on your armor.";
                    break;
                case 2:
                    if (parts.Contains(PartFlags.Guts))
                    {
                        msg = "$n spills their guts all over the floor.";
                        vnum = 11;
                    }
                    break;
                case 3:
                    if (parts.Contains(PartFlags.Head))
                    {
                        msg = "$n's severed head plops on the ground.";
                        vnum = 7;
                    }
                    break;
                case 4:
                    if (parts.Contains(PartFlags.Heart))
                    {
                        msg = "$n's heart is torn from their chest.";
                        vnum = 8;
                    }
                    break;
                case 5:
                    if (parts.Contains(PartFlags.Arms))
                    {
                        msg = "$n's arm is sliced from their dead body.";
                        vnum = 9;
                    }
                    break;
                case 6:
                    if (parts.Contains(PartFlags.Legs))
                    {
                        msg = "$n's leg is sliced from their dead body.";
                        vnum = 10;
                    }
                    break;
                case 7:
                    if (parts.Contains(PartFlags.Brains))
                    {
                        msg = "$n's head is shattered, and their brains splash all over you.";
                        vnum = 12;
                    }
                    break;
            } // switch/select random part

            Act(msg + "\n\r", type: ActType.ToRoom);

            if (vnum != 0 && ItemTemplateData.Templates.TryGetValue(vnum, out ItemTemplateData template) && Room != null)
            {
                var item = new ItemData(template, Room)
                {
                    timer = Utility.Random(4, 7)
                };
                item.ShortDescription = string.Format(item.ShortDescription, GetShortDescription(null));
                item.LongDescription = string.Format(item.LongDescription, GetShortDescription(null));
                item.Description = string.Format(item.Description, GetShortDescription(null));
            }

            if (!(this is Player))
                msg = "You hear something's death cry.";
            else
                msg = "You hear someone's death cry.";

            if (Room != null)
                // send death cry to surrounding rooms
                foreach (var exit in Room.exits)
                {
                    if (exit != null && exit.destination != null && exit.destination != Room)
                    {
                        foreach (Character other in exit.destination.Characters)
                        {
                            other.send(msg + "\n\r");
                        }
                    }
                }
        } // end of deathcry

        /// <summary>
        /// Add an affect to the affect list and apply it
        /// </summary>
        /// <param name="source"></param>
        public void AffectToChar(AffectData source)
        {
            var aff = new AffectData(source);
            AffectsList.Insert(0, aff);

            if (aff.flags.Count > 0)
            {
                foreach (var flag in aff.flags)
                {
                    AffectedBy.ADDFLAG(flag);
                }


            }

            AffectApply(source);
        }

        /// <summary>
        /// Remove an affect from the affects list and remove its affect
        /// </summary>
        /// <param name="aff"></param>
        public void AffectFromChar(AffectData aff, AffectRemoveReason reason, bool silent = false)
        {
            if (aff == null) return;

            if (!AffectsList.Remove(aff))
                return;

            aff.RemovedReason = reason;
            // don't duplicate affectapply removes for affectfromchar, item affects are applied directly with affectapply
            AffectApply(aff, true, silent);

            foreach (var otheraff in AffectsList.ToArray())
            {
                // Remove entire affect linked by skill/spell or flags, maybe use name instead?
                if (otheraff.skillSpell != null && otheraff.skillSpell == aff.skillSpell && otheraff.duration == aff.duration && otheraff.affectType == aff.affectType)
                    AffectFromChar(otheraff, reason, silent);
                else if (otheraff.flags.Count > 0 && otheraff.flags.Count == aff.flags.Count && string.Join(" ", otheraff.flags) == string.Join(" ", aff.flags) && otheraff.affectType == aff.affectType)
                    AffectFromChar(otheraff, reason, silent);
            }

            if (aff.skillSpell != null && aff.skillSpell.EndFunction != null)
                aff.skillSpell.EndFunction(this, aff);

            if (!aff.endProgram.ISEMPTY())
            {
                if (Programs.AffectProgramLookup(aff.endProgram, out var prog))
                {
                    prog.Execute(this, aff, null, null, aff.skillSpell, Programs.ProgramTypes.AffectEnd, "");
                }
                if (NLuaPrograms.ProgramLookup(aff.endProgram, out var luaprog))
                    luaprog.Execute(this, null, this.Room, null, aff.skillSpell, aff, Programs.ProgramTypes.AffectEnd, "");

                if (prog == null && luaprog == null)
                    Game.log("AffectEndProgram not found: {0}", aff.endProgram);
            }
            if (aff.flags.ISSET(AffectFlags.SuddenDeath))
            {
                this.HitPoints = -15;
                Combat.CheckIsDead(this, this, -15);
            }
            if (aff.flags.ISSET(AffectFlags.Sleep))
            {
                CharacterDoFunctions.DoStand(this, "");
                DoActInfo.DoLook(this, "auto");
            }
        }

        /// <summary>
        /// Apply an affect without adding it to affect list
        /// Also removes flags if remove is true / plays remove message if not silent
        /// </summary>
        /// <param name="aff">Affect to by applied or removed</param>
        /// <param name="remove">Remove the affect rather than add it</param>
        /// <param name="silent">Don't display endmessage of affect</param>
        public void AffectApply(AffectData aff, bool remove = false, bool silent = false)
        {
            if (aff.where == AffectWhere.ToWeapon)
                return;
            if (remove)
            {

                if (!silent && !string.IsNullOrEmpty(aff.endMessage))
                    Act(aff.endMessage, type: ActType.ToChar);
                else if (!silent && aff.skillSpell != null && !aff.skillSpell.MessageOff.ISEMPTY())
                    Act(aff.skillSpell.MessageOff, type: ActType.ToChar);

                if (!silent && !string.IsNullOrEmpty(aff.endMessageToRoom))
                    Act(aff.endMessageToRoom, type: ActType.ToRoom);
                else if (!silent && aff.skillSpell != null && !aff.skillSpell.MessageOffToRoom.ISEMPTY())
                    Act(aff.skillSpell.MessageOffToRoom, type: ActType.ToRoom);

                if (aff.flags.Count > 0)
                {
                    foreach (var flag in aff.flags)
                    {
                        // don't remove affect provided by another debuff( for example, blinded then dirt kicked, dirt kick won't remove blind of blindness )

                        if (AffectsList.Any(affect => affect != aff && affect.flags.ISSET(flag)))
                            continue;
                        else
                            AffectedBy.REMOVEFLAG(flag);
                    }
                }
            }
            else
            {
                foreach (var flag in aff.flags)
                    AffectedBy.ADDFLAG(flag);

                if (!silent && !string.IsNullOrEmpty(aff.beginMessage))
                    Act(aff.beginMessage, type: ActType.ToChar);
                else if (!silent && aff.skillSpell != null && !aff.skillSpell.MessageOn.ISEMPTY())
                    Act(aff.skillSpell.MessageOn, type: ActType.ToChar);

                if (!silent && !string.IsNullOrEmpty(aff.beginMessageToRoom))
                    Act(aff.beginMessageToRoom, type: ActType.ToRoom);
                else if (!silent && aff.skillSpell != null && !aff.skillSpell.MessageOnToRoom.ISEMPTY())
                    Act(aff.skillSpell.MessageOnToRoom, type: ActType.ToRoom);

            }

            if (aff.where == AffectWhere.ToImmune && remove)
                ImmuneFlags.RemoveWhere(w => aff.DamageTypes.Contains(w));
            else if (aff.where == AffectWhere.ToResist && remove)
                ResistFlags.RemoveWhere(w => aff.DamageTypes.Contains(w));
            else if (aff.where == AffectWhere.ToVulnerabilities && remove)
                VulnerableFlags.RemoveWhere(w => aff.DamageTypes.Contains(w));
            else if (aff.where == AffectWhere.ToImmune && !remove)
                ImmuneFlags.SETBITS(aff.DamageTypes);
            else if (aff.where == AffectWhere.ToResist && !remove)
                ResistFlags.SETBITS(aff.DamageTypes);
            else if (aff.where == AffectWhere.ToVulnerabilities && !remove)
                VulnerableFlags.SETBITS(aff.DamageTypes);

            if (aff.modifier != 0)
            {
                var modifier = aff.modifier;
                if (remove) modifier = -modifier;
                switch (aff.location)
                {
                    case ApplyTypes.Strength:
                        this.ModifiedStats[PhysicalStatTypes.Strength] += modifier;
                        break;
                    case ApplyTypes.Wisdom:
                        this.ModifiedStats[PhysicalStatTypes.Wisdom] += modifier;
                        break;
                    case ApplyTypes.Intelligence:
                        this.ModifiedStats[PhysicalStatTypes.Intelligence] += modifier;
                        break;
                    case ApplyTypes.Dexterity:
                        this.ModifiedStats[PhysicalStatTypes.Dexterity] += modifier;
                        break;
                    case ApplyTypes.Constitution:
                        this.ModifiedStats[PhysicalStatTypes.Constitution] += modifier;
                        break;
                    case ApplyTypes.Charisma:
                        this.ModifiedStats[PhysicalStatTypes.Charisma] += modifier;
                        break;
                    case ApplyTypes.Hitroll:
                        HitRoll += modifier;
                        break;
                    case ApplyTypes.Damroll:
                        DamageRoll += modifier;
                        break;
                    case ApplyTypes.Armor:
                        //ArmorBash += modifier;
                        //ArmorSlash += modifier;
                        //ArmorPierce += modifier;
                        //ArmorExotic += modifier;
                        ArmorClass += modifier;
                        break;
                    case ApplyTypes.Hitpoints:
                        MaxHitPoints += modifier;
                        break;
                    case ApplyTypes.Mana:
                        MaxManaPoints += modifier;
                        break;
                    case ApplyTypes.Move:
                        MaxMovementPoints += modifier;
                        break;
                    case ApplyTypes.Saves:
                    case ApplyTypes.SavingSpell:
                        SavingThrow += modifier;
                        break;
                }
            }

            ItemData wield;
            // TODO Check weapon weight here
            if (!IsNPC && (wield = GetEquipment(WearSlotIDs.Wield)) != null &&
                wield.Weight > (PhysicalStats.StrengthApply[GetCurrentStat(PhysicalStatTypes.Strength)].Wield))
            {
                Act("You drop $p.", null, wield, null, ActType.ToChar);
                Act("$n drops $p.", null, wield, null, ActType.ToRoom);

                RemoveEquipment(wield, !silent, true);

                Room.items.Insert(0, wield);
                wield.Room = Room;
            }
            if (!IsNPC && (wield = GetEquipment(WearSlotIDs.DualWield)) != null &&
                wield.Weight > (PhysicalStats.StrengthApply[GetCurrentStat(PhysicalStatTypes.Strength)].Wield))
            {
                Act("You drop $p.", null, wield, null, ActType.ToChar);
                Act("$n drops $p.", null, wield, null, ActType.ToRoom);

                RemoveEquipment(wield, !silent, true);

                Room.items.Insert(0, wield);
                wield.Room = Room;
            }
        }

        public void Act(string msg, Character victim = null, ItemData item = null, ItemData item2 = null, ActType type = ActType.ToChar, params object[] args)
        {
            if (string.IsNullOrEmpty(msg)) return;
            if ((this.Room == null && type != ActType.ToChar))
                return;

            if (type == ActType.ToVictim)
            {
                if (victim == null)
                {
                    Game.bug("Act: null vch with TO_VICT.");
                    return;
                }

                if (victim.Room == null)
                    return;
            }

            if (type == ActType.ToRoom || type == ActType.ToRoomNotVictim)
            {
                foreach (var @to in this.Room.Characters)
                {
                    if (to != this && (to != victim || type != ActType.ToRoomNotVictim) && to.Position != Positions.Sleeping)
                    {
                        var output = FormatActMessage(msg, to, victim, item, item2, args);
                        to.send(output);
                    }
                }
            }
            else if (type == ActType.ToGroupInRoom)
            {
                foreach (var @to in this.Room.Characters)
                {
                    if (to != this && (to != victim) && to.Position != Positions.Sleeping && to.IsSameGroup(this))
                    {
                        var output = FormatActMessage(msg, to, victim, item, item2, args);
                        to.send(output);
                    }
                }
            }
            else if (type == ActType.ToAll)
            {
                foreach (var @to in Character.Characters)
                {
                    if (to != this && (to != victim || type != ActType.ToRoomNotVictim) && to.Position != Positions.Sleeping)
                    {
                        var output = FormatActMessage(msg, to, victim, item, item2, args);
                        to.send(output);
                    }
                }
            }
            else if (type == ActType.GlobalNotVictim)
            {
                foreach (var @to in Character.Characters)
                {
                    if (to != this && to != victim)
                    {
                        var output = FormatActMessage(msg, to, victim, item, item2, args);
                        to.send(output);
                    }
                }
            }
            else if (type == ActType.ToVictim)
            {
                var output = FormatActMessage(msg, victim, victim, item, item2, args);
                victim.send(output);
            }
            else if (type == ActType.ToChar)
            {
                var output = FormatActMessage(msg, this, victim, item, item2, args);
                this.send(output);
            }
        }

        public string FormatActMessage(string msg, Character to, Character victim = null, ItemData item = null, ItemData item2 = null, params object[] args)
        {
            var formatmsg = new StringBuilder();

            for (int i = 0; i < msg.Length; i++)
            {
                if (msg[i] == '$')
                {
                    i++;
                    if (i < msg.Length)
                    {
                        switch (msg[i])
                        {
                            case 'n':
                                formatmsg.Append(this.Display(to));
                                break;
                            case 'N':
                                if (victim != null)
                                    formatmsg.Append(victim.Display(to));
                                break;
                            case 'p':
                                if (item != null)
                                    formatmsg.Append(item.Display(to));
                                break;
                            case 'P':
                                if (item2 != null)
                                    formatmsg.Append(item2.Display(to));
                                break;
                            case 'o':
                                if (item != null)
                                {
                                    var display = item.Display(to);
                                    if (display.StartsWith("a ")) display = display.Substring(2);
                                    if (display.StartsWith("the ")) display = display.Substring(4);
                                    formatmsg.Append(display);
                                }
                                break;
                            case 'e':
                                formatmsg.Append(this.Sex == Sexes.Male ? "he" : (this.Sex == Sexes.Female ? "she" : "it"));
                                break;
                            case 'E':
                                if (victim != null)
                                    formatmsg.Append(victim.Sex == Sexes.Male ? "he" : (victim.Sex == Sexes.Female ? "she" : "it"));
                                break;
                            case 'm':
                                formatmsg.Append(this.Sex == Sexes.Male ? "him" : (this.Sex == Sexes.Female ? "her" : "it"));
                                break;
                            case 'M':
                                if (victim != null)
                                    formatmsg.Append(victim.Sex == Sexes.Male ? "him" : (victim.Sex == Sexes.Female ? "her" : "it"));
                                break;
                            case 's':
                                formatmsg.Append(this.Sex == Sexes.Male ? "his" : (this.Sex == Sexes.Female ? "her" : "their"));
                                break;
                            case 'S':
                                if (victim != null)
                                    formatmsg.Append(victim.Sex == Sexes.Male ? "his" : (victim.Sex == Sexes.Female ? "her" : "their"));
                                break;

                            case '$':
                                formatmsg.Append("$");
                                break;
                            default:
                                formatmsg.Append(msg[i]);
                                break;
                        }
                    }
                }
                else
                    formatmsg.Append(msg[i]);

            }
            if (!msg.EndsWith("\n\r"))
                formatmsg.AppendLine();
            var output = string.Format(formatmsg.ToString(), args);
            if (!string.IsNullOrEmpty(output) && output.Length > 1)
            {
                if (output.StartsWith("\\") && output.Length > 3)
                {
                    output = output.Substring(0, 2) + Char.ToUpper(output[2]) + output.Substring(3);
                }
                else
                    output = Char.ToUpper(output[0]) + output.Substring(1);
            }
            return output;
        }

        public void WaitState(int time)
        {
            Wait += time;
        }

        public XElement Element
        {
            get
            {
                XElement element = new XElement("Character",
                        new XElement("name", Name.TOSTRINGTRIM()),
                        new XElement("description", Description.TOSTRINGTRIM()),
                        new XElement("longDescription", LongDescription.TOSTRINGTRIM()),
                        new XElement("shortDescription", ShortDescription.TOSTRINGTRIM()),
                        !NightLongDescription.ISEMPTY() ? new XElement("NightLongDescription", NightLongDescription) : null,
                        !NightShortDescription.ISEMPTY() ? new XElement("NightShortDescription", NightShortDescription) : null,
                        new XElement("Sex", _Sex),
                        new XElement("level", Level),
                        new XElement("hitpoints", HitPoints),
                        new XElement("maxhitpoints", MaxHitPoints),
                        new XElement("manapoints", ManaPoints),
                        new XElement("maxmanapoints", MaxManaPoints),
                        new XElement("movementpoints", MovementPoints),
                        new XElement("maxmovementpoints", MaxMovementPoints),
                        new XElement("ArmorBash", ArmorBash),
                        new XElement("ArmorSlash", ArmorSlash),
                        new XElement("ArmorPierce", ArmorPierce),
                        new XElement("ArmorExotic", ArmorExotic),
                        new XElement("size", Size.ToString()),
                        (this is NPCTemplateData && ((NPCTemplateData)this).Vnum == 0 ?
                        new XComment("Valid sizes are " + (string.Join(" ", from chsize in Utility.GetEnumValues<CharacterSize>() select chsize))) : null),
                        Xp != 0 ? new XElement("xp", Xp) : null,
                        XpTotal != 0 ? new XElement("xpTotal", XpTotal) : null,
                        Race != null ?
                        new XElement("race", Race.name) : null,
                        !Title.ISEMPTY() ? new XElement("Title", Title) : null,
                        !ExtendedTitle.ISEMPTY() ? new XElement("ExtendedTitle", ExtendedTitle) : null,
                        new XElement("alignment", Alignment.ToString()),
                        (this is NPCTemplateData && ((NPCTemplateData)this).Vnum == 0 ?
                        new XComment("Valid alignments are " + (string.Join(" ", from chalign in Utility.GetEnumValues<Alignment>() select chalign))) : null),
                        new XElement("ethos", Ethos.ToString()),
                        (this is NPCTemplateData && ((NPCTemplateData)this).Vnum == 0 ?
                        new XComment("Valid ethos are " + (string.Join(" ", from chethos in Utility.GetEnumValues<Ethos>() select chethos))) : null),
                        Room != null ? new XElement("room", Room != null ? Room.Vnum : 0) : null,
                        new XElement("hitroll", HitRoll),
                        new XElement("damRoll", DamageRoll),
                        new XElement("armorclass", ArmorClass),
                        new XElement("silver", this.Silver),
                        new XElement("gold", this.Gold),
                        SilverBank != 0 ? new XElement("silverbank", this.SilverBank) : null,
                        //GoldBank != 0 ? new XElement("goldbank", this.GoldBank) : null,
                        Practices != 0 ? new XElement("practices", this.Practices) : null,
                        Trains != 0 ? new XElement("trains", this.Trains) : null,
                        Hunger != 0 ? new XElement("hunger", this.Hunger) : null,
                        Thirst != 0 ? new XElement("thirst", this.Thirst) : null,
                        Drunk != 0 ? new XElement("drunk", this.Drunk) : null,
                        Dehydrated != 0 ? new XElement("dehydrated", this.Dehydrated) : null,
                        Starving != 0 ? new XElement("starving", this.Starving) : null,
                        new XElement("SavingThrow", this.SavingThrow),
                        new XElement("Guild", this.Guild != null ? this.Guild.name : ""),
                        new XElement("flags", string.Join(" ", from flag in Flags select flag.ToString())),
                        new XElement("immune", string.Join(" ", from flag in ImmuneFlags select flag.ToString())),
                        new XElement("resist", string.Join(" ", from flag in ResistFlags select flag.ToString())),
                        new XElement("vulnerable", string.Join(" ", from flag in VulnerableFlags select flag.ToString())),

                        new XElement("affectedBy", string.Join(" ", from flag in AffectedBy.Distinct() select flag.ToString())),
                        new XElement("WiznetFlags", string.Join(" ", from flag in WiznetFlags select flag.ToString())),

                        WeaponDamageMessage != null ? new XElement("WeaponDamageMessage", WeaponDamageMessage.Keyword) : null,
                        DamageDice != null && DamageDice.HasValue ? new XElement("DamageDiceSides", DamageDice.DiceSides) : null,
                        DamageDice != null && DamageDice.HasValue ? new XElement("DamageDiceCount", DamageDice.DiceCount) : null,
                        DamageDice != null && DamageDice.HasValue ? new XElement("DamageDiceBonus", DamageDice.DiceBonus) : null,

                        PermanentStats != null && PermanentStats.HasValue() ? (PermanentStats ?? new PhysicalStats(20, 20, 20, 20, 20, 20)).Element("PermanentStats") : null,
                        ModifiedStats != null && ModifiedStats.HasValue() ? (ModifiedStats ?? new PhysicalStats(0, 0, 0, 0, 0, 0)).Element("ModifiedStats") : null,

                        !IsNPC && Learned != null && Learned.Any() ?
                        new XElement("Learned",
                            from learned in this.Learned
                            select new XElement("SkillSpell", new XAttribute("Name", learned.Key.internalName), new XAttribute("Value", learned.Value.Percentage), new XAttribute("Level", learned.Value.Level))) : null,
                        IsNPC && Guild == null && Learned != null && Learned.Any() ?
                        new XElement("Learned",
                            from learned in this.Learned
                            select new XElement("SkillSpell", new XAttribute("Name", learned.Key.internalName), new XAttribute("Value", learned.Value.Percentage), new XAttribute("Level", 1))) : null,
                        (AffectsList != null && AffectsList.Any() ?
                        new XElement("Affects",
                            from aff in AffectsList
                            where !aff.RemoveAndSaveFlags.ISSET(AffectData.StripAndSaveFlags.DoNotSave)
                            select aff.Element
                       ) : null),
                        (Inventory != null && Inventory.Any() ?
                        (new XElement("Inventory",
                            from item in Inventory select item.Element)) : null
                        ),
                        Equipment != null && Equipment.Any(eq => eq.Value != null) ?
                        (new XElement("Equipment",
                            from slot in Equipment
                            select new XElement("Slot",
                                new XAttribute("SlotID", slot.Key.ToString()), slot.Value.Element))) : null,
                        ((Pet != null && Pet.Room == Room && Pet is NPCData) ? new XElement("Pet", ((NPCData)Pet).Element) : null)

                    );

                return element;
            }
        }

        public string Title { get; set; } = string.Empty;
        public string ExtendedTitle { get; set; } = string.Empty;

        //public long GoldBank { get; set; }
        public long SilverBank { get; set; }
        public Character ReplyTo { get; set; }

        public void CheckImprove(string skname, bool success, int multiplier)
        {
            CheckImprove(SkillSpell.SkillLookup(skname), success, multiplier);
        }

        public void CheckImprove(SkillSpell sk, bool success, int multiplier)
        {
            Character victim;
            int chance;

            if (sk == null)
                return;

            if (IsNPC)
                return;

            if ((Level < GetLevelSkillLearnedAt(sk) && !sk.SkillTypes.ISSET(SkillSpellTypes.Form))
                || GetSkillPercentage(sk) <= 1
                || GetSkillPercentage(sk) >= 100)
                return;  /* skill is not known */

            if (Form != null && !sk.SkillTypes.ISSET(SkillSpellTypes.Form) && sk.SkillTypes.ISSET(SkillSpellTypes.InForm) && sk.spellFun == null) //sk.name != "trance" && sk.name != "meditation")
            {
                return;
            }

            if (!Learned.ContainsKey(sk) && Learned[sk] != null)
                return;


            /* check to see if the character has a chance to learn */
            chance = 10 * PhysicalStats.IntelligenceApply[GetCurrentStatOutOfForm(PhysicalStatTypes.Intelligence)].Learn; // * int_app[get_curr_stat(ch, STAT_INT)].learn;
            chance /= (multiplier * GetSkillRating(sk) * 4) != 0 ? (multiplier * GetSkillRating(sk) * 4) : 1;
            chance += Level;
            if ((victim = Fighting) != null)
            {
                if (victim.IsNPC)
                {
                    if (victim.Level > Level)
                    {
                        chance += (victim.Level - Level) * 10;
                    }
                }
                if (!victim.IsNPC)
                {
                    chance += (victim.Level) * 2;
                }
            }
            else
            {
                chance += Level;
            }

            chance = (int)(chance * BonusInfo.LearningBonus);

            if (Utility.Random(1, 1000) > chance)
                return;

            /* now that the character has a CHANCE to learn, see if they really have */
            var prereqsnotmet = (from l in Learned where l.Key.PrerequisitesMet(this) == false select l).ToArray();

            if (success)
            {
                chance = Utility.URANGE(5, 100 - GetSkillPercentage(sk), 95);
                if (Utility.NumberPercent() - 28 < chance)
                {
                    Learned[sk].Percentage += 1;
                    GainExperience(2 * GetSkillRating(sk));
                    if (sk.SkillTypes.ISSET(SkillSpellTypes.Form))
                    {
                        if (GetSkillPercentage(sk) == 100)
                            send("\\GYou feel confident as a {0}!\\x\n\r", (from form in ShapeshiftForm.Forms where form.FormSkill == sk select form.Name).FirstOrDefault() ?? "unknown");
                        else
                            send("\\GYou feel more confident as a {0}!\\x\n\r", (from form in ShapeshiftForm.Forms where form.FormSkill == sk select form.Name).FirstOrDefault() ?? "unknown");
                    }
                    else if (GetSkillPercentage(sk) == 100)
                    {
                        send("\\GYou have perfected {0}!\\x\n\r", sk.name);
                    }
                    else
                    {
                        send("\\YYou have become better at {0}!\\x\n\r", sk.name);
                    }
                }
            }

            else
            {
                chance = Utility.URANGE(5, GetSkillPercentage(sk) / 2, 30);
                if (Utility.NumberPercent() - 28 < chance)
                {
                    Learned[sk].Percentage += Utility.Random(1, 3);
                    Learned[sk].Percentage = Math.Min(Learned[sk].Percentage, 100);
                    GainExperience(2 * GetSkillRating(sk));

                    if (sk.SkillTypes.ISSET(SkillSpellTypes.Form))
                    {
                        if (GetSkillPercentage(sk) == 100)
                            send("\\GYou feel confident as a {0}!\\x\n\r", (from form in ShapeshiftForm.Forms where form.FormSkill == sk select form.Name).FirstOrDefault() ?? "unknown");
                        else
                            send("\\GYou feel more confident as a {0}!\\x\n\r", (from form in ShapeshiftForm.Forms where form.FormSkill == sk select form.Name).FirstOrDefault() ?? "unknown");
                    }
                    else if (GetSkillPercentage(sk) == 100)
                    {
                        send("\\GYou learn from your mistakes, and manage to perfect {0}!\\x\n\r", sk.name);
                    }
                    else
                    {
                        send("\\YYou learn from your mistakes, and your {0} skill improves!\\x\n\r", sk.name);

                    }
                }
            }
            foreach (var prereqnotmet in prereqsnotmet)
            {
                if (prereqnotmet.Key.PrerequisitesMet(this))
                {
                    send("\\CYou feel a rush of insight into {0}!\\x\n\r", prereqnotmet.Key.name);
                }
            }
        } // End of check improve

        public int HitpointGain()
        {
            int gain;
            int number;

            if (Room == null)
                return 0;

            //if (inRoom.vnum == ROOM_VNUM_NIGHTWALK || ch.inRoom.vnum == 2901)
            //{
            //    number = gsn_shadowplane;
            //    damage_old(ch, ch, 40, number, DAM_NEGATIVE, TRUE);
            //    return 0;
            //}

            //if (is_affected(ch, gsn_atrophy) || is_affected(ch, gsn_prevent_healing))
            //    return 0;

            if (!IsNPC && !IsAffected(AffectFlags.Ghost))
            {
                if (Starving > 6 && !IsAffected(AffectFlags.Sated))
                    return 0;
                if (Dehydrated > 4 && !IsAffected(AffectFlags.Quenched))
                    return 0;
            }

            if (IsNPC)
            {
                gain = 5 + Level;
                //if (IS_AFFECTED(ch, AFF_REGENERATION))
                //    gain *= 2;

                switch (Position)
                {
                    default: gain /= 2; break;
                    case Positions.Sleeping: gain = 3 * gain / 2; break;
                    case Positions.Resting: break;
                    case Positions.Fighting: gain /= 3; break;
                }

            }
            else
            {
                gain = Math.Max(3, GetCurrentStat(PhysicalStatTypes.Constitution) - 3 + Level / 2);
                gain += Guild.HitpointGainMax;
                number = Utility.NumberPercent();
                if (number < GetSkillPercentage(SkillSpell.SkillLookup("fast healing")))
                {
                    gain += number * gain / 100;
                    if (HitPoints < MaxHitPoints)
                        CheckImprove(SkillSpell.SkillLookup("fast healing"), true, 8);
                }
                else
                    CheckImprove(SkillSpell.SkillLookup("fast healing"), false, 4);

                if (IsAffected(AffectFlags.EnhancedFastHealing))
                {
                    gain += (Utility.NumberPercent() * gain / 100) * 2;
                }

                switch (Position)
                {
                    default: gain /= 4; break;
                    case Positions.Sleeping: break;
                    case Positions.Resting: gain /= 2; break;
                    case Positions.Fighting: gain /= 6; break;
                }

                if (Hunger == 0 && !IsAffected(AffectFlags.Sated))
                    gain /= 2;

                if (Thirst == 0 && !IsAffected(AffectFlags.Quenched))
                    gain /= 2;
            }

            //if (ch->on != NULL && ch->on->item_type == ITEM_FURNITURE)
            //    gain = (gain * 7 / 5);


            if (IsAffected(AffectFlags.Plague))
                gain /= 8;

            //if (position == Positions.Sleeping && get_skill(ch, gsn_dark_dream) > 5)
            //{
            //    if (number_percent() < get_skill(ch, gsn_dark_dream))
            //    {
            //        check_improve(ch, gsn_dark_dream, TRUE, 7);
            //        gain *= 3;
            //        gain /= 2;
            //    }
            //}

            if (IsAffected(AffectFlags.Haste))
                gain /= 2;
            if (IsAffected(AffectFlags.Slow))
            {
                gain *= 17;
                gain /= 10;
            }
            if (IsAffected(AffectFlags.Burrow))
            {
                gain *= 17;
                gain /= 10;
            }

            if (GetSkillPercentage("slow metabolism") > 1)
            {
                gain *= 17;
                gain /= 10;
            }
            if (this.IsAffected(SkillSpell.SkillLookup("camp")))
            {
                gain *= 2;
            }
            gain *= 2;
            return Math.Min(gain, MaxHitPoints - HitPoints);
        } // end of HitpointGain

        public int ManaPointsGain()
        {
            int gain;
            int number;

            if (Room == null)
                return 0;
            //if (is_affected(ch, gsn_atrophy) || is_affected(ch, gsn_prevent_healing))
            //    return 0;

            if (!IsNPC && !IsAffected(AffectFlags.Ghost))
            {
                if (Starving > 6 && !IsAffected(AffectFlags.Sated))
                    return 0;
                if (Dehydrated > 4 && !IsAffected(AffectFlags.Quenched))
                    return 0;
            }

            if (IsNPC)
            {

                gain = 5 + Level;

                if (Race == Race.GetRace("malefisti"))
                    gain *= 2;

                switch (Position)
                {
                    default: gain /= 2; break;
                    case Positions.Sleeping: gain = 3 * gain / 2; break;
                    case Positions.Resting: break;
                    case Positions.Fighting: gain /= 3; break;
                }
            }
            else
            {
                gain = (GetCurrentStat(PhysicalStatTypes.Wisdom) / 2 - 9
                    + GetCurrentStat(PhysicalStatTypes.Intelligence) * 2 + Level);
                number = Utility.NumberPercent();
                if (number < GetSkillPercentage(SkillSpell.SkillLookup("meditation")))
                {
                    gain += number * gain / 100;
                    if (ManaPoints < MaxManaPoints)
                        CheckImprove(SkillSpell.SkillLookup("meditation"), true, 4);
                }
                number = Utility.NumberPercent();
                if (number < GetSkillPercentage(SkillSpell.SkillLookup("trance")))
                {
                    gain += number * gain / 100;
                    if (ManaPoints < MaxManaPoints)
                        CheckImprove(SkillSpell.SkillLookup("trance"), true, 4);
                }

                switch (Position)
                {
                    default: gain /= 4; break;
                    case Positions.Sleeping: break;
                    case Positions.Resting: gain /= 2; break;
                    case Positions.Fighting: gain /= 6; break;
                }

                if (Hunger == 0 && !IsAffected(AffectFlags.Sated))
                    gain /= 2;

                if (Thirst == 0 && !IsAffected(AffectFlags.Quenched))
                    gain /= 2;

            }

            //if (ch->on != NULL && ch->on->item_type == ITEM_FURNITURE)
            //    gain = gain * 7 / 5;

            if (IsAffected(AffectFlags.Poison))
                gain /= 4;

            //if (position ==  Positions.Sleeping && get_skill(ch, gsn_dark_dream) > 5)
            //{
            //    if (number_percent() < get_skill(ch, gsn_dark_dream))
            //    {
            //        check_improve(ch, gsn_dark_dream, TRUE, 5);
            //        gain *= 3;
            //        gain /= 2;
            //    }

            //}

            if (IsAffected(AffectFlags.Plague))
                gain /= 8;

            if (IsAffected(AffectFlags.Haste))
                gain /= 2;
            if (IsAffected(AffectFlags.Slow))
                gain += (11 * gain / 10);
            if (IsAffected(AffectFlags.Burrow))
                gain += (11 * gain / 10);
            if (GetSkillPercentage("slow metabolism") > 1)
                gain += (11 * gain / 10);
            if (this.IsAffected(SkillSpell.SkillLookup("camp")))
            {
                gain *= 2;
            }
            gain *= 2;
            return Math.Min(gain, MaxManaPoints);// - ManaPoints);
        } // end ManaPointsGain

        public int MovementPointsGain()
        {
            int gain;

            if (Room == null)
                return 0;
            //if (is_affected(ch, gsn_atrophy) || is_affected(ch, gsn_prevent_healing))
            //    return 0;

            if (!IsNPC && !IsAffected(AffectFlags.Ghost))
            {
                if (Starving > 6 && !IsAffected(AffectFlags.Sated))
                    return 0;
                if (Dehydrated > 4 && !IsAffected(AffectFlags.Quenched))
                    return 0;
            }

            if (IsNPC)
            {
                gain = Level;
            }
            else
            {
                gain = Math.Max(15, Level);

                switch (Position)
                {
                    case Positions.Sleeping: gain += GetCurrentStat(PhysicalStatTypes.Dexterity); break;
                    case Positions.Resting: gain += GetCurrentStat(PhysicalStatTypes.Dexterity) / 2; break;
                }

                if (Hunger == 0 && !IsAffected(AffectFlags.Sated))
                    gain /= 2;

                if (Thirst == 0 && !IsAffected(AffectFlags.Quenched))
                    gain /= 2;

            }

            //gain = gain * ch->in_room->heal_rate / 100;

            //if (ch->on != NULL && ch->on->item_type == ITEM_FURNITURE)
            //    gain = gain * 6 / 5;


            //if (ch->position == POS_SLEEPING && get_skill(ch, gsn_dark_dream) > 5)
            //{
            //    if (number_percent() < get_skill(ch, gsn_dark_dream))
            //    {
            //        check_improve(ch, gsn_dark_dream, TRUE, 8);
            //        gain *= 3;
            //        gain /= 2;
            //    }
            //}

            if (IsAffected(AffectFlags.Poison))
                gain /= 4;

            if (IsAffected(AffectFlags.Plague))
                gain /= 8;

            if (IsAffected(AffectFlags.Haste) || IsAffected(AffectFlags.Slow))
                gain *= 2;
            if (IsAffected(AffectFlags.Burrow))
                gain *= 2;
            if (GetSkillPercentage("slow metabolism") > 1)
                gain += (11 * gain / 10);
            if (this.IsAffected(SkillSpell.SkillLookup("camp")))
            {
                gain *= 2;
            }
            gain *= 2;
            return Math.Min(gain, MaxMovementPoints - MovementPoints);
        } // end MovementPointsGain

        

        public static Character GetCharacterWorld(string argument, bool onlyPlayers = true, bool truename = false) => GetCharacterWorld(null, argument, onlyPlayers, false);

        public static Character GetCharacterWorld(Character ch, string argument, bool onlyPlayers = true, bool checkCanSee = true, bool truename = false)
        {
            string arg = "";
            Character wch;
            int number;
            int count = 0;

            if (argument.StringCmp("self"))
                return ch;

            if (ch != null)
                if ((wch = ch.GetCharacterFromRoomByName(argument, ref count)) != null)
                    return wch;

            number = argument.number_argument(ref arg);
            count = 0;

            foreach (var other in Character.Characters.ToArray())
            {
                if (other.Room == null || (checkCanSee && !ch.CanSee(other))
                    || (!other.GetName.IsName(arg) && ((!truename || !other.Name.StringCmp(arg)) || ((ch != null && !ch.IsImmortal && (!ch.IsNPC || !onlyPlayers)) || !other.Name.IsName(arg)))))
                    continue;

                if (++count >= number)
                    return other;
            }

            return null;
        }

        public static ItemData GetItemWorld(Character ch, string argument)
        {
            string arg = "";
            ItemData item;
            int number;
            int count = 0;

            if ((item = ch.GetItemHere(argument, ref count)) != null)
                return item;

            number = argument.number_argument(ref arg);
            count = 0;

            foreach (var other in ItemData.Items.ToArray())
            {
                if (other.Room == null || !ch.CanSee(other)
                    || (!other.Name.IsName(arg)))
                    continue;

                if (++count >= number)
                    return other;
            }

            return null;
        }

        public static RoomData FindLocation(Character ch, string arg)
        {
            Character victim;
            ItemData obj;

            if (int.TryParse(arg, out var vnum) && RoomData.Rooms.TryGetValue(vnum, out var room))
                return room;

            if ((victim = GetCharacterWorld(ch, arg)) != null)
                return victim.Room;

            if ((obj = GetItemWorld(ch, arg)) != null)
                return obj.Room;

            return null;
        }

        public void StopFollowing()
        {
            if (Following != null)
            {
                send("You stop following " + (Following.Display(this)) + ".\n\r");
                Following.send(Display(Following) + " stops following you.\n\r");
                if (Following.Group.Contains(this))
                    Following.Group.Remove(this);
                if (Following.Pet == this)
                    Following.Pet = null;
                Following = null;
                Leader = null;
            }
        }

        public RoomData GetRecallRoom()
        {
            RoomData recallroom = null;
            if (Alignment == Alignment.Good)
            {
                if (RoomData.Rooms.TryGetValue(19089, out recallroom))
                {
                    return recallroom;
                }
            }
            else if (Alignment == Alignment.Evil)
            {
                if (RoomData.Rooms.TryGetValue(19090, out recallroom))
                {
                    return recallroom;
                }
            }
            else
            {
                if (RoomData.Rooms.TryGetValue(19091, out recallroom))
                {
                    return recallroom;
                }
            }

            RoomData.Rooms.TryGetValue(3001, out recallroom);
            return recallroom;
        }

        

        public ImmuneStatus CheckImmune(WeaponDamageTypes DamageType)
        {

            if (ImmuneFlags.Contains(DamageType) || (Race != null && Race.ImmuneFlags.Contains(DamageType)))
            {
                return ImmuneStatus.Immune;
            }
            else if (ResistFlags.Contains(DamageType) || (Race != null && Race.ResistFlags.Contains(DamageType)))
            {
                return ImmuneStatus.Resistant;
            }
            else if (VulnerableFlags.Contains(DamageType) || (Race != null && Race.VulnerableFlags.Contains(DamageType)))
                return ImmuneStatus.Vulnerable;
            else
                return ImmuneStatus.Normal;
        }

        

        public void LearnSkill(string skill, int percentage, int Level = 60) => LearnSkill(SkillSpell.SkillLookup(skill), percentage, Level);


        public void LearnSkill(SkillSpell skill, int percentage, int Level = 60)
        {
            LearnedSkillSpell learned;
            if (skill == null) return;
            if (!Learned.TryGetValue(skill, out learned))
            {
                if (Level == 60 && Guild != null && skill.skillLevel.ContainsKey(Guild.name))
                    Level = skill.skillLevel[Guild.name];
                Learned[skill] = new LearnedSkillSpell() { Skill = skill, SkillName = skill.name, Percentage = percentage, Level = Level };
            }
            else
            {
                if (Level == 60 && Guild != null && skill.skillLevel.ContainsKey(Guild.name))
                    Level = skill.skillLevel[Guild.name];
                if (learned.Level == 0 || Level < learned.Level)
                    learned.Level = Level;
                learned.Percentage = percentage;
            }
        }

        

        public Character[] GetGroupMembersInRoom() => Room == null ? new Character[] { } :
            (from other in Room.Characters
             where other.IsSameGroup(this) ||
             (other.Master != null && other.Master.IsSameGroup(this)) ||
             (other.Leader != null && other.Leader.IsSameGroup(this))
             select other).ToArray();

        
        public static void CheckPeek(Character ch, Character victim)
        {
            StringBuilder carrying = new StringBuilder();
            var tempItemList = new Dictionary<string, int>();
            int chance = 0;
            if ((chance = ch.GetSkillPercentage("peek") + 20) <= 21)
                return;
            else if (chance < Utility.NumberPercent())
            {
                ch.Act("You failed to peek at $N's inventory.\n\r", victim);
                ch.CheckImprove("peek", false, 1);
                return;
            }

            ch.CheckImprove("peek", true, 1);
            ch.Act("You peek at $N's belongings.", victim);
            ch.Act("$N is carrying:", victim);
            //carrying.AppendLine(victim.Display(ch) + " is carrying:");

            if (victim.Form == null && victim.Inventory.Any())
            {
                foreach (var item in victim.Inventory)
                {
                    if (!ch.CanSee(item)) continue;

                    var itemshow = item.DisplayFlags(ch) + item.Display(ch);

                    if (tempItemList.ContainsKey(itemshow))
                        tempItemList[itemshow] = tempItemList[itemshow] + 1;
                    else
                        tempItemList[itemshow] = 1;
                    //    carrying.AppendLine();
                }
                foreach (var itemkvp in tempItemList)
                {
                    carrying.AppendLine("    " + (itemkvp.Value > 1 ? "[" + itemkvp.Value + "] " : "") + itemkvp.Key);
                }
            }
            else
                carrying.AppendLine("    Nothing.");

            using (new Page(ch))
                ch.SendToChar(carrying.ToString());
        }
        

        internal void StripAffect(AffectFlags Flag, bool silent = false)
        {
            var affects = FindAffects(Flag);
            foreach (var affect in affects.ToArray())
            {
                AffectFromChar(affect, AffectRemoveReason.Stripped, silent);
            }
        }
        
        /// <summary>
        /// Get the damage table for characters level applying a multiplier
        /// </summary>
        /// <param name="LowEndMultiplier"></param>
        /// <param name="HighEndMultiplier"></param>
        /// <returns></returns>

        public int GetDamage(int level, float LowEndMultiplier = 1, float HighEndMultiplier = 2, float bonus = 0)
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

            if (this.IsNPC)
                level = Math.Min(level, 51);
            level = Math.Min(level, dam_each.Length - 1);
            level = Math.Max(0, level);
            return Utility.Random((int)(dam_each[level] * LowEndMultiplier + bonus), (int)(dam_each[level] * HighEndMultiplier + bonus));
        } // end get damage

        

        public static void EndFirstAid(Character ch, Character victim)
        {
            var skill = SkillSpell.SkillLookup("first aid");
            if (victim == ch)
            {
                ch.Act("You successfully apply bandages to yourself.", victim);
                ch.Act("$n successfully applies bandages to themselves.", victim, type: ActType.ToRoom);
                ch.Act("You feel better.");
            }
            else
            {
                ch.Act("You successfully apply bandages you prepared for $N.", victim);
                ch.Act("$n successfully applies bandages $e perpared for $N.", victim, type: ActType.ToRoomNotVictim);
                ch.Act("$n successfully applies bandages $e had prepared for you.", victim, type: ActType.ToVictim);
                ch.Act("You feel better.", victim, type: ActType.ToVictim);
            }

            ch.HitPoints += (int)(ch.MaxHitPoints * 0.2);
            victim.HitPoints = Math.Min(victim.HitPoints, victim.MaxHitPoints);

            if (Utility.NumberPercent() < Math.Max(1, ch.Level / 2) && victim.IsAffected(AffectFlags.Plague))
            {
                var aff = victim.FindAffect(SkillSpell.SkillLookup("plague"));
                aff.duration /= 2;
                aff.modifier /= 2;
                aff.level /= 2;
                victim.Act("The sores on $n's body look less severe.\n\r", type: ActType.ToRoom);
                victim.Act("The sores on your body look less severe.\n\r");
            }
            if (Utility.NumberPercent() < Math.Max(1, (ch.Level)) && ch.IsAffected(SkillSpell.SkillLookup("blindness")))
            {
                victim.AffectFromChar(ch.FindAffect(SkillSpell.SkillLookup("blindness")), AffectRemoveReason.Cleansed);
                victim.send("Your vision returns!\n\r");
            }
            if (Utility.NumberPercent() < Math.Max(1, ch.Level / 2) && victim.IsAffected(AffectFlags.Poison))
            {
                var aff = victim.FindAffect(SkillSpell.SkillLookup("poison"));
                aff.duration /= 2;
                aff.modifier /= 2;
                aff.level /= 2;
                victim.Act("The poison through your body feels less severe.");
                victim.Act("The poison running through $n's body seems less severe.", type: ActType.ToRoom);
            }
            if (Utility.NumberPercent() < Math.Max(1, ch.Level / 2) && victim.IsAffected("bleeding"))
            {
                var aff = victim.FindAffect("bleeding");
                aff.duration /= 2;
                aff.modifier /= 2;
                aff.level /= 2;
                victim.Act("Your bleeding wound feels less severe.");
                victim.Act("$n's bleeding would looks less severe.", type: ActType.ToRoom);
            }
            ch.CheckImprove(skill, true, 1);

            var af = new AffectData();

            af.where = AffectWhere.ToAffects;
            af.skillSpell = skill;
            af.location = 0;
            af.duration = 2;
            af.modifier = 0;
            af.level = ch.Level;
            af.affectType = AffectTypes.Skill;
            af.displayName = "first aid";
            af.endMessage = "You can once again receive first aid.";
            victim.AffectToChar(af);
            ch.WaitState(skill.waitTime);
        }

        

        public bool HasBuilderPermission(AreaData area) => area != null && (area.Builders.IsName(Name, true) || (Level == Game.MAX_LEVEL && !IsNPC));
        public bool HasBuilderPermission(RoomData room) => room.Area.Builders.IsName(Name, true) || (Level == Game.MAX_LEVEL && !IsNPC);
        public bool HasBuilderPermission(ItemTemplateData item) => item.Area.Builders.IsName(Name, true) || (Level == Game.MAX_LEVEL && !IsNPC);
        public bool HasBuilderPermission(NPCTemplateData npc) => npc.Area.Builders.IsName(Name, true) || (Level == Game.MAX_LEVEL && !IsNPC);


        public bool IsInactive { get { return (this is Player player && DateTime.Now - player.LastActivity > TimeSpan.FromMinutes(5)); } }

        public bool IsAFK { get { return this.Flags.ISSET(ActFlags.AFK) || IsInactive; } }


        public string DisplayFlags(Character viewer)
        {
            var flags = "";
            Player player = null;

            if (this is Player && (player = (Player)this).IsAFK)
                flags += "\\r(AFK)\\x";
            if (player != null && player.inanimate != null)
                flags += "(inanimate)";
            if (this.Flags.ISSET(ActFlags.WizInvis))
                flags += "\\w(WizInvis)\\x";
            if (this.AffectedBy.ISSET(AffectFlags.Ghost))
                flags += "\\W(Ghost)\\x";
            if (this.IsAffected(AffectFlags.Burrow))
                flags += "(Burrowed)";
            if (this.IsAffected(AffectFlags.Camouflage))
                flags += "\\G(Camouflaged)\\x";
            if (this.AffectedBy.ISSET(AffectFlags.Hide))
                flags = "\\R(Hidden)\\x";
            if (this.AffectedBy.ISSET(AffectFlags.Invisible))
                flags += "(Invis)";
            if (this.AffectedBy.ISSET(AffectFlags.Sanctuary))
                flags += "\\W(White Aura)\\x";
            if (this.AffectedBy.ISSET(AffectFlags.Haven))
                flags += "(Haven)";
            if (viewer.AffectedBy.ISSET(AffectFlags.KnowAlignment) && this.Alignment == Alignment.Evil)
                flags += "\\r(Red Aura)\\x";
            if (viewer.AffectedBy.ISSET(AffectFlags.KnowAlignment) && this.Alignment == Alignment.Good)
                flags += "\\y(Golden Aura)\\x";
            if (this.IsAffected(AffectFlags.FaerieFire) || this.IsAffected(SkillSpell.SkillLookup("faerie fog")))
                flags += "\\m(Purple)\\x";
            if (this.IsAffected(AffectFlags.Smelly))
                flags += "(Smelly)";

            if (flags.Length > 0) flags += " ";
            return flags;
        }

        public (int acBash, int acSlash, int acPierce, int acExotic) GetArmorClass()
        {
            int acBash = 0;
            int acSlash = 0;
            int acPierce = 0;
            int acExotic = 0;

            if (Form == null)
            {
                foreach (var eq in Equipment)
                {
                    if (eq.Value != null)
                    {
                        acBash += -eq.Value.ArmorBash;
                        acSlash += -eq.Value.ArmorSlash;
                        acPierce += -eq.Value.ArmorPierce;
                        acExotic += -eq.Value.ArmorExotic;
                    }
                }
                acBash += ArmorBash;
                acSlash += ArmorSlash;
                acPierce += ArmorPierce;
                acExotic += ArmorExotic;
            }
            else
            {
                acBash += Form.ArmorBash;
                acSlash += Form.ArmorSlash;
                acPierce += Form.ArmorPierce;
                acExotic += Form.ArmorExotic;
            }
            acBash += ArmorClass;
            acSlash += ArmorClass;
            acPierce += ArmorClass;
            acExotic += ArmorClass;

            var dexAC = PhysicalStats.DexterityApply[GetCurrentStat(PhysicalStatTypes.Dexterity)].Defensive;

            acBash += dexAC;
            acSlash += dexAC;
            acPierce += dexAC;
            acExotic += dexAC;

            return (acBash, acSlash, acPierce, acExotic);

        }

        public static void SendItemList(Character ch, ItemData lookitem)
        {
            var tempItemList = new Dictionary<string, int>();

            if (lookitem.Contains.Count != 0)
            {
                using (new Page(ch))
                {
                    foreach (var subitem in lookitem.Contains)
                    {
                        if (!ch.CanSee(subitem)) continue;
                        string flags = "";
                        if (subitem.extraFlags.Contains(ExtraFlags.Glow))
                            flags = "(Glowing)";
                        if (subitem.extraFlags.Contains(ExtraFlags.Hum))
                            flags += "(Humming)";
                        if (subitem.extraFlags.Contains(ExtraFlags.Invisibility))
                            flags += "(Invis)";
                        if (subitem.extraFlags.Contains(ExtraFlags.Magic) &&
                            (ch.IsAffected(AffectFlags.DetectMagic) || ch.IsAffected(AffectFlags.ArcaneVision)))
                            flags += "(Magic)";
                        if (subitem.IsAffected(AffectFlags.Poison))
                            flags += "(Poisonous)";
                        if (flags.Length > 0) flags += " ";
                        var itemshow = flags + (!subitem.ShortDescription.ISEMPTY() ? subitem.ShortDescription : subitem.Name);
                        if (tempItemList.ContainsKey(itemshow))
                            tempItemList[itemshow] = tempItemList[itemshow] + 1;
                        else
                            tempItemList[itemshow] = 1;
                        //    carrying.AppendLine();
                    }
                    foreach (var itemkvp in tempItemList)
                    {
                        ch.send("    " + (itemkvp.Value > 1 ? "[" + itemkvp.Value + "] " : "") + itemkvp.Key + "\n\r");
                    }
                    if (tempItemList.Count == 0)
                        ch.Act("$p appears to be empty.\n\r", null, lookitem);
                }

                //foreach (var subitem in lookitem.Contains)
                //    ch.send("    " + subitem.ShortDescription + "\n\r");
            }

            else ch.Act("$p appears to be empty.\n\r", null, lookitem);
        }

        public static void ScanDirection(Character ch, Direction direction, RoomData room = null, int depth = 0)
        {
            depth++;
            if (room == null) room = ch.Room;
            if (depth == 1)
            {
                ch.send("You scan " + direction.ToString().ToLower() + ".\n\r");
                ch.Act("$n scans " + direction.ToString().ToLower() + ".\n\r", type: ActType.ToRoom);
            }
            ExitData exit;
            if ((exit = room.exits[(int)direction]) != null && exit.destination != null)// && !exit.flags.ISSET(ExitFlags.Closed))
            {
                var IsDark = exit.destination.IsDark;

                var others = from other in exit.destination.Characters where ch.CanSee(other) && ch != other select other;

                var fog = SkillSpell.SkillLookup("faerie fog");
                if (exit.flags.ISSET(ExitFlags.Closed))
                {
                    ch.send("**** " + depth + " " + direction.ToString() + " ****\n\r");
                    ch.send("Closed\n\r");
                    return;
                }
                //else if (!ch.IsAffected(AffectFlags.Infrared) && !ch.IsAffected(AffectFlags.DarkVision) && !ch.IsAffected(AffectFlags.NightVision)  && IsDark)
                //{
                //    ch.send("**** " + depth + " " + direction.ToString() + " ****\n\r");
                //    ch.send("Too dark to tell\n\r");
                //}
                else if (others.Any())
                {
                    ch.send("**** " + depth + " " + direction.ToString() + " ****\n\r");
                    foreach (var other in others)
                    {

                        ch.send(other.DisplayFlags(ch));
                        ch.Act(other.GetLongDescription(ch).Trim());

                    }
                }
                if (depth < 4 && !exit.flags.ISSET(ExitFlags.Closed))
                    ScanDirection(ch, direction, exit.destination, depth);

                // if (depth == 1)
                //ch.send("\n\r");

            }
            else
                return;
        }

        public static void ShowSpells(Character ch, string arguments)
        {
            int lastLevel = 0;
            int column = 0;
            var text = new StringBuilder();

            if (!arguments.ISEMPTY())
            {
                var skills = (from sk in SkillSpell.Skills.Values
                              where sk.name.StringPrefix(arguments) &&
                             (sk.SkillTypes.Contains(SkillSpellTypes.Spell)
                                || sk.SkillTypes.Contains(SkillSpellTypes.Commune)
                                || sk.SkillTypes.Contains(SkillSpellTypes.Song)
                                || (sk.SkillTypes.ISSET(SkillSpellTypes.InForm) && sk.spellFun != null))
                              orderby ch.GetLevelSkillLearnedAt(sk)
                              select sk);

                if (!skills.Any())
                {
                    ch.send("You don't know any abilities by that name");
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
                            ch.send("You haven't learned that ability yet.");
                            return;
                        }

                        ch.send(skill.name + " " + percent + "% {0} mana\n\r", skill.GetManaCost(ch));
                    }
                    return;
                }
            }

            using (new Page(ch))
            {
                foreach (var skill in from tempskill in SkillSpell.Skills.Values
                                      where (tempskill.SkillTypes.Contains(SkillSpellTypes.Spell)
                                      || tempskill.SkillTypes.Contains(SkillSpellTypes.Commune)
                                      || tempskill.SkillTypes.Contains(SkillSpellTypes.Song)
                                      || (tempskill.SkillTypes.ISSET(SkillSpellTypes.InForm) && tempskill.spellFun != null))
                                      orderby ch.GetLevelSkillLearnedAt(tempskill)
                                      select tempskill) //skills)
                {
                    //ch.Learned.TryGetValue(skill, out int percent);
                    //skill.skillLevel.TryGetValue(ch.Guild.name, out int lvl);

                    var percent = ch.GetSkillPercentage(skill);
                    var lvl = ch.GetLevelSkillLearnedAt(skill);

                    if ((lvl <= Game.LEVEL_HERO || ch.IsImmortal) || percent > 1) // || (ch.Level > lvl && lvl > 0))
                    {
                        if (lvl != lastLevel)
                        {
                            lastLevel = lvl;
                            column = 0;
                            text.AppendLine();
                            text.Append("Lvl " + lvl + ": ".PadRight(5));
                            text.AppendLine();
                        }

                        text.Append("    " + ((skill.name + " " + (ch.Level > lvl || percent > 1 ? percent + "% " : "N/A")).PadLeft(30) + (percent >= 1 ? skill.GetManaCost(ch) + " mana" : "")).PadRight(35));

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
            }
        }

        public bool GetItem(ItemData item, ItemData container = null)
        {
            if (item.wearFlags.ISSET(WearFlags.Take))
            {
                if (Carry + 1 > MaxCarry)
                {
                    send("You can't carry anymore items.\n\r");
                    return false;
                }

                if (container == null)
                {
                    if (TotalWeight + item.Weight > MaxWeight)
                    {
                        send("You can't carry anymore weight.\n\r");
                        return false;
                    }
                    Room.items.Remove(item);
                    item.Room = null;
                    Act("You get $p.\n\r", null, item, type: ActType.ToChar);
                    Act("$n gets $p.\n\r", null, item, null, ActType.ToRoom);
                }
                else
                {
                    if (container.CarriedBy != this && TotalWeight + item.Weight > MaxWeight)
                    {
                        send("You can't carry anymore weight.\n\r");
                        return false;
                    }

                    container.Contains.Remove(item);
                    item.Container = null;
                    Act("You get $p from $P.\n\r", null, item, container, ActType.ToChar);
                    Act("$n gets $p from $P.\n\r", null, item, container, ActType.ToRoom);
                }
                AddInventoryItem(item);
                return true;
            }
            else
                return false;
        }

        public static bool PutItem(Character ch, ItemData item, ItemData container)
        {
            if (container != null && container.ItemType.Contains(ItemTypes.Container) && item != null)
            {
                if (container.extraFlags.Contains(ExtraFlags.Closed))
                {
                    ch.Act("$p is closed.\n\r", null, container, null, ActType.ToChar);
                    return false;
                }
                float weight = container.totalweight;

                if (weight + item.Weight < container.MaxWeight)
                {

                    ch.Inventory.Remove(item);
                    container.Contains.Insert(0, item);
                    ch.Act("You put $p in $P.\n\r", null, item, container, ActType.ToChar);
                    ch.Act("$n puts $p in $P.\n\r", null, item, container, ActType.ToRoom);
                    item.CarriedBy = container.CarriedBy;
                    item.Container = container;
                    return true;
                }
                else
                {
                    ch.send("{0} cannot hold that much weight.\n\r", container.Display(ch));
                }
            }
            return false;
        }
    } // end of character
} // end namespace