using UnityEngine;

public class FullscreenStart : MonoBehaviour
{
    void Start()
    {
        Screen.fullScreen = true;

        // Optional: gewünschte Auflösung setzen
        Screen.SetResolution(Display.main.systemWidth,
                             Display.main.systemHeight,
                             true);
    }
}
