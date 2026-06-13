using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// Дополнения HUD: переключатель оружия (список с подсветкой текущего, клавиша Q)
    /// и строка цели уровня. Ссылки на игрока/оружие находит сам. Стиль — как в остальных HUD.
    /// </summary>
    public class HudExtras : MonoBehaviour
    {
        [SerializeField] Text weaponsLabel;
        [SerializeField] Text goalLabel;

        WeaponController weapon;
        readonly StringBuilder sb = new StringBuilder();

        void Update()
        {
            if (weapon == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) weapon = p.GetComponent<WeaponController>();
            }

            if (weaponsLabel != null && weapon != null)
            {
                sb.Length = 0;
                sb.Append("Оружие (Q):  ");
                var ws = weapon.Weapons;
                int cur = weapon.CurrentIndex;
                if (ws != null)
                    for (int i = 0; i < ws.Count; i++)
                    {
                        string n = ws[i] != null ? ws[i].displayName : "—";
                        if (i == cur) sb.Append('[').Append(n).Append(']');
                        else sb.Append(n);
                        if (i < ws.Count - 1) sb.Append("  ·  ");
                    }
                weaponsLabel.text = sb.ToString();
            }

            if (goalLabel != null)
            {
                int biome = BiomeManager.Instance != null ? BiomeManager.Instance.CurrentBiome : 0;
                goalLabel.text = biome == 0
                    ? "Обучение — дойди до портала →"
                    : "Цель: зачисти путь и дойди до портала →";
            }
        }
    }
}
