using UnityEngine;

public class AlienBullet : MonoBehaviour
{
    [Header("Inscribed")]

    // Units per second. Was 5 (.1 per physics step); raised for difficulty, see CODE_ISSUES.md
    [SerializeField] private float speed = 9f;

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
