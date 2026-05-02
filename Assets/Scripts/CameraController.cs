using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Target")]
    public Transform player;

    [Header("Follow")]
    public float smoothTime        = 0.25f;
    public float lookAheadDistance = 1.5f;

    [Header("Mouse Pan")]
    public float mousePanDistance = 1.5f;
    public float mousePanSmooth   = 0.4f;

    [Header("Shift Pan")]
    public float shiftPanDistance = 3f;
    public float shiftSmoothTime  = 0.4f;

    [Header("Camera Shake")]
    public float traumaDecay = 2.5f;
    public float maxOffset   = 0.3f;
    public float maxAngle    = 3f;
    public float frequency   = 25f;

    Rigidbody2D playerRb;
    Camera      cam;
    Vector3     followVelocity;
    Vector3     shiftVelocity;
    Vector3     mousePanVelocity;
    Vector3     shiftOffset;
    Vector3     mousePanOffset;
    Vector3     smoothedPosition;
    float       trauma;
    float       seed;

    void Awake()
    {
        Instance = this;
        seed     = Random.value * 100f;
    }

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("CameraController: Player not assigned!", this);
            return;
        }

        playerRb           = player.GetComponent<Rigidbody2D>();
        cam                = Camera.main;
        smoothedPosition   = new Vector3(player.position.x, player.position.y, transform.position.z);
        transform.position = smoothedPosition;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector2 moveDir    = playerRb != null ? playerRb.linearVelocity.normalized : Vector2.zero;
        Vector2 targetPos  = (Vector2)player.position + moveDir * lookAheadDistance;
        Vector3 baseTarget = new Vector3(targetPos.x, targetPos.y, smoothedPosition.z);

        Vector3 mouse      = Input.mousePosition;
        mouse.z            = Mathf.Abs(cam.transform.position.z);
        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouse);
        Vector2 toMouse    = (Vector2)mouseWorld - (Vector2)player.position;

        // Mouse pan: sempre ativo, clampado em mousePanDistance
        Vector2 clampedMouse = Vector2.ClampMagnitude(toMouse, mousePanDistance);
        // Normaliza pelo pan máximo para que o offset seja proporcional à distância
        Vector2 mousePanDir  = toMouse.magnitude > 0.01f
            ? clampedMouse / mousePanDistance * mousePanDistance
            : Vector2.zero;

        mousePanOffset = Vector3.SmoothDamp(
            mousePanOffset,
            new Vector3(clampedMouse.x, clampedMouse.y, 0f),
            ref mousePanVelocity,
            mousePanSmooth);

        // Shift pan: offset adicional ao segurar Shift
        Vector3 desiredShift = Vector3.zero;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            Vector2 shiftDir = Vector2.ClampMagnitude(toMouse, shiftPanDistance);
            desiredShift     = new Vector3(shiftDir.x, shiftDir.y, 0f);
        }

        shiftOffset = Vector3.SmoothDamp(shiftOffset, desiredShift, ref shiftVelocity, shiftSmoothTime);

        smoothedPosition = Vector3.SmoothDamp(
            smoothedPosition,
            baseTarget + mousePanOffset + shiftOffset,
            ref followVelocity,
            smoothTime);

        ApplyShake();
    }

    void ApplyShake()
    {
        trauma = Mathf.Max(0f, trauma - traumaDecay * Time.fixedDeltaTime);

        float shake   = trauma * trauma;
        float offsetX = maxOffset * (Mathf.PerlinNoise(seed,      Time.time * frequency) * 2f - 1f) * shake;
        float offsetY = maxOffset * (Mathf.PerlinNoise(seed + 1f, Time.time * frequency) * 2f - 1f) * shake;
        float angle   = maxAngle  * (Mathf.PerlinNoise(seed + 2f, Time.time * frequency) * 2f - 1f) * shake;

        transform.position = smoothedPosition + new Vector3(offsetX, offsetY, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }
}