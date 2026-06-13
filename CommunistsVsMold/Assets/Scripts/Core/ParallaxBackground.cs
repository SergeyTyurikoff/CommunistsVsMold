using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Параллакс-фон биома: следует за камерой с коэффициентом (0 — статичен в мире,
    /// 1 — намертво за камерой). Спрайт делается крупным, чтобы не было видно краёв.
    /// Вешается на фоновый объект со SpriteRenderer позади всего (низкий sortingOrder).
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] Transform cam;
        [SerializeField, Range(0f, 1f)] float factor = 0.5f;
        [SerializeField] bool followY = true;

        Vector3 startPos;
        Vector3 camStart;

        void Start()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            startPos = transform.position;
            if (cam != null) camStart = cam.position;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Vector3 d = cam.position - camStart;
            transform.position = new Vector3(
                startPos.x + d.x * factor,
                followY ? startPos.y + d.y * factor : startPos.y,
                startPos.z);
        }
    }
}
