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
        static void DisplayMap(Character ch, RoomData room)
        {
            StringBuilder buffer = new StringBuilder();

            var Width = 26;
            var Height = 6;

            var map = new Dictionary<Point, string>();

            int X = Width / 2, Y = Height / 2;

            MapRoom(map, room, X, Y, Width, Height);

            for (var y = 0; y < Height * 3; y++)
            {
                for (var x = 0; x < Width * 3; x++)
                {
                    if (map.TryGetValue(new Point(x, y), out var str))
                        buffer.Append(CrimsonStainedLands.Extensions.color.colorString(str));
                    else
                        buffer.Append(CrimsonStainedLands.Extensions.color.colorString("\\x "));
                }
                buffer.AppendLine();
            }
            buffer.AppendLine();
            ch.send(buffer.ToString());
        }

        static void MapRoom(Dictionary<Point, string> map, RoomData room, int x, int y, int width, int height)
        {
            var reversedirectionoffsets = new int[,] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
            map[new Point(x, y)] = " ";
            for (int exit = 0; exit < 4; exit++)
            {
                if (room.exits[exit] != null && room.exits[exit].destination != null)
                {
                    var subx = x + reversedirectionoffsets[exit, 0];
                    var suby = y + reversedirectionoffsets[exit, 1];
                    if (subx >= 0 && subx < width && suby >= 0 && suby < height && !map.ContainsKey(new Point(subx, suby)))
                        MapRoom(map, room.exits[exit].destination, subx, suby, width, height);

                }

                for (var yoffset = 0; yoffset < 3; yoffset++)
                {
                    for (var xoffset = 0; xoffset < 3; xoffset++)
                    {
                        AddMapChar(map, room, x, y, xoffset, yoffset, width, height);
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
                        AsciiString = "\\B^";
                    else if (UpExit.flags.ISSET(ExitFlags.Locked))
                        AsciiString = "\\Y^";
                    else
                        AsciiString = "\\D^";
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
                        AsciiString = "|";
                    else if (pExit.flags.ISSET(ExitFlags.Locked))
                        AsciiString = "\\Y-";
                    else
                        AsciiString = "\\D-";
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
                        AsciiString = "\\Y-";
                    else
                        AsciiString = "\\D-";
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
                        AsciiString = "-";
                    else if (pExit.flags.ISSET(ExitFlags.Locked))
                        AsciiString = "\\Y|";
                    else
                        AsciiString = "\\D|";
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
                        AsciiString = "\\Y|";
                    else
                        AsciiString = "\\D|";
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
                AsciiString = "\\y*\\x";
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
                    case SectorTypes.Hills: return "\\Gn";
                    case SectorTypes.Desert: return "\\Y+";
                    case SectorTypes.City: return "\\W+";
                    case SectorTypes.Underground: return "\\D+";
                    case SectorTypes.Mountain: return "\\y^";
                    case SectorTypes.Inside: return "\\W+";
                    case SectorTypes.Field: return "\\g\"";
                    case SectorTypes.Forest: return "\\G+";
                    case SectorTypes.Swim:
                        if (Utility.Random(1, 3) < 2)
                            return "\\C~";
                        else
                            return "\\c~";
                    case SectorTypes.NoSwim:
                        if (Utility.Random(1, 3) < 2)
                            return "\\B~";
                        else
                            return "\\b~";
                    default: return " ";
                }
            }
            else
            {
                switch (room.sector)
                {
                    case SectorTypes.Desert:
                    case SectorTypes.City:
                    case SectorTypes.Mountain:
                    case SectorTypes.Underground:
                    case SectorTypes.Inside: return "\\W#";
                    case SectorTypes.Hills:
                    case SectorTypes.Field: return "\\G\"";
                    case SectorTypes.Forest: return "\\g@";
                    case SectorTypes.Swim:
                        if (Utility.Random(1, 3) < 2)
                            return "\\C~";
                        else
                            return "\\c~";
                    case SectorTypes.Ocean:
                    case SectorTypes.Underwater:
                    case SectorTypes.NoSwim:
                        if (Utility.Random(1, 3) < 2)
                            return "\\B~";
                        else
                            return "\\b~";
                    default: return " ";
                }
            }
        }
    }
}
