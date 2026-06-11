using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ElementumDefense.Cards;
using ElementumDefense.UI;
using ElementumDefense.Elements;
using ElementumDefense.Waves;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using ElementumDefense.Enemies;
using ElementumDefense.Players;


namespace ElementumDefense.Waves
{
[System.Serializable]
public class WaveModifiers
{
    public float enemyHPMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public float enemyCountMultiplier = 1f;
    public float spawnRateMultiplier = 1f;
    public bool overrideElement = false;
    public ElementType newElement = ElementType.None;
    public List<GameObject> bonusEnemyPrefabs = new List<GameObject>();

    // Self-sabotage modifiers
    public float goldRewardMultiplier = 1f;
    public bool disableBuilding = false;

    // Wave-spawn modifiers (set by sabotages, consumed by SpawnEnemy)
    public int forceArmorStacks = 0;        // >0 = every spawned enemy gets that many armor stacks this wave
    public bool forceArmorOnSpawn = false;
    public bool forceReviveOnSpawn = false; // every spawned enemy will revive once
    public GameObject forceRevivePrefab = null;
    public float forceReviveHpPercent = 0.5f;
    public float regenPercentPerSecond = 0f; // >0 = every spawned enemy regenerates % maxHP per second
    public bool hideHealthBars = false;
    public bool hideElementColors = false;   // mask element type visually
    public bool useEnemyResistElement = false;
    public ElementType resistElement = ElementType.None;
    public float resistMultiplier = 0.5f;    // damage multiplier for resisted element

    public void Reset()
    {
        enemyHPMultiplier = 1f;
        enemySpeedMultiplier = 1f;
        enemyCountMultiplier = 1f;
        spawnRateMultiplier = 1f;
        overrideElement = false;
        newElement = ElementType.None;
        bonusEnemyPrefabs.Clear();
        goldRewardMultiplier = 1f;
        disableBuilding = false;

        forceArmorStacks = 0;
        forceArmorOnSpawn = false;
        forceReviveOnSpawn = false;
        forceRevivePrefab = null;
        forceReviveHpPercent = 0.5f;
        regenPercentPerSecond = 0f;
        hideHealthBars = false;
        hideElementColors = false;
        useEnemyResistElement = false;
        resistElement = ElementType.None;
        resistMultiplier = 0.5f;
    }
}

/// <summary>
/// Owns wave data, spawn primitives, network sync, and the active modifier
/// stack. The actual phase flow (announce → draft → spawn → mayhem) lives in
/// <see cref="WaveStateMachine"/> and the IWaveState implementations under
/// <c>States/</c>.
/// 
/// Keeping flow out of this class lets us add new phases (e.g. mini-boss
/// intermission, between-wave shop) without touching the spawn / sync logic.
/// </summary>
public class WaveManager : MonoBehaviourPunCallbacks
{
    [Header("Wave Configuration")]
    [SerializeField] private WaveData[] waves;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Paths")]
    [SerializeField] private Paths[] paths;

    [Header("UI")]
    [SerializeField] private float waveAnnounceDuration = 2f;

    [Header("Mayhem")]
    [SerializeField, Tooltip("Wave data for the Mayhem round (endless survival)")]
    private WaveData mayhemWave;

    [SerializeField, Tooltip("Gold bonus given to each player before Mayhem starts")]
    private int mayhemBonusGold = 200;

    // ==========================================
    // RUNTIME STATE
    // ==========================================

    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private int enemiesAlive = 0;
    private int enemiesSpawned = 0;
    private int totalEnemiesInCurrentWave = 0;
    private bool isMayhemActive = false;
    private bool normalWavesComplete = false;

    private WaveModifiers activeModifiers = new WaveModifiers();

    private DraftManager draftManager;
    private SabotageDraftManager sabotageDraftManager;
    private ArenaOwner arenaOwner;

    private const string WAVES_COMPLETE_KEY = "wavesComplete";
    private const string WAVE_INDEX_KEY = "wave_index";

    /// <summary>Highest wave index this player has completed locally (-1 = none yet).</summary>
    private int localCompletedWaveIndex = -1;

    // ==========================================
    // INITIALIZATION
    // ==========================================

