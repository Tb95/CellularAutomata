using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Pathfinding
{

    #region variables
    Node[,] nodeMap;
    MapGenerator mapGen;
    #endregion

    public Pathfinding(int[,] map, MapGenerator mapGen, List<MapGenerator.Room> rooms)
    {
        this.mapGen = mapGen;
        nodeMap = new Node[map.GetLength(0), map.GetLength(1)];

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                int cost = 100;

                if (map[x, y] == 0)
                {
                    if (mapGen.NeighbouringWalls(x, y) > 0)
                        cost = 30;
                    else
                        cost = 1;
                }

                nodeMap[x, y] = new Node(cost, mapGen.CoordToWorldPoint(new MapGenerator.Coord(x, y)));
            }
        }

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (map[x, y] == 0)
                {
                    List<Node> neighbours = new List<Node>();

                    if (x > 0)
                        neighbours.Add(nodeMap[x - 1, y]);
                    if (x < map.GetLength(0) - 1)
                        neighbours.Add(nodeMap[x + 1, y]);
                    if (y > 0)
                        neighbours.Add(nodeMap[x, y - 1]);
                    if (y < map.GetLength(1) - 1)
                        neighbours.Add(nodeMap[x, y + 1]);

                    if (x > 0 && y > 0)
                        neighbours.Add(nodeMap[x - 1, y - 1]);
                    if (x < map.GetLength(0) - 1 && y > 0)
                        neighbours.Add(nodeMap[x + 1, y - 1]);
                    if (x > 0 && y < map.GetLength(1) - 1)
                        neighbours.Add(nodeMap[x - 1, y + 1]);
                    if (x < map.GetLength(0) - 1 && y < map.GetLength(1) - 1)
                        neighbours.Add(nodeMap[x + 1, y + 1]);

                    nodeMap[x, y].SetNeighbours(neighbours.ToArray());
                }
                else
                {
                    List<Node> neighbours = new List<Node>();

                    if (x > 0 && map[x - 1, y] == 0)
                        neighbours.Add(nodeMap[x - 1, y]);
                    if (x < map.GetLength(0) - 1 && map[x + 1, y] == 0)
                        neighbours.Add(nodeMap[x + 1, y]);
                    if (y > 0 && map[x, y - 1] == 0)
                        neighbours.Add(nodeMap[x, y - 1]);
                    if (y < map.GetLength(1) - 1 && map[x, y + 1] == 0)
                        neighbours.Add(nodeMap[x, y + 1]);

                    if (x > 0 && y > 0 && map[x - 1, y - 1] == 0)
                        neighbours.Add(nodeMap[x - 1, y - 1]);
                    if (x < map.GetLength(0) - 1 && y > 0 && map[x + 1, y - 1] == 0)
                        neighbours.Add(nodeMap[x + 1, y - 1]);
                    if (x > 0 && y < map.GetLength(1) - 1 && map[x - 1, y + 1] == 0)
                        neighbours.Add(nodeMap[x - 1, y + 1]);
                    if (x < map.GetLength(0) - 1 && y < map.GetLength(1) - 1 && map[x + 1, y + 1] == 0)
                        neighbours.Add(nodeMap[x + 1, y + 1]);

                    nodeMap[x, y].SetNeighbours(neighbours.ToArray());
                }
            }
        }
    }

    public List<Vector3> GetPath(Vector3 from, Vector3 to)
    {
        MapGenerator.Coord start = mapGen.WorldToCoordPoint(from);
        MapGenerator.Coord end = mapGen.WorldToCoordPoint(to);
        Node startNode = nodeMap[start.tileX, start.tileY];
        Node endNode = nodeMap[end.tileX, end.tileY];

        return startNode.FindPathTo(endNode);
    }

    public void SetCost(Vector3 position, int cost)
    {
        MapGenerator.Coord tile = mapGen.WorldToCoordPoint(position);
        nodeMap[tile.tileX, tile.tileY].SetCost(cost);
    }

    public bool AreNeighbours(Vector3 a, Vector3 b)
    {
        MapGenerator.Coord tileA = mapGen.WorldToCoordPoint(a);
        MapGenerator.Coord tileB = mapGen.WorldToCoordPoint(b);
        return nodeMap[tileA.tileX, tileA.tileY].IsNeighbour(nodeMap[tileB.tileX, tileB.tileY]);
    }

    class Node
    {
        Vector3 position;
        int cost;
        Node[] neighbours;
        Dictionary<Node, List<Vector3>> cache;

        public Node(int cost, Vector3 position)
        {
            this.position = position;
            this.cost = cost;
            this.cache = new Dictionary<Node, List<Vector3>>();
        }

        public void SetNeighbours(Node[] neighbours)
        {
            this.neighbours = neighbours;
        }

        public void SetCost(int cost)
        {
            this.cost = cost;
            this.FreeCache();
        }

        public void FreeCache()
        {
            this.cache.Clear();
            foreach (var node in neighbours)
            {
                node.FreeCache();
            }
        }

        public List<Vector3> FindPathTo(Node end)
        {
            if (cache.ContainsKey(end))
                return cache[end];

            List<Node> closedSet = new List<Node>();
            List<Node> openSet = new List<Node>();
            Dictionary<Node, float> g_score = new Dictionary<Node, float>();
            Dictionary<Node, float> f_score = new Dictionary<Node, float>();
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();

            g_score.Add(this, 0);
            f_score.Add(this, heuristicDistance(this, end));
            openSet.Add(this);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet[0];

                if (currentNode == end)
                {
                    List<Vector3> path = ReconstructPath(cameFrom, end, new List<Vector3>());
                    cache[end] = path;
                    //end.cache[this] = path.AsEnumerable().Reverse().ToList();
                    return path;
                }

                openSet.RemoveAt(0);
                closedSet.Add(currentNode);

                if (currentNode.neighbours != null && currentNode.neighbours.Length > 0)
                {
                    foreach (var neighbour in currentNode.neighbours)
                    {
                        float tentative_g_score = g_score[currentNode] + RealDistance(currentNode, neighbour);

                        if (closedSet.Contains(neighbour))
                        {
                            if(tentative_g_score >= g_score[currentNode])
                                continue;
                            else
                            {
                                closedSet.Remove(currentNode);
                            }
                        }                        

                        if (openSet.Contains(neighbour))
                        {
                            if (tentative_g_score >= g_score[neighbour])
                                continue;
                            else
                                openSet.Remove(neighbour);
                        }

                        cameFrom[neighbour] = currentNode;
                        g_score[neighbour] = tentative_g_score;
                        f_score[neighbour] = tentative_g_score + heuristicDistance(neighbour, end);

                        int index = openSet.TakeWhile(node => f_score[node] < f_score[neighbour]).ToList().Count;
                        openSet.Insert(index, neighbour);
                    }
                }
            }

            return null;
        }

        float heuristicDistance(Node a, Node b)
        {
            //Octagonal approximation
            float dx = Mathf.Abs(a.position.x - b.position.x);
            float dy = Mathf.Abs(a.position.z - b.position.z);

            float max = Mathf.Max(dx, dy);
            float min = Mathf.Min(dx, dy);

            const float A = 1007f / 1024f;
            const float B = 441f / 1024f;
            
            return A * max + B * min;
        }

        List<Vector3> ReconstructPath(Dictionary<Node, Node> cameFrom, Node end, List<Vector3> currentPath)
        {
            Vector3 pos = end.position;

            currentPath.Insert(0, pos);

            if (cameFrom.ContainsKey(end))
            {
                Node previous = cameFrom[end];
                previous.cache[end] = currentPath;
                return ReconstructPath(cameFrom, previous, currentPath);
            }
            else
                return currentPath;
        }

        float RealDistance(Node a, Node b)
        {
            return (a.cost + b.cost) / 2;
        }

        public bool IsNeighbour(Node other)
        {
            return this.neighbours.Contains(other) || other.neighbours.Contains(this);
        }
    }
}