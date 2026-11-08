using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SteppedAnimator : MonoBehaviour
{
    [Tooltip("Target framerate for the animation. 12 fps is 'on twos' at a 24fps base.")]
    public int targetFrameRate = 12;

    private Animator animator;
    private float timer;
    private float frameInterval;

    void Awake()
    {
        animator = GetComponent<Animator>();
        // Disable automatic updating so we can control it manually
        animator.enabled = false;
        frameInterval = 1f / targetFrameRate;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= frameInterval)
        {
            // Advance the animation by the accumulated time
            animator.Update(timer);

            // Subtract interval to keep timing accurate without drifting
            timer %= frameInterval;
        }
    }
}