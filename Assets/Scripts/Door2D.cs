using UnityEngine;

public class Door : MonoBehaviour
{
    public float doorForce = 10f; // Força do empurrão
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Verifica se quem colidiu tem a tag Player ou Enemy
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            PushDoor(collision.transform.position);
            
            // Lógica extra: Se a porta estiver muito rápida, ela "atropela"
            if (rb.angularVelocity > 200f) 
            {
                // Aqui você chamaria uma função de Knockout no inimigo
                CheckKnockout(collision.gameObject);
            }
        }
    }

    void PushDoor(Vector3 pusherPosition)
    {
        // Calcula a direção baseada na posição de quem empurrou
        Vector2 direction = (transform.position - pusherPosition).normalized;
        rb.AddForce(direction * doorForce, ForceMode2D.Impulse);
    }

    void CheckKnockout(GameObject target)
    {
        if (target.CompareTag("Enemy"))
        {
            Debug.Log("Inimigo nocauteado pela porta!");
            // Adicione aqui sua lógica de derrubar o inimigo
        }
    }
}