using UnityEngine;
using System.Collections.Generic;

public class SheepGroupManager : MonoBehaviour
{
    public static SheepGroupManager Instance { get; private set; }

    public List<SheepController> allSheep = new List<SheepController>();

    [Header("Group Regroup")]
    public bool hasGroupRegroupTarget = false;
    public Vector3 groupRegroupPosition;
    public float regroupDistance = 6f;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterSheep(SheepController sheep)
    {
        if (!allSheep.Contains(sheep))
            allSheep.Add(sheep);
    }

    public void UnregisterSheep(SheepController sheep)
    {
        allSheep.Remove(sheep);
    }

    public void CreateGroupRegroupTarget(Vector3 sheepPosition, Vector3 fleeDirection)
    {
        if (hasGroupRegroupTarget) return;

        fleeDirection.y = 0f;

        if (fleeDirection.sqrMagnitude < 0.001f) return;

        groupRegroupPosition = sheepPosition + fleeDirection.normalized * regroupDistance;
        hasGroupRegroupTarget = true;
    }

    public void ClearGroupRegroupTarget()
    {
        hasGroupRegroupTarget = false;
    }

    public List<SheepController> GetNearbySheep(SheepController currentSheep, float radius)
    {
        List<SheepController> result = new List<SheepController>();
        float radiusSqr = radius * radius;

        foreach (SheepController other in allSheep)
        {
            if (other == null || other == currentSheep) continue;

            Vector3 diff = other.transform.position - currentSheep.transform.position;
            diff.y = 0f;

            if (diff.sqrMagnitude <= radiusSqr)
                result.Add(other);
        }

        return result;
    }
}