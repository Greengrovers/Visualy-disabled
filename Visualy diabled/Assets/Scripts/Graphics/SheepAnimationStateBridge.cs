using UnityEngine;

// Bridges SheepController state changes to texture clip playback.
public class SheepAnimationStateBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SheepController sheepController;
    [SerializeField] private sheep_animation_etc textureAnimator;

    [Header("Clip IDs")]
    [SerializeField] private string idleClipId = "idle";
    [SerializeField] private string runClipId = "run";

    private SheepState lastState;
    private bool hasInitialized;

    private void Awake()
    {
        if (sheepController == null)
        {
            sheepController = GetComponentInParent<SheepController>();
        }

        if (textureAnimator == null)
        {
            textureAnimator = GetComponentInChildren<sheep_animation_etc>();
        }
    }

    private void OnEnable()
    {
        ForceRefresh();
    }

    private void Update()
    {
        if (sheepController == null || textureAnimator == null)
        {
            return;
        }

        if (!hasInitialized || sheepController.currentState != lastState)
        {
            ApplyAnimationForState(sheepController.currentState);
            lastState = sheepController.currentState;
            hasInitialized = true;
        }
    }

    public void ForceRefresh()
    {
        if (sheepController == null || textureAnimator == null)
        {
            return;
        }

        ApplyAnimationForState(sheepController.currentState);
        lastState = sheepController.currentState;
        hasInitialized = true;
    }

    private void ApplyAnimationForState(SheepState state)
    {
        if (state == SheepState.Fleeing)
        {
            textureAnimator.Play(runClipId);
        }
        else
        {
            textureAnimator.Play(idleClipId);
        }
    }
}
