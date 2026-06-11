using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ElementumDefense.StatusEffects;
using ElementumDefense.Enemies;


namespace ElementumDefense.Turrets
{
/// <summary>
/// Light element turret â€” pure support, does NOT attack.
/// Passively buffs nearby allied turrets and optionally applies
/// Expose to enemies in range (armor reduction).
/// 
/// On synergy (set via Inspector after Merge), applies an
/// additional elemental aura effect to enemies (e.g., small Burn DoT).
/// </summary>
public class LightAuraTurret : MonoBehaviour
{
    [Header("Aura Range")]
    [Tooltip("Radius within which allied turrets are buffed")]
    [SerializeField] private float auraRadius = 8f;

    [Tooltip("How often the aura pulses (seconds)")]
    [SerializeField] private float auraTick = 0.5f;

    [Header("Turret Buffs (applied to Turret components in range)")]
    [Tooltip("Bonus damage multiplier added to nearby turrets (0.15 = +15%)")]
    [SerializeField] private float damageBonusPercent = 0.15f;

    [Tooltip("Bonus attack range added to nearby turrets")]
    [SerializeField] private float rangeBonusUnits = 1f;

    [Tooltip("Bonus fire rate multiplier (0.1 = +10%)")]
    [SerializeField] private float fireRateBonusPercent = 0.10f;

    [Header("Enemy Aura (Expose)")]
    [Tooltip("Apply Expose to enemies in range? (reduces their armor)")]
    [SerializeField] private bool applyExposeToEnemies = true;

    [Tooltip("Expose duration in seconds")]
    [SerializeField] private float exposeDuration = 4f;

    [Tooltip("Armor reduction from Expose (0.30 = -30%)")]
    [SerializeField] private float exposeArmorReduction = 0.30f;

    [Header("Synergy Aura (set by TurretMergeManager after merge)")]
    [Tooltip("Synergy element â€” determines what aura effect is applied to enemies")]
    [SerializeField] private ElementumDefense.Elements.ElementType synergyElement
        = ElementumDefense.Elements.ElementType.None;

    [Tooltip("Synergy DoT damage per tick (0 = no synergy DoT)")]
    [SerializeField] private float synergyDotDamage = 0f;

    [Tooltip("Synergy effect type applied to enemies")]
    [SerializeField] private StatusEffectType synergyEffect = StatusEffectType.Expose;

    [Tooltip("Synergy effect duration")]
    [SerializeField] private float synergyEffectDuration = 2f;

    [Header("Visual")]
    [SerializeField] private Light auraLight;
    [SerializeField] private ParticleSystem auraParticles;

    private Coroutine auraCoroutine;
    private int ownerPhotonViewID = -1;

    // Tracks which turrets we're currently buffing (to remove buff on range exit)
    private HashSet<Turret> buffedTurrets = new HashSet<Turret>();

    // ==========================================
    // INITIALIZATION
    // ==========================================

    public void Initialize(int ownerID)
    {
        ownerPhotonViewID = ownerID;
        StartAura();
    }

    /// <summary>Called by TurretMergeManager to set synergy after merge</summary>
    public void SetSynergy(ElementumDefense.Elements.ElementType element,
                           StatusEffectType effectType,
                           float dotDamage,
                           float effectDuration)
    {
        synergyElement = element;
        synergyEffect = effectType;
        synergyDotDamage = dotDamage;
        synergyEffectDuration = effectDuration;

        Debug.Log($"[LightAura] Synergy set: {element} â€” {effectType} " +
                  $"({dotDamage} DoT, {effectDuration}s)");
    }

    // ==========================================
    // AURA LOOP
    // ==========================================

    private void StartAura()
    {
        if (auraCoroutine != null) StopCoroutine(auraCoroutine);
        auraCoroutine = StartCoroutine(AuraLoop());
    }

    private IEnumerator AuraLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(auraTick);

            PulseAura();
        }
    }

    private void PulseAura()
    {
        // -- TURRET BUFFS --
        HashSet<Turret> currentlyInRange = new HashSet<Turret>();
        Collider[] hits = Physics.OverlapSphere(transform.position, auraRadius);

        foreach (var col in hits)
        {
            Turret t = col.GetComponent<Turret>();
            if (t == null || t.gameObject == gameObject) continue;

            currentlyInRange.Add(t);

            if (!buffedTurrets.Contains(t))
            {
                // Apply buff on entry
                t.AddAuraBuff(damageBonusPercent, rangeBonusUnits, fireRateBonusPercent);
                buffedTurrets.Add(t);
            }
        }

        // Remove buff from turrets that left range
        foreach (var t in new List<Turret>(buffedTurrets))
        {
            if (t == null || !currentlyInRange.Contains(t))
            {
                t?.RemoveAuraBuff(damageBonusPercent, rangeBonusUnits, fireRateBonusPercent);
                buffedTurrets.Remove(t);
            }
        }

        // -- ENEMY EFFECTS (Expose + optional Synergy) --
        foreach (var col in hits)
        {
            EnemyHealth enemy = col.GetComponent<EnemyHealth>();
            if (enemy == null) continue;

            StatusEffectManager sem = enemy.GetComponent<StatusEffectManager>();
            if (sem == null) continue;

            // Base Expose (always)
            if (applyExposeToEnemies && !sem.HasEffect(StatusEffectType.Expose))
            {
                sem.ApplyEffect(new ExposeEffect(exposeArmorReduction) );
                // Manually set duration via Initialize â€” workaround since ExposeEffect uses base Initialize
                sem.GetEffect(StatusEffectType.Expose)?.Initialize(enemy, exposeDuration);
            }

            // Synergy DoT (only if synergy is active)
            if (synergyDotDamage > 0f)
            {
                ApplySynergyEffect(enemy, sem);
            }
        }
    }

    private void ApplySynergyEffect(EnemyHealth enemy, StatusEffectManager sem)
    {
        StatusEffect synergyStatusEffect = synergyEffect switch
        {
            StatusEffectType.Burn   => new BurnEffect(synergyDotDamage, synergyEffectDuration),
            StatusEffectType.Chill  => new ChillEffect(),
            StatusEffectType.Poison => new PoisonEffect(synergyDotDamage, synergyEffectDuration),
            StatusEffectType.Slow   => new SlowEffect(0.3f, synergyEffectDuration),
            _                       => null
        };

        if (synergyStatusEffect != null)
            sem.ApplyEffect(synergyStatusEffect);
    }

    // ==========================================
    // CLEANUP
    // ==========================================

    private void OnDestroy()
    {
        // Remove all buffs from turrets when destroyed
        foreach (var t in buffedTurrets)
        {
            t?.RemoveAuraBuff(damageBonusPercent, rangeBonusUnits, fireRateBonusPercent);
        }
        buffedTurrets.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.5f, 0.25f);
        Gizmos.DrawSphere(transform.position, auraRadius);
        Gizmos.color = new Color(1f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
}
}
