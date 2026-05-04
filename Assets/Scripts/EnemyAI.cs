using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────────
    // MÁQUINA DE ESTADOS
    // ─────────────────────────────────────────────
    public enum AIState { Patrol, Hold, Chase, Search, Combat, PickupWeapon, Stunned }

    [Header("Estado Atual (Apenas Leitura)")]
    public AIState currentState = AIState.Patrol;

    // ─────────────────────────────────────────────
    // CONFIGURAÇÕES BASE
    // ─────────────────────────────────────────────
    [Header("Configurações Base")]
    [SerializeField] private float health      = 1f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed  = 5f;
    private Rigidbody2D rb;

    // ─────────────────────────────────────────────
    // MÓDULO DE VISÃO (FOV)
    // ─────────────────────────────────────────────
    [Header("Módulo de Visão (FOV)")]
    [SerializeField] private float               detectionRadius = 12f;
    [SerializeField][Range(0, 360)] private float viewAngle      = 90f;
    [SerializeField] private LayerMask           obstacleLayer;

    // ─────────────────────────────────────────────
    // MÓDULO DE COMBATE
    // ─────────────────────────────────────────────
    [Header("Módulo de Combate")]
    [SerializeField] private float attackRadius   = 8f;
    [SerializeField] private float aiReactionTime = 0.5f;
    private float nextAiAttackTime;

    // ─────────────────────────────────────────────
    // MÓDULO DE PATRULHA
    // ─────────────────────────────────────────────
    [Header("Módulo de Patrulha")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float       patrolWaitTime = 1f;
    private int   currentPatrolIndex;
    private float waitTimer;

    // ─────────────────────────────────────────────
    // MÓDULO DE BUSCA (SEARCH)
    // ─────────────────────────────────────────────
    [Header("Módulo de Busca")]
    [SerializeField] private float searchDuration      = 4f;
    [SerializeField] private float searchRotationSpeed = 90f;
    private Vector2 lastKnownPlayerPosition;
    private float   searchTimer;

    // ─────────────────────────────────────────────
    // MÓDULO DE ARMA
    // ─────────────────────────────────────────────
    [Header("Módulo de Arma")]
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform  holdPoint;
    [SerializeField] private float      stunDuration = 2f; // tempo no chão após levar arma arremessada
    [SerializeField] private float      stunKnockbackForce   = 1.5f; // ← reduzido; ajuste no Inspector
    [SerializeField] private float      stunnedZRotation     = 90f;

    private IWeapon heldWeapon;
    private IWeapon droppedWeapon;
    private bool    readyToPickupWeapon;
    private float   stunTimer;

    // ─────────────────────────────────────────────
    // REFERÊNCIAS E ESTADO INTERNO
    // ─────────────────────────────────────────────
    [Header("Referências")]
    [SerializeField] private Transform playerTransform;

    private bool       isDead;
    private bool       isAggro;
    private Collider2D enemyCollider;

    // ══════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gameObject.tag = "Enemy";
        enemyCollider  = GetComponent<Collider2D>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        SpawnWeapon();
        SetInitialState();
    }

    void Update()
{
    if (isDead || playerTransform == null) return;

    // BLOQUEIO TOTAL: Se estiver atordoado, ele executa apenas a lógica de stun
    if (currentState == AIState.Stunned)
    {
        StunnedBehavior();
        return; // O 'return' impede que o UpdateVisionModule() seja chamado abaixo
    }

    UpdateVisionModule();
    ExecuteCurrentState();
}

    // ══════════════════════════════════════════════
    // INICIALIZAÇÃO
    // ══════════════════════════════════════════════
    private void SpawnWeapon()
    {
        if (weaponPrefab == null) return;

        Transform spawnPoint = holdPoint != null ? holdPoint : transform;
        GameObject go = Instantiate(weaponPrefab, spawnPoint.position, Quaternion.identity);
        heldWeapon = go.GetComponent<IWeapon>();

        if (heldWeapon != null)
        {
            heldWeapon.OnPickup(spawnPoint, enemyCollider);
        }
        else
        {
            Debug.LogWarning($"[EnemyAI] {name}: prefab '{weaponPrefab.name}' não tem IWeapon!");
            Destroy(go);
        }
    }

    private void SetInitialState()
    {
        bool singlePoint = patrolPoints != null && patrolPoints.Length == 1;
        currentState = singlePoint ? AIState.Hold : AIState.Patrol;
    }

    // ══════════════════════════════════════════════
    // MÓDULO 1 — VISÃO E TOMADA DE DECISÃO
    // ══════════════════════════════════════════════
    private void UpdateVisionModule()
    {
        float distToPlayer  = Vector2.Distance(transform.position, playerTransform.position);
        bool  hasLOS        = CheckLineOfSight(distToPlayer);
        bool  inFOV         = isAggro || IsPlayerInCone();
        bool  playerVisible = hasLOS && distToPlayer <= detectionRadius && inFOV;

        if (playerVisible)
        {
            isAggro                 = true;
            lastKnownPlayerPosition = playerTransform.position;

            float range = GetAttackRange();
            ChangeState(distToPlayer <= range ? AIState.Combat : AIState.Chase);
        }
        else if (isAggro)
        {
            if (currentState == AIState.Combat || currentState == AIState.Chase)
                ChangeState(AIState.Search);
            // Search e PickupWeapon se gerenciam sozinhos
        }
        else
        {
            if (currentState != AIState.PickupWeapon)
            {
                bool singlePoint = patrolPoints != null && patrolPoints.Length == 1;
                ChangeState(singlePoint ? AIState.Hold : AIState.Patrol);
            }
        }
    }

    private bool CheckLineOfSight(float distance)
    {
        Vector2      dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, distance, obstacleLayer);
        return hit.collider == null;
    }

    private bool IsPlayerInCone()
    {
        Vector2 dir   = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        float   angle = Vector2.Angle(transform.up, dir);
        return angle < viewAngle / 2f;
    }

    private float GetAttackRange()
    {
        if (heldWeapon != null && heldWeapon.type == WeaponType.Melee)
            return heldWeapon.meleeRange;
        return attackRadius;
    }

    // ══════════════════════════════════════════════
    // MÓDULO 2 — MÁQUINA DE ESTADOS
    // ══════════════════════════════════════════════
    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case AIState.Patrol:       PatrolBehavior();       break;
            case AIState.Hold:         HoldBehavior();         break;
            case AIState.Chase:        ChaseBehavior();        break;
            case AIState.Search:       SearchBehavior();       break;
            case AIState.Combat:       CombatBehavior();       break;
            case AIState.PickupWeapon: PickupWeaponBehavior(); break;
        }
    }

    private void ChangeState(AIState newState)
{
    if (currentState == newState) return;

    // TRAVA: Se estiver em Stun, ignora mudanças de estado automáticas da visão
    if (currentState == AIState.Stunned)
    {
        // Só permite sair do Stun se o novo estado for PickupWeapon ou Patrol (pós-recuperação)
        if (newState != AIState.PickupWeapon && newState != AIState.Patrol && newState != AIState.Hold)
        {
            return;
        }
    }

    // Ao entrar em estado passivo, verifica se há arma esperando ser pega
    if (readyToPickupWeapon)
    {
        if (droppedWeapon == null || droppedWeapon.IsHeld())
        {
            readyToPickupWeapon = false;
            droppedWeapon = null;
        }
        else
        {
            bool isPassiveState = newState == AIState.Patrol
                               || newState == AIState.Hold
                               || newState == AIState.Search;
            if (isPassiveState)
            {
                currentState = AIState.PickupWeapon;
                return;
            }
        }
    }

    currentState = newState;
    if (newState == AIState.Search) searchTimer = searchDuration;
}

    // ══════════════════════════════════════════════
    // MÓDULO 3 — COMPORTAMENTOS DE ESTADO
    // ══════════════════════════════════════════════
    private void PatrolBehavior()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform target = patrolPoints[currentPatrolIndex];
        MoveTowards(target.position, patrolSpeed);
        LookAtTarget(target.position);

        if (Vector2.Distance(transform.position, target.position) < 0.2f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                waitTimer          = patrolWaitTime;
            }
        }
    }

    private void HoldBehavior()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
            MoveTowards(patrolPoints[0].position, patrolSpeed);

        transform.Rotate(0f, 0f, searchRotationSpeed * 0.25f * Time.deltaTime);
    }

    private void ChaseBehavior()
    {
        Vector2 target = (Vector2)playerTransform.position;
        MoveTowards(target, chaseSpeed);
        LookAtTarget(target);
    }

    private void SearchBehavior()
    {
        float distToLast = Vector2.Distance(transform.position, lastKnownPlayerPosition);

        if (distToLast > 0.5f)
        {
            MoveTowards(lastKnownPlayerPosition, patrolSpeed);
            LookAtTarget(lastKnownPlayerPosition);
        }
        else
        {
            transform.Rotate(0f, 0f, searchRotationSpeed * Time.deltaTime);
        }

        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            isAggro = false;
            bool singlePoint = patrolPoints != null && patrolPoints.Length == 1;
            ChangeState(singlePoint ? AIState.Hold : AIState.Patrol);
        }
    }

    private void CombatBehavior()
    {
        LookAtTarget(playerTransform.position);

        if (heldWeapon != null && heldWeapon.type == WeaponType.Melee)
            MoveTowards(playerTransform.position, chaseSpeed);

        ExecuteAttack();
    }

    private void PickupWeaponBehavior()
    {
        if (droppedWeapon == null || droppedWeapon.IsHeld())
        {
            readyToPickupWeapon = false;
            droppedWeapon       = null;
            ChangeState(isAggro ? AIState.Chase : AIState.Patrol);
            return;
        }

        Vector2 weaponPos = droppedWeapon.transform.position;
        MoveTowards(weaponPos, patrolSpeed);
        LookAtTarget(weaponPos);

        if (Vector2.Distance(transform.position, weaponPos) < 0.5f)
        {
            heldWeapon          = droppedWeapon;
            droppedWeapon       = null;
            readyToPickupWeapon = false;

            Transform pickup = holdPoint != null ? holdPoint : transform;
            heldWeapon.OnPickup(pickup, enemyCollider);

            ChangeState(isAggro ? AIState.Chase : AIState.Patrol);
        }
    }

    private void StunnedBehavior()
{
    // Força o Rigidbody a parar gradualmente para não deslizar infinitamente
    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 3f);

    stunTimer -= Time.deltaTime;
    if (stunTimer <= 0f)
    {
        RecoverFromStun();
    }
}

    private void RecoverFromStun()
{
    // Reseta a rotação para o inimigo ficar "em pé" novamente
    transform.rotation = Quaternion.identity;

    if (droppedWeapon != null && !droppedWeapon.IsHeld())
    {
        readyToPickupWeapon = true;
        // Forçamos a troca de estado ignorando a trava anterior
        currentState = AIState.PickupWeapon; 
    }
    else
    {
        droppedWeapon = null;
        readyToPickupWeapon = false;
        bool singlePoint = patrolPoints != null && patrolPoints.Length == 1;
        currentState = isAggro ? AIState.Chase : (singlePoint ? AIState.Hold : AIState.Patrol);
    }
}

    // ══════════════════════════════════════════════
    // MÓDULO 4 — ATAQUE
    // ══════════════════════════════════════════════
    private void ExecuteAttack()
    {
        if (heldWeapon == null || Time.time < nextAiAttackTime) return;

        Vector2 aimDir   = ((Vector2)playerTransform.position - (Vector2)heldWeapon.transform.position).normalized;
        bool    attacked = false;

        switch (heldWeapon.type)
        {
            case WeaponType.Firearm: attacked = heldWeapon.TryShoot(aimDir);       break;
            case WeaponType.Melee:   attacked = heldWeapon.TryMeleeAttack(aimDir); break;
        }

        if (attacked) nextAiAttackTime = Time.time + aiReactionTime;
    }

    // ══════════════════════════════════════════════
    // MÓDULO 5 — SISTEMA DE DANO (IDamageable)
    // ══════════════════════════════════════════════
    public void TakeDamage(float damage, DamageType damageType)
{
    if (isDead) return;

    isAggro = true;

    switch (damageType)
    {
        case DamageType.Bullet:
        case DamageType.Melee:
            health -= damage;
            if (health <= 0) Die();
            break;

        case DamageType.Thrown:
            OnHitByThrownWeapon();
            break;

        // NOVO CASO: Finalização no chão
        case DamageType.Finisher:
            ExecuteFinisherLogic();
            break;
    }
}

