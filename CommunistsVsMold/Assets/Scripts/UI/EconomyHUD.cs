using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// HUD экономики: постоянная строка «Деньги / Аптечки / Противогаз», а в зоне
    /// магазина — список предложений с ценами и клавишами. Ссылки на игрока/магазин
    /// ищет сам, если не заданы. Стиль — как в RushHUD/PlayerHUD (UnityEngine.UI.Text).
    /// </summary>
    public class EconomyHUD : MonoBehaviour
    {
        [SerializeField] Text statusLabel;   // деньги / аптечки / противогаз
        [SerializeField] Text shopLabel;     // предложения магазина (видно в зоне)
        [SerializeField] Wallet wallet;
        [SerializeField] UtilityInventory util;
        [SerializeField] Leveling lvl;
        [SerializeField] Shop shop;

        readonly StringBuilder sb = new StringBuilder();

        void Update()
        {
            EnsureRefs();

            if (statusLabel != null)
            {
                string mask = util == null ? "" :
                    (util.MaskActive ? "Противогаз: ВКЛ" :
                     (util.MaskCooldownLeft > 0f ? "Противогаз: откат" : "Противогаз: готов (2)"));
                statusLabel.text = "Ур. " + (lvl != null ? lvl.Level : 1)
                                 + " (" + (lvl != null ? Mathf.FloorToInt(lvl.Xp) : 0) + "/" + (lvl != null ? Mathf.FloorToInt(lvl.XpNext) : 0) + ")"
                                 + "    Деньги: " + (wallet != null ? wallet.Money : 0)
                                 + "    Аптечки: " + (util != null ? util.Medkits : 0) + " (1)"
                                 + "    " + mask;
            }

            if (shopLabel != null)
            {
                bool inShop = shop != null && shop.PlayerInRange && shop.Offers != null;
                if (shopLabel.gameObject.activeSelf != inShop) shopLabel.gameObject.SetActive(inShop);
                if (inShop) shopLabel.text = BuildShop();
            }
        }

        void EnsureRefs()
        {
            if (wallet != null && util != null && lvl != null) return;
            var p = GameObject.FindWithTag("Player");
            if (p == null) return;
            if (wallet == null) wallet = p.GetComponent<Wallet>();
            if (util == null) util = p.GetComponent<UtilityInventory>();
            if (lvl == null) lvl = p.GetComponent<Leveling>();
        }

        string BuildShop()
        {
            sb.Length = 0;
            sb.Append("МАГАЗИН:   ");
            var o = shop.Offers;
            for (int i = 0; i < o.Length && i < 3; i++)
                sb.Append("[").Append(i + 3).Append("] ").Append(o[i].label).Append(" — ").Append(o[i].price).Append("    ");
            return sb.ToString();
        }
    }
}
