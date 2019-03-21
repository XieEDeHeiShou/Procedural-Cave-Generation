using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Random = System.Random;

public class MapGenerator : MonoBehaviour {
    private const int SpaceForRoom = 0;
    private const int SpaceForWall = 1;

    private int[,] _map;
    public int height;

    [Range(0, 100)] public int randomFillPercent;

    public string seed;
    public bool useRandomSeed;

    public int width;

    private void Start() {
        GenerateMap();
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) GenerateMap();
    }

    private void GenerateMap() {
        _map = new int[width, height];
        RandomFillMap();

        for (var i = 0; i < 5; i++) SmoothMap();

        ProcessMap();

        const int borderSize = 1;
        var borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        for (var x = 0; x < borderedMap.GetLength(0); x++)
        for (var y = 0; y < borderedMap.GetLength(1); y++)
            if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                borderedMap[x, y] = _map[x - borderSize, y - borderSize];
            else
                borderedMap[x, y] = 1;

        var meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 1);
    }

    private void ProcessMap() {
        var wallRegions = GetRegions(1);
        const int wallThresholdSize = 50;

        foreach (var wallRegion in wallRegions) {
            if (wallRegion.Count >= wallThresholdSize) continue;
            foreach (var tile in wallRegion) _map[tile.tileX, tile.tileY] = 0;
        }

        var roomRegions = GetRegions(0);
        const int roomThresholdSize = 50;
        var survivingRooms = new List<Room>();

        foreach (var roomRegion in roomRegions)
            if (roomRegion.Count < roomThresholdSize)
                foreach (var tile in roomRegion)
                    _map[tile.tileX, tile.tileY] = 1;
            else
                survivingRooms.Add(new Room(roomRegion, _map));

        survivingRooms.Sort();
        survivingRooms[0].isAccessibleFromMainRoom = true;

        ConnectClosestRooms(survivingRooms);
    }

    private void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false) {
        while (true) {
            var roomListA = new List<Room>();
            var roomListB = new List<Room>();

            if (forceAccessibilityFromMainRoom) {
                foreach (var room in allRooms)
                    if (room.isAccessibleFromMainRoom)
                        roomListB.Add(room);
                    else
                        roomListA.Add(room);
            }
            else {
                roomListA = allRooms;
                roomListB = allRooms;
            }

            var bestDistance = 0;
            var bestTileA = new Coordinate(0, 0);
            var bestTileB = new Coordinate(0, 0);
            var bestRoomA = new Room();
            var bestRoomB = new Room();
            var possibleConnectionFound = false;

            foreach (var roomA in roomListA) {
                if (!forceAccessibilityFromMainRoom) {
                    possibleConnectionFound = false;
                    if (roomA.connectedRooms.Count > 0) continue;
                }

                foreach (var roomB in roomListB) {
                    if (roomA == roomB || roomA.IsConnected(roomB)) continue;

                    foreach (var tileA in roomA.edgeTiles)
                    foreach (var tileB in roomB.edgeTiles) {
                        var xDistance = Mathf.Pow(tileA.tileX - tileB.tileX, 2);
                        var yDistance = Mathf.Pow(tileA.tileY - tileB.tileY, 2);
                        var distanceBetweenRooms = (int) (xDistance + yDistance);

                        if (distanceBetweenRooms >= bestDistance && possibleConnectionFound) continue;

                        bestDistance = distanceBetweenRooms;
                        possibleConnectionFound = true;
                        bestTileA = tileA;
                        bestTileB = tileB;
                        bestRoomA = roomA;
                        bestRoomB = roomB;
                    }
                }

                if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
                    CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }

            if (possibleConnectionFound && forceAccessibilityFromMainRoom) {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
                ConnectClosestRooms(allRooms, true);
            }

            if (!forceAccessibilityFromMainRoom) {
                forceAccessibilityFromMainRoom = true;
                continue;
            }

            break;
        }
    }

    private void CreatePassage(Room roomA, Room roomB, Coordinate tileA, Coordinate tileB) {
        Room.ConnectRooms(roomA, roomB);

        var line = GetLine(tileA, tileB);
        foreach (var c in line) DrawCircle(c, 5);
    }

    private void DrawCircle(Coordinate c, int r) {
        for (var x = -r; x <= r; x++)
        for (var y = -r; y <= r; y++) {
            if (x * x + y * y > r * r) continue;
            var drawX = c.tileX + x;
            var drawY = c.tileY + y;
            if (IsInMapRange(drawX, drawY)) _map[drawX, drawY] = 0;
        }
    }

    private static IEnumerable<Coordinate> GetLine(Coordinate from, Coordinate to) {
        var line = new List<Coordinate>();

        var x = from.tileX;
        var y = from.tileY;

        var dx = to.tileX - from.tileX;
        var dy = to.tileY - from.tileY;

        var inverted = false;
        var step = Math.Sign(dx);
        var gradientStep = Math.Sign(dy);

        var longest = Mathf.Abs(dx);
        var shortest = Mathf.Abs(dy);

        if (longest < shortest) {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        var gradientAccumulation = longest / 2;
        for (var i = 0; i < longest; i++) {
            line.Add(new Coordinate(x, y));

            if (inverted)
                y += step;
            else
                x += step;

            gradientAccumulation += shortest;
            if (gradientAccumulation < longest) continue;
            if (inverted)
                x += gradientStep;
            else
                y += gradientStep;

            gradientAccumulation -= longest;
        }

        return line;
    }

    private IEnumerable<List<Coordinate>> GetRegions(int tileType) {
        var regions = new List<List<Coordinate>>();
        var mapFlags = new int[width, height];

        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++) {
            if (mapFlags[x, y] != 0 || _map[x, y] != tileType) continue;
            var newRegion = GetRegionTiles(x, y);
            regions.Add(newRegion);

            foreach (var tile in newRegion) mapFlags[tile.tileX, tile.tileY] = 1;
        }

        return regions;
    }

    private List<Coordinate> GetRegionTiles(int startX, int startY) {
        var tiles = new List<Coordinate>();
        var mapFlags = new int[width, height];
        var tileType = _map[startX, startY];

        var queue = new Queue<Coordinate>();
        queue.Enqueue(new Coordinate(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0) {
            var tile = queue.Dequeue();
            tiles.Add(tile);

            for (var x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            for (var y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                if (!IsInMapRange(x, y) || y != tile.tileY && x != tile.tileX) continue;
                if (mapFlags[x, y] != 0 || _map[x, y] != tileType) continue;

                mapFlags[x, y] = 1;
                queue.Enqueue(new Coordinate(x, y));
            }
        }

        return tiles;
    }

    private bool IsInMapRange(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    /// <summary>
    ///     Fill _map with 1 and 0, restrict count(1)/count(0) ~= randomFillPercent
    /// </summary>
    private void RandomFillMap() {
        if (useRandomSeed) seed = Time.time.ToString(CultureInfo.CurrentCulture);

        var pseudoRandom = new Random(seed.GetHashCode());

        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                _map[x, y] = 1;
            else
                _map[x, y] = pseudoRandom.Next(0, 100) < randomFillPercent ? 1 : 0;
    }

    /// <summary>
    ///     Smooth the _map: replace the center number with 1 if it is surrounded by more than 4, otherwise replace with 0.
    /// </summary>
    private void SmoothMap() {
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++) {
            var neighbourWallTiles = GetSurroundingWallCount(x, y);

            if (neighbourWallTiles > 4)
                _map[x, y] = 1;
            else if (neighbourWallTiles < 4)
                _map[x, y] = 0;
        }
    }

    private int GetSurroundingWallCount(int gridX, int gridY) {
        var wallCount = 0;
        for (var neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        for (var neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            if (IsInMapRange(neighbourX, neighbourY)) {
                if (neighbourX != gridX || neighbourY != gridY) wallCount += _map[neighbourX, neighbourY];
            }
            else {
                wallCount++;
            }

        return wallCount;
    }
}