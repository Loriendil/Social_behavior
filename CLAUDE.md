# RelationshipSimulator — модель отношений и эмоций NPC

## Цель проекта

Настольное приложение на C#, реализующее и объединяющее логику двух научных работ:

1. **O'Connor, "A Relationship Model for Believable Social Dynamics of Characters in Games"** (Trinity College Dublin, 2015) — модель **RelationshipGraph**: направленный граф отношений между сущностями (персонажи, группы, предметы, локации). Оригинальный код автора: https://github.com/pandaboy/RelationshipGraph
2. **Ochs, Sabouret, Corruble, "Simulation of the Dynamics of Nonplayer Characters' Emotions and Social Relations in Games"** (IEEE TCIAIG, 2009) — **динамика**: события порождают эмоции (упрощённая OCC-модель), личность масштабирует их интенсивность, эмоции обновляют социальные отношения.

**Ключевая идея объединения:** граф О'Коннора — это хранилище и API; модель Ochs — правила обновления значений на рёбрах. Атрибут `Relationship` на ребре — не enum вида "FRIEND", а объект `SocialRelation` с четырьмя числовыми измерениями из статьи Ochs.

PDF обеих статей лежат в `docs/` (если папки нет — попросить пользователя добавить). При реализации формул сверяться с разделом IV статьи Ochs и главами 3–4 диссертации О'Коннора.

**Как читать PDF в этой рабочей среде:** встроенный рендер PDF-страниц (`pdftoppm`/poppler-utils) не установлен и недоступен через обычные пакетные менеджеры в этой песочнице. Рабочий обходной путь — Python + PyMuPDF:

```bash
python -m pip install --quiet pymupdf
python - <<'EOF'
import fitz
doc = fitz.open(r"...\docs\Simulation_of_the_Dynamics_of_Nonplayer_Characters.pdf")
page = doc[8]                       # индекс с 0; номер на странице статьи обычно index+282 (см. footer)
page.get_pixmap(dpi=300).save(r"...\page.png")   # можно передать clip=fitz.Rect(...) для кропа конкретного рисунка
EOF
```

Полученный PNG читать через инструмент Read как обычное изображение. Проверено на практике 2026-07-02 — так были прочитаны рис. 2-6 и раздел IV статьи Ochs и найдены реальные расхождения с более ранним пересказом (см. историю формул ниже). Используй `python -c "import fitz"` для проверки, установлен ли уже пакет, прежде чем ставить заново.

## Структура solution (принятые решения)

| Проект | Тип | Платформа | Назначение |
|---|---|---|---|
| `RelationshipCore` | Class Library | .NET 10 | Ядро: граф, эмоции, формулы. Без зависимостей от UI |
| `RelationshipCore.Tests` | xUnit | .NET 10 | Юнит-тесты + сценарии из статьи Ochs как интеграционные тесты |
| `RelationshipSimulator` | WPF | .NET 10 | UI (добавляется позже, когда ядро заработает) |

Все проекты — строго на одной версии .NET (10.0, LTS). Ядро должно оставаться переносимым (потенциально netstandard2.1 для Unity), поэтому никаких ссылок на WPF/WinForms из `RelationshipCore`.

## Архитектурные решения (уже обсуждены и приняты)

1. **Идентификация без `IEquatable<T>`.** Автор диссертации в разделе 5.4 сам признаёт, что завязка на `IEquatable` была ошибкой (мешает расширениям). Используем собственное свойство `EntityId` / метод `Matches()`.
2. **Отношение — атрибут ребра, а не само ребро** (раздел 3.1.1 диссертации). Обновление отношения = поиск Connection + обновление атрибута.
3. **Прямые и косвенные рёбра** (direct/indirect edges): косвенное ребро — это *предположение* сущности об отношениях двух других сущностей; оно может не совпадать с реальным прямым ребром (поддержка слухов, обмана, неполного знания).
4. **HistoryEdge** — ребро, хранящее историю значений Relationship (для «персонаж помнит, что они были врагами»).
5. **Группы** — обычные узлы графа; членство через отношение MEMBER; поддержка broadcast-сообщений членам группы и наследования отношений группы («враг группы — враг каждого члена»).
6. **Внутреннее хранилище графа** — не список с линейным поиском (автор жалуется на O(n²) в разделе 5.4), а Dictionary/индексы по возможности.

