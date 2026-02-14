// Assets/PrzemekSkrypty/UI/QuestPanelSlider.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using ElementumDefense.Progression;
using System.Linq;

namespace ElementumDefense.UI
{
    public class QuestPanelSlider : MonoBehaviour
    {
        public static QuestPanelSlider Instance { get; private set; } // DODANE - singleton

        [Header("References")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Button tabButton;
        [SerializeField] private RectTransform tabButtonRect;

        [Header("Slide Settings")]
        [SerializeField] private float panelWidth = 350f;
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Tab Button Settings")]
        [SerializeField] private bool rotateTabWhenOpen = true;
        [SerializeField] private float tabRotationAngle = 180f;

        [Header("Slide Direction")]
        [SerializeField] private SlideDirection direction = SlideDirection.FromRight;

        [Header("Notification Badge")]
        [SerializeField] private GameObject notificationBadge;
        [SerializeField] private TextMeshProUGUI notificationCountText;

        [Header("Close Options")]
        [SerializeField] private bool closeOnClickOutside = true;

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        public enum SlideDirection
        {
            FromRight,
            FromLeft
        }

        // State
        private bool isOpen = false;
        private bool isAnimating = false;
        private bool closeTemporarilyDisabled = false; // DODANE - blokada zamykania
        private Vector2 closedPosition;
        private Vector2 openPosition;
        private AudioSource audioSource;

        // Events
        public System.Action OnPanelOpened;
        public System.Action OnPanelClosed;

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            CalculatePositions();
            SetupButtons();

            // Start closed
            SetPanelPosition(closed: true, instant: true);
        }

        private void Start()
        {
            // Subscribe to quest updates
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestListUpdated += UpdateNotificationBadge;
                UpdateNotificationBadge();
            }
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestListUpdated -= UpdateNotificationBadge;
            }

            if (Instance == this)
                Instance = null;
        }

        private void CalculatePositions()
        {
            if (panelRect == null) return;

            if (direction == SlideDirection.FromRight)
            {
                closedPosition = new Vector2(panelWidth, panelRect.anchoredPosition.y);
                openPosition = new Vector2(0, panelRect.anchoredPosition.y);
            }
            else
            {
                closedPosition = new Vector2(-panelWidth, panelRect.anchoredPosition.y);
                openPosition = new Vector2(0, panelRect.anchoredPosition.y);
            }
        }

        private void SetupButtons()
        {
            if (tabButton != null)
            {
                tabButton.onClick.RemoveAllListeners();
                tabButton.onClick.AddListener(Toggle);
            }
        }

        private void UpdateNotificationBadge()
        {
            if (notificationBadge == null) return;

            int claimableCount = 0;

            if (QuestManager.Instance != null)
            {
                claimableCount = QuestManager.Instance.activeQuests
                    .Count(q => q.isCompleted && !q.isClaimed);
            }

            bool showBadge = claimableCount > 0;
            notificationBadge.SetActive(showBadge);

            if (showBadge && notificationCountText != null)
            {
                notificationCountText.text = claimableCount > 9 ? "9+" : claimableCount.ToString();
            }
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void Open()
        {
            if (isOpen || isAnimating) return;

            PlaySound(openSound);
            StartCoroutine(SlidePanel(open: true));
        }

        public void Close()
        {
            if (!isOpen || isAnimating) return;

            PlaySound(closeSound);
            StartCoroutine(SlidePanel(open: false));
        }

        public void Toggle()
        {
            if (isAnimating) return;

            if (isOpen)
                Close();
            else
                Open();
        }

        public bool IsOpen => isOpen;

        // ==========================================
        // CLOSE BLOCKING (dla popup'Ûw) - NOWE
        // ==========================================

        /// <summary>
        /// Temporarily prevents panel from closing on outside click
        /// Call this when showing overlays/popups
        /// </summary>
        public void DisableCloseTemporarily()
        {
            closeTemporarilyDisabled = true;
        }

        /// <summary>
        /// Re-enables close on outside click
        /// </summary>
        public void EnableClose()
        {
            closeTemporarilyDisabled = false;
        }

        /// <summary>
        /// Checks if any popup is blocking close
        /// </summary>
        private bool ShouldBlockClose()
        {
            // Sprawdü czy popup lootboxa jest aktywny
            if (LootboxRewardPopup.Instance != null && LootboxRewardPopup.Instance.IsShowing)
            {
                return true;
            }

            // Sprawdü flagÍ tymczasowej blokady
            if (closeTemporarilyDisabled)
            {
                return true;
            }

            return false;
        }

        // ==========================================
        // ANIMATION
        // ==========================================

        private IEnumerator SlidePanel(bool open)
        {
            isAnimating = true;

            Vector2 startPos = panelRect.anchoredPosition;
            Vector2 endPos = open ? openPosition : closedPosition;

            Quaternion startRot = tabButtonRect != null ? tabButtonRect.localRotation : Quaternion.identity;
            Quaternion endRot = startRot;

            if (rotateTabWhenOpen && tabButtonRect != null)
            {
                float targetAngle = open ? tabRotationAngle : 0f;
                endRot = Quaternion.Euler(0, 0, targetAngle);
            }

            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / slideDuration;
                float curvedT = slideCurve.Evaluate(t);

                panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, curvedT);

                if (tabButtonRect != null && rotateTabWhenOpen)
                {
                    tabButtonRect.localRotation = Quaternion.Lerp(startRot, endRot, curvedT);
                }

                yield return null;
            }

            panelRect.anchoredPosition = endPos;

            if (tabButtonRect != null && rotateTabWhenOpen)
            {
                tabButtonRect.localRotation = endRot;
            }

            isOpen = open;
            isAnimating = false;

            if (open)
                OnPanelOpened?.Invoke();
            else
                OnPanelClosed?.Invoke();
        }

        private void SetPanelPosition(bool closed, bool instant)
        {
            if (!instant) return;

            panelRect.anchoredPosition = closed ? closedPosition : openPosition;
            isOpen = !closed;

            if (tabButtonRect != null && rotateTabWhenOpen)
            {
                float angle = closed ? 0f : tabRotationAngle;
                tabButtonRect.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // ==========================================
        // CLOSE ON CLICK OUTSIDE - ZAKTUALIZOWANE
        // ==========================================

        private void Update()
        {
            if (!closeOnClickOutside || !isOpen || isAnimating) return;

            // NOWE - sprawdü czy zamykanie jest zablokowane
            if (ShouldBlockClose()) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverPanel() && !IsPointerOverTabButton())
                {
                    Close();
                }
            }
        }

        private bool IsPointerOverPanel()
        {
            if (panelRect == null) return false;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRect,
                Input.mousePosition,
                null,
                out localPoint
            );

            return panelRect.rect.Contains(localPoint);
        }

        private bool IsPointerOverTabButton()
        {
            if (tabButtonRect == null) return false;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tabButtonRect,
                Input.mousePosition,
                null,
                out localPoint
            );

            return tabButtonRect.rect.Contains(localPoint);
        }

        // ==========================================
        // EDITOR HELPERS
        // ==========================================

        private void OnValidate()
        {
            if (panelRect != null)
            {
                CalculatePositions();
            }
        }

        [ContextMenu("Test Open")]
        private void TestOpen() => Open();

        [ContextMenu("Test Close")]
        private void TestClose() => Close();

        [ContextMenu("Test Toggle")]
        private void TestToggle() => Toggle();

        [ContextMenu("Reset to Closed Position")]
        private void ResetToClosed()
        {
            CalculatePositions();
            SetPanelPosition(closed: true, instant: true);
        }
    }
}