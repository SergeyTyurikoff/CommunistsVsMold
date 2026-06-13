using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// HUD боезапаса: показывает имя текущего оружия и число патронов.
    /// Читает публичное API WeaponController (CurrentWeapon, CurrentAmmo).
    /// Привязки (weapon, label) выставляются в инспекторе:
    /// weapon — объект Player, label — Text-объект на Canvas.
    /// </summary>
    public class AmmoHUD : MonoBehaviour
    {
        [SerializeField] WeaponController weapon;
        [SerializeField] Text label;

        void Update()
        {
            if (label == null)
                return;

            if (weapon != null && weapon.CurrentWeapon != null)
            {
                string name = weapon.CurrentWeapon.displayName;

                int a = weapon.CurrentAmmo;
                string ammoStr = (a >= int.MaxValue) ? "∞" : a.ToString();

                label.text = $"{name}\n{ammoStr}";
            }
            else
            {
                // Нет данных об оружии — очищаем строку.
                label.text = string.Empty;
            }
        }
    }
}
