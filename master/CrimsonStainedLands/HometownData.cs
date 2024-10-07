using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrimsonStainedLands.World;

namespace CrimsonStainedLands
{
    public class HometownData
    {
        public static List<HometownData> Hometowns = new List<HometownData>();
        public string name { get; set; }

        public AreaData area;

        public int evilSpawnVnum;
        public int goodSpawnVnum;
        public int neutralSpawnVnum;

    }
}
