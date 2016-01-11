using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour {

    public SquareGrid squareGrid;
    public MeshFilter walls;
    public MeshFilter cave;
    [Range(1, 100)]
    public int textureRepeatAmount;

    List<Vector3> vertices;
    List<int> triangles;
    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();

    public void GenerateMesh(int[,] map, float squareSize, float wallHeight, bool is2D)
    {
        outlines.Clear();
        checkedVertices.Clear();
        triangleDictionary.Clear();

        squareGrid = new SquareGrid(map, squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        cave.mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) * squareSize / 2, map.GetLength(0) * squareSize / 2, vertices[i].x) * textureRepeatAmount;
            float percentY = Mathf.InverseLerp(-map.GetLength(1) * squareSize / 2, map.GetLength(1) * squareSize / 2, vertices[i].z) * textureRepeatAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
        mesh.uv = uvs;

        if (!is2D)
        {
            cave.transform.rotation = Quaternion.Euler(0, 0, 0);
            CreateWallMesh(wallHeight);
        }
        else
        {
            cave.transform.rotation = Quaternion.Euler(270, 0, 0);
            Generate2DColliders();
        }
    }

    void Generate2DColliders()
    {
        EdgeCollider2D[] currentColliders = GetComponents<EdgeCollider2D>();
        foreach (var collider in currentColliders)
        {
            Destroy(collider);
        }

        CalculateMeshOutline();

        foreach (var outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];

            for (int i = 0; i < outline.Count; i++)
            {
                edgePoints[i] = new Vector2(vertices[outline[i]].x, vertices[outline[i]].z);
            }
            edgeCollider.points = edgePoints;
        }
    }

    void CreateWallMesh(float wallHeight)
    {
        CalculateMeshOutline();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();

        foreach (var outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]);                                 //left
                wallVertices.Add(vertices[outline[i + 1]]);                             //right
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);       //bottomLeft
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight);   //bottomRight

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.RecalculateNormals();
        walls.mesh = wallMesh;

        Vector2[] uvs = new Vector2[wallVertices.Count];
        for (int i = 0; i < wallVertices.Count; i+=4)
        {
            float leftX = 1f / textureRepeatAmount * (i / 4);
            float rightX = 1f / textureRepeatAmount * ((i / 4) + 1);
            uvs[i] = new Vector2(leftX, 1);             //left
            uvs[i + 1] = new Vector2(rightX, 1);        //right
            uvs[i + 2] = new Vector2(leftX, 0);         //bottomLeft
            uvs[i + 3] = new Vector2(rightX, 0);        //bottomRight
        }
        wallMesh.uv = uvs;

        MeshCollider wallCollider = walls.gameObject.GetComponent<MeshCollider>();
        if(wallCollider == null)
            wallCollider  = walls.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;
    }

    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            //1 point
            case 1:
                MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                break;

            //2 points
            case 3:
                MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 6:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                break;
            case 5:
                MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            //3 points
            case 7:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;

            //4 points
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                checkedVertices.Add(square.topLeft.index);
                checkedVertices.Add(square.topRight.index);
                checkedVertices.Add(square.bottomRight.index);
                checkedVertices.Add(square.bottomLeft.index);
                break;
        }
    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);
    }

    void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].index == -1)
            {
                points[i].index = vertices.Count;
                vertices.Add(points[i].position);
            }                
        }
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.index);
        triangles.Add(b.index);
        triangles.Add(c.index);

        Triangle triangle = new Triangle(a.index, b.index, c.index);
        AddTriangleToDictionary(a.index, triangle);
        AddTriangleToDictionary(b.index, triangle);
        AddTriangleToDictionary(c.index, triangle);
    }

    void AddTriangleToDictionary(int indexKey, Triangle triangle)
    {
        if (triangleDictionary.ContainsKey(indexKey))
            triangleDictionary[indexKey].Add(triangle);
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(indexKey, triangleList);
        }
    }

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        foreach (var triangle in trianglesContainingA)
        {
            if (triangle.Contains(vertexB))
            {
                sharedTriangleCount++;
                if (sharedTriangleCount > 1)
                    break;
            }
        }

        return sharedTriangleCount == 1;
    }

    int GetConnectedOutlineVertex(int vertex)
    {
        List<Triangle> triangleContainingVertex = triangleDictionary[vertex];

        foreach (var triangle in triangleContainingVertex)
        {
            for (int i = 0; i < 3; i++)
            {
                int vertexB = triangle[i];
                if (vertex != vertexB && !checkedVertices.Contains(vertexB) && IsOutlineEdge(vertex, vertexB))
                    return vertexB;
            }
        }

        return -1;
    }

    void CalculateMeshOutline()
    {
        for (int index = 0; index < vertices.Count; index++)
        {
            if (!checkedVertices.Contains(index))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(index);
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(index);
                    List<int> newOutline = new List<int>();
                    newOutline.Add(index);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(index);
                }
            }
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);
        if (nextVertexIndex != -1)
            FollowOutline(nextVertexIndex, outlineIndex);
    }    

    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;

        int[] vertices;

        public int this[int i]
        {
            get { return vertices[i];  }
        }

        public Triangle(int a, int b, int c)
        {
            this.vertexIndexA = a;
            this.vertexIndexB = b;
            this.vertexIndexC = c;
            this.vertices = new int[3];
            this.vertices[0] = a;
            this.vertices[1] = b;
            this.vertices[2] = c;
        }

        public bool Contains(int vertex)
        {
            return (vertex == vertexIndexA || vertex == vertexIndexB || vertex == vertexIndexC);
        }
    }

    public class Node
    {
        public Vector3 position;
        public int index = -1;

        public Node(Vector3 pos)
        {
            this.position = pos;
        }
    }

    public class ControlNode : Node
    {
        public bool active;
        public Node topNode;
        public Node rightNode;

        public ControlNode(Vector3 pos, bool active, float squareSize)
            : base(pos)
        {
            this.active = active;
            topNode = new Node(pos + Vector3.forward * squareSize / 2);
            rightNode = new Node(pos + Vector3.right * squareSize / 2);
        }
    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomLeft, bottomRight;
        public Node centreTop, centreRight, centreBottom, centreLeft;
        public int configuration;

        public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft)
        {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomLeft = bottomLeft;
            this.bottomRight = bottomRight;

            this.centreTop = topLeft.rightNode;
            this.centreRight = bottomRight.topNode;
            this.centreBottom = bottomLeft.rightNode;
            this.centreLeft = bottomLeft.topNode;

            configuration = 0;
            if (topLeft.active)
                configuration += 8;
            if (topRight.active)
                configuration += 4;
            if (bottomRight.active)
                configuration += 2;
            if (bottomLeft.active)
                configuration += 1;
        }
    }

    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];
            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    Vector3 pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0, -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }
    }
}
