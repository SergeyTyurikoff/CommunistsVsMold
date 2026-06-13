using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Открытие способностей героя по биомам (PORT_SPEC §3, abilityUnlocks).
    /// Биом 0 (обучение) — без способностей; биом ≥1 — двойной прыжок; ≥2 — турбо;
    /// ≥3 — стоп-время. Накопительно; вызывает методы Unlock* у <see cref="PlayerController"/>.
    /// </summary>
    public class AbilityUnlocker : MonoBehaviour
    {
        PlayerController pc;

        void Awake()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) pc = p.GetComponent<PlayerController>();
        }

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
            if (pc == null) return;
            if (biome >= 1) pc.UnlockDoubleJump();
            if (biome >= 2) pc.UnlockTurbo();
            if (biome >= 3) pc.UnlockTimeStop();
        }
    }
}
