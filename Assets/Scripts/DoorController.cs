using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform doorPivot;

    [Header("Configurações")]
    [SerializeField] private float openSpeed     = 300f;
    [SerializeField] private float autoCloseTime = 15f;
    [SerializeField] private float openAngle     = 90f;

    [Header("Detecção de Inimigos")]
    [SerializeField] private float knockCheckRadius   = 0.8f;
    [SerializeField] private float knockCheckDistance = 0.6f;

    private Rigidbody2D rb;
    private bool       isMoving;
    private Quaternion closedRotation;
    private Vector3    closedPosition;
    private Coroutine  openCoroutine;
    private Coroutine  autoCloseCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        closedRotation = transform.rotation;
        closedPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool isPlayer = collision.gameObject.CompareTag("Player");
        bool isEnemy  = collision.gameObject.CompareTag("Enemy");

        if (!isPlayer && !isEnemy) return;

        // Inimigo stunnado no chão é ignorado pela porta
        if (isEnemy && IsStunned(collision.gameObject)) return;

        Vector2 pushDir = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;

        if (isPlayer)
            KnockEnemiesOnOtherSide(pushDir);

        TriggerOpen(pushDir);
    }

    public void OnHitByBullet(Vector2 bulletDirection)
    {
        KnockEnemiesOnOtherSide(bulletDirection);
        TriggerOpen(bulletDirection);
    }

    private void TriggerOpen(Vector2 pushDir)
    {
        float cross       = Vector3.Cross(transform.up, (Vector3)pushDir).z;
        float rotationDir = cross >= 0f ? 1f : -1f;

        Quaternion targetRotation = closedRotation * Quaternion.Euler(0f, 0f, rotationDir * openAngle);
        float diff = Mathf.DeltaAngle(transform.eulerAngles.z, targetRotation.eulerAngles.z);

        if (Mathf.Abs(diff) < 1f)
        {
            RestartAutoClose();
            return;
        }

        if (openCoroutine      != null) StopCoroutine(openCoroutine);
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        openCoroutine = StartCoroutine(RotateDoor(diff, opening: true));
    }

    private IEnumerator RotateDoor(float totalAngle, bool opening)
    {
        isMoving = true;

        float remaining = Mathf.Abs(totalAngle);
        float sign      = Mathf.Sign(totalAngle);

        while (remaining > 0f)
        {
            float step = Mathf.Min(openSpeed * Time.deltaTime, remaining);
            transform.RotateAround(doorPivot.position, Vector3.forward, sign * step);
            remaining -= step;
            yield return null;
        }

        isMoving = false;

        if (opening)
            RestartAutoClose();
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseTime);

        float diff = Mathf.DeltaAngle(transform.eulerAngles.z, closedRotation.eulerAngles.z);
        yield return StartCoroutine(RotateDoor(diff, opening: false));

        transform.SetPositionAndRotation(closedPosition, closedRotation);
    }

    private void RestartAutoClose()
    {
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = StartCoroutine(AutoClose());
    }

    private void KnockEnemiesOnOtherSide(Vector2 pushDir)
    {
        Vector2      checkCenter = (Vector2)transform.position + pushDir * knockCheckDistance;
        Collider2D[] hits        = Physics2D.OverlapCircleAll(checkCenter, knockCheckRadius);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (IsStunned(hit.gameObject)) continue;
            hit.GetComponent<EnemyAI>()?.TakeKnockdown(pushDir);
        }
    }

    private bool IsStunned(GameObject go)
    {
        EnemyAI enemy = go.GetComponent<EnemyAI>();
        return enemy != null && enemy.currentState == EnemyAI.AIState.Stunned;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Vector2 previewCenter = (Vector2)transform.position + Vector2.right * knockCheckDistance;
        Gizmos.DrawWireSphere(previewCenter, knockCheckRadius);
    }
}