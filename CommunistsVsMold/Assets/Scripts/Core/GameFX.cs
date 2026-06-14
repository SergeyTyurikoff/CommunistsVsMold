using System.Collections;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Синглтон «сочности»: hit-stop (заморозка времени), screen shake и разлёт кусков (gibs).
    /// Вешается на один объект в сцене (объект с компонентом GameFX).
    /// Все методы безопасны: при отсутствии зависимостей тихо игнорируются.
    /// </summary>
    public class GameFX : MonoBehaviour
    {
        public static GameFX Instance { get; private set; }

        // Кэш ссылки на камеру-следящую.
        CameraFollow cameraFollow;

        // Идёт ли сейчас hit-stop, и до какого реального времени он продлён.
        bool hitStopActive;
        float hitStopEndRealtime;

        // Статичный белый спрайт 1x1 для кусков (создаётся один раз).
        static Sprite gibSprite;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────────────────────── Hit-stop ─────────────────────────

        /// <summary>
        /// Краткая заморозка времени (timeScale = 0) на seconds реальных секунд.
        /// Если hit-stop уже идёт — не накладываем вторую корутину, а лишь продлеваем длительность.
        /// </summary>
        public void HitStop(float seconds)
        {
            if (seconds <= 0f) return;

            float targetEnd = Time.realtimeSinceStartup + seconds;

            if (hitStopActive)
            {
                // Продлеваем, если новый конец дальше текущего.
                if (targetEnd > hitStopEndRealtime) hitStopEndRealtime = targetEnd;
                return;
            }

            hitStopActive = true;
            hitStopEndRealtime = targetEnd;
            StartCoroutine(HitStopRoutine());
        }

        IEnumerator HitStopRoutine()
        {
            float prevScale = Time.timeScale;
            Time.timeScale = 0f;

            // Ждём, пока не пройдёт реальное время (с учётом возможного продления).
            while (Time.realtimeSinceStartup < hitStopEndRealtime)
            {
                yield return null;
            }

            // Возвращаем нормальный ход времени.
            Time.timeScale = (prevScale > 0f) ? prevScale : 1f;
            hitStopActive = false;
        }

        // ───────────────────────── Screen shake ─────────────────────────

        /// <summary>
        /// Тряска камеры. Если CameraFollow не найден — тихо игнорируем.
        /// </summary>
        public void Shake(float duration, float magnitude)
        {
            if (duration <= 0f || magnitude <= 0f) return;

            if (cameraFollow == null)
            {
                if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
                if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow>();
            }

            if (cameraFollow != null) cameraFollow.AddShake(duration, magnitude);
        }

        // ───────────────────────── Gibs (разлёт кусков) ─────────────────────────

        /// <summary>
        /// Разлёт count мелких кусков из точки pos заданного цвета.
        /// </summary>
        public void SpawnGibs(Vector2 pos, Color color, int count)
        {
            if (count <= 0) return;

            EnsureGibSprite();

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Gib");
                go.transform.position = pos;
                float s = Random.Range(0.1f, 0.25f);
                go.transform.localScale = new Vector3(s, s, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = gibSprite;
                sr.color = color;
                sr.sortingOrder = 1000;

                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 2.5f;

                // Радиальный вектор: вверх-вбок.
                float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad; // верхняя полусфера
                float speed = Random.Range(2f, 6f);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                rb.linearVelocity = dir * speed;
                rb.angularVelocity = Random.Range(-720f, 720f);

                // Затухание альфы и самоуничтожение.
                go.AddComponent<GibFade>().Init(0.8f);
            }
        }

        static void EnsureGibSprite()
        {
            if (gibSprite != null) return;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            gibSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        // ───────────────────────── Числа урона ─────────────────────────

        /// <summary>Всплывающее число урона над целью (мировой TextMesh, поднимается и тает).</summary>
        public void SpawnDamageNumber(Vector2 pos, float dmg)
        {
            int val = Mathf.RoundToInt(dmg);
            if (val <= 0) return;

            var go = new GameObject("DamageNumber");
            go.transform.position = (Vector3)pos + new Vector3(Random.Range(-0.2f, 0.2f), 0.8f, 0f);

            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = val.ToString();
            tm.fontSize = 48;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.95f, 0.5f, 1f);

            var mr = go.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;   // TextMesh без своего материала не рисуется
            mr.sortingOrder = 1200;

            go.AddComponent<DamageNumber>().Init(0.7f);
        }

        /// <summary>Метка хедшота: красная всплывающая надпись «ХЕДШОТ!».</summary>
        public void Headshot(Vector2 pos)
        {
            var go = new GameObject("Headshot");
            go.transform.position = (Vector3)pos + new Vector3(0f, 0.4f, 0f);

            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = "ХЕДШОТ!";
            tm.fontSize = 44;
            tm.characterSize = 0.05f;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.32f, 0.2f, 1f);

            var mr = go.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = 1250;

            go.AddComponent<DamageNumber>().Init(0.6f);
        }

        // ───────────────────────── Дульная вспышка ─────────────────────────

        /// <summary>Дульная вспышка: яркое вытянутое ядро + внешнее свечение + искры по направлению.</summary>
        public void MuzzleFlash(Vector2 pos, int facing)
        {
            EnsureGibSprite();

            // Ядро — вытянуто по направлению выстрела, яркое.
            var core = new GameObject("MuzzleFlash");
            core.transform.position = (Vector3)pos;
            core.transform.localScale = new Vector3(0.95f * (facing < 0 ? -1f : 1f), 0.5f, 1f);
            var sr = core.AddComponent<SpriteRenderer>();
            sr.sprite = gibSprite; sr.color = new Color(1f, 0.96f, 0.65f, 0.95f); sr.sortingOrder = 950;
            core.AddComponent<GibFade>().Init(0.07f);

            // Внешнее свечение — крупнее и теплее, тусклее.
            var glow = new GameObject("MuzzleGlow");
            glow.transform.position = (Vector3)pos;
            glow.transform.localScale = Vector3.one * 0.8f;
            var gsr = glow.AddComponent<SpriteRenderer>();
            gsr.sprite = gibSprite; gsr.color = new Color(1f, 0.6f, 0.15f, 0.5f); gsr.sortingOrder = 949;
            glow.AddComponent<GibFade>().Init(0.1f);

            // Искры, летящие по направлению выстрела.
            for (int i = 0; i < 3; i++)
            {
                var sp = new GameObject("Spark");
                sp.transform.position = (Vector3)pos;
                float s = Random.Range(0.06f, 0.12f);
                sp.transform.localScale = new Vector3(s, s, 1f);
                var ssr = sp.AddComponent<SpriteRenderer>();
                ssr.sprite = gibSprite; ssr.color = new Color(1f, 0.85f, 0.4f, 0.9f); ssr.sortingOrder = 951;
                var rb = sp.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0.5f;
                rb.linearVelocity = new Vector2((facing < 0 ? -1f : 1f) * Random.Range(6f, 11f), Random.Range(-2.5f, 2.5f));
                sp.AddComponent<GibFade>().Init(0.15f);
            }
        }

        /// <summary>Трассер: яркая короткая полоса по направлению выстрела (быстро гаснет).</summary>
        public void Tracer(Vector2 from, Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            EnsureGibSprite();
            dir = dir.normalized;
            const float len = 1.15f;
            var go = new GameObject("Tracer");
            go.transform.position = (Vector3)(from + dir * (len * 0.5f));
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            go.transform.localScale = new Vector3(len, 0.08f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = gibSprite; sr.color = new Color(1f, 0.92f, 0.5f, 0.85f); sr.sortingOrder = 940;
            go.AddComponent<GibFade>().Init(0.06f);
        }
    }

    /// <summary>
    /// Лёгкое затухание альфы куска и самоуничтожение через lifetime секунд.
    /// </summary>
    public class GibFade : MonoBehaviour
    {
        SpriteRenderer sr;
        float lifetime = 0.8f;
        float t;
        Color baseColor;

        public void Init(float life)
        {
            lifetime = (life > 0f) ? life : 0.8f;
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) baseColor = sr.color;
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            if (sr == null) return;
            t += Time.deltaTime;
            // Затухаем в последней трети жизни.
            float k = Mathf.Clamp01(1f - (t / lifetime));
            var c = baseColor;
            c.a = baseColor.a * k;
            sr.color = c;
        }
    }

    /// <summary>Всплывающее число урона: поднимается и затухает, затем самоуничтожается.</summary>
    public class DamageNumber : MonoBehaviour
    {
        float life = 0.7f, t;
        TextMesh tm;
        Color baseColor;

        public void Init(float l)
        {
            life = l > 0f ? l : 0.7f;
            tm = GetComponent<TextMesh>();
            if (tm != null) baseColor = tm.color;
            Destroy(gameObject, life);
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * (Time.deltaTime * 1.3f);
            if (tm != null)
            {
                var c = baseColor;
                c.a = Mathf.Clamp01(1f - t / life);
                tm.color = c;
            }
        }
    }
}
