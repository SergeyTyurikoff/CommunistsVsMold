using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// HUD биома: показывает номер текущего биома (из <see cref="BiomeManager"/>).
    /// label — Text-объект на Canvas; задаётся в инспекторе.
    /// </summary>
    public class BiomeHUD : MonoBehaviour
    {
        [SerializeField] Text label;
        [SerializeField] string prefix = "Биом ";

        void Update()
        {
            if (label == null) return;
            int b = BiomeManager.Instance != null ? BiomeManager.Instance.CurrentBiome : 1;
            label.text = prefix + b;
        }
    }
}
