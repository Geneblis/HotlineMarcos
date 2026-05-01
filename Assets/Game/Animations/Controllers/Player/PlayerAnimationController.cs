using UnityEngine;

// Coloca esse script na raiz do Player, junto com o PlayerController
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator lowerAnimator;   // arrasta o Animator do LowerBody aqui

    Rigidbody2D rb;

    // Nomes dos estados no Animator (devem bater exatamente com os do Animator)
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
        // Evita chamar Play toda frame se o estado não mudou
        if (currentState == newState) return;

        lowerAnimator.Play(newState);
        currentState = newState;
    }
}