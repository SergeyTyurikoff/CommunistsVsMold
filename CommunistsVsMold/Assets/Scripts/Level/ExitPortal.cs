using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Переход в следующую локацию — НЕВИДИМЫЙ край справа (без портала): когда игрок доходит
    /// до правого края локации (позиция этого объекта ставится LevelBuilder в конец уровня),
    /// идёт переход через <see cref="BiomeManager"/>. Во время катсцены переход не срабатывает.
    /// </summary>
    public class ExitPortal : MonoBehaviour
    {
        [SerializeField] float rearmDelay = 1f;  // антидребезг между срабатываниями
        float lockUntil;
        Transform player;

        void Awake()
        {
            // Порталов больше нет — прячем любой визуал/коллайдер.
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            lockUntil = Time.time + rearmDelay;   // не сработать сразу на старте
        }

        void Update()
        {
            if (Time.time < lockUntil) return;
            if (CutsceneManager.IsPlaying) return;
            if (BiomeManager.Instance == null) return;
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) player = p.transform; else return;
            }
            // Дошёл до правого края локации → следующая локация.
            if (player.position.x >= transform.position.x)
            {
                lockUntil = Time.time + rearmDelay;
                BiomeManager.Instance.NextBiome();
            }
        }
    }
}
