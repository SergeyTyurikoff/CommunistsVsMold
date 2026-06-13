using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Простой ИИ врага-зомби: патруль → агр → подход → ближний удар.
    /// Вешается на объект-зомби (рядом должны быть Rigidbody2D, Collider2D, Health).
    /// Движение по X управляется через rb.linearVelocity, Y не трогаем (гравитация).
    /// Игрок ищется по тегу "Player"; урон наносится через PlayerController.Damage.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ZombieAI : MonoBehaviour, IBiomeScalable
    {
        [Header("Движение")]
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] float patrolHalfWidth = 3f; // полуширина зоны патруля от стартовой X

        [Header("Агр / ближний удар")]
        [SerializeField] float detectRange = 8f;
        [SerializeField] float meleeRange = 1f;
        [SerializeField] float meleeDamage = 11f;
        [SerializeField] float meleeCooldown = 0.9f;

        Rigidbody2D rb;
        Health health;
        Transform playerTf;
        PlayerController pc;
        SpriteRenderer sprite;

        float startX;
        int facing = -1;
        float meleeCdTimer;

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
            if (meleeCdTimer > 0f) meleeCdTimer -= dt;

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
                // АГР: двигаемся к игроку.
                facing = dx >= 0f ? 1 : -1;
                rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);

                // Ближний удар по кулдауну.
                if (dist <= meleeRange && meleeCdTimer <= 0f)
                {
                    if (pc != null) pc.Damage(meleeDamage);
                    meleeCdTimer = meleeCooldown;
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

        public void ApplyBiomeScale(int biome, int boost)
        {
            moveSpeed *= BiomeScaling.EnemySpeed(biome, boost);
            meleeDamage *= BiomeScaling.EnemyDmg(biome, boost);
            GetComponent<Health>()?.ScaleMaxHp(BiomeScaling.EnemyHp(biome, boost));
        }
    }
}
