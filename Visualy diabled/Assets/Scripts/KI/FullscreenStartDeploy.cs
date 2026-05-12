using UnityEngine;

public class FullscreenStartDeploy : MonoBehaviour
{
    void Start()
    {
        Screen.SetResolution(
            Display.main.systemWidth,
            Display.main.systemHeight,
            FullScreenMode.ExclusiveFullScreen
        );
    }
}