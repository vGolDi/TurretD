using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Base ScriptableObject for card effects
    /// Inherit from this to create new card mechanics
    /// </summary>
    public abstract class CardEffectBase : ScriptableObject, ICardEffect
    {
        [Header("Effect Configuration")]
        [TextArea(2, 4)]
        [Tooltip("Technical description of effect (shown in tooltip)")]
        public string effectDescription = "Effect description...";

        // ==========================================
        // INTERFACE IMPLEMENTATION
        // ==========================================

        public abstract void Activate(PhotonView ownerPhotonView);

        public virtual void Deactivate(PhotonView ownerPhotonView)
        {
            // Default: do nothing (most effects are permanent)
        }
        public virtual string GetEffectDescription()
        {
            return effectDescription;
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Gets PlayerGold component from owner
        /// </summary>
        protected PlayerGold GetPlayerGold(PhotonView owner)
        {
            if (owner == null) return null;
            return owner.GetComponent<PlayerGold>();
        }

        /// <summary>
        /// Gets BuildManager component from owner
        /// </summary>
        protected BuildManager GetBuildManager(PhotonView owner)
        {
            if (owner == null) return null;
            return owner.GetComponent<BuildManager>();
        }

        /// <summary>
        /// Gets PlayerHealth component from owner
        /// </summary>
        protected PlayerHealth GetPlayerHealth(PhotonView owner)
        {
            if (owner == null) return null;
            return owner.GetComponent<PlayerHealth>();
        }

        /// <summary>
        /// Logs effect activation (debug)
        /// </summary>
        protected void LogActivation(PhotonView owner, string message)
        {
            string playerName = owner?.Owner?.NickName ?? "Unknown";
            Debug.Log($"[CardEffect] {GetType().Name} activated for {playerName}: {message}");
        }
    }
}