using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using ElementumDefense.Players;
using ElementumDefense.Turrets;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Manages all sabotage visual effects — arena VFX, turret indicators,
    /// screen flashes, and sounds.
    /// 
    /// Place on the Player object (alongside PlayerCardManager).
    /// Listens to SabotageDraftManager.OnSabotageApplied.
    /// </summary>
    public class SabotageVFXManager : MonoBehaviour
    {
        public static SabotageVFXManager Instance { get; private set; }

        [Header("Screen Flash")]
        [Tooltip("Full-screen Image for flash overlay (create a Canvas > Image over entire screen)")]
        [SerializeField] private Image screenFlashImage;

        [Tooltip("Flash fade-in time")]
        [SerializeField] private float flashFadeInTime = 0.15f;

        [Tooltip("Flash hold time")]
        [SerializeField] private float flashHoldTime = 0.3f;

        [Tooltip("Flash fade-out time")]
        [SerializeField] private float flashFadeOutTime = 0.8f;

        [Header("Arena VFX")]
        [Tooltip("Transform where arena VFX spawns (center of your arena)")]
        [SerializeField] private Transform arenaVFXSpawnPoint;

        [Header("Turret Indicator")]
        [Tooltip("Y offset above turret for indicator icons")]
        [SerializeField] private float indicatorYOffset = 2.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;

        [Header("Announcement")]
        [Tooltip("Text popup prefab for sabotage name announcement.\n" +
                 "Should have a Text/TMP_Text component. Will be destroyed after duration.")]
        [SerializeField] private GameObject announcementPrefab;
        [SerializeField] private Transform announcementParent;
        [SerializeField] private float announcementDuration = 2.5f;

        // Runtime tracking
        private List<GameObject> activeIndicators = new List<GameObject>();
        private List<GameObject> activeArenaVFX = new List<GameObject>();
        private Coroutine flashCoroutine;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            var pv = GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Auto-subscribe to sabotage events
            if (SabotageDraftManager.Instance != null)
            {
                SabotageDraftManager.Instance.OnSabotageApplied += OnSabotageApplied;
            }

            // Ensure flash image starts invisible
            if (screenFlashImage != null)
            {
                var c = screenFlashImage.color;
                screenFlashImage.color = new Color(c.r, c.g, c.b, 0f);
                screenFlashImage.raycastTarget = false; // Don't block input
                screenFlashImage.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (SabotageDraftManager.Instance != null)
                SabotageDraftManager.Instance.OnSabotageApplied -= OnSabotageApplied;

            CleanupAll();
        }

        // ==========================================
        // EVENT HANDLER
        // ==========================================

        /// <summary>
        /// Called when any sabotage is applied to this player.
        /// Reads VFX data from SabotageCardData and spawns effects.
        /// </summary>
        private void OnSabotageApplied(SabotageCardData sabotage, PhotonView caster)
        {
            if (sabotage == null) return;

            Debug.Log($"[SabotageVFX] Playing VFX for: {sabotage.sabotageName}");

            // 1. Screen Flash
            if (sabotage.screenFlashColor.a > 0.01f)
            {
                PlayScreenFlash(sabotage.screenFlashColor);
            }

            // 2. Arena VFX
            if (sabotage.arenaVFXPrefab != null)
            {
                SpawnArenaVFX(sabotage.arenaVFXPrefab, sabotage.arenaVFXDuration);
            }

            // 3. Turret Indicators
            if (sabotage.turretIndicatorPrefab != null)
            {
                float duration = GetSabotageDuration(sabotage);
                SpawnTurretIndicators(sabotage.turretIndicatorPrefab, duration);
            }

            // 4. Sound
            if (sabotage.activationSound != null)
            {
                PlaySound(sabotage.activationSound);
            }

            // 5. Announcement
            if (announcementPrefab != null)
            {
                ShowAnnouncement(sabotage);
            }
        }

        // ==========================================
        // SCREEN FLASH
        // ==========================================

        public void PlayScreenFlash(Color flashColor)
        {
            if (screenFlashImage == null)
            {
                Debug.LogWarning("[SabotageVFX] No screenFlashImage assigned!");
                return;
            }

            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(ScreenFlashCoroutine(flashColor));
        }

        private IEnumerator ScreenFlashCoroutine(Color flashColor)
        {
            screenFlashImage.gameObject.SetActive(true);

            // Fade in
            float t = 0f;
            while (t < flashFadeInTime)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, flashColor.a, t / flashFadeInTime);
                screenFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
                yield return null;
            }

            screenFlashImage.color = flashColor;

            // Hold
            yield return new WaitForSeconds(flashHoldTime);

            // Fade out
            t = 0f;
            while (t < flashFadeOutTime)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(flashColor.a, 0f, t / flashFadeOutTime);
                screenFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
                yield return null;
            }

            screenFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            screenFlashImage.gameObject.SetActive(false);
            flashCoroutine = null;
        }

        // ==========================================
        // ARENA VFX
        // ==========================================

        public void SpawnArenaVFX(GameObject vfxPrefab, float duration)
        {
            Transform spawnAt = arenaVFXSpawnPoint != null
                ? arenaVFXSpawnPoint
                : transform;

            GameObject vfx = Instantiate(vfxPrefab, spawnAt.position, Quaternion.identity, spawnAt);
            activeArenaVFX.Add(vfx);

            if (duration > 0f)
            {
                StartCoroutine(DestroyAfter(vfx, duration));
            }
            else
            {
                // Auto-detect duration from ParticleSystem
                ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
                if (ps != null && !ps.main.loop)
                {
                    StartCoroutine(DestroyAfter(vfx, ps.main.duration + ps.main.startLifetime.constantMax));
                }
                else
                {
                    // Fallback — destroy after 5s
                    StartCoroutine(DestroyAfter(vfx, 5f));
                }
            }

            Debug.Log($"[SabotageVFX] Arena VFX spawned: {vfxPrefab.name}");
        }

        // ==========================================
        // TURRET INDICATORS
        // ==========================================

        /// <summary>
        /// Spawns an indicator icon/prefab above every turret on the local player's arena.
        /// Auto-removes after duration (or when sabotage ends).
        /// </summary>
        public void SpawnTurretIndicators(GameObject indicatorPrefab, float duration)
        {
            // Find all turrets on my arena
            Turret[] turrets = FindMyTurrets();

            if (turrets.Length == 0)
            {
                Debug.Log("[SabotageVFX] No turrets found for indicators.");
                return;
            }

            foreach (var turret in turrets)
            {
                if (turret == null) continue;

                Vector3 pos = turret.transform.position + Vector3.up * indicatorYOffset;
                GameObject indicator = Instantiate(indicatorPrefab, pos, Quaternion.identity, turret.transform);

                // Make indicator face camera (billboard)
                var billboard = indicator.AddComponent<BillboardToCamera>();

                activeIndicators.Add(indicator);
            }

            if (duration > 0f)
            {
                StartCoroutine(RemoveIndicatorsAfter(duration));
            }

            Debug.Log($"[SabotageVFX] Spawned {turrets.Length} turret indicators.");
        }

        /// <summary>
        /// Removes all active turret indicators.
        /// Called when sabotage expires or is cleared.
        /// </summary>
        public void ClearTurretIndicators()
        {
            foreach (var indicator in activeIndicators)
            {
                if (indicator != null)
                    Destroy(indicator);
            }
            activeIndicators.Clear();
        }

        private IEnumerator RemoveIndicatorsAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearTurretIndicators();
        }

        // ==========================================
        // SOUND
        // ==========================================

        public void PlaySound(AudioClip clip)
        {
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
            else
            {
                // Fallback: play at camera position
                AudioSource.PlayClipAtPoint(clip, Camera.main != null
                    ? Camera.main.transform.position
                    : Vector3.zero);
            }
        }

        // ==========================================
        // ANNOUNCEMENT
        // ==========================================

        private void ShowAnnouncement(SabotageCardData sabotage)
        {
            if (announcementPrefab == null) return;

            Transform parent = announcementParent != null
                ? announcementParent
                : transform;

            GameObject announcement = Instantiate(announcementPrefab, parent);

            // Try to set text
            var tmpText = announcement.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = sabotage.IsSelfSabotage
                    ? $"⚠ {sabotage.sabotageName}"
                    : $"💀 {sabotage.sabotageName}";
                tmpText.color = sabotage.sabotageColor;
            }
            else
            {
                var uiText = announcement.GetComponentInChildren<Text>();
                if (uiText != null)
                {
                    uiText.text = sabotage.IsSelfSabotage
                        ? $"CHALLENGE: {sabotage.sabotageName}"
                        : $"SABOTAGED: {sabotage.sabotageName}";
                    uiText.color = sabotage.sabotageColor;
                }
            }

            StartCoroutine(DestroyAfter(announcement, announcementDuration));
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private Turret[] FindMyTurrets()
        {
            PhotonView myView = GetComponent<PhotonView>();
            if (myView == null) return new Turret[0];

            // Find all arenas, match by owner
            ArenaOwner[] arenas = FindObjectsByType<ArenaOwner>(FindObjectsSortMode.None);
            foreach (var arena in arenas)
            {
                if (arena.ownerPhotonView == myView)
                {
                    return arena.GetComponentsInChildren<Turret>();
                }
            }

            return new Turret[0];
        }

        private float GetSabotageDuration(SabotageCardData sabotage)
        {
            if (sabotage.durationType == SabotageDurationType.Permanent)
                return -1f; // Infinite

            if (sabotage.durationType == SabotageDurationType.Instant)
                return 2f; // Short flash for instant effects

            if (sabotage.durationRounds > 0)
                return sabotage.durationRounds * 30f; // Rough estimate: ~30s per round

            return sabotage.duration;
        }

        private IEnumerator DestroyAfter(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
            {
                activeArenaVFX.Remove(obj);
                activeIndicators.Remove(obj);
                Destroy(obj);
            }
        }

        public void CleanupAll()
        {
            ClearTurretIndicators();
            foreach (var vfx in activeArenaVFX)
            {
                if (vfx != null) Destroy(vfx);
            }
            activeArenaVFX.Clear();
        }

        // ==========================================
        // PUBLIC API (for manual VFX triggering)
        // ==========================================

        /// <summary>
        /// Manually trigger VFX for any sabotage (useful for testing).
        /// </summary>
        public void PlayVFXForSabotage(SabotageCardData sabotage)
        {
            OnSabotageApplied(sabotage, null);
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Test Screen Flash (Red)")]
        private void TestRedFlash()
        {
            PlayScreenFlash(new Color(1f, 0f, 0f, 0.4f));
        }

        [ContextMenu("Test Screen Flash (Gold)")]
        private void TestGoldFlash()
        {
            PlayScreenFlash(new Color(1f, 0.8f, 0f, 0.3f));
        }

        [ContextMenu("Print Active VFX")]
        private void DebugPrint()
        {
            Debug.Log($"[SabotageVFX] Active indicators: {activeIndicators.Count}");
            Debug.Log($"[SabotageVFX] Active arena VFX: {activeArenaVFX.Count}");
        }
    }

    // ==========================================
    // HELPER COMPONENT: Billboard to Camera
    // ==========================================

    /// <summary>
    /// Makes a GameObject always face the main camera (billboard effect).
    /// Used for turret sabotage indicators.
    /// </summary>
    public class BillboardToCamera : MonoBehaviour
    {
        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;
        }

        private void LateUpdate()
        {
            if (mainCam == null)
            {
                mainCam = Camera.main;
                if (mainCam == null) return;
            }

            // Face camera
            transform.LookAt(
                transform.position + mainCam.transform.rotation * Vector3.forward,
                mainCam.transform.rotation * Vector3.up);
        }
    }
}
