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
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Projectile colidiu com: {other.name} | Layer: {other.gameObject.layer} ({LayerMask.LayerToName(other.gameObject.layer)})");

        // Ignora colisão com outros projéteis
        if (other.GetComponent<Projectile>() != null)
            return;

        // Verifica se bateu no layer "Map"
        if (other.gameObject.layer == LayerMask.NameToLayer("Map"))
        {
            Debug.Log("Deletando projétil - bateu no Map!");
            Destroy(gameObject);
            return;
        }

        // Só reage a inimigos e ao player — ignora tudo o mais (paredes, props, etc.)
        // Remova o check de "Player" se não quiser que balas do player acertem ele mesmo
        bool isValidTarget = other.CompareTag("Enemy") || other.CompareTag("Player");
        if (!isValidTarget) return;

        // other.GetComponent<IDamageable>()?.TakeDamage(damage);
        Debug.Log($"Projectile hit: {other.name}");

        Destroy(gameObject);
    }
}