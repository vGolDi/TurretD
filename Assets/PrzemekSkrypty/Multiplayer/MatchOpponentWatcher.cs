using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using ElementumDefense.Players;

namespace ElementumDefense.Multiplayer
{
    /// <summary>
    /// Game-scene helper that watches the opponent for soft / hard disconnect
    /// and decides whether the local player should automatically win.
    /// 
    /// Behaviour:
    ///  - Soft disconnect (Photon flips IsInactive=true): start a grace timer
    ///    (<see cref="graceSeconds"/>, default 25s). If the opponent rejoins
    ///    (HasRejoined=true) within the window we cancel.
    ///  - Hard disconnect (OnPlayerLeftRoom with IsInactive=false) — no rejoin
    ///    possible — declare victory immediately.
    ///  - Explicit forfeit (room property "forfeit" == opponent's actor number)
    ///    — declare victory immediately. This is what the menu's "Forfeit"
    ///    button raises so the still-playing peer doesn't have to wait for
    ///    the grace timer.
    ///  - Grace timer expires — declare victory.
    /// 
    /// We don't manipulate enemy state; we just trigger
    /// <see cref="GameEndManager.ShowVictory"/>. That manager owns the rest of
    /// the flow (ELO change, XP, panel reveal) and clears <see cref="PendingMatchState"/>.
    /// 
    /// Bind: place ONE instance in your game scene (or attach to the local
    /// player; either works because we don't touch transforms).
    /// </summary>
    public class MatchOpponentWatcher : MonoBehaviourPunCallbacks
    {
        public const string FORFEIT_PROP_KEY = "forfeitActor";
        public const string DEAD_ACTOR_KEY = "deadActor";

        [Tooltip("Grace seconds to wait for opponent to rejoin after a soft disconnect. " +
                 "Should be less than NetworkManager.RECONNECT_PLAYER_TTL_MS to give a snappier " +
                 "victory experience while still allowing quick network blips.")]
        [SerializeField] private int graceSeconds = 90;

        private Coroutine graceCoroutine;
        private bool victoryAwarded = false;

        // ==========================================
        // INITIAL CHECK
        // ==========================================

        private void Start()
        {
            // The room may already carry a forfeit / death flag if the opponent
            // quit or died before this scene finished loading (e.g. during our
            // reconnect). Honor it on first frame.
            if (PhotonNetwork.CurrentRoom != null)
            {
                CheckForfeitProperty(PhotonNetwork.CurrentRoom.CustomProperties);
                CheckDeathProperty(PhotonNetwork.CurrentRoom.CustomProperties);
            }
        }

        // ==========================================
        // PHOTON CALLBACKS
        // ==========================================

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (otherPlayer == null || otherPlayer.IsLocal) return;

            if (otherPlayer.IsInactive)
            {
                Debug.Log($"[OpponentWatcher] {otherPlayer.NickName} soft-left (inactive). " +
                          $"Starting {graceSeconds}s grace timer.");
                StartGrace(otherPlayer);
            }
            else
            {
                Debug.Log($"[OpponentWatcher] {otherPlayer.NickName} hard-left (forfeit). Awarding victory.");
                AwardVictory();
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            // Photon fires this on rejoin too (HasRejoined=true). Cancel grace.
            if (newPlayer == null || newPlayer.IsLocal) return;

            if (newPlayer.HasRejoined)
            {
                Debug.Log($"[OpponentWatcher] {newPlayer.NickName} rejoined within grace window. Resuming match.");
                CancelGrace();
            }
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            CheckForfeitProperty(propertiesThatChanged);
            CheckDeathProperty(propertiesThatChanged);
        }

        // ==========================================
        // FORFEIT FLAG
        // ==========================================

