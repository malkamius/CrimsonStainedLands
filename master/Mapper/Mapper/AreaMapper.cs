using CrimsonStainedLands;
using CrimsonStainedLands.World;
using CLSMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper
{
    public class AreaMapper
    {
        public Dictionary<RoomData, (int X, int Y, int Zone)> roomPositions = new Dictionary<RoomData, (int X, int Y, int Zone)>();
        private HashSet<(int X, int Y, int Zone)> occupiedPositions = new HashSet<(int X, int Y, int Zone)>();
        private int currentZone = 0;
        private AreaData? areaData = null;
        private bool allRooms = false;

        public void MapRooms(AreaData area)
        {
            areaData = area;
            allRooms = false;
            currentZone = 0;
            var visited = new HashSet<RoomData>();
            foreach (var room in area.Rooms.Values)
            {
                if (!visited.Contains(room) && !roomPositions.ContainsKey(room))
                {
                    MapComponent(room, visited);
                    currentZone++;
                }
            }
        }

        public void MapRooms(AreaData area, IEnumerable<RoomData> rooms)
        {
            areaData = area;
            allRooms = true;
            currentZone = 0;
            var visited = new HashSet<RoomData>();

            foreach (var room in area.Rooms.Values.Concat(rooms.OrderByDescending(r => r.Vnum)))
            {
                if (!visited.Contains(room) && !roomPositions.ContainsKey(room))
                {
                    MapComponent(room, visited);
                    currentZone++;
                }
            }
        }

        private void MapComponent(RoomData startRoom, HashSet<RoomData> visited)
        {
            var queue = new Queue<(RoomData room, int x, int y)>();
            queue.Enqueue((startRoom, 0, 0));

            while (queue.Count > 0)
            {
                var (currentRoom, currentX, currentY) = queue.Dequeue();

                if (visited.Contains(currentRoom) || roomPositions.ContainsKey(currentRoom))
                    continue;

                visited.Add(currentRoom);

                PlaceRoom(currentRoom, currentX, currentY, currentZone);

                if (areaData == currentRoom.Area || allRooms)
                {
                    foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                    {
                        if (currentRoom.GetExit(direction, out var exit) && exit.destination != null && !roomPositions.ContainsKey(exit.destination))
                        {
                            var (newX, newY) = GetNewPosition(currentX, currentY, direction);//, !allRooms || exit.destination.Area == currentRoom.Area? 1 : 50);
                            ResolveConflict(ref newX, ref newY, currentZone, direction);

                            queue.Enqueue((exit.destination, newX, newY));
                        }
                    }
                }
            }
        }

        private void PlaceRoom(RoomData room, int x, int y, int zone)
        {
            roomPositions[room] = (x, y, zone);
            occupiedPositions.Add((x, y, zone));
        }

        private (int X, int Y) GetNewPosition(int x, int y, Direction direction, int distance = 1)
        {
            return direction switch
            {
                Direction.North => (x, y - distance),
                Direction.East => (x + distance, y),
                Direction.South => (x, y + distance),
                Direction.West => (x - distance, y),
                Direction.Up => (x + distance, y - distance),
                Direction.Down => (x - distance, y + distance),
                _ => throw new ArgumentException("Invalid direction")
            };
        }

        private (int X, int Y) GetVector(Direction direction)
        {
            return direction switch
            {
                Direction.North => (0, -1),
                Direction.East => (1, 0),
                Direction.South => (0, 1),
                Direction.West => (-1, 0),
                Direction.Up => (1, -1),
                Direction.Down => (-1, 1),
                _ => throw new ArgumentException("Invalid direction")
            };
        }

        private void ResolveConflict(ref int x, ref int y, int zone, Direction direction)
        {
            while (occupiedPositions.Contains((x, y, zone)))
            {
                var vector = GetVector(direction);
                x += vector.X;
                y += vector.Y;
            }
        }
    }
}