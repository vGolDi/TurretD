using UnityEngine;
using TMPro;

namespace ElementumDefense.UI
{
    public class DamageNumber : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float fadeSpeed = 1f;

        [Header("Randomization")]
        [SerializeField] private float randomXOffset = 0.3f;
        [SerializeField] private float randomYOffset = 0.2f;

        [Header("Size Settings")] 
        [SerializeField] private float normalFontSize = 4f;    
        [SerializeField] private float criticalFontSize = 6f;

        private TextMeshPro textMesh;
        private float timer = 0f;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Color startColor;
        private Camera mainCamera;
        private DamageNumberManager manager; // Reference to manager

        private void Awake()
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        private void OnEnable()
        {
            // Reset on enable
            timer = 0f;

            if (textMesh != null)
            {
                startColor = textMesh.color;
            }
            FindCamera();
        }
        private void FindCamera()
        {
            if (mainCamera == null)
            {
                // Metoda 1: Szukaj po tagu (wymaga tagu "MainCamera" na kamerze gracza)
                mainCamera = Camera.main;

                // Metoda 2: Jeúli Camera.main nie dzia≥a, znajdü pierwszπ aktywnπ
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }

                // Debug
                if (mainCamera != null)
                {
                    Debug.Log($"[DamageNumber] Camera found: {mainCamera.name}");
                }
                else
                {
                    Debug.LogWarning("[DamageNumber] No camera found!");
                }
            }
        }
        public void Setup(int damage, DamageNumberType type, DamageNumberManager mgr)
        {
            manager = mgr;

            if (textMesh == null) return;

            // Set position
            startPosition = transform.position;

            // Random offset
            float randomX = Random.Range(-randomXOffset, randomXOffset);
            float randomY = Random.Range(0f, randomYOffset);
            targetPosition = startPosition + new Vector3(randomX, moveSpeed + randomY, 0f);

            // Set text
            string prefix = type == DamageNumberType.Critical ? "CRIT! " : "";
            textMesh.text = $"{prefix}-{damage}";

            // Set color
            Color color = type switch
            {
                DamageNumberType.Normal => Color.white,
                DamageNumberType.Critical => new Color(1f, 0.8f, 0f),
                DamageNumberType.Effective => Color.green,
                DamageNumberType.Resisted => Color.red,
                DamageNumberType.Heal => Color.cyan,
                _ => Color.white
            };

            textMesh.color = color;
            startColor = color;

            // Set size
            float fontSize = type == DamageNumberType.Critical ? criticalFontSize : normalFontSize;
            textMesh.fontSize = fontSize;
            if (type == DamageNumberType.Critical)
            {
                transform.localScale = Vector3.one * 1.2f;
            }
            else
            {
                transform.localScale = Vector3.one;
            }

            // Force settings
            textMesh.alignment = TMPro.TextAlignmentOptions.Center;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float progress = timer / lifetime;

            // Move upward
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

            // Billboard (face camera)
            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }

            // Fade out
            if (textMesh != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(1f, 0f, progress * fadeSpeed);
                textMesh.color = color;
            }

            // Return to pool when done
            if (timer >= lifetime)
            {
                if (manager != null)
                {
                    manager.ReturnToPool(gameObject);
                }
                else
                {
                    Destroy(gameObject); // Fallback
                }
            }
        }
    }

    public enum DamageNumberType
    {
        Normal,
        Critical,
        Effective,
        Resisted,
        Heal
    }
}