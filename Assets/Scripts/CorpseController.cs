using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CorpseController : MonoBehaviour
{
    [Header("Initial Impulse")]
    [Tooltip("Force applied in the opposite direction of the shot. 2-4 gives a convincing effect.")]
    [SerializeField] private float impulseForce = 3f;

    [Tooltip("Torque applied when thrown (slight spin). 0 = no spin.")]
    [SerializeField] private float impulseTorque = 30f;

    [Header("Corpse Stopping")]
    [Tooltip("Linear velocity below which the body is permanently locked.")]
    [SerializeField] private float stopVelocityThreshold = 0.05f;

    [Tooltip("How long (seconds) the body can move before being forced to stop.")]
    [SerializeField] private float maxSlideTime = 1.2f;

    [Tooltip("Linear damping applied to the Rigidbody2D to slow down naturally.")]
    [SerializeField] private float linearDrag = 6f;

    [Header("Visuals")]
    [Tooltip("Extra Z angle added to the enemy's rotation upon death. 90° = body lying on its side (Hotline Miami style).")]
    [SerializeField] private float extraDeathTiltDegrees = 90f;

    private Rigidbody2D rb;
    private bool isFrozen;
    private float slideTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale    = 0f;
        rb.linearDamping   = linearDrag;
        rb.angularDamping  = 8f;
        rb.constraints     = RigidbodyConstraints2D.None;
    }

    private void Update()
    {
        if (isFrozen) return;

        slideTimer += Time.deltaTime;

        bool tooSlow   = rb.linearVelocity.sqrMagnitude < stopVelocityThreshold * stopVelocityThreshold;
        bool timedOut  = slideTimer >= maxSlideTime;

        if (tooSlow || timedOut) FreezeCorpse();
    }

    public void Initialize(Vector2 hitDirection)
    {
        float currentZ  = transform.eulerAngles.z;
        transform.rotation = Quaternion.Euler(0f, 0f, currentZ + extraDeathTiltDegrees);

        if (hitDirection.sqrMagnitude < 0.001f) hitDirection = Vector2.up;

        rb.AddForce(hitDirection * impulseForce, ForceMode2D.Impulse);

        float torqueSign = (hitDirection.x >= 0f) ? 1f : -1f;
        rb.AddTorque(impulseTorque * torqueSign, ForceMode2D.Impulse);
    }

    private void FreezeCorpse()
    {
        if (isFrozen) return;
        isFrozen = true;

        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        enabled = false;
    }
}