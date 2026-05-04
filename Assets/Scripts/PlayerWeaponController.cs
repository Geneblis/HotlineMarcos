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
    [Header("Punch Settings")]
    public float punchRange = 1.2f;
    public float punchDamage = 0.2f;
    public float punchCooldown = 0.4f;
    private float nextPunchTime;
    [Header("Finisher Settings")]
    public KeyCode finisherKey = KeyCode.Space; // Tecla customizável
    public float finisherRadius = 1.5f;         // Distância para conseguir finalizar

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

    // Botão Direito: Pegar ou Jogar
    if (Input.GetMouseButtonDown(1))
    {
        if (equippedWeapon == null)
        {
            TryPickup();
            // IMPORTANTE: Se pegamos a arma, saímos do Update aqui 
            // para não correr o risco de jogá-la no mesmo clique.
            if (equippedWeapon != null) return; 
        }
        else
        {
            ThrowWeapon(mouseDir);
            return; // Sai do update após jogar
        }
    }

    // Botão de Finalização (Space)
    if (Input.GetKeyDown(finisherKey))
    {
        TryExecuteFinisher();
    }

    // Lógica de Ataque (Botão Esquerdo)
    if (equippedWeapon != null)
    {
        HandleShooting(mouseDir);
    }
    else if (Input.GetMouseButtonDown(0))
    {
        PerformPunch(mouseDir);
    }
}

    //_________________________________________
    // Parte do soco do personagem
    //_________________________________________
    void PerformPunch(Vector2 direction)
    {
        if (Time.time < nextPunchTime) return;

        // Dispara um círculo na direção do soco para detectar colisões
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 0.5f, direction, punchRange);

        foreach (var hit in hits)
        {
            // Verifica se o objeto atingido tem a Tag "Enemy"
            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                if (target != null)
                {
                    // Envia o dano tipo Thrown para ativar o Stun no EnemyAI
                    target.TakeDamage(punchDamage, DamageType.Thrown);

                    // Impacto físico
                    Rigidbody2D rbEnemy = hit.collider.GetComponent<Rigidbody2D>();
                    if (rbEnemy != null)
                    {
                        rbEnemy.AddForce(direction * 4f, ForceMode2D.Impulse);
                    }

                    Debug.Log("Soco atingiu o inimigo!");
                    
                    // Se você quiser que o soco pare no primeiro inimigo atingido:
                    break; 
                }
            }
        }

        nextPunchTime = Time.time + punchCooldown;
    }
    void TryExecuteFinisher()
    {
        // Procura todos os objetos ao redor do player
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, finisherRadius);

        foreach (var obj in nearbyObjects)
        {
            if (obj.CompareTag("Enemy"))
            {
                EnemyAI enemy = obj.GetComponent<EnemyAI>();

                // Só finaliza se o inimigo existir E estiver no estado Stunned
                if (enemy != null && enemy.currentState == EnemyAI.AIState.Stunned)
                {
                    ExecuteFinisher(enemy);
                    break; // Finaliza apenas um por vez
                }
            }
        }
    }

    // Finalizaçao do inimigo
    // No PlayerWeaponController.cs, dentro da função ExecuteFinisher:
void ExecuteFinisher(EnemyAI enemy)
{
    // Chamamos o Finisher em vez de Bullet para o inimigo saber como morreu
    enemy.TakeDamage(999f, DamageType.Finisher); 
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
