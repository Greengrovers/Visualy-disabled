using UnityEngine;

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
    public float obstacleCheckDistance = 0.8f;

    private Vector3 fleeFromPosition;

    void Start()
    {
        previousState = currentState;
        Debug.Log("Start State: " + currentState);
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

        Vector3 direction = sheepPos - targetPos;

        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();

            if (IsBlocked(direction)) return;

            transform.position += direction * fleeSpeed * Time.deltaTime;
            RotateTowards(direction);
        }
    }

    void HandleRegroupMovement()
    {
        if (regroupTarget == null) return;

        Vector3 sheepPos = transform.position;
        Vector3 targetPos = regroupTarget.position;
        targetPos.y = sheepPos.y;

        Vector3 direction = targetPos - sheepPos;

        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();

            if (IsBlocked(direction)) return;

            transform.position += direction * regroupSpeed * Time.deltaTime;
            RotateTowards(direction);
        }
    }

    bool IsBlocked(Vector3 direction)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;

        if (Physics.Raycast(origin, direction, out hit, obstacleCheckDistance))
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeStartRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fleeStopRadius);

        Gizmos.color = Color.blue;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawLine(origin, origin + transform.forward * obstacleCheckDistance);
    }
}