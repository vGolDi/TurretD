using System;
using Photon.Pun;
using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.Enemies;
using ElementumDefense.UI;
using ElementumDefense.Players;


namespace ElementumDefense.Enemies
{
public class EnemyHealth : MonoBehaviour, IEnemyPoolable
{
    // ==========================================
    // CONFIGURATION
    // ==========================================

    [Header("Data Source (optional)")]
    [SerializeField, Tooltip("Optional EnemyData SO. If assigned, overrides maxHP / element / goldReward in Awake. Leave null to use the inspector fields below.")]
    private EnemyData enemyData;

    [Header("Stats")]
    [SerializeField, Tooltip("Enemy health points")]
    private int maxHP = 100;

    [SerializeField, Tooltip("Elemental type of this enemy (affects damage taken)")]
    private ElementType elementType = ElementType.None;

    [SerializeField, Tooltip("Show damage numbers when hit?")]
    private bool showDamageNumbers = true;

    [SerializeField, Tooltip("Gold awarded to whoever lands the killing blow")]
    private int goldReward = 10;

    [SerializeField, Tooltip("Marks this enemy as a boss for bonus damage / gold cards")]
    private bool isBoss = false;

    // ==========================================
    // EVENTS
    // ==========================================

    /// <summary>
    /// Fired RIGHT BEFORE the enemy returns to the pool / is destroyed.
    /// Args: this EnemyHealth, killerPhotonViewID (-1 if unknown).
    /// Used by modular death mechanics: SplitOnDeath, ReviveOnDeath, etc.
    /// </summary>
    public event Action<EnemyHealth, int> OnDeath;

    /// <summary>
    /// Fired every time the enemy takes damage (after all modifiers).
    /// Args: finalDamage, currentHP after damage.
    /// </summary>
    public event Action<int, int> OnDamageTaken;

    // ==========================================
    // RUNTIME
    // ==========================================

    private int currentHP;
    private int prefabMaxHP;            // snapshot for pool reset
    private ElementType originalElementType; // snapshot for pool reset
    private bool killRewardGiven = false;

    private EnemyArmor armor;
    private PooledEnemy pooled;
    private HealthBar healthBar;
    private ElementumDefense.StatusEffects.StatusEffectManager statusEffectManager;

    // ==========================================
    // PUBLIC ACCESSORS
    // ==========================================

    public int GetMaxHP() => maxHP;
    public ElementType GetElementType() => elementType;
    public bool IsBoss => isBoss;

