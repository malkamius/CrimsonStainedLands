using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public class SkillSpellGroup
    {
        public static Dictionary<string, SkillSpellGroup> SkillSpellGroups = new Dictionary<string, SkillSpellGroup>();

        public string groupName;
        public List<SkillSpell> skillSpells = new List<SkillSpell>();

        public static void LoadSkillSpellGroups()
        {
            var element = XElement.Load("data\\skillGroups.xml", LoadOptions.PreserveWhitespace);

            foreach(var subElement in element.Elements())
            {
                var name = subElement.GetAttributeValue("Name");
                var skills = subElement.GetAttributeValue("skills");

                var newSkillGroup = new SkillSpellGroup() { groupName = name };
                if(!string.IsNullOrEmpty(skills))
                {
                    string skillName = "";
                    SkillSpell skill;
                    while (!string.IsNullOrEmpty(skills = skills.OneArgument(ref skillName)) || !string.IsNullOrEmpty(skillName))
                        if((skill = SkillSpell.SkillLookup(skillName)) != null)
                            newSkillGroup.skillSpells.Add(skill);
                }
                SkillSpellGroups.Add(name, newSkillGroup);
            }
        }

        public static SkillSpellGroup Lookup(string name)
        {
            foreach (var group in SkillSpellGroups)
                if (group.Key.StringPrefix(name))
                    return group.Value;

            return null;
        }

        public void LearnGroup(Character ch, int percent = 1)
        {
            foreach(var skill in skillSpells)
            {
                if (!ch.Learned.TryGetValue(skill, out LearnedSkillSpell learned) || learned.Percentage < percent)
                    ch.LearnSkill(skill, percent);
            }
        }
    }
}
