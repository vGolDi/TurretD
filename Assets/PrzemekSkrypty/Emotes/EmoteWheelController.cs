using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;

namespace ElementumDefense.Emotes
{
    /// <summary>
    /// Handles emote input (hold B → wheel appears), selection, and sending via PUN RPC.
    /// Displays emote popup on opponent's screen when received.
    /// 
    /// Place on the Player object (needs PhotonView).
    /// </summary>
    public class EmoteWheelController : MonoBehaviourPunCallbacks
    {
        public static EmoteWheelController LocalInstance { get; private set; }

        [Header("=== INPUT ===")]
        [SerializeField] private KeyCode emoteKey = KeyCode.B;

        [Tooltip("Minimum hold time before wheel opens (tap = last used emote)")]
        [SerializeField] private float holdThreshold = 0.2f;

        [Header("=== WHEEL UI ===")]
        [Tooltip("The radial wheel UI root (hidden by default)")]
        [SerializeField] private GameObject wheelUI;

        [Tooltip("Wheel slot Image components (8 slots, clockwise from top)")]
        [SerializeField] private Image[] wheelSlotImages = new Image[EmoteInventory.WHEEL_SLOTS];

        [Tooltip("Wheel slot highlight/selection indicators")]
        [SerializeField] private GameObject[] wheelSlotHighlights = new GameObject[EmoteInventory.WHEEL_SLOTS];

        [Tooltip("Center label showing hovered emote name")]
        [SerializeField] private TMPro.TMP_Text wheelCenterLabel;

        [Header("=== DISPLAY (Incoming Emote) ===")]
        [Tooltip("UI Image for showing received emotes (opponent's emote appears here)")]
        [SerializeField] private Image incomingEmoteImage;

        [Tooltip("Parent transform for incoming emote popup (position on screen edge)")]
        [SerializeField] private RectTransform incomingEmoteParent;

        [Tooltip("TMP label for emote name under the popup")]
        [SerializeField] private TMPro.TMP_Text incomingEmoteLabel;

        [Header("=== COOLDOWN (Burst System) ===")]
        [Tooltip("Max emotes before long cooldown")]
        [SerializeField] private int burstLimit = 3;

        [Tooltip("Short cooldown between burst emotes (seconds)")]
        [SerializeField] private float burstCooldown = 1f;

        [Tooltip("Long cooldown after burst is exhausted (seconds)")]
        [SerializeField] private float longCooldown = 20f;

        [Header("=== AUDIO ===")]
        [SerializeField] private AudioSource emoteAudioSource;

        // Runtime
        private float holdTimer = 0f;
        private bool wheelOpen = false;
        private int hoveredSlot = -1;
        private int lastUsedSlot = 0;
        private int burstRemaining;
        private float cooldownTimer = 0f;
        private bool isLongCooldown = false;
        private Coroutine displayCoroutine;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            if (photonView != null && photonView.IsMine)
                LocalInstance = this;

            burstRemaining = burstLimit;
        }

        private void Start()
        {
            if (wheelUI != null)
                wheelUI.SetActive(false);

            HideIncomingEmote();

            // Only process input for local player
            if (!photonView.IsMine)
            {
                enabled = false;
                return;
            }

            RefreshWheelIcons();
        }

        private void OnDestroy()
        {
            if (LocalInstance == this)
                LocalInstance = null;
        }

        private void Update()
        {
            if (!photonView.IsMine) return;

            // Cooldown
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;

                // Long cooldown expired — refill burst
                if (cooldownTimer <= 0f && isLongCooldown)
                {
                    burstRemaining = burstLimit;
                    isLongCooldown = false;
                    Debug.Log($"[EmoteWheel] Burst recharged! ({burstLimit} emotes ready)");
                }
            }

            // Hold to open wheel
            if (Input.GetKeyDown(emoteKey))
            {
                holdTimer = 0f;
            }

            if (Input.GetKey(emoteKey))
            {
                holdTimer += Time.deltaTime;

                if (holdTimer >= holdThreshold && !wheelOpen)
                {
                    OpenWheel();
                }

                if (wheelOpen)
                {
                    UpdateWheelSelection();
                }
            }

