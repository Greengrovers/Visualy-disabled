using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    void LateUpdate()
{
    transform.rotation = Quaternion.Euler(0, 0, 0);
}
}