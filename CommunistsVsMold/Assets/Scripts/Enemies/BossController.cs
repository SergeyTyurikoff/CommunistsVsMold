using System.Collections;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Базовый контроллер босса (PORT_SPEC §6). Общее поведение всех боссов биомов:
    /// подход к игроку с удержанием дистанции, контактный урон, периодическое «турбо»,
    /// фаза 2 при HP &lt; 40%, телеграф-кольцо перед залпом, блокировка портала выхода
    /// пока босс жив. Конкретная атака — в подклассе через переопределение DoAttack().
    ///
    /// Публичный API заморожен (под него пишется BossHUD): Active, Hp01, BossName,
    /// Phase, IsAlive, OnDefeated, ApplyBiomeScale.
    /// Движение по X — через rb.linearVelocity; Y НЕ трогаем (гравитация).
    /// Игрок ищется по тегу "Player"; урон игроку — через PlayerController.Damage;
    /// отброс игрока — через его Rigidbody2D.AddForce.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public abstract class BossController : MonoBehaviour, IBiomeScalable
    {
        // ───────────────────────── Идентификация / HUD ─────────────────────────
        [Header("HUD")]
        [Tooltip("Имя босса для полосы здоровья.")]
        [SerializeField] string bossName = "Босс";

        // ───────────────────────── Движение / бой ─────────────────────────
        [Header("Движение / дистанция")]
        [Tooltip("Базовая скорость подхода, юнитов/с.")]
        [SerializeField] protected float moveSpeed = 2.5f;
        [Tooltip("Дистанция, которую босс старается удерживать до игрока, юнитов.")]
        [SerializeField] protected float preferredRange = 4f;
        [Tooltip("Мёртвая зона вокруг preferredRange, чтобы не дёргаться туда-сюда, юнитов.")]
        [SerializeField] protected float rangeDeadzone = 0.6f;

        [Header("Контактный урон")]
        [Tooltip("Дистанция касания игрока для контактного урона, юнитов.")]
        [SerializeField] protected float contactRange = 1.5f;
        [Tooltip("Контактный урон игроку.")]
        [SerializeField] protected float contactDamage = 12f;
        [Tooltip("Кулдаун контактного урона, секунд.")]
        [SerializeField] protected float contactCooldown = 0.7f;
        [Tooltip("Горизонтальный импульс отброса игрока при контакте.")]
        [SerializeField] protected float contactKnockX = 8f;
        [Tooltip("Вертикальный импульс отброса игрока при контакте (вверх).")]
        [SerializeField] protected float contactKnockUp = 4f;

        // ───────────────────────── Турбо ─────────────────────────
        [Header("Турбо")]
        [Tooltip("Период между включениями турбо, секунд.")]
        [SerializeField] protected float turboInterval = 8f;
        [Tooltip("Длительность турбо, секунд (150 кадров @60fps = 2.5с).")]
        [SerializeField] protected float turboDuration = 2.5f;
        [Tooltip("Множитель скорости во время турбо.")]
        [SerializeField] protected float turboSpeedMult = 1.75f;

        // ───────────────────────── Фаза 2 ─────────────────────────
        [Header("Фаза 2 (HP < 40%)")]
        [Tooltip("Порог доли HP для перехода в фазу 2.")]
        [SerializeField] protected float phase2Threshold = 0.4f;
        [Tooltip("Множитель скорости в фазе 2.")]
        [SerializeField] protected float phase2SpeedMult = 1.55f;
        [Tooltip("Множитель fireDelay в фазе 2 (атаки чаще).")]
        [SerializeField] protected float phase2FireMult = 0.62f;
        [Tooltip("Красноватая подсветка спрайта в фазе 2.")]
        [SerializeField] protected Color phase2Tint = new Color(1f, 0.55f, 0.5f, 1f);

        // ───────────────────────── Атака / телеграф ─────────────────────────
        [Header("Атака")]
        [Tooltip("Задержка между атаками (циклами огня), секунд. В фазе 2 умножается на phase2FireMult.")]
        [SerializeField] protected float fireDelay = 2.2f;
        [Tooltip("Радиус, в котором базовый «залп» наносит урон игроку.")]
        [SerializeField] protected float attackRange = 14f;
        [Tooltip("Урон базового «залпа» (подкласс может игнорировать).")]
        [SerializeField] protected float attackDamage = 14f;

        [Header("Телеграф выстрела (кольцо-зарядка)")]
        [Tooltip("Длительность телеграфа перед атакой, секунд (10 кадров @60fps ≈ 0.17с).")]
        [SerializeField] protected float telegraphTime = 0.17f;
        [Tooltip("Цвет телеграф-кольца.")]
        [SerializeField] protected Color telegraphColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        [Tooltip("Радиус телеграф-кольца, юнитов.")]
        [SerializeField] protected float telegraphRadius = 0.9f;

        // ───────────────────────── Смерть ─────────────────────────
        [Header("Смерть")]
        [Tooltip("Сколько кусков (gibs) разбросать при гибели босса.")]
        [SerializeField] protected int deathGibs = 28;
        [Tooltip("Цвет кусков при гибели.")]
        [SerializeField] protected Color deathGibColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        [Tooltip("Амплитуда тряски при гибели.")]
        [SerializeField] protected float deathShakeAmp = 0.5f;
        [Tooltip("Длительность тряски при гибели, секунд.")]
        [SerializeField] protected float deathShakeDur = 0.4f;

        // ───────────────────────── Кэш / состояние ─────────────────────────
        protected Rigidbody2D rb;
        protected Health health;
        protected SpriteRenderer sprite;
        protected Transform playerTf;
        protected PlayerController player;
        protected Rigidbody2D playerRb;

        protected int facing = -1;        // направление взгляда: -1 влево, +1 вправо

        int phase = 1;                    // текущая фаза (1 или 2)
        bool phase2Entered;               // фаза 2 уже активирована
        Color baseSpriteColor = Color.white;

        float fireTimer;                  // отсчёт до следующей атаки
        bool isAttacking;                 // идёт ли телеграф/атака (блокирует новый цикл)
        float contactTimer;               // кулдаун контактного урона

        float turboTimer;                 // отсчёт до следующего турбо
        float turboLeft;                  // остаток активного турбо, секунд
        bool turboActive;                 // турбо активно сейчас

        ExitPortal portal;                // портал выхода (блокируем, пока босс жив)
        bool defeated;                    // OnDefeated уже вызван (один раз)

        // ───────────────────────── Публичный API (ЗАМОРОЖЕН) ─────────────────────────

        /// <summary>Текущий живой босс или null.</summary>
        public static BossController Active { get; private set; }

        /// <summary>Доля HP 0..1 (health.Hp / health.MaxHp).</summary>
        public float Hp01
        {
            get
            {
                if (health == null || health.MaxHp <= 0f) return 0f;
                return Mathf.Clamp01(health.Hp / health.MaxHp);
            }
        }

        /// <summary>Имя босса для HUD.</summary>
        public string BossName => bossName;

        /// <summary>Текущая фаза: 1 или 2.</summary>
        public int Phase => phase;

        /// <summary>Жив ли босс.</summary>
        public bool IsAlive => health != null && !health.IsDead;

        /// <summary>Событие гибели босса (вызывается один раз).</summary>
        public event System.Action OnDefeated;

        // ───────────────────────── Доступ для подклассов ─────────────────────────

        /// <summary>Текущий множитель скорости (турбо/фаза 2/база) — для подклассов.</summary>
        protected float CurrentSpeedMult
        {
            get
            {
                float m = 1f;
                if (turboActive) m *= turboSpeedMult;
                if (phase == 2) m *= phase2SpeedMult;
                return m;
            }
        }

        /// <summary>Направление взгляда босса (-1 / +1) — для подклассов.</summary>
        protected int Facing => facing;

        // ───────────────────────── Жизненный цикл ─────────────────────────

        // Awake базы виртуальный, чтобы подкласс мог расширить (LeninBoss: размер/скорость).
        protected virtual void Awake()
        {
            Active = this;

            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) baseSpriteColor = sprite.color;

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                playerTf = playerGo.transform;
                player = playerGo.GetComponent<PlayerController>();
                playerRb = playerGo.GetComponent<Rigidbody2D>();
            }

            if (health != null) health.OnDeath += OnBossDeath;

            fireTimer = fireDelay;
            turboTimer = turboInterval;

            // Блокируем портал выхода, пока босс жив.
            portal = FindAnyObjectByType<ExitPortal>();
            if (portal != null) portal.enabled = false;
        }

        protected virtual void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Мёртв — стоим.
            if (health != null && health.IsDead)
            {
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            // Нет игрока — стоим.
            if (playerTf == null)
            {
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                ApplyFacing();
                return;
            }

            TickTurbo(dt);
            CheckPhase2();
            TickMovement(dt);
            TickContact(dt);
            TickAttack(dt);

            ApplyFacing();
        }

        // ───────────────────────── Турбо ─────────────────────────

        // Каждые turboInterval сек включаем турбо на turboDuration сек (×turboSpeedMult к скорости).
        void TickTurbo(float dt)
        {
            if (turboActive)
            {
                turboLeft -= dt;
                if (turboLeft <= 0f)
                {
                    turboActive = false;
                    turboTimer = turboInterval;
                }
                return;
            }

            turboTimer -= dt;
            if (turboTimer <= 0f)
            {
                turboActive = true;
                turboLeft = turboDuration;
            }
        }

        // ───────────────────────── Фаза 2 ─────────────────────────

        // При падении HP ниже порога — один раз включаем фазу 2 (быстрее, чаще атаки, красная подсветка).
        void CheckPhase2()
        {
            if (phase2Entered) return;
            if (Hp01 >= phase2Threshold) return;

            phase2Entered = true;
            phase = 2;
            fireDelay *= phase2FireMult;
            if (sprite != null) sprite.color = phase2Tint;
        }

        // ───────────────────────── Движение / бой ─────────────────────────

        // Подход к игроку с удержанием preferredRange (с мёртвой зоной). Лицом к игроку.
        protected virtual void TickMovement(float dt)
        {
            float dx = playerTf.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);
            facing = dx >= 0f ? 1 : -1;

            float speed = moveSpeed * CurrentSpeedMult;
            float vx = 0f;

            if (dist > preferredRange + rangeDeadzone)
                vx = facing * speed;            // далеко — подходим
            else if (dist < preferredRange - rangeDeadzone)
                vx = -facing * speed;           // слишком близко — отступаем
            // иначе держим позицию (в мёртвой зоне)

            if (rb != null) rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        // Контактный урон игроку по кулдауну при касании.
        void TickContact(float dt)
        {
            if (contactTimer > 0f) contactTimer -= dt;
            if (contactTimer > 0f) return;

            Vector2 d = playerTf.position - transform.position;
            if (d.sqrMagnitude > contactRange * contactRange) return;

            DealContactDamage(facing >= 0 ? 1 : -1);
            contactTimer = contactCooldown;
        }

        /// <summary>
        /// Нанести игроку контактный урон с отбросом в направлении dir (-1/+1) и экранной отдачей.
        /// Подклассы могут вызывать для своих тяжёлых ударов (передав свой урон/отброс через override полей).
        /// </summary>
        protected void DealContactDamage(int dir)
        {
            if (player == null) return;

            player.Damage(contactDamage);
            if (playerRb != null)
            {
                Vector2 force = new Vector2(dir * contactKnockX, contactKnockUp);
                playerRb.AddForce(force, ForceMode2D.Impulse);
            }
            GameFX.Instance?.Shake(0.12f, 0.18f);
        }

        // ───────────────────────── Атака + телеграф ─────────────────────────

        // Таймер атаки: по истечении fireDelay запускаем цикл телеграф → DoAttack.
        void TickAttack(float dt)
        {
            if (isAttacking) return;

            fireTimer -= dt;
            if (fireTimer <= 0f)
            {
                fireTimer = fireDelay;
                StartCoroutine(AttackRoutine());
            }
        }

        // Цикл атаки: показать телеграф-кольцо ~telegraphTime, затем выполнить DoAttack().
        IEnumerator AttackRoutine()
        {
            isAttacking = true;

            // Телеграф: временное кольцо у босса (null-safe, без префабов) + лёгкая подсветка спрайта.
            GameObject ring = SpawnTelegraphRing();
            Color flash = phase == 2 ? Color.red : telegraphColor;
            Color hold = sprite != null ? sprite.color : Color.white;
            if (sprite != null) sprite.color = Color.Lerp(hold, flash, 0.6f);

            float t = 0f;
            while (t < telegraphTime)
            {
                if (health != null && health.IsDead) break;
                t += Time.deltaTime;
                yield return null;
            }

            if (ring != null) Destroy(ring);
            if (sprite != null) sprite.color = hold;

            // Если умер во время телеграфа — атаку не выполняем.
            if (health == null || !health.IsDead)
                DoAttack();

            isAttacking = false;
        }

        // Создаёт временное кольцо-телеграф из дочернего LineRenderer (окружность). Без префабов.
        GameObject SpawnTelegraphRing()
        {
            var go = new GameObject("BossTelegraphRing");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            var lr = go.AddComponent<LineRenderer>();
            const int seg = 24;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = seg;
            lr.startWidth = 0.06f;
            lr.endWidth = 0.06f;
            lr.numCapVertices = 2;
            lr.sortingOrder = 5;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = telegraphColor;
            lr.endColor = telegraphColor;

            for (int i = 0; i < seg; i++)
            {
                float a = (i / (float)seg) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * telegraphRadius, Mathf.Sin(a) * telegraphRadius, 0f));
            }
            return go;
        }

        /// <summary>
        /// Атака босса (вызывается после телеграфа). База — простой «залп»: урон игроку,
        /// если он в радиусе attackRange. Подклассы переопределяют под свою механику.
        /// </summary>
        protected virtual void DoAttack()
        {
            if (player == null || playerTf == null) return;

            Vector2 d = playerTf.position - transform.position;
            if (d.sqrMagnitude <= attackRange * attackRange)
            {
                player.Damage(attackDamage);
                int dir = d.x >= 0f ? 1 : -1;
                if (playerRb != null)
                    playerRb.AddForce(new Vector2(dir * contactKnockX * 0.5f, contactKnockUp * 0.5f), ForceMode2D.Impulse);
            }
        }

        // ───────────────────────── Смерть ─────────────────────────

        // Реакция на смерть Health: финальная фаза, разблокировка портала, событие, эффекты.
        void OnBossDeath()
        {
            if (defeated) return;
            defeated = true;

            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // Разблокируем выход из биома.
            if (portal != null) portal.enabled = true;

            OnDefeated?.Invoke();
            Active = null;

            GameFX.Instance?.Shake(deathShakeDur, deathShakeAmp);
            GameFX.Instance?.SpawnGibs(transform.position, deathGibColor, deathGibs);
            // Health сам уничтожит объект после OnDeath.
        }

        protected virtual void OnDestroy()
        {
            if (Active == this) Active = null;
            if (health != null) health.OnDeath -= OnBossDeath;
        }

        // ───────────────────────── Утилиты ─────────────────────────

        // Поворот спрайта. Спрайт по умолчанию смотрит вправо (flipX=false → вправо).
        protected void ApplyFacing()
        {
            if (sprite != null)
                sprite.flipX = facing < 0;
        }

        // ───────────────────────── IBiomeScalable ─────────────────────────

        /// <summary>Масштаб босса по биому/уровню: скорость, контактный урон, максимальный HP.</summary>
        public virtual void ApplyBiomeScale(int biome, int levelBoost)
        {
            moveSpeed *= BiomeScaling.BossSpeed(biome, levelBoost);
            contactDamage *= BiomeScaling.BossDmg(biome, levelBoost);
            attackDamage *= BiomeScaling.BossDmg(biome, levelBoost);
            if (health != null)
                health.ScaleMaxHp(BiomeScaling.BossHp(biome, levelBoost));
        }
    }
}
