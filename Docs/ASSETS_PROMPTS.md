# Ассеты для генерации (ChatGPT / image gen)

Список юнитов, оружия, пуль и объектов игры с готовыми промтами под пиксель-арт
спрайты. Версия игры **V 1.3.0** (огнемёт убран — везде газ).

---

## Краткое описание игры

**«Коммунисты против… Плесени!»** — 2D сайд-скроллер-шутер с тёмным гротескным
юмором в советской индустриальной эстетике. Игрок — революционер **Феликс**
(красноармеец в длинной шинели и фуражке с красной звездой) пробивается через
**6 биомов** (Лес → Зима → Пустыня → Болото → Город → Мавзолей), отстреливая орды
живой **Плесени** и заражённых ею солдат. В каждом биоме свой босс, финал —
ожившая гротескная фигура **Ленина**. Игра быстрая, аркадная, кровавая и абсурдная.

---

## Общий стиль (префикс к каждому промту)

Скопируй этот блок ПЕРЕД каждым конкретным промтом:

```
2D side-scroller game sprite, hand-painted pixel art, dark grotesque Soviet
atmosphere, gritty muted palette (charcoal, rust, faded khaki) with bold red
accents and sickly toxic-green mold. Side view, character facing RIGHT, full
body, single subject centered, clean fully TRANSPARENT background (PNG with
alpha), consistent soft top-left light, subtle rim light, no text, no UI frame,
no ground shadow. Horror-comedy revolutionary madness. Render large for
downscaling.
```

> Технические правила: фон строго прозрачный, один объект на картинку, вид сбоку,
> лицом вправо, ноги/основание у нижнего края. Размеры ниже — это размер в игре
> (px); генерируй крупно (например, ×8) и потом уменьшим.

---

## 1. Герой

| ID | Имя | Размер (px) | Промт |
|---|---|---|---|
| hero | Феликс | 36×52 | `Soviet Red Army revolutionary soldier, long grey greatcoat, peaked cap with a red star, determined stubbled face, holding a rifle, mid-stride; gritty hero of the people` |

Доп. позы (если делать анимацию): `idle stance`, `running`, `jumping up`,
`falling`, `shooting recoil`. Та же фигура, разные позы, единый стиль.

---

## 2. Рядовые враги (плесень и заражённые)

| ID | Имя | Размер | Промт |
|---|---|---|---|
| zombie | Грибной зомби | 44×58 | `shambling humanoid consumed by green mold, tattered flesh, fungal caps growing from shoulders, glowing spores, dead eyes, melee monster` |
| runner | Бегун | 44×58 | `lean fast mold-infected ghoul in a lurching sprint, ragged clothes, dripping green spores, aggressive` |
| pistol | Плесневый стрелок | 44×58 | `mold-infected Soviet soldier aiming a pistol, fungal growths bursting through the uniform, sickly green skin` |
| gunner | Пулемётчик | 55×60 | `bulky mold-infected soldier firing a light machine gun, heavy stance, green fungal armor plates` |
| rifleman | Винтовочник | 44×58 | `mold-infected soldier with an old bolt-action rifle, ghoulish face, spores on the barrel` |
| gasman | Газовик | 44×58 | `mold soldier in a gas mask with a backpack tank spraying toxic green gas, hazmat-revolutionary look` |
| sabreur | Сабельщик | 46×58 | `mold-infected cossack swordsman lunging with a raised sabre, torn cloak, fanatical` |
| horse | Всадник (кавалерия) | 78×52 | `mold-infected cavalryman riding a rotting fungal horse, charging sideways, sabre raised, spores trailing` |
| kamikaze | Камикадзе | 42×52 | `crazed mold fanatic strapped with dynamite, big red X on the chest, wild grin, glowing fuse` |
| shielder | Щитоносец | 50×56 | `heavy armored mold trooper hunched behind a huge riveted metal riot shield, slow tank unit` |
| sniper | Снайпер | 44×58 | `mold sniper in a tattered ghillie suit with a long scoped rifle, camouflaged with moss and fungus` |
| maxim | Пулемётная точка | 72×46 | `stationary Maxim machine-gun emplacement on a wheeled mount with a crouched mold gunner and a steel shield, firing` |
| miniboss | Офицер плесени | 62×64 | `elite mold officer, taller and bulkier, ornate Soviet greatcoat with red epaulettes, commander cap, oozing fungal authority` |

---

## 3. Боссы (по биомам)

