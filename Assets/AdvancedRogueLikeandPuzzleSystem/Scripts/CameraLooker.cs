using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class CameraLooker : MonoBehaviour
    {
        void Update()
        {
            if (GameManager.Instance == null) return;

            Transform activeCamera = GameManager.Instance.GetCurrentCamera();
            if (activeCamera == null) return;

            transform.LookAt(activeCamera);
        }
    }
}
