using UnityEngine;
using System.Collections.Generic;

namespace ElementumDefense.Projectiles
{
    public class ProjectileStatsManager : MonoBehaviour
    {
        public static ProjectileStatsManager Instance { get; private set; }

        // ========== EXPANDED STATS ==========
        public int TotalShotsFired { get; private set; } = 0;
        public int TotalHits { get; private set; } = 0;
        public int TotalKills { get; private set; } = 0;
        public int TotalDamageDealt { get; private set; } = 0;
        public int TotalAOEHits { get; private set; } = 0; // NEW!

        // Per-projectile-type stats
        private Dictionary<string, int> shotsByType = new Dictionary<string, int>();
        private Dictionary<string, int> hitsByType = new Dictionary<string, int>();
        // ====================================

        public System.Action<int, int> OnStatsUpdated;
        public System.Action<int> OnKillRegistered; // NEW!

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterShotFired(string projectileType = "Unknown")
        {
            TotalShotsFired++;

            if (!shotsByType.ContainsKey(projectileType))
                shotsByType[projectileType] = 0;
            shotsByType[projectileType]++;

            OnStatsUpdated?.Invoke(TotalShotsFired, TotalHits);
        }

        public void RegisterHit(string projectileType = "Unknown")
        {
            TotalHits++;

            if (!hitsByType.ContainsKey(projectileType))
                hitsByType[projectileType] = 0;
            hitsByType[projectileType]++;

            OnStatsUpdated?.Invoke(TotalShotsFired, TotalHits);
        }

        // ========== NEW METHODS ==========
        public void RegisterKill()
        {
            TotalKills++;
            OnKillRegistered?.Invoke(TotalKills);
        }

        public void RegisterDamage(int amount)
        {
            TotalDamageDealt += amount;
        }

        public void RegisterAOEHit()
        {
            TotalAOEHits++;
        }

        public float GetAccuracyForType(string type)
        {
            if (!shotsByType.ContainsKey(type) || shotsByType[type] == 0)
                return 0f;

            int shots = shotsByType[type];
            int hits = hitsByType.ContainsKey(type) ? hitsByType[type] : 0;

            return (float)hits / shots * 100f;
        }

        public string GetStatsReport()
        {
            string report = $"=== PROJECTILE STATS ===\n";
            report += $"Total Shots: {TotalShotsFired}\n";
            report += $"Total Hits: {TotalHits}\n";
            report += $"Accuracy: {GetAccuracy():F1}%\n";
            report += $"Total Kills: {TotalKills}\n";
            report += $"Total Damage: {TotalDamageDealt}\n";
            report += $"AOE Hits: {TotalAOEHits}\n\n";

            report += "Per Type:\n";
            foreach (var kvp in shotsByType)
            {
                string type = kvp.Key;
                int shots = kvp.Value;
                int hits = hitsByType.ContainsKey(type) ? hitsByType[type] : 0;
                float acc = shots > 0 ? (float)hits / shots * 100f : 0f;

                report += $"  {type}: {hits}/{shots} ({acc:F1}%)\n";
            }

            return report;
        }
        // ================================

        public float GetAccuracy()
        {
            if (TotalShotsFired == 0) return 0f;
            return (float)TotalHits / TotalShotsFired * 100f;
        }

        public void ResetStats()
        {
            TotalShotsFired = 0;
            TotalHits = 0;
            TotalKills = 0;
            TotalDamageDealt = 0;
            TotalAOEHits = 0;
            shotsByType.Clear();
            hitsByType.Clear();
            OnStatsUpdated?.Invoke(0, 0);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ========== DEBUG ==========
        [ContextMenu("Print Stats Report")]
        private void PrintReport()
        {
            Debug.Log(GetStatsReport());
        }
    }
}