    private void Start()
    {
        Debug.Log($"[WaveManager] Started on {gameObject.name}");

        arenaOwner = GetComponentInParent<ArenaOwner>();

        if (arenaOwner != null && arenaOwner.ownerPhotonView != null)
        {
            draftManager = arenaOwner.ownerPhotonView.GetComponent<DraftManager>();
            sabotageDraftManager = arenaOwner.ownerPhotonView.GetComponent<SabotageDraftManager>();
        }

        Debug.Log($"[WaveManager] DraftManager: {(draftManager != null)}");
        Debug.Log($"[WaveManager] SabotageDraftManager: {(sabotageDraftManager != null)}");

        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("[WaveManager] No waves assigned!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[WaveManager] No spawn points!");
            return;
        }

        if (paths == null || paths.Length == 0)
        {
            Debug.LogError("[WaveManager] No paths!");
            return;
        }

        UpdateWaveBadge();
    }

    public void StartWaves()
    {
        ArenaOwner ao = GetComponentInParent<ArenaOwner>();
        if (ao != null && ao.ownerPhotonView != null && !ao.ownerPhotonView.IsMine)
        {
            Debug.Log("[WaveManager] Not my arena!");
            return;
        }

        // Reconnect: a restore is taking over — it will call StartWavesFromIndex
        // with the correct resume wave. Suppress the normal wave-0 start.
        if (ElementumDefense.Multiplayer.Reconnect.MatchRestoreService.RestorePending)
        {
            Debug.Log("[WaveManager] StartWaves suppressed — restore pending.");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning("[WaveManager] Already running!");
            return;
        }

        if (draftManager == null && ao != null && ao.ownerPhotonView != null)
        {
            draftManager = ao.ownerPhotonView.GetComponent<DraftManager>();
        }

        StartCoroutine(RunFlow());
    }

    /// <summary>
    /// Reconnect restore: resume the wave flow from a given wave index instead
    /// of starting at 0. Re-running the draft for the resumed wave is harmless —
    /// it is guarded by DraftManager.nextDraftWave (restored from snapshot), so a
    /// draft that already happened will not re-trigger.
    /// </summary>
    public void StartWavesFromIndex(int index)
    {
        ArenaOwner ao = GetComponentInParent<ArenaOwner>();
        if (ao != null && ao.ownerPhotonView != null && !ao.ownerPhotonView.IsMine)
        {
            Debug.Log("[WaveManager] StartWavesFromIndex: not my arena!");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning("[WaveManager] StartWavesFromIndex: already running!");
            return;
        }

        if (draftManager == null && ao != null && ao.ownerPhotonView != null)
            draftManager = ao.ownerPhotonView.GetComponent<DraftManager>();

        int clamped = Mathf.Clamp(index, 0, GetTotalWaves() - 1);
        currentWaveIndex = clamped;

        // Tell the opponent's barrier we already cleared the prior waves.
        PublishWaveIndexForRestore(clamped);

        Debug.Log($"[WaveManager] Resuming wave flow from index {clamped}.");
        StartCoroutine(RunFlowFrom(clamped));
    }

    private IEnumerator RunFlowFrom(int index)
    {
        isSpawning = true;

        var machine = new WaveStateMachine(this);
        // Resume at the ANNOUNCE phase, NOT the draft phase. The draft for this
        // wave already happened before the disconnect (its decisions are restored
        // from the snapshot), and mid-game drafts are master-synchronized in real
        // time — re-running a draft alone on reconnect would hang waiting for the
        // opponent's rarity RPC that never comes. Subsequent waves draft normally.
        yield return machine.RunFrom(new WaveAnnounceState(index));

        isSpawning = false;
        Debug.Log("[WaveManager] Resumed flow finished.");
    }

    /// <summary>
    /// Boots the state machine. Equivalent to the old RunGameWaves +
    /// CheckAndStartMayhem loop, but each phase is a standalone state.
    /// </summary>
    private IEnumerator RunFlow()
    {
        isSpawning = true;

        // Overwrite any stale wave-sync custom properties carried over from a
        // PREVIOUS match (Photon keeps player properties across rooms). Without
        // this, the opponent's barrier could see a stale high wave_index and not
        // wait on the early waves of a fresh match.
        ClearWaveSyncProperties();

        var machine = new WaveStateMachine(this);
        // Wave 0 has no mid-game draft, but starting on WaveDraftState keeps
        // the entry point uniform — the state itself short-circuits when index==0.
        yield return machine.RunFrom(new WaveDraftState(0));

        isSpawning = false;
        Debug.Log("[WaveManager] Flow finished.");
    }

