using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    public enum AIState
    {
        Patrol,
        Hold,
        Chase,
        Search,
        Combat,
        PickupWeapon,
        Stunned,
        Random
    }

    private enum WeaponHoldCategory
    {
        None,
        OneHand,
        TwoHand
    }

    [Header("Current State")]
    public AIState currentState = AIState.Patrol;

    [Header("Base Settings")]
    [SerializeField] private float health = 1f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Vision Module")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField][Range(0, 360)] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Combat Module")]
    [SerializeField] private float attackRadius = 8f;
    [SerializeField] private float aiReactionTime = 0.5f;

    [Header("Patrol Module")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 1f;

    [Header("Search Module")]
    [SerializeField] private float searchDuration = 4f;
    [SerializeField] private float searchRotationSpeed = 90f;

    [Header("Weapon Module")]
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float stunKnockbackForce = 1.5f;
    [SerializeField] private float stunnedZRotation = 90f;

    [Header("Weapon Search Module")]
    [SerializeField] private float weaponSearchRadius = 20f;
    [SerializeField] private float weaponSearchInterval = 2f;

    [Header("Animation Module")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleAnimation = "Idle";
    [SerializeField] private string walkAnimation = "Walk";
    [SerializeField] private string oneHandIdleAnimation = "OneHand_Idle";
    [SerializeField] private string oneHandWalkAnimation = "OneHand_Walk";
    [SerializeField] private string twoHandIdleAnimation = "TwoHand_Idle";
    [SerializeField] private string twoHandWalkAnimation = "TwoHand_Walk";
    [SerializeField] private string onGroundAnimation = "OnGround";
    [SerializeField] private float movingVelocityThreshold = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioClip groundSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float audioVolume = 1f;

    [Header("Random State")]
    [SerializeField] private float randomWanderRadius = 8f;
    [SerializeField] private float randomArriveDistance = 0.35f;
    [SerializeField] private float randomWaitTime = 0.75f;

    [Header("References")]
    [SerializeField] private Transform playerTransform;

    private Rigidbody2D rb;
    private Collider2D enemyCollider;

    private IWeapon heldWeapon;
    private IWeapon droppedWeapon;
    private bool readyToPickupWeapon;

    private bool isDead;
    private bool isAggro;

    private int currentPatrolIndex;
    private float waitTimer;
    private float searchTimer;
    private float stunTimer;
    private float weaponSearchTimer;
    private float nextAiAttackTime;

    private Vector2 lastKnownPlayerPosition;
    private Vector2 randomWanderOrigin;
    private Vector2 randomTargetPosition;
    private bool hasRandomTarget;
    private float randomWaitTimer;

    private string currentAnimation;
    private AIState preferredIdleState = AIState.Patrol;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();
        randomWanderOrigin = transform.position;
        weaponSearchTimer = 0f;
        gameObject.tag = "Enemy";

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        SpawnWeapon();

        if (currentState == AIState.Random)
        {
            preferredIdleState = AIState.Random;
            BeginRandomWander();
        }
        else
        {
            SetInitialState();
        }
    }

    void Update()
    {
        if (isDead)
        {
            return;
        }

        if (currentState == AIState.Stunned)
        {
            StunnedBehavior();
            UpdateAnimations();
            return;
        }

        if (playerTransform != null)
        {
            UpdateVisionModule();
        }

        UpdateWeaponSearchModule();
        ExecuteCurrentState();
        UpdateAnimations();
    }

    private void SetInitialState()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            preferredIdleState = AIState.Random;
            currentState = AIState.Random;
            BeginRandomWander();
            return;
        }

        if (patrolPoints.Length == 1)
        {
            preferredIdleState = AIState.Hold;
            currentState = AIState.Hold;
            return;
        }

        preferredIdleState = AIState.Patrol;
        currentState = AIState.Patrol;
        currentPatrolIndex = 0;
        waitTimer = patrolWaitTime;
    }

    private void UpdateWeaponSearchModule()
    {
        if (heldWeapon != null)
        {
            return;
        }

        if (currentState == AIState.PickupWeapon || currentState == AIState.Stunned)
        {
            return;
        }

        weaponSearchTimer -= Time.deltaTime;
        if (weaponSearchTimer > 0f)
        {
            return;
        }

        weaponSearchTimer = weaponSearchInterval;

        IWeapon nearestWeapon = FindNearestAvailableWeapon();
        if (nearestWeapon == null)
        {
            return;
        }

        droppedWeapon = nearestWeapon;
        readyToPickupWeapon = true;
        currentState = AIState.PickupWeapon;
    }

    private IWeapon FindNearestAvailableWeapon()
    {
        MonoBehaviour[] allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        IWeapon nearestWeapon = null;
        float nearestDistance = weaponSearchRadius;

        foreach (MonoBehaviour behaviour in allBehaviours)
        {
            if (behaviour is not IWeapon weapon)
            {
                continue;
            }

            if (weapon.IsHeld())
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, weapon.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestWeapon = weapon;
            }
        }

        return nearestWeapon;
    }

    private void SpawnWeapon()
    {
        if (weaponPrefab == null)
        {
            return;
        }

        Transform spawnPoint = holdPoint != null ? holdPoint : transform;
        GameObject weaponObject = Instantiate(weaponPrefab, spawnPoint.position, Quaternion.identity);
        heldWeapon = weaponObject.GetComponent<IWeapon>();

        if (heldWeapon != null)
        {
            heldWeapon.OnPickup(spawnPoint, enemyCollider);
            return;
        }

        Debug.LogWarning($"[EnemyAI] {name}: prefab '{weaponPrefab.name}' has no IWeapon component!");
        Destroy(weaponObject);
    }

    private void UpdateVisionModule()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        bool hasLineOfSight = CheckLineOfSight(distanceToPlayer);
        bool inFieldOfView = isAggro || IsPlayerInCone();
        bool playerVisible = hasLineOfSight && distanceToPlayer <= detectionRadius && inFieldOfView;

        if (playerVisible)
        {
            isAggro = true;
            lastKnownPlayerPosition = playerTransform.position;

            float attackRange = GetAttackRange();
            ChangeState(distanceToPlayer <= attackRange ? AIState.Combat : AIState.Chase);
            return;
        }

        if (isAggro)
        {
            if (currentState == AIState.Combat || currentState == AIState.Chase)
            {
                ChangeState(AIState.Search);
            }

            return;
        }

        if (currentState != AIState.PickupWeapon && currentState != AIState.Random)
        {
            ChangeState(GetIdleState());
        }
    }

    private bool CheckLineOfSight(float distance)
    {
        Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);
        return hit.collider == null;
    }

    private bool IsPlayerInCone()
    {
        if (playerTransform == null)
        {
            return false;
        }

        Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        float angle = Vector2.Angle(transform.up, direction);
        return angle < viewAngle / 2f;
    }

    private float GetAttackRange()
    {
        if (heldWeapon != null && IsMeleeWeapon(heldWeapon.type))
        {
            return heldWeapon.meleeRange;
        }

        return attackRadius;
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                PatrolBehavior();
                break;
            case AIState.Hold:
                HoldBehavior();
                break;
            case AIState.Chase:
                ChaseBehavior();
                break;
            case AIState.Search:
                SearchBehavior();
                break;
            case AIState.Combat:
                CombatBehavior();
                break;
            case AIState.PickupWeapon:
                PickupWeaponBehavior();
                break;
            case AIState.Random:
                RandomBehavior();
                break;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState)
        {
            if (newState == AIState.Random && !hasRandomTarget)
            {
                BeginRandomWander();
            }

            return;
        }

        if (currentState == AIState.Stunned)
        {
            if (newState != AIState.PickupWeapon &&
                newState != AIState.Patrol &&
                newState != AIState.Hold &&
                newState != AIState.Random &&
                newState != AIState.Search)
            {
                return;
            }
        }

        if (readyToPickupWeapon)
        {
            if (droppedWeapon == null || droppedWeapon.IsHeld())
            {
                readyToPickupWeapon = false;
                droppedWeapon = null;
            }
            else
            {
                bool isPassiveState = newState == AIState.Patrol ||
                                      newState == AIState.Hold ||
                                      newState == AIState.Search ||
                                      newState == AIState.Random;

                bool isUnarmedAndAggressive = heldWeapon == null &&
                                              (newState == AIState.Chase || newState == AIState.Combat);

                if (isPassiveState || isUnarmedAndAggressive)
                {
                    currentState = AIState.PickupWeapon;
                    return;
                }
            }
        }

        currentState = newState;

        if (newState == AIState.Search)
        {
            searchTimer = searchDuration;
        }

        if (newState == AIState.Random)
        {
            preferredIdleState = AIState.Random;
            BeginRandomWander();
        }

        if (newState == AIState.Patrol)
        {
            preferredIdleState = AIState.Patrol;
        }

        if (newState == AIState.Hold)
        {
            preferredIdleState = AIState.Hold;
        }
    }

    private AIState GetIdleState()
    {
        if (preferredIdleState == AIState.Random)
        {
            return AIState.Random;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return AIState.Random;
        }

        if (patrolPoints.Length == 1)
        {
            return AIState.Hold;
        }

        return AIState.Patrol;
    }

    private void PatrolBehavior()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            ChangeState(GetIdleState());
            return;
        }

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        MoveTowards(targetPoint.position, patrolSpeed);
        LookAtTarget(targetPoint.position);

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.2f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                waitTimer = patrolWaitTime;
            }
        }
    }

    private void HoldBehavior()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            MoveTowards(patrolPoints[0].position, patrolSpeed);
        }

        transform.Rotate(0f, 0f, searchRotationSpeed * 0.25f * Time.deltaTime);
    }

    private void ChaseBehavior()
    {
        if (playerTransform == null)
        {
            ChangeState(GetIdleState());
            return;
        }

        Vector2 targetPosition = playerTransform.position;
        MoveTowards(targetPosition, chaseSpeed);
        LookAtTarget(targetPosition);
    }

    private void SearchBehavior()
    {
        float distanceToLastKnownPosition = Vector2.Distance(transform.position, lastKnownPlayerPosition);

        if (distanceToLastKnownPosition > 0.5f)
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
            ChangeState(GetIdleState());
        }
    }

    private void CombatBehavior()
    {
        if (playerTransform == null)
        {
            ChangeState(GetIdleState());
            return;
        }

        LookAtTarget(playerTransform.position);

        if (heldWeapon != null && IsMeleeWeapon(heldWeapon.type))
        {
            MoveTowards(playerTransform.position, chaseSpeed);
        }

        ExecuteAttack();
    }

    private void RandomBehavior()
    {
        if (randomWaitTimer > 0f)
        {
            randomWaitTimer -= Time.deltaTime;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 6f);

            if (randomWaitTimer <= 0f)
            {
                hasRandomTarget = false;
            }

            return;
        }

        if (!hasRandomTarget)
        {
            randomTargetPosition = GetRandomWanderPoint();
            hasRandomTarget = true;
        }

        float distanceToTarget = Vector2.Distance(transform.position, randomTargetPosition);

        if (distanceToTarget <= randomArriveDistance)
        {
            rb.linearVelocity = Vector2.zero;
            randomWaitTimer = randomWaitTime;
            return;
        }

        MoveTowards(randomTargetPosition, patrolSpeed);
        LookAtTarget(randomTargetPosition);
    }

    private Vector2 GetRandomWanderPoint()
    {
        Vector2 offset = Random.insideUnitCircle * randomWanderRadius;
        return randomWanderOrigin + offset;
    }

    private void BeginRandomWander()
    {
        hasRandomTarget = false;
        randomWaitTimer = 0f;
    }

    private void PickupWeaponBehavior()
    {
        if (droppedWeapon == null || droppedWeapon.IsHeld())
        {
            readyToPickupWeapon = false;
            droppedWeapon = null;
            ChangeState(isAggro ? AIState.Search : GetIdleState());
            return;
        }

        Vector2 weaponPosition = droppedWeapon.transform.position;
        MoveTowards(weaponPosition, patrolSpeed);
        LookAtTarget(weaponPosition);

        if (Vector2.Distance(transform.position, weaponPosition) < 0.5f)
        {
            heldWeapon = droppedWeapon;
            droppedWeapon = null;
            readyToPickupWeapon = false;

            Transform pickupPoint = holdPoint != null ? holdPoint : transform;
            heldWeapon.OnPickup(pickupPoint, enemyCollider);

            ChangeState(isAggro ? AIState.Search : GetIdleState());
        }
    }

    private void StunnedBehavior()
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 3f);

        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
        {
            RecoverFromStun();
        }
    }

    private void RecoverFromStun()
    {
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (droppedWeapon != null && !droppedWeapon.IsHeld())
        {
            readyToPickupWeapon = true;
            currentState = AIState.PickupWeapon;
            return;
        }

        droppedWeapon = null;
        readyToPickupWeapon = false;

        if (isAggro)
        {
            currentState = AIState.Search;
            searchTimer = searchDuration;
            return;
        }

        currentState = GetIdleState();

        if (currentState == AIState.Random)
        {
            BeginRandomWander();
        }
    }

    private void ExecuteAttack()
    {
        if (heldWeapon == null)
        {
            return;
        }

        if (Time.time < nextAiAttackTime)
        {
            return;
        }

        if (IsFirearm(heldWeapon.type) && heldWeapon.IsEmpty())
        {
            return;
        }

        Vector2 aimDirection = playerTransform != null
            ? ((Vector2)playerTransform.position - (Vector2)heldWeapon.transform.position).normalized
            : transform.up;

        bool attacked = false;

        switch (heldWeapon.type)
        {
            case WeaponType.OneHandFirearm:
            case WeaponType.TwoHandFirearm:
                attacked = heldWeapon.TryShoot(aimDirection);
                break;

            case WeaponType.OneHandMelee:
            case WeaponType.TwoHandMelee:
                attacked = heldWeapon.TryMeleeAttack(aimDirection);
                break;
        }

        if (attacked)
        {
            nextAiAttackTime = Time.time + GetWeaponAttackCooldown();
        }
    }

    private float GetWeaponAttackCooldown()
    {
        if (heldWeapon == null)
        {
            return Mathf.Max(0.01f, aiReactionTime);
        }

        if (heldWeapon.fireRate > 0f)
        {
            return Mathf.Max(0.01f, 1f / heldWeapon.fireRate);
        }

        return Mathf.Max(0.01f, aiReactionTime);
    }

    private bool IsFirearm(WeaponType weaponType)
    {
        return weaponType == WeaponType.OneHandFirearm || weaponType == WeaponType.TwoHandFirearm;
    }

    private bool IsMeleeWeapon(WeaponType weaponType)
    {
        return weaponType == WeaponType.OneHandMelee || weaponType == WeaponType.TwoHandMelee;
    }

    public void TakeDamage(float damage, DamageType damageType)
    {
        if (isDead)
        {
            return;
        }

        isAggro = true;

        switch (damageType)
        {
            case DamageType.Bullet:
            case DamageType.Melee:
                health -= damage;
                if (health <= 0f)
                {
                    Die();
                }
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
        if (isDead || currentState == AIState.Stunned)
        {
            return;
        }

        isAggro = true;
        PlayGroundSound();
        EnterStunnedState(knockbackDir);
    }

    private void OnHitByThrownWeapon()
    {
        if (isDead)
        {
            return;
        }

        PlayGroundSound();

        Vector2 knockbackDir = playerTransform != null
            ? ((Vector2)transform.position - (Vector2)playerTransform.position).normalized
            : Vector2.down;

        EnterStunnedState(knockbackDir);
    }

    private void EnterStunnedState(Vector2 knockbackDir)
    {
        if (heldWeapon != null)
        {
            droppedWeapon = heldWeapon;
            DropWeapon();
        }

        readyToPickupWeapon = droppedWeapon != null && !droppedWeapon.IsHeld();

        currentState = AIState.Stunned;
        stunTimer = stunDuration;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.AddForce(knockbackDir * stunKnockbackForce, ForceMode2D.Impulse);

        transform.rotation = Quaternion.Euler(0f, 0f, stunnedZRotation);
    }

    private void ExecuteFinisherLogic()
    {
        Die();
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        PlayDeathSound();
        DropWeapon();

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Destroy(gameObject);
    }

    private void DropWeapon()
    {
        if (heldWeapon == null)
        {
            return;
        }

        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude < 0.0001f)
        {
            randomDirection = Vector2.up;
        }

        heldWeapon.OnThrow(randomDirection.normalized * 0.2f);
        heldWeapon = null;
    }

    private void PlayGroundSound()
    {
        if (groundSound != null)
        {
            AudioSource.PlayClipAtPoint(groundSound, transform.position, audioVolume);
        }
    }

    private void PlayDeathSound()
    {
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, audioVolume);
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null)
        {
            return;
        }

        if (currentState == AIState.Stunned)
        {
            PlayAnimation(onGroundAnimation);
            return;
        }

        bool isMoving = rb.linearVelocity.sqrMagnitude > movingVelocityThreshold * movingVelocityThreshold;
        WeaponHoldCategory weaponCategory = GetHeldWeaponCategory();

        string targetAnimation = weaponCategory switch
        {
            WeaponHoldCategory.None => isMoving ? walkAnimation : idleAnimation,
            WeaponHoldCategory.OneHand => isMoving ? oneHandWalkAnimation : oneHandIdleAnimation,
            WeaponHoldCategory.TwoHand => isMoving ? twoHandWalkAnimation : twoHandIdleAnimation,
            _ => idleAnimation
        };

        PlayAnimation(targetAnimation);
    }

    private WeaponHoldCategory GetHeldWeaponCategory()
    {
        if (heldWeapon == null)
        {
            return WeaponHoldCategory.None;
        }

        if (heldWeapon.type == WeaponType.OneHandFirearm || heldWeapon.type == WeaponType.OneHandMelee)
        {
            return WeaponHoldCategory.OneHand;
        }

        return WeaponHoldCategory.TwoHand;
    }

    private void PlayAnimation(string animationName)
    {
        if (string.IsNullOrEmpty(animationName) || animationName == currentAnimation)
        {
            return;
        }

        currentAnimation = animationName;
        animator.Play(animationName);
    }

    private void MoveTowards(Vector2 target, float speed)
    {
        Vector2 direction = target - (Vector2)transform.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = direction.normalized * speed;
    }

    private void LookAtTarget(Vector2 targetPosition)
    {
        Vector2 direction = targetPosition - (Vector2)transform.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, weaponSearchRadius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, randomWanderRadius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Vector3 leftRay = Quaternion.Euler(0f, 0f, viewAngle / 2f) * transform.up;
        Vector3 rightRay = Quaternion.Euler(0f, 0f, -viewAngle / 2f) * transform.up;
        Gizmos.DrawRay(transform.position, leftRay * detectionRadius);
        Gizmos.DrawRay(transform.position, rightRay * detectionRadius);
    }
}