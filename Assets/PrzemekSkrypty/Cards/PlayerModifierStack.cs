using System;
using System.Collections.Generic;
using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.Players;
using ElementumDefense.Turrets;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Aggregates all turret/economy modifiers from active cards AND sabotage
    /// effects into one query surface. Other systems (Turret, BuildManager)
    /// read final values via <c>GetModifiedDamage(...)</c> etc.
    /// 
    /// Design points:
    ///  - Card modifiers are recomputed from scratch on every change
    ///    (idempotent — no floating-point drift across long games).
    ///  - <b>Sabotage modifiers are tracked as a list of named entries</b>
    ///    (one per active sabotage). Final value = product of all active
    ///    multipliers. Apply pushes; Remove pops by ID. Zero divide-back math,
    ///    zero drift across thousands of apply/remove cycles.
    ///  - <see cref="OnModifiersChanged"/> fires when any modifier changes so
    ///    Turrets can recalculate their stats once instead of polling.
    /// </summary>
    public class PlayerModifierStack : MonoBehaviour
    {
        // ==========================================
        // CARD MODIFIERS
        // ==========================================

        [Header("Card Modifiers (Read-Only)")]
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float fireRateMultiplier = 1f;
        [SerializeField] private float rangeMultiplier = 1f;
        [SerializeField] private float turretCostMultiplier = 1f;
        [SerializeField] private int passiveGoldPerSecond = 0;

        // Combat extension — driven by CombatModifierEffect cards.
        [SerializeField, Range(0f, 1f), Tooltip("Per-shot crit chance (0..1)")]
        private float critChance = 0f;
        [SerializeField, Tooltip("Crit damage multiplier (e.g. 2 = ×2 dmg on crit)")]
        private float critMultiplier = 2f;
        [SerializeField, Tooltip("Bonus gold awarded for every enemy kill")]
        private int bonusGoldPerKill = 0;
        [SerializeField, Range(0f, 5f), Tooltip("Extra gold multiplier for boss kills (1 = no change, 1.5 = +50%)")]
        private float bossKillGoldMultiplier = 1f;

        // Conditional modifiers — applied at query time when the predicate is satisfied.
        // Each entry is "context X gives +N% damage". Stored as additive percent
        // (so two cards giving +30% each = +60%, not 1.3 × 1.3).
        [Serializable]
        public class ConditionalDamageMod
        {
            public string id;
            public ConditionalContext context;
            public float bonusPercent;       // e.g. 0.3 = +30%
            public float thresholdValue;     // for LowHP: hp percent threshold; WaveStart: seconds; etc.
        }

        public enum ConditionalContext
        {
            VsBoss,             // target is boss
            VsNormal,           // target is NOT boss (used for bossSlayer's downside)
            LowPlayerHp,        // player HP below threshold
            WaveOpening,        // first N seconds of a wave
            WaveClosing,        // last N seconds of a wave (matches WaveRush downside)
            UnderdogGold        // local player has less gold than opponent
        }

        [Header("Conditional Modifiers (Read-Only)")]
        [SerializeField] private List<ConditionalDamageMod> conditionalDmgMods = new List<ConditionalDamageMod>();

        // Per-element modifiers — keyed by element type.
        private readonly Dictionary<ElementType, TurretModifiers> elementModifiers
            = new Dictionary<ElementType, TurretModifiers>();

        // ==========================================
        // SABOTAGE MODIFIERS (ID-based stack)
        // ==========================================

        public enum SabotageStat { Damage, FireRate, Range, Cost, PassiveGold }

        [Serializable]
        public class SabotageMod
        {
            public string id;
            public SabotageStat stat;
            public float multiplier;

            public SabotageMod(string id, SabotageStat stat, float multiplier)
            {
                this.id = id;
                this.stat = stat;
                this.multiplier = multiplier;
            }
        }

        // Inspector view of all currently-active sabotage entries (debugging).
        [Header("Sabotage Modifiers (Read-Only)")]
        [SerializeField] private List<SabotageMod> activeSabotageMods = new List<SabotageMod>();

        [SerializeField] private bool upgradesDisabled = false;

        // Counter to make auto-IDs unique when callers don't pass one.
        private int autoIdCounter = 0;

        // ==========================================
        // EVENTS
        // ==========================================

        /// <summary>Fires whenever ANY modifier (card or sabotage) changes.</summary>
        public Action OnModifiersChanged;

        // ==========================================
        // PROPERTIES
        // ==========================================

        public float DamageMultiplier => damageMultiplier;
        public float FireRateMultiplier => fireRateMultiplier;
        public float RangeMultiplier => rangeMultiplier;
        public float TurretCostMultiplier => turretCostMultiplier;
        public int PassiveGoldPerSecond => passiveGoldPerSecond;

        // Combat ext.
        public float CritChance => critChance;
        public float CritMultiplier => critMultiplier;
        public int BonusGoldPerKill => bonusGoldPerKill;
        public float BossKillGoldMultiplier => bossKillGoldMultiplier;

        public IReadOnlyList<ConditionalDamageMod> ConditionalDamageMods => conditionalDmgMods;

        /// <summary>
        /// Effective passive gold per tick — base flat value scaled by any
        /// active passive-gold multipliers (e.g. Frugal sabotage cuts it,
        /// economy buffs could multiply it). Round to int at the use site.
        /// </summary>
        public float EffectivePassiveGoldPerSecond => passiveGoldPerSecond * PassiveGoldProduct;

        public bool AreUpgradesDisabled => upgradesDisabled;

        // Final sabotage multipliers (product of all active entries on each stat).
        public float SabotageDamageProduct => ProductOf(SabotageStat.Damage);
        public float SabotageFireRateProduct => ProductOf(SabotageStat.FireRate);
        public float SabotageRangeProduct => ProductOf(SabotageStat.Range);
        public float SabotageCostProduct => ProductOf(SabotageStat.Cost);
        public float PassiveGoldProduct => ProductOf(SabotageStat.PassiveGold);

        // ==========================================
        // CARD-SIDE RECALCULATION
        // ==========================================

        /// <summary>
        /// Rebuilds card-driven modifiers from the supplied active card list.
        /// Call this every time a card is activated/deactivated.
        /// Sabotage modifiers are NOT touched here — they live independently.
        /// </summary>
        public void RecalculateFromCards(IReadOnlyList<CardData> activeCards)
        {
            damageMultiplier = 1f;
            fireRateMultiplier = 1f;
            rangeMultiplier = 1f;
            turretCostMultiplier = 1f;
            passiveGoldPerSecond = 0;
            critChance = 0f;
            critMultiplier = 2f;
            bonusGoldPerKill = 0;
            bossKillGoldMultiplier = 1f;
            conditionalDmgMods.Clear();
            elementModifiers.Clear();

            // Pre-pass — count cards by element and type for synergy effects.
            var elementCounts = new Dictionary<ElementType, int>();
            int economyCount = 0;
            int uniqueElements = 0;
            for (int i = 0; i < activeCards.Count; i++)
            {
                var c = activeCards[i];
                if (c == null) continue;
                if (c.cardType == CardType.Economy) economyCount++;
                if (c.associatedElement != ElementType.None)
                {
                    if (!elementCounts.ContainsKey(c.associatedElement))
                        elementCounts[c.associatedElement] = 0;
                    elementCounts[c.associatedElement]++;
                }
            }
            foreach (var kv in elementCounts) if (kv.Value > 0) uniqueElements++;

            for (int i = 0; i < activeCards.Count; i++)
            {
                CardData card = activeCards[i];
                if (card?.cardEffect == null) continue;

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
                        var element = turretMod.targetElement;
                        if (!elementModifiers.ContainsKey(element))
                            elementModifiers[element] = new TurretModifiers();

                        var mods = elementModifiers[element];
                        mods.damageMultiplier *= turretMod.damageMultiplier;
                        mods.fireRateMultiplier *= turretMod.fireRateMultiplier;
                        mods.rangeMultiplier *= turretMod.rangeMultiplier;
                        mods.addAOERadius += turretMod.addAOERadius;
                        mods.addPierceCount += turretMod.addPierceCount;
                        mods.addChainTargets += turretMod.addChainTargets;
                    }

                    // Element Avatar tradeoff: penalize all OTHER element families.
                    if (turretMod.otherElementsPenaltyPercent > 0f &&
                        turretMod.targetElement != ElementType.None &&
                        !turretMod.affectsAllTurrets)
                    {
                        float penaltyMul = 1f - turretMod.otherElementsPenaltyPercent / 100f;
                        foreach (var elm in s_AllElements)
                        {
                            if (elm == turretMod.targetElement) continue;
                            if (!elementModifiers.ContainsKey(elm))
                                elementModifiers[elm] = new TurretModifiers();
                            elementModifiers[elm].damageMultiplier *= penaltyMul;
                        }
                    }
                }

                if (card.cardEffect is EconomyCardEffect economy)
                {
                    passiveGoldPerSecond += economy.goldPerSecond;
                    if (economy.turretCostDiscount > 0)
                        turretCostMultiplier *= (1f - economy.turretCostDiscount / 100f);
                    if (economy.globalDamagePenaltyPercent > 0f)
                        damageMultiplier *= (1f - economy.globalDamagePenaltyPercent / 100f);
                    if (economy.globalRangePenaltyPercent > 0f)
                        rangeMultiplier *= (1f - economy.globalRangePenaltyPercent / 100f);
                    if (economy.globalFireRatePenaltyPercent > 0f)
                        fireRateMultiplier *= (1f - economy.globalFireRatePenaltyPercent / 100f);
                }

                if (card.cardEffect is CombatModifierEffect combat)
                {
                    // Crit chance is additive across cards, capped at 1.0.
                    critChance = Mathf.Clamp01(critChance + combat.critChanceAdd);
                    // Crit multiplier — take the highest, no stacking abuse.
                    if (combat.critMultiplierOverride > critMultiplier)
                        critMultiplier = combat.critMultiplierOverride;
                    bonusGoldPerKill += combat.bonusGoldPerKill;
                    if (combat.bossKillGoldMultiplier > bossKillGoldMultiplier)
                        bossKillGoldMultiplier = combat.bossKillGoldMultiplier;
                    if (combat.globalDamagePenaltyPercent > 0f)
                        damageMultiplier *= (1f - combat.globalDamagePenaltyPercent / 100f);
                    if (combat.globalFireRatePenaltyPercent > 0f)
                        fireRateMultiplier *= (1f - combat.globalFireRatePenaltyPercent / 100f);
                }

                if (card.cardEffect is ConditionalEffect cond)
                {
                    conditionalDmgMods.Add(new ConditionalDamageMod
                    {
                        id = card.cardName,
                        context = cond.context,
                        bonusPercent = cond.bonusDamagePercent / 100f,
                        thresholdValue = cond.thresholdValue
                    });
                    // Apply unconditional downsides (e.g. BossSlayer −10% vs normal)
                    if (cond.normalEnemyPenaltyPercent > 0f)
                    {
                        // Stored as a separate VsNormal entry with NEGATIVE bonus,
                        // so it triggers only on non-boss targets.
                        conditionalDmgMods.Add(new ConditionalDamageMod
                        {
                            id = card.cardName + "_penalty",
                            context = ConditionalContext.VsNormal,
                            bonusPercent = -cond.normalEnemyPenaltyPercent / 100f,
                            thresholdValue = 0f
                        });
                    }
                }

                if (card.cardEffect is SynergyEffect syn)
                {
                    int count = 0;
                    switch (syn.scaleBy)
                    {
                        case SynergyEffect.ScaleSource.ElementCardsInDeck:
                            elementCounts.TryGetValue(syn.targetElement, out count);
                            break;
                        case SynergyEffect.ScaleSource.EconomyCardsInDeck:
                            count = economyCount;
                            break;
                        case SynergyEffect.ScaleSource.UniqueElementsInDeck:
                            count = uniqueElements;
                            break;
                    }
                    count = Mathf.Min(count, syn.maxStacks);

                    if (syn.bonusDamagePercentPerStack > 0f)
                        damageMultiplier *= 1f + (syn.bonusDamagePercentPerStack / 100f) * count;
                    if (syn.bonusGoldPerSecondPerStack > 0)
                        passiveGoldPerSecond += syn.bonusGoldPerSecondPerStack * count;
                }
            }

            OnModifiersChanged?.Invoke();
        }

        private static readonly ElementType[] s_AllElements = new[]
        {
            ElementType.Fire, ElementType.Ice, ElementType.Lightning,
            ElementType.Nature, ElementType.Dark, ElementType.Light
        };

        // ==========================================
        // SABOTAGE — NEW ID-BASED API (preferred)
        // ==========================================

        /// <summary>
        /// Registers a sabotage modifier. Call this from a sabotage effect's
        /// Apply method, then call <see cref="RemoveById"/> on Remove to undo
        /// — without any divide-back math.
        /// </summary>
        /// <param name="id">Unique ID per sabotage instance. Use sabotage name + caster ID, or any GUID.</param>
        /// <param name="stat">Which stat this modifier affects.</param>
        /// <param name="multiplier">e.g. 0.5f = -50%, 1.3f = +30%.</param>
        public void ApplyById(string id, SabotageStat stat, float multiplier)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[PlayerModifierStack] ApplyById called with empty ID — falling back to auto-ID");
                id = AutoId(stat);
            }

            // Guard against double-apply with same ID — would silently
            // duplicate the modifier.
            for (int i = 0; i < activeSabotageMods.Count; i++)
            {
                if (activeSabotageMods[i].id == id && activeSabotageMods[i].stat == stat)
                {
                    Debug.LogWarning($"[PlayerModifierStack] Sabotage '{id}' for {stat} already active — replacing.");
                    activeSabotageMods[i].multiplier = multiplier;
                    OnModifiersChanged?.Invoke();
                    return;
                }
            }

            activeSabotageMods.Add(new SabotageMod(id, stat, multiplier));
            OnModifiersChanged?.Invoke();
        }

        /// <summary>Removes a sabotage modifier added via <see cref="ApplyById"/>.</summary>
        public bool RemoveById(string id, SabotageStat stat)
        {
            for (int i = activeSabotageMods.Count - 1; i >= 0; i--)
            {
                if (activeSabotageMods[i].id == id && activeSabotageMods[i].stat == stat)
                {
                    activeSabotageMods.RemoveAt(i);
                    OnModifiersChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }

        /// <summary>Returns a snapshot of all active modifiers for a given stat (for UI).</summary>
        public List<SabotageMod> GetActiveModifiersByStat(SabotageStat stat)
        {
            var result = new List<SabotageMod>();
            for (int i = 0; i < activeSabotageMods.Count; i++)
                if (activeSabotageMods[i].stat == stat)
                    result.Add(activeSabotageMods[i]);
            return result;
        }

        /// <summary>Returns full list of active sabotage entries (UI tooltip).</summary>
        public IReadOnlyList<SabotageMod> GetAllActiveSabotageMods() => activeSabotageMods;

        private float ProductOf(SabotageStat stat)
        {
            float product = 1f;
            for (int i = 0; i < activeSabotageMods.Count; i++)
                if (activeSabotageMods[i].stat == stat)
                    product *= activeSabotageMods[i].multiplier;
            return product;
        }

        private string AutoId(SabotageStat stat) => $"auto_{stat}_{++autoIdCounter}";

        // ==========================================
        // SABOTAGE — LEGACY API (forwarders to ID-based)
        // 
        // Kept so existing sabotage code (and the PlayerCardManager facade)
        // keeps working. New sabotage effects should call ApplyById/RemoveById
        // with a stable ID per sabotage instance — that's what makes the
        // "show -50% dmg from Curse, +30% range from Buff" UI possible.
        // ==========================================

        public void SetUpgradesDisabled(bool disabled)
        {
            upgradesDisabled = disabled;
            OnModifiersChanged?.Invoke();
        }

        public void ApplySabotageDamage(float multiplier)
            => ApplyById(AutoId(SabotageStat.Damage), SabotageStat.Damage, multiplier);

        public void RemoveSabotageDamage(float multiplier)
            => RemoveOldestMatching(SabotageStat.Damage, multiplier);

        public void ApplySabotageFireRate(float multiplier)
            => ApplyById(AutoId(SabotageStat.FireRate), SabotageStat.FireRate, multiplier);

        public void RemoveSabotageFireRate(float multiplier)
            => RemoveOldestMatching(SabotageStat.FireRate, multiplier);

        public void ApplySabotageRange(float multiplier)
            => ApplyById(AutoId(SabotageStat.Range), SabotageStat.Range, multiplier);

        public void RemoveSabotageRange(float multiplier)
            => RemoveOldestMatching(SabotageStat.Range, multiplier);

        public void ApplySabotageCost(float multiplier)
            => ApplyById(AutoId(SabotageStat.Cost), SabotageStat.Cost, multiplier);

        public void RemoveSabotageCost(float multiplier)
            => RemoveOldestMatching(SabotageStat.Cost, multiplier);

        /// <summary>
        /// Legacy Remove API — when caller doesn't track IDs, find and pop
        /// the oldest entry matching this multiplier. Safer than divide-back
        /// because there's no float-point math involved.
        /// </summary>
        private void RemoveOldestMatching(SabotageStat stat, float multiplier)
        {
            for (int i = 0; i < activeSabotageMods.Count; i++)
            {
                var m = activeSabotageMods[i];
                if (m.stat != stat) continue;
                // Float compare with tolerance — sabotage cards typically use
                // fixed values like 0.5 / 1.3, no chance of a 7-decimal mismatch.
                if (Mathf.Abs(m.multiplier - multiplier) < 0.0001f)
                {
                    activeSabotageMods.RemoveAt(i);
                    OnModifiersChanged?.Invoke();
                    return;
                }
            }
            Debug.LogWarning($"[PlayerModifierStack] RemoveOldestMatching({stat}, {multiplier}) " +
                             $"found no matching entry. Possible double-remove?");
        }

        /// <summary>Reset ALL sabotage modifiers (called by ClearAllSabotages).</summary>
        public void ResetSabotageModifiers()
        {
            activeSabotageMods.Clear();
            upgradesDisabled = false;
            OnModifiersChanged?.Invoke();
        }

        // ==========================================
        // QUERY API (final = base * cardGlobal * cardElement * sabotage)
        // ==========================================

        public float GetModifiedDamage(float baseDamage, ElementType element)
        {
            float elementMod = elementModifiers.TryGetValue(element, out var mods)
                ? mods.damageMultiplier : 1f;
            return baseDamage * damageMultiplier * elementMod * SabotageDamageProduct;
        }

        public float GetModifiedFireRate(float baseFireRate, ElementType element)
        {
            float elementMod = elementModifiers.TryGetValue(element, out var mods)
                ? mods.fireRateMultiplier : 1f;
            return baseFireRate * fireRateMultiplier * elementMod * SabotageFireRateProduct;
        }

        public float GetModifiedRange(float baseRange, ElementType element)
        {
            float elementMod = elementModifiers.TryGetValue(element, out var mods)
                ? mods.rangeMultiplier : 1f;
            return baseRange * rangeMultiplier * elementMod * SabotageRangeProduct;
        }

        public int GetModifiedTurretCost(int baseCost)
        {
            return Mathf.RoundToInt(baseCost * turretCostMultiplier * SabotageCostProduct);
        }

        public float GetAdditionalAOE(ElementType element)
            => elementModifiers.TryGetValue(element, out var mods) ? mods.addAOERadius : 0f;

        public int GetAdditionalPierce(ElementType element)
            => elementModifiers.TryGetValue(element, out var mods) ? mods.addPierceCount : 0;

        public int GetAdditionalChainTargets(ElementType element)
            => elementModifiers.TryGetValue(element, out var mods) ? mods.addChainTargets : 0;

        // ==========================================
        // COMBAT EXTENSION QUERIES
        // ==========================================

        /// <summary>
        /// Rolls crit, returns final damage. Use this in TurretShooter instead
        /// of plain GetModifiedDamage when crit support is desired.
        /// </summary>
        /// <param name="wasCrit">Out — true if this shot critted (for VFX/SFX).</param>
        public float GetDamageWithCritRoll(float baseDamage, ElementType element, out bool wasCrit)
        {
            float dmg = GetModifiedDamage(baseDamage, element);
            wasCrit = false;
            if (critChance > 0f && UnityEngine.Random.value < critChance)
            {
                dmg *= critMultiplier;
                wasCrit = true;
            }
            return dmg;
        }

        /// <summary>
        /// Computes additive bonus damage % from all conditional cards whose
        /// predicate is currently satisfied. Returns e.g. 0.30 for a single
        /// active "+30% in low HP" card.
        /// 
        /// Caller passes whatever context flags it knows (see ConditionalContext).
        /// </summary>
        public float GetConditionalDamageBonus(
            bool isBoss,
            float playerHpPercent,
            float waveElapsed,
            float waveTotalDuration,
            int localGold,
            int opponentGold)
        {
            if (conditionalDmgMods.Count == 0) return 0f;
            float bonus = 0f;
            for (int i = 0; i < conditionalDmgMods.Count; i++)
            {
                var m = conditionalDmgMods[i];
                bool active = false;
                switch (m.context)
                {
                    case ConditionalContext.VsBoss:
                        active = isBoss; break;
                    case ConditionalContext.VsNormal:
                        active = !isBoss; break;
                    case ConditionalContext.LowPlayerHp:
                        // thresholdValue interpreted as percent (0..1). 0.3 = below 30% HP.
                        active = playerHpPercent <= (m.thresholdValue > 0f ? m.thresholdValue : 0.3f);
                        break;
                    case ConditionalContext.WaveOpening:
                        active = waveElapsed <= (m.thresholdValue > 0f ? m.thresholdValue : 10f);
                        break;
                    case ConditionalContext.WaveClosing:
                        // thresholdValue = trailing seconds at end of wave (default 10s).
                        float window = m.thresholdValue > 0f ? m.thresholdValue : 10f;
                        active = waveTotalDuration > 0f && (waveTotalDuration - waveElapsed) <= window;
                        break;
                    case ConditionalContext.UnderdogGold:
                        active = localGold < opponentGold;
                        break;
                }
                if (active) bonus += m.bonusPercent;
            }
            return bonus;
        }

        /// <summary>Convenience overload for callers that only know the boss flag.</summary>
        public float GetConditionalDamageBonusForTarget(bool isBoss)
            => GetConditionalDamageBonus(isBoss, 1f, 0f, 0f, 0, 0);

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Print All Modifiers")]
        private void PrintModifiers()
        {
            Debug.Log($"=== CARD MODIFIERS ===\n" +
                      $"  DMG:  {damageMultiplier:F2}x\n" +
                      $"  FR:   {fireRateMultiplier:F2}x\n" +
                      $"  RNG:  {rangeMultiplier:F2}x\n" +
                      $"  Cost: {turretCostMultiplier:F2}x\n" +
                      $"  Gold: {passiveGoldPerSecond}/s");

            Debug.Log($"=== SABOTAGE MODIFIERS ({activeSabotageMods.Count}) ===\n" +
                      $"  DMG:  {SabotageDamageProduct:F2}x\n" +
                      $"  FR:   {SabotageFireRateProduct:F2}x\n" +
                      $"  RNG:  {SabotageRangeProduct:F2}x\n" +
                      $"  Cost: {SabotageCostProduct:F2}x\n" +
                      $"  Upgrades Disabled: {upgradesDisabled}");

            foreach (var mod in activeSabotageMods)
                Debug.Log($"    [{mod.id}] {mod.stat} x{mod.multiplier:F2}");

            foreach (var kvp in elementModifiers)
                Debug.Log($"  [{kvp.Key}] DMG={kvp.Value.damageMultiplier:F2}x, " +
                          $"FR={kvp.Value.fireRateMultiplier:F2}x, " +
                          $"RNG={kvp.Value.rangeMultiplier:F2}x, " +
                          $"AOE+{kvp.Value.addAOERadius}, " +
                          $"Pierce+{kvp.Value.addPierceCount}, " +
                          $"Chain+{kvp.Value.addChainTargets}");
        }
    }
}