## Стартовые интерфейсы ядра (согласованный черновик)

```csharp
namespace RelationshipCore;

/// <summary>Узел графа: персонаж, группа, предмет, локация.</summary>
public interface INode
{
    int EntityId { get; }
    void HandleMessage(IMessage message);
}

/// <summary>Отношение — атрибут ребра, а не само ребро.</summary>
public interface IRelationship
{
    bool Matches(IRelationship other);
}

/// <summary>Ребро: направленная связь от одной сущности к другой.</summary>
public interface IEdge
{
    INode From { get; }
    INode To { get; }
    IRelationship Relationship { get; set; }
}

public interface IMessage { }
```

## Архитектура объединения слоёв (уточнено с Fable 5, 2026-07-02)

Консультация по вопросу «как объединить O'Connor и Ochs, не смешивая ответственности». Принцип:

> Слой O'Connor — структура и адресация (кто про кого что хранит). Слой Ochs — чистая математика над значениями. Между ними — один оркестратор, который знает про оба слоя; сами слои друг про друга не знают.

### Неймспейсы

```
RelationshipCore                          — интерфейсы (заморожено, см. черновик выше)
RelationshipCore.Nodes/.Edges/.Graphs     — слой O'Connor (Этап 1 + HistoryEdge на Этапе 5)
RelationshipCore.Dynamics                 — типы-значения Ochs, БЕЗ ссылок на Graphs/INode
RelationshipCore.Dynamics.Rules           — чистые статические формулы
RelationshipCore.Simulation               — оркестратор (Этап 3): единственное место, где встречаются оба слоя
```

Правило зависимостей: `Dynamics`/`Dynamics.Rules` не ссылаются на `Graphs` и не принимают `INode` — только `int entityId` и `IRelationship`. Это делает слой Ochs тестируемым «в пробирке» против чисел из статьи и переносимым в Unity без графа вообще.

### Ключевые решения (архитектура, не детали формул)

1. **Формулы — статические функции в `*.Rules`, `SocialRelation` — immutable.** `SocialRelation : IRelationship` — sealed class с конструктором, клэмпящим все 4 измерения в `[-1,1]`; обновление ребра — всегда `edge.Relationship = новыйSocialRelation`, никогда мутация полей. Это уже сейчас совместимо с `DeepGraph.AddEdge` (обновляет атрибут существующего ребра) и бесплатно даст снимки для будущего `HistoryEdge` (Этап 5).
2. **`EmotionVector`/`Personality`/`Appraisal` персонажа НЕ хранятся в `Node`.** Будут жить в `NpcState` (Этап 3) — параллельном реестре `Dictionary<int, NpcState>` по `EntityId` внутри `Simulation`, а не как поля узла. Причина: среди `INode` есть локации и предметы без личности; плодить `CharacterNode : Node` в ядре плохо переносится в Unity (там персонаж — `MonoBehaviour`). `HandleMessage` остаётся точкой входа для *реакции игры* на изменение состояния (уведомления), а не для самой физики модели.
3. **`GameEvent` — НЕ `IMessage`.** Основной путь: `SocialDynamicsEngine.Perceive(perceiverId, evt, time)` / `.ObserveExpression(observerId, expresserId, time)`, явно читающие `NpcState` и пишущие в `DeepGraph`. `IMessage` — отдельный транспортный слой для broadcast группам и распространения слухов (косвенные рёбра, Этап 5); сообщения-адаптеры знают про движок, а не наоборот.
4. **Словарь игры расщеплён по субъективности статьи:** `ActionDictionary.GetEffect` — один общий объект на игру (объективно); `Appraisal.GetAttitude`/`GetPraise` — компонент внутри `NpcState` каждого NPC (субъективно). `GetAttitude(int entityId)` берёт `EntityId`, поэтому естественно работает и для персонажей, и для предметов/локаций — единая точка стыковки двух статей.
5. **`Personality` — `readonly struct`, поле будущего `NpcState`, только композиция.** Никакого наследования от `Node` в ядре.
6. **`IRelationship.Matches`** для `SocialRelation` — двойная диспетчеризация: `SocialRelation` сравнивается с другим `SocialRelation` по эпсилон-допуску, либо с `SocialRelationPattern` (диапазоны по каждому измерению, `null` = любое значение) — так «друзья/враги» становятся производной от непрерывных чисел, а не отдельным enum на ребре. Роли (cop/gangster) — генератор начальных значений (`SocialRoleTable`, Этап 3), не часть текущего состояния отношения.

