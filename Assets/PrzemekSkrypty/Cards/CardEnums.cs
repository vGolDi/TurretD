namespace ElementumDefense.Cards
{
    /// <summary>
    /// Rarity tiers for cards
    /// Affects drop rates and deck limits
    /// </summary>
    public enum CardRarity
    {
        Common,     // 70% drop, max 15 in deck
        Rare,       // 25% drop, max 10 in deck
        Legendary   // 5% drop, max 1 in deck (unique)
    }

    /// <summary>
    /// Card category - defines how card is used
    /// </summary>
    public enum CardType
    {
        Turret,     // Unlocks/upgrades turrets
        Economy,    // Gold generation, cost reduction
        Utility,    // Player buffs/debuffs, special mechanics
        Defensive   // Health, armor, wave delay
    }

    /// <summary>
    /// When can this card be activated?
    /// </summary>
    public enum CardActivationType
    {
        OnDraft,    // ⚡ Instant effect when drafted (e.g., +100 gold, instant heal)
        Continuous  // 🔄 Passive modifier active whole game (e.g., +10% damage, +5 gold/s)
    }
}