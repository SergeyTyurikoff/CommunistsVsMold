using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Режиссёр биомов и боссов (PORT_SPEC §6, §8). Хранит таблицу из 6 биомов
    /// (имя, цвет фона, префаб босса) и по смене биома спавнит босса этого биома,
    /// если он задан и нет уже живого босса. После спавна масштабирует босса под
    /// биом через <see cref="IBiomeScalable"/>.
    /// Подписка на <see cref="BiomeManager.OnBiomeChanged"/> по образцу RushManager.
    /// </summary>
    public class BiomeBossDirector : MonoBehaviour
    {
        /// <summary>Описание одного биома: имя, фон и префаб его босса.</summary>
        [System.Serializable]
        public class BiomeDef
        {
            public string name;
            public Color bg = Color.black;
            public GameObject bossPrefab;   // назначает сборщик сцены; может быть null
        }

        [Header("Биомы (6 шт., PORT_SPEC §6)")]
        [SerializeField]
        BiomeDef[] biomes = new BiomeDef[]
        {
            new BiomeDef { name = "Лес" },
            new BiomeDef { name = "Зима" },
            new BiomeDef { name = "Пустыня" },
            new BiomeDef { name = "Болото" },
            new BiomeDef { name = "Город" },
            new BiomeDef { name = "Мавзолей" },
        };

        [Header("Спавн босса")]
        [SerializeField] Transform bossSpawn;   // где спавнить; null — относительно себя
        [SerializeField] int heroLevel = 1;     // уровень героя (для levelBoost при масштабе)

        /// <summary>Кол-во биомов в таблице.</summary>
        public int BiomeCount => biomes != null ? biomes.Length : 0;

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

        void OnBiome(int biome) => SpawnBoss(biome);

        /// <summary>
        /// Заспавнить босса указанного биома (с 1). Ничего не делает, если уже есть
        /// живой босс или у биома не задан префаб. Доступен для ручного/QA-вызова.
        /// </summary>
        public void SpawnBoss(int biome)
        {
            if (biome < 1) return;   // биом 0 — обучение, без босса
            // Не плодим дубль — один живой босс за раз.
            if (BossController.Active != null) return;
            if (biomes == null || biomes.Length == 0) return;

            int idx = Mathf.Clamp(biome - 1, 0, biomes.Length - 1);
            var def = biomes[idx];
            if (def == null || def.bossPrefab == null) return;

            Vector3 pos = bossSpawn != null ? bossSpawn.position : transform.position;
            Quaternion rot = bossSpawn != null ? bossSpawn.rotation : Quaternion.identity;

            var go = Instantiate(def.bossPrefab, pos, rot);
            if (go == null) return;

            // Масштаб под биом: levelBoost = heroLevel-1 (как договорено в задаче).
            var scalable = go.GetComponent<IBiomeScalable>();
            if (scalable != null) scalable.ApplyBiomeScale(biome, Mathf.Max(0, heroLevel - 1));
        }

        /// <summary>Имя биома из таблицы (с клампом); иначе "Биом N".</summary>
        public string BiomeName(int biome)
        {
            if (biomes != null && biomes.Length > 0)
            {
                int idx = Mathf.Clamp(biome - 1, 0, biomes.Length - 1);
                var def = biomes[idx];
                if (def != null && !string.IsNullOrEmpty(def.name)) return def.name;
            }
            return "Биом " + biome;
        }
    }
}
