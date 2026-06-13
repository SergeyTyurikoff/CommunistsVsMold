using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Комбо-множитель урона (PORT_SPEC §3). Каждое попадание игрока (через глобальное
    /// <see cref="Health.OnAnyDamaged"/>) увеличивает счётчик и обновляет окно. По порогам
    /// попаданий [1,4,8,14] множитель урона = [1, 1.5, 2.2, 3.5]. Окно ~130 кадров (2.17с);
    /// по истечении комбо сбрасывается. Висит на игроке; читается WeaponController.
    /// </summary>
    public class ComboTracker : MonoBehaviour
    {
        [SerializeField] float window = 2.17f;                 // 130 кадров @60
        [SerializeField] int[] thresholds = { 1, 4, 8, 14 };
        [SerializeField] float[] mults = { 1f, 1.5f, 2.2f, 3.5f };

        int hits;
        float timer;

        public int Hits => hits;
        public float Multiplier { get; private set; } = 1f;

        void OnEnable() => Health.OnAnyDamaged += OnHit;
        void OnDisable() => Health.OnAnyDamaged -= OnHit;

        void OnHit(Health h, float dmg)
        {
            hits++;
            timer = window;
            Recalc();
        }

        void Update()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f) { hits = 0; Multiplier = 1f; }
            }
        }

        void Recalc()
        {
            float m = 1f;
            for (int i = 0; i < thresholds.Length; i++)
                if (hits >= thresholds[i]) m = mults[Mathf.Min(i, mults.Length - 1)];
            Multiplier = m;
        }
    }
}
