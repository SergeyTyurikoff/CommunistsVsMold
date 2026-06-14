using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kommunisty
{
    /// <summary>
    /// Оружие героя на основе данных <see cref="WeaponSO"/> + расход патронов через <see cref="AmmoInventory"/>.
    /// Огонь — удержание ЛКМ или J (с учётом fireDelay текущего оружия).
    /// Смена оружия — Q (циклически по списку weapons).
    /// Виды стрельбы (по <see cref="WeaponKind"/>): Gun — одна пуля; Shotgun — веер дроби;
    /// Gas — пока как Gun (одна пуля, позже заменим облаком); Melee — без пули, удар OverlapBox перед игроком.
    /// Снаряды — через пул <see cref="BulletPool"/>. Числа баланса — в ассетах WeaponSO.
    /// Ввод — новый Input System (UnityEngine.InputSystem), чтение через .current с null-guard.
    /// Вешается на объект Player (рядом с <see cref="PlayerController"/> и <see cref="AmmoInventory"/>).
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("Арсенал")]
        [SerializeField] List<WeaponSO> weapons = new List<WeaponSO>();
        [SerializeField] int current = 0;

        [Header("Цель и точка выстрела")]
        [SerializeField] LayerMask targetMask;   // по кому пули/удар (слой Enemy)
        [SerializeField] Transform muzzle;        // откуда вылетают; если null — расчёт от Facing
        [SerializeField] float noiseRadius = 16f; // на каком радиусе враги слышат выстрел

        [Header("Визуал оружия")]
        [SerializeField] SpriteRenderer weaponVisual;   // дочерний "WeaponVisual" на Player, назначается в сцене
        [SerializeField] float handForwardX = 0.28f;    // вынос оружия вперёд «в руку» по направлению взгляда

        PlayerController pc;
        AmmoInventory ammo;
        ComboTracker combo;
        float cooldown;

        // Переиспользуемый буфер под результаты OverlapBox для мили (без аллокаций каждый удар).
        static readonly Collider2D[] MeleeHits = new Collider2D[16];

        // --- Публичное API для HUD ---

        /// <summary>Текущее оружие или null, если список пуст.</summary>
        public WeaponSO CurrentWeapon =>
            (weapons != null && weapons.Count > 0)
                ? weapons[Mathf.Clamp(current, 0, weapons.Count - 1)]
                : null;

        /// <summary>Сколько патронов под текущее оружие (для None — int.MaxValue из AmmoInventory).</summary>
        public int CurrentAmmo
        {
            get
            {
                var w = CurrentWeapon;
                return (w == null || ammo == null) ? 0 : ammo.Get(w.ammo);
            }
        }

        /// <summary>Список оружия (для HUD-переключателя).</summary>
        public IReadOnlyList<WeaponSO> Weapons => weapons;

        /// <summary>Индекс текущего оружия в списке.</summary>
        public int CurrentIndex => (weapons != null && weapons.Count > 0) ? Mathf.Clamp(current, 0, weapons.Count - 1) : -1;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            ammo = GetComponent<AmmoInventory>();
            combo = GetComponent<ComboTracker>();
        }

        void Update()
        {
            if (cooldown > 0f)
                cooldown -= Time.deltaTime;

            if (weapons == null || weapons.Count == 0)
                return;

            current = Mathf.Clamp(current, 0, weapons.Count - 1);

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            // Смена оружия — Q (циклически).
            if (kb != null && kb.qKey.wasPressedThisFrame)
                current = (current + 1) % weapons.Count;

            // Огонь — удержание ЛКМ или J (кулдаун проверяется в Fire).
            bool firing = (mouse != null && mouse.leftButton.isPressed)
                       || (kb != null && kb.jKey.isPressed);

            if (firing)
                Fire();

            // Каждый кадр подгоняем визуал оружия под текущее оружие и направление взгляда.
            UpdateWeaponVisual();
        }

        // Обновляет дочерний спрайт оружия (weapon-overlay): спрайт под текущее оружие,
        // видимость и отзеркаливание по направлению взгляда героя. Полностью null-safe.
        void UpdateWeaponVisual()
        {
            if (weaponVisual == null) return;

            var w = CurrentWeapon;
            weaponVisual.enabled = (w != null && w.overlaySprite != null);
            if (!weaponVisual.enabled) return;

            weaponVisual.sprite = w.overlaySprite;

            // Поворот по взгляду: отзеркаливаем через flipX, не трогая localScale дочернего объекта.
            int facing = pc != null ? pc.Facing : 1;
            weaponVisual.flipX = facing < 0;

            // Выносим оружие вперёд по направлению взгляда (в руку), а не по центру тела.
            // Высоту (localPosition.y) оставляем как выставлено в сцене.
            var lp = weaponVisual.transform.localPosition;
            lp.x = facing * handForwardX;
            weaponVisual.transform.localPosition = lp;
        }

        // Выстрел текущим оружием: проверка кулдауна, наличия данных и патронов, затем стрельба по виду.
        void Fire()
        {
            if (cooldown > 0f) return;
            if (ammo == null) return;
            if (weapons == null || weapons.Count == 0) return;

            WeaponSO w = weapons[Mathf.Clamp(current, 0, weapons.Count - 1)];
            if (w == null) return;

            // Нет патронов — не стреляем (для None Has всегда true).
            if (!ammo.Has(w.ammo, w.ammoPerShot)) return;

            int facing = pc != null ? pc.Facing : 1;
            Vector2 baseDir = AimDir(facing);   // прицел учитывает ↑/↓ (диагонали)

            switch (w.kind)
            {
                case WeaponKind.Melee:
                    DoMelee(w, facing);
                    break;

                case WeaponKind.Shotgun:
                    if (!FireProjectiles(w, baseDir, facing, w.pellets)) return;
                    break;

                case WeaponKind.Gas:   // пока ведёт себя как Gun (одна пуля) — позже заменим облаком.
                case WeaponKind.Gun:
                default:
                    if (!FireProjectiles(w, baseDir, facing, 1)) return;
                    break;
            }

            // Патроны тратим и ставим кулдаун только если выстрел реально состоялся.
            ammo.Use(w.ammo, w.ammoPerShot);
            cooldown = w.fireDelay;
            AudioManager.Instance?.PlayShot(w.kind, w.ammo);
            GunfireAlarm.Report(transform.position, noiseRadius);   // враги слышат выстрел
        }

        // Стрельба снарядами через пул. count=1 → как Gun/Gas; count>1 → веер (Shotgun).
        // Возвращает false, если пул недоступен (выстрел не состоялся — патроны не тратим).
        bool FireProjectiles(WeaponSO w, Vector2 baseDir, int facing, int count)
        {
            if (BulletPool.Instance == null) return false;

            int pellets = Mathf.Max(1, count);
            Vector2 muzzlePos = MuzzlePos(facing);

            for (int i = 0; i < pellets; i++)
            {
                // Веер: поворот базового направления на (i - (pellets-1)/2) * spread радиан.
                float angle = (pellets > 1)
                    ? (i - (pellets - 1) * 0.5f) * w.spread
                    : 0f;

                Vector2 dir = (pellets > 1) ? Rotate(baseDir, angle) : baseDir;

                Bullet b = BulletPool.Instance.Get();
                if (b == null) continue;
                b.Init(muzzlePos, dir, w.projectileSpeed, w.damage * ComboMult(), w.range, w.knockback, targetMask);
                GameFX.Instance?.Tracer(muzzlePos, dir);   // яркая трасса по направлению
            }

            GameFX.Instance?.MuzzleFlash(MuzzlePos(facing), facing);
            return true;
        }

        // Мили-удар: OverlapBox перед игроком, урон каждому IDamageable на targetMask.
        void DoMelee(WeaponSO w, int facing)
        {
            Vector2 center = (Vector2)transform.position
                           + new Vector2(facing * w.meleeRange * 0.5f, 0.9f);
            Vector2 size = new Vector2(Mathf.Max(0.01f, w.meleeRange), Mathf.Max(0.01f, w.meleeArc));
            Vector2 knock = new Vector2(facing, 0f) * w.knockback;

            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(targetMask);
            int n = Physics2D.OverlapBox(center, size, 0f, filter, MeleeHits);
            for (int i = 0; i < n; i++)
            {
                Collider2D col = MeleeHits[i];
                if (col == null) continue;

                IDamageable target = col.GetComponent<IDamageable>();
                if (target == null)
                    target = col.GetComponentInParent<IDamageable>();

                target?.TakeDamage(w.damage * ComboMult(), knock);
            }
        }

        // Точка вылета: muzzle, если задан; иначе смещение от центра игрока по Facing.
        Vector2 MuzzlePos(int facing)
        {
            return muzzle != null
                ? (Vector2)muzzle.position
                : (Vector2)transform.position + new Vector2(facing * 0.6f, 0.9f);
        }

        // Множитель урона от комбо (1, если трекера нет).
        float ComboMult() => combo != null ? combo.Multiplier : 1f;

        // Направление выстрела — к курсору мыши («стреляй, куда наводишь»). Фолбэк — по Facing.
        Vector2 AimDir(int facing)
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse != null && cam != null)
            {
                Vector3 mw = cam.ScreenToWorldPoint(mouse.position.ReadValue());
                Vector2 d = new Vector2(mw.x, mw.y) - MuzzlePos(facing);
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }
            return new Vector2(facing, 0f);
        }

        // Поворот 2D-вектора на угол в радианах.
        static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
