using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Support Pod: a small geometric drone (built from primitives in code) that floats beside the ship.
// Destroying targets charges it; when full, E or Left Shift fires Pod Program: Laser, a beam straight
// up that kills every alien in its path through Alien.Die(), so the fleet's counts, speed-up and win
// check all still run. PodMeter shows the charge on the HUD.
public class Pod : MonoBehaviour
{
    [Header("Inscribed")]
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private Spaceship ship;
    [Tooltip("Found automatically if left empty")]
    [SerializeField] private AlienFleet fleet;
    [Tooltip("Unlit material for the Pod and beam, e.g. Nier_Unlit. In the Editor one is created automatically if empty.")]
    [SerializeField] private Material material;

    [Header("Follow")]
    [Tooltip("Offset from the ship. The Pod switches sides to stay on the side with more room.")]
    [SerializeField] private Vector2 offset = new Vector2(2.4f, 1.2f);
    [SerializeField] private float followSmoothTime = .12f;
    [SerializeField] private float bobHeight = .12f;
    [SerializeField] private float bobSpeed = 2.5f;

    [Header("Pod Program: Laser")]
    [SerializeField] private int killsToCharge = 8;
    [SerializeField] private float laserDuration = .6f;
    [SerializeField] private float beamWidth = .9f;
    [SerializeField] private float coreWidth = .35f;
    [Tooltip("Aliens whose centre is within this distance of the beam's centre line are destroyed")]
    [SerializeField] private float hitHalfWidth = .9f;
    [SerializeField] private float beamLength = 40f;
    [SerializeField] private float screenShake = .12f;

    // (charge, killsToCharge) whenever the charge changes
    public event Action<int, int> ChargeChanged;
    // When the charge becomes full
    public event Action Ready;
    // When the laser fires
    public event Action Fired;

    public int Charge { get; private set; }
    public int KillsToCharge => killsToCharge;
    public bool IsFiring { get; private set; }
    public bool IsReady => Charge >= killsToCharge && !IsFiring;

    private GameManager gameManager;
    private Vector3 velocity;
    private float side = -1; // -1 = left of the ship, +1 = right

    private Transform beam;
    private Transform beamOuter;
    private Transform beamCore;
    private float laserTimer;
    private readonly List<Alien> hits = new List<Alien>();

    private Transform shakeCamera;
    private Vector3 cameraRestPosition;

