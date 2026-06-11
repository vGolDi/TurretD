using System;
using UnityEngine;
using ElementumDefense.Players;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Klikalna zbroja. Dopóki armorStacks > 0 wróg jest IGNOROWANY przez wieżyczki
    /// i nie przyjmuje obrażeń (TakeDamage zwraca natychmiast). Gracz musi go ręcznie
    /// kliknąć X razy, żeby zdjąć kolejne warstwy. Po zejściu do 0 wróg staje się
    /// normalnym celem.
    ///
    /// UWAGA: nie zarządza UI samodzielnie - emituje eventy, do których wpina się
    /// healthbar / oddzielny ArmorIndicator.
    /// </summary>
    public class EnemyArmor : MonoBehaviour, IEnemyPoolable
    {
        // Globalny rejestr wszystkich aktywnych opancerzonych wrogów - używany
        // przez InteractionManager do screen-space fallback (znajdowanie wroga
        // najbliższego kursorowi w pikselach). HashSet, bo dodajemy/usuwamy
        // częściej niż iterujemy, a iteracja po HashSet jest też szybka.
        public static readonly System.Collections.Generic.HashSet<EnemyArmor> AllArmored
            = new System.Collections.Generic.HashSet<EnemyArmor>();

        [Header("Armor")]
        [SerializeField, Tooltip("Ile kliknięć potrzeba do zdjęcia zbroi")]
        private int armorStacks = 3;

        [Tooltip("Czy AOE/efekty obszarowe powinny łamać armor (ZALECANE: false - " +
                 "armor to mechanika dla single-target). Jeśli true, każdy hit zabiera stack.")]
        [SerializeField] private bool aoeBreaksArmor = false;

        [Header("Status Effects")]
        [Tooltip("Czy blokować również NAKŁADANIE status effects (Burn/Slow/Freeze/Curse...) " +
                 "gdy wróg jest opancerzony? ZALECANE: true - armor to pełna immunia, daje " +
                 "graczowi jasny sygnał że trzeba klikać. Ustaw false dla wrogów którzy " +
                 "mają dawać się np. spowalniać mimo armoru.")]
        [SerializeField] private bool blockStatusEffectsWhileArmored = true;

        [Header("Audio / VFX (opcjonalne)")]
        [SerializeField] private GameObject onClickVfx;
        [SerializeField] private GameObject onArmorBrokenVfx;

        [Header("Debug")]
        [SerializeField, Tooltip("Włącz logi do konsoli (do diagnozy)")]
        private bool debugLogs = false;

        private int initialStacks;
        private int prefabStacks; // remembers original prefab inspector value, never mutated by sabotage

        public int ArmorStacks => armorStacks;
        public int InitialStacks => initialStacks;
        public bool IsArmored => armorStacks > 0;
        public bool BlockStatusEffectsWhileArmored => blockStatusEffectsWhileArmored;

        /// <summary>Emituje stack count po każdej zmianie (UI HP bar).</summary>
        public event Action<int, int> OnArmorChanged; // (current, max)
        /// <summary>Emituje gdy armor spadnie do 0 (gracz może go normalnie atakować).</summary>
        public event Action OnArmorBroken;

        private void Awake()
        {
            initialStacks = armorStacks;
            prefabStacks = armorStacks;
        }

        // ==========================================
        // POOLING
        // ==========================================

        /// <summary>Restore armor to prefab defaults before re-enable.</summary>
        public void OnSpawnedFromPool()
        {
            // Restore to ORIGINAL prefab value, not the last sabotage value.
            armorStacks = prefabStacks;
            initialStacks = prefabStacks;
            // OnEnable runs after this; AllArmored will re-add through that path.
        }

        public void OnReturnedToPool()
        {
            // OnDisable already removes us from AllArmored.
        }

        private void OnEnable()
        {
            if (IsArmored) AllArmored.Add(this);
        }

        private void OnDisable()
        {
            AllArmored.Remove(this);
        }

        private void Start()
        {
            // Powiadom UI o stanie startowym
            OnArmorChanged?.Invoke(armorStacks, initialStacks);
        }

        /// <summary>
        /// Wywoływane przez InteractionManager przy kliknięciu LMB w tego wroga.
        /// </summary>
        /// <summary>
        /// Sabotage entry point: arms this enemy with the given number of armor
        /// stacks for one wave. Pool reset (<see cref="OnSpawnedFromPool"/>) wipes
        /// it on next reuse.
        /// </summary>
        public void ApplyFromSabotage(int stacks)
        {
            if (stacks <= 0) return;
            armorStacks = stacks;
            initialStacks = stacks; // so UI reflects the new max
            if (gameObject.activeInHierarchy && IsArmored)
                AllArmored.Add(this);
            OnArmorChanged?.Invoke(armorStacks, initialStacks);
        }

        public void OnPlayerClicked()
        {
            if (debugLogs)
                Debug.Log($"[EnemyArmor:{name}] OnPlayerClicked - stacksBefore={armorStacks}, IsArmored={IsArmored}");

            if (!IsArmored)
            {
                if (debugLogs)
                    Debug.Log($"[EnemyArmor:{name}] OnPlayerClicked IGNORED - już nieopancerzony");
                return;
            }

            armorStacks--;
            OnArmorChanged?.Invoke(armorStacks, initialStacks);
            if (debugLogs)
                Debug.Log($"[EnemyArmor:{name}] Stack zdjęty, pozostało={armorStacks}");

            if (onClickVfx != null)
            {
                Instantiate(onClickVfx, transform.position + Vector3.up, Quaternion.identity);
            }

            if (armorStacks <= 0)
            {
                BreakArmor();
            }
        }

        private void BreakArmor()
        {
            armorStacks = 0;
            AllArmored.Remove(this); // już nie potrzebny w rejestrze fallback
            if (debugLogs)
                Debug.Log($"[EnemyArmor:{name}] ARMOR BROKEN - od teraz wieżyczki widzą tego wroga");
            if (onArmorBrokenVfx != null)
            {
                Instantiate(onArmorBrokenVfx, transform.position + Vector3.up, Quaternion.identity);
            }
            OnArmorBroken?.Invoke();
        }

        /// <summary>
        /// Awaryjny callback dla AOE/projectile, gdyby flaga aoeBreaksArmor była włączona.
        /// </summary>
        public void NotifyHitFromAOE()
        {
            if (!aoeBreaksArmor || !IsArmored) return;
            armorStacks--;
            OnArmorChanged?.Invoke(armorStacks, initialStacks);
            if (armorStacks <= 0) BreakArmor();
        }
    }
}
