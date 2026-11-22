using UnityEngine;

namespace PurgeFPS
{
    /// <summary>
    /// A hitbox attached to an enemy's weapon or hand bone. It follows the bone, so the hit comes
    /// from where the weapon really is during the swing.
    ///
    /// Two ways to switch it on:
    ///  - By SoulsEnemy (timing numbers or AttackHitStart / AttackHitEnd animation events).
    ///  - By the animation clip itself: animate this object's collider "Enabled" (or the GameObject's
    ///    "Is Active") and tick "Animator Controls Hitboxes" on the attack. While the collider is on, it hurts.
    ///
    /// Setup:
    ///  1. Make an empty child of the weapon or hand bone and add this component.
    ///  2. Add a BoxCollider, SphereCollider or CapsuleCollider to the same object and size it to the weapon.
    ///     It is forced to Is Trigger and starts switched off. It is only used as a shape, so it never pushes anything.
    ///  3. Drag this object into the attack's "Hitboxes" list on SoulsEnemy.
    ///
    /// Tip: make the shape a little thicker than the blade. Fast swings can pass through a thin one between frames.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Enemies/Enemy Hitbox")]
    public class EnemyHitbox : MonoBehaviour
    {
        [Tooltip("The enemy this hitbox belongs to. Found automatically in the parents if empty.")]
        public SoulsEnemy owner;

        [Tooltip("Which layers the hitbox can touch. Include the player's layer.")]
        public LayerMask hitMask = ~0;

        [Tooltip("Draws the hitbox in the Scene view (and the Game view with Gizmos on). Red while it can hurt.")]
        public bool drawGizmo = true;

        /// <summary>True while this hitbox can hurt: switched on by SoulsEnemy, or by the animation when it controls the collider.</summary>
        public bool IsOpen
        {
            get
            {
                if (!isActiveAndEnabled)
                    return false;

                if (animatorControlled)
                {
                    Collider c = Shape;
                    return c != null && c.enabled;
                }

                return open;
            }
        }

        Collider shape;
        bool open;
        bool animatorControlled;
        readonly Collider[] results = new Collider[16];

        Collider Shape
        {
            get
            {
                if (shape == null)
                    shape = GetComponent<Collider>();

                return shape;
            }
        }

        void Awake()
        {
            Collider c = Shape;

            if (c == null)
            {
                Debug.LogWarning($"[{name}] EnemyHitbox needs a Box, Sphere or Capsule collider on the same object as a shape guide.", this);
            }
            else
            {
                c.isTrigger = true;   // a shape only, never pushes the player
                c.enabled = false;
            }

            if (owner == null)
                owner = GetComponentInParent<SoulsEnemy>();
        }

        void OnDisable()
        {
            open = false;
        }

        /// <summary>Called by SoulsEnemy for attacks timed by numbers or events.</summary>
        public void SetOpen(bool value)
        {
            open = value;
        }

        /// <summary>Called by SoulsEnemy: true = the animation switches the collider on and off, false = SoulsEnemy does.</summary>
        public void SetAnimatorControlled(bool value)
        {
            animatorControlled = value;
        }

        // LateUpdate so the check uses this frame's animated bone pose.
        void LateUpdate()
        {
            if (!IsOpen || owner == null || Shape == null)
                return;

            int count = Overlap();

            for (int i = 0; i < count; i++)
            {
                Collider other = results[i];

                if (other == null || other.transform.IsChildOf(owner.transform))
                    continue;   // ignore the enemy's own colliders

                owner.OnHitboxContact(other);
            }
        }

        int Overlap()
        {
            Transform t = transform;
            Vector3 scale = t.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            if (shape is BoxCollider box)
            {
                Vector3 center = t.TransformPoint(box.center);
                Vector3 half = Vector3.Scale(box.size * 0.5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                return Physics.OverlapBoxNonAlloc(center, half, results, t.rotation, hitMask, QueryTriggerInteraction.Ignore);
            }

            if (shape is SphereCollider sphere)
            {
                Vector3 center = t.TransformPoint(sphere.center);
                return Physics.OverlapSphereNonAlloc(center, sphere.radius * maxScale, results, hitMask, QueryTriggerInteraction.Ignore);
            }

            if (shape is CapsuleCollider capsule)
            {
                Vector3 axis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
                float radius = capsule.radius * maxScale;
                float halfLength = Mathf.Max(capsule.height * maxScale * 0.5f - radius, 0f);

                Vector3 center = t.TransformPoint(capsule.center);
                Vector3 worldAxis = t.TransformDirection(axis).normalized;

                return Physics.OverlapCapsuleNonAlloc(
                    center + worldAxis * halfLength,
                    center - worldAxis * halfLength,
                    radius, results, hitMask, QueryTriggerInteraction.Ignore);
            }

            return 0;
        }

        void OnDrawGizmos()
        {
            if (!drawGizmo)
                return;

            Collider guide = Shape;

            if (guide == null)
                return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = IsOpen ? new Color(1f, 0.15f, 0.1f) : new Color(1f, 0.5f, 0f, 0.5f);

            if (guide is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (guide is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (guide is CapsuleCollider capsule)
            {
                Vector3 axis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
                float half = Mathf.Max(capsule.height * 0.5f - capsule.radius, 0f);

                Gizmos.DrawWireSphere(capsule.center + axis * half, capsule.radius);
                Gizmos.DrawWireSphere(capsule.center - axis * half, capsule.radius);
                Gizmos.DrawLine(capsule.center + axis * half, capsule.center - axis * half);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}