            if (Input.GetKeyUp(emoteKey))
            {
                if (wheelOpen)
                {
                    // Release with selection
                    if (hoveredSlot >= 0)
                    {
                        SendEmote(hoveredSlot);
                    }
                    CloseWheel();
                }
                else if (holdTimer < holdThreshold)
                {
                    // Quick tap = repeat last emote
                    SendEmote(lastUsedSlot);
                }

                holdTimer = 0f;
            }
        }

        // ==========================================
        // WHEEL UI
        // ==========================================

        private void OpenWheel()
        {
            wheelOpen = true;
            hoveredSlot = -1;

            if (wheelUI != null)
                wheelUI.SetActive(true);

            RefreshWheelIcons();

            // Optionally slow down time or show cursor
            // Cursor.visible = true;
            // Cursor.lockState = CursorLockMode.None;

            Debug.Log("[EmoteWheel] Opened");
        }

        private void CloseWheel()
        {
            wheelOpen = false;
            hoveredSlot = -1;

            if (wheelUI != null)
                wheelUI.SetActive(false);

            // Clear highlights
            for (int i = 0; i < wheelSlotHighlights.Length; i++)
            {
                if (wheelSlotHighlights[i] != null)
                    wheelSlotHighlights[i].SetActive(false);
            }

            if (wheelCenterLabel != null)
                wheelCenterLabel.text = "";
        }

        private void UpdateWheelSelection()
        {
            // Get mouse direction from screen center
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mousePos = Input.mousePosition;
            Vector2 direction = mousePos - screenCenter;

            float distance = direction.magnitude;

            // Dead zone in center
            if (distance < 30f)
            {
                SetHoveredSlot(-1);
                return;
            }

            // Calculate angle → slot (8 slots, 45° each, starting from top)
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            int slot = Mathf.FloorToInt(((angle + 22.5f) % 360f) / 45f);
            slot = Mathf.Clamp(slot, 0, EmoteInventory.WHEEL_SLOTS - 1);

            SetHoveredSlot(slot);
        }

        private void SetHoveredSlot(int slot)
        {
            if (slot == hoveredSlot) return;

            // Unhighlight previous
            if (hoveredSlot >= 0 && hoveredSlot < wheelSlotHighlights.Length
                && wheelSlotHighlights[hoveredSlot] != null)
            {
                wheelSlotHighlights[hoveredSlot].SetActive(false);
            }

            hoveredSlot = slot;

            // Highlight current
            if (hoveredSlot >= 0 && hoveredSlot < wheelSlotHighlights.Length
                && wheelSlotHighlights[hoveredSlot] != null)
            {
                wheelSlotHighlights[hoveredSlot].SetActive(true);
            }

            // Update center label
            if (wheelCenterLabel != null)
            {
                var inv = EmoteInventory.Instance;
                if (inv != null && hoveredSlot >= 0)
                {
                    EmoteData emote = inv.GetWheelSlot(hoveredSlot);
                    wheelCenterLabel.text = emote?.emoteName ?? "";
                }
                else
                {
                    wheelCenterLabel.text = "";
                }
            }
        }

        public void RefreshWheelIcons()
        {
            var inv = EmoteInventory.Instance;
            if (inv == null) return;

            for (int i = 0; i < EmoteInventory.WHEEL_SLOTS; i++)
            {
                if (i >= wheelSlotImages.Length || wheelSlotImages[i] == null) continue;

                EmoteData emote = inv.GetWheelSlot(i);
                if (emote != null && emote.emoteIcon != null)
                {
                    wheelSlotImages[i].sprite = emote.emoteIcon;
                    wheelSlotImages[i].color = Color.white;
                    wheelSlotImages[i].enabled = true;
                }
                else
                {
                    wheelSlotImages[i].enabled = false;
                }
            }
        }

        // ==========================================
        // SEND EMOTE
        // ==========================================

        private void SendEmote(int slot)
        {
            // Blocked by cooldown
            if (cooldownTimer > 0f)
            {
                if (isLongCooldown)
                    Debug.Log($"[EmoteWheel] Spam cooldown! Wait {cooldownTimer:F1}s");
                else
                    Debug.Log($"[EmoteWheel] Wait {cooldownTimer:F1}s between emotes");
                return;
            }

            var inv = EmoteInventory.Instance;
            if (inv == null) return;

            EmoteData emote = inv.GetWheelSlot(slot);
            if (emote == null)
            {
                Debug.Log($"[EmoteWheel] Slot {slot} is empty.");
                return;
            }

            lastUsedSlot = slot;
            burstRemaining--;

            if (burstRemaining <= 0)
            {
                // Burst exhausted — long cooldown
                cooldownTimer = longCooldown;
                isLongCooldown = true;
                Debug.Log($"[EmoteWheel] Burst limit! {longCooldown}s cooldown");
            }
            else
            {
                // Short cooldown between burst emotes
                cooldownTimer = burstCooldown;
                Debug.Log($"[EmoteWheel] {burstRemaining}/{burstLimit} emotes remaining");
            }

            // Play own sound
            if (emote.emoteSound != null && emoteAudioSource != null)
                emoteAudioSource.PlayOneShot(emote.emoteSound);

            // Send to all players
            photonView.RPC("RPC_ShowEmote", RpcTarget.Others, emote.emoteId);

            Debug.Log($"[EmoteWheel] Sent emote: {emote.emoteName}");
        }

        // ==========================================
        // RECEIVE EMOTE (RPC)
        // ==========================================

        [PunRPC]
        private void RPC_ShowEmote(string emoteId)
        {
            // Find emote data
            var inv = EmoteInventory.Instance;
            if (inv == null) return;

            EmoteData emote = inv.GetAllEmotes().Find(e => e.emoteId == emoteId);
            if (emote == null)
            {
                Debug.LogWarning($"[EmoteWheel] Unknown emote ID: {emoteId}");
                return;
            }

            string senderName = photonView.Owner?.NickName ?? "Player";
            Debug.Log($"[EmoteWheel] Received emote '{emote.emoteName}' from {senderName}");

            // Show on local screen
            if (EmoteWheelController.LocalInstance != null)
                EmoteWheelController.LocalInstance.DisplayIncomingEmote(emote, senderName);
        }

        // ==========================================
        // DISPLAY INCOMING EMOTE
        // ==========================================

        public void DisplayIncomingEmote(EmoteData emote, string senderName)
        {
            if (displayCoroutine != null)
                StopCoroutine(displayCoroutine);

            displayCoroutine = StartCoroutine(ShowEmotePopup(emote, senderName));
        }

        private IEnumerator ShowEmotePopup(EmoteData emote, string senderName)
        {
            // Show icon
            if (incomingEmoteImage != null && emote.emoteIcon != null)
            {
                incomingEmoteImage.sprite = emote.emoteIcon;
                incomingEmoteImage.color = emote.emoteColor;
                incomingEmoteImage.gameObject.SetActive(true);
            }

            // Show label
            if (incomingEmoteLabel != null)
            {
                incomingEmoteLabel.text = $"{senderName}: {emote.emoteName}";
                incomingEmoteLabel.gameObject.SetActive(true);
            }

            // Show parent
            if (incomingEmoteParent != null)
            {
                incomingEmoteParent.gameObject.SetActive(true);

                // Animate: slide in from side + scale pop
                Vector3 targetPos = incomingEmoteParent.localPosition;
                Vector3 startPos = targetPos + new Vector3(200f, 0f, 0f);
                incomingEmoteParent.localPosition = startPos;
                incomingEmoteParent.localScale = Vector3.zero;

                // Slide + scale in
                float animTime = 0.3f;
                float elapsed = 0f;
                while (elapsed < animTime)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / animTime;
                    float ease = EaseOutBack(t);
                    incomingEmoteParent.localPosition = Vector3.Lerp(startPos, targetPos, ease);
                    incomingEmoteParent.localScale = Vector3.one * ease;
                    yield return null;
                }
                incomingEmoteParent.localPosition = targetPos;
                incomingEmoteParent.localScale = Vector3.one;
            }

            // Play sound
            if (emote.emoteSound != null)
            {
                if (emoteAudioSource != null)
                    emoteAudioSource.PlayOneShot(emote.emoteSound);
                else
                    AudioSource.PlayClipAtPoint(emote.emoteSound,
                        Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            }

            // Spawn animated prefab (if any)
            GameObject animObj = null;
            if (emote.animatedPrefab != null && incomingEmoteParent != null)
            {
                animObj = Instantiate(emote.animatedPrefab, incomingEmoteParent);
            }

            // Hold
            yield return new WaitForSeconds(emote.displayDuration);

            // Fade out
            if (incomingEmoteParent != null)
            {
                float fadeTime = 0.4f;
                float fadeElapsed = 0f;
                Vector3 origScale = incomingEmoteParent.localScale;

                while (fadeElapsed < fadeTime)
                {
                    fadeElapsed += Time.deltaTime;
                    float t = fadeElapsed / fadeTime;
                    incomingEmoteParent.localScale = origScale * (1f - t);

                    if (incomingEmoteImage != null)
                    {
                        Color c = incomingEmoteImage.color;
                        incomingEmoteImage.color = new Color(c.r, c.g, c.b, 1f - t);
                    }

                    yield return null;
                }
            }

            // Cleanup
            if (animObj != null) Destroy(animObj);
            HideIncomingEmote();
            displayCoroutine = null;
        }

        private void HideIncomingEmote()
        {
            if (incomingEmoteImage != null)
                incomingEmoteImage.gameObject.SetActive(false);
            if (incomingEmoteLabel != null)
                incomingEmoteLabel.gameObject.SetActive(false);
            if (incomingEmoteParent != null)
                incomingEmoteParent.gameObject.SetActive(false);
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Test Send Emote (Slot 0)")]
        private void DebugSendSlot0()
        {
            SendEmote(0);
        }

        [ContextMenu("Test Receive Emote")]
        private void DebugReceive()
        {
            var inv = EmoteInventory.Instance;
            if (inv == null) return;
            EmoteData first = inv.GetWheelSlot(0);
            if (first != null) DisplayIncomingEmote(first, "TestPlayer");
        }
    }
}
