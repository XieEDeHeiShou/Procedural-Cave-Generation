using UnityEngine;

namespace Mesh.Generator {
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