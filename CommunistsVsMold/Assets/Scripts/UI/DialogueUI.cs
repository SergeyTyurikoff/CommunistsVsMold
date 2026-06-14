using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// Панель диалога для катсцен: внизу экрана — имя говорящего + реплика.
    /// Самодостаточна (строит свой Canvas), синглтон. Управляется из CutsceneManager:
    /// Show(speaker, text) показать, Hide() убрать.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        GameObject panel;
        Text speakerText, bodyText;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Build();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Show(string speaker, string text)
        {
            if (panel == null) Build();
            panel.SetActive(true);
            speakerText.text = speaker;
            bodyText.text = text;
        }

        public void Hide() { if (panel != null) panel.SetActive(false); }

        void Build()
        {
            var cgo = new GameObject("DialogueCanvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Панель внизу.
            panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(cgo.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0f); prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0f);
            prt.sizeDelta = new Vector2(820f, 120f);
            prt.anchoredPosition = new Vector2(0f, 24f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);

            // Жёлтая рамка-полоса сверху панели.
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(panel.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 5f); brt.anchoredPosition = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0.78f, 0.12f, 0.1f, 1f); // красная (большевистская)

            speakerText = MakeText(font, 20, new Color(1f, 0.82f, 0.11f), FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(20f, -14f), new Vector2(780f, 26f));
            bodyText = MakeText(font, 18, Color.white, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(20f, -44f), new Vector2(780f, 64f));

            panel.SetActive(false);
        }

        Text MakeText(Font font, int size, Color color, FontStyle style, TextAnchor anchor, Vector2 pos, Vector2 sz)
        {
            var go = new GameObject("T", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = sz; rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
