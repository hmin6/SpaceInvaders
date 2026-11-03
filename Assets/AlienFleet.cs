using UnityEngine;
using System.Collections.Generic;

public class AlienFleet : MonoBehaviour
{
    [Header("Inscribed")]

    public GameObject alienPrefab;

    public AlienCounter alienCounter;

    // Used for the lose line. Found automatically if left empty.
    public Spaceship spaceship;

    [Header("Speed")]
    [Tooltip("Sideways speed in units/sec with the full fleet. Also the speed the fleet steps down at.")]
    [SerializeField] private float baseSpeed = 5f;
    [Tooltip("Speed multiplier when only one alien is left")]
    [SerializeField] private float maxSpeedMultiplier = 6f;
    [Tooltip("1 = same speed-up per kill. Higher values save more of the speed-up for the last few aliens.")]
    [SerializeField] private float speedUpExponent = 2f;

    [Header("Lose Line")]
    [Tooltip("How far above the top of the spaceship the bottom of the lowest alien can reach before the game is lost")]
    [SerializeField] private float loseLineOffset = 0.5f;

    private List<Alien> aliens = new List<Alien>();
    private int startingCount;

    float speed;         // current sideways speed, units per second
    float direction = 1; // +1 = moving right, -1 = moving left
    float leftBound = -16;
    float rightBound = 16;
    float downTime = .1f;
    bool movingDown = false;

    void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                GameObject alienGO = Instantiate<GameObject>(alienPrefab);
                alienGO.transform.position = new Vector3(2 * i - 12, 10 - 2 * j, 0);
                Alien alien = alienGO.GetComponent<Alien>();
                alien.fleet = this;
                aliens.Add(alien);
            }
        }
        startingCount = aliens.Count;
        UpdateSpeed();

        alienCounter.SetRemaining(aliens.Count);

        if (spaceship == null)
        {
            spaceship = FindAnyObjectByType<Spaceship>();
        }
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("No GameManager in the scene, so winning and losing won't do anything.", this);
        }
    }

    public void RemoveAlien(Alien alien)
    {
        // Remove returns false if this alien was already removed (e.g. hit twice in one frame)
        if (aliens.Remove(alien))
        {
            alienCounter.SetRemaining(aliens.Count);

            if (aliens.Count == 0)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.Win();
                }
            }
            else
            {
                UpdateSpeed();
            }
        }
    }

    // Like classic Space Invaders, the fleet speeds up as aliens are destroyed.
    // t goes from 0 with the full fleet to 1 when only one alien is left.
    void UpdateSpeed()
    {
        float t = startingCount > 1 ? 1f - (aliens.Count - 1) / (float)(startingCount - 1) : 1f;
        speed = baseSpeed * Mathf.Lerp(1f, maxSpeedMultiplier, Mathf.Pow(t, speedUpExponent));
    }

    void FixedUpdate()
    {
        float sideStep = direction * speed * Time.fixedDeltaTime;
        // Step down at the base speed so each drop is the same distance however fast the fleet is going
        float downStep = baseSpeed * Time.fixedDeltaTime;

        //move all aliens
        foreach (Alien alien in aliens)
        {
            Vector3 position = alien.transform.position;
            if (!movingDown)
            {
                position.x += sideStep;
            }
            else
            {
                position.y -= downStep;
            }
            alien.transform.position = position;
        }

        //find the fleet's edges and lowest point
        float left = 0, right = 0;
        float lowest = float.MaxValue;
        foreach (Alien alien in aliens)
        {
            Vector3 position = alien.transform.position;
            if (position.x > right)
            {
                right = position.x;
            }
            if (position.x < left)
            {
                left = position.x;
            }
            lowest = Mathf.Min(lowest, alien.BottomY);
        }

        //lose if the aliens get down to the spaceship
        if (spaceship != null && lowest <= spaceship.TopY + loseLineOffset)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
            return;
        }

        //change alien direction
        if (direction > 0 && right >= rightBound || direction < 0 && left <= leftBound)
        {
            direction = -direction;
            movingDown = true;
            Invoke("startSidewaysMotion", downTime);
        }
    }

    void startSidewaysMotion()
    {
        movingDown = false;
    }
}
