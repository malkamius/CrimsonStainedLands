using CrimsonStainedLands;
using CrimsonStainedLands.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLSMapper
{
    public class RoomDisplay
    {
        public RoomData Room;

        public RoomDisplay(RoomData room)
        { this.Room = room; }

        public string Display { get { return Room.Vnum + " - " + Room.Name; } }
    }
}
