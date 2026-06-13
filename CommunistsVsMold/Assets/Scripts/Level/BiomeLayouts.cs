using System.Collections.Generic;
using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Точный порт функций build() и buildEnemySpawns() из веб-файла world.js
    /// в структуры данных LevelLayout. Координаты — ВЕБ-ПИКСЕЛИ (конвертацию
    /// в мировые единицы делает другой код). Только данные: ни рендера, ни
    /// физики, ни коллизий.
    ///
    /// Наша нумерация биомов отличается от веба:
    ///   biome == 0  -> ОБУЧЕНИЕ   (в world.js это ветка tutorial = (i===0))
    ///   biome >= 1  -> обычный уровень; для выбора врагов/лута берём
    ///                  li = Clamp(biome - 1, 0, 5) вместо levelIndex.
    /// </summary>
    public static class BiomeLayouts
    {
        public static LevelLayout Build(int biome)
        {
            var layout = new LevelLayout();

            // tutorial-флаг и индекс уровня для не-tutorial логики
            bool tutorial = (biome == 0);
            int li = Mathf.Clamp(biome - 1, 0, 5); // аналог levelIndex в не-tutorial ветке world.js

            // ширина уровня (this.levelW)
            float levelW = tutorial ? 2240f : 3200f;
            layout.worldWpx = levelW;

            // --- земля (this.addPlatform(0,485,levelW,80,'ground')) ---
            var ground = new PlatformDef(0f, 485f, levelW, 80f, false);
            layout.platforms.Add(ground);

            // --- sky-платформы (цикл по n) ---
            // Порядок добавления в локальный список skyList важен: он повторяет
            // порядок push в world.js, потому что buildEnemySpawns использует idx.
            var skyList = new List<PlatformDef>();
            int skyCount = tutorial ? 7 : 12;
            for (int n = 0; n < skyCount; n++)
            {
                float x = 170f + n * 230f;
                float y = 390f - (n % 3) * 38f;

                var p = new PlatformDef(x, y, 175f, 18f, true);
                skyList.Add(p);

                if (n % 3 == 1)
                {
                    var p2 = new PlatformDef(x + 95f, y - 76f, 155f, 18f, true);
                    skyList.Add(p2);
                }
            }

            // --- высокие платформы high1/high2/high3 ---
            var high1 = new PlatformDef(700f, 285f, 240f, 18f, true);
            var high2 = new PlatformDef(tutorial ? 1350f : 1550f, 292f, 255f, 18f, true);
            var high3 = new PlatformDef(tutorial ? 1860f : 2380f, 300f, 240f, 18f, true);

            // Добавляем платформы в layout. Земля уже добавлена выше; добавляем
            // high1..high3 и все sky.
            layout.platforms.Add(high1);
            layout.platforms.Add(high2);
            layout.platforms.Add(high3);
            foreach (var sp in skyList)
                layout.platforms.Add(sp);

            // --- магазин (всегда фиксирован) ---
            layout.shopX = 185f;
            layout.shopY = 415f;

            // --- сундуки ---
            if (tutorial)
            {
                layout.chests.Add(new ChestDef(730f, 444f, "heal"));
                layout.chests.Add(new ChestDef(1260f, 444f, "gasMask"));
                layout.chests.Add(new ChestDef(1710f, 444f, "money"));
            }
            else
            {
                // i%3===0 ? 'heal' : (i%2===0 ? 'money' : 'ammo')  — где i = li
                string loot1 = (li % 3 == 0) ? "heal" : ((li % 2 == 0) ? "money" : "ammo");
                layout.chests.Add(new ChestDef(760f, 444f, loot1));

                // ['sabre','smg','gasSprayer','shotgun','ammo','gasSprayer'][i] || 'money'
                string[] mid = { "sabre", "smg", "gasSprayer", "shotgun", "ammo", "gasSprayer" };
                string loot2 = (li >= 0 && li < mid.Length) ? mid[li] : "money";
                layout.chests.Add(new ChestDef(1320f, 444f, loot2));

                // ['money','heal','sabre','gasSprayer','shotgun','heal'][i] || 'money'
                string[] far = { "money", "heal", "sabre", "gasSprayer", "shotgun", "heal" };
                string loot3 = (li >= 0 && li < far.Length) ? far[li] : "money";
                layout.chests.Add(new ChestDef(1980f, 444f, loot3));
            }

            // --- ящики ---
            float[] crateXs = tutorial
                ? new float[] { 560f, 980f, 1460f }
                : new float[] { 540f, 880f, 1180f, 1520f, 1840f, 2200f, 2480f };
            foreach (var cx in crateXs)
                layout.crates.Add(new CrateDef(cx, 449f));

            // --- хинты обучения (раш-триггеры в порт не нужны) ---
            if (tutorial)
            {
                layout.hints.Add(new HintDef(90f, "Иди вправо и прыгай на платформы. Первый биом короткий и учебный."));
                layout.hints.Add(new HintDef(520f, "Стреляй мышью. Q меняет оружие, цифры 1-6 для предметов."));
                layout.hints.Add(new HintDef(1160f, "Аптечки уходят в инвентарь. Используй их цифрой 1."));
                layout.hints.Add(new HintDef(1620f, "В конце будет быстрый враг. Держи темп и реагируй сразу."));
            }

            // --- портал ---
            layout.portalX = tutorial ? 2040f : 2960f;
            layout.portalY = 395f;

            // --- спавны врагов ---
            // Собираем список платформ в ТОМ ЖЕ порядке, что и в world.js:
            // [ground, high1, high2, high3, ...sky]. Порядок важен для idx.
            var spawnPlatforms = new List<PlatformDef>();
            spawnPlatforms.Add(ground);
            spawnPlatforms.Add(high1);
            spawnPlatforms.Add(high2);
            spawnPlatforms.Add(high3);
            spawnPlatforms.AddRange(skyList);

            BuildEnemySpawns(layout, spawnPlatforms, ground, tutorial, li);

            return layout;
        }

        /// <summary>
        /// Порт buildEnemySpawns(). Боссы (bossKind) НЕ добавляются — их спавнит
        /// отдельный режиссёр.
        /// </summary>
        private static void BuildEnemySpawns(LevelLayout layout, List<PlatformDef> platforms,
                                             PlatformDef ground, bool tutorial, int li)
        {
            if (tutorial) // this.levelIndex === 0
            {
                // [{x:760,'zombie'},{x:1120,'pistol'},{x:1490,'zombie'},{x:1860,'runner'}]
                layout.spawns.Add(new SpawnDef(760f, ground.y, "zombie"));
                layout.spawns.Add(new SpawnDef(1120f, ground.y, "pistol"));
                layout.spawns.Add(new SpawnDef(1490f, ground.y, "zombie"));
                layout.spawns.Add(new SpawnDef(1860f, ground.y, "runner"));
                return;
            }

            // normalByLevel[li]
            string[][] normalByLevel = new string[][]
            {
                new[] { "zombie", "runner", "pistol" },
                new[] { "zombie", "runner", "pistol", "gasman" },
                new[] { "runner", "rifleman", "sabreur", "gasman", "horse" },
                new[] { "runner", "gunner", "gasman", "sabreur", "kamikaze" },
                new[] { "rifleman", "gunner", "gasman", "maxim", "shielder", "miniboss" },
                new[] { "gunner", "rifleman", "maxim", "horse", "shielder", "sniper", "kamikaze", "sabreur" }
            };
            string[] kinds = normalByLevel[li];

            // наземные спавны: фиксированные иксы
            float[] xs = { 720f, 1020f, 1320f, 1620f, 1940f, 2260f, 2520f };
            for (int idx = 0; idx < xs.Length; idx++)
            {
                string kind = kinds[idx % kinds.Length];
                layout.spawns.Add(new SpawnDef(xs[idx], ground.y, kind));
            }

            // спавны на sky-платформах: каждая чётная (idx%2===0) подходящая
            // платформа (sky, x в [620..2550], w>=140), точка x = центр платформы
            // в [620..2580].
            for (int idx = 0; idx < platforms.Count; idx++)
            {
                var p = platforms[idx];
                if (!p.sky || p.x < 620f || p.x > 2550f || p.w < 140f) continue;
                if (idx % 2 == 0)
                {
                    float x = p.x + p.w * 0.5f;
                    if (x >= 620f && x <= 2580f)
                    {
                        string kind = kinds[(idx + 1) % kinds.Length];
                        layout.spawns.Add(new SpawnDef(x, p.y, kind));
                    }
                }
            }
            // bossKind — пропущено (боссов спавнит режиссёр).
        }
    }
}
