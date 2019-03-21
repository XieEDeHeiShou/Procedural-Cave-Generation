using UnityEngine;

namespace Mesh.Generator {
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
}