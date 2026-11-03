using UnityEngine;
using UnityEngine.InputSystem;

public class Spaceship : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject dartPrefab;

    [SerializeField] private float shotCooldown = .05f;
    public float speed = 10f;

    [Tooltip("Camera used to find the screen edges. Uses the Main Camera if left empty.")]
    [SerializeField] private Camera viewCamera;

    private float lastShot = 0;

    // Distances from the ship's position to the edges of its meshes (the ship isn't symmetric)
    private float leftExtent, rightExtent, topExtent;

    // World y of the top of the ship, used by AlienFleet for the lose line
    public float TopY => transform.position.y + topExtent;

    void Awake()
    {
        MeasureExtents();
    }

    void Start()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
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

        if (keyboard.spaceKey.wasPressedThisFrame && Time.time - lastShot > shotCooldown)
        {
            Vector3 position = transform.position;
            GameObject bullet = Instantiate<GameObject>(dartPrefab);
            bullet.transform.position = position;
            lastShot = Time.time;
        }
    }

    // Called by AlienBullet when it hits the ship
    public void Hit()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerHit();
        }
    }

    bool GameIsPlaying()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
    }

    // The ship's meshes are on child objects, so combine their bounds
    void MeasureExtents()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
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
