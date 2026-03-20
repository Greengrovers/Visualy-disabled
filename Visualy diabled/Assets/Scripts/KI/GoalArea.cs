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

            Destroy(sheepRoot);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}