    /// <summary>
    /// Resets the wave-sync custom properties to a clean "no waves completed"
    /// baseline, overwriting any values left over from a previous match.
    /// </summary>
    private void ClearWaveSyncProperties()
    {
        localCompletedWaveIndex = -1;
        normalWavesComplete = false;
        if (PhotonNetwork.LocalPlayer == null) return;

        var props = new ExitGames.Client.Photon.Hashtable
        {
            { WAVE_INDEX_KEY, -1 },
            { WAVES_COMPLETE_KEY, false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log("[WaveManager] Cleared stale wave-sync properties for fresh match.");
    }

    // ==========================================
    // STATE-FACING API (called by IWaveState implementations)
    // ==========================================

    internal float WaveAnnounceDuration => waveAnnounceDuration;
    internal int EnemiesAlive => enemiesAlive;

    /// <summary>Wave SO at index. States resolve once at construction.</summary>
    internal WaveData GetWaveData(int index) => waves[index];

    /// <summary>Lazy resolution — DraftManager may be created after WaveManager.Start.</summary>
    internal DraftManager ResolveDraftManager()
    {
        if (draftManager == null && arenaOwner != null && arenaOwner.ownerPhotonView != null)
            draftManager = arenaOwner.ownerPhotonView.GetComponent<DraftManager>();
        return draftManager;
    }

    internal SabotageDraftManager ResolveSabotageDraftManager()
    {
        if (sabotageDraftManager == null && arenaOwner != null && arenaOwner.ownerPhotonView != null)
            sabotageDraftManager = arenaOwner.ownerPhotonView.GetComponent<SabotageDraftManager>();
        return sabotageDraftManager;
    }

    internal void PrepareWaveCounters(int index)
    {
        currentWaveIndex = index;
        WaveData currentWave = waves[index];

        totalEnemiesInCurrentWave = 0;
        foreach (var part in currentWave.waveParts)
            totalEnemiesInCurrentWave += part.enemyCount;

        enemiesSpawned = 0;
    }

    /// <summary>
    /// Sets the current wave index at the START of a mid-game draft, so any
    /// snapshot saved during that draft (card/sabotage pick) captures the wave
    /// the player is heading INTO — not the previous wave (currentWaveIndex is
    /// otherwise only updated later in WaveAnnounceState).
    /// </summary>
    internal void SetCurrentWaveForDraft(int index) => currentWaveIndex = index;

    internal void NotifyCardManagerWaveCompleted()
    {
        ArenaOwner ao = GetComponentInParent<ArenaOwner>();
        if (ao?.ownerPhotonView == null) return;

        PlayerCardManager cardManager = ao.ownerPhotonView.GetComponent<PlayerCardManager>();
        cardManager?.OnWaveCompleted();
    }

    internal void ResetActiveModifiers() => activeModifiers.Reset();

    internal void MarkLocalWaveComplete(int waveIndex) => SetSingleWaveCompleteProperty(waveIndex, true);

    internal bool AreAllPlayersOnWave(int waveIndex) => AllPlayersSingleWaveComplete(waveIndex);

    internal bool AreAllPlayersWavesComplete() => AllPlayersWavesComplete();

    internal void MarkAllNormalWavesComplete()
    {
        normalWavesComplete = true;
        SetWavesCompleteProperty(true);
        WaveHUD.Instance?.ShowAllWavesComplete();
    }

    internal bool HasMayhemWave() => mayhemWave != null;
    internal WaveData GetMayhemWave() => mayhemWave;

    internal bool IsLocalPlayerDead()
    {
        var localHealth = PlayerHealth.LocalInstance;
        return localHealth == null || localHealth.IsDead;
    }

    internal void BeginMayhem()
    {
        isMayhemActive = true;
        isSpawning = true;

        totalEnemiesInCurrentWave = 0;
        foreach (var part in mayhemWave.waveParts)
            totalEnemiesInCurrentWave += part.enemyCount;
        enemiesSpawned = 0;
        UpdateSpawnProgress();
    }

    internal void EndMayhem()
    {
        isMayhemActive = false;
        isSpawning = false;
    }

    // ==========================================
    // SPAWNING (used by states)
    // ==========================================

    internal IEnumerator SpawnWaveCoroutine(WaveData wave)
    {
        enemiesAlive = 0;

        foreach (WavePart part in wave.waveParts)
        {
            if (part.pathIndex < 0 || part.pathIndex >= paths.Length)
            {
                Debug.LogError($"[WaveManager] Invalid path index {part.pathIndex}!");
                continue;
            }

            if (part.pathIndex >= spawnPoints.Length)
            {
                Debug.LogError($"[WaveManager] No spawn for path {part.pathIndex}!");
                continue;
            }

            int finalEnemyCount = Mathf.RoundToInt(part.enemyCount * activeModifiers.enemyCountMultiplier);
            float finalSpawnInterval = part.spawnInterval * activeModifiers.spawnRateMultiplier;

            for (int j = 0; j < finalEnemyCount; j++)
            {
                SpawnEnemy(part.enemyPrefab, spawnPoints[part.pathIndex], paths[part.pathIndex]);
                yield return new WaitForSeconds(finalSpawnInterval);
            }
        }

        // Spawn bonus enemies (bosses from sabotage)
        if (activeModifiers.bonusEnemyPrefabs.Count > 0)
        {
            Debug.Log($"[WaveManager] Spawning {activeModifiers.bonusEnemyPrefabs.Count} bonus enemies!");
            int defaultPathIndex = 0;
            if (paths.Length > 0 && spawnPoints.Length > 0)
            {
                foreach (GameObject bonusPrefab in activeModifiers.bonusEnemyPrefabs)
                {
                    if (bonusPrefab != null)
                    {
                        SpawnEnemy(bonusPrefab, spawnPoints[defaultPathIndex], paths[defaultPathIndex]);
                        yield return new WaitForSeconds(1f);
                    }
                }
            }
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab, Transform spawnPoint, Paths path)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[WaveManager] Enemy prefab null!");
            return;
        }

        // Pool-aware spawn: reuses instances instead of allocating per enemy.
        GameObject enemyObj = ElementumDefense.Enemies.EnemyPoolManager.Instance != null
            ? ElementumDefense.Enemies.EnemyPoolManager.Instance.Spawn(
                enemyPrefab,
                spawnPoint.position,
                Quaternion.identity)
            : Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemyObj.transform.SetParent(transform.root);

        EnemyMovement movement = enemyObj.GetComponent<EnemyMovement>();
        if (movement != null && path != null)
        {
            movement.SetPath(path);
            if (activeModifiers.enemySpeedMultiplier != 1f)
            {
                movement.SetBaseSpeed(movement.GetBaseSpeed() * activeModifiers.enemySpeedMultiplier);
            }
        }

        enemiesAlive++;
        enemiesSpawned++;
        UpdateSpawnProgress();

        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            if (activeModifiers.enemyHPMultiplier != 1f)
            {
                health.SetMaxHP(Mathf.RoundToInt(health.GetMaxHP() * activeModifiers.enemyHPMultiplier));
            }
            if (activeModifiers.overrideElement)
            {
                health.SetElementType(activeModifiers.newElement);
            }

            // ========== Sabotage modifiers applied at spawn ==========
            ApplySpawnSabotages(enemyObj, health);
            // =========================================================

            StartCoroutine(TrackEnemyLifetime(enemyObj));
        }
    }

