using Photon.Pun;
using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.UI;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Tooltip("Enemy health points")] private int maxHP = 100;
    private int currentHP;

    [Header("Element")]
    [SerializeField, Tooltip("Elemental type of this enemy (affects damage taken)")]
    private ElementType elementType = ElementType.None;

    [SerializeField, Tooltip("Show damage numbers when hit?")]
    private bool showDamageNumbers = true;

    private HealthBar healthBar;

    [SerializeField] private int goldReward = 10;
    private bool killRewardGiven = false;

    void Start()
    {
        currentHP = maxHP;
        healthBar = GetComponentInChildren<HealthBar>();

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHP);
        }
        UpdateHealthBarColor();
    }

    /// <summary>
    /// Applies damage with elemental calculations
    /// </summary>
    /// <param name="baseDamage">Raw damage before modifiers</param>
    /// <param name="attackerPhotonViewID">PhotonView ID of turret owner</param>
    /// <param name="damageElement">Element type of attacking turret</param>
    //public void TakeDamage(int baseDamage, int attackerPhotonViewID = -1, ElementType damageElement = ElementType.None)
    //{
    //    // Calculate elemental modifier
    //    float elementMultiplier = ElementUtility.GetDamageMultiplier(damageElement, elementType);
    //    int finalDamage = Mathf.RoundToInt(baseDamage * elementMultiplier);

    //    currentHP -= finalDamage;

    //    // NOWE: Log damage with element info
    //    Debug.Log($"[EnemyHealth] {gameObject.name} took {finalDamage} damage " +
    //              $"(base: {baseDamage}, element: {damageElement} vs {elementType}, mult: {elementMultiplier}x)");

    //    // NOWE: Show damage number (opcjonalne - zrobimy póŸniej)
    //    if (showDamageNumbers)
    //    {
    //        ShowDamageNumber(finalDamage, elementMultiplier);
    //    }

    //    // Update healthbar
    //    if (healthBar != null)
    //    {
    //        healthBar.SetHealth(currentHP);
    //    }

    //    if (currentHP <= 0 && !killRewardGiven)
    //    {
    //        killRewardGiven = true;
    //        Die(attackerPhotonViewID);
    //    }
    //}
    public void TakeDamage(int baseDamage, int attackerPhotonViewID = -1, ElementType damageElement = ElementType.None)
    {
        float elementMultiplier = ElementUtility.GetDamageMultiplier(damageElement, elementType);
        int finalDamage = Mathf.RoundToInt(baseDamage * elementMultiplier);

        currentHP -= finalDamage;

        // ========== ZMIENIONE: Show damage number ==========
        if (showDamageNumbers && DamageNumberManager.Instance != null)
        {
            DamageNumberType numberType = DamageNumberType.Normal;

            if (elementMultiplier > 1.0f)
                numberType = DamageNumberType.Effective; // Green
            else if (elementMultiplier < 1.0f)
                numberType = DamageNumberType.Resisted;  // Red

            DamageNumberManager.Instance.ShowDamageNumberAtEnemy(this, finalDamage, numberType);
        }
        // ===================================================

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHP);
        }

        if (currentHP <= 0 && !killRewardGiven)
        {
            killRewardGiven = true;
            Die(attackerPhotonViewID);
        }
    }
    public void TakeDamage(int damage, int attackerPhotonViewID = -1)
    {
        TakeDamage(damage, attackerPhotonViewID, ElementType.None);
    }
    private void Die(int killerPhotonViewID)
    {
        // Award gold to the player who killed this enemy
        if (killerPhotonViewID != -1)
        {
            PhotonView killerView = PhotonView.Find(killerPhotonViewID);
            if (killerView != null && killerView.IsMine)
            {
                PlayerGold playerGold = killerView.GetComponent<PlayerGold>();
                if (playerGold != null)
                {
                    playerGold.AddGold(goldReward);
                }
            }
        }

        // TODO: Death VFX/SFX
        Destroy(gameObject);
    }
    /// <summary>
    /// Updates healthbar color based on element type
    /// </summary>
    private void UpdateHealthBarColor()
    {
        if (healthBar == null) return;

        // Get fill image
        var fillImage = healthBar.GetComponent<UnityEngine.UI.Image>();
        if (fillImage != null)
        {
            fillImage.color = ElementUtility.GetElementColor(elementType);
        }
    }

    /// <summary>
    /// Shows floating damage number (visual feedback)
    /// TODO: Implement DamageNumberController
    /// </summary>
    private void ShowDamageNumber(int damage, float multiplier)
    {
        // Placeholder - zaimplementujemy póŸniej z VFX system
        Color numberColor = Color.white;

        if (multiplier > 1.0f)
            numberColor = Color.green; // Effective!
        else if (multiplier < 1.0f)
            numberColor = Color.red;   // Resisted!

        // TODO: Spawn floating text with damage value
        Debug.Log($"[DamageNumber] {damage} (color: {numberColor})");
    }

    /// <summary>
    /// Public getter for element type (for UI/effects)
    /// </summary>
    public ElementType GetElementType() => elementType;

    /// <summary>
    /// Allows changing element dynamically (for special events/sabotage)
    /// </summary>
    public void SetElementType(ElementType newElement)
    {
        elementType = newElement;
        UpdateHealthBarColor();
        Debug.Log($"[EnemyHealth] Element changed to {newElement}");
    }
}
