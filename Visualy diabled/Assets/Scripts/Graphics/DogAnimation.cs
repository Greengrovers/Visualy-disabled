using UnityEngine;
using System;
using System.Collections.Generic;

public class DogAnimation : MonoBehaviour
{
    [Serializable]
    public class FrameAnimationClip
    {
        public string id = "idle";
        public Texture2D[] frames;
        [Min(1f)] public float fps = 8f;
        public bool loop = true;
    }

    [Header("Renderer")]
    [Tooltip("If empty, uses Renderer on this GameObject.")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Clips")]
    [SerializeField] private FrameAnimationClip[] clips;
    [SerializeField] private string initialClip = "idle";
    [SerializeField] private bool randomizeInitialFrame = true;

    [Header("Playback")]
    [SerializeField] private float speedMultiplier = 1f;
    [Tooltip("If true, animation ignores Time.timeScale and always runs in real time.")]
    [SerializeField] private bool ignoreTimeScale = false;

    [Header("State From Gaze Movement")]
    [Tooltip("Automatically switches between run/idle based on gaze movement speed.")]
    [SerializeField] private bool useGazeMovementState = true;
    [Tooltip("Optional: assign the 'Dog Trigger for Gaze' transform with GazeWorldTarget.")]
    [SerializeField] private Transform gazeMovementSource;
    [SerializeField] private string idleClipId = "idle";
    [SerializeField] private string runClipId = "run";
    [Min(0f)] [SerializeField] private float idleAfterSeconds = 2f;
    [Min(0f)] [SerializeField] private float worldMovementThreshold = 0.01f;
    [Min(0f)] [SerializeField] private float gazeMovementThreshold = 0.004f;

    private readonly Dictionary<string, int> clipLookup = new Dictionary<string, int>();
    private MaterialPropertyBlock propertyBlock;
    private FrameAnimationClip currentClip;
    private int currentFrame;
    private float frameTimer;
    private bool hasValidClip;
    private Vector3 lastMovementSourcePosition;
    private bool hasLastMovementSourcePosition;
    private Vector2 lastGazeViewport;
    private bool hasLastGazeViewport;
    private float noGazeMovementTimer;
    private bool isIdleFromGaze;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (gazeMovementSource == null)
        {
            GazeWorldTarget worldTarget = FindFirstObjectByType<GazeWorldTarget>();
            if (worldTarget != null)
            {
                gazeMovementSource = worldTarget.transform;
            }
        }

        propertyBlock = new MaterialPropertyBlock();
        BuildLookup();
    }

    private void OnEnable()
    {
        hasLastMovementSourcePosition = false;
        hasLastGazeViewport = false;
        noGazeMovementTimer = 0f;
        isIdleFromGaze = false;

        Play(initialClip, true);
    }

