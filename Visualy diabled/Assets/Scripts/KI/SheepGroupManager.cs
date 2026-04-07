using UnityEngine;
using System.Collections.Generic;

public class SheepGroupManager : MonoBehaviour
{
    public static SheepGroupManager Instance { get; private set; }

    private readonly List<SheepController> allSheep = new List<SheepController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterSheep(SheepController sheep)
    {
        if (sheep == null) return;
        if (!allSheep.Contains(sheep))
        {
            allSheep.Add(sheep);
        }
    }

    public void UnregisterSheep(SheepController sheep)
    {
        if (sheep == null) return;
        allSheep.Remove(sheep);
    }

    public List<SheepController> GetNearbySheep(SheepController currentSheep, float radius)
    {
        List<SheepController> result = new List<SheepController>();

        if (currentSheep == null) return result;

        Vector3 currentPos = currentSheep.transform.position;
        float radiusSqr = radius * radius;

        for (int i = 0; i < allSheep.Count; i++)
        {
            SheepController other = allSheep[i];

            if (other == null) continue;
            if (other == currentSheep) continue;

            Vector3 diff = other.transform.position - currentPos;
            diff.y = 0f;

            if (diff.sqrMagnitude <= radiusSqr)
            {
                result.Add(other);
            }
        }

        return result;
    }
}