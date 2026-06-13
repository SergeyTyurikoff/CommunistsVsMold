using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Сундук-триггер: при касании игроком открывается ОДИН раз и выдаёт лут
    /// по строке-описанию. После открытия затемняется и самоуничтожается.
    /// </summary>
    public class Chest : MonoBehaviour
    {
        // Строка лута, заданная при спавне (например "heal", "money", "ammo", "gasMask", "sabre"...).
        [SerializeField] private string loot;

        // Флаг: сундук уже открыт (открытие строго один раз).
        private bool opened;

        /// <summary>
        /// Создаёт сундук на позиции pos с указанным лутом.
        /// </summary>
        /// <param name="pos">Позиция сундука в мире.</param>
        /// <param name="loot">Строка лута (см. маппинг в Open).</param>
        /// <param name="sprite">Спрайт сундука; если null — рисуется коричневым прямоугольником.</param>
        public static Chest Spawn(Vector3 pos, string loot, Sprite sprite)
        {
            var go = new GameObject("Chest");
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sr.sprite = sprite;
                // Масштабируем так, чтобы высота спрайта была ~1.2 юнита.
                float h = sprite.bounds.size.y;
                if (h > 0.0001f)
                {
                    float scale = 1.2f / h;
                    go.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
            else
            {
                // Фолбэк-цвет: тёплый коричневый.
                sr.color = new Color(0.7f, 0.5f, 0.15f);
            }

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.7f;

            var chest = go.AddComponent<Chest>();
            chest.loot = loot;
            return chest;
        }

        /// <summary>
        /// При входе игрока в триггер — открыть сундук (один раз).
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (opened) return;

            var pc = other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                Open(pc);
            }
        }

        /// <summary>
        /// Открыть сундук: выдать ОДИН пикап по строке loot, проиграть звук,
        /// затемнить спрайт и уничтожить объект.
        /// </summary>
        private void Open(PlayerController pc)
        {
            if (opened) return;
            opened = true;

            // Пикап спавнится чуть выше сундука.
            Vector3 dropPos = transform.position + Vector3.up * 0.6f;

            // Маппинг строки лута на конкретный пикап.
            switch (loot)
            {
                case "heal":
                    Pickup.Spawn(PickupKind.Health, 40, dropPos, null);
                    break;
                case "money":
                    Pickup.Spawn(PickupKind.Money, 15, dropPos, null);
                    break;
                case "ammo":
                    Pickup.Spawn(PickupKind.AmmoPistol, 24, dropPos, null);
                    break;
                case "gasMask":
                    // Отдельного пикапа противогаза нет — выдаём аптечку как фолбэк.
                    Pickup.Spawn(PickupKind.Medkit, 1, dropPos, null);
                    break;
                default:
                    // Оружие (sabre/smg/gasSprayer/shotgun и пр.) и всё остальное —
                    // покупок оружия через пикап нет, фолбэк деньгами.
                    Pickup.Spawn(PickupKind.Money, 25, dropPos, null);
                    break;
            }

            AudioManager.Instance?.PlayPickup();

            // Затемняем спрайт (вполовину яркости и прозрачности) как визуальную метку «открыт».
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color * 0.5f;
                c.a = 0.5f;
                sr.color = c;
            }

            Destroy(gameObject, 0.05f);
        }
    }
}
