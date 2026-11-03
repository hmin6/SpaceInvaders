using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Spaceship : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject dartPrefab;

    // Was .05; raised for difficulty, see CODE_ISSUES.md
    [SerializeField] private float shotCooldown = .25f;
    [Tooltip("Most darts that can be on screen at once. 1 = classic Space Invaders.")]
    [SerializeField] private int maxDartsOnScreen = 2;
    public float speed = 10f;

    [Header("Invulnerability")]
    [Tooltip("Seconds the ship can't be hit after losing a life")]
    [SerializeField] private float invulnerableDuration = 1.5f;
    [Tooltip("Seconds between blinks while invulnerable")]
    [SerializeField] private float blinkInterval = .1f;

    [Tooltip("Camera used to find the screen edges. Uses the Main Camera if left empty.")]
    [SerializeField] private Camera viewCamera;

    private float lastShot = 0;
    private List<GameObject> darts = new List<GameObject>();

    private Renderer[] renderers;
    private bool invulnerable = false;
    private Coroutine invulnerabilityRoutine;
    private GameManager gameManager;

    // Distances from the ship's position to the edges of its meshes (the ship isn't symmetric)
    private float leftExtent, rightExtent, topExtent;

    // World y of the top of the ship, used by AlienFleet for the lose line
    public float TopY => transform.position.y + topExtent;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        MeasureExtents();
    }

    void Start()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.StateChanged += OnGameStateChanged;
        }
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged -= OnGameStateChanged;
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !GameIsPlaying())
        {
            return;
        }

        if (keyboard.rightArrowKey.isPressed)
        {
            Vector3 position = transform.position;
            position.x += speed * Time.deltaTime;
            transform.position = position;
        }
        if (keyboard.leftArrowKey.isPressed)
        {
            Vector3 position = transform.position;
            position.x -= speed * Time.deltaTime;
            transform.position = position;
        }
        // Clamp every frame (not only when moving) so resizing the Game view can't leave the ship off-screen
        ClampToScreen();

        if (keyboard.spaceKey.wasPressedThisFrame && Time.time - lastShot > shotCooldown
            && ActiveDartCount() < maxDartsOnScreen)
        {
            Vector3 position = transform.position;
            GameObject bullet = Instantiate<GameObject>(dartPrefab);
            bullet.transform.position = position;
            darts.Add(bullet);
            lastShot = Time.time;
        }
    }

    // Darts destroy themselves (off-screen or on hitting something), so drop the destroyed ones first
    int ActiveDartCount()
    {
        for (int i = darts.Count - 1; i >= 0; i--)
        {
            if (darts[i] == null)
            {
                darts.RemoveAt(i);
            }
        }
        return darts.Count;
    }

    // Called by AlienBullet when it hits the ship
    public void Hit()
    {
        if (invulnerable || !GameIsPlaying())
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerHit();
        }

        // Only blink if that hit didn't end the game
        if (GameIsPlaying())
        {
            invulnerabilityRoutine = StartCoroutine(Invulnerability());
        }
    }

    IEnumerator Invulnerability()
    {
        invulnerable = true;
        WaitForSeconds wait = new WaitForSeconds(blinkInterval);
        float endTime = Time.time + invulnerableDuration;
        bool visible = true;

        while (Time.time < endTime)
        {
            visible = !visible;
            SetVisible(visible);
            yield return wait;
        }

        invulnerabilityRoutine = null;
        EndInvulnerability();
    }

    void EndInvulnerability()
    {
        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
            invulnerabilityRoutine = null;
        }
        invulnerable = false;
        SetVisible(true);
    }

    // If the game ends mid-blink (e.g. the aliens land), make sure the ship is left visible
    void OnGameStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Playing)
        {
            EndInvulnerability();
        }
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer rend in renderers)
        {
            rend.enabled = visible;
        }
    }

    bool GameIsPlaying()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
    }

    // The ship's meshes are on child objects, so combine their bounds
    void MeasureExtents()
    {
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 position = transform.position;
        leftExtent = position.x - bounds.min.x;
        rightExtent = bounds.max.x - position.x;
        topExtent = bounds.max.y - position.y;
    }

    void ClampToScreen()
    {
        if (viewCamera == null)
        {
            return;
        }

        Vector3 position = transform.position;
        float minX = ScreenEdgeX(0) + leftExtent;
        float maxX = ScreenEdgeX(1) - rightExtent;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        transform.position = position;
    }

    // World x of the left (viewportX = 0) or right (viewportX = 1) edge of the screen at the ship's
    // height and depth. Works for orthographic and perspective cameras.
    float ScreenEdgeX(float viewportX)
    {
        float viewportY = viewCamera.WorldToViewportPoint(transform.position).y;
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0));
        Plane shipPlane = new Plane(Vector3.forward, transform.position);
        shipPlane.Raycast(ray, out float distance);
        return ray.GetPoint(distance).x;
    }
}
