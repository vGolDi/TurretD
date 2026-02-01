using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ElementumDefense.Progression;
using System; // Potrzebne do DateTime

public class QuestSlotUI : MonoBehaviour
{
    [Header("Standard Content")]
    [SerializeField] private GameObject contentRoot; // Przypisz tutaj "wszystko co ma znikn¹æ" (opisy, paski)
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Button claimButton;

    [Header("Claimed Overlay")]
    [SerializeField] private GameObject claimedOverlay; // Panel który siê pojawia po odebraniu
    [SerializeField] private TextMeshProUGUI nextQuestTimerText; // Tekst "Next quest in: HH:MM:SS"

    private Quest myQuest;
    private bool isTimerActive = false;

    public void Setup(Quest quest)
    {
        myQuest = quest;

        // Ustawienie tekstów standardowych
        if (descriptionText != null) descriptionText.text = quest.description;
        if (progressText != null) progressText.text = $"{quest.currentProgress}/{quest.targetAmount}";
        if (rewardText != null) rewardText.text = $"{quest.rewardGold} Gold | {quest.rewardXP} XP";
        if (progressBar != null)
        {
            progressBar.maxValue = 1f;
            progressBar.value = quest.GetProgress01();
        }

        UpdateState();
    }

    private void UpdateState()
    {
        if (myQuest.isClaimed)
        {
            // STAN: ODEBRANO
            if (contentRoot != null) contentRoot.SetActive(false); // Ukryj stare rzeczy
            if (claimButton != null) claimButton.gameObject.SetActive(false);

            if (claimedOverlay != null)
            {
                claimedOverlay.SetActive(true); // Poka¿ zaœlepkê "Claimed"
                isTimerActive = true; // W³¹cz odliczanie
            }
        }
        else
        {
            // STAN: W TRAKCIE / UKOÑCZONO
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
        // Logika odliczania do pó³nocy
        if (isTimerActive && nextQuestTimerText != null)
        {
            TimeSpan timeToMidnight = DateTime.Today.AddDays(1) - DateTime.Now;
            nextQuestTimerText.text = $"New quest in: {timeToMidnight:hh\\:mm\\:ss}";
        }
    }

    private void OnClaimClicked()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ClaimReward(myQuest);
            // UpdateState wywo³a siê automatycznie, bo QuestUI odœwie¿y ca³¹ listê po evencie z Managera
        }
    }
}