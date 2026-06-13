using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// HUD босса: полоса здоровья сверху экрана. Виден только пока жив активный
    /// босс (<see cref="BossController.Active"/>). Показывает имя босса, долю HP
    /// и в фазе ярости (Phase >= 2) — суффикс « — ЯРОСТЬ» и красный оттенок полосы.
    /// Привязки (root, fill, nameLabel) выставляются в инспекторе.
    /// </summary>
    public class BossHUD : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] GameObject root;      // корневая панель — показывать/скрывать
        [SerializeField] Image fill;           // заполнение полосы (fillAmount = Hp01)
        [SerializeField] Text nameLabel;       // подпись имени босса

        [Header("Цвета полосы")]
        [SerializeField] Color normalColor = Color.white;   // фаза 1
        [SerializeField] Color rageColor = Color.red;        // фаза 2 (ярость)

        void Update()
        {
            var b = BossController.Active;

            // Нет живого босса — прячем панель и выходим.
            if (b == null || !b.IsAlive)
            {
                if (root != null) root.SetActive(false);
                return;
            }

            if (root != null) root.SetActive(true);

            // Доля HP.
            if (fill != null)
            {
                fill.fillAmount = Mathf.Clamp01(b.Hp01);
                fill.color = b.Phase >= 2 ? rageColor : normalColor;
            }

            // Имя (+ суффикс ярости во 2-й фазе).
            if (nameLabel != null)
            {
                string title = b.BossName ?? "";
                nameLabel.text = b.Phase >= 2 ? title + " — ЯРОСТЬ" : title;
            }
        }
    }
}
