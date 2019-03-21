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

            const int tileAmount = 10;
            var uvs = new Vector2[_vertices.Count];
            for (var i = 0; i < _vertices.Count; i++) {
                var f = map.GetLength(0) * squareSize / 2;
                var percentX = Mathf.InverseLerp(-f, f, _vertices[i].x) * tileAmount;
                var percentY = Mathf.InverseLerp(-f, f, _vertices[i].z) * tileAmount;
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
            const float wallHeight = 5;

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
            foreach (var item in currentColliders)
                Destroy(item);

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
                // ReSharper disable once RedundantEmptySwitchSection  High frequency context
                default:
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

        private void AssignVertices(IEnumerable<Node> points) {
            foreach (var item in points)
                if (item.vertexIndex == -1) {
                    item.vertexIndex = _vertices.Count;
                    _vertices.Add(item.position);
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
                var triangleList = new List<Triangle> {triangle};
                _triangleDictionary.Add(vertexIndexKey, triangleList);
            }
        }

        private void CalculateMeshOutlines() {
            for (var vertexIndex = 0; vertexIndex < _vertices.Count; vertexIndex++)
                if (!_checkedVertices.Contains(vertexIndex)) {
                    var newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                    if (newOutlineVertex == -1) continue;
                    _checkedVertices.Add(vertexIndex);

                    var newOutline = new List<int> {vertexIndex};
                    _outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, _outlines.Count - 1);
                    _outlines[_outlines.Count - 1].Add(vertexIndex);
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
                    if (dir == dirOld) continue;
                    dirOld = dir;
                    simplifiedOutline.Add(_outlines[outlineIndex][i]);
                }

                _outlines[outlineIndex] = simplifiedOutline;
            }
        }

        private void FollowOutline(int vertexIndex, int outlineIndex) {
            while (true) {
                _outlines[outlineIndex].Add(vertexIndex);
                _checkedVertices.Add(vertexIndex);
                var nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

                if (nextVertexIndex != -1) {
                    vertexIndex = nextVertexIndex;
                    continue;
                }

                break;
            }
        }

        private int GetConnectedOutlineVertex(int vertexIndex) {
            var trianglesContainingVertex = _triangleDictionary[vertexIndex];

            foreach (var triangle in trianglesContainingVertex)
                for (var j = 0; j < 3; j++) {
                    var vertexB = triangle[j];
                    if (vertexB == vertexIndex || _checkedVertices.Contains(vertexB)) continue;
                    if (IsOutlineEdge(vertexIndex, vertexB))
                        return vertexB;
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
    }
}