using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Плавное следование камеры за игроком через Vector3.SmoothDamp.
    /// Z держим из offset; по Y не опускаемся ниже minY (чтобы не уезжать под уровень).
    /// Вешается на Main Camera, target = Player.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 1f, -10f);
        [SerializeField] float smoothTime = 0.15f;
        [SerializeField] float minY = -3f;
        [SerializeField] float deadZone = 0.06f;     // игнор микро-смещений цели → нет дрожи у стоящего игрока

        [Header("Проекция (2D)")]
        [SerializeField] bool forceOrthographic = true;     // 2D-платформер: камера ОБЯЗАНА быть ортографической
        [SerializeField] float orthographicSize = 5.77f;    // ≈ прежний перспективный кадр (FOV 60 @ z=-10)

        Vector3 vel;
        Vector3 followPos;       // «зафиксированная» позиция цели с учётом дедзоны
        bool followInit;

        // Тряска экрана (screen shake). Тикает на unscaledDeltaTime, чтобы трясло даже при hit-stop.
        float shakeTimer;
        float shakeDur;
        float shakeMag;

        void Awake()
        {
            // Перспективная камера на 2D-уровне даёт паразитный сдвиг/дрожь и неверный
            // масштаб при движении по Z — принудительно делаем ортографической.
            if (!forceOrthographic) return;
            var c = GetComponent<Camera>();
            if (c != null) { c.orthographic = true; c.orthographicSize = orthographicSize; }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Дедзона: следуем за целью, но не дёргаемся на её микро-колебаниях (когда игрок
            // почти стоит, физика может давать субпиксельную дрожь позиции).
            Vector3 tp = target.position;
            if (!followInit) { followPos = tp; followInit = true; }
            if (Mathf.Abs(tp.x - followPos.x) > deadZone) followPos.x = tp.x;
            if (Mathf.Abs(tp.y - followPos.y) > deadZone) followPos.y = tp.y;

            Vector3 desired = followPos + offset;
            if (desired.y < minY) desired.y = minY;
            desired.z = offset.z;

            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
            next.z = offset.z;

            // Затухающий случайный сдвиг поверх следования; Z не трогаем.
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                if (shakeTimer <= 0f) { shakeTimer = 0f; shakeMag = 0f; } // полный сброс амплитуды по окончании
                float amt = (shakeDur > 0f) ? shakeMag * (shakeTimer / shakeDur) : 0f;
                next.x += Random.Range(-amt, amt);
                next.y += Random.Range(-amt, amt);
            }

            transform.position = next;
        }

        /// <summary>
        /// Запустить тряску экрана. Если уже трясётся — берём максимум по длительности и амплитуде.
        /// </summary>
        public void AddShake(float duration, float magnitude)
        {
            if (duration <= 0f || magnitude <= 0f) return;

            if (duration > shakeTimer)
            {
                shakeTimer = duration;
                shakeDur = duration;
            }
            if (magnitude > shakeMag) shakeMag = magnitude;
        }

        public void SetTarget(Transform t) => target = t;
    }
}
