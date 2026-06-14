using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// HUD способностей (R24): три компактных индикатора — «Перекат (Z)», «Турбо (C)»,
    /// «Стоп-время (F)». Каждый индикатор: подпись + полоска заливки готовности/активности.
    /// Самодостаточен — сам строит свой Canvas и элементы в коде (как EconomyHUD рисует Text/Image).
    /// Игрока находит по тегу "Player", берёт PlayerController; стоп-время читает через TimeStop.Instance.
    /// Достаточно повесить компонент на любой объект в сцене — ссылки назначать не нужно.
    /// </summary>
    public class AbilityHUD : MonoBehaviour
    {
        [SerializeField] PlayerController player;

        // Цвета в стиле HUD: тусклый фон полосы, яркая заливка, подсветка готовности, «заблокировано».
        static readonly Color BarBg     = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color FillReady = new Color(0.45f, 1f, 0.55f, 0.95f);   // готово — зелёный
        static readonly Color FillCd    = new Color(1f, 0.8f, 0.3f, 0.95f);     // откат — жёлтый
        static readonly Color FillActive= new Color(0.4f, 0.85f, 1f, 0.95f);    // активно — голубой
        static readonly Color LockedCol = new Color(1f, 1f, 1f, 0.25f);         // заблокировано — тускло
        static readonly Color TextCol   = new Color(1f, 1f, 1f, 0.92f);

        // Один индикатор способности: подпись слева + полоска (фон + заливка).
        class Row
        {
            public Text label;
            public Image fill;
        }

        Row dodgeRow, turboRow, timeStopRow;
        Font font;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            BuildUI();
        }

        void EnsureRefs()
        {
            if (player != null) return;
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerController>();
        }

        void Update()
        {
            EnsureRefs();
            if (player == null) return;

            // Перекат: заполнение = DodgeReady01 (1 = готов), подсветка зелёным когда готов.
            float dodge = player.DodgeReady01;
            bool dodgeReady = dodge >= 0.999f;
            SetRow(dodgeRow, "Перекат (Z)", dodge, dodgeReady ? FillReady : FillCd, false);

            // Турбо: заблокировано / активно / откат-готовность.
            if (!player.TurboUnlocked)
            {
                SetRow(turboRow, "Турбо (C)", 1f, LockedCol, true);
            }
            else if (player.TurboActive)
            {
                SetRow(turboRow, "Турбо (C): активно", player.TurboActive01, FillActive, false);
            }
            else
            {
                float t = player.TurboReady01;
                SetRow(turboRow, "Турбо (C)", t, t >= 0.999f ? FillReady : FillCd, false);
            }

            // Стоп-время: заблокировано / активно (ВКЛ) / откат-готовность через TimeStop API.
            if (!player.TimeStopUnlocked)
            {
                SetRow(timeStopRow, "Стоп-время (F)", 1f, LockedCol, true);
            }
            else
            {
                var ts = TimeStop.Instance;
                if (ts != null && ts.Active)
                    SetRow(timeStopRow, "Стоп-время (F): ВКЛ", 1f, FillActive, false);
                else if (ts != null && ts.Ready)
                    SetRow(timeStopRow, "Стоп-время (F)", 1f, FillReady, false);
                else
                    SetRow(timeStopRow, "Стоп-время (F): откат", 0.35f, FillCd, false);
            }
        }

        // Обновить подпись, длину/цвет заливки. locked = тусклый «выключенный» вид.
        void SetRow(Row r, string text, float fill01, Color fillColor, bool locked)
        {
            if (r == null) return;
            if (r.label != null)
            {
                r.label.text = text;
                r.label.color = locked ? LockedCol : TextCol;
            }
            if (r.fill != null)
            {
                r.fill.color = fillColor;
                r.fill.fillAmount = locked ? 1f : Mathf.Clamp01(fill01);
            }
        }

        // --- Построение UI в коде ---

        void BuildUI()
        {
            // Свой Canvas (ScreenSpaceOverlay), как самодостаточный HUD.
            var canvasGO = new GameObject("AbilityHUD_Canvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Панель-контейнер слева снизу (не перекрывает верхний EconomyHUD).
            var panel = new GameObject("Abilities", typeof(RectTransform));
            panel.transform.SetParent(canvasGO.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0f, 0f);
            prt.pivot = new Vector2(0f, 0f);
            prt.anchoredPosition = new Vector2(24f, 24f);

            // Три строки снизу вверх: перекат, турбо, стоп-время.
            const float rowH = 26f;
            const float gap = 8f;
            dodgeRow    = CreateRow(panel.transform, 0 * (rowH + gap));
            turboRow    = CreateRow(panel.transform, 1 * (rowH + gap));
            timeStopRow = CreateRow(panel.transform, 2 * (rowH + gap));
        }

        // Одна строка: подпись (слева) + полоска готовности (под подписью).
        Row CreateRow(Transform parent, float y)
        {
            const float width = 240f;
            const float barH = 8f;
            const float labelH = 18f;

            var rowGO = new GameObject("Row", typeof(RectTransform));
            rowGO.transform.SetParent(parent, false);
            var rrt = rowGO.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0f, 0f);
            rrt.anchorMax = new Vector2(0f, 0f);
            rrt.pivot = new Vector2(0f, 0f);
            rrt.anchoredPosition = new Vector2(0f, y);
            rrt.sizeDelta = new Vector2(width, labelH + barH);

            // Подпись
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(rowGO.transform, false);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0f, 0f);
            lrt.anchoredPosition = new Vector2(0f, barH + 2f);
            lrt.sizeDelta = new Vector2(0f, labelH);
            var label = labelGO.AddComponent<Text>();
            label.font = font;
            label.fontSize = 15;
            label.color = TextCol;
            label.alignment = TextAnchor.LowerLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            // Фон полоски
            var bgGO = new GameObject("BarBg", typeof(RectTransform));
            bgGO.transform.SetParent(rowGO.transform, false);
            var bgrt = bgGO.GetComponent<RectTransform>();
            bgrt.anchorMin = new Vector2(0f, 0f);
            bgrt.anchorMax = new Vector2(0f, 0f);
            bgrt.pivot = new Vector2(0f, 0f);
            bgrt.anchoredPosition = new Vector2(0f, 0f);
            bgrt.sizeDelta = new Vector2(width, barH);
            var bg = bgGO.AddComponent<Image>();
            bg.color = BarBg;

            // Заливка (Filled, по горизонтали слева направо)
            var fillGO = new GameObject("BarFill", typeof(RectTransform));
            fillGO.transform.SetParent(bgGO.transform, false);
            var frt = fillGO.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(1f, 1f);
            frt.pivot = new Vector2(0f, 0.5f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var fill = fillGO.AddComponent<Image>();
            fill.color = FillReady;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            return new Row { label = label, fill = fill };
        }
    }
}
