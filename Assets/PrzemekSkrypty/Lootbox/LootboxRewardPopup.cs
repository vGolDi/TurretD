// Assets/PrzemekSkrypty/UI/LootboxRewardPopup.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ElementumDefense.Lootbox;
using System.Collections;
using System.Collections.Generic;

namespace ElementumDefense.UI
{
    public class LootboxRewardPopup : MonoBehaviour
    {
        public static LootboxRewardPopup Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private Image lootboxIcon;
        [SerializeField] private Image glowEffect;
        [SerializeField] private TMP_Text lootboxNameText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private Button okButton;
        [SerializeField] private Button openNowButton;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 4f;
        [SerializeField] private bool autoHide = true;
        [SerializeField] private float fadeInTime = 0.3f;
        [SerializeField] private float fadeOutTime = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioClip rewardSound;
        [SerializeField] private AudioClip legendaryRewardSound;

        private AudioSource audioSource;
        private LootboxData pendingLootbox;
        private Queue<RewardQueueItem> rewardQueue = new Queue<RewardQueueItem>();
        private bool isShowing = false;
        private CanvasGroup canvasGroup;

        // NOWE - Public property do sprawdzania stanu
        public bool IsShowing => isShowing;

        private class RewardQueueItem
        {
            public LootboxData lootbox;
            public string reason;
        }

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            if (popupPanel != null)
            {
                canvasGroup = popupPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = popupPanel.AddComponent<CanvasGroup>();

                popupPanel.SetActive(false);
            }
        }

        private void Start()
        {
            SubscribeToEvents();
            SetupButtons();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (Instance == this) Instance = null;
        }

        // ==========================================
        // EVENT SUBSCRIPTION
        // ==========================================

        private void SubscribeToEvents()
        {
            if (LootboxRewardGiver.Instance != null)
            {
                LootboxRewardGiver.Instance.OnLootboxRewarded -= OnLootboxRewardedWithReason;
                LootboxRewardGiver.Instance.OnLootboxRewarded += OnLootboxRewardedWithReason;
                Debug.Log("[LootboxRewardPopup] Subscribed to LootboxRewardGiver.OnLootboxRewarded");
            }
            else
            {
                Debug.LogWarning("[LootboxRewardPopup] LootboxRewardGiver not found!");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (LootboxRewardGiver.Instance != null)
                LootboxRewardGiver.Instance.OnLootboxRewarded -= OnLootboxRewardedWithReason;
        }

        private void SetupButtons()
        {
            if (okButton != null)
            {
                okButton.onClick.RemoveAllListeners();
                okButton.onClick.AddListener(OnOkButtonClicked); // ZMIENIONE
            }

            if (openNowButton != null)
            {
                openNowButton.onClick.RemoveAllListeners();
                openNowButton.onClick.AddListener(OnOpenNowButtonClicked); // ZMIENIONE
            }
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private void OnLootboxRewardedWithReason(LootboxData lootbox, string reason)
        {
            Debug.Log($"[LootboxRewardPopup] Received reward event: {lootbox?.lootboxName} - {reason}");
            QueueReward(lootbox, reason);
        }

        // ==========================================
        // QUEUE SYSTEM
        // ==========================================

        public void QueueReward(LootboxData lootbox, string reason)
        {
            if (lootbox == null)
            {
                Debug.LogWarning("[LootboxRewardPopup] Tried to queue null lootbox");
                return;
            }

            rewardQueue.Enqueue(new RewardQueueItem { lootbox = lootbox, reason = reason });
            Debug.Log($"[LootboxRewardPopup] Queued: {lootbox.lootboxName} ({reason}). Queue size: {rewardQueue.Count}");

            if (!isShowing)
            {
                ShowNextReward();
            }
        }

        private void ShowNextReward()
        {
            if (rewardQueue.Count == 0)
            {
                isShowing = false;
                return;
            }

            var item = rewardQueue.Dequeue();
            ShowPopup(item.lootbox, item.reason);
        }

        // ==========================================
        // POPUP DISPLAY
        // ==========================================

        public void ShowPopup(LootboxData lootbox, string reason)
        {
            if (lootbox == null || popupPanel == null)
            {
                Debug.LogError("[LootboxRewardPopup] Cannot show - lootbox or panel is null");
                ShowNextReward();
                return;
            }

            pendingLootbox = lootbox;
            isShowing = true;

            Debug.Log($"[LootboxRewardPopup] Showing popup for: {lootbox.lootboxName}");

            // Setup visuals
            if (lootboxIcon != null && lootbox.lootboxIcon != null)
                lootboxIcon.sprite = lootbox.lootboxIcon;

            if (glowEffect != null)
            {
                glowEffect.color = lootbox.GetRarityColor();
                glowEffect.gameObject.SetActive(true);
            }

            if (lootboxNameText != null)
            {
                lootboxNameText.text = lootbox.lootboxName;
               // lootboxNameText.color = lootbox.GetRarityColor();
            }

            if (reasonText != null)
                reasonText.text = reason;

            popupPanel.SetActive(true);
            StartCoroutine(FadeIn());

            PlayRewardSound(lootbox);

            if (autoHide)
            {
                StartCoroutine(AutoHideCoroutine());
            }
        }

        private void PlayRewardSound(LootboxData lootbox)
        {
            if (audioSource == null) return;

            AudioClip clip = rewardSound;

            if (lootbox.rarity == LootboxRarity.Legendary && legendaryRewardSound != null)
            {
                clip = legendaryRewardSound;
            }

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // ==========================================
        // ANIMATIONS
        // ==========================================

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < fadeInTime)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInTime);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut(bool closeQuestPanel = false)
        {
            if (canvasGroup == null)
            {
                popupPanel.SetActive(false);
                isShowing = false;

                if (closeQuestPanel)
                {
                    CloseQuestPanel();
                }

                ShowNextReward();
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < fadeOutTime)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutTime);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            popupPanel.SetActive(false);
            isShowing = false;

            // NOWE - zamknij quest panel tylko jeœli "Open Now"
            if (closeQuestPanel)
            {
                CloseQuestPanel();
            }

            ShowNextReward();
        }

