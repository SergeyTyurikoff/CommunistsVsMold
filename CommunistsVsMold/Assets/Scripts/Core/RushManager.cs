using System.Collections.Generic;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>Фаза раша: ожидание триггера → предупреждение (часы) → бой → итог.</summary>
    public enum RushPhase { Idle, Warning, Active, Done }

    /// <summary>Мини-цель раша (чередуется): перебить волну или продержаться.</summary>
    public enum RushGoal { KillWave, Survive }

    /// <summary>
    /// Раш-волны (PORT_SPEC §9). 1 раш на биом, на 6-м (Мавзолей) — 2.
    /// Когда игрок доходит до триггер-зоны по X — появляются «часы» с обратным
    /// отсчётом + предупреждение; по нулю спавнится волна только из зомби.
    /// Мини-цель чередуется: «перебить волну (N)» / «продержаться surviveSeconds».
    /// Награда — лечение (деньги — позже, Раунд 11). Кол-во зомби растёт от биома.
    /// Висит на отдельном объекте сцены; данные читает HUD (<see cref="RushHUD"/>).
    /// </summary>
    public class RushManager : MonoBehaviour
    {
        public static RushManager Instance { get; private set; }

        [Header("Ссылки")]
        [SerializeField] PlayerController player;
        [SerializeField] GameObject zombiePrefab;
        [SerializeField] Transform waveSpawn;     // откуда сыпать волну; null — справа от игрока
        [SerializeField] GameObject warningClock;  // мировой маркер «часы» (виден в фазе Warning)

        [Header("Триггеры по X")]
        [SerializeField] float triggerX = 8f;        // первый раш стартует, когда игрок дошёл сюда
        [SerializeField] float secondTriggerX = 10.5f; // второй раш (только биом 6)

        [Header("Параметры волны")]
        [SerializeField] float countdown = 2.5f;     // часы-предупреждение
        [SerializeField] int baseWave = 3;           // волна в 1-м биоме
        [SerializeField] int perBiome = 1;           // +N зомби за каждый биом
        [SerializeField] float surviveSeconds = 8f;  // цель «продержаться»
        [SerializeField] float spawnSpread = 1.6f;   // разброс спавна по X
        [SerializeField] float healReward = 60f;     // награда-лечение
        [SerializeField] float doneShow = 1.5f;      // сколько показывать «волна отбита»

        public RushPhase Phase { get; private set; } = RushPhase.Idle;
        public RushGoal Goal { get; private set; }
        public float Timer { get; private set; }     // отсчёт часов / таймер «продержаться»
        public float WarnDuration => countdown;       // полная длительность предупреждения (для кругового таймера)
        public int Killed { get; private set; }
        public int WaveSize { get; private set; }

        /// <summary>Уведомление для HUD при смене состояния/таймера.</summary>
        public event System.Action OnRushChanged;

        readonly List<Health> alive = new List<Health>();
        readonly Queue<float> triggers = new Queue<float>();
        int rushIndex;   // глобальный счётчик рашей — для чередования цели
        int biome = 1;

        void Awake() => Instance = this;

        void Start()
        {
            if (warningClock != null) warningClock.SetActive(false);
            if (BiomeManager.Instance != null)
            {
                BiomeManager.Instance.OnBiomeChanged += OnBiome;
                ArmBiome(BiomeManager.Instance.CurrentBiome);
            }
            else ArmBiome(1);
        }

        void OnDestroy()
        {
            if (BiomeManager.Instance != null) BiomeManager.Instance.OnBiomeChanged -= OnBiome;
            if (Instance == this) Instance = null;
        }

        void OnBiome(int b) => ArmBiome(b);

        // Зарядить раши нового биома: размер волны и список триггер-точек.
        void ArmBiome(int b)
        {
            biome = b;
            WaveSize = Mathf.Max(1, baseWave + (b - 1) * perBiome);
            triggers.Clear();
            triggers.Enqueue(triggerX);
            if (b >= 6) triggers.Enqueue(secondTriggerX);
            alive.Clear();
            Killed = 0;
            SetPhase(RushPhase.Idle);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (Phase)
            {
                case RushPhase.Idle:
                    if (triggers.Count > 0 && player != null &&
                        player.transform.position.x >= triggers.Peek())
                        StartWarning();
                    break;

                case RushPhase.Warning:
                    Timer -= dt;
                    if (Timer <= 0f) SpawnWave();
                    OnRushChanged?.Invoke();
                    break;

                case RushPhase.Active:
                    if (Goal == RushGoal.Survive)
                    {
                        Timer -= dt;
                        if (Timer <= 0f) Complete();
                    }
                    else
                    {
                        alive.RemoveAll(h => h == null || h.IsDead);
                        Killed = WaveSize - alive.Count;
                        if (alive.Count == 0) Complete();
                    }
                    OnRushChanged?.Invoke();
                    break;

                case RushPhase.Done:
                    Timer -= dt;
                    if (Timer <= 0f)
                    {
                        if (triggers.Count > 0) triggers.Dequeue();
                        rushIndex++;
                        SetPhase(RushPhase.Idle);
                        OnRushChanged?.Invoke();
                    }
                    break;
            }
        }

        void StartWarning()
        {
            Goal = (rushIndex % 2 == 0) ? RushGoal.KillWave : RushGoal.Survive;
            Timer = countdown;
            if (warningClock != null) warningClock.SetActive(true);
            SetPhase(RushPhase.Warning);
            OnRushChanged?.Invoke();
        }

        void SpawnWave()
        {
            if (warningClock != null) warningClock.SetActive(false);
            alive.Clear();
            Killed = 0;

            Vector3 basePos = waveSpawn != null
                ? waveSpawn.position
                : (player != null ? player.transform.position + new Vector3(7f, 0f, 0f) : transform.position);

            if (zombiePrefab != null)
                for (int i = 0; i < WaveSize; i++)
                {
                    Vector3 p = basePos + new Vector3(i * spawnSpread, 0f, 0f);
                    var go = Instantiate(zombiePrefab, p, Quaternion.identity);
                    var h = go.GetComponent<Health>();
                    if (h != null) alive.Add(h);
                }

            if (Goal == RushGoal.Survive) Timer = surviveSeconds;
            SetPhase(RushPhase.Active);
            OnRushChanged?.Invoke();
        }

        void Complete()
        {
            if (player != null) player.Heal(healReward);   // TODO: + деньги (Раунд 11 — экономика)

            // «Продержаться» — оставшиеся зомби рассеиваются.
            if (Goal == RushGoal.Survive)
                foreach (var h in alive)
                    if (h != null && !h.IsDead) Destroy(h.gameObject);
            alive.Clear();

            Timer = doneShow;
            SetPhase(RushPhase.Done);
            OnRushChanged?.Invoke();
        }

        void SetPhase(RushPhase p) => Phase = p;
    }
}