### Известный риск (не мешает Этапам 2-3, учесть на Этапе 5)

`DeepGraph.FindEdge` ищет ровно одно ребро на пару `(from, to)` у владельца. К Этапу 5 понадобятся одновременно `SocialRelation`-ребро и структурное `MEMBER`-ребро между теми же двумя узлами — они будут конфликтовать за один слот. Решать добавлением типа отношения в ключ поиска внутри `DeepGraph`; интерфейсы (`IEdge`/`IRelationship`) менять не потребуется.

## Модель Ochs — что реализовать (сверено с оригиналом PDF, 2026-07-02, рис. 2-6 и раздел IV прочитаны напрямую — см. «Как читать PDF» выше)

- **Событие**: кортеж `⟨agent, action, patient, dc⟩`, где `dc ∈ [0,1]` — степень уверенности (1 — очевидец, 0 — ожидаемое не произошло).
- **Словарь игры**: действия с объективным `effect ∈ [-1,1]`; субъективные `attitude(x, объект/персонаж) ∈ [-1,1]` и `praise(x, действие) ∈ [-1,1]` для каждого NPC.
- **10 эмоций** (упрощение OCC по Ortony 2002): joy/distress, hope/fear, relief/disappointment, pride/shame, admiration/anger. Вектор значений `[0,1]`.
- **Правила триггера** (рис. 2 статьи, `RelationshipCore.Dynamics.Rules.EmotionRules`):
  - Верхняя ветка (`effect ≥ 0 И attitude(i,patient) > 0`) ИЛИ (`effect < 0 И attitude < 0`) — **важно: `effect ≥ 0`, не строго `> 0`**, поэтому `effect == 0` при `attitude > 0` всё равно попадает в "положительную" ветку; обычный `MathF.Sign(effect)` тут даст неверный 0 и погасит эмоцию — нужен явный `effect >= 0 ? 1 : -1`. Ветка даёт: `dc=1` → Joy, `dc∈]0,1[` → Hope, `dc=0` → Disappointment.
  - Нижняя ветка (`effect ≥ 0 И attitude < 0`) ИЛИ (`effect < 0 И attitude > 0`): `dc=1` → Distress, `dc∈]0,1[` → Fear, `dc=0` → Relief.
  - `attitude == 0` → ни одна ветка не срабатывает (нет эмоции этого типа).
  - Praise-ветки (`praise(i,action) > 0` → agent=i: Pride, agent≠i: Admiration; `praise(i,action) < 0` → agent=i: Shame, agent≠i: Anger) **помечены `dc=1`** на диаграмме — срабатывают только для очевидца, не при любом dc.
