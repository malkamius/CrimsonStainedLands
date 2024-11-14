using CrimsonStainedLands;

namespace CLSMapper
{
    internal class SpellDisplay
    {
        public ItemSpellData spell;

        public SpellDisplay(ItemSpellData spell)
        {
            this.spell = spell;
        }

        public string Display
        {
            get
            {
                return spell.SpellName + " - " + spell.Level.ToString();
            }
        }
    }
}