    void Awake()
    {
        Material baseMaterial = material;
        if (baseMaterial == null)
        {
            // Shader.Find only works in builds if the shader is included, which is why the material field exists
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                baseMaterial = new Material(shader);
            }
        }
        BuildBody(baseMaterial);
        BuildBeam(baseMaterial);
    }

    void Start()
    {
        if (ship == null)
        {
            ship = FindAnyObjectByType<Spaceship>();
        }
        if (fleet == null)
        {
            fleet = FindAnyObjectByType<AlienFleet>();
        }
        if (Camera.main != null)
        {
            shakeCamera = Camera.main.transform;
        }

        gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.TargetDestroyed += OnTargetDestroyed;
            gameManager.StateChanged += OnStateChanged;
        }

        if (ship != null)
        {
            transform.position = TargetPosition();
        }
        ChargeChanged?.Invoke(Charge, killsToCharge);
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.TargetDestroyed -= OnTargetDestroyed;
            gameManager.StateChanged -= OnStateChanged;
        }
    }

    void Update()
    {
        if (ship == null)
        {
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, TargetPosition(), ref velocity, followSmoothTime);

        bool playing = gameManager == null || gameManager.IsPlaying;
        Keyboard keyboard = Keyboard.current;
        if (playing && keyboard != null && IsReady
            && (keyboard.eKey.wasPressedThisFrame || keyboard.leftShiftKey.wasPressedThisFrame))
        {
            FireLaser();
        }

        if (IsFiring)
        {
            UpdateBeamVisuals();
            laserTimer -= Time.deltaTime;
            if (laserTimer <= 0)
            {
                StopLaser();
            }
        }
    }

    // Damage in FixedUpdate, in step with the fleet's movement
    void FixedUpdate()
    {
        if (!IsFiring || fleet == null)
        {
            return;
        }

        fleet.CollectAliensInBeam(transform.position.x, hitHalfWidth, transform.position.y, hits);
        foreach (Alien alien in hits)
        {
            alien.Die(); // may clear the level, which ends the laser through OnStateChanged
        }
    }

    Vector3 TargetPosition()
    {
        Vector3 shipPosition = ship.transform.position;
        // Float on whichever side of the ship has more room
        side = shipPosition.x > 0 ? -1 : 1;
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        return new Vector3(shipPosition.x + side * offset.x, shipPosition.y + offset.y + bob, shipPosition.z);
    }

    void OnTargetDestroyed()
    {
        // Laser kills don't recharge the laser
        if (IsFiring || Charge >= killsToCharge)
        {
            return;
        }
        Charge++;
        ChargeChanged?.Invoke(Charge, killsToCharge);
        if (Charge >= killsToCharge)
        {
            Ready?.Invoke();
        }
    }

    void OnStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Playing)
        {
            StopLaser();
        }
    }

    void FireLaser()
    {
        Charge = 0;
        ChargeChanged?.Invoke(Charge, killsToCharge);
        IsFiring = true;
        laserTimer = laserDuration;
        beam.gameObject.SetActive(true);
        if (shakeCamera != null)
        {
            cameraRestPosition = shakeCamera.localPosition;
        }
        UpdateBeamVisuals();
        Fired?.Invoke();
    }

    void StopLaser()
    {
        if (!IsFiring)
        {
            return;
        }
        IsFiring = false;
        beam.gameObject.SetActive(false);
        if (shakeCamera != null)
        {
            shakeCamera.localPosition = cameraRestPosition;
        }
    }

    // Widens quickly, holds with a slight flicker, then narrows away; shakes the camera meanwhile
    void UpdateBeamVisuals()
    {
        float elapsed = laserDuration - laserTimer;
        float envelope = Mathf.Min(Mathf.Clamp01(elapsed / .08f), Mathf.Clamp01(laserTimer / .15f));
        float flicker = 1f + UnityEngine.Random.Range(-.08f, .08f);

        beamOuter.localScale = new Vector3(beamWidth * envelope * flicker, beamLength, .1f);
        beamCore.localScale = new Vector3(coreWidth * envelope, beamLength, .1f);

        if (shakeCamera != null)
        {
            shakeCamera.localPosition = cameraRestPosition + (Vector3)(UnityEngine.Random.insideUnitCircle * screenShake * envelope);
        }
    }

    // ---------- Building ----------

    // An original design: a dark diamond with an orange eye, two side fins and a small thruster
    void BuildBody(Material baseMaterial)
    {
        Transform body = new GameObject("Body").transform;
        body.SetParent(transform, false);

        CreatePart("Core", body, baseMaterial, NierStyle.InkDark, Vector3.zero, new Vector3(.5f, .5f, .3f), 45);
        CreatePart("Eye", body, baseMaterial, NierStyle.Accent, new Vector3(0, 0, -.2f), new Vector3(.16f, .16f, .1f), 45);
        CreatePart("FinLeft", body, baseMaterial, NierStyle.InkDark, new Vector3(-.48f, 0, 0), new Vector3(.1f, .55f, .2f), 0);
        CreatePart("FinRight", body, baseMaterial, NierStyle.InkDark, new Vector3(.48f, 0, 0), new Vector3(.1f, .55f, .2f), 0);
        CreatePart("Thruster", body, baseMaterial, NierStyle.Ink, new Vector3(0, -.45f, 0), new Vector3(.22f, .07f, .2f), 0);
    }

    // A bright outer beam with an orange core, drawn in front of the aliens. Hidden until fired.
    void BuildBeam(Material baseMaterial)
    {
        beam = new GameObject("LaserBeam").transform;
        beam.SetParent(transform, false);
        beam.localPosition = new Vector3(0, beamLength / 2, -.5f);

        beamOuter = CreatePart("Outer", beam, baseMaterial, NierStyle.Paper, Vector3.zero, Vector3.one, 0).transform;
        beamCore = CreatePart("Core", beam, baseMaterial, NierStyle.Accent, new Vector3(0, 0, -.1f), Vector3.one, 0).transform;
        beam.gameObject.SetActive(false);
    }

    GameObject CreatePart(string name, Transform parent, Material baseMaterial, Color color,
        Vector3 position, Vector3 scale, float rotationZ)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        // Purely visual, so bullets pass through. Immediate, so a bullet can't hit (and destroy) a part this frame.
        DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(0, 0, rotationZ);
        part.transform.localScale = scale;

        MeshRenderer partRenderer = part.GetComponent<MeshRenderer>();
        partRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        partRenderer.receiveShadows = false;
        if (baseMaterial != null)
        {
            Material partMaterial = new Material(baseMaterial);
            partMaterial.color = color;
            partMaterial.SetColor("_BaseColor", color); // URP's name for the main color
            partRenderer.sharedMaterial = partMaterial;
        }
        return part;
    }
}
