using UnityEngine;
using System;

public class mine_boom : MonoBehaviour
{
    [Serializable]
    public class FrameAnimationClip
    {
        public string id = "explode";
        public Texture2D[] frames;
        [Min(1f)] public float fps = 12f;
        public bool loop = false;
    }

    [Header("Renderer")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Explosion Animation")]
    [SerializeField] private FrameAnimationClip explosionClip;
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool destroyAfterAnimation = false;
    [SerializeField] private bool hideAfterAnimation = true;

    private MaterialPropertyBlock propertyBlock;
    private int currentFrame;
    private float frameTimer;
    private bool isPlaying;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private void Awake()
{
    if (targetRenderer == null)
    {
        targetRenderer = GetComponent<Renderer>();
    }

    propertyBlock = new MaterialPropertyBlock();
}

    private void Start()
    {
        if (playOnStart)
        {
            PlayExplosion();
        }
    }

    private void Update()
    {
        if (!isPlaying || explosionClip == null || explosionClip.frames == null || explosionClip.frames.Length == 0)
        {
            return;
        }

        float frameDuration = 1f / Mathf.Max(1f, explosionClip.fps);
        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            AdvanceFrame();
        }
    }

    public void PlayExplosion()
    {
        if (explosionClip == null || explosionClip.frames == null || explosionClip.frames.Length == 0)
        {
            return;
        }

        if (targetRenderer != null)
        {
            targetRenderer.enabled = true;
        }

        currentFrame = 0;
        frameTimer = 0f;
        isPlaying = true;

        ApplyClipFrame(currentFrame);
    }

    private void AdvanceFrame()
    {
        int nextFrame = currentFrame + 1;

        if (nextFrame >= explosionClip.frames.Length)
        {
            if (explosionClip.loop)
            {
                nextFrame = 0;
            }
            else
            {
                isPlaying = false;

                if (hideAfterAnimation && targetRenderer != null)
                {
                    targetRenderer.enabled = false;
                }

                if (destroyAfterAnimation)
                {
                    Destroy(gameObject);
                }

                return;
            }
        }

        currentFrame = nextFrame;
        ApplyClipFrame(currentFrame);
    }

    private void ApplyClipFrame(int frameIndex)
    {
        if (targetRenderer == null || explosionClip.frames[frameIndex] == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(MainTexId, explosionClip.frames[frameIndex]);
        propertyBlock.SetTexture(BaseMapId, explosionClip.frames[frameIndex]);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}