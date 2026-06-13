using UnityEngine;
using UnityEngine.InputSystem;

namespace Kommunisty
{
    /// <summary>
    /// Утилиты-слоты игрока (цифры, PORT_SPEC §3): 1 — аптечка (лечит 38, макс 9),
    /// 2 — противогаз (действует 3 c, откат 5 c; даёт иммунитет к газу). Висит на Player
    /// рядом с <see cref="PlayerController"/>. Газ-облако спрашивает <see cref="GasImmune"/>.
    /// </summary>
    public class UtilityInventory : MonoBehaviour
    {
        [Header("Аптечка (клавиша 1)")]
        [SerializeField] int medkits = 1;
        [SerializeField] int maxMedkits = 9;
        [SerializeField] float medkitHeal = 38f;

        [Header("Противогаз (клавиша 2) — 3 c действия / 5 c откат")]
        [SerializeField] float maskDuration = 3f;
        [SerializeField] float maskCooldown = 5f;

        PlayerController pc;
        float maskTimer, maskCdTimer;

        public int Medkits => medkits;
        public bool MaskActive => maskTimer > 0f;
        /// <summary>Иммунитет к газу, пока противогаз надет.</summary>
        public bool GasImmune => maskTimer > 0f;
        public float MaskCooldownLeft => Mathf.Max(0f, maskCdTimer);
        public event System.Action OnChanged;

        void Awake() => pc = GetComponent<PlayerController>();

        /// <summary>Положить аптечки в слот (с пикапа/магазина). Вернёт true, если что-то добавилось.</summary>
        public bool AddMedkit(int n)
        {
            if (n <= 0) return false;
            int before = medkits;
            medkits = Mathf.Min(maxMedkits, medkits + n);
            if (medkits != before) { OnChanged?.Invoke(); return true; }
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (maskTimer > 0f)
            {
                maskTimer -= dt;
                if (maskTimer <= 0f) { maskCdTimer = maskCooldown; OnChanged?.Invoke(); }
            }
            else if (maskCdTimer > 0f) maskCdTimer -= dt;

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) UseMedkit();
            if (kb.digit2Key.wasPressedThisFrame) ActivateMask();
        }

        void UseMedkit()
        {
            if (medkits <= 0 || pc == null) return;
            if (pc.Health >= pc.MaxHealth) return;   // не тратить впустую
            medkits--;
            pc.Heal(medkitHeal);
            OnChanged?.Invoke();
        }

        void ActivateMask()
        {
            if (maskTimer > 0f || maskCdTimer > 0f) return;  // уже надет или на откате
            maskTimer = maskDuration;
            OnChanged?.Invoke();
        }
    }
}
