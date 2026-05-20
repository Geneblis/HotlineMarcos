using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Hold Point")]
    public Transform holdPoint;

    [Header("Pickup")]
    public float     pickupRadius = 1f;
    public LayerMask weaponLayer;

    [Header("Audio")]
    public AudioClip pickupSound;
    public AudioClip throwSound;

    [Header("Punch Settings")]
    public float punchRange    = 1.2f;
    public float punchDamage   = 0.2f;
    public float punchCooldown = 0.4f;
    private float nextPunchTime;

    [Header("Finisher Settings")]
    public KeyCode finisherKey    = KeyCode.Space;
    public float   finisherRadius = 1.5f;

    IWeapon                      equippedWeapon;
    Camera                       cam;
    Collider2D                   playerCollider;
    AudioSource                  audioSource;
    PlayerBodyAnimationController bodyAnimController;

    void Awake()
    {
        cam                = FindFirstObjectByType<Camera>();
        playerCollider     = GetComponent<Collider2D>();
        audioSource        = GetComponent<AudioSource>();
        bodyAnimController = FindFirstObjectByType<PlayerBodyAnimationController>();
    }

    void Update()
    {
        if (PlayerBodyAnimationController.IsExecuting) return;

        Vector2 mouseDir = GetMouseDirection();

        if (Input.GetMouseButtonDown(1))
        {
            if (equippedWeapon == null)
            {
                TryPickup();
                if (equippedWeapon != null) return;
            }
            else
            {
                ThrowWeapon(mouseDir);
                return;
            }
        }

        if (Input.GetKeyDown(finisherKey))
        {
            TryExecuteFinisher();
        }

        if (equippedWeapon != null)
        {
            HandleShooting(mouseDir);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            PerformPunch(mouseDir);
        }
    }

    void PerformPunch(Vector2 direction)
    {
        if (Time.time < nextPunchTime) return;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 0.5f, direction, punchRange);

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(punchDamage, DamageType.Thrown);

                    Rigidbody2D rbEnemy = hit.collider.GetComponent<Rigidbody2D>();
                    if (rbEnemy != null)
                        rbEnemy.AddForce(direction * 4f, ForceMode2D.Impulse);

                    break;
                }
            }
        }

        nextPunchTime = Time.time + punchCooldown;
    }

    void TryExecuteFinisher()
    {
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, finisherRadius);

        foreach (var obj in nearbyObjects)
        {
            if (obj.CompareTag("Enemy"))
            {
                EnemyAI enemy = obj.GetComponent<EnemyAI>();

                if (enemy != null && enemy.currentState == EnemyAI.AIState.Stunned)
                {
                    ExecuteFinisher(enemy);
                    break;
                }
            }
        }
    }

    void ExecuteFinisher(EnemyAI enemy)
    {
        transform.position = enemy.transform.position;
        enemy.TakeDamage(999f, DamageType.Finisher);
        bodyAnimController?.TriggerExecution();
    }

    public IWeapon GetEquippedWeapon() => equippedWeapon;

    void TryPickup()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, weaponLayer);
        IWeapon closest   = null;
        float   minDist   = float.MaxValue;

        foreach (var hit in hits)
        {
            IWeapon w = hit.GetComponent<IWeapon>();
            if (w == null || w.IsHeld()) continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < minDist) { minDist = dist; closest = w; }
        }

        if (closest != null)
            Equip(closest);
    }

    void Equip(IWeapon weapon)
    {
        equippedWeapon = weapon;
        equippedWeapon.OnPickup(holdPoint, playerCollider);
        PlayPickupSound();
        NotifyAmmoHUD();
    }

    void ThrowWeapon(Vector2 direction)
    {
        equippedWeapon.OnThrow(direction);
        PlayThrowSound();
        equippedWeapon = null;
        GameManager.Instance?.ClearAmmo();
    }

    void HandleShooting(Vector2 mouseDir)
    {
        bool buttonDown    = Input.GetMouseButton(0);
        bool buttonPressed = Input.GetMouseButtonDown(0);

        if (equippedWeapon.IsOfType(WeaponType.OneHandMelee) || equippedWeapon.IsOfType(WeaponType.TwoHandMelee))
        {
            if (buttonPressed) equippedWeapon.TryMeleeAttack(mouseDir);
            return;
        }

        bool wantsToShoot = false;
        if (equippedWeapon.IsOfType(WeaponType.OneHandFirearm) || equippedWeapon.IsOfType(WeaponType.TwoHandFirearm))
        {
            wantsToShoot = equippedWeapon.fireType == FireType.Automatic ? buttonDown : buttonPressed;
        }

        if (wantsToShoot)
        {
            bool shotFired = equippedWeapon.TryShoot(mouseDir);
            if (shotFired) NotifyAmmoHUD();
        }
    }

    void NotifyAmmoHUD()
    {
        if (GameManager.Instance == null || equippedWeapon == null) return;

        if (equippedWeapon.IsOfType(WeaponType.OneHandMelee) || equippedWeapon.IsOfType(WeaponType.TwoHandMelee))
        {
            GameManager.Instance.ClearAmmo();
            return;
        }

        GameManager.Instance.UpdateAmmo(equippedWeapon.currentAmmo, equippedWeapon.maxAmmo);
    }

    void PlayPickupSound()
    {
        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound);
    }

    void PlayThrowSound()
    {
        if (throwSound != null && audioSource != null)
            audioSource.PlayOneShot(throwSound);
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