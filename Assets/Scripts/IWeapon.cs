using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class IWeapon : MonoBehaviour
{
    [Header("General")]
    public string     weaponName;
    public WeaponType type;
    public int        maxAmmo;
    public int        currentAmmo;

    [Header("Origin Point")]
    [Tooltip("Transform that indicates where bullets/attacks originate from")]
    public Transform firePoint;

    [Header("Throw")]
    public float throwForce            = 10f;
    public float throwDrag             = 3f;
    public float stopVelocityThreshold = 0.05f;

    [Header("Firearm")]
    public FireType   fireType;
    public float      fireRate       = 5f;
    public int        pelletsPerShot = 1;
    public float      spreadAngle    = 0f;
    public GameObject projectilePrefab;

    [Header("Melee")]
    public bool  isBladed    = false;
    public float meleeRange  = 1.2f;
    public float meleeDamage = 50f;

    bool        isHeld;
    bool        isThrown;
    float       fireCooldown;
    Rigidbody2D rb;
    Collider2D  col;
    Collider2D  playerCol;
    Vector3     originalScale;

    void Awake()
    {
        rb            = GetComponent<Rigidbody2D>();
        col           = GetComponent<Collider2D>();
        originalScale = transform.localScale;

        currentAmmo = maxAmmo;
        SetPhysicsState(true);
    }

    void Update()
    {
        if (fireCooldown > 0)
            fireCooldown -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (!isThrown) return;

        if (rb.linearVelocity.sqrMagnitude < stopVelocityThreshold * stopVelocityThreshold)
        {
            rb.angularVelocity = 0f;
            rb.constraints     = RigidbodyConstraints2D.FreezeRotation;
            isThrown           = false;
        }
    }

    public void OnPickup(Transform holdPoint, Collider2D pickerCollider)
    {
        isHeld    = true;
        isThrown  = false;
        playerCol = pickerCollider;

        SetPhysicsState(false);

        transform.SetParent(holdPoint);
        transform.localPosition = Vector2.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale    = originalScale;
    }

    public void OnThrow(Vector2 direction)
    {
        isHeld   = false;
        isThrown = true;

        transform.SetParent(null);
        transform.localScale = originalScale;

        SetPhysicsState(true);

        rb.constraints     = RigidbodyConstraints2D.None;
        rb.linearDamping   = throwDrag;
        rb.AddForce(direction.normalized * throwForce, ForceMode2D.Impulse);
        rb.angularVelocity = throwForce * 40f;

        if (playerCol != null)
            Physics2D.IgnoreCollision(col, playerCol, true);
    }

    public bool TryShoot(Vector2 direction)
    {
        if (!isHeld)                    return false;
        if (type != WeaponType.Firearm) return false;
        if (currentAmmo <= 0)           return false;
        if (fireCooldown > 0)           return false;

        Shoot(direction);
        return true;
    }

    public bool TryMeleeAttack(Vector2 direction)
    {
        if (!isHeld)                  return false;
        if (type != WeaponType.Melee) return false;
        if (fireCooldown > 0)         return false;

        MeleeAttack(direction);
        return true;
    }

    public bool IsEmpty()   => currentAmmo <= 0;
    public bool IsHeld()    => isHeld;
    public bool IsFirearm() => type == WeaponType.Firearm;
    public bool IsMelee()   => type == WeaponType.Melee;

    void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"Weapon '{weaponName}': projectilePrefab not assigned!");
            return;
        }

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        for (int i = 0; i < pelletsPerShot; i++)
        {
            Vector2    dir = ApplySpread(direction, spreadAngle);
            GameObject go  = Instantiate(projectilePrefab, origin, Quaternion.identity);
            go.GetComponent<Projectile>()?.Launch(dir, col);
        }

        currentAmmo--;
        fireCooldown = 1f / fireRate;
    }

    void MeleeAttack(Vector2 direction)
    {
        Vector2    origin   = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2    hitPoint = origin + direction.normalized * meleeRange;
        Collider2D hit      = Physics2D.OverlapCircle(hitPoint, 0.4f);

        if (hit != null && !hit.CompareTag("Player"))
        {
            // hit.GetComponent<IDamageable>()?.TakeDamage(isBladed ? 9999 : (int)meleeDamage);
            Debug.Log($"Melee hit: {hit.name} | Bladed: {isBladed}");
        }

        fireCooldown = 1f / fireRate;
    }

    Vector2 ApplySpread(Vector2 dir, float degrees)
    {
        if (degrees <= 0) return dir;
        float angle = Random.Range(-degrees / 2f, degrees / 2f);
        return Quaternion.Euler(0, 0, angle) * dir;
    }

    void SetPhysicsState(bool active)
    {
        rb.bodyType        = active ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;
        col.isTrigger      = !active;

        if (active && playerCol != null)
            Physics2D.IgnoreCollision(col, playerCol, false);
    }
}

public enum WeaponType { Firearm, Melee }
public enum FireType   { SemiAuto, Automatic }