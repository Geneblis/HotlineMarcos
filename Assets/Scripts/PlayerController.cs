using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Rotation Speed")]
    public float bodyRotationSpeed = 720f;
    public float legsRotationSpeed = 540f;

    [Header("Vida")]
    [SerializeField] private float maxHealth     = 100f;
    [SerializeField] private float currentHealth;

    [Header("References")]
    public Transform body; // gira em direção ao mouse
    public Transform legs; // gira em direção ao WASD

    Rigidbody2D rb;
    Camera      cam;
    Vector2     moveInput;
    float       lastMoveAngle;
    bool        isDead;

    void Awake()
    {
        rb            = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;

        cam = Camera.main ?? FindFirstObjectByType<Camera>();

        if (cam  == null) Debug.LogError("PlayerController: nenhuma câmera encontrada!", this);
        if (body == null) Debug.LogError("PlayerController: campo 'Body' não atribuído!", this);
        if (legs == null) Debug.LogError("PlayerController: campo 'Legs' não atribuído!", this);
    }

    void Update()
    {
        if (isDead) return;

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (moveInput.sqrMagnitude > 0)
            lastMoveAngle = Vector2ToAngle(moveInput);

        RotateTowardsMouse();
        RotateLegs();
    }

    void FixedUpdate()
    {
        if (isDead) return;
        rb.linearVelocity = moveInput * moveSpeed;
    }

    // ──────────────────────────────────────────────
    // IDamageable
    // ──────────────────────────────────────────────
    public void TakeDamage(float damage, DamageType damageType)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        isDead            = true;
        rb.linearVelocity = Vector2.zero;
        // TODO: tocar animação de morte, tela de game over, etc.
        gameObject.SetActive(false);
    }

    void RotateTowardsMouse()
    {
        if (cam == null || body == null) return;
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir      = mousePos - body.position;
        float   angle    = Vector2ToAngle(dir);
        body.rotation = Quaternion.RotateTowards(body.rotation, Quaternion.Euler(0, 0, angle), bodyRotationSpeed * Time.deltaTime);
    }

    void RotateLegs()
    {
        Quaternion target = Quaternion.Euler(0, 0, lastMoveAngle);
        legs.rotation = Quaternion.RotateTowards(legs.rotation, target, legsRotationSpeed * Time.deltaTime);
    }

    float Vector2ToAngle(Vector2 dir)
        => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
}