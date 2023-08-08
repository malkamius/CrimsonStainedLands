using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CrimsonStainedLands
{
    public class ItemSpellData
    {
        public int Level;
        public string SpellName;
        public SkillSpell Spell;

        public ItemSpellData(int level, string spellName )
        {
            Level = level;
            SpellName = spellName;
            this.Spell = SkillSpell.SkillLookup(SpellName);
        }

        public ItemSpellData(XElement element)
        {
            this.Level = element.GetAttributeValueInt("Level");
            this.SpellName = element.GetAttributeValue("SpellName");
            this.Spell = SkillSpell.SkillLookup(SpellName);

            if(Spell == null ) {
                Game.log("Unknown spell in ItemSpellData: {0}", SpellName);
            }
        }

        public XElement Eelement { get {
                return new XElement("Spell", new XAttribute("Level", Level), new XAttribute("SpellName", SpellName));
            } }
    }
}
