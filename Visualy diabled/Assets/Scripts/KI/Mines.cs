using UnityEngine;

public class Mines : MonoBehaviour
{
    public float triggerRadius = 1.5f;
    public LayerMask sheepLayer;
    public bool destroyTrapAfterUse = true;
    public float destructionRadius = 4f;
    public float destroyDelay = 2f;

    private bool hasTriggered = false;

    void Update()
    {
        if (hasTriggered) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius, sheepLayer);

        if (hits.Length > 0)
        {
            hasTriggered = true;

            mine_boom boom = GetComponentInChildren<mine_boom>();

            if (boom != null)
            {
                boom.PlayExplosion();
            }

            Collider[] explosion = Physics.OverlapSphere(transform.position, destructionRadius, sheepLayer);

            foreach (Collider hit in explosion)
            {
                SheepController sheep = hit.GetComponentInParent<SheepController>();

                if (sheep != null)
                {
                    Destroy(sheep.gameObject);
                }
            }

            if (destroyTrapAfterUse)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, destructionRadius);
    }
}