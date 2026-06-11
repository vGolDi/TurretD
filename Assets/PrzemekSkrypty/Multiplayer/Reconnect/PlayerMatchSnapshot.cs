using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementumDefense.Multiplayer.Reconnect
{
    /// <summary>
    /// Serializable snapshot of a single player's in-match state, used to
    /// restore the game after a reconnect.
    ///
    /// <para>
    /// DESIGN: we store "facts", not computed values. Turret stats, modifier
    /// stacks, and auras are deterministically rebuilt by re-activating cards
    /// and re-applying sabotages on restore — so they are NOT serialized here.
    /// </para>
    ///
    /// <para>
    /// JsonUtility constraints honored: no Dictionary; Vector3 serializes fine;
    /// ScriptableObjects are referenced by their asset <c>name</c> (string),
    /// matching the existing PlayerCollection / deck save pattern.
    /// </para>
    /// </summary>
    [Serializable]
    public class PlayerMatchSnapshot
    {
        /// <summary>Schema version — bump when fields change to invalidate old saves.</summary>
        public int version = CURRENT_VERSION;

        public const int CURRENT_VERSION = 1;

        /// <summary>
        /// Photon room this snapshot belongs to. Restore is rejected if it does
        /// not match the current room — prevents a stale snapshot from a previous
        /// match leaking into a brand-new match.
        /// </summary>
        public string roomName;

        /// <summary>Wave the player should resume from (0-based).</summary>
        public int currentWaveIndex;

        /// <summary>In-match gold balance (PlayerGold.currentGold).</summary>
        public int currentGold;

        /// <summary>In-match HP (PlayerHealth.currentHealth).</summary>
        public int playerHP;

        public List<TurretSnapshot> turrets = new List<TurretSnapshot>();

        /// <summary>Active cards by SO asset name, in activation order.</summary>
        public List<string> activeCardNames = new List<string>();

        public List<ActiveSabotageSnapshot> sabotages = new List<ActiveSabotageSnapshot>();

        public List<SelfChallengeSnapshot> selfChallenges = new List<SelfChallengeSnapshot>();

        public DraftStateSnapshot draft = new DraftStateSnapshot();
    }

    [Serializable]
    public class TurretSnapshot
    {
        /// <summary>TurretData asset name. The current upgrade level IS the assigned SO.</summary>
        public string turretDataName;

        /// <summary>World-space position (placement always uses Quaternion.identity).</summary>
        public Vector3 position;
    }

    [Serializable]
    public class ActiveSabotageSnapshot
    {
        public string sabotageName;       // SabotageCardData asset name
        public int casterActorNumber;     // re-resolve caster PhotonView by actor number
        public float remainingDuration;
        public int remainingRounds;
    }

    [Serializable]
    public class SelfChallengeSnapshot
    {
        public string sabotageName;
        public int wavesRemaining;
        public int totalWaves;
    }

    [Serializable]
    public class DraftStateSnapshot
    {
        public bool isStarterDraftComplete;
        public int nextDraftWave;
        public int currentDraftWaveIndex;
        public List<string> starterDraftedCardNames = new List<string>();
        public bool midGameCardSelected;
        public bool sabotageSelected;
        public string selectedSabotageName;

        /// <summary>Sabotage draft's own cadence (separate from the card draft's nextDraftWave).</summary>
        public int nextSabotageWave;

        /// <summary>Deck the player used this match — restored so mid-game drafts draw from the right pool.</summary>
        public string selectedDeckName;

        // One-shot draft overrides set by sabotages.
        public int nextDraftChoiceOverride;
        public bool nextDraftMulliganDisabled;
        public bool currentDraftMulliganDisabled;
    }
}
