using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// ИИ врага-щитоносца: медленно наступает на игрока, держа фронтальный щит.
    /// Вешается на объект-щитоносца (рядом должны быть Rigidbody2D, Collider2D, Health).
    /// Движение по X управляется через rb.linearVelocity, Y не трогаем (гравитация).
    /// Игрок ищется по тегу "Player"; урон наносится через PlayerController.Damage.
    ///
    /// Щит — реализация IDamageFilter: урон, прилетевший СПЕРЕДИ (толкает щитоносца
    /// назад, т.е. знак knockback.x противоположен facing), режется множителем frontMul.
    /// Health сам прогоняет входящий урон через этот фильтр.
    ///
    /// Биом-масштабирование — IBiomeScalable: скорость/контактный урон/HP. frontMul не масштабируется.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ShielderAI : MonoBehaviour, IBiomeScalable, IDamageFilter
    {
        [Header("Движение")]
        [SerializeField] float moveSpeed = 1.5f;       // медленнее зомби
        [SerializeField] float patrolHalfWidth = 3f;   // полуширина зоны патруля от стартовой X

        [Header("Агр / детект")]
        [SerializeField] float detectRange = 10f;

        [Header("Контактный удар")]
        [SerializeField] float meleeRange = 1.1f;
        [SerializeField] float contactDamage = 10f;
        [SerializeField] float contactCooldown = 0.9f;

        [Header("Щит")]
        [SerializeField] float frontMul = 0.12f;       // множитель урона при фронтальном попадании

        Rigidbody2D rb;
        Health health;
        Transform playerTf;
        PlayerController pc;
        SpriteRenderer sprite;

        float startX;
        int facing = -1;                                // -1 влево / +1 вправо; это и есть направление щита
        float contactCdTimer;

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
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (contactCdTimer > 0f) contactCdTimer -= dt;

            // Мёртв — ничего не делаем.
            if (health != null && health.IsDead)
                return;

            // Нет игрока — только патруль.
            if (playerTf == null)
            {
                Patrol();
                ApplyFacing();
                return;
            }

            float selfX = transform.position.x;
            float dx = playerTf.position.x - selfX;
            float dist = Mathf.Abs(dx);

            if (dist <= detectRange)
            {
                // АГР: поворачиваемся лицом к игроку (щитом к нему) и медленно наступаем.
                facing = dx >= 0f ? 1 : -1;
                rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);

                // Контактный удар по кулдауну.
                if (dist <= meleeRange && contactCdTimer <= 0f)
                {
                    if (pc != null) pc.Damage(contactDamage);
                    contactCdTimer = contactCooldown;
                }
            }
            else
            {
                Patrol();
            }

            ApplyFacing();
        }

        // Патруль между [startX - patrolHalfWidth; startX + patrolHalfWidth], медленнее агра.
        void Patrol()
        {
            float selfX = transform.position.x;
            if (selfX <= startX - patrolHalfWidth) facing = 1;
            else if (selfX >= startX + patrolHalfWidth) facing = -1;

            rb.linearVelocity = new Vector2(facing * moveSpeed * 0.5f, rb.linearVelocity.y);
        }

        // Поворот спрайта. Спрайт по умолчанию смотрит вправо (flipX=false → вправо).
        void ApplyFacing()
        {
            if (sprite != null)
                sprite.flipX = facing < 0;
        }

        /// <summary>
        /// Фильтр входящего урона (щит). Удар считается фронтальным, если он толкает
        /// щитоносца назад: знак knockback.x противоположен направлению facing.
        /// Фронтальный урон режется множителем frontMul. Удары сзади/сверху (kb.x≈0) —
        /// полный урон.
        /// </summary>
        public float ModifyDamage(float dmg, Vector2 kb)
        {
            bool frontal = Mathf.Abs(kb.x) > 0.001f && Mathf.Sign(kb.x) == -facing;
            return frontal ? dmg * frontMul : dmg;
        }

        /// <summary>
        /// Биом-масштабирование: скорость, контактный урон и HP растут от биома/уровня.
        /// frontMul (стойкость щита) намеренно не масштабируется.
        /// </summary>
        public void ApplyBiomeScale(int biome, int levelBoost)
        {
            moveSpeed *= BiomeScaling.EnemySpeed(biome, levelBoost);
            contactDamage *= BiomeScaling.EnemyDmg(biome, levelBoost);
            if (health != null)
                health.ScaleMaxHp(BiomeScaling.EnemyHp(biome, levelBoost));
        }
    }
}
