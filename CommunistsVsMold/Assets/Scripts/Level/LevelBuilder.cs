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
        int builtBiome;   // биом, под который сейчас строится геометрия (для тинта кромок)

        // Цвет верхней кромки платформы по биому (поверхностный «грунт»): лес→земля→холод→песок→багрянец→фиолет.
        static readonly Color[] BiomeTints =
        {
            new Color(0.36f, 0.55f, 0.28f),
            new Color(0.45f, 0.38f, 0.22f),
            new Color(0.30f, 0.45f, 0.55f),
            new Color(0.55f, 0.45f, 0.30f),
            new Color(0.50f, 0.30f, 0.30f),
            new Color(0.35f, 0.30f, 0.45f),
        };
        static Color BiomeTint(int b) => BiomeTints[Mathf.Clamp(b, 0, BiomeTints.Length - 1)];

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

            // Слой 1 — каменное тело платформы.
            AddQuad(go.transform, "Body", w, h, 0f, p.sky ? skyColor : groundColor, -10);

            // Слой 2 — верхняя кромка с биом-тинтом (читается как «грунт/поверхность»).
            float topH = Mathf.Min(0.16f, h * 0.5f);
            AddQuad(go.transform, "Top", w, topH, h * 0.5f - topH * 0.5f, BiomeTint(builtBiome), -9);

            // Слой 3 — тонкий светлый блик сразу под кромкой (объём/край).
            float glintH = Mathf.Min(0.05f, h * 0.2f);
            AddQuad(go.transform, "Glint", w * 0.98f, glintH, h * 0.5f - topH - glintH * 0.5f,
                    new Color(1f, 1f, 1f, 0.35f), -8);
        }

        // Дочерний спрайт-слой платформы: unitSprite, размер через localScale, смещение по локальному Y.
        static void AddQuad(Transform parent, string name, float w, float h, float localY, Color color, int order)
        {
            var q = new GameObject(name);
            q.transform.SetParent(parent, false);
            q.transform.localPosition = new Vector3(0f, localY, 0f);
            q.transform.localScale = new Vector3(w, h, 1f);
            var sr = q.AddComponent<SpriteRenderer>();
            sr.sprite = unitSprite;
            sr.color = color;
            sr.sortingOrder = order;
        }

        // Ставит объект так, чтобы НИЗ его коллайдера лёг ровно на пол floorY
        // (единый инвариант «низ сущности = верх опоры»). Заменяет прежние
        // магические сдвиги (−0.3f): работает при любом пивоте/масштабе спрайта.
        static void SnapBottomTo(GameObject go, float floorY)
        {
            Physics2D.SyncTransforms();
            var col = go.GetComponent<Collider2D>();
            if (col == null) return;
            float dy = floorY - col.bounds.min.y;
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
