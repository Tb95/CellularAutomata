using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Pathfinding {

    Node[,] nodeMap;
    MapGenerator mapGen;

    public Pathfinding(int[,] map, MapGenerator mapGen)
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
                    List<Vector3> path = ReconstructPath(cameFrom, end);
                    cache[end] = path;
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

                            /*float my_f_score = tentative_g_score + heuristicDistance(neighbour, end);
                            List<Node> beforeList = openSet.TakeWhile(node => f_score[node] < my_f_score).ToList();
                            List<Node> afterList = openSet.SkipWhile(node => f_score[node] < my_f_score).ToList();
                            beforeList.Add(neighbour);
                            openSet = beforeList.Concat(afterList).ToList();*/
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
            return Mathf.Pow(a.position.x - b.position.x, 2) + Mathf.Pow(a.position.z - b.position.z, 2);
        }

        List<Vector3> ReconstructPath(Dictionary<Node, Node> cameFrom, Node end)
        {
            Vector3 pos = end.position;
            List<Vector3> result;

            if (cameFrom.ContainsKey(end))
                result = ReconstructPath(cameFrom, cameFrom[end]);
            else
                result = new List<Vector3>();
            result.Add(pos);

            return result;
        }

        float RealDistance(Node a, Node b)
        {
            return (a.cost + b.cost) / 2;
        }
    }
}
