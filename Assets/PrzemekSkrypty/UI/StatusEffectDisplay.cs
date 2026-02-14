using UnityEngine;
using System.Collections.Generic;
using ElementumDefense.StatusEffects;

namespace ElementumDefense.UI
{
    public class StatusEffectDisplay : MonoBehaviour
    {
        [Header("Positioning")]
        [SerializeField] private float yOffset = 2.0f;
        [SerializeField] private float iconSize = 0.5f;
        [SerializeField] private float iconSpacing = 0.6f;

        [Header("Duration Bar Visuals")]
        [SerializeField] private bool showDurationBar = true;
        [SerializeField] private float durationBarHeight = 0.08f;
        [SerializeField] private float durationBarWidthRatio = 1.0f;
        [SerializeField] private float durationBarYOffset = -0.4f;
        [SerializeField] private Color durationBarColor = Color.green;
        [SerializeField] private Color durationBarBackground = new Color(0, 0, 0, 0.5f);

        [Header("Effect Icons")]
        [SerializeField] private StatusEffectIcon[] effectIcons;

        private StatusEffectManager effectManager;
        private Camera mainCamera;
        private Transform iconContainer;

        private Dictionary<StatusEffectType, GameObject> activeIcons = new Dictionary<StatusEffectType, GameObject>();

        // ========== CACHE na sprite – żeby nie tworzyć nowego przy każdym uzyciu ==========
        private Sprite _cachedBarSprite;

        [System.Serializable]
        public class StatusEffectIcon
        {
            public StatusEffectType effectType;
            public Sprite icon;
            public Color tintColor = Color.white;
        }

        private void Awake()
        {
            effectManager = GetComponent<StatusEffectManager>();
            mainCamera = Camera.main;

            GameObject containerObj = new GameObject("StatusIconsContainer");
            iconContainer = containerObj.transform;
            iconContainer.SetParent(transform);
            iconContainer.localPosition = new Vector3(0, yOffset, 0);
        }

        private void OnEnable()
        {
            ClearIcons();
        }

        private void Update()
        {
            if (effectManager == null) return;
            if (mainCamera == null) mainCamera = Camera.main;

            if (mainCamera != null && iconContainer != null)
            {
                iconContainer.rotation = mainCamera.transform.rotation;
            }

            UpdateIcons();
        }

        private void UpdateIcons()
        {
            List<StatusEffect> activeEffects = effectManager.GetActiveEffects();
            HashSet<StatusEffectType> currentEffects = new HashSet<StatusEffectType>();

            for (int i = 0; i < activeEffects.Count; i++)
            {
                StatusEffect effect = activeEffects[i];
                currentEffects.Add(effect.EffectType);

                bool iconExists = activeIcons.ContainsKey(effect.EffectType)
                               && activeIcons[effect.EffectType] != null;

                if (!iconExists)
                {
                    if (activeIcons.ContainsKey(effect.EffectType))
                        activeIcons.Remove(effect.EffectType);

                    CreateIcon(effect, i, activeEffects.Count);
                }
                else
                {
                    UpdateIconPosition(effect.EffectType, i, activeEffects.Count);
                    UpdateIconVisuals(effect.EffectType, effect);
                }
            }

            List<StatusEffectType> toRemove = new List<StatusEffectType>();
            foreach (var kvp in activeIcons)
            {
                if (!currentEffects.Contains(kvp.Key) || kvp.Value == null)
                    toRemove.Add(kvp.Key);
            }
            foreach (var type in toRemove)
            {
                RemoveIcon(type);
            }
        }

        private void CreateIcon(StatusEffect effect, int index, int totalCount)
        {
            GameObject iconObj = new GameObject($"Icon_{effect.EffectType}");
            iconObj.transform.SetParent(iconContainer, false);

            // ===== Sprite ikony =====
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(iconObj.transform, false);
            spriteObj.transform.localScale = Vector3.one * iconSize;

            SpriteRenderer spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 100;

            StatusEffectIcon iconConfig = GetIconConfig(effect.EffectType);
            if (iconConfig != null && iconConfig.icon != null)
            {
                spriteRenderer.sprite = iconConfig.icon;
                spriteRenderer.color = iconConfig.tintColor;
            }
            else
            {
                spriteRenderer.sprite = GetBarSprite();
            }

            // ===== Duration bar =====
            if (showDurationBar)
            {
                CreateDurationBar(iconObj.transform);
            }

            // ===== Stack counter =====
            if (effect.IsStackable && effect.StackCount > 1)
            {
                CreateStackCounter(iconObj.transform, effect.StackCount);
            }

            // FIX: Dodajemy do słownika PRZED wywołaniem UpdateIconPosition
            activeIcons[effect.EffectType] = iconObj;
            UpdateIconPosition(effect.EffectType, index, totalCount);
        }

