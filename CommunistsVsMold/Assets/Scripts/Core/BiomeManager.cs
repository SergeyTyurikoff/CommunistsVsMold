using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Менеджер биомов/уровня: номер текущего биома, чекпойнт-респаун, переход в следующий биом.
    /// Переход (через <see cref="ExitPortal"/>) переносит игрока на старт нового биома, ставит
    /// чекпойнт, лечит игрока, меняет фон камеры по палитре и (опц.) спавнит врага.
    /// Чекпойнты (<see cref="Checkpoint"/>) двигают точку возрождения. Висит на отдельном объекте сцены.
    /// </summary>
    public class BiomeManager : MonoBehaviour
    {
        public static BiomeManager Instance { get; private set; }

        [Header("Ссылки")]
        [SerializeField] PlayerController player;
        [SerializeField] Transform biomeStart;   // куда ставим игрока в начале каждого биома
        [SerializeField] Camera mainCamera;        // для смены фона; если null — Camera.main

        [Header("Биомы")]
        [SerializeField] int startBiome = 1;
        [SerializeField] Color[] palette = new Color[]
        {
            new Color(0.08f, 0.08f, 0.11f),  // мавзолей — холодный тёмный
            new Color(0.11f, 0.07f, 0.05f),  // тёплый бурый
            new Color(0.05f, 0.10f, 0.07f),  // болотно-зелёный (плесень)
            new Color(0.09f, 0.05f, 0.12f),  // фиолетовый
        };

        [Header("Враги (опционально)")]
        [SerializeField] GameObject enemyPrefab;  // спавнить по одному на новый биом
        [SerializeField] Transform enemySpawn;

        /// <summary>Номер текущего биома (с 1).</summary>
        public int CurrentBiome { get; private set; }

        /// <summary>Текущая точка возрождения (чекпойнт).</summary>
        public Vector3 CurrentCheckpoint { get; private set; }

        /// <summary>Срабатывает при смене биома (в т.ч. на старте) с новым номером.</summary>
        public event System.Action<int> OnBiomeChanged;

        void Awake()
        {
            Instance = this;
            CurrentBiome = Mathf.Max(1, startBiome);
            if (mainCamera == null) mainCamera = Camera.main;

            Vector3 cp = biomeStart != null
                ? biomeStart.position
                : (player != null ? player.transform.position : transform.position);
            CurrentCheckpoint = cp;
            if (player != null) player.SetRespawnPoint(cp);
        }

        void Start()
        {
            ApplyBiomeVisuals();
            OnBiomeChanged?.Invoke(CurrentBiome);
        }

        /// <summary>Поставить новый чекпойнт (вызывает триггер <see cref="Checkpoint"/>).</summary>
        public void SetCheckpoint(Vector3 pos)
        {
            CurrentCheckpoint = pos;
            if (player != null) player.SetRespawnPoint(pos);
        }

        /// <summary>Перейти в следующий биом: игрок на старт, чекпойнт, лечение, фон, спавн врага.</summary>
        public void NextBiome()
        {
            CurrentBiome++;

            Vector3 startPos = biomeStart != null
                ? biomeStart.position
                : (player != null ? player.transform.position : transform.position);

            if (player != null)
            {
                player.transform.position = startPos;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                player.SetRespawnPoint(startPos);
                player.Heal(player.MaxHealth);
            }
            CurrentCheckpoint = startPos;

            ApplyBiomeVisuals();

            if (enemyPrefab != null)
            {
                Vector3 ep = enemySpawn != null
                    ? enemySpawn.position
                    : startPos + new Vector3(6f, 0f, 0f);
                Instantiate(enemyPrefab, ep, Quaternion.identity);
            }

            OnBiomeChanged?.Invoke(CurrentBiome);
        }

        void ApplyBiomeVisuals()
        {
            if (mainCamera != null && palette != null && palette.Length > 0)
                mainCamera.backgroundColor = palette[(CurrentBiome - 1) % palette.Length];
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
