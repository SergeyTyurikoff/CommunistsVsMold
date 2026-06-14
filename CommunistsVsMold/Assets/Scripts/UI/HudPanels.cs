using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// Доп. панели HUD к паритету с оригиналом (ui.js quickSlots + combo):
    /// — ряд слотов предметов снизу по центру (1 — Аптечка, 2 — Противогаз);
    /// — крупный пульсирующий индикатор КОМБО ×N при множителе &gt; 1.
    /// Сам строит Canvas; данные берёт у игрока (UtilityInventory, ComboTracker).
    /// Достаточно повесить компонент на любой объект в сцене.
    /// </summary>
    public class HudPanels : MonoBehaviour
    {
        UtilityInventory util;
        ComboTracker combo;

        // Слоты предметов
        const int SlotCount = 6;
        Image[] slotBg = new Image[SlotCount];
        Text[] slotKey = new Text[SlotCount];
        Text[] slotVal = new Text[SlotCount];

        Text comboText;
        bool built;

        void Awake() { Build(); }

        void Update()
        {
            if (util == null || combo == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) { util = p.GetComponent<UtilityInventory>(); combo = p.GetComponent<ComboTracker>(); }
            }

            // Слот 1 — аптечка (кол-во), слот 2 — противогаз (состояние), прочие — пусто.
            SetSlot(0, "Аптечка", util != null ? ("x" + util.Medkits) : "x0", util != null && util.Medkits > 0);
            string maskVal = util == null ? "нет"
                           : (util.MaskActive ? "ВКЛ" : (util.MaskCooldownLeft > 0f ? "откат" : "готов"));
            SetSlot(1, "Противогаз", maskVal, util != null && util.MaskActive);
            for (int i = 2; i < SlotCount; i++) SetSlot(i, "", "пусто", false);

            // Комбо-индикатор.
            float mul = combo != null ? combo.Multiplier : 1f;
            bool show = mul > 1.001f;
            if (comboText.gameObject.activeSelf != show) comboText.gameObject.SetActive(show);
            if (show)
            {
                comboText.text = "КОМБО ×" + mul.ToString("0.0");
                comboText.color = mul >= 3f ? new Color(1f, 0.27f, 0f)
                               : mul >= 2f ? new Color(1f, 0.53f, 0f)
                               : new Color(1f, 0.82f, 0.11f);
                float pulse = 0.86f + Mathf.Sin(Time.unscaledTime * 12f) * 0.14f;
                var rt = comboText.rectTransform;
                rt.localScale = Vector3.one * pulse;
            }
        }

        void SetSlot(int i, string title, string value, bool hot)
        {
            slotBg[i].color = hot ? new Color(0.16f, 0.28f, 0.36f, 0.84f) : new Color(0.03f, 0.03f, 0.03f, 0.84f);
            slotKey[i].text = (i + 1).ToString();
            slotVal[i].text = string.IsNullOrEmpty(title) ? value : (title + "\n" + value);
            slotVal[i].color = hot ? new Color(0.6f, 0.85f, 0.29f) : new Color(0.77f, 0.82f, 0.85f);
        }

        // ───────────── построение UI ─────────────
        void Build()
        {
            if (built) return; built = true;

            var cgo = new GameObject("HudPanelsCanvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Ряд слотов снизу по центру.
            const float slotW = 104f, slotH = 46f, gap = 8f;
            float totalW = SlotCount * slotW + (SlotCount - 1) * gap;
            float startX = -totalW * 0.5f + slotW * 0.5f;
            for (int i = 0; i < SlotCount; i++)
            {
                var slot = new GameObject("Slot" + (i + 1), typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(cgo.transform, false);
                var rt = slot.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(slotW, slotH);
                rt.anchoredPosition = new Vector2(startX + i * (slotW + gap), 14f);
                slotBg[i] = slot.GetComponent<Image>();

                slotKey[i] = ChildText(slot.transform, font, 11, new Color(0.62f, 0.78f, 1f), TextAnchor.UpperLeft);
                Stretch(slotKey[i].rectTransform, 6f, 4f);
                slotVal[i] = ChildText(slot.transform, font, 12, new Color(0.77f, 0.82f, 0.85f), TextAnchor.MiddleCenter);
                Stretch(slotVal[i].rectTransform, 4f, 2f);
            }

            // Комбо-текст слева, под статус-строкой.
            var cgoT = new GameObject("Combo", typeof(RectTransform), typeof(Text));
            cgoT.transform.SetParent(cgo.transform, false);
            var crt = cgoT.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot = new Vector2(0f, 1f);
            crt.sizeDelta = new Vector2(360f, 40f);
            crt.anchoredPosition = new Vector2(20f, -96f);
            comboText = cgoT.GetComponent<Text>();
            comboText.font = font; comboText.fontSize = 26; comboText.fontStyle = FontStyle.Bold;
            comboText.alignment = TextAnchor.UpperLeft;
            comboText.horizontalOverflow = HorizontalWrapMode.Overflow;
            comboText.gameObject.SetActive(false);
        }

        Text ChildText(Transform parent, Font font, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("T", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void Stretch(RectTransform rt, float padX, float padY)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY); rt.offsetMax = new Vector2(-padX, -padY);
        }
    }
}
