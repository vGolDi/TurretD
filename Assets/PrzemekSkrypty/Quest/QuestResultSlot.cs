using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ElementumDefense.Progression;
using UnityEditor.PackageManager.Requests;

public class QuestResultSlot : MonoBehaviour
{
    //[SerializeField] private TextMeshProUGUI descriptionText;
    //[SerializeField] private TextMeshProUGUI progressText;
    //[SerializeField] private Slider progressBar;
    //[SerializeField] private GameObject completedCheckmark;
    //[SerializeField] private GameObject claimedBadge; 

    [Header("Standard Content")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private GameObject claimedBadge;

    [Header("Lootbox Reward Display")]
    [SerializeField] private GameObject lootboxRewardContainer;
    [SerializeField] private TextMeshProUGUI lootboxNameText;

    [Header("Tier Badge")]
    [SerializeField] private Image tierBadge;

    [Header("Tier Colors")]
    [SerializeField] private Color dailyColor = new Color(0.5f, 0.8f, 0.3f);
    [SerializeField] private Color weeklyColor = new Color(0.3f, 0.5f, 1f);
    [SerializeField] private Color specialColor = new Color(1f, 0.5f, 0f);

    private Quest myQuest;

    public void Setup(Quest quest)
    {
        myQuest = quest;

        if (descriptionText != null) descriptionText.text = quest.description;

        if (progressText != null)
            progressText.text = $"{quest.currentProgress}/{quest.targetAmount}";

        if (progressBar != null)
            progressBar.value = quest.GetProgress01();

        if (claimedBadge != null)
            claimedBadge.SetActive(quest.isClaimed);


        UpdateRewardDisplay();
        UpdateTierBadge();
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
                if (lootboxNameText != null)
                    lootboxNameText.text = myQuest.rewardLootbox.lootboxName;;
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
}