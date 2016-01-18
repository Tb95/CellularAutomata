using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MeshGenerator : MonoBehaviour
{

    #region variables
    public SquareGrid squareGrid;
    public Transform walls;
    public Transform cave;
    [Range(1, 100)]
    public int textureRepeatAmount;

    List<Vector3> vertices;
    List<int> triangles;
    List<List<Vector3>> cappedVertexLists;
    List<List<int>> cappedTriangleLists;
    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();
    #endregion

    public void GenerateMesh(int[,] map, float squareSize, float wallHeight, bool is2D)
    {
        outlines.Clear();
        checkedVertices.Clear();
        triangleDictionary.Clear();

        squareGrid = new SquareGrid(map, squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();
        cappedVertexLists = new List<List<Vector3>>();
        cappedTriangleLists = new List<List<int>>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        if (is2D)
        {
            CreateCeilingMesh(map, squareSize);

            foreach (var item in GameObject.FindGameObjectsWithTag("Wall"))
            {
                if (Application.isPlaying)
                    Destroy(item);
                else
                    DestroyImmediate(item);
            }

            cave.transform.rotation = Quaternion.Euler(270, 0, 0);
            Generate2DColliders();
        }
        else
        {
            CreateCeilingMesh(map, squareSize);

            EdgeCollider2D[] currentColliders = GetComponents<EdgeCollider2D>();
            foreach (var collider in currentColliders)
            {
                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }

            cave.transform.rotation = Quaternion.Euler(0, 0, 0);
            CreateWallMesh(wallHeight);
        }
    }

    private void CreateCeilingMesh(int[,] map, float squareSize)
    {
        foreach (var item in GameObject.FindGameObjectsWithTag("Cave"))
        {
            if (Application.isPlaying)
                    Destroy(item);
                else
                    DestroyImmediate(item);
        }

        for (int i = 0; i < cappedVertexLists.Count; i++)
        {
            Vector2[] uvs = new Vector2[cappedVertexLists[i].Count];
            for (int j = 0; j < cappedVertexLists[i].Count; j++)
            {
                float percentX = Mathf.InverseLerp(-map.GetLength(0) * squareSize / 2, map.GetLength(0) * squareSize / 2,
                    cappedVertexLists[i][j].x) * textureRepeatAmount;
                float percentY = Mathf.InverseLerp(-map.GetLength(1) * squareSize / 2, map.GetLength(1) * squareSize / 2,
                    cappedVertexLists[i][j].z) * textureRepeatAmount;
                uvs[j] = new Vector2(percentX, percentY);
            }

            InstantiateCaveMesh(cappedVertexLists[i], cappedTriangleLists[i], uvs);
        }
    }

    void Generate2DColliders()
    {
        EdgeCollider2D[] currentColliders = GetComponents<EdgeCollider2D>();
        foreach (var collider in currentColliders)
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
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
        foreach (var item in GameObject.FindGameObjectsWithTag("Wall"))
        {
            if (Application.isPlaying)
                Destroy(item);
            else
                DestroyImmediate(item);
        }

        CalculateMeshOutline();

        List<Vector3> bigWallVertices = new List<Vector3>();
        List<int> bigWallTriangles = new List<int>();
        List<Vector2> bigWallUvs = new List<Vector2>();

        foreach (var outline in outlines)
        {
            if (2 * outline.Count > 40000)
            {
                if(2 * outline.Count < 65000)
                    InstantiateOutline(wallHeight, outline);
                else
                {
                    List<int> newOutline = outline.Take(30001).ToList();
                    List<int> outlineRest = outline.Skip(30000).ToList();
                    InstantiateHalfOutline(wallHeight, newOutline);

                    while (2 * outlineRest.Count > 65000)
                    {
                        newOutline = outlineRest.Take(30001).ToList();
                        outlineRest = outlineRest.Skip(30000).ToList();
                        InstantiateHalfOutline(wallHeight, newOutline);
                    }

                    InstantiateHalfOutline(wallHeight, outlineRest);
                }
            }
            else
            {
                if (bigWallVertices.Count + 2 * outline.Count > 60000)
                {
                    InstantiateWallMesh(bigWallVertices, bigWallTriangles, bigWallUvs);
                    bigWallVertices = new List<Vector3>();
                    bigWallTriangles = new List<int>();
                    bigWallUvs = new List<Vector2>();
                }

                bigWallVertices.Add(vertices[outline[0]]);                                 //topLeft
                bigWallUvs.Add(new Vector2(0, 1));
                bigWallVertices.Add(vertices[outline[0]] - Vector3.up * wallHeight);       //bottomLeft
                bigWallUvs.Add(new Vector2(0, 0));

                float textureRepeatAmount = (wallHeight / squareGrid.squareSize);
                float xPositionUv = 0;

                for (int i = 1; i < outline.Count - 1; i++)
                {
                    int startIndex = bigWallVertices.Count;
                    xPositionUv += 1 / textureRepeatAmount;

                    bigWallVertices.Add(vertices[outline[i]]);                                 //topRight
                    bigWallUvs.Add(new Vector2(xPositionUv, 1));
                    bigWallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);       //bottomRight
                    bigWallUvs.Add(new Vector2(xPositionUv, 0));

                    bigWallTriangles.Add(startIndex - 2);
                    bigWallTriangles.Add(startIndex - 1);
                    bigWallTriangles.Add(startIndex + 1);
                    bigWallTriangles.Add(startIndex + 1);
                    bigWallTriangles.Add(startIndex - 0);
                    bigWallTriangles.Add(startIndex - 2);
                }

                xPositionUv += 1 / textureRepeatAmount;

                bigWallVertices.Add(vertices[outline[0]]);                                 //topLeft
                bigWallUvs.Add(new Vector2(xPositionUv, 1));
                bigWallVertices.Add(vertices[outline[0]] - Vector3.up * wallHeight);       //bottomLeft
                bigWallUvs.Add(new Vector2(xPositionUv, 0));

                int lastIndex = bigWallVertices.Count - 1;

                bigWallTriangles.Add(lastIndex - 3);
                bigWallTriangles.Add(lastIndex - 2);
                bigWallTriangles.Add(lastIndex - 0);
                bigWallTriangles.Add(lastIndex - 0);
                bigWallTriangles.Add(lastIndex - 1);
                bigWallTriangles.Add(lastIndex - 3);
            }
        }

        InstantiateWallMesh(bigWallVertices, bigWallTriangles, bigWallUvs);
    }

    private void InstantiateOutline(float wallHeight, List<int> outline)
    {
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        wallVertices.Add(vertices[outline[0]]);                                 //topLeft
        uvs.Add(new Vector2(0, 1));
        wallVertices.Add(vertices[outline[0]] - Vector3.up * wallHeight);       //bottomLeft
        uvs.Add(new Vector2(0, 0));

        float textureRepeatAmount = (wallHeight / squareGrid.squareSize);
        float xPositionUv = 0;

        for (int i = 1; i < outline.Count - 1; i++)
        {
            int startIndex = wallVertices.Count;
            xPositionUv += 1 / textureRepeatAmount;

            wallVertices.Add(vertices[outline[i]]);                                 //topRight
            uvs.Add(new Vector2(xPositionUv, 1));
            wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);       //bottomRight
            uvs.Add(new Vector2(xPositionUv, 0));

            wallTriangles.Add(startIndex - 2);
            wallTriangles.Add(startIndex - 1);
            wallTriangles.Add(startIndex + 1);
            wallTriangles.Add(startIndex + 1);
            wallTriangles.Add(startIndex - 0);
            wallTriangles.Add(startIndex - 2);
        }

        xPositionUv += 1 / textureRepeatAmount;

        wallVertices.Add(vertices[outline[0]]);                                 //topLeft
        uvs.Add(new Vector2(xPositionUv, 1));
        wallVertices.Add(vertices[outline[0]] - Vector3.up * wallHeight);       //bottomLeft
        uvs.Add(new Vector2(xPositionUv, 0));

        int lastIndex = wallVertices.Count - 1;

        wallTriangles.Add(lastIndex - 3);
        wallTriangles.Add(lastIndex - 2);
        wallTriangles.Add(lastIndex - 0);
        wallTriangles.Add(lastIndex - 0);
        wallTriangles.Add(lastIndex - 1);
        wallTriangles.Add(lastIndex - 3);

        InstantiateWallMesh(wallVertices, wallTriangles, uvs);
    }

    private void InstantiateHalfOutline(float wallHeight, List<int> outline)
    {
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        wallVertices.Add(vertices[outline[0]]);                                 //topLeft
        uvs.Add(new Vector2(0, 1));
        wallVertices.Add(vertices[outline[0]] - Vector3.up * wallHeight);       //bottomLeft
        uvs.Add(new Vector2(0, 0));

        float textureRepeatAmount = (wallHeight / squareGrid.squareSize);
        float xPositionUv = 0;

        for (int i = 1; i < outline.Count; i++)
        {
            int startIndex = wallVertices.Count;
            xPositionUv += 1 / textureRepeatAmount;

            wallVertices.Add(vertices[outline[i]]);                                 //topRight
            uvs.Add(new Vector2(xPositionUv, 1));
            wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);       //bottomRight
            uvs.Add(new Vector2(xPositionUv, 0));

            wallTriangles.Add(startIndex - 2);
            wallTriangles.Add(startIndex - 1);
            wallTriangles.Add(startIndex + 1);
            wallTriangles.Add(startIndex + 1);
            wallTriangles.Add(startIndex - 0);
            wallTriangles.Add(startIndex - 2);
        }

        InstantiateWallMesh(wallVertices, wallTriangles, uvs);
    }

    private void InstantiateWallMesh(List<Vector3> wallVertices, List<int> wallTriangles, List<Vector2> uvs)
    {
        Mesh wallMesh = new Mesh();

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.uv = uvs.ToArray();
        wallMesh.RecalculateNormals();

        GameObject outlineObject = new GameObject();
        outlineObject.transform.parent = walls;
        outlineObject.tag = "Wall";
        outlineObject.name = "Outline (" + wallVertices.Count + " vertices)";

        MeshRenderer wallRenderer = outlineObject.AddComponent<MeshRenderer>();
        wallRenderer.sharedMaterial = walls.GetComponent<MeshRenderer>().sharedMaterial;

        MeshFilter wallFilter = outlineObject.AddComponent<MeshFilter>();
        wallFilter.sharedMesh = wallMesh;

        MeshCollider wallCollider = outlineObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;
    }

    private void InstantiateCaveMesh(List<Vector3> vertices, List<int> triangles, Vector2[] uvs)
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        GameObject caveObject = new GameObject();
        caveObject.transform.parent = cave;
        caveObject.tag = "Cave";
        caveObject.name = "Cave (" + vertices.Count + " vertices)";

        MeshRenderer renderer = caveObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = cave.GetComponent<MeshRenderer>().sharedMaterial;

        MeshFilter filter = caveObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
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
                checkedVertices.Add(square.topLeft.absoluteVertexIndex);
                checkedVertices.Add(square.topRight.absoluteVertexIndex);
                checkedVertices.Add(square.bottomRight.absoluteVertexIndex);
                checkedVertices.Add(square.bottomLeft.absoluteVertexIndex);
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
            if (points[i].vertexIndex == -1)
            {
                if (cappedVertexLists.Count == 0 || cappedVertexLists[cappedVertexLists.Count - 1].Count > 60000)
                {
                    cappedVertexLists.Add(new List<Vector3>());
                    cappedTriangleLists.Add(new List<int>());
                }

                points[i].listIndex = cappedVertexLists.Count - 1;
                points[i].vertexIndex = cappedVertexLists[cappedVertexLists.Count - 1].Count;
                points[i].absoluteVertexIndex = vertices.Count;

                cappedVertexLists[cappedVertexLists.Count - 1].Add(points[i].position);
                vertices.Add(points[i].position);
            }                
        }
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        if (a.listIndex == b.listIndex && b.listIndex == c.listIndex)
        {
            cappedTriangleLists[a.listIndex].Add(a.vertexIndex);
            cappedTriangleLists[b.listIndex].Add(b.vertexIndex);
            cappedTriangleLists[c.listIndex].Add(c.vertexIndex);
        }
        else if (a.listIndex == b.listIndex || b.listIndex == c.listIndex || a.listIndex == c.listIndex)
        {
            Node differentNode = c;

            int sameListIndex = a.listIndex;

            int aSameIndex = a.vertexIndex;
            int bSameIndex = b.vertexIndex;
            int cSameIndex = cappedVertexLists[a.listIndex].Count;

            if (a.listIndex == c.listIndex && c.listIndex != b.listIndex)
            {
                differentNode = b;

                bSameIndex = cappedVertexLists[a.listIndex].Count;
                cSameIndex = c.vertexIndex;
            }
            else if (c.listIndex == b.listIndex && b.listIndex != a.listIndex)
            {
                sameListIndex = b.listIndex;
                differentNode = a;

                aSameIndex = cappedVertexLists[b.listIndex].Count;
                cSameIndex = c.vertexIndex;
            }

            cappedVertexLists[sameListIndex].Add(differentNode.position);

            cappedTriangleLists[sameListIndex].Add(aSameIndex);
            cappedTriangleLists[sameListIndex].Add(bSameIndex);
            cappedTriangleLists[sameListIndex].Add(cSameIndex);
        }
        else
        {
            cappedVertexLists[a.listIndex].Add(b.position);
            cappedVertexLists[a.listIndex].Add(c.position);

            cappedTriangleLists[a.listIndex].Add(a.vertexIndex);
            cappedTriangleLists[a.listIndex].Add(cappedTriangleLists[a.listIndex].Count - 2);
            cappedTriangleLists[a.listIndex].Add(cappedTriangleLists[a.listIndex].Count - 1);

            cappedVertexLists[b.listIndex].Add(a.position);
            cappedVertexLists[b.listIndex].Add(c.position);

            cappedTriangleLists[b.listIndex].Add(cappedTriangleLists[b.listIndex].Count - 2);
            cappedTriangleLists[b.listIndex].Add(b.vertexIndex);
            cappedTriangleLists[b.listIndex].Add(cappedTriangleLists[b.listIndex].Count - 1);

            cappedVertexLists[c.listIndex].Add(a.position);
            cappedVertexLists[c.listIndex].Add(b.position);

            cappedTriangleLists[c.listIndex].Add(cappedTriangleLists[c.listIndex].Count - 2);
            cappedTriangleLists[c.listIndex].Add(cappedTriangleLists[c.listIndex].Count - 1);
            cappedTriangleLists[c.listIndex].Add(c.vertexIndex);
        }

        triangles.Add(a.absoluteVertexIndex);
        triangles.Add(b.absoluteVertexIndex);
        triangles.Add(c.absoluteVertexIndex);

        Triangle triangle = new Triangle(a.absoluteVertexIndex, b.absoluteVertexIndex, c.absoluteVertexIndex);
        AddTriangleToDictionary(a.absoluteVertexIndex, triangle);
        AddTriangleToDictionary(b.absoluteVertexIndex, triangle);
        AddTriangleToDictionary(c.absoluteVertexIndex, triangle);
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
        while (vertexIndex != -1)
        {
            outlines[outlineIndex].Add(vertexIndex);
            checkedVertices.Add(vertexIndex);
            int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);
            vertexIndex = nextVertexIndex;
        }
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
        public int listIndex = -1;
        public int vertexIndex = -1;
        public int absoluteVertexIndex = -1;

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
        public float squareSize;

        public SquareGrid(int[,] map, float squareSize)
        {
            this.squareSize = squareSize;

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
