using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Прокачка героя (PORT_SPEC §3). XP капает с убийств (через глобальное
    /// <see cref="Health.OnAnyDeath"/>), масштабируется по биому. При уровне:
    /// xpNext = round(xpNext×1.4 + 30); maxHealth += 12; лечение +38; ёмкость патронов
    /// +6 (пулемёт +18, газ +10). Висит на Player. Уровень читает спавнер для биом-скейла.
    /// </summary>
    public class Leveling : MonoBehaviour
    {
        [SerializeField] int level = 1;
        [SerializeField] float xp = 0f;
        [SerializeField] float xpNext = 70f;

        [Header("Награда за убийство (если у врага нет EnemyXp)")]
        [SerializeField] float baseKillXp = 15f;

        [Header("Бонусы за уровень")]
        [SerializeField] float healOnLevel = 38f;
        [SerializeField] float maxHealthPerLevel = 12f;
        [SerializeField] int ammoCapPerLevel = 6;
        [SerializeField] int machinegunCapPerLevel = 18;
        [SerializeField] int gasCapPerLevel = 10;

        PlayerController pc;
        AmmoInventory ammo;

        public int Level => level;
        public float Xp => xp;
        public float XpNext => xpNext;
        public event System.Action OnChanged;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            ammo = GetComponent<AmmoInventory>();
        }

        void OnEnable() => Health.OnAnyDeath += OnKill;
        void OnDisable() => Health.OnAnyDeath -= OnKill;

        void OnKill(Health h)
        {
            if (h == null) return;
            float reward = baseKillXp;
            var ex = h.GetComponent<EnemyXp>();
            if (ex != null) reward = ex.Xp;

            int biome = BiomeManager.Instance != null ? BiomeManager.Instance.CurrentBiome : 1;
            reward *= BiomeScaling.EnemyXp(biome);
            AddXp(reward);
        }

        public void AddXp(float amount)
        {
            if (amount <= 0f) return;
            xp += amount;
            while (xp >= xpNext) { xp -= xpNext; LevelUp(); }
            OnChanged?.Invoke();
        }

        void LevelUp()
        {
            level++;
            xpNext = Mathf.Round(xpNext * 1.4f + 30f);

            if (pc != null)
            {
                pc.AddMaxHealth(maxHealthPerLevel, false);
                pc.Heal(healOnLevel);
            }
            if (ammo != null)
            {
                ammo.AddCapacity(AmmoKind.Pistol, ammoCapPerLevel);
                ammo.AddCapacity(AmmoKind.Rifle, ammoCapPerLevel);
                ammo.AddCapacity(AmmoKind.Shells, ammoCapPerLevel);
                ammo.AddCapacity(AmmoKind.Machinegun, machinegunCapPerLevel);
                ammo.AddCapacity(AmmoKind.Gas, gasCapPerLevel);
            }
        }
    }

    /// <summary>Необязательная награда XP конкретного врага (иначе берётся базовая).</summary>
    public class EnemyXp : MonoBehaviour
    {
        [SerializeField] float xp = 15f;
        public float Xp => xp;
    }
}
