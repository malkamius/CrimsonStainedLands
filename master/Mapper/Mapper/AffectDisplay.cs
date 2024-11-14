using CrimsonStainedLands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLSMapper
{
    public class AffectDisplay
    {
        public AffectData affect;
        public AffectDisplay(AffectData affect) 
        { 
            this.affect = affect;
        }

        public string Display { 
            get {
                return affect.location.ToString() + " - " + affect.modifier.ToString();
            } 
        }
    }
}
