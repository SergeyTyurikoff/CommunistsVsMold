using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Снаряд ВРАГА. Бьёт ТОЛЬКО игрока (через PlayerController.Damage), по другим врагам не реагирует.
    /// Создаётся процедурно через статический Spawn() — отдельный GameObject со своим коллайдером и спрайтом.
    /// Движение — через transform в FixedUpdate; самоуничтожение при достижении дальности range
    /// или при попадании в Ground (слой 8) / игрока.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        const int GroundLayer = 8; // слой земли/платформ
        const int EnemyLayer = 10; // слой врагов — по ним не реагируем

        Vector2 dir;
        float speed;
        float damage;
        float range;
        float travelled; // пройденный путь, для самоуничтожения по дальности

        /// <summary>
        /// Создаёт снаряд врага в позиции pos, летящий в направлении dir.
        /// </summary>
        /// <param name="pos">Стартовая позиция.</param>
        /// <param name="dir">Направление полёта (нормализуется внутри).</param>
        /// <param name="speed">Скорость, юнитов/с.</param>
        /// <param name="damage">Урон по игроку.</param>
        /// <param name="range">Максимальная дальность полёта, юнитов.</param>
        /// <param name="color">Цвет спрайта снаряда.</param>
        public static EnemyProjectile Spawn(Vector3 pos, Vector2 dir, float speed, float damage, float range, Color color)
        {
            var go = new GameObject("EnemyProjectile");
            go.transform.position = pos;

            // Коллайдер-триггер для регистрации попаданий.
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.15f;

            // Визуал: спрайт можно оставить null (рисуется только цвет), но цвет задаём.
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = color;
            sr.sortingOrder = 5;

            var proj = go.AddComponent<EnemyProjectile>();
            proj.Init(dir, speed, damage, range);
            return proj;
        }

        void Init(Vector2 d, float spd, float dmg, float rng)
        {
            dir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right;
            speed = spd;
            damage = dmg;
            range = rng;
            travelled = 0f;
        }

        void FixedUpdate()
        {
            float step = speed * Time.fixedDeltaTime;
            transform.position += (Vector3)(dir * step);

            travelled += step;
            if (travelled >= range)
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // По врагам не реагируем.
            if (other.gameObject.layer == EnemyLayer)
                return;

            // Попадание в землю/платформу — снаряд гаснет.
            if (other.gameObject.layer == GroundLayer)
            {
                Destroy(gameObject);
                return;
            }

            // Попадание в игрока — урон только если он уязвим.
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                if (!pc.IsInvulnerable)
                {
                    pc.Damage(damage);
                    Destroy(gameObject);
                }
                // Если игрок в i-frames — снаряд пролетает дальше (не тратится впустую).
            }
        }
    }
}
