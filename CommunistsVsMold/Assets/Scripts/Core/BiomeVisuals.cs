using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Визуал биома: меняет спрайт фона при смене биома (PORT_SPEC §8). Массив фонов
    /// индексируется (биом-1). Подписка на <see cref="BiomeManager.OnBiomeChanged"/>
    /// по паттерну RushManager (с обработкой текущего биома в Start, отпиской в OnDestroy).
    /// </summary>
    public class BiomeVisuals : MonoBehaviour
    {
        [SerializeField] SpriteRenderer backgroundRenderer;
        [SerializeField] Sprite[] biomeBackgrounds;   // индекс = биом-1

        void Start()
        {
            if (BiomeManager.Instance != null)
            {
                BiomeManager.Instance.OnBiomeChanged += OnBiome;
                OnBiome(BiomeManager.Instance.CurrentBiome);
            }
        }

        void OnDestroy()
        {
            if (BiomeManager.Instance != null) BiomeManager.Instance.OnBiomeChanged -= OnBiome;
        }

        void OnBiome(int biome)
        {
            if (backgroundRenderer == null || biomeBackgrounds == null || biomeBackgrounds.Length == 0) return;
            int i = Mathf.Clamp(biome - 1, 0, biomeBackgrounds.Length - 1);
            if (biomeBackgrounds[i] != null) backgroundRenderer.sprite = biomeBackgrounds[i];
        }
    }
}