    /// <summary>
    /// Applies wave-level sabotage modifiers to a fresh enemy. Called once per
    /// spawn from <see cref="SpawnEnemy"/>. All flags read from
    /// <see cref="activeModifiers"/>; if a flag is off, no-op.
    /// 
    /// IMPORTANT: an enemy prefab must already have the relevant component
    /// (EnemyArmor / EnemyReviveOnDeath) for the modifier to take effect.
    /// Components are configured at spawn — pool reset handles cleanup on
    /// next reuse via OnSpawnedFromPool.
    /// </summary>
    private void ApplySpawnSabotages(GameObject enemyObj, EnemyHealth health)
    {
        var mods = activeModifiers;

        // Force armor on every enemy this wave
        if (mods.forceArmorOnSpawn && mods.forceArmorStacks > 0)
        {
            var armor = enemyObj.GetComponent<ElementumDefense.Enemies.EnemyArmor>();
            armor?.ApplyFromSabotage(mods.forceArmorStacks);
        }

        // Force revive on every enemy this wave
        if (mods.forceReviveOnSpawn)
        {
            var revive = enemyObj.GetComponent<ElementumDefense.Enemies.EnemyReviveOnDeath>();
            revive?.ApplyFromSabotage(mods.forceRevivePrefab, mods.forceReviveHpPercent);
        }

        // Regen
        if (mods.regenPercentPerSecond > 0f)
        {
            var regen = enemyObj.GetComponent<ElementumDefense.Enemies.EnemyRegenSabotage>();
            if (regen == null) regen = enemyObj.AddComponent<ElementumDefense.Enemies.EnemyRegenSabotage>();
            regen.SetRegenRate(mods.regenPercentPerSecond);
        }

        // Element resist
        if (mods.useEnemyResistElement)
        {
            var resist = enemyObj.GetComponent<ElementumDefense.Enemies.EnemyElementResistSabotage>();
            if (resist == null) resist = enemyObj.AddComponent<ElementumDefense.Enemies.EnemyElementResistSabotage>();
            resist.SetResist(mods.resistElement, mods.resistMultiplier);
        }

        // Hide healthbars
        if (mods.hideHealthBars)
        {
            var hb = enemyObj.GetComponentInChildren<ElementumDefense.Enemies.HealthBar>();
            if (hb != null) hb.gameObject.SetActive(false);
        }

        // Hide element colors (apply gray tint)
        if (mods.hideElementColors)
        {
            var hb = enemyObj.GetComponentInChildren<ElementumDefense.Enemies.HealthBar>();
            var fill = hb != null ? hb.GetComponent<UnityEngine.UI.Image>() : null;
            if (fill != null) fill.color = new Color(0.5f, 0.5f, 0.5f);
        }
    }

