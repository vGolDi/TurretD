using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace ElementumDefense.UI
{
    public class StarfieldInjector : MonoBehaviour
    {
        public static StarfieldInjector Instance
        {
            get; private set;
        }

        [Header("Particle Count (per container)")]
        [SerializeField] private int starCount = 80;
        [SerializeField] private int dustCount = 40;

        [Header("Star Settings")]
        [SerializeField] private float starMinSize = 1f;
        [SerializeField] private float starMaxSize = 3f;
        [SerializeField]
        private float starMinOpacity = 0.15f;
        [SerializeField]
        private float starMaxOpacity = 0.6f;

        [Header("Dust Settings")]
        [SerializeField] private float dustMinSize = 1f;
        [SerializeField] private float dustMaxSize = 2f;
        [SerializeField]
        private float dustMinOpacity = 0.05f;
        [SerializeField]
        private float dustMaxOpacity = 0.2f;

        [Header("Animation")]
        [SerializeField]
        private float twinkleSpeed = 0.5f;
        [SerializeField]
        private bool enableTwinkle = true;
        [SerializeField]
        private bool enableDrift = true;

        [Header("Colors")]
        [SerializeField]
        private Color starColor =
            new Color(0.9f, 0.85f, 0.7f);
        [SerializeField]
        private Color dustColor =
            new Color(0.6f, 0.55f, 0.45f);

        private const string LAYER_NAME =
            "starfield-managed";

        private struct Particle
        {
            public VisualElement el;
            public float baseOpacity;
            public float phase;
            public float rate;
            public float driftX;
            public float driftY;
            public bool isDust;
        }

        private class InjectedContainer
        {
            public VisualElement container;
            public VisualElement layer;
            public List<Particle> particles =
                new List<Particle>();
        }

        private Dictionary<VisualElement,
            InjectedContainer> activeContainers =
            new Dictionary<VisualElement,
                InjectedContainer>();

        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Register(
            VisualElement container)
        {
            if (container == null) return;
            if (activeContainers.ContainsKey(
                container))
                return;

            var ic = InjectInto(container);
            if (ic != null)
                activeContainers[container] = ic;
        }

        public void Unregister(
            VisualElement container)
        {
            if (container == null) return;
            if (activeContainers.TryGetValue(
                container, out var ic))
            {
                RemoveInjection(ic);
                activeContainers.Remove(container);
            }
        }

        private void Update()
        {
            if (activeContainers.Count == 0) return;
            if (!enableTwinkle && !enableDrift) return;

            float t = Time.time;

            foreach (var kvp in activeContainers)
            {
                var particles = kvp.Value.particles;

                for (int i = 0;
                    i < particles.Count; i++)
                {
                    var p = particles[i];
                    if (p.el == null) continue;

                    if (enableTwinkle)
                    {
                        float wave = Mathf.Sin(
                            t * p.rate + p.phase);
                        float n =
                            wave * 0.5f + 0.5f;
                        float alpha =
                            p.baseOpacity *
                            Mathf.Lerp(
                                0.2f, 1f, n);
                        p.el.style.opacity = alpha;
                    }

                    if (enableDrift && !p.isDust)
                    {
                        float dx = Mathf.Sin(
                            t * 0.07f * p.driftX +
                            p.phase) * 3f;
                        float dy = Mathf.Cos(
                            t * 0.05f * p.driftY +
                            p.phase) * 2f;
                        p.el.style.translate =
                            new StyleTranslate(
                                new Translate(
                                    dx, dy));
                    }
                }
            }
        }

        private InjectedContainer InjectInto(
            VisualElement container)
        {
            var old = container.Q<VisualElement>(
                LAYER_NAME);
            if (old != null)
                container.Remove(old);

            var layer = new VisualElement();
            layer.name = LAYER_NAME;
            layer.pickingMode = PickingMode.Ignore;
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.right = 0;
            layer.style.bottom = 0;
            layer.style.overflow = Overflow.Hidden;

            container.Insert(0, layer);

            var ic = new InjectedContainer
            {
                container = container,
                layer = layer
            };

            for (int i = 0; i < dustCount; i++)
                SpawnParticle(ic, true);
            for (int i = 0; i < starCount; i++)
                SpawnParticle(ic, false);

            return ic;
        }

        private void SpawnParticle(
            InjectedContainer ic, bool isDust)
        {
            float size, opacity;
            Color col;

            if (isDust)
            {
                size = Random.Range(
                    dustMinSize, dustMaxSize);
                opacity = Random.Range(
                    dustMinOpacity, dustMaxOpacity);
                col = dustColor;
            }
            else
            {
                size = Random.Range(
                    starMinSize, starMaxSize);
                opacity = Random.Range(
                    starMinOpacity, starMaxOpacity);
                col = starColor;
            }

            var dot = new VisualElement();
            dot.pickingMode = PickingMode.Ignore;
            dot.style.position = Position.Absolute;
            dot.style.width = size;
            dot.style.height = size;

            float r = size * 0.5f;
            dot.style.borderTopLeftRadius = r;
            dot.style.borderTopRightRadius = r;
            dot.style.borderBottomLeftRadius = r;
            dot.style.borderBottomRightRadius = r;

            dot.style.backgroundColor =
                new StyleColor(col);
            dot.style.opacity = opacity;

            dot.style.left = new StyleLength(
                new Length(
                    Random.Range(1f, 99f),
                    LengthUnit.Percent));
            dot.style.top = new StyleLength(
                new Length(
                    Random.Range(1f, 99f),
                    LengthUnit.Percent));

            ic.layer.Add(dot);

            ic.particles.Add(new Particle
            {
                el = dot,
                baseOpacity = opacity,
                phase = Random.Range(
                    0f, Mathf.PI * 2f),
                rate = Random.Range(
                    twinkleSpeed * 0.4f,
                    twinkleSpeed * 2.5f),
                driftX = Random.Range(0.2f, 0.8f),
                driftY = Random.Range(0.2f, 0.8f),
                isDust = isDust
            });
        }

        private void RemoveInjection(
            InjectedContainer ic)
        {
            if (ic.layer?.parent != null)
                ic.layer.parent.Remove(ic.layer);
            ic.particles.Clear();
        }

        private void OnDestroy()
        {
            foreach (var kvp in activeContainers)
                RemoveInjection(kvp.Value);
            activeContainers.Clear();
        }
    }
}