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
    [Tooltip("If empty, uses Renderer on this GameObject.")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Explosion Clip")]
    [SerializeField] private FrameAnimationClip explosionClip;
    [SerializeField] private bool randomizeInitialFrame = false;

    [Header("Behaviour")]
    [SerializeField] private bool autoTriggerFromMines = true;
    [SerializeField] private bool hideStaticMineWhenTriggered = true;
    [SerializeField] private bool destroyAfterAnimation = true;

    private MaterialPropertyBlock propertyBlock;
    private Mines mines;
    private bool hasTriggered;
    private bool isExplosionInstance;
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

    mines = GetComponent<Mines>();
    propertyBlock = new MaterialPropertyBlock();

    if (isExplosionInstance)
    {
        StartExplosionVisual();
    }
    // else: nichts tun – die Mine behält einfach ihr normales Material
}

    private void Update()
    {
        if (!isExplosionInstance && autoTriggerFromMines && !hasTriggered)
        {
            if (ShouldTriggerExplosion())
            {
                TriggerExplosion();
            }
        }

        if (!isExplosionInstance || !hasValidClip || explosionClip == null || explosionClip.frames == null || explosionClip.frames.Length == 0)
        {
            return;
        }

        float fps = Mathf.Max(1f, explosionClip.fps);
        float frameDuration = 1f / fps;
        frameTimer += Time.deltaTime;

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

    private bool ShouldTriggerExplosion()
    {
        if (mines == null)
        {
            mines = GetComponent<Mines>();
        }

        if (mines == null)
        {
            return false;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, mines.triggerRadius, mines.sheepLayer);
        return hits != null && hits.Length > 0;
    }

    public void TriggerExplosion()
    {
        if (hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        GameObject explosionObject = CreateExplosionInstance();
        if (explosionObject != null)
        {
            mine_boom explosion = explosionObject.GetComponent<mine_boom>();
            if (explosion != null)
            {
                explosion.InitializeAsExplosion(explosionClip, randomizeInitialFrame, destroyAfterAnimation);
            }
        }

        if (hideStaticMineWhenTriggered)
        {
            Destroy(gameObject);
        }
    }

    private GameObject CreateExplosionInstance()
    {
        GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
        clone.name = gameObject.name + "_Explosion";

        Mines cloneMines = clone.GetComponent<Mines>();
        if (cloneMines != null)
        {
            Destroy(cloneMines);
        }

        Collider[] colliders = clone.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        Rigidbody rb = clone.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }

        mine_boom cloneBoom = clone.GetComponent<mine_boom>();
        if (cloneBoom != null)
        {
            cloneBoom.isExplosionInstance = true;
            cloneBoom.autoTriggerFromMines = false;
            cloneBoom.hideStaticMineWhenTriggered = false;
        }

        return clone;
    }

    public void InitializeAsExplosion(FrameAnimationClip clip, bool randomizeFrame, bool destroyWhenFinished)
    {
        explosionClip = clip;
        randomizeInitialFrame = randomizeFrame;
        destroyAfterAnimation = destroyWhenFinished;
        isExplosionInstance = true;
        hasTriggered = true;
        StartExplosionVisual();
    }

    private void StartExplosionVisual()
    {
        hasValidClip = explosionClip != null && explosionClip.frames != null && explosionClip.frames.Length > 0;
        frameTimer = 0f;

        if (!hasValidClip)
        {
            if (destroyAfterAnimation)
            {
                Destroy(gameObject);
            }

            return;
        }

        if (randomizeInitialFrame)
        {
            currentFrame = UnityEngine.Random.Range(0, explosionClip.frames.Length);
        }
        else
        {
            currentFrame = 0;
        }

        ApplyClipFrame(currentFrame);
    }

    private void AdvanceFrame()
    {
        if (explosionClip == null || explosionClip.frames == null || explosionClip.frames.Length == 0)
        {
            return;
        }

        int next = currentFrame + 1;
        int frameCount = explosionClip.frames.Length;

        if (next >= frameCount)
        {
            if (explosionClip.loop)
            {
                next = 0;
            }
            else
            {
                next = frameCount - 1;
                ApplyClipFrame(next);

                if (destroyAfterAnimation)
                {
                    Destroy(gameObject);
                }

                enabled = false;
                return;
            }
        }

        if (next == currentFrame)
        {
            return;
        }

        currentFrame = next;
        ApplyClipFrame(currentFrame);
    }

    private void ApplyClipFrame(int frameIndex)
    {
        if (targetRenderer == null || explosionClip == null || explosionClip.frames == null)
        {
            return;
        }

        if (frameIndex < 0 || frameIndex >= explosionClip.frames.Length)
        {
            return;
        }

        Texture2D frame = explosionClip.frames[frameIndex];
        if (frame == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(MainTexId, frame);
        propertyBlock.SetTexture(BaseMapId, frame);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void HideStaticMineVisuals()
    {
        if (targetRenderer != null)
        {
            targetRenderer.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private void OnValidate()
    {
        if (explosionClip == null)
        {
            return;
        }

        if (explosionClip.frames != null && explosionClip.frames.Length == 0)
        {
            Debug.LogWarning("Explosion clip has no frames assigned.", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!autoTriggerFromMines || mines == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, mines.triggerRadius);
    }
}
