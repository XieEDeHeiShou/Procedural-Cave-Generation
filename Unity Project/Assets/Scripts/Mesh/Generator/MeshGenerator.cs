using System.Collections.Generic;
using UnityEngine;

namespace Mesh.Generator {
    public class MeshGenerator : MonoBehaviour {
        private readonly HashSet<int> _checkedVertices = new HashSet<int>();
        private readonly List<List<int>> _outlines = new List<List<int>>();

        private readonly Dictionary<int, List<Triangle>> _triangleDictionary = new Dictionary<int, List<Triangle>>();
        private List<int> _triangles;

        private List<Vector3> _vertices;
        public MeshFilter cave;

        public bool is2D;

        public SquareGrid squareGrid;
        public MeshFilter walls;

        public void GenerateMesh(int[,] map, float squareSize) {
            _triangleDictionary.Clear();
            _outlines.Clear();
            _checkedVertices.Clear();

            squareGrid = new SquareGrid(map, squareSize);

            _vertices = new List<Vector3>();
            _triangles = new List<int>();

            for (var x = 0; x < squareGrid.squares.GetLength(0); x++)
            for (var y = 0; y < squareGrid.squares.GetLength(1); y++)
                TriangulateSquare(squareGrid.squares[x, y]);

            var mesh = new UnityEngine.Mesh();
            cave.mesh = mesh;

            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.RecalculateNormals();

            var tileAmount = 10;
            var uvs = new Vector2[_vertices.Count];
            for (var i = 0; i < _vertices.Count; i++) {
                var percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize,
                                   _vertices[i].x) * tileAmount;
                var percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize,
                                   _vertices[i].z) * tileAmount;
                uvs[i] = new Vector2(percentX, percentY);
            }

            mesh.uv = uvs;


