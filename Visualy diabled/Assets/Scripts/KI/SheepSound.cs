using UnityEngine;

public class SheepSound : MonoBehaviour
{
    private AudioSource audioSource;
    private Camera cam;
    public float maxDistance = 10f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        cam = Camera.main;
    }

  public float minInterval = 2f;
public float maxInterval = 5f;
private float nextPlayTime = 0f;

void Update()
{
    Ray ray = new Ray(cam.transform.position, cam.transform.forward);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, maxDistance))
    {
        if (hit.transform == transform)
        {
            if (Time.time >= nextPlayTime)
            {
                audioSource.Play();
                nextPlayTime = Time.time + Random.Range(minInterval, maxInterval);
            }
        }
    }
}
}

