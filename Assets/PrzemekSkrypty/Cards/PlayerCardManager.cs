using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;
using ElementumDefense.Turrets;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Backwards-compatible facade over the three real card-system components:
    ///  - <see cref="PlayerCardActivator"/> — owns active card list
    ///  - <see cref="PlayerModifierStack"/> — aggregates modifiers, exposes getters
    ///  - <see cref="PlayerSabotageController"/> — orchestrates sabotage lifecycle
    /// 
    /// Existing callers (Turret, BuildManager, sabotage cards, DraftManager,
    /// WaveManager) talk to this facade unchanged. New code should prefer
    /// addressing the components directly — they're easier to test and reason about.
    /// 
    /// The three sibling components are auto-added via [RequireComponent].
    /// </summary>
    [RequireComponent(typeof(PlayerCardActivator))]
    [RequireComponent(typeof(PlayerModifierStack))]
    [RequireComponent(typeof(PlayerSabotageController))]
    [RequireComponent(typeof(PlayerWaveTriggerListener))]
    public class PlayerCardManager : MonoBehaviour
    {
        // Cached siblings
        private PlayerCardActivator activator;
        private PlayerModifierStack modifierStack;
        private PlayerSabotageController sabotageController;

        // ==========================================
        // EVENT FORWARD
        // ==========================================

        /// <summary>Forwards PlayerModifierStack.OnModifiersChanged so existing turret subscribers keep working.</summary>
        public System.Action OnModifiersChanged;

        private void Awake()
        {
            activator = GetComponent<PlayerCardActivator>();
            modifierStack = GetComponent<PlayerModifierStack>();
            sabotageController = GetComponent<PlayerSabotageController>();

            // Forward the underlying event so old subscribers (Turret) don't change.
            modifierStack.OnModifiersChanged += () => OnModifiersChanged?.Invoke();
        }

        // ==========================================
        // CARD ACTIVATION (forwards to activator)
        // ==========================================

        public void ActivateCard(CardData card) => activator.ActivateCard(card);
        public void DeactivateAllCards() => activator.DeactivateAllCards();
        public bool HasCard(CardData card) => activator.HasCard(card);
        public int GetCardCountByType(CardType cardType) => activator.GetCardCountByType(cardType);

        public int ActiveCardCount => activator.ActiveCardCount;
        public List<CardData> ActiveCards => new List<CardData>(activator.ActiveCards);

        // ==========================================
        // MODIFIER QUERIES (forwards to modifier stack)
        // ==========================================

        public float DamageMultiplier => modifierStack.DamageMultiplier;
        public float FireRateMultiplier => modifierStack.FireRateMultiplier;
        public float RangeMultiplier => modifierStack.RangeMultiplier;
        public float TurretCostMultiplier => modifierStack.TurretCostMultiplier;
        public int PassiveGoldPerSecond => modifierStack.PassiveGoldPerSecond;
        public bool AreUpgradesDisabled => modifierStack.AreUpgradesDisabled;

        public float GetModifiedDamage(float baseDamage, ElementumDefense.Elements.ElementType element)
            => modifierStack.GetModifiedDamage(baseDamage, element);

        public float GetModifiedFireRate(float baseFireRate, ElementumDefense.Elements.ElementType element)
            => modifierStack.GetModifiedFireRate(baseFireRate, element);

        public float GetModifiedRange(float baseRange, ElementumDefense.Elements.ElementType element)
            => modifierStack.GetModifiedRange(baseRange, element);

        public int GetModifiedTurretCost(int baseCost) => modifierStack.GetModifiedTurretCost(baseCost);

        public float GetAdditionalAOE(ElementumDefense.Elements.ElementType element)
            => modifierStack.GetAdditionalAOE(element);

        public int GetAdditionalPierce(ElementumDefense.Elements.ElementType element)
            => modifierStack.GetAdditionalPierce(element);

        public int GetAdditionalChainTargets(ElementumDefense.Elements.ElementType element)
            => modifierStack.GetAdditionalChainTargets(element);

        // Backward-compat shortcuts (no element argument).
        public int GetModifiedTurretDamage(int baseDamage)
            => Mathf.RoundToInt(baseDamage * modifierStack.DamageMultiplier);

        public float GetModifiedFireRate(float baseFireRate)
            => baseFireRate * modifierStack.FireRateMultiplier;

        public float GetModifiedRange(float baseRange)
            => baseRange * modifierStack.RangeMultiplier;

        // ==========================================
        // SABOTAGE (forwards to sabotage controller)
        // ==========================================

        public void ApplySabotage(SabotageCardData sabotage, PhotonView casterPhotonView)
            => sabotageController.ApplySabotage(sabotage, casterPhotonView);

        public void OnWaveCompleted() => sabotageController.OnWaveCompleted();

        public void ClearAllSabotages() => sabotageController.ClearAllSabotages();

        public List<ActiveSabotage> GetActiveSabotages() => sabotageController.GetActiveSabotages();

        /// <summary>Reconnect restore — re-apply a sabotage with remaining duration/rounds.</summary>
        public void RestoreSabotage(SabotageCardData sabotage, PhotonView caster,
                                    float remainingDuration, int remainingRounds)
            => sabotageController.RestoreSabotage(sabotage, caster, remainingDuration, remainingRounds);

        // Sabotage modifier mutators — these are old method names sabotage cards
        // already call. Forward to the stack; logging is preserved on stack side.
        public void SetUpgradesDisabled(bool disabled) => modifierStack.SetUpgradesDisabled(disabled);

        public void ApplySabotageDamageModifier(float multiplier) => modifierStack.ApplySabotageDamage(multiplier);
        public void RemoveSabotageDamageModifier(float multiplier) => modifierStack.RemoveSabotageDamage(multiplier);

        public void ApplySabotageFireRateModifier(float multiplier) => modifierStack.ApplySabotageFireRate(multiplier);
        public void RemoveSabotageFireRateModifier(float multiplier) => modifierStack.RemoveSabotageFireRate(multiplier);

        public void ApplySabotageRangeModifier(float multiplier) => modifierStack.ApplySabotageRange(multiplier);
        public void RemoveSabotageRangeModifier(float multiplier) => modifierStack.RemoveSabotageRange(multiplier);

        public void ApplySabotageCostModifier(float multiplier) => modifierStack.ApplySabotageCost(multiplier);
        public void RemoveSabotageCostModifier(float multiplier) => modifierStack.RemoveSabotageCost(multiplier);

        // ==========================================
        // SABOTAGE — ID-BASED API (preferred for new sabotage cards)
        //
        // Use these when authoring a new SabotageEffect: in Apply, store an
        // ID (e.g. sabotage.sabotageId + caster.ViewID), pass it to ApplyById;
        // in Remove, call RemoveById with the same ID. No divide-back math,
        // no floating-point drift, and the UI can read GetActiveModifiersByStat
        // to render "this sabotage is currently giving -50% dmg".
        // ==========================================

        public void ApplySabotageById(string id, PlayerModifierStack.SabotageStat stat, float multiplier)
            => modifierStack.ApplyById(id, stat, multiplier);

        public bool RemoveSabotageById(string id, PlayerModifierStack.SabotageStat stat)
            => modifierStack.RemoveById(id, stat);

        public IReadOnlyList<PlayerModifierStack.SabotageMod> GetAllActiveSabotageMods()
            => modifierStack.GetAllActiveSabotageMods();

        /// <summary>
        /// Forwarder for ForcePickSabotage — sets the next mid-game draft to
        /// have a reduced choice count. Resets after that draft consumes it.
        /// </summary>
        public void SetNextDraftChoiceCount(int count)
        {
            var draftMgr = GetComponent<DraftManager>();
            draftMgr?.SetNextDraftChoiceOverride(count);
        }

        /// <summary>
        /// Forwarder for NoMulliganSelfSabotage — disables mulligan in the
        /// next mid-game draft. Resets after that draft starts.
        /// </summary>
        public void SetNextDraftMulliganDisabled(bool disabled)
        {
            var draftMgr = GetComponent<DraftManager>();
            draftMgr?.SetNextDraftMulliganDisabled(disabled);
        }
    }

    // ==========================================
    // SHARED DATA TYPES
    // (kept here for backward compatibility — sabotage code uses
    //  ElementumDefense.Cards.ActiveSabotage / TurretModifiers without an
    //  explicit using directive)
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
