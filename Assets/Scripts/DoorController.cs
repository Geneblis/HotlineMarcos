using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour
{
    [Header("Física da Porta")]
    [SerializeField] private float doorPushForce   = 10f;
    [SerializeField] private float bulletPushForce = 20f;

    [Header("Detecção de Inimigos")]
    [SerializeField] private float knockCheckRadius   = 0.8f;
    [SerializeField] private float knockCheckDistance = 0.6f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool isPlayer = collision.gameObject.CompareTag("Player");
        bool isEnemy  = collision.gameObject.CompareTag("Enemy");

        if (!isPlayer && !isEnemy) return;

        Vector2 pushDir = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
        PushDoor(pushDir, doorPushForce);

        if (isPlayer)
            KnockEnemiesOnOtherSide(pushDir);
    }

    public void OnHitByBullet(Vector2 bulletDirection)
    {
        PushDoor(bulletDirection, bulletPushForce);
        KnockEnemiesOnOtherSide(bulletDirection);
    }

    private void PushDoor(Vector2 direction, float force)
    {
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    private void KnockEnemiesOnOtherSide(Vector2 pushDir)
    {
        Vector2 checkCenter = (Vector2)transform.position + pushDir * knockCheckDistance;
        Collider2D[] hits   = Physics2D.OverlapCircleAll(checkCenter, knockCheckRadius);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            hit.GetComponent<EnemyAI>()?.TakeKnockdown(pushDir);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Vector2 previewCenter = (Vector2)transform.position + Vector2.right * knockCheckDistance;
        Gizmos.DrawWireSphere(previewCenter, knockCheckRadius);
    }
}