using UnityEngine;

public class Dart : MonoBehaviour
{
    [Header("Inscribed")]

    // Units per second (was .3 per physics step at 50 steps/sec)
    [SerializeField] private float speed = 15f;

    void FixedUpdate()
    {
        Vector3 position = transform.position;
        position.y += speed * Time.fixedDeltaTime;
        transform.position = position;
        if (position.y > 10)
        {
            Destroy(gameObject);
        }
    }
}
