using UnityEngine;
using System.Collections.Generic;

public class AlienFleet : MonoBehaviour
{
    [Header("Inscribed")]

    public GameObject alienPrefab;

    public AlienCounter alienCounter;

    private List<Alien> aliens = new List<Alien>();

    float speed = 5f; // units per second (was .1 per physics step at 50 steps/sec)
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

        alienCounter.SetRemaining(aliens.Count);
    }

    public void RemoveAlien(Alien alien)
    {
        // Remove returns false if this alien was already removed (e.g. hit twice in one frame)
        if (aliens.Remove(alien))
        {
            alienCounter.SetRemaining(aliens.Count);
        }
    }

    void FixedUpdate()
    {
        float step = speed * Time.fixedDeltaTime;

        //move all aliens
        foreach (Alien alien in aliens)
        {
            Vector3 position = alien.transform.position;
            if (!movingDown)
            {
                position.x += step;
            }
            else
            {
                position.y += -Mathf.Abs(step);
            }
            alien.transform.position = position;
        }

        //change alien direction
        float left = 0, right = 0;
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
        }
        if (speed > 0 && right >= rightBound || speed < 0 && left <= leftBound)
        {
            speed = -speed;
            movingDown = true;
            Invoke("startSidewaysMotion", downTime);
        }
    }

    void startSidewaysMotion()
    {
        movingDown = false;
    }
}
