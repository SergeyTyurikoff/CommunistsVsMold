using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Статистика забега: число убийств (через <see cref="Health.OnAnyDeath"/>) и время
    /// игры (накапливается, пока <see cref="Counting"/> = true — выставляет <see cref="GameUI"/>).
    /// Время — в нескейленных секундах, чтобы пауза (timeScale=0) его не двигала.
    /// </summary>
    public class GameStats : MonoBehaviour
    {
        public static GameStats Instance { get; private set; }

        public int Kills { get; private set; }
        public float Elapsed { get; private set; }
        public bool Counting;

        void Awake() => Instance = this;
        void OnEnable() => Health.OnAnyDeath += OnKill;
        void OnDisable() => Health.OnAnyDeath -= OnKill;
        void OnKill(Health h) => Kills++;

        void Update() { if (Counting) Elapsed += Time.unscaledDeltaTime; }

        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
