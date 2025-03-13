using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public class GuildData
    {
        public class GuildTitle
        {
            public string MaleTitle { get; set; }
            public string FemaleTitle { get; set; }
        }
        public static List<GuildData> Guilds = new List<GuildData>();
        public string name;
        public string whoName;
        public List<PcRace> races = new List<PcRace>();
        public string guildGroup;
        public string guildBasicsGroup;
        public List<Alignment> alignments = new List<Alignment>();
        public int startingWeapon;
        public int HitpointGain = 7;
        public int HitpointGainMax = 10;
        public int THAC0 = 20;
        public int THAC032 = -4;

        public Magic.CastType CastType = Magic.CastType.None;
        public Dictionary<int, GuildTitle> Titles = new Dictionary<int, GuildTitle>();
        public static void LoadGuilds()
        {
            var element = XElement.Load(System.IO.Path.Join(Settings.DataPath, "guilds.xml"), LoadOptions.PreserveWhitespace);

            foreach (var guildElement in element.Elements())
            {

                if (guildElement.HasAttribute("Name") && guildElement.HasAttribute("Races"))
                {
                    var guild = new GuildData();
                    guild.name = guildElement.GetAttributeValue("Name");
                    guild.whoName = guildElement.GetAttributeValue("WhoName");
                    guild.guildGroup = guildElement.GetAttributeValue("GuildGroup");
                    guild.guildBasicsGroup = guildElement.GetAttributeValue("GuildBasicsGroup");
                    guild.startingWeapon = guildElement.GetAttributeValueInt("StartingWeapon", 0);
                    guild.HitpointGain = guildElement.GetAttributeValueInt("HitpointGain", 7);
                    guild.HitpointGainMax = guildElement.GetAttributeValueInt("HitpointGainMax", 10);
                    guild.THAC0 = guildElement.GetAttributeValueInt("THAC0");
                    guild.THAC032 = guildElement.GetAttributeValueInt("THAC032");
                    Utility.GetEnumValues<Alignment>(guildElement.GetAttributeValue("alignments"), ref guild.alignments);
                    Utility.GetEnumValue(guildElement.GetAttributeValue("CastType"), ref guild.CastType, Magic.CastType.None);
                    var raceStrings = guildElement.GetAttributeValue("Races");
                    string raceName = "";
                    PcRace race;
                    while (!string.IsNullOrEmpty(raceStrings = raceStrings.OneArgument(ref raceName)) || !string.IsNullOrEmpty(raceName))
                        if ((race = PcRace.GetRace(raceName)) != null)
                            guild.races.Add(race);

                    if (System.IO.File.Exists(System.IO.Path.Join(Settings.GuildsPath, guild.name + "-titles.xml")))
                    {
                        var titleselement = XElement.Load(System.IO.Path.Join(Settings.GuildsPath, guild.name + "-titles.xml"));

                        foreach(var title in titleselement.Elements("Title"))
                        {
                            var level = title.GetAttributeValueInt("Level");
                            guild.Titles[level] = new GuildTitle() { MaleTitle = title.GetAttributeValue("Male"), FemaleTitle = title.GetAttributeValue("Female") };
                        }
                    }
                    Guilds.Add(guild);
                    Game.log("Loaded {0} with {1} races", guild.name, guild.races.Count);
                }
            }
        }

        internal static GuildData GuildLookup(string v)
        {
            foreach (var guild in Guilds)
                if (guild.name.IsName(v))
                    return guild;

            return null;
        }

        internal static void WriteGuildSkillsHtml()
        {
            var html = new StringBuilder();

            html.AppendLine("<html><body>");

            html.AppendLine("<table><tr>");
            foreach (var guild in Guilds)
            {
                html.Append("<td><a href=#" + guild.name + ">" + guild.name + "</a></td>");
            }
            html.AppendLine("</tr></table>");

            foreach (var guild in Guilds)
            {
                html.AppendLine("<table width=\"100%\"><tr><td><a name=\"" + guild.name + "\"</a>" + guild.name + "</td><td>" + guild.name + " Continued</td></tr>");
                var skills = (from sk in SkillSpell.Skills where sk.Value.skillLevel.ContainsKey(guild.name) orderby sk.Value.skillLevel[guild.name] select sk);
                var total = skills.Count();
                int count = 0;
                int lastLevel = 0;
                bool secondcolumn = false;
                html.AppendLine("<tr valign=\"top\"><td style=\"width: 50%;\"><font>");
                foreach (var skill in skills)
                {
                    count++;
                    if (skill.Value.skillLevel[guild.name] != lastLevel)
                    {
                        if (secondcolumn == false && count > total / 2)
                        {
                            html.AppendLine("</font></td><td style=\"width: 50%;\">");
                            secondcolumn = true;
                            html.AppendLine("<u><b>Level " + skill.Value.skillLevel[guild.name] + "</b></u><br>");
                        }
                        else if(lastLevel == 0)
                            html.AppendLine("<u><b>Level " + skill.Value.skillLevel[guild.name] + "</b></u><br>");
                        else
                            html.AppendLine("<br><u><b>Level " + skill.Value.skillLevel[guild.name] + "</b></u><br>");

                    }

                    var skilltype = skill.Value.SkillTypes.Contains(SkillSpellTypes.Skill) ? "Skill:" :
                        guild.CastType == Magic.CastType.Sing && skill.Value.SkillTypes.Contains(SkillSpellTypes.Song) ? "Song:" :
                        guild.CastType == Magic.CastType.Commune && skill.Value.SkillTypes.Contains(SkillSpellTypes.Commune) ? "Supplication:" :
                        guild.CastType == Magic.CastType.Cast && skill.Value.SkillTypes.Contains(SkillSpellTypes.Spell) ? "Spell:" : "Unknown:";
                    var prereq = "";

                    if (!skill.Value.Prerequisites.ISEMPTY())
                        prereq = " requires " + skill.Value.Prerequisites + " " + skill.Value.PrerequisitePercentage;
                    else if (skill.Value.guildPreRequisiteSkill.ContainsKey(guild.name) && skill.Value.guildPreRequisiteSkillPercentage.ContainsKey(guild.name))
                        prereq = " requires " +  skill.Value.guildPreRequisiteSkill[guild.name] + " " + skill.Value.guildPreRequisiteSkillPercentage[guild.name];

                    html.AppendLine("<u><b>" + skilltype + "</b></u> " + skill.Value.name + " " + prereq + "<br>");

                    lastLevel = skill.Value.skillLevel[guild.name];
                }
                if(guild.name == "shapeshifter")
                {
                    html.AppendLine("</font></td></tr></table><br><br>");
                    html.AppendLine("<tr valign=\"top\"><td style=\"width: 50%;\"><font>");
                    ShapeshiftForm.FormTier lastTier = ShapeshiftForm.FormTier.Tier1;
                    ShapeshiftForm.FormType lastType = ShapeshiftForm.FormType.None;
                    var forms = from fsk in SkillSpell.Skills where fsk.Value.SkillTypes.Contains(SkillSpellTypes.Form) && ShapeshiftForm.Forms.Any(f => f.FormSkill == fsk.Value) select new { Skill = fsk.Value, Form = (from frm in ShapeshiftForm.Forms where frm.FormSkill == fsk.Value select frm).FirstOrDefault() };
                    foreach (var form in (from frm in forms orderby frm.Form.Type, frm.Form.Tier select frm))
                    {
                        if(lastTier != form.Form.Tier || lastType != form.Form.Type)
                        {
                            html.AppendLine("<u><b>Type: " + form.Form.Type.ToString() + "</b></u> Tier: " + form.Form.Tier.ToString() + "<br>");
                        }
                        else
                        {
                            html.AppendLine(form.Form.Name + "<br>");
                        }
                        lastTier = form.Form.Tier; 
                        lastType = form.Form.Type;
                    }
                }
                html.AppendLine("</font></td></tr></table><br><br><hr width=\"50%\" /><br><br>");
            }

            html.AppendLine("</body></html>");
            System.IO.File.WriteAllText("guild_skilllevels.html", html.ToString());
        }
    }
}
