using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Снаряд. Летит по прямой до maxDist, наносит урон цели на targetMask.
    /// Управляется пулом (BulletPool) и оружием через Init.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Bullet : MonoBehaviour
    {
        // Рантайм-состояние — НЕ сериализуется.
        private Vector2 dir;
        private float speed;
        private float damage;
        private float knockback;
        private float maxDist;
        private float travelled;
        private LayerMask targetMask;

        /// <summary>
        /// Инициализация снаряда из пула/оружия. Включает объект, задаёт позицию,
        /// разворачивает по направлению и сбрасывает пройденный путь.
        /// </summary>
        public void Init(Vector2 pos, Vector2 direction, float speed, float damage, float range, float knockback, LayerMask targetMask)
        {
            transform.position = pos;

            dir = direction.normalized;
            this.speed = speed;
            this.damage = damage;
            this.maxDist = range;
            this.knockback = knockback;
            this.targetMask = targetMask;
            travelled = 0f;

            // Поворот спрайта по направлению полёта (z-rotation).
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {
            float step = speed * Time.fixedDeltaTime;
            transform.position += (Vector3)(dir * step);
            travelled += step;

            if (travelled >= maxDist)
                Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Цель должна быть на одном из слоёв targetMask.
            if ((targetMask.value & (1 << other.gameObject.layer)) == 0)
                return;

            var target = other.GetComponent<IDamageable>();
            if (target == null)
                target = other.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                target.TakeDamage(damage, dir.normalized * knockback);
                Despawn();
            }
        }

        private void Despawn()
        {
            if (BulletPool.Instance != null)
                BulletPool.Instance.Return(this);
            else
                gameObject.SetActive(false);
        }
    }
}
