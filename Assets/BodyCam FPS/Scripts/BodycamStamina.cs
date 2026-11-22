using UnityEngine;
using UnityEngine.UI;

namespace PurgeFPS
{
    /// <summary>
    /// Stamina for running, jumping, dodging and melee swings.
    /// Put this on the player root (the object with BodycamFPSController).
    ///
    ///  - Running drains stamina every second.
    ///  - Jump, dodge and melee swings cost a fixed amount and are blocked if there isn't enough.
    ///  - Stamina regenerates after a short delay once you stop spending it.
    ///  - If stamina hits zero the player is "exhausted": running stays blocked until it recovers
    ///    to Exhausted Recover Amount.
    ///  - Stamina Enabled turns the whole system off: nothing costs stamina, nothing is blocked,
    ///    and the bar is hidden. Use Toggle Key to switch it in Play mode.
    ///
    /// The stamina bar is created automatically at runtime (no UI setup needed).
    /// Melee swing cost is set per swing on the melee weapon (Attack Chain > Stamina Cost).
    /// </summary>
    [AddComponentMenu("Realistic Bodycam/Stamina")]
    public class BodycamStamina : MonoBehaviour
    {
        [Header("TOGGLE")]
        [Tooltip("Off = stamina is disabled: running, jumping, dodging and swings are free and never blocked, and the bar is hidden.")]
        public bool staminaEnabled = true;

        [Tooltip("Optional key to switch stamina on and off while playing. None = no shortcut.")]
        public KeyCode toggleKey = KeyCode.None;

        [Header("STAMINA")]
        public float maxStamina = 100f;

        [Tooltip("Stamina recovered per second.")]
        public float regenRate = 20f;

        [Tooltip("Seconds after the last use before regeneration starts.")]
        public float regenDelay = 1f;

        [Tooltip("After running out, running stays blocked until stamina recovers to this amount.")]
        public float exhaustedRecoverAmount = 20f;

        [Header("COSTS")]
        public float runDrainPerSecond = 12f;
        public float jumpCost = 15f;
        public float dodgeCost = 25f;

        [Header("RUN DETECTION")]
        public KeyCode runKey = KeyCode.LeftShift;

        [Tooltip("Horizontal speed (m/s) above which you count as running. Between your walk and run speed.")]
        public float runningSpeedThreshold = 3.5f;

        [Header("BAR")]
        public bool showBar = true;
        public Vector2 barSize = new Vector2(320f, 12f);

        [Tooltip("Offset from the bottom center of the screen, in pixels (at 1080p).")]
        public Vector2 barOffset = new Vector2(0f, 48f);

        public Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);
        public Color fillColor = new Color(0.95f, 0.85f, 0.25f, 1f);
        public Color exhaustedColor = new Color(0.85f, 0.2f, 0.15f, 1f);
        public bool hideWhenFull = true;
        public float fadeSpeed = 4f;

        float current;
        float lastUseTime = -999f;
        bool exhausted;

        Vector3 lastPosition;

        // bar
        CanvasGroup canvasGroup;
        RectTransform fillRect;
        Image fillImage;
        float shownFill = -1f;
        bool shownExhausted;

        public float Current => current;
        public float Normalized => maxStamina > 0f ? current / maxStamina : 0f;
        public bool IsExhausted => exhausted;
        public bool IsEnabled => staminaEnabled;

        /// <summary>
        /// False while exhausted or empty. BodycamFPSController checks this before running.
        /// Always true while stamina is switched off.
        /// </summary>
        public bool CanRun => !staminaEnabled || (!exhausted && current > 0f);

        void Awake()
        {
            current = maxStamina;
        }

        void Start()
        {
            lastPosition = transform.position;

            if (showBar)
                BuildBar();
        }

        void OnDisable()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        /// <summary>Switches the stamina system on or off. Turning it off refills stamina and clears exhaustion.</summary>
        public void SetStaminaEnabled(bool on)
        {
            staminaEnabled = on;

            if (!on)
            {
                current = maxStamina;
                exhausted = false;
            }
        }

        /// <summary>Flips the stamina system on or off (handy for a UI button).</summary>
        public void ToggleStamina()
        {
            SetStaminaEnabled(!staminaEnabled);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
                SetStaminaEnabled(!staminaEnabled);

            if (dt <= 0f)
                return;

            // Switched off in the Inspector: keep stamina full, no drain, bar hidden.
            if (!staminaEnabled)
            {
                if (current < maxStamina)
                    current = maxStamina;

                exhausted = false;
                lastPosition = transform.position;
                UpdateBar(dt);
                return;
            }

            // Real horizontal speed (the controller moves the player with two Move() calls per frame).
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            float speed = delta.magnitude / dt;
            lastPosition = transform.position;

            bool running = Input.GetKey(runKey)
                           && Input.GetAxisRaw("Vertical") > 0.1f
                           && speed > runningSpeedThreshold;

            if (running)
            {
                Drain(runDrainPerSecond * dt);
                lastUseTime = Time.time;
            }
            else if (current < maxStamina && Time.time - lastUseTime >= regenDelay)
            {
                current = Mathf.Min(maxStamina, current + regenRate * dt);
            }

            if (exhausted && current >= exhaustedRecoverAmount)
                exhausted = false;

            UpdateBar(dt);
        }

        /// <summary>Spends stamina if there is enough. Returns false (and spends nothing) otherwise. Always true while stamina is switched off.</summary>
        public bool TryConsume(float cost)
        {
            if (!staminaEnabled)
                return true;

            if (cost <= 0f)
                return true;

            if (current < cost)
                return false;

            Drain(cost);
            lastUseTime = Time.time;
            return true;
        }

        public void Restore(float amount)
        {
            current = Mathf.Min(maxStamina, current + amount);
        }

        void Drain(float amount)
        {
            current = Mathf.Max(0f, current - amount);

            if (current <= 0f)
                exhausted = true;
        }

        // ---------------- bar (built at runtime, no UI setup needed) ----------------

        void BuildBar()
        {
            GameObject canvasObject = new GameObject("StaminaBarCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = hideWhenFull ? 0f : 1f;

            // Background
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);

            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 0f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0f);
            backgroundRect.pivot = new Vector2(0.5f, 0f);
            backgroundRect.anchoredPosition = barOffset;
            backgroundRect.sizeDelta = barSize;

            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;

            // Fill (resized with anchors, so no sprite is needed)
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(background.transform, false);

            fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            fillImage = fill.GetComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            shownFill = -1f;
        }

        void UpdateBar(float dt)
        {
            if (canvasGroup == null || fillRect == null)
                return;

            // Only touch the UI when something changed.
            float fill = Normalized;

            if (!Mathf.Approximately(fill, shownFill))
            {
                shownFill = fill;
                fillRect.anchorMax = new Vector2(fill, 1f);
            }

            if (exhausted != shownExhausted)
            {
                shownExhausted = exhausted;
                fillImage.color = exhausted ? exhaustedColor : fillColor;
            }

            // The bar is always hidden while stamina is switched off.
            float targetAlpha = staminaEnabled && (!hideWhenFull || exhausted || current < maxStamina - 0.01f) ? 1f : 0f;

            if (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * dt);
        }
    }
}