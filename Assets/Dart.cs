using UnityEngine;

public class Dart : MonoBehaviour
{
    [Header("Inscribed")]

    // Units per second (was .3 per physics step at 50 steps/sec)
    [SerializeField] private float speed = 15f;

    [Header("Trail")]
    [SerializeField] private float trailTime = .12f;
    [SerializeField] private float trailWidth = .1f;

    // Moved only by this script; collisions must not push it (they still fire OnCollisionEnter)
    void Awake()
    {
        if (TryGetComponent(out Rigidbody body))
        {
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        NierArt art = NierArt.Instance;
        if (art != null)
        {
            NierModels.HideOriginalRenderers(gameObject);
            NierModels.BuildDart(art, transform);
            AddTrail(art);
        }
    }

    // A short trail that narrows and blends from the dart's color into the background,
    // which reads as fading on the opaque unlit material
    void AddTrail(NierArt art)
    {
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = art.GetMaterial(Color.white); // white, so the vertex colors below show as-is
        trail.time = trailTime;
        trail.minVertexDistance = .05f;
        trail.widthMultiplier = trailWidth;
        trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        Gradient colors = new Gradient();
        colors.SetKeys(
            new[] { new GradientColorKey(NierStyle.Hull, 0), new GradientColorKey(NierStyle.Background, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        trail.colorGradient = colors;
    }

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
