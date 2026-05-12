using UnityEngine;


//NOTE:
// O projetil precisa de funcionalidade para IGNORAR (passar direto por) um inimigo que esteja derrubado.
// Isso é necessário para que o jogador possa atirar por baixo de um inimigo caído, sem que a bala seja bloqueada por ele.

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

    public void Launch(Vector2 direction, Collider2D weaponCol = null)
    {
        if (weaponCol != null)
            Physics2D.IgnoreCollision(col, weaponCol, true);

        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Projectile>() != null) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Map"))
        {
            other.GetComponent<DoorController>()?.OnHitByBullet(rb.linearVelocity.normalized);
            Destroy(gameObject);
            return;
        }

        bool isValidTarget = other.CompareTag("Enemy") || other.CompareTag("Player");
        if (!isValidTarget) return;

        other.GetComponent<IDamageable>()?.TakeDamage(damage, DamageType.Bullet);
        Destroy(gameObject);
    }
}
