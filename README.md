# Коммунисты против… Плесени — Unity

Unity-версия игры. Отдельный git-репозиторий.

> ⚠️ **Путь только ASCII, без пробелов и спецсимволов.** Поэтому проект живёт здесь
> (`C:\prog\kommunisty-unity`), а НЕ внутри кириллической папки игры. Unity ломается
> на путях с кириллицей/«...»/пробелами (именно из-за этого первая попытка в
> `…Коммунисты против... Плесени - Unity` не достроилась).

HTML5-прототип (референс поведения и баланса) лежит отдельно:
`C:\prog\brainiac\99_Программы\Игры\Коммунисты против... Плесени\Коммунисты против... Плесени - Игра`.

---

## Что уже лежит здесь

```
Docs/
  PORT_SPEC.md        ← ГЛАВНЫЙ чертёж: механики, формулы, числа, порядок переноса
  balance.csv         ← числа баланса → ScriptableObject
  ASSETS_PROMPTS.md   ← список ассетов и промты
  REGEN_SPRITES.md    ← заметка по перегенерации спрайтов
_ImportArt/Sprites/   ← готовые PNG (characters/bosses/weapons/objects/projectiles)
.gitignore            ← Unity-стандарт
README.md             ← этот файл
```

После создания проекта перетащи спрайты из `_ImportArt/Sprites/` в `Assets/Art/Sprites/`.

---

## Как создать проект (актуальная версия Unity 6)

1. **Unity Hub → Installs** — убедись, что стоит **6000.4.4f1** (LTS) с модулем
   **Windows Build Support (IL2CPP)**. Если нет — Install Editor.
2. **Projects → New project** (синяя кнопка справа).
3. Шаблон слева — **2D** («2D (Built-In Render Pipeline)»; для 2D-света позже можно URP).
4. Справа:
   - **Project name:** `CommunistsVsMold`
   - **Location:** `C:\prog\kommunisty-unity`
   - Editor version: **6000.4.4f1**.
   - Unity создаст `C:\prog\kommunisty-unity\CommunistsVsMold\` — это и откроется.
5. **Create project**.

> Если Hub ругается, что папка занята — целевой подпапки `CommunistsVsMold` быть не
> должно; `Docs/_ImportArt/.gitignore` в корне не мешают (Unity делает свою подпапку).

### Импорт ассетов
- В окне **Project**: создать `Assets/Art/Sprites`, перетащить туда `_ImportArt/Sprites/*`.
- Выделить спрайты → **Inspector**: `Texture Type = Sprite (2D and UI)`,
  `Filter Mode = Point (no filter)`, `Compression = None` → **Apply**.
- Для чёткого пиксель-арта — пакет **2D Pixel Perfect** + **Pixel Perfect Camera**.

---

## Структура `Assets/` (рекомендуется)

```
Assets/
  Art/Sprites/{Characters,Bosses,Weapons,Objects,Projectiles}/
  Data/                ← ScriptableObjects: WeaponSO, EnemySO, AmmoSO, BiomeSO
  Prefabs/             ← Player, враги, пули, пикапы, портал, магазин
  Scenes/              ← Main (геймплей), Menu
  Scripts/{Core,Player,Enemies,Systems}/
```

Числа — из `Docs/balance.csv`; поведение/формулы и порядок работ — `Docs/PORT_SPEC.md` (§15).

---

## Git
```
git remote add origin <URL нового репозитория>
git push -u origin master
```
