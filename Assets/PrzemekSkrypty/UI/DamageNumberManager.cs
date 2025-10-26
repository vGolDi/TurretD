using UnityEngine;
using System.Collections.Generic;

namespace ElementumDefense.UI
{
    public class DamageNumberManager : MonoBehaviour
    {
        public static DamageNumberManager Instance { get; private set; }

        [Header("Prefab")]
        [SerializeField] private GameObject damageNumberPrefab;

        [Header("Pooling")]
        [SerializeField] private int poolSize = 50;

        private Queue<GameObject> availablePool = new Queue<GameObject>();
        private List<GameObject> activeNumbers = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializePool();
        }

        private void InitializePool()
        {
            if (damageNumberPrefab == null)
            {
                Debug.LogError("[DamageNumberManager] Damage number prefab not assigned!");
                return;
            }

            // Create pool objects
            for (int i = 0; i < poolSize; i++)
            {
                CreateNewDamageNumber();
            }

            Debug.Log($"[DamageNumberManager] Pool initialized with {poolSize} damage numbers");
        }

        private void CreateNewDamageNumber()
        {
            // ========== KLUCZOWE: Spawn BEZ PARENTA! ==========
            GameObject dmgNum = Instantiate(damageNumberPrefab, Vector3.zero, Quaternion.identity);
            dmgNum.SetActive(false);
            availablePool.Enqueue(dmgNum);
            // ==================================================
        }

        public void ShowDamageNumber(Vector3 worldPosition, int damage, DamageNumberType type = DamageNumberType.Normal)
        {
            GameObject dmgNum = null;

            if (availablePool.Count > 0)
            {
                dmgNum = availablePool.Dequeue();
            }
            else
            {
                Debug.LogWarning("[DamageNumberManager] Pool exhausted! Creating new.");
                dmgNum = Instantiate(damageNumberPrefab, Vector3.zero, Quaternion.identity);
            }

            // ========== KLUCZOWE: Set position BEFORE activate ==========
            dmgNum.transform.position = worldPosition;
            dmgNum.transform.rotation = Quaternion.identity;
            // ===========================================================

            dmgNum.SetActive(true);

            DamageNumber damageScript = dmgNum.GetComponent<DamageNumber>();
            if (damageScript != null)
            {
                damageScript.Setup(damage, type, this); // Pass manager reference!
            }

            activeNumbers.Add(dmgNum);
        }

        public void ShowDamageNumberAtEnemy(EnemyHealth enemy, int damage, DamageNumberType type = DamageNumberType.Normal)
        {
            if (enemy == null) return;

            Vector3 position = enemy.transform.position + Vector3.up * 2f;
            ShowDamageNumber(position, damage, type);
        }

        // ========== NOWA FUNKCJA: Return to pool ==========
        public void ReturnToPool(GameObject dmgNum)
        {
            if (dmgNum == null) return;

            dmgNum.SetActive(false);
            activeNumbers.Remove(dmgNum);
            availablePool.Enqueue(dmgNum);
        }
        // ==================================================

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // Cleanup all
            foreach (var num in activeNumbers)
            {
                if (num != null) Destroy(num);
            }

            while (availablePool.Count > 0)
            {
                var num = availablePool.Dequeue();
                if (num != null) Destroy(num);
            }
        }
    }
}