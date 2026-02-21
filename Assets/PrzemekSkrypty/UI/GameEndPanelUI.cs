using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ElementumDefense.Progression;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameEndPanelUI : MonoBehaviour
    {
        public static GameEndPanelUI Instance
        { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioClip victorySound;
        [SerializeField] private AudioClip defeatSound;
        [SerializeField] private AudioClip buttonClickSound;

        private AudioSource audioSource;
        private VisualElement root;

        // Cached elements
        private VisualElement endgameRoot;
        private VisualElement vignette;
        private VisualElement scarsContainer;
        private VisualElement radialOrigin;
        private Label titleLabel;
        private VisualElement titleGlow;
        private Label subtitleLabel;

        // Corners
        private VisualElement[] outerCorners;
        private VisualElement[] innerCorners;

        // Radial lines
        private List<VisualElement> radialLines;

        // Ornaments
        private VisualElement ornLineL, ornLineR;
        private VisualElement ornDiaL, ornDiaR, ornDiaC;

        // Divider
        private VisualElement divLineL, divLineR, divDot;

        // Stats
        private VisualElement statsSection;
        private Label levelNumber;
        private Label levelLabel;
        private Label xpGainLabel;
        private VisualElement xpFill;
        private Label xpText;

        // Ranked
        private VisualElement rankedSection;
        private VisualElement rankIconBg;
        private VisualElement rankIconBorder;
        private Label rankIconText;
        private Label rankName;
        private Label eloChangeLabel;
        private VisualElement eloFill;
        private Label eloMin, eloMax;

        // Quests
        private VisualElement questsSection;
        private VisualElement questList;

        // Button
        private Button returnButton;

        // State
        private bool isShowing = false;

        public System.Action OnReturnToMenu;

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

            var bg = root.Q<VisualElement>("endgame-root");
            StarfieldInjector.Instance?.Register(bg);

            QueryElements();
            BindButtons();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ==========================================
        // QUERY ELEMENTS
        // ==========================================

        private void QueryElements()
        {
            endgameRoot =
                root.Q<VisualElement>("endgame-root");
            vignette =
                root.Q<VisualElement>(
                    "endgame-vignette");
            scarsContainer =
                root.Q<VisualElement>("endgame-scars");
            radialOrigin =
                root.Q<VisualElement>("endgame-radial");

            titleLabel =
                root.Q<Label>("endgame-title");
            titleGlow =
                root.Q<VisualElement>(
                    "endgame-title-glow");
            subtitleLabel =
                root.Q<Label>("endgame-subtitle");

            // Corners
            outerCorners = new[]
            {
                root.Q<VisualElement>("corner-tl"),
                root.Q<VisualElement>("corner-tr"),
                root.Q<VisualElement>("corner-bl"),
                root.Q<VisualElement>("corner-br")
            };
            innerCorners = new[]
            {
                root.Q<VisualElement>("corner-inner-tl"),
                root.Q<VisualElement>("corner-inner-tr"),
                root.Q<VisualElement>("corner-inner-bl"),
                root.Q<VisualElement>("corner-inner-br")
            };

            // Radial lines
            radialLines = new List<VisualElement>();
            if (radialOrigin != null)
            {
                radialOrigin.Query(
                    className: "endgame-radial-line")
                    .ForEach(e => radialLines.Add(e));
            }

            // Ornaments
            ornLineL =
                root.Q<VisualElement>("orn-line-l");
            ornLineR =
                root.Q<VisualElement>("orn-line-r");
            ornDiaL =
                root.Q<VisualElement>("orn-dia-l");
            ornDiaR =
                root.Q<VisualElement>("orn-dia-r");
            ornDiaC =
                root.Q<VisualElement>("orn-dia-c");

            // Divider
            divLineL =
                root.Q<VisualElement>("div-line-l");
            divLineR =
                root.Q<VisualElement>("div-line-r");
            divDot =
                root.Q<VisualElement>("div-dot");

            // Stats
            statsSection =
                root.Q<VisualElement>("endgame-stats");
            levelNumber =
                root.Q<Label>("endgame-level-number");
            levelLabel =
                root.Q<Label>("endgame-level-label");
            xpGainLabel =
                root.Q<Label>("endgame-xp-gain");
            xpFill =
                root.Q<VisualElement>("endgame-xp-fill");
            xpText =
                root.Q<Label>("endgame-xp-text");

            // Ranked
            rankedSection =
                root.Q<VisualElement>("endgame-ranked");
            rankIconBg =
                root.Q<VisualElement>(
                    "endgame-rank-icon-bg");
            rankIconBorder =
                root.Q<VisualElement>(
                    "endgame-rank-icon-border");
            rankIconText =
                root.Q<Label>(
                    "endgame-rank-icon-text");
            rankName =
                root.Q<Label>("endgame-rank-name");
            eloChangeLabel =
                root.Q<Label>("endgame-elo-change");
            eloFill =
                root.Q<VisualElement>(
                    "endgame-elo-fill");
            eloMin =
                root.Q<Label>("endgame-elo-min");
            eloMax =
                root.Q<Label>("endgame-elo-max");

            // Quests
            questsSection =
                root.Q<VisualElement>("endgame-quests");
            questList =
                root.Q<VisualElement>(
                    "endgame-quest-list");

            // Button
            returnButton =
                root.Q<Button>("endgame-return-btn");
        }

        // ==========================================
        // BIND
        // ==========================================

        private void BindButtons()
        {
            returnButton?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlaySound(buttonClickSound);
                    OnReturnToMenu?.Invoke();
                    evt.StopPropagation();
                });
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void ShowVictory(
            int xpGained, int playerLevel,
            float xpCurrent, float xpMax)
        {
            ApplyTheme(true);
            SetStatsDisplay(
                xpGained, playerLevel,
                xpCurrent, xpMax);
            SetRankedVisible(false);
            BuildQuestDisplay();
            Reveal(true);
        }

        public void ShowVictoryRanked(
            int xpGained, int playerLevel,
            float xpCurrent, float xpMax,
            int eloChange, int currentElo,
            string rankNameStr, Color rankColor,
            int rankMin, int rankMax)
        {
            ApplyTheme(true);
            SetStatsDisplay(
                xpGained, playerLevel,
                xpCurrent, xpMax);
            SetRankedDisplay(
                eloChange, currentElo,
                rankNameStr, rankColor,
                rankMin, rankMax);
            BuildQuestDisplay();
            Reveal(true);
        }

        public void ShowDefeat(
            int xpGained, int playerLevel,
            float xpCurrent, float xpMax)
        {
            ApplyTheme(false);
            SetStatsDisplay(
                xpGained, playerLevel,
                xpCurrent, xpMax);
            SetRankedVisible(false);
            BuildQuestDisplay();
            Reveal(false);
        }

        public void ShowDefeatRanked(
            int xpGained, int playerLevel,
            float xpCurrent, float xpMax,
            int eloChange, int currentElo,
            string rankNameStr, Color rankColor,
            int rankMin, int rankMax)
        {
            ApplyTheme(false);
            SetStatsDisplay(
                xpGained, playerLevel,
                xpCurrent, xpMax);
            SetRankedDisplay(
                eloChange, currentElo,
                rankNameStr, rankColor,
                rankMin, rankMax);
            BuildQuestDisplay();
            Reveal(false);
        }

        public void Hide()
        {
            if (endgameRoot == null) return;
            endgameRoot.AddToClassList("hidden");
            endgameRoot.RemoveFromClassList(
                "endgame-root-visible");
            isShowing = false;
        }

        // ==========================================
        // THEME APPLICATION
        // ==========================================

        private void ApplyTheme(bool isVictory)
        {
            string v = "victory";
            string d = "defeat";
            string use = isVictory ? v : d;
            string remove = isVictory ? d : v;

            // Title
            if (titleLabel != null)
            {
                titleLabel.text = isVictory
                    ? "VICTORY"
                    : "DEFEAT";
                SwapClass(titleLabel,
                    $"endgame-title-{use}",
                    $"endgame-title-{remove}");
            }

            // Subtitle
            if (subtitleLabel != null)
            {
                subtitleLabel.text = isVictory
                    ? "THE REALM IS DEFENDED"
                    : "THE DARKNESS PREVAILS";
                SwapClass(subtitleLabel,
                    $"endgame-subtitle-{use}",
                    $"endgame-subtitle-{remove}");
            }

            // Title glow (victory only)
            if (titleGlow != null)
            {
                SwapClass(titleGlow,
                    "endgame-title-glow-victory",
                    null);
                if (!isVictory)
                    titleGlow.RemoveFromClassList(
                        "endgame-title-glow-victory");
            }

            // Vignette
            if (vignette != null)
            {
                SwapClass(vignette,
                    $"endgame-vignette-{use}",
                    $"endgame-vignette-{remove}");
            }

            // Scars (defeat only)
            SetVisible(scarsContainer, !isVictory);

            // Corners
            foreach (var c in outerCorners)
            {
                if (c == null) continue;
                SwapClass(c,
                    $"endgame-corner-{use}",
                    $"endgame-corner-{remove}");
            }
            foreach (var c in innerCorners)
            {
                if (c == null) continue;
                SwapClass(c,
                    $"endgame-corner-inner-{use}",
                    $"endgame-corner-inner-{remove}");
            }

            // Radial lines
            foreach (var line in radialLines)
            {
                SwapClass(line,
                    $"endgame-radial-line-{use}",
                    $"endgame-radial-line-{remove}");
            }

            // Ornaments
            ApplyOrnamentTheme(ornLineL, use, remove,
                "endgame-ornament-line");
            ApplyOrnamentTheme(ornLineR, use, remove,
                "endgame-ornament-line");
            ApplyOrnamentTheme(ornDiaL, use, remove,
                "endgame-ornament-diamond");
            ApplyOrnamentTheme(ornDiaR, use, remove,
                "endgame-ornament-diamond");
            ApplyOrnamentTheme(ornDiaC, use, remove,
                "endgame-ornament-diamond-center");

            // Divider
            ApplyOrnamentTheme(divLineL, use, remove,
                "endgame-divider-line");
            ApplyOrnamentTheme(divLineR, use, remove,
                "endgame-divider-line");
            ApplyOrnamentTheme(divDot, use, remove,
                "endgame-divider-dot");
        }

        private void ApplyOrnamentTheme(
            VisualElement el,
            string use, string remove,
            string prefix)
        {
            if (el == null) return;
            SwapClass(el,
                $"{prefix}-{use}",
                $"{prefix}-{remove}");
        }

        // ==========================================
        // STATS DISPLAY
        // ==========================================

        private void SetStatsDisplay(
            int xpGained, int playerLevel,
            float xpCurrent, float xpMax)
        {
            if (levelNumber != null)
                levelNumber.text =
                    playerLevel.ToString();

            if (xpGainLabel != null)
                xpGainLabel.text = $"+{xpGained} XP";

            if (xpText != null)
            {
                xpText.text =
                    $"{(int)xpCurrent} / " +
                    $"{(int)xpMax} XP";
            }

            if (xpFill != null)
            {
                // Start at 0, animate to current
                xpFill.style.width =
                    new StyleLength(
                        new Length(0, LengthUnit.Percent));

                // Schedule to trigger transition
                xpFill.schedule.Execute(() =>
                {
                    float pct = xpMax > 0
                        ? (xpCurrent / xpMax) * 100f
                        : 0f;
                    xpFill.style.width =
                        new StyleLength(
                            new Length(
                                pct,
                                LengthUnit.Percent));
                }).ExecuteLater(100);
            }
        }

        // ==========================================
        // RANKED DISPLAY
        // ==========================================

        private void SetRankedVisible(bool visible)
        {
            SetVisible(rankedSection, visible);
        }

        private void SetRankedDisplay(
            int eloChange, int currentElo,
            string rankNameStr, Color rankColor,
            int rankMin, int rankMax)
        {
            SetRankedVisible(true);

            if (rankName != null)
            {
                rankName.text = rankNameStr;
                rankName.style.color =
                    new StyleColor(rankColor);
            }

            if (rankIconBg != null)
            {
                rankIconBg.style.backgroundColor =
                    new StyleColor(
                        new Color(
                            rankColor.r,
                            rankColor.g,
                            rankColor.b, 0.1f));
            }

            if (rankIconBorder != null)
            {
                rankIconBorder.style.borderTopColor =
                    new StyleColor(rankColor);
                rankIconBorder.style.borderBottomColor =
                    new StyleColor(rankColor);
                rankIconBorder.style.borderLeftColor =
                    new StyleColor(rankColor);
                rankIconBorder.style.borderRightColor =
                    new StyleColor(rankColor);
            }

            if (rankIconText != null)
            {
                rankIconText.style.color =
                    new StyleColor(rankColor);
            }

            if (eloChangeLabel != null)
            {
                string sign = eloChange >= 0
                    ? "+" : "";
                eloChangeLabel.text =
                    $"{sign}{eloChange} ELO";

                eloChangeLabel.RemoveFromClassList(
                    "endgame-elo-positive");
                eloChangeLabel.RemoveFromClassList(
                    "endgame-elo-negative");
                eloChangeLabel.AddToClassList(
                    eloChange >= 0
                        ? "endgame-elo-positive"
                        : "endgame-elo-negative");
            }

            if (eloMin != null)
                eloMin.text = rankMin.ToString();
            if (eloMax != null)
                eloMax.text = rankMax.ToString();

            if (eloFill != null)
            {
                eloFill.RemoveFromClassList(
                    "endgame-elo-fill-positive");
                eloFill.RemoveFromClassList(
                    "endgame-elo-fill-negative");
                eloFill.AddToClassList(
                    eloChange >= 0
                        ? "endgame-elo-fill-positive"
                        : "endgame-elo-fill-negative");

                // Start at 0, animate
                eloFill.style.width =
                    new StyleLength(
                        new Length(
                            0, LengthUnit.Percent));

                eloFill.schedule.Execute(() =>
                {
                    float range = rankMax - rankMin;
                    float pct = range > 0
                        ? ((currentElo - rankMin) /
                            range) * 100f
                        : 0f;
                    pct = Mathf.Clamp(pct, 0f, 100f);

                    eloFill.style.width =
                        new StyleLength(
                            new Length(
                                pct,
                                LengthUnit.Percent));
                }).ExecuteLater(100);
            }
        }

        // ==========================================
        // QUEST DISPLAY
        // ==========================================

        private void BuildQuestDisplay()
        {
            if (questList == null) return;
            questList.Clear();

            if (QuestManager.Instance == null ||
                QuestManager.Instance.activeQuests == null ||
                QuestManager.Instance
                    .activeQuests.Count == 0)
            {
                var empty = new Label(
                    "NO ACTIVE QUESTS");
                empty.AddToClassList(
                    "endgame-quests-empty");
                questList.Add(empty);
                return;
            }

            foreach (var quest in
                QuestManager.Instance.activeQuests)
            {
                if (quest.isClaimed) continue;

                var slot = BuildQuestSlot(quest);
                questList.Add(slot);
            }

            // If all claimed
            if (questList.childCount == 0)
            {
                var empty = new Label(
                    "ALL QUESTS CLAIMED");
                empty.AddToClassList(
                    "endgame-quests-empty");
                questList.Add(empty);
            }
        }

        private VisualElement BuildQuestSlot(
            Quest quest)
        {
            var slot = new VisualElement();
            slot.AddToClassList("endgame-quest-slot");

            // Tier dot
            var tierDot = new VisualElement();
            tierDot.AddToClassList(
                "endgame-quest-tier-dot");
            tierDot.AddToClassList(
                GetTierClass(quest.tier));
            slot.Add(tierDot);

            // Info column
            var info = new VisualElement();
            info.AddToClassList("endgame-quest-info");

            var desc = new Label(quest.description);
            desc.AddToClassList("endgame-quest-desc");
            info.Add(desc);

            // Progress row
            var progressRow = new VisualElement();
            progressRow.AddToClassList(
                "endgame-quest-progress-row");

            var progressBg = new VisualElement();
            progressBg.AddToClassList(
                "endgame-quest-progress-bg");

            var progressFill = new VisualElement();
            progressFill.AddToClassList(
                "endgame-quest-progress-fill");

            float pct =
                quest.GetProgress01() * 100f;
            progressFill.style.width =
                new StyleLength(
                    new Length(
                        pct, LengthUnit.Percent));

            if (quest.isCompleted)
                progressFill.AddToClassList(
                    "endgame-quest-progress-fill-complete");

            progressBg.Add(progressFill);
            progressRow.Add(progressBg);

            var progressText = new Label(
                $"{quest.currentProgress}/" +
                $"{quest.targetAmount}");
            progressText.AddToClassList(
                "endgame-quest-progress-text");
            progressRow.Add(progressText);

            info.Add(progressRow);
            slot.Add(info);

            // Check mark for completed
            if (quest.isCompleted)
            {
                var check = new Label("✓");
                check.AddToClassList(
                    "endgame-quest-check");
                slot.Add(check);
            }

            return slot;
        }

        private string GetTierClass(QuestTier tier)
        {
            return tier switch
            {
                QuestTier.Daily =>
                    "endgame-quest-tier-daily",
                QuestTier.Weekly =>
                    "endgame-quest-tier-weekly",
                QuestTier.Special =>
                    "endgame-quest-tier-special",
                _ => "endgame-quest-tier-daily"
            };
        }

        // ==========================================
        // REVEAL
        // ==========================================

        private void Reveal(bool isVictory)
        {
            if (endgameRoot == null) return;

            isShowing = true;

            // Show root (remove hidden)
            endgameRoot.RemoveFromClassList("hidden");

            // Trigger animations via class
            endgameRoot.schedule.Execute(() =>
            {
                endgameRoot.AddToClassList(
                    "endgame-root-visible");
            }).ExecuteLater(50);

            // Audio
            PlaySound(isVictory
                ? victorySound
                : defeatSound);
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private void SwapClass(
            VisualElement el,
            string add, string remove)
        {
            if (el == null) return;
            if (!string.IsNullOrEmpty(remove))
                el.RemoveFromClassList(remove);
            if (!string.IsNullOrEmpty(add))
                el.AddToClassList(add);
        }

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
                audioSource.PlayOneShot(clip, 0.8f);
        }
    }
}