using UnityEngine;
using System.Collections.Generic;

public enum SheepState
{
    Wandering,
    Fleeing,
    Regrouping
}

public class SheepController : MonoBehaviour
{
    [Header("State")]
    public SheepState currentState = SheepState.Wandering;
    private SheepState previousState;

    [Header("Gaze")]
    public Transform gazeTarget;
    public float fleeStartRadius = 8f;
    public float fleeStopRadius = 12f;
    public float fleeSpeed = 4f;

    [Header("Regroup")]
    public Transform regroupTarget;
    public float regroupSpeed = 2f;
    public float regroupReachedDistance = 1f;

    [Header("Rotation")]
    public float rotationSpeed = 8f;

    [Header("Timing")]
    public float minStateTime = 0.3f;
    private float stateTimer = 0f;

    [Header("Obstacle")]
    public float obstacleCheckDistance = 1.5f;

    [Header("Group Behaviour")]
    public float neighborRadius = 4f;
    public float separationRadius = 1.5f;

    [Range(0f, 1f)]
    public float alignmentInfluence = 0.35f;

    [Range(0f, 2f)]
    public float separationInfluence = 0.8f;

    private Vector3 fleeFromPosition;
    private float slowMultiplier = 1f;

    private void OnEnable()
    {
        if (SheepGroupManager.Instance != null)
        {
            SheepGroupManager.Instance.RegisterSheep(this);
        }
    }

    private void Start()
    {
        previousState = currentState;

        if (SheepGroupManager.Instance != null)
        {
            SheepGroupManager.Instance.RegisterSheep(this);
        }

        Debug.Log("Start State: " + currentState);
    }

    private void OnDisable()
    {
        if (SheepGroupManager.Instance != null)
        {
            SheepGroupManager.Instance.UnregisterSheep(this);
        }
    }

    void Update()
    {
        stateTimer += Time.deltaTime;

        CheckStateTransitions();
        HandleState();

        if (currentState != previousState)
        {
            Debug.Log("Neuer State: " + currentState);
            previousState = currentState;
        }
    }

    void ChangeState(SheepState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        stateTimer = 0f;

        if (newState == SheepState.Fleeing && gazeTarget != null)
        {
            fleeFromPosition = gazeTarget.position;
            fleeFromPosition.y = transform.position.y;
        }
    }

    void HandleState()
    {
        switch (currentState)
        {
            case SheepState.Wandering:
                break;

            case SheepState.Fleeing:
                HandleFleeMovement();
                break;

            case SheepState.Regrouping:
                HandleRegroupMovement();
                break;
        }
    }

    void CheckStateTransitions()
    {
        if (gazeTarget == null) return;
        if (stateTimer < minStateTime) return;

        Vector3 sheepPos = transform.position;
        Vector3 gazePos = gazeTarget.position;
        gazePos.y = sheepPos.y;

        float distanceToGaze = Vector3.Distance(sheepPos, gazePos);

        switch (currentState)
        {
            case SheepState.Wandering:
                if (distanceToGaze < fleeStartRadius)
                {
                    ChangeState(SheepState.Fleeing);
                }
                break;

            case SheepState.Fleeing:
                if (distanceToGaze > fleeStopRadius)
                {
                    ChangeState(SheepState.Regrouping);
                }
                break;

            case SheepState.Regrouping:
                if (distanceToGaze < fleeStartRadius)
                {
                    ChangeState(SheepState.Fleeing);
                    break;
                }

                if (regroupTarget != null)
                {
                    Vector3 regroupPos = regroupTarget.position;
                    regroupPos.y = sheepPos.y;

                    float distanceToGroup = Vector3.Distance(sheepPos, regroupPos);

                    if (distanceToGroup < regroupReachedDistance)
                    {
                        ChangeState(SheepState.Wandering);
                    }
                }
                break;
        }
    }

    void HandleFleeMovement()
    {
        Vector3 sheepPos = transform.position;
        Vector3 targetPos = fleeFromPosition;
        targetPos.y = sheepPos.y;

        Vector3 ownDirection = sheepPos - targetPos;
        ownDirection.y = 0f;

        if (ownDirection.sqrMagnitude <= 0.001f) return;

        ownDirection.Normalize();

        Vector3 finalDirection = ApplyGroupBehaviour(ownDirection);

        if (finalDirection.sqrMagnitude <= 0.001f) return;

        finalDirection.Normalize();

        if (IsBlocked(finalDirection)) return;

        transform.position += finalDirection * fleeSpeed * slowMultiplier * Time.deltaTime;
        RotateTowards(finalDirection);
    }

    void HandleRegroupMovement()
    {
        if (regroupTarget == null) return;

        Vector3 sheepPos = transform.position;
        Vector3 targetPos = regroupTarget.position;
        targetPos.y = sheepPos.y;

        Vector3 direction = targetPos - sheepPos;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f) return;

        direction.Normalize();

        Vector3 finalDirection = ApplyGroupBehaviour(direction);

        if (finalDirection.sqrMagnitude <= 0.001f) return;

        finalDirection.Normalize();

        if (IsBlocked(finalDirection)) return;

        transform.position += finalDirection * regroupSpeed * slowMultiplier * Time.deltaTime;
        RotateTowards(finalDirection);
    }

    Vector3 ApplyGroupBehaviour(Vector3 baseDirection)
    {
        if (SheepGroupManager.Instance == null) return baseDirection;

        List<SheepController> neighbors = SheepGroupManager.Instance.GetNearbySheep(this, neighborRadius);

        if (neighbors.Count == 0) return baseDirection;

        Vector3 alignment = Vector3.zero;
        Vector3 separation = Vector3.zero;

        Vector3 myPos = transform.position;

        for (int i = 0; i < neighbors.Count; i++)
        {
            SheepController other = neighbors[i];
            Vector3 toOther = other.transform.position - myPos;
            toOther.y = 0f;

            float distance = toOther.magnitude;
            if (distance <= 0.001f) continue;

            if (other.currentState == SheepState.Fleeing || other.currentState == SheepState.Regrouping)
            {
                alignment += other.GetCurrentForwardOnPlane();
            }

            if (distance < separationRadius)
            {
                Vector3 pushAway = (myPos - other.transform.position);
                pushAway.y = 0f;

                if (pushAway.sqrMagnitude > 0.001f)
                {
                    separation += pushAway.normalized * ((separationRadius - distance) / separationRadius);
                }
            }
        }

        Vector3 finalDirection = baseDirection;

        if (alignment.sqrMagnitude > 0.001f)
        {
            alignment.Normalize();
            finalDirection += alignment * alignmentInfluence;
        }

        if (separation.sqrMagnitude > 0.001f)
        {
            separation.Normalize();
            finalDirection += separation * separationInfluence;
        }

        finalDirection.y = 0f;
        return finalDirection.normalized;
    }

    bool IsBlocked(Vector3 direction)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;
        float sphereRadius = 0.25f;

        if (Physics.SphereCast(origin, sphereRadius, direction, out hit, obstacleCheckDistance))
        {
            if (hit.collider.GetComponent<BlockingObstacle>() != null ||
                hit.collider.GetComponentInParent<BlockingObstacle>() != null)
            {
                return true;
            }
        }

        return false;
    }

    void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void SetSlowMultiplier(float multiplier)
    {
        slowMultiplier = multiplier;
    }

    public Vector3 GetCurrentForwardOnPlane()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return forward.normalized;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeStartRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fleeStopRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}