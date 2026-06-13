using System.Collections.Generic;

namespace Kommunisty
{
    /// <summary>
    /// Перевод координат веб-прототипа (пиксели, ось Y вниз) в юниты Unity (ось Y вверх).
    /// 32 px = 1 юнит. Верхняя поверхность земли веб (wy=485) маппится в Unity Y = 0,
    /// дальше вверх — положительные Y. Размеры просто делятся на 32.
    /// </summary>
    public static class LevelMetrics
    {
        public const float PixelsPerUnit = 32f;
        public const float GroundSurfaceWebY = 485f;   // wy верхней грани земли в вебе

        public static float UX(float wx) => wx / PixelsPerUnit;
        public static float UY(float wy) => (GroundSurfaceWebY - wy) / PixelsPerUnit;
        public static float U(float px) => px / PixelsPerUnit;
    }

    /// <summary>Платформа уровня (в ВЕБ-пикселях). sky=true — односторонняя (прыжок снизу, спуск по S).</summary>
    public class PlatformDef
    {
        public float x, y, w, h;
        public bool sky;
        public PlatformDef(float x, float y, float w, float h, bool sky) { this.x = x; this.y = y; this.w = w; this.h = h; this.sky = sky; }
    }

    /// <summary>Сундук (веб-пиксели) + тип лута: heal/money/ammo/gasMask/sabre/smg/gasSprayer/shotgun.</summary>
    public class ChestDef
    {
        public float x, y;
        public string loot;
        public ChestDef(float x, float y, string loot) { this.x = x; this.y = y; this.loot = loot; }
    }

    /// <summary>Разрушаемый ящик (веб-пиксели).</summary>
    public class CrateDef
    {
        public float x, y;
        public CrateDef(float x, float y) { this.x = x; this.y = y; }
    }

    /// <summary>Точка спавна врага (веб-пиксели) + тип (zombie/runner/pistol/…). Боссов сюда НЕ кладём.</summary>
    public class SpawnDef
    {
        public float x, floorY;
        public string kind;
        public SpawnDef(float x, float floorY, string kind) { this.x = x; this.floorY = floorY; this.kind = kind; }
    }

    /// <summary>Обучающая подсказка (веб-пиксели X + текст).</summary>
    public class HintDef
    {
        public float x;
        public string text;
        public HintDef(float x, string text) { this.x = x; this.text = text; }
    }

    /// <summary>Полный макет уровня одного биома (всё в ВЕБ-пикселях; конвертит LevelBuilder).</summary>
    public class LevelLayout
    {
        public float worldWpx = 3200f;
        public readonly List<PlatformDef> platforms = new List<PlatformDef>();
        public readonly List<ChestDef> chests = new List<ChestDef>();
        public readonly List<CrateDef> crates = new List<CrateDef>();
        public readonly List<SpawnDef> spawns = new List<SpawnDef>();
        public readonly List<HintDef> hints = new List<HintDef>();
        public float shopX = 185f, shopY = 415f;
        public float portalX = 2960f, portalY = 395f;
    }
}
