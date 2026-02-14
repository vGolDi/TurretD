// Assets/PrzemekSkrypty/Quest/QuestUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ElementumDefense.Progression;
using System.Collections.Generic;
using System.Linq;

public class QuestUI : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject questSlotPrefab;

    [Header("Scroll View")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewportRect;

    [Header("Scroll Indicators")]
    [SerializeField] private GameObject scrollUpIndicator;
    [SerializeField] private GameObject scrollDownIndicator;

    [Header("Settings")]
    //[SerializeField] private float slotHeight = 100f; // Wysokoœæ jednego slotu
    [SerializeField] private int visibleSlots = 3;    // Ile slotów widocznych
    [SerializeField] private float scrollIndicatorThreshold = 0.05f;

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private QuestTier? currentFilter = null;

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestListUpdated += RefreshUI;
            RefreshUI();
        }

        SetupScrollListeners();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestListUpdated -= RefreshUI;
        }
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    private void SetupScrollListeners()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }
    }

    /// <summary>
    /// Filters quests by tier
    /// </summary>
    public void FilterQuests(QuestTier? tier)
    {
        currentFilter = tier;
        RefreshUI();

        // Update tab visuals
        UpdateTabVisuals();
    }

    private void UpdateTabVisuals()
    {
        // Mo¿esz tu zmieniæ kolory/stany przycisków zak³adek
        // np. podœwietliæ aktywn¹ zak³adkê
    }

    public void RefreshUI()
    {
        if (QuestManager.Instance == null) return;

        // Clear old slots
        foreach (var slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        spawnedSlots.Clear();

        // Get quests (filtered or all)
        List<Quest> questsToShow;

        if (currentFilter.HasValue)
        {
            questsToShow = QuestManager.Instance.GetQuestsByTier(currentFilter.Value);
        }
        else
        {
            questsToShow = QuestManager.Instance.activeQuests;
        }

        // Sort: unclaimed first, then by tier (daily, weekly, special)
        questsToShow = questsToShow
            .OrderBy(q => q.isClaimed ? 1 : 0)
            .ThenBy(q => q.tier)
            .ThenBy(q => q.isCompleted ? 0 : 1)
            .ToList();

        // Spawn quest slots
        foreach (var quest in questsToShow)
        {
            GameObject slotObj = Instantiate(questSlotPrefab, container);
            spawnedSlots.Add(slotObj);

            QuestSlotUI slotScript = slotObj.GetComponent<QuestSlotUI>();
            if (slotScript != null)
            {
                slotScript.Setup(quest);
            }
        }

    

        // Update scroll indicators
        UpdateScrollIndicators();

        // Force layout rebuild
        StartCoroutine(ForceLayoutRebuild());

        Debug.Log($"[QuestUI] Refreshed - showing {questsToShow.Count} quests");
    }

    

    private void OnScrollValueChanged(Vector2 scrollPosition)
    {
        UpdateScrollIndicators();
    }

    private void UpdateScrollIndicators()
    {
        if (scrollRect == null) return;

        int totalQuests = spawnedSlots.Count;
        bool canScroll = totalQuests > visibleSlots;

        // Scroll up indicator (pokazuje siê gdy nie jesteœmy na górze)
        if (scrollUpIndicator != null)
        {
            bool showUp = canScroll && scrollRect.verticalNormalizedPosition < (1f - scrollIndicatorThreshold);
            scrollUpIndicator.SetActive(showUp);
        }

        // Scroll down indicator (pokazuje siê gdy nie jesteœmy na dole)
        if (scrollDownIndicator != null)
        {
            bool showDown = canScroll && scrollRect.verticalNormalizedPosition > scrollIndicatorThreshold;
            scrollDownIndicator.SetActive(showDown);
        }

    }

    private System.Collections.IEnumerator ForceLayoutRebuild()
    {
        yield return null;

        if (container != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
        }

        // Reset scroll to top
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        UpdateScrollIndicators();
    }

    // ==========================================
    // PUBLIC API
    // ==========================================

    /// <summary>
    /// Scrolls to show a specific quest
    /// </summary>
    public void ScrollToQuest(Quest quest)
    {
        int index = QuestManager.Instance?.activeQuests.IndexOf(quest) ?? -1;
        if (index < 0 || scrollRect == null) return;

        float normalizedPosition = 1f - ((float)index / Mathf.Max(1, spawnedSlots.Count - visibleSlots));
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
    }

    /// <summary>
    /// Scrolls to first unclaimed quest
    /// </summary>
    public void ScrollToFirstUnclaimed()
    {
        var unclaimed = QuestManager.Instance?.activeQuests.FirstOrDefault(q => !q.isClaimed);
        if (unclaimed != null)
        {
            ScrollToQuest(unclaimed);
        }
    }
}