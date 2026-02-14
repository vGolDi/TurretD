using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    public class PlayerCardManager : MonoBehaviour
    {
        [Header("Active Cards")]
        [SerializeField] private List<CardData> activeCards = new List<CardData>();

        [Header("Card Modifiers (Read-Only)")]
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float fireRateMultiplier = 1f;
        [SerializeField] private float rangeMultiplier = 1f;
        [SerializeField] private float turretCostMultiplier = 1f;
        [SerializeField] private int passiveGoldPerSecond = 0;

        [Header("Sabotage Modifiers (Read-Only)")]
        [SerializeField] private float sabotageDamageModifier = 1f;
        [SerializeField] private float sabotageFireRateModifier = 1f;
        [SerializeField] private float sabotageRangeModifier = 1f;
        [SerializeField] private float sabotageCostModifier = 1f;
        [SerializeField] private bool upgradesDisabled = false;

        [Header("Active Sabotages")]
        [SerializeField] private List<ActiveSabotage> activeSabotages = new List<ActiveSabotage>();

        // Per-element modifiers
        private Dictionary<ElementumDefense.Elements.ElementType, TurretModifiers> elementModifiers
            = new Dictionary<ElementumDefense.Elements.ElementType, TurretModifiers>();

        // References
        private PhotonView photonView;
        private PlayerGold playerGold;
        private PlayerHealth playerHealth;
        private BuildManager buildManager;

        // Passive gold timer
        private float passiveGoldTimer = 0f;
        private const float PASSIVE_GOLD_INTERVAL = 1f;

        // Event for turrets to listen to
        public System.Action OnModifiersChanged;

        // ==========================================
        // PROPERTIES
        // ==========================================

        public float DamageMultiplier => damageMultiplier;
        public float FireRateMultiplier => fireRateMultiplier;
        public float RangeMultiplier => rangeMultiplier;
        public float TurretCostMultiplier => turretCostMultiplier;
        public int PassiveGoldPerSecond => passiveGoldPerSecond;
        public int ActiveCardCount => activeCards.Count;
        public List<CardData> ActiveCards => new List<CardData>(activeCards);
        public bool AreUpgradesDisabled => upgradesDisabled;

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
        // CARD ACTIVATION
        // ==========================================

        public void ActivateCard(CardData card)
        {
            if (card == null)
            {
                Debug.LogError("[PlayerCardManager] Cannot activate null card!");
                return;
            }

            if (card.cardEffect == null)
            {
                Debug.LogError($"[PlayerCardManager] Card '{card.cardName}' has no effect!");
                return;
            }

            activeCards.Add(card);
            card.cardEffect.Activate(photonView);

            RecalculateModifiers();
            OnModifiersChanged?.Invoke();

            Debug.Log($"[PlayerCardManager] ✅ Activated: {card.cardName}");
        }

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
            OnModifiersChanged?.Invoke();

            Debug.Log("[PlayerCardManager] Deactivated all cards");
        }

        // ==========================================
        // MODIFIER AGGREGATION
        // ==========================================

        private void RecalculateModifiers()
        {
            // Reset to base values
            damageMultiplier = 1f;
            fireRateMultiplier = 1f;
            rangeMultiplier = 1f;
            turretCostMultiplier = 1f;
            passiveGoldPerSecond = 0;

            // Clear per-element modifiers
            elementModifiers.Clear();

            foreach (CardData card in activeCards)
            {
                if (card?.cardEffect == null) continue;

                // Turret modifiers
                if (card.cardEffect is TurretCardEffect turretMod)
                {
                    if (turretMod.affectsAllTurrets)
                    {
                        damageMultiplier *= turretMod.damageMultiplier;
                        fireRateMultiplier *= turretMod.fireRateMultiplier;
                        rangeMultiplier *= turretMod.rangeMultiplier;
                    }
                    else
                    {
                        var elementType = turretMod.targetElement;

                        if (!elementModifiers.ContainsKey(elementType))
                        {
                            elementModifiers[elementType] = new TurretModifiers();
                        }

                        elementModifiers[elementType].damageMultiplier *= turretMod.damageMultiplier;
                        elementModifiers[elementType].fireRateMultiplier *= turretMod.fireRateMultiplier;
                        elementModifiers[elementType].rangeMultiplier *= turretMod.rangeMultiplier;
                        elementModifiers[elementType].addAOERadius += turretMod.addAOERadius;
                        elementModifiers[elementType].addPierceCount += turretMod.addPierceCount;
                        elementModifiers[elementType].addChainTargets += turretMod.addChainTargets;
                    }
                }

                // Economy modifiers
                if (card.cardEffect is EconomyCardEffect economy)
                {
                    passiveGoldPerSecond += economy.goldPerSecond;

                    if (economy.turretCostDiscount > 0)
                    {
                        turretCostMultiplier *= (1f - economy.turretCostDiscount / 100f);
                    }
                }
            }

            Debug.Log($"[PlayerCardManager] Card Modifiers: " +
                      $"DMG={damageMultiplier:F2}x, FR={fireRateMultiplier:F2}x, " +
                      $"RNG={rangeMultiplier:F2}x, Cost={turretCostMultiplier:F2}x, " +
                      $"Gold={passiveGoldPerSecond}/s");

            if (sabotageDamageModifier != 1f || sabotageFireRateModifier != 1f ||
                sabotageRangeModifier != 1f || sabotageCostModifier != 1f)
            {
                Debug.Log($"[PlayerCardManager] Sabotage Modifiers: " +
                          $"DMG={sabotageDamageModifier:F2}x, " +
                          $"FR={sabotageFireRateModifier:F2}x, " +
                          $"RNG={sabotageRangeModifier:F2}x, " +
                          $"Cost={sabotageCostModifier:F2}x, " +
                          $"UpgradesDisabled={upgradesDisabled}");
            }

            foreach (var kvp in elementModifiers)
            {
                Debug.Log($"[PlayerCardManager] {kvp.Key} mods: " +
                          $"DMG={kvp.Value.damageMultiplier:F2}x, " +
                          $"FR={kvp.Value.fireRateMultiplier:F2}x, " +
                          $"RNG={kvp.Value.rangeMultiplier:F2}x");
            }
        }

        // ==========================================
        // MODIFIER QUERY (card + sabotage combined)
        // ==========================================

        /// <summary>
        /// Gets FINAL damage: base * cardGlobal * cardElement * sabotage
        /// </summary>
        public float GetModifiedDamage(float baseDamage,
            ElementumDefense.Elements.ElementType element)
        {
            float globalMod = damageMultiplier;
            float elementMod = 1f;

            if (elementModifiers.TryGetValue(element, out TurretModifiers mods))
            {
                elementMod = mods.damageMultiplier;
            }

            return baseDamage * globalMod * elementMod * sabotageDamageModifier;
        }

        /// <summary>
        /// Gets FINAL fire rate: base * cardGlobal * cardElement * sabotage
        /// </summary>
        public float GetModifiedFireRate(float baseFireRate,
            ElementumDefense.Elements.ElementType element)
        {
            float globalMod = fireRateMultiplier;
            float elementMod = 1f;

            if (elementModifiers.TryGetValue(element, out TurretModifiers mods))
            {
                elementMod = mods.fireRateMultiplier;
            }

            return baseFireRate * globalMod * elementMod * sabotageFireRateModifier;
        }

        /// <summary>
        /// Gets FINAL range: base * cardGlobal * cardElement * sabotage
        /// </summary>
        public float GetModifiedRange(float baseRange,
            ElementumDefense.Elements.ElementType element)
        {
            float globalMod = rangeMultiplier;
            float elementMod = 1f;

            if (elementModifiers.TryGetValue(element, out TurretModifiers mods))
            {
                elementMod = mods.rangeMultiplier;
            }

            return baseRange * globalMod * elementMod * sabotageRangeModifier;
        }

        /// <summary>
        /// Gets FINAL turret cost: base * cardDiscount * sabotageInflation
        /// </summary>
        public int GetModifiedTurretCost(int baseCost)
        {
            return Mathf.RoundToInt(baseCost * turretCostMultiplier * sabotageCostModifier);
        }

        /// <summary>
        /// Gets additional AOE radius for element
        /// </summary>
        public float GetAdditionalAOE(ElementumDefense.Elements.ElementType element)
        {
            if (elementModifiers.TryGetValue(element, out TurretModifiers mods))
            {
                return mods.addAOERadius;
            }
            return 0f;
        }

        /// <summary>
        /// Gets additional pierce count for element
        /// </summary>
        public int GetAdditionalPierce(ElementumDefense.Elements.ElementType element)
        {
            if (elementModifiers.TryGetValue(element, out TurretModifiers mods))
            {
                return mods.addPierceCount;
            }
            return 0;
        }

        /// <summary>
        /// Gets additional chain targets for element
        /// </summary>
        public int GetAdditionalChainTargets(ElementumDefense.Elements.ElementType element)
        {
            if (elementModifiers.TryGetValue(element, out TurretModifiers mods))
            {
                return mods.addChainTargets;
            }
            return 0;
        }

        // ========== Backward compatibility ==========

        public int GetModifiedTurretDamage(int baseDamage)
        {
            return Mathf.RoundToInt(baseDamage * damageMultiplier * sabotageDamageModifier);
        }

        public float GetModifiedFireRate(float baseFireRate)
        {
            return baseFireRate * fireRateMultiplier * sabotageFireRateModifier;
        }

        public float GetModifiedRange(float baseRange)
        {
            return baseRange * rangeMultiplier * sabotageRangeModifier;
        }

        // ==========================================
        // SABOTAGE SYSTEM
        // ==========================================

        /// <summary>
        /// Applies sabotage card to this player.
        /// Called by SabotageDraftManager after reveal phase.
        /// </summary>
        public void ApplySabotage(SabotageCardData sabotage, PhotonView casterPhotonView)
        {
            if (sabotage == null)
            {
                Debug.LogError("[PlayerCardManager] Sabotage is null!");
                return;
            }

            if (sabotage.sabotageEffect == null)
            {
                Debug.LogError($"[PlayerCardManager] Sabotage '{sabotage.sabotageName}' " +
                               $"has NO EFFECT assigned!");
                return;
            }

            // Apply the effect
            sabotage.sabotageEffect.Apply(photonView, casterPhotonView);

            // Track if not instant
            if (sabotage.durationType != SabotageDurationType.Instant)
            {
                ActiveSabotage activeSabotage = new ActiveSabotage
                {
                    sabotageData = sabotage,
                    casterPhotonView = casterPhotonView,
                    remainingDuration = sabotage.duration,
                    remainingRounds = sabotage.durationRounds
                };

                activeSabotages.Add(activeSabotage);
            }

            string casterName = casterPhotonView?.Owner?.NickName ?? "Unknown";
            Debug.Log($"[PlayerCardManager] ✅ Sabotage applied: " +
                      $"'{sabotage.sabotageName}' from {casterName} " +
                      $"({sabotage.durationType}, " +
                      $"{sabotage.GetDurationText()})");
        }

        /// <summary>
        /// Updates active sabotages - countdown and DOT effects
        /// </summary>
        private void UpdateSabotages(float deltaTime)
        {
            for (int i = activeSabotages.Count - 1; i >= 0; i--)
            {
                ActiveSabotage sabotage = activeSabotages[i];

                // Skip permanent sabotages
                if (sabotage.sabotageData.durationType == SabotageDurationType.Permanent)
                    continue;

                // Countdown
                sabotage.remainingDuration -= deltaTime;

                // Update effect (for DOT sabotages like GoldDrain)
                if (sabotage.sabotageData.sabotageEffect != null)
                {
                    sabotage.sabotageData.sabotageEffect.OnUpdate(photonView, deltaTime);
                }

                // Check if expired
                if (sabotage.remainingDuration <= 0f)
                {
                    // Remove effect
                    if (sabotage.sabotageData.sabotageEffect != null)
                    {
                        sabotage.sabotageData.sabotageEffect.Remove(
                            photonView, sabotage.casterPhotonView);
                    }

                    string name = sabotage.sabotageData.sabotageName;
                    activeSabotages.RemoveAt(i);

                    Debug.Log($"[PlayerCardManager] ⏰ Sabotage expired: {name}");
                }
            }
        }

        /// <summary>
        /// Called by WaveManager after each wave.
        /// Reduces round-based sabotage durations.
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
                        if (sabotage.sabotageData.sabotageEffect != null)
                        {
                            sabotage.sabotageData.sabotageEffect.Remove(
                                photonView, sabotage.casterPhotonView);
                        }

                        string name = sabotage.sabotageData.sabotageName;
                        activeSabotages.RemoveAt(i);

                        Debug.Log($"[PlayerCardManager] ⏰ Round sabotage expired: {name}");
                    }
                    else
                    {
                        Debug.Log($"[PlayerCardManager] Sabotage " +
                                  $"'{sabotage.sabotageData.sabotageName}' " +
                                  $"has {sabotage.remainingRounds} rounds left");
                    }
                }
            }
        }

        public List<ActiveSabotage> GetActiveSabotages()
        {
            return new List<ActiveSabotage>(activeSabotages);
        }

        // ==========================================
        // SABOTAGE MODIFIER METHODS
        // ==========================================

        // --- Upgrades Disabled ---

        public void SetUpgradesDisabled(bool disabled)
        {
            upgradesDisabled = disabled;
            Debug.Log($"[PlayerCardManager] Upgrades " +
                      $"{(disabled ? "🚫 DISABLED" : "✅ ENABLED")}");
        }

        // --- Damage ---

        public void ApplySabotageDamageModifier(float multiplier)
        {
            sabotageDamageModifier *= multiplier;
            Debug.Log($"[PlayerCardManager] Sabotage DMG: {sabotageDamageModifier:F2}x");
            OnModifiersChanged?.Invoke();
        }

        public void RemoveSabotageDamageModifier(float multiplier)
        {
            if (multiplier > 0f)
                sabotageDamageModifier /= multiplier;
            else
                sabotageDamageModifier = 1f;

            // Safety clamp - prevent floating point drift
            if (Mathf.Abs(sabotageDamageModifier - 1f) < 0.001f)
                sabotageDamageModifier = 1f;

            Debug.Log($"[PlayerCardManager] Sabotage DMG restored: " +
                      $"{sabotageDamageModifier:F2}x");
            OnModifiersChanged?.Invoke();
        }

        // --- Fire Rate ---

        public void ApplySabotageFireRateModifier(float multiplier)
        {
            sabotageFireRateModifier *= multiplier;
            Debug.Log($"[PlayerCardManager] Sabotage FR: {sabotageFireRateModifier:F2}x");
            OnModifiersChanged?.Invoke();
        }

        public void RemoveSabotageFireRateModifier(float multiplier)
        {
            if (multiplier > 0f)
                sabotageFireRateModifier /= multiplier;
            else
                sabotageFireRateModifier = 1f;

            if (Mathf.Abs(sabotageFireRateModifier - 1f) < 0.001f)
                sabotageFireRateModifier = 1f;

            Debug.Log($"[PlayerCardManager] Sabotage FR restored: " +
                      $"{sabotageFireRateModifier:F2}x");
            OnModifiersChanged?.Invoke();
        }

        // --- Range ---

        public void ApplySabotageRangeModifier(float multiplier)
        {
            sabotageRangeModifier *= multiplier;
            Debug.Log($"[PlayerCardManager] Sabotage RNG: {sabotageRangeModifier:F2}x");
            OnModifiersChanged?.Invoke();
        }

        public void RemoveSabotageRangeModifier(float multiplier)
        {
            if (multiplier > 0f)
                sabotageRangeModifier /= multiplier;
            else
                sabotageRangeModifier = 1f;

            if (Mathf.Abs(sabotageRangeModifier - 1f) < 0.001f)
                sabotageRangeModifier = 1f;

            Debug.Log($"[PlayerCardManager] Sabotage RNG restored: " +
                      $"{sabotageRangeModifier:F2}x");
            OnModifiersChanged?.Invoke();
        }

        // --- Cost ---

        public void ApplySabotageCostModifier(float multiplier)
        {
            sabotageCostModifier *= multiplier;
            Debug.Log($"[PlayerCardManager] Sabotage COST: {sabotageCostModifier:F2}x");
        }

        public void RemoveSabotageCostModifier(float multiplier)
        {
            if (multiplier > 0f)
                sabotageCostModifier /= multiplier;
            else
                sabotageCostModifier = 1f;

            if (Mathf.Abs(sabotageCostModifier - 1f) < 0.001f)
                sabotageCostModifier = 1f;

            Debug.Log($"[PlayerCardManager] Sabotage COST restored: " +
                      $"{sabotageCostModifier:F2}x");
        }

        // ==========================================
        // UTILITY
        // ==========================================

        public bool HasCard(CardData card) => activeCards.Contains(card);

        public int GetCardCountByType(CardType cardType)
        {
            return activeCards.Count(card => card.cardType == cardType);
        }

        /// <summary>
        /// Clears ALL sabotages and resets all sabotage modifiers.
        /// Can be used by a "Cleanse" card or end-of-game cleanup.
        /// </summary>
        public void ClearAllSabotages()
        {
            foreach (var sabotage in activeSabotages)
            {
                if (sabotage.sabotageData?.sabotageEffect != null)
                {
                    sabotage.sabotageData.sabotageEffect.Remove(
                        photonView, sabotage.casterPhotonView);
                }
            }

            activeSabotages.Clear();

            // Reset ALL sabotage modifiers
            sabotageDamageModifier = 1f;
            sabotageFireRateModifier = 1f;
            sabotageRangeModifier = 1f;
            sabotageCostModifier = 1f;
            upgradesDisabled = false;

            OnModifiersChanged?.Invoke();

            Debug.Log("[PlayerCardManager] 🧹 Cleared all sabotages + modifiers");
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
                Debug.Log($"  - {card.cardName} ({card.cardType})");
            }
        }

        [ContextMenu("Print All Modifiers")]
        private void PrintModifiers()
        {
            Debug.Log($"=== CARD MODIFIERS ===");
            Debug.Log($"  DMG:  {damageMultiplier:F2}x");
            Debug.Log($"  FR:   {fireRateMultiplier:F2}x");
            Debug.Log($"  RNG:  {rangeMultiplier:F2}x");
            Debug.Log($"  Cost: {turretCostMultiplier:F2}x");
            Debug.Log($"  Gold: {passiveGoldPerSecond}/s");

            Debug.Log($"=== SABOTAGE MODIFIERS ===");
            Debug.Log($"  DMG:  {sabotageDamageModifier:F2}x");
            Debug.Log($"  FR:   {sabotageFireRateModifier:F2}x");
            Debug.Log($"  RNG:  {sabotageRangeModifier:F2}x");
            Debug.Log($"  Cost: {sabotageCostModifier:F2}x");
            Debug.Log($"  Upgrades Disabled: {upgradesDisabled}");

            Debug.Log($"=== FINAL VALUES (example base=10) ===");
            Debug.Log($"  DMG:  10 → {10 * damageMultiplier * sabotageDamageModifier:F1}");
            Debug.Log($"  FR:   1 → {1 * fireRateMultiplier * sabotageFireRateModifier:F2}");
            Debug.Log($"  RNG:  5 → {5 * rangeMultiplier * sabotageRangeModifier:F1}");
            Debug.Log($"  Cost: 100 → {Mathf.RoundToInt(100 * turretCostMultiplier * sabotageCostModifier)}");

            Debug.Log($"=== ELEMENT MODIFIERS ===");
            foreach (var kvp in elementModifiers)
            {
                Debug.Log($"  {kvp.Key}: DMG={kvp.Value.damageMultiplier:F2}x, " +
                          $"FR={kvp.Value.fireRateMultiplier:F2}x, " +
                          $"RNG={kvp.Value.rangeMultiplier:F2}x, " +
                          $"AOE=+{kvp.Value.addAOERadius}, " +
                          $"Pierce=+{kvp.Value.addPierceCount}, " +
                          $"Chain=+{kvp.Value.addChainTargets}");
            }
        }

        [ContextMenu("Print Active Sabotages")]
        private void PrintActiveSabotages()
        {
            Debug.Log($"=== ACTIVE SABOTAGES ({activeSabotages.Count}) ===");

            if (activeSabotages.Count == 0)
            {
                Debug.Log("  (none)");
                return;
            }

            foreach (var sabotage in activeSabotages)
            {
                string caster = sabotage.casterPhotonView?.Owner?.NickName ?? "Unknown";
                string duration;

                if (sabotage.sabotageData.durationType == SabotageDurationType.Permanent)
                {
                    duration = "PERMANENT";
                }
                else if (sabotage.sabotageData.durationRounds > 0)
                {
                    duration = $"{sabotage.remainingRounds} rounds left";
                }
                else
                {
                    duration = $"{sabotage.remainingDuration:F1}s left";
                }

                string hasEffect = sabotage.sabotageData.sabotageEffect != null
                    ? "✅" : "❌ NO EFFECT";

                Debug.Log($"  - {sabotage.sabotageData.sabotageName} " +
                          $"from {caster} ({duration}) {hasEffect}");
            }
        }

        [ContextMenu("Force Clear All Sabotages")]
        private void ForceClearSabotages()
        {
            ClearAllSabotages();
        }
    }

    // ==========================================
    // HELPER CLASSES
    // ==========================================

    [System.Serializable]
    public class TurretModifiers
    {
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float rangeMultiplier = 1f;
        public float addAOERadius = 0f;
        public int addPierceCount = 0;
        public int addChainTargets = 0;
    }

    [System.Serializable]
    public class ActiveSabotage
    {
        public SabotageCardData sabotageData;
        public PhotonView casterPhotonView;
        public float remainingDuration;
        public int remainingRounds;
    }
}