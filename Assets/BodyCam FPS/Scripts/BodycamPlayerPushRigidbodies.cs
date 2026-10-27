using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Player Push Rigidbodies")]
    [RequireComponent(typeof(CharacterController))]
    public class BodycamPlayerPushRigidbodies : MonoBehaviour
    {
        public float pushPower = 3f;

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody rb = hit.collider.attachedRigidbody;

            if (rb == null || rb.isKinematic)
                return;

            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);

            rb.AddForceAtPosition(
                pushDir * pushPower,
                hit.point,
                ForceMode.Impulse
            );
        }
    }
}
