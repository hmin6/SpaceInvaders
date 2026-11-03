using UnityEngine;

// Square-particle bursts and a quick flash, NieR hacking-minigame style. The particle systems are
// created and configured in code; call PlayAlienDestroyed / PlayShipHit through HitEffects.Instance.
public class HitEffects : MonoBehaviour
{
    public static HitEffects Instance { get; private set; }

    [Tooltip("Material for the particles, e.g. one using Universal Render Pipeline/Particles/Unlit. " +
             "Needed for builds; in the Editor one is created automatically if this is empty.")]
    [SerializeField] private Material particleMaterial;

    [Header("Alien Destroyed")]
    [SerializeField] private int alienDebris = 12;
    [SerializeField] private int alienAccentDebris = 4;
    [SerializeField] private float alienFlashSize = 1.6f;

    [Header("Ship Hit")]
    [SerializeField] private int shipDebris = 18;
    [SerializeField] private int shipDarkDebris = 8;
    [SerializeField] private float shipFlashSize = 3f;

    private ParticleSystem debris;
    private ParticleSystem flash;

    // Enter Play Mode Options has domain reload disabled, so reset the static here
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
        {
            Debug.LogWarning("More than one HitEffects in the scene; destroying the extra one.", this);
            Destroy(this);
            return;
        }
        Instance = this;

        Material material = particleMaterial;
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                material = new Material(shader);
            }
            else
            {
                Debug.LogWarning("HitEffects: no particle material assigned and the URP particle shader wasn't found.", this);
            }
        }

        debris = CreateDebrisSystem(material);
        flash = CreateFlashSystem(material);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayAlienDestroyed(Vector3 position)
    {
        position.z -= 1; // in front of the meshes
        Emit(flash, position, NierStyle.Paper, 1, alienFlashSize);
        Emit(debris, position, NierStyle.InkDark, alienDebris);
        Emit(debris, position, NierStyle.Accent, alienAccentDebris);
    }

    public void PlayShipHit(Vector3 position)
    {
        position.z -= 1;
        Emit(flash, position, NierStyle.Accent, 1, shipFlashSize);
        Emit(debris, position, NierStyle.Accent, shipDebris);
        Emit(debris, position, NierStyle.InkDark, shipDarkDebris);
    }

    void Emit(ParticleSystem system, Vector3 position, Color color, int count, float size = -1)
    {
        system.transform.position = position;
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.startColor = color;
        if (size > 0)
        {
            emitParams.startSize = size;
        }
        system.Emit(emitParams, count);
    }

    // Small spinning squares that fly outward and shrink away
    ParticleSystem CreateDebrisSystem(Material material)
    {
        ParticleSystem system = CreateSystem("Debris", material);

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.3f, .6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(.12f, .3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .2f;

        ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-8f, 8f);

        ShrinkOverLifetime(system);
        system.Play();
        return system;
    }

    // One large square, turned 45 degrees, that shrinks away almost immediately
    ParticleSystem CreateFlashSystem(Material material)
    {
        ParticleSystem system = CreateSystem("Flash", material);

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = .12f;
        main.startSpeed = 0;
        main.startSize = 1.6f;
        main.startRotation = Mathf.PI / 4;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false;

        ShrinkOverLifetime(system);
        system.Play();
        return system;
    }

    ParticleSystem CreateSystem(string name, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.playOnAwake = false;
        main.loop = true; // keeps the system running; particles only come from Emit()
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true; // so the last hit still animates on the frozen end screen
        main.gravityModifier = 0;
        main.maxParticles = 1000;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        ParticleSystemRenderer particleRenderer = go.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (material != null)
        {
            particleRenderer.sharedMaterial = material;
        }
        return system;
    }

    static void ShrinkOverLifetime(ParticleSystem system)
    {
        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
    }
}
