using UnityEngine;
using System.Collections.Generic;

public class GoalArea : MonoBehaviour
{
    public float triggerRadius = 2f;
    public LayerMask sheepLayer;

    private HashSet<GameObject> alreadyScored = new HashSet<GameObject>();

    private void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius, sheepLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            GameObject sheepRoot = hits[i].transform.root.gameObject;

            if (alreadyScored.Contains(sheepRoot)) continue;

            alreadyScored.Add(sheepRoot);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddSheepScore();
            }

            SheepController sheep = sheepRoot.GetComponent<SheepController>();
            if (sheep != null)
            {
                sheep.isInGoal = true;
                sheep.enabled = false;
            }

            var agent = sheepRoot.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = true;
            }

            Rigidbody rb = sheepRoot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}