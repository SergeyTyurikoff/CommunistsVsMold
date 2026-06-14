using UnityEngine;
using UnityEngine.InputSystem;

namespace Kommunisty
{
    /// <summary>
    /// Магазин снабженца у входа (PORT_SPEC §8, §10). Зона-коллайдер: пока игрок внутри,
    /// клавиши 3/4/5 покупают предложения 1/2/3 за деньги (<see cref="Wallet"/>).
    /// Нахождение игрока в зоне определяется перекрытием коллайдера в Update (надёжно,
    /// без зависимости от событий триггера). Список предложений — в инспекторе.
    /// HUD читает <see cref="PlayerInRange"/>/<see cref="Offers"/> (<see cref="EconomyHUD"/>).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Shop : MonoBehaviour
    {
        [System.Serializable]
        public class Offer
        {
            public string label = "Товар";
            public PickupKind kind = PickupKind.Medkit;
            public int amount = 1;
            public int price = 20;
            public WeaponSO weapon;   // если задано — покупка ОРУЖИЯ (добавляется в арсенал)
        }

        [SerializeField] Offer[] offers = new Offer[]
        {
            new Offer { label = "Аптечка",          kind = PickupKind.Medkit,     amount = 1,  price = 20 },
            new Offer { label = "Патроны пистолет", kind = PickupKind.AmmoPistol, amount = 24, price = 12 },
            new Offer { label = "Патроны винтовка", kind = PickupKind.AmmoRifle,  amount = 12, price = 18 },
        };

        [Header("Зона покупки (близость к снабженцу)")]
        [SerializeField] float nearRangeX = 1.8f;   // по X — подойти вплотную
        [SerializeField] float nearRangeY = 3f;     // по Y — щедро (игрок может стоять чуть выше/ниже)

        Collider2D zone;
        PlayerController pc;
        Transform playerTf;
        Wallet wallet;
        AmmoInventory ammo;
        UtilityInventory util;
        WeaponController weaponCtrl;
        bool playerInRange;

        public bool PlayerInRange => playerInRange;
        public Offer[] Offers => offers;

        /// <summary>Это оружие уже куплено (для затемнения в окне магазина).</summary>
        public bool IsOwned(Offer o) => o != null && o.weapon != null && weaponCtrl != null && weaponCtrl.HasWeapon(o.weapon);

        void Awake()
        {
            zone = GetComponent<Collider2D>();
            if (zone != null) zone.isTrigger = true;
            CachePlayer();
        }

        void CachePlayer()
        {
            var p = GameObject.FindWithTag("Player");
            if (p == null) return;
            pc = p.GetComponent<PlayerController>();
            playerTf = p.transform;
            wallet = p.GetComponent<Wallet>();
            ammo = p.GetComponent<AmmoInventory>();
            util = p.GetComponent<UtilityInventory>();
            weaponCtrl = p.GetComponent<WeaponController>();
        }

        void Update()
        {
            if (playerTf == null) CachePlayer();
            // Близость по позиции снабженца, а не OverlapPoint по ногам игрока: точка ног
            // (y≈0 после выравнивания координат) могла не попадать в зону-триггер магазина.
            if (playerTf != null)
            {
                float dx = Mathf.Abs(playerTf.position.x - transform.position.x);
                float dy = Mathf.Abs(playerTf.position.y - transform.position.y);
                playerInRange = dx <= nearRangeX && dy <= nearRangeY;
            }
            else playerInRange = false;
            // Покупкой теперь управляет модальное окно ShopWindow (открывается по E):
            // оно вызывает Buy() по клавишам 3/4/5, только когда окно открыто. Здесь —
            // только определение близости к снабженцу (для подсказки и открытия окна).
        }

        /// <summary>Купить предложение i. Вернёт false, если нет денег/нет предложения.</summary>
        public bool Buy(int i)
        {
            if (offers == null || i < 0 || i >= offers.Length || wallet == null) return false;
            var o = offers[i];

            // Покупка ОРУЖИЯ — добавляем в арсенал (если ещё нет).
            if (o.weapon != null)
            {
                if (weaponCtrl != null && weaponCtrl.HasWeapon(o.weapon)) { Toast.Show("Уже есть: " + o.label); return false; }
                if (!wallet.TrySpend(o.price)) { Toast.Show("Не хватает денег: " + o.label + " (" + o.price + ")"); return false; }
                weaponCtrl?.AddWeapon(o.weapon);
                AudioManager.Instance?.PlayPickup();
                Toast.Show("Куплено оружие: " + o.label);
                return true;
            }

            // Расходники / патроны.
            if (!wallet.TrySpend(o.price))
            {
                Toast.Show("Не хватает денег: " + o.label + " (" + o.price + ")");
                return false;
            }
            Grant(o);
            AudioManager.Instance?.PlayPickup();
            Toast.Show("Куплено: " + o.label);
            return true;
        }

        void Grant(Offer o)
        {
            switch (o.kind)
            {
                case PickupKind.Medkit: util?.AddMedkit(o.amount); break;
                case PickupKind.Health: pc?.Heal(o.amount); break;
                case PickupKind.Money:  wallet?.AddMoneyExact(o.amount); break;
                default:                ammo?.Add(Pickup.AmmoOf(o.kind), o.amount); break;
            }
        }
    }
}
