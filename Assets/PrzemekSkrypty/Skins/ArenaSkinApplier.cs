using UnityEngine;
using ElementumDefense.Skins;
using ElementumDefense.Multiplayer;


namespace ElementumDefense.Skins
{
/// <summary>
/// Attach to the Arena_Prefab root. On Start(), reads the player's
/// equipped skins and applies them to child objects.
/// 
/// Expected Arena hierarchy:
///   Arena_Prefab (root) ← ArenaSkinApplier here
///     ├── Map            ← ground/terrain (skinnable)
///     ├── GoldMine       ← resource generator (skinnable)  
///     ├── Base           ← portal/building (skinnable)
///     ├── Path           ← waypoints (NOT skinned, only material)
///     └── DecorPoints    ← empty transforms where decorations spawn
/// 
/// Skinnable target IDs (match with SkinData.targetId):
///   "arena_map"      — Map child
///   "arena_base"     — Base child  
///   "arena_goldmine"  — GoldMine child
/// </summary>
public class ArenaSkinApplier : MonoBehaviour
{
    [Header("=== SKINNABLE CHILDREN ===")]
    [Tooltip("The Map/ground child object")]
    [SerializeField] private Transform mapChild;

    [Tooltip("The Base/portal child object")]
    [SerializeField] private Transform baseChild;

    [Tooltip("The GoldMine child object(s)")]
    [SerializeField] private Transform[] goldMineChildren;

    [Header("=== PATH (material only) ===")]
    [Tooltip("Renderer(s) on the path — theme will swap their material")]
    [SerializeField] private Renderer[] pathRenderers;

    [Header("=== DECORATION SPAWN POINTS ===")]
    [Tooltip("Empty transforms where theme decorations will be spawned")]
    [SerializeField] private Transform[] decorPoints;

    [Header("=== AMBIENT PARTICLES ===")]
    [Tooltip("Parent transform for ambient particle systems (snow, embers etc.)")]
    [SerializeField] private Transform ambientParticleParent;

    [Header("=== TARGET IDs ===")]
    [Tooltip("SkinData.targetId for the map ground")]
    [SerializeField] private string mapTargetId = "arena_map";

    [Tooltip("SkinData.targetId for the base/portal")]
    [SerializeField] private string baseTargetId = "arena_base";

    [Tooltip("SkinData.targetId for gold mines")]
    [SerializeField] private string goldMineTargetId = "arena_goldmine";

    [Header("=== DEBUG ===")]
    [SerializeField] private bool logAppliedSkins = true;

    [Header("=== ARENA TYPE ===")]
    [Tooltip("Set by GameManager_MP when spawning. Used to filter compatible skins.\n" +
             "e.g. 'Fire', 'Ice', 'Earth'. Empty = accept all skins.")]
    public string arenaType = "";

    // Runtime refs for cleanup
    private GameObject[] spawnedDecorations;
    private GameObject spawnedAmbientParticles;

    // ==========================================
    // LIFECYCLE
    // ==========================================

    private void Start()
    {
        // Small delay to ensure SkinInventory is loaded
        Invoke(nameof(ApplyAllSkins), 0.1f);
    }

    /// <summary>
    /// Main entry point — applies all skins based on player's equipped loadout.
    /// Can be called again if skins change at runtime.
    /// </summary>
    [ContextMenu("Apply All Skins")]
    public void ApplyAllSkins()
    {
        var skinInv = SkinInventory.Instance;
        if (skinInv == null)
        {
            Debug.LogWarning("[ArenaSkin] SkinInventory not available yet.");
            return;
        }

        // 1. Individual skins (Base, GoldMine)
        ApplyIndividualSkin(baseChild, baseTargetId, "Base");
        foreach (var mine in goldMineChildren)
        {
            if (mine != null)
                ApplyIndividualSkin(mine, goldMineTargetId, "GoldMine");
        }

        // 2. Map theme (combines ground swap + path material + decorations + lighting)
        ApplyMapTheme(skinInv);

        // 3. Fallback: if no MapTheme but player has a simple map skin
        if (mapChild != null)
        {
            SkinData mapSkin = skinInv.GetEquippedSkin(mapTargetId);
            if (mapSkin != null && mapSkin.IsCompatibleWith(arenaType))
                skinInv.ApplySkin(mapTargetId, mapChild.gameObject);
        }

        if (logAppliedSkins)
            Debug.Log("[ArenaSkin] All arena skins applied.");
    }

