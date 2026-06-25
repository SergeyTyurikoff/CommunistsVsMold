using System.Collections;
using UnityEngine;

namespace CvM.Test
{
    /// <summary>
    /// Разлёт спрайта на НЕквадратные меш-осколки.
    /// Строит джиттер-сетку (cols x rows), каждая ячейка = кривой четырёхугольник-меш
    /// с UV на текстуру спрайта; каждому осколку Rigidbody2D + взрывной импульс наружу
    /// (во все стороны + подброс вверх) + вращение. Работает с любым одиночным спрайтом.
    /// Требует у текстуры Read/Write (isReadable=true) — для проверки пустых ячеек.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteShatter : MonoBehaviour
    {
        [Header("Сетка осколков")]
        public int cols = 5;
        public int rows = 6;
        [Range(0f, 0.9f)] public float jitter = 0.6f;  // 0 = квадраты, выше = кривее

        [Header("Разлёт")]
        public float force = 6f;          // базовый импульс наружу
        public float forceRandom = 3.5f;  // случайная добавка
        public float upBias = 3f;         // подброс вверх
        public float torque = 70f;
        public float gravityScale = 1f;
        public float pieceLifetime = 5f;

        [Header("Авто-тест")]
        public bool shatterOnStart = true;
        public float startDelay = 1.0f;

        private SpriteRenderer _sr;
        private static Shader _shader;

        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }

        private IEnumerator Start()
        {
            if (shatterOnStart) { yield return new WaitForSeconds(startDelay); Shatter(); }
        }

        public void Shatter()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            Sprite sprite = _sr.sprite;
            if (sprite == null) return;

            Texture2D tex = sprite.texture;
            float ppu = sprite.pixelsPerUnit;
            Rect r = sprite.rect;
            float w = r.width / ppu, h = r.height / ppu;
            Vector3 origin = transform.position;

            if (_shader == null) _shader = Shader.Find("Sprites/Default");
            Material mat = new Material(_shader);
            mat.mainTexture = tex;

            int nx = cols + 1, ny = rows + 1;
            Vector2[,] g = new Vector2[nx, ny];
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    float u = (float)i / cols, v = (float)j / rows;
                    float jx = (i > 0 && i < cols) ? (Random.value - 0.5f) * jitter / cols : 0f;
                    float jy = (j > 0 && j < rows) ? (Random.value - 0.5f) * jitter / rows : 0f;
                    g[i, j] = new Vector2(Mathf.Clamp01(u + jx), Mathf.Clamp01(v + jy));
                }

            Color[] white = { Color.white, Color.white, Color.white, Color.white };
            int[] tris = { 0, 2, 1, 0, 3, 2 };

            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    Vector2 a = g[i, j], b = g[i + 1, j], c = g[i + 1, j + 1], d = g[i, j + 1];
                    if (IsCellEmpty(tex, r, a, c)) continue;

                    Vector2 La = N2L(a, w, h), Lb = N2L(b, w, h), Lc = N2L(c, w, h), Ld = N2L(d, w, h);
                    Vector2 cen = (La + Lb + Lc + Ld) * 0.25f;

                    GameObject go = new GameObject("shard");
                    go.transform.position = origin + (Vector3)cen;

                    Mesh mesh = new Mesh();
                    mesh.vertices = new Vector3[] { (Vector3)(La - cen), (Vector3)(Lb - cen), (Vector3)(Lc - cen), (Vector3)(Ld - cen) };
                    mesh.uv = new Vector2[] { N2UV(a, r, tex), N2UV(b, r, tex), N2UV(c, r, tex), N2UV(d, r, tex) };
                    mesh.colors = white;
                    mesh.triangles = tris;
                    mesh.RecalculateBounds();

                    go.AddComponent<MeshFilter>().mesh = mesh;
                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = mat;
                    mr.sortingLayerID = _sr.sortingLayerID;
                    mr.sortingOrder = _sr.sortingOrder + 1;

                    PolygonCollider2D pc = go.AddComponent<PolygonCollider2D>();
                    pc.points = new Vector2[] { La - cen, Lb - cen, Lc - cen, Ld - cen };

                    Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
                    rb.gravityScale = gravityScale;

                    Vector2 dir = cen.sqrMagnitude < 0.0001f ? Random.insideUnitCircle : ((Vector2)cen).normalized;
                    dir = (dir + Random.insideUnitCircle * 0.7f).normalized;
                    Vector2 impulse = dir * (force + Random.value * forceRandom) + Vector2.up * upBias * Random.value;
                    rb.AddForce(impulse, ForceMode2D.Impulse);
                    rb.AddTorque(Random.Range(-torque, torque));

                    Destroy(go, pieceLifetime);
                }

            _sr.enabled = false;
        }

        private static Vector2 N2L(Vector2 n, float w, float h) { return new Vector2((n.x - 0.5f) * w, (n.y - 0.5f) * h); }
        private static Vector2 N2UV(Vector2 n, Rect r, Texture2D t) { return new Vector2((r.x + n.x * r.width) / t.width, (r.y + n.y * r.height) / t.height); }

        private bool IsCellEmpty(Texture2D tex, Rect r, Vector2 a, Vector2 c)
        {
            try
            {
                int x0 = (int)(r.x + Mathf.Min(a.x, c.x) * r.width), x1 = (int)(r.x + Mathf.Max(a.x, c.x) * r.width);
                int y0 = (int)(r.y + Mathf.Min(a.y, c.y) * r.height), y1 = (int)(r.y + Mathf.Max(a.y, c.y) * r.height);
                int sx = Mathf.Max(1, (x1 - x0) / 6), sy = Mathf.Max(1, (y1 - y0) / 6);
                for (int y = y0; y < y1; y += sy)
                    for (int x = x0; x < x1; x += sx)
                        if (tex.GetPixel(x, y).a > 0.06f) return false;
                return true;
            }
            catch { return false; }
        }
    }
}
