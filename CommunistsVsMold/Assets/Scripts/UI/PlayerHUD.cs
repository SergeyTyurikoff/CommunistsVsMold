using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// HUD игрока: полоса здоровья (fillAmount) и вспышка экрана при получении урона.
    /// Привязки (player, healthFill, damageFlash) выставляются в инспекторе.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] Image healthFill;
        [SerializeField] Image damageFlash;
        [SerializeField] float flashFade = 2f;

        float prevHealth;
        bool hasPrev;

        void Update()
        {
            if (player != null && healthFill != null)
            {
                float hp = player.Health;
                healthFill.fillAmount = Mathf.Clamp01(hp / Mathf.Max(1f, player.MaxHealth));

                // Если в этом кадре здоровье уменьшилось — вспышка урона.
                if (hasPrev && hp < prevHealth && damageFlash != null)
                {
                    var c = damageFlash.color;
                    c.a = 0.4f;
                    damageFlash.color = c;
                }

                prevHealth = hp;
                hasPrev = true;
            }

            // Плавно гасим вспышку к нулю каждый кадр.
            if (damageFlash != null)
            {
                var c = damageFlash.color;
                if (c.a > 0f)
                {
                    c.a = Mathf.MoveTowards(c.a, 0f, flashFade * Time.deltaTime);
                    damageFlash.color = c;
                }
            }
        }
    }
}
