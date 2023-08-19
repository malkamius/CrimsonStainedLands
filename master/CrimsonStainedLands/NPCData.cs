using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{

public class NPCData : Character, IDisposable
    {
        public static List<NPCData> NPCs = new List<NPCData>();

        public int vnum;
        public NPCTemplateData template;
        public int BuyProfitPercent = 0;
        public int SellProfitPercent = 0;
        public List<ItemTypes> BuyTypes = new List<ItemTypes>();
        public int ShopOpenHour = 0;
        public int ShopCloseHour = 0;
        public List<int> Protects = new List<int>();
        public List<int> PetVNums = new List<int>();
        public List<Programs.Program<NPCData>> Programs = new List<Programs.Program<NPCData>>();
        public List<NLuaPrograms.NLuaProgram> LuaPrograms = new List<NLuaPrograms.NLuaProgram>();

        public NPCData(NPCTemplateData template, RoomData room)
        {
            this.template = template;
            vnum = template.Vnum;
            Name = template.Name;
            ShortDescription = template.ShortDescription;
            LongDescription = template.LongDescription;
            NightShortDescription = template.NightShortDescription;
            NightLongDescription = template.NightLongDescription;
            Description = template.Description;
            Alignment = template.Alignment;
            Ethos = template.Ethos;
            Position = template.Position;
            Race = template.Race;
            HitPoints = template.HitPoints;
            MaxHitPoints = template.MaxHitPoints;

            Size = template.Size;
            Flags.AddRange(template.Flags);
            ImmuneFlags.AddRange(template.ImmuneFlags);
            ResistFlags.AddRange(template.ResistFlags);
            VulnerableFlags.AddRange(template.VulnerableFlags);
            AffectedBy.AddRange(template.AffectedBy);

            HitRoll = template.HitRoll;
            DamageRoll = template.DamageRoll;
            ArmorClass = template.ArmorClass;
            Level = template.Level;

            ArmorBash = template.ArmorBash;
            ArmorSlash = template.ArmorSlash;
            ArmorPierce = template.ArmorPierce;
            ArmorExotic = template.ArmorExotic;

            PermanentStats = new PhysicalStats(template.PermanentStats);
            if (PermanentStats.Dexterity == 20)
                PermanentStats = new PhysicalStats(16, 16, 16, 16, 16, 16);
            DamageDice = new Dice(template.DamageDice);
            Guild = template.Guild;
            BuyProfitPercent = template.BuyProfitPercent;
            SellProfitPercent = template.SellProfitPercent;
            BuyTypes.AddRange(template.BuyTypes);
            ShopOpenHour = template.ShopOpenHour;
            ShopCloseHour = template.ShopCloseHour;
            Learned = new Dictionary<SkillSpell, LearnedSkillSpell>(template.Learned);
            Protects.AddRange(template.Protects);
            PetVNums = new List<int>(template.PetVNums);
            DefaultPosition = template.DefaultPosition;
            Programs.AddRange(template.Programs);
            LuaPrograms.AddRange(template.LuaPrograms);

            WeaponDamageMessage = template.WeaponDamageMessage;

            ManaPoints = 100;
            MaxManaPoints = 100;
            MovementPoints = 100;
            MaxMovementPoints = 100;
            if (template.HitPointDice.HasValue)
            {
                MaxHitPoints = template.HitPointDice.Roll();
                HitPoints = MaxHitPoints;
            }

            if (template.ManaPointDice.HasValue)
            {
                MaxManaPoints = template.ManaPointDice.Roll();
                ManaPoints = MaxManaPoints;
            }
            // Generate random gold for each 
            var wealth = Utility.Random(template.Level + (MaxHitPoints / 2), (int)(template.Level * 3 + (MaxHitPoints / 2)));

            if (Race == null || Race.HasCoins)
            {
                Silver = wealth % 1000;
                Gold = (wealth - Silver) / 1000;
            }

            if (template.Gold != 0 || template.Silver != 0)
            {
                Gold = template.Gold;
                Silver = template.Silver;
            }
            

            if (template != null) template.Count++;
            NPCs.Add(this);
            AddCharacterToRoom(room);
        }

        public NPCData(XElement element, RoomData room)
        {
            if (room == null) return;
            if (element.HasElement("Flags"))
                Utility.GetEnumValues<ActFlags>(element.GetElementValue("flags"), ref this.Flags);

            Utility.GetEnumValues<WeaponDamageTypes>(element.GetElementValue("Immune"), ref this.ImmuneFlags);
            Utility.GetEnumValues<WeaponDamageTypes>(element.GetElementValue("Resist"), ref this.ResistFlags);
            Utility.GetEnumValues<WeaponDamageTypes>(element.GetElementValue("Vulnerable"), ref this.VulnerableFlags);


            Utility.GetEnumValues<ActFlags>(element.GetElementValue("OffenseFlags"), ref this.Flags, false);
            Utility.GetEnumValues<AffectFlags>(element.GetElementValue("AffectedBy"), ref this.AffectedBy);
            vnum = element.GetElementValueInt("vnum");
            NPCTemplateData template;
            if (vnum != 0 && NPCTemplateData.Templates.TryGetValue(vnum, out template))
            {
                this.template = template;
            }
            Name = element.GetElementValue("name");
            ShortDescription = element.GetElementValue("shortDescription").Trim();
            LongDescription = element.GetElementValue("longDescription").Trim();
            NightShortDescription = element.GetElementValue("NightShortDescription").Trim();
            NightLongDescription = element.GetElementValue("NightLongDescription").Trim();
            Description = element.GetElementValue("description").Trim().Replace("\r\n", "\n");
            Level = element.GetElementValueInt("level", 1);
            HitPoints = element.GetElementValueInt("hitpoints", Level * 20);
            MaxHitPoints = element.GetElementValueInt("maxHitpoints", Level * 20);

            MovementPoints = element.GetElementValueInt("MovementPoints", Level * 20);
            MaxMovementPoints = element.GetElementValueInt("MaxMovementPoints", Level * 20);
            ManaPoints = element.GetElementValueInt("MovementPoints", Level * 20);
            MaxManaPoints = element.GetElementValueInt("MaxMovementPoints", Level * 20);


            ArmorBash = element.GetElementValueInt("ArmorBash");
            ArmorSlash = element.GetElementValueInt("ArmorSlash");
            ArmorPierce = element.GetElementValueInt("ArmorPierce");
            ArmorExotic = element.GetElementValueInt("ArmorExotic");
            SavingThrow = element.GetElementValueInt("SavingThrow");
            Guild = GuildData.GuildLookup(element.GetElementValue("guild"));
            Gold = element.GetElementValueInt("Gold");
            var sex = Sexes.Either;
            Utility.GetEnumValue<Sexes>(element.GetElementValue("Sex", "none"), ref sex);
            Sex = sex;
            Utility.GetEnumValue<Alignment>(element.GetElementValue("Alignment", "neutral"), ref Alignment);
            Utility.GetEnumValue<CharacterSize>(element.GetElementValue("Size", "Medium"), ref Size);
            Utility.GetEnumValues<AffectFlags>(element.GetElementValue("affectedBy"), ref this.AffectedBy);
            Utility.GetEnumValue(element.GetElementValue("DefaultPosition", "standing"), ref DefaultPosition);

            
            Position = Positions.Standing;
            if (element.HasElement("race"))
                Race = Race.GetRace(element.GetElementValue("race", "human")) ?? Race.GetRace("human");
            else
                Race = Race.GetRace("human");

            WeaponDamageMessage = WeaponDamageMessage.GetWeaponDamageMessage(element.GetElementValue("WeaponDamageMessage", Race != null && Race.parts.ISSET(PartFlags.Claws)? "claw" : "punch"));

            HitRoll = element.GetElementValueInt("Hitroll", Level * 3);
            DamageRoll = element.GetElementValueInt("damroll", Level * 2);
            DamageDice = new Dice(element.GetElementValueInt("DamageDiceSides", 0), element.GetElementValueInt("DamageDiceCount", 0), element.GetElementValueInt("DamageDiceBonus", 0));

            Utility.GetEnumValue<CharacterSize>(element.GetElementValue("Size", "Medium"), ref Size, CharacterSize.Medium);

            if (element.HasElement("Armor"))
                ArmorClass = element.GetElementValueInt("Armor", Level * 20);

            if (element.HasElement("ArmorClass"))
                ArmorClass = element.GetElementValueInt("ArmorClass", 30);

            if (element.HasElement("Guild"))
            {
                Guild = GuildData.GuildLookup(element.GetElementValue("Guild"));
            }

            if (element.HasElement("Affects"))
            {
                var affects = element.GetElement("Affects");

                foreach (var affElement in affects.Elements())
                {
                    var newAff = new AffectData(affElement);

                    //if (newAff.flags.Any(f => f >= AffectFlags.Holy && f <= AffectFlags.Summon))
                    //{
                    //    var flaglist = string.Join(" ", from f in newAff.flags where f >= AffectFlags.Holy && f <= AffectFlags.Summon select f.ToString());
                    //    newAff.DamageTypes.AddRange(utility.LoadFlagList<WeaponDamageTypes>(flaglist));
                    //    newAff.flags.RemoveAll(f => f >= AffectFlags.Holy && f <= AffectFlags.Summon);
                        
                    //}
                    this.AffectsList.Insert(0, newAff);
                }
            }

            if (element.HasElement("Learned"))
            {
                foreach (var skillElement in element.Element("Learned").Elements("SkillSpell"))
                {
                    SkillSpell skill;

                    if ((skill = SkillSpell.SkillLookup(skillElement.GetAttributeValue("Name"))) != null)
                    {
                        LearnSkill(skill, skillElement.GetAttributeValueInt("Value"));
                    }
                    else
                        Game.bug("Skill for npc not found: {1}", skillElement.GetAttributeValue("Name"));
                }
            }

            if(element.HasElement("Programs"))
            {
                var programsElement = element.GetElement("Programs");
                foreach(var programElement in programsElement.Elements())
                {
                    
                    if (CrimsonStainedLands.Programs.NPCProgramLookup(programElement.GetAttributeValue("Name"), out var program)) 
                    { 
                        Programs.Add(program); 
                    }
                    else if (CrimsonStainedLands.NLuaPrograms.ProgramLookup(programElement.GetAttributeValue("Name"), out var luaprogram))
                    {
                        LuaPrograms.Add(luaprogram);
                    }
                    else
                        Game.log("Program not found: {0}", programElement.GetAttributeValue("Name"));
                }
            }

            if (element.HasElement("Inventory"))
            {
                var inventoryElement = element.GetElement("Inventory");
                foreach (var itemElement in inventoryElement.Elements())
                {
                    var item = new ItemData(itemElement);
                    Inventory.Add(item);
                    item.CarriedBy = this;
                }

            }

            if (element.HasElement("Equipment"))
            {
                var eqElement = element.GetElement("Equipment");
                foreach (var slotElement in eqElement.Elements())
                {
                    if (slotElement.HasAttribute("slotid") && slotElement.HasElement("Item"))
                    {
                        WearSlotIDs id = WearSlotIDs.Chest;
                        if (Utility.GetEnumValue<WearSlotIDs>(slotElement.GetAttribute("slotID").Value, ref id))
                        {
                            Equipment[id] = new ItemData(slotElement.GetElement("Item"));
                            Equipment[id].CarriedBy = this;
                            // don't reapply affects saved to character stats
                        }
                    }

                }
            }

            if (this.template != null) this.template.Count++;

            NPCData.NPCs.Add(this);
            Character.Characters.Add(this);

            AddCharacterToRoom(room);
        }

        public new XElement Element
        {
            get
            {
                var element = base.Element;
                element.AddFirst(new XElement("Vnum", vnum));
                return element;
            }
        }
        public new void Dispose()
        {
            if (template != null) template.Count--;
            base.Dispose();
            RemoveCharacterFromRoom();
            if (NPCs.Contains(this))
                NPCs.Remove(this);
        }
    }
}
