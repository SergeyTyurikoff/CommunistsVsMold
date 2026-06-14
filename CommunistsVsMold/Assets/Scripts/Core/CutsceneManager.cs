using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kommunisty
{
    /// <summary>
    /// Менеджер катсцен: запускает скриптовую последовательность (корутину), на время
    /// которой блокируется управление игроком (PlayerController/WeaponController смотрят
    /// на <see cref="IsPlaying"/>). Даёт хелперы для сценариев: Say (реплика в DialogueUI),
    /// Wait, MoveActor (перемещение персонажа). Реплику можно пропустить кликом/Enter.
    /// </summary>
    public class CutsceneManager : MonoBehaviour
    {
        public static CutsceneManager Instance { get; private set; }
        public static bool IsPlaying { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Запустить катсцену (корутину шагов).</summary>
        public void Play(IEnumerator routine) { StartCoroutine(Run(routine)); }

        IEnumerator Run(IEnumerator routine)
        {
            IsPlaying = true;
            yield return StartCoroutine(routine);
            IsPlaying = false;
            DialogueUI.Instance?.Hide();
        }

        /// <summary>Реплика: показать в панели и подождать (авто по длине текста или клик/Enter).</summary>
        public IEnumerator Say(string speaker, string text, float seconds = 0f)
        {
            DialogueUI.Instance?.Show(speaker, text);
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

        /// <summary>Просто пауза.</summary>
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

        static bool Clicked()
        {
            var m = Mouse.current; var k = Keyboard.current;
            return (m != null && m.leftButton.wasPressedThisFrame)
                || (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame));
        }
    }
}
