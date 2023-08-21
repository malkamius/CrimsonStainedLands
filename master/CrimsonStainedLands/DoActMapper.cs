/***************************************************************************
 * Detailed, 3x3 display ASCII automapper for ROM MUDs.                    *
 *  ---------------------------------------------------------------------  *
 * Some of the code within is indirectly derived from mlk's asciimap.c.    *
 * Thanks go out to him for sharing his code, as things I learned working  *
 * with it in my earlier coding days stayed with me and went into this     *
 * snippet.  A portion of mlk's header has been included below out of      *
 * respect.                                                                *
 *  ---------------------------------------------------------------------  *
 * This code may be used freely, all I ask is that you send any feedback   *
 * or bug reports you come up with my way.                                 *
 *                                         -- Midboss (sfritzjr@gmail.com) *
 ***************************************************************************/

/************************************************************************/
/* mlkesl@stthomas.edu  =====>  Ascii Automapper utility                */
/* Let me know if you use this. Give a newbie some _credit_,            */
/* at least I'm not asking how to add classes...                        */
/* Also, if you fix something could ya send me mail about, thanks       */
/* PLEASE mail me if you use this or like it, that way I will keep it up*/
/************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrimsonStainedLands;
using CrimsonStainedLands.Extensions;
using System.Drawing;
using System.Windows.Forms;

namespace CrimsonStainedLands
{
    public class DoActMapper
    {
        public static void DoMap(Character ch, string arguments)
        {
            if (ch.Room != null)
                DisplayMap(ch, ch.Room);
            else
                ch.send("You aren't in a room.\n\r");
        }

        private class MapRoom
        {
            public Dictionary<Point, string> map;
            public HashSet<RoomData> rooms;
            public Dictionary<char, string> POIs;
            public RoomData room;
            public int x;
            public int y;
            public int width;
            public int height;
            public Queue<MapRoom> queue;

            public MapRoom(Dictionary<Point, string> map, HashSet<RoomData> rooms, Dictionary<char, string> POI, RoomData room, int x, int y, int width, int height, Queue<MapRoom> queue)
            {
                this.map = map;
                this.rooms = rooms;
                this.POIs = POI;
                this.room = room;
                this.x = x;
                this.y = y;
                this.width = width;
                this.height = height;
                this.queue = queue;
            }

            public MapRoom(MapRoom copyfrom, RoomData room, int x, int y)
            {
                this.map = copyfrom.map;
                this.rooms = copyfrom.rooms;
                this.POIs = copyfrom.POIs;
                this.room = room;
                this.x = x;
                this.y = y;
                this.width = copyfrom.width;
                this.height = copyfrom.height;
                this.queue = copyfrom.queue;
            }
        }
        
        static void DisplayMap(Character ch, RoomData room)
        {
            StringBuilder buffer = new StringBuilder();
            if(ch.MapLastDisplayed != default(DateTime) && DateTime.Now - ch.MapLastDisplayed < TimeSpan.FromSeconds(5))
            {
                ch.send("Please wait a few seconds before displaying the map again.\n\r");
                return;
            }    
            ch.MapLastDisplayed = DateTime.Now;

            var Width = 26;
            var Height = 12;

            var map = new Dictionary<Point, string>();
            var POIs = new Dictionary<char, string>();

            int X = Width / 2, Y = Height / 2;
            var ToMap = new Queue<MapRoom>();
            
            ToMap.Enqueue(new MapRoom(map, new HashSet<RoomData>(), POIs, room, X, Y, Width, Height, ToMap));

            while (ToMap.Count > 0)
                MapOneRoom(ToMap.Dequeue());

            for (var y = 0; y < Height * 3; y++)
            {
                for (var x = 0; x < Width * 3; x++)
                {
                    if (map.TryGetValue(new Point(x, y), out var str))
                        buffer.Append(str);
                    else
                        buffer.Append("\\x ");
                }
                buffer.AppendLine();
            }
            buffer.AppendLine();
            foreach (var POI in POIs)
            {
                buffer.AppendLine(string.Format("{0,5} :: {1}", POI.Key, POI.Value));
            }
            ch.send(buffer.ToString());
        }

        static void MapOneRoom(MapRoom arguments)
        {
            var reversedirectionoffsets = new int[,] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
            arguments.map[new Point(arguments.x * 3, arguments.y * 3)] = " ";
            arguments.rooms.Add(arguments.room);
            for (int exit = 0; exit < 4; exit++)
            {
                if (arguments.room.exits[exit] != null && arguments.room.exits[exit].destination != null)
                {
                    var subx = arguments.x + reversedirectionoffsets[exit, 0];
                    var suby = arguments.y + reversedirectionoffsets[exit, 1];
                    if (subx >= 0 && subx < arguments.width && suby >= 0 && suby < arguments.height && !arguments.rooms.Contains(arguments.room.exits[exit].destination) && (!arguments.map.ContainsKey(new Point(subx * 3, suby * 3)) || arguments.map[new Point(subx * 3, suby * 3)] == " "))
                        arguments.queue.Enqueue(new MapRoom(arguments, arguments.room.exits[exit].destination, subx, suby));
                        //MapRoom(map, rooms, POI, room.exits[exit].destination, subx, suby, width, height);

                }

                
            }

            for (var yoffset = 0; yoffset < 3; yoffset++)
            {
                for (var xoffset = 0; xoffset < 3; xoffset++)
                {
                    if (!arguments.map.TryGetValue(new Point(arguments.x * 3 + xoffset, arguments.y * 3 + yoffset), out var existing) || existing == " ")
                        AddMapChar(arguments.map, arguments.room, arguments.x, arguments.y, xoffset, yoffset, arguments.width, arguments.height);


                }
            }

            foreach (var ch in arguments.room.Characters)
            {
                if (ch.Flags.ISSET(ActFlags.Practice, ActFlags.Train, ActFlags.Shopkeeper, ActFlags.Healer))
                {
                    char POIChar;
                    if (arguments.POIs.Count > 8 && arguments.POIs.Count < 'Z' - 'A' + 9)
                    {
                        POIChar = (char)('A' + (arguments.POIs.Count - 9));
                    }
                    else if (arguments.POIs.Count < 9)
                    {
                        POIChar = (char)('1' + (arguments.POIs.Count));


                    }
                    else
                        POIChar = '\0';

                    if (POIChar != '\0')
                    {
                        arguments.map[new Point(arguments.x * 3 + 1, arguments.y * 3 + 1)] = POIChar.ToString();
                        arguments.POIs[POIChar] = ch.GetShortDescription(null);
                    }
                }
            }
        }

        static void AddMapChar(Dictionary<Point, string> map, RoomData room, int x, int y, int xoffset, int yoffset, int width, int height)
        {
            var UpExit = room.exits[(int)Direction.Up];
            var DownExit = room.exits[(int)Direction.Down];

            var roomWall = GetRoomAsciiCharacter(room, true);
            var roomFloor = GetRoomAsciiCharacter(room, false);
            ExitData pExit;
            RoomData yRoom;
            RoomData xRoom;

            string AsciiString = " ";
            // Top left
            if (yoffset == 0 && xoffset == 0)
            {
                if (UpExit != null && UpExit.destination != null &&
                (!UpExit.flags.ISSET(ExitFlags.Hidden) &&
                (!UpExit.flags.ISSET(ExitFlags.HiddenWhileClosed) || !UpExit.flags.ISSET(ExitFlags.Closed))))
                {
                    if (!UpExit.flags.ISSET(ExitFlags.Closed))
                        AsciiString = "\\B^\\x";
                    else if (UpExit.flags.ISSET(ExitFlags.Locked))
                        AsciiString = "\\Y^\\x";
                    else
                        AsciiString = "\\D^\\x";
                }
                else if ((pExit = room.exits[(int)Direction.West]) == null
                    || (xRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit = room.exits[(int)Direction.North]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden))
                    AsciiString = roomWall;

                //Pretty up fields and large halls by removing unsightly corners.
                else if (((pExit = room.exits[(int)Direction.North]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = room.exits[(int)Direction.West]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.East]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.North]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.South]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.West]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden)))
                    AsciiString = roomWall;

                else
                    AsciiString = roomWall;
            }
            // Bottom right
            else if (yoffset == 2 && xoffset == 2)
            {

                if (DownExit != null && DownExit.destination != null &&
                (!DownExit.flags.ISSET(ExitFlags.Hidden) &&
                (!DownExit.flags.ISSET(ExitFlags.HiddenWhileClosed) || !DownExit.flags.ISSET(ExitFlags.Closed))))
                {
                    if (!DownExit.flags.ISSET(ExitFlags.Closed))
                        AsciiString = "\\Bv";
                    else if (DownExit.flags.ISSET(ExitFlags.Locked))
                        AsciiString = "\\Yv";
                    else
                        AsciiString = "\\Dv";
                }
                else if ((pExit = room.exits[(int)Direction.East]) == null
                    || (xRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit = room.exits[(int)Direction.South]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden))
                    AsciiString = roomWall;

                //Pretty up fields and large halls by removing unsightly corners.
                else if (((pExit = room.exits[(int)Direction.South]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = room.exits[(int)Direction.East]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.West]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.South]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.North]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.East]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden)))
                    AsciiString = roomWall;

                else
                    AsciiString = roomWall;
            }
            else if (yoffset == 0 && xoffset == 2) // top right
            {
                if ((pExit = room.exits[(int)Direction.East]) == null
                    || (xRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit = room.exits[(int)Direction.North]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden))
                    AsciiString = roomWall;

                //Pretty up fields and large halls by removing unsightly corners.
                else if (((pExit = room.exits[(int)Direction.North]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = room.exits[(int)Direction.East]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.West]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.North]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.South]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.East]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden)))
                    AsciiString = roomWall;

                else
                    AsciiString = roomWall;
            }
            // Bottom left
            else if (yoffset == 2 && xoffset == 0)
            {

                if ((pExit = room.exits[(int)Direction.West]) == null
                    || (xRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit = room.exits[(int)Direction.South]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden))
                    AsciiString = roomWall;

                //Pretty up fields and large halls by removing unsightly corners.
                else if (((pExit = room.exits[(int)Direction.South]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = room.exits[(int)Direction.West]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.East]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.South]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.North]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.West]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden)))
                    AsciiString = roomWall;

                else
                    AsciiString = roomWall;
            }
            else if (xoffset == 1 && yoffset == 0) //Upper Mid
            {

                //Make sure map rooms aren't null.
                if ((pExit = room.exits[(int)Direction.North]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit.flags.ISSET(ExitFlags.HiddenWhileClosed) && pExit.flags.ISSET(ExitFlags.Closed)))
                    AsciiString = roomWall;

                else if ((pExit = room.exits[(int)Direction.North]) != null && pExit.destination != null
                        && (pExit = yRoom.exits[(int)Direction.South]) != null)
                {
                    pExit = room.exits[(int)Direction.North];

                    //Display doors.  Grey are locked, off white are just closed.
                    if (!pExit.flags.ISSET(ExitFlags.Closed))
                        AsciiString = "|\\x";
                    else if (pExit.flags.ISSET(ExitFlags.Locked))
                        AsciiString = "\\Y-\\x";
                    else
                        AsciiString = "\\D-\\x";
                }
                else
                    AsciiString = roomWall;
            }
            else if (xoffset == 1 && yoffset == 2) //Bottom Mid
            {

                //Make sure map rooms aren't null.
                if ((pExit = room.exits[(int)Direction.South]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit.flags.ISSET(ExitFlags.HiddenWhileClosed) && pExit.flags.ISSET(ExitFlags.Closed)))
                    AsciiString = roomWall;

                else if ((pExit = room.exits[(int)Direction.South]) != null && pExit.destination != null
                        && (pExit = yRoom.exits[(int)Direction.North]) != null)
                {
                    pExit = room.exits[(int)Direction.South];

                    //Display doors.  Grey are locked, off white are just closed.
                    if (!pExit.flags.ISSET(ExitFlags.Closed))
                        AsciiString = "|";
                    else if (pExit.flags.ISSET(ExitFlags.Locked))
                        AsciiString = "\\Y-\\x";
                    else
                        AsciiString = "\\D-\\x";
                }
                else
                    AsciiString = roomWall;
            }
            else if (xoffset == 0 && yoffset == 1) //Left Mid
            {

                //Make sure map rooms aren't null.
                if ((pExit = room.exits[(int)Direction.West]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit.flags.ISSET(ExitFlags.HiddenWhileClosed) && pExit.flags.ISSET(ExitFlags.Closed)))
                    AsciiString = roomWall;

                else if ((pExit = room.exits[(int)Direction.West]) != null && pExit.destination != null
                        && (pExit = yRoom.exits[(int)Direction.East]) != null)
                {
                    pExit = room.exits[(int)Direction.West];

                    //Display doors.  Grey are locked, off white are just closed.
                    if (!pExit.flags.ISSET(ExitFlags.Closed))
                        AsciiString = "-\\x";
                    else if (pExit.flags.ISSET(ExitFlags.Locked))
                        AsciiString = "\\Y|\\x";
                    else
                        AsciiString = "\\D|\\x";
                }
                else
                    AsciiString = roomWall;
            }
            else if (xoffset == 2 && yoffset == 1) //Right Mid
            {

                //Make sure map rooms aren't null.
                if ((pExit = room.exits[(int)Direction.East]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit.flags.ISSET(ExitFlags.HiddenWhileClosed) && pExit.flags.ISSET(ExitFlags.Closed)))
                    AsciiString = roomWall;

                else if ((pExit = room.exits[(int)Direction.East]) != null && pExit.destination != null
                        && (pExit = yRoom.exits[(int)Direction.West]) != null)
                {
                    pExit = room.exits[(int)Direction.East];

                    //Display doors.  Grey are locked, off white are just closed.
                    if (!pExit.flags.ISSET(ExitFlags.Closed))
                        AsciiString = "-";
                    else if (pExit.flags.ISSET(ExitFlags.Locked))
                        AsciiString = "\\Y|\\x";
                    else
                        AsciiString = "\\D|\\x";
                }
                else
                    AsciiString = roomWall;
            }
            else if (xoffset == 2 && yoffset == 0) //Upper Right
            {

                //Make sure map rooms aren't null.
                if ((pExit = room.exits[(int)Direction.East]) == null
                    || (xRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit = room.exits[(int)Direction.North]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden))
                    AsciiString = roomWall;

                //Pretty up fields and large halls by removing unsightly corners.
                else if (((pExit = room.exits[(int)Direction.North]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = room.exits[(int)Direction.East]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.West]) != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.North]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.South]) != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.East]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden)))
                    AsciiString = roomFloor;

                else
                    AsciiString = roomWall;
            }
            else if (xoffset == 0 && yoffset == 2) //Bottom Left
            {

                //Make sure map rooms aren't null.
                if ((pExit = room.exits[(int)Direction.West]) == null
                    || (xRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden)
                    || (pExit = room.exits[(int)Direction.South]) == null
                    || (yRoom = pExit.destination) == null
                    || pExit.flags.ISSET(ExitFlags.Hidden))
                    AsciiString = roomWall;

                //Pretty up fields and large halls by removing unsightly corners.
                else if (((pExit = room.exits[(int)Direction.South]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = room.exits[(int)Direction.West]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.East]) != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = xRoom.exits[(int)Direction.South]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.North]) != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden))
                        || ((pExit = yRoom.exits[(int)Direction.West]) != null && pExit.destination != null
                            && !pExit.flags.ISSET(ExitFlags.Door)
                            && !pExit.flags.ISSET(ExitFlags.Hidden)))
                    AsciiString = roomFloor;

                else
                    AsciiString = roomWall;
            }
            else if (xoffset == 1 && yoffset == 1 && x == width / 2 && y == height / 2)
                AsciiString = "\\Y*\\x";
            else
                AsciiString = roomFloor;

            map[new Point(x * 3 + xoffset, y * 3 + yoffset)] = AsciiString;
        }

        static string GetRoomAsciiCharacter(RoomData room, bool isWall)
        {
            if (room == null)
                return " ";
            else if (!isWall)
            {
                switch (room.sector)
                {
                    case SectorTypes.Hills: return "\\Gn\\x";
                    case SectorTypes.Desert: return "\\Y+\\x";
                    case SectorTypes.City: return "\\W+\\x";
                    case SectorTypes.Underground: return "\\D+\\x";
                    case SectorTypes.Mountain: return "\\y^\\x";
                    case SectorTypes.Trail:
                    case SectorTypes.Road:
                    case SectorTypes.Inside: return "\\W+\\x";
                    case SectorTypes.Field: return "\\g\"\\x";
                    case SectorTypes.Forest: return "\\G+\\x";
                    case SectorTypes.River:
                    case SectorTypes.Swim:
                        if (Utility.Random(1, 3) < 2)
                            return "\\C~\\x";
                        else
                            return "\\c~\\x";
                    case SectorTypes.NoSwim:
                        if (Utility.Random(1, 3) < 2)
                            return "\\B~\\x";
                        else
                            return "\\b~\\x";
                    default: return " ";
                }
            }
            else
            {
                switch (room.sector)
                {
                    case SectorTypes.Desert:
                    case SectorTypes.Road:
                    case SectorTypes.Trail:
                    case SectorTypes.City:
                    case SectorTypes.Mountain:
                    case SectorTypes.Underground:
                    case SectorTypes.Inside: return "\\W#\\x";
                    case SectorTypes.Hills:
                    case SectorTypes.Field: return "\\G\"\\x";
                    case SectorTypes.Forest: return "\\g@\\x";
                    case SectorTypes.River:
                    case SectorTypes.Swim:
                        if (Utility.Random(1, 3) < 2)
                            return "\\C~\\x";
                        else
                            return "\\c~\\x";
                    case SectorTypes.Ocean:
                    case SectorTypes.Underwater:
                    case SectorTypes.NoSwim:
                        if (Utility.Random(1, 3) < 2)
                            return "\\B~\\x";
                        else
                            return "\\b~\\x";
                    default: return " ";
                }
            }
        }
    }
}