    private void Update()
    {
        UpdateStateFromGazeMovement();

        if (!hasValidClip || currentClip == null || currentClip.frames == null || currentClip.frames.Length == 0)
        {
            return;
        }

        float fps = Mathf.Max(1f, currentClip.fps * Mathf.Max(0f, speedMultiplier));
        float frameDuration = 1f / fps;
        frameTimer += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

        if (frameTimer < frameDuration)
        {
            return;
        }

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            AdvanceFrame();
        }
    }

    public bool Play(string clipId, bool restartIfSame = false)
    {
        if (string.IsNullOrWhiteSpace(clipId) || !clipLookup.TryGetValue(clipId, out int clipIndex))
        {
            hasValidClip = false;
            return false;
        }

        FrameAnimationClip nextClip = clips[clipIndex];
        if (nextClip == null || nextClip.frames == null || nextClip.frames.Length == 0)
        {
            hasValidClip = false;
            return false;
        }

        if (!restartIfSame && currentClip == nextClip && hasValidClip)
        {
            return true;
        }

        currentClip = nextClip;
        hasValidClip = true;
        frameTimer = 0f;

        if (randomizeInitialFrame)
        {
            currentFrame = UnityEngine.Random.Range(0, currentClip.frames.Length);
        }
        else
        {
            currentFrame = 0;
        }

        ApplyFrame(currentFrame);
        return true;
    }

    public void SetSpeedMultiplier(float value)
    {
        speedMultiplier = Mathf.Max(0f, value);
    }

    private void AdvanceFrame()
    {
        int next = currentFrame + 1;
        int frameCount = currentClip.frames.Length;

        if (next >= frameCount)
        {
            if (!currentClip.loop)
            {
                next = frameCount - 1;
            }
            else
            {
                next = 0;
            }
        }

        if (next == currentFrame)
        {
            return;
        }

        currentFrame = next;
        ApplyFrame(currentFrame);
    }

    private void ApplyFrame(int frameIndex)
    {
        if (targetRenderer == null || currentClip == null)
        {
            return;
        }

        Texture2D frame = currentClip.frames[frameIndex];
        if (frame == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(MainTexId, frame);
        propertyBlock.SetTexture(BaseMapId, frame);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void BuildLookup()
    {
        clipLookup.Clear();

        if (clips == null)
        {
            return;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            FrameAnimationClip clip = clips[i];
            if (clip == null || string.IsNullOrWhiteSpace(clip.id))
            {
                continue;
            }

            if (!clipLookup.ContainsKey(clip.id))
            {
                clipLookup.Add(clip.id, i);
            }
        }
    }

    private void UpdateStateFromGazeMovement()
    {
        if (!useGazeMovementState)
        {
            return;
        }

        float dt = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

        if (gazeMovementSource == null)
        {
            GazeWorldTarget worldTarget = FindFirstObjectByType<GazeWorldTarget>();
            if (worldTarget != null)
            {
                gazeMovementSource = worldTarget.transform;
            }
        }

        if (gazeMovementSource != null)
        {
            Vector3 currentPosition = gazeMovementSource.position;
            if (!hasLastMovementSourcePosition)
            {
                hasLastMovementSourcePosition = true;
                lastMovementSourcePosition = currentPosition;
                noGazeMovementTimer = 0f;
                TrySwitchToRun();
                return;
            }

            float movementSqrWorld = (currentPosition - lastMovementSourcePosition).sqrMagnitude;
            float thresholdSqrWorld = worldMovementThreshold * worldMovementThreshold;

            if (movementSqrWorld >= thresholdSqrWorld)
            {
                noGazeMovementTimer = 0f;
                TrySwitchToRun();
            }
            else
            {
                noGazeMovementTimer += dt;
                TrySwitchToIdle();
            }

            lastMovementSourcePosition = currentPosition;
            return;
        }

        Vector2 currentViewport;
        bool hasValidGaze = TobiiManager.Instance != null && TobiiManager.Instance.HasValidGazeData;

        if (!hasValidGaze)
        {
            noGazeMovementTimer += dt;
            TrySwitchToIdle();
            return;
        }

        currentViewport = TobiiManager.Instance.GazePointViewport;
        if (!hasLastGazeViewport)
        {
            hasLastGazeViewport = true;
            lastGazeViewport = currentViewport;
            noGazeMovementTimer = 0f;
            TrySwitchToRun();
            return;
        }

        float movementSqr = (currentViewport - lastGazeViewport).sqrMagnitude;
        float thresholdSqr = gazeMovementThreshold * gazeMovementThreshold;

        if (movementSqr >= thresholdSqr)
        {
            noGazeMovementTimer = 0f;
            TrySwitchToRun();
        }
        else
        {
            noGazeMovementTimer += dt;
            TrySwitchToIdle();
        }

        lastGazeViewport = currentViewport;
    }

    private void TrySwitchToIdle()
    {
        if (isIdleFromGaze || noGazeMovementTimer < idleAfterSeconds)
        {
            return;
        }

        if (Play(idleClipId, false))
        {
            isIdleFromGaze = true;
        }
    }

    private void TrySwitchToRun()
    {
        if (!isIdleFromGaze)
        {
            return;
        }

        if (Play(runClipId, false))
        {
            isIdleFromGaze = false;
        }
    }

    private void OnValidate()
    {
        if (clips == null)
        {
            return;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            FrameAnimationClip clip = clips[i];
            if (clip == null || string.IsNullOrWhiteSpace(clip.id))
            {
                continue;
            }

            if (clip.frames != null && clip.frames.Length == 0)
            {
                Debug.LogWarning($"Clip '{clip.id}' has no frames assigned.", this);
            }
        }
    }
}
