using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Rotation Speed")]
    public float bodyRotationSpeed = 720f;
    public float legsRotationSpeed = 540f;

    [Header("References")]
    public Transform body;   // gira em direção ao mouse
    public Transform legs;   // gira em direção ao WASD

    Rigidbody2D rb;
    Camera cam;
    Vector2 moveInput;
    float lastMoveAngle;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();

        // Camera.main exige a tag "MainCamera" — se falhar, busca qualquer câmera na cena
        cam = Camera.main ?? FindFirstObjectByType<Camera>();

        if (cam == null)
            Debug.LogError("PlayerController: nenhuma câmera encontrada na cena!", this);
        if (body == null)
            Debug.LogError("PlayerController: campo 'Body' não atribuído no Inspector!", this);
        if (legs == null)
            Debug.LogError("PlayerController: campo 'Legs' não atribuído no Inspector!", this);
    }

    void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (moveInput.sqrMagnitude > 0)
            lastMoveAngle = Vector2ToAngle(moveInput);

        RotateTowardsMouse();
        RotateLegs();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    void RotateTowardsMouse()
    {
        if (cam == null || body == null) return;

        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mousePos - body.position;
        float angle = Vector2ToAngle(dir);

        body.rotation = Quaternion.RotateTowards(body.rotation, Quaternion.Euler(0, 0, angle), bodyRotationSpeed * Time.deltaTime);
    }

    void RotateLegs()
    {
        Quaternion target = Quaternion.Euler(0, 0, lastMoveAngle);
        legs.rotation = Quaternion.RotateTowards(legs.rotation, target, legsRotationSpeed * Time.deltaTime);
    }

    // Atan2 retorna ângulo em graus apontando para o eixo +Y (cima)
    float Vector2ToAngle(Vector2 dir)
    {
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
    }
}