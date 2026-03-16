using UnityEngine;

public class GazeWorldTarget : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask groundLayer;
    public float maxDistance = 100f;
    public float smoothSpeed = 8f;

    void Update()
    {
        if (TobiiManager.Instance == null) return;
        if (!TobiiManager.Instance.HasValidGazeData) return;

        Ray ray;
        if (!TobiiManager.Instance.GetGazeRay(out ray)) return;

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance, groundLayer))
        {
            Vector3 targetPosition = hit.point;
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                smoothSpeed * Time.deltaTime
            );
        }
    }
}