using UnityEngine;

public class Trap : MonoBehaviour
{
    public float triggerRadius = 1.5f;
    public LayerMask sheepLayer;

    public bool destroyTrapAfterUse = false;

    void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius, sheepLayer);

        foreach (Collider hit in hits)
        {
            // wichtig: Root holen!
            SheepController sheep = hit.GetComponentInParent<SheepController>();

            if (sheep != null)
            {
                Destroy(sheep.gameObject);
            }
        }

        if (hits.Length > 0 && destroyTrapAfterUse)
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