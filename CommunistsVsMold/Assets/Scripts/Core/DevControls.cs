using UnityEngine;
using UnityEngine.InputSystem;

namespace Kommunisty
{
    /// <summary>
    /// Dev/служебные клавиши (PORT_SPEC): T — следующий биом, Y — предыдущий биом,
    /// R — рестарт текущего биома (с начала). Через <see cref="BiomeManager"/>.
    /// </summary>
    public class DevControls : MonoBehaviour
    {
        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || BiomeManager.Instance == null) return;
            if (kb.tKey.wasPressedThisFrame) BiomeManager.Instance.NextBiome();
            if (kb.yKey.wasPressedThisFrame) BiomeManager.Instance.PrevBiome();
            if (kb.rKey.wasPressedThisFrame) BiomeManager.Instance.RestartBiome();
        }
    }
}
