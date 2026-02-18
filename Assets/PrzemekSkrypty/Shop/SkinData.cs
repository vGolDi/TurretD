// Assets/PrzemekSkrypty/Shop/SkinData.cs
using UnityEngine;

namespace ElementumDefense.Shop
{
    /// <summary>
    /// Placeholder ScriptableObject for skin definitions.
    /// Will be expanded when skin system is implemented.
    /// Create via: Right Click → Create → Tower Defense → Shop → Skin Data
    /// </summary>
    public enum SkinCategory
    {
        Tower,      // Skin for a specific tower
        Map,        // Map visual theme
        UI,         // UI theme
        Projectile, // Projectile visual
        Effect      // Particle/VFX skin
    }

    [CreateAssetMenu(fileName = "New Skin", menuName = "Tower Defense/Shop/Skin Data")]
    public class SkinData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Unique identifier for save/load")]
        public string skinId;

        public string skinName = "Default Skin";

        [TextArea(2, 3)]
        public string description = "A cosmetic skin.";

        public Sprite previewImage;
        public SkinCategory category = SkinCategory.Tower;

        [Header("Application Target")]
        [Tooltip("Identifier of the tower/map/entity this skin applies to")]
        public string targetId;

        [Header("Visuals (Placeholder - expand later)")]
        [Tooltip("Color tint applied to the target")]
        public Color skinTint = Color.white;

        // ==========================================
        // FUTURE EXPANSION (uncomment when ready)
        // ==========================================
        // public Material skinMaterial;
        // public RuntimeAnimatorController skinAnimator;
        // public GameObject skinPrefabOverride;
        // public Sprite[] spriteSheet;
        // public ParticleSystem customVFX;

        [Header("Rarity")]
        public SkinRarity rarity = SkinRarity.Common;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(skinId))
                skinId = name;
        }
    }

    public enum SkinRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}