using System;
using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Horror Camera HUD")]
    public class BodycamHorrorCameraHUD : MonoBehaviour
    {
        [Header("DISPLAY")]
        public bool showRecording = true;
        public bool blinkRecording = true;
        public string recordingText = "REC";
        public bool showRecordingMode = true;
        public string recordingMode = "SP";
        public bool showCounter = true;
        public bool showDate = true;
        public bool showClock = true;

        [Header("DATE & TIME")]
        public bool useSystemDate = false;
        public int month = 8;
        public int day = 7;
        public int year = 2026;
        [Range(0, 23)] public int hour = 14;
        [Range(0, 59)] public int minute = 22;
        [Range(0, 59)] public int second = 30;
        public bool use12HourClock = true;
        public bool advanceDisplayedClock = true;

        [Header("STYLE")]
        public Font cameraFont;
        [Range(12, 80)] public int fontSize = 34;
        public Color textColor = new Color(0.82f, 0.84f, 0.77f, 0.92f);
        public Color recordingColor = new Color(0.9f, 0.22f, 0.18f, 0.95f);
        [Range(0f, 4f)] public float shadowOffset = 2f;
        [Range(0f, 1f)] public float shadowAlpha = 0.7f;
        [Range(0f, 0.4f)] public float flickerAmount = 0.07f;
        [Range(1f, 30f)] public float visualFPS = 12f;

        [Header("LAYOUT")]
        public Vector2 topLeftMargin = new Vector2(72f, 45f);
        public Vector2 topRightMargin = new Vector2(72f, 45f);
        public Vector2 bottomLeftMargin = new Vector2(72f, 52f);
        [Range(0.5f, 2f)] public float interfaceScale = 1f;

        [Header("4:3 CAMERA FRAME")]
        public bool enableFourThreeFrame = false;
        [Range(0f, 1f)] public float sideBarOpacity = 1f;

        [Header("LOW FPS SIGNAL")]
        public bool enableSignalJitter = true;
        [Range(0f, 8f)] public float positionJitter = 1.2f;

        float recordingStartTime;
        DateTime customStartDate;
        Vector2 currentJitter;
        float currentFlicker = 1f;
        float nextVisualUpdate;

        GUIStyle leftStyle;
        GUIStyle rightStyle;
        Texture2D whiteTexture;

        void Awake()
        {
            recordingStartTime = Time.unscaledTime;
            customStartDate = new DateTime(
                Mathf.Clamp(year, 1, 9999),
                Mathf.Clamp(month, 1, 12),
                Mathf.Clamp(day, 1, DateTime.DaysInMonth(Mathf.Clamp(year, 1, 9999), Mathf.Clamp(month, 1, 12))),
                hour, minute, second);

            leftStyle = CreateStyle(TextAnchor.UpperLeft);
            rightStyle = CreateStyle(TextAnchor.UpperRight);

            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }

        GUIStyle CreateStyle(TextAnchor alignment)
        {
            return new GUIStyle
            {
                alignment = alignment,
                font = cameraFont,
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                clipping = TextClipping.Overflow
            };
        }

        void Update()
        {
            if (Time.unscaledTime < nextVisualUpdate)
                return;

            nextVisualUpdate = Time.unscaledTime + 1f / Mathf.Max(1f, visualFPS);
            currentFlicker = UnityEngine.Random.Range(1f - flickerAmount, 1f);
            currentJitter = enableSignalJitter
                ? UnityEngine.Random.insideUnitCircle * positionJitter
                : Vector2.zero;
        }

        void OnGUI()
        {
            DrawFourThreeFrame();

            float scale = interfaceScale * Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            leftStyle.font = cameraFont;
            rightStyle.font = cameraFont;
            leftStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(fontSize * scale));
            rightStyle.fontSize = leftStyle.fontSize;

            float elapsed = Time.unscaledTime - recordingStartTime;
            float left = topLeftMargin.x * scale + currentJitter.x;
            float top = topLeftMargin.y * scale + currentJitter.y;
            float right = topRightMargin.x * scale - currentJitter.x;

            if (showRecording && (!blinkRecording || Mathf.FloorToInt(elapsed * 2f) % 2 == 0))
                DrawLabel(new Rect(left, top, 400f * scale, 70f * scale), recordingText, recordingColor, leftStyle);

            string topRightText = "";

            if (showRecordingMode)
                topRightText += recordingMode;

            if (showCounter)
            {
                if (topRightText.Length > 0)
                    topRightText += "   ";

                topRightText += FormatCounter(elapsed);
            }

            if (!string.IsNullOrEmpty(topRightText))
                DrawLabel(new Rect(Screen.width - right - 600f * scale, top, 600f * scale, 70f * scale),
                    topRightText, textColor, rightStyle);

            DateTime displayedTime = GetDisplayedTime(elapsed);
            string bottomText = "";

            if (showDate)
                bottomText += displayedTime.ToString("MMM dd yyyy").ToUpperInvariant();

            if (showClock)
            {
                if (bottomText.Length > 0)
                    bottomText += "\n";

                bottomText += use12HourClock
                    ? displayedTime.ToString("hh:mm:ss tt").ToUpperInvariant()
                    : displayedTime.ToString("HH:mm:ss");
            }

            if (!string.IsNullOrEmpty(bottomText))
            {
                float bottom = bottomLeftMargin.y * scale;
                DrawLabel(new Rect(left, Screen.height - bottom - 115f * scale,
                    600f * scale, 115f * scale), bottomText, textColor, leftStyle);
            }
        }

        DateTime GetDisplayedTime(float elapsed)
        {
            if (useSystemDate)
                return DateTime.Now;

            return advanceDisplayedClock ? customStartDate.AddSeconds(elapsed) : customStartDate;
        }

        string FormatCounter(float elapsed)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsed));
            int hours = totalSeconds / 3600;
            int minutes = totalSeconds / 60 % 60;
            int seconds = totalSeconds % 60;
            return string.Format("{0:0}:{1:00}:{2:00}", hours, minutes, seconds);
        }

        void DrawLabel(Rect rect, string value, Color color, GUIStyle style)
        {
            Color finalColor = new Color(color.r, color.g, color.b, color.a * currentFlicker);

            if (shadowOffset > 0f)
            {
                Rect shadowRect = new Rect(rect.x + shadowOffset, rect.y + shadowOffset, rect.width, rect.height);
                style.normal.textColor = new Color(0f, 0f, 0f, finalColor.a * shadowAlpha);
                GUI.Label(shadowRect, value, style);
            }

            style.normal.textColor = finalColor;
            GUI.Label(rect, value, style);
        }

        void DrawFourThreeFrame()
        {
            if (!enableFourThreeFrame || whiteTexture == null)
                return;

            float visibleWidth = Screen.height * (4f / 3f);
            float barWidth = Mathf.Max(0f, (Screen.width - visibleWidth) * 0.5f);

            if (barWidth <= 0f)
                return;

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, sideBarOpacity);
            GUI.DrawTexture(new Rect(0f, 0f, barWidth, Screen.height), whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - barWidth, 0f, barWidth, Screen.height), whiteTexture);
            GUI.color = previousColor;
        }

        public void ResetRecordingCounter()
        {
            recordingStartTime = Time.unscaledTime;
        }

        void OnDestroy()
        {
            if (whiteTexture != null)
                Destroy(whiteTexture);
        }
    }
}
