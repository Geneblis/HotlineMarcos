// IDamageable.cs
// Interface central de dano. Todo objeto que pode ser danificado implementa isso.
public enum DamageType { Bullet, Melee, Thrown }

public interface IDamageable
{
    void TakeDamage(float damage, DamageType damageType);
}