    public void SetMaxHP(int newMaxHP)
    {
        maxHP = newMaxHP;
        currentHP = newMaxHP;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHP);
            healthBar.SetHealth(currentHP);
        }
    }

    /// <summary>Allows changing element dynamically (sabotage / events).</summary>
    public void SetElementType(ElementType newElement)
    {
        elementType = newElement;
        UpdateHealthBarColor();
    }

    /// <summary>
    /// Heals the enemy by <paramref name="amount"/>, capped at maxHP. Used by
    /// regen sabotage. Doesn't fire OnDamageTaken — that's only for damage events.
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        if (healthBar != null) healthBar.SetHealth(currentHP);
    }

    // ==========================================
    // LIFECYCLE
    // ==========================================

    private void Awake()
    {
        statusEffectManager = GetComponent<ElementumDefense.StatusEffects.StatusEffectManager>();
        armor = GetComponent<EnemyArmor>();
        pooled = GetComponent<PooledEnemy>();

        // Apply EnemyData overrides BEFORE snapshotting prefab defaults so the
        // SO values become the "reset target" for pool reuse.
        if (enemyData != null)
        {
            maxHP = enemyData.maxHP;
            goldReward = enemyData.goldReward;
            elementType = enemyData.elementType;
            isBoss = enemyData.isBoss;
        }

        prefabMaxHP = maxHP;
        originalElementType = elementType;
    }

    private void Start()
    {
        currentHP = maxHP;
        healthBar = GetComponentInChildren<HealthBar>();

        if (healthBar != null)
            healthBar.SetMaxHealth(maxHP);

        UpdateHealthBarColor();
    }

    // ==========================================
    // POOLING
    // ==========================================

    /// <summary>Reset all runtime state before the pool re-enables this object.</summary>
    public void OnSpawnedFromPool()
    {
        if (healthBar == null)
            healthBar = GetComponentInChildren<HealthBar>();

        // Restore prefab/SO defaults. SplitOnDeath / sabotage will override AFTER spawn.
        maxHP = prefabMaxHP;
        currentHP = prefabMaxHP;
        elementType = originalElementType;
        killRewardGiven = false;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHP);
            healthBar.SetHealth(maxHP);
        }
        UpdateHealthBarColor();
    }

    /// <summary>Drop subscribers so a fresh wave can re-attach cleanly.</summary>
    public void OnReturnedToPool()
    {
        OnDeath = null;
        OnDamageTaken = null;
    }

    // ==========================================
    // DAMAGE
    // ==========================================

    public void TakeDamage(int baseDamage, int attackerPhotonViewID = -1, ElementType damageElement = ElementType.None)
    {
        // Armor guard: armored enemies are immune to ALL damage from turrets /
        // projectiles / AOE. Player must click them off (EnemyArmor.OnPlayerClicked).
        if (armor != null && armor.IsArmored)
        {
            armor.NotifyHitFromAOE(); // no-op unless aoeBreaksArmor=true
            return;
        }

        float elementMultiplier = ElementUtility.GetDamageMultiplier(damageElement, elementType);

        // ===== Sabotage: per-enemy element resist =====
        // EnemyElementResistSabotage component (added at spawn time) overrides
        // the matchup multiplier when the incoming element matches.
        var resist = GetComponent<EnemyElementResistSabotage>();
        if (resist != null && resist.ResistedElement == damageElement && resist.ResistedElement != ElementType.None)
        {
            elementMultiplier *= resist.DamageMultiplier;
        }
        // ===============================================

        // Curse (+35% dmg taken) and Expose (armor reduction) modifiers from
        // status effects. Combined multiplicatively with the element matchup.
        float statusMultiplier = statusEffectManager != null
            ? statusEffectManager.IncomingDamageMultiplier * (1f + statusEffectManager.ArmorReduction)
            : 1f;

        int finalDamage = Mathf.RoundToInt(baseDamage * elementMultiplier * statusMultiplier);
        currentHP -= finalDamage;

        if (showDamageNumbers && DamageNumberManager.Instance != null)
        {
            DamageNumberType numberType =
                elementMultiplier > 1.0f ? DamageNumberType.Effective :
                elementMultiplier < 1.0f ? DamageNumberType.Resisted :
                DamageNumberType.Normal;

            DamageNumberManager.Instance.ShowDamageNumberAtEnemy(this, finalDamage, numberType);
        }

        if (healthBar != null)
            healthBar.SetHealth(currentHP);

        OnDamageTaken?.Invoke(finalDamage, currentHP);

        if (currentHP <= 0 && !killRewardGiven)
        {
            killRewardGiven = true;
            Die(attackerPhotonViewID);
        }
    }

    private void Die(int killerPhotonViewID)
    {
        // Award gold to the killer (if it was a real attacker, not a DOT).
        if (killerPhotonViewID != -1)
        {
            PhotonView killerView = PhotonView.Find(killerPhotonViewID);
            if (killerView != null && killerView.IsMine)
            {
                PlayerGold playerGold = killerView.GetComponent<PlayerGold>();
                if (playerGold != null)
                {
                    int reward = goldReward;

                    // Combat cards: bonus gold per kill + boss kill multiplier.
                    var modStack = killerView.GetComponent<ElementumDefense.Cards.PlayerModifierStack>();
                    if (modStack != null)
                    {
                        if (isBoss && modStack.BossKillGoldMultiplier > 1f)
                            reward = Mathf.RoundToInt(reward * modStack.BossKillGoldMultiplier);
                        reward += modStack.BonusGoldPerKill;
                    }

                    playerGold.AddGold(reward);
                }
            }
        }

        // Quest progression — counts every kill on the map (co-op friendly).
        ElementumDefense.Progression.QuestManager.Instance?.ReportProgress(
            ElementumDefense.Progression.QuestType.KillEnemies,
            1);

        // Modular death hooks (Split, Revive, Bomb, ...) — must run BEFORE
        // return-to-pool so they can read this enemy's transform / components.
        OnDeath?.Invoke(this, killerPhotonViewID);

        ReleaseToPoolOrDestroy();
    }

    /// <summary>
    /// Returns this enemy to the pool if it has a PooledEnemy component, or
    /// falls back to Destroy. Used by both Die() and EnemyMovement.OnPathCompleted.
    /// </summary>
    public void ReleaseToPoolOrDestroy()
    {
        if (pooled != null)
            pooled.ReturnToPool();
        else
            Destroy(gameObject);
    }

    // ==========================================
    // VISUALS
    // ==========================================

    private void UpdateHealthBarColor()
    {
        if (healthBar == null) return;

        var fillImage = healthBar.GetComponent<UnityEngine.UI.Image>();
        if (fillImage != null)
            fillImage.color = ElementUtility.GetElementColor(elementType);
    }
}
}