        private void CreateDurationBar(Transform parent)
        {
            float width = iconSize * durationBarWidthRatio;

            // Pivot – punkt odniesienia na lewej krawędzi paska
            GameObject barPivot = new GameObject("BarPivot");
            barPivot.transform.SetParent(parent, false);
            barPivot.transform.localPosition = new Vector3(-width / 2f, durationBarYOffset, 0f);

            Sprite barSprite = GetBarSprite(); // 1x1 unit sprite

            // ===== Tło (Background) – statyczne, zawsze pełna szerokość =====
            GameObject barBg = new GameObject("BarBG");
            barBg.transform.SetParent(barPivot.transform, false);
            // Lewa krawędź na pivocie (x=0), środek przesuniemy o width/2
            barBg.transform.localPosition = new Vector3(width / 2f, 0f, 0.01f);
            barBg.transform.localScale = new Vector3(width, durationBarHeight, 1f);

            SpriteRenderer bgRenderer = barBg.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = barSprite;
            bgRenderer.color = durationBarBackground;
            bgRenderer.sortingOrder = 90;

            // ===== Wypełnienie (Fill) – dynamiczne =====
            GameObject barFill = new GameObject("BarFill");
            barFill.transform.SetParent(barPivot.transform, false);
            // Startowo: pełna szerokość, wycentrowane tak samo jak BG
            barFill.transform.localPosition = new Vector3(width / 2f, 0f, -0.01f);
            barFill.transform.localScale = new Vector3(width, durationBarHeight, 1f);

            SpriteRenderer fillRenderer = barFill.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = barSprite;
            fillRenderer.color = durationBarColor;
            fillRenderer.sortingOrder = 95;
        }

        private void UpdateIconVisuals(StatusEffectType type, StatusEffect effect)
        {
            if (!activeIcons.ContainsKey(type)) return;
            GameObject iconObj = activeIcons[type];
            if (iconObj == null) return;

            // ===== Duration bar update =====
            if (showDurationBar)
            {
                Transform barPivot = iconObj.transform.Find("BarPivot");
                if (barPivot != null)
                {
                    Transform barFill = barPivot.Find("BarFill");
                    if (barFill != null)
                    {
                        float progress = effect.GetProgress();
                        float maxWidth = iconSize * durationBarWidthRatio;
                        float currentWidth = maxWidth * progress;

                        // Skalujemy szerokość
                        Vector3 scale = barFill.localScale;
                        scale.x = currentWidth;
                        barFill.localScale = scale;

                        // Przesuwamy środek tak, żeby lewa krawędź była na x=0 pivota
                        // Lewa krawędź = pos.x - (spriteNativeWidth * scale.x / 2)
                        // Sprite ma natywną szerokość 1 (PPU=1, 1px), więc:
                        // Lewa krawędź = pos.x - currentWidth/2
                        // Chcemy lewa krawędź = 0, więc pos.x = currentWidth/2
                        Vector3 pos = barFill.localPosition;
                        pos.x = currentWidth / 2f;
                        barFill.localPosition = pos;
                    }
                }
            }

            // ===== Miganie ikony gdy efekt się kończy =====
            Transform spriteTransform = iconObj.transform.Find("Sprite");
            if (spriteTransform != null)
            {
                SpriteRenderer sr = spriteTransform.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (effect.RemainingDuration < 1.0f)
                    {
                        float alpha = Mathf.PingPong(Time.time * 5f, 1f) * 0.5f + 0.5f;
                        Color c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }
                    else
                    {
                        Color c = sr.color;
                        c.a = 1f;
                        sr.color = c;
                    }
                }
            }
        }

        private void UpdateIconPosition(StatusEffectType type, int index, int totalCount)
        {
            if (!activeIcons.ContainsKey(type)) return;
            GameObject iconObj = activeIcons[type];
            if (iconObj == null) return;

            float totalWidth = (totalCount - 1) * iconSpacing;
            float startX = -totalWidth / 2f;
            float xPos = startX + (index * iconSpacing);

            iconObj.transform.localPosition = new Vector3(xPos, 0f, 0f);
            iconObj.transform.localRotation = Quaternion.identity;
        }

        private void RemoveIcon(StatusEffectType type)
        {
            if (activeIcons.ContainsKey(type))
            {
                if (activeIcons[type] != null) Destroy(activeIcons[type]);
                activeIcons.Remove(type);
            }
        }

        private void ClearIcons()
        {
            foreach (var kvp in activeIcons)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            activeIcons.Clear();
        }

        // ===================== Helpers =====================

        private void CreateStackCounter(Transform parent, int stackCount)
        {
            GameObject textObj = new GameObject("StackCount");
            textObj.transform.SetParent(parent, false);
            textObj.transform.localPosition = new Vector3(iconSize * 0.4f, iconSize * 0.4f, -0.1f);
            textObj.transform.localScale = Vector3.one * 0.15f;

            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = stackCount.ToString();
            tm.fontSize = 24;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
            textObj.GetComponent<MeshRenderer>().sortingOrder = 110;
        }

        private StatusEffectIcon GetIconConfig(StatusEffectType type)
        {
            foreach (var config in effectIcons)
                if (config.effectType == type) return config;
            return null;
        }

        /// <summary>
        /// Tworzy biały sprite 1×1 z PPU=1, 
        /// dzięki czemu localScale bezpośrednio odpowiada rozmiarowi w jednostkach świata.
        /// Wynik jest cache'owany – tylko jedna alokacja.
        /// </summary>
        private Sprite GetBarSprite()
        {
            if (_cachedBarSprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                // PPU = 1 → 1 piksel = 1 jednostka → scale = rozmiar w world units
                _cachedBarSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return _cachedBarSprite;
        }
    }
}