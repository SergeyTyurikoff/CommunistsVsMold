using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Враг, чьи характеристики масштабируются при спавне под номер биома и уровень героя.
    /// Реализуется каждым ИИ-врагом и боссом; вызывается спавнером сразу после Instantiate.
    /// </summary>
    public interface IBiomeScalable
    {
        /// <summary>Применить масштаб биома. biome — номер биома (с 1),
        /// levelBoost — (уровень героя на входе в биом − 1).</summary>
        void ApplyBiomeScale(int biome, int levelBoost);
    }

    /// <summary>
    /// Формулы масштабирования врагов по биому (PORT_SPEC §7, applyBiomeScaling).
    /// levelBoost = (уровень героя на входе в биом) − 1. Враги и боссы — разные коэффициенты.
    /// Каждый множитель применяется к базовым характеристикам один раз при спавне.
    /// </summary>
    public static class BiomeScaling
    {
        // --- Рядовые враги ---
        public static float EnemyHp(int biome, int boost)    => 1f + biome * 0.14f + boost * 0.16f;
        public static float EnemyDmg(int biome, int boost)   => 1f + biome * 0.11f + boost * 0.10f;
        public static float EnemySpeed(int biome, int boost) => 1f + biome * 0.09f + boost * 0.02f;
        public static float EnemyXp(int biome)               => 1f + biome * 0.14f;
        public static float EnemyMoney(int biome)            => 1f + biome * 0.18f;

        // --- Боссы (свои коэффициенты) ---
        public static float BossHp(int biome, int boost)    => 1f + biome * 0.18f + boost * 0.20f;
        public static float BossDmg(int biome, int boost)   => 1f + biome * 0.14f + boost * 0.13f;
        public static float BossSpeed(int biome, int boost) => 1f + biome * 0.05f + boost * 0.015f;
    }
}