- **Интенсивность стимула** (раздел IV-A, фиг. 3): для `{joy, distress, relief, disappointment}` — `av(|attitude(i,patient)|, |effect_action|)` (среднее, без множителя dc — в том числе для relief/disappointment, у которых `dc=0`); для `{hope, fear}` — то же самое `* dc`; для `{pride, admiration, shame, anger}` — `|praise(i,action)|`.
- **Личность** (раздел IV-C): пара `(extraversion, neuroticism) ∈ [-1,1]²`. Текст статьи явно определяет эффект только для `p ∈ [0,1]` (экстраверт усиливает **позитивные** эмоции, нейротик — **негативные**, без указания, какие конкретно 10 эмоций считаются позитивными/негативными для этой цели — не путать со списками из рис. 4!). Мультипликативный фактор: `p=0` → ×1.0, `p=1` → ×1.5 (текст статьи подтверждает именно это). Поведение при `p<0` (интроверт/спокойный) в прочитанной части статьи не уточнено; реализовано симметрично (`p=-1` → ×0.5) как разумное расширение бипоlярной шкалы — это НЕ прямая цитата формулы.
- **Затухание** (раздел IV-A-3): `e(t) = e(t-1) * exp(-decreaseRate)`; в реализации авторов `decreaseRate = 0.1` для всех эмоций. При событии: `e(t) = max(триггер, затухшее старое)`.
- **Социальное отношение** (раздел IV-D-1): кортеж `⟨liking, dominance, familiarity, solidarity⟩`, несимметричное (i→j ≠ j→i). **Важно — диапазоны РАЗНЫЕ**: `liking, dominance ∈ [-1,1]`; `familiarity, solidarity ∈ [0,1]` (это прямо в определении квадруплета, легко упустить при беглом чтении). Начальные значения — из пар социальных ролей (cop/gangster и т.п.), таблица усредняется при множественных ролях.
- **Динамика отношений от эмоций** — три независимых источника, реализованы как `SocialRelationRules.From*`, складывающиеся через `SocialRelationDelta` и применяемые ОДИН раз функцией `Apply` (= статейная `φ_sr = g_sr(relation, f_sr(...))`, не несколько последовательных вызовов g_sr):
  - **Рис. 4 (liking)**, "emotions of i caused by j" — т.е. СВОЯ эмоция, вызванная действием собеседника: `{joy, hope, admiration, pride}` → `+`; `{distress, fear, anger, shame}` → `-`. **relief и disappointment в этот набор не входят** (легко ошибиться, взяв общий список "позитивных/негативных" эмоций).
  - **Рис. 5 (dominance)** — ДВА разных источника:
    - "emotions of i caused by j" (своя эмоция): `{pride, anger}` → `+`; `{fear, distress, admiration, shame}` → `-` (на самой диаграмме shame отсутствует, но текст раздела IV-D-2 явно перечисляет все 4 — доверяем тексту, вероятная опечатка в фигуре).
    - "emotions expressed by j" (эмоция, которую я НАБЛЮДАЮ у собеседника): **только** `{fear, distress}` → `+` к моей dominance. Никакой связи с pride/anger/admiration здесь нет — это не "комплементарный переворот" первого списка, а отдельное самостоятельное правило.
  - **Рис. 6 (solidarity)** — тоже три источника:
    - совпадение выражаемых эмоций `{joy_i&joy_j, hope_i&hope_j, distress_i&distress_j, fear_i&fear_j}` → `+`;
    - несовпадение (конкретные пары) `{joy_i&distress_j, hope_i&fear_j, distress_i&joy_j, fear_i&hope_j}` → `-`;
    - своя эмоция, вызванная j: `{distress, fear, disappointment, shame, anger}` → `-` (обратите внимание: это тоже "emotions of i caused by j", как и в рис. 4/5 — НЕ эмоция, которую выражает собеседник).
  - **Familiarity** — статья прямо говорит "этот механизм не представлен в этой работе"; в коде — собственное расширение (`UpdateFamiliarityFromLikingShift`, растёт с `|Δliking|`), не из статьи.
  - Функция обновления `g_sr` — статья описывает как монотонно возрастающую, с малым наклоном у 1 и -1 ("отношение трудно изменить на экстремумах"), и **прямо предлагает синусоиду как пример реализации**, не давая точной формулы. Реализовано как `cos`/`sin`-демпфер (`ApplyBoundedSigned`/`ApplyBoundedUnit`) с нижним порогом 0.15, чтобы величины, стартующие ровно с края диапазона (например, `familiarity=0`, `solidarity=0` у новых отношений), не залипали навсегда с нулевым шагом.
- **Пороги**: порог активации (эмоция ниже — не влияет на поведение) и порог насыщения (выше — отключает рациональное принятие решений). Точных чисел статья не даёт (пример "0.2 для joy" — иллюстративный).

## Эталонные сценарии для тестов (из статьи Ochs, раздел IV-B)

