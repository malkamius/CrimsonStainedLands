using CrimsonStainedLands;
using CrimsonStainedLands.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLSMapper
{
    public class RoomWrapper
    {
       // public Label label;
        public int vnum;
        public RoomData room;
        public int x;
        public int y;
        public int z;

        internal Drawer.Box Box { get; set; }
    }

    public class MapRoomOp
    {
        public RoomData room;
        public Direction direction;
        public int x;
        public int y;
        public int z;
    }
}
