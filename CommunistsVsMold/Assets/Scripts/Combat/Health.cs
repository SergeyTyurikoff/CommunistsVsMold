using System;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Здоровье сущности. Реализует IDamageable: принимает урон и нокбэк.
    /// Само НЕ убывает (по спецификации) — только через TakeDamage / Heal.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHp = 100f;

        public float Hp { get; private set; }
        public float MaxHp => maxHp;
        public bool IsDead { get; private set; }

        /// <summary>Вызывается перед уничтожением объекта (для эффектов/звука/счёта).</summary>
        public event Action OnDeath;

        /// <summary>Глобальное событие смерти любой сущности с Health (для дропа лута и пр.).</summary>
        public static event Action<Health> OnAnyDeath;

        private Rigidbody2D rb;
        private IDamageFilter filter;   // опц. модификатор урона (щит и т.п.)

        private void Awake()
        {
            Hp = maxHp;
            rb = GetComponent<Rigidbody2D>();
            filter = GetComponent<IDamageFilter>();
        }

        /// <summary>Масштабировать максимум HP (биом-скейл при спавне). Доливает до нового максимума.</summary>
        public void ScaleMaxHp(float mul)
        {
            maxHp *= mul;
            Hp = maxHp;
        }

        public void TakeDamage(float dmg, Vector2 knockback)
        {
            if (IsDead)
                return;

            if (filter != null)
                dmg = filter.ModifyDamage(dmg, knockback);

            Hp -= dmg;

            if (dmg > 0f) GameFX.Instance?.SpawnDamageNumber(transform.position, dmg);

            if (rb != null && knockback != Vector2.zero)
                rb.AddForce(knockback, ForceMode2D.Impulse);

            if (Hp <= 0f)
            {
                IsDead = true;
                Die();
            }
        }

        public void Heal(float a)
        {
            if (IsDead)
                return;

            Hp = Mathf.Min(Hp + a, maxHp);
        }

        private void Die()
        {
            OnDeath?.Invoke();
            OnAnyDeath?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
