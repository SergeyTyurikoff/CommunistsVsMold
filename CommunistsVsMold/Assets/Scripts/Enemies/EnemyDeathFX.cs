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
        [SerializeField] Color bloodColor = new Color(0.5f, 0.05f, 0.05f); // тёмно-красный «сок»
        [SerializeField] int gibCount = 18;
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
            // «Разрывает на части»: куски плесени + тёмно-красные брызги.
            GameFX.Instance?.SpawnGibs(p, gibColor, gibCount);
            GameFX.Instance?.SpawnGibs(p, bloodColor, gibCount / 2);
            GameFX.Instance?.Shake(0.18f, 0.22f);
            GameFX.Instance?.HitStop(0.05f);
            AudioManager.Instance?.PlayEnemyDeath();

            // Кровь на экран — только если смерть рядом с игроком (на виду), чтобы не спамить.
            var player = GameObject.FindWithTag("Player");
            if (player != null && Vector2.Distance(player.transform.position, transform.position) < 9f)
                GameFX.Instance?.BloodSplat(Random.Range(3, 6));
        }

        void OnDestroy()
        {
            if (health != null) health.OnDeath -= OnDie;
        }
    }
}
