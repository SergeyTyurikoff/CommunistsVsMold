using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Бегун (runner, PORT_SPEC §5): быстрый ближний; прыгает к игроку (как и зомби —
    /// только они делают боевые прыжки). Патруль → агр → подход с прыжками → ближний удар.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RunnerAI : MonoBehaviour, IBiomeScalable
    {
        [Header("Движение")]
        [SerializeField] float moveSpeed = 4.5f;
        [SerializeField] float patrolHalfWidth = 3f;

        [Header("Агр / удар")]
        [SerializeField] float detectRange = 11f;
        [SerializeField] float meleeRange = 1f;
        [SerializeField] float meleeDamage = 9f;
        [SerializeField] float meleeCooldown = 0.7f;

        [Header("Прыжок к игроку")]
        [SerializeField] float jumpForce = 9f;
        [SerializeField] float jumpCooldown = 1.4f;

        Rigidbody2D rb; Health health; Transform playerTf; PlayerController pc; SpriteRenderer sprite;
        float startX; int facing = -1; float meleeCd, jumpCd;

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
            if (jumpCd > 0f) jumpCd -= dt;
            if (health != null && health.IsDead) return;

            if (playerTf == null) { Patrol(); Face(); return; }

            float dx = playerTf.position.x - transform.position.x;
            float dy = playerTf.position.y - transform.position.y;
            float dist = Mathf.Abs(dx);

            if (dist <= detectRange)
            {
                facing = dx >= 0f ? 1 : -1;
                rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);

                // Прыжок: к игроку выше или для сближения (только когда «на земле»).
                if (jumpCd <= 0f && Mathf.Abs(rb.linearVelocity.y) < 0.05f && (dy > 1f || dist > 2f))
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    jumpCd = jumpCooldown;
                }
                if (dist <= meleeRange && meleeCd <= 0f) { if (pc != null) pc.Damage(meleeDamage); meleeCd = meleeCooldown; }
            }
            else Patrol();
            Face();
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
