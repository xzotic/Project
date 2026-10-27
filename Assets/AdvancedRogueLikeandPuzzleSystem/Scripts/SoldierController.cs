using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// Melee enemy AI on a NavMeshAgent, driven by the <see cref="Status"/> state machine:
    /// Idle (patrollers sometimes walk to a random Patrolling_Point) → Realizing (spots the hero
    /// within Range, plays an alert animation) → Hitting (chases, then attacks within 2.5 m) →
    /// Died. Damage reaches the hero through the HittingByAnimation animation event, so the
    /// hit lines up with the swing. Detection is polled once per second, not every frame.
    /// </summary>
    public class SoldierController : MonoBehaviour
    {
        public string Name;
        public bool isEnemy = false;
        public float speed = 0;
        public int health = 100;
        public int damagePower = 5;
        public float attackPeriod = 1.5f;
        private float lastAttackTime = 0;
        private float lastCheckPeriod = 0.5f;
        private float lastCheck = 0;
        private float lastCheckForRealize = 0;
        public NavMeshAgent agent;
        public Animator anim;
        private int totalHealth = 100;
        public Transform target;
        public Status status;
        public AudioClip[] Audio_Attacks;
        public AudioClip[] Audio_Realize;
        public AudioClip[] Audio_Die;
        public AudioSource AudioSource;
        public float Range = 15;
        public bool isPatroller = false;
        public Transform[] Patrolling_Points;
        public GainScript gain;
        float dyingYpos = 0;

        private float LastGetHittoAttackPeriod = 0.5f;

        private void OnEnable()
        {
            totalHealth = health;
            attackPeriod = Random.Range(attackPeriod - 0.25f, attackPeriod + 0.25f);
            lastCheckPeriod = Random.Range(lastCheckPeriod, attackPeriod + 0.2f);
            Idle();
        }

        public void Idle()
        {
            target = null;
            status = Status.Idle;

            float decisionTime = Random.Range(0, 6);
            if (decideRoutine != null)
            {
                StopCoroutine(decideRoutine);
            }
            decideRoutine = StartCoroutine(DecideWhattoDo(decisionTime));
        }

        private Coroutine decideRoutine;

        public void Realize()
        {
            if (status == Status.Realizing) return;

            if (target != null && agent.enabled)
            {
                status = Status.Realizing;
                agent.isStopped = true;
                transform.LookAt(target.position);
                anim.SetTrigger("Realize" + Random.Range(1, 3).ToString());
                StartCoroutine(PlaySound(SoundType.Realize));
                StartCoroutine(HitStatusAfterRealize());
            }
        }

        IEnumerator HitStatusAfterRealize()
        {
            yield return new WaitForSeconds(2.5f);
            target = HeroController.instance.transform;
            transform.LookAt(target.position);
            status = Status.Hitting;
        }

        IEnumerator DecideWhattoDo(float time)
        {
            yield return new WaitForSeconds(time);
            if (status == Status.Idle || status == Status.Moving)
            {
                int decision = isPatroller ? Random.Range(0, 2) : 0;
                if (decision == 0)
                {
                    Idle();
                }
                else if (decision == 1)
                {
                    StartCoroutine(GoSomeWhere(false));
                }
            }
        }

        private Transform Get_RandomTarget()
        {
            return Patrolling_Points[Random.Range(0, Patrolling_Points.Length)];
        }

        public IEnumerator GoSomeWhere(bool Escape)
        {
            if (agent.enabled && isPatroller && Patrolling_Points.Length > 0)
            {
                target = Get_RandomTarget();
                agent.angularSpeed = 0;
                agent.ResetPath();
                agent.autoRepath = true;
                agent.SetDestination(target.position);
                yield return new WaitUntil(() => !agent.pathPending);
                agent.angularSpeed = 120;
                int moveType = Escape ? 0 : 1;
                if (moveType == 0)
                {
                    status = Escape ? Status.Escaping : Status.Moving;
                    agent.speed = speed * 2;
                }
                else
                {
                    status = Status.Moving;
                    agent.speed = speed;
                }
            }
        }

        void Update()
        {
            if (status == Status.Died || gettingHit)
            {
                if (status == Status.Died && transform.position.y > dyingYpos)
                {
                    transform.position = new Vector3(transform.position.x, dyingYpos, transform.position.z);
                }
                return;
            }
            else if (status == Status.Escaping || status == Status.Moving)
            {
                if (target != null && Time.time > lastCheck + lastCheckPeriod && Vector3.Distance(transform.position, target.position) < 1)
                {
                    lastCheck = Time.time;
                    agent.isStopped = true;
                    Idle();
                }
            }
            else if (status == Status.Hitting && Time.time > LastTimeDamage + LastGetHittoAttackPeriod)
            {
                Attack();
            }
            if (anim != null && agent != null && agent.enabled)
            {
                anim.SetFloat("locomotion", agent.velocity.magnitude);
            }
            CheckForRealize();
        }

        private void CheckForRealize()
        {
            if (HeroController.instance == null) return;

            if (Time.time > lastCheckForRealize + 1)
            {
                lastCheckForRealize = Time.time;
                if (status != Status.Realizing)
                {
                    float distance = Vector3.Distance(transform.position, HeroController.instance.transform.position);
                    if (distance <= Range && HeroController.instance.Health > 0)
                    {
                        if (status == Status.Idle || status == Status.Moving)
                        {
                            target = HeroController.instance.transform;
                            Realize();
                        }
                    }
                    else if (target != null && agent != null && Vector3.Distance(transform.position, target.position) <= agent.stoppingDistance)
                    {
                        if (agent.enabled)
                        {
                            agent.isStopped = true;
                        }
                        if (status != Status.Idle)
                        {
                            Idle();
                        }
                    }
                }
            }
        }

        public void TargetYourRival()
        {
            if (status == Status.Escaping || status == Status.Moving)
            {
                agent.isStopped = true;
            }
            target = HeroController.instance.transform;
            status = Status.Hitting;
        }

        private float GetDamagePeriod = 0.25f;
        float LastTimeDamage = 0;

        IEnumerator DeactivateAnimator()
        {
            yield return new WaitForSeconds(2.5f);
            anim.enabled = false;
        }

        public void GetDamage(int damage)
        {
            if (health <= 0)
            {
                return;
            }

            if (Time.time >= LastTimeDamage + GetDamagePeriod)
            {
                LastTimeDamage = Time.time;
                health = health - damage;
                GameCanvas_Controller.instance.Show_EnemyHealthPanel(Name, health, totalHealth);
                if (health > 0)
                {
                    TargetYourRival();
                    anim.SetInteger("Health", health);
                    anim.SetTrigger("GetHit");
                }
                else
                {
                    dyingYpos = transform.position.y;
                    health = 0;
                    status = Status.Died;
                    if (agent != null && agent.enabled)
                    {
                        agent.isStopped = true;
                        agent.enabled = false;
                    }
                    Rigidbody rigidbody = transform.gameObject.AddComponent<Rigidbody>();
                    rigidbody.mass = 1000;
                    rigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
                    StartCoroutine(PlaySound(SoundType.Die));
                    Destroy(gameObject, 5);
                    StartCoroutine(DeactivateAnimator());
                    anim.SetInteger("Health", health);
                    anim.SetTrigger("GetHit");
                    if (gain != null)
                    {
                        gain.Spread();
                    }
                }
                float currentSpeed = agent != null ? agent.speed : 0f;

                StartCoroutine(ResetGetHit(currentSpeed));
            }
        }

        private bool gettingHit = false;

        IEnumerator ResetGetHit(float spee)
        {
            gettingHit = true;
            if (agent != null) agent.speed = 0;
            yield return new WaitForSeconds(0.25f);
            if (agent != null) agent.speed = spee;
            gettingHit = false;
        }

        private float lastTimeSend = 0;

        public IEnumerator GoToEnemy()
        {
            if (agent.enabled)
            {
                if (Time.time > lastTimeSend + 0.5f)
                {
                    lastTimeSend = Time.time;
                    agent.angularSpeed = 0;
                    agent.autoRepath = true;
                    agent.ResetPath();
                    agent.SetDestination(target.position);
                    agent.speed = speed * 2;
                    yield return new WaitUntil(() => !agent.pathPending);
                    agent.angularSpeed = 120;
                }
            }
        }

        public void Attack()
        {
            if (target == null || HeroController.instance == null)
            {
                Idle();
            }
            else if (target == HeroController.instance.transform && HeroController.instance.CompareTag("Player"))
            {
                Vector3 direction = target.position - transform.position;
                direction.y = 0;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), 5 * Time.deltaTime);
                if (Time.time >= lastAttackTime + attackPeriod)
                {
                    lastAttackTime = Time.time;
                    if (target != null)
                    {
                        float distance = Vector3.Distance(transform.position, target.position);
                        if (distance > 2.5f)
                        {
                            StartCoroutine(GoToEnemy());
                        }
                        else if (distance <= 2.5f && agent.enabled)
                        {
                            agent.isStopped = true;
                            HitNow();
                        }
                    }
                }
            }
        }

        void HitNow()
        {
            if (Random.Range(0, 4) < 3 && !gettingHit)
            {
                anim.SetTrigger("Attack" + Random.Range(1, 4).ToString());
            }
        }

        public void HittingByAnimation()
        {
            StartCoroutine(PlaySound(SoundType.Hit));
            if (HeroController.instance == null) return;
            HeroController.instance.GetDamagePlayer(damagePower);
            if (HeroController.instance.Health <= 0)
            {
                Idle();
            }
        }

        IEnumerator PlaySound(SoundType type)
        {
            yield return new WaitForSeconds(0.5f);
            if (AudioSource == null) yield break;

            AudioClip clip = null;
            switch (type)
            {
                case SoundType.Die:
                    clip = PickRandomClip(Audio_Die);
                    break;
                case SoundType.Hit:
                    clip = PickRandomClip(Audio_Attacks);
                    break;
                case SoundType.Realize:
                    clip = PickRandomClip(Audio_Realize);
                    break;
            }
            if (clip == null) yield break;

            AudioSource.clip = clip;
            AudioSource.Play();
        }

        private static AudioClip PickRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }

    public enum Status
    {
        Hitting,
        Escaping,
        Moving,
        Died,
        Realizing,
        Idle
    }
}