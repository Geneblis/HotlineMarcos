using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Hold Point")]
    [Tooltip("Child transform of UpperBody where the weapon is positioned in the hand")]
    public Transform holdPoint;

    [Header("Pickup")]
    public float pickupRadius = 1f;
    public LayerMask weaponLayer;

    IWeapon      equippedWeapon;
    Camera      cam;
    Collider2D  playerCollider;

    void Awake()
    {
        cam = Camera.main ?? FindFirstObjectByType<Camera>();
        playerCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        Vector2 mouseDir = GetMouseDirection();

        if (Input.GetMouseButtonDown(1))
        {
            if (equippedWeapon == null)
                TryPickup();
            else
                ThrowWeapon(mouseDir);
        }

        if (equippedWeapon != null)
            HandleShooting(mouseDir);
    }

    void TryPickup()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, weaponLayer);

        IWeapon closest  = null;
        float  minDist  = float.MaxValue;

        foreach (var hit in hits)
        {
            IWeapon w = hit.GetComponent<IWeapon>();
            if (w == null || w.IsHeld()) continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = w;
            }
        }

        if (closest != null)
            Equip(closest);
    }

    void Equip(IWeapon weapon)
    {
        equippedWeapon = weapon;
        equippedWeapon.OnPickup(holdPoint, playerCollider);
    }

    void ThrowWeapon(Vector2 direction)
    {
        equippedWeapon.OnThrow(direction);
        equippedWeapon = null;
    }

    void HandleShooting(Vector2 mouseDir)
    {
        if (equippedWeapon.IsMelee())
        {
            if (Input.GetMouseButtonDown(0))
                equippedWeapon.TryMeleeAttack(mouseDir);

            return;
        }

        bool buttonDown = Input.GetMouseButton(0);
        bool buttonPressed = Input.GetMouseButtonDown(0);

        bool wantsToShoot = equippedWeapon.IsFirearm() &&
            (equippedWeapon.fireType == FireType.Automatic
                ? buttonDown
                : buttonPressed);

        if (wantsToShoot)
        {
            bool fired = equippedWeapon.TryShoot(mouseDir);

            if (!fired && equippedWeapon.IsEmpty())
                Debug.Log("Out of ammo!");
        }
    }

    Vector2 GetMouseDirection()
    {
        if (cam == null) return Vector2.right;

        Vector3 mouse = Input.mousePosition;
        mouse.z       = Mathf.Abs(cam.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(mouse);

        return ((Vector2)world - (Vector2)transform.position).normalized;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}