using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Hold Point")]
    [Tooltip("Child transform of UpperBody where the weapon is positioned in the hand")]
    public Transform holdPoint;

    [Header("Pickup")]
    public float     pickupRadius = 1f;
    public LayerMask weaponLayer;

    [Header("Audio")]
    [Tooltip("Som para tocar ao pegar uma arma")]
    public AudioClip pickupSound;
    [Tooltip("Som para tocar ao arremessar uma arma")]
    public AudioClip throwSound;

    IWeapon     equippedWeapon;
    Camera      cam;
    Collider2D  playerCollider;
    AudioSource audioSource;

    void Awake()
    {
        cam            = FindFirstObjectByType<Camera>();
        playerCollider = GetComponent<Collider2D>();
        audioSource    = GetComponent<AudioSource>();
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
        float   minDist  = float.MaxValue;

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

        if (equippedWeapon.IsOfType(WeaponType.Melee))
        {
            if (buttonPressed) equippedWeapon.TryMeleeAttack(mouseDir);
            return;
        }

        bool wantsToShoot = false;
        if (equippedWeapon.IsOfType(WeaponType.Firearm))
        {
            wantsToShoot = equippedWeapon.fireType == FireType.Automatic
                ? buttonDown //if
                : buttonPressed; //else
        }

        if (wantsToShoot)
        {
            bool shotFired = equippedWeapon.TryShoot(mouseDir);
            if (shotFired)
                NotifyAmmoHUD();
        }
    }

    void NotifyAmmoHUD()
    {
        if (GameManager.Instance == null || equippedWeapon == null) return;

        if (equippedWeapon.IsOfType(WeaponType.Melee))
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
