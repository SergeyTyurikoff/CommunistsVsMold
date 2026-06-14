using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Реплики врага в «облачке» над головой (порт alertText/alertTimer из entities.js):
    /// тёмное окошко с жёлтым текстом ~1.8 c. Триггеры: реплика при обнаружении игрока
    /// (сближение) + редкая болтовня рядом. Враг может явно сказать фразу через Say().
    /// Облачко — отдельный объект, следующий за врагом (чтобы не наследовать flip спрайта),
    /// уничтожается вместе с врагом.
    /// </summary>
    public class EnemySpeech : MonoBehaviour
    {
        static readonly string[] Lines =
        {
            "Где мой паёк?", "Не дыши на грибницу.", "Кто выключил вечность?",
            "Смена тревожная.", "За плесень!", "Стой, гражданин!", "Тут не пройдёшь."
        };

        [SerializeField] float detectRange = 8f;    // на этой дистанции враг «замечает» и говорит
        [SerializeField] float chatterRange = 14f;  // в этой зоне иногда болтает
        [SerializeField] float headOffset = 3.3f;   // высота облачка над врагом (выше полосы HP, чтобы не перекрывать)

        Transform player;
        Transform bubble;       // корень облачка (не дочерний врагу)
        TextMesh tm;
        Transform box;
        Transform border;
        float timer;
        bool aware;

        static Sprite whiteSprite;

        void Awake()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) player = p.transform; else return;
            }

            float dx = Mathf.Abs(player.position.x - transform.position.x);
            float dy = Mathf.Abs(player.position.y - transform.position.y);
            float dist = dx + dy * 0.5f;

            // Реплика при обнаружении (один раз на сближение).
            if (!aware && dist < detectRange) { aware = true; Say(Pick()); }
            else if (aware && dist > detectRange + 3f) aware = false;

            // Редкая болтовня, пока игрок рядом.
            if (timer <= 0f && aware && dist < chatterRange && Random.value < 0.004f) Say(Pick());

            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                if (bubble != null)
                {
                    bubble.position = transform.position + Vector3.up * headOffset;
                    bubble.rotation = Quaternion.identity;
                    if (timer <= 0f) bubble.gameObject.SetActive(false);
                }
            }
        }

        string Pick()
        {
            int i = Mathf.Abs(GetInstanceID() * 31 + Mathf.RoundToInt(Time.time * 9f)) % Lines.Length;
            return Lines[i];
        }

        /// <summary>Показать реплику над врагом на seconds секунд.</summary>
        public void Say(string text, float seconds = 1.8f)
        {
            if (string.IsNullOrEmpty(text)) return;
            EnsureBubble();
            tm.text = text;
            float w = Mathf.Max(1.4f, text.Length * 0.16f);
            box.localScale = new Vector3(w, 0.55f, 1f);
            if (border != null) border.localScale = new Vector3(w + 0.14f, 0.69f, 1f);
            timer = seconds;
            bubble.gameObject.SetActive(true);
        }

        void EnsureBubble()
        {
            if (bubble != null) return;
            EnsureWhite();

            var root = new GameObject("SpeechBubble");
            bubble = root.transform;
            bubble.position = transform.position + Vector3.up * headOffset;

            // Жёлтая рамка позади — даёт контраст на тёмном фоне (как strokeRect в оригинале).
            var bd = new GameObject("Border");
            bd.transform.SetParent(bubble, false);
            border = bd.transform;
            border.localScale = new Vector3(1.74f, 0.69f, 1f);
            var brd = bd.AddComponent<SpriteRenderer>();
            brd.sprite = whiteSprite;
            brd.color = new Color(1f, 0.82f, 0.11f, 0.9f);
            brd.sortingOrder = 1299;

            // Тёмная подложка.
            var b = new GameObject("Box");
            b.transform.SetParent(bubble, false);
            box = b.transform;
            box.localScale = new Vector3(1.6f, 0.55f, 1f);
            var bsr = b.AddComponent<SpriteRenderer>();
            bsr.sprite = whiteSprite;
            bsr.color = new Color(0.07f, 0.04f, 0.02f, 0.96f);
            bsr.sortingOrder = 1300;

            // Текст.
            var t = new GameObject("Text");
            t.transform.SetParent(bubble, false);
            t.transform.localPosition = Vector3.zero;
            tm = t.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.fontSize = 44;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.82f, 0.11f, 1f);
            var mr = t.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = 1301;

            bubble.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (bubble != null) Destroy(bubble.gameObject);
        }

        static void EnsureWhite()
        {
            if (whiteSprite != null) return;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
