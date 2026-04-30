using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Follow Settings")]
    public float smoothTime        = 0.25f;  // lag do follow normal
    public float lookAheadDistance = 1.5f;   // quanto se adianta na direção do movimento

    [Header("Shift Zoom-Out")]
    public float shiftPanDistance  = 3f;     // distância máxima que a câmera vai na direção do mouse
    public float shiftSmoothTime   = 0.4f;   // quão suave é a transição ao segurar/soltar Shift

    Rigidbody2D playerRb;
    Camera       cam;
    Vector3      velocity      = Vector3.zero;
    Vector3      shiftVelocity = Vector3.zero;
    Vector3      shiftOffset   = Vector3.zero; // offset extra aplicado ao segurar Shift

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("CameraFollow: campo 'Player' não atribuído no Inspector!", this);
            return;
        }

        playerRb = player.GetComponent<Rigidbody2D>();
        cam      = Camera.main ?? FindFirstObjectByType<Camera>();

        transform.position = new Vector3(player.position.x, player.position.y, transform.position.z);
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // ── Follow base (igual ao anterior) ──────────────────────────────
        Vector2 moveDir   = playerRb != null ? playerRb.linearVelocity.normalized : Vector2.zero;
        Vector2 targetPos = (Vector2)player.position + moveDir * lookAheadDistance;
        Vector3 baseTarget = new Vector3(targetPos.x, targetPos.y, transform.position.z);

        // ── Shift: pan em direção ao mouse ────────────────────────────────
        Vector3 desiredShiftOffset = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (cam != null)
            {
                // Direção do player até o mouse no world space
                Vector3 mouse     = Input.mousePosition;
                mouse.z           = Mathf.Abs(cam.transform.position.z);
                Vector3 mouseWorld = cam.ScreenToWorldPoint(mouse);

                Vector2 toMouse   = (Vector2)mouseWorld - (Vector2)player.position;

                // Clamp: não vai além de shiftPanDistance independente do quão longe o mouse está
                if (toMouse.magnitude > shiftPanDistance)
                    toMouse = toMouse.normalized * shiftPanDistance;

                desiredShiftOffset = new Vector3(toMouse.x, toMouse.y, 0f);
            }
        }

        // Suaviza a entrada e saída do shift offset (sem pular)
        shiftOffset = Vector3.SmoothDamp(shiftOffset, desiredShiftOffset, ref shiftVelocity, shiftSmoothTime);

        // ── Posição final da câmera ───────────────────────────────────────
        Vector3 finalTarget = baseTarget + shiftOffset;

        transform.position = Vector3.SmoothDamp(transform.position, finalTarget, ref velocity, smoothTime);
    }
}