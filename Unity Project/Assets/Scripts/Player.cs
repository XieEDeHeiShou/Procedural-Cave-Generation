using UnityEngine;

public class Player : MonoBehaviour {
    private Rigidbody _rigidbody;
    private Vector3 _velocity;

    private void Start() {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Update() {
        _velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * 10;
    }

    private void FixedUpdate() {
        _rigidbody.MovePosition(_rigidbody.position + _velocity * Time.fixedDeltaTime);
    }
}