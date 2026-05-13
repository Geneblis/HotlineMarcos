using UnityEngine;

public class PlayerBodyAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator upperAnimator;
    public PlayerWeaponController weaponController;

    const string IDLE = "PlayerBodyIdle";
    const string ONE_HAND = "PlayerBody1Hand";
    const string TWO_HAND = "PlayerBody2Hand";

    int currentStateHash;

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
        if (upperAnimator == null || weaponController == null)
            return;

        IWeapon equippedWeapon = weaponController.GetEquippedWeapon();

        if (equippedWeapon == null)
        {
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

    void ChangeState(string newState)
    {
        int newStateHash = Animator.StringToHash(newState);

        if (currentStateHash == newStateHash)
            return;

        if (!upperAnimator.HasState(0, newStateHash))
        {
            Debug.LogWarning($"PlayerBodyAnimationController: o estado '{newState}' não existe na layer 0 do Animator.", this);
            return;
        }

        upperAnimator.Play(newStateHash, 0, 0f);
        currentStateHash = newStateHash;
    }
}