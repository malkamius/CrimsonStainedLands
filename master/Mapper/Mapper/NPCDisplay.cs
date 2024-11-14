using CrimsonStainedLands;
using CrimsonStainedLands.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLSMapper
{
    public class NPCDisplay
    {
        public NPCTemplateData NPC;

        public NPCDisplay(NPCTemplateData npc)
        { this.NPC = npc; }

        public string Display { get { return NPC.Vnum + " - " + NPC.Name; } }
    }
}
