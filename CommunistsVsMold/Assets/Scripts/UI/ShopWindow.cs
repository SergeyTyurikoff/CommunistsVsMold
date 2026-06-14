using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// Модальное окно магазина (порт ui.js shop): открывается клавишей E у снабженца,
    /// показывает «МАГАЗИН СНАБЖЕНЦА» — деньги и список товаров с ценами; покупка
    /// клавишами 3/4/5 (не конфликтует с предметами 1/2), закрытие E/Esc или при отходе.
    /// Сам строит свой Canvas. Достаточно повесить компонент на любой объект в сцене.
    /// Покупкой занимается <see cref="Shop"/> (Buy + тосты), окно — только UI и ввод.
    /// </summary>
    public class ShopWindow : MonoBehaviour
    {
        /// <summary>Открыто ли окно магазина (на случай, если другим системам нужно знать).</summary>
        public static bool IsOpen { get; private set; }

        Shop shop;
        Wallet wallet;
        GameObject panel;
        Text moneyText, footerText;
        Text[] rows = new Text[6];
        bool built;

        void Awake() { Build(); }

        void Update()
        {
            if (shop == null) shop = FindFirstObjectByType<Shop>();
            if (wallet == null) { var p = GameObject.FindWithTag("Player"); if (p != null) wallet = p.GetComponent<Wallet>(); }
            if (shop == null) return;

            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                if (IsOpen) SetOpen(false);
                else if (shop.PlayerInRange) SetOpen(true);
            }
            if (IsOpen && kb != null && kb.escapeKey.wasPressedThisFrame) SetOpen(false);
            if (IsOpen && !shop.PlayerInRange) SetOpen(false);   // отошёл — закрыть

            if (!IsOpen) return;

            if (kb != null)
            {
                if (kb.digit3Key.wasPressedThisFrame) shop.Buy(0);
                if (kb.digit4Key.wasPressedThisFrame) shop.Buy(1);
                if (kb.digit5Key.wasPressedThisFrame) shop.Buy(2);
            }
            Refresh();
        }

        void SetOpen(bool v)
        {
            IsOpen = v;
            if (panel != null) panel.SetActive(v);
            if (v) Refresh();
        }

        void Refresh()
        {
            if (moneyText != null) moneyText.text = "Деньги: " + (wallet != null ? wallet.Money : 0);
            var o = shop.Offers;
            for (int i = 0; i < rows.Length; i++)
            {
                bool has = o != null && i < o.Length && i < 3;   // покупка завязана на 3/4/5
                rows[i].gameObject.SetActive(has);
                if (has) rows[i].text = "[" + (i + 3) + "]   " + o[i].label + "   —   " + o[i].price + " мон.";
            }
        }

        // ───────────── построение UI ─────────────
        void Build()
        {
            if (built) return; built = true;

            var cgo = new GameObject("ShopWindowCanvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Панель по центру.
            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(cgo.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(620f, 420f);
            prt.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

            // Цветная рамка-полоса сверху (узнаваемость как в оригинале).
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(panel.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 6f); brt.anchoredPosition = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0.725f, 0.537f, 0.271f, 1f); // #b98945

            var title = MakeText(font, 26, new Color(1f, 0.82f, 0.11f), TextAnchor.UpperLeft, -22f, 40f);
            title.fontStyle = FontStyle.Bold;
            title.text = "МАГАЗИН СНАБЖЕНЦА";

            moneyText = MakeText(font, 17, new Color(0.93f, 0.93f, 0.93f), TextAnchor.UpperLeft, -64f, 28f);
            moneyText.text = "Деньги: 0";

            for (int i = 0; i < rows.Length; i++)
                rows[i] = MakeText(font, 16, new Color(0.94f, 0.94f, 0.94f), TextAnchor.UpperLeft, -104f - i * 38f, 34f);

            footerText = MakeText(font, 14, new Color(0.6f, 0.78f, 1f), TextAnchor.LowerLeft, -380f, 30f);
            footerText.text = "3 / 4 / 5 — купить        E / Esc — закрыть";

            panel.SetActive(false);
        }

        // Текст-строка, прикреплённая к ВЕРХУ панели: y — смещение вниз (пиксели), h — высота.
        Text MakeText(Font font, int size, Color color, TextAnchor anchor, float y, float h)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-72f, h);              // ширина панели минус поля
            rt.anchoredPosition = new Vector2(0f, y);
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }
    }
}
