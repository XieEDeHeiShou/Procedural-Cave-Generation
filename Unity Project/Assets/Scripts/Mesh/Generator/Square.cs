namespace Mesh.Generator {
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
}