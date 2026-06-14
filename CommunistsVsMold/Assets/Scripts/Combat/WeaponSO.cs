using UnityEngine;

namespace Kommunisty
{
    public enum WeaponKind { Gun, Shotgun, Melee, Gas }

    public enum AmmoKind { None, Pistol, Rifle, Machinegun, Shells, Gas }

    [CreateAssetMenu(menuName = "Kommunisty/Weapon", fileName = "Weapon")]
    public class WeaponSO : ScriptableObject
    {
        public string displayName = "Оружие";
        [TextArea] public string desc = "";    // краткое описание для окна инвентаря/магазина
        public WeaponKind kind = WeaponKind.Gun;
        public AmmoKind ammo = AmmoKind.Pistol;
        public float damage = 18f;
        public float fireDelay = 0.27f;        // сек между выстрелами
        public float projectileSpeed = 18f;
        public float range = 9f;
        public float knockback = 3f;
        public int pellets = 1;                // дробь (Shotgun)
        public float spread = 0.08f;           // разброс конусом, радианы (Shotgun)
        public int ammoPerShot = 1;
        public float meleeRange = 1.2f;        // Melee
        public float meleeArc = 1.4f;          // Melee — ширина дуги (м)

        [Tooltip("Спрайт оружия, рисуемый ПОВЕРХ героя (в руке)")]
        public Sprite overlaySprite;           // визуал оружия на герое (weapon-overlay)
    }
}
