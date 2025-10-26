using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ElementumDefense.StatusEffects;

namespace ElementumDefense.UI
{
    public class StatusEffectDisplay : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float yOffset = 1.8f;
        [SerializeField] private float iconSize = 0.3f; // World space size!
        [SerializeField] private float iconSpacing = 0.4f; // World space spacing!

        [Header("Visual Settings")]
        [SerializeField] private bool showDurationBar = true;

        private StatusEffectManager effectManager;
        private Camera mainCamera;

        // Active icon instances
        private Dictionary<StatusEffectType, GameObject> activeIcons = new Dictionary<StatusEffectType, GameObject>();

        private void Awake()
        {
            effectManager = GetComponent<StatusEffectManager>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (effectManager == null) return;

            UpdateIcons();
            BillboardIcons();
        }

        private void UpdateIcons()
        {
            List<StatusEffect> activeEffects = effectManager.GetActiveEffects();

            // Track which effects are currently active
            HashSet<StatusEffectType> currentEffects = new HashSet<StatusEffectType>();

            // Update/create icons
            for (int i = 0; i < activeEffects.Count; i++)
            {
                StatusEffect effect = activeEffects[i];
                currentEffects.Add(effect.EffectType);

                if (!activeIcons.ContainsKey(effect.EffectType))
                {
                    CreateIcon(effect, i);
                }
                else
                {
                    UpdateIconPosition(effect.EffectType, i, activeEffects.Count);
                    UpdateIconDuration(effect.EffectType, effect.GetProgress());
                }
            }

            // Remove expired icons
            List<StatusEffectType> toRemove = new List<StatusEffectType>();

            foreach (var kvp in activeIcons)
            {
                if (!currentEffects.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var type in toRemove)
            {
                RemoveIcon(type);
            }
        }

        private void CreateIcon(StatusEffect effect, int index)
        {
            // ========== KLUCZOWE: Create as 3D Quad, NOT UI! ==========
            GameObject iconObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            iconObj.name = $"Icon_{effect.EffectType}";

            // Remove collider (don't need it)
            Destroy(iconObj.GetComponent<Collider>());

            // Set position (local to enemy)
            UpdateIconPosition(effect.EffectType, index, index + 1);

            // Set color based on effect type
            Renderer renderer = iconObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = GetColorForEffect(effect.EffectType);
            }

            // Scale
            iconObj.transform.localScale = Vector3.one * iconSize;

            // Parent to enemy (so it follows)
            iconObj.transform.SetParent(transform);

            // Optional: Duration bar
            GameObject durationBar = null;
            if (showDurationBar)
            {
                durationBar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                durationBar.name = "DurationBar";
                Destroy(durationBar.GetComponent<Collider>());

                durationBar.transform.SetParent(iconObj.transform);
                durationBar.transform.localPosition = new Vector3(0f, -iconSize * 0.7f, 0f);
                durationBar.transform.localScale = new Vector3(1f, 0.1f, 1f);

                Renderer barRenderer = durationBar.GetComponent<Renderer>();
                if (barRenderer != null)
                {
                    barRenderer.material = new Material(Shader.Find("Sprites/Default"));
                    barRenderer.material.color = Color.yellow;
                }
            }

            // Add to dictionary
            activeIcons.Add(effect.EffectType, iconObj);

            Debug.Log($"[StatusEffectDisplay] Created icon for {effect.EffectType}");
        }

        private void UpdateIconPosition(StatusEffectType type, int index, int totalCount)
        {
            if (!activeIcons.ContainsKey(type)) return;

            GameObject iconObj = activeIcons[type];

            // Calculate position
            float totalWidth = (totalCount - 1) * iconSpacing;
            float startX = -totalWidth / 2f;
            float xPos = startX + (index * iconSpacing);

            // Set local position relative to enemy
            iconObj.transform.localPosition = new Vector3(xPos, yOffset, 0f);
        }

        private void UpdateIconDuration(StatusEffectType type, float progress)
        {
            if (!activeIcons.ContainsKey(type)) return;

            GameObject iconObj = activeIcons[type];
            Transform durationBar = iconObj.transform.Find("DurationBar");

            if (durationBar != null)
            {
                // Update bar scale (fill amount)
                Vector3 scale = durationBar.localScale;
                scale.x = progress; // 1.0 = full, 0.0 = empty
                durationBar.localScale = scale;
            }
        }

        private void BillboardIcons()
        {
            if (mainCamera == null) return;

            // Make all icons face camera
            foreach (var kvp in activeIcons)
            {
                GameObject iconObj = kvp.Value;
                if (iconObj != null)
                {
                    iconObj.transform.LookAt(iconObj.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                            mainCamera.transform.rotation * Vector3.up);
                }
            }
        }

        private void RemoveIcon(StatusEffectType type)
        {
            if (activeIcons.ContainsKey(type))
            {
                Destroy(activeIcons[type]);
                activeIcons.Remove(type);

                Debug.Log($"[StatusEffectDisplay] Removed icon for {type}");
            }
        }

        private Color GetColorForEffect(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Burn => new Color(1f, 0.3f, 0f), // Orange
                StatusEffectType.Freeze => new Color(0.5f, 0.9f, 1f), // Cyan
                StatusEffectType.Slow => new Color(0.3f, 0.5f, 1f), // Blue
                StatusEffectType.Poison => new Color(0.3f, 0.8f, 0.3f), // Green
                StatusEffectType.Stun => Color.yellow,
                _ => Color.white
            };
        }

        private void OnDestroy()
        {
            // Cleanup icons
            foreach (var kvp in activeIcons)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            activeIcons.Clear();
        }
    }
}