    // ==========================================
    // INDIVIDUAL SKINS (Base, GoldMine)
    // ==========================================

    private void ApplyIndividualSkin(Transform target, string targetId, string label)
    {
        if (target == null) return;

        var skinInv = SkinInventory.Instance;
        if (skinInv == null) return;

        SkinData skin = skinInv.GetEquippedSkin(targetId);
        if (skin == null)
        {
            if (logAppliedSkins)
                Debug.Log($"[ArenaSkin] {label}: no skin equipped, keeping default.");
            return;
        }

        // Check arena compatibility
        if (!skin.IsCompatibleWith(arenaType))
        {
            if (logAppliedSkins)
                Debug.Log($"[ArenaSkin] {label}: skin '{skin.skinName}' not compatible with arena '{arenaType}', skipping.");
            return;
        }

        skinInv.ApplySkin(targetId, target.gameObject);

        if (logAppliedSkins)
            Debug.Log($"[ArenaSkin] {label}: applied '{skin.skinName}'");
    }

    // ==========================================
    // MAP THEME (full visual theme)
    // ==========================================

    private void ApplyMapTheme(SkinInventory skinInv)
    {
        // Check if equipped map skin has a MapThemeData reference
        SkinData mapSkin = skinInv.GetEquippedSkin(mapTargetId);
        if (mapSkin == null) return;

        // Check compatibility
        if (!mapSkin.IsCompatibleWith(arenaType))
        {
            if (logAppliedSkins)
                Debug.Log($"[ArenaSkin] MapTheme: skin '{mapSkin.skinName}' not compatible with arena '{arenaType}', skipping.");
            return;
        }

        // The skinPrefab on a Map SkinData can optionally hold a MapThemeData
        // We store theme reference via skinId convention
        MapThemeData theme = FindThemeForSkin(mapSkin);
        if (theme == null)
        {
            // No full theme — just a simple model/material swap (handled in ApplyAllSkins)
            return;
        }

        ApplyTheme(theme);
    }

    private void ApplyTheme(MapThemeData theme)
    {
        if (logAppliedSkins)
            Debug.Log($"[ArenaSkin] Applying map theme: '{theme.themeName}'");

        // --- Ground ---
        if (theme.groundPrefabOverride != null && mapChild != null)
        {
            SwapChildModel(mapChild, theme.groundPrefabOverride);
        }
        else if (theme.groundMaterial != null && mapChild != null)
        {
            ApplyMaterialToRenderers(mapChild, theme.groundMaterial);
        }

        // --- Path material ---
        if (theme.pathMaterial != null && pathRenderers != null)
        {
            foreach (var r in pathRenderers)
            {
                if (r == null) continue;
                Material[] mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = theme.pathMaterial;
                r.materials = mats;
            }
        }

        // --- Base override ---
        if (theme.basePrefabOverride != null && baseChild != null)
        {
            SwapChildModel(baseChild, theme.basePrefabOverride);
        }

        // --- GoldMine override ---
        if (theme.goldMinePrefabOverride != null)
        {
            foreach (var mine in goldMineChildren)
            {
                if (mine != null)
                    SwapChildModel(mine, theme.goldMinePrefabOverride);
            }
        }

        // --- Ambient light ---
        if (theme.ambientLightColor != Color.clear)
        {
            RenderSettings.ambientLight = theme.ambientLightColor;
        }

        // --- Fog ---
        if (theme.fogColor.a > 0.01f)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = theme.fogColor;
        }

