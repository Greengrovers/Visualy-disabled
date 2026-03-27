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

    void Update()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            if (hit.transform == transform)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
                else
                {
                 if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                }       
            }
        }
    }
}
