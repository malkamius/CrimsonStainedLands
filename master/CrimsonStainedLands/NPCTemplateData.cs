using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{

    public enum OffenseFlags
    {
        area_attack,
        backstab,
        bash,
        berserk,
        disarm,
        dodge,
        fade,
        fast,
        kick,
        dirt_kick,
        parry,
        rescue,
        tail,
        trip,
        crush,
        assist_all,
        assist_align,
        assist_race,
        assist_players,
        assist_guard,
        assist_vnum,
        parting_blow,
    };

    public class NPCTemplateData : Character
    {
        public static ConcurrentDictionary<int, NPCTemplateData> Templates = new ConcurrentDictionary<int, NPCTemplateData>();

        public AreaData Area;
        public int Vnum;
        public Dice HitPointDice;
        public Dice ManaPointDice;



        public int BuyProfitPercent = 0;
        public int SellProfitPercent = 0;
        public List<ItemTypes> BuyTypes = new List<ItemTypes>();
        public int ShopOpenHour = 0;
        public int ShopCloseHour = 0;
        
        public List<int> Protects = new List<int>();
        public List<int> PetVNums = new List<int>();

        public int Count;
        public int MaxCount;
        public List<Programs.Program<NPCData>> Programs = new List<Programs.Program<NPCData>>();
        public List<NLuaPrograms.NLuaProgram> LuaPrograms = new List<NLuaPrograms.NLuaProgram>();

        public NPCTemplateData()
        {

        }
        public NPCTemplateData(AreaData area, XElement element)
        {
            this.Area = area;

            Vnum = element.GetElementValueInt("vnum");
            try
            {
                area.NPCTemplates.Add(Vnum, this);
            }
            catch
            {
                Game.log("Bad Area.NPCTemplates.Add - vnum " + Vnum + " in area " + area != null ? area.Name : "null");
            }

            if (element.HasElement("Flags"))
                Utility.GetEnumValues<ActFlags>(element.GetElementValue("flags"), ref this.Flags);

            Utility.GetEnumValues<WeaponDamageTypes>(element.GetElementValue("Immune"), ref this.ImmuneFlags);
            Utility.GetEnumValues<WeaponDamageTypes>(element.GetElementValue("Resist"), ref this.ResistFlags);
            Utility.GetEnumValues<WeaponDamageTypes>(element.GetElementValue("Vulnerable"), ref this.VulnerableFlags);

            Utility.GetEnumValues<ActFlags>(element.GetElementValue("OffenseFlags"), ref this.Flags, false);
            Utility.GetEnumValues<AffectFlags>(element.GetElementValue("AffectedBy"), ref this.AffectedBy);
            Name = element.GetElementValue("name");
            ShortDescription = element.GetElementValue("shortDescription").Trim();
            LongDescription = element.GetElementValue("longDescription").Trim();
            NightShortDescription = element.GetElementValue("NightShortDescription").Trim();
            NightLongDescription = element.GetElementValue("NightLongDescription").Trim();
            Description = element.GetElementValue("description").Trim();
            Level = element.GetElementValueInt("level", 1);
            HitPoints = element.GetElementValueInt("hitpoints", Level * 20);
            MaxHitPoints = element.GetElementValueInt("maxHitpoints", Level * 20);
            Utility.GetEnumValue(element.GetElementValue("DefaultPosition", "standing"), ref DefaultPosition);

            HitPointDice = new Dice(element.GetElementValueInt("HitPointDiceSides", 0), element.GetElementValueInt("HitPointDiceCount", 0), element.GetElementValueInt("HitPointDiceBonus", 0));
            ManaPointDice = new Dice(element.GetElementValueInt("ManaPointDiceSides", 0), element.GetElementValueInt("ManaPointDiceCount", 0), element.GetElementValueInt("ManaPointDiceBonus", 0));
            DamageDice = new Dice(element.GetElementValueInt("DamageDiceSides", 0), element.GetElementValueInt("DamageDiceCount", 0), element.GetElementValueInt("DamageDiceBonus", 0));
            
            ArmorBash = element.GetElementValueInt("ArmorBash");
            ArmorSlash = element.GetElementValueInt("ArmorSlash");
            ArmorPierce = element.GetElementValueInt("ArmorPierce");
            ArmorExotic = element.GetElementValueInt("ArmorExotic");
            SavingThrow = element.GetElementValueInt("SavingThrow");
            Guild = GuildData.GuildLookup(element.GetElementValue("guild"));
            Gold = element.GetElementValueInt("Gold");
            Silver = element.GetElementValueInt("Silver");
            Sexes sex = Sexes.Either;
            Utility.GetEnumValue<Sexes>(element.GetElementValue("Sex", "none"), ref sex);
            Sex = sex;
            Utility.GetEnumValue<Alignment>(element.GetElementValue("Alignment", "neutral"), ref Alignment);
            Utility.GetEnumValue<CharacterSize>(element.GetElementValue("Size", "Medium"), ref Size);
            //utility.GetEnumValues<AffectFlags>(npc.GetElementValue("affectedBy"), ref this.AffectedBy);

            if (element.HasElement("Shop"))
            {
                Flags.ADDFLAG(ActFlags.Shopkeeper);
                var shopdata = element.GetElement("Shop");
                BuyProfitPercent = shopdata.GetAttributeValueInt("ProfitBuy");
                SellProfitPercent = shopdata.GetAttributeValueInt("ProfitSell");
                ShopOpenHour = shopdata.GetAttributeValueInt("OpenHour");
                ShopCloseHour = shopdata.GetAttributeValueInt("CloseHour");
                BuyTypes.AddRange(Utility.LoadFlagList<ItemTypes>(shopdata.GetAttributeValue("BuyTypes")));
                if (shopdata.HasElement("Pet"))
                {
                    foreach (var pet in shopdata.Elements("Pet"))
                    {
                        PetVNums.Add(pet.GetAttributeValueInt("Vnum"));
                    }
                }
            }

            if (element.HasElement("Protects"))
            {
                foreach (var room in element.GetElement("Protects").Elements("Room"))
                {
                    Protects.Add(room.GetAttributeValueInt("Vnum"));
                }
            }

            if (element.HasElement("Learned"))
            {
                foreach (var skillElement in element.Element("Learned").Elements("SkillSpell"))
                {
                    SkillSpell skill;

                    if ((skill = SkillSpell.SkillLookup(skillElement.GetAttributeValue("Name"))) != null)
                    {
                        Learned[skill] = new LearnedSkillSpell() { Percentage = skillElement.GetAttributeValueInt("Value"), Level = 1, Skill = skill, SkillName = skill.name };
                    }
                    else
                        Game.bug("Skill for npc {0} not found: {1}", Vnum, skillElement.GetAttributeValue("Name"));
                }
            }
            Position = Positions.Standing;
            if (element.HasElement("race"))
                Race = Race.GetRace(element.GetElementValue("race", "human")) ?? Race.GetRace("human");
            else
                Race = Race.GetRace("human");
            if(Race != null)
            PermanentStats = new PhysicalStats(Race.Stats);
            if (PermanentStats != null && PermanentStats.Dexterity == 20)
                PermanentStats = new PhysicalStats(16, 16, 16, 16, 16, 16);
            WeaponDamageMessage = WeaponDamageMessage.GetWeaponDamageMessage(element.GetElementValue("WeaponDamageMessage", Race != null && Race.parts.ISSET(PartFlags.Claws) ? "claw" : "punch"));
            
            HitRoll = element.GetElementValueInt("Hitroll", Level * 3);
            DamageRoll = element.GetElementValueInt("damroll", Math.Max(Level, DamageDice.DiceBonus));

            Utility.GetEnumValue<CharacterSize>(element.GetElementValue("Size", "Medium"), ref Size, CharacterSize.Medium);

            if (element.HasElement("Armor"))
                ArmorClass = element.GetElementValueInt("Armor", Level * 20);

            if (element.HasElement("ArmorClass"))
                ArmorClass = element.GetElementValueInt("ArmorClass", 30);

            if (element.HasElement("Guild"))
            {
                Guild = GuildData.GuildLookup(element.GetElementValue("Guild"));
            }

            if (Race != null && Race.affects != null)
            {
                AffectedBy.AddRange(Race.affects);
            }

            if (Guild == null)
            {
                if (Flags.ISSET(ActFlags.Cleric) || Flags.ISSET(ActFlags.Healer))
                {
                    Guild = GuildData.GuildLookup("healer");
                    foreach (var skill in SkillSpell.Skills.Values)
                    {
                        var learnedat = GetLevelSkillLearnedAt(skill);
                        if (learnedat < 60 && Level > GetLevelSkillLearnedAt(skill))
                        {
                            LearnSkill(skill, 80, 1);
                        }
                    }
                }

                else if (Flags.ISSET(ActFlags.Mage))
                {
                    Guild = GuildData.GuildLookup("mage");
                    foreach (var skill in SkillSpell.Skills.Values)
                    {
                        var learnedat = GetLevelSkillLearnedAt(skill);
                        if (learnedat < 60 && Level > GetLevelSkillLearnedAt(skill))
                        {
                            LearnSkill(skill, 80, 1);
                        }
                    }
                }

                else if (Flags.ISSET(ActFlags.Thief))
                {
                    Guild = GuildData.GuildLookup("thief");
                    foreach (var skill in SkillSpell.Skills.Values)
                    {
                        var learnedat = GetLevelSkillLearnedAt(skill);
                        if (learnedat < 60 && Level > GetLevelSkillLearnedAt(skill))
                        {
                            LearnSkill(skill, 80, 1);
                        }
                    }
                }

                else if (Flags.ISSET(ActFlags.Warrior))
                {
                    Guild = GuildData.GuildLookup("warrior");
                    foreach (var skill in SkillSpell.Skills.Values)
                    {
                        var learnedat = GetLevelSkillLearnedAt(skill);
                        if (learnedat < 60 && Level > GetLevelSkillLearnedAt(skill))
                        {
                            LearnSkill(skill, 80, 1);
                        }
                    }
                }
            }
            if (Guild != null)
                foreach (var skill in SkillSpell.Skills.Values)
                {
                    var learnedat = GetLevelSkillLearnedAt(skill);
                    if (learnedat < 60 && Level > GetLevelSkillLearnedAt(skill))
                    {
                        LearnSkill(skill, 80, 1);
                    }
                }
            if ((Race != null && Race.act.ISSET(ActFlags.Dodge)) || Flags.ISSET(ActFlags.Dodge))
                LearnSkill("dodge", 80, 1);
            if ((Race != null && Race.act.ISSET(ActFlags.DirtKick)) || Flags.ISSET(ActFlags.DirtKick))
                LearnSkill("dirt kick", 80, 1); 
            if ((Race != null && Race.act.ISSET(ActFlags.Bash)) || Flags.ISSET(ActFlags.Bash))
                LearnSkill("bash", 80, 1);
            if ((Race != null && Race.act.ISSET(ActFlags.Berserk)) || Flags.ISSET(ActFlags.Berserk))
                LearnSkill("berserk", 80, 1);
            if ((Race != null && Race.act.ISSET(ActFlags.Disarm)) || Flags.ISSET(ActFlags.Disarm))
                LearnSkill("disarm", 80, 1);
            if ((Race != null && Race.act.ISSET(ActFlags.Parry)) || Flags.ISSET(ActFlags.Parry))
                LearnSkill("parry", 80, 1);
            if ((Race != null && Race.act.ISSET(ActFlags.Kick)) || Flags.ISSET(ActFlags.Kick))
                LearnSkill("kick", 80, 1);
            if ((Race != null && Race.act.ISSET(ActFlags.Trip)) || Flags.ISSET(ActFlags.Trip))
                LearnSkill("trip", 80, 1); 
            if ((Race != null && Race.act.ISSET(ActFlags.Fast)) || Flags.ISSET(ActFlags.Fast))
                LearnSkill("second attack", 80, 1);

            if (element.HasElement("Programs"))
            {
                var programsElement = element.GetElement("Programs");
                foreach (var programElement in programsElement.Elements())
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

            try
            {
                NPCTemplateData.Templates.TryAdd(Vnum, this);
            }
            catch
            {
                Game.log("Bad NPCTemplateData.Templates.Add - vnum " + Vnum + " in area " + area != null ? area.Name : "null");
            }

        }

        public XElement NPCTemplateElement
        {
            get
            {
                var element = this.Element;
                element.Name = "NPC";
                element.AddFirst(new XElement("Vnum", Vnum));

                if (element.HasElement("level"))
                {
                    element.GetElement("level").AddAfterSelf(
                        new XElement("HitPointDiceSides", HitPointDice.DiceSides),
                        new XElement("HitPointDiceCount", HitPointDice.DiceCount),
                        new XElement("HitPointDiceBonus", HitPointDice.DiceBonus),

                        new XElement("ManaPointDiceSides", ManaPointDice.DiceSides),
                        new XElement("ManaPointDiceCount", ManaPointDice.DiceCount),
                        new XElement("ManaPointDiceBonus", ManaPointDice.DiceBonus));
                }
                else
                    element.Add(
                        new XElement("HitPointDiceSides", HitPointDice.DiceSides),
                        new XElement("HitPointDiceCount", HitPointDice.DiceCount),
                        new XElement("HitPointDiceBonus", HitPointDice.DiceBonus),
    
                        new XElement("ManaPointDiceSides", ManaPointDice.DiceSides),
                        new XElement("ManaPointDiceCount", ManaPointDice.DiceCount),
                        new XElement("ManaPointDiceBonus", ManaPointDice.DiceBonus));

                //element.Add(new XElement("DamageDiceSides", DamageDice.DiceSides));
                //element.Add(new XElement("DamageDiceCount", DamageDice.DiceCount));
                //element.Add(new XElement("DamageDiceBonus", DamageDice.DiceBonus));

                element.Add((Programs.Any() || LuaPrograms.Any() ?
                        new XElement("Programs",
                            (from program in Programs select new XElement("Program", new XAttribute("Name", program.Name))).
                            Concat(from luaprogram in LuaPrograms select new XElement("Program", new XAttribute("Name", luaprogram.Name)))) : null));

                if (Protects.Count > 0)
                {
                    element.Add(new XElement("Protects", from vnum in Protects select new XElement("Room", new XAttribute("Vnum", vnum))));
                }

                if (Flags.ISSET(ActFlags.Shopkeeper))
                {
                    element.Add(new XElement("Shop",
                        new XAttribute("ProfitBuy", BuyProfitPercent),
                        new XAttribute("ProfitSell", SellProfitPercent),
                        new XAttribute("OpenHour", ShopOpenHour),
                        new XAttribute("CloseHour", ShopCloseHour),
                        new XAttribute("BuyTypes", string.Join(" ", BuyTypes))),
                        from pet in PetVNums select new XElement("Pet", new XAttribute("Vnum", pet)));
                }
                //element.Add(new XElement("Skills",
                //    from skill in learned select new XElement("Skill", new XAttribute("name", skill.Key.internalName), new XAttribute("value", skill.Value))));

                return element;
            }
        }
    }
}
