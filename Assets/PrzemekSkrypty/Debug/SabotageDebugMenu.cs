#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ElementumDefense.Cards;
using ElementumDefense.Players;
using ElementumDefense.Waves;

namespace ElementumDefense.DebugTools
{
    /// <summary>
    /// In-game debug overlay (Editor / Development Build only). Toggle with F12.
    /// 
    /// Lets you:
    ///  - Browse all SabotageCardData assets and apply them to yourself or your opponent
    ///  - Force-spawn next wave / skip current
    ///  - Add gold / health / clear sabotages
    ///  - Print active sabotage / modifier state
    /// 
    /// Auto-creates itself in scene via [RuntimeInitializeOnLoadMethod]. Drag-and-drop
    /// not needed. Stripped from release builds via the conditional compile.
    /// </summary>
    public class SabotageDebugMenu : MonoBehaviour
    {
        private const KeyCode TOGGLE_KEY = KeyCode.F12;

        // Singleton instance — only one overlay per session.
        private static SabotageDebugMenu instance;

        private bool isOpen = false;
        private Vector2 scrollPos;
        private string searchFilter = "";

        // Runtime cache — discovered once on first open via Resources.LoadAll.
        // We use Resources.LoadAll because it scans the whole project. If your
        // sabotage SOs sit OUTSIDE Resources/, see "Setup notes" below.
        private List<SabotageCardData> allSabotages;

        // Tab state
        private enum Tab { Sabotages, Wave, Player, Modifiers }
        private Tab currentTab = Tab.Sabotages;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject("[SabotageDebugMenu]");
            instance = go.AddComponent<SabotageDebugMenu>();
            DontDestroyOnLoad(go);
            Debug.Log("[SabotageDebugMenu] Active. Press F12 to toggle.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(TOGGLE_KEY))
            {
                isOpen = !isOpen;
                if (isOpen && allSabotages == null)
                    LoadSabotages();
            }
        }

        private void LoadSabotages()
        {
            // Pull every SabotageCardData asset under Resources/. If you keep them
            // elsewhere, drop a copy in any Resources/ folder — Unity will pick it up.
            var loaded = Resources.LoadAll<SabotageCardData>("");
            allSabotages = loaded.Where(s => s != null && s.sabotageEffect != null)
                                 .OrderBy(s => s.sabotageName)
                                 .ToList();
            Debug.Log($"[SabotageDebugMenu] Loaded {allSabotages.Count} sabotages.");
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            // Background
            GUI.Box(new Rect(10, 10, 460, 700), "SABOTAGE DEBUG — F12 to close");

            // Tabs
            GUILayout.BeginArea(new Rect(20, 35, 440, 680));
            DrawTabs();
            GUILayout.Space(8);

            switch (currentTab)
            {
                case Tab.Sabotages: DrawSabotagesTab(); break;
                case Tab.Wave: DrawWaveTab(); break;
                case Tab.Player: DrawPlayerTab(); break;
                case Tab.Modifiers: DrawModifiersTab(); break;
            }

            GUILayout.EndArea();
        }

        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sabotages")) currentTab = Tab.Sabotages;
            if (GUILayout.Button("Wave")) currentTab = Tab.Wave;
            if (GUILayout.Button("Player")) currentTab = Tab.Player;
            if (GUILayout.Button("Modifiers")) currentTab = Tab.Modifiers;
            GUILayout.EndHorizontal();
        }

        // ==========================================
        // TAB: SABOTAGES
        // ==========================================

        private void DrawSabotagesTab()
        {
            if (allSabotages == null || allSabotages.Count == 0)
            {
                GUILayout.Label("No sabotages loaded. Are SOs under Resources/?");
                if (GUILayout.Button("Reload")) LoadSabotages();
                return;
            }

            GUILayout.Label($"Loaded: {allSabotages.Count} sabotages");
            searchFilter = GUILayout.TextField(searchFilter);

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(550));

