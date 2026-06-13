using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Стрелок (PORT_SPEC §5): держит дистанцию и стреляет во врага <see cref="EnemyProjectile"/>.
    /// Универсален параметрами: одиночный выстрел (rifleman) / веер из pellets (gunner),
    /// либо неподвижная турель с частым огнём (maxim — moveSpeed = 0). Боевых прыжков не делает.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ShooterAI : MonoBehaviour, IBiomeScalable
    {
        [Header("Движение (0 = неподвижная турель)")]
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] float patrolHalfWidth = 3f;
        [SerializeField] float detectRange = 14f;
        [SerializeField] float fireRange = 12f;
        [SerializeField] float tooClose = 4f;     // ближе — отходит (держит дистанцию)

        [Header("Огонь")]
        [SerializeField] float fireCooldown = 1.2f;
        [SerializeField] int pellets = 1;          // gunner/maxim = 3
        [SerializeField] float spread = 0.12f;     // разброс веера, радианы
        [SerializeField] float shotDamage = 10f;
        [SerializeField] float shotSpeed = 16f;
        [SerializeField] float shotRange = 16f;
        [SerializeField] Color shotColor = new Color(1f, 0.85f, 0.3f);

        Rigidbody2D rb; Health health; Transform playerTf; PlayerController pc; SpriteRenderer sprite;
        float startX; int facing = -1; float fireCd;

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
            if (fireCd > 0f) fireCd -= dt;
            if (health != null && health.IsDead) return;

            bool turret = moveSpeed <= 0.01f;

            if (playerTf == null)
            {
                if (turret) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                else Patrol();
                Face();
                return;
            }

            float dx = playerTf.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);

            if (dist <= detectRange || GunfireAlarm.Hears(transform.position))
            {
                facing = dx >= 0f ? 1 : -1;
                if (turret) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                else if (dist < tooClose) rb.linearVelocity = new Vector2(-facing * moveSpeed, rb.linearVelocity.y);
                else if (dist > fireRange) rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);
                else rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                if (dist <= fireRange && fireCd <= 0f) { Fire(); fireCd = fireCooldown; }
            }
            else
            {
                if (turret) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                else Patrol();
            }
            Face();
        }

        void Fire()
        {
            if (playerTf == null) return;
            Vector2 origin = (Vector2)transform.position + new Vector2(facing * 0.6f, 0.4f);
            Vector2 baseDir = ((Vector2)playerTf.position - origin).normalized;
            int n = Mathf.Max(1, pellets);
            for (int i = 0; i < n; i++)
            {
                float ang = (n > 1) ? (i - (n - 1) * 0.5f) * spread : 0f;
                Vector2 dir = Rotate(baseDir, ang);
                EnemyProjectile.Spawn(origin, dir, shotSpeed, shotDamage, shotRange, shotColor);
            }
        }

        static Vector2 Rotate(Vector2 v, float r)
        {
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
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
            if (moveSpeed > 0.01f) moveSpeed *= BiomeScaling.EnemySpeed(biome, boost);
            shotDamage *= BiomeScaling.EnemyDmg(biome, boost);
            GetComponent<Health>()?.ScaleMaxHp(BiomeScaling.EnemyHp(biome, boost));
        }
    }
}
