// Assets/PrzemekSkrypty/Quest/QuestSlotUI.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ElementumDefense.Progression;
using ElementumDefense.Lootbox;
using System;

public class QuestSlotUI : MonoBehaviour
{
    [Header("Standard Content")]
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Button claimButton;

    [Header("Lootbox Reward Display")]
    [SerializeField] private GameObject lootboxRewardContainer;
    [SerializeField] private Image lootboxIcon;
    [SerializeField] private TextMeshProUGUI lootboxNameText;
    [SerializeField] private Image lootboxGlow;

    [Header("Claimed Overlay")]
    [SerializeField] private GameObject claimedOverlay;
    [SerializeField] private TextMeshProUGUI nextQuestTimerText;

    [Header("Tier Badge")]
    [SerializeField] private Image tierBadge;

    [Header("Tier Colors")]
    [SerializeField] private Color dailyColor = new Color(0.5f, 0.8f, 0.3f);
    [SerializeField] private Color weeklyColor = new Color(0.3f, 0.5f, 1f);
    [SerializeField] private Color specialColor = new Color(1f, 0.5f, 0f);

    private Quest myQuest;
    private bool isTimerActive = false;

    public void Setup(Quest quest)
    {
        myQuest = quest;

        if (descriptionText != null)
            descriptionText.text = quest.description;

        if (progressText != null)
            progressText.text = $"{quest.currentProgress}/{quest.targetAmount}";

        if (progressBar != null)
        {
            progressBar.maxValue = 1f;
            progressBar.value = quest.GetProgress01();
        }

        UpdateRewardDisplay();
        UpdateTierBadge();
        UpdateState();
    }

    private void UpdateRewardDisplay()
    {
        if (rewardText != null)
        {
            string rewards = "";

            if (myQuest.rewardGold > 0)
                rewards += $"{myQuest.rewardGold} Gold ";

            if (myQuest.rewardXP > 0 && myQuest.rewardGold > 0)
                rewards += "\n";

            if (myQuest.rewardXP > 0)
                rewards += $"{myQuest.rewardXP} XP";

            rewardText.text = rewards;
        }

        if (lootboxRewardContainer != null)
        {
            bool hasLootbox = myQuest.HasLootboxReward;
            lootboxRewardContainer.SetActive(hasLootbox);

            if (hasLootbox && myQuest.rewardLootbox != null)
            {
                if (lootboxIcon != null && myQuest.rewardLootbox.lootboxIcon != null)
                    lootboxIcon.sprite = myQuest.rewardLootbox.lootboxIcon;

                if (lootboxNameText != null)
                    lootboxNameText.text = myQuest.rewardLootbox.lootboxName;

                if (lootboxGlow != null)
                    lootboxGlow.color = myQuest.rewardLootbox.GetRarityColor();
            }
        }
    }

    private void UpdateTierBadge()
    {
        if (tierBadge == null) return;

        Color tierColor;

        switch (myQuest.tier)
        {
            case QuestTier.Weekly:
                tierColor = weeklyColor;
                break;
            case QuestTier.Special:
                tierColor = specialColor;
                break;
            default:
                tierColor = dailyColor;
                break;
        }

        if (tierBadge != null)
            tierBadge.color = tierColor;

    }

    private void UpdateState()
    {
        if (myQuest.isClaimed)
        {
            if (contentRoot != null) contentRoot.SetActive(false);
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (lootboxRewardContainer != null) lootboxRewardContainer.SetActive(false);

            if (claimedOverlay != null)
            {
                claimedOverlay.SetActive(true);
                isTimerActive = true;
            }
        }
        else
        {
            if (contentRoot != null) contentRoot.SetActive(true);
            if (claimedOverlay != null) claimedOverlay.SetActive(false);
            isTimerActive = false;

            if (myQuest.isCompleted)
            {
                if (claimButton != null)
                {
                    claimButton.gameObject.SetActive(true);
                    claimButton.interactable = true;
                    claimButton.onClick.RemoveAllListeners();
                    claimButton.onClick.AddListener(OnClaimClicked);

                    TextMeshProUGUI buttonText = claimButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = myQuest.HasLootboxReward ? "CLAIM BOX!" : "CLAIM";
                    }
                }
            }
            else
            {
                if (claimButton != null) claimButton.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (isTimerActive && nextQuestTimerText != null)
        {
            UpdateTimerText();
        }
    }

    /// <summary>
    /// Updates timer based on quest tier
    /// </summary>
    private void UpdateTimerText()
    {
        TimeSpan timeRemaining;
        string label;

        switch (myQuest.tier)
        {
            case QuestTier.Weekly:
                // Czas do nastêpnego poniedzia³ku o pó³nocy
                timeRemaining = GetTimeUntilNextMonday();
                label = "New weekly in:";
                break;

            case QuestTier.Special:
                // Special questy mog¹ mieæ w³asny timer lub nie resetowaæ siê
                // Na razie pokazujemy "Event ended" lub czas do pó³nocy
                timeRemaining = GetTimeUntilMidnight();
                label = "Event ends in:";
                break;

            default: // Daily
                timeRemaining = GetTimeUntilMidnight();
                label = "New quest in:";
                break;
        }

        // Formatowanie
        if (timeRemaining.TotalDays >= 1)
        {
            // Poka¿ dni i godziny
            nextQuestTimerText.text = $"{label} {timeRemaining.Days}d {timeRemaining.Hours}h";
        }
        else
        {
            // Poka¿ godziny:minuty:sekundy
            nextQuestTimerText.text = $"{label} {timeRemaining:hh\\:mm\\:ss}";
        }
    }

    /// <summary>
    /// Gets time until midnight (daily reset)
    /// </summary>
    private TimeSpan GetTimeUntilMidnight()
    {
        DateTime now = DateTime.Now;
        DateTime midnight = DateTime.Today.AddDays(1);
        return midnight - now;
    }

    /// <summary>
    /// Gets time until next Monday at midnight (weekly reset)
    /// </summary>
    private TimeSpan GetTimeUntilNextMonday()
    {
        DateTime now = DateTime.Now;

        // Oblicz ile dni do nastêpnego poniedzia³ku
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;

        // Jeœli dziœ jest poniedzia³ek, czekamy do nastêpnego
        if (daysUntilMonday == 0)
            daysUntilMonday = 7;

        DateTime nextMonday = DateTime.Today.AddDays(daysUntilMonday);
        return nextMonday - now;
    }

    private void OnClaimClicked()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ClaimReward(myQuest);
        }
    }
}