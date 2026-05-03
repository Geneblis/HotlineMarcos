using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Configurações Base")]
    [SerializeField] private float health = 1f;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float aiReactionTime = 0.5f; 
    private float nextAiAttackTime;
    
    [Header("Detecção e Combate")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float attackRadius = 2f;
    
    [Header("Referências")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private IWeapon heldWeapon; // Agora puxa direto o seu script IWeapon!
    [SerializeField] private Transform holdPoint; // Um ponto vazio dentro do inimigo onde a arma deve ficar

    private bool isDead = false;

    void Start()
    {
        gameObject.tag = "Enemy";

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // SE O INIMIGO COMEÇAR COM UMA ARMA, ELE "PEGA" ELA
        if (heldWeapon != null)
        {
            // O inimigo precisa ter um Collider2D para passar aqui e não atirar no próprio pé
            Collider2D enemyCollider = GetComponent<Collider2D>(); 
            
            // Se não tiver holdPoint, a arma fica no centro do inimigo mesmo
            Transform pointToHold = holdPoint != null ? holdPoint : transform; 
            
            heldWeapon.OnPickup(pointToHold, enemyCollider);
        }
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // 1. AJUSTE DE RANGE DINÂMICO
        float currentAttackRange = attackRadius; 
        
        // Se a arma for Melee, o range de ataque do inimigo vira o range da arma!
        if (heldWeapon != null && heldWeapon.type == WeaponType.Melee)
        {
            currentAttackRange = heldWeapon.meleeRange;
        }

        if (distanceToPlayer <= currentAttackRange)
        {
            Attack();
            LookAtPlayer();

            // Se for corpo a corpo, é importante que ele continue andando na sua 
            // direção enquanto bate, pra não errar o golpe se você der um passinho pra trás.
            if (heldWeapon != null && heldWeapon.type == WeaponType.Melee)
            {
                transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
            }
        }
        else if (distanceToPlayer <= detectionRadius)
        {
            Chase();
        }
    }

    private void Chase()
    {
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
        LookAtPlayer();
    }

    private void LookAtPlayer()
    {
        Vector2 direction = playerTransform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle - 90f)); 
    }

    private void Attack()
    {
        // 2. CONTROLE DE MULTIPLOS HITS
        // Se a arma sumiu ou se o inimigo ainda tá se "recuperando" do último ataque, ele não faz nada
        if (heldWeapon == null || Time.time < nextAiAttackTime) return;

        Vector2 aimDirection = (playerTransform.position - heldWeapon.transform.position).normalized;
        bool didAttack = false;

        if (heldWeapon.type == WeaponType.Firearm)
        {
            didAttack = heldWeapon.TryShoot(aimDirection);
        }
        else if (heldWeapon.type == WeaponType.Melee)
        {
            didAttack = heldWeapon.TryMeleeAttack(aimDirection);
        }

        // Se o ataque funcionou de fato, ele reseta o cronômetro pra bater/atirar de novo
        if (didAttack)
        {
            nextAiAttackTime = Time.time + aiReactionTime;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;
        if (health <= 0)
        {
            Die();
        }
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
            // Em vez de fazermos a lógica do zero, usamos o seu método OnThrow!
            // Vamos jogar a arma numa direção um pouco aleatória pra dar um efeito legal de morte
            Vector2 randomDropDirection = Random.insideUnitCircle.normalized;
            
            // Força reduzida para não voar longe demais (Multipliquei por 0.2f, ajuste se precisar)
            heldWeapon.OnThrow(randomDropDirection * 0.2f); 

            heldWeapon = null;
        }
    }
}