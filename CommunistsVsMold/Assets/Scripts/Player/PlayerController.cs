using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kommunisty
{
    /// <summary>
    /// Игрок (Феликс): ходьба/бег, прыжок + двойной прыжок, перекат (Z, i-frames),
    /// турбо (C, тратит здоровье), здоровье ("время"). Числа — из docs/PORT_SPEC.md,
    /// переведены в Unity-единицы (метры/сек), все поля настраиваются в инспекторе.
    /// Ввод — новый Input System (UnityEngine.InputSystem), чтение Keyboard.current с null-guard.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Движение")]
        [SerializeField] float walkSpeed = 6.5f;
        [SerializeField] float runMultiplier = 1.95f;   // PORT_SPEC: бег ×1.95
        [SerializeField] float accel = 60f;             // как быстро набираем скорость
        [SerializeField] float groundFriction = 50f;    // торможение без ввода

        [Header("Прыжок")]
        [SerializeField] float jumpVelocity = 14f;
        [SerializeField] float gravityScale = 3.5f;
        [SerializeField] float fallGravityMult = 1.4f;  // быстрее падать = приятнее
        [SerializeField] bool doubleJumpUnlocked = true; // в игре открывается со 2-го биома
        [SerializeField] float coyoteTime = 0.10f;
        [SerializeField] float jumpBuffer = 0.10f;

        [Header("Перекат (Z) — i-frames, без затрат здоровья")]
        [SerializeField] float dodgeSpeed = 16f;
        [SerializeField] float dodgeDuration = 0.23f;   // ~14 кадров @60
        [SerializeField] float dodgeCooldown = 0.97f;   // ~58 кадров @60

        [Header("Турбо (C) — единственный режим, тратит здоровье")]
        [SerializeField] bool turboUnlocked = false;
        [SerializeField] float turboDuration = 5f;
        [SerializeField] float turboCooldown = 5f;
        [SerializeField] float turboSpeedMult = 1.55f;
        [SerializeField] float turboHealthPerSec = 7.2f; // ~0.12/кадр × 60
        [SerializeField] float weakDuration = 5f;
        [SerializeField] float weakSpeedMult = 0.52f;

        [Header("Здоровье (\"время\")")]
        [SerializeField] float maxHealth = 140f;

        [Header("Смерть / респаун")]
        [SerializeField] float respawnDelay = 1.5f;

        [Header("Проверка земли")]
        [SerializeField] Transform groundCheck;
        [SerializeField] float groundRadius = 0.18f;
        [SerializeField] LayerMask groundMask;

        [Header("Спуск сквозь one-way платформы (S / ↓)")]
        [SerializeField] LayerMask oneWayMask;
        [SerializeField] float dropThroughTime = 0.35f;

        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public int Facing { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public bool IsDodging => dodgeTimer > 0f;
        public bool IsInvulnerable => invulnTimer > 0f;

        Rigidbody2D rb;
        SpriteRenderer sprite;
        Collider2D selfCol;
        int jumpsLeft;
        float coyoteTimer, jumpBufferTimer;
        float dodgeTimer, dodgeCdTimer, dodgeDir;
        float turboTimer, turboCdTimer, weakTimer;
        float invulnTimer;
        float slowTimer, slowMult = 1f;   // газ-замедление (PORT_SPEC §3)

        bool isDead;
        Vector3 spawnPoint;
        float respawnTimer;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            selfCol = GetComponent<Collider2D>();
            rb.freezeRotation = true;
            rb.gravityScale = gravityScale;
            Health = maxHealth;
            spawnPoint = transform.position;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Смерть и респаун обрабатываем раньше обычного ввода.
            if (Health <= 0f && !isDead) Die();

            if (isDead)
            {
                respawnTimer -= dt;
                if (respawnTimer <= 0f)
                {
                    transform.position = spawnPoint;
                    rb.linearVelocity = Vector2.zero;
                    Health = maxHealth;
                    isDead = false;
                    invulnTimer = 0.6f;
                }
                return;
            }

            Tick(ref coyoteTimer, dt);
            Tick(ref jumpBufferTimer, dt);
            Tick(ref dodgeCdTimer, dt);
            Tick(ref invulnTimer, dt);
            Tick(ref slowTimer, dt);

            // Таймеры турбо/слабости
            if (turboTimer > 0f)
            {
                turboTimer -= dt;
                Health -= turboHealthPerSec * dt;
                if (Health <= 0f) { Health = 0f; /* TODO: смерть */ }
                if (turboTimer <= 0f) { weakTimer = weakDuration; turboCdTimer = turboCooldown; }
            }
            else if (turboCdTimer > 0f) turboCdTimer -= dt;
            if (weakTimer > 0f) weakTimer -= dt;

            var kb = Keyboard.current;

            // Буфер прыжка
            if (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
                jumpBufferTimer = jumpBuffer;

            // Турбо
            if (turboUnlocked && kb != null && kb.cKey.wasPressedThisFrame && turboTimer <= 0f && weakTimer <= 0f && turboCdTimer <= 0f && Health > 20f)
                turboTimer = turboDuration;

            // Перекат
            if (kb != null && kb.zKey.wasPressedThisFrame && dodgeTimer <= 0f && dodgeCdTimer <= 0f)
            {
                dodgeDir = HInput() != 0 ? Mathf.Sign(HInput()) : Facing;
                dodgeTimer = dodgeDuration;
                dodgeCdTimer = dodgeCooldown;
                invulnTimer = dodgeDuration;
            }

            // Спуск сквозь one-way платформу (S / ↓)
            if (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) && IsGrounded)
                TryDropThrough();

            // Поворот спрайта
            float h = HInput();
            if (dodgeTimer <= 0f && h != 0) { Facing = (int)Mathf.Sign(h); if (sprite) sprite.flipX = Facing < 0; }
        }

        // Ищет one-way платформу под ногами и временно отключает с ней столкновение.
        void TryDropThrough()
        {
            if (selfCol == null || groundCheck == null) return;
            var hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.3f, oneWayMask);
            if (hit.collider != null)
                StartCoroutine(DropThroughRoutine(hit.collider));
        }

        IEnumerator DropThroughRoutine(Collider2D platCol)
        {
            Physics2D.IgnoreCollision(selfCol, platCol, true);
            yield return new WaitForSeconds(dropThroughTime);
            if (platCol != null && selfCol != null)
                Physics2D.IgnoreCollision(selfCol, platCol, false);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Перекат — фиксированная скорость, без гравитации по горизонтали
            if (dodgeTimer > 0f)
            {
                dodgeTimer -= dt;
                rb.linearVelocity = new Vector2(dodgeDir * dodgeSpeed, 0f);
                return;
            }

            // Земля
            IsGrounded = groundCheck && Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
                jumpsLeft = doubleJumpUnlocked ? 2 : 1;
            }

            // Горизонталь
            var kb = Keyboard.current;
            bool running = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            float speedMult = (running ? runMultiplier : 1f)
                            * (turboTimer > 0f ? turboSpeedMult : 1f)
                            * (weakTimer > 0f ? weakSpeedMult : 1f)
                            * (slowTimer > 0f ? slowMult : 1f);
            float target = HInput() * walkSpeed * speedMult;
            float rate = Mathf.Abs(target) > 0.01f ? accel : groundFriction;
            float vx = Mathf.MoveTowards(rb.linearVelocity.x, target, rate * dt);

            // Прыжок (земля/coyote или двойной)
            float vy = rb.linearVelocity.y;
            if (!isDead && jumpBufferTimer > 0f)
            {
                if (coyoteTimer > 0f) { vy = jumpVelocity; jumpsLeft--; jumpBufferTimer = 0f; coyoteTimer = 0f; AudioManager.Instance?.PlayJump(); }
                else if (jumpsLeft > 0) { vy = jumpVelocity; jumpsLeft--; jumpBufferTimer = 0f; AudioManager.Instance?.PlayJump(); }
            }

            // Усиленная гравитация на падении
            rb.gravityScale = vy < 0f ? gravityScale * fallGravityMult : gravityScale;

            rb.linearVelocity = new Vector2(vx, vy);
        }

        float HInput()
        {
            if (isDead) return 0f;
            var kb = Keyboard.current;
            if (kb == null) return 0f;
            float x = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            return x;
        }

        public void Damage(float amount)
        {
            if (IsInvulnerable) return;
            Health = Mathf.Max(0f, Health - amount);
            GameFX.Instance?.Shake(0.12f, 0.12f);
            AudioManager.Instance?.PlayPlayerHit();
            // TODO: откидывание/неуязвимость после удара
        }

        // Смерть игрока: фиксируем состояние, запускаем таймер респауна и фидбэк.
        void Die()
        {
            isDead = true;
            respawnTimer = respawnDelay;
            rb.linearVelocity = Vector2.zero;
            GameFX.Instance?.Shake(0.3f, 0.4f);
            GameFX.Instance?.HitStop(0.12f);
            AudioManager.Instance?.PlayPlayerDown();
        }

        public void Heal(float amount) => Health = Mathf.Min(maxHealth, Health + amount);

        /// <summary>Поднять максимум здоровья (прокачка). По умолчанию доливает на ту же величину.</summary>
        public void AddMaxHealth(float delta, bool heal = true)
        {
            if (delta <= 0f) return;
            maxHealth += delta;
            if (heal) Health = Mathf.Min(maxHealth, Health + delta);
        }

        /// <summary>Газ-замедление: множитель скорости (×0.5) на duration сек. Повторные вызовы
        /// (пока игрок в облаке) рефрешат таймер. Урон НЕ наносит (PORT_SPEC §3).</summary>
        public void ApplySlow(float mult, float duration)
        {
            slowMult = Mathf.Clamp(mult, 0.05f, 1f);
            if (duration > slowTimer) slowTimer = duration;
        }

        /// <summary>Точка возрождения (чекпойнт). Ставится BiomeManager при чекпойнте/смене биома.</summary>
        public void SetRespawnPoint(Vector3 p) => spawnPoint = p;

        /// <summary>Текущая точка возрождения игрока.</summary>
        public Vector3 RespawnPoint => spawnPoint;

        static void Tick(ref float t, float dt) { if (t > 0f) t -= dt; }

        void OnDrawGizmosSelected()
        {
            if (!groundCheck) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}