            if (is2D)
                Generate2DColliders();
            else
                CreateWallMesh();
        }

        private void CreateWallMesh() {
            var currentCollider = GetComponent<MeshCollider>();
            Destroy(currentCollider);

            CalculateMeshOutlines();

            var wallVertices = new List<Vector3>();
            var wallTriangles = new List<int>();
            var wallMesh = new UnityEngine.Mesh();
            float wallHeight = 5;

            foreach (var outline in _outlines)
                for (var i = 0; i < outline.Count - 1; i++) {
                    var startIndex = wallVertices.Count;
                    wallVertices.Add(_vertices[outline[i]]); // left
                    wallVertices.Add(_vertices[outline[i + 1]]); // right
                    wallVertices.Add(_vertices[outline[i]] - Vector3.up * wallHeight); // bottom left
                    wallVertices.Add(_vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                    wallTriangles.Add(startIndex + 0);
                    wallTriangles.Add(startIndex + 2);
                    wallTriangles.Add(startIndex + 3);

                    wallTriangles.Add(startIndex + 3);
                    wallTriangles.Add(startIndex + 1);
                    wallTriangles.Add(startIndex + 0);
                }

            wallMesh.vertices = wallVertices.ToArray();
            wallMesh.triangles = wallTriangles.ToArray();
            walls.mesh = wallMesh;

            var wallCollider = gameObject.AddComponent<MeshCollider>();
            wallCollider.sharedMesh = wallMesh;
        }

        private void Generate2DColliders() {
            var currentColliders = gameObject.GetComponents<EdgeCollider2D>();
            for (var i = 0; i < currentColliders.Length; i++) Destroy(currentColliders[i]);

            CalculateMeshOutlines();

            foreach (var outline in _outlines) {
                var edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
                var edgePoints = new Vector2[outline.Count];

                for (var i = 0; i < outline.Count; i++)
                    edgePoints[i] = new Vector2(_vertices[outline[i]].x, _vertices[outline[i]].z);
                edgeCollider.points = edgePoints;
            }
        }

        private void TriangulateSquare(Square square) {
            switch (square.configuration) {
                case 0:
                    break;

                // 1 points:
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

                // 2 points:
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
                    MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom,
                        square.bottomLeft, square.centreLeft);
                    break;
                case 10:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight,
                        square.centreBottom, square.centreLeft);
                    break;

                // 3 point:
                case 7:
                    MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft,
                        square.centreLeft);
                    break;
                case 11:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight,
                        square.bottomLeft);
                    break;
                case 13:
                    MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom,
                        square.bottomLeft);
                    break;
                case 14:
                    MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom,
                        square.centreLeft);
                    break;

                // 4 point:
                case 15:
                    MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                    _checkedVertices.Add(square.topLeft.vertexIndex);
                    _checkedVertices.Add(square.topRight.vertexIndex);
                    _checkedVertices.Add(square.bottomRight.vertexIndex);
                    _checkedVertices.Add(square.bottomLeft.vertexIndex);
                    break;
            }
        }

        private void MeshFromPoints(params Node[] points) {
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

        private void AssignVertices(Node[] points) {
            for (var i = 0; i < points.Length; i++)
                if (points[i].vertexIndex == -1) {
                    points[i].vertexIndex = _vertices.Count;
                    _vertices.Add(points[i].position);
                }
        }

        private void CreateTriangle(Node a, Node b, Node c) {
            _triangles.Add(a.vertexIndex);
            _triangles.Add(b.vertexIndex);
            _triangles.Add(c.vertexIndex);

            var triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
            AddTriangleToDictionary(triangle.vertexIndexA, triangle);
            AddTriangleToDictionary(triangle.vertexIndexB, triangle);
            AddTriangleToDictionary(triangle.vertexIndexC, triangle);
        }

        private void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle) {
            if (_triangleDictionary.ContainsKey(vertexIndexKey)) {
                _triangleDictionary[vertexIndexKey].Add(triangle);
            }
            else {
                var triangleList = new List<Triangle>();
                triangleList.Add(triangle);
                _triangleDictionary.Add(vertexIndexKey, triangleList);
            }
        }

        private void CalculateMeshOutlines() {
            for (var vertexIndex = 0; vertexIndex < _vertices.Count; vertexIndex++)
                if (!_checkedVertices.Contains(vertexIndex)) {
                    var newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                    if (newOutlineVertex != -1) {
                        _checkedVertices.Add(vertexIndex);

                        var newOutline = new List<int>();
                        newOutline.Add(vertexIndex);
                        _outlines.Add(newOutline);
                        FollowOutline(newOutlineVertex, _outlines.Count - 1);
                        _outlines[_outlines.Count - 1].Add(vertexIndex);
                    }
                }

            SimplifyMeshOutlines();
        }

        private void SimplifyMeshOutlines() {
            for (var outlineIndex = 0; outlineIndex < _outlines.Count; outlineIndex++) {
                var simplifiedOutline = new List<int>();
                var dirOld = Vector3.zero;
                for (var i = 0; i < _outlines[outlineIndex].Count; i++) {
                    var p1 = _vertices[_outlines[outlineIndex][i]];
                    var p2 = _vertices[_outlines[outlineIndex][(i + 1) % _outlines[outlineIndex].Count]];
                    var dir = p1 - p2;
                    if (dir != dirOld) {
                        dirOld = dir;
                        simplifiedOutline.Add(_outlines[outlineIndex][i]);
                    }
                }

                _outlines[outlineIndex] = simplifiedOutline;
            }
        }

        private void FollowOutline(int vertexIndex, int outlineIndex) {
            _outlines[outlineIndex].Add(vertexIndex);
            _checkedVertices.Add(vertexIndex);
            var nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

            if (nextVertexIndex != -1) FollowOutline(nextVertexIndex, outlineIndex);
        }

        private int GetConnectedOutlineVertex(int vertexIndex) {
            var trianglesContainingVertex = _triangleDictionary[vertexIndex];

            for (var i = 0; i < trianglesContainingVertex.Count; i++) {
                var triangle = trianglesContainingVertex[i];

                for (var j = 0; j < 3; j++) {
                    var vertexB = triangle[j];
                    if (vertexB != vertexIndex && !_checkedVertices.Contains(vertexB))
                        if (IsOutlineEdge(vertexIndex, vertexB))
                            return vertexB;
                }
            }

            return -1;
        }

        private bool IsOutlineEdge(int vertexA, int vertexB) {
            var trianglesContainingVertexA = _triangleDictionary[vertexA];
            var sharedTriangleCount = 0;

            for (var i = 0; i < trianglesContainingVertexA.Count; i++)
                if (trianglesContainingVertexA[i].Contains(vertexB)) {
                    sharedTriangleCount++;
                    if (sharedTriangleCount > 1) break;
                }

            return sharedTriangleCount == 1;
        }

        private struct Triangle {
            public readonly int vertexIndexA;
            public readonly int vertexIndexB;
            public readonly int vertexIndexC;
            private readonly int[] _vertices;

            public Triangle(int a, int b, int c) {
                vertexIndexA = a;
                vertexIndexB = b;
                vertexIndexC = c;

                _vertices = new int[3];
                _vertices[0] = a;
                _vertices[1] = b;
                _vertices[2] = c;
            }

            public int this[int i] => _vertices[i];


            public bool Contains(int vertexIndex) {
                return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
            }
        }

        public class SquareGrid {
            public Square[,] squares;

            public SquareGrid(int[,] map, float squareSize) {
                var nodeCountX = map.GetLength(0);
                var nodeCountY = map.GetLength(1);
                var mapWidth = nodeCountX * squareSize;
                var mapHeight = nodeCountY * squareSize;

                var controlNodes = new ControlNode[nodeCountX, nodeCountY];

                for (var x = 0; x < nodeCountX; x++)
                for (var y = 0; y < nodeCountY; y++) {
                    var pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0,
                        -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }

                squares = new Square[nodeCountX - 1, nodeCountY - 1];
                for (var x = 0; x < nodeCountX - 1; x++)
                for (var y = 0; y < nodeCountY - 1; y++)
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1],
                        controlNodes[x + 1, y], controlNodes[x, y]);
            }
        }

        public class Square {
            public Node centreTop, centreRight, centreBottom, centreLeft;
            public int configuration;

            public ControlNode topLeft, topRight, bottomRight, bottomLeft;

            public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight,
                ControlNode bottomLeft) {
                this.topLeft = topLeft;
                this.topRight = topRight;
                this.bottomRight = bottomRight;
                this.bottomLeft = bottomLeft;

                centreTop = this.topLeft.right;
                centreRight = this.bottomRight.above;
                centreBottom = this.bottomLeft.right;
                centreLeft = this.bottomLeft.above;

                if (this.topLeft.active)
                    configuration += 8;
                if (this.topRight.active)
                    configuration += 4;
                if (this.bottomRight.active)
                    configuration += 2;
                if (this.bottomLeft.active)
                    configuration += 1;
            }
        }

        public class Node {
            public Vector3 position;
            public int vertexIndex = -1;

            public Node(Vector3 pos) {
                position = pos;
            }
        }

        public class ControlNode : Node {
            public Node above, right;

            public bool active;

            public ControlNode(Vector3 pos, bool active, float squareSize) : base(pos) {
                this.active = active;
                above = new Node(position + Vector3.forward * squareSize / 2f);
                right = new Node(position + Vector3.right * squareSize / 2f);
            }
        }
    }
}