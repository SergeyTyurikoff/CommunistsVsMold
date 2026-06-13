using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Строит настоящий уровень биома из данных <see cref="BiomeLayouts"/> (порт world.js):
    /// платформы (земля = слой Ground, sky = слой OneWay + PlatformEffector2D), сундуки,
    /// ящики, враги по всему уровню (через <see cref="EnemyCatalog"/>), и двигает портал
    /// выхода в конец уровня. Координаты конвертит из веб-пикселей через <see cref="LevelMetrics"/>.
    /// Перестраивается на каждое <see cref="BiomeManager.OnBiomeChanged"/>.
    /// Старт игрока и магазин стоят слева и постоянны (выставляются в сцене).
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] PlayerController player;
        [SerializeField] ExitPortal portal;

        [Header("Слои платформ")]
        [SerializeField] int groundLayer = 8;
        [SerializeField] int oneWayLayer = 9;

        [Header("Спрайты предметов")]
        [SerializeField] Sprite chestSprite;
        [SerializeField] Sprite crateSprite;

        [Header("Цвета платформ")]
        [SerializeField] Color groundColor = new Color(0.16f, 0.16f, 0.20f, 1f);
        [SerializeField] Color skyColor = new Color(0.30f, 0.30f, 0.36f, 1f);

        GameObject geomRoot, entRoot;
        static Sprite unitSprite;

        void Start()
        {
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) player = p.GetComponent<PlayerController>();
            }
            if (portal == null) portal = FindAnyObjectByType<ExitPortal>();

            if (BiomeManager.Instance != null)
            {
                BiomeManager.Instance.OnBiomeChanged += OnBiome;
                Build(BiomeManager.Instance.CurrentBiome);
            }
        }

        void OnDestroy()
        {
            if (BiomeManager.Instance != null) BiomeManager.Instance.OnBiomeChanged -= OnBiome;
        }

        void OnBiome(int biome) => Build(biome);

        int HeroBoost()
        {
            var l = player != null ? player.GetComponent<Leveling>() : null;
            return l != null ? Mathf.Max(0, l.Level - 1) : 0;
        }

        public void Build(int biome)
        {
            var L = BiomeLayouts.Build(biome);
            EnsureUnitSprite();

            if (geomRoot != null) Destroy(geomRoot);
            if (entRoot != null) Destroy(entRoot);
            geomRoot = new GameObject("LevelGeometry");
            entRoot = new GameObject("LevelEntities");

            foreach (var p in L.platforms) BuildPlatform(p);

            // Портал выхода — в конец уровня.
            if (portal != null)
                portal.transform.position = new Vector3(
                    LevelMetrics.UX(L.portalX) + LevelMetrics.U(70) * 0.5f,
                    LevelMetrics.UY(L.portalY) - LevelMetrics.U(90) * 0.5f, 0f);

            foreach (var c in L.chests)
            {
                var pos = new Vector3(LevelMetrics.UX(c.x), LevelMetrics.UY(c.y) - 0.3f, 0f);
                var ch = Chest.Spawn(pos, c.loot, chestSprite);
                if (ch != null) ch.transform.SetParent(entRoot.transform, true);
            }

            foreach (var c in L.crates)
            {
                var pos = new Vector3(LevelMetrics.UX(c.x), LevelMetrics.UY(c.y) - 0.3f, 0f);
                var cr = Crate.Spawn(pos, 1f, crateSprite);
                if (cr != null) cr.transform.SetParent(entRoot.transform, true);
            }

            int boost = HeroBoost();
            foreach (var s in L.spawns)
            {
                var pf = EnemyCatalog.Instance != null ? EnemyCatalog.Instance.Get(s.kind) : null;
                if (pf == null) continue;
                var pos = new Vector3(LevelMetrics.UX(s.x), LevelMetrics.UY(s.floorY) + 1.1f, 0f);
                var go = Instantiate(pf, pos, Quaternion.identity, entRoot.transform);
                var sc = go.GetComponent<IBiomeScalable>();
                if (sc != null) sc.ApplyBiomeScale(biome, boost);
            }
        }

        void BuildPlatform(PlatformDef p)
        {
            float w = LevelMetrics.U(p.w), h = LevelMetrics.U(p.h);
            float cx = LevelMetrics.UX(p.x) + w * 0.5f;
            float cy = LevelMetrics.UY(p.y) - h * 0.5f;

            var go = new GameObject(p.sky ? "Sky" : "Ground");
            go.transform.SetParent(geomRoot.transform, true);
            go.transform.position = new Vector3(cx, cy, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            go.layer = p.sky ? oneWayLayer : groundLayer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = unitSprite;
            sr.color = p.sky ? skyColor : groundColor;
            sr.sortingOrder = -10;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;   // в локальных, множится на localScale → мир (w,h)

            if (p.sky)
            {
                var eff = go.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                col.usedByEffector = true;
            }
        }

        static void EnsureUnitSprite()
        {
            if (unitSprite != null) return;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            unitSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f); // PPU 1 → 1px = 1 юнит
        }
    }
}
