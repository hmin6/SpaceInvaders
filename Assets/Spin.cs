using UnityEngine;

// Rotates this object continuously (game time, so it stops while the game is frozen)
public class Spin : MonoBehaviour
{
    public Vector3 axis = Vector3.forward;
    [Tooltip("Degrees per second")]
    public float speed = 90f;

    void Update()
    {
        transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
    }
}
