using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Камикадзе (PORT_SPEC §5): подбегает и взрывается при сближении или низком HP.
    /// Перед взрывом мигает (фитиль). Взрыв — урон по площади + отброс игрока.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class KamikazeAI : MonoBehaviour, IBiomeScalable
    {
        [Header("Движение")]
        [SerializeField] float moveSpeed = 3.5f;
        [SerializeField] float patrolHalfWidth = 3f;
        [SerializeField] float detectRange = 11f;

        [Header("Взрыв")]
        [SerializeField] float fuseRange = 1.6f;     // дистанция поджига фитиля
        [SerializeField] float lowHpFrac = 0.3f;     // взрыв при низком HP
        [SerializeField] float fuseTime = 0.7f;      // мигание перед взрывом
        [SerializeField] float blastRadius = 3f;
        [SerializeField] float blastDamage = 50f;
        [SerializeField] float blastKnock = 14f;

        Rigidbody2D rb; Health health; Transform playerTf; PlayerController pc; Rigidbody2D playerRb; SpriteRenderer sprite;
        float startX; int facing = -1; bool fusing; float fuseT, blink;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            startX = transform.position.x;
            var g = GameObject.FindWithTag("Player");
            if (g != null) { playerTf = g.transform; pc = g.GetComponent<PlayerController>(); playerRb = g.GetComponent<Rigidbody2D>(); }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (health != null && health.IsDead) return;

            if (fusing) { TickFuse(dt); return; }

            // Взрыв при низком HP.
            if (health != null && health.MaxHp > 0f && health.Hp / health.MaxHp <= lowHpFrac) { StartFuse(); return; }

            if (playerTf == null) { Patrol(); Face(); return; }

            float dx = playerTf.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);
            if (dist <= detectRange || GunfireAlarm.Hears(transform.position))
            {
                facing = dx >= 0f ? 1 : -1;
                rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);
                if (Vector2.Distance(transform.position, playerTf.position) <= fuseRange) StartFuse();
            }
            else Patrol();
            Face();
        }

        void StartFuse()
        {
            if (fusing) return;
            fusing = true; fuseT = fuseTime;
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        void TickFuse(float dt)
        {
            fuseT -= dt; blink += dt;
            if (sprite != null) sprite.color = (Mathf.FloorToInt(blink * 12f) % 2 == 0) ? Color.white : Color.red;
            if (fuseT <= 0f) Explode();
        }

        void Explode()
        {
            if (playerTf != null && pc != null)
            {
                float d = Vector2.Distance(transform.position, playerTf.position);
                if (d <= blastRadius)
                {
                    pc.Damage(blastDamage);
                    if (playerRb != null)
                    {
                        float sx = Mathf.Sign(playerTf.position.x - transform.position.x);
                        playerRb.AddForce(new Vector2(sx * blastKnock, blastKnock * 0.5f), ForceMode2D.Impulse);
                    }
                }
            }
            GameFX.Instance?.Shake(0.3f, 0.4f);
            GameFX.Instance?.HitStop(0.06f);
            GameFX.Instance?.SpawnGibs(transform.position, new Color(1f, 0.5f, 0.1f), 16);
            if (health != null) health.TakeDamage(99999f, Vector2.zero);
            else Destroy(gameObject);
        }

        void Patrol()
        {
            float x = transform.position.x;
            if (x <= startX - patrolHalfWidth) facing = 1;
            else if (x >= startX + patrolHalfWidth) facing = -1;
            rb.linearVelocity = new Vector2(facing * moveSpeed * 0.5f, rb.linearVelocity.y);
        }

        void Face() { if (sprite != null && !fusing) sprite.flipX = facing < 0; }

        public void ApplyBiomeScale(int biome, int boost)
        {
            moveSpeed *= BiomeScaling.EnemySpeed(biome, boost);
            blastDamage *= BiomeScaling.EnemyDmg(biome, boost);
            GetComponent<Health>()?.ScaleMaxHp(BiomeScaling.EnemyHp(biome, boost));
        }
    }
}