| ID | Биом | Размер | Промт |
|---|---|---|---|
| mushroomBoss | Лес | 110×92 | `giant mutant mushroom monster boss, massive spotted cap with glaring eyes, thick fungal limbs, bursting spore clouds, grotesque` |
| treeBoss | Зима | 110×92 | `colossal frost-covered twisted tree-monster boss, icy bark face, clawed branch arms, frozen mold, looming` |
| sandBoss | Пустыня | 110×92 | `huge desert sandworm boss erupting from sand, fungal maw with rings of teeth, dusty spores` |
| swampBoss | Болото | 110×92 | `bloated swamp horror boss, dripping mold and slime, bog tentacles, toxic green glow` |
| factoryBoss | Город | 110×92 | `massive rust-covered Soviet mech-tank boss, riveted armor, smokestacks, red star, mold leaking from seams; (will be scaled bigger later)` |
| lenin | Мавзолей (финал) | 88×92 | `grotesque reanimated Lenin boss, pale, black suit with red tie, goatee, glowing eyes, arm outstretched in a charging ram pose, undead aura` |

---

## 4. Оружие в руках (вид сбоку, ствол вправо)

| ID | Имя | Размер | Промт |
|---|---|---|---|
| pistol | Пистолет (ТТ) | 34×20 | `Soviet TT pistol, side view, barrel pointing right, worn metal` |
| mosin | Винтовка Мосина | 70×22 | `Mosin–Nagant bolt rifle with bayonet, side view, long barrel right` |
| smg | Пулемёт/ППШ | 58×23 | `PPSh-41 submachine gun with round drum magazine, side view` |
| gasSprayer | Газомёт | 66×24 | `improvised gas sprayer gun with a hose and pressurized green tank, side view` |
| sabre | Шашка | 54×18 | `cossack cavalry sabre, curved steel blade, side view pointing right` |
| shotgun | Обрез | 54×23 | `sawn-off double-barrel shotgun, side view, worn wooden grip` |

---

## 5. Пули и снаряды

| ID | Имя | Размер | Промт |
|---|---|---|---|
| bullet | Пуля (пистолет/винтовка) | ~14×6 | `glowing bullet tracer, small horizontal streak, warm yellow-white, motion blur, transparent bg` |
| pellet | Картечь (обрез) | ~9×9 | `small round shotgun pellet, metallic, faint spark` |
| sniperRound | Снайперский снаряд | ~16×6 | `long bright sniper tracer round, sharp yellow streak` |
| enemyBolt | Снаряд врага | ~12×12 | `toxic green mold projectile blob, glowing, dripping, transparent bg` |
| gasCloud | Газовое облако | ~120×120 | `soft toxic green gas cloud puff, semi-transparent, swirling spores, no edges` |
| muzzle | Дульная вспышка | ~16×12 | `small muzzle flash burst, yellow-orange star shape, transparent bg` |

---

## 6. Объекты, предметы и интерфейсные иконки

| ID | Имя | Размер | Промт |
|---|---|---|---|
| shop | Лавка снабженца | 70×70 (+вывеска) | `revolutionary supply stall: wooden counter with sandbags, red awning, a hanging sign, crates of ammo, dim lantern; Soviet field depot` |
| chest | Сундук/ящик снабжения | 38×26 | `small wooden supply crate/chest with iron bands and a red star lock, closeable lid` |
| portal | Портал перехода | 70×90 | `glowing cyan time portal, swirling energy ring on a metal base, sci-fi exit gate, transparent bg` |
| ammoBox | Ящик патронов | 22×22 | `small military ammo box, olive green with a stripe and a bullet icon` |
| money | Монета | 22×22 | `gold coin with a hammer-and-sickle star embossed, shiny` |
| timePickup | Кристалл времени/здоровья | 22×22 | `cyan hourglass crystal, glowing, faceted, health pickup` |
| medkit | Аптечка | 22×22 | `red first-aid kit with a white cross, worn metal box` |
| gasMask | Противогаз | 22×22 | `Soviet gas mask icon, round eye lenses and filter, olive rubber` |

---

## Подсказки по использованию

- Генерируй по одному объекту за раз, всегда с префиксом стиля.
- Проси «**transparent background, PNG**»; если фон всё же сплошной — у нас есть
  `tools/cut_bg.py` (вырезает фон заливкой от краёв).
- Держи единый источник света и палитру, чтобы спрайты смотрелись из одного набора.
- Боссам можно просить «**centered, full body, dramatic**», им фон тоже прозрачный.
- Имена файлов — латиницей по `ID` из таблиц (например `hero.png`, `gasman.png`,
  `mushroom_boss.png`), чтобы потом легко подключить в `js/assets.js`.
