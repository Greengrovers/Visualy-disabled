using UnityEngine;

public class Mines : MonoBehaviour
{
    public float triggerRadius = 1.5f;
    public LayerMask sheepLayer;
    public bool destroyTrapAfterUse = true;
    public float destructionRadius = 4f;

    private bool hasTriggered = false; // Mehrfach-Trigger verhindern

    void Update()
    {
        if (hasTriggered) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius, sheepLayer);

        if (hits.Length > 0)
        {
            hasTriggered = true;

            // 1. Erst Explosion visuell triggern
            mine_boom boom = GetComponent<mine_boom>();
            if (boom != null)
            {
                boom.TriggerExplosion(); // kümmert sich selbst um Destroy(gameObject)
            }

            // 2. Dann Schafe zerstören
            Collider[] explosion = Physics.OverlapSphere(transform.position, destructionRadius, sheepLayer);
            foreach (Collider hit in explosion)
            {
                SheepController sheep = hit.GetComponentInParent<SheepController>();
                if (sheep != null)
                {
                    Destroy(sheep.gameObject);
                }
            }

            // 3. Nur zerstören wenn kein mine_boom vorhanden
            if (boom == null && destroyTrapAfterUse)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}