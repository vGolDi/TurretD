using UnityEngine;

namespace ElementumDefense.Emotes
{
    /// <summary>
    /// Defines a single emote (emoji/reaction).
    /// Works like SkinData — can be owned, equipped to wheel slots, bought in shop.
    /// </summary>
    [CreateAssetMenu(fileName = "New Emote", menuName = "Tower Defense/Emotes/Emote Data")]
    public class EmoteData : ScriptableObject
    {
        [Header("=== IDENTITY ===")]
        [Tooltip("Unique ID for save/load")]
        public string emoteId;

        public string emoteName = "Smile";

        [TextArea(1, 2)]
        public string description = "A friendly smile.";

        [Header("=== VISUAL ===")]
        [Tooltip("Emote icon displayed in wheel and on screen")]
        public Sprite emoteIcon;

        [Tooltip("Color tint for the emote popup")]
        public Color emoteColor = Color.white;

        [Tooltip("Optional animated prefab (3D model, particles) spawned on display.\n" +
                 "If null, just shows the icon sprite.")]
        public GameObject animatedPrefab;

        [Tooltip("Display duration on opponent's screen")]
        public float displayDuration = 2.5f;

        [Tooltip("Optional sound effect")]
        public AudioClip emoteSound;

        [Header("=== SHOP / UNLOCK ===")]
        public EmoteRarity rarity = EmoteRarity.Common;

        [Tooltip("Is this emote free for everyone?")]
        public bool isDefault = false;

        [Tooltip("Category for shop filtering")]
        public EmoteCategory category = EmoteCategory.Reaction;

        // ==========================================
        // HELPERS
        // ==========================================

        public Color GetRarityColor()
        {
            return rarity switch
            {
                EmoteRarity.Common => new Color(0.7f, 0.7f, 0.7f),
                EmoteRarity.Rare => new Color(0.3f, 0.6f, 1f),
                EmoteRarity.Epic => new Color(0.6f, 0.2f, 0.9f),
                EmoteRarity.Legendary => new Color(1f, 0.8f, 0f),
                _ => Color.white
            };
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(emoteId))
                emoteId = name;
        }
    }

    // ==========================================
    // ENUMS
    // ==========================================

    public enum EmoteRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum EmoteCategory
    {
        Reaction,   // Smile, sad, angry, laugh
        Taunt,      // BM emotes
        Strategic,  // "Help!", "Good game", "Thanks"
        Seasonal    // Holiday/event emotes
    }
}