    private IEnumerator TrackEnemyLifetime(GameObject enemy)
    {
        // With pooling, an enemy is SetActive(false) when it "dies", but the
        // GameObject reference stays valid. We watch active state, not nullity.
        while (enemy != null && enemy.activeInHierarchy)
        {
            yield return null;
        }

        enemiesAlive--;
    }

    /// <summary>
    /// Rejestracja enemy spawnowanego DYNAMICZNIE (np. przez SplitOnDeath, Spawner aura).
    /// Inkrementuje licznik enemiesAlive i podpina śledzenie czasu życia,
    /// żeby wave nie zakończył się przedwcześnie.
    /// 
    /// Również zwiększa <c>totalEnemiesInCurrentWave</c> i <c>enemiesSpawned</c>
    /// o 1, żeby pasek "spawned X / Y" w HUD pozostał spójny: dynamiczne dzieci
    /// są częścią fali tak samo jak normalni wrogowie. Bez tego pasek pokazywałby
    /// 100% mimo że wave wciąż czeka na śmierć splitterów.
    /// </summary>
    public void RegisterDynamicEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        enemiesAlive++;
        enemiesSpawned++;
        totalEnemiesInCurrentWave++;
        UpdateSpawnProgress();
        StartCoroutine(TrackEnemyLifetime(enemy));
    }

    // ==========================================
    // GOLD / BONUS PAYOUTS (called by states)
    // ==========================================

    internal void PayWaveCompletionBonus(WaveData wave)
    {
        if (wave.waveCompletionBonus <= 0) return;

        ArenaOwner ao = GetComponentInParent<ArenaOwner>();
        if (ao?.ownerPhotonView == null) return;

        PlayerGold playerGold = ao.ownerPhotonView.GetComponent<PlayerGold>();
        if (playerGold != null)
        {
            playerGold.AddGold(wave.waveCompletionBonus);
            Debug.Log($"[WaveManager] Wave completion bonus: +{wave.waveCompletionBonus} gold");
        }
    }

    internal void PayMayhemBonusGold()
    {
        if (mayhemBonusGold <= 0) return;

        ArenaOwner ao = GetComponentInParent<ArenaOwner>();
        if (ao?.ownerPhotonView == null) return;

        PlayerGold playerGold = ao.ownerPhotonView.GetComponent<PlayerGold>();
        if (playerGold != null)
        {
            playerGold.AddGold(mayhemBonusGold);
            Debug.Log($"[WaveManager] Mayhem bonus: +{mayhemBonusGold} gold");
        }
    }

    // ==========================================
    // MULTIPLAYER SYNC
    // ==========================================

    private void SetSingleWaveCompleteProperty(int waveIndex, bool complete)
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        // Single monotonic counter instead of accumulating wave_{i}_complete
        // booleans. The counter survives reconnect cleanly (no stale flags) and
        // lets the barrier compare "have all players reached wave N".
        if (complete && waveIndex > localCompletedWaveIndex)
            localCompletedWaveIndex = waveIndex;

        var props = new ExitGames.Client.Photon.Hashtable();
        props[WAVE_INDEX_KEY] = localCompletedWaveIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"[WaveManager] Published wave_index={localCompletedWaveIndex}");
    }

    /// <summary>
    /// Reconnect restore: re-publish how many waves we had completed before the
    /// disconnect so the opponent's barrier (waiting on us) releases once we
    /// re-complete the resumed wave. resumeIndex = wave we are about to (re)play,
    /// so completed waves = resumeIndex - 1.
    /// </summary>
    public void PublishWaveIndexForRestore(int resumeIndex)
    {
        localCompletedWaveIndex = Mathf.Max(localCompletedWaveIndex, resumeIndex - 1);
        if (PhotonNetwork.LocalPlayer == null) return;

        var props = new ExitGames.Client.Photon.Hashtable();
        props[WAVE_INDEX_KEY] = localCompletedWaveIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"[WaveManager] Restore published wave_index={localCompletedWaveIndex}");
    }

    private bool AllPlayersSingleWaveComplete(int waveIndex)
    {
        if (!PhotonNetwork.InRoom) return true;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.TryGetValue(WAVE_INDEX_KEY, out object val))
                return false;
            if (!(val is int completed) || completed < waveIndex)
                return false;
        }

        return true;
    }

    private void SetWavesCompleteProperty(bool complete)
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        var props = new ExitGames.Client.Photon.Hashtable();
        props[WAVES_COMPLETE_KEY] = complete;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"[WaveManager] Set wavesComplete={complete}");
    }

    private bool AllPlayersWavesComplete()
    {
        if (!PhotonNetwork.InRoom) return true;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.TryGetValue(WAVES_COMPLETE_KEY, out object val))
                return false;
            if (!(bool)val) return false;
        }

        return true;
    }

    internal bool BothPlayersAlive()
    {
        PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        foreach (var ph in allPlayers)
            if (ph.IsDead) return false;

        return allPlayers.Length > 0;
    }

    // ==========================================
    // UI UPDATES
    // ==========================================

    internal void UpdateWaveBadge()
    {
        var hud = WaveHUD.Instance;
        if (hud == null) return;
        hud.SetWave(currentWaveIndex + 1, waves != null ? waves.Length : 0);
    }

    internal void UpdateSpawnProgress()
    {
        var hud = WaveHUD.Instance;
        if (hud == null) return;
        hud.SetSpawnProgress(enemiesSpawned, totalEnemiesInCurrentWave);
    }

    // ==========================================
    // PUBLIC API (preserved for sabotage cards / split / revive / UI)
    // ==========================================

    public WaveModifiers GetActiveModifiers() => activeModifiers;

    public void ApplyWaveModifiers(System.Action<WaveModifiers> modifierAction)
    {
        if (modifierAction != null)
        {
            modifierAction.Invoke(activeModifiers);
            Debug.Log($"[WaveManager] Applied wave modifiers. Current state: " +
                      $"HP={activeModifiers.enemyHPMultiplier}x, " +
                      $"Speed={activeModifiers.enemySpeedMultiplier}x, " +
                      $"Count={activeModifiers.enemyCountMultiplier}x, " +
                      $"ElementOverride={activeModifiers.overrideElement}");
        }
    }

    [ContextMenu("Clear All Enemies")]
    public void ClearAllEnemies()
    {
        // Prefer the pool — returns instances cleanly so they can be reused.
        var poolMgr = ElementumDefense.Enemies.EnemyPoolManager.Instance;
        if (poolMgr != null)
        {
            poolMgr.ClearAll();
        }

        // Fallback for any non-pooled stragglers (e.g., debug-instantiated).
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy.activeSelf) Destroy(enemy);
        }

        enemiesAlive = 0;
        enemiesSpawned = 0;
        UpdateSpawnProgress();
    }

    public int GetCurrentWaveIndex() => currentWaveIndex;

    public int GetTotalWaves() => waves != null ? waves.Length : 0;

    public bool IsSpawning => isSpawning;

    public bool IsMayhemActive => isMayhemActive;
}
}
