using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    public enum AIState { Patrol, Hold, Chase, Search, Combat, PickupWeapon, Stunned }

    private enum WeaponHoldCategory { None, OneHand, TwoHand }

    [Header("Current State (Read Only)")]
    public AIState currentState = AIState.Patrol;

    [Header("Base Settings")]
    [SerializeField] private float health      = 1f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed  = 5f;
    private Rigidbody2D rb;

    [Header("Vision Module (FOV)")]
    [SerializeField] private float               detectionRadius = 12f;
    [SerializeField][Range(0, 360)] private float viewAngle      = 90f;
    [SerializeField] private LayerMask           obstacleLayer;

    [Header("Combat Module")]
    [SerializeField] private float attackRadius   = 8f;
    [SerializeField] private float aiReactionTime = 0.5f;
    private float nextAiAttackTime;

    [Header("Patrol Module")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float       patrolWaitTime = 1f;
    private int   currentPatrolIndex;
    private float waitTimer;

    [Header("Search Module")]
    [SerializeField] private float searchDuration      = 4f;
    [SerializeField] private float searchRotationSpeed = 90f;
    private Vector2 lastKnownPlayerPosition;
    private float   searchTimer;

    [Header("Weapon Module")]
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform  holdPoint;
    [SerializeField] private float      stunDuration       = 2f;
    [SerializeField] private float      stunKnockbackForce = 1.5f;
    [SerializeField] private float      stunnedZRotation   = 90f;

    private IWeapon heldWeapon;
    private IWeapon droppedWeapon;
    private bool    readyToPickupWeapon;
    private float   stunTimer;

    // ─────────────────────────────────────────────────────────────────
    [Header("Weapon Search Module")]
    [Tooltip("Maximum distance at which the enemy can detect and decide to pick up a weapon on the ground.")]
    [SerializeField] private float weaponSearchRadius   = 20f;
    [Tooltip("Interval (in seconds) at which the enemy will scan for nearby weapons when unarmed.")]
    [SerializeField] private float weaponSearchInterval = 2f;

    private float weaponSearchTimer; // conta regressiva até a próxima varredura
    // ─────────────────────────────────────────────────────────────────

    [Header("Animation Module")]
    [SerializeField] private Animator animator;
    [SerializeField] private string   idleAnimation           = "Idle";
    [SerializeField] private string   walkAnimation           = "Walk";
    [SerializeField] private string   oneHandIdleAnimation    = "OneHand_Idle";
    [SerializeField] private string   oneHandWalkAnimation    = "OneHand_Walk";
    [SerializeField] private string   twoHandIdleAnimation    = "TwoHand_Idle";
    [SerializeField] private string   twoHandWalkAnimation    = "TwoHand_Walk";
    [SerializeField] private string   onGroundAnimation       = "OnGround";
    [SerializeField] private float    movingVelocityThreshold = 0.1f;

    private string currentAnimation;

    [Header("References")]
    [SerializeField] private Transform playerTransform;

    private bool       isDead;
    private bool       isAggro;
    private Collider2D enemyCollider;

    void Start()
    {
        rb             = GetComponent<Rigidbody2D>();
        enemyCollider  = GetComponent<Collider2D>();
        gameObject.tag = "Enemy";

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

        if (currentState == AIState.Stunned)
        {
            StunnedBehavior();
            UpdateAnimations();
            return;
        }

        UpdateVisionModule();
        UpdateWeaponSearchModule(); // ← novo: busca armas no cenário quando desarmado
        ExecuteCurrentState();
        UpdateAnimations();
    }
    private void UpdateWeaponSearchModule()
    {
        // Só age quando desarmado e em estado compatível
        if (heldWeapon != null)                  return;
        if (currentState == AIState.PickupWeapon) return;
        if (currentState == AIState.Stunned)      return;

        weaponSearchTimer -= Time.deltaTime;
        if (weaponSearchTimer > 0f) return;

        weaponSearchTimer = weaponSearchInterval; // reinicia o intervalo

        IWeapon nearest = FindNearestAvailableWeapon();
        if (nearest == null) return;

        droppedWeapon       = nearest;
        readyToPickupWeapon = true;
        currentState        = AIState.PickupWeapon; // força direto, sem passar pelo ChangeState
    }
    private IWeapon FindNearestAvailableWeapon()
    {
        MonoBehaviour[] allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        IWeapon nearest = null;
        float nearestDist = weaponSearchRadius;
        foreach (MonoBehaviour behaviour in allBehaviours)
        {
            if (behaviour is not IWeapon weapon){continue;}
            if (weapon.IsHeld()){continue;}
            float dist = Vector3.Distance(transform.position, weapon.transform.position);
            if (dist < nearestDist){
                nearestDist = dist;
                nearest = weapon;
            }
        }

        return nearest;
    }

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
            Debug.LogWarning($"[EnemyAI] {name}: prefab '{weaponPrefab.name}' has no IWeapon component!");
            Destroy(go);
        }
    }

    private void SetInitialState()
    {
        bool singlePoint = patrolPoints != null && patrolPoints.Length == 1;
        currentState = singlePoint ? AIState.Hold : AIState.Patrol;
    }

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
        Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
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
        if (heldWeapon != null && (heldWeapon.type == WeaponType.OneHandMelee || heldWeapon.type == WeaponType.TwoHandMelee))
            return heldWeapon.meleeRange;
        return attackRadius;
    }

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

        if (currentState == AIState.Stunned)
        {
            if (newState != AIState.PickupWeapon && newState != AIState.Patrol && newState != AIState.Hold)
                return;
        }

        if (readyToPickupWeapon)
        {
            if (droppedWeapon == null || droppedWeapon.IsHeld())
            {
                readyToPickupWeapon = false;
                droppedWeapon       = null;
            }
            else
            {
                // Sem arma em mãos: prioriza pegar a arma em qualquer estado passivo
                // OU quando o inimigo está desarmado mesmo em estados agressivos
                bool isPassiveState = newState == AIState.Patrol
                                   || newState == AIState.Hold
                                   || newState == AIState.Search;

                bool isUnarmedAndAggressive = heldWeapon == null
                                           && (newState == AIState.Chase || newState == AIState.Combat);

                if (isPassiveState || isUnarmedAndAggressive)
                {
                    currentState = AIState.PickupWeapon;
                    return;
                }
            }
        }

        currentState = newState;
        if (newState == AIState.Search) searchTimer = searchDuration;
    }

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

        if (heldWeapon != null && (heldWeapon.type == WeaponType.OneHandMelee || heldWeapon.type == WeaponType.TwoHandMelee))
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
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 3f);

        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f) RecoverFromStun();
    }

    private void RecoverFromStun()
    {
        transform.rotation = Quaternion.identity;

        if (droppedWeapon != null && !droppedWeapon.IsHeld())
        {
            readyToPickupWeapon = true;
            currentState        = AIState.PickupWeapon;
        }
        else
        {
            droppedWeapon       = null;
            readyToPickupWeapon = false;
            bool singlePoint    = patrolPoints != null && patrolPoints.Length == 1;
            currentState        = isAggro ? AIState.Chase : (singlePoint ? AIState.Hold : AIState.Patrol);
        }
    }

    private void ExecuteAttack()
    {
        if (heldWeapon == null || Time.time < nextAiAttackTime) return;

        Vector2 aimDir = ((Vector2)playerTransform.position - (Vector2)heldWeapon.transform.position).normalized;
        bool attacked  = false;

        switch (heldWeapon.type)
        {
            case WeaponType.OneHandFirearm: attacked = heldWeapon.TryShoot(aimDir);       break;
            case WeaponType.TwoHandFirearm: attacked = heldWeapon.TryShoot(aimDir);       break;
            case WeaponType.TwoHandMelee:   attacked = heldWeapon.TryMeleeAttack(aimDir); break;
            case WeaponType.OneHandMelee:   attacked = heldWeapon.TryMeleeAttack(aimDir); break;
        }

        if (attacked) nextAiAttackTime = Time.time + aiReactionTime;
    }

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

            case DamageType.Finisher:
                ExecuteFinisherLogic();
                break;
        }
    }

    public void TakeKnockdown(Vector2 knockbackDir)
    {
        if (isDead || currentState == AIState.Stunned) return;

        isAggro       = true;
        droppedWeapon = heldWeapon;
        DropWeapon();

        currentState = AIState.Stunned;
        stunTimer    = stunDuration;

        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.AddForce(knockbackDir * stunKnockbackForce, ForceMode2D.Impulse);

        transform.rotation = Quaternion.Euler(0f, 0f, stunnedZRotation);
    }

    private void OnHitByThrownWeapon()
    {
        droppedWeapon = heldWeapon;
        DropWeapon();

        currentState = AIState.Stunned;
        stunTimer    = stunDuration;

        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector2 knockbackDir = (transform.position - playerTransform.position).normalized;
        rb.AddForce(knockbackDir * stunKnockbackForce, ForceMode2D.Impulse);

        transform.rotation = Quaternion.Euler(0f, 0f, stunnedZRotation);
    }

    private void ExecuteFinisherLogic()
    {
        Debug.Log($"{name} was executed!");
        Die();
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

    private void UpdateAnimations()
    {
        if (animator == null) return;

        if (currentState == AIState.Stunned)
        {
            PlayAnimation(onGroundAnimation);
            return;
        }

        bool isMoving = rb.linearVelocity.sqrMagnitude > movingVelocityThreshold * movingVelocityThreshold;
        WeaponHoldCategory weaponCategory = GetHeldWeaponCategory();

        string targetAnimation = weaponCategory switch
        {
            WeaponHoldCategory.None    => isMoving ? walkAnimation        : idleAnimation,
            WeaponHoldCategory.OneHand => isMoving ? oneHandWalkAnimation : oneHandIdleAnimation,
            WeaponHoldCategory.TwoHand => isMoving ? twoHandWalkAnimation : twoHandIdleAnimation,
            _                          => idleAnimation
        };

        PlayAnimation(targetAnimation);
    }

    private WeaponHoldCategory GetHeldWeaponCategory()
    {
        if (heldWeapon == null) return WeaponHoldCategory.None;

        if (heldWeapon.type == WeaponType.OneHandFirearm || heldWeapon.type == WeaponType.OneHandMelee)
            return WeaponHoldCategory.OneHand;

        return WeaponHoldCategory.TwoHand;
    }

    private void PlayAnimation(string animationName)
    {
        if (string.IsNullOrEmpty(animationName) || animationName == currentAnimation) return;
        currentAnimation = animationName;
        animator.Play(animationName);
    }

    private void MoveTowards(Vector2 target, float speed)
    {
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

        // Raio de busca de armas (verde)
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, weaponSearchRadius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Vector3 leftRay  = Quaternion.Euler(0, 0,  viewAngle / 2f) * transform.up;
        Vector3 rightRay = Quaternion.Euler(0, 0, -viewAngle / 2f) * transform.up;
        Gizmos.DrawRay(transform.position, leftRay  * detectionRadius);
        Gizmos.DrawRay(transform.position, rightRay * detectionRadius);
    }
}