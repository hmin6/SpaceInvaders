using UnityEngine;

// An alien shot. Orange shots are destructible: a dart that hits one destroys both. Dark shots
// are indestructible: they destroy the dart and keep going, so they have to be dodged.
public class AlienBullet : MonoBehaviour
{
    [Header("Inscribed")]

    // Units per second. Was 5 (.1 per physics step); raised for difficulty, see CODE_ISSUES.md
    [SerializeField] private float speed = 9f;

    public bool IsDark { get; private set; }

    private bool modelBuilt = false;

    // Moved only by this script; collisions must not push it (they still fire OnCollisionEnter)
    void Awake()
    {
        if (TryGetComponent(out Rigidbody body))
        {
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    // Called by Alien.Fire right after spawning. bulletSpeed <= 0 keeps the prefab's speed.
    public void Configure(float bulletSpeed, bool dark)
    {
        if (bulletSpeed > 0)
        {
            speed = bulletSpeed;
        }
        IsDark = dark;
        BuildModel();
    }

    // For bullets spawned without Configure
    void Start()
    {
        BuildModel();
    }

    void BuildModel()
    {
        NierArt art = NierArt.Instance;
        if (modelBuilt || art == null)
        {
            return;
        }
        modelBuilt = true;
        NierModels.HideOriginalRenderers(gameObject);
        NierModels.BuildAlienBullet(art, transform, IsDark);
    }

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
        // Alien shots pass through each other
        if (coll.collider.GetComponentInParent<AlienBullet>() != null)
        {
            return;
        }

        Spaceship ship = coll.collider.GetComponentInParent<Spaceship>();
        if (ship != null)
        {
            // The ship isn't destroyed here; it reports the hit and GameManager decides what happens
            ship.Hit();
            Destroy(gameObject);
            return;
        }

        Dart dart = coll.collider.GetComponentInParent<Dart>();
        if (dart != null)
        {
            Destroy(dart.gameObject);
            if (!IsDark)
            {
                Destroy(gameObject);
            }
            return;
        }

        Destroy(gameObject);
    }
}
