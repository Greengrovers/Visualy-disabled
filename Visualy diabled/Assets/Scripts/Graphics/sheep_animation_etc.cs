using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class sheep_animation_etc : MonoBehaviour
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
        public string id = "default";
        public FrameAnimationClip[] clips;
    }

    [Header("Renderer")]
    [Tooltip("If empty, uses Renderer on this GameObject.")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Legacy Clips (Auto-Migrated)")]
    [SerializeField] private FrameAnimationClip[] clips;
    [SerializeField] private string initialClip = "idle";
    [SerializeField] private bool randomizeInitialFrame = true;

    [Header("Color Groups")]
    [SerializeField] private ColorGroup[] colorGroups;
    [Tooltip("Keeps the chosen color group when this object is disabled and re-enabled.")]
    [SerializeField] private bool keepColorGroupOnReenable = true;

    [Header("Playback")]
    [SerializeField] private float speedMultiplier = 1f;
    [Tooltip("If true, animation ignores Time.timeScale and always runs in real time.")]
    [SerializeField] private bool ignoreTimeScale = false;

    [Header("State Sync (Optional)")]
    [Tooltip("If enabled, this animator follows SheepController state from a parent object.")]
    [SerializeField] private bool syncWithSheepController = true;
    [SerializeField] private SheepController sheepController;
    [SerializeField] private string wanderingClipId = "idle";
    [SerializeField] private string fleeingClipId = "run";
    [SerializeField] private string regroupingClipId = "run";

    private readonly Dictionary<string, int> clipLookup = new Dictionary<string, int>();
    private MaterialPropertyBlock propertyBlock;
    private FrameAnimationClip[] activeClipSet;
    private FrameAnimationClip currentClip;
    private int currentFrame;
    private float frameTimer;
    private bool hasValidClip;
    private bool hasSyncedState;
    private SheepState lastSyncedState;
    private int selectedColorGroupIndex = -1;
    private bool hasSelectedColorGroup;

    // Works with both built-in and URP/HDRP shader property names.
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (sheepController == null)
        {
            sheepController = GetComponentInParent<SheepController>();
        }

        propertyBlock = new MaterialPropertyBlock();
        MigrateLegacyClipsToBrownGroup();
        EnsureColorGroupSelected();
        BuildLookup();
    }

    private void OnEnable()
    {
        MigrateLegacyClipsToBrownGroup();
        EnsureColorGroupSelected();
        BuildLookup();

        if (syncWithSheepController && sheepController != null)
        {
            SyncWithState(true);
        }
        else
        {
            Play(initialClip, true);
        }
    }

    private void Update()
    {
        if (syncWithSheepController)
        {
            SyncWithState(false);
        }

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
        {
            return;
        }

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
        {
            return null;
        }

        if (selectedColorGroupIndex < 0 || selectedColorGroupIndex >= colorGroups.Length)
        {
            return null;
        }

        ColorGroup selectedGroup = colorGroups[selectedColorGroupIndex];
        if (selectedGroup == null || selectedGroup.clips == null || selectedGroup.clips.Length == 0)
        {
            return null;
        }

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
        {
            return;
        }

        selectedColorGroupIndex = validGroupIndices[UnityEngine.Random.Range(0, validGroupIndices.Length)];

        hasSelectedColorGroup = true;
    }

    private int[] GetValidGroupIndices()
    {
        if (colorGroups == null)
        {
            return Array.Empty<int>();
        }

        List<int> valid = new List<int>();

        for (int i = 0; i < colorGroups.Length; i++)
        {
            ColorGroup group = colorGroups[i];
            if (group != null && group.clips != null && group.clips.Length > 0)
            {
                valid.Add(i);
            }
        }

        return valid.ToArray();
    }

    private void MigrateLegacyClipsToBrownGroup()
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        if (colorGroups == null)
        {
            colorGroups = Array.Empty<ColorGroup>();
        }

        int brownIndex = -1;
        for (int i = 0; i < colorGroups.Length; i++)
        {
            ColorGroup group = colorGroups[i];
            if (group == null || string.IsNullOrWhiteSpace(group.id))
            {
                continue;
            }

            if (group.id.Trim().ToLowerInvariant() == "brown")
            {
                brownIndex = i;
                break;
            }
        }

        if (brownIndex < 0)
        {
            ColorGroup brownGroup = new ColorGroup
            {
                id = "brown",
                clips = clips
            };

            List<ColorGroup> expanded = new List<ColorGroup>(colorGroups.Where(g => g != null));
            expanded.Insert(0, brownGroup);
            colorGroups = expanded.ToArray();
        }
        else
        {
            ColorGroup brownGroup = colorGroups[brownIndex];
            if (brownGroup.clips == null || brownGroup.clips.Length == 0)
            {
                brownGroup.clips = clips;
            }
            else
            {
                Dictionary<string, FrameAnimationClip> merged = new Dictionary<string, FrameAnimationClip>();

                for (int i = 0; i < brownGroup.clips.Length; i++)
                {
                    FrameAnimationClip clip = brownGroup.clips[i];
                    if (clip == null || string.IsNullOrWhiteSpace(clip.id))
                    {
                        continue;
                    }
                    string key = clip.id.Trim().ToLowerInvariant();
                    if (!merged.ContainsKey(key))
                    {
                        merged.Add(key, clip);
                    }
                }

                for (int i = 0; i < clips.Length; i++)
                {
                    FrameAnimationClip clip = clips[i];
                    if (clip == null || string.IsNullOrWhiteSpace(clip.id))
                    {
                        continue;
                    }
                    string key = clip.id.Trim().ToLowerInvariant();
                    if (!merged.ContainsKey(key))
                    {
                        merged.Add(key, clip);
                    }
                }

                brownGroup.clips = new List<FrameAnimationClip>(merged.Values).ToArray();
            }

            colorGroups[brownIndex] = brownGroup;
        }

        clips = Array.Empty<FrameAnimationClip>();
    }

    private void SyncWithState(bool force)
    {
        if (sheepController == null)
        {
            sheepController = GetComponentInParent<SheepController>();
            if (sheepController == null)
            {
                return;
            }
        }

        SheepState state = sheepController.currentState;
        if (!force && hasSyncedState && state == lastSyncedState)
        {
            return;
        }

        switch (state)
        {
            case SheepState.Wandering:
                Play(wanderingClipId, false);
                break;
            case SheepState.Fleeing:
                Play(fleeingClipId, false);
                break;
            case SheepState.Regrouping:
                Play(regroupingClipId, false);
                break;
        }

        lastSyncedState = state;
        hasSyncedState = true;
    }

    private void OnValidate()
    {
        MigrateLegacyClipsToBrownGroup();
        ValidateClipArray(clips);

        if (colorGroups == null)
        {
            return;
        }

        for (int i = 0; i < colorGroups.Length; i++)
        {
            ColorGroup group = colorGroups[i];
            if (group == null)
            {
                continue;
            }

            ValidateClipArray(group.clips);
        }
    }

    private void ValidateClipArray(FrameAnimationClip[] clipArray)
    {
        if (clipArray == null)
        {
            return;
        }

        for (int i = 0; i < clipArray.Length; i++)
        {
            FrameAnimationClip clip = clipArray[i];
            if (clip == null || string.IsNullOrWhiteSpace(clip.id))
            {
                continue;
            }

            string clipId = clip.id.Trim().ToLowerInvariant();
            if (clipId == "idle" || clipId == "run")
            {
                clip.loop = true;
            }

            if (clipId == "idle" && clip.frames != null && clip.frames.Length != 8)
            {
                Debug.LogWarning("Clip 'idle' is expected to have 8 frames.", this);
            }

            if (clipId == "run" && clip.frames != null && clip.frames.Length != 4)
            {
                Debug.LogWarning("Clip 'run' is expected to have 4 frames.", this);
            }
        }
    }
}
