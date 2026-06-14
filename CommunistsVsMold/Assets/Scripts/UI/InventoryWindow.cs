using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// Окно инвентаря (порт ui.js inventory): открывается клавишей I, показывает список
    /// оружия игрока с подсветкой активного и счётчиком патронов. Q (как обычно) меняет
    /// оружие — список обновляется. Закрытие I/Esc. Сам строит свой Canvas.
    /// Достаточно повесить компонент на любой объект в сцене.
    /// </summary>
    public class InventoryWindow : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        WeaponController weapon;
        AmmoInventory ammo;
        GameObject panel;
        Text[] rows = new Text[8];
        bool built;

        void Awake() { Build(); }

        void Update()
        {
            if (weapon == null || ammo == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) { weapon = p.GetComponent<WeaponController>(); ammo = p.GetComponent<AmmoInventory>(); }
            }

            var kb = Keyboard.current;
            if (kb != null && kb.iKey.wasPressedThisFrame) SetOpen(!IsOpen);
            if (IsOpen && kb != null && kb.escapeKey.wasPressedThisFrame) SetOpen(false);

            if (IsOpen) Refresh();
        }

        void SetOpen(bool v)
        {
            IsOpen = v;
            if (panel != null) panel.SetActive(v);
            if (v) Refresh();
        }

        void Refresh()
        {
            var ws = weapon != null ? weapon.Weapons : null;
            int cur = weapon != null ? weapon.CurrentIndex : -1;
            for (int i = 0; i < rows.Length; i++)
            {
                bool has = ws != null && i < ws.Count;
                rows[i].gameObject.SetActive(has);
                if (!has) continue;
                var w = ws[i];
                string name = w != null ? w.displayName : "—";
                string ammoStr = (w == null || w.ammo == AmmoKind.None) ? "—"
                               : (ammo != null ? ammo.Get(w.ammo).ToString() : "?") + " патр.";
                bool active = i == cur;
                rows[i].text = (active ? "▶ " : "•  ") + name + "      " + ammoStr;
                rows[i].color = active ? new Color(1f, 0.82f, 0.11f) : new Color(0.93f, 0.93f, 0.93f);
            }
        }

        // ───────────── построение UI ─────────────
        void Build()
        {
            if (built) return; built = true;

            var cgo = new GameObject("InventoryWindowCanvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 81;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Панель справа (как оригинал — у правого края).
            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(cgo.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.78f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(360f, 400f);
            prt.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.04f, 0.027f, 0.016f, 0.92f);

            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(panel.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 6f); brt.anchoredPosition = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0.725f, 0.537f, 0.271f, 1f);

            var title = MakeText(font, 22, new Color(1f, 0.82f, 0.11f), TextAnchor.UpperLeft, -22f, 34f);
            title.fontStyle = FontStyle.Bold;
            title.text = "ИНВЕНТАРЬ";

            for (int i = 0; i < rows.Length; i++)
                rows[i] = MakeText(font, 16, new Color(0.93f, 0.93f, 0.93f), TextAnchor.UpperLeft, -70f - i * 36f, 32f);

            var footer = MakeText(font, 13, new Color(0.6f, 0.78f, 1f), TextAnchor.LowerLeft, -366f, 28f);
            footer.text = "Q — следующее оружие     I / Esc — закрыть";

            panel.SetActive(false);
        }

        Text MakeText(Font font, int size, Color color, TextAnchor anchor, float y, float h)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(panel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-36f, h);
            rt.anchoredPosition = new Vector2(0f, y);
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }
    }
}
