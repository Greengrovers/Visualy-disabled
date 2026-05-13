using UnityEngine;
using System.Collections.Generic;

public class BlockingObstacle : MonoBehaviour
{
    [Header("Gaze")]
    public Transform gazeTarget;
    public float gazeAwayDistance = 8f;

    [Header("Sheep Detection")]
    public LayerMask sheepLayer;
    public float scanRadius = 6f;
    public float nearObstacleDistance = 2f;

    [Header("Stuck Check")]
    public float sampleInterval = 0.4f;
    public float minMoveDistance = 0.12f;
    public float stuckTimeNeeded = 1.0f;

    [Header("Lure")]
    public Transform lurePoint;
    public float lureMoveDistance = 3f;

    private Collider[] obstacleColliders;

    private class SheepStuckData
    {
        public Vector3 lastPosition;
        public float sampleTimer;
        public float stuckTimer;
    }

    private Dictionary<SheepController, SheepStuckData> sheepData =
        new Dictionary<SheepController, SheepStuckData>();

    private void Awake()
    {
        obstacleColliders = GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (gazeTarget == null || lurePoint == null)
        {
            return;
        }

        bool gazeIsAway =
            Vector3.Distance(gazeTarget.position, transform.position) > gazeAwayDistance;

        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, sheepLayer);
        HashSet<SheepController> sheepSeenThisFrame = new HashSet<SheepController>();

        foreach (Collider hit in hits)
        {
            SheepController sheep = hit.GetComponentInParent<SheepController>();

            if (sheep == null || sheep.isInGoal)
            {
                continue;
            }

            sheepSeenThisFrame.Add(sheep);

            if (!gazeIsAway)
            {
                ResetSheep(sheep);
                continue;
            }

            if (sheep.currentState == SheepState.Lured)
            {
                ResetSheep(sheep);
                continue;
            }

            if (!IsSheepNearObstacle(sheep))
            {
                ResetSheep(sheep);
                continue;
            }

            CheckStuckAndLure(sheep);
        }

        CleanupOldSheep(sheepSeenThisFrame);
    }

    private void CheckStuckAndLure(SheepController sheep)
    {
        if (!sheepData.ContainsKey(sheep))
        {
            sheepData[sheep] = new SheepStuckData
            {
                lastPosition = sheep.transform.position,
                sampleTimer = 0f,
                stuckTimer = 0f
            };

            return;
        }

        SheepStuckData data = sheepData[sheep];

        data.sampleTimer += Time.deltaTime;

        if (data.sampleTimer < sampleInterval)
        {
            return;
        }

        float movedDistance =
            Vector3.Distance(sheep.transform.position, data.lastPosition);

        if (movedDistance < minMoveDistance)
        {
            data.stuckTimer += data.sampleTimer;
        }
        else
        {
            data.stuckTimer = 0f;
        }

        data.lastPosition = sheep.transform.position;
        data.sampleTimer = 0f;

        if (data.stuckTimer >= stuckTimeNeeded)
        {
            sheep.LureTowardsPointForDistance(lurePoint.position, lureMoveDistance);
            ResetSheep(sheep);
        }
    }

    private bool IsSheepNearObstacle(SheepController sheep)
    {
        if (obstacleColliders == null || obstacleColliders.Length == 0)
        {
            return Vector3.Distance(sheep.transform.position, transform.position)
                   <= nearObstacleDistance;
        }

        Vector3 sheepPos = sheep.transform.position;

        foreach (Collider col in obstacleColliders)
        {
            if (col == null) continue;

            Vector3 closestPoint = col.ClosestPoint(sheepPos);
            float distance = Vector3.Distance(sheepPos, closestPoint);

            if (distance <= nearObstacleDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void ResetSheep(SheepController sheep)
    {
        if (sheepData.ContainsKey(sheep))
        {
            sheepData.Remove(sheep);
        }
    }

    private void CleanupOldSheep(HashSet<SheepController> sheepSeenThisFrame)
    {
        List<SheepController> toRemove = new List<SheepController>();

        foreach (SheepController sheep in sheepData.Keys)
        {
            if (sheep == null || !sheepSeenThisFrame.Contains(sheep))
            {
                toRemove.Add(sheep);
            }
        }

        foreach (SheepController sheep in toRemove)
        {
            sheepData.Remove(sheep);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, nearObstacleDistance);

        if (lurePoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lurePoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, lurePoint.position);
        }
    }
}