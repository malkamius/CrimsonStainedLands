using CrimsonStainedLands.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public class ItemTemplateData
    {
        public static Dictionary<int, ItemTemplateData> Templates = new Dictionary<int, ItemTemplateData>();

        public AreaData Area;

        public int Vnum;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string NightShortDescription { get; set; } = "";
        public string LongDescription { get; set; } = "";
        public string NightLongDescription { get; set; } = "";
        public int Level;
        public WeaponDamageMessage WeaponDamageType;

        public WeaponTypes WeaponType;
        public Dice DamageDice = new Dice(0, 0, 0);
        //public int DamageDiceCount;
        //public int DamageDiceSides;
        //public int DamageDiceBonus;

        public int Silver;
        public int Gold;
        public int Nutrition;

        public string Liquid;

        public List<WearFlags> wearFlags = new List<WearFlags>();
        public List<ItemTypes> itemTypes = new List<ItemTypes>();
        public List<ExtraFlags> extraFlags = new List<ExtraFlags>();
        public List<AffectData> affects = new List<AffectData>();
        public List<ItemSpellData> spells = new List<ItemSpellData>();

        public List<int> Keys = new List<int>();

        public float Weight;
        public float MaxWeight;
        public string Material;
        public int Charges;
        public int MaxCharges;

        public int ArmorBash = 0;
        public int ArmorPierce = 0;
        public int ArmorSlash = 0;
        public int ArmorExotic = 0;
        public int Value = 0;
        public int MaxDurability = 100;
        
        public List<Programs.Program<ItemData>> Programs = new List<Programs.Program<ItemData>>();

        public List<ExtraDescription> ExtraDescriptions = new List<ExtraDescription>();

        public ItemTemplateData()
        {

        }

        public ItemTemplateData(AreaData area, XElement element)
        {
            Vnum = element.GetElementValueInt("vnum");
            this.Area = area;
            try
            {
                area.ItemTemplates.Add(Vnum, this);
            }
            catch
            {
                game.log("Bad Area.ItemTemplates.Add - vnum " + Vnum + " in area " + area != null ? area.name : "null");
            }
            Level = element.GetElementValueInt("level");

            Weight = element.GetElementValueFloat("weight");
            
            Name = element.GetElementValue("name", "").Trim();
            ShortDescription = element.GetElementValue("ShortDescription", "").Trim();
            LongDescription = element.GetElementValue("LongDescription", "").Trim();
            Description = element.GetElementValue("Description", "").TrimStart().Replace("\r\n", "\n").Replace("\n\r", "\n");
            NightShortDescription = element.GetElementValue("NightShortDescription", "").Trim();
            NightLongDescription = element.GetElementValue("NightLongDescription", "").TrimStart();

            wearFlags.AddRange(Utility.LoadFlagList<WearFlags>(element.GetElementValue("WearFlags")));
            extraFlags.AddRange(Utility.LoadFlagList<ExtraFlags>(element.GetElementValue("ExtraFlags")));
            itemTypes.AddRange(Utility.LoadFlagList<ItemTypes>(element.GetElementValue("ItemTypes")));
            itemTypes.AddRange(Utility.LoadFlagList<ItemTypes>(element.GetElementValue("type")));

            if(element.HasElement("Keys"))
            {
                Keys.Clear();
                Keys.AddRange(from keyelement in element.GetElement("Keys").Elements() select keyelement.GetAttributeValueInt("vnum"));
            }

            if((extraFlags.ISSET(ExtraFlags.Locked) && Keys.Count == 0) || (extraFlags.ISSET(ExtraFlags.Locked) && !extraFlags.ISSET(ExtraFlags.Closed)))
            {
                extraFlags.REMOVEFLAG(ExtraFlags.Locked);
            }

            if (element.HasElement("Affects"))
            {
                var affectsElement = element.GetElement("Affects");
                foreach (var affectElement in affectsElement.Elements("Affect"))
                {
                    AffectData affect;
                    affects.Add((affect = new AffectData(affectElement)));
                    if (affect.duration == 0)
                        affect.duration = -1;
                    
                    //foreach (var affect in affects)
                    //    if (affect.flags.Any(f => f >= AffectFlags.Holy && f <= AffectFlags.Summon))
                    //    {
                    //        var flaglist = string.Join(" ", from f in affect.flags where f >= AffectFlags.Holy && f <= AffectFlags.Summon select f.ToString());
                    //        affect.DamageTypes.AddRange(utility.LoadFlagList<WeaponDamageTypes>(flaglist));
                    //        affect.flags.RemoveAll(f => f >= AffectFlags.Holy && f <= AffectFlags.Summon);
                    //        Area.saved = false;
                    //    }
                }
            }

            if (element.HasElement("Spells"))
            {
                var spellsElement = element.GetElement("Spells");
                foreach (var spellElement in spellsElement.Elements("Spell"))
                {
                    spells.Add(new ItemSpellData(spellElement));
                }
            }

            //utility.GetEnumValue<WeaponDamageTypes>(element.GetElementValue("weapondamagetype"), ref weapondamagetype);
            WeaponDamageType = WeaponDamageMessage.GetWeaponDamageMessage(element.GetElementValue("weapondamagetype"));
            Utility.GetEnumValue<WeaponTypes>(element.GetElementValue("WeaponType"), ref WeaponType);

            if (WeaponType == WeaponTypes.Spear || WeaponType == WeaponTypes.Staff || WeaponType == WeaponTypes.Polearm)
            {
                extraFlags.SETBIT(ExtraFlags.TwoHands);
            }

            DamageDice = new Dice(element.GetElementValueInt("DiceSides", 1), element.GetElementValueInt("DiceCount", 1), element.GetElementValueInt("DiceBonus", 1));

            Value = element.GetElementValueInt("Cost", 0);
            Silver = element.GetElementValueInt("silver", 0);
            Gold = element.GetElementValueInt("gold", 0);
            Nutrition = element.GetElementValueInt("nutrition", 0);
            MaxWeight = element.GetElementValueInt("maxweight", 0);

            MaxCharges = element.GetElementValueInt("maxcharges", 0);
            Charges = element.GetElementValueInt("charges", MaxCharges);
            MaxDurability = element.GetElementValueInt("MaxDurability", 100);
            ArmorBash = element.GetElementValueInt("ArmorBash", 0);
            ArmorSlash = element.GetElementValueInt("ArmorSlash", 0);
            ArmorPierce = element.GetElementValueInt("ArmorPierce", 0);
            ArmorExotic = element.GetElementValueInt("ArmorExotic", 0);

            Liquid = element.GetElementValue("liquid");
            Material = element.GetElementValue("material");

            if (element.HasElement("Programs"))
            {
                var programsElement = element.GetElement("Programs");
                foreach (var programElement in programsElement.Elements())
                {
                    var program = CrimsonStainedLands.Programs.ItemProgramLookup(programElement.GetAttributeValue("Name"));
                    if (program != null) { Programs.Add(program); }
                }
            }

            if (element.HasElement("ExtraDescriptions"))
            {
                foreach (var EDElement in element.Element("ExtraDescriptions").Elements())
                {
                    ExtraDescriptions.Add(new ExtraDescription(EDElement.GetElementValue("keyword"), EDElement.GetElementValue("description")));
                }
            }

            try
            {
                if (!ItemTemplateData.Templates.ContainsKey(Vnum))
                    ItemTemplateData.Templates.Add(Vnum, this);
                else
                    game.log("Bad ItemTemplateData.Templates.Add - duplicate vnum " + Vnum + " in area " + (area != null ? area.name : "null"));
            }
            catch
            {
                game.log("Bad ItemTemplateData.Templates.Add - vnum " + Vnum + " in area " + (area != null ? area.name : "null"));
            }

            //if (!area.name.ToLower().Contains("astoria"))
            //{
            //    Weight = Weight / 10;
            //    MaxWeight = MaxWeight / 10;
            //}
        } // end constructor (area, xelement)

        public XElement Element
        {
            get
            {
                return new XElement("Item",
                            new XElement("VNum", Vnum),
                            new XElement("Name", Name.TOSTRINGTRIM()),
                            new XElement("ShortDescription", ShortDescription.TOSTRINGTRIM()),
                            new XElement("LongDescription", LongDescription.TOSTRINGTRIM()),
                            new XElement("Description", Description.TOSTRINGTRIM()),
                            !NightLongDescription.ISEMPTY()? new XElement("NightLongDescription", NightLongDescription.TOSTRINGTRIM()) : null,
                            !NightShortDescription.ISEMPTY() ? new XElement("NightShortDescription", NightShortDescription.TOSTRINGTRIM()) : null,
                            new XElement("Level", Level),
                            (WeaponDamageType != null && !WeaponDamageType.Keyword.ISEMPTY()) ?
                            new XElement("WeaponDamageType", WeaponDamageType.Keyword) : null,
                                 (Vnum == 0 ?
                        new XComment("Valid weapon damage types are " + (string.Join(" ", from wdt in WeaponDamageMessage.WeaponDamageMessages select wdt.Keyword))) : null),
                            new XElement("WeaponType", WeaponType.ToString()),
                             (Vnum == 0 ?
                        new XComment("Valid weapon types are " + (string.Join(" ", from wtype in Utility.GetEnumValues<WeaponTypes>() select wtype))) : null),
                            new XElement("DiceSides", DamageDice.DiceSides),
                            new XElement("DiceCount", DamageDice.DiceCount),
                            new XElement("DiceBonus", DamageDice.DiceBonus),
                            new XElement("Weight", Weight),
                            new XElement("WearFlags", string.Join(" ", from flag in wearFlags select flag.ToString())),
                            new XElement("ExtraFlags", string.Join(" ", from flag in extraFlags select flag.ToString())),
                            new XElement("ItemTypes", string.Join(" ", from itemtype in itemTypes select itemtype.ToString())),
                            Keys.Count > 0?
                                new XElement("Keys", from key in Keys select new XElement("Key", new XAttribute("vnum", key))) : null,
                            new XElement("Affects", from aff in affects select aff.Element),
                            (spells.Any()? 
                            new XElement("Spells", from spell in  spells select spell.Eelement) : null),
                            new XElement("Silver", Silver),
                            new XElement("Gold", Gold),
                            new XElement("Cost", Value),
                            new XElement("Nutrition", Nutrition),
                            new XElement("Liquid", Liquid),
                            new XElement("MaxWeight", MaxWeight),
                            new XElement("Material", Material),
                            new XElement("Charges", Charges),
                            new XElement("MaxCharges", MaxCharges),
                            new XElement("MaxDurability", MaxDurability),
                            new XElement("ArmorBash", ArmorBash),
                            new XElement("ArmorSlash", ArmorSlash),
                            new XElement("ArmorPierce", ArmorPierce),
                            new XElement("ArmorExotic", ArmorExotic),
                            Programs.Any() ? new XElement("Programs", from program in Programs select new XElement("Program", new XAttribute("Name", program.Name))) : null,
                new XElement("ExtraDescriptions",
                                from ED in ExtraDescriptions
                                select new XElement("ExtraDescription",
                                    new XElement("Keyword", ED.Keywords),
                                    new XElement("Description", ED.Description)
                                    )
                                )
                );
            }
        } // end Element
    } // End ItemTemplateData

}