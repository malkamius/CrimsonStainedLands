using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Schema;

namespace CrimsonStainedLands
{
    public class ShapeshiftForm
    {
        public static List<ShapeshiftForm> Forms = new List<ShapeshiftForm>();

        public enum FormType
        {
            None,
            Offense,
            Defense,
            Utility,
            Air,
            Water,
            Offensive = Offense,
            Defensive = Defense
        }

        public enum FormTier
        {
            Tier1,
            Tier2,
            Tier3,
            Tier4
        }

        public string Name { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string LongDescription { get; set; } = "";
        public string Description { get; set; } = "";

        public PhysicalStats Stats { get; set; } = null;
        public int ArmorBash { get; set; } = 0;
        public int ArmorSlash { get; set; } = 0;
        public int ArmorPierce { get; set; } = 0;
        public int ArmorExotic { get; set; } = 0;

        public int DamageReduction { get; set; } = 0;

        public int AttacksPerRound { get; set; } = 0;
        public WeaponDamageMessage DamageType { get; set; } = WeaponDamageMessage.GetWeaponDamageMessage("none");
        public int ParryModifier { get; set; } = 0;
        public Dice DamageDice { get; set; } = new Dice(0, 0, 0);
        public int HitRoll { get; set; } = 0;
        public int DamageRoll { get; set; } = 0;
        public int SavesSpell { get; set; } = 0;

        public List<WeaponDamageTypes> ImmuneFlags = new List<WeaponDamageTypes>();
        public List<WeaponDamageTypes> ResistFlags = new List<WeaponDamageTypes>();
        public List<WeaponDamageTypes> VulnerableFlags = new List<WeaponDamageTypes>();

        public List<AffectFlags> AffectedBy = new List<AffectFlags>();

        public List<PartFlags> Parts = new List<PartFlags>();

        public string Yell { get; set; } = "";
        public Dictionary<SkillSpell, int> Skills { get; set; } = new Dictionary<SkillSpell, int>();

        public FormTier Tier = FormTier.Tier4;

        public FormType Type = FormType.Utility;

        public SkillSpell FormSkill { get; set; } = null;

        static ShapeshiftForm()
        {
            //Forms.Add(new ShapeshiftForm("lion", "a ferocious lion", "A massive, yellow maned lion whips around and regards you dispassionately before turning away.", "",
            //    new PhysicalStats(23, 17, 18, 16, 20, 13), -170, -170, -170, -170, 125, 5, WeaponDamageMessage.GetWeaponDamageMessage("claw"), 30,
            //    new Dice(20, 12, 30), 100, 160, -30, new List<WeaponDamageTypes>(), new List<WeaponDamageTypes>(), new List<WeaponDamageTypes>(),
            //    new List<AffectFlags>(), "The loud roar of a lion echoes nearby.", new Dictionary<string, int>()
            //    {
            //        { "dodge", 100 },
            //        { "bite", 100 },
            //        { "claw", 100 },
            //        { "evasion", 75 },
            //        { "second attack", 100 },
            //        { "third attack", 100 },
            //        { "fourth attack", 100 }
            //    }, FormTier.Tier1, FormType.Offense, "form lion"));

            //Forms.Add(new ShapeshiftForm("armadillo", "a funny looking shelled creature", "A funny looking armadillo stands here looking around.", "",
            //    new PhysicalStats(23, 17, 18, 16, 20, 13), -170, -170, -170, -170, 125, 2, WeaponDamageMessage.GetWeaponDamageMessage("claw"), 50,
            //    new Dice(10, 5, 30), 20, 20, -30, new List<WeaponDamageTypes>(), new List<WeaponDamageTypes>(), new List<WeaponDamageTypes>(),
            //    new List<AffectFlags>(), "The loud squeal of an armadillo echos nearby.", new Dictionary<string, int>()
            //    {
            //        { "dodge", 75 },
            //        { "bite", 100 },
            //        { "damage reduction", 100 },
            //        { "evasion", 75 },
            //        { "second attack", 80 }
            //    }, FormTier.Tier1, FormType.Defense, "form armadillo"));
        }

        public ShapeshiftForm(string name, string shortDescription, string longDescription, string description, PhysicalStats stats, int armorBash, int armorSlash, int armorPierce, int armorExotic, int damageReduction, int attacksPerRound, WeaponDamageMessage damageType, int parryModifier, Dice damageDice, int hitRoll, int damageRoll, int savesSpell, List<WeaponDamageTypes> immuneFlags, List<WeaponDamageTypes> resistFlags, List<WeaponDamageTypes> vulnerableFlags, List<AffectFlags> affectedBy, string yell, Dictionary<string, int> skills, FormTier tier, FormType type, string formSkill)
        {
            Name = name;
            ShortDescription = shortDescription;
            LongDescription = longDescription;
            Description = description;
            Stats = stats;
            ArmorBash = armorBash;
            ArmorSlash = armorSlash;
            ArmorPierce = armorPierce;
            ArmorExotic = armorExotic;
            DamageReduction = damageReduction;
            AttacksPerRound = attacksPerRound;
            DamageType = damageType;
            ParryModifier = parryModifier;
            DamageDice = damageDice;
            HitRoll = hitRoll;
            DamageRoll = damageRoll;
            SavesSpell = savesSpell;
            ImmuneFlags = immuneFlags;
            ResistFlags = resistFlags;
            VulnerableFlags = vulnerableFlags;
            AffectedBy = affectedBy;
            Yell = yell;
            //Skills = skills;
            if (skills != null)
            {
                Skills.Clear();
                foreach (var sk in skills)
                {
                    var skill = SkillSpell.SkillLookup(sk.Key);
                    if (skill != null)
                        Skills[skill] = sk.Value;
                }
            }
            Tier = tier;
            Type = type;
            FormSkill = SkillSpell.SkillLookup(formSkill);
            if (FormSkill == null)
                Game.log("Form skill not found: {0}", formSkill);
        }


        public static ShapeshiftForm GetForm(Character ch, string name, bool strprefix = true)
        {
            foreach (var form in Forms)
            {
                int learned;
                if ((learned = ch.GetSkillPercentage(form.FormSkill)) <= 1)
                    continue;
                //if(!ch.Forms.TryGetValue(form, out var learned) || learned < 1)
                //{
                //    continue;
                //}

                if (strprefix && form.Name.StringPrefix(name))
                    return form;
                else if (!strprefix && form.Name.StringCmp(name)) return form;
            }
            return null;
        }

        public static void DoShapeshift(Character ch, string arguments)
        {
            var form = GetForm(ch, arguments);

            if (form == null)
            {
                ch.send("You don't know that form.\n\r");
                return;
            }
            else if (!form.Parts.ISSET(PartFlags.Legs) && form.AffectedBy.ISSET(AffectFlags.WaterBreathing))
            {
                ch.send("You need to be in the water to become that.\n\r");
                return;
            }
            else
            {
                ch.Act("The form of $n begins to twist and stretch as $e changes into {0}.", null, null, null, ActType.ToRoom, form.ShortDescription);
                ch.Act("You begin to twist and stretch as you change into {0}.", null, null, null, ActType.ToChar, form.ShortDescription);
                checkControls(ch);
                ch.Form = form;
            }
        }

        private static void checkControls(Character ch)
        {
            AffectData affect;
            SkillSpell spell;
            SkillSpell control;

            var spells=new Dictionary<string, string>() { { "fly","control levitation"},
                { "pass door","control phase"},{"stone skin","control skin" },
                    { "haste","control speed"},{"slow","control speed"} };

            foreach(var pair in spells)
            {
                spell = SkillSpell.SkillLookup(pair.Key);

                if ((affect = ch.FindAffect(spell)) != null)
                {
                    control = SkillSpell.SkillLookup(pair.Value);
                    var chance = ch.GetSkillPercentageOutOfForm(control) + 20;
                    if (chance <= 1 || chance < Utility.NumberPercent())
                    {
                        ch.AffectFromChar(affect, AffectRemoveReason.WoreOff);
                        if (chance > 1) ch.CheckImprove(control, false, 1);
                    }
                    else ch.CheckImprove(control,true,1);
                }
            }
            
        }

        public static void DoRevert(Character ch, string arguments)
        {
            if (ch.Form != null)
            {
                if (ch.IsAffected(AffectFlags.Retract))
                {
                    Combat.DoRetract(ch, "");
                }
                ch.Act("The form of $n begins to twist and stretch as $e returns to $s normal form.", null, null, null, ActType.ToRoom);
                ch.Act("You feel your bones begin to twist and stretch as you revert to your natural form.", null, null, null, ActType.ToChar);
                ch.Form = null;
                checkControls(ch);
            }
            else
                ch.send("You aren't shapeshifted.\n\r");
        }

        public static void CheckGainForm(Character ch)
        {
            //;
            if (!(ch is Player) || ch.Guild == null || ch.Guild.name != "shapeshifter") return;
            var player = (Player)ch;

            var tier4forms = (from form in Forms where form.Tier == FormTier.Tier4 && form.Type == player.ShapeFocusMajor && ch.GetSkillPercentage(form.FormSkill) > 1 select form).Count();
            var tier3forms = (from form in Forms where form.Tier == FormTier.Tier3 && (form.Type == player.ShapeFocusMajor || form.Type == player.ShapeFocusMinor) && ch.GetSkillPercentage(form.FormSkill) > 1 select form).Count();
            var tier2forms = (from form in Forms where form.Tier == FormTier.Tier2 && (form.Type == player.ShapeFocusMajor || form.Type == player.ShapeFocusMinor) && ch.GetSkillPercentage(form.FormSkill) > 1 select form).Count();
            var tier1forms = (from form in Forms where form.Tier == FormTier.Tier1 && (form.Type == player.ShapeFocusMajor || form.Type == player.ShapeFocusMinor) && ch.GetSkillPercentage(form.FormSkill) > 1 select form).Count();

            if (player.Level >= 5 || (player.Level >= 8 && 30 > Utility.NumberPercent()))
            {

                var hasTier4Major = tier4forms > 0;

                if (!hasTier4Major)
                {
                    var selectedform = (from form in Forms where form.Tier == FormTier.Tier4 && form.Type == player.ShapeFocusMajor && form.FormSkill != null && ch.GetSkillPercentage(form.FormSkill) <= 1 select form).SelectRandom();

                    if (selectedform != null)
                    {
                        var skill = selectedform.FormSkill;
                        ch.LearnSkill(skill, 75, ch.Level);
                        ch.send("\\GYou have learned how to shapeshift into the form of {0}{1}!\\x\n\r", "aeiou".Contains(selectedform.Name[0]) ? "an " : "a ", selectedform.Name);
                        player.SaveCharacterFile();
                    }
                }
            }

            if (player.Level >= 18 || (player.Level >= 16 && 30 > Utility.NumberPercent()))
            {

                var hasTier3Major = tier3forms > 0;

                if (!hasTier3Major)
                {
                    var selectedform = (from form in Forms where form.Tier == FormTier.Tier3 && form.Type == player.ShapeFocusMajor && form.FormSkill != null && ch.GetSkillPercentage(form.FormSkill) <= 1 select form).SelectRandom();

                    if (selectedform != null)
                    {
                        var skill = selectedform.FormSkill;
                        ch.LearnSkill(skill, 75, ch.Level);
                        ch.send("\\GYou have learned how to shapeshift into the form of {0}{1}!\\x\n\r", "aeiou".Contains(selectedform.Name[0]) ? "an " : "a ", selectedform.Name);
                        player.SaveCharacterFile();
                    }
                }
            }

            if (player.Level >= 25 || (player.Level >= 22 && 30 > Utility.NumberPercent()))
            {

                var hasTier3Minor = tier3forms > 1;

                if (!hasTier3Minor)
                {
                    var selectedform = (from form in Forms where form.Tier == FormTier.Tier3 && form.Type == player.ShapeFocusMinor && form.FormSkill != null && ch.GetSkillPercentage(form.FormSkill) <= 1 select form).SelectRandom();

                    if (selectedform != null)
                    {
                        var skill = selectedform.FormSkill;
                        ch.LearnSkill(skill, 75, ch.Level);
                        ch.send("\\GYou have learned how to shapeshift into the form of {0}{1}!\\x\n\r", "aeiou".Contains(selectedform.Name[0]) ? "an " : "a ", selectedform.Name);
                        player.SaveCharacterFile();
                    }
                }
            }



            if (player.Level >= 30 || (player.Level >= 28 && 30 > Utility.NumberPercent()))
            {

                var hasTier2Major = tier2forms > 0;

                if (!hasTier2Major)
                {
                    var selectedform = (from form in Forms where form.Tier == FormTier.Tier2 && form.Type == player.ShapeFocusMajor && form.FormSkill != null && ch.GetSkillPercentage(form.FormSkill) <= 1 select form).SelectRandom();

                    if (selectedform != null)
                    {
                        var skill = selectedform.FormSkill;
                        ch.LearnSkill(skill, 75, ch.Level);
                        ch.send("\\GYou have learned how to shapeshift into the form of {0}{1}!\\x\n\r", "aeiou".Contains(selectedform.Name[0]) ? "an " : "a ", selectedform.Name);
                        player.SaveCharacterFile();
                    }
                }
            }

            if (player.Level >= 33 || (player.Level >= 31 && 30 > Utility.NumberPercent()))
            {

                var hasTier2Minor = tier2forms > 1;

                if (!hasTier2Minor)
                {
                    var selectedform = (from form in Forms where form.Tier == FormTier.Tier2 && form.Type == player.ShapeFocusMinor && form.FormSkill != null && ch.GetSkillPercentage(form.FormSkill) <= 1 select form).SelectRandom();

                    if (selectedform != null)
                    {
                        var skill = selectedform.FormSkill;
                        ch.LearnSkill(skill, 75, ch.Level);
                        ch.send("\\GYou have learned how to shapeshift into the form of {0}{1}!\\x\n\r", "aeiou".Contains(selectedform.Name[0]) ? "an " : "a ", selectedform.Name);
                        player.SaveCharacterFile();
                    }
                }
            }



            if (player.Level >= 44 || (player.Level >= 42 && 30 > Utility.NumberPercent()))
            {

                var hasTier1Major = tier1forms > 0;

                if (!hasTier1Major)
                {
                    var selectedform = (from form in Forms where form.Tier == FormTier.Tier1 && form.Type == player.ShapeFocusMajor && form.FormSkill != null && ch.GetSkillPercentage(form.FormSkill) <= 1 select form).SelectRandom();

                    if (selectedform != null)
                    {
                        var skill = selectedform.FormSkill;
                        ch.LearnSkill(skill, 75, ch.Level);
                        ch.send("\\GYou have learned how to shapeshift into the form of {0}{1}!\\x\n\r", "aeiou".Contains(selectedform.Name[0]) ? "an " : "a ", selectedform.Name);
                        player.SaveCharacterFile();
                    }
                }
            }

            if (player.Level >= 48 || (player.Level >= 47 && 30 > Utility.NumberPercent()))
            {

                var hasTier1Minor = tier1forms > 1;

                if (!hasTier1Minor)
                {
                    var selectedform = (from form in Forms where form.Tier == FormTier.Tier1 && form.Type == player.ShapeFocusMinor && form.FormSkill != null && ch.GetSkillPercentage(form.FormSkill) <= 1 select form).SelectRandom();

                    if (selectedform != null)
                    {
                        var skill = selectedform.FormSkill;
                        ch.LearnSkill(skill, 75, ch.Level);
                        ch.send("\\GYou have learned how to shapeshift into the form of {0}{1}!\\x\n\r", "aeiou".Contains(selectedform.Name[0]) ? "an " : "a ", selectedform.Name);
                        player.SaveCharacterFile();
                    }
                }
            }
        } // end CheckGainForm

        public static void SaveShapeshiftForms()
        {
            var element = new XElement("ShapeshiftForms");
            FormType lastType = FormType.None;
            FormTier lastTier = FormTier.Tier1;

            foreach (var form in from f in ShapeshiftForm.Forms orderby f.Type, f.Tier, f.Name select f)
            {
                if (lastType != form.Type || lastTier != form.Tier)
                {
                    element.Add(new XComment("=== " + form.Type.ToString() + " " + form.Tier.ToString() + " ==="));
                    lastType = form.Type;
                    lastTier = form.Tier;
                }
                element.Add(new XElement("Form",
                    new XAttribute("Name", form.Name),
                    new XAttribute("Tier", form.Tier.ToString()),
                    new XAttribute("Type", form.Type.ToString()),
                    new XAttribute("FormSkill", form.FormSkill.name),
                    new XAttribute("ShortDescription", form.ShortDescription),
                    new XAttribute("LongDescription", form.LongDescription),
                    new XAttribute("Yell", form.Yell),
                    new XAttribute("AffectedBy", string.Join(" ", from aff in form.AffectedBy select aff.ToString())),
                    new XAttribute("AttacksPerRound", form.AttacksPerRound),
                    new XAttribute("DamageRoll", form.DamageRoll),
                    new XAttribute("HitRoll", form.HitRoll),
                    new XAttribute("DamageDiceSides", form.DamageDice.DiceSides),
                    new XAttribute("DamageDiceCount", form.DamageDice.DiceCount),
                    new XAttribute("DamageDiceBonus", form.DamageDice.DiceBonus),
                    new XAttribute("DamageReduction", form.DamageReduction),
                    new XAttribute("DamageMessage", form.DamageType.Keyword),
                    new XAttribute("SavesSpell", form.SavesSpell),
                    new XAttribute("ArmorBash", form.ArmorBash),
                    new XAttribute("ArmorSlash", form.ArmorSlash),
                    new XAttribute("ArmorPierce", form.ArmorPierce),
                    new XAttribute("ArmorExotic", form.ArmorExotic),
                    new XAttribute("Immune", string.Join(" ", from flag in form.ImmuneFlags select flag.ToString())),
                    new XAttribute("Resist", string.Join(" ", from flag in form.ResistFlags select flag.ToString())),
                    new XAttribute("Vulnerable", string.Join(" ", from flag in form.VulnerableFlags select flag.ToString())),
                    new XAttribute("Parts", string.Join(" ", from flag in form.Parts select flag.ToString())),
                    new XAttribute("ParryModifier", form.ParryModifier),
                    form.Stats.Element("Stats"),
                    new XElement("Description", form.Description),
                    new XElement("Skills",
                    from sk in form.Skills
                    where sk.Key != null
                    select new XElement("Skill", new XAttribute("Name", sk.Key.name), new XAttribute("Value", sk.Value.ToString())))
                    )
                );
            }

            //element.Save("data\\shapeshiftforms.xml");
            System.IO.File.WriteAllText(Settings.DataPath + "\\shapeshiftforms.xml", element.ToStringFormatted());
        }

        public static void LoadShapeshiftForms()
        {
            ShapeshiftForm.Forms.Clear();
            var element = XElement.Load(Settings.DataPath + "\\shapeshiftforms.xml");

            foreach (var formelement in element.Elements("Form"))
            {
                var form = new ShapeshiftForm(formelement.GetAttributeValue("Name"),
                    formelement.GetAttributeValue("ShortDescription"),
                    formelement.GetAttributeValue("LongDescription"),
                    formelement.GetElementValue("Description"),
                    new PhysicalStats(formelement.GetElement("Stats")),
                    formelement.GetAttributeValueInt("ArmorBash"),
                    formelement.GetAttributeValueInt("ArmorSlash"),
                    formelement.GetAttributeValueInt("ArmorPierce"),
                    formelement.GetAttributeValueInt("ArmorExotic"),
                    formelement.GetAttributeValueInt("DamageReduction"),
                    formelement.GetAttributeValueInt("AttacksPerRound"),
                    WeaponDamageMessage.GetWeaponDamageMessage(formelement.GetAttributeValue("DamageMessage")),
                    formelement.GetAttributeValueInt("ParryModifier"),
                    new Dice(formelement.GetAttributeValueInt("DamageDiceSides"), formelement.GetAttributeValueInt("DamageDiceCount"), formelement.GetAttributeValueInt("DamageDiceBonus")),
                    formelement.GetAttributeValueInt("HitRoll"),
                    formelement.GetAttributeValueInt("DamageRoll"),
                    formelement.GetAttributeValueInt("SavesSpell"),
                    null, null, null, null,
                    formelement.GetAttributeValue("Yell"),
                    null,
                    FormTier.Tier1, FormType.None,
                    formelement.GetAttributeValue("FormSkill")
                    );
                if (formelement.HasElement("Skills"))
                    foreach (var skill in formelement.GetElement("Skills").Elements())
                    {
                        var name = skill.GetAttributeValue("Name");
                        var value = skill.GetAttributeValueInt("value");
                        var sp = SkillSpell.SkillLookup(name);
                        if (sp != null)
                        {
                            form.Skills[sp] = value;
                        }
                    }
                form.Skills[SkillSpell.SkillLookup("recall")] = 100;
                Utility.GetEnumValues(formelement.GetAttributeValue("Immune"), ref form.ImmuneFlags);
                Utility.GetEnumValues(formelement.GetAttributeValue("Resist"), ref form.ResistFlags);
                Utility.GetEnumValues(formelement.GetAttributeValue("Vulnerable"), ref form.VulnerableFlags);

                Utility.GetEnumValues(formelement.GetAttributeValue("Parts", "Head Arms Legs Heart Brains Guts Hands Feet Fingers Ear Eye"), ref form.Parts);

                Utility.GetEnumValues(formelement.GetAttributeValue("AffectedBy"), ref form.AffectedBy);
                Utility.GetEnumValue(formelement.GetAttributeValue("Tier"), ref form.Tier);
                Utility.GetEnumValue(formelement.GetAttributeValue("Type"), ref form.Type);
                ShapeshiftForm.Forms.Add(form);
            }
        }
        public static void DoShapeFocus(Character ch, string arguments)
        {
            string arg = "";
            arguments = arguments.OneArgument(ref arg);

            if (!(ch is Player))
                return;

            var player = (Player)ch;

            if (ch.Guild == null || ch.Guild.name != "shapeshifter")
            {
                ch.Act("Only shapeshifters can choose a shapefocus.");
                return;
            }

            if (arg.StringCmp("major"))
            {
                if (player.ShapeFocusMajor == ShapeshiftForm.FormType.None && Utility.GetEnumValue(arguments, ref player.ShapeFocusMajor, ShapeshiftForm.FormType.None) && player.ShapeFocusMajor != ShapeshiftForm.FormType.None && player.ShapeFocusMajor != ShapeshiftForm.FormType.Air && player.ShapeFocusMajor != ShapeshiftForm.FormType.Water)
                {
                    ch.send("You have chosen the major focus {0}.\n\r", player.ShapeFocusMajor.ToString().ToLower());
                    if (player.ShapeFocusMajor == ShapeshiftForm.FormType.Offense)
                    {
                        player.LearnSkill(SkillSpell.SkillLookup("tiger frenzy"), 1, 30);
                        player.LearnSkill(SkillSpell.SkillLookup("bestial fury"), 1, 36);

                    }
                    if (player.ShapeFocusMajor == ShapeshiftForm.FormType.Defense)
                    {
                        player.LearnSkill(SkillSpell.SkillLookup("primal tenacity"), 1, 30);
                        player.LearnSkill(SkillSpell.SkillLookup("skin of the displacer"), 1, 36);

                    }
                    if (player.ShapeFocusMajor == ShapeshiftForm.FormType.Utility)
                    {
                        player.LearnSkill(SkillSpell.SkillLookup("slyness of the fox"), 1, 30);
                        player.LearnSkill(SkillSpell.SkillLookup("recovery of the snake"), 1, 36);

                    }
                    ShapeshiftForm.CheckGainForm(ch);
                }
                else if (player.ShapeFocusMajor == ShapeshiftForm.FormType.Air || player.ShapeFocusMajor == ShapeshiftForm.FormType.Water)
                {
                    player.ShapeFocusMajor = ShapeshiftForm.FormType.None;
                    ch.send("Syntax: shapefocus major [{0}].\n\r", string.Join(" ", Utility.GetEnumValues<ShapeshiftForm.FormType>().Where(f => f != ShapeshiftForm.FormType.Water && f != ShapeshiftForm.FormType.Air).Distinct()));

                }
                else if (player.ShapeFocusMajor != ShapeshiftForm.FormType.None)
                {
                    ch.send("Your major shapefocus is in {0} forms.\n\r", player.ShapeFocusMajor.ToString().ToLower());
                }
                else
                    ch.send("Syntax: shapefocus major [{0}].\n\r", string.Join(" ", Utility.GetEnumValues<ShapeshiftForm.FormType>().Where(f => f != ShapeshiftForm.FormType.Water && f != ShapeshiftForm.FormType.Air).Distinct()));


            }
            else if (arg.StringCmp("minor"))
            {
                if (player.ShapeFocusMinor == ShapeshiftForm.FormType.None && Utility.GetEnumValue(arguments, ref player.ShapeFocusMinor, ShapeshiftForm.FormType.None) && player.ShapeFocusMinor != ShapeshiftForm.FormType.None && player.ShapeFocusMinor != ShapeshiftForm.FormType.Air && player.ShapeFocusMinor != ShapeshiftForm.FormType.Water)
                {
                    ch.send("You have chosen the minor focus {0}.\n\r", player.ShapeFocusMinor.ToString().ToLower());
                    ShapeshiftForm.CheckGainForm(ch);
                }
                else if (player.ShapeFocusMinor == ShapeshiftForm.FormType.Air || player.ShapeFocusMinor == ShapeshiftForm.FormType.Water)
                {
                    player.ShapeFocusMinor = ShapeshiftForm.FormType.None;
                    ch.send("Syntax: shapefocus minor [{0}].\n\r", string.Join(" ", Utility.GetEnumValues<ShapeshiftForm.FormType>().Where(f => f != ShapeshiftForm.FormType.Water && f != ShapeshiftForm.FormType.Air).Distinct()));
                }
                else if (player.ShapeFocusMinor != ShapeshiftForm.FormType.None)
                {
                    ch.send("Your minor shapefocus is in {0} forms.\n\r", player.ShapeFocusMinor.ToString().ToLower());
                }
                else
                    ch.send("Syntax: shapefocus minor [{0}].\n\r", string.Join(" ", Utility.GetEnumValues<ShapeshiftForm.FormType>().Where(f => f != ShapeshiftForm.FormType.Water && f != ShapeshiftForm.FormType.Air).Distinct()));

            }
            else
                ch.send("Syntax: shapefocus [major|minor] [{0}].\n\r", string.Join(" ", Utility.GetEnumValues<ShapeshiftForm.FormType>().Where(f => f != ShapeshiftForm.FormType.Water && f != ShapeshiftForm.FormType.Air).Distinct()));
        } // end shape focus
        public static void DoEnliven(Character ch, string arguments)
        {
            var sksp = new string[] { "fly", "pass door", "stone skin", "haste", "slow", };
            var greaterEnlivens = new string[] { "tiger frenzy", "bestial fury" ,"primal tenacity" ,
                "skin of the displacer","slyness of the fox","recovery of the snake"};

            if (ch.Form == null)
            {
                ch.Act("You can only enliven while in form!");
                return;
            }
            else if (arguments.ISEMPTY())
            {
                ch.Act("Enliven which of the following spells : {0}?", args: string.Join(", ", from name in sksp.Concat(greaterEnlivens) where ch.GetSkillPercentageOutOfForm(name) > 1 select name));
                return;
            }

            
            
            foreach (var spell in sksp.Concat(greaterEnlivens))
            {
                if (spell.StringPrefix(arguments))
                {
                    var sk = SkillSpell.SkillLookup(spell);
                    var chance = ch.GetSkillPercentageOutOfForm(sk) + 20;

                    if (chance <= 21)
                    {
                        if (greaterEnlivens.Contains(spell)) ch.Act("You haven't learned that greater enliven yet.");
                        else ch.Act("You haven't learned that spell yet.");
                    }
                    else if (chance < Utility.NumberPercent())
                    {
                        ch.Act("You failed to enliven {0}.", args: spell);
                        ch.CheckImprove(sk, false, 1);
                        ch.WaitState(sk.waitTime);
                    }
                    else
                    {
                        if(greaterEnlivens.Contains(spell))
                        {
                            foreach (var enliven in greaterEnlivens)
                            {
                                AffectData affect;
                                var enlivenskill = SkillSpell.SkillLookup(enliven);
                                while ((affect = ch.FindAffect(enlivenskill)) != null)
                                    ch.AffectFromChar(affect, AffectRemoveReason.WoreOff);
                            }
                        }
                        ch.Act("You enliven {0}.", args: spell);
                        sk.spellFun(Magic.CastType.None, sk, ch.Level, ch, ch, null, arguments, TargetIsType.targetChar);
                        ch.CheckImprove(sk, true, 1);
                        ch.WaitState(sk.waitTime);
                    }
                    return;
                }
            }
            ch.Act("Enliven which of the following spells : {0}?", args: string.Join(", ", from name in sksp.Concat(greaterEnlivens) where ch.GetSkillPercentageOutOfForm(name) > 1 select name));
        } // end enliven
    } // end ShapeshiftForm
} // end Namespace
