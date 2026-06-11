using UnityEngine;
using System.Collections;
using Photon.Pun;
using ElementumDefense.Cards;
using ElementumDefense.UI;
using ElementumDefense.Waves;


namespace ElementumDefense.Multiplayer
{
public class GameStartCountdown : MonoBehaviourPunCallbacks
{
    [Header("Countdown Settings")]
    [SerializeField] private float countdownTime = 5f;

    [Header("Game References")]
    public WaveManager waveManager;

    private DraftManager draftManager;
    private bool countdownStarted = false;

    public new void OnEnable()
    {
        base.OnEnable();
    }

    /// <summary>
    /// Called by DraftManager after all players
    /// have finished drafting.
    /// </summary>
    public void StartCountdown()
    {
        if (countdownStarted)
        {
            Debug.LogWarning(
                "[GameStartCountdown] " +
                "Already started!");
            return;
        }

        Debug.Log(
            "[GameStartCountdown] " +
            "Starting countdown...");
        countdownStarted = true;

        var hud = WaveHUD.Instance;
        if (hud != null)
        {
            hud.StartCountdown(
                countdownTime,
                OnCountdownComplete);
        }
        else
        {
            // Fallback: no UI, just wait
            Debug.LogWarning(
                "[GameStartCountdown] " +
                "WaveHUD not found! " +
                "Starting without UI.");
            StartCoroutine(
                FallbackCountdown());
        }
    }

    private IEnumerator FallbackCountdown()
    {
        yield return new WaitForSeconds(
            countdownTime);
        OnCountdownComplete();
    }

    private void OnCountdownComplete()
    {
        Debug.Log(
            "[GameStartCountdown] " +
            "Countdown complete!");

        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        Debug.Log(
            $"[{PhotonNetwork.LocalPlayer.NickName}]" +
            $" Activating cards...");

        draftManager = DraftManager.Instance;
        if (draftManager == null)
        {
            Debug.LogError(
                "[GameStartCountdown] " +
                "FATAL: DraftManager not found!");
            yield break;
        }

        draftManager.ActivateStarterCards();

        Debug.Log(
            $"[{PhotonNetwork.LocalPlayer.NickName}]" +
            $" Starting waves...");

        if (waveManager == null)
        {
            waveManager =
                GetComponentInParent<WaveManager>(
                    true);
            if (waveManager == null)
            {
                Debug.LogError(
                    "[GameStartCountdown] " +
                    "WaveManager not found!");
                yield break;
            }
        }

        waveManager.StartWaves();
    }
}
}
