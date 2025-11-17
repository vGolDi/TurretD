using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Manages active cards for a single player
    /// Handles activation, deactivation, and modifier aggregation
    /// Attach to Player prefab
    /// </summary>
    public class PlayerCardManager : MonoBehaviour
    {
        [Header("Active Cards")]
        [Tooltip("Cards currently active (drafted in this match)")]
        [SerializeField] private List<CardData> activeCards = new List<CardData>();

        [Header("Modifiers (Read-Only)")]
        [SerializeField, Tooltip("Current damage multiplier from cards")]
        private float damageMultiplier = 1f;

        [SerializeField, Tooltip("Current fire rate multiplier from cards")]
        private float fireRateMultiplier = 1f;

        [SerializeField, Tooltip("Current range multiplier from cards")]
        private float rangeMultiplier = 1f;

        [SerializeField, Tooltip("Current turret cost multiplier from cards")]
        private float turretCostMultiplier = 1f;

        [SerializeField, Tooltip("Passive gold per second from cards")]
        private int passiveGoldPerSecond = 0;

        [Header("Active Sabotages (Received)")]
        [SerializeField] private List<ActiveSabotage> activeSabotages = new List<ActiveSabotage>();

        [Header("References")]
        private PhotonView photonView;
        private PlayerGold playerGold;
        private PlayerHealth playerHealth;
        private BuildManager buildManager;

        // Passive gold timer
        private float passiveGoldTimer = 0f;
        private const float PASSIVE_GOLD_INTERVAL = 1f; // Give gold every 1 second

        // ==========================================
        // PROPERTIES (Public Read-Only)
        // ==========================================

        public float DamageMultiplier => damageMultiplier;
        public float FireRateMultiplier => fireRateMultiplier;
        public float RangeMultiplier => rangeMultiplier;
        public float TurretCostMultiplier => turretCostMultiplier;
        public int PassiveGoldPerSecond => passiveGoldPerSecond;

        public int ActiveCardCount => activeCards.Count;
        public List<CardData> ActiveCards => new List<CardData>(activeCards);

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
            playerGold = GetComponent<PlayerGold>();
            playerHealth = GetComponent<PlayerHealth>();
            buildManager = GetComponent<BuildManager>();
        }

        private void Update()
        {
            // Only update for local player
            if (!photonView.IsMine) return;

            // Passive gold generation
            if (passiveGoldPerSecond > 0)
            {
                passiveGoldTimer += Time.deltaTime;

                if (passiveGoldTimer >= PASSIVE_GOLD_INTERVAL)
                {
                    playerGold?.AddGold(passiveGoldPerSecond);
                    passiveGoldTimer = 0f;
                }
            }

            // Update active sabotages
            UpdateSabotages(Time.deltaTime);
        }

        // ==========================================
        // CARD ACTIVATION (Called by Draft System)
        // ==========================================

        /// <summary>
        /// Activates drafted card
        /// </summary>
        public void ActivateCard(CardData card)
        {
            if (card == null)
            {
                Debug.LogError("[PlayerCardManager] Cannot activate null card!");
                return;
            }

            if (card.cardEffect == null)
            {
                Debug.LogError($"[PlayerCardManager] Card '{card.cardName}' has no effect assigned!");
                return;
            }

            // Add to active cards
            activeCards.Add(card);

            // Execute effect
            card.cardEffect.Activate(photonView);

            // Recalculate modifiers
            RecalculateModifiers();

            Debug.Log($"[PlayerCardManager] Activated card: {card.cardName} ({card.activationType})");
        }

        /// <summary>
        /// Deactivates all cards (when game ends)
        /// </summary>
        public void DeactivateAllCards()
        {
            foreach (CardData card in activeCards)
            {
                if (card?.cardEffect != null)
                {
                    card.cardEffect.Deactivate(photonView);
                }
            }

            activeCards.Clear();
            RecalculateModifiers();

            Debug.Log("[PlayerCardManager] Deactivated all cards");
        }

        // ==========================================
        // MODIFIER AGGREGATION
        // ==========================================

        /// <summary>
        /// Recalculates all modifiers from active cards
        /// Called after each card activation
        /// </summary>
        private void RecalculateModifiers()
        {
            // Reset to base values
            damageMultiplier = 1f;
            fireRateMultiplier = 1f;
            rangeMultiplier = 1f;
            turretCostMultiplier = 1f;
            passiveGoldPerSecond = 0;

            // Aggregate from active cards
            foreach (CardData card in activeCards)
            {
                if (card?.cardEffect == null) continue;

                // Check if effect is a turret modifier
                if (card.cardEffect is TurretCardEffect turretMod)
                {
                    damageMultiplier *= turretMod.damageMultiplier;
                    fireRateMultiplier *= turretMod.fireRateMultiplier;
                    rangeMultiplier *= turretMod.rangeMultiplier;
                }

                // Check if effect is economy
                if (card.cardEffect is EconomyCardEffect economy)
                {
                    passiveGoldPerSecond += economy.goldPerSecond;

                    if (economy.turretCostDiscount > 0)
                    {
                        turretCostMultiplier *= (1f - economy.turretCostDiscount / 100f);
                    }
                }

                // TODO: Add other effect types (UtilityCardEffect, etc.)
            }

            Debug.Log($"[PlayerCardManager] Modifiers updated: DMG={damageMultiplier:F2}x, FR={fireRateMultiplier:F2}x, RNG={rangeMultiplier:F2}x, Cost={turretCostMultiplier:F2}x, Gold={passiveGoldPerSecond}/s");
        }

        /// <summary>
        /// Gets final turret damage after all modifiers
        /// </summary>
        public int GetModifiedTurretDamage(int baseDamage)
        {
            return Mathf.RoundToInt(baseDamage * damageMultiplier);
        }

        /// <summary>
        /// Gets final turret fire rate after all modifiers
        /// </summary>
        public float GetModifiedFireRate(float baseFireRate)
        {
            return baseFireRate * fireRateMultiplier;
        }

        /// <summary>
        /// Gets final turret range after all modifiers
        /// </summary>
        public float GetModifiedRange(float baseRange)
        {
            return baseRange * rangeMultiplier;
        }

        /// <summary>
        /// Gets final turret cost after all modifiers
        /// </summary>
        public int GetModifiedTurretCost(int baseCost)
        {
            return Mathf.RoundToInt(baseCost * turretCostMultiplier);
        }

        // ==========================================
        // SABOTAGE SYSTEM
        // ==========================================

        /// <summary>
        /// Applies sabotage card to this player
        /// Called via RPC from opponent
        /// </summary>
        public void ApplySabotage(SabotageCardData sabotage, PhotonView casterPhotonView)
        {
            if (sabotage == null || sabotage.sabotageEffect == null)
            {
                Debug.LogError("[PlayerCardManager] Invalid sabotage!");
                return;
            }

            // Create active sabotage instance
            ActiveSabotage activeSabotage = new ActiveSabotage
            {
                sabotageData = sabotage,
                casterPhotonView = casterPhotonView,
                remainingDuration = sabotage.duration,
                remainingRounds = sabotage.durationRounds
            };

            // Apply effect
            sabotage.sabotageEffect.Apply(photonView, casterPhotonView);

            // Add to active list (if not instant)
            if (sabotage.durationType != SabotageDurationType.Instant)
            {
                activeSabotages.Add(activeSabotage);
            }

            Debug.Log($"[PlayerCardManager] Sabotage applied: {sabotage.sabotageName} ({sabotage.durationType})");
        }

        /// <summary>
        /// Updates active sabotages (duration countdown)
        /// </summary>
        private void UpdateSabotages(float deltaTime)
        {
            for (int i = activeSabotages.Count - 1; i >= 0; i--)
            {
                ActiveSabotage sabotage = activeSabotages[i];

                // Skip permanent sabotages
                if (sabotage.sabotageData.durationType == SabotageDurationType.Permanent)
                    continue;

                // Countdown duration
                sabotage.remainingDuration -= deltaTime;

                // Call update on effect (for DOT sabotages, etc.)
                sabotage.sabotageData.sabotageEffect.OnUpdate(photonView, deltaTime);

                // Check if expired
                if (sabotage.remainingDuration <= 0f)
                {
                    // Remove effect
                    sabotage.sabotageData.sabotageEffect.Remove(photonView, sabotage.casterPhotonView);

                    activeSabotages.RemoveAt(i);

                    Debug.Log($"[PlayerCardManager] Sabotage expired: {sabotage.sabotageData.sabotageName}");
                }
            }
        }

        /// <summary>
        /// Reduces sabotage round duration (called by WaveManager after each wave)
        /// </summary>
        public void OnWaveCompleted()
        {
            for (int i = activeSabotages.Count - 1; i >= 0; i--)
            {
                ActiveSabotage sabotage = activeSabotages[i];

                if (sabotage.sabotageData.durationRounds > 0)
                {
                    sabotage.remainingRounds--;

                    if (sabotage.remainingRounds <= 0)
                    {
                        // Remove effect
                        sabotage.sabotageData.sabotageEffect.Remove(photonView, sabotage.casterPhotonView);

                        activeSabotages.RemoveAt(i);

                        Debug.Log($"[PlayerCardManager] Round-based sabotage expired: {sabotage.sabotageData.sabotageName}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets list of active sabotages (for UI display)
        /// </summary>
        public List<ActiveSabotage> GetActiveSabotages()
        {
            return new List<ActiveSabotage>(activeSabotages);
        }

        // ==========================================
        // UTILITY
        // ==========================================

        /// <summary>
        /// Checks if specific card is active
        /// </summary>
        public bool HasCard(CardData card)
        {
            return activeCards.Contains(card);
        }

        /// <summary>
        /// Gets count of specific card type
        /// </summary>
        public int GetCardCountByType(CardType cardType)
        {
            return activeCards.Count(card => card.cardType == cardType);
        }

        /// <summary>
        /// Clears all sabotages (e.g., special cleanse card)
        /// </summary>
        public void ClearAllSabotages()
        {
            foreach (var sabotage in activeSabotages)
            {
                sabotage.sabotageData.sabotageEffect.Remove(photonView, sabotage.casterPhotonView);
            }

            activeSabotages.Clear();

            Debug.Log("[PlayerCardManager] Cleared all sabotages");
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Print Active Cards")]
        private void PrintActiveCards()
        {
            Debug.Log($"=== ACTIVE CARDS ({activeCards.Count}) ===");
            foreach (var card in activeCards)
            {
                Debug.Log($"  - {card.cardName} ({card.cardType}, {card.activationType})");
            }
        }

        [ContextMenu("Print Active Sabotages")]
        private void PrintActiveSabotages()
        {
            Debug.Log($"=== ACTIVE SABOTAGES ({activeSabotages.Count}) ===");
            foreach (var sabotage in activeSabotages)
            {
                Debug.Log($"  - {sabotage.sabotageData.sabotageName} ({sabotage.remainingDuration:F1}s remaining)");
            }
        }
    }

    // ==========================================
    // HELPER CLASS - Active Sabotage Instance
    // ==========================================

    /// <summary>
    /// Runtime instance of active sabotage
    /// Tracks duration and caster
    /// </summary>
    [System.Serializable]
    public class ActiveSabotage
    {
        public SabotageCardData sabotageData;
        public PhotonView casterPhotonView;
        public float remainingDuration;
        public int remainingRounds;
    }
}