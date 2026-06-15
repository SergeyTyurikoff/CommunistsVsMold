using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kommunisty
{
    /// <summary>
    /// Менеджер катсцен: проигрывает скриптовую последовательность (корутину), на время
    /// которой блокируется управление игроком (PlayerController/WeaponController смотрят на
    /// <see cref="IsPlaying"/>). Реплики показываются В ОКОШКЕ НАД ГОЛОВОЙ говорящего
    /// (мировое облачко, следует за персонажем). Хелперы: Say/Wait/MoveActor.
    /// Реплику можно пропустить кликом/Enter.
    /// </summary>
    public class CutsceneManager : MonoBehaviour
    {
        public static CutsceneManager Instance { get; private set; }
        public static bool IsPlaying { get; private set; }

        [SerializeField] float headOffset = 2.6f;   // высота окошка над персонажем

        // Облачко-реплика (мировое).
        Transform bubble, box, border;
        TextMesh speakerTM, bodyTM;
        Transform target;
        static Sprite white;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void LateUpdate()
        {
            if (bubble != null && bubble.gameObject.activeSelf && target != null)
            {
                bubble.position = target.position + Vector3.up * headOffset;
                bubble.rotation = Quaternion.identity;
            }
        }

        public void Play(IEnumerator routine) { StartCoroutine(Run(routine)); }

        IEnumerator Run(IEnumerator routine)
        {
            IsPlaying = true;
            yield return StartCoroutine(routine);
            IsPlaying = false;
            HideBubble();
        }

        /// <summary>Реплика В ОКОШКЕ НАД ГОЛОВОЙ персонажа over. Ждёт (авто по длине / клик/Enter).</summary>
        public IEnumerator Say(Transform over, string speaker, string text, float seconds = 0f)
        {
            ShowBubble(over, speaker, text);
            float min = 0.45f;
            float auto = seconds > 0f ? seconds : Mathf.Clamp(1.6f + text.Length * 0.05f, 1.8f, 6f);
            float t = 0f;
            while (t < auto)
            {
                t += Time.unscaledDeltaTime;
                if (t > min && Clicked()) break;   // пропуск реплики
                yield return null;
            }
        }

        public IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        /// <summary>Переместить персонажа к мировому X со скоростью speed (через transform).</summary>
        public IEnumerator MoveActor(Transform tr, float targetX, float speed)
        {
            if (tr == null) yield break;
            while (tr != null && Mathf.Abs(tr.position.x - targetX) > 0.08f)
            {
                var p = tr.position;
                p.x = Mathf.MoveTowards(p.x, targetX, speed * Time.deltaTime);
                tr.position = p;
                yield return null;
            }
        }

        // ───────────── облачко над головой ─────────────

        void ShowBubble(Transform over, string speaker, string text)
        {
            EnsureBubble();
            target = over;

            // Перенос длинного текста по словам (TextMesh сам не переносит).
            string wrapped = Wrap(text, 22);
            int lines = 1; foreach (var c in wrapped) if (c == '\n') lines++;
            float longest = LongestLine(wrapped);

            bodyTM.text = wrapped;
            speakerTM.text = speaker;

            float w = Mathf.Clamp(longest * 0.135f + 0.5f, 1.6f, 4.2f);
            float h = 0.42f + lines * 0.34f;          // место под имя + строки
            box.localScale = new Vector3(w, h, 1f);
            border.localScale = new Vector3(w + 0.14f, h + 0.14f, 1f);
            speakerTM.transform.localPosition = new Vector3(0f, h * 0.5f - 0.18f, 0f);
            bodyTM.transform.localPosition = new Vector3(0f, h * 0.5f - 0.5f, 0f);

            if (over != null) bubble.position = over.position + Vector3.up * headOffset;
            bubble.gameObject.SetActive(true);
        }

        void HideBubble() { if (bubble != null) bubble.gameObject.SetActive(false); }

        void EnsureBubble()
        {
            if (bubble != null) return;
            EnsureWhite();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = new GameObject("CutsceneBubble");
            bubble = root.transform;

            border = MakeQuad("Border", new Color(0.78f, 0.12f, 0.1f, 0.95f), 1298).transform; // красная рамка
            box = MakeQuad("Box", new Color(0.05f, 0.04f, 0.03f, 0.92f), 1300).transform;       // тёмная подложка

            speakerTM = MakeText(font, 44, new Color(1f, 0.82f, 0.11f), FontStyle.Bold, 1302);
            bodyTM = MakeText(font, 40, Color.white, FontStyle.Normal, 1302);

            bubble.gameObject.SetActive(false);
        }

        GameObject MakeQuad(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bubble, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = white; sr.color = color; sr.sortingOrder = order;
            return go;
        }

        TextMesh MakeText(Font font, int fontSize, Color color, FontStyle style, int order)
        {
            var go = new GameObject("T");
            go.transform.SetParent(bubble, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = font; tm.fontSize = fontSize; tm.characterSize = 0.04f; tm.fontStyle = style;
            tm.anchor = TextAnchor.UpperCenter; tm.alignment = TextAlignment.Center; tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = order;
            return tm;
        }

        static string Wrap(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder();
            int lineLen = 0;
            foreach (var word in text.Split(' '))
            {
                if (lineLen > 0 && lineLen + 1 + word.Length > maxChars) { sb.Append('\n'); lineLen = 0; }
                else if (lineLen > 0) { sb.Append(' '); lineLen++; }
                sb.Append(word); lineLen += word.Length;
            }
            return sb.ToString();
        }

        static float LongestLine(string wrapped)
        {
            int max = 0, cur = 0;
            foreach (var c in wrapped) { if (c == '\n') { if (cur > max) max = cur; cur = 0; } else cur++; }
            if (cur > max) max = cur;
            return max;
        }

        static void EnsureWhite()
        {
            if (white != null) return;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        static bool Clicked()
        {
            var m = Mouse.current; var k = Keyboard.current;
            return (m != null && m.leftButton.wasPressedThisFrame)
                || (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame));
        }
    }
}
