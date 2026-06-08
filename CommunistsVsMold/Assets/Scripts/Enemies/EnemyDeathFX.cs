using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Фидбэк смерти врага: разлёт зелёных кусков плесени, тряска и краткий hit-stop.
    /// Подписывается на Health.OnDeath, вешается на объект врага (зомби и т.п.).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyDeathFX : MonoBehaviour
    {
        [SerializeField] Color gibColor = new Color(0.45f, 0.7f, 0.25f); // зелёная плесень
        [SerializeField] int gibCount = 12;
        [SerializeField] float yOffset = 1f;

        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
            if (health != null) health.OnDeath += OnDie;
        }

        void OnDie()
        {
            var p = (Vector2)transform.position + Vector2.up * yOffset;
            GameFX.Instance?.SpawnGibs(p, gibColor, gibCount);
            GameFX.Instance?.Shake(0.18f, 0.22f);
            GameFX.Instance?.HitStop(0.05f);
        }

        void OnDestroy()
        {
            if (health != null) health.OnDeath -= OnDie;
        }
    }
}
