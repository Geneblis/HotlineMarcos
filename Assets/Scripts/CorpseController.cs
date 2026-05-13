using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CorpseController : MonoBehaviour
{
    [Header("Initial Impulse")]
    [Tooltip("Force applied in the opposite direction of the shot. 2-4 gives a convincing effect.")]
    [SerializeField] private float impulseForce = 3f;

    [Tooltip("Torque applied when thrown (slight spin). 0 = no spin.")]
    [SerializeField] private float impulseTorque = 30f;

    [Header("Corpse Stopping")]
    [Tooltip("Linear velocity below which the body is permanently locked.")]
    [SerializeField] private float stopVelocityThreshold = 0.05f;

    [Tooltip("How long (seconds) the body can move before being forced to stop.")]
    [SerializeField] private float maxSlideTime = 1.2f;

    [Tooltip("Linear damping applied to the Rigidbody2D to slow down naturally.")]
    [SerializeField] private float linearDrag = 6f;

    [Header("Visuals")]
    [Tooltip("Extra Z angle added to the enemy's rotation upon death. 90° = body lying on its side (Hotline Miami style).")]
    [SerializeField] private float extraDeathTiltDegrees = 90f;

    [Header("Blood Puddle")]
    [Tooltip("Sprite used for the blood puddle.")]
    [SerializeField] private Sprite bloodPuddleSprite;

    [Tooltip("How much below the corpse the blood should render.")]
    [SerializeField] private int bloodSortingOrderOffset = -1;

    [Tooltip("How long the puddle takes to grow.")]
    [SerializeField] private float bloodGrowDuration = 0.35f;

    [Tooltip("Final local scale multiplier for the blood puddle.")]
    [SerializeField] private float bloodFinalScale = 1.4f;

    [Tooltip("How many degrees the blood puddle rotates while growing.")]
    [SerializeField] private float bloodRotateDegrees = 25f;

    [Tooltip("Final alpha for the blood puddle.")]
    [Range(0f, 1f)]
    [SerializeField] private float bloodAlpha = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer corpseSpriteRenderer;
    private bool isFrozen;
    private float slideTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        corpseSpriteRenderer = GetComponent<SpriteRenderer>();

        if (corpseSpriteRenderer == null)
        {
            corpseSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        rb.gravityScale = 0f;
        rb.linearDamping = linearDrag;
        rb.angularDamping = 8f;
        rb.constraints = RigidbodyConstraints2D.None;
    }

    private void Update()
    {
        if (isFrozen)
        {
            return;
        }

        slideTimer += Time.deltaTime;

        bool tooSlow = rb.linearVelocity.sqrMagnitude < stopVelocityThreshold * stopVelocityThreshold;
        bool timedOut = slideTimer >= maxSlideTime;

        if (tooSlow || timedOut)
        {
            FreezeCorpse();
        }
    }

    public void Initialize(Vector2 hitDirection)
    {
        float currentZ = transform.eulerAngles.z;
        transform.rotation = Quaternion.Euler(0f, 0f, currentZ + extraDeathTiltDegrees);

        if (hitDirection.sqrMagnitude < 0.001f)
        {
            hitDirection = Vector2.up;
        }

        rb.AddForce(hitDirection * impulseForce, ForceMode2D.Impulse);

        float torqueSign = (hitDirection.x >= 0f) ? 1f : -1f;
        rb.AddTorque(impulseTorque * torqueSign, ForceMode2D.Impulse);
    }

    private void FreezeCorpse()
    {
        if (isFrozen)
        {
            return;
        }

        isFrozen = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        CreateBloodPuddle();

        enabled = false;
    }

    private void CreateBloodPuddle()
    {
        if (bloodPuddleSprite == null)
        {
            return;
        }

        GameObject bloodObject = new GameObject("BloodPuddle");
        bloodObject.transform.position = transform.position;

        SpriteRenderer bloodSpriteRenderer = bloodObject.AddComponent<SpriteRenderer>();
        bloodSpriteRenderer.sprite = bloodPuddleSprite;

        if (corpseSpriteRenderer != null)
        {
            bloodSpriteRenderer.sortingLayerID = corpseSpriteRenderer.sortingLayerID;
            bloodSpriteRenderer.sortingOrder = corpseSpriteRenderer.sortingOrder + bloodSortingOrderOffset;
        }

        Color bloodColor = bloodSpriteRenderer.color;
        bloodColor.a = 0f;
        bloodSpriteRenderer.color = bloodColor;

        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one * bloodFinalScale;

        bloodObject.transform.localScale = startScale;

        StartCoroutine(AnimateBloodPuddle(bloodObject.transform, bloodSpriteRenderer, targetScale));
    }

    private IEnumerator AnimateBloodPuddle(Transform bloodTransform, SpriteRenderer bloodSpriteRenderer, Vector3 targetScale)
    {
        float elapsedTime = 0f;

        float startRotation = Random.Range(0f, 360f);
        float endRotation = startRotation + bloodRotateDegrees;

        Vector3 startScale = Vector3.zero;
        Quaternion startQuaternion = Quaternion.Euler(0f, 0f, startRotation);
        Quaternion endQuaternion = Quaternion.Euler(0f, 0f, endRotation);

        while (elapsedTime < bloodGrowDuration)
        {
            float progress = elapsedTime / bloodGrowDuration;

            bloodTransform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            bloodTransform.rotation = Quaternion.Lerp(startQuaternion, endQuaternion, progress);

            Color bloodColor = bloodSpriteRenderer.color;
            bloodColor.a = Mathf.Lerp(0f, bloodAlpha, progress);
            bloodSpriteRenderer.color = bloodColor;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        bloodTransform.localScale = targetScale;
        bloodTransform.rotation = endQuaternion;

        Color finalColor = bloodSpriteRenderer.color;
        finalColor.a = bloodAlpha;
        bloodSpriteRenderer.color = finalColor;
    }
}