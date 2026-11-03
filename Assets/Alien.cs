using UnityEngine;

public class Alien : MonoBehaviour
{
    [Header("Inscribed")]

    public GameObject bulletPrefab;
    public AlienFleet fleet;

    [Tooltip("Replace this prefab's own meshes with the code-built NieR model. Untick on custom " +
             "enemy prefabs (LevelData's Alien Prefab Override) that bring their own visuals.")]
    [SerializeField] private bool useCodeModel = true;

    // Formation column, so AlienFleet can find the lowest alien in each column
    public int Column { get; private set; }

    private float bottomOffset; // distance from this alien's position down to the bottom of its mesh
    private bool dead = false;

    // World y of the bottom of this alien, used by AlienFleet for the lose line
    public float BottomY => transform.position.y - bottomOffset;

    void Awake()
    {
        // Moved only by AlienFleet; collisions must not push it (they still fire OnCollisionEnter)
        if (TryGetComponent(out Rigidbody body))
        {
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            bottomOffset = transform.position.y - rend.bounds.min.y;
        }
    }

    // Called by AlienFleet right after spawning. Each row gets a different shape; anomaly
    // levels get corrupted, glitching versions.
    public void Initialize(AlienFleet owner, int column, int row, bool anomaly)
    {
        fleet = owner;
        Column = column;

        NierArt art = NierArt.Instance;
        if (useCodeModel && art != null)
        {
            NierModels.HideOriginalRenderers(gameObject);
            NierModels.BuildAlien(art, transform, row % 3, anomaly);
        }
    }

    // Called by AlienFleet, which decides when and which alien fires.
    // bulletSpeed <= 0 keeps the AlienBullet prefab's own speed. Dark shots can't be shot down.
    public void Fire(float bulletSpeed, bool dark)
    {
        GameObject bullet = Instantiate<GameObject>(bulletPrefab);
        bullet.transform.position = transform.position;
        if (bullet.TryGetComponent(out AlienBullet alienBullet))
        {
            alienBullet.Configure(bulletSpeed, dark);
        }
    }

    // The one way an alien dies (darts and the Pod's laser), so the fleet's counts,
    // speed-up and win check always run. Safe to call more than once.
    public void Die()
    {
        if (dead)
        {
            return;
        }
        dead = true;

        if (HitEffects.Instance != null)
        {
            HitEffects.Instance.PlayAlienDestroyed(transform.position);
        }
        fleet.RemoveAlien(this);
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision coll)
    {
        // Only the player's darts kill aliens (so an alien touching the ship doesn't destroy it)
        Dart dart = coll.collider.GetComponentInParent<Dart>();
        if (dart == null)
        {
            return;
        }

        Destroy(dart.gameObject);
        Die();
    }
}