1. **Допрос грабителя полицейским.** Стартовые отношения: dominance = -0.3, liking = -0.5. Событие `⟨policeman, arrest, burglar, 0.8⟩` → fear → liking падает. Кофе (attitude 0.4) → hope → liking растёт. Рассказ о похищении → distress, но полицейский не виновен → liking не падает; совпадение distress у обоих → solidarity↑ и liking↑; dominance грабителя падает. С нейротичной личностью негативные эмоции сильнее.
2. **Собеседование.** Совпадение выражаемых эмоций → solidarity директора растёт; выражение страха соискателем → dominance директора растёт; событие `⟨PC, offer, money, 1⟩` → joy → liking↑.

Тест проходит, если качественная динамика (направления изменений) совпадает с рис. 9–11 статьи.

## План работ (roadmap)

- [x] Выбор платформы: .NET 10, Class Library + xUnit + WPF
- [x] **Этап 1:** интерфейсы ядра (`INode`, `IEdge`, `IRelationship`, `IMessage`) + классы `Graph`/`DeepGraph`
- [x] **Этап 2:** классы модели Ochs: `Personality`, `EmotionVector`, `SocialRelation`, `GameEvent`, словарь действий/attitude/praise + формулы раздела IV (стимул → личность → триггер → затухание → обновление отношений) — формулы реализованы вместе с типами как чистые статические функции, см. `RelationshipCore.Dynamics.Rules`
- [ ] **Этап 3:** `NpcState` (реестр личность+эмоции+attitude/praise по EntityId), `SocialDynamicsEngine` (оркестратор: читает NpcState, пишет в DeepGraph через GetEdge/AddEdge), `SocialRoleTable` для начальных отношений через `AddCommonEdge`
- [ ] **Этап 4:** тесты на эталонных сценариях (грабитель, собеседование)
- [ ] **Этап 5:** HistoryEdge, группы, broadcast-сообщения, learning (принятие чужих Connection по весу)
- [ ] **Этап 6:** WPF-приложение: редактор персонажей/ролей/словаря, редактор сценария событий, графики эмоций во времени (OxyPlot или LiveCharts2), визуализация графа (Microsoft.MSAGL или GraphShape)

## Соглашения

- Язык кода и XML-комментариев: комментарии на русском допустимы, имена — английские, соответствующие терминологии статей (Entity, Connection, Relationship, liking, dominance, solidarity, familiarity).
- Обсуждение с пользователем ведём на русском языке.
- Не начинать с UI: вся ценность — в ядре; UI лишь визуализирует состояние графа.
- Значения модели держать в заявленных диапазонах ([-1,1], [0,1]) — добавить проверки/clamp.

## Контекст переноса (где мы остановились)

Обсуждение начато в Claude (чат). Пользователь создаёт проект `RelationshipCore` (шаблон «Библиотека классов», .NET 10) в Visual Studio. Согласованы стартовые интерфейсы (см. выше).

**Этап 1 реализован:** `Interfaces/` (`INode`, `IEdge`, `IRelationship`, `IMessage` — точно по черновику выше, в корневом namespace `RelationshipCore`), `Nodes/Node.cs`, `Edges/Edge.cs` — минимальные реализации, `Graphs/Graph.cs` — хранилище узлов/рёбер на `Dictionary<int, ...>` по `EntityId` (без O(n²)-поиска), `Graphs/DeepGraph.cs` — семантика прямых/косвенных рёбер (`AddDirectEdge`, `AddEdge` с произвольным owner для косвенных рёбер, `GetDirectEdges`/`GetIndirectEdges`, `WithRelationship`/`WithRelationshipTo`, `AddCommonEdge`). Добавлен `RelationshipCore.Tests` (xUnit) с 12 тестами на граф — все проходят (`dotnet test`).

Важный нюанс структуры: `RelationshipCore.Tests` лежит вложенной папкой внутри каталога `RelationshipCore` (а не рядом на уровне решения), поэтому в `RelationshipCore.csproj` пришлось добавить `<Compile Remove="RelationshipCore.Tests/**" />` — иначе SDK-проект по умолчанию глобит файлы тестов в основную сборку.

