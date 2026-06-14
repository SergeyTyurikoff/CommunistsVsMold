using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// Главная статус-панель HUD (порт ui.js hud() — правый верхний блок): спрайт текущего
    /// оружия + название + патроны, полоса здоровья и полоса опыта, деньги / уровень / убийства.
    /// Самодостаточна: строит свой Canvas и берёт данные у игрока (PlayerController, WeaponController,
    /// Wallet, Leveling) и GameStats. Достаточно повесить компонент на объект в сцене.
    /// </summary>
    public class StatusPanel : MonoBehaviour
    {
        PlayerController pc;
        WeaponController weapon;
        Wallet wallet;
        Leveling lvl;

        Image weaponIcon, hpFill, xpFill;
        Text nameText, ammoText, hpText, xpText, moneyText, lvlText, killsText;
        bool built;

        static readonly Color Cyan = new Color(0.4f, 0.85f, 1f);
        static readonly Color LowAmmo = new Color(1f, 0.36f, 0.36f);
        static readonly Color HpGood = new Color(0.4f, 0.91f, 1f);
        static readonly Color HpLow = new Color(1f, 0.42f, 0.37f);

        void Awake() { Build(); }

        void Update()
        {
            if (pc == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) { pc = p.GetComponent<PlayerController>(); weapon = p.GetComponent<WeaponController>(); wallet = p.GetComponent<Wallet>(); lvl = p.GetComponent<Leveling>(); }
            }

            // Оружие + патроны
            var w = weapon != null ? weapon.CurrentWeapon : null;
            if (w != null)
            {
                weaponIcon.enabled = w.overlaySprite != null;
                if (weaponIcon.enabled) weaponIcon.sprite = w.overlaySprite;
                nameText.text = w.displayName;
                int a = weapon.CurrentAmmo;
                if (a >= int.MaxValue) { ammoText.text = "∞"; ammoText.color = new Color(0.67f, 0.72f, 0.77f); }
                else { ammoText.text = a + " патр."; ammoText.color = a < 10 ? LowAmmo : new Color(0.67f, 0.72f, 0.77f); }
            }

            // Здоровье
            if (pc != null)
            {
                float r = Mathf.Clamp01(pc.Health / Mathf.Max(1f, pc.MaxHealth));
                hpFill.fillAmount = r;
                hpFill.color = r > 0.35f ? HpGood : HpLow;
                hpText.text = "Здоровье " + Mathf.Ceil(pc.Health) + "/" + Mathf.RoundToInt(pc.MaxHealth);
            }

            // Опыт / уровень
            if (lvl != null)
            {
                xpFill.fillAmount = Mathf.Clamp01(lvl.Xp / Mathf.Max(1f, lvl.XpNext));
                xpText.text = "XP " + Mathf.FloorToInt(lvl.Xp) + "/" + Mathf.FloorToInt(lvl.XpNext);
                lvlText.text = "LVL " + lvl.Level;
            }

            moneyText.text = "Деньги: " + (wallet != null ? wallet.Money : 0);
            killsText.text = "★ " + (GameStats.Instance != null ? GameStats.Instance.Kills : 0);
        }

        // ───────────── построение UI ─────────────
        void Build()
        {
            if (built) return; built = true;

            var cgo = new GameObject("StatusPanelCanvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Панель в правом верхнем углу.
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(cgo.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(1f, 1f); prt.pivot = new Vector2(1f, 1f);
            prt.sizeDelta = new Vector2(304f, 86f);
            prt.anchoredPosition = new Vector2(-12f, -10f);
            panel.GetComponent<Image>().color = new Color(0.02f, 0.027f, 0.04f, 0.86f);
            var pt = panel.transform;

            // Цветная полоса слева (узнаваемость как в оригинале — голубая рамка).
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(pt, false);
            var barrt = bar.GetComponent<RectTransform>();
            barrt.anchorMin = new Vector2(0f, 0f); barrt.anchorMax = new Vector2(0f, 1f); barrt.pivot = new Vector2(0f, 0.5f);
            barrt.sizeDelta = new Vector2(4f, 0f); barrt.anchoredPosition = Vector2.zero;
            bar.GetComponent<Image>().color = new Color(0.35f, 0.74f, 1f, 0.6f);

            // Иконка оружия (слева сверху).
            var ico = new GameObject("WeaponIcon", typeof(RectTransform), typeof(Image));
            ico.transform.SetParent(pt, false);
            var icrt = ico.GetComponent<RectTransform>();
            icrt.anchorMin = icrt.anchorMax = new Vector2(0f, 1f); icrt.pivot = new Vector2(0f, 1f);
            icrt.sizeDelta = new Vector2(48f, 28f); icrt.anchoredPosition = new Vector2(12f, -10f);
            weaponIcon = ico.GetComponent<Image>();
            weaponIcon.preserveAspect = true;

            nameText  = Label(pt, font, 15, Color.white, TextAnchor.UpperLeft, FontStyle.Bold, new Vector2(66f, -10f), new Vector2(230f, 20f));
            ammoText  = Label(pt, font, 13, new Color(0.67f,0.72f,0.77f), TextAnchor.UpperLeft, FontStyle.Normal, new Vector2(66f, -30f), new Vector2(150f, 18f));

            moneyText = Label(pt, font, 13, new Color(1f,0.82f,0.11f), TextAnchor.UpperRight, FontStyle.Bold, new Vector2(-10f, -10f), new Vector2(150f, 18f));
            lvlText   = Label(pt, font, 12, new Color(0.94f,0.94f,0.9f), TextAnchor.UpperRight, FontStyle.Bold, new Vector2(-10f, -28f), new Vector2(150f, 16f));
            killsText = Label(pt, font, 12, new Color(0.74f,0.74f,0.74f), TextAnchor.UpperRight, FontStyle.Normal, new Vector2(-10f, -44f), new Vector2(150f, 16f));

            // Полоса здоровья (низ слева) + подпись.
            MakeBar(pt, new Vector2(12f, 14f), new Vector2(150f, 11f), out hpFill, HpGood);
            hpText = Label(pt, font, 10, new Color(0.84f,0.96f,1f), TextAnchor.LowerLeft, FontStyle.Bold, new Vector2(12f, 26f), new Vector2(160f, 14f));

            // Полоса опыта (низ справа) + подпись.
            MakeBar(pt, new Vector2(170f, 14f), new Vector2(122f, 11f), out xpFill, new Color(0.48f,0.84f,0.16f));
            xpText = Label(pt, font, 10, new Color(0.71f,0.95f,0.42f), TextAnchor.LowerLeft, FontStyle.Bold, new Vector2(170f, 26f), new Vector2(140f, 14f));
        }

        // Полоса: фон + заливка (Filled Horizontal). Якорь — низ-лево панели; pos — от низ-лево.
        void MakeBar(Transform parent, Vector2 pos, Vector2 size, out Image fill, Color fillColor)
        {
            var bg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(parent, false);
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0f, 0f); brt.pivot = new Vector2(0f, 0f);
            brt.sizeDelta = size; brt.anchoredPosition = pos;
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            var fl = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fl.transform.SetParent(bg.transform, false);
            var frt = fl.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            fill = fl.GetComponent<Image>();
            fill.color = fillColor;
            fill.sprite = WhiteSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
        }

        static Sprite white;
        static Sprite WhiteSprite()
        {
            if (white != null) return white;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return white;
        }

        Text Label(Transform parent, Font font, int size, Color color, TextAnchor anchor, FontStyle style, Vector2 pos, Vector2 sz)
        {
            // Якорим к верх-лево панели; pos.y отрицательный = вниз. Для UpperRight pos.x от правого края — задаём через anchor.
            var go = new GameObject("T", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            bool right = anchor == TextAnchor.UpperRight || anchor == TextAnchor.LowerRight;
            float ax = right ? 1f : 0f;
            rt.anchorMin = rt.anchorMax = new Vector2(ax, 1f); rt.pivot = new Vector2(ax, 1f);
            rt.sizeDelta = sz; rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
