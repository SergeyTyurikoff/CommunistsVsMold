using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Сабельщик (sabreur, PORT_SPEC §5): подходит и делает выпад шашкой (рывок вперёд
    /// с уроном). Между выпадами — откат. Боевых прыжков не делает.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SabreurAI : MonoBehaviour, IBiomeScalable
    {
        [Header("Движение")]
        [SerializeField] float moveSpeed = 2.5f;
        [SerializeField] float patrolHalfWidth = 3f;
        [SerializeField] float detectRange = 10f;

        [Header("Выпад шашкой")]
        [SerializeField] float lungeRange = 2.6f;
        [SerializeField] float lungeSpeed = 11f;
        [SerializeField] float lungeTime = 0.18f;
        [SerializeField] float lungeCooldown = 1.6f;
        [SerializeField] float hitRange = 1.4f;
        [SerializeField] float lungeDamage = 16f;

        Rigidbody2D rb; Health health; Transform playerTf; PlayerController pc; SpriteRenderer sprite;
        float startX; int facing = -1; float lungeCd, lungeT; bool lunging, hitThisLunge;

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
            if (lungeCd > 0f) lungeCd -= dt;
            if (health != null && health.IsDead) return;

            if (lunging)
            {
                lungeT -= dt;
                rb.linearVelocity = new Vector2(facing * lungeSpeed, rb.linearVelocity.y);
                if (!hitThisLunge && playerTf != null && Vector2.Distance(transform.position, playerTf.position) <= hitRange)
                { if (pc != null) pc.Damage(lungeDamage); hitThisLunge = true; }
                if (lungeT <= 0f) lunging = false;
                Face();
                return;
            }

            if (playerTf == null) { Patrol(); Face(); return; }

            float dx = playerTf.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);
            if (dist <= detectRange)
            {
                facing = dx >= 0f ? 1 : -1;
                if (dist <= lungeRange && lungeCd <= 0f)
                {
                    lunging = true; lungeT = lungeTime; lungeCd = lungeCooldown; hitThisLunge = false;
                    GameFX.Instance?.Shake(0.05f, 0.05f);
                }
                else rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);
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
            lungeSpeed *= BiomeScaling.EnemySpeed(biome, boost);
            lungeDamage *= BiomeScaling.EnemyDmg(biome, boost);
            GetComponent<Health>()?.ScaleMaxHp(BiomeScaling.EnemyHp(biome, boost));
        }
    }
}
