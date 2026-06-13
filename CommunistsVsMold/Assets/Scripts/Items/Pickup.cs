using UnityEngine;

namespace Kommunisty
{
    /// <summary>Что даёт пикап (PORT_SPEC §11): деньги, аптечка, лечение, патроны по типам.</summary>
    public enum PickupKind { Money, Medkit, Health, AmmoPistol, AmmoRifle, AmmoMachinegun, AmmoShells, AmmoGas }

    /// <summary>
    /// Подбираемый предмет: триггер, при касании игроком применяет эффект и исчезает.
    /// Создаётся процедурно через <see cref="Spawn"/> (лут с врагов) или ставится в сцену.
    /// Лёгкое «покачивание» для заметности.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        [SerializeField] PickupKind kind = PickupKind.Money;
        [SerializeField] int amount = 1;
        [SerializeField] float bobAmp = 0.15f;
        [SerializeField] float bobSpeed = 3f;
        [SerializeField] float life = 25f;   // авто-исчезновение, чтобы не копились

        float baseY, t;

        void Start()
        {
            baseY = transform.position.y;
            if (life > 0f) Destroy(gameObject, life);
        }

        void Update()
        {
            t += Time.deltaTime;
            var p = transform.position;
            p.y = baseY + Mathf.Sin(t * bobSpeed) * bobAmp;
            transform.position = p;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null) return;

            Apply(pc);
            AudioManager.Instance?.PlayPickup();
            Destroy(gameObject);
        }

        void Apply(PlayerController pc)
        {
            var go = pc.gameObject;
            switch (kind)
            {
                case PickupKind.Money:  go.GetComponent<Wallet>()?.AddMoney(amount); break;
                case PickupKind.Medkit: go.GetComponent<UtilityInventory>()?.AddMedkit(amount); break;
                case PickupKind.Health: pc.Heal(amount); break;
                default:
                    var ai = go.GetComponent<AmmoInventory>();
                    if (ai != null) ai.Add(AmmoOf(kind), amount);
                    break;
            }
        }

        public static AmmoKind AmmoOf(PickupKind k)
        {
            switch (k)
            {
                case PickupKind.AmmoPistol:     return AmmoKind.Pistol;
                case PickupKind.AmmoRifle:      return AmmoKind.Rifle;
                case PickupKind.AmmoMachinegun: return AmmoKind.Machinegun;
                case PickupKind.AmmoShells:     return AmmoKind.Shells;
                case PickupKind.AmmoGas:        return AmmoKind.Gas;
                default:                        return AmmoKind.None;
            }
        }

        /// <summary>Процедурно создать пикап (лут). sprite может быть null (тогда жёлтый квадрат-заглушка).</summary>
        public static Pickup Spawn(PickupKind kind, int amount, Vector3 pos, Sprite sprite)
        {
            var go = new GameObject("Pickup_" + kind);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;
            if (sprite == null) sr.color = Color.yellow;
            else sr.color = Color.white;
            // Заметный размер пикапа независимо от исходного размера спрайта.
            if (sprite != null)
            {
                float h = sprite.bounds.size.y;
                if (h > 0.01f) go.transform.localScale = Vector3.one * (0.7f / h);
            }
            else go.transform.localScale = Vector3.one * 0.4f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var p = go.AddComponent<Pickup>();
            p.kind = kind;
            p.amount = amount;
            return p;
        }
    }
}
