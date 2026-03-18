using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("Trap Settings")]
    public float triggerRadius = 1.5f;
    public LayerMask sheepLayer;

    [Header("Optional")]
    public bool destroyTrapAfterUse = false;

    private bool hasTriggered = false;

    void Update()
    {
        if (hasTriggered) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius, sheepLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            GameObject sheepRoot = hits[i].transform.root.gameObject;
            TriggerTrap(sheepRoot);
        }
    }

    void TriggerTrap(GameObject sheepRoot)
    {
        hasTriggered = true;

        Destroy(sheepRoot);

        if (destroyTrapAfterUse)
        {
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}