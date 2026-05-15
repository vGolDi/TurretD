using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Attached to ANY GameObject with a UIDocument.
/// On Awake, it finds the PanelSettings used by that UIDocument
/// and patches the scale settings so the UI scales uniformly
/// across different screen resolutions.
///
/// HOW IT WORKS:
/// PanelSettings has a "Match" slider (0–1):
///   0   = scale based on WIDTH only
///   1   = scale based on HEIGHT only
///   0.5 = scale based on AVERAGE of both (best default)
///
/// We set it to 0.5 so the UI looks correct whether the window
/// is wider OR taller than the reference resolution.
///
/// WHY A SCRIPT AND NOT JUST INSPECTOR?
/// PanelSettings is a shared asset — if you have multiple scenes
/// using it, this script ensures the value is always correct
/// at runtime regardless of how the asset was saved.
/// </summary>
public class UIPanelScaleFixer : MonoBehaviour
{
    [Header("Scale Settings")]
    [Tooltip("0 = match width, 1 = match height, 0.5 = balanced (recommended)")]
    [Range(0f, 1f)]
    [SerializeField] private float matchWidthOrHeight = 0.5f;

    [Tooltip("Reference resolution the UI was designed for")]
    [SerializeField] private Vector2Int referenceResolution = new Vector2Int(1920, 1080);

    [Tooltip("If true, overrides the PanelSettings reference resolution too")]
    [SerializeField] private bool overrideReferenceResolution = true;

    private void Awake()
    {
        // Find all PanelSettings in use and patch them
        PatchAllPanelSettings();
    }

    private void PatchAllPanelSettings()
    {
        // Find all UIDocuments in the scene
        UIDocument[] allDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

        // Track which PanelSettings we've already patched (avoid duplicates)
        var patched = new System.Collections.Generic.HashSet<int>();

        foreach (UIDocument doc in allDocs)
        {
            PanelSettings ps = doc.panelSettings;
            if (ps == null) continue;

            int instanceId = ps.GetInstanceID();
            if (patched.Contains(instanceId)) continue;

            patched.Add(instanceId);

            // Patch the match value
            // This controls how the UI scales between width/height
            ps.match = matchWidthOrHeight;

            // Optionally override reference resolution
            if (overrideReferenceResolution)
            {
                ps.referenceResolution = referenceResolution;
            }

            // Ensure scale mode is "Scale With Screen Size"
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            // Ensure screen match mode is "Match Width Or Height"
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;

            Debug.Log($"[UIPanelScaleFixer] Patched PanelSettings '{ps.name}': " +
                      $"match={matchWidthOrHeight}, " +
                      $"ref={ps.referenceResolution}, " +
                      $"scaleMode={ps.scaleMode}");
        }

        if (patched.Count == 0)
        {
            Debug.LogWarning("[UIPanelScaleFixer] No PanelSettings found to patch!");
        }
    }

#if UNITY_EDITOR
    // In editor, allow re-patching when values change in inspector
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            PatchAllPanelSettings();
        }
    }
#endif
}
