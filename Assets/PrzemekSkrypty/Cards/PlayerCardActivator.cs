using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Owns the player's list of active cards. Activates/deactivates them and
    /// triggers a recompute on the <see cref="PlayerModifierStack"/> sibling.
    /// 
    /// Doesn't know anything about sabotage — that lives in
    /// <see cref="PlayerSabotageController"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerModifierStack))]
    public class PlayerCardActivator : MonoBehaviour
    {
        [Header("Active Cards")]
        [SerializeField] private List<CardData> activeCards = new List<CardData>();

        // Cached siblings
        private PhotonView photonView;
        private PlayerModifierStack modifierStack;

        // ==========================================
        // PROPERTIES
        // ==========================================

        public IReadOnlyList<CardData> ActiveCards => activeCards;
        public int ActiveCardCount => activeCards.Count;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
            modifierStack = GetComponent<PlayerModifierStack>();
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void ActivateCard(CardData card)
        {
            if (card == null)
            {
                Debug.LogError("[PlayerCardActivator] Cannot activate null card!");
                return;
            }

            if (card.cardEffect == null)
            {
                Debug.LogError($"[PlayerCardActivator] Card '{card.cardName}' has no effect!");
                return;
            }

            activeCards.Add(card);
            card.cardEffect.Activate(photonView);

            modifierStack.RecalculateFromCards(activeCards);

            Debug.Log($"[PlayerCardActivator] Activated: {card.cardName}");
        }

        public void DeactivateAllCards()
        {
            foreach (CardData card in activeCards)
                card?.cardEffect?.Deactivate(photonView);

            activeCards.Clear();
            modifierStack.RecalculateFromCards(activeCards);

            Debug.Log("[PlayerCardActivator] Deactivated all cards");
        }

        public bool HasCard(CardData card) => activeCards.Contains(card);

        public int GetCardCountByType(CardType cardType)
            => activeCards.Count(c => c.cardType == cardType);

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Print Active Cards")]
        private void PrintActiveCards()
        {
            Debug.Log($"=== ACTIVE CARDS ({activeCards.Count}) ===");
            foreach (var card in activeCards)
                Debug.Log($"  - {card.cardName} ({card.cardType})");
        }
    }
}
