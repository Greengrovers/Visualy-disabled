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

    private readonly Dictionary<string, int> clipLookup = new Dictionary<string, int>();
    private MaterialPropertyBlock propertyBlock;
    private FrameAnimationClip currentClip;
    private int currentFrame;
    private float frameTimer;
    private bool hasValidClip;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
        BuildLookup();
    }

    private void OnEnable()
    {
        Play(initialClip, true);
    }

    private void Update()
    {
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
