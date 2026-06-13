using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Минибосс (PORT_SPEC §5): элитный ближний враг с периодическим щитом, поглощающим
    /// один удар. Реализует <see cref="IDamageFilter"/> — при активном щите входящий урон
    /// гасится в 0 (щит уходит на перезарядку). Визуальный телеграф: голубоватый оттенок,
    /// пока щит готов.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MinibossAI : MonoBehaviour, IBiomeScalable, IDamageFilter
    {
        [Header("Движение / удар")]
        [SerializeField] float moveSpeed = 2.2f;
        [SerializeField] float patrolHalfWidth = 3f;
        [SerializeField] float detectRange = 12f;
        [SerializeField] float meleeRange = 1.4f;
        [SerializeField] float meleeDamage = 18f;
        [SerializeField] float meleeCooldown = 1f;

        [Header("Щит (поглощает 1 удар)")]
        [SerializeField] float shieldRecharge = 4f;

        Rigidbody2D rb; Health health; Transform playerTf; PlayerController pc; SpriteRenderer sprite;
        float startX; int facing = -1; float meleeCd;
        bool shieldReady = true; float shieldCd;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            startX = transform.position.x;
            var g = GameObject.FindWithTag("Player");
            if (g != null) { playerTf = g.transform; pc = g.GetComponent<PlayerController>(); }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (meleeCd > 0f) meleeCd -= dt;
            if (!shieldReady) { shieldCd -= dt; if (shieldCd <= 0f) shieldReady = true; }
            if (health != null && health.IsDead) return;

            if (playerTf != null)
            {
                float dx = playerTf.position.x - transform.position.x;
                float dist = Mathf.Abs(dx);
                if (dist <= detectRange)
                {
                    facing = dx >= 0f ? 1 : -1;
                    rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);
                    if (dist <= meleeRange && meleeCd <= 0f) { if (pc != null) pc.Damage(meleeDamage); meleeCd = meleeCooldown; }
                }
                else Patrol();
            }
            else Patrol();

            // Телеграф щита: голубоватый, пока щит готов; белый — пока на перезарядке.
            if (sprite != null) sprite.color = shieldReady ? new Color(0.7f, 0.82f, 1f) : Color.white;
            Face();
        }

        /// <summary>Щит поглощает один удар целиком, затем уходит на перезарядку.</summary>
        public float ModifyDamage(float dmg, Vector2 kb)
        {
            if (shieldReady) { shieldReady = false; shieldCd = shieldRecharge; return 0f; }
            return dmg;
        }

        void Patrol()
        {
            float x = transform.position.x;
            if (x <= startX - patrolHalfWidth) facing = 1;
            else if (x >= startX + patrolHalfWidth) facing = -1;
            rb.linearVelocity = new Vector2(facing * moveSpeed * 0.5f, rb.linearVelocity.y);
        }

        void Face() { if (sprite != null) sprite.flipX = facing < 0; }

        public void ApplyBiomeScale(int biome, int boost)
        {
            moveSpeed *= BiomeScaling.EnemySpeed(biome, boost);
            meleeDamage *= BiomeScaling.EnemyDmg(biome, boost);
            GetComponent<Health>()?.ScaleMaxHp(BiomeScaling.EnemyHp(biome, boost));
        }
    }
}
