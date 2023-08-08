using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public enum SkillSpellTypes
    {
        Skill = 1,
        Spell = 2,
        Commune,
        Power,
        Song,
        Form,
        InForm,
        WarriorSpecialization,
        None
    }
    public enum TargetTypes
    {
        targetIgnore = 0,
        targetCharOffensive = 1,
        targetCharDefensive,
        targetCharSelf,
        targetItemInventory,
        targetItemCharDef,
        targetItemCharOff
    }

    public enum TargetIsType
    {
        targetChar = 0,
        targetItem = 1,
        targetRoom = 2,
        targetNone = 3
    }

    public delegate void SpellFunc(Magic.CastType castType, SkillSpell spell, int level, Character ch, Character victim, ItemData item, string arguments, TargetIsType targetIsType);
    public delegate void TickFunc(Character character, AffectData affect);

    public class SkillSpell
    {
        public static readonly bool UseRoslyn = false;

        public static Dictionary<string, SkillSpell> Skills = new Dictionary<string, SkillSpell>();
        public string name;
        public string internalName;

        public Dictionary<string, int> skillLevel = new Dictionary<string, int>();
        public Dictionary<string, int> rating;
        public Dictionary<string, string> guildPreRequisiteSkill = new Dictionary<string, string>();
        public Dictionary<string, int> guildPreRequisiteSkillPercentage = new Dictionary<string, int>();
        public SpellFunc spellFun;
        public TickFunc TickFunction;
        public TickFunc EndFunction;
        public TargetTypes targetType;
        public Positions minimumPosition = Positions.Standing;
        public int minimumMana;
        public int waitTime;
        public string NounDamage = "";
        public string MessageOn = "";
        public string MessageOnToRoom = "";
        public string MessageOff = "";
        public string MessageOffToRoom = "";
        public string MessageItem = "";
        public string MessageItemToRoom = "";

        public string AutoCastScript = "";

        public string Lyrics = "";

        public bool BoolAutoCast = true;
        public Func<Character, bool> AutoCast;
        public List<SkillSpellTypes> SkillTypes = new List<SkillSpellTypes>();
        public string Prerequisites;
        public int PrerequisitePercentage;

        public bool PrerequisitesMet(Character ch)
        {
            if (ch.IsImmortal || ch.IsNPC) return true;

            string prereqs = Prerequisites.TOSTRINGTRIM();
            string prereq = "";
            while (prereqs.Length > 0)
            {
                prereqs = prereqs.OneArgument(ref prereq);
                LearnedSkillSpell learned;
                var skill = SkillSpell.SkillLookup(prereq);
                if (skill != null && ch.Learned.TryGetValue(skill, out learned) && learned.Percentage >= PrerequisitePercentage)
                    continue;
                else
                {
                    return false;
                }
            }

            if (ch.Guild != null && guildPreRequisiteSkill.ContainsKey(ch.Guild.name) && guildPreRequisiteSkillPercentage.ContainsKey(ch.Guild.name) && !guildPreRequisiteSkill[ch.Guild.name].ISEMPTY())
            {
                prereqs = guildPreRequisiteSkill[ch.Guild.name];

                if (prereqs.Contains("|"))
                {
                    var prereqlist = prereqs.Split('|');
                    int failed = 0;
                    foreach (var list in prereqlist)
                    {
                        prereqs = list;
                        bool failthis = false;
                        while (prereqs.Length > 0)
                        {
                            prereqs = prereqs.OneArgument(ref prereq);
                            LearnedSkillSpell learned;
                            var skill = SkillSpell.SkillLookup(prereq);
                            if (skill != null && ch.Learned.TryGetValue(skill, out learned) && learned.Percentage >= guildPreRequisiteSkillPercentage[ch.Guild.name])
                                continue;
                            else
                            {
                                failthis = true;
                                break;
                            }
                        }
                        if (failthis) failed++;

                    }
                    if (failed == prereqlist.Count()) return false;
                }
                else
                {
                    while (prereqs.Length > 0)
                    {
                        prereqs = prereqs.OneArgument(ref prereq);
                        LearnedSkillSpell learned;
                        var skill = SkillSpell.SkillLookup(prereq);
                        if (skill != null && ch.Learned.TryGetValue(skill, out learned) && learned.Percentage >= guildPreRequisiteSkillPercentage[ch.Guild.name])
                            continue;
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        //public SkillSpell(string name, string internalName,
        //    Action<Magic.CastType, SkillSpell, int, Character, Character, ItemData, string, TargetIsType> spellFun,
        //    TargetTypes targetType, Positions minimumPosition, int minimumMana, int waitTime, string nounDamage, string messageOff, string messageItem, IEnumerable<SkillSpellTypes> types)
        public SkillSpell(string name, string internalName, SpellFunc spellFun, int waitTime, Func<Character, bool> autocast)
        {
            this.name = name;
            this.internalName = internalName;
            this.skillLevel = new Dictionary<string, int>();
            this.rating = new Dictionary<string, int>();
            this.spellFun = spellFun;
            //this.targetType = targetType;
            //this.minimumPosition = minimumPosition;
            //this.minimumMana = minimumMana;
            this.waitTime = waitTime;
            //this.NounDamage = nounDamage;
            //this.MessageOff = messageOff;
            //this.MessageItem = messageItem;
            this.SkillTypes = new List<SkillSpellTypes>();
            this.AutoCast = autocast;
            Skills.Add(internalName, this);

        }

        public SkillSpell(string name, SpellFunc spellFun, int waitTime, Func<Character, bool> autocast)
        {
            this.name = name;
            this.internalName = name;
            this.skillLevel = new Dictionary<string, int>();
            this.rating = new Dictionary<string, int>();
            this.spellFun = spellFun;
            //this.targetType = targetType;
            //this.minimumPosition = minimumPosition;
            //this.minimumMana = minimumMana;
            this.waitTime = waitTime;
            //this.NounDamage = nounDamage;
            //this.MessageOff = messageOff;
            //this.MessageItem = messageItem;
            this.SkillTypes = new List<SkillSpellTypes>();
            this.AutoCast = autocast;
            Skills.Add(internalName, this);

        }

        public SkillSpell(string name, string internalName, SpellFunc spellFun, int waitTime, bool autocast = true)
        {
            this.name = name;
            this.internalName = internalName;
            this.skillLevel = new Dictionary<string, int>();
            this.rating = new Dictionary<string, int>();
            this.spellFun = spellFun;
            //this.targetType = targetType;
            //this.minimumPosition = minimumPosition;
            //this.minimumMana = minimumMana;
            this.waitTime = waitTime;
            //this.NounDamage = nounDamage;
            //this.MessageOff = messageOff;
            //this.MessageItem = messageItem;
            this.SkillTypes = new List<SkillSpellTypes>();
            //this.AutoCast = new Func<Character, bool>(c => autocast);
            this.BoolAutoCast = autocast;
            Skills.Add(internalName, this);

        }

        public SkillSpell(string name, SpellFunc spellFun, int waitTime, bool autocast = true)
        {
            this.name = name;
            this.internalName = name;
            this.skillLevel = new Dictionary<string, int>();
            this.rating = new Dictionary<string, int>();
            this.spellFun = spellFun;
            //this.targetType = targetType;
            //this.minimumPosition = minimumPosition;
            //this.minimumMana = minimumMana;
            this.waitTime = waitTime;
            //this.NounDamage = nounDamage;
            //this.MessageOff = messageOff;
            //this.MessageItem = messageItem;
            this.SkillTypes = new List<SkillSpellTypes>();
            //this.AutoCast = new Func<Character, bool>(c => autocast);
            this.BoolAutoCast = autocast;
            Skills.Add(internalName, this);

        }

        public int getSkillLevel(GuildData guild)
        {
            int level = 0;
            if (skillLevel.TryGetValue(guild.name, out level))
                return level;

            return 0;
        }

        static SkillSpell()
        {
            //SkillSpell skill;
            

            Songs.InitializeSongSkills();

            LoadSkills();

            LoadSongs();


            SetAutoCast("heal", c => c.HitPoints < c.MaxHitPoints);
            SetAutoCast("cure critical", c => c.HitPoints < c.MaxHitPoints);
            SetAutoCast("cure serious", c => c.HitPoints < c.MaxHitPoints);
            SetAutoCast("cure light", c => c.HitPoints < c.MaxHitPoints);
            SetAutoCast("remove curse", c => c.IsAffected(AffectFlags.Curse));

            SetAutoCast("sanctuary", c => c.IsAffected(AffectFlags.Sanctuary));
            SetAutoCast("protection evil", c => !c.IsAffected(AffectFlags.ProtectEvil));
            SetAutoCast("protection good", c => !c.IsAffected(AffectFlags.ProtectGood));
            SetAutoCast("cure blindness", c => c.IsAffected(AffectFlags.Blind));
            SetAutoCast("cure poison", c => c.IsAffected(AffectFlags.Poison));
            SetAutoCast("bind wounds", c => c.HitPoints < c.MaxHitPoints);
            SetAutoCast("herbs", c => c.HitPoints < c.MaxHitPoints);
            SetAutoCast("fly", c => !c.IsAffected(AffectFlags.Flying));
            SetAutoCast("curse", c => !c.IsAffected(AffectFlags.Curse));
            SetAutoCast("weaken", c => !c.IsAffected(AffectFlags.Weaken));
            SetAutoCast("frenzy", c => !c.IsAffected(AffectFlags.Berserk));
            SetAutoCast("berserk", c => !c.IsAffected(AffectFlags.Berserk));

            //SaveSkills();

            //SaveSongs();

        }

        public static void SetAutoCast(string name, Func<Character, bool> func)
        {
            SkillSpell spell;
            if ((spell = SkillLookup(name)) != null)
                spell.AutoCast = func;
            else
                game.log("Skill {0} not found to set autocast on.", name);
        }

        private static void LoadSkills()
        {
            SkillSpell skill;
            var element = XElement.Load("data\\skilllevels.xml");

            foreach (var skElement in element.Elements("SkillSpell"))
            {
                var skname = skElement.GetAttributeValue("Name");

                if (Skills.TryGetValue(skname, out skill))// (skill = SkillLookup(skname)) != null)
                {
                    Utility.GetEnumValue<TargetTypes>(skElement.GetAttributeValue("TargetType"), ref skill.targetType);
                    Utility.GetEnumValue<Positions>(skElement.GetAttributeValue("MinimumPosition", "Standing"), ref skill.minimumPosition);
                    Utility.GetEnumValues<SkillSpellTypes>(skElement.GetAttributeValue("SkillTypes"), ref skill.SkillTypes);
                    skill.minimumMana = skElement.GetAttributeValueInt("MinimumMana");
                    skill.NounDamage = skElement.GetAttributeValue("NounDamage");
                    skill.MessageOn = skElement.GetAttributeValue("MessageOn");
                    skill.MessageOnToRoom = skElement.GetAttributeValue("MessageOnToRoom");
                    skill.MessageOff = skElement.GetAttributeValue("MessageOff");
                    skill.MessageOffToRoom = skElement.GetAttributeValue("MessageOffToRoom");
                    skill.MessageItem = skElement.GetAttributeValue("MessageItem");
                    skill.MessageItemToRoom = skElement.GetAttributeValue("MessageItemToRoom");
                    skill.Prerequisites = skElement.GetAttributeValue("Prerequisites");
                    skill.PrerequisitePercentage = skElement.GetAttributeValueInt("PrerequisitePercentage");
                    skill.BoolAutoCast = skElement.GetAttributeValue("AutoCast", "true") == "true";
                    skill.AutoCastScript = skElement.GetAttributeValue("AutoCastScript");
                    
                    // Avoid using Roslyn for now, it adds startup time significantly.
                    
                    //if (!skill.AutoCastScript.ISEMPTY() && UseRoslyn) RoslynScripts.PrepareCharacterBoolScript(skill.AutoCastScript);

                    string SpellFuncType;
                    string SpellFuncName;
                    if(!(SpellFuncType = skElement.GetAttributeValue("SpellFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("SpellFuncName")).ISEMPTY())
                    {
                        var type = Type.GetType(SpellFuncType);

                        if(type != null)
                        {
                            var method = type.GetMethod(SpellFuncName);

                            if (method != null)
                            {
                                skill.spellFun = (SpellFunc)method.CreateDelegate(typeof(SpellFunc));
                            }
                            else
                                game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                        }
                        else
                            game.log("Type {0} not found", SpellFuncType);
                    }

                    if (!(SpellFuncType = skElement.GetAttributeValue("TickFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("TickFuncName")).ISEMPTY())
                    {
                        var type = Type.GetType(SpellFuncType);

                        if (type != null)
                        {
                            var method = type.GetMethod(SpellFuncName);

                            if (method != null)
                            {
                                skill.TickFunction = (TickFunc)method.CreateDelegate(typeof(TickFunc));
                            }
                            else
                                game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                        }
                        else
                            game.log("Type {0} not found", SpellFuncType);
                    }

                    if (!(SpellFuncType = skElement.GetAttributeValue("EndFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("EndFuncName")).ISEMPTY())
                    {
                        var type = Type.GetType(SpellFuncType);

                        if (type != null)
                        {
                            var method = type.GetMethod(SpellFuncName);

                            if (method != null)
                            {
                                skill.EndFunction = (TickFunc)method.CreateDelegate(typeof(TickFunc));
                            }
                            else
                                game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                        }
                        else
                            game.log("Type {0} not found", SpellFuncType);
                    }
                    
                    if (skElement.GetAttributeValue("AutoCast").StringCmp("false"))
                        skill.BoolAutoCast = false;

                    skill.waitTime = skElement.GetAttributeValueInt("WaitTime", skill.waitTime);
                    foreach (var levelelement in skElement.Elements("SkillLevel"))
                    {
                        var guildname = levelelement.GetAttributeValue("Guild");
                        skill.skillLevel[guildname] = levelelement.GetAttributeValueInt("Level");
                        skill.rating[guildname] = levelelement.GetAttributeValueInt("Rating");
                        if (levelelement.HasAttribute("Prerequisites"))
                        {
                            skill.guildPreRequisiteSkill[guildname] = levelelement.GetAttributeValue("Prerequisites");
                            skill.guildPreRequisiteSkillPercentage[guildname] = levelelement.GetAttributeValueInt("PrerequisitePercentage");
                        }
                    }
                }
                else
                {
                    skill = new SkillSpell(skname, skname, null, game.PULSE_PER_SECOND, true);
                    Utility.GetEnumValue<TargetTypes>(skElement.GetAttributeValue("TargetType", "targetIgnore"), ref skill.targetType);
                    Utility.GetEnumValue<Positions>(skElement.GetAttributeValue("MinimumPosition", "Standing"), ref skill.minimumPosition);
                    Utility.GetEnumValues<SkillSpellTypes>(skElement.GetAttributeValue("SkillTypes", "Skill"), ref skill.SkillTypes);
                    skill.minimumMana = skElement.GetAttributeValueInt("MinimumMana");
                    skill.NounDamage = skElement.GetAttributeValue("NounDamage");
                    skill.MessageOn = skElement.GetAttributeValue("MessageOn");
                    skill.MessageOnToRoom = skElement.GetAttributeValue("MessageOnToRoom");
                    skill.MessageOff = skElement.GetAttributeValue("MessageOff");
                    skill.MessageOffToRoom = skElement.GetAttributeValue("MessageOffToRoom");
                    skill.MessageItem = skElement.GetAttributeValue("MessageItem");
                    skill.MessageItemToRoom = skElement.GetAttributeValue("MessageItemToRoom");
                    skill.Prerequisites = skElement.GetAttributeValue("Prerequisites");
                    skill.PrerequisitePercentage = skElement.GetAttributeValueInt("PrerequisitePercentage");
                    skill.waitTime = skElement.GetAttributeValueInt("WaitTime", game.PULSE_VIOLENCE);
                    skill.AutoCastScript = skElement.GetAttributeValue("AutoCastScript");
                    //if (!skill.AutoCastScript.ISEMPTY() && UseRoslyn) RoslynScripts.PrepareCharacterBoolScript(skill.AutoCastScript);
                    if (skElement.GetAttributeValue("AutoCast", "true") == "false")
                        skill.BoolAutoCast = false;
                    string SpellFuncType;
                    string SpellFuncName;
                    if (!(SpellFuncType = skElement.GetAttributeValue("SpellFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("SpellFuncName")).ISEMPTY())
                    {
                        var type = Type.GetType(SpellFuncType);

                        if (type != null)
                        {
                            var method = type.GetMethod(SpellFuncName);

                            if (method != null)
                            {
                                skill.spellFun = (SpellFunc)method.CreateDelegate(typeof(SpellFunc));
                            }
                            else
                                game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                        }
                        else
                            game.log("Type {0} not found", SpellFuncType);
                    }
                    if (!(SpellFuncType = skElement.GetAttributeValue("TickFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("TickFuncName")).ISEMPTY())
                    {
                        var type = Type.GetType(SpellFuncType);

                        if (type != null)
                        {
                            var method = type.GetMethod(SpellFuncName);

                            if (method != null)
                            {
                                skill.TickFunction = (TickFunc)method.CreateDelegate(typeof(TickFunc));
                            }
                            else
                                game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                        }
                        else
                            game.log("Type {0} not found", SpellFuncType);
                    }

                    if (!(SpellFuncType = skElement.GetAttributeValue("EndFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("EndFuncName")).ISEMPTY())
                    {
                        var type = Type.GetType(SpellFuncType);

                        if (type != null)
                        {
                            var method = type.GetMethod(SpellFuncName);

                            if (method != null)
                            {
                                skill.EndFunction = (TickFunc)method.CreateDelegate(typeof(TickFunc));
                            }
                            else
                                game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                        }
                        else
                            game.log("Type {0} not found", SpellFuncType);
                    }
                    foreach (var levelelement in skElement.Elements("SkillLevel"))
                    {
                        var guildname = levelelement.GetAttributeValue("Guild");
                        skill.skillLevel[guildname] = levelelement.GetAttributeValueInt("Level");
                        skill.rating[guildname] = levelelement.GetAttributeValueInt("Rating");
                        if (levelelement.HasAttribute("Prerequisites"))
                        {
                            skill.guildPreRequisiteSkill[guildname] = levelelement.GetAttributeValue("Prerequisites");
                            skill.guildPreRequisiteSkillPercentage[guildname] = levelelement.GetAttributeValueInt("PrerequisitePercentage");
                        }
                    }
                }
            }
        }

        private static void LoadSongs()
        {
            SkillSpell skill;
            
            if (!System.IO.File.Exists("data\\songs.xml"))
                return;

            //var element = XElement.Load("data\\songs.xml", LoadOptions.PreserveWhitespace);
            var settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            using (var xmlReader = XmlReader.Create("data\\songs.xml", settings))
            {
                var element = XElement.Load(xmlReader);
                foreach (var skElement in element.Elements("Song"))
                {
                    var skname = skElement.GetAttributeValue("Name");

                    if (Skills.TryGetValue(skname, out skill)) //if ((skill = SkillLookup(skname)) != null)
                    {
                        Utility.GetEnumValue<TargetTypes>(skElement.GetAttributeValue("TargetType", "targetIgnore"), ref skill.targetType);
                        Utility.GetEnumValue<Positions>(skElement.GetAttributeValue("MinimumPosition", "Fighting"), ref skill.minimumPosition);
                        Utility.GetEnumValues<SkillSpellTypes>(skElement.GetAttributeValue("SkillTypes", "Song"), ref skill.SkillTypes);
                        skill.minimumMana = skElement.GetAttributeValueInt("MinimumMana", skill.minimumMana);
                        skill.NounDamage = skElement.GetAttributeValue("NounDamage", skill.NounDamage);
                        skill.MessageOn = skElement.GetAttributeValue("MessageOn");
                        skill.MessageOnToRoom = skElement.GetAttributeValue("MessageOnToRoom");
                        skill.MessageOff = skElement.GetAttributeValue("MessageOff");
                        skill.MessageOffToRoom = skElement.GetAttributeValue("MessageOffToRoom");
                        skill.MessageItem = skElement.GetAttributeValue("MessageItem");
                        skill.MessageItemToRoom = skElement.GetAttributeValue("MessageItemToRoom");
                        
                        skill.Prerequisites = skElement.GetAttributeValue("Prerequisites");
                        skill.PrerequisitePercentage = skElement.GetAttributeValueInt("PrerequisitePercentage");
                        skill.waitTime = skElement.GetAttributeValueInt("WaitTime", skill.waitTime);
                        

                        skill.Lyrics = skElement.GetElementValue("Lyrics", skill.Lyrics).Trim();
                        
                        skill.AutoCastScript = skElement.GetAttributeValue("AutoCastScript");
                        //if (!skill.AutoCastScript.ISEMPTY() && UseRoslyn) RoslynScripts.PrepareCharacterBoolScript(skill.AutoCastScript);
                        if (skElement.GetAttributeValue("AutoCast").StringCmp("false"))
                            skill.BoolAutoCast = false;
                        string SpellFuncType;
                        string SpellFuncName;
                        if (!(SpellFuncType = skElement.GetAttributeValue("SpellFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("SpellFuncName")).ISEMPTY())
                        {
                            var type = Type.GetType(SpellFuncType);

                            if (type != null)
                            {
                                var method = type.GetMethod(SpellFuncName);

                                if (method != null)
                                {
                                    skill.spellFun = (SpellFunc)method.CreateDelegate(typeof(SpellFunc));
                                }
                                else
                                    game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                            }
                            else
                                game.log("Type {0} not found", SpellFuncType);
                        }
                        if (!(SpellFuncType = skElement.GetAttributeValue("TickFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("TickFuncName")).ISEMPTY())
                        {
                            var type = Type.GetType(SpellFuncType);

                            if (type != null)
                            {
                                var method = type.GetMethod(SpellFuncName);

                                if (method != null)
                                {
                                    skill.TickFunction = (TickFunc)method.CreateDelegate(typeof(TickFunc));
                                }
                                else
                                    game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                            }
                            else
                                game.log("Type {0} not found", SpellFuncType);
                        }

                        if (!(SpellFuncType = skElement.GetAttributeValue("EndFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("EndFuncName")).ISEMPTY())
                        {
                            var type = Type.GetType(SpellFuncType);

                            if (type != null)
                            {
                                var method = type.GetMethod(SpellFuncName);

                                if (method != null)
                                {
                                    skill.EndFunction = (TickFunc)method.CreateDelegate(typeof(TickFunc));
                                }
                                else
                                    game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                            }
                            else
                                game.log("Type {0} not found", SpellFuncType);
                        }
                        foreach (var levelelement in skElement.Elements("SkillLevel"))
                        {
                            var guildname = levelelement.GetAttributeValue("Guild");
                            skill.skillLevel[guildname] = levelelement.GetAttributeValueInt("Level");
                            skill.rating[guildname] = levelelement.GetAttributeValueInt("Rating");
                            if (levelelement.HasElement("Prerequisites"))
                            {
                                skill.guildPreRequisiteSkill[guildname] = levelelement.GetAttributeValue("Prerequisites");
                                skill.guildPreRequisiteSkillPercentage[guildname] = levelelement.GetAttributeValueInt("PrerequisitePercentage");
                            }
                        }
                    }
                    else
                    {
                        skill = new SkillSpell(skname, skname, null, game.PULSE_PER_SECOND, true);
                        Utility.GetEnumValue<TargetTypes>(skElement.GetAttributeValue("TargetType", "targetIgnore"), ref skill.targetType);
                        Utility.GetEnumValue<Positions>(skElement.GetAttributeValue("MinimumPosition", "Standing"), ref skill.minimumPosition);
                        Utility.GetEnumValues<SkillSpellTypes>(skElement.GetAttributeValue("SkillTypes", "Skill"), ref skill.SkillTypes);
                        skill.minimumMana = skElement.GetAttributeValueInt("MinimumMana");
                        skill.NounDamage = skElement.GetAttributeValue("NounDamage");
                        skill.MessageOn = skElement.GetAttributeValue("MessageOn");
                        skill.MessageOnToRoom = skElement.GetAttributeValue("MessageOnToRoom");
                        skill.MessageOff = skElement.GetAttributeValue("MessageOff");
                        skill.MessageOffToRoom = skElement.GetAttributeValue("MessageOffToRoom");
                        skill.MessageItem = skElement.GetAttributeValue("MessageItem");
                        skill.MessageItemToRoom = skElement.GetAttributeValue("MessageItemToRoom");
                        //skill.Lyrics = skElement.GetAttributeValue("Lyrics").Trim();
                        skill.Prerequisites = skElement.GetAttributeValue("Prerequisites");
                        skill.PrerequisitePercentage = skElement.GetAttributeValueInt("PrerequisitePercentage");
                        skill.waitTime = skElement.GetAttributeValueInt("WaitTime", game.PULSE_VIOLENCE);
                        skill.Lyrics = skElement.GetElementValue("Lyrics", skill.Lyrics).Trim();
                        skill.AutoCastScript = skElement.GetAttributeValue("AutoCastScript");
                        //if (!skill.AutoCastScript.ISEMPTY() && UseRoslyn) RoslynScripts.PrepareCharacterBoolScript(skill.AutoCastScript);
                        if (skElement.GetAttributeValue("AutoCast").StringCmp("false"))
                            skill.BoolAutoCast = false;
                        string SpellFuncType;
                        string SpellFuncName;
                        if (!(SpellFuncType = skElement.GetAttributeValue("SpellFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("SpellFuncName")).ISEMPTY())
                        {
                            var type = Type.GetType(SpellFuncType);

                            if (type != null)
                            {
                                var method = type.GetMethod(SpellFuncName);

                                if (method != null)
                                {
                                    skill.spellFun = (SpellFunc)method.CreateDelegate(typeof(SpellFunc));
                                }
                                else
                                    game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                            }
                            else
                                game.log("Type {0} not found", SpellFuncType);
                        }
                        if (!(SpellFuncType = skElement.GetAttributeValue("TickFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("TickFuncName")).ISEMPTY())
                        {
                            var type = Type.GetType(SpellFuncType);

                            if (type != null)
                            {
                                var method = type.GetMethod(SpellFuncName);

                                if (method != null)
                                {
                                    skill.TickFunction = (TickFunc)method.CreateDelegate(typeof(TickFunc));
                                }
                                else
                                    game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                            }
                            else
                                game.log("Type {0} not found", SpellFuncType);
                        }

                        if (!(SpellFuncType = skElement.GetAttributeValue("EndFuncType")).ISEMPTY() && !(SpellFuncName = skElement.GetAttributeValue("EndFuncName")).ISEMPTY())
                        {
                            var type = Type.GetType(SpellFuncType);

                            if (type != null)
                            {
                                var method = type.GetMethod(SpellFuncName);

                                if (method != null)
                                {
                                    skill.EndFunction = (TickFunc)method.CreateDelegate(typeof(TickFunc));
                                }
                                else
                                    game.log("Method {0} not found in type {1}", SpellFuncName, SpellFuncType);
                            }
                            else
                                game.log("Type {0} not found", SpellFuncType);
                        }
                        foreach (var levelelement in skElement.Elements("SkillLevel"))
                        {
                            var guildname = levelelement.GetAttributeValue("Guild");
                            skill.skillLevel[guildname] = levelelement.GetAttributeValueInt("Level");
                            skill.rating[guildname] = levelelement.GetAttributeValueInt("Rating");
                            if (levelelement.HasElement("Prerequisites"))
                            {
                                skill.guildPreRequisiteSkill[guildname] = levelelement.GetAttributeValue("Prerequisites");
                                skill.guildPreRequisiteSkillPercentage[guildname] = levelelement.GetAttributeValueInt("PrerequisitePercentage");
                            }
                        }
                    }
                }
            }
        }

        private static void SaveSongs()
        {
            var element = new XElement("Songs",
               from sk in Skills.Values
               where sk.SkillTypes.ISSET(SkillSpellTypes.Song)
               select new XElement("Song", new XAttribute("Name", sk.internalName),
               sk.minimumPosition != Positions.Standing ?
               new XAttribute("MinimumPosition", sk.minimumPosition.ToString()) : null,
               sk.minimumMana != 0 ?
               new XAttribute("MinmumMana", sk.minimumMana) : null,
               new XAttribute("WaitTime", sk.waitTime),
               !sk.NounDamage.ISEMPTY() ?
               new XAttribute("NounDamage", sk.NounDamage.TOSTRINGTRIM()) : null,
               !sk.MessageOn.ISEMPTY() ?
               new XAttribute("MessageOn", sk.MessageOn.TOSTRINGTRIM()) : null,
               !sk.MessageOnToRoom.ISEMPTY() ?
               new XAttribute("MessageOnToRoom", sk.MessageOnToRoom.TOSTRINGTRIM()) : null,
               !sk.MessageOff.ISEMPTY() ?
               new XAttribute("MessageOff", sk.MessageOff.TOSTRINGTRIM()) : null,
               !sk.MessageOffToRoom.ISEMPTY() ?
               new XAttribute("MessageOffToRoom", sk.MessageOffToRoom.TOSTRINGTRIM()) : null,
               !sk.BoolAutoCast ? new XAttribute("AutoCast", false) : null,
              sk.spellFun != null ?
               new XAttribute("SpellFuncType", sk.spellFun.Method.DeclaringType.AssemblyQualifiedName) : null,
                sk.spellFun != null ?
               new XAttribute("SpellFuncName", sk.spellFun.Method.Name) : null,
                sk.TickFunction != null ?
               new XAttribute("TickFuncType", sk.TickFunction.Method.DeclaringType.AssemblyQualifiedName) : null,
                sk.TickFunction != null ?
               new XAttribute("TickFuncName", sk.TickFunction.Method.Name) : null,
                sk.EndFunction != null ?
               new XAttribute("EndFuncType", sk.EndFunction.Method.DeclaringType.AssemblyQualifiedName) : null,
                sk.EndFunction != null ?
               new XAttribute("EndFuncName", sk.EndFunction.Method.Name) : null,
               !sk.Prerequisites.ISEMPTY() ?
               new XAttribute("Prerequisites", sk.Prerequisites.TOSTRINGTRIM()) : null,
               sk.PrerequisitePercentage != 0 ?
               new XAttribute("PrerequisitePercentage", sk.PrerequisitePercentage) : null,
               !sk.AutoCastScript.ISEMPTY() ? new XAttribute("AutoCastScript", sk.AutoCastScript) : null,
               new XAttribute("SkillTypes", string.Join(" ", from t in sk.SkillTypes select t.ToString())),
               (
                   from guildlevel in sk.skillLevel
                   select new XElement("SkillLevel",
                       new XAttribute("Guild", guildlevel.Key.ToString()),
                       new XAttribute("Level", guildlevel.Value),
                       sk.guildPreRequisiteSkill.ContainsKey(guildlevel.Key) && !sk.guildPreRequisiteSkill[guildlevel.Key].ISEMPTY() ?
                       new XAttribute("Prerequisites", sk.guildPreRequisiteSkill[guildlevel.Key]) : null,
                       sk.guildPreRequisiteSkillPercentage.ContainsKey(guildlevel.Key) && sk.guildPreRequisiteSkillPercentage[guildlevel.Key] != 0 ?
                       new XAttribute("PrerequisitePercentage", sk.guildPreRequisiteSkillPercentage[guildlevel.Key]) : null,
                       new XAttribute("Rating", 1))),
                !sk.Lyrics.ISEMPTY() ?
               new XElement("Lyrics", "\n" + sk.Lyrics.TOSTRINGTRIM() + "\n    ") : null
                       )
                );
            System.IO.File.WriteAllText("data\\songs.xml", element.ToStringFormatted());
            //element.Save("data\\songs.xml");
        }

        private static void SaveSkills()
        {
            var element = new XElement("SkillLevels",
               from sk in Skills.Values
               where !sk.SkillTypes.ISSET(SkillSpellTypes.Song)
               select new XElement("SkillSpell", new XAttribute("Name", sk.internalName),
               sk.targetType != TargetTypes.targetIgnore ?
               new XAttribute("TargetType", sk.targetType.ToString()) : null,
               sk.minimumPosition != Positions.Standing ?
               new XAttribute("MinimumPosition", sk.minimumPosition.ToString()) : null,
               sk.minimumMana != 0 ?
               new XAttribute("MinmumMana", sk.minimumMana) : null,
               new XAttribute("WaitTime", sk.waitTime),
               !sk.NounDamage.ISEMPTY() ?
               new XAttribute("NounDamage", sk.NounDamage.TOSTRINGTRIM()) : null,
               !sk.MessageOn.ISEMPTY() ?
               new XAttribute("MessageOn", sk.MessageOn.TOSTRINGTRIM()) : null,
               !sk.MessageOnToRoom.ISEMPTY() ?
               new XAttribute("MessageOnToRoom", sk.MessageOnToRoom.TOSTRINGTRIM()) : null,
               !sk.MessageOff.ISEMPTY() ?
               new XAttribute("MessageOff", sk.MessageOff.TOSTRINGTRIM()) : null,
               !sk.MessageOffToRoom.ISEMPTY() ?
               new XAttribute("MessageOffToRoom", sk.MessageOffToRoom.TOSTRINGTRIM()) : null,
               !sk.MessageItem.ISEMPTY() ?
               new XAttribute("MessageItem", sk.MessageItem.TOSTRINGTRIM()) : null,
               !sk.MessageItemToRoom.ISEMPTY() ?
               new XAttribute("MessageItemToRoom", sk.MessageItemToRoom.TOSTRINGTRIM()) : null,
               
               new XAttribute("AutoCast", sk.BoolAutoCast),
              sk.spellFun != null ?
               new XAttribute("SpellFuncType", sk.spellFun.Method.DeclaringType.AssemblyQualifiedName) : null,
                sk.spellFun != null ?
               new XAttribute("SpellFuncName", sk.spellFun.Method.Name) : null,
                 sk.TickFunction != null ?
               new XAttribute("TickFuncType", sk.TickFunction.Method.DeclaringType.AssemblyQualifiedName) : null,
                sk.TickFunction != null ?
               new XAttribute("TickFuncName", sk.TickFunction.Method.Name) : null,
                sk.EndFunction != null ?
               new XAttribute("EndFuncType", sk.EndFunction.Method.DeclaringType.AssemblyQualifiedName) : null,
                sk.EndFunction != null ?
               new XAttribute("EndFuncName", sk.EndFunction.Method.Name) : null,
               !sk.Prerequisites.ISEMPTY() ?
               new XAttribute("Prerequisites", sk.Prerequisites.TOSTRINGTRIM()) : null,
               sk.PrerequisitePercentage != 0 ?
               new XAttribute("PrerequisitePercentage", sk.PrerequisitePercentage) : null,
               !sk.AutoCastScript.ISEMPTY()? new XAttribute("AutoCastScript", sk.AutoCastScript) : null,
           new XAttribute("SkillTypes", string.Join(" ", from t in sk.SkillTypes select t.ToString())),
               (
                   from guildlevel in sk.skillLevel
                   select new XElement("SkillLevel",
                       new XAttribute("Guild", guildlevel.Key.ToString()),
                       new XAttribute("Level", guildlevel.Value),
                       sk.guildPreRequisiteSkill.ContainsKey(guildlevel.Key) && !sk.guildPreRequisiteSkill[guildlevel.Key].ISEMPTY() ?
                       new XAttribute("Prerequisites", sk.guildPreRequisiteSkill[guildlevel.Key]) : null,
                       sk.guildPreRequisiteSkillPercentage.ContainsKey(guildlevel.Key) && sk.guildPreRequisiteSkillPercentage[guildlevel.Key] != 0 ?
                       new XAttribute("PrerequisitePercentage", sk.guildPreRequisiteSkillPercentage[guildlevel.Key]) : null,
                       new XAttribute("Rating", 1)))));
            //element.Save("data\\skilllevels.xml");
            System.IO.File.WriteAllText("data\\skilllevels.xml", element.ToStringFormatted());
        }

        public static SkillSpell SkillLookup(string name)
        {
            if (name.ISEMPTY()) return null;
            SkillSpell skillspell;
            if (Skills.TryGetValue(name, out skillspell)) return skillspell;
            foreach (var skill in Skills)
            {
                if (skill.Value.name.StringPrefix(name) || skill.Value.internalName.StringPrefix(name))
                {
                    return skill.Value;
                }
            }
            if (!name.ISEMPTY())
                game.log("Skill not found: {0}", name);

            return null;
        }

        public static SkillSpell FindSpell(Character ch, string name)
        {
            return (from tempskill in SkillSpell.Skills
                     where
                       (tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Skill) ||
                       tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Spell) ||
                       tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Commune) ||
                       tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.Song) ||
                       (tempskill.Value.SkillTypes.ISSET(SkillSpellTypes.InForm) && tempskill.Value.spellFun != null))
                     && (ch.Level >= ch.GetLevelSkillLearnedAtOutOfForm(tempskill.Value) && ch.GetSkillPercentageOutOfForm(tempskill.Value) >= 1)
                     && tempskill.Value.name.StringPrefix(name)
                     orderby ch.GetLevelSkillLearnedAtOutOfForm(tempskill.Value) // skill.Value.skillLevel.ContainsKey(ch.Guild.name) ? skill.Value.skillLevel[ch.Guild.name] : 60
                     select tempskill.Value).FirstOrDefault();
            //foreach (var skill in Skills)
            //{
            //    if ((skill.Value.name.StringPrefix(name) || skill.Value.internalName.StringPrefix(name)) && (skill.Value.skillLevel.ContainsKey(ch.Guild != null ? ch.Guild.name : "") || ch.GetSkillPercentage(skill.Value) > 0))
            //    {
            //        return skill.Value;
            //    }
            //}
            //return null;
        }

        public int GetManaCost(Character ch)
        {
            if (ch.Guild != null && skillLevel.ContainsKey(ch.Guild.name) && ch.Level + 2 == skillLevel[ch.Guild.name])
                return 50;
            else if (ch.Guild != null && skillLevel.ContainsKey(ch.Guild.name))
                return Math.Max(
                minimumMana,
                100 / (2 + ch.Level - skillLevel[ch.Guild.name]));
            else
                return 50;
        }
    }

    public class LearnedSkillSpell
    {
        public SkillSpell Skill;
        public string SkillName;
        public int Percentage;
        public int Level;

    }
}
