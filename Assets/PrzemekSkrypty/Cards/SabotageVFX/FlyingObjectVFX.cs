using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Universal flying object VFX for sabotages.
    /// Spawns N copies of a 3D model that fly along a bezier arc,
    /// optionally dropping a secondary object mid-flight.
    /// 
    /// Usage: Create a prefab with this script as root.
    /// Set it as SabotageCardData.arenaVFXPrefab.
    /// SabotageVFXManager will spawn it, this script handles the rest.
    /// 
    /// Examples:
    ///   StealGold: bat model + gold bag drop
    ///   WaveBoss:  skull models spiraling up
    ///   AllIn:     coins flying in an arc
    /// </summary>
    public class FlyingObjectVFX : MonoBehaviour
    {
        [Header("=== FLYING OBJECTS ===")]
        [Tooltip("3D model prefab to fly (bat, skull, coin, etc.)")]
        [SerializeField] private GameObject flyingModelPrefab;

        [Tooltip("How many flying objects to spawn")]
        [SerializeField] private int spawnCount = 3;

        [Tooltip("Delay between spawning each object (stagger)")]
        [SerializeField] private float spawnInterval = 0.3f;

        [Tooltip("Total flight time per object")]
        [SerializeField] private float flightDuration = 2.5f;

        [Tooltip("Scale of spawned models")]
        [SerializeField] private float modelScale = 1f;

        [Header("=== FLIGHT PATH ===")]
        [Tooltip("How the objects fly")]
        [SerializeField] private FlightPattern flightPattern = FlightPattern.Arc;

        [Tooltip("Starting area radius (objects spawn randomly within this)")]
        [SerializeField] private float startRadius = 3f;

        [Tooltip("End area radius (where objects fly TO)")]
        [SerializeField] private float endRadius = 2f;

        [Tooltip("Height of the arc apex")]
        [SerializeField] private float arcHeight = 5f;

        [Tooltip("End offset from center (e.g. fly toward edge of arena)")]
        [SerializeField] private Vector3 endOffset = new Vector3(0f, 1f, 8f);

        [Tooltip("Add random rotation while flying")]
        [SerializeField] private bool rotateWhileFlying = true;

        [Tooltip("Rotation speed (degrees/sec)")]
        [SerializeField] private float rotationSpeed = 180f;

        [Header("=== DROP OBJECT (Optional) ===")]
        [Tooltip("Object dropped mid-flight (gold bag, bomb, etc.). Leave empty for no drop.")]
        [SerializeField] private GameObject dropPrefab;

        [Tooltip("When during flight to drop (0-1, e.g. 0.5 = halfway)")]
        [Range(0f, 1f)]
        [SerializeField] private float dropAtProgress = 0.5f;

        [Tooltip("Drop fall speed")]
        [SerializeField] private float dropFallSpeed = 8f;

        [Tooltip("Drop scale")]
        [SerializeField] private float dropScale = 0.7f;

        [Tooltip("Time before dropped object disappears")]
        [SerializeField] private float dropLifetime = 1.5f;

        [Header("=== TRAIL / PARTICLES (Optional) ===")]
        [Tooltip("Trail particle prefab attached to each flying object")]
        [SerializeField] private GameObject trailParticlePrefab;

        [Header("=== TIMING ===")]
        [Tooltip("Destroy entire VFX root after this time (0 = auto-calculate)")]
        [SerializeField] private float totalLifetime = 0f;

        [Tooltip("Delay before spawning starts")]
        [SerializeField] private float startDelay = 0f;

        // Runtime
        private List<GameObject> spawnedObjects = new List<GameObject>();

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Start()
        {
            StartCoroutine(RunVFXSequence());
        }

        private IEnumerator RunVFXSequence()
        {
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            // Spawn flying objects with stagger
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnFlyingObject(i);

                if (spawnInterval > 0f && i < spawnCount - 1)
                    yield return new WaitForSeconds(spawnInterval);
            }

            // Wait for all flights to finish, then self-destroy
            float autoLifetime = totalLifetime > 0f
                ? totalLifetime
                : startDelay + (spawnCount * spawnInterval) + flightDuration + 1f;

            yield return new WaitForSeconds(flightDuration + 1f);

            // Cleanup
            foreach (var obj in spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }

            Destroy(gameObject);
        }

        // ==========================================
        // SPAWN
        // ==========================================

        private void SpawnFlyingObject(int index)
        {
            if (flyingModelPrefab == null)
            {
                Debug.LogWarning("[FlyingObjectVFX] No flyingModelPrefab assigned!");
                return;
            }

            // Random start position within radius
            Vector2 randomCircle = Random.insideUnitCircle * startRadius;
            Vector3 startPos = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

            GameObject flyObj = Instantiate(flyingModelPrefab, startPos, Quaternion.identity, transform);
            flyObj.transform.localScale = Vector3.one * modelScale;
            spawnedObjects.Add(flyObj);

            // Attach trail particles if provided
            if (trailParticlePrefab != null)
            {
                Instantiate(trailParticlePrefab, flyObj.transform);
            }

            // Calculate end position
            Vector2 endCircle = Random.insideUnitCircle * endRadius;
            Vector3 endPos = transform.position + endOffset + new Vector3(endCircle.x, 0f, endCircle.y);

            // Start flight coroutine
            StartCoroutine(FlyAlongPath(flyObj, startPos, endPos, index));
        }

        // ==========================================
        // FLIGHT PATHS
        // ==========================================

        private IEnumerator FlyAlongPath(GameObject obj, Vector3 start, Vector3 end, int index)
        {
            float elapsed = 0f;
            bool hasDropped = false;

            // Pre-calculate bezier control point (arc apex)
            Vector3 midPoint = (start + end) / 2f;
            Vector3 controlPoint = GetControlPoint(start, end, midPoint, index);

            Quaternion initialRotation = obj.transform.rotation;

            while (elapsed < flightDuration)
            {
                if (obj == null) yield break;

                float t = elapsed / flightDuration;
                float easedT = EaseInOutCubic(t);

                // Position
                Vector3 pos = flightPattern switch
                {
                    FlightPattern.Arc => BezierPoint(start, controlPoint, end, easedT),
                    FlightPattern.Straight => Vector3.Lerp(start, end, easedT),
                    FlightPattern.Spiral => SpiralPoint(start, end, easedT, index),
                    FlightPattern.Wave => WavePoint(start, end, easedT, index),
                    _ => Vector3.Lerp(start, end, easedT)
                };

                obj.transform.position = pos;

                // Rotation
                if (rotateWhileFlying)
                {
                    obj.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

                    // Face direction of movement
                    if (t > 0.01f)
                    {
                        Vector3 nextPos = flightPattern switch
                        {
                            FlightPattern.Arc => BezierPoint(start, controlPoint, end,
                                Mathf.Min(1f, easedT + 0.02f)),
                            _ => Vector3.Lerp(start, end, Mathf.Min(1f, easedT + 0.02f))
                        };
                        Vector3 dir = (nextPos - pos).normalized;
                        if (dir.sqrMagnitude > 0.001f)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                            obj.transform.rotation = Quaternion.Slerp(
                                obj.transform.rotation, targetRot, 5f * Time.deltaTime);
                        }
                    }
                }

                // Drop object at specified progress
                if (!hasDropped && dropPrefab != null && t >= dropAtProgress)
                {
                    hasDropped = true;
                    SpawnDropObject(obj.transform.position);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // End of flight — fade out or destroy
            if (obj != null)
            {
                // Quick scale-down
                float fadeTime = 0.3f;
                float fadeElapsed = 0f;
                Vector3 origScale = obj.transform.localScale;

                while (fadeElapsed < fadeTime && obj != null)
                {
                    fadeElapsed += Time.deltaTime;
                    float s = 1f - (fadeElapsed / fadeTime);
                    obj.transform.localScale = origScale * s;
                    yield return null;
                }

                if (obj != null) Destroy(obj);
            }
        }

        // ==========================================
        // DROP
        // ==========================================

        private void SpawnDropObject(Vector3 dropPosition)
        {
            if (dropPrefab == null) return;

            GameObject drop = Instantiate(dropPrefab, dropPosition, Quaternion.identity, transform);
            drop.transform.localScale = Vector3.one * dropScale;
            spawnedObjects.Add(drop);

            StartCoroutine(DropFallAndDestroy(drop));
        }

        private IEnumerator DropFallAndDestroy(GameObject drop)
        {
            float elapsed = 0f;
            Vector3 startPos = drop.transform.position;

            // Find ground level (raycast or assume y=0)
            float groundY = transform.position.y;

            while (elapsed < dropLifetime && drop != null)
            {
                elapsed += Time.deltaTime;

                // Fall with gravity
                float fallDistance = dropFallSpeed * elapsed * elapsed * 0.5f;
                Vector3 pos = startPos - new Vector3(0f, fallDistance, 0f);

                // Stop at ground
                if (pos.y <= groundY)
                {
                    pos.y = groundY;
                    drop.transform.position = pos;

                    // Bounce effect
                    yield return StartCoroutine(DropBounce(drop));
                    break;
                }

                // Slight tumble rotation
                drop.transform.Rotate(
                    Random.Range(-1f, 1f) * 200f * Time.deltaTime,
                    Random.Range(-1f, 1f) * 100f * Time.deltaTime,
                    Random.Range(-1f, 1f) * 150f * Time.deltaTime);

                drop.transform.position = pos;
                yield return null;
            }

            // Fade out
            if (drop != null)
            {
                float fadeTime = 0.5f;
                float fadeElapsed = 0f;
                Vector3 origScale = drop.transform.localScale;

                while (fadeElapsed < fadeTime && drop != null)
                {
                    fadeElapsed += Time.deltaTime;
                    drop.transform.localScale = origScale * (1f - fadeElapsed / fadeTime);
                    yield return null;
                }

                if (drop != null) Destroy(drop);
            }
        }

        private IEnumerator DropBounce(GameObject drop)
        {
            if (drop == null) yield break;

            Vector3 groundPos = drop.transform.position;
            float bounceHeight = 0.5f;
            float bounceTime = 0.3f;

            // Up
            float t = 0f;
            while (t < bounceTime && drop != null)
            {
                t += Time.deltaTime;
                float progress = t / bounceTime;
                float y = Mathf.Sin(progress * Mathf.PI) * bounceHeight;
                drop.transform.position = groundPos + Vector3.up * y;
                yield return null;
            }

            if (drop != null)
                drop.transform.position = groundPos;

            // Wait before fade
            yield return new WaitForSeconds(0.5f);
        }

        // ==========================================
        // MATH HELPERS
        // ==========================================

        private Vector3 GetControlPoint(Vector3 start, Vector3 end, Vector3 mid, int index)
        {
            // Arc: control point is above the midpoint
            return mid + Vector3.up * arcHeight +
                   new Vector3(
                       Mathf.Sin(index * 1.2f) * 2f,
                       0f,
                       Mathf.Cos(index * 1.2f) * 2f);
        }

        /// <summary>Quadratic bezier curve point</summary>
        private Vector3 BezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private Vector3 SpiralPoint(Vector3 start, Vector3 end, float t, int index)
        {
            Vector3 linear = Vector3.Lerp(start, end, t);
            float angle = t * Mathf.PI * 4f + index * Mathf.PI * 0.5f;
            float radius = (1f - t) * 3f; // Spiral inward
            linear.x += Mathf.Cos(angle) * radius;
            linear.z += Mathf.Sin(angle) * radius;
            linear.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            return linear;
        }

        private Vector3 WavePoint(Vector3 start, Vector3 end, float t, int index)
        {
            Vector3 linear = Vector3.Lerp(start, end, t);
            float waveOffset = Mathf.Sin(t * Mathf.PI * 3f + index) * 1.5f;
            linear.y += Mathf.Sin(t * Mathf.PI) * arcHeight + waveOffset * 0.3f;
            linear.x += waveOffset;
            return linear;
        }

        private float EaseInOutCubic(float t)
        {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        // ==========================================
        // DEBUG
        // ==========================================

        private void OnDrawGizmosSelected()
        {
            // Draw start area
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, startRadius);

            // Draw end area
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + endOffset, endRadius);

            // Draw arc preview
            Gizmos.color = Color.yellow;
            Vector3 start = transform.position;
            Vector3 end = transform.position + endOffset;
            Vector3 mid = (start + end) / 2f;
            Vector3 control = GetControlPoint(start, end, mid, 0);

            Vector3 prev = start;
            for (int i = 1; i <= 20; i++)
            {
                float t = i / 20f;
                Vector3 point = BezierPoint(start, control, end, t);
                Gizmos.DrawLine(prev, point);
                prev = point;
            }

            // Draw drop point
            if (dropPrefab != null)
            {
                Vector3 dropPos = BezierPoint(start, control, end, dropAtProgress);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(dropPos, 0.3f);
                Gizmos.DrawLine(dropPos, new Vector3(dropPos.x, transform.position.y, dropPos.z));
            }
        }
    }

    // ==========================================
    // ENUM
    // ==========================================

    public enum FlightPattern
    {
        Arc,        // Smooth bezier arc (default — bats, birds)
        Straight,   // Direct line (bullets, fast projectiles)
        Spiral,     // Spiral upward/inward (magic, dark portal)
        Wave        // Wavy sinusoidal path (ghostly, ethereal)
    }
}
