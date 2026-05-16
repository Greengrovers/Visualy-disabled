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
        // Singleton-Guard: verhindert dass eine zweite Instanz die erste still ueberschreibt.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SheepGroupManager: Duplicate instance detected, destroying self.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        RegisterExistingSheepInScene();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

    public void TryClearRegroupTargetIfAllFinished()
    {
        if (!hasGroupRegroupTarget) return;

        foreach (SheepController sheep in allSheep)
        {
            if (sheep == null) continue;

            if (sheep.currentState == SheepState.Regrouping)
                return;
        }

        ClearGroupRegroupTarget();
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

    private void RegisterExistingSheepInScene()
    {
        SheepController[] sceneSheep = FindObjectsByType<SheepController>(FindObjectsSortMode.None);

        foreach (SheepController sheep in sceneSheep)
        {
            if (sheep == null)
                continue;

            RegisterSheep(sheep);
        }
    }
}