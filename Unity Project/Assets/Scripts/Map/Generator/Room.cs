using System;
using System.Collections.Generic;

namespace Map.Generator {
    public class Room : IComparable<Room> {
        private readonly int _roomSize;
        public readonly List<Room> connectedRooms;
        public readonly List<Coordinate> edgeTiles;
        public bool isAccessibleFromMainRoom;

        public Room() {
        }

        public Room(IReadOnlyCollection<Coordinate> tiles, int[,] map) {
            _roomSize = tiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coordinate>();
            foreach (var tile in tiles)
                for (var x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                for (var y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                    if (x != tile.tileX && y != tile.tileY) continue;
                    if (map[x, y] == 1) edgeTiles.Add(tile);
                }
        }

        public int CompareTo(Room otherRoom) {
            return otherRoom._roomSize.CompareTo(_roomSize);
        }

        private void SetAccessibleFromMainRoom() {
            if (isAccessibleFromMainRoom) return;
            isAccessibleFromMainRoom = true;
            foreach (var connectedRoom in connectedRooms) connectedRoom.SetAccessibleFromMainRoom();
        }

        public static void ConnectRooms(Room roomA, Room roomB) {
            if (roomA.isAccessibleFromMainRoom)
                roomB.SetAccessibleFromMainRoom();
            else if (roomB.isAccessibleFromMainRoom) roomA.SetAccessibleFromMainRoom();

            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom) {
            return connectedRooms.Contains(otherRoom);
        }
    }
}