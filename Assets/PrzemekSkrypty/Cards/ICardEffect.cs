using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Interface for card effects
    /// All card mechanics must implement this
    /// </summary>
    public interface ICardEffect
    {
        /// <summary>
        /// Activates card effect for given player
        /// </summary>
        /// <param name="ownerPhotonView">Player who owns this card</param>
        void Activate(PhotonView ownerPhotonView);

        /// <summary>
        /// Deactivates card effect (for temporary cards)
        /// </summary>
        void Deactivate(PhotonView ownerPhotonView);

        /// <summary>
        /// Description of what this card does (for UI tooltip)
        /// </summary>
        string GetEffectDescription();
    }
}