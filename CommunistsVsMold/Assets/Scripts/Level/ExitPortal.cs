using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Портал-выход в конце биома. Триггер: когда в него входит игрок — переход в следующий биом
    /// через <see cref="BiomeManager"/>. Есть антидребезг, чтобы не сработать дважды за переход.
    /// Нужен Collider2D с isTrigger = true.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExitPortal : MonoBehaviour
    {
        [SerializeField] float rearmDelay = 1f;  // антидребезг между срабатываниями
        float lockUntil;

        void Reset()
        {
            var c = GetComponent<Collider2D>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Time.time < lockUntil) return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            if (BiomeManager.Instance == null) return;

            lockUntil = Time.time + rearmDelay;
            BiomeManager.Instance.NextBiome();
        }
    }
}
