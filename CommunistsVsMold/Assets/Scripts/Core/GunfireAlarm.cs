using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Тревога по выстрелам (PORT_SPEC §5): игрок стреляет → враги в радиусе «слышат»
    /// и в течение окна памяти считают игрока обнаруженным (идут в агр), даже если он
    /// вне их обычного радиуса. Это даёт реакцию на огонь, память и «поддержку»
    /// (реагируют все в радиусе). Враг проверяет <see cref="Hears"/> в своём детекте.
    /// </summary>
    public static class GunfireAlarm
    {
        public const float Memory = 2.5f;   // сколько секунд враги помнят выстрел

        static Vector2 pos;
        static float time = -999f;
        static float radius;

        // Сброс статики между Play-сессиями в редакторе.
        [RuntimeInitializeOnLoadMethod]
        static void ResetState() { time = -999f; }

        /// <summary>Сообщить о выстреле: позиция и радиус слышимости.</summary>
        public static void Report(Vector2 p, float r) { pos = p; radius = r; time = Time.time; }

        /// <summary>Слышит ли враг в этой точке недавний выстрел в радиусе.</summary>
        public static bool Hears(Vector2 enemyPos)
        {
            if (Time.time - time > Memory) return false;
            return (enemyPos - pos).sqrMagnitude <= radius * radius;
        }
    }
}