        // --- Skybox ---
        if (theme.skyboxMaterial != null)
        {
            RenderSettings.skybox = theme.skyboxMaterial;
        }

        // --- Decorations ---
        SpawnDecorations(theme);

        // --- Ambient particles ---
        if (theme.ambientParticlesPrefab != null)
        {
            Transform parent = ambientParticleParent != null
                ? ambientParticleParent
                : transform;

            if (spawnedAmbientParticles != null)
                Destroy(spawnedAmbientParticles);

            spawnedAmbientParticles = Instantiate(
                theme.ambientParticlesPrefab,
                parent.position,
                Quaternion.identity,
                parent);
        }
    }

    // ==========================================
    // DECORATIONS
    // ==========================================

    private void SpawnDecorations(MapThemeData theme)
    {
        // Cleanup old decorations
        if (spawnedDecorations != null)
        {
            foreach (var d in spawnedDecorations)
            {
                if (d != null) Destroy(d);
            }
        }

        if (theme.decorationPrefabs == null || theme.decorationPrefabs.Length == 0)
            return;

        if (decorPoints == null || decorPoints.Length == 0)
        {
            if (logAppliedSkins)
                Debug.Log("[ArenaSkin] No DecorPoints assigned — skipping decorations.");
            return;
        }

        spawnedDecorations = new GameObject[decorPoints.Length];

        for (int i = 0; i < decorPoints.Length; i++)
        {
            if (decorPoints[i] == null) continue;

            // Cycle through decoration prefabs
            int prefabIdx = i % theme.decorationPrefabs.Length;
            GameObject prefab = theme.decorationPrefabs[prefabIdx];
            if (prefab == null) continue;

            spawnedDecorations[i] = Instantiate(
                prefab,
                decorPoints[i].position,
                decorPoints[i].rotation,
                decorPoints[i]);
        }

        if (logAppliedSkins)
            Debug.Log($"[ArenaSkin] Spawned {decorPoints.Length} decorations.");
    }

    // ==========================================
    // HELPERS
    // ==========================================

    /// <summary>
    /// Swaps the visual model on a target transform.
    /// Destroys all children, spawns new prefab as child.
    /// Preserves the target's position/rotation/scripts.
    /// </summary>
    private void SwapChildModel(Transform target, GameObject newPrefab)
    {
        // Destroy existing visual children (meshes)
        // Keep components on the target itself intact
        for (int i = target.childCount - 1; i >= 0; i--)
        {
            Transform child = target.GetChild(i);
            // Skip children that have important game logic
            if (child.GetComponent<Canvas>() != null) continue;
            Destroy(child.gameObject);
        }

        // Spawn new visual
        GameObject newVisual = Instantiate(newPrefab, target);
        newVisual.transform.localPosition = Vector3.zero;
        newVisual.transform.localRotation = Quaternion.identity;
    }

    private void ApplyMaterialToRenderers(Transform target, Material mat)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;
            r.materials = mats;
        }
    }

    /// <summary>
    /// Finds a MapThemeData asset that matches the skin's themeId.
    /// Convention: SkinData for maps stores the theme asset name in targetId or skinId.
    /// </summary>
    private MapThemeData FindThemeForSkin(SkinData skin)
    {
        // Try loading from Resources by convention
        // Theme assets should be at: Resources/MapThemes/{themeId}
        MapThemeData theme = Resources.Load<MapThemeData>($"MapThemes/{skin.skinId}");
        if (theme != null) return theme;

        // Fallback: try by targetId
        theme = Resources.Load<MapThemeData>($"MapThemes/{skin.targetId}");
        return theme;
    }

    // ==========================================
    // CLEANUP
    // ==========================================

    private void OnDestroy()
    {
        if (spawnedDecorations != null)
        {
            foreach (var d in spawnedDecorations)
            {
                if (d != null) Destroy(d);
            }
        }

        if (spawnedAmbientParticles != null)
            Destroy(spawnedAmbientParticles);
    }
}
}
