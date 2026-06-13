using System.Collections.Generic;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Хранилище боезапаса игрока. Висит компонентом на Player.
    /// Использует enum <see cref="AmmoKind"/> из Combat/WeaponSO.cs (здесь НЕ объявляется).
    /// Тип <see cref="AmmoKind.None"/> трактуется как мили-оружие — патроны бесконечны.
    /// </summary>
    public class AmmoInventory : MonoBehaviour
    {
        [System.Serializable]
        public struct AmmoStart
        {
            public AmmoKind kind;
            public int amount;
            public int max;
        }

        [Tooltip("Стартовые патроны. Если пусто — берутся дефолты в Awake.")]
        [SerializeField] private List<AmmoStart> starting = new List<AmmoStart>();

        // Большой максимум по умолчанию, если для типа не задан предел.
        private const int DefaultMax = 999;

        private readonly Dictionary<AmmoKind, int> counts = new Dictionary<AmmoKind, int>();
        private readonly Dictionary<AmmoKind, int> maxes = new Dictionary<AmmoKind, int>();

        private void Awake()
        {
            counts.Clear();
            maxes.Clear();

            if (starting != null && starting.Count > 0)
            {
                foreach (var s in starting)
                {
                    // None в стартовом списке игнорируем — патроны мили бесконечны.
                    if (s.kind == AmmoKind.None) continue;

                    int max = s.max > 0 ? s.max : DefaultMax;
                    int amount = Mathf.Clamp(s.amount, 0, max);

                    counts[s.kind] = amount;
                    maxes[s.kind] = max;
                }
            }
            else
            {
                ApplyDefaults();
            }
        }

        private void ApplyDefaults()
        {
            SetDefault(AmmoKind.Pistol, 60, 120);
            SetDefault(AmmoKind.Rifle, 24, 60);
            SetDefault(AmmoKind.Machinegun, 90, 240);
            SetDefault(AmmoKind.Shells, 16, 40);
            SetDefault(AmmoKind.Gas, 40, 120);
        }

        private void SetDefault(AmmoKind k, int amount, int max)
        {
            maxes[k] = max;
            counts[k] = Mathf.Clamp(amount, 0, max);
        }

        /// <summary>Текущее количество патронов. Для None — бесконечно (int.MaxValue).</summary>
        public int Get(AmmoKind k)
        {
            if (k == AmmoKind.None) return int.MaxValue;
            return counts.TryGetValue(k, out int v) ? v : 0;
        }

        /// <summary>Хватает ли n патронов. Для None — всегда true.</summary>
        public bool Has(AmmoKind k, int n)
        {
            if (k == AmmoKind.None) return true;
            return Get(k) >= n;
        }

        /// <summary>Потратить n патронов (не ниже 0). Для None — ничего не делает.</summary>
        public void Use(AmmoKind k, int n)
        {
            if (k == AmmoKind.None) return;
            if (n <= 0) return;

            int current = counts.TryGetValue(k, out int v) ? v : 0;
            counts[k] = Mathf.Max(0, current - n);
        }

        /// <summary>Добавить n патронов, но не выше Max(k). Для None — ничего не делает.</summary>
        public void Add(AmmoKind k, int n)
        {
            if (k == AmmoKind.None) return;
            if (n <= 0) return;

            int current = counts.TryGetValue(k, out int v) ? v : 0;
            counts[k] = Mathf.Min(Max(k), current + n);
        }

        /// <summary>Максимум для типа патронов. Если не задан — большой дефолт. Для None — int.MaxValue.</summary>
        public int Max(AmmoKind k)
        {
            if (k == AmmoKind.None) return int.MaxValue;
            return maxes.TryGetValue(k, out int v) ? v : DefaultMax;
        }
    }
}
