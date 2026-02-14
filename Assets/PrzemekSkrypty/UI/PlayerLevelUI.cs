// Assets/PrzemekSkrypty/UI/PlayerLevelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ElementumDefense.Cards;
using System.Collections;

namespace ElementumDefense.UI
{
    /// <summary>
    /// UI component showing player level, XP bar, and currency
    /// </summary>
    public class PlayerLevelUI : MonoBehaviour
    {
        [Header("Level Display")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image levelBadgeImage; // Zmieniona nazwa dla jasnoœci

        [Header("Level Badge Sprites")] // NOWE
        [SerializeField] private LevelBadgeConfig[] levelBadges;

        [Header("XP Bar")]
        [SerializeField] private Slider xpSlider;
        [SerializeField] private Image xpFillImage;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private TMP_Text xpPercentText;

        [Header("Currency Display")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text crystalsText;

        [Header("Rank Display (Optional)")]
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text eloText;
        [SerializeField] private Image rankIcon;

        [Header("Level Up Animation")]
        [SerializeField] private GameObject levelUpPopup;
        [SerializeField] private TMP_Text levelUpText;
        [SerializeField] private float levelUpDisplayTime = 2f;
        [SerializeField] private AudioClip levelUpSound;

        [Header("XP Gain Animation")]
        [SerializeField] private bool animateXPBar = true;
        [SerializeField] private float xpAnimationSpeed = 2f;

        [Header("XP Bar Colors")]
        [SerializeField] private Color xpBarColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color xpBarFullColor = new Color(1f, 0.8f, 0f);

        private AudioSource audioSource;
        private float targetXPValue;
        private float currentXPValue;
        private bool isAnimatingXP = false;
        private int lastKnownLevel = 0;

        // ==========================================
        // LEVEL BADGE CONFIG (NOWE)
        // ==========================================

        [System.Serializable]
        public class LevelBadgeConfig
        {
            [Tooltip("Minimum level for this badge (inclusive)")]
            public int minLevel = 1;

            [Tooltip("Badge sprite for this level range")]
            public Sprite badgeSprite;
        }

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            if (levelUpPopup != null)
                levelUpPopup.SetActive(false);
        }

        private void Start()
        {
            SubscribeToEvents();
            RefreshAllUI();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            RefreshAllUI();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Animate XP bar
            if (isAnimatingXP && animateXPBar)
            {
                currentXPValue = Mathf.MoveTowards(currentXPValue, targetXPValue, xpAnimationSpeed * Time.deltaTime);

                if (xpSlider != null)
                    xpSlider.value = currentXPValue;

                if (Mathf.Approximately(currentXPValue, targetXPValue))
                {
                    isAnimatingXP = false;
                }
            }
        }

        // ==========================================
        // EVENT SUBSCRIPTIONS
        // ==========================================

        private void SubscribeToEvents()
        {
            if (PlayerCollection.Instance == null) return;

            PlayerCollection.Instance.OnLevelChanged -= OnLevelChanged;
            PlayerCollection.Instance.OnLevelChanged += OnLevelChanged;

            PlayerCollection.Instance.OnXPChanged -= OnXPChanged;
            PlayerCollection.Instance.OnXPChanged += OnXPChanged;

            PlayerCollection.Instance.OnGoldChanged -= OnGoldChanged;
            PlayerCollection.Instance.OnGoldChanged += OnGoldChanged;

            PlayerCollection.Instance.OnCrystalsChanged -= OnCrystalsChanged;
            PlayerCollection.Instance.OnCrystalsChanged += OnCrystalsChanged;

            PlayerCollection.Instance.OnEloChanged -= OnEloChanged;
            PlayerCollection.Instance.OnEloChanged += OnEloChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (PlayerCollection.Instance == null) return;

            PlayerCollection.Instance.OnLevelChanged -= OnLevelChanged;
            PlayerCollection.Instance.OnXPChanged -= OnXPChanged;
            PlayerCollection.Instance.OnGoldChanged -= OnGoldChanged;
            PlayerCollection.Instance.OnCrystalsChanged -= OnCrystalsChanged;
            PlayerCollection.Instance.OnEloChanged -= OnEloChanged;
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private void OnLevelChanged(int newLevel)
        {
            UpdateLevelDisplay(newLevel);

            // Show level up animation if actually leveled up
            if (newLevel > lastKnownLevel && lastKnownLevel > 0)
            {
                ShowLevelUpAnimation(newLevel);
            }

            lastKnownLevel = newLevel;

            // Reset XP bar for new level
            UpdateXPDisplay(0, PlayerCollection.Instance?.GetXPForNextLevel() ?? 1000);
        }

        private void OnXPChanged(int currentXP, int requiredXP)
        {
            UpdateXPDisplay(currentXP, requiredXP);
        }

        private void OnGoldChanged(int newGold)
        {
            UpdateGoldDisplay(newGold);
        }

        private void OnCrystalsChanged(int newCrystals)
        {
            UpdateCrystalsDisplay(newCrystals);
        }

        private void OnEloChanged(int newElo)
        {
            UpdateRankDisplay();
        }

        // ==========================================
        // UI UPDATES
        // ==========================================

        /// <summary>
        /// Refreshes all UI elements from current PlayerCollection state
        /// </summary>
        public void RefreshAllUI()
        {
            if (PlayerCollection.Instance == null)
            {
                Debug.LogWarning("[PlayerLevelUI] PlayerCollection not found");
                return;
            }

            int level = PlayerCollection.Instance.GetLevel();
            int currentXP = PlayerCollection.Instance.GetCurrentXP();
            int requiredXP = PlayerCollection.Instance.GetXPForNextLevel();
            int gold = PlayerCollection.Instance.GetGold();
            int crystals = PlayerCollection.Instance.GetCrystals();

            lastKnownLevel = level;

            UpdateLevelDisplay(level);
            UpdateXPDisplay(currentXP, requiredXP, animate: false);
            UpdateGoldDisplay(gold);
            UpdateCrystalsDisplay(crystals);
            UpdateRankDisplay();
        }

        private void UpdateLevelDisplay(int level)
        {
            if (levelText != null)
                levelText.text = $"Level {level}";

            // Zmieñ sprite badge'a w zale¿noœci od levelu
            if (levelBadgeImage != null)
            {
                LevelBadgeConfig badgeConfig = GetBadgeForLevel(level);

                if (badgeConfig != null)
                {
                    if (badgeConfig.badgeSprite != null)
                    {
                        levelBadgeImage.sprite = badgeConfig.badgeSprite;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the appropriate badge config for given level
        /// </summary>
        private LevelBadgeConfig GetBadgeForLevel(int level)
        {
            if (levelBadges == null || levelBadges.Length == 0)
                return null;

            // ZnajdŸ badge z najwy¿szym minLevel który jest <= level
            LevelBadgeConfig bestMatch = null;

            foreach (var badge in levelBadges)
            {
                if (level >= badge.minLevel)
                {
                    if (bestMatch == null || badge.minLevel > bestMatch.minLevel)
                    {
                        bestMatch = badge;
                    }
                }
            }

            return bestMatch;
        }

        private void UpdateXPDisplay(int currentXP, int requiredXP, bool animate = true)
        {
            float progress = (float)currentXP / Mathf.Max(1, requiredXP);

            targetXPValue = progress;

            if (animate && animateXPBar)
            {
                isAnimatingXP = true;
            }
            else
            {
                currentXPValue = progress;
                if (xpSlider != null)
                    xpSlider.value = progress;
            }

            // Update text
            if (xpText != null)
                xpText.text = $"{currentXP} / {requiredXP}";

            if (xpPercentText != null)
                xpPercentText.text = $"{Mathf.RoundToInt(progress * 100)}%";

            // Update color based on fullness
            if (xpFillImage != null)
            {
                xpFillImage.color = Color.Lerp(xpBarColor, xpBarFullColor, progress);
            }
        }

        private void UpdateGoldDisplay(int gold)
        {
            if (goldText != null)
                goldText.text = FormatNumber(gold);
        }

        private void UpdateCrystalsDisplay(int crystals)
        {
            if (crystalsText != null)
                crystalsText.text = FormatNumber(crystals);
        }

        private void UpdateRankDisplay()
        {
            if (PlayerCollection.Instance == null) return;

            if (rankText != null)
            {
                rankText.text = PlayerCollection.Instance.GetRankName();
                rankText.color = PlayerCollection.Instance.GetRankColor();
            }

            if (eloText != null)
            {
                eloText.text = $"{PlayerCollection.Instance.GetElo()} ELO";
            }
        }

        // ==========================================
        // LEVEL UP ANIMATION
        // ==========================================

        private void ShowLevelUpAnimation(int newLevel)
        {
            // Play sound
            if (levelUpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(levelUpSound);
            }

            // Show popup
            if (levelUpPopup != null)
            {
                if (levelUpText != null)
                {
                    // Poka¿ te¿ nazwê nowego badge'a jeœli siê zmieni³
                    var badge = GetBadgeForLevel(newLevel);
                    levelUpText.text = $"LEVEL UP!\nLevel {newLevel}";
                }

                levelUpPopup.SetActive(true);
                StartCoroutine(HideLevelUpPopup());
            }

            // Animate level text
            if (levelText != null)
            {
                StartCoroutine(PunchScale(levelText.transform));
            }

            // Animate badge
            if (levelBadgeImage != null)
            {
                StartCoroutine(PunchScale(levelBadgeImage.transform));
            }
        }

        private IEnumerator HideLevelUpPopup()
        {
            yield return new WaitForSeconds(levelUpDisplayTime);

            if (levelUpPopup != null)
                levelUpPopup.SetActive(false);
        }

        private IEnumerator PunchScale(Transform target)
        {
            Vector3 originalScale = target.localScale;
            Vector3 punchScale = originalScale * 1.3f;

            float duration = 0.3f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2);
                target.localScale = Vector3.Lerp(originalScale, punchScale, t);
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2);
                target.localScale = Vector3.Lerp(punchScale, originalScale, t);
                yield return null;
            }

            target.localScale = originalScale;
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:F1}M";
            if (number >= 1000)
                return $"{number / 1000f:F1}K";
            return number.ToString();
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Refresh UI")]
        private void DebugRefresh()
        {
            RefreshAllUI();
        }

        [ContextMenu("Test Level Up Animation")]
        private void DebugLevelUp()
        {
            ShowLevelUpAnimation(99);
        }

        [ContextMenu("Print Badge Info")]
        private void DebugPrintBadges()
        {
            if (levelBadges == null || levelBadges.Length == 0)
            {
                Debug.Log("[PlayerLevelUI] No badges configured!");
                return;
            }

            Debug.Log($"[PlayerLevelUI] {levelBadges.Length} badges configured:");
            foreach (var badge in levelBadges)
            {
                Debug.Log($"  Level {badge.minLevel}+: (sprite: {(badge.badgeSprite != null ? badge.badgeSprite.name : "NULL")})");
            }

            int currentLevel = PlayerCollection.Instance?.GetLevel() ?? 1;
            var currentBadge = GetBadgeForLevel(currentLevel);
            Debug.Log($"Current level {currentLevel} uses badge: {currentBadge.badgeSprite.name ?? "NONE"}");
        }
    }
}