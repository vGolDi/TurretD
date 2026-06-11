using UnityEngine;
using ElementumDefense.Skins;


namespace ElementumDefense.Skins
{
/// <summary>
/// ScriptableObject defining a map visual theme.
/// Does NOT change layout/waypoints — only visuals (materials, decorations, lighting).
/// 
/// Usage: Assign to ArenaSkinApplier.mapThemeOverride, or let the system
/// load it from the player's equipped skin of category SkinCategory.Map.
/// </summary>
[CreateAssetMenu(fileName = "New Map Theme", menuName = "Tower Defense/Skins/Map Theme")]
public class MapThemeData : ScriptableObject
{
    [Header("=== IDENTITY ===")]
    [Tooltip("Unique ID — must match the SkinData.skinId that references this theme")]
    public string themeId;
    public string themeName = "Default Theme";

    [TextArea(2, 3)]
    public string description = "A visual theme for the arena map.";

    [Header("=== GROUND ===")]
    [Tooltip("Material applied to the Map/ground object")]
    public Material groundMaterial;

    [Tooltip("Alternative ground prefab (replaces the Map child entirely)")]
    public GameObject groundPrefabOverride;

    [Header("=== PATH ===")]
    [Tooltip("Material applied to the path/walkway")]
    public Material pathMaterial;

    [Tooltip("Optional border/edge prefab spawned along path waypoints")]
    public GameObject pathBorderPrefab;

    [Header("=== LIGHTING & SKY ===")]
    [Tooltip("Ambient light color tint")]
    public Color ambientLightColor = new Color(1f, 0.95f, 0.9f);

    [Tooltip("Fog color (set to clear/transparent to disable)")]
    public Color fogColor = new Color(0.5f, 0.5f, 0.6f, 0f);

    [Tooltip("Skybox material override (null = keep default)")]
    public Material skyboxMaterial;

    [Header("=== DECORATIONS ===")]
    [Tooltip("Decoration prefabs spawned at predefined DecorPoint locations")]
    public GameObject[] decorationPrefabs;

    [Tooltip("Particle system prefab for ambient effects (snow, embers, leaves, etc.)")]
    public GameObject ambientParticlesPrefab;

    [Header("=== STRUCTURE OVERRIDES ===")]
    [Tooltip("Override model for player base/portal (null = keep default)")]
    public GameObject basePrefabOverride;

    [Tooltip("Override model for gold mines (null = keep default)")]
    public GameObject goldMinePrefabOverride;

    // ==========================================
    // HELPERS
    // ==========================================

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(themeId))
            themeId = name;
    }
}
}
