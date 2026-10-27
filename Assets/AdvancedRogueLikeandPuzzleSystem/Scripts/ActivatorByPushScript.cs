using UnityEngine;
using UnityEngine.Events;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class ActivatorByPushScript : MonoBehaviour
    {
        public bool isInteractable = false;
        private bool isDoneCorrectly = false;
        public UnityEvent eventstoInvoke;

        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
        private Renderer cachedRenderer;
        private MaterialPropertyBlock propertyBlock;

        private int materialCount;

        private void Awake()
        {
            cachedRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            materialCount = cachedRenderer != null ? cachedRenderer.sharedMaterials.Length : 0;
        }

        private void SetEmission(Color color)
        {
            if (cachedRenderer == null || materialCount < 2) return;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            cachedRenderer.GetPropertyBlock(propertyBlock, 1);
            propertyBlock.SetColor(EmissionColorID, color);
            cachedRenderer.SetPropertyBlock(propertyBlock, 1);
        }

        public void Activate()
        {
            SetEmission(Color.green * 5);
            isInteractable = true;
        }

        public void Close()
        {
            SetEmission(Color.red * 5);
            isDoneCorrectly = false;
        }

        public void Open()
        {
            if (eventstoInvoke != null)
            {
                eventstoInvoke.Invoke();
                eventstoInvoke = null;
            }
            if (AudioManager.instance != null)
            {
                AudioManager.instance.Play_ButtonPressing();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isDoneCorrectly)
            {
                if (isInteractable && other.CompareTag("Player"))
                {
                    Open();
                    this.enabled = false;
                }
            }
        }
    }
}