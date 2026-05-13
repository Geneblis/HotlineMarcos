using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Rotation Speed")]
    public float bodyRotationSpeed = 720f;
    public float legsRotationSpeed = 540f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 1f;
    [SerializeField] private float currentHealth;

    [Header("References")]
    public Transform body;
    public Transform legs;

    [Header("Death")]
    [SerializeField] private GameObject corpsePrefab;
    [SerializeField] private float corpseLayerOffset = 3f;

    Rigidbody2D rb;
    Camera cam;
    Vector2 moveInput;
    float lastMoveAngle;
    bool isDead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        cam = FindFirstObjectByType<Camera>();

        if (cam == null) Debug.LogError("PlayerController: no camera found!", this);
        if (body == null) Debug.LogError("PlayerController: 'Body' not assigned!", this);
        if (legs == null) Debug.LogError("PlayerController: 'Legs' not assigned!", this);
        if (corpsePrefab == null) Debug.LogError("PlayerController: 'Corpse Prefab' not assigned!", this);
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

    public void TakeDamage(float damage, DamageType damageType)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (corpsePrefab != null)
        {
            Vector3 corpsePosition = transform.position;
            corpsePosition.z += corpseLayerOffset;

            float randomRotation = Random.Range(-3, 12);
            Quaternion corpseRotation = transform.rotation * Quaternion.Euler(0f, 0f, randomRotation);

            Instantiate(corpsePrefab, corpsePosition, corpseRotation);
        }

        gameObject.SetActive(false);
        GameManager.Instance?.OnPlayerDied();
    }

    void RotateTowardsMouse()
    {
        if (cam == null || body == null) return;

        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mousePos - body.position;
        float angle = Vector2ToAngle(dir);

        body.rotation = Quaternion.RotateTowards(
            body.rotation,
            Quaternion.Euler(0, 0, angle),
            bodyRotationSpeed * Time.deltaTime
        );
    }

    void RotateLegs()
    {
        if (legs == null) return;

        Quaternion target = Quaternion.Euler(0, 0, lastMoveAngle);
        legs.rotation = Quaternion.RotateTowards(
            legs.rotation,
            target,
            legsRotationSpeed * Time.deltaTime
        );
    }

    float Vector2ToAngle(Vector2 dir)
        => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
}