        private IEnumerator AutoHideCoroutine()
        {
            yield return new WaitForSecondsRealtime(showDuration);

            if (isShowing)
            {
                // Auto-hide dzia³a jak "OK" - nie zamyka panelu questów
                HidePopup(closeQuestPanel: false);
            }
        }

        // ==========================================
        // USER ACTIONS - NOWE METODY
        // ==========================================

        /// <summary>
        /// Called when OK button is clicked
        /// Panel questów ZOSTAJE otwarty
        /// </summary>
        private void OnOkButtonClicked()
        {
            HidePopup(closeQuestPanel: false);
        }

        /// <summary>
        /// Called when Open Now button is clicked
        /// Panel questów siê ZAMYKA
        /// </summary>
        private void OnOpenNowButtonClicked()
        {
            OpenLootboxNow();
        }

        /// <summary>
        /// Hide popup
        /// </summary>
        /// <param name="closeQuestPanel">Whether to also close the quest panel</param>
        public void HidePopup(bool closeQuestPanel = false)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOut(closeQuestPanel));
        }

        /// <summary>
        /// Opens the lootbox immediately and closes quest panel
        /// </summary>
        private void OpenLootboxNow()
        {
            if (pendingLootbox == null) return;

            LootboxData lootboxToOpen = pendingLootbox;
            pendingLootbox = null;

            // Hide popup and close quest panel
            StopAllCoroutines();
            popupPanel.SetActive(false);
            isShowing = false;

            // Close quest panel
            CloseQuestPanel();

            // Find and open LootboxUI
            LootboxUI lootboxUI = FindFirstObjectByType<LootboxUI>(FindObjectsInactive.Include);
            if (lootboxUI != null)
            {
                lootboxUI.OpenLootboxMenu();
                StartCoroutine(DelayedOpenLootbox(lootboxUI, lootboxToOpen));
            }
            else
            {
                Debug.LogWarning("[LootboxRewardPopup] LootboxUI not found in scene!");
            }

            ShowNextReward();
        }

        /// <summary>
        /// Closes the quest panel if it's open
        /// </summary>
        private void CloseQuestPanel()
        {
            if (QuestPanelSlider.Instance != null && QuestPanelSlider.Instance.IsOpen)
            {
                QuestPanelSlider.Instance.Close();
                Debug.Log("[LootboxRewardPopup] Closed quest panel");
            }
        }

        private IEnumerator DelayedOpenLootbox(LootboxUI ui, LootboxData lootbox)
        {
            yield return new WaitForSecondsRealtime(0.3f);
            ui.TryOpenLootbox(lootbox);
        }

        // ==========================================
        // MANUAL TESTING
        // ==========================================

        [ContextMenu("Test Show Common Lootbox")]
        private void TestShowCommon()
        {
            LootboxData[] lootboxes = Resources.LoadAll<LootboxData>("Lootboxes");
            var common = System.Array.Find(lootboxes, l => l.rarity == LootboxRarity.Common);
            if (common != null)
            {
                QueueReward(common, "Test Reward!");
            }
        }

        [ContextMenu("Test Show Legendary Lootbox")]
        private void TestShowLegendary()
        {
            LootboxData[] lootboxes = Resources.LoadAll<LootboxData>("Lootboxes");
            var legendary = System.Array.Find(lootboxes, l => l.rarity == LootboxRarity.Legendary);
            if (legendary != null)
            {
                QueueReward(legendary, "LEGENDARY Test!");
            }
        }

        [ContextMenu("Test Queue Multiple")]
        private void TestQueueMultiple()
        {
            LootboxData[] lootboxes = Resources.LoadAll<LootboxData>("Lootboxes");
            foreach (var lb in lootboxes)
            {
                QueueReward(lb, $"Test: {lb.lootboxName}");
            }
        }
    }
}