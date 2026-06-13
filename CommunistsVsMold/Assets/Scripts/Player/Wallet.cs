using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Кошелёк игрока (PORT_SPEC §10). Доход с убийств/сундуков/пикапов проходит через
    /// глобальный множитель <see cref="moneyMult"/> (economy.moneyMult = 0.5). Траты
    /// (магазин) — без множителя. Висит компонентом на Player.
    /// </summary>
    public class Wallet : MonoBehaviour
    {
        [SerializeField] int money = 18;                       // старт: 18 (PORT_SPEC §3)
        [SerializeField, Range(0f, 2f)] float moneyMult = 0.5f; // economy.moneyMult

        public int Money => money;
        public event System.Action OnChanged;

        /// <summary>Доход (с убийств/пикапов) — с учётом moneyMult.</summary>
        public void AddMoney(int raw)
        {
            if (raw <= 0) return;
            money += Mathf.Max(1, Mathf.RoundToInt(raw * moneyMult));
            OnChanged?.Invoke();
        }

        /// <summary>Начислить ровно столько (без множителя) — для возвратов/наград.</summary>
        public void AddMoneyExact(int v)
        {
            if (v == 0) return;
            money = Mathf.Max(0, money + v);
            OnChanged?.Invoke();
        }

        /// <summary>Списать стоимость, если хватает денег. Вернёт false, если не хватило.</summary>
        public bool TrySpend(int cost)
        {
            if (cost < 0 || money < cost) return false;
            money -= cost;
            OnChanged?.Invoke();
            return true;
        }
    }
}
