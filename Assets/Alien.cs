using UnityEngine;

public class Alien : MonoBehaviour
{
    [Header("Inscribed")]

    public GameObject bulletPrefab;
    public AlienFleet fleet;

    // Chance of firing per second (was .002 per physics step at 50 steps/sec)
    [SerializeField] private float fireChancePerSecond = .1f;
    [SerializeField] private float shotCooldown = .1f;

    private float lastShot = 0;

    void FixedUpdate()
    {
        if (Random.value < fireChancePerSecond * Time.fixedDeltaTime && Time.time - lastShot > shotCooldown)
        {
            GameObject bullet = Instantiate<GameObject>(bulletPrefab);
            bullet.transform.position = transform.position;
            lastShot = Time.time;
        }
    }

    void OnCollisionEnter(Collision coll)
    {
        fleet.RemoveAlien(this);
        Destroy(gameObject);
        Destroy(coll.gameObject);
    }
}
