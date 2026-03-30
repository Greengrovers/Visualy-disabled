using UnityEngine;

public class SheepGroupManager : MonoBehaviour
{
    public static SheepGroupManager Instance { get; private set; }

    [Header("Shared Flee Direction")]
    public Vector3 sharedFleeDirection = Vector3.zero;
    public bool hasSharedDirection = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetSharedFleeDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        sharedFleeDirection = direction.normalized;
        hasSharedDirection = true;
    }

    public void ClearSharedFleeDirection()
    {
        sharedFleeDirection = Vector3.zero;
        hasSharedDirection = false;
    }
}