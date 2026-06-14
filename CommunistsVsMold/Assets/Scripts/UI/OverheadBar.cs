using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Полоса здоровья над головой: для врагов (читает <see cref="Health"/>) и для героя
    /// (читает <see cref="PlayerController"/>). Враги показывают полосу только при уроне,
    /// герой — всегда (alwaysShow). Полоса — отдельный объект, следует за сущностью
    /// (не наследует flip спрайта), уничтожается вместе с ней. Цвет по доле HP.
    /// </summary>
    public class OverheadBar : MonoBehaviour
    {
        [SerializeField] float width = 1.1f;
        [SerializeField] float height = 0.14f;
        [SerializeField] float offsetY = 2.45f;
        [SerializeField] bool alwaysShow = false;

        Health health;
        PlayerController pc;
        Transform bar;
        Transform fill;
        SpriteRenderer fillSr;
        static Sprite white;

        public void SetAlwaysShow(bool v) { alwaysShow = v; }

        void Awake()
        {
            health = GetComponent<Health>();
            pc = GetComponent<PlayerController>();
        }

        bool GetState(out float frac, out bool alive)
        {
            frac = 1f; alive = true;
            if (health != null) { alive = !health.IsDead; frac = Mathf.Clamp01(health.Hp / Mathf.Max(1f, health.MaxHp)); return true; }
            if (pc != null) { alive = !pc.IsDead; frac = Mathf.Clamp01(pc.Health / Mathf.Max(1f, pc.MaxHealth)); return true; }
            return false;
        }

        void LateUpdate()
        {
            if (!GetState(out float frac, out bool alive)) return;

            bool show = alive && (alwaysShow || frac < 0.999f);
            if (!show)
            {
                if (bar != null && bar.gameObject.activeSelf) bar.gameObject.SetActive(false);
                return;
            }

            EnsureBar();
            if (!bar.gameObject.activeSelf) bar.gameObject.SetActive(true);

            bar.position = transform.position + Vector3.up * offsetY;
            bar.rotation = Quaternion.identity;

            fill.localScale = new Vector3(width * frac, height, 1f);
            fill.localPosition = new Vector3(-width * 0.5f + width * frac * 0.5f, 0f, 0f);
            fillSr.color = frac > 0.5f ? new Color(0.45f, 0.9f, 0.3f)
                         : frac > 0.25f ? new Color(1f, 0.8f, 0.2f)
                         : new Color(1f, 0.3f, 0.25f);
        }

        void EnsureBar()
        {
            if (bar != null) return;
            EnsureWhite();

            var root = new GameObject("OverheadBar");
            bar = root.transform;

            var bg = new GameObject("Bg");
            bg.transform.SetParent(bar, false);
            bg.transform.localScale = new Vector3(width + 0.08f, height + 0.06f, 1f);
            var bgsr = bg.AddComponent<SpriteRenderer>();
            bgsr.sprite = white;
            bgsr.color = new Color(0f, 0f, 0f, 0.7f);
            bgsr.sortingOrder = 1200;

            var f = new GameObject("Fill");
            f.transform.SetParent(bar, false);
            fill = f.transform;
            fill.localScale = new Vector3(width, height, 1f);
            fillSr = f.AddComponent<SpriteRenderer>();
            fillSr.sprite = white;
            fillSr.color = new Color(0.45f, 0.9f, 0.3f);
            fillSr.sortingOrder = 1201;

            bar.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (bar != null) Destroy(bar.gameObject);
        }

        static void EnsureWhite()
        {
            if (white != null) return;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
