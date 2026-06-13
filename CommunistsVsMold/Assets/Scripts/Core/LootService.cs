using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Дроп лута с врагов (PORT_SPEC §10). Подписан на глобальное событие смерти
    /// <see cref="Health.OnAnyDeath"/> — один объект в сцене обслуживает всех врагов
    /// (без компонента на каждом префабе). С каждого врага падают деньги, с шансом
    /// medkitChance (0.26) — аптечка. Спрайты пикапов назначаются в инспекторе.
    /// </summary>
    public class LootService : MonoBehaviour
    {
        [Header("Деньги (диапазон, до moneyMult)")]
        [SerializeField] int moneyMin = 2;
        [SerializeField] int moneyMax = 6;

        [Header("Аптечка")]
        [SerializeField, Range(0f, 1f)] float medkitChance = 0.26f;

        [Header("Спрайты пикапов")]
        [SerializeField] Sprite moneySprite;
        [SerializeField] Sprite medkitSprite;

        void OnEnable()  => Health.OnAnyDeath += OnDeath;
        void OnDisable() => Health.OnAnyDeath -= OnDeath;

        void OnDeath(Health h)
        {
            if (h == null) return;
            Vector3 pos = h.transform.position;

            int money = Random.Range(moneyMin, moneyMax + 1);
            Pickup.Spawn(PickupKind.Money, money, pos + new Vector3(0f, 0.3f, 0f), moneySprite);

            if (Random.value < medkitChance)
                Pickup.Spawn(PickupKind.Medkit, 1, pos + new Vector3(0.45f, 0.3f, 0f), medkitSprite);
        }
    }
}
