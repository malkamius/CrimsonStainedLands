using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public enum WearFlags
    {
        Take,
        Finger,
        About,
        Neck,
        Head,
        Legs,
        Feet,
        Hands,
        Wield,
        Hold,
        Float,
        Tattoo,
        Body,
        Waist,
        Wrist,
        Arms,
        Shield,
        NoSac,

        WearFloat = Float,
        Brand = Tattoo
    }

    public enum ItemTypes
    {
        Armor = 1,
        Boat,
        Cabal,
        Clothing,
        Container,
        Corpse,
        DrinkContainer,
        Food,
        Fountain,
        Furniture,
        Gem,
        Gold,
        Instrument,
        Jewelry,
        Key,
        Light,
        Map,
        Money,
        NPCCorpse,
        Pill,
        Portal,
        Potion,
        RoomKey,
        Scroll,
        Silver,
        Skeleton,
        Staff,
        Trash,
        Treasure,
        Wand,
        Weapon,
        WarpStone,

        PC_Corpse = Corpse,
        Talisman = Staff,
        ThiefPick = 33
    }
    public enum WeaponTypes
    {
        None,
        Sword,
        Axe,
        Dagger,
        Mace,
        Staff,
        Spear,
        Whip,
        Flail,
        Polearm,
        Exotic
    }
    public enum WeaponDamageTypes
    {
        Bite,
        Sting,
        Slap,
        Divine,
        Wrath,
        Whack,
        Slice,
        Pierce,
        Smash,
        Bash,
        Slash,
        Pound,
        Cut,
        Chop,
        Crush,
        Claw,
        Burn,
        Freeze,
        FrBite,
        Corrosion,
        Poison,
        Toxin,
        Psychic,
        Blast,
        Decay,
        Holy,
        Electrical,
        Acid,
        Energy,
        Lightning,
        Fire,
        Cold,
        Negative,
        Light,
        Mental,
        Sound,
        Force, // anything magical
        Whomp,
        Other,
        Magic,
        Disease,
        Summon,
        Silver,
        Mithril,
        Wood,
        Iron,
        Drowning,
        Charm,
        Weapon,
        None,
        Air,
        Blind
    }


    public enum ExtraFlags
    {
        Glow = 1,
        Hum,
        Dark,
        Lock,
        Locked,
        Evil,
        Invisibility,
        Magic,
        NoDrop,
        Bless,
        AntiGood,
        AntiNeutral,
        AntiEvil,
        NoRemove,
        NoPurge,
        RotDeath,
        VisDeath,
        Fixed,
        NoDisarm,
        NoLocate,
        MeltDrop,
        Closable,
        PickProof,
        Closed,
        WeaponFlaming,
        Poison,
        Heart,
        PouchNourishment,
        EverfullSkin,
        BurnProof,
        Flaming,
        Frost,
        Vorpal,
        Shocking,
        Unholy,
        Sharp,
        TwoHands,
        NoUncurse,
        SellExtract,
        PutIn,
        PutOn,
        PutAt,
        StandAt,
        StandOn,
        SitAt,
        SitIn,
        SitOn,
        RestIn,
        RestOn,
        RestAt,
        SleepAt,
        SleepOn,
        SleepIn,
        Inventory,
        Outfit,
        UniqueEquip,
        Closeable = Closable,
        Invis = Invisibility
    }

    public class ItemData : IDisposable
    {
        public static List<ItemData> Items = new List<ItemData>();

        public AreaData Area;
        public int Vnum;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string NightShortDescription { get; set; } = "";
        public string LongDescription { get; set; } = "";
        public string NightLongDescription { get; set; } = "";

        public int Level;

        public int Nutrition;

        public float Weight;

        public RoomData Room;

        public int MaxDurability = 100;
        private int DurabilityValue = 100;

        public int Durability
        {
            get
            {
                return DurabilityValue;
            }
            set
            {
                if (DurabilityValue != value)
                {
                    WearSlot slot;

                    // apply affects if item repaired and being worn already
                    if (DurabilityValue <= 0 && value > 0 && CarriedBy != null && (slot = CarriedBy.GetEquipmentWearSlot(this)) != null)
                    {
                        foreach (var affect in this.affects)
                        {
                            CarriedBy.AffectApply(affect, false, false);
                        }
                    }

                    // set new durability
                    DurabilityValue = value;

                    // remove affects if item damaged to 0 and item is being worn
                    if (value == 0 && CarriedBy != null && (slot = CarriedBy.GetEquipmentWearSlot(this)) != null)
                    {
                        foreach (var affect in this.affects)
                        {
                            CarriedBy.AffectApply(affect, true, false);
                        }
                    }
                }
            }
        }

        public Dice DamageDice = new Dice(0, 0, 0);

        public WeaponDamageMessage WeaponDamageType;
        public WeaponTypes WeaponType;

        public HashSet<WearFlags> wearFlags = new HashSet<WearFlags>();
        public HashSet<ItemTypes> ItemType = new HashSet<ItemTypes>();
        public HashSet<ExtraFlags> extraFlags = new HashSet<ExtraFlags>();
        public List<AffectData> affects = new List<AffectData>();

        public List<ItemData> Contains = new List<ItemData>();

        public List<int> Keys = new List<int>();
        public int timer;

        public int Silver;
        public int Gold;
        public string Owner = "";
        /// <summary>
        /// as in max weight a container can contain
        /// </summary>
        public float MaxWeight;
        public string Material;

        public string Liquid;
        public int Charges;
        public int MaxCharges;

        public int ArmorBash = 0;
        public int ArmorPierce = 0;
        public int ArmorSlash = 0;
        public int ArmorExotic = 0;
        public int Value = 0;
        public List<ExtraDescription> ExtraDescriptions = new List<ExtraDescription>();
        public List<ItemSpellData> Spells = new List<ItemSpellData>();

        public List<Programs.Program<ItemData>> Programs = new List<Programs.Program<ItemData>>();
        public List<NLuaPrograms.NLuaProgram> LuaPrograms = new List<NLuaPrograms.NLuaProgram>();

        internal CharacterSize Size;

        /// <summary>
        /// returns item flags
        /// </summary>
        public string DisplayFlags(Character to)
        {
            string flags = "";
            if (this.extraFlags.Contains(ExtraFlags.Glow))
                flags = "(Glowing)";
            if (this.extraFlags.Contains(ExtraFlags.Hum))
                flags += "(Humming)";
            if (this.extraFlags.Contains(ExtraFlags.Invisibility))
                flags += "(Invis)";
            if (this.extraFlags.Contains(ExtraFlags.Magic) &&
                (to.IsAffected(AffectFlags.DetectMagic) || to.IsAffected(AffectFlags.ArcaneVision)))
                flags += "(Magic)";
            if (this.IsAffected(AffectFlags.Poison))
                flags += "(Poisonous)";

            if (this.Durability == 0)
                flags += "(Broken)";
            else if (this.Durability < this.MaxDurability * .75)
                flags += "(Damaged)";

            if (flags.Length > 0) flags += " ";
            return !flags.ISEMPTY() ? flags : "";
        }

        /// <summary>
        /// returns item short description unless missing, then returns item name
        /// </summary>
        public string Display(Character to)
        {
            if (!to.CanSee(this))
                return "something";
            return ((TimeInfo.IS_NIGHT && !this.NightShortDescription.ISEMPTY()) ? this.NightShortDescription : !ShortDescription.ISEMPTY() ? ShortDescription : Name);
        }

        public string DisplayToRoom(Character to)
        {
            if (!to.CanSee(this))
                return "something is here.";
            return (((TimeInfo.IS_NIGHT && !this.NightLongDescription.ISEMPTY()) ? this.NightLongDescription :
                   ((TimeInfo.IS_NIGHT && !this.NightShortDescription.ISEMPTY()) ? this.NightShortDescription :
                   (this.LongDescription.ISEMPTY() ? (this.ShortDescription.ISEMPTY() ? this.Name : this.ShortDescription) : this.LongDescription.Trim()))));
        }

        public void Dispose()
        {
            ((IDisposable)this).Dispose();

        }
        void IDisposable.Dispose()
        {
            if (Room != null)
            {
                Room.items.Remove(this);
                Room = null;
            }

            if (CarriedBy != null)
            {
                if (CarriedBy.Inventory.Contains(this))
                { CarriedBy.Inventory.Remove(this); }
                CarriedBy.RemoveEquipment(this, false, true);
                CarriedBy = null;
            }

            if (Container != null)
            {
                if (Container.Contains.Contains(this))
                {
                    Container.Contains.Remove(this);
                }
                Container = null;
            }

            Items.Remove(this);
        }

        public ItemData(ItemTemplateData template)
        {
            Area = template.Area;
            Vnum = template.Vnum;
            Name = template.Name;
            Description = template.Description;
            ShortDescription = template.ShortDescription;
            NightShortDescription = template.NightShortDescription;
            LongDescription = template.LongDescription;
            NightLongDescription = template.NightLongDescription;
            Level = template.Level;
            WeaponDamageType = template.WeaponDamageType;
            DamageDice = new Dice(template.DamageDice);
            Silver = template.Silver;
            Gold = template.Gold;
            wearFlags.AddRange(template.wearFlags);
            extraFlags.AddRange(template.extraFlags);
            ItemType.AddRange(template.itemTypes);
            foreach (var af in template.affects)
            {
                if (af.duration == 0)
                {
                    af.duration = -1;
                    template.Area.saved = false;
                }
            }

            affects.AddRange(from af in template.affects select new AffectData(af));

            Nutrition = template.Nutrition;
            Liquid = template.Liquid;
            Weight = template.Weight;
            MaxWeight = template.MaxWeight;
            Material = template.Material;
            Charges = template.Charges;
            MaxCharges = template.MaxCharges;
            WeaponType = template.WeaponType;
            ArmorBash = template.ArmorBash;
            ArmorPierce = template.ArmorPierce;
            ArmorSlash = template.ArmorSlash;
            ArmorExotic = template.ArmorExotic;
            Value = template.Value;
            MaxDurability = template.MaxDurability;
            Durability = MaxDurability;
            ExtraDescriptions = template.ExtraDescriptions;
            Programs.AddRange(template.Programs);
            LuaPrograms.AddRange(template.LuaPrograms);
            Keys.AddRange(template.Keys);
            Spells.AddRange(from spell in template.spells select new ItemSpellData(spell.Level, spell.SpellName));
            ItemData.Items.Add(this);
        }
        public ItemData(ItemTemplateData template, RoomData room) : this(template)
        {
            //ItemToRoom(room);
            room.items.Insert(0, this);
            this.Room = room;
        }

        public ItemData(ItemTemplateData template, ItemData item) : this(template)
        {
            item.Contains.Add(this);
            Container = item;
        }

        public ItemData(ItemTemplateData template, Character ch, bool wear = false) : this(template)
        {
            if (!wear || !ch.wearItem(this, false))
                ch.AddInventoryItem(this);
            CarriedBy = ch;
        }
        public float totalweight
        {
            get
            {
                float weight;
                weight = this.Weight;
                foreach (var contained in Contains) weight += contained.totalweight;
                return weight;
            }
        }

        public ItemData(XElement element)
        {
            Vnum = element.GetElementValueInt("vnum");

            ItemTemplateData template;

            ItemTemplateData.Templates.TryGetValue(Vnum, out template);
            Level = element.GetElementValueInt("level", template != null ? template.Level : 0);

            Weight = element.GetElementValueFloat("weight", template != null ? template.Weight : 0); ;

            Name = element.GetElementValue("name", template != null ? template.Name : "");
            if (string.IsNullOrEmpty(Name) && template != null)
                Name = template.Name;
            ShortDescription = element.GetElementValue("ShortDescription", template != null ? template.ShortDescription : "");
            if (string.IsNullOrEmpty(ShortDescription) && template != null)
                ShortDescription = template.ShortDescription;
            LongDescription = element.GetElementValue("LongDescription", template != null ? template.LongDescription : "");
            if (string.IsNullOrEmpty(LongDescription) && template != null)
                LongDescription = template.LongDescription;

            NightShortDescription = element.GetElementValue("NightShortDescription", template != null ? template.NightShortDescription : "");
            NightLongDescription = element.GetElementValue("NightLongDescription", template != null ? template.NightLongDescription : "");



            Description = element.GetElementValue("Description", template != null ? template.Description : "");
            if (string.IsNullOrEmpty(Description) && template != null)
                Description = template.Description;
            Material = element.GetElementValue("Material", null);
            if (string.IsNullOrEmpty(Material) && template != null)
                Material = template.Material;

            Liquid = element.GetElementValue("Liquid", null);
            if (string.IsNullOrEmpty(Liquid) && template != null)
                Liquid = template.Liquid;

            ArmorBash = element.GetElementValueInt("ArmorBash", template != null ? template.ArmorBash : 0);
            ArmorSlash = element.GetElementValueInt("ArmorSlash", template != null ? template.ArmorSlash : 0);
            ArmorPierce = element.GetElementValueInt("ArmorPierce", template != null ? template.ArmorPierce : 0);
            ArmorExotic = element.GetElementValueInt("ArmorExotic", template != null ? template.ArmorExotic : 0);

            MaxDurability = element.GetElementValueInt("MaxDurability", template != null ? template.MaxDurability : 0);
            Durability = element.GetElementValueInt("Durability", template != null ? template.MaxDurability : 100);

            timer = element.GetElementValueInt("timer");

            if (element.HasElement("Keys"))
            {
                Keys.AddRange(from keyelement in element.GetElement("Keys").Elements() select keyelement.GetAttributeValueInt("vnum"));
            }
            else if (template != null && template.Keys.Count > 0)
                Keys.AddRange(template.Keys);


            if (!element.HasElement("WearFlags") && template != null && template.wearFlags != null)
                wearFlags.AddRange(template.wearFlags);
            if (!element.HasElement("ExtraFlags") && template != null && template.extraFlags != null)
                extraFlags.AddRange(template.extraFlags);
            if (!element.HasElement("ItemTypes") && template != null && template.itemTypes != null)
                ItemType.AddRange(template.itemTypes);

            wearFlags.AddRange(from flag in Utility.LoadFlagList<WearFlags>(element.GetElementValue("WearFlags")).Distinct() where !wearFlags.ISSET(flag) select flag);
            extraFlags.AddRange(from flag in Utility.LoadFlagList<ExtraFlags>(element.GetElementValue("ExtraFlags")).Distinct() where !extraFlags.ISSET(flag) select flag);
            ItemType.AddRange(from flag in Utility.LoadFlagList<ItemTypes>(element.GetElementValue("ItemTypes")).Distinct() where !ItemType.ISSET(flag) select flag);

            if (template != null)
                Spells.AddRange(template.spells);

            if (element.HasElement("Affects"))
            {
                var affectsElement = element.GetElement("Affects");
                foreach (var affectElement in affectsElement.Elements("Affect"))
                {
                    var affect = new AffectData(affectElement);



                    //if (!template.affects.Any(af => af.skillSpell == affect.skillSpell && af.modifier == affect.modifier && af.location == affect.location))
                    affects.Add(affect);
                }
            }

            if (affects.Count == 0 && template != null && template.affects != null)
                affects.AddRange(from aff in template.affects where !aff.flags.ISSET(AffectFlags.Curse) select new AffectData(aff));



            if (element.HasElement("contains"))
            {
                foreach (var item in element.GetElement("contains").Elements())
                {
                    Contains.Add(new ItemData(item));
                }
            }
            WeaponDamageType = WeaponDamageMessage.GetWeaponDamageMessage(!string.IsNullOrEmpty(element.GetElementValue("weapondamagetype")) ? element.GetElementValue("weapondamagetype") : (template != null ? template.WeaponDamageType.Keyword : "none"));
            Utility.GetEnumValue<WeaponTypes>(element.GetElementValue("WeaponType"), ref WeaponType, template != null ? template.WeaponType : WeaponTypes.None);

            DamageDice.DiceCount = element.GetElementValueInt("DiceCount", template != null ? template.DamageDice.DiceCount : 1);
            DamageDice.DiceSides = element.GetElementValueInt("DiceSides", template != null ? template.DamageDice.DiceSides : 1);
            DamageDice.DiceBonus = element.GetElementValueInt("DiceBonus", template != null ? template.DamageDice.DiceBonus : 1);

            Silver = element.GetElementValueInt("silver", template != null ? template.Silver : 0);
            Gold = element.GetElementValueInt("gold", template != null ? template.Gold : 0);
            Nutrition = element.GetElementValueInt("nutrition", template != null ? template.Nutrition : 0);
            MaxWeight = element.GetElementValueFloat("maxweight", template != null ? template.MaxWeight : 0);
            Charges = element.GetElementValueInt("charges", template != null ? template.Charges : 0);
            MaxCharges = element.GetElementValueInt("maxcharges", template != null ? template.MaxCharges : 0);
            Value = element.GetElementValueInt("Cost", template != null ? template.Value : 0);

            if (element.HasElement("Programs"))
            {
                var programsElement = element.GetElement("Programs");
                foreach (var programElement in programsElement.Elements())
                {

                    if (CrimsonStainedLands.Programs.ItemProgramLookup(programElement.GetAttributeValue("Name"), out var program))
                    {
                        Programs.Add(program);
                    }
                    else if (CrimsonStainedLands.NLuaPrograms.ProgramLookup(programElement.GetAttributeValue("Name"), out var luaprogram))
                    {
                        LuaPrograms.Add(luaprogram);
                    }
                }
            }

            if (template != null && template.Programs.Any())
            {
                foreach (var program in template.Programs)
                {
                    if (!Programs.Any(p => p.Name == program.Name))
                    { 
                        Programs.Add(program); 
                    }
                }
            }

            if (template != null && template.LuaPrograms.Any())
            {
                foreach (var program in template.LuaPrograms)
                {
                    if (!LuaPrograms.Any(p => p.Name == program.Name))
                    { 
                        LuaPrograms.Add(program); 
                    }
                }
            }

            if (element.HasElement("Spells"))
            {
                var spellsElement = element.GetElement("Spells");
                foreach (var spellElement in spellsElement.Elements("Spell"))
                {
                    Spells.Add(new ItemSpellData(spellElement));
                }
            }

            if (element.HasElement("ExtraDescriptions"))
            {
                foreach (var EDElement in element.Element("ExtraDescriptions").Elements())
                {
                    ExtraDescriptions.Add(new ExtraDescription(EDElement.GetElementValue("keyword"), EDElement.GetElementValue("description")));
                }
            }

            if (template != null) { ExtraDescriptions.AddRange(template.ExtraDescriptions); }
            ItemData.Items.Add(this);
        }

        public ItemTemplateData Template
        {
            get
            {
                ItemTemplateData template = null;
                ItemTemplateData.Templates.TryGetValue(Vnum, out template);
                return template;
            }
        }

        public XElement Element
        {
            get
            {
                var elements = new XElement("Item");
                var template = Template;
                // TODO Extra descriptions not compared to template
                elements.Add(new XElement("VNum", Vnum));

                if (template == null || (Name != template.Name && !string.IsNullOrEmpty(Name)))
                    elements.Add(new XElement("Name", Name.TOSTRINGTRIM()));

                if (template == null || (ShortDescription != template.ShortDescription && !string.IsNullOrEmpty(ShortDescription)))
                    elements.Add(new XElement("ShortDescription", ShortDescription.TOSTRINGTRIM()));

                if (template == null || (LongDescription != template.LongDescription && !string.IsNullOrEmpty(LongDescription)))
                    elements.Add(new XElement("LongDescription", LongDescription.TOSTRINGTRIM()));

                if (template == null || (NightShortDescription != template.NightShortDescription && !string.IsNullOrEmpty(NightShortDescription)))
                    elements.Add(new XElement("NightShortDescription", NightShortDescription.TOSTRINGTRIM()));

                if (template == null || (NightLongDescription != template.NightLongDescription && !string.IsNullOrEmpty(NightLongDescription)))
                    elements.Add(new XElement("NightLongDescription", NightLongDescription.TOSTRINGTRIM()));

                if (template == null || (Description != template.Description && !string.IsNullOrEmpty(Description)))
                    elements.Add(new XElement("Description", Description.TOSTRINGTRIM()));

                if (template == null || Level != template.Level)
                    elements.Add(new XElement("Level", Level));

                if (template == null || WeaponDamageType != template.WeaponDamageType)
                    elements.Add(new XElement("WeaponDamageType", WeaponDamageType.Keyword));

                if (template == null || WeaponType != template.WeaponType)
                    elements.Add(new XElement("WeaponType", WeaponType));

                if (template == null || DamageDice.DiceSides != template.DamageDice.DiceSides)
                    elements.Add(new XElement("DiceSides", DamageDice.DiceSides));

                if (template == null || DamageDice.DiceCount != template.DamageDice.DiceCount)
                    elements.Add(new XElement("DiceCount", DamageDice.DiceCount));

                if (template == null || DamageDice.DiceBonus != template.DamageDice.DiceBonus)
                    elements.Add(new XElement("DiceBonus", DamageDice.DiceBonus));

                if (template == null || Weight != template.Weight)
                    elements.Add(new XElement("Weight", Weight));

                if (template == null || Silver != template.Silver)
                    elements.Add(new XElement("Silver", Silver));

                if (template == null || Gold != template.Gold)
                    elements.Add(new XElement("Gold", Gold));

                if (template == null || Nutrition != template.Nutrition)
                    elements.Add(new XElement("Nutrition", Nutrition));

                if (template == null || Liquid != template.Liquid)
                    elements.Add(new XElement("Liquid", Liquid));

                if (template == null || MaxWeight != template.MaxWeight)
                    elements.Add(new XElement("MaxWeight", MaxWeight));

                if (template == null || Material != template.Material)
                    elements.Add(new XElement("Material", Material));

                if (template == null || Charges != template.Charges)
                    elements.Add(new XElement("Charges", Charges));

                if (template == null || MaxCharges != template.MaxCharges)
                    elements.Add(new XElement("MaxCharges", MaxCharges));

                if (template == null || ArmorBash != template.ArmorBash)
                    elements.Add(new XElement("ArmorBash", ArmorBash));

                if (template == null || ArmorSlash != template.ArmorSlash)
                    elements.Add(new XElement("ArmorSlash", ArmorSlash));

                if (template == null || ArmorPierce != template.ArmorPierce)
                    elements.Add(new XElement("ArmorPierce", ArmorPierce));

                if (template == null || ArmorExotic != template.ArmorExotic)
                    elements.Add(new XElement("ArmorExotic", ArmorExotic));

                if (template == null || Value != template.Value)
                    elements.Add(new XElement("Cost", Value));

                if (template == null || MaxDurability != template.MaxDurability)
                    elements.Add(new XElement("MaxDurability", MaxDurability));

                elements.Add(new XElement("Durability", Durability));

                if (timer > 0)
                    elements.Add(new XElement("timer", timer));

                // Only save wear flags if they are different than template or template doesn't exist
                var wearFlagsString = string.Join(" ", (from flag in wearFlags.Distinct() select flag.ToString()));

                if (string.IsNullOrEmpty(wearFlagsString) && template != null)
                {
                    var templateWearFlagsString = string.Join(" ", (from flag in wearFlags select flag.ToString()));
                    if (templateWearFlagsString != wearFlagsString)
                        elements.Add(new XElement("WearFlags", wearFlagsString));
                }
                else
                    elements.Add(new XElement("WearFlags", wearFlagsString));


                // Only save extra flags if they are different than template or template doesn't exist
                var extraFlagsString = string.Join(" ", (from flag in extraFlags.Distinct() select flag.ToString()));

                //if (template != null)
                //{
                //    var templateExtraFlagsString = string.Join(" ", (from flag in extraFlags select flag.ToString()));
                //    if (templateExtraFlagsString != extraFlagsString)
                //        elements.Add(new XElement("ExtraFlags", extraFlagsString));
                //}
                //else
                elements.Add(new XElement("ExtraFlags", extraFlagsString));
                elements.Add(Programs.Any() ? new XElement("Programs", from program in Programs select new XElement("Program", new XAttribute("Name", program.Name))) : null);
                //elements.Add(new XElement("ExtraFlags", string.Join(" ", from flag in extraFlags select flag.ToString())));
                elements.Add(new XElement("Affects", from aff in affects select aff.Element));
                elements.Add(new XElement("ItemTypes", string.Join(" ", from itemtype in ItemType.Distinct() select itemtype.ToString())));
                elements.Add(new XElement("Contains", from item in Contains ?? new List<ItemData>() select item.Element));
                if (Keys.Count > 0)
                {
                    elements.Add(new XElement("Keys", from key in Keys select new XElement("Key", new XAttribute("vnum", key))));
                }
                return elements;
            }
        }
        public AffectData FindAffect(SkillSpell skillSpell) => skillSpell == null ? null : (from aff in affects where aff.skillSpell == skillSpell select aff).FirstOrDefault();

        public AffectData FindAffect(string skillname) => FindAffect(SkillSpell.SkillLookup(skillname));

        public AffectData FindAffect(AffectFlags flag) => (from aff in affects where aff.flags.ISSET(flag) select aff).FirstOrDefault();

        public bool IsAffected(AffectFlags flag)
        {
            // if (AffectedBy.ISSET(flag)) return true;
            foreach (var aff in affects)
                if (aff.flags.Contains(flag))
                    return true;
            return false;
        }

        public bool IsAffected(SkillSpell AffectSkill)
        {
            if (AffectSkill != null)
                foreach (var aff in affects)
                    if (aff.skillSpell == AffectSkill)
                        return true;
            return false;
        }
        public Character CarriedBy { get; set; }
        public ItemData Container { get; internal set; }
        public Alignment Alignment { get; internal set; }

        public static DateTime LastSaveCorpsesAndPits = DateTime.Now;
        public static void SaveCorpsesAndPits(bool force = false)
        {
            if (DateTime.Now >= LastSaveCorpsesAndPits.AddMinutes(5) || force)
            {
                LastSaveCorpsesAndPits = DateTime.Now;

                var pitVnums = new List<int>() { 19000, 3010 };
                XElement itemData = new XElement("Items");
                foreach (var container in ItemData.Items)
                {
                    if (container.Room != null && (container.ItemType.ISSET(ItemTypes.PC_Corpse) || pitVnums.Contains(container.Vnum)))
                    {
                        itemData.Add(new XComment(string.Format("{0} in room {1}", container.ShortDescription ?? container.Name,
                            container.Room.Vnum + " - " + container.Room.Name)));
                        var itemElement = container.Element;
                        itemElement.Add(new XAttribute("RoomVnum", container.Room.Vnum));
                        itemData.Add(itemElement);
                    }
                }

                System.IO.File.WriteAllText(Settings.DataPath + "\\corpses_and_pits.xml", itemData.ToStringFormatted());
            }
        }

        public static void LoadCorpsesAndPits()
        {
            if (System.IO.File.Exists(Settings.DataPath + "\\corpses_and_pits.xml"))
            {
                XElement itemsData = XElement.Load(Settings.DataPath + "\\corpses_and_pits.xml");

                foreach (var itemElement in itemsData.Elements())
                {
                    if (RoomData.Rooms.TryGetValue(itemElement.GetAttributeValueInt("RoomVnum"), out var room))
                    {
                        var item = new ItemData(itemElement);
                        item.Room = room;
                        room.items.Insert(0, item);
                    }
                }
            }
        }
    }
}