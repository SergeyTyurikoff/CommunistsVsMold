using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// ИИ врага-«газовика» (gasman). PORT_SPEC §5: только ближняя дистанция
    /// (детект ~8u, держит дистанцию engage ~5u / hold ~2.5u): подходит к игроку,
    /// удерживает зазор и по кулдауну ставит облако газа (GasCloud), которое замедляет игрока (×0.5).
    /// Также наносит небольшой контактный урон, если игрок вплотную.
    /// Вешается на объект-газовика (рядом нужны Rigidbody2D, Collider2D, Health).
    /// Движение по X через rb.linearVelocity, Y не трогаем (гравитация).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class GasmanAI : MonoBehaviour, IBiomeScalable
    {
        [Header("Движение")]
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] float patrolHalfWidth = 3f; // полуширина зоны патруля от стартовой X

        [Header("Агр / дистанции")]
        [SerializeField] float detectRange = 8f;   // радиус детекта игрока
        [SerializeField] float engageRange = 5f;   // ~155px: в пределах — атакует газом
        [SerializeField] float holdRange = 2.5f;   // ~80px: ближе — отходит назад
        [SerializeField] float meleeRange = 1f;    // вплотную — контактный урон

        [Header("Контактный урон")]
        [SerializeField] float contactDamage = 6f;
        [SerializeField] float contactCooldown = 1f;

        [Header("Газовая атака")]
        [SerializeField] float gasCooldown = 2.5f; // откат между постановками облака
        [SerializeField] float cloudRadius = 2f;   // ~64px радиус облака
        [SerializeField] float cloudLife = 3.2f;   // ~190f время жизни облака
        [SerializeField] float cloudDrift = 0f;    // дрейф облака по X (0 = неподвижно)
        [SerializeField] float gasSpawnAhead = 0.5f; // насколько перед собой ставить облако, если игрок далеко

        // Сила замедления газа фиксирована (×0.5), биомом не масштабируется.
        const float GasSlowMult = 0.5f;

        Rigidbody2D rb;
        Health health;
        Transform playerTf;
        PlayerController pc;
        SpriteRenderer sprite;

        float startX;
        int facing = -1;
        float contactCdTimer;
        float gasCdTimer;

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
            if (gasCdTimer > 0f) gasCdTimer -= dt;

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
                // АГР: всегда смотрим на игрока.
                facing = dx >= 0f ? 1 : -1;

                // Удержание дистанции (kiting):
                if (dist < holdRange)
                {
                    // Игрок слишком близко — отходим назад (от игрока).
                    rb.linearVelocity = new Vector2(-facing * moveSpeed, rb.linearVelocity.y);
                }
                else if (dist <= engageRange)
                {
                    // В зоне поражения газом — держим позицию (медленно/стоим).
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                else
                {
                    // Между engage и detect — приближаемся к игроку.
                    rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);
                }

                // Газовая атака: игрок в зоне engage и откат прошёл.
                if (dist <= engageRange && gasCdTimer <= 0f)
                {
                    SpawnGas();
                    gasCdTimer = gasCooldown;
                }

                // Контактный урон, если игрок вплотную.
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

        // Ставит облако газа у позиции игрока; если игрок дальше engage — чуть перед собой по facing.
        void SpawnGas()
        {
            Vector3 pos;
            if (playerTf != null)
                pos = new Vector3(playerTf.position.x, playerTf.position.y, transform.position.z);
            else
                pos = transform.position + new Vector3(facing * gasSpawnAhead, 0f, 0f);

            GasCloud.Spawn(pos, cloudRadius, cloudLife, GasSlowMult, cloudDrift);
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
        /// Масштабирование под биом/уровень. Скорость и контактный урон масштабируются,
        /// HP — через Health.ScaleMaxHp. Сила замедления газа фиксирована (×0.5).
        /// </summary>
        public void ApplyBiomeScale(int biome, int levelBoost)
        {
            moveSpeed *= BiomeScaling.EnemySpeed(biome, levelBoost);
            contactDamage *= BiomeScaling.EnemyDmg(biome, levelBoost);
            GetComponent<Health>()?.ScaleMaxHp(BiomeScaling.EnemyHp(biome, levelBoost));
        }
    }
}
