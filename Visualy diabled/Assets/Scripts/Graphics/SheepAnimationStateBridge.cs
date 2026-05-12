using UnityEngine;

// Bruecke zwischen SheepController-Zustand und Textur-Animation.
//
// HINWEIS: Dieses Skript ist nur noetig wenn SheepController.sheepAnimation NICHT
// gesetzt ist. Ist sheepAnimation im SheepController zugewiesen, uebernimmt dieser
// die Animationssteuerung bereits direkt — dann kann SheepAnimationStateBridge
// deaktiviert oder entfernt werden, um doppelte Zustandsverwaltung zu vermeiden.

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
            sheepController = GetComponentInParent<SheepController>();

        if (textureAnimator == null)
            textureAnimator = GetComponentInChildren<sheep_animation_etc>();
    }

    private void OnEnable()
    {
        ForceRefresh();
    }

    private void Update()
    {
        if (sheepController == null || textureAnimator == null)
            return;

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
            return;

        ApplyAnimationForState(sheepController.currentState);
        lastState = sheepController.currentState;
        hasInitialized = true;
    }

    private void ApplyAnimationForState(SheepState state)
    {
        switch (state)
        {
            case SheepState.Fleeing:
            case SheepState.Regrouping:
                textureAnimator.Play(runClipId);
                break;

            default:
                textureAnimator.Play(idleClipId);
                break;
        }
    }
}