        /// <summary>
        /// Static helper for the menu controller to call before quitting Photon.
        /// Sets a room custom property "forfeitActor" = actor number; the still-
        /// playing peer's <see cref="OnRoomPropertiesUpdate"/> picks it up.
        /// 
        /// Returns true if the property was raised. Caller can wait briefly
        /// before disconnecting to let Photon flush the message.
        /// </summary>
        public static bool RaiseForfeit()
        {
            if (PhotonNetwork.CurrentRoom == null) return false;
            int actorNr = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
            if (actorNr < 0) return false;

            var props = new Hashtable { { FORFEIT_PROP_KEY, actorNr } };
            return PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        private void CheckForfeitProperty(Hashtable props)
        {
            if (props == null) return;
            if (!props.TryGetValue(FORFEIT_PROP_KEY, out object obj)) return;
            if (obj is not int forfeitActor) return;

            // If forfeit was raised by SOMEONE ELSE — that's our victory.
            if (PhotonNetwork.LocalPlayer != null &&
                PhotonNetwork.LocalPlayer.ActorNumber != forfeitActor)
            {
                Debug.Log($"[OpponentWatcher] Forfeit flag detected (actor {forfeitActor}). Awarding victory.");
                AwardVictory();
            }
        }

        /// <summary>
        /// Reconnect-robust death handling. The dying player publishes a room
        /// property <see cref="DEAD_ACTOR_KEY"/> = their actor number. The other
        /// player's client reads it here and wins. This replaces the fragile
        /// per-PhotonView RPC_PlayerDied path, which can miss its target after a
        /// reconnect changes/duplicates player view IDs.
        /// </summary>
        private void CheckDeathProperty(Hashtable props)
        {
            if (props == null) return;
            if (!props.TryGetValue(DEAD_ACTOR_KEY, out object obj)) return;
            if (obj is not int deadActor) return;
            if (PhotonNetwork.LocalPlayer == null) return;

            // Someone else died → we win. (Our own death shows defeat locally.)
            if (PhotonNetwork.LocalPlayer.ActorNumber != deadActor)
            {
                Debug.Log($"[OpponentWatcher] Death flag detected (actor {deadActor}). Awarding victory.");
                AwardVictory();
            }
        }

        // ==========================================
        // GRACE TIMER
        // ==========================================

        private void StartGrace(Player opponent)
        {
            CancelGrace();
            graceCoroutine = StartCoroutine(GraceTimer(opponent, graceSeconds));
        }

        private void CancelGrace()
        {
            if (graceCoroutine != null)
            {
                StopCoroutine(graceCoroutine);
                graceCoroutine = null;
            }
        }

        private IEnumerator GraceTimer(Player opponent, int seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                yield return new WaitForSeconds(1f);
                t += 1f;
                // Stop early if the opponent already rejoined (HasRejoined sets IsInactive=false).
                if (opponent != null && !opponent.IsInactive && PhotonNetwork.CurrentRoom != null
                    && PhotonNetwork.CurrentRoom.Players.ContainsKey(opponent.ActorNumber))
                {
                    Debug.Log($"[OpponentWatcher] Opponent active again before timeout. Cancel.");
                    graceCoroutine = null;
                    yield break;
                }
            }

            Debug.Log($"[OpponentWatcher] Grace expired ({seconds}s). Awarding victory.");
            graceCoroutine = null;
            AwardVictory();
        }

        // ==========================================
        // VICTORY HANDOFF
        // ==========================================

        private void AwardVictory()
        {
            if (victoryAwarded) return;
            victoryAwarded = true;

            // Clear pending match — we're not the one who needs to rejoin.
            PendingMatchState.Clear();
            // The match is over for us — drop our own state snapshot so it can't
            // leak into the next match.
            ElementumDefense.Multiplayer.Reconnect.MatchSnapshotService.Instance?.Clear();

            var gem = FindAnyObjectByType<GameEndManager>();
            if (gem != null) gem.ShowVictory();
            else Debug.LogError("[OpponentWatcher] No GameEndManager in scene to award victory!");
        }
    }
}
