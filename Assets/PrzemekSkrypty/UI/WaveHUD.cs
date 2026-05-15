using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class WaveHUD : MonoBehaviour
    {
        public static WaveHUD Instance
        { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioClip countdownTickSound;
        [SerializeField] private AudioClip countdownGoSound;
        [SerializeField] private AudioClip waveStartSound;

        private AudioSource audioSource;
        private VisualElement root;

        // Wave badge
        private Label waveNumber;
        private Label waveTotal;

        // Progress
        private VisualElement waveProgress;
        private VisualElement waveProgressFill;
        private Label waveProgressText;

        // Countdown
        private VisualElement countdownOverlay;
        private Label countdownNumber;
        private Label countdownSublabel;

        // Wave announce
        private VisualElement waveAnnounce;
        private Label waveAnnounceTitle;
        private Label waveAnnounceSubtitle;

        // Complete banner
        private VisualElement waveComplete;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource =
                    gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            var uiDoc = GetComponent<UIDocument>();
            root = uiDoc.rootVisualElement;
            QueryElements();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void QueryElements()
        {
            waveNumber =
                root.Q<Label>("wave-number");
            waveTotal =
                root.Q<Label>("wave-total");

            waveProgress =
                root.Q<VisualElement>(
                    "wave-progress");
            waveProgressFill =
                root.Q<VisualElement>(
                    "wave-progress-fill");
            waveProgressText =
                root.Q<Label>("wave-progress-text");

            countdownOverlay =
                root.Q<VisualElement>(
                    "countdown-overlay");
            countdownNumber =
                root.Q<Label>("countdown-number");
            countdownSublabel =
                root.Q<Label>("countdown-sublabel");

            waveAnnounce =
                root.Q<VisualElement>(
                    "wave-announce");
            waveAnnounceTitle =
                root.Q<Label>(
                    "wave-announce-title");
            waveAnnounceSubtitle =
                root.Q<Label>(
                    "wave-announce-subtitle");

            waveComplete =
                root.Q<VisualElement>(
                    "wave-complete");
        }

        // ==========================================
        // WAVE BADGE
        // ==========================================

        /// <summary>
        /// Update the wave number display.
        /// Call at start of each wave.
        /// </summary>
        public void SetWave(
            int currentWave, int totalWaves)
        {
            if (waveNumber != null)
                waveNumber.text =
                    currentWave.ToString();

            if (waveTotal != null)
                waveTotal.text =
                    $"/ {totalWaves}";
        }

        // ==========================================
        // ENEMY SPAWN PROGRESS
        // ==========================================

        /// <summary>
        /// Update spawn progress: "spawned X of Y"
        /// </summary>
        public void SetSpawnProgress(
            int spawned, int total)
        {
            // Show progress container
            if (waveProgress != null)
            {
                if (!waveProgress.ClassListContains(
                    "wave-progress-visible"))
                {
                    waveProgress.AddToClassList(
                        "wave-progress-visible");
                }
            }

            if (waveProgressFill != null)
            {
                float pct = total > 0
                    ? ((float)spawned / total) * 100f
                    : 0f;

                waveProgressFill.style.width =
                    new StyleLength(
                        new Length(
                            pct,
                            LengthUnit.Percent));
            }

            if (waveProgressText != null)
            {
                waveProgressText.text =
                    $"{spawned} / {total} SPAWNED";
            }
        }

        /// <summary>
        /// Hide the progress bar (between waves)
        /// </summary>
        public void HideSpawnProgress()
        {
            waveProgress?.RemoveFromClassList(
                "wave-progress-visible");
        }

        // ==========================================
        // COUNTDOWN
        // ==========================================

        /// <summary>
        /// Starts a visual countdown.
        /// Returns a Coroutine you can yield on,
        /// or call and forget.
        /// </summary>
        public Coroutine StartCountdown(
            float seconds,
            System.Action onComplete = null)
        {
            return StartCoroutine(
                CountdownRoutine(
                    seconds, onComplete));
        }

        private IEnumerator CountdownRoutine(
            float seconds,
            System.Action onComplete)
        {
            // Show overlay
            SetVisible(countdownOverlay, true);

            if (countdownSublabel != null)
                countdownSublabel.text =
                    "PREPARE YOUR DEFENSES";

            float timer = seconds;

            while (timer > 0f)
            {
                int display =
                    Mathf.CeilToInt(timer); 

                if (countdownNumber != null)
                {
                    countdownNumber.text =
                        display.ToString();
                    countdownNumber
                        .RemoveFromClassList(
                            "countdown-go");
                }

                PlaySound(countdownTickSound);

                // Wait 1 second
                float segmentEnd = timer - 1f;
                while (timer > segmentEnd &&
                       timer > 0f)
                {
                    timer -= Time.deltaTime;
                    yield return null;
                }
            }

            // Show GO!
            if (countdownNumber != null)
            {
                countdownNumber.text = "GO!";
                countdownNumber.AddToClassList(
                    "countdown-go");
            }

            if (countdownSublabel != null)
                countdownSublabel.text = "";

            PlaySound(countdownGoSound);

            yield return new WaitForSeconds(1f);

            // Hide overlay
            SetVisible(countdownOverlay, false);

            onComplete?.Invoke();
        }

        // ==========================================
        // WAVE ANNOUNCEMENT
        // ==========================================

        /// <summary>
        /// Show a brief wave announcement overlay.
        /// </summary>
        public Coroutine ShowWaveAnnouncement(
            int waveNum, int totalWaves,
            float duration = 2f)
        {
            return StartCoroutine(
                WaveAnnouncementRoutine(
                    waveNum, totalWaves, duration));
        }

        private IEnumerator WaveAnnouncementRoutine(
            int waveNum, int totalWaves,
            float duration)
        {
            if (waveAnnounceTitle != null)
                waveAnnounceTitle.text =
                    $"WAVE {waveNum}";

            if (waveAnnounceSubtitle != null)
            {
                if (waveNum == totalWaves)
                    waveAnnounceSubtitle.text =
                        "FINAL WAVE";
                else
                    waveAnnounceSubtitle.text =
                        "ENEMIES APPROACH";
            }

            // Show
            SetVisible(waveAnnounce, true);
            waveAnnounce?.AddToClassList(
                "wave-announce-visible");

            PlaySound(waveStartSound);

            yield return new WaitForSeconds(duration);

            // Hide
            waveAnnounce?.RemoveFromClassList(
                "wave-announce-visible");

            yield return new WaitForSeconds(0.4f);

            SetVisible(waveAnnounce, false);
        }

        // ==========================================
        // ALL WAVES COMPLETE
        // ==========================================

        public void ShowAllWavesComplete()
        {
            SetVisible(waveComplete, true);
            waveComplete?.AddToClassList(
                "wave-complete-visible");

            HideSpawnProgress();
        }
        /// <summary>
        /// Shows a waiting message using the
        /// countdown overlay (for draft sync).
        /// </summary>
        public void ShowWaitingMessage(string message)
        {
            SetVisible(countdownOverlay, true);

            if (countdownNumber != null)
            {
                countdownNumber.text = "...";
                countdownNumber.RemoveFromClassList(
                    "countdown-go");
            }

            if (countdownSublabel != null)
                countdownSublabel.text = message;
        }

        /// <summary>
        /// Hides the waiting message.
        /// Called when countdown starts.
        /// </summary>
        public void HideWaitingMessage()
        {
            SetVisible(countdownOverlay, false);
        }
        public void HideAllWavesComplete()
        {
            waveComplete?.RemoveFromClassList(
                "wave-complete-visible");
            SetVisible(waveComplete, false);
        }

        // ==========================================
        // MAYHEM
        // ==========================================

        /// <summary>
        /// Shows a dramatic Mayhem announcement.
        /// Reuses the wave-announce overlay.
        /// </summary>
        public Coroutine ShowMayhemAnnouncement(
            float duration = 2f)
        {
            return StartCoroutine(
                MayhemAnnouncementRoutine(
                    duration));
        }

        private IEnumerator
            MayhemAnnouncementRoutine(
                float duration)
        {
            if (waveAnnounceTitle != null)
                waveAnnounceTitle.text =
                    "\u26A1 MAYHEM \u26A1";

            if (waveAnnounceSubtitle != null)
                waveAnnounceSubtitle.text =
                    "LAST ONE STANDING";

            // Show
            SetVisible(waveAnnounce, true);
            waveAnnounce?.AddToClassList(
                "wave-announce-visible");

            PlaySound(waveStartSound);

            yield return
                new WaitForSeconds(duration);

            // Hide
            waveAnnounce?.RemoveFromClassList(
                "wave-announce-visible");

            yield return
                new WaitForSeconds(0.4f);

            SetVisible(waveAnnounce, false);
        }

        /// <summary>
        /// Updates wave badge to show MAYHEM
        /// instead of a wave number.
        /// </summary>
        public void SetMayhemBadge()
        {
            if (waveNumber != null)
                waveNumber.text = "\u26A1";

            if (waveTotal != null)
                waveTotal.text = "MAYHEM";
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private void SetVisible(
            VisualElement el, bool visible)
        {
            if (el == null) return;
            if (visible)
                el.RemoveFromClassList("hidden");
            else
                el.AddToClassList("hidden");
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(
                    clip, 0.7f);
        }
    }
}