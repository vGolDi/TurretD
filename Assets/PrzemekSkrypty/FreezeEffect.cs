using UnityEngine;

namespace ElementumDefense.StatusEffects
{
    /// <summary>
    /// Ice freeze effect - completely stops enemy movement
    /// Short duration, doesn't stack
    /// Associated with Ice element
    /// </summary>
    public class FreezeEffect : StatusEffect
    {
        public override StatusEffectType EffectType => StatusEffectType.Freeze;
        public override string DisplayName => "Frozen";
        public override string Icon => "FREEZEICON";

        public override int MaxStacks => 1;
        public override bool IsStackable => false;
        public override bool RefreshOnReapply => false; // Don't refresh - can be OP

        private GameObject freezeVFX;
        private Color originalColor;
        private Renderer targetRenderer;

        public FreezeEffect(float duration)
        {
            MaxDuration = duration;
        }

        protected override void OnApplied()
        {
            base.OnApplied();

            // Visual effect - tint enemy blue
            targetRenderer = targetGameObject.GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                originalColor = targetRenderer.material.color;
                targetRenderer.material.color = new Color(0.5f, 0.9f, 1f); // Icy blue
            }

            // TODO: Spawn ice VFX
            // freezeVFX = Object.Instantiate(Resources.Load<GameObject>("VFX/FreezeEffect"), targetGameObject.transform);

            Debug.Log($"[FreezeEffect]  Enemy frozen for {MaxDuration}s");
        }

        public override void OnExpired()
        {
            base.OnExpired();
            RestoreVisuals();
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            RestoreVisuals();
        }

        private void RestoreVisuals()
        {
            // Restore original color
            if (targetRenderer != null)
            {
                targetRenderer.material.color = originalColor;
            }

            // Destroy VFX
            if (freezeVFX != null)
            {
                Object.Destroy(freezeVFX);
            }
        }
    }
}