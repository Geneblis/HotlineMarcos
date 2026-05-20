using System.Collections;
using UnityEngine;

public class PlayerBodyAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator upperAnimator;
    public PlayerWeaponController weaponController;

    [Header("Punch")]
    [SerializeField] private AudioClip punchSound;
    [SerializeField][Range(0f, 1f)] private float punchVolume = 1f;

    const string IDLE      = "PlayerBodyIdle";
    const string ONE_HAND  = "PlayerBody1Hand";
    const string TWO_HAND  = "PlayerBody2Hand";
    const string EXECUTION = "PlayerBody2Execution";

    public static bool IsExecuting { get; private set; }

    int  currentStateHash;
    bool isPunching;

    void Awake()
    {
        if (weaponController == null)
            weaponController = FindFirstObjectByType<PlayerWeaponController>();
        if (upperAnimator == null)
            Debug.LogError("PlayerBodyAnimationController: campo 'Upper Animator' não atribuído no Inspector!", this);
        if (weaponController == null)
            Debug.LogError("PlayerBodyAnimationController: PlayerWeaponController não encontrado!", this);
    }

    void Update()
    {
        if (upperAnimator == null || weaponController == null) return;
        if (IsExecuting) return;

        IWeapon equippedWeapon = weaponController.GetEquippedWeapon();

        if (equippedWeapon == null)
        {
            if (Input.GetMouseButtonDown(0) && !isPunching)
            {
                TriggerPunch();
                return;
            }

            if (!isPunching)
                ChangeState(IDLE);

            return;
        }

        if (equippedWeapon.IsOfType(WeaponType.OneHandFirearm) || equippedWeapon.IsOfType(WeaponType.OneHandMelee))
        {
            ChangeState(ONE_HAND);
            return;
        }

        if (equippedWeapon.IsOfType(WeaponType.TwoHandFirearm) || equippedWeapon.IsOfType(WeaponType.TwoHandMelee))
        {
            ChangeState(TWO_HAND);
            return;
        }

        ChangeState(IDLE);
    }

    public void TriggerExecution()
    {
        int execHash = Animator.StringToHash(EXECUTION);

        if (!upperAnimator.HasState(0, execHash))
        {
            Debug.LogWarning("PlayerBodyAnimationController: estado 'PlayerBody2Execution' não encontrado no Animator.", this);
            return;
        }

        StopAllCoroutines();
        isPunching = false;
        upperAnimator.Play(execHash, 0, 0f);
        currentStateHash = execHash;
        StartCoroutine(ResetAfterExecution());
    }

    IEnumerator ResetAfterExecution()
    {
        IsExecuting = true;

        yield return null;

        AnimatorStateInfo stateInfo = upperAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);

        IsExecuting = false;
        ChangeState(IDLE);
    }

    void TriggerPunch()
    {
        int punchHash = Animator.StringToHash(ONE_HAND);

        if (!upperAnimator.HasState(0, punchHash))
        {
            Debug.LogWarning("PlayerBodyAnimationController: estado 'PlayerBody1Hand' não encontrado no Animator.", this);
            return;
        }

        upperAnimator.Play(punchHash, 0, 0f);
        currentStateHash = punchHash;

        if (punchSound != null)
            AudioSource.PlayClipAtPoint(punchSound, transform.position, punchVolume);

        StopAllCoroutines();
        StartCoroutine(ResetAfterPunch());
    }

    IEnumerator ResetAfterPunch()
    {
        isPunching = true;

        yield return null;

        AnimatorStateInfo stateInfo = upperAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);

        isPunching = false;
    }

    void ChangeState(string newState)
    {
        int newStateHash = Animator.StringToHash(newState);
        if (currentStateHash == newStateHash) return;

        if (!upperAnimator.HasState(0, newStateHash))
        {
            Debug.LogWarning($"PlayerBodyAnimationController: estado '{newState}' não existe na layer 0.", this);
            return;
        }

        upperAnimator.Play(newStateHash, 0, 0f);
        currentStateHash = newStateHash;
    }
}