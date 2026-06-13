using System.Collections.Generic;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Спавнер врагов с биом-скейлом (PORT_SPEC §5/§7).
    /// Хранит «ростер» типов врагов; на каждый биом считает количество каждого
    /// типа (растёт от номера биома), снайпер — «ровно 1 на биом» (<see cref="SpawnEntry.oneOnly"/>).
    /// После Instantiate каждому врагу применяет масштаб биома через
    /// <see cref="IBiomeScalable.ApplyBiomeScale(int,int)"/> (boost = heroLevel-1).
    /// Подписывается на <see cref="BiomeManager.OnBiomeChanged"/> по паттерну
    /// RushManager: в Start() и сразу обрабатывает текущий биом (защита от гонки
    /// порядка Start), в OnDestroy() отписывается.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        /// <summary>Описание одного типа врага в ростере (префаб назначит сборщик сцены).</summary>
        [System.Serializable]
        public class SpawnEntry
        {
            public string label;             // человекочитаемое имя (газовик/снайпер/…)
            public GameObject prefab;        // префаб врага
            public int baseCount = 1;        // базовое количество в 1-м биоме
            public int perBiome = 0;         // +N штук за каждый следующий биом
            public bool oneOnly = false;     // «ровно 1 на биом» (снайпер)
            public float spread = 2f;        // шаг между экземплярами по X
        }

        [Header("Ростер врагов")]
        [SerializeField] List<SpawnEntry> roster = new List<SpawnEntry>();

        [Header("Ссылки")]
        [SerializeField] Transform spawnPoint;        // откуда спавнить; null — перед игроком
        [SerializeField] PlayerController player;     // опц.; иначе ищем по тегу в Awake

        [Header("Параметры")]
        [SerializeField] int heroLevel = 1;           // уровень героя; boost = heroLevel-1
        [SerializeField] float aheadOffset = 6f;      // насколько перед игроком спавнить (если нет spawnPoint)
        [SerializeField] bool autoSpawnOnBiome = true; // false — спавн только по ручному SpawnFor (QA)

        // Заспавненные этим спавнером враги (для очистки и подсчёта живых).
        readonly List<GameObject> spawned = new List<GameObject>();

        /// <summary>Число живых заспавненных врагов (мёртвые/null отсеиваются).</summary>
        public int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = spawned.Count - 1; i >= 0; i--)
                {
                    var go = spawned[i];
                    if (go == null) { spawned.RemoveAt(i); continue; }
                    var h = go.GetComponent<Health>();
                    if (h != null && h.IsDead) continue;
                    n++;
                }
                return n;
            }
        }

        void Awake()
        {
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.GetComponent<PlayerController>();
            }
        }

        void Start()
        {
            if (BiomeManager.Instance != null)
            {
                BiomeManager.Instance.OnBiomeChanged += OnBiome;
                if (autoSpawnOnBiome) SpawnFor(BiomeManager.Instance.CurrentBiome);
            }
            else if (autoSpawnOnBiome) SpawnFor(1);
        }

        void OnDestroy()
        {
            if (BiomeManager.Instance != null) BiomeManager.Instance.OnBiomeChanged -= OnBiome;
        }

        void OnBiome(int biome)
        {
            if (autoSpawnOnBiome) SpawnFor(biome);
        }

        /// <summary>
        /// Заспавнить весь ростер под указанный биом. Сначала уничтожает ранее
        /// заспавненных этим спавнером, затем по каждому типу инстанцирует нужное
        /// количество вдоль X и применяет биом-скейл.
        /// </summary>
        public void SpawnFor(int biome)
        {
            ClearSpawned();

            Vector3 basePos = GetBasePos();
            int boost = heroLevel - 1;

            foreach (var entry in roster)
            {
                if (entry == null || entry.prefab == null) continue;

                int count = entry.oneOnly
                    ? 1
                    : Mathf.Max(1, entry.baseCount + entry.perBiome * (biome - 1));

                for (int i = 0; i < count; i++)
                {
                    Vector3 p = basePos + new Vector3(i * entry.spread, 0f, 0f);
                    var go = Instantiate(entry.prefab, p, Quaternion.identity);
                    go.GetComponent<IBiomeScalable>()?.ApplyBiomeScale(biome, boost);
                    spawned.Add(go);
                }
            }
        }

        /// <summary>Уничтожить всех заспавненных этим спавнером врагов и очистить список.</summary>
        public void ClearSpawned()
        {
            for (int i = 0; i < spawned.Count; i++)
                if (spawned[i] != null) Destroy(spawned[i]);
            spawned.Clear();
        }

        // Базовая точка спавна: spawnPoint, иначе перед игроком, иначе своя позиция.
        Vector3 GetBasePos()
        {
            if (spawnPoint != null) return spawnPoint.position;
            if (player != null) return player.transform.position + new Vector3(aheadOffset, 0f, 0f);
            return transform.position;
        }
    }
}
