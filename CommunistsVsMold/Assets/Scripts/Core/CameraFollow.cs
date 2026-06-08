using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Плавное следование камеры за игроком через Vector3.SmoothDamp.
    /// Z держим из offset; по Y не опускаемся ниже minY (чтобы не уезжать под уровень).
    /// Вешается на Main Camera, target = Player.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 1f, -10f);
        [SerializeField] float smoothTime = 0.15f;
        [SerializeField] float minY = -3f;

        Vector3 vel;

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            if (desired.y < minY) desired.y = minY;
            desired.z = offset.z;

            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
            next.z = offset.z;
            transform.position = next;
        }

        public void SetTarget(Transform t) => target = t;
    }
}
