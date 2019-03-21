namespace Mesh.Generator {
    public struct Triangle {
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
}