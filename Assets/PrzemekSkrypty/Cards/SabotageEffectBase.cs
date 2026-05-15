using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Base class for sabotage effects
    /// Applied to OPPONENT (not caster)
    /// </summary>
    public abstract class SabotageEffectBase : ScriptableObject
    {
        [Header("Effect Configuration")]
        [TextArea(2, 4)]
        public string effectDescription = "Sabotage effect description...";

        // ==========================================
        // CORE METHODS
        // ==========================================

        /// <summary>
        /// Applies sabotage to target player
        /// </summary>
        /// <param name="targetPhotonView">Opponent being sabotaged</param>
        /// <param name="casterPhotonView">Player who cast sabotage</param>
        public abstract void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView);

        /// <summary>
        /// Removes sabotage effect (when duration expires)
        /// </summary>
        public virtual void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // Default: do nothing (instant sabotages don't need removal)
        }

        /// <summary>
        /// Update loop for temporary sabotages (optional)
        /// </summary>
        public virtual void OnUpdate(PhotonView targetPhotonView, float deltaTime)
        {
            // Override if needed (e.g., DOT sabotage)
        }

        public virtual string GetEffectDescription()
        {
            return effectDescription;
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        protected PlayerGold GetPlayerGold(PhotonView player)
        {
            return player?.GetComponent<PlayerGold>();
        }

        protected PlayerHealth GetPlayerHealth(PhotonView player)
        {
            return player?.GetComponent<PlayerHealth>();
        }

        protected BuildManager GetBuildManager(PhotonView player)
        {
            return player?.GetComponent<BuildManager>();
        }

        /// <summary>
        /// Finds the WaveManager belonging to a player's arena.
        /// WaveManager lives on the Arena object (child of ArenaOwner),
        /// NOT on the player object. So we search all ArenaOwners
        /// and match by ownerPhotonView.
        /// </summary>
        protected WaveManager GetWaveManager(PhotonView playerPhotonView)
        {
            if (playerPhotonView == null) return null;

            ArenaOwner[] arenas = Object.FindObjectsByType<ArenaOwner>(
                FindObjectsSortMode.None);

            foreach (ArenaOwner arena in arenas)
            {
                if (arena.ownerPhotonView == playerPhotonView)
                {
                    WaveManager wm = arena.GetComponentInChildren<WaveManager>();
                    if (wm != null) return wm;
                }
            }

            Debug.LogWarning($"[SabotageEffectBase] WaveManager not found for player " +
                             $"{playerPhotonView.Owner?.NickName ?? "?"}");
            return null;
        }

        protected void LogSabotage(PhotonView target, PhotonView caster, string message)
        {
            string targetName = target?.Owner?.NickName ?? "Unknown";
            string casterName = caster?.Owner?.NickName ?? "Unknown";
            Debug.Log($"[Sabotage] {casterName} → {targetName}: {message}");
        }
    }
}