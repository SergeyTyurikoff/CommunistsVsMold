using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Чекпойнт: триггер, который при входе игрока ставит точку возрождения в <see cref="BiomeManager"/>.
    /// По умолчанию срабатывает один раз. Нужен Collider2D с isTrigger = true.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] bool once = true;
        bool used;

        void Reset()
        {
            var c = GetComponent<Collider2D>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (once && used) return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            if (BiomeManager.Instance == null) return;

            used = true;
            BiomeManager.Instance.SetCheckpoint(transform.position);
        }
    }
}
