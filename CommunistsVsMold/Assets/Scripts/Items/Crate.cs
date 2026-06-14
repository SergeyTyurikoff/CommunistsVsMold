using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Разрушаемый ящик. Принимает урон как <see cref="IDamageable"/>, но НЕ использует
    /// <c>Health</c> — чтобы не считаться убийством и не получать двойной лут через LootService.
    /// При разрушении сам спавнит лут и эффект разлёта.
    /// </summary>
    public class Crate : MonoBehaviour, IDamageable
    {
        [Header("Прочность")]
        [SerializeField] private float hp = 12f;   // 1 выстрел пистолета (18) ломает

        [Header("Лут")]
        [Tooltip("Минимум денег при разрушении (включительно).")]
        [SerializeField] private int moneyMin = 2;
        [Tooltip("Максимум денег при разрушении (исключительно для Random.Range(int)).")]
        [SerializeField] private int moneyMax = 6;
        [Tooltip("Шанс выпадения дополнительного патрона к пистолету (0..1).")]
        [SerializeField] private float ammoChance = 0.4f;
        [Tooltip("Количество патронов к пистолету при выпадении.")]
        [SerializeField] private int ammoAmount = 10;

        // Коричневый цвет ящика по умолчанию (фолбэк-спрайт и гибы).
        private static readonly Color CrateColor = new Color(0.55f, 0.35f, 0.15f);

        // Флаг, чтобы Break() не сработал дважды (повторный урон в кадре разрушения).
        private bool _broken;

        /// <summary>
        /// Создаёт ящик в мире: спрайт-рендер, не-триггерный BoxCollider2D, слой Enemy (10),
        /// масштаб под заданный размер мира. Возвращает добавленный компонент Crate.
        /// </summary>
        /// <param name="pos">Позиция в мире.</param>
        /// <param name="worldSize">Желаемый размер ящика в юнитах (высота при наличии спрайта).</param>
        /// <param name="sprite">Спрайт ящика; если null — рисуется коричневым квадратом ~1×1.</param>
        public static Crate Spawn(Vector3 pos, float worldSize, Sprite sprite)
        {
            var go = new GameObject("Crate");
            go.transform.position = pos;
            // Слой Enemy=10 — по нему бьют пули игрока (Bullet ищет IDamageable на этом слое).
            go.layer = 10;

            var sr = go.AddComponent<SpriteRenderer>();
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = false;

            // Kinematic Rigidbody2D: пуля бьёт через OnTriggerEnter2D, а Unity шлёт
            // триггер-события только если в паре есть хотя бы один Rigidbody2D. Без него
            // ящик (статичный коллайдер) не получал урон — «не ломался» от выстрела.
            // Dynamic + gravity 0 + FreezeAll: тело неподвижно, но (в отличие от Kinematic)
            // НАДЁЖНО генерирует триггер-контакты с пулей — поэтому ящик ломается выстрелом
            // (Kinematic-vs-Kinematic триггер не срабатывал). Враги по той же причине Dynamic.
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;

            if (sprite != null)
            {
                sr.sprite = sprite;
                // Масштабируем так, чтобы высота спрайта стала ~worldSize юнитов.
                float h = sprite.bounds.size.y;
                go.transform.localScale = Vector3.one * (h > 0f ? worldSize / h : worldSize);
                // ВАЖНО: размер коллайдера задаём ЯВНО по спрайту. Авто-подгон BoxCollider2D
                // случился при ДОБАВЛЕНИИ (когда спрайт был null) и дал size=0 → пули летели
                // сквозь ящик, он не ломался. Задаём локальный размер = размер спрайта.
                var bs = sprite.bounds.size;
                box.size = new Vector2(Mathf.Max(0.1f, bs.x), Mathf.Max(0.1f, bs.y));
            }
            else
            {
                // Фолбэк: коричневый цвет, коллайдер ~1×1, масштаб = worldSize.
                sr.color = CrateColor;
                box.size = Vector2.one;
                go.transform.localScale = Vector3.one * worldSize;
            }

            return go.AddComponent<Crate>();
        }

        /// <summary>
        /// Получение урона. Отбрасывание (knockback) для статичного ящика игнорируется.
        /// При hp &lt;= 0 разрушает ящик.
        /// </summary>
        public void TakeDamage(float dmg, Vector2 kb)
        {
            if (_broken) return;

            hp -= dmg;
            if (hp <= 0f)
                Break();
        }

        /// <summary>
        /// Разрушение ящика: спавн лута, эффект разлёта, уничтожение объекта.
        /// </summary>
        private void Break()
        {
            if (_broken) return;
            _broken = true;

            Vector3 lootPos = transform.position + Vector3.up * 0.3f;

            // Гарантированные деньги.
            Pickup.Spawn(PickupKind.Money, Random.Range(moneyMin, moneyMax), lootPos, null);

            // С шансом — патроны к пистолету (чуть в сторону, чтобы не слипались).
            if (Random.value < ammoChance)
                Pickup.Spawn(PickupKind.AmmoPistol, ammoAmount, lootPos + Vector3.right * 0.4f, null);

            // Эффект разлёта щепок (null-safe).
            GameFX.Instance?.SpawnGibs(transform.position, CrateColor, 10);

            Destroy(gameObject);
        }
    }
}
