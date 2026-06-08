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
        public bool IsDead { get; private set; }

        /// <summary>Вызывается перед уничтожением объекта (для эффектов/звука/счёта).</summary>
        public event Action OnDeath;

        private Rigidbody2D rb;

        private void Awake()
        {
            Hp = maxHp;
            rb = GetComponent<Rigidbody2D>();
        }

        public void TakeDamage(float dmg, Vector2 knockback)
        {
            if (IsDead)
                return;

            Hp -= dmg;

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
            Destroy(gameObject);
        }
    }
}
