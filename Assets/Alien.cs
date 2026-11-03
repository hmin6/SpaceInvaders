using UnityEngine;

public class Alien : MonoBehaviour
{
    [Header("Inscribed")]

    public GameObject bulletPrefab;
    public AlienFleet fleet;

    // Formation column, set by AlienFleet so it can find the lowest alien in each column
    public int Column { get; set; }

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

    // Called by AlienFleet, which decides when and which alien fires
    public void Fire()
    {
        GameObject bullet = Instantiate<GameObject>(bulletPrefab);
        bullet.transform.position = transform.position;
    }

    void OnCollisionEnter(Collision coll)
    {
        // Only the player's darts kill aliens (so an alien touching the ship doesn't destroy it)
        Dart dart = coll.collider.GetComponentInParent<Dart>();
        if (dart == null)
        {
            return;
        }

        if (HitEffects.Instance != null)
        {
            HitEffects.Instance.PlayAlienDestroyed(transform.position);
        }
        fleet.RemoveAlien(this);
        Destroy(gameObject);
        Destroy(dart.gameObject);
    }
}
