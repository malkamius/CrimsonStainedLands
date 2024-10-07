using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands.World
{
    public class RoomAffectData
    {
        public Character owner;
        public string Name;

        public AffectWhere Where;
        public AffectTypes Type;
        public int Level;
        public int Duration;
        public ApplyTypes Location;
        public int Modifier;
        public List<AffectFlags> Flags = new List<AffectFlags>();
        public SkillSpell skillSpell;
    }
}
