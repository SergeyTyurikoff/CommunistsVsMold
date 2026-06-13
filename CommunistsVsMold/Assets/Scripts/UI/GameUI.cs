using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// Оболочка игры (PORT_SPEC §2): главное меню, пауза (Esc), экран смерти, статистика,
    /// подсказки обучения (биом 0). Оверлей строится процедурно под Canvas. Игра стартует
    /// в меню (timeScale=0); ENTER — играть; Esc — пауза/продолжить; R (в паузе) — рестарт.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        enum UIState { Menu, Playing, Paused, Dead }
        UIState state = UIState.Menu;

        PlayerController pc;
        Wallet wallet;
        Leveling lvl;
        BiomeBossDirector director;

        GameObject overlay;
        Text overlayText;
        Text hintText;

        const string Title = "КОММУНИСТЫ ПРОТИВ… ПЛЕСЕНИ!";
        const string Controls =
            "A/D — ход · W — прыжок · ЛКМ — огонь · Z — перекат · C — турбо\n" +
            "1 — аптечка · 2 — противогаз · 3/4/5 — магазин · Esc — пауза · R — рестарт · T/Y — биом ±";

        void Awake() => BuildUI();

        void Start()
        {
            CacheRefs();
            EnterMenu();
        }

        void CacheRefs()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) { pc = p.GetComponent<PlayerController>(); wallet = p.GetComponent<Wallet>(); lvl = p.GetComponent<Leveling>(); }
            director = FindAnyObjectByType<BiomeBossDirector>();
        }

        void Update()
        {
            if (pc == null) CacheRefs();
            var kb = Keyboard.current;

            switch (state)
            {
                case UIState.Menu:
                    if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                        StartGame();
                    break;

                case UIState.Playing:
                    if (pc != null && pc.IsDead) { EnterDead(); break; }
                    if (kb != null && kb.escapeKey.wasPressedThisFrame) EnterPause();
                    UpdateHint();
                    break;

                case UIState.Paused:
                    if (kb != null && kb.escapeKey.wasPressedThisFrame) Resume();
                    else if (kb != null && kb.rKey.wasPressedThisFrame) { BiomeManager.Instance?.RestartBiome(); Resume(); }
                    break;

                case UIState.Dead:
                    if (pc != null && !pc.IsDead) Resume();   // авто-возрождение прошло
                    break;
            }
        }

        void EnterMenu()
        {
            state = UIState.Menu;
            Time.timeScale = 0f;
            if (GameStats.Instance != null) GameStats.Instance.Counting = false;
            ShowOverlay(Title + "\n\nНажмите ENTER — Играть\n\n" + Controls);
            if (hintText != null) hintText.gameObject.SetActive(false);
        }

        void StartGame()
        {
            state = UIState.Playing;
            Time.timeScale = 1f;
            if (GameStats.Instance != null) GameStats.Instance.Counting = true;
            HideOverlay();
        }

        void EnterPause()
        {
            state = UIState.Paused;
            Time.timeScale = 0f;
            if (GameStats.Instance != null) GameStats.Instance.Counting = false;
            ShowOverlay("ПАУЗА\n\n" + StatsText() + "\n\nEsc — продолжить · R — рестарт биома");
        }

        void Resume()
        {
            state = UIState.Playing;
            Time.timeScale = 1f;
            if (GameStats.Instance != null) GameStats.Instance.Counting = true;
            HideOverlay();
        }

        void EnterDead()
        {
            state = UIState.Dead;
            if (GameStats.Instance != null) GameStats.Instance.Counting = false;
            ShowOverlay("ВЫ ПАЛИ\n\n" + StatsText() + "\n\nВозрождение на чекпойнте…");
        }

        void UpdateHint()
        {
            if (hintText == null) return;
            bool tutorial = BiomeManager.Instance != null && BiomeManager.Instance.CurrentBiome == 0;
            if (hintText.gameObject.activeSelf != tutorial) hintText.gameObject.SetActive(tutorial);
            if (tutorial) hintText.text = "ОБУЧЕНИЕ: иди вправо (A/D), прыгай (W), стреляй (ЛКМ). Дойди до портала справа.";
        }

        string StatsText()
        {
            var sb = new StringBuilder();
            int biome = BiomeManager.Instance != null ? BiomeManager.Instance.CurrentBiome : 0;
            string bn = director != null ? director.BiomeName(biome) : ("Биом " + biome);
            sb.Append("Биом: ").Append(biome).Append(" — ").Append(bn).Append('\n');
            sb.Append("Уровень: ").Append(lvl != null ? lvl.Level : 1).Append('\n');
            sb.Append("Деньги: ").Append(wallet != null ? wallet.Money : 0).Append('\n');
            if (GameStats.Instance != null)
            {
                sb.Append("Убито: ").Append(GameStats.Instance.Kills).Append('\n');
                int t = Mathf.FloorToInt(GameStats.Instance.Elapsed);
                sb.Append("Время: ").Append(t / 60).Append(':').Append((t % 60).ToString("00"));
            }
            return sb.ToString();
        }

        // ───────────────────────── Постройка UI ─────────────────────────
        void BuildUI()
        {
            var canvasGo = GameObject.Find("Canvas");
            Transform parent = canvasGo != null ? canvasGo.transform : null;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            overlay = new GameObject("GameOverlay", typeof(RectTransform), typeof(Image));
            if (parent != null) overlay.transform.SetParent(parent, false);
            var ort = overlay.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one; ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            var txtGo = new GameObject("OverlayText", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(overlay.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.08f, 0.12f); trt.anchorMax = new Vector2(0.92f, 0.92f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            overlayText = txtGo.GetComponent<Text>();
            overlayText.font = font; overlayText.fontSize = 24; overlayText.color = Color.white;
            overlayText.alignment = TextAnchor.UpperCenter;

            var hintGo = new GameObject("TutorialHint", typeof(RectTransform), typeof(Text));
            if (parent != null) hintGo.transform.SetParent(parent, false);
            var hrt = hintGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.5f, 1f); hrt.anchorMax = new Vector2(0.5f, 1f); hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -140f); hrt.sizeDelta = new Vector2(980f, 30f);
            hintText = hintGo.GetComponent<Text>();
            hintText.font = font; hintText.fontSize = 18; hintText.color = new Color(1f, 0.95f, 0.6f);
            hintText.alignment = TextAnchor.MiddleCenter;
            hintGo.SetActive(false);
        }

        void ShowOverlay(string text)
        {
            if (overlay != null) overlay.SetActive(true);
            if (overlayText != null) overlayText.text = text;
        }

        void HideOverlay()
        {
            if (overlay != null) overlay.SetActive(false);
        }
    }
}
