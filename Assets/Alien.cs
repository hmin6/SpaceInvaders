using UnityEngine;

public class Alien : MonoBehaviour
{
    [Header("Inscribed")]

    public GameObject bulletPrefab;
    public AlienFleet fleet;

    // Chance of firing per second (was .002 per physics step at 50 steps/sec)
    [SerializeField] private float fireChancePerSecond = .1f;
    [SerializeField] private float shotCooldown = .1f;

    private float lastShot = 0;
    private float bottomOffset; // distance from this alien's position down to the bottom of its mesh

    // World y of the bottom of this alien, used by AlienFleet for the lose line
    public float BottomY => transform.position.y - bottomOffset;

    void Awake()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            bottomOffset = transform.position.y - rend.bounds.min.y;
        }
    }

    void FixedUpdate()
    {
        if (Random.value < fireChancePerSecond * Time.fixedDeltaTime && Time.time - lastShot > shotCooldown)
        {
            GameObject bullet = Instantiate<GameObject>(bulletPrefab);
            bullet.transform.position = transform.position;
            lastShot = Time.time;
        }
    }

    void OnCollisionEnter(Collision coll)
    {
        // Only the player's darts kill aliens (so an alien touching the ship doesn't destroy it)
        Dart dart = coll.collider.GetComponentInParent<Dart>();
        if (dart == null)
        {
            return;
        }

        fleet.RemoveAlien(this);
        Destroy(gameObject);
        Destroy(dart.gameObject);
    }
}
