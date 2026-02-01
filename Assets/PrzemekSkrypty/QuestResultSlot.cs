using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ElementumDefense.Progression;

public class QuestResultSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject completedCheckmark;
    [SerializeField] private GameObject claimedBadge; // NOWE: Napis/Ikonka "CLAIMED"

    public void Setup(Quest quest)
    {
        if (descriptionText != null) descriptionText.text = quest.description;

        if (progressText != null)
            progressText.text = $"{quest.currentProgress}/{quest.targetAmount}";

        if (progressBar != null)
            progressBar.value = quest.GetProgress01();

        // Obs³uga checkmarka (Zrobione ale nie odebrane)
        if (completedCheckmark != null)
            completedCheckmark.SetActive(quest.isCompleted && !quest.isClaimed);

        // Obs³uga badge'a (Odebrane)
        if (claimedBadge != null)
            claimedBadge.SetActive(quest.isClaimed);
    }
}