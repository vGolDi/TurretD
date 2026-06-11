using UnityEngine;
using ElementumDefense.Turrets;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Animated indicator that floats above a turret during sabotage.
    /// Handles: bobbing, pulsing scale, optional rotation, fade-in/out.
    /// 
    /// Attach to a prefab with a SpriteRenderer or MeshRenderer.
    /// SabotageVFXManager spawns this above each turret and auto-adds BillboardToCamera.
    /// </summary>
    public class SabotageIndicator : MonoBehaviour
    {
        [Header("Bobbing")]
        [Tooltip("Vertical bob amplitude")]
        [SerializeField] private float bobAmplitude = 0.15f;

        [Tooltip("Bobbing speed")]
        [SerializeField] private float bobSpeed = 2f;

        [Header("Pulse")]
        [Tooltip("Scale pulse amount (0 = no pulse)")]
        [SerializeField] private float pulseAmount = 0.1f;

        [Tooltip("Pulse speed")]
        [SerializeField] private float pulseSpeed = 3f;

        [Header("Rotation")]
        [Tooltip("Spin around Y axis (degrees/sec, 0 = no spin)")]
        [SerializeField] private float spinSpeed = 0f;

        [Header("Fade")]
        [Tooltip("Fade in duration")]
        [SerializeField] private float fadeInTime = 0.3f;

        [Tooltip("Should the indicator flash/blink?")]
        [SerializeField] private bool blinkEnabled = false;

        [Tooltip("Blink interval (on/off cycle)")]
        [SerializeField] private float blinkInterval = 0.8f;

        [Header("Color")]
        [Tooltip("Override color (applied to SpriteRenderer or Material _Color)")]
        [SerializeField] private Color indicatorColor = Color.white;

        [Tooltip("Apply color override")]
        [SerializeField] private bool applyColor = false;

        // Runtime
        private Vector3 baseLocalPos;
        private Vector3 baseScale;
        private float timeAlive = 0f;
        private Renderer cachedRenderer;
        private SpriteRenderer cachedSprite;
        private float currentAlpha = 0f;

        private void Start()
        {
            baseLocalPos = transform.localPosition;
            baseScale = transform.localScale;

            // Start invisible for fade-in
            transform.localScale = Vector3.zero;

            // Cache renderer
            cachedSprite = GetComponentInChildren<SpriteRenderer>();
            if (cachedSprite == null)
                cachedRenderer = GetComponentInChildren<Renderer>();

            // Apply color
            if (applyColor)
            {
                if (cachedSprite != null)
                    cachedSprite.color = indicatorColor;
                else if (cachedRenderer != null)
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    cachedRenderer.GetPropertyBlock(block);
                    block.SetColor("_Color", indicatorColor);
                    cachedRenderer.SetPropertyBlock(block);
                }
            }
        }

        private void Update()
        {
            timeAlive += Time.deltaTime;

            // Fade in
            if (timeAlive < fadeInTime)
            {
                float t = timeAlive / fadeInTime;
                currentAlpha = t;
                transform.localScale = baseScale * EaseOutBack(t);
            }
            else
            {
                currentAlpha = 1f;

                // Bobbing
                float bobY = Mathf.Sin(timeAlive * bobSpeed) * bobAmplitude;
                transform.localPosition = baseLocalPos + Vector3.up * bobY;

                // Pulse
                float pulse = 1f + Mathf.Sin(timeAlive * pulseSpeed) * pulseAmount;
                transform.localScale = baseScale * pulse;
            }

            // Spin
            if (spinSpeed != 0f)
            {
                transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            }

            // Blink
            if (blinkEnabled && timeAlive > fadeInTime)
            {
                float blinkPhase = Mathf.PingPong(timeAlive / blinkInterval, 1f);
                float blinkAlpha = Mathf.Lerp(0.3f, 1f, blinkPhase);
                SetAlpha(blinkAlpha);
            }
        }

        private void SetAlpha(float alpha)
        {
            if (cachedSprite != null)
            {
                Color c = cachedSprite.color;
                cachedSprite.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        /// <summary>
        /// Smooth fade-out then self-destroy.
        /// Called when sabotage expires.
        /// </summary>
        public void FadeOutAndDestroy(float duration = 0.5f)
        {
            StartCoroutine(FadeOutCoroutine(duration));
        }

        private System.Collections.IEnumerator FadeOutCoroutine(float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = startScale * (1f - t);
                SetAlpha(1f - t);
                yield return null;
            }

            Destroy(gameObject);
        }

        // Easing function for bouncy appear
        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
