using UnityEngine;

public class GoldZone : MonoBehaviour
{
    [Tooltip("Ile golda dawaæ na interwa³")]
    [SerializeField] private int goldPerTick = 5;

    [Tooltip("Czas pomiêdzy dotacjami (sekundy)")]
    [SerializeField] private float interval = 2f;

    private bool playerInZone = false;
    private float timer = 0f;

    private void Update()
    {
        if (playerInZone)
        {
            timer += Time.deltaTime;
            if (timer >= interval)
            {
                PlayerGold.Instance.AddGold(goldPerTick);
                timer = 0f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInZone = false;
    }
}
