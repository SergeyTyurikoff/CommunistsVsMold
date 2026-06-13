using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// ИИ врага-снайпера (PORT_SPEC §5): дальнобой, длинный прицел-разгон перед выстрелом.
    /// Почти неподвижен — держит максимальную дистанцию: если игрок ближе tooClose, медленно отходит,
    /// иначе стоит на месте. Всегда поворачивается лицом к игроку.
    /// Цикл огня: кулдаун (fireCooldown) → прицел-разгон (scopeTime) с телеграфом-лучом → выстрел EnemyProjectile.
    /// Движение по X через rb.linearVelocity, Y не трогаем (гравитация).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SniperAI : MonoBehaviour, IBiomeScalable
    {
        [Header("Движение / дистанция")]
        [Tooltip("Скорость медленного отступления, юнитов/с.")]
        [SerializeField] float moveSpeed = 1.2f;
        [Tooltip("Если игрок ближе этой дистанции — снайпер отходит и не стреляет.")]
        [SerializeField] float tooClose = 6f;

        [Header("Стрельба")]
        [Tooltip("Дальность открытия огня, юнитов (640px ≈ 20u).")]
        [SerializeField] float fireRange = 20f;
        [Tooltip("Кулдаун между циклами огня, секунд.")]
        [SerializeField] float fireCooldown = 3f;
        [Tooltip("Длительность прицела-разгона с телеграфом перед выстрелом, секунд (80f@60fps ≈ 1.33с).")]
        [SerializeField] float scopeTime = 1.33f;
        [Tooltip("Урон от выстрела (высокий).")]
        [SerializeField] float shotDamage = 30f;
        [Tooltip("Скорость пули, юнитов/с (большая).")]
        [SerializeField] float shotSpeed = 22f;
        [Tooltip("Дальность полёта пули, юнитов.")]
        [SerializeField] float shotRange = 24f;
        [Tooltip("Цвет пули.")]
        [SerializeField] Color shotColor = new Color(1f, 0.25f, 0.15f, 1f);
        [Tooltip("Локальное смещение точки выстрела (дула) относительно снайпера.")]
        [SerializeField] Vector2 muzzleOffset = new Vector2(0f, 0.3f);

        [Header("Телеграф (луч прицела)")]
        [Tooltip("Цвет луча в начале разгона.")]
        [SerializeField] Color scopeStartColor = Color.yellow;
        [Tooltip("Цвет луча к моменту выстрела.")]
        [SerializeField] Color scopeEndColor = Color.red;
        [Tooltip("Толщина луча прицела, юнитов.")]
        [SerializeField] float scopeLineWidth = 0.04f;

        [Header("Отдача")]
        [Tooltip("Сила тряски экрана при выстреле.")]
        [SerializeField] float shotShake = 0.15f;
        [Tooltip("Длительность тряски экрана при выстреле, секунд.")]
        [SerializeField] float shotShakeTime = 0.12f;

        Rigidbody2D rb;
        Health health;
        Transform playerTf;
        PlayerController pc;
        SpriteRenderer sprite;

        LineRenderer scopeLine; // телеграф разгона, создаётся в коде
        int facing = -1;

        float cdTimer;        // отсчёт кулдауна до начала прицеливания
        float scopeTimer;     // отсчёт прицела-разгона
        bool isScoping;       // идёт ли разгон прицела
        Vector2 lockedTarget; // зафиксированная точка прицеливания (без упреждения)

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            sprite = GetComponentInChildren<SpriteRenderer>();

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                playerTf = playerGo.transform;
                pc = playerGo.GetComponent<PlayerController>();
            }

            cdTimer = fireCooldown;
            BuildScopeLine();
        }

        // Создаём дочерний LineRenderer для телеграфа разгона (изначально выключен).
        void BuildScopeLine()
        {
            var lineGo = new GameObject("SniperScopeLine");
            lineGo.transform.SetParent(transform, false);

            scopeLine = lineGo.AddComponent<LineRenderer>();
            scopeLine.positionCount = 2;
            scopeLine.useWorldSpace = true;
            scopeLine.startWidth = scopeLineWidth;
            scopeLine.endWidth = scopeLineWidth;
            scopeLine.numCapVertices = 2;
            scopeLine.sortingOrder = 4;
            // Простой unlit-материал, чтобы цвет рисовался без освещения.
            scopeLine.material = new Material(Shader.Find("Sprites/Default"));
            scopeLine.enabled = false;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Мёртв — стоим и гасим телеграф.
            if (health != null && health.IsDead)
            {
                StopScope();
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            // Нет игрока — стоим.
            if (playerTf == null)
            {
                StopScope();
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                ApplyFacing();
                return;
            }

            float selfX = transform.position.x;
            float dx = playerTf.position.x - selfX;
            float dist = Mathf.Abs(dx);

            // Лицом к игроку всегда.
            facing = dx >= 0f ? 1 : -1;

            // --- Движение: держим максимальную дистанцию ---
            float vx = 0f;
            if (dist < tooClose)
                vx = -facing * moveSpeed; // отходим от игрока
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

            // --- Логика огня ---
            if (isScoping)
            {
                UpdateScoping(dt);
            }
            else
            {
                if (cdTimer > 0f) cdTimer -= dt;

                bool canFire = dist <= fireRange && dist >= tooClose;
                if (canFire && cdTimer <= 0f)
                    BeginScope();
            }

            ApplyFacing();
        }

        // Начало прицела-разгона: фиксируем точку игрока (без упреждения), включаем телеграф.
        void BeginScope()
        {
            isScoping = true;
            scopeTimer = 0f;
            lockedTarget = playerTf.position;

            if (scopeLine != null)
            {
                scopeLine.enabled = true;
                UpdateScopeLine(0f);
            }
        }

        // Прогресс разгона: тянем луч к зафиксированной точке, меняем цвет жёлтый→красный, по завершении стреляем.
        void UpdateScoping(float dt)
        {
            scopeTimer += dt;
            float t = scopeTime > 0f ? Mathf.Clamp01(scopeTimer / scopeTime) : 1f;
            UpdateScopeLine(t);

            if (scopeTimer >= scopeTime)
                Fire();
        }

        // Обновляем геометрию и цвет луча. t: 0 — начало разгона, 1 — момент выстрела.
        void UpdateScopeLine(float t)
        {
            if (scopeLine == null) return;

            Vector3 from = MuzzleWorld();
            scopeLine.SetPosition(0, from);
            scopeLine.SetPosition(1, lockedTarget);

            Color c = Color.Lerp(scopeStartColor, scopeEndColor, t);
            scopeLine.startColor = c;
            scopeLine.endColor = c;
        }

        // Выстрел: создаём снаряд в сторону зафиксированной точки, гасим телеграф, лёгкая тряска, кулдаун.
        void Fire()
        {
            Vector3 muzzle = MuzzleWorld();
            Vector2 dir = (lockedTarget - (Vector2)muzzle);
            if (dir.sqrMagnitude < 0.0001f)
                dir = new Vector2(facing, 0f);

            EnemyProjectile.Spawn(muzzle, dir, shotSpeed, shotDamage, shotRange, shotColor);
            GameFX.Instance?.Shake(shotShake, shotShakeTime);

            StopScope();
            cdTimer = fireCooldown;
        }

        // Сброс состояния прицеливания и выключение телеграфа.
        void StopScope()
        {
            isScoping = false;
            scopeTimer = 0f;
            if (scopeLine != null)
                scopeLine.enabled = false;
        }

        // Мировая позиция дула с учётом направления взгляда.
        Vector3 MuzzleWorld()
        {
            return transform.position + new Vector3(muzzleOffset.x * facing, muzzleOffset.y, 0f);
        }

        // Поворот спрайта. Спрайт по умолчанию смотрит вправо (flipX=false → вправо).
        void ApplyFacing()
        {
            if (sprite != null)
                sprite.flipX = facing < 0;
        }

        /// <summary>Применить масштаб биома: скорость, урон выстрела и максимальный HP.</summary>
        public void ApplyBiomeScale(int biome, int levelBoost)
        {
            moveSpeed *= BiomeScaling.EnemySpeed(biome, levelBoost);
            shotDamage *= BiomeScaling.EnemyDmg(biome, levelBoost);
            if (health != null)
                health.ScaleMaxHp(BiomeScaling.EnemyHp(biome, levelBoost));
        }
    }
}