//finalizaçao
private void ExecuteFinisherLogic()
{
    // Aqui você pode adicionar efeitos especiais antes de morrer
    Debug.Log($"{name} foi executado brutalmente!");
    
    // Se tiver um sistema de partículas de sangue:
    // Instantiate(bloodSplashPrefab, transform.position, Quaternion.identity);

    Die(); // Chama o método de morte que você já tem
    
}

    private void OnHitByThrownWeapon()
{
    droppedWeapon = heldWeapon;
    DropWeapon();

    // Entra no estado de Stun primeiro
    currentState = AIState.Stunned;
    stunTimer = stunDuration;

    // Zera inércia anterior para o knockback ser consistente
    rb.linearVelocity = Vector2.zero;
    rb.angularVelocity = 0f;

    Vector2 knockbackDir = (transform.position - playerTransform.position).normalized;
    rb.AddForce(knockbackDir * stunKnockbackForce, ForceMode2D.Impulse);
    
    // Gira o boneco para o lado (visual de queda)
    transform.rotation = Quaternion.Euler(0, 0, stunnedZRotation);
}

    private void Die()
    {
        isDead = true;
        DropWeapon();
        if (enemyCollider != null) enemyCollider.enabled = false;
        this.enabled = false;
        Destroy(gameObject, 0.05f);
    }

    private void DropWeapon()
    {
        if (heldWeapon == null) return;
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        heldWeapon.OnThrow(randomDir * 0.2f);
        heldWeapon = null;
    }

    // ══════════════════════════════════════════════
    // UTILITÁRIOS
    // ══════════════════════════════════════════════
    private void MoveTowards(Vector2 target, float speed)
{
    // Em vez de mudar a posição, definimos a velocidade (Velocity)
    Vector2 direction = (target - (Vector2)transform.position).normalized;
    rb.linearVelocity = direction * speed; 
}
    private void LookAtTarget(Vector2 targetPos)
    {
        Vector2 dir   = targetPos - (Vector2)transform.position;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Vector3 leftRay  = Quaternion.Euler(0, 0,  viewAngle / 2f) * transform.up;
        Vector3 rightRay = Quaternion.Euler(0, 0, -viewAngle / 2f) * transform.up;
        Gizmos.DrawRay(transform.position, leftRay  * detectionRadius);
        Gizmos.DrawRay(transform.position, rightRay * detectionRadius);
    }
}