**Этап 2 реализован** (после архитектурной консультации с Fable 5 — см. раздел «Архитектура объединения слоёв» выше), **затем сверен и исправлен по оригинальному PDF статьи Ochs** (2026-07-02, тот же день — см. «Как читать PDF в этой рабочей среде» выше про PyMuPDF-обходной путь):

- `Dynamics/` — `EmotionKind` (enum, 10 эмоций), `EmotionVector` (immutable, array-backed `readonly struct`, индексатор + `With`, клэмп `[0,1]`), `EmotionThresholds`, `Personality` (`readonly struct`, клэмп `[-1,1]²`), `ActionId`, `GameEvent`, `ActionDictionary` (объективный effect), `Appraisal` (субъективные attitude/praise одного NPC), `SocialRelation` (`IRelationship`, immutable; **liking/dominance клэмп `[-1,1]`, familiarity/solidarity клэмп `[0,1]`** — разные диапазоны, поправлено после сверки с PDF), `FloatRange`, `SocialRelationPattern` (запрос-паттерн для `Matches`).
- `Dynamics/Rules/` — `EmotionRules.ComputeStimulus` (триггер рис. 2 + интенсивность IV-A; поправлено: `effect==0` считается неотрицательным для желательности, praise-эмоции требуют `dc==1`), `PersonalityRules.Modulate`, `DecayRules.Decay`/`Merge`, `EmotionValence` (internal — точные наборы эмоций по измерениям: `LikingPositive/Negative`, `DominancePositive/Negative`, `DominanceExpressedPositive`, `SolidarityNegative`, `CoincidenceKinds`, `IncongruentPairs`), `SocialRelationDelta` (структура-дельта, складывается через `+`), `SocialRelationRules` (`FromOwnEmotion`/`FromObservedExpression`/`FromEmotionalCoincidence` — вычисляют дельту (= `f_sr`), `Apply` — применяет её один раз (= `g_sr`, internal `ApplyBoundedSigned`/`ApplyBoundedUnit`, см. `AssemblyInfo.cs` → `InternalsVisibleTo("RelationshipCore.Tests")`), `UpdateFamiliarityFromLikingShift`).
- Архитектура `SocialRelationRules` поменялась относительно первой версии: раньше три метода каждый сразу возвращал обновлённый `SocialRelation` (несколько источников за одно взаимодействие давали бы двойное демпфирование g_sr); теперь методы `From*` возвращают складываемую `SocialRelationDelta`, а `Apply` — единственная точка, где применяется пологая функция у краёв. Это прямое соответствие формуле статьи `φ_sr = g_sr(relation, f_sr(...))`.
- 62 юнит-теста в `RelationshipCore.Tests/Dynamics/` — все проходят (`dotnet test`), включая тесты на конкретные найденные баги (effect=0, dc-gate для praise, диапазоны familiarity/solidarity, точные наборы эмоций по рис. 4-6, отсутствие залипания на границах 0/1).
- Список найденных и исправленных расхождений с первой (пересказанной) версией — подробно в разделе «Модель Ochs — что реализовать» выше (это теперь сверенный со статьёй текст, не пересказ по памяти).

**Следующий шаг (Этап 3):** `NpcState` (Personality + EmotionVector + Appraisal, реестр `Dictionary<int, NpcState>` по `EntityId`), `SocialDynamicsEngine` (оркестратор в `RelationshipCore.Simulation`: `Perceive`/`ObserveExpression`, ленивое затухание по времени последнего обновления, запись результата в `DeepGraph` через `GetEdge`/`AddEdge`), `SocialRoleTable` (начальные `SocialRelation` из пар ролей через `AddCommonEdge`). Полный план — в разделе «Архитектура объединения слоёв» выше. Двигаться небольшими шагами с тестами.

Также: репозиторий разрабатывается локально без git; пользователь попросил выгрузить наработки на GitHub — уточнить у него реквизиты (имя репозитория, публичность) и данные аутентификации `gh`/git remote, если сессия сменилась до завершения этой части.
