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

        Vector3 vel;

        // Тряска экрана (screen shake). Тикает на unscaledDeltaTime, чтобы трясло даже при hit-stop.
        float shakeTimer;
        float shakeDur;
        float shakeMag;

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            if (desired.y < minY) desired.y = minY;
            desired.z = offset.z;

            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
            next.z = offset.z;

            // Затухающий случайный сдвиг поверх следования; Z не трогаем.
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                if (shakeTimer < 0f) shakeTimer = 0f;
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
