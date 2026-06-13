using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Облако ядовитого газа. Создаётся процедурно (без префаба) фабричным методом Spawn.
    /// Газ НЕ наносит урон — пока игрок внутри облака, тот замедляется (через PlayerController.ApplySlow).
    /// Живёт фиксированное время, после чего самоуничтожается. Может медленно дрейфовать по X.
    /// PORT_SPEC §3 «Газомёт/Газ»: cloudLife ~190f≈3.2с, радиус ~64px≈2u, slow ×0.5.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class GasCloud : MonoBehaviour
    {
        float life;          // сколько секунд живёт облако
        float slowMult;      // множитель скорости игрока (×0.5)
        float drift;         // скорость дрейфа по X (юниты/сек), может быть 0
        float lifeTimer;     // обратный отсчёт жизни

        // Полупрозрачный зелёный цвет облака газа.
        static readonly Color GasColor = new Color(0.35f, 0.85f, 0.25f, 0.45f);

        /// <summary>
        /// Создаёт и инициализирует облако газа в указанной точке.
        /// </summary>
        /// <param name="pos">Позиция центра облака в мире.</param>
        /// <param name="radius">Радиус облака (юниты), ~2u.</param>
        /// <param name="life">Время жизни (сек), ~3.2с.</param>
        /// <param name="slowMult">Множитель скорости игрока в облаке, ~0.5.</param>
        /// <param name="drift">Скорость дрейфа по X (юниты/сек), может быть 0.</param>
        public static GasCloud Spawn(Vector3 pos, float radius, float life, float slowMult, float drift)
        {
            var go = new GameObject("GasCloud");
            go.transform.position = pos;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;

            // Полупрозрачный зелёный визуал. Sprite оставляем null (можно подменить позже),
            // но цвет/масштаб настраиваем, чтобы при наличии спрайта облако было видно.
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = GasColor;
            sr.sortingOrder = 5;

            var cloud = go.AddComponent<GasCloud>();
            cloud.Init(radius, life, slowMult, drift);
            return cloud;
        }

        /// <summary>
        /// Инициализация параметров облака (вызывается фабрикой Spawn).
        /// </summary>
        public void Init(float radius, float life, float slowMult, float drift)
        {
            this.life = life;
            this.slowMult = slowMult;
            this.drift = drift;
            this.lifeTimer = life;

            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = radius;
            }
        }

        void Update()
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // Медленный дрейф по X.
            if (drift != 0f)
                transform.position += new Vector3(drift * Time.deltaTime, 0f, 0f);
        }

        // Пока игрок внутри облака — каждый кадр рефрешим замедление (без урона).
        void OnTriggerStay2D(Collider2D other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null || pc.IsDead) return;
            // Противогаз (UtilityInventory) даёт иммунитет к газу.
            var util = pc.GetComponent<UtilityInventory>();
            if (util != null && util.GasImmune) return;
            pc.ApplySlow(slowMult, 0.45f);
        }
    }
}
