using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShellCasing : MonoBehaviour
{
    [Header("Ejection")]
    public float ejectionForce    = 20f;
    public float ejectionSpread   = 35f;
    public float spinSpeed        = 1800f;

    [Header("Stop")]
    public float drag          = 8f;
    public float stopThreshold = 0.08f;

    Rigidbody2D rb;

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        rb.linearDamping = drag;
    }

    public void Eject(Vector2 firingDirection)
    {
        float   sideAngle  = 90f + Random.Range(-ejectionSpread * 0.5f, ejectionSpread * 0.5f);
        Vector2 ejectDir   = Quaternion.Euler(0f, 0f, sideAngle) * firingDirection.normalized;
        float   force      = ejectionForce * Random.Range(0.8f, 1.2f);

        rb.AddForce(ejectDir * force, ForceMode2D.Impulse);
        rb.angularVelocity = spinSpeed * Mathf.Sign(Random.Range(-1f, 1f));
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity.sqrMagnitude > stopThreshold * stopThreshold) return;

        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints     = RigidbodyConstraints2D.FreezeAll;
        enabled            = false;
    }
}
