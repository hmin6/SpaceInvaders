using UnityEngine;

public class AlienBullet : MonoBehaviour
{
    [Header("Inscribed")]

    // Units per second (was .1 per physics step at 50 steps/sec)
    [SerializeField] private float speed = 5f;

    void FixedUpdate()
    {
        Vector3 position = transform.position;
        position.y -= speed * Time.fixedDeltaTime;
        transform.position = position;
        if (position.y < -10)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision coll)
    {
        Spaceship ship = coll.collider.GetComponentInParent<Spaceship>();
        if (ship != null)
        {
            // The ship isn't destroyed here; it reports the hit and GameManager decides what happens
            ship.Hit();
        }
        else
        {
            Destroy(coll.gameObject); // e.g. a Dart
        }
        Destroy(gameObject);
    }
}
