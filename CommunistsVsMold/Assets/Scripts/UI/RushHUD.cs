using UnityEngine;
using UnityEngine.UI;

namespace Kommunisty
{
    /// <summary>
    /// HUD раша: по центру показывает обратный отсчёт «часов», текст мини-цели и
    /// её прогресс, затем «волна отбита». Данные берёт из <see cref="RushManager"/>.
    /// label — Text-объект на Canvas; задаётся в инспекторе.
    /// </summary>
    public class RushHUD : MonoBehaviour
    {
        [SerializeField] Text label;

        void Start()
        {
            if (RushManager.Instance != null) RushManager.Instance.OnRushChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (RushManager.Instance != null) RushManager.Instance.OnRushChanged -= Refresh;
        }

        void Refresh()
        {
            if (label == null) return;
            var rm = RushManager.Instance;
            if (rm == null) { label.text = ""; return; }

            switch (rm.Phase)
            {
                case RushPhase.Warning:
                    label.text = $"РАШ через {rm.Timer:0.0}";
                    break;
                case RushPhase.Active:
                    label.text = rm.Goal == RushGoal.KillWave
                        ? $"ПЕРЕБЕЙ ВОЛНУ: {rm.Killed}/{rm.WaveSize}"
                        : $"ПРОДЕРЖИСЬ: {rm.Timer:0.0}";
                    break;
                case RushPhase.Done:
                    label.text = "ВОЛНА ОТБИТА! +лечение";
                    break;
                default:
                    label.text = "";
                    break;
            }
        }
    }
}
