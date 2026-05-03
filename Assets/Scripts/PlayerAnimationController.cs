using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator lowerAnimator;

    Rigidbody2D rb;

    const string IDLE = "PlayerLegIdle";
    const string WALK = "PlayerLegWalk";

    string currentState;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (lowerAnimator == null)
            Debug.LogError("PlayerAnimator: campo 'Lower Animator' não atribuído no Inspector!", this);
    }

    void Update()
    {
        bool isMoving = rb.linearVelocity.sqrMagnitude > 0.01f;
        ChangeState(isMoving ? WALK : IDLE);
    }

    void ChangeState(string newState)
    {
        if (currentState == newState) return;

        lowerAnimator.Play(newState);
        currentState = newState;
    }
}