using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.Turrets;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Sabotage card - affects opponent(s)
    /// Drawn from global pool (not player's deck)
    /// </summary>
    [CreateAssetMenu(fileName = "New Sabotage", menuName = "Tower Defense/Cards/Sabotage Card")]
    public class SabotageCardData : ScriptableObject
    {
        [Header("Basic Info")]
        public string sabotageName = "New Sabotage";

        [TextArea(3, 5)]
        [Tooltip("What this sabotage does")]
        public string description = "Sabotage description...";

        [Header("Sabotage Properties")]
        public CardRarity rarity = CardRarity.Common;

        [Tooltip("Category/tag of sabotage (for anti-spam)")]
        public SabotageTag sabotageTag = SabotageTag.Economy;

        [Header("Duration")]
        public SabotageDurationType durationType = SabotageDurationType.Temporary;

        [Tooltip("Duration in seconds (0 = instant, -1 = permanent)")]
        public float duration = 10f;

        [Tooltip("Duration in rounds/waves (alternative to seconds)")]
        public int durationRounds = 0;

        [Header("Visual")]
        public Sprite sabotageIcon;
        public Color sabotageColor = Color.red;

        [Header("VFX — Visual Feedback")]
        [Tooltip("Efekt VFX spawnowany na arenie ofiary (np. lecące nietoperze, eksplozja)")]
        public GameObject arenaVFXPrefab;

        [Tooltip("Czas życia efektu areny w sekundach (0 = auto-destroy z ParticleSystem)")]
        public float arenaVFXDuration = 3f;

        [Tooltip("Ikona/prefab przypinany do każdego turretu ofiary (np. kłódka, zębatka).\n" +
                 "Pozostaw puste jeśli sabotaż nie dotyczy turretów.")]
        public GameObject turretIndicatorPrefab;

        [Tooltip("Kolor flash na ekranie ofiary (alpha 0 = brak flash)")]
        public Color screenFlashColor = new Color(1f, 0f, 0f, 0f);

        [Tooltip("Dźwięk odtwarzany przy aktywacji sabotażu")]
        public AudioClip activationSound;

        [Header("Drop Rate (for weighted random)")]
        [Tooltip("Weight for random selection (higher = more common)")]
        [Range(1f, 100f)]
        public float dropWeight = 50f;

        // ==========================================
        // EFFECT SYSTEM
        // ==========================================

        [Header("Effect Implementation")]
        [Tooltip("Drag SabotageEffect ScriptableObject here")]
        public SabotageEffectBase sabotageEffect;

        // ==========================================
        // SELF-SABOTAGE (GAMBLE)
        // ==========================================

        [Header("Self-Sabotage / Gamble")]
        [Tooltip("Opponent = klasyczny sabotaz na wroga. Self = utrudnienie sobie gry za nagrode.")]
        public SabotageTarget targetType = SabotageTarget.Opponent;

        [Tooltip("Bonus gold za przetrwanie self-sabotazu (in-match gold dodawany do PlayerGold)")]
        public int rewardGold = 0;

        [Tooltip("Mnoznik golda z fali (np. 2.0 = 2x wiecej golda z zabitych wrogow)")]
        public float rewardGoldMultiplier = 1f;

        [Tooltip("Czy nagroda jest przyznawana TYLKO jesli gracz przezyje fale?")]
        public bool rewardOnSurvive = true;

        [Tooltip("Ile fal trwa self-sabotaz (nagroda po przetrwaniu tylu fal)")]
        public int challengeWaves = 1;

        /// <summary>Czy to self-sabotaz?</summary>
        public bool IsSelfSabotage => targetType == SabotageTarget.Self;

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Returns formatted tooltip
        /// </summary>
        public string GetTooltip()
        {
            string tooltip = $"<b>{sabotageName}</b> [{rarity}]\n\n";
            tooltip += $"{description}\n\n";

            if (sabotageEffect != null)
            {
                tooltip += $"<i>{sabotageEffect.GetEffectDescription()}</i>\n";
            }

            // Duration info
            string durationInfo = GetDurationText();
            tooltip += $"\n⏱️ {durationInfo}";

            return tooltip;
        }

        /// <summary>
        /// Returns human-readable duration
        /// </summary>
        public string GetDurationText()
        {
            if (durationType == SabotageDurationType.Permanent)
            {
                return "Permanent (rest of game)";
            }

            if (durationType == SabotageDurationType.Instant)
            {
                return "Instant (one-time)";
            }

            // Temporary
            if (durationRounds > 0)
            {
                return $"{durationRounds} round{(durationRounds > 1 ? "s" : "")}";
            }

            return $"{duration:F0} seconds";
        }

        /// <summary>
        /// Returns rarity color
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                CardRarity.Common => new Color(0.8f, 0.8f, 0.8f),
                CardRarity.Rare => new Color(0.3f, 0.6f, 1f),
                CardRarity.Legendary => new Color(1f, 0.8f, 0f),
                _ => Color.white
            };
        }
        public Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
        private void OnValidate()
        {
            if (sabotageEffect == null)
            {
                Debug.LogWarning($"[SabotageCardData] {sabotageName} has no effect assigned!");
            }

            // Auto-set instant duration
            if (durationType == SabotageDurationType.Instant)
            {
                duration = 0f;
                durationRounds = 0;
            }
        }
    }

    // ==========================================
    // ENUMS
    // ==========================================

    /// <summary>
    /// Sabotage category (for anti-spam)
    /// Max 2 of same tag in one draft
    /// </summary>
    public enum SabotageTag
    {
        Economy,    // Gold generation, costs, income
        Turrets,    // Turret damage, upgrades, building
        Enemies,    // Enemy HP, speed, element
        Arena,      // Build spots, fog, path manipulation
        Player,     // Player HP, movement, vision
        SelfSabotage // Self-imposed challenge for reward
    }

    /// <summary>
    /// Who does the sabotage target?
    /// </summary>
    public enum SabotageTarget
    {
        Opponent,   // Classic — hurts the enemy player
        Self        // Gamble — hurts yourself for a reward
    }

    /// <summary>
    /// How long does sabotage last?
    /// </summary>
    public enum SabotageDurationType
    {
        Instant,    // One-time effect (e.g., destroy random turret)
        Temporary,  // Lasts X seconds/rounds
        Permanent   // Lasts until end of game
    }
}