using System.Collections.Generic;
using UnityEngine;
using ElementumDefense.Turrets;

namespace ElementumDefense.Multiplayer.Reconnect
{
    /// <summary>
    /// Name -> <see cref="TurretData"/> lookup, used by the restore flow to
    /// rebuild turrets from a snapshot (which stores the SO asset name only).
    ///
    /// <para>
    /// Why this exists: cards/sabotages load from <c>Resources</c>, but TurretData
    /// SOs live under <c>Turret/TurretSO/</c> outside Resources and are referenced
    /// by direct prefab links. Rather than move assets, we register them here.
    /// </para>
    ///
    /// <para>
    /// Populate via the inspector, or use the "Auto-Populate From Project" context
    /// menu in the editor to scan all TurretData assets automatically.
    /// </para>
    ///
    /// Place ONE instance in Resources (e.g. <c>Resources/TurretRegistry.asset</c>)
    /// so it can be loaded at runtime without a scene reference.
    /// </summary>
    [CreateAssetMenu(fileName = "TurretRegistry", menuName = "Tower Defense/Reconnect/Turret Registry")]
    public class TurretRegistry : ScriptableObject
    {
        [Tooltip("All TurretData assets that can appear in a match (including every upgrade level).")]
        [SerializeField] private List<TurretData> turrets = new List<TurretData>();

        private Dictionary<string, TurretData> lookup;

        private const string RESOURCES_PATH = "TurretRegistry";
        private static TurretRegistry s_instance;

        /// <summary>Loads the registry from Resources (cached). Null if missing.</summary>
        public static TurretRegistry Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = Resources.Load<TurretRegistry>(RESOURCES_PATH);
                    if (s_instance == null)
                        Debug.LogError($"[TurretRegistry] No registry at Resources/{RESOURCES_PATH}. " +
                                       "Create one (Tower Defense/Reconnect/Turret Registry) and place it in a Resources folder.");
                }
                return s_instance;
            }
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, TurretData>();
            foreach (var t in turrets)
            {
                if (t == null) continue;
                lookup[t.name] = t;
            }
        }

        /// <summary>Resolves a TurretData by its asset name, or null if unknown.</summary>
        public TurretData Resolve(string turretDataName)
        {
            if (string.IsNullOrEmpty(turretDataName)) return null;
            if (lookup == null) BuildLookup();
            if (lookup.TryGetValue(turretDataName, out var data)) return data;

            Debug.LogError($"[TurretRegistry] Unknown TurretData '{turretDataName}'. " +
                           "Is it registered? (Auto-Populate From Project)");
            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Populate From Project")]
        private void AutoPopulate()
        {
            turrets.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TurretData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<TurretData>(path);
                if (data != null) turrets.Add(data);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[TurretRegistry] Auto-populated {turrets.Count} TurretData assets.");
        }
#endif
    }
}
