using UnityEngine;
using ElementumDefense.Progression;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private GameObject questSlotPrefab;

    private void Start()
    {
        // Subskrypcja na starcie (dla pewnoœci)
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestListUpdated += RefreshUI;
            RefreshUI(); // Pierwsze odœwie¿enie
        }
    }

    private void OnDestroy()
    {
        // Sprz¹tanie subskrypcji przy niszczeniu obiektu
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestListUpdated -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        if (QuestManager.Instance == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        foreach (var quest in QuestManager.Instance.activeQuests)
        {
            GameObject slotObj = Instantiate(questSlotPrefab, container);
            QuestSlotUI slotScript = slotObj.GetComponent<QuestSlotUI>();

            if (slotScript != null)
            {
                slotScript.Setup(quest);
            }
        }

        Debug.Log("[QuestUI] UI Refreshed from Event.");
    }
}