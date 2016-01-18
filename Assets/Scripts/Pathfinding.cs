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

    public Pathfinding(int[,] map, MapGenerator mapGen)
    {
        this.mapGen = mapGen;
        nodeMap = new Node[map.GetLength(0), map.GetLength(1)];

        int id = 0;

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                int cost = 100;

                if (map[x, y] == 0)
                {
                    if (mapGen.NeighbouringWalls(x, y) > 0)
                        cost = 5;
                    else
                        cost = 1;
                }

                nodeMap[x, y] = new Node(cost, mapGen.CoordToWorldPoint(new MapGenerator.Coord(x, y)), id);
                id++;
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

    public LinkedList<Vector3> GetPath(Vector3 from, Vector3 to)
    {
        MapGenerator.Coord start = mapGen.WorldToCoordPoint(from);
        MapGenerator.Coord end = mapGen.WorldToCoordPoint(to);
        Node startNode = nodeMap[start.tileX, start.tileY];
        Node endNode = nodeMap[end.tileX, end.tileY];

        LinkedList<Vector3> path = null;
        System.Threading.Thread newThread = new System.Threading.Thread(() => path = startNode.FindPathTo(endNode));
        newThread.Start();
        newThread.Join();

        return path;
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
        int id;
        public int Id
        {
            get { return id; }
        }
        Vector3 position;
        int cost;
        Node[] neighbours;

        public Node(int cost, Vector3 position, int id)
        {
            this.position = position;
            this.cost = cost;
            this.id = id;
        }

        public void SetNeighbours(Node[] neighbours)
        {
            this.neighbours = neighbours;
        }

        public void SetCost(int cost)
        {
            this.cost = cost;
            //this.FreeCache();
        }

        /*public void FreeCache()
        {
            this.cache.Clear();
            foreach (var node in neighbours)
            {
                node.FreeCache();
            }
        }*/

        public LinkedList<Vector3> FindPathTo(Node end)
        {
            HashSet<Node> closedSet = new HashSet<Node>();
            PriorityQueue openSet = new PriorityQueue();
            Dictionary<Node, int> g_score = new Dictionary<Node, int>();
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();

            g_score.Add(this, 0);
            openSet.Insert(heuristicDistance(this, end), this);

            while (openSet.Length > 0)
            {
                Node currentNode = openSet.ExtractMin();

                if (currentNode == end)
                {
                    LinkedList<Vector3> path = ReconstructPath(cameFrom, end);
                    return path;
                }

                closedSet.Add(currentNode);

                if (currentNode.neighbours != null && currentNode.neighbours.Length > 0)
                {
                    foreach (var neighbour in currentNode.neighbours)
                    {
                        int tentative_g_score = g_score[currentNode] + RealDistance(currentNode, neighbour);

                        if (closedSet.Contains(neighbour))
                        {
                            if(tentative_g_score >= g_score[currentNode])
                                continue;
                            else
                            {
                                Debug.Log("Remove");
                                closedSet.Remove(currentNode);
                            }
                        }

                        if (openSet.Contains(neighbour))
                        {
                            if (tentative_g_score >= g_score[neighbour])
                                continue;
                            else
                                openSet.DecreaseKey(tentative_g_score + heuristicDistance(neighbour, end), neighbour);
                        }
                        else
                            openSet.Insert(tentative_g_score + heuristicDistance(neighbour, end), neighbour);

                        cameFrom[neighbour] = currentNode;
                        g_score[neighbour] = tentative_g_score;
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

        LinkedList<Vector3> ReconstructPath(Dictionary<Node, Node> cameFrom, Node end)
        {
            if (cameFrom.ContainsKey(end))
            {
                Node previous = cameFrom[end];
                LinkedList<Vector3> path = ReconstructPath(cameFrom, previous);
                path.AddLast(end.position);
                return path;
            }
            else
                return new LinkedList<Vector3>();
        }

        int RealDistance(Node a, Node b)
        {
            return (a.cost + b.cost) / 2;
        }

        public bool IsNeighbour(Node other)
        {
            return this.neighbours.Contains(other) || other.neighbours.Contains(this);
        }
    }

    class PriorityQueue
    {
        List<KeyValuePair<float, Node>> A;
        Dictionary<int, int> indexes;
        int nextElement;

        public PriorityQueue()
        {
            A = new List<KeyValuePair<float, Node>>();
            indexes = new Dictionary<int, int>();
            nextElement = 0;
        }

        public int Length
        {
            get { return nextElement; }
        }

        public Node Min()
        {
            if (Length < 1)
                throw new System.Exception("Heap underflow");

            return A[0].Value;
        }

        public Node ExtractMin()
        {
            if (Length < 1)
                throw new System.Exception("Heap underflow");

            Node min = A[0].Value;
            A[0] = A[nextElement - 1];
            indexes.Remove(min.Id);
            nextElement--;
            MinHeapify(0);

            return min;
        }

        public void DecreaseKey(float newKey, Node node)
        {
            if(!indexes.ContainsKey(node.Id))
                return;

            int index = indexes[node.Id];

            if (newKey > A[index].Key)
                return;

            A[index] = new KeyValuePair<float, Node>(newKey, A[index].Value);

            while (index > 0 && A[(index - 1) / 2].Key > A[index].Key)
            {
                indexes[A[index].Value.Id] = (index - 1) / 2;
                indexes[A[(index - 1) / 2].Value.Id] = index;

                KeyValuePair<float, Node> tmp = A[index];
                A[index] = A[(index - 1) / 2];
                A[(index - 1) / 2] = tmp;

                index = (index - 1) / 2;
            }
        }

        public void Insert(float key, Node value)
        {
            if(nextElement >= A.Count)
                A.Add(new KeyValuePair<float, Node>(key, value));
            else
                A[nextElement] = new KeyValuePair<float, Node>(key, value);
            nextElement++;
            indexes.Add(value.Id, nextElement - 1);
            DecreaseKey(key, value);
        }

        public bool Contains(Node value)
        {
            return indexes.ContainsKey(value.Id);
        }

        void MinHeapify(int index)
        {
            int left = 2 * index + 1;
            int right = left + 1;

            int smallest;

            if (left < Length && A[left].Key < (A[index].Key))
                smallest = left;
            else
                smallest = index;
            if (right < Length && A[right].Key < A[smallest].Key)
                smallest = right;

            if (smallest != index)
            {
                indexes[A[index].Value.Id] = smallest;
                indexes[A[smallest].Value.Id] = index;

                KeyValuePair<float, Node> tmp = A[index];
                A[index] = A[smallest];
                A[smallest] = tmp;

                MinHeapify(smallest);
            }
        }
    }
}