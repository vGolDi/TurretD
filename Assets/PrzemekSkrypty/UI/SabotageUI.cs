using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ElementumDefense.Cards
{
    public class SabotageUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject sabotageDraftPanel;
        [SerializeField] private GameObject revealPanel;

        [Header("Draft UI (3 choices)")]
        [SerializeField] private GameObject[] sabotageSlotObjects = new GameObject[3];
        [SerializeField] private TextMeshProUGUI draftTimerText;

        [Header("Reveal UI")]
        [SerializeField] private Transform revealContainer;
        [SerializeField] private GameObject revealCardPrefab;
        [SerializeField] private TextMeshProUGUI revealTimerText;
        [SerializeField] private TextMeshProUGUI revealHeaderText;

        private SabotageDraftManager sabotageDraftManager;
        private bool isInitialized = false;

        // ========== NOWE: Retry system ==========
        private float initializationRetryTimer = 0f;
        private const float RETRY_INTERVAL = 0.5f;
        // ========================================

        private void Start()
        {
            Debug.Log("[SabotageUI] Waiting for SabotageDraftManager to spawn...");
            HideAllPanels();
        }

        private void Update()
        {
            // ========== NOWE: Retry initialization ==========
            if (!isInitialized)
            {
                initializationRetryTimer += Time.deltaTime;

                if (initializationRetryTimer >= RETRY_INTERVAL)
                {
                    initializationRetryTimer = 0f;
                    TryInitialize();
                }

                return;
            }
            // ================================================
        }

        private void TryInitialize()
        {
            sabotageDraftManager = SabotageDraftManager.Instance;

            if (sabotageDraftManager == null)
            {
                Debug.LogWarning("[SabotageUI] Still waiting for SabotageDraftManager...");
                return;
            }

            // Subscribe to events
            sabotageDraftManager.OnSabotageOffered += ShowSabotageDraft;
            sabotageDraftManager.OnDraftTimerUpdate += UpdateDraftTimer;
            sabotageDraftManager.OnRevealPhaseStart += ShowRevealPhase;
            sabotageDraftManager.OnRevealPhaseEnd += HideRevealPhase;
            sabotageDraftManager.OnDraftTimeout += OnTimeout;

            isInitialized = true;

            Debug.Log("[SabotageUI] ✅ Successfully initialized!");
        }

        private void OnDestroy()
        {
            if (sabotageDraftManager != null)
            {
                sabotageDraftManager.OnSabotageOffered -= ShowSabotageDraft;
                sabotageDraftManager.OnDraftTimerUpdate -= UpdateDraftTimer;
                sabotageDraftManager.OnRevealPhaseStart -= ShowRevealPhase;
                sabotageDraftManager.OnRevealPhaseEnd -= HideRevealPhase;
                sabotageDraftManager.OnDraftTimeout -= OnTimeout;
            }
        }

        // ... rest of code (ShowSabotageDraft, UpdateSabotageSlot, etc.) ...

        private void ShowSabotageDraft(SabotageCardData[] cards)
        {
            HideAllPanels();

            if (sabotageDraftPanel != null)
            {
                sabotageDraftPanel.SetActive(true);
            }

            for (int i = 0; i < sabotageSlotObjects.Length && i < cards.Length; i++)
            {
                if (sabotageSlotObjects[i] != null && cards[i] != null)
                {
                    UpdateSabotageSlot(sabotageSlotObjects[i], cards[i], i);
                }
            }

            Debug.Log("[SabotageUI] Showing sabotage draft");
        }

        private void UpdateSabotageSlot(GameObject slotObj, SabotageCardData sabotage, int index)
        {
            Image sabotageIcon = slotObj.transform.Find("SabotageIcon")?.GetComponent<Image>();
            TextMeshProUGUI sabotageName = slotObj.transform.Find("SabotageName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI description = slotObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI durationText = slotObj.transform.Find("DurationText")?.GetComponent<TextMeshProUGUI>();
            Image rarityBorder = slotObj.transform.Find("RarityBorder")?.GetComponent<Image>();
            Button selectBtn = slotObj.GetComponent<Button>();

            if (sabotageIcon != null)
                sabotageIcon.sprite = sabotage.sabotageIcon;

            if (sabotageName != null)
                sabotageName.text = sabotage.sabotageName;

            if (description != null)
                description.text = sabotage.description;

            if (durationText != null)
                durationText.text = sabotage.GetDurationText();

            if (rarityBorder != null)
                rarityBorder.color = sabotage.GetRarityColor();

            if (selectBtn != null)
            {
                selectBtn.onClick.RemoveAllListeners();
                int capturedIndex = index;
                selectBtn.onClick.AddListener(() => OnSabotageSelected(capturedIndex));
            }
        }

        private void OnSabotageSelected(int choiceIndex)
        {
            sabotageDraftManager.SelectSabotage(choiceIndex);

            if (sabotageDraftPanel != null)
            {
                sabotageDraftPanel.SetActive(false);
            }

            Debug.Log($"[SabotageUI] Selected sabotage {choiceIndex}");
        }

        private void UpdateDraftTimer(float timeRemaining)
        {
            if (draftTimerText != null && sabotageDraftPanel.activeSelf)
            {
                draftTimerText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}s";

                if (timeRemaining <= 5f)
                    draftTimerText.color = Color.red;
                else
                    draftTimerText.color = Color.white;
            }
        }

        private void ShowRevealPhase(Dictionary<int, SabotageCardData> playerSelections)
        {
            HideAllPanels();

            if (revealPanel != null)
            {
                revealPanel.SetActive(true);
            }

            if (revealHeaderText != null)
            {
                revealHeaderText.text = "SABOTAGES REVEALED!";
            }

            if (revealContainer != null)
            {
                foreach (Transform child in revealContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (var kvp in playerSelections)
            {
                int actorNumber = kvp.Key;
                SabotageCardData sabotage = kvp.Value;

                Photon.Realtime.Player player = Photon.Pun.PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
                string playerName = player?.NickName ?? $"Player{actorNumber}";

                if (revealCardPrefab != null && revealContainer != null)
                {
                    GameObject revealCard = Instantiate(revealCardPrefab, revealContainer);

                    TextMeshProUGUI playerNameText = revealCard.transform.Find("PlayerName")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI sabotageNameText = revealCard.transform.Find("SabotageName")?.GetComponent<TextMeshProUGUI>();
                    Image sabotageIcon = revealCard.transform.Find("Icon")?.GetComponent<Image>();

                    if (playerNameText != null)
                        playerNameText.text = playerName;

                    if (sabotageNameText != null)
                        sabotageNameText.text = sabotage.sabotageName;

                    if (sabotageIcon != null)
                        sabotageIcon.sprite = sabotage.sabotageIcon;
                }
            }

            Debug.Log("[SabotageUI] Showing reveal phase");
            StartCoroutine(RevealCountdown());
        }

        private System.Collections.IEnumerator RevealCountdown()
        {
            float timeRemaining = 5f;

            while (timeRemaining > 0f && revealPanel.activeSelf)
            {
                if (revealTimerText != null)
                {
                    revealTimerText.text = $"Wave starting in: {Mathf.CeilToInt(timeRemaining)}s";
                }

                timeRemaining -= Time.deltaTime;
                yield return null;
            }
        }

        private void HideRevealPhase()
        {
            if (revealPanel != null)
            {
                revealPanel.SetActive(false);
            }

            Debug.Log("[SabotageUI] Hiding reveal phase");
        }

        private void OnTimeout()
        {
            Debug.Log("[SabotageUI] Sabotage draft TIMEOUT!");
        }

        private void HideAllPanels()
        {
            if (sabotageDraftPanel != null) sabotageDraftPanel.SetActive(false);
            if (revealPanel != null) revealPanel.SetActive(false);
        }
    }
}