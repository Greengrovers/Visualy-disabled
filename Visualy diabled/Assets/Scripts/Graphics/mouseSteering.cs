using UnityEngine;

public class mouseSteering : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask groundLayer;
    public float maxDistance = 100f;
    public float smoothSpeed = 8f;

    private GazeWorldTarget gazeWorldTarget;
    private bool disabledGazeWorldTarget;

    private void OnEnable()
    {
        gazeWorldTarget = GetComponent<GazeWorldTarget>();

        if (gazeWorldTarget != null && gazeWorldTarget.enabled)
        {
            gazeWorldTarget.enabled = false;
            disabledGazeWorldTarget = true;
        }
    }

    private void OnDisable()
    {
        if (gazeWorldTarget != null && disabledGazeWorldTarget)
        {
            gazeWorldTarget.enabled = true;
        }

        disabledGazeWorldTarget = false;
    }

    private void Update()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance, groundLayer))
        {
            transform.position = Vector3.Lerp(
                transform.position,
                hit.point,
                smoothSpeed * Time.deltaTime
            );
            return;
        }

        Plane movementPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        float distanceToPlane;
        if (movementPlane.Raycast(ray, out distanceToPlane))
        {
            Vector3 targetPosition = ray.GetPoint(distanceToPlane);
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                smoothSpeed * Time.deltaTime
            );
        }
    }
}
