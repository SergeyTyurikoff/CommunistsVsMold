using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kommunisty
{
    /// <summary>
    /// Оружие героя: стрельба снарядами через пул (BulletPool).
    /// Огонь — удержание ЛКМ или J (с учётом fireDelay каждого оружия).
    /// Смена оружия — Q (циклически). Числа оружия — в инспекторе (weapons).
    /// Ввод — новый Input System (UnityEngine.InputSystem), чтение через .current с null-guard.
    /// Вешается на объект Player (рядом с PlayerController).
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        /// <summary>Описание одного оружия. Настраивается в инспекторе.</summary>
        [System.Serializable]
        public class WeaponDef
        {
            public string name;
            public float damage;
            public float fireDelay;       // задержка между выстрелами, сек
            public float projectileSpeed; // скорость пули, м/с
            public float range;           // дальность полёта пули, м
            public float knockback;       // импульс отбрасывания цели
        }

        [Header("Арсенал")]
        [SerializeField] List<WeaponDef> weapons = new List<WeaponDef>();
        [SerializeField] int current = 0;

        [Header("Цель и точка выстрела")]
        [SerializeField] LayerMask targetMask;  // по кому пули (слой Enemy)
        [SerializeField] Transform muzzle;       // откуда вылетают; если null — расчёт от Facing

        PlayerController pc;
        float cooldown;

        void Reset()
        {
            EnsureDefaults();
        }

        void Awake()
        {
            EnsureDefaults();
            pc = GetComponent<PlayerController>();
            current = ClampIndex(current);
        }

        // Заполняет список оружия дефолтами, если он пуст.
        void EnsureDefaults()
        {
            if (weapons != null && weapons.Count > 0) return;

            weapons = new List<WeaponDef>
            {
                new WeaponDef { name = "Пистолет", damage = 18f, fireDelay = 0.27f, projectileSpeed = 18f, range = 9f, knockback = 3f },
                new WeaponDef { name = "Газомёт",  damage = 6f,  fireDelay = 0.12f, projectileSpeed = 10f, range = 5f, knockback = 1f },
            };
        }

        void Update()
        {
            if (cooldown > 0f)
                cooldown -= Time.deltaTime;

            if (weapons == null || weapons.Count == 0)
                return;

            current = ClampIndex(current);

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            // Смена оружия — Q (циклически).
            if (kb != null && kb.qKey.wasPressedThisFrame)
                current = (current + 1) % weapons.Count;

            // Огонь — удержание ЛКМ или J.
            bool firing = (mouse != null && mouse.leftButton.isPressed)
                       || (kb != null && kb.jKey.isPressed);

            if (firing)
                Fire();
        }

        // Выстрел текущим оружием, если прошёл кулдаун и пул доступен.
        void Fire()
        {
            if (cooldown > 0f) return;
            if (BulletPool.Instance == null) return;
            if (weapons == null || weapons.Count == 0) return;

            WeaponDef w = weapons[ClampIndex(current)];

            int facing = pc != null ? pc.Facing : 1;
            Vector2 dir = new Vector2(facing, 0f);

            Vector2 muzzlePos = muzzle != null
                ? (Vector2)muzzle.position
                : (Vector2)transform.position + new Vector2(facing * 0.6f, 0.9f);

            Bullet b = BulletPool.Instance.Get();
            b.Init(muzzlePos, dir, w.projectileSpeed, w.damage, w.range, w.knockback, targetMask);

            cooldown = w.fireDelay;
        }

        // Безопасный индекс в пределах списка (на случай правок в инспекторе).
        int ClampIndex(int i)
        {
            if (weapons == null || weapons.Count == 0) return 0;
            return Mathf.Clamp(i, 0, weapons.Count - 1);
        }
    }
}
