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
    public float fleeStartRadius = 12f;
    public float fleeStopRadius = 18f;
    public float fleeSpeed = 4f;

    [Header("Regroup")]
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
    public float alignmentInfluence = 0.2f;

    [Range(0f, 2f)]
    public float separationInfluence = 0.9f;

    [Header("Animation")]
    public sheep_animation_etc sheepAnimation;
    public string idleAnimationId = "idle";
    public string runAnimationId = "run";

    private Vector3 fleeFromPosition;
    private Vector3 lastFleeDirection;
    private float slowMultiplier = 1f;

    private void OnEnable()
    {
        if (SheepGroupManager.Instance != null)
            SheepGroupManager.Instance.RegisterSheep(this);
    }

    private void Start()
    {
        previousState = currentState;

        if (SheepGroupManager.Instance != null)
            SheepGroupManager.Instance.RegisterSheep(this);

        if (sheepAnimation == null)
            sheepAnimation = GetComponentInChildren<sheep_animation_etc>();

        PlayAnimationForState();

        Debug.Log("Start State: " + currentState);
    }

    private void OnDisable()
    {
        if (SheepGroupManager.Instance != null)
            SheepGroupManager.Instance.UnregisterSheep(this);
    }

    void Update()
    {
        stateTimer += Time.deltaTime;

        CheckStateTransitions();
        HandleState();

        if (currentState != previousState)
        {
            Debug.Log("Neuer State: " + currentState);
            PlayAnimationForState();
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

            lastFleeDirection = transform.position - fleeFromPosition;
            lastFleeDirection.y = 0f;

            if (lastFleeDirection.sqrMagnitude > 0.001f)
                lastFleeDirection.Normalize();
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
                    if (SheepGroupManager.Instance != null)
                    {
                        SheepGroupManager.Instance.CreateGroupRegroupTarget(
                            transform.position,
                            lastFleeDirection
                        );
                    }

                    ChangeState(SheepState.Regrouping);
                }
                break;

            case SheepState.Regrouping:
                if (distanceToGaze < fleeStartRadius)
                {
                    ChangeState(SheepState.Fleeing);
                    break;
                }

                if (SheepGroupManager.Instance != null &&
                    SheepGroupManager.Instance.hasGroupRegroupTarget)
                {
                    Vector3 targetPos = SheepGroupManager.Instance.groupRegroupPosition;
                    targetPos.y = sheepPos.y;

                    float distanceToGroup = Vector3.Distance(sheepPos, targetPos);

                    if (distanceToGroup < regroupReachedDistance)
                    {
                        ChangeState(SheepState.Wandering);

                        SheepGroupManager.Instance.TryClearRegroupTargetIfAllFinished();
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

        Vector3 finalDirection = ApplyGroupBehaviour(ownDirection, true);

        if (finalDirection.sqrMagnitude <= 0.001f) return;

        finalDirection.Normalize();

        if (IsBlocked(finalDirection)) return;

        transform.position += finalDirection * fleeSpeed * slowMultiplier * Time.deltaTime;
        RotateTowards(finalDirection);
    }

    void HandleRegroupMovement()
    {
        if (SheepGroupManager.Instance == null) return;
        if (!SheepGroupManager.Instance.hasGroupRegroupTarget) return;

        Vector3 sheepPos = transform.position;
        Vector3 targetPos = SheepGroupManager.Instance.groupRegroupPosition;
        targetPos.y = sheepPos.y;

        Vector3 direction = targetPos - sheepPos;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f) return;

        direction.Normalize();

        Vector3 finalDirection = ApplyGroupBehaviour(direction, false);

        if (finalDirection.sqrMagnitude <= 0.001f) return;

        finalDirection.Normalize();

        if (IsBlocked(finalDirection)) return;

        transform.position += finalDirection * regroupSpeed * slowMultiplier * Time.deltaTime;
        RotateTowards(finalDirection);
    }

    Vector3 ApplyGroupBehaviour(Vector3 baseDirection, bool useAlignment)
    {
        if (SheepGroupManager.Instance == null) return baseDirection;

        List<SheepController> neighbors =
            SheepGroupManager.Instance.GetNearbySheep(this, neighborRadius);

        if (neighbors.Count == 0) return baseDirection;

        Vector3 alignment = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 myPos = transform.position;

        foreach (SheepController other in neighbors)
        {
            if (other == null) continue;

            Vector3 toOther = other.transform.position - myPos;
            toOther.y = 0f;

            float distance = toOther.magnitude;
            if (distance <= 0.001f) continue;

            if (useAlignment && other.currentState == SheepState.Fleeing)
            {
                alignment += other.GetCurrentForwardOnPlane();
            }

            if (distance < separationRadius)
            {
                Vector3 pushAway = myPos - other.transform.position;
                pushAway.y = 0f;

                if (pushAway.sqrMagnitude > 0.001f)
                {
                    separation += pushAway.normalized *
                                  ((separationRadius - distance) / separationRadius);
                }
            }
        }

        Vector3 finalDirection = baseDirection;

        if (useAlignment && alignment.sqrMagnitude > 0.001f)
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

        if (Physics.SphereCast(origin, 0.25f, direction, out hit, obstacleCheckDistance))
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

    void PlayAnimationForState()
    {
        if (sheepAnimation == null) return;

        switch (currentState)
        {
            case SheepState.Wandering:
                sheepAnimation.Play(idleAnimationId);
                break;

            case SheepState.Fleeing:
            case SheepState.Regrouping:
                sheepAnimation.Play(runAnimationId);
                break;
        }
    }

    public void SetSlowMultiplier(float multiplier)
    {
        slowMultiplier = multiplier;

        if (sheepAnimation != null)
        {
            sheepAnimation.SetSpeedMultiplier(multiplier);
        }
    }

    public Vector3 GetCurrentForwardOnPlane()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.zero;
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