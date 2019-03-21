using UnityEngine;

public class Player2D : MonoBehaviour {
    private Rigidbody2D _rigidbody;
    private Vector2 _velocity;

    private void Start() {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        _velocity = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized * 10;
    }

    private void FixedUpdate() {
        _rigidbody.MovePosition(_rigidbody.position + _velocity * Time.fixedDeltaTime);
    }
}