using Unity.VisualScripting;
using UnityEngine;

public class Mines : MonoBehaviour
{
    public float triggerRadius = 1.5f;
    public LayerMask sheepLayer;

    public bool destroyTrapAfterUse = true;

    public float destructionRadius = 4f;

   void Update()
{
    Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius, sheepLayer);

    if (hits.Length > 0) // sobald irgendwer triggert
    {
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