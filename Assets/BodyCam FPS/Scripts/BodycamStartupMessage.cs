using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Startup Message")]
    public class BodycamStartupMessage : MonoBehaviour
    {
        [Header("MESSAGE")]
        public string message = "CAM CONNECTED";
        public Color textColor = new Color(0.1f, 1f, 0.25f, 1f);
        public int fontSize = 34;
        public Vector2 screenOffset = Vector2.zero;

        [Header("CONNECTED TIMING")]
        [Min(0f)]
        [UnityEngine.Serialization.FormerlySerializedAs("initialDelay")]
        public float connectedInitialDelay = 0.35f;

        [Min(0.01f)]
        [UnityEngine.Serialization.FormerlySerializedAs("typingDuration")]
        public float connectedTypingDuration = 0.4f;

        [Min(0f)]
        [UnityEngine.Serialization.FormerlySerializedAs("visibleDuration")]
        public float connectedVisibleDuration = 1.5f;

        [Min(0.01f)]
        [UnityEngine.Serialization.FormerlySerializedAs("fadeOutDuration")]
        public float connectedFadeOutDuration = 0.5f;

        [Header("DISCONNECTED TIMING")]
        [Min(0f)] public float disconnectedInitialDelay = 0.05f;
        [Min(0.01f)] public float disconnectedTypingDuration = 0.3f;
        [Min(0f)] public float disconnectedVisibleDuration = 0.75f;
        [Min(0.01f)] public float disconnectedFadeOutDuration = 0.18f;

        [Header("DIGITAL EFFECT")]
        public bool enableTypingEffect = true;
        public bool enableLowFPSFlicker = true;

        [Min(1f)]
        public float visualFPS = 12f;

        [Range(0f, 0.5f)]
        public float flickerStrength = 0.12f;

        [Min(0f)]
        public float positionJitter = 1f;

        [Header("OPTIONAL BACKGROUND")]
        public bool showBackground = false;
        public Color backgroundColor = new Color(0f, 0f, 0.2f, 0.35f);
        public Vector2 backgroundSize = new Vector2(420f, 100f);

        [Header("OPTIONAL AUDIO")]
        public AudioSource audioSource;
        public AudioClip connectionSound;
        public AudioClip disconnectionSound;

        [Header("CAMERA STATUS")]
        public string connectedMessage = "CAM CONNECTED";
        public string disconnectedMessage = "CAM DISCONNECTED";
        public Color connectedColor = new Color(0.1f, 1f, 0.25f, 1f);
        public Color disconnectedColor = new Color(1f, 0.12f, 0.08f, 1f);

        public float TotalSequenceDuration => currentInitialDelay + currentTypingDuration +
                                              currentVisibleDuration + currentFadeOutDuration;

        float startTime;
        float currentAlpha = 1f;
        float flickerMultiplier = 1f;
        float nextVisualUpdate;
        Vector2 currentJitter;
        string displayedMessage = "";
        bool soundPlayed;
        bool messageFinished;
        AudioClip currentStatusSound;
        bool statusSoundAssigned;
        float currentInitialDelay;
        float currentTypingDuration;
        float currentVisibleDuration;
        float currentFadeOutDuration;

        GUIStyle messageStyle;
        Texture2D whiteTexture;

        void OnEnable()
        {
            startTime = Time.unscaledTime;
            currentAlpha = 1f;
            displayedMessage = enableTypingEffect ? "" : message;
            soundPlayed = false;
            messageFinished = false;
        }

        public void ShowConnected()
        {
            ApplyConnectedTiming();
            ShowStatus(connectedMessage, connectedColor, connectionSound);
        }

        public void ShowDisconnected()
        {
            ApplyDisconnectedTiming();
            ShowStatus(disconnectedMessage, disconnectedColor, disconnectionSound);
        }

        void ApplyConnectedTiming()
        {
            currentInitialDelay = connectedInitialDelay;
            currentTypingDuration = connectedTypingDuration;
            currentVisibleDuration = connectedVisibleDuration;
            currentFadeOutDuration = connectedFadeOutDuration;
        }

        void ApplyDisconnectedTiming()
        {
            currentInitialDelay = disconnectedInitialDelay;
            currentTypingDuration = disconnectedTypingDuration;
            currentVisibleDuration = disconnectedVisibleDuration;
            currentFadeOutDuration = disconnectedFadeOutDuration;
        }

        public void ShowStatus(string statusMessage, Color statusColor, AudioClip statusSound = null)
        {
            message = statusMessage;
            textColor = statusColor;
            currentStatusSound = statusSound;
            statusSoundAssigned = true;

            if (!enabled)
                enabled = true;
            else
                RestartMessage();
        }

        void RestartMessage()
        {
            startTime = Time.unscaledTime;
            currentAlpha = 1f;
            displayedMessage = enableTypingEffect ? "" : message;
            soundPlayed = false;
            messageFinished = false;
            nextVisualUpdate = 0f;
        }

        void Awake()
        {
            ApplyConnectedTiming();

            messageStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Normal,
                clipping = TextClipping.Overflow
            };

            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }

        void Update()
        {
            if (messageFinished)
                return;

            float elapsedTime = Time.unscaledTime - startTime;

            if (elapsedTime < currentInitialDelay)
            {
                displayedMessage = "";
                return;
            }

            if (!soundPlayed)
            {
                soundPlayed = true;

                AudioClip soundToPlay = statusSoundAssigned ? currentStatusSound : connectionSound;

                if (audioSource != null && soundToPlay != null)
                    audioSource.PlayOneShot(soundToPlay);
            }

            float messageTime = elapsedTime - currentInitialDelay;

            if (enableTypingEffect)
            {
                float typingProgress = Mathf.Clamp01(messageTime / currentTypingDuration);
                int visibleCharacters = Mathf.Clamp(
                    Mathf.CeilToInt(message.Length * typingProgress),
                    0,
                    message.Length
                );

                displayedMessage = message.Substring(0, visibleCharacters);
            }
            else
            {
                displayedMessage = message;
            }

            float fadeStart = currentTypingDuration + currentVisibleDuration;

            if (messageTime >= fadeStart)
            {
                currentAlpha = 1f - Mathf.Clamp01(
                    (messageTime - fadeStart) / currentFadeOutDuration
                );
            }

            UpdateDigitalEffect();

            if (messageTime >= fadeStart + currentFadeOutDuration)
            {
                messageFinished = true;
                displayedMessage = "";
                enabled = false;
            }
        }

        void UpdateDigitalEffect()
        {
            if (!enableLowFPSFlicker)
            {
                flickerMultiplier = 1f;
                currentJitter = Vector2.zero;
                return;
            }

            if (Time.unscaledTime < nextVisualUpdate)
                return;

            nextVisualUpdate = Time.unscaledTime + 1f / visualFPS;
            flickerMultiplier = Random.Range(1f - flickerStrength, 1f);
            currentJitter = Random.insideUnitCircle * positionJitter;
        }

        void OnGUI()
        {
            if (messageFinished || string.IsNullOrEmpty(displayedMessage))
                return;

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            center += screenOffset + currentJitter;

            if (showBackground && whiteTexture != null)
            {
                GUI.color = new Color(
                    backgroundColor.r,
                    backgroundColor.g,
                    backgroundColor.b,
                    backgroundColor.a * currentAlpha
                );

                GUI.DrawTexture(
                    new Rect(
                        center.x - backgroundSize.x * 0.5f,
                        center.y - backgroundSize.y * 0.5f,
                        backgroundSize.x,
                        backgroundSize.y
                    ),
                    whiteTexture
                );
            }

            messageStyle.fontSize = fontSize;
            messageStyle.normal.textColor = new Color(
                textColor.r,
                textColor.g,
                textColor.b,
                textColor.a * currentAlpha * flickerMultiplier
            );

            GUI.color = Color.white;
            GUI.Label(
                new Rect(center.x - 400f, center.y - 50f, 800f, 100f),
                displayedMessage,
                messageStyle
            );
        }

        void OnDestroy()
        {
            if (whiteTexture != null)
                Destroy(whiteTexture);
        }
    }
}
