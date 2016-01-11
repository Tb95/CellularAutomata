using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

    [Range(0, 1000)]
    public int width;
    [Range(0, 1000)]
    public int height;
    [Range(0, 100)]
    public float fillPercentage;
    [Range(0, 10)]
    public int smoothAmount;
    [Range(0, 10)]
    public int borderSize;
    [Range(0, 100)]
    public int smallestWallRegion;
    [Range(0, 100)]
    public int smallestRoomRegion;
    [Range(1, 5)]
    public int passagewayRadius;
    [Range(1, 100)]
    public float squareSize;
    [Range(1, 100)]
    public float wallHeight;
    public string seed;
    public bool randomSeed;
    public bool is2D;
    public GameObject player2D;
    public GameObject player3D;

    int[,] map;
    Coord spawnTile;
    GameObject player;

	void Start () {
        if(map == null)
            GenerateMap();
	}
	
	void GenerateMap () {
        map = new int[width, height];
        RandomFillMap();

        for (int i = 0; i < smoothAmount; i++)
		{
		    SmoothMap();	 
		}

        ProcessMap();

        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                else
                    borderedMap[x, y] = 1;
            }
        }

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, squareSize, wallHeight, is2D);

        SetGroundPlane();

        InstantiatePlayer(spawnTile, is2D ? player2D : player3D);
	}

    void RandomFillMap()
    {
        if (randomSeed)
        {
            seed = Time.time.ToString();
        }

        System.Random randGen = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (randGen.Next(0, 101) < fillPercentage) ? 1 : 0;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbouringWalls = NeighbouringWalls(x, y);
                
                if (neighbouringWalls > 4)
                    map[x, y] = 1;
                else if (neighbouringWalls < 4)
                    map[x, y] = 0;
            }
        }
    }

    int NeighbouringWalls(int gridX, int gridY)
    {
        int count = 0;
        for (int x = gridX - 1; x <= gridX + 1; x++)
        {
            for (int y = gridY - 1; y <= gridY + 1; y++)
            {
                if (IsInMap(x, y))
                {
                    if (!(x == gridX && y == gridY))
                    {
                        count += map[x, y];
                    }
                }
                else
                    count++;
                        
            }
        }

        return count;
    }

    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMap(x, y) && (x == tile.tileX || y == tile.tileY) && mapFlags[x, y] == 0 && map[x, y] == tileType)
                    {
                        mapFlags[x, y] = 1;
                        queue.Enqueue(new Coord(x, y));
                    }
                }
            }
        }

        return tiles;
    }

    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (var tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach (var wallRegion in wallRegions)
        {
            if(wallRegion.Count < smallestWallRegion)
                foreach (var tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
        }

        List<List<Coord>> roomRegions = GetRegions(0);
        List<Room> rooms = new List<Room>();

        foreach (var roomRegion in roomRegions)
        {
            if (roomRegion.Count < smallestRoomRegion)
                foreach (var tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = 1;
                }
            else
                rooms.Add(new Room(roomRegion, map));
        }

        rooms.Sort();
        rooms[0].isMainRoom = true;
        rooms[0].isAccessibleFromMainRoom = true;

        spawnTile = rooms[0].RandomPointInside();

        ConnectClosestRooms(rooms);
    }

    void ConnectClosestRooms(List<Room> rooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if(forceAccessibilityFromMainRoom){
            foreach (var room in rooms)
            {
                if (room.isAccessibleFromMainRoom)
                    roomListB.Add(room);
                else
                    roomListA.Add(room);
            }
        }
        else
        {
            roomListA = rooms;
            roomListB = rooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (var roomA in roomListA)
        {
            if (!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                    break;
            }                

            foreach (var roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                    continue;
                foreach (var tileA in roomA.edgeTiles)
                {
                    foreach (var tileB in roomB.edgeTiles)
                    {
                        int distance = (int)(Math.Pow((tileA.tileX - tileB.tileX), 2) + Math.Pow((tileA.tileY - tileB.tileY), 2));
                        if (distance < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distance;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
        }

        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(rooms, true);
        }
            

        if (!forceAccessibilityFromMainRoom)
            ConnectClosestRooms(rooms, true);
    }

    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);

        List<Coord> line = GetLine(tileA, tileB);
        foreach (var tile in line)
        {
            DrawCircle(tile, passagewayRadius);
        }
    }

    void DrawCircle(Coord tile, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y < radius; y++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    int realX = tile.tileX + x;
                    int realY = tile.tileY + y;
                    if (IsInMap(realX, realY))
                        map[realX, realY] = 0;
                }
            }
        }
    }

    List<Coord> GetLine(Coord start, Coord end)
    {
        List<Coord> line = new List<Coord>();

        int x = start.tileX;
        int y = start.tileY;

        int dx = end.tileX - start.tileX;
        int dy = end.tileY - start.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Math.Abs(dx);
        int shortest = Math.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Math.Abs(dy);
            shortest = Math.Abs(dx);
            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulator = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x, y));

            if (inverted)
                y += step;
            else
                x += step;

            gradientAccumulator += shortest;

            if (gradientAccumulator >= longest)
            {
                if (inverted)
                    x += gradientStep;
                else
                    y += gradientStep;
                gradientAccumulator -= longest;
            }
        }

        return line;
    }

    Vector3 CoordToWorldPoint(Coord tile)
    {
        if(!is2D)
            return new Vector3(-width / 2 + tile.tileX * squareSize + squareSize / 2, 0, -height / 2 + tile.tileY * squareSize + squareSize / 2);
        else
            return new Vector3(-width / 2 + tile.tileX * squareSize + squareSize / 2, -height / 2 + tile.tileY * squareSize + squareSize / 2, 0);
    }

    bool IsInMap(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

    void InstantiatePlayer(Coord tile, GameObject playerPrefab)
    {
        if (player != null)
            Destroy(player);

        player = Instantiate(playerPrefab, CoordToWorldPoint(tile), Quaternion.identity) as GameObject;

        if (player.GetComponentInChildren<Camera>() != null && Camera.main != null && player.GetComponentInChildren<Camera>() != Camera.main)
            Camera.main.gameObject.SetActive(false);
    }

    void SetGroundPlane()
    {
        Transform plane = transform.GetChild(2);
        plane.localScale = new Vector3(width / 10.0f, 1, height / 10.0f);

        if (is2D)
        {
            plane.rotation = Quaternion.Euler(270, 0, 0);
            plane.position = new Vector3(plane.position.x, plane.position.y, 1);
        }
        else
        {
            plane.position = new Vector3(plane.position.x, -wallHeight, plane.position.z);
            plane.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    class Room : IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int size;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public Room() { }

        public Room(List<Coord> tiles, int[,] map)
        {
            this.tiles = tiles;
            this.size = tiles.Count;
            this.edgeTiles = new List<Coord>();
            this.connectedRooms = new List<Room>();
            this.isAccessibleFromMainRoom = false;
            this.isMainRoom = false;

            foreach (var tile in tiles)
            {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if ((x == tile.tileX || y == tile.tileY) && map[x, y] == 1)
                            edgeTiles.Add(tile);
                    }
                }
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);

            if (roomA.isAccessibleFromMainRoom)
                roomB.SetAccessibleFromMainRoom();
            else if(roomB.isAccessibleFromMainRoom)
                roomA.SetAccessibleFromMainRoom();
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }

        public Coord RandomPointInside()
        {
            int i = UnityEngine.Random.Range(0, tiles.Count);

            while(edgeTiles.Contains(tiles[i]))
                i = UnityEngine.Random.Range(0, tiles.Count);

            return tiles[i];
        }

        public void SetAccessibleFromMainRoom()
        {
            if (!isAccessibleFromMainRoom)
                isAccessibleFromMainRoom = true;

            foreach (var room in connectedRooms)
            {
                room.isAccessibleFromMainRoom = true;
            }
        }

        public int CompareTo(Room other)
        {
            return other.size.CompareTo(this.size);
        }
    }

    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int tileX, int tileY)
        {
            this.tileX = tileX;
            this.tileY = tileY;
        }
    }
}
