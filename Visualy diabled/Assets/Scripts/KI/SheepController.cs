using UnityEngine;
using System.Collections.Generic;

public enum SheepState
{
    Wandering,
    Fleeing,
    Regrouping,
    Lured
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

    [Header("Lured")]
    public float luredSpeed = 2.5f;
    public float luredReachedDistance = 0.2f;

    private Vector3 luredTarget;
    private Vector3 luredStartPosition;
    private float luredMoveDistance;
    private bool hasLuredTarget = false;

    [Header("Rotation")]
    public float rotationSpeed = 8f;

    [Header("Timing")]
    public float minStateTime = 0.3f;
    private float stateTimer = 0f;

    [Header("Obstacle")]
    public float obstacleCheckDistance = 1.5f;
    public float obstacleSphereRadius = 0.25f;
    public bool isInGoal = false;

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

        if (sheepAnimation == null)
            sheepAnimation = GetComponentInChildren<sheep_animation_etc>();

        PlayAnimationForState();
    }

    private void OnDisable()
    {
        if (SheepGroupManager.Instance != null)
            SheepGroupManager.Instance.UnregisterSheep(this);
    }

    void Update()
    {
        if (isInGoal) return;

        stateTimer += Time.deltaTime;

        CheckStateTransitions();
        HandleState();

        if (currentState != previousState)
        {
            PlayAnimationForState();
            previousState = currentState;
        }
    }

    public void EnterGoal()
    {
        if (isInGoal) return;

        isInGoal = true;
        hasLuredTarget = false;

        if (sheepAnimation != null)
        {
            sheepAnimation.Play(idleAnimationId);
            sheepAnimation.SetSpeedMultiplier(0f);
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
    }

    public void LureTowardsPointForDistance(Vector3 point, float moveDistance)
    {
        if (isInGoal) return;
        if (currentState == SheepState.Lured) return;

        Vector3 direction = point - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f) return;

        direction.Normalize();

        luredStartPosition = transform.position;
        luredTarget = transform.position + direction * moveDistance;
        luredTarget.y = transform.position.y;

        luredMoveDistance = moveDistance;
        hasLuredTarget = true;

        ChangeState(SheepState.Lured);
    }

    void ChangeState(SheepState newState)
    {
        if (isInGoal) return;
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

            hasLuredTarget = false;
        }

        if (newState == SheepState.Wandering)
        {
            hasLuredTarget = false;
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

            case SheepState.Lured:
                HandleLuredMovement();
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

        if (currentState == SheepState.Lured)
        {
            if (distanceToGaze < fleeStartRadius)
            {
                ChangeState(SheepState.Fleeing);
            }

            return;
        }

        switch (currentState)
        {
            case SheepState.Wandering:
                if (distanceToGaze < fleeStartRadius)
                    ChangeState(SheepState.Fleeing);
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
        finalDirection = FindSteeringDirection(finalDirection);

        if (finalDirection.sqrMagnitude <= 0.001f) return;

        transform.position += finalDirection * fleeSpeed * slowMultiplier * Time.deltaTime;
        RotateTowards(finalDirection);

        lastFleeDirection = finalDirection;
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
        finalDirection = FindSteeringDirection(finalDirection);

        if (finalDirection.sqrMagnitude <= 0.001f)
        {
            ChangeState(SheepState.Wandering);
            return;
        }

        transform.position += finalDirection * regroupSpeed * slowMultiplier * Time.deltaTime;
        RotateTowards(finalDirection);
    }

    void HandleLuredMovement()
    {
        if (!hasLuredTarget)
        {
            ChangeState(SheepState.Wandering);
            return;
        }

        Vector3 fromStart = transform.position - luredStartPosition;
        fromStart.y = 0f;

        if (fromStart.magnitude >= luredMoveDistance)
        {
            ChangeState(SheepState.Wandering);
            return;
        }

        Vector3 direction = luredTarget - transform.position;
        direction.y = 0f;

        if (direction.magnitude < luredReachedDistance)
        {
            ChangeState(SheepState.Wandering);
            return;
        }

        direction.Normalize();

        Vector3 steeringDirection = FindSteeringDirection(direction);

        if (steeringDirection.sqrMagnitude <= 0.001f)
        {
            steeringDirection = direction;
        }

        transform.position += steeringDirection * luredSpeed * slowMultiplier * Time.deltaTime;
        RotateTowards(steeringDirection);
    }

    Vector3 FindSteeringDirection(Vector3 preferred)
    {
        preferred.y = 0f;

        if (preferred.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        preferred.Normalize();

        if (!IsBlocked(preferred))
            return preferred;

        float[] angles = { 35f, -35f, 70f, -70f, 110f, -110f, 150f, -150f };

        foreach (float angle in angles)
        {
            Vector3 candidate = Quaternion.Euler(0f, angle, 0f) * preferred;
            candidate.y = 0f;

            if (candidate.sqrMagnitude <= 0.001f) continue;

            candidate.Normalize();

            if (!IsBlocked(candidate))
                return candidate;
        }

        return Vector3.zero;
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
            if (other.isInGoal) continue;

            Vector3 toOther = other.transform.position - myPos;
            toOther.y = 0f;

            float distance = toOther.magnitude;
            if (distance <= 0.001f) continue;

            if (useAlignment && other.currentState == SheepState.Fleeing)
                alignment += other.GetCurrentForwardOnPlane();

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
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return true;

        direction.Normalize();

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;

        if (Physics.SphereCast(origin, obstacleSphereRadius, direction, out hit, obstacleCheckDistance))
        {
            BlockingObstacle obstacle =
                hit.collider.GetComponent<BlockingObstacle>() ??
                hit.collider.GetComponentInParent<BlockingObstacle>();

            if (obstacle != null)
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
            case SheepState.Lured:
                sheepAnimation.Play(runAnimationId);
                break;
        }
    }

    public void SetSlowMultiplier(float multiplier)
    {
        slowMultiplier = multiplier;

        if (sheepAnimation != null)
            sheepAnimation.SetSpeedMultiplier(multiplier);
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