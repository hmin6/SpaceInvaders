using UnityEngine;
using System.Collections.Generic;

public class AlienFleet : MonoBehaviour
{
    [Header("Inscribed")]

    public GameObject alienPrefab;

    public AlienCounter alienCounter;

    // Used for the lose line and aimed shots. Found automatically if left empty.
    public Spaceship spaceship;

    [Header("Formation")]
    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 3;

    [Header("Speed")]
    [Tooltip("Sideways speed in units/sec with the full fleet. Also the speed the fleet steps down at.")]
    [SerializeField] private float baseSpeed = 5f;
    [Tooltip("Speed multiplier when only one alien is left")]
    [SerializeField] private float maxSpeedMultiplier = 5f;
    [Tooltip("1 = same speed-up per kill. Higher values save more of the speed-up for the last few aliens.")]
    [SerializeField] private float speedUpExponent = 1.5f;

    [Header("Firing")]
    [Tooltip("Shots per second from the whole fleet with the full fleet")]
    [SerializeField] private float baseFireRate = 1.5f;
    [Tooltip("Shots per second from the whole fleet when only one alien is left")]
    [SerializeField] private float maxFireRate = 3.5f;
    [Tooltip("Chance that a shot comes from the column nearest the player instead of a random column")]
    [Range(0, 1)]
    [SerializeField] private float aimedShotChance = 0.35f;
    [Tooltip("Alien bullet speed in units/sec. 0 = use the AlienBullet prefab's speed.")]
    [SerializeField] private float alienBulletSpeed = 0f;

    [Header("Lose Line")]
    [Tooltip("How far above the top of the spaceship the bottom of the lowest alien can reach before the game is lost")]
    [SerializeField] private float loseLineOffset = 0.5f;

    private List<Alien> aliens = new List<Alien>();
    private int startingCount;
    private float loseLineY; // world y the aliens' bottom edge must not reach, set in Start

    // Reused by TryFire so firing doesn't allocate
    private Alien[] lowestInColumn;
    private List<Alien> shooters = new List<Alien>();

    float speed;         // current sideways speed, units per second
    float fireRate;      // current fleet-wide shots per second
    float direction = 1; // +1 = moving right, -1 = moving left
    float leftBound = -16;
    float rightBound = 16;
    float downTime = .1f;
    bool movingDown = false;

    void Start()
    {
        // The settings above are used as-is when GameManager has no levels
        GameObject prefab = alienPrefab;
        if (GameManager.Instance != null && GameManager.Instance.CurrentLevel != null)
        {
            LevelData level = GameManager.Instance.CurrentLevel;
            ApplyLevel(level);
            if (level.AlienPrefabOverride != null)
            {
                prefab = level.AlienPrefabOverride;
            }
        }

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                GameObject alienGO = Instantiate<GameObject>(prefab);
                alienGO.transform.position = new Vector3(2 * i - 12, 10 - 2 * j, 0);
                Alien alien = alienGO.GetComponent<Alien>();
                alien.fleet = this;
                alien.Column = i;
                aliens.Add(alien);
            }
        }
        startingCount = aliens.Count;
        lowestInColumn = new Alien[columns];
        UpdateDifficulty();

        alienCounter.SetRemaining(aliens.Count);

        if (spaceship == null)
        {
            spaceship = FindAnyObjectByType<Spaceship>();
        }

        // Fixed at level start, so nothing that happens to the ship later can move the line
        if (spaceship != null)
        {
            loseLineY = spaceship.TopY + loseLineOffset;
        }
        else
        {
            loseLineY = float.NegativeInfinity;
            Debug.LogWarning("AlienFleet: no Spaceship found, so the aliens can't land.", this);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterAliens(startingCount);
        }
        else
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AlienDestroyed();
            }

            if (aliens.Count == 0)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LevelCleared();
                }
            }
            else
            {
                UpdateDifficulty();
            }
        }
    }

    // Copies the level's settings over this fleet's Inspector values
    void ApplyLevel(LevelData level)
    {
        columns = level.Columns;
        rows = level.Rows;
        baseSpeed = level.BaseSpeed;
        maxSpeedMultiplier = level.MaxSpeedMultiplier;
        speedUpExponent = level.SpeedUpExponent;
        baseFireRate = level.BaseFireRate;
        maxFireRate = level.MaxFireRate;
        aimedShotChance = level.AimedShotChance;
        alienBulletSpeed = level.AlienBulletSpeed;
    }

    // Like classic Space Invaders, the fleet gets faster and fires more as aliens are destroyed.
    // t goes from 0 with the full fleet to 1 when only one alien is left.
    void UpdateDifficulty()
    {
        float t = startingCount > 1 ? 1f - (aliens.Count - 1) / (float)(startingCount - 1) : 1f;
        speed = baseSpeed * Mathf.Lerp(1f, maxSpeedMultiplier, Mathf.Pow(t, speedUpExponent));
        fireRate = Mathf.Lerp(baseFireRate, maxFireRate, t);
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

        //lose if the aliens get down to the spaceship's line
        if (lowest <= loseLineY)
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

        TryFire();
    }

    // The fleet fires at fireRate shots/sec on average. Only the lowest alien in each column can fire,
    // and some shots come from the column nearest the player.
    void TryFire()
    {
        if (aliens.Count == 0 || Random.value >= fireRate * Time.fixedDeltaTime)
        {
            return;
        }

        System.Array.Clear(lowestInColumn, 0, lowestInColumn.Length);
        foreach (Alien alien in aliens)
        {
            Alien current = lowestInColumn[alien.Column];
            if (current == null || alien.transform.position.y < current.transform.position.y)
            {
                lowestInColumn[alien.Column] = alien;
            }
        }

        shooters.Clear();
        foreach (Alien alien in lowestInColumn)
        {
            if (alien != null)
            {
                shooters.Add(alien);
            }
        }

        Alien shooter;
        if (spaceship != null && Random.value < aimedShotChance)
        {
            shooter = ClosestToX(shooters, spaceship.transform.position.x);
        }
        else
        {
            shooter = shooters[Random.Range(0, shooters.Count)];
        }
        shooter.Fire(alienBulletSpeed);
    }

    // Fills results with every alien within halfWidth of x and above minY (for the Pod's laser).
    // Callers kill them afterwards, since killing removes them from the list being searched.
    public void CollectAliensInBeam(float x, float halfWidth, float minY, List<Alien> results)
    {
        results.Clear();
        foreach (Alien alien in aliens)
        {
            Vector3 position = alien.transform.position;
            if (Mathf.Abs(position.x - x) <= halfWidth && position.y >= minY)
            {
                results.Add(alien);
            }
        }
    }

    static Alien ClosestToX(List<Alien> candidates, float x)
    {
        Alien closest = candidates[0];
        for (int i = 1; i < candidates.Count; i++)
        {
            if (Mathf.Abs(candidates[i].transform.position.x - x) < Mathf.Abs(closest.transform.position.x - x))
            {
                closest = candidates[i];
            }
        }
        return closest;
    }

    void startSidewaysMotion()
    {
        movingDown = false;
    }
}
