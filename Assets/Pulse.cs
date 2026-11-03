using UnityEngine;

// Gently grows and shrinks this object, e.g. for a glowing core (unscaled time, so it keeps
// pulsing on frozen screens)
public class Pulse : MonoBehaviour
{
    [Tooltip("Fraction of the base size added at the peak")]
    public float amount = .18f;
    [Tooltip("Pulses per second")]
    public float speed = 1.5f;

    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float wave = .5f + .5f * Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2);
        transform.localScale = baseScale * (1 + amount * wave);
    }
}
