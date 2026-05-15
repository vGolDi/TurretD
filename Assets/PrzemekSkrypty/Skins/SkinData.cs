// Assets/PrzemekSkrypty/Skins/SkinData.cs
using UnityEngine;

namespace ElementumDefense.Skins
{
    public enum SkinCategory
    {
        Character,
        Turret,
        Projectile,
        Effect
    }

    public enum SkinRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "New Skin", menuName = "Tower Defense/Skins/Skin Data")]
    public class SkinData : ScriptableObject
    {
        [Header("=== IDENTITY ===")]
        [Tooltip("Unique ID for save/load. Auto-fills from asset name.")]
        public string skinId;

        public string skinName = "Default Skin";

        [TextArea(2, 3)]
        public string description = "A cosmetic skin.";

        public SkinCategory category = SkinCategory.Turret;
        public SkinRarity rarity = SkinRarity.Common;

        [Header("=== TARGET ===")]
        [Tooltip("Which entity this skin is for (e.g., turret asset name or character ID)")]
        public string targetId;

        [Tooltip("Human-readable target name for UI grouping (e.g., 'Fire Turret')")]
        public string targetDisplayName;

        [Header("=== VISUALS ===")]
        [Tooltip("Preview image for shop/inventory UI")]
        public Sprite previewIcon;

        [Tooltip("Alternate display model to swap in (replaces child model on the entity)")]
        public GameObject skinPrefab;

        [Tooltip("Alternate material to apply to all renderers")]
        public Material skinMaterial;

        [Tooltip("Color tint applied on top of material")]
        public Color skinTint = Color.white;

        [Header("=== FLAGS ===")]
        [Tooltip("Is this a default skin that every player owns from the start?")]
        public bool isDefault = false;

        // ==========================================
        // HELPERS
        // ==========================================

        public Color GetRarityColor()
        {
            return rarity switch
            {
                SkinRarity.Common => new Color(0.7f, 0.7f, 0.7f),
                SkinRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
                SkinRarity.Rare => new Color(0.2f, 0.5f, 1f),
                SkinRarity.Epic => new Color(0.6f, 0.2f, 0.9f),
                SkinRarity.Legendary => new Color(1f, 0.8f, 0f),
                _ => Color.white
            };
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(skinId))
                skinId = name;
        }
    }
}
