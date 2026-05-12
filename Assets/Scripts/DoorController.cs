using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform doorPivot;

    [Header("Settings")]
    [SerializeField] private float openSpeed = 300f;
    [SerializeField] private float autoCloseTime = 15f;
    [SerializeField] private float openAngle = 90f;

    [Header("Enemy Detection")]
    [SerializeField] private float knockCheckRadius = 0.8f;
    [SerializeField] private float knockCheckDistance = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioClip interactionSound;
    [SerializeField] private float soundVolume = 1f;

    private Rigidbody2D rb;
    private Quaternion closedRotation;
    private Vector3 closedPosition;
    private Coroutine openCoroutine;
    private Coroutine autoCloseCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        closedRotation = transform.rotation;
        closedPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool isPlayer = collision.gameObject.CompareTag("Player");
        bool isEnemy = collision.gameObject.CompareTag("Enemy");

        if (!isPlayer && !isEnemy) return;

        // Stunned enemy on the ground is ignored by the door
        if (isEnemy && IsStunned(collision.gameObject)) return;

        Vector2 pushDirection = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;

        if (isPlayer)
            KnockEnemiesOnOtherSide(pushDirection);

        TriggerOpen(pushDirection);
    }

    public void OnHitByBullet(Vector2 bulletDirection)
    {
        KnockEnemiesOnOtherSide(bulletDirection);
        TriggerOpen(bulletDirection);
    }

    private void TriggerOpen(Vector2 pushDirection)
    {
        float cross = Vector3.Cross(transform.up, (Vector3)pushDirection).z;
        float rotationDirection = cross >= 0f ? 1f : -1f;

        Quaternion targetRotation = closedRotation * Quaternion.Euler(0f, 0f, rotationDirection * openAngle);
        float difference = Mathf.DeltaAngle(transform.eulerAngles.z, targetRotation.eulerAngles.z);

        if (Mathf.Abs(difference) < 1f)
        {
            RestartAutoClose();
            return;
        }

        if (openCoroutine != null) StopCoroutine(openCoroutine);
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        openCoroutine = StartCoroutine(RotateDoor(difference, opening: true));
    }

    private IEnumerator RotateDoor(float totalAngle, bool opening)
    {
        float remainingAngle = Mathf.Abs(totalAngle);
        float sign = Mathf.Sign(totalAngle);
        bool hasRotated = false;

        while (remainingAngle > 0f)
        {
            float step = Mathf.Min(openSpeed * Time.deltaTime, remainingAngle);
            transform.RotateAround(doorPivot.position, Vector3.forward, sign * step);
            remainingAngle -= step;

            if (!hasRotated && step > 0f)
            {
                PlayInteractionSound();
                hasRotated = true;
            }

            yield return null;
        }

        if (opening)
            RestartAutoClose();
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseTime);

        float difference = Mathf.DeltaAngle(transform.eulerAngles.z, closedRotation.eulerAngles.z);

        if (Mathf.Abs(difference) < 1f)
            yield break;

        yield return StartCoroutine(RotateDoor(difference, opening: false));
        transform.SetPositionAndRotation(closedPosition, closedRotation);
    }

    private void RestartAutoClose()
    {
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = StartCoroutine(AutoClose());
    }

    private void KnockEnemiesOnOtherSide(Vector2 pushDirection)
    {
        Vector2 checkCenter = (Vector2)transform.position + pushDirection * knockCheckDistance;
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkCenter, knockCheckRadius);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (IsStunned(hit.gameObject)) continue;
            hit.GetComponent<EnemyAI>()?.TakeKnockdown(pushDirection);
        }
    }

    private bool IsStunned(GameObject gameObjectReference)
    {
        EnemyAI enemy = gameObjectReference.GetComponent<EnemyAI>();
        return enemy != null && enemy.currentState == EnemyAI.AIState.Stunned;
    }

    private void PlayInteractionSound()
    {
        if (interactionSound == null) return;
        AudioSource.PlayClipAtPoint(interactionSound, transform.position, soundVolume);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Vector2 previewCenter = (Vector2)transform.position + Vector2.right * knockCheckDistance;
        Gizmos.DrawWireSphere(previewCenter, knockCheckRadius);
    }
}