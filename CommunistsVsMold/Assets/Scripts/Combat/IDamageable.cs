namespace Kommunisty
{
    /// <summary>
    /// Всё, что может получать урон (герой, враги, разрушаемые объекты).
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float dmg, UnityEngine.Vector2 knockback);
    }
}
