using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Обычный босс биома (грибной/древесный/песчаный/болотный/заводской) на базе
    /// <see cref="BossController"/>. Вся общая логика (подход, фаза 2, турбо, телеграф,
    /// блок портала) — в базе; здесь переопределена атака: после телеграфа выпускает
    /// веер снарядов <see cref="EnemyProjectile"/> в игрока. Отличия боссов — спрайт,
    /// HP, цвет и параметры залпа в инспекторе/префабе. Ленин — отдельный подкласс (таран).
    /// </summary>
    public class MoldBoss : BossController
    {
        [Header("Босс-стрелок: залп")]
        [SerializeField] int pellets = 3;
        [SerializeField] float spread = 0.18f;       // разброс веера, радианы
        [SerializeField] float shotSpeed = 12f;
        [SerializeField] float shotRange = 16f;
        [SerializeField] Color shotColor = new Color(0.6f, 1f, 0.4f, 1f);

        protected override void DoAttack()
        {
            if (playerTf == null) return;

            Vector2 origin = (Vector2)transform.position + new Vector2(Facing * 0.8f, 0.4f);
            Vector2 baseDir = ((Vector2)playerTf.position - origin).normalized;
            int n = Mathf.Max(1, pellets);
            for (int i = 0; i < n; i++)
            {
                float ang = (n > 1) ? (i - (n - 1) * 0.5f) * spread : 0f;
                Vector2 dir = Rotate(baseDir, ang);
                EnemyProjectile.Spawn(origin, dir, shotSpeed, attackDamage, shotRange, shotColor);
            }
            GameFX.Instance?.Shake(0.06f, 0.08f);
        }

        static Vector2 Rotate(Vector2 v, float r)
        {
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
