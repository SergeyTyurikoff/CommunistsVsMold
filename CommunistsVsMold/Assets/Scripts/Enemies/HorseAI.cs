using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// ИИ врага-«всадника» (horse): самый быстрый враг — чарджер.
    /// Стейт-машина: Patrol (медленный патруль вокруг стартовой X) → Charge
    /// (фиксирует направление на игрока и несётся горизонтально) → Recover
    /// (после удара / промаха тормозит и берёт короткую передышку, затем снова целится).
    /// Вешается на объект-всадника (рядом должны быть Rigidbody2D, Collider2D, Health).
    /// Движение по X — через rb.linearVelocity; Y НЕ трогаем (гравитация, боевых прыжков нет).
    /// Игрок ищется по тегу "Player"; урон — через PlayerController.Damage; отброс — через его Rigidbody2D.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class HorseAI : MonoBehaviour, IBiomeScalable
    {
        // Фазы поведения стейт-машины.
        enum Phase { Patrol, Charge, Recover }

        [Header("Патруль")]
        [SerializeField] float patrolSpeed = 2f;       // медленная скорость патруля
        [SerializeField] float patrolHalfWidth = 3f;   // полуширина зоны патруля от стартовой X

        [Header("Детект / чардж")]
        [SerializeField] float detectRange = 10f;      // дистанция, на которой всадник срывается в чардж
        [SerializeField] float chargeSpeed = 10f;      // скорость рывка (крайне быстрый из всех врагов)

        [Header("Контактный удар")]
        [SerializeField] float contactRange = 1.2f;    // дистанция контакта с игроком во время чарджа
        [SerializeField] float contactDamage = 16f;    // урон при таране
        [SerializeField] float knockX = 14f;           // горизонтальный импульс отброса игрока (по направлению чарджа)
        [SerializeField] float upKnock = 6f;           // вертикальный импульс отброса игрока (вверх)

        [Header("Экранная отдача удара")]
        [SerializeField] float shakeAmp = 0.25f;       // амплитуда тряски камеры
        [SerializeField] float shakeDur = 0.18f;       // длительность тряски камеры
        [SerializeField] float hitStop = 0.06f;        // короткая остановка времени при таране

        [Header("Восстановление / антизастревание")]
        [SerializeField] float recoverTime = 0.9f;     // передышка после удара или промаха
        [SerializeField] float overshootMargin = 0.4f; // насколько нужно «проскочить» игрока, чтобы считать промах
        [SerializeField] float stuckSpeedEps = 0.4f;   // порог скорости, ниже которого во время чарджа считаем «упёрся»
        [SerializeField] float stuckTimeMax = 0.35f;   // сколько секунд терпеть «упор», прежде чем уйти в Recover

        Rigidbody2D rb;
        Health health;
        Transform playerTf;
        PlayerController pc;
        Rigidbody2D playerRb;
        SpriteRenderer sprite;

        Phase phase = Phase.Patrol;
        float startX;
        int facing = -1;        // направление взгляда/движения: -1 влево, +1 вправо
        int chargeDir = 1;      // зафиксированное направление текущего чарджа
        float recoverTimer;     // обратный отсчёт передышки
        float stuckTimer;       // накопитель времени «упора» во время чарджа

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            startX = transform.position.x;

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                playerTf = playerGo.transform;
                pc = playerGo.GetComponent<PlayerController>();
                playerRb = playerGo.GetComponent<Rigidbody2D>();
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Мёртв — ничего не делаем.
            if (health != null && health.IsDead)
                return;

            // Нет игрока — только патруль.
            if (playerTf == null)
            {
                phase = Phase.Patrol;
                Patrol();
                ApplyFacing();
                return;
            }

            switch (phase)
            {
                case Phase.Patrol:  TickPatrol();  break;
                case Phase.Charge:  TickCharge(dt); break;
                case Phase.Recover: TickRecover(dt); break;
            }

            ApplyFacing();
        }

        // PATROL: медленно ходим у стартовой X; при детекте игрока — срыв в чардж.
        void TickPatrol()
        {
            float dx = playerTf.position.x - transform.position.x;
            if (Mathf.Abs(dx) <= detectRange)
            {
                StartCharge(dx);
                return;
            }
            Patrol();
        }

        // Патруль между [startX - patrolHalfWidth; startX + patrolHalfWidth].
        void Patrol()
        {
            float selfX = transform.position.x;
            if (selfX <= startX - patrolHalfWidth) facing = 1;
            else if (selfX >= startX + patrolHalfWidth) facing = -1;

            rb.linearVelocity = new Vector2(facing * patrolSpeed, rb.linearVelocity.y);
        }

        // Вход в чардж: фиксируем направление на игрока, обнуляем таймеры антизастревания.
        void StartCharge(float dx)
        {
            chargeDir = dx >= 0f ? 1 : -1;
            facing = chargeDir;
            stuckTimer = 0f;
            phase = Phase.Charge;
        }

        // CHARGE: несёмся горизонтально в зафиксированном направлении.
        // Выходы: контакт с игроком → удар + Recover; проскочили игрока → Recover; упёрлись → Recover.
        void TickCharge(float dt)
        {
            rb.linearVelocity = new Vector2(chargeDir * chargeSpeed, rb.linearVelocity.y);

            float dx = playerTf.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);

            // Контакт во время чарджа — таран.
            if (dist <= contactRange)
            {
                HitPlayer();
                return;
            }

            // Overshoot: игрок остался заметно позади по направлению чарджа — промах.
            if (dx * chargeDir < -overshootMargin)
            {
                EnterRecover();
                return;
            }

            // Антизастревание: если во время чарджа скорость почти нулевая (упёрлись в стену/уступ).
            if (Mathf.Abs(rb.linearVelocity.x) < stuckSpeedEps)
            {
                stuckTimer += dt;
                if (stuckTimer >= stuckTimeMax)
                {
                    EnterRecover();
                    return;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }

        // Таран игрока: урон, сильный отброс, экранная отдача, затем передышка.
        void HitPlayer()
        {
            if (pc != null) pc.Damage(contactDamage);

            if (playerRb != null)
            {
                Vector2 force = new Vector2(chargeDir * knockX, upKnock);
                playerRb.AddForce(force, ForceMode2D.Impulse);
            }

            GameFX.Instance?.Shake(shakeAmp, shakeDur);
            GameFX.Instance?.HitStop(hitStop);

            EnterRecover();
        }

        // Вход в Recover: гасим горизонтальную скорость и заводим таймер передышки.
        void EnterRecover()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            recoverTimer = recoverTime;
            phase = Phase.Recover;
        }

        // RECOVER: стоим (по X), отсчитываем передышку, затем снова целимся.
        void TickRecover(float dt)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            recoverTimer -= dt;
            if (recoverTimer <= 0f)
            {
                float dx = playerTf.position.x - transform.position.x;
                if (Mathf.Abs(dx) <= detectRange)
                    StartCharge(dx);        // игрок ещё в зоне — новый рывок
                else
                    phase = Phase.Patrol;   // ушёл далеко — возвращаемся к патрулю
            }
        }

        // Поворот спрайта по направлению движения. Спрайт по умолчанию смотрит вправо (flipX=false → вправо).
        void ApplyFacing()
        {
            if (sprite != null)
                sprite.flipX = facing < 0;
        }

        /// <summary>
        /// Масштабирование под биом/уровень: скорости, урон тарана и максимальное HP.
        /// </summary>
        public void ApplyBiomeScale(int biome, int levelBoost)
        {
            float spd = BiomeScaling.EnemySpeed(biome, levelBoost);
            float dmg = BiomeScaling.EnemyDmg(biome, levelBoost);

            chargeSpeed *= spd;
            patrolSpeed *= spd;
            contactDamage *= dmg;

            if (health != null)
                health.ScaleMaxHp(BiomeScaling.EnemyHp(biome, levelBoost));
        }
    }
}
