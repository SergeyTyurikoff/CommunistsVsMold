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
        static Sprite stoneTile;   // тайлящаяся каменная текстура со швами/кирпичными стыками (как world.js)
        int builtBiome;            // биом, под который сейчас строится геометрия

        // Тело камня платформы (как world.js): земля #252530, sky #30303a.
        static readonly Color GroundStone = new Color(0.145f, 0.145f, 0.188f, 1f);
        static readonly Color SkyStone    = new Color(0.188f, 0.188f, 0.227f, 1f);

        // Точные цвета верхней полосы-«грунта» по уровню (world.js surfColor, levelIndex 0..5):
        // лес #3a5228, снег #a0b8c2, песок #8a6038, болото #2a5038, завод #404050, плесень #5a2020.
        static readonly Color[] SurfaceColors =
        {
            new Color(0.227f, 0.322f, 0.157f),
            new Color(0.627f, 0.722f, 0.761f),
            new Color(0.541f, 0.376f, 0.220f),
            new Color(0.165f, 0.314f, 0.220f),
            new Color(0.251f, 0.251f, 0.314f),
            new Color(0.353f, 0.125f, 0.125f),
        };
        // biome 0 (обучение) визуально = лес (idx 0); biome>=1 → idx = biome-1 (как li в BiomeLayouts).
        static Color SurfaceColor(int biome) => SurfaceColors[Mathf.Clamp(biome == 0 ? 0 : biome - 1, 0, SurfaceColors.Length - 1)];

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
            builtBiome = biome;
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

            // Единый инвариант: ящики и сундуки СТОЯТ на земле — низ их коллайдера лежит
            // на верхней грани земли (UY(485) = 0). Без ad-hoc сдвигов −0.3f: спавним на
            // полу и доводим SnapBottomTo по реальным bounds (не зависит от пивота/масштаба).
            float floorY = LevelMetrics.UY(LevelMetrics.GroundSurfaceWebY);

            foreach (var c in L.chests)
            {
                var pos = new Vector3(LevelMetrics.UX(c.x), floorY, 0f);
                var ch = Chest.Spawn(pos, c.loot, chestSprite);
                if (ch != null)
                {
                    ch.transform.SetParent(entRoot.transform, true);
                    SnapBottomTo(ch.gameObject, floorY);
                }
            }

            foreach (var c in L.crates)
            {
                var pos = new Vector3(LevelMetrics.UX(c.x), floorY, 0f);
                var cr = Crate.Spawn(pos, 1f, crateSprite);
                if (cr != null)
                {
                    cr.transform.SetParent(entRoot.transform, true);
                    SnapBottomTo(cr.gameObject, floorY);
                }
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
                if (go.GetComponent<EnemySpeech>() == null) go.AddComponent<EnemySpeech>();  // реплики над врагом
                if (go.GetComponent<OverheadBar>() == null) { var hb = go.AddComponent<OverheadBar>(); hb.SetAlwaysShow(true); }  // полоса HP над врагом (всегда видна)
            }
        }

        void BuildPlatform(PlatformDef p)
        {
            float w = LevelMetrics.U(p.w), h = LevelMetrics.U(p.h);
            float cx = LevelMetrics.UX(p.x) + w * 0.5f;
            float cy = LevelMetrics.UY(p.y) - h * 0.5f;

            // Родитель несёт коллайдер + слой; масштаб оставляем 1, размеры задаём ЯВНО
            // (у коллайдера и у дочерних спрайтов-слоёв), иначе слои наследовали бы scale.
            var go = new GameObject(p.sky ? "Sky" : "Ground");
            go.transform.SetParent(geomRoot.transform, true);
            go.transform.position = new Vector3(cx, cy, 0f);
            go.layer = p.sky ? oneWayLayer : groundLayer;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, h);

            if (p.sky)
            {
                var eff = go.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                col.usedByEffector = true;
            }

            // Слой 1 — каменное тело: тайлящаяся текстура (швы + кирпичные стыки), тинт камнем.
            EnsureStoneTile();
            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            var bsr = body.AddComponent<SpriteRenderer>();
            bsr.sprite = stoneTile;
            bsr.drawMode = SpriteDrawMode.Tiled;   // повтор тайла по размеру → стыки каждые 32px (1 юнит)
            bsr.size = new Vector2(w, h);
            bsr.color = p.sky ? SkyStone : GroundStone;
            bsr.sortingOrder = -10;

            // Слой 2 — верхняя полоса-«грунт» с точным цветом биома.
            float topH = Mathf.Min(0.16f, h * 0.5f);
            AddQuad(go.transform, "Top", w, topH, 0f, h * 0.5f - topH * 0.5f, SurfaceColor(builtBiome), -9);

            // Слой 3 — тонкий светлый блик кромки.
            float glintH = Mathf.Min(0.05f, h * 0.2f);
            AddQuad(go.transform, "Glint", w, glintH, 0f, h * 0.5f - glintH * 0.5f, new Color(1f, 1f, 1f, 0.16f), -8);

            // Слой 4 — тёмные кромки: у sky — нижняя «висячая» грань, у земли — боковые.
            if (p.sky)
            {
                AddQuad(go.transform, "Under", w, 0.12f, 0f, -h * 0.5f + 0.06f, new Color(0f, 0f, 0f, 0.45f), -9);
            }
            else
            {
                float eW = 0.12f;
                AddQuad(go.transform, "EdgeL", eW, h, -w * 0.5f + eW * 0.5f, 0f, new Color(0f, 0f, 0f, 0.30f), -9);
                AddQuad(go.transform, "EdgeR", eW, h,  w * 0.5f - eW * 0.5f, 0f, new Color(0f, 0f, 0f, 0.30f), -9);
            }
        }

        // Дочерний спрайт-слой платформы (Simple): unitSprite, размер через localScale, смещение по X/Y.
        static void AddQuad(Transform parent, string name, float w, float h, float localX, float localY, Color color, int order)
        {
            var q = new GameObject(name);
            q.transform.SetParent(parent, false);
            q.transform.localPosition = new Vector3(localX, localY, 0f);
            q.transform.localScale = new Vector3(w, h, 1f);
            var sr = q.AddComponent<SpriteRenderer>();
            sr.sprite = unitSprite;
            sr.color = color;
            sr.sortingOrder = order;
        }

        // Процедурный каменный тайл 32×32 (PPU 32 → 1 тайл = 1 юнит): белая база (тинтуется камнем),
        // вертикальный кирпичный стык (левый край) + горизонтальный шов (низ) + лёгкая неоднородность.
        static void EnsureStoneTile()
        {
            if (stoneTile != null) return;
            const int S = 32;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float v = 1f;
                    if (((x * 7 + y * 13) % 17) == 0) v -= 0.06f;   // крапинки темнее
                    if (((x * 5 + y * 11) % 23) == 0) v += 0.05f;   // крапинки светлее
                    if (x == 0) v *= 0.72f;                          // вертикальный стык
                    if (y == 0) v *= 0.80f;                          // горизонтальный шов
                    v = Mathf.Clamp01(v);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            tex.Apply();
            stoneTile = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 32f);
        }

        // Ставит объект так, чтобы НИЗ его коллайдера лёг ровно на пол floorY
        // (единый инвариант «низ сущности = верх опоры»). Заменяет прежние
        // магические сдвиги (−0.3f): работает при любом пивоте/масштабе спрайта.
        static void SnapBottomTo(GameObject go, float floorY)
        {
            Physics2D.SyncTransforms();
            // Ровняем по НИЗУ ВИДИМОЙ части (спрайт): у сундука круг-коллайдер меньше спрайта,
            // и привязка по коллайдеру топила спрайт под платформу. Спрайт — то, что видит игрок.
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            float bottom;
            if (sr != null) bottom = sr.bounds.min.y;
            else { var col = go.GetComponent<Collider2D>(); if (col == null) return; bottom = col.bounds.min.y; }
            float dy = floorY - bottom;
            go.transform.position += new Vector3(0f, dy, 0f);
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
