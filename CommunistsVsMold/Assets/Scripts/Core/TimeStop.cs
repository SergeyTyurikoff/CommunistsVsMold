using System.Collections.Generic;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Стоп-время (PORT_SPEC §3): на время замораживает врагов/боссов и вражеские снаряды,
    /// игрок действует как обычно. Заморозка — отключение компонентов всех врагов
    /// (реализуют <see cref="IBiomeScalable"/>) и <see cref="EnemyProjectile"/> + обнуление
    /// их скорости; по окончании — включение обратно. Откат после действия. Здоровье не тратит.
    /// Активируется игроком клавишей F (если способность открыта).
    /// </summary>
    public class TimeStop : MonoBehaviour
    {
        public static TimeStop Instance { get; private set; }

        [SerializeField] float duration = 5f;     // 300 кадров
        [SerializeField] float cooldown = 7f;     // 420 кадров

        float activeTimer, cdTimer;
        readonly List<MonoBehaviour> frozen = new List<MonoBehaviour>();

        public bool Active => activeTimer > 0f;
        public bool Ready => activeTimer <= 0f && cdTimer <= 0f;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Попробовать включить стоп-время. false, если идёт или на откате.</summary>
        public bool TryActivate()
        {
            if (!Ready) return false;
            Activate();
            return true;
        }

        void Activate()
        {
            activeTimer = duration;
            frozen.Clear();

            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb == null || !mb.enabled) continue;
                if (mb is IBiomeScalable || mb is EnemyProjectile)
                {
                    mb.enabled = false;
                    frozen.Add(mb);
                    var rb = mb.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                }
            }
            GameFX.Instance?.Shake(0.12f, 0.12f);
        }

        void Update()
        {
            if (activeTimer > 0f)
            {
                activeTimer -= Time.deltaTime;
                if (activeTimer <= 0f) { Unfreeze(); cdTimer = cooldown; }
            }
            else if (cdTimer > 0f) cdTimer -= Time.deltaTime;
        }

        void Unfreeze()
        {
            foreach (var mb in frozen)
                if (mb != null) mb.enabled = true;
            frozen.Clear();
        }
    }
}
