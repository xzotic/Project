using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Shootable Target")]
    public class BodycamShootableTarget : MonoBehaviour
    {
        BodycamPopUpTargetTrigger targetController;
        Transform controlledTarget;

        public void Initialize(
            BodycamPopUpTargetTrigger controller,
            Transform targetTransform
        )
        {
            targetController = controller;
            controlledTarget = targetTransform;
        }

        public void TakeDamage(float damage)
        {
            if (targetController != null)
                targetController.KnockDownTarget(controlledTarget);
        }
    }
}
