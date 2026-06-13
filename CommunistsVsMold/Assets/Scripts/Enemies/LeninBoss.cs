using System.Collections;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Финальный босс «Ленин» — танк-таран (PORT_SPEC §6).
    /// Огромный (~¾ экрана) и медленный. Вместо обычного залпа выполняет ТАРАН:
    /// вынд-ап с красной стрелой-телеграфом в сторону игрока (игрок успевает отойти),
    /// затем рывок в зафиксированном направлении; при контакте в рывке — тяжёлый урон,
    /// сильный отброс, тряска и hit-stop; после — длинный откат.
    /// Боссов топтать нельзя (механики стомпа нет) — отдельной обработки не требуется.
    /// </summary>
    public class LeninBoss : BossController
    {
        [Header("Ленин: габариты")]
        [Tooltip("Множитель размера (¾ экрана).")]
        [SerializeField] float bodyScale = 3.5f;
        [Tooltip("Базовая скорость подхода Ленина (низкая, переопределяет поле базы).")]
        [SerializeField] float leninMoveSpeed = 1.6f;

        [Header("Ленин: таран")]
        [Tooltip("Длительность вынд-апа (телеграф стрелой), секунд (34 кадра @60fps ≈ 0.57с).")]
        [SerializeField] float windUpTime = 0.57f;
        [Tooltip("Скорость рывка, юнитов/с.")]
        [SerializeField] float ramSpeed = 12f;
        [Tooltip("Длительность рывка, секунд (42 кадра @60fps = 0.7с).")]
        [SerializeField] float ramTime = 0.7f;
        [Tooltip("Откат после рывка, секунд (260 кадров @60fps ≈ 4.33с).")]
        [SerializeField] float ramCooldown = 4.33f;
        [Tooltip("Дистанция, ближе которой начинается вынд-ап тарана, юнитов.")]
        [SerializeField] float ramTriggerRange = 11f;

        [Header("Ленин: урон тарана")]
        [Tooltip("Тяжёлый урон игроку при попадании рывком.")]
        [SerializeField] float ramDamage = 34f;
        [Tooltip("Дистанция контакта с игроком во время рывка, юнитов.")]
        [SerializeField] float ramContactRange = 2.2f;
        [Tooltip("Горизонтальный импульс отброса игрока при таране.")]
        [SerializeField] float ramKnockX = 20f;
        [Tooltip("Вертикальный импульс отброса игрока при таране (вверх).")]
        [SerializeField] float ramKnockUp = 8f;

        [Header("Ленин: отдача тарана")]
        [Tooltip("Амплитуда тряски при попадании тараном.")]
        [SerializeField] float ramShakeAmp = 0.45f;
        [Tooltip("Длительность тряски при попадании тараном, секунд.")]
        [SerializeField] float ramShakeDur = 0.3f;
        [Tooltip("Hit-stop при попадании тараном, секунд.")]
        [SerializeField] float ramHitStop = 0.09f;
        [Tooltip("Лёгкая тряска в начале рывка.")]
        [SerializeField] float ramStartShake = 0.18f;

        [Header("Ленин: стрела-телеграф")]
        [Tooltip("Цвет красной стрелы-телеграфа.")]
        [SerializeField] Color arrowColor = new Color(1f, 0.12f, 0.1f, 1f);
        [Tooltip("Длина стрелы-телеграфа, юнитов.")]
        [SerializeField] float arrowLength = 4.5f;
        [Tooltip("Толщина стрелы-телеграфа, юнитов.")]
        [SerializeField] float arrowWidth = 0.18f;

        bool isRamming;          // идёт ли цикл тарана (вынд-ап/рывок/откат) — не запускать второй

        protected override void Awake()
        {
            base.Awake();

            // Огромный и медленный: размер ~¾ экрана, низкая скорость.
            transform.localScale = new Vector3(bodyScale, bodyScale, 1f);
            moveSpeed = leninMoveSpeed;
        }

        // Переопределяем атаку: вместо залпа Ленин таранит.
        // Таран длиннее, чем телеграф базы, поэтому запускаем собственную корутину
        // и не даём стартовать новой, пока текущая идёт.
        protected override void DoAttack()
        {
            if (isRamming) return;
            if (playerTf == null) return;

            // Таранить имеет смысл, только если игрок в пределах ramTriggerRange.
            float dist = Mathf.Abs(playerTf.position.x - transform.position.x);
            if (dist > ramTriggerRange) return;

            StartCoroutine(RamRoutine());
        }

        // Полный цикл тарана: вынд-ап со стрелой → рывок → откат.
        IEnumerator RamRoutine()
        {
            isRamming = true;

            // --- ВЫНД-АП: красная стрела-телеграф в сторону игрока, босс стоит ---
            int dir = (playerTf != null && playerTf.position.x >= transform.position.x) ? 1 : -1;
            GameObject arrow = SpawnArrowTelegraph(dir);
            GameFX.Instance?.Shake(0.12f, 0.1f);

            float t = 0f;
            while (t < windUpTime)
            {
                if (IsDeadNow()) { CleanupRam(arrow); yield break; }

                // Стоим на месте (по X). Стрелу обновляем под текущее направление —
                // даём ощущение «прицеливания», но фиксируем итоговое направление в конце.
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                t += Time.deltaTime;
                yield return null;
            }

            if (arrow != null) Destroy(arrow);

            // Фиксируем направление рывка по позиции игрока на момент старта (он мог отойти).
            if (playerTf != null)
                dir = playerTf.position.x >= transform.position.x ? 1 : -1;

            // --- РЫВОК: несёмся в зафиксированном направлении, ловим контакт ---
            GameFX.Instance?.Shake(ramStartShake, 0.12f);
            bool hit = false;
            float r = 0f;
            while (r < ramTime)
            {
                if (IsDeadNow()) { CleanupRam(null); yield break; }

                if (rb != null) rb.linearVelocity = new Vector2(dir * ramSpeed, rb.linearVelocity.y);

                if (!hit && playerTf != null)
                {
                    Vector2 d = playerTf.position - transform.position;
                    if (d.sqrMagnitude <= ramContactRange * ramContactRange)
                    {
                        RamHitPlayer(dir);
                        hit = true; // одно попадание за рывок
                    }
                }

                r += Time.deltaTime;
                yield return null;
            }

            // --- ОТКАТ: тормозим и ждём ---
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            float cd = 0f;
            while (cd < ramCooldown)
            {
                if (IsDeadNow()) { CleanupRam(null); yield break; }
                cd += Time.deltaTime;
                yield return null;
            }

            isRamming = false;
        }

        // Тяжёлый удар тараном: урон, сильный отброс, тряска + hit-stop.
        void RamHitPlayer(int dir)
        {
            if (player == null) return;

            player.Damage(ramDamage);
            if (playerRb != null)
            {
                Vector2 force = new Vector2(dir * ramKnockX, ramKnockUp);
                playerRb.AddForce(force, ForceMode2D.Impulse);
            }

            GameFX.Instance?.Shake(ramShakeDur, ramShakeAmp);
            GameFX.Instance?.HitStop(ramHitStop);
        }

        // Пока идёт таран — обычное движение базы не работает (FixedUpdate базы всё ещё крутит
        // подход/контакт/атаку, но скоростью по X в рывке управляет корутина; чтобы база не
        // перебивала рывок, гасим её движение во время тарана).
        protected override void TickMovement(float dt)
        {
            if (isRamming)
                return; // скоростью по X управляет RamRoutine
            base.TickMovement(dt);
        }

        // Стрела-телеграф: дочерний LineRenderer от тела к точке перед боссом в сторону dir.
        // Локальные координаты учитывают масштаб родителя, поэтому делим длину на bodyScale.
        GameObject SpawnArrowTelegraph(int dir)
        {
            var go = new GameObject("LeninRamArrow");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 4; // древко + две грани наконечника
            float w = arrowWidth / Mathf.Max(0.01f, bodyScale);
            lr.startWidth = w;
            lr.endWidth = w;
            lr.numCapVertices = 2;
            lr.sortingOrder = 6;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = arrowColor;
            lr.endColor = arrowColor;

            float len = arrowLength / Mathf.Max(0.01f, bodyScale);
            float head = len * 0.28f;
            float tip = dir * len;
            // Древко: центр → наконечник; затем две грани «галочки» наконечника.
            lr.SetPosition(0, new Vector3(0f, 0f, 0f));
            lr.SetPosition(1, new Vector3(tip, 0f, 0f));
            lr.SetPosition(2, new Vector3(tip - dir * head, head * 0.7f, 0f));
            lr.SetPosition(3, new Vector3(tip - dir * head, -head * 0.7f, 0f));
            return go;
        }

        bool IsDeadNow()
        {
            return health != null && health.IsDead;
        }

        // Аккуратно прервать таран при смерти: убрать стрелу, погасить скорость, снять флаг.
        void CleanupRam(GameObject arrow)
        {
            if (arrow != null) Destroy(arrow);
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            isRamming = false;
        }
    }
}
