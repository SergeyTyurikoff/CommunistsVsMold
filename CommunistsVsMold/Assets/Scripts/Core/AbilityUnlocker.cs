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

        // Чтобы тост «способность открыта» показывался один раз на каждую способность.
        bool annDouble, annTurbo, annTime;

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
            if (biome >= 1) { pc.UnlockDoubleJump(); if (!annDouble) { annDouble = true; Toast.Show("Открыто: двойной прыжок (W в воздухе)"); } }
            if (biome >= 2) { pc.UnlockTurbo();      if (!annTurbo)  { annTurbo = true;  Toast.Show("Открыто: турбо (C)"); } }
            if (biome >= 3) { pc.UnlockTimeStop();   if (!annTime)   { annTime = true;   Toast.Show("Открыто: стоп-время (F)"); } }
        }
    }
}
