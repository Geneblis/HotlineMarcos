using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed    = 20f;
    public float lifetime = 3f;
    public int   damage   = 100;

    Rigidbody2D rb;
    Collider2D  col;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    // weaponCol = collider da arma que disparou; projétil ignora ela
    public void Launch(Vector2 direction, Collider2D weaponCol = null)
    {
        if (weaponCol != null)
            Physics2D.IgnoreCollision(col, weaponCol, true);

        rb.linearVelocity = direction.normalized * speed;

        // Rotaciona o sprite do projétil na direção do disparo
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignora outros projéteis
        if (other.GetComponent<Projectile>() != null) return;

        // Destrói ao bater no mapa
        if (other.gameObject.layer == LayerMask.NameToLayer("Map"))
        {
            Destroy(gameObject);
            return;
        }

        // Só processa dano em alvos válidos
        bool isValidTarget = other.CompareTag("Enemy") || other.CompareTag("Player");
        if (!isValidTarget) return;

        other.GetComponent<IDamageable>()?.TakeDamage(damage, DamageType.Bullet);

        Destroy(gameObject);
    }
}