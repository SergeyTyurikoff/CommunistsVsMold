using System.Collections;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Интро-катсцена в Мавзолее (на ЗАГЛУШКАХ — цветные боксы вместо арта).
    /// Сценарий: офицер будит Ленина → диалог → офицер уходит → Ленин идёт следом,
    /// подбирает обрез → офицер превращается в плесень → Ленин расстреливает его
    /// (разлёт на части + кровь на экран) → «Досвидос». Мавзолей — чисто сюжетная
    /// локация (врагов на время сцены убираем). Запускается на Start.
    /// </summary>
    public class IntroCutscene : MonoBehaviour
    {
        [SerializeField] bool playOnStart = true;
        bool played;
        static Sprite box;

        void Start() { if (playOnStart) StartCoroutine(WaitThenBegin()); }

        // Ждём старта игры (меню снято, timeScale>0), потом играем интро.
        IEnumerator WaitThenBegin()
        {
            while (Time.timeScale <= 0f) yield return null;
            yield return null;
            Begin();
        }

        public void Begin()
        {
            if (played) return;
            played = true;
            if (CutsceneManager.Instance != null) CutsceneManager.Instance.Play(Routine());
        }

        IEnumerator Routine()
        {
            var cm = CutsceneManager.Instance;
            var player = GameObject.FindWithTag("Player");
            var ptf = player != null ? player.transform : null;
            float px = ptf != null ? ptf.position.x : 0f;
            float py = ptf != null ? ptf.position.y : 0f;

            // Мавзолей — без боя: убираем врагов на время сюжета.
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb != null && mb.GetType().Name.EndsWith("AI")) Destroy(mb.gameObject);

            // Заглушки.
            var officer = MakeBox("Officer", new Vector3(px + 2.2f, py + 0.9f, 0f), new Vector2(0.9f, 1.8f), new Color(0.72f, 0.12f, 0.1f));
            var sawn = MakeBox("ObrezPickup", new Vector3(px + 5.2f, py + 0.3f, 0f), new Vector2(0.6f, 0.22f), new Color(0.55f, 0.38f, 0.16f));
            MakeBox("Corpse", new Vector3(px + 5.2f, py + 0.15f, 0f), new Vector2(1.5f, 0.4f), new Color(0.4f, 0.4f, 0.42f));

            yield return cm.Say("Ленин", "Поднимите мне веки.");
            yield return cm.Say("Человек", "Наконец-то!");
            yield return cm.Say("Ленин", "Который сейчас час??");
            yield return cm.Say("Человек", "Нас охватила страшная болезнь, только вы нам поможете.");
            yield return cm.Say("Ленин", "Ты не ответил на вопрос.");

            // Человек уходит вправо.
            yield return cm.MoveActor(officer != null ? officer.transform : null, px + 6.8f, 3f);
            yield return cm.Say("Ленин", "Куда ты уходишь?");

            // Ленин идёт следом, проходит мимо трупа, берёт обрез.
            yield return cm.MoveActor(ptf, px + 5.0f, 3.2f);
            if (sawn != null) Destroy(sawn);
            yield return cm.Wait(0.2f);

            yield return cm.Say("Ленин", "Так всё-таки который сейчас час?");

            // Превращение: офицер зеленеет, по полу расходится плесень.
            if (officer != null)
            {
                var sr = officer.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.4f, 0.75f, 0.25f);
            }
            var mold = MakeBox("Mold", new Vector3(px + 6.8f, py + 0.05f, 0f), new Vector2(0.5f, 0.2f), new Color(0.35f, 0.7f, 0.2f, 0.7f));
            StartCoroutine(GrowMold(mold != null ? mold.transform : null));
            yield return cm.Say("Плесень", "Твоё время умирать, ахах!");

            // Ленин не раздумывая стреляет — разлёт на части + кровь на экран.
            Vector3 opos = officer != null ? officer.transform.position : new Vector3(px + 6.8f, py + 0.9f, 0f);
            if (ptf != null) GameFX.Instance?.MuzzleFlash((Vector2)ptf.position + new Vector2(1f, 0.9f), 1);
            GameFX.Instance?.SpawnGibs(opos, new Color(0.4f, 0.75f, 0.25f), 18);   // куски плесени
            GameFX.Instance?.SpawnGibs(opos, new Color(0.55f, 0.05f, 0.05f), 16);  // кровь
            SpawnLimbs(opos);                                                       // руки/ноги
            GameFX.Instance?.BloodSplat(9);
            GameFX.Instance?.Shake(0.45f, 0.55f);
            GameFX.Instance?.HitStop(0.12f);
            if (officer != null) Destroy(officer);
            AudioManager.Instance?.PlayEnemyDeath();

            yield return cm.Wait(0.6f);
            yield return cm.Say("Ленин", "Досвидос.");
            // Конец. Управление вернётся; дальше Ленин выходит вправо (переход по краю — S2).
        }

        // Плесень растёт вширь.
        IEnumerator GrowMold(Transform mold)
        {
            if (mold == null) yield break;
            float w = 0.5f;
            while (mold != null && w < 6f)
            {
                w += Time.deltaTime * 4f;
                mold.localScale = new Vector3(w, 0.25f, 1f);
                yield return null;
            }
        }

        // Руки/ноги — несколько вытянутых кусков с физикой, разлетаются.
        void SpawnLimbs(Vector3 pos)
        {
            EnsureBox();
            var cols = new[] { new Color(0.45f, 0.7f, 0.25f), new Color(0.5f, 0.08f, 0.08f) };
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Limb");
                go.transform.position = pos + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0f, 0.6f), 0f);
                go.transform.localScale = new Vector3(0.5f, 0.16f, 1f);
                go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = box; sr.color = cols[i % 2]; sr.sortingOrder = 1000;
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 2.5f;
                rb.linearVelocity = new Vector2(Random.Range(3f, 8f), Random.Range(4f, 9f));
                rb.angularVelocity = Random.Range(-720f, 720f);
                Destroy(go, 1.2f);
            }
        }

        GameObject MakeBox(string name, Vector3 pos, Vector2 size, Color color)
        {
            EnsureBox();
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = box; sr.color = color; sr.sortingOrder = 3;
            return go;
        }

        static void EnsureBox()
        {
            if (box != null) return;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            box = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
