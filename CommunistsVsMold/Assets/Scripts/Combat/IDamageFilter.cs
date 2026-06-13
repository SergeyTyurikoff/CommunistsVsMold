using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Необязательный модификатор входящего урона. Если на объекте с <see cref="Health"/>
    /// есть компонент, реализующий этот интерфейс, — <see cref="Health.TakeDamage"/> прогоняет
    /// урон через <see cref="ModifyDamage"/> перед применением.
    /// Используется щитоносцем (<c>ShielderAI</c>): фронтальный урон режется ×0.12.
    /// </summary>
    public interface IDamageFilter
    {
        /// <summary>Вернуть итоговый урон. knockback — вектор отброса от источника
        /// (его X-знак позволяет понять, спереди прилетело или сзади).</summary>
        float ModifyDamage(float dmg, Vector2 knockback);
    }
}
