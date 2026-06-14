using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// Простая система всплывающих сообщений (тостов), R24. Вызов: Toast.Show("текст", 2f).
    /// Самосоздающийся синглтон: при первом Show создаёт GameObject со своим Canvas
    /// (ScreenSpaceOverlay, высокий sortingOrder) и Text по центру-сверху. Текст висит
    /// заданное число секунд, затем плавно гаснет (alpha → 0). Очереди нет — повторный
    /// Show просто перебивает предыдущее сообщение и сбрасывает таймер. Внешних зависимостей нет.
    /// </summary>
    public class Toast : MonoBehaviour
    {
        const float FadeTime = 0.5f;   // длительность плавного гашения

        static Toast instance;

        Text text;
        CanvasGroup group;
        float holdTimer;     // сколько ещё держать на полной видимости
        float fadeTimer;     // сколько ещё гаснуть (0 = не гаснем)

        /// <summary>Показать тост. seconds — сколько держать на полной видимости до гашения.</summary>
        public static void Show(string msg, float seconds = 2f)
        {
            EnsureInstance();
            instance.ShowInternal(msg, seconds);
        }

        static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("Toast");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Toast>();
            instance.Build();
        }

        void Build()
        {
            // Свой Canvas поверх всего.
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // Текст по центру-сверху.
            var txtGO = new GameObject("ToastText", typeof(RectTransform));
            txtGO.transform.SetParent(transform, false);
            var rt = txtGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -90f);
            rt.sizeDelta = new Vector2(1200f, 80f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            text = txtGO.AddComponent<Text>();
            text.font = font;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 1f, 1f, 1f);
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            // Тень для читаемости на любом фоне.
            var shadow = txtGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        void ShowInternal(string msg, float seconds)
        {
            if (text != null) text.text = msg;
            if (group != null) group.alpha = 1f;
            holdTimer = Mathf.Max(0f, seconds);
            fadeTimer = 0f;
        }

        void Update()
        {
            if (group == null) return;

            // Таймеры тоста живут в реальном времени — не зависят от Time.timeScale
            // (на случай паузы/стоп-времени с timeScale=0).
            float dt = Time.unscaledDeltaTime;

            if (holdTimer > 0f)
            {
                holdTimer -= dt;
                if (holdTimer <= 0f) fadeTimer = FadeTime;
            }
            else if (fadeTimer > 0f)
            {
                fadeTimer -= dt;
                group.alpha = Mathf.Clamp01(fadeTimer / FadeTime);
            }
        }
    }
}