            var filter = string.IsNullOrEmpty(searchFilter) ? "" : searchFilter.ToLower();
            foreach (var sab in allSabotages)
            {
                if (filter.Length > 0 && !sab.sabotageName.ToLower().Contains(filter)) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{sab.rarity}] {sab.sabotageName}",
                                GUILayout.Width(280));

                // Apply to SELF
                if (GUILayout.Button("Self", GUILayout.Width(50)))
                    ApplySabotageToLocal(sab);

                // Apply to OPPONENT
                if (GUILayout.Button("Opp", GUILayout.Width(50)))
                    ApplySabotageToOpponent(sab);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void ApplySabotageToLocal(SabotageCardData sab)
        {
            var pcm = FindLocalPlayerCardManager();
            if (pcm == null) { Debug.LogError("[Debug] No local PlayerCardManager"); return; }

            var view = pcm.GetComponent<Photon.Pun.PhotonView>();
            pcm.ApplySabotage(sab, view); // caster = self
            Debug.Log($"[Debug] Applied '{sab.sabotageName}' to LOCAL player");
        }

        private void ApplySabotageToOpponent(SabotageCardData sab)
        {
            var managers = FindObjectsByType<PlayerCardManager>(FindObjectsSortMode.None);
            foreach (var pcm in managers)
            {
                var view = pcm.GetComponent<Photon.Pun.PhotonView>();
                if (view == null || view.IsMine) continue;
                pcm.ApplySabotage(sab, FindLocalPlayerView());
                Debug.Log($"[Debug] Applied '{sab.sabotageName}' to OPPONENT ({view.Owner?.NickName})");
                return;
            }
            // No opponent found (single-player test) — apply to self with caster=null
            Debug.Log("[Debug] No remote opponent — falling back to self");
            ApplySabotageToLocal(sab);
        }

        // ==========================================
        // TAB: WAVE
        // ==========================================

        private void DrawWaveTab()
        {
            var wm = FindMyWaveManager();
            if (wm == null) { GUILayout.Label("No WaveManager in scene."); return; }

            GUILayout.Label($"Current Wave: {wm.GetCurrentWaveIndex() + 1} / {wm.GetTotalWaves()}");
            GUILayout.Label($"Spawning: {wm.IsSpawning}");
            GUILayout.Label($"Mayhem: {wm.IsMayhemActive}");
            GUILayout.Space(8);

            if (GUILayout.Button("Clear All Enemies"))
                wm.ClearAllEnemies();

            GUILayout.Space(4);

            GUILayout.Label("Wave Modifiers (current):");
            var mods = wm.GetActiveModifiers();
            GUILayout.Label($"  HP x{mods.enemyHPMultiplier:F2}, Speed x{mods.enemySpeedMultiplier:F2}");
            GUILayout.Label($"  Count x{mods.enemyCountMultiplier:F2}, SpawnRate x{mods.spawnRateMultiplier:F2}");
            GUILayout.Label($"  GoldMul x{mods.goldRewardMultiplier:F2}, BuildDisabled={mods.disableBuilding}");

            GUILayout.Space(4);
            if (GUILayout.Button("Reset Wave Modifiers"))
                wm.ApplyWaveModifiers(m => m.Reset());
        }

        // ==========================================
        // TAB: PLAYER
        // ==========================================

        private void DrawPlayerTab()
        {
            var gold = FindLocalPlayerGold();
            var health = FindLocalPlayerHealth();

            if (gold != null)
            {
                GUILayout.Label($"Gold: {gold.GetGold()}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+100")) gold.AddGold(100);
                if (GUILayout.Button("+1000")) gold.AddGold(1000);
                if (GUILayout.Button("-100")) gold.AddGold(-100);
                if (GUILayout.Button("Reset to 200")) gold.AddGold(200 - gold.GetGold());
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);

            if (health != null)
            {
                GUILayout.Label($"HP: {health.CurrentHealth} / {health.MaxHealth}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+10 HP")) health.Heal(10);
                if (GUILayout.Button("-10 HP")) health.TakeDamage(10);
                if (GUILayout.Button("Kill")) health.TakeDamage(health.CurrentHealth);
                GUILayout.EndHorizontal();
            }
        }

        // ==========================================
        // TAB: MODIFIERS
        // ==========================================

        private void DrawModifiersTab()
        {
            var pcm = FindLocalPlayerCardManager();
            if (pcm == null) { GUILayout.Label("No PlayerCardManager in scene."); return; }

            var stack = pcm.GetComponent<PlayerModifierStack>();
            if (stack == null) { GUILayout.Label("No PlayerModifierStack."); return; }

            GUILayout.Label("=== CARD MODIFIERS ===");
            GUILayout.Label($"Damage   x{stack.DamageMultiplier:F2}");
            GUILayout.Label($"FireRate x{stack.FireRateMultiplier:F2}");
            GUILayout.Label($"Range    x{stack.RangeMultiplier:F2}");
            GUILayout.Label($"Cost     x{stack.TurretCostMultiplier:F2}");
            GUILayout.Label($"Gold/s   {stack.PassiveGoldPerSecond}");

            GUILayout.Space(8);
            GUILayout.Label("=== SABOTAGE PRODUCTS ===");
            GUILayout.Label($"Damage   x{stack.SabotageDamageProduct:F2}");
            GUILayout.Label($"FireRate x{stack.SabotageFireRateProduct:F2}");
            GUILayout.Label($"Range    x{stack.SabotageRangeProduct:F2}");
            GUILayout.Label($"Cost     x{stack.SabotageCostProduct:F2}");
            GUILayout.Label($"PassiveG x{stack.PassiveGoldProduct:F2}");
            GUILayout.Label($"Upgrades disabled: {stack.AreUpgradesDisabled}");

            GUILayout.Space(8);
            var activeMods = stack.GetAllActiveSabotageMods();
            GUILayout.Label($"=== ACTIVE SABOTAGE ENTRIES ({activeMods.Count}) ===");
            foreach (var m in activeMods)
                GUILayout.Label($"  [{m.id}] {m.stat} x{m.multiplier:F2}");

            GUILayout.Space(8);
            if (GUILayout.Button("Clear All Sabotages"))
                pcm.ClearAllSabotages();
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private PlayerCardManager FindLocalPlayerCardManager()
        {
            var managers = FindObjectsByType<PlayerCardManager>(FindObjectsSortMode.None);
            foreach (var m in managers)
            {
                var view = m.GetComponent<Photon.Pun.PhotonView>();
                if (view == null || view.IsMine) return m;
            }
            return null;
        }

        private Photon.Pun.PhotonView FindLocalPlayerView()
        {
            var pcm = FindLocalPlayerCardManager();
            return pcm?.GetComponent<Photon.Pun.PhotonView>();
        }

        private PlayerGold FindLocalPlayerGold() => PlayerGold.LocalInstance;
        private PlayerHealth FindLocalPlayerHealth() => PlayerHealth.LocalInstance;

        private WaveManager FindMyWaveManager()
        {
            var pcm = FindLocalPlayerCardManager();
            if (pcm == null) return FindAnyObjectByType<WaveManager>();

            // Find the WaveManager in the local player's arena
            var arenas = FindObjectsByType<ArenaOwner>(FindObjectsSortMode.None);
            foreach (var arena in arenas)
            {
                var view = arena.ownerPhotonView;
                if (view != null && view.IsMine)
                {
                    var wm = arena.GetComponentInChildren<WaveManager>();
                    if (wm != null) return wm;
                }
            }
            return FindAnyObjectByType<WaveManager>();
        }
    }
}
#endif
