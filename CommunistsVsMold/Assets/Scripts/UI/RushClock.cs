using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Круговой таймер раша: в фазе предупреждения (RushPhase.Warning) над игроком
    /// появляется тёмный круг с обратным отсчётом — жёлтое кольцо сжимается по мере
    /// приближения волны + число секунд в центре. Сам строит мировые спрайты.
    /// Достаточно повесить компонент на объект в сцене.
    /// </summary>
    public class RushClock : MonoBehaviour
    {
        Transform player;
        Transform root, fill;
        TextMesh label;
        static Sprite circle;

        void Awake()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
            Build();
        }

        void Update()
        {
            var rm = RushManager.Instance;
            bool warn = rm != null && rm.Phase == RushPhase.Warning;
            if (root.gameObject.activeSelf != warn) root.gameObject.SetActive(warn);
            if (!warn) return;

            if (player != null) root.position = player.position + Vector3.up * 3.2f;
            root.rotation = Quaternion.identity;

            float frac = rm.WarnDuration > 0f ? Mathf.Clamp01(rm.Timer / rm.WarnDuration) : 0f;
            fill.localScale = new Vector3(0.85f * frac, 0.85f * frac, 1f);   // кольцо сжимается
            label.text = Mathf.CeilToInt(Mathf.Max(0f, rm.Timer)).ToString();
        }

        void Build()
        {
            EnsureCircle();
            var go = new GameObject("RushClock");
            root = go.transform;

            // Тёмный фон-круг.
            var bg = new GameObject("Bg");
            bg.transform.SetParent(root, false);
            bg.transform.localScale = Vector3.one * 1.0f;
            var bsr = bg.AddComponent<SpriteRenderer>();
            bsr.sprite = circle; bsr.color = new Color(0.05f, 0.05f, 0.05f, 0.8f); bsr.sortingOrder = 1100;

            // Жёлтое кольцо-заполнение (сжимается).
            var f = new GameObject("Fill");
            f.transform.SetParent(root, false);
            fill = f.transform;
            var fsr = f.AddComponent<SpriteRenderer>();
            fsr.sprite = circle; fsr.color = new Color(1f, 0.82f, 0.11f, 0.9f); fsr.sortingOrder = 1101;

            // Число секунд.
            var t = new GameObject("Sec");
            t.transform.SetParent(root, false);
            t.transform.localPosition = Vector3.zero;
            label = t.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font; label.fontSize = 64; label.characterSize = 0.04f; label.fontStyle = FontStyle.Bold;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.color = new Color(0.1f, 0.05f, 0.02f, 1f);
            var mr = t.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = 1102;

            root.gameObject.SetActive(false);
        }

        static void EnsureCircle()
        {
            if (circle != null) return;
            const int S = 48;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            float c = (S - 1) * 0.5f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = d <= 1f ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);   // PPU=S → 1 юнит
        }
    }
}
