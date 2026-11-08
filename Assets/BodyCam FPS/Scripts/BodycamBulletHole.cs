using UnityEngine;

namespace PurgeFPS
{
    /// <summary>
    /// Added automatically to every bullet hole by BodycamAdvancedShootingSystem.
    /// Fades the hole out at the end of its life and then switches it off so it can be reused.
    /// Needs no setup. It only runs while the hole is visible.
    /// </summary>
    [DisallowMultipleComponent]
    public class BodycamBulletHole : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP
        static readonly int ColorId = Shader.PropertyToID("_Color");         // built-in / legacy

        Renderer[] renderers;
        int[] colorIds;
        Color[] baseColors;

        MaterialPropertyBlock block;

        float endTime;
        float fadeDuration;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            colorIds = new int[renderers.Length];
            baseColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].sharedMaterial;

                if (material != null && material.HasProperty(BaseColorId))
                    colorIds[i] = BaseColorId;
                else if (material != null && material.HasProperty(ColorId))
                    colorIds[i] = ColorId;
                else
                    colorIds[i] = -1;   // cannot be faded, it will simply disappear

                if (colorIds[i] != -1)
                    baseColors[i] = material.GetColor(colorIds[i]);
            }

            block = new MaterialPropertyBlock();
        }

        /// <summary>Starts the hole's life. lifetime = total seconds (the last fadeTime seconds are the fade-out).</summary>
        public void Begin(float lifetime, float fadeTime)
        {
            ResetAlpha();

            if (lifetime <= 0f)
            {
                // Stays until it is reused.
                enabled = false;
                return;
            }

            fadeDuration = Mathf.Min(fadeTime, lifetime);
            endTime = Time.time + lifetime;
            enabled = true;
        }

        void Update()
        {
            float remaining = endTime - Time.time;

            if (remaining <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            if (fadeDuration > 0f && remaining < fadeDuration)
                SetAlpha(remaining / fadeDuration);
        }

        void SetAlpha(float alpha)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (colorIds[i] == -1)
                    continue;

                Color color = baseColors[i];
                color.a *= alpha;

                renderers[i].GetPropertyBlock(block);
                block.SetColor(colorIds[i], color);
                renderers[i].SetPropertyBlock(block);
            }
        }

        void ResetAlpha()
        {
            if (renderers == null)
                return;

            for (int i = 0; i < renderers.Length; i++)
                renderers[i].SetPropertyBlock(null);
        }
    }
}