using UnityEngine;
using ElementumDefense.Turrets;

namespace ElementumDefense.Elements
{
    /// <summary>
    /// Defines all element types in the game.
    /// Used by turrets, enemies, and damage calculations.
    /// 
    /// IMPORTANT: enum values are EXPLICIT. We removed Water (was 2) and
    /// Earth (was 4) but kept the remaining numbers stable so existing
    /// SO assets (turrets, cards, enemies) that store an int value
    /// don't get silently re-mapped to a different element.
    /// 
    /// If you ever see "INVALID" element in an Inspector, that asset
    /// originally pointed to Water or Earth — delete or reassign it.
    /// </summary>
    public enum ElementType
    {
        None      = 0,  // Neutral - no bonuses/penalties
        Fire      = 1,  // High damage, burn effect
        // 2 was Water — removed
        Ice       = 3,  // Slows enemies
        // 4 was Earth — removed
        Lightning = 5,  // Chain damage, fast attack
        Nature    = 6,  // DOT (poison), summons
        Dark      = 7,  // Lifesteal, debuffs
        Light     = 8   // Buffs allies, healing
    }

    /// <summary>
    /// Static utility class for element-related calculations.
    /// Handles damage modifiers, color coding, and relationships.
    /// </summary>
    public static class ElementUtility
    {
        // ==========================================
        // DAMAGE MODIFIERS
        // ==========================================

        /// <summary>
        /// Returns damage multiplier based on attacker vs defender element.
        /// Example: Fire vs Ice = 1.5x damage (strong against).
        ///          Fire vs Nature = depends (Fire is strong vs Nature in current chart).
        /// </summary>
        public static float GetDamageMultiplier(ElementType attackerElement, ElementType defenderElement)
        {
            if (attackerElement == ElementType.None || defenderElement == ElementType.None)
                return 1.0f;

            if (attackerElement == defenderElement)
                return 0.75f;

            if (IsStrongAgainst(attackerElement, defenderElement))
                return 1.5f;

            if (IsWeakAgainst(attackerElement, defenderElement))
                return 0.5f;

            return 1.0f;
        }

        /// <summary>
        /// Strong matchups for the 6-element setup (Fire/Ice/Lightning/Nature/Dark/Light).
        /// Pairs:
        ///   Fire    > Ice, Nature
        ///   Ice     > Lightning
        ///   Lightning > Nature
        ///   Nature  > Dark   (life > corruption)
        ///   Dark    <-> Light
        /// </summary>
        private static bool IsStrongAgainst(ElementType attacker, ElementType defender)
        {
            return attacker switch
            {
                ElementType.Fire      => defender == ElementType.Ice || defender == ElementType.Nature,
                ElementType.Ice       => defender == ElementType.Lightning,
                ElementType.Lightning => defender == ElementType.Nature,
                ElementType.Nature    => defender == ElementType.Dark,
                ElementType.Light     => defender == ElementType.Dark,
                ElementType.Dark      => defender == ElementType.Light,
                _ => false
            };
        }

        private static bool IsWeakAgainst(ElementType attacker, ElementType defender)
            => IsStrongAgainst(defender, attacker);

        // ==========================================
        // VISUAL HELPERS
        // ==========================================

        public static Color GetElementColor(ElementType element)
        {
            return element switch
            {
                ElementType.Fire      => new Color(1f, 0.3f, 0f),        // Orange-red
                ElementType.Ice       => new Color(0.5f, 0.9f, 1f),      // Cyan
                ElementType.Lightning => new Color(1f, 1f, 0.3f),        // Yellow
                ElementType.Nature    => new Color(0.2f, 0.8f, 0.2f),    // Green
                ElementType.Dark      => new Color(0.3f, 0f, 0.5f),      // Purple
                ElementType.Light     => new Color(1f, 1f, 0.8f),        // White-yellow
                _ => Color.white
            };
        }

        public static string GetElementIcon(ElementType element)
        {
            return element switch
            {
                ElementType.Fire      => "Fire",
                ElementType.Ice       => "Ice",
                ElementType.Lightning => "Lightning",
                ElementType.Nature    => "Nature",
                ElementType.Dark      => "Dark",
                ElementType.Light     => "Light",
                _ => "Neutral"
            };
        }

        public static string GetElementName(ElementType element) => element.ToString();

        // ==========================================
        // DEBUG HELPERS
        // ==========================================

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogElementMatchups()
        {
            Debug.Log("========== ELEMENT MATCHUP TABLE ==========");

            foreach (ElementType attacker in System.Enum.GetValues(typeof(ElementType)))
            {
                if (attacker == ElementType.None) continue;

                string log = $"{GetElementIcon(attacker)} {attacker}: ";

                foreach (ElementType defender in System.Enum.GetValues(typeof(ElementType)))
                {
                    if (defender == ElementType.None) continue;

                    float mult = GetDamageMultiplier(attacker, defender);

                    if (mult > 1.0f)      log += $"   {defender}({mult}x)";
                    else if (mult < 1.0f) log += $"   {defender}({mult}x)";
                }

                Debug.Log(log);
            }
        }
    }
}
