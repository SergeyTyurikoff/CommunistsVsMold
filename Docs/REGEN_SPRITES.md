# Перегенерация спрайтов персонажей (для ChatGPT)

## Зачем
В наборе v1.3.0 листы персонажей пришли с **запечённым тёмным студийным фоном**
(градиент-виньетка), а не с настоящей прозрачностью. Автоматическая чистка
оставила лёгкий **коричневый ореол** у тёмных/мшистых врагов. Нужно перегенерить
персонажей на **чистом фоне**, чтобы вырез был идеальным.

## ГЛАВНОЕ ТРЕБОВАНИЕ (передать модели дословно)
> Фон строго **однотонный чисто-белый `#FFFFFF`** (или прозрачный). **НИКАКИХ**
> виньеток, теней-градиентов, студийного затемнения, шахматки, рамок и подписей.
> Один персонаж по центру, **в полный рост, вид сбоку, лицом ВПРАВО**, ровный
> мягкий свет, без отбрасываемой тени на фон. Каждый персонаж — **отдельной
> картинкой** (так проще резать). Рисуй крупно, потом уменьшим.

Стиль (тот же, что в `ASSETS_PROMPTS.md`): пиксель-арт/живопись, тёмный гротеск,
советская эстетика, грязная палитра (уголь, ржавчина, хаки) + красные акценты и
ядовито-зелёная плесень.

## Что перегенерить

### КРИТИЧНО (был ореол — обязательно)
| ID | Имя | Размер | Промт (после требования о фоне) |
|---|---|---|---|
| `zombie` | Грибной зомби | 44×58 | shambling humanoid consumed by green mold, fungal caps on shoulders, glowing spores, dead eyes, melee monster |
| `runner` | Бегун | 44×58 | lean fast mold-infected ghoul in a lurching sprint, ragged clothes, dripping green spores |
| `horse` | Всадник/кавалерия | 78×52 | mold-infected cavalryman riding a rotting fungal horse, side view, sabre raised, spores trailing |
| `kamikaze` | Камикадзе | 42×52 | crazed mold fanatic strapped with dynamite, big red X on chest, wild grin, glowing fuse |

### Желательно (для единообразия набора — можно весь лист персонажей)
| ID | Имя | Размер | Промт |
|---|---|---|---|
| `hero` | Феликс | 36×52 | Soviet Red Army revolutionary, long grey greatcoat, peaked cap with red star, holding a rifle, mid-stride |
| `mold_pistol_soldier` | Плесневый стрелок | 44×58 | mold-infected Soviet soldier aiming a pistol, fungal growths through the uniform |
| `gunner` | Пулемётчик | 55×60 | bulky mold-infected soldier firing a light machine gun, green fungal plates |
| `rifleman` | Винтовочник | 44×58 | mold soldier with a bolt-action rifle, ghoulish face, spores on the barrel |
| `gasman` | Газовик | 44×58 | mold soldier in a gas mask with a backpack tank spraying toxic green gas |
| `sniper` | Снайпер | 44×58 | mold sniper in a tattered ghillie suit with a long scoped rifle, moss camo |
| `sabreur` | Сабельщик | 46×58 | mold-infected cossack swordsman lunging with a raised sabre, torn cloak |
| `shielder` | Щитоносец | 50×56 | heavy armored mold trooper behind a huge riveted metal riot shield |
| `maxim` | Пулемётная точка | 72×46 | stationary Maxim machine-gun on a wheeled mount with a crouched mold gunner and steel shield |
| `miniboss` | Офицер плесени | 62×64 | elite mold officer, ornate Soviet greatcoat with red epaulettes, commander cap |

> Боссы, оружие, объекты и снаряды **перегенерировать не нужно** — их фон уже чистый.

## Как прислать обратно
- По одному PNG на персонажа, имя файла = `ID` латиницей (`zombie.png`, `horse.png`…).
- Я прогоню их через `tools/cut_bg.py` (вырез белого фона) и обрежу по фигуре —
  как с hero_move (там вышло идеально), затем подменю в `img/sprites/characters/`.
