using UnityEngine;

namespace ElementumDefense.Elements
{
    /// <summary>
    /// Defines all element types in the game
    /// Used by turrets, enemies, and damage calculations
    /// </summary>
    public enum ElementType
    {
        None,       // Neutral - no bonuses/penalties
        Fire,       //  High damage, burn effect
        Water,      //  Balanced, splash damage
        Ice,        // ❄ Slows enemies
        Earth,      //  Tank killer, armor penetration
        Lightning,  //  Chain damage, fast attack
        Nature,     //  DOT (poison), summons
        Dark,       //  Lifesteal, debuffs
        Light       //  Buffs allies, healing
    }

    /// <summary>
    /// Static utility class for element-related calculations
    /// Handles damage modifiers, color coding, and relationships
    /// </summary>
    public static class ElementUtility
    {
        // ==========================================
        // DAMAGE MODIFIERS
        // ==========================================

        /// <summary>
        /// Returns damage multiplier based on attacker vs defender element
        /// Example: Fire vs Ice = 1.5x damage (strong against)
        ///          Fire vs Water = 0.5x damage (weak against)
        /// </summary>
        /// <param name="attackerElement">Element of attacking turret</param>
        /// <param name="defenderElement">Element of target enemy</param>
        /// <returns>Damage multiplier (1.0 = normal, 1.5 = strong, 0.5 = weak)</returns>
        public static float GetDamageMultiplier(ElementType attackerElement, ElementType defenderElement)
        {
            // None element always deals normal damage
            if (attackerElement == ElementType.None || defenderElement == ElementType.None)
                return 1.0f;

            // Same element = slight resistance
            if (attackerElement == defenderElement)
                return 0.75f;

            // Check strong matchups (1.5x damage)
            if (IsStrongAgainst(attackerElement, defenderElement))
                return 1.5f;

            // Check weak matchups (0.5x damage)
            if (IsWeakAgainst(attackerElement, defenderElement))
                return 0.5f;

            // Neutral matchup
            return 1.0f;
        }

        /// <summary>
        /// Checks if attackerElement is STRONG against defenderElement
        /// Based on classic RPG element wheel
        /// </summary>
        private static bool IsStrongAgainst(ElementType attacker, ElementType defender)
        {
            return attacker switch
            {
                ElementType.Fire => defender == ElementType.Ice || defender == ElementType.Nature,
                ElementType.Water => defender == ElementType.Fire || defender == ElementType.Earth,
                ElementType.Ice => defender == ElementType.Water || defender == ElementType.Earth,
                ElementType.Earth => defender == ElementType.Lightning,
                ElementType.Lightning => defender == ElementType.Water,
                ElementType.Nature => defender == ElementType.Water || defender == ElementType.Earth,
                ElementType.Light => defender == ElementType.Dark,
                ElementType.Dark => defender == ElementType.Light,
                _ => false
            };
        }

        /// <summary>
        /// Checks if attackerElement is WEAK against defenderElement
        /// (Inverse of strong matchups)
        /// </summary>
        private static bool IsWeakAgainst(ElementType attacker, ElementType defender)
        {
            return IsStrongAgainst(defender, attacker);
        }

        // ==========================================
        // VISUAL HELPERS
        // ==========================================

        /// <summary>
        /// Returns signature color for each element
        /// Used for UI, particles, indicators
        /// </summary>
        public static Color GetElementColor(ElementType element)
        {
            return element switch
            {
                ElementType.Fire => new Color(1f, 0.3f, 0f),        // Orange-red
                ElementType.Water => new Color(0f, 0.5f, 1f),       // Blue
                ElementType.Ice => new Color(0.5f, 0.9f, 1f),       // Cyan
                ElementType.Earth => new Color(0.6f, 0.4f, 0.2f),   // Brown
                ElementType.Lightning => new Color(1f, 1f, 0.3f),   // Yellow
                ElementType.Nature => new Color(0.2f, 0.8f, 0.2f),  // Green
                ElementType.Dark => new Color(0.3f, 0f, 0.5f),      // Purple
                ElementType.Light => new Color(1f, 1f, 0.8f),       // White-yellow
                _ => Color.white                                     // Neutral
            };
        }

        /// <summary>
        /// Returns emoji icon for element (for debug/UI)
        /// </summary>
        public static string GetElementIcon(ElementType element)
        {
            return element switch
            {
                ElementType.Fire => "Fire",
                ElementType.Water => "Water",
                ElementType.Ice => "Ice",
                ElementType.Earth => "Earth",
                ElementType.Lightning => "Lightning",
                ElementType.Nature => "Nature",
                ElementType.Dark => "Dark",
                ElementType.Light => "Light",
                _ => "Neutral"
            };
        }

        /// <summary>
        /// Returns descriptive name for element
        /// </summary>
        public static string GetElementName(ElementType element)
        {
            return element.ToString();
        }

        // ==========================================
        // DEBUG HELPERS
        // ==========================================

        /// <summary>
        /// Logs element matchup table (for testing/balancing)
        /// </summary>
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

                    if (mult > 1.0f)
                        log += $"   {defender}({mult}x)";
                    else if (mult < 1.0f)
                        log += $"   {defender}({mult}x)";
                }

                Debug.Log(log);
            }
        }
    }
}