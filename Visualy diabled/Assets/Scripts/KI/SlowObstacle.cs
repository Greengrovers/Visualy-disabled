using UnityEngine;

public class SlowArea : MonoBehaviour
{
    public float slowMultiplier = 0.5f; // 50% langsamer

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<SheepController>(out var sheep))
        {
            sheep.SetSlowMultiplier(slowMultiplier);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<SheepController>(out var sheep))
        {
            sheep.SetSlowMultiplier(1f); // normal speed
        }
    }
}