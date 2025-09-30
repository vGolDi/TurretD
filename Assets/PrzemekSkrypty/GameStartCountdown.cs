using UnityEngine;
using TMPro;
using System.Collections;

public class GameStartCountdown : MonoBehaviour
{
    public float countdownTime = 5f;
    public TextMeshProUGUI countdownText;
    public WaveManager waveManager;

    void Start()
    {
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        float timer = countdownTime;

        while (timer > 0)
        {
            countdownText.text = Mathf.Ceil(timer).ToString();
            timer -= Time.deltaTime;
            yield return null;
        }

        countdownText.text = ""; // Schowaj tekst
        waveManager.StartWaves(); // wystartuj fale
    }
}
