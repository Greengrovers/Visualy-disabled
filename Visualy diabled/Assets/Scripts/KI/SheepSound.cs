using UnityEngine;

public class SheepSound : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    public float maxDistance = 10f;
    public float minInterval = 2f;
    public float maxInterval = 5f;

    private Camera cam;
    private float nextPlayTime = 0f;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        cam = Camera.main;
        nextPlayTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    void Update()
    {
        if (audioSource == null || cam == null) return;
        if (Time.time < nextPlayTime || audioSource.isPlaying) return;

        // Distanzbasiert statt Raycast: Schaf macht Geraeusch wenn nah genug an der Kamera.
        float distanceSqr = (cam.transform.position - transform.position).sqrMagnitude;

        if (distanceSqr <= maxDistance * maxDistance)
        {
            audioSource.Play();
            nextPlayTime = Time.time + Random.Range(minInterval, maxInterval);
        }
    }
}