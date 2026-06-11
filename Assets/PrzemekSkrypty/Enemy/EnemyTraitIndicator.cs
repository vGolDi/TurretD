using UnityEngine;
using UnityEngine.UI;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Pokazuje nad healthbarem wroga ikonki opisujące jego "specjalne" cechy:
    /// - Armor (tarcza + licznik pozostałych kliknięć, ukrywa się po zbiciu)
    /// - Split on Death (ikonka rozdwajania)
    /// - Dowolne dodatkowe trait-y w przyszłości (Revive, Boss, Cloak...)
    ///
    /// Komponent BUDUJE SAM swój world-space canvas w Awake - nie wymaga prefabu.
    /// Sprity są opcjonalne (fallback: kolorowe kwadraty z literą).
    ///
    /// Jak użyć: dodaj komponent na prefab wroga obok EnemyArmor / EnemySplitOnDeath
    /// i podaj opcjonalne sprite w inspektorze.
    /// </summary>
    public class EnemyTraitIndicator : MonoBehaviour
    {
        [Header("Position")]
        [Tooltip("Offset od pozycji wroga (w world-space, Y=wysokość nad głową)")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.4f, 0f);

        [Tooltip("Rozmiar pojedynczej ikonki w world units")]
        [SerializeField] private float iconSize = 0.4f;

        [Tooltip("Odstęp pomiędzy ikonkami")]
        [SerializeField] private float iconSpacing = 0.05f;

        [Header("Icons (opcjonalne - fallback to kolorowe kwadraty)")]
        [SerializeField] private Sprite armorIcon;
        [SerializeField] private Sprite splitIcon;
        [SerializeField] private Sprite reviveIcon;

        [Header("Colors")]
        [SerializeField] private Color armorColor = new Color(0.85f, 0.85f, 0.95f, 1f);
        [SerializeField] private Color splitColor = new Color(1f, 0.55f, 0.2f, 1f);
        [SerializeField] private Color reviveColor = new Color(0.7f, 0.3f, 0.9f, 1f); // fioletowy/feniks
        [SerializeField] private Color textColor = Color.white;

        [Header("Optional label (np. 'ELITE', 'BOSS')")]
        [SerializeField] private string customLabel = "";

        // Runtime
        private Canvas canvas;
        private RectTransform iconsContainer;
        private Camera cam;

        private EnemyArmor armor;
        private EnemySplitOnDeath splitter;
        private EnemyReviveOnDeath reviver;

        private GameObject armorIconObj;
        private Text armorStackText;
        private GameObject splitIconObj;
        private GameObject reviveIconObj;
        private Text labelText;

        private void Awake()
        {
            armor = GetComponent<EnemyArmor>();
            splitter = GetComponent<EnemySplitOnDeath>();
            reviver = GetComponent<EnemyReviveOnDeath>();

            // Nic do pokazania → samozniszczenie
            if (armor == null && splitter == null && reviver == null && string.IsNullOrEmpty(customLabel))
            {
                Destroy(this);
                return;
            }

            BuildCanvas();
            BuildIcons();

            if (armor != null)
            {
                armor.OnArmorChanged += HandleArmorChanged;
                armor.OnArmorBroken += HandleArmorBroken;
            }
        }

        private void Start()
        {
            cam = Camera.main;
            // Initial sync (Start uruchomi się po Start z EnemyArmor który emituje event,
            // ale dla pewności sami też ustawiamy tekst)
            if (armor != null && armorStackText != null)
            {
                armorStackText.text = armor.ArmorStacks.ToString();
            }
        }

        private void OnDestroy()
        {
            if (armor != null)
            {
                armor.OnArmorChanged -= HandleArmorChanged;
                armor.OnArmorBroken -= HandleArmorBroken;
            }
        }

        private void LateUpdate()
        {
            if (canvas == null) return;

            // Pozycja podążająca za wrogiem
            canvas.transform.position = transform.position + worldOffset;

            // Billboard do kamery (jak HealthBar)
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                canvas.transform.LookAt(
                    canvas.transform.position + cam.transform.rotation * Vector3.forward,
                    cam.transform.rotation * Vector3.up);
            }
        }

        // ==========================================
        // BUILD UI
        // ==========================================

        private void BuildCanvas()
        {
            GameObject canvasGo = new GameObject($"{name}_TraitCanvas");
            canvasGo.transform.SetParent(null); // world-space, niezależny od skali wroga
            canvasGo.transform.position = transform.position + worldOffset;

            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100f;

            RectTransform canvasRT = canvas.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(2f, 1f);
            canvasRT.localScale = Vector3.one * 0.01f; // standard dla world-space UI

            // Zniszcz canvas razem z wrogiem
            var followCleanup = canvasGo.AddComponent<TraitCanvasCleanup>();
            followCleanup.owner = transform;

            // Kontener na ikonki (HorizontalLayoutGroup)
            GameObject containerGo = new GameObject("Icons");
            containerGo.transform.SetParent(canvasGo.transform, false);
            iconsContainer = containerGo.AddComponent<RectTransform>();
            iconsContainer.anchorMin = new Vector2(0.5f, 0.5f);
            iconsContainer.anchorMax = new Vector2(0.5f, 0.5f);
            iconsContainer.sizeDelta = new Vector2(200f, 60f);
            iconsContainer.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup hlg = containerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = iconSpacing * 100f; // canvas units
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
        }

        private void BuildIcons()
        {
            float iconUnits = iconSize * 100f; // canvas units

            // Armor icon
            if (armor != null)
            {
                armorIconObj = CreateIcon("ArmorIcon", armorIcon, armorColor, iconUnits);
                armorStackText = CreateOverlayText(armorIconObj, armor.ArmorStacks.ToString(), iconUnits);
            }

            // Split icon
            if (splitter != null)
            {
                splitIconObj = CreateIcon("SplitIcon", splitIcon, splitColor, iconUnits);
                if (splitIcon == null)
                {
                    // fallback "X" - litera oznaczająca split
                    CreateOverlayText(splitIconObj, "✂", iconUnits);
                }
            }

            // Revive icon
            if (reviver != null)
            {
                reviveIconObj = CreateIcon("ReviveIcon", reviveIcon, reviveColor, iconUnits);
                if (reviveIcon == null)
                {
                    // fallback - feniks/strzałka w górę
                    CreateOverlayText(reviveIconObj, "↺", iconUnits);
                }
            }

            // Custom label
            if (!string.IsNullOrEmpty(customLabel))
            {
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(iconsContainer, false);
                RectTransform rt = labelGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(iconUnits * 2f, iconUnits * 0.5f);

                labelText = labelGo.AddComponent<Text>();
                labelText.text = customLabel;
                labelText.color = textColor;
                labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelText.fontSize = 28;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.fontStyle = FontStyle.Bold;
            }
        }

        private GameObject CreateIcon(string objName, Sprite sprite, Color fallbackColor, float size)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(iconsContainer, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            Image img = go.AddComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
            }
            else
            {
                // Fallback: kolorowy kwadrat z lekkim okrągłym zaokrągleniem (przez canvas alpha)
                img.color = fallbackColor;
            }
            return go;
        }

        private Text CreateOverlayText(GameObject parent, string text, float size)
        {
            GameObject txtGo = new GameObject("Count");
            txtGo.transform.SetParent(parent.transform, false);

            RectTransform rt = txtGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Text t = txtGo.AddComponent<Text>();
            t.text = text;
            t.color = textColor;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = Mathf.RoundToInt(size * 0.55f);
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;

            // Kontur dla czytelności na każdym tle
            Outline outline = txtGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return t;
        }

        // ==========================================
        // EVENTS
        // ==========================================

        private void HandleArmorChanged(int current, int max)
        {
            if (armorStackText != null)
                armorStackText.text = current.ToString();
        }

        private void HandleArmorBroken()
        {
            // Po zbiciu zbroi ukryj samą ikonkę armora.
            // Pozostałe ikonki (split, label) zostają.
            if (armorIconObj != null)
            {
                armorIconObj.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Pomocniczy komponent - czyści światowy canvas gdy wróg umiera
    /// (canvas jest zaparentowany do null, więc Destroy(gameObject) na enemy
    /// nie zabiera go ze sobą).
    /// </summary>
    public class TraitCanvasCleanup : MonoBehaviour
    {
        public Transform owner;
        private void Update()
        {
            if (owner == null)
            {
                Destroy(gameObject);
            }
        }
    }
}
