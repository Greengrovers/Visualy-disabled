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

    [Serializable]
    public class ColorGroup
    {
        public string id = "standard";
        public FrameAnimationClip[] clips;
    }

    [Header("Renderer")]
    [Tooltip("If empty, uses Renderer on this GameObject.")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Clips")]
    [SerializeField] private FrameAnimationClip[] clips;
    [SerializeField] private string initialClip = "idle";
    [SerializeField] private bool randomizeInitialFrame = true;

    [Header("Color Groups")]
    [SerializeField] private ColorGroup[] colorGroups;
    [Tooltip("Keeps the selected color group when this object is disabled and re-enabled.")]
    [SerializeField] private bool keepColorGroupOnReenable = true;

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
    private FrameAnimationClip[] activeClipSet;
    private FrameAnimationClip currentClip;
    private int currentFrame;
    private float frameTimer;
    private bool hasValidClip;
    private int selectedColorGroupIndex = -1;
    private bool hasSelectedColorGroup;
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
        MigrateLegacyClipsToStandardGroup();
        EnsureColorGroupSelected();
        BuildLookup();
    }

    private void OnEnable()
    {
        MigrateLegacyClipsToStandardGroup();
        EnsureColorGroupSelected();
        BuildLookup();

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
        if (activeClipSet == null || activeClipSet.Length == 0)
        {
            hasValidClip = false;
            return false;
        }

        if (string.IsNullOrWhiteSpace(clipId) || !clipLookup.TryGetValue(clipId, out int clipIndex))
        {
            hasValidClip = false;
            return false;
        }

        FrameAnimationClip nextClip = activeClipSet[clipIndex];
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

        activeClipSet = GetActiveClipSet();

        if (activeClipSet == null)
            return;

        for (int i = 0; i < activeClipSet.Length; i++)
        {
            FrameAnimationClip clip = activeClipSet[i];
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

    private FrameAnimationClip[] GetActiveClipSet()
    {
        if (colorGroups == null || colorGroups.Length == 0)
            return null;

        if (selectedColorGroupIndex < 0 || selectedColorGroupIndex >= colorGroups.Length)
            return null;

        ColorGroup selectedGroup = colorGroups[selectedColorGroupIndex];
        if (selectedGroup == null || selectedGroup.clips == null || selectedGroup.clips.Length == 0)
            return null;

        return selectedGroup.clips;
    }

    private void EnsureColorGroupSelected()
    {
        int[] validGroupIndices = GetValidGroupIndices();
        if (validGroupIndices.Length == 0)
        {
            selectedColorGroupIndex = -1;
            hasSelectedColorGroup = false;
            return;
        }

        if (hasSelectedColorGroup && keepColorGroupOnReenable)
            return;

        int standardIndex = FindGroupIndexById("standard");
        if (standardIndex >= 0)
        {
            selectedColorGroupIndex = standardIndex;
        }
        else
        {
            selectedColorGroupIndex = validGroupIndices[0];
        }

        hasSelectedColorGroup = true;
    }

    private int[] GetValidGroupIndices()
    {
        if (colorGroups == null)
            return Array.Empty<int>();

        List<int> valid = new List<int>();

        for (int i = 0; i < colorGroups.Length; i++)
        {
            ColorGroup group = colorGroups[i];
            if (group != null && group.clips != null && group.clips.Length > 0)
                valid.Add(i);
        }

        return valid.ToArray();
    }

    private int FindGroupIndexById(string groupId)
    {
        if (colorGroups == null || string.IsNullOrWhiteSpace(groupId))
            return -1;

        string normalizedGroupId = groupId.Trim().ToLowerInvariant();

        for (int i = 0; i < colorGroups.Length; i++)
        {
            ColorGroup group = colorGroups[i];
            if (group == null || string.IsNullOrWhiteSpace(group.id))
                continue;

            if (group.id.Trim().ToLowerInvariant() == normalizedGroupId)
                return i;
        }

        return -1;
    }

    private void MigrateLegacyClipsToStandardGroup()
    {
        if (clips == null || clips.Length == 0)
            return;

        if (colorGroups == null)
            colorGroups = Array.Empty<ColorGroup>();

        int standardIndex = FindGroupIndexById("standard");

        if (standardIndex < 0)
        {
            ColorGroup standardGroup = new ColorGroup { id = "standard", clips = clips };
            List<ColorGroup> expanded = new List<ColorGroup>(colorGroups);
            expanded.Insert(0, standardGroup);
            colorGroups = expanded.ToArray();
        }
        else
        {
            ColorGroup standardGroup = colorGroups[standardIndex];
            if (standardGroup.clips == null || standardGroup.clips.Length == 0)
            {
                standardGroup.clips = clips;
            }

            colorGroups[standardIndex] = standardGroup;
        }

        clips = Array.Empty<FrameAnimationClip>();
    }

    public void SetColorGroupByIndex(int index)
    {
        if (colorGroups == null || index < 0 || index >= colorGroups.Length)
            return;

        ColorGroup group = colorGroups[index];
        if (group == null || group.clips == null || group.clips.Length == 0)
            return;

        bool changed = selectedColorGroupIndex != index;
        selectedColorGroupIndex = index;
        hasSelectedColorGroup = true;

        BuildLookup();

        if (changed)
            Play(initialClip, true);
    }

    public void SetColorGroupById(string groupId)
    {
        int index = FindGroupIndexById(groupId);
        if (index >= 0)
            SetColorGroupByIndex(index);
    }

    public string GetSelectedColorGroupId()
    {
        if (colorGroups == null || selectedColorGroupIndex < 0 || selectedColorGroupIndex >= colorGroups.Length)
            return string.Empty;

        ColorGroup group = colorGroups[selectedColorGroupIndex];
        return group != null ? group.id : string.Empty;
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
        MigrateLegacyClipsToStandardGroup();

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
