using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // --- MÁQUINA DE ESTADOS (MODULARIDADE) ---
    public enum AIState { Patrol, Chase, Combat }
    
    [Header("Estado Atual (Apenas Leitura)")]
    public AIState currentState = AIState.Patrol;

    [Header("Configurações Base")]
    [SerializeField] private float health = 1f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 5f;
    
    [Header("Módulo de Visão (FOV)")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] [Range(0, 360)] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleLayer; 
    
    [Header("Módulo de Combate")]
    [SerializeField] private float attackRadius = 8f; 
    [SerializeField] private float aiReactionTime = 0.5f; 
    private float nextAiAttackTime;
    private bool isAggro = false; // Define se ele já percebeu o player

    [Header("Módulo de Patrulha")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 1f;
    private int currentPatrolIndex;
    private float waitTimer;

    [Header("Referências")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private IWeapon heldWeapon; 
    [SerializeField] private Transform holdPoint;

    private bool isDead = false;

    void Start()
    {
        gameObject.tag = "Enemy";

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (heldWeapon != null)
        {
            Collider2D enemyCollider = GetComponent<Collider2D>(); 
            Transform pointToHold = holdPoint != null ? holdPoint : transform; 
            heldWeapon.OnPickup(pointToHold, enemyCollider);
        }
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        // O cérebro do inimigo é dividido em módulos. 
        // 1. Primeiro ele atualiza a visão para entender o mundo.
        UpdateVisionModule();

        // 2. Depois ele age com base no estado atual dele.
        switch (currentState)
        {
            case AIState.Patrol:
                PatrolBehavior();
                break;
            case AIState.Chase:
                ChaseBehavior();
                break;
            case AIState.Combat:
                CombatBehavior();
                break;
        }
    }

    // ==========================================
    // MÓDULO 1: VISÃO E DECISÃO
    // ==========================================
    private void UpdateVisionModule()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        
        // Dispara o "laser" para ver se tem parede no meio
        bool hasLineOfSight = CheckLineOfSight(distanceToPlayer);

        // Define o raio de ataque correto (Arma de Fogo vs Corpo a Corpo)
        float currentAttackRange = (heldWeapon != null && heldWeapon.type == WeaponType.Melee) ? heldWeapon.meleeRange : attackRadius;

        // Se tem visão limpa (sem paredes) E o player tá perto
        if (hasLineOfSight && distanceToPlayer <= detectionRadius)
        {
            // O player tá dentro do cone frontal de visão? Ou o inimigo já tava puto (aggro)?
            if (isAggro || IsPlayerInCone())
            {
                isAggro = true; // Viu o player, não tem mais volta!

                if (distanceToPlayer <= currentAttackRange)
                {
                    currentState = AIState.Combat; // Tá na distância de bater/atirar
                }
                else
                {
                    currentState = AIState.Chase; // Tá longe, vai correr atrás
                }
            }
        }
        else
        {
            // Se o player se escondeu atrás da parede ou saiu da distância
            if (isAggro)
            {
                // Se ele tava caçando/lutando, ele continua indo na direção do player pra procurar
                currentState = AIState.Chase; 
            }
            else
            {
                currentState = AIState.Patrol;
            }
        }
    }

    private bool CheckLineOfSight(float distance)
    {
        // Cria uma direção do inimigo para o player
        Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
        
        // Atira um raio. Se bater na layer "Parede", a variável 'hit.collider' não será nula.
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, distance, obstacleLayer);
        
        // Se for null, o caminho está limpo! (Retorna true)
        return hit.collider == null; 
    }

    private bool IsPlayerInCone()
    {
        Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float angleToPlayer = Vector2.Angle(transform.up, dirToPlayer); // Usa transform.up pro Top-Down
        return angleToPlayer < viewAngle / 2f;
    }

    // ==========================================
    // MÓDULO 2: COMPORTAMENTOS (ESTADOS)
    // ==========================================
    private void PatrolBehavior()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        
        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, patrolSpeed * Time.deltaTime);
        LookAtTarget(targetPoint.position);

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.2f)
        {
            waitTimer -= Time.deltaTime; 
            if (waitTimer <= 0)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                waitTimer = patrolWaitTime; 
            }
        }
    }

    private void ChaseBehavior()
    {
        // Corre na direção do player
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, chaseSpeed * Time.deltaTime);
        LookAtTarget(playerTransform.position);
    }

    private void CombatBehavior()
    {
        // No combate ele para (ou se move bem devagar se for melee) e tenta atacar
        LookAtTarget(playerTransform.position);

        if (heldWeapon != null && heldWeapon.type == WeaponType.Melee)
        {
            // Se for corpo a corpo, continua indo pra cima enquanto bate
            transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, chaseSpeed * Time.deltaTime);
        }

        ExecuteAttack();
    }

    // ==========================================
    // MÓDULO 3: AÇÕES (ATACAR, MORRER, OLHAR)
    // ==========================================
    private void ExecuteAttack()
    {
        if (heldWeapon == null || Time.time < nextAiAttackTime) return;

        Vector2 aimDirection = (playerTransform.position - heldWeapon.transform.position).normalized;
        bool didAttack = false;

        if (heldWeapon.type == WeaponType.Firearm) didAttack = heldWeapon.TryShoot(aimDirection);
        else if (heldWeapon.type == WeaponType.Melee) didAttack = heldWeapon.TryMeleeAttack(aimDirection);

        if (didAttack) nextAiAttackTime = Time.time + aiReactionTime;
    }

    private void LookAtTarget(Vector2 targetPos)
    {
        Vector2 direction = targetPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle - 90f)); 
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        isAggro = true; // Se tomou tiro pelas costas, já sabe que tá sendo atacado
        
        health -= damage;
        if (health <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        DropWeapon();
        GetComponent<Collider2D>().enabled = false; 
        this.enabled = false; 
    }

    private void DropWeapon()
    {
        if (heldWeapon != null)
        {
            Vector2 randomDropDirection = Random.insideUnitCircle.normalized;
            heldWeapon.OnThrow(randomDropDirection * 0.2f); 
            heldWeapon = null;
        }
    }
}