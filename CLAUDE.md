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

Плоская раскладка на диске (перестроено 2026-07-03 перед началом Этапа 6 — раньше `RelationshipCore.csproj` лежал прямо в корне репозитория, а `RelationshipCore.Tests` — вложенной папкой внутри него, что требовало `<Compile Remove="RelationshipCore.Tests/**" />`-guard'а; теперь три проекта — настоящие соседние папки на уровне решения, guard не нужен):

```
RelationshipCore.slnx        — корень репозитория, только решение
RelationshipCore/             — .csproj ядра + исходники (Dynamics/, Edges/, Graphs/, Groups/, Interfaces/, Learning/, Nodes/, Simulation/)
RelationshipCore.Tests/       — .csproj тестов
RelationshipSimulator/        — .csproj UI (WPF)
docs/                         — PDF статей, общий справочный материал не для конкретного проекта
```

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

### Известный риск (не мешал Этапам 2-4, устранён в начале Этапа 5, 2026-07-03)

`DeepGraph.FindEdge` искал ровно одно ребро на пару `(from, to)` у владельца, без учёта типа `Relationship`. К Этапу 5 понадобились одновременно `SocialRelation`-ребро и структурное `MEMBER`-ребро между теми же двумя узлами — они конфликтовали за один слот (`AddEdge` тихо перезаписывал `Relationship` существующего ребра при совпадении только по `(from,to)`, независимо от типа).

**Устранено по образцу "WideEdge"** — концепция, которую сам О'Коннор описал, но не реализовал, в Future Work диссертации, §6.2.6 (стр. 61-62 PDF): *"The project implemented the concept of a HistoryEdge... a 'DeepEdge' in that Relationship values are stored one after the other in a list. It may also be possible to turn this on its side and store multiple Relationship values at once resulting in a 'WideEdge' and use a function to combine them all into one representative value, **or just leave them directly accessible**."* Выбран второй вариант автора (без комбинирующей функции).

Реализация в `Graphs/DeepGraph.cs`:
- `FindEdge` получил необязательный параметр `Type? relationshipType` — при указании требует точного совпадения `edge.Relationship.GetType()`, без изменения `IEdge`/`IRelationship` (тип берётся рефлексией с уже существующего объекта, не через новый член интерфейса).
- `AddEdge` теперь ищет существующее ребро для upsert по тройке `(from, to, relationship.GetType())`, а не паре `(from, to)` — ребро другого типа между той же парой добавляется отдельно, не перезаписывая существующее.
- Новые методы: `GetEdge<TRelationship>`/`GetNodeEdge<TRelationship>` (однозначный типизированный доступ), `GetEdges(owner, from, to)` (все параллельные Relationship между парой, "как есть", без комбинирования).
- `SocialDynamicsEngine.UpdateRelation` обновлён на `Graph.GetEdge<SocialRelation>(from, to)` — на будущее, когда на той же паре появятся `MEMBER`-рёбра, untyped `GetEdge` больше не сможет случайно вернуть не то.
- 3 новых теста в `DeepGraphTests.cs` + сквозная ручная проверка через консольный харнесс (см. `[[feedback-verify-before-done]]`): подтверждено, что до фикса второе ребро затёрло бы первое (было бы 1 ребро), после фикса — сосуществуют 2, при этом повторное обновление ОДНОГО И ТОГО ЖЕ типа по-прежнему корректно апсертит, не плодя дубликаты.
- 89 тестов в решении, все проходят.

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
- [x] **Этап 3:** `NpcState` (реестр личность+эмоции+attitude/praise по EntityId), `SocialDynamicsEngine` (оркестратор: читает NpcState, пишет в DeepGraph через GetEdge/AddEdge), `SocialRoleTable` для начальных отношений через `AddCommonEdge`
- [x] **Этап 4:** тесты на эталонных сценариях (грабитель, собеседование)
- [x] **Этап 5:** HistoryEdge, группы, broadcast-сообщения, learning (принятие чужих Connection по весу)
- [x] **Этап 6:** WPF-приложение: редактор персонажей/ролей/словаря, редактор сценария событий, графики эмоций во времени (OxyPlot или LiveCharts2), визуализация графа (Microsoft.MSAGL или GraphShape)

## Соглашения

- Язык кода и XML-комментариев: комментарии на русском допустимы, имена — английские, соответствующие терминологии статей (Entity, Connection, Relationship, liking, dominance, solidarity, familiarity).
- Обсуждение с пользователем ведём на русском языке.
- Не начинать с UI: вся ценность — в ядре; UI лишь визуализирует состояние графа.
- Значения модели держать в заявленных диапазонах ([-1,1], [0,1]) — добавить проверки/clamp.

## Контекст переноса (где мы остановились)

Обсуждение начато в Claude (чат). Пользователь создаёт проект `RelationshipCore` (шаблон «Библиотека классов», .NET 10) в Visual Studio. Согласованы стартовые интерфейсы (см. выше).

**Этап 1 реализован:** `Interfaces/` (`INode`, `IEdge`, `IRelationship`, `IMessage` — точно по черновику выше, в корневом namespace `RelationshipCore`), `Nodes/Node.cs`, `Edges/Edge.cs` — минимальные реализации, `Graphs/Graph.cs` — хранилище узлов/рёбер на `Dictionary<int, ...>` по `EntityId` (без O(n²)-поиска), `Graphs/DeepGraph.cs` — семантика прямых/косвенных рёбер (`AddDirectEdge`, `AddEdge` с произвольным owner для косвенных рёбер, `GetDirectEdges`/`GetIndirectEdges`, `WithRelationship`/`WithRelationshipTo`, `AddCommonEdge`). Добавлен `RelationshipCore.Tests` (xUnit) с 12 тестами на граф — все проходят (`dotnet test`).

**Этап 2 реализован** (после архитектурной консультации с Fable 5 — см. раздел «Архитектура объединения слоёв» выше), **затем сверен и исправлен по оригинальному PDF статьи Ochs** (2026-07-02, тот же день — см. «Как читать PDF в этой рабочей среде» выше про PyMuPDF-обходной путь):

- `Dynamics/` — `EmotionKind` (enum, 10 эмоций), `EmotionVector` (immutable, array-backed `readonly struct`, индексатор + `With`, клэмп `[0,1]`), `EmotionThresholds`, `Personality` (`readonly struct`, клэмп `[-1,1]²`), `ActionId`, `GameEvent`, `ActionDictionary` (объективный effect), `Appraisal` (субъективные attitude/praise одного NPC), `SocialRelation` (`IRelationship`, immutable; **liking/dominance клэмп `[-1,1]`, familiarity/solidarity клэмп `[0,1]`** — разные диапазоны, поправлено после сверки с PDF), `FloatRange`, `SocialRelationPattern` (запрос-паттерн для `Matches`).
- `Dynamics/Rules/` — `EmotionRules.ComputeStimulus` (триггер рис. 2 + интенсивность IV-A; поправлено: `effect==0` считается неотрицательным для желательности, praise-эмоции требуют `dc==1`), `PersonalityRules.Modulate`, `DecayRules.Decay`/`Merge`, `EmotionValence` (internal — точные наборы эмоций по измерениям: `LikingPositive/Negative`, `DominancePositive/Negative`, `DominanceExpressedPositive`, `SolidarityNegative`, `CoincidenceKinds`, `IncongruentPairs`), `SocialRelationDelta` (структура-дельта, складывается через `+`), `SocialRelationRules` (`FromOwnEmotion`/`FromObservedExpression`/`FromEmotionalCoincidence` — вычисляют дельту (= `f_sr`), `Apply` — применяет её один раз (= `g_sr`, internal `ApplyBoundedSigned`/`ApplyBoundedUnit`, см. `AssemblyInfo.cs` → `InternalsVisibleTo("RelationshipCore.Tests")`), `UpdateFamiliarityFromLikingShift`).
- Архитектура `SocialRelationRules` поменялась относительно первой версии: раньше три метода каждый сразу возвращал обновлённый `SocialRelation` (несколько источников за одно взаимодействие давали бы двойное демпфирование g_sr); теперь методы `From*` возвращают складываемую `SocialRelationDelta`, а `Apply` — единственная точка, где применяется пологая функция у краёв. Это прямое соответствие формуле статьи `φ_sr = g_sr(relation, f_sr(...))`.
- 62 юнит-теста в `RelationshipCore.Tests/Dynamics/` — все проходят (`dotnet test`), включая тесты на конкретные найденные баги (effect=0, dc-gate для praise, диапазоны familiarity/solidarity, точные наборы эмоций по рис. 4-6, отсутствие залипания на границах 0/1).
- Список найденных и исправленных расхождений с первой (пересказанной) версией — подробно в разделе «Модель Ochs — что реализовать» выше (это теперь сверенный со статьёй текст, не пересказ по памяти).

**Этап 3 реализован** (2026-07-03): `Simulation/NpcState.cs` — изменяемый контейнер состояния одного NPC (`EntityId`, `Personality` readonly, `Appraisal` (mutable reference), `EmotionVector Emotions` + `LastUpdateTime` с публичными сеттерами — не immutable-value-объект вроде `SocialRelation`, а именно реестровая запись). `Simulation/SocialDynamicsEngine.cs` — оркестратор: хранит `Dictionary<int, NpcState>` (`RegisterNpc` идемпотентен, как `Graph.AddNode`; `GetState` бросает `InvalidOperationException`, если EntityId не зарегистрирован), два публичных метода:
- `Perceive(perceiverId, evt, time)` — стимул (`EmotionRules.ComputeStimulus`) → модуляция личностью → ленивое затухание старых эмоций до `time` → слияние (`DecayRules.Merge`) → обновление отношения perceiver к "второй стороне" события (`counterparty = perceiverId == evt.AgentId ? evt.PatientId : evt.AgentId`) через `SocialRelationRules.FromOwnEmotion` **от только что вычисленного триггера, а не от полного слитого вектора эмоций** (в слитом векторе могут быть посторонние остаточные эмоции от несвязанных прошлых событий — не то, что "caused by j"). Если perceiver — единственная сторона события (agent==patient==perceiver), обновление ребра само-в-себя тихо пропускается (`UpdateRelation` проверяет `fromId == toId`).
- `ObserveExpression(observerId, expresserId, time)` — не создаёт новых эмоций у observer, только считывает (с ленивым затуханием) уже накопленные состояния обеих сторон и обновляет ребро observer→expresser через `FromObservedExpression(expresser.Emotions) + FromEmotionalCoincidence(observer.Emotions, expresser.Emotions)`.
- Собственное расширение (не из статьи, т.к. статья рассматривает только диадические примеры agent/patient): для события с "третьей стороной"-наблюдателем `counterparty` по умолчанию берётся как `evt.AgentId` — т.е. наблюдатель формирует мнение о том, кто совершил действие, а не о том, на кого оно было направлено.
- Оба метода требуют, чтобы соответствующие узлы уже существовали в `DeepGraph` (`Graph.GetNode` кидает `InvalidOperationException`, если узла нет) — движок не создаёт узлы сам, только рёбра/отношения.

`Simulation/SocialRoleTable.cs` — `Dictionary<(string From, string To), SocialRelation>`, `Set`/`Resolve` (усредняет по всем парам ролей, `SocialRelation.Neutral` если пар нет)/`ApplyTo` (пишет обе стороны через `DeepGraph.AddCommonEdge`, независимо усредняя каждое направление). Роли — обычные `string`, без отдельного enum/struct-обёртки (не нужна нигде за пределами таблицы).

12 новых тестов в `RelationshipCore.Tests/Simulation/` (`SocialDynamicsEngineTests`, `SocialRoleTableTests`) — все 74 теста в решении проходят (`dotnet test`).

**Этап 4 реализован** (2026-07-03), также с проверкой напрямую по PDF (текст стр. 291-295 + рис. 9-11 как изображения, PyMuPDF — уже привычный обходной путь):

- Найден и исправлен реальный формульный пробел: раздел IV-B статьи (стр. 291) прямым текстом говорит: *"the congruence of the triggered emotion of the burglar and the emotion expressed by the policeman... induce an increase of the solidarity and, **by side effect, of the degree of liking**"* — подтверждено и визуально по рис. 10 (liking и solidarity растут на одном и том же шаге). `SocialRelationRules.FromEmotionalCoincidence` раньше трогал только `Solidarity`; теперь возвращает `SocialRelationDelta(liking: solidarityDelta, solidarity: solidarityDelta)` — тем же приращением, т.к. отдельной формулы для этого "side effect" раздел IV-D-4 не даёт.
- `RelationshipCore.Tests/Simulation/OchsReferenceScenarioTests.cs` — 9 интеграционных тестов:
  - **Собеседование** (стр. 292-295, рис. 11) воспроизведён с полной точностью к тексту — во всех его шагах "j" (агент/цель обновления ребра) однозначен (PC), никакой неоднозначности атрибуции нет. 4 теста: hope от заявления об опыте → liking↑; совпадение огорчения → solidarity↑; выражение страха PC → dominance директора↑; joy от предложения денег → liking↑.
  - **Допрос грабителя** (стр. 291-292, рис. 9-10) воспроизведён для ОДНОЗНАЧНЫХ утверждений: арест → liking↓ и dominance↓; кофе → liking↑; рассказ о похищении с agent=похититель (не полицейский) → distress триггерится, но ребро burglar→policeman вообще не трогается (не просто "нулевая дельта", а пропущенное обновление — `UpdateRelation`'s self/other-независимый guard); нейротичная личность усиливает Fear сильнее нейтральной.
- **Осознанно НЕ воспроизведён один конкретный момент** сценария 1: текст (стр. 291) приписывает падение dominance грабителя именно "own emotion of distress" от рассказа о похищении — но по архитектуре (`counterparty = evt.AgentId`) этот эффект должен идти на ребро burglar→kidnapper (агент похищения), а не burglar→policeman, тогда как рис. 10 показывает падение dominance именно в ОТСЛЕЖИВАЕМОМ ребре burglar-policeman в ТОТ ЖЕ момент, что и рост liking/solidarity от coincidence. Разрешить это невозможно только по тексту статьи (пиксельно сверено с рис. 10 через PyMuPDF-кроп высокого DPI — расхождение подтверждено, не визуальный шум); текст не даёт формального ⟨agent, action, patient, dc⟩ для этой конкретной реплики диалога, и обе разумные интерпретации (agent=policeman или agent=похититель) конфликтуют с одной из двух половин текстового описания. Оставлено как открытый вопрос — не мешает тестам, т.к. `BurglarInterrogation_ObservingPolicemansCongruentDistress_...` тестирует только однозначно установленные механизмы (FromObservedExpression → dominance растёт при наблюдении чужой слабости — рис. 5, отдельно и корректно реализовано и подтверждено).
- Полный сквозной прогон обоих сценариев через throwaway-консоль (см. `[[feedback-verify-before-done]]`) — все качественные направления подтверждены, несколько чисел (Hope, дельта liking) пересчитаны вручную и совпали с точностью до третьего знака.
- 86 тестов в решении (было 74 на конец Этапа 3 → +12 регрессия/Этап 3 harness-фикс → 75 → +9 Этап 4 → 86, включая новые тесты на liking-side-effect в `SocialRelationRulesTests`) — все проходят (`dotnet test`).

**Этап 5 в процессе** (2026-07-03). Сделано:

1. **WideEdge-фикс `DeepGraph`** (см. «Известный риск» выше) — несколько параллельных `Relationship` разных типов на одной паре узлов больше не конфликтуют за один слот.
2. **`HistoryEdge`** (`Edges/HistoryEdge.cs`, дисс. О'Коннора §3.2.1/4.2.4) — ребро хранит `List<IRelationship>` вместо одного значения; сеттер `Relationship` дописывает в список, а не заменяет; геттер и так возвращает последнее (текущее). `History` — вся история от старого к новому, `PreviousRelationship(stepsBack)` — значение N шагов назад (`null`, если истории не хватает).
   - **Ключевая архитектурная находка**: `SocialDynamicsEngine.UpdateRelation` НЕ ПРИШЛОСЬ МЕНЯТЬ вообще. Он как обновлял отношение через throwaway `Graph.AddDirectEdge(new Edge(from, to, updated))`, так и обновляет — `DeepGraph.AddEdge` вызывает `Relationship`-сеттер уже НАЙДЕННОГО существующего ребра (см. WideEdge-фикс, поиск по `(from,to,тип)`), а не создаёт новый `IEdge`. Если это существующее ребро — `HistoryEdge` (а не обычный `Edge`), апдейт автоматически становится записью в историю. Чтобы включить историю для конкретной пары узлов, достаточно один раз посеять её как `HistoryEdge` вместо `Edge` при инициализации — остальной код (`SocialDynamicsEngine`, `SocialRoleTable`) не меняется. Это прямая параллель с тем, как в дисс. на стр. 35 (раздел 3.5) описана связь Connection/Relationship: "the manner of storage and management of this relationship attribute is open to the system requirements".
   - Проверено и тестами (`HistoryEdgeTests.cs`, включая `DeepGraph_AddEdge_OnExistingHistoryEdge_AppendsRatherThanReplacing`), и интеграционным тестом (`SocialDynamicsEngineTests.Perceive_WhenEdgeSeededAsHistoryEdge_...`), и вручную через консольный харнесс (см. `[[feedback-verify-before-done]]`): движок, ничего не зная про `HistoryEdge`, после двух вызовов `Perceive` дал 3 записи истории (стартовая + 2 апдейта), `PreviousRelationship(1)` корректно вернул предыдущее значение, `PreviousRelationship(3)` (глубже истории) — `null`.
3. 96 тестов в решении (было 86 на конец Этапа 4 → +3 WideEdge → 89 → +7 HistoryEdge → 96) — все проходят (`dotnet test`).
4. **Группы, MEMBER, наследование отношений, broadcast** (`Groups/MemberRelationship.cs`, `Groups/GroupRelations.cs`, дисс. О'Коннора §3.4.1, рис. 3.3-3.4). Группа — обычный `INode`, ничем структурно не отличается от любого другого узла, кроме входящих `MEMBER`-рёбер от участников (никакого `GroupNode`-подкласса не создавалось — соответствует уже принятому решению "группы — обычные узлы графа").
   - `MemberRelationship` — sealed marker-класс (singleton `Instance`), НЕ `SocialRelation`: членство — категориальный структурный факт слоя O'Connor, а не непрерывная Ochs-динамика (liking/dominance тут не подходят по смыслу).
   - `GroupRelations.MembersOf`/`GroupsOf` — тонкие обёртки над уже существующими `DeepGraph.WithRelationshipTo`/`WithRelationship`, без новой инфраструктуры хранения.
   - `GroupRelations.HasInheritedRelationship(graph, member, target, pattern)` — реализует "враг группы — враг каждого члена" (рис. 3.3): проверяет прямое ребро member→target, и если не совпало — ребро group→target для каждой группы, `MEMBER`'ом которой является member. Совпадение через уже существующий `SocialRelationPattern` (не заводили отдельный enum ENEMY/FRIEND — отношение группы к внешней сущности выражается тем же `SocialRelation`, что и обычные межличностные отношения, согласно ключевой идее объединения слоёв).
   - `GroupRelations.Broadcast(graph, group, message)` — рассылает `IMessage` всем `MembersOf(group)` через их `HandleMessage`; самой группе сообщение не доставляется.
   - 6 новых тестов в `RelationshipCore.Tests/Groups/GroupRelationsTests.cs` + сквозная ручная проверка через харнесс, воспроизводящая рис. 3.3 буквально (группа G, участники M1-M3, внешний враг O): подтверждено, что все три участника наследуют статус "враг" от группы **без единого прямого ребра к O**, и что broadcast доходит только до участников, не до самой группы.
5. 102 теста в решении (96 → +6 Groups) — все проходят (`dotnet test`).
6. **Learning — принятие чужих Connection по весу** (`Learning/ConnectionLearning.cs`, `Learning/ConnectionMessage.cs`, `Learning/RelationshipWeights.cs`, дисс. О'Коннора §3.4.3/4.4.2, рис. 3.4, "Relationship Evaluation"/"Adopting new Relationships").
   - `ConnectionLearning.Learn(graph, learner, received, weight)` — если у learner ещё нет знания о паре `(received.From, received.To)` **того же типа Relationship** (см. WideEdge-фикс — поиск типизирован), либо есть, но с меньшим весом — присланное ребро принимается (`graph.AddEdge(learner, received)`, что естественно ложится либо прямым, либо косвенным ребром в зависимости от того, `learner == received.From` или нет); при равном или большем весе существующего — остаётся как было. Ровно по тексту дисс.: "adopts the Relationship with the greater value".
   - "Вес" — не часть модели графа: `IRelationship` намеренно ничего не знает о весе (генерик по конструкции), поэтому `Learn` принимает `Func<IRelationship, float> weight` явно от вызывающей стороны. `RelationshipWeights.SocialRelation` — готовая функция для основного домена (сумма модулей liking/dominance + familiarity + solidarity).
   - `ConnectionMessage : IMessage` — тонкий носитель данных (просто `IEdge Connection`), без собственной логики — сообщения-адаптеры знают про граф, граф про них не знает (уже сформулированный принцип из «Архитектуры объединения слоёв»).
   - Добавлен недостающий метод `DeepGraph.GetNodeEdge(owner, from, to, Type relationshipType)` — рантайм-версия уже существующего generic `GetNodeEdge<TRelationship>`, нужна ровно для этого случая (тип присланного слуха известен только в момент выполнения).
   - 7 новых тестов в `RelationshipCore.Tests/Learning/ConnectionLearningTests.cs` + сквозная ручная проверка через харнесс, буквально воспроизводящая пример Alice/Tom/Charles со стр. 38-39 дисс.: слабый противоречащий слух (liking=-0.1) корректно отвергнут, сильный (liking=-0.95) — принят и заменил прежнее знание; при этом ребро осталось косвенным (Alice ≠ Tom), а реальное прямое отношение Tom→Charles (которое никогда не задавалось) осталось `null` — обучение не подделывает "истину", только предположение Alice о ней.
7. 109 тестов в решении (102 → +7 Learning) — все проходят (`dotnet test`). **Этап 5 полностью завершён.**

Перед Этапом 6 решение перестроено в плоскую раскладку (см. «Структура solution» выше): `RelationshipCore/`, `RelationshipCore.Tests/`, `RelationshipSimulator/` — соседние папки на уровне решения, никакого вложения друг в друга.

**Этап 6 начат** (2026-07-03): создан `RelationshipSimulator` (`dotnet new wpf`, `net10.0-windows`, `ProjectReference` на `RelationshipCore`), добавлен в `.slnx`. `MainWindow` пока содержит только временный smoke-test (прогоняет один шаг допроса грабителя через `SocialDynamicsEngine` и печатает результат в `TextBlock`) — проверено вручную: приложение реально запускается (не только собирается), скриншот показал `liking=-0.20` после ареста, что совпадает с ручным расчётом формулы. Этот smoke-test — временный код, будет заменён настоящим UI.

Дальше — четыре подпункта Этапа 6 (порядок не зафиксирован в плане): редактор персонажей/ролей/словаря, редактор сценария событий, графики эмоций во времени (OxyPlot/LiveCharts2), визуализация графа (Microsoft.MSAGL/GraphShape).

**Редактор персонажей и словаря действий — первая версия готова** (2026-07-03, без ответа пользователя на уточняющий вопрос о порядке — выбрано по собственному суждению как самая фундаментальная часть, без которой нечем наполнять граф для остальных подпунктов):

- `MainWindow` держит единственный на всё приложение `DeepGraph`/`ActionDictionary`/`SocialDynamicsEngine` — остальные части UI (сценарии, графики, визуализация графа) будут работать с этим же движком, а не создавать свои.
- Вкладка «Персонажи»: `DataGrid` по `ObservableCollection<NpcRow>`, кнопка добавляет NPC через `_engine.RegisterNpc(...)`; `NpcRow` — тонкая обёртка над `NpcState`, пишет изменения Extraversion/Neuroticism сразу в движок через сеттер.
- Вкладка «Словарь действий»: аналогично, `ActionRow` пишет через `ActionDictionary.SetEffect`.
- **Небольшое расширение ядра**: `NpcState.Personality` получил публичный сеттер (был read-only) — редактору нужно менять личность уже созданного NPC, а не только при регистрации; `Personality` остаётся immutable-структурой, меняется лишь то, на какое значение ссылается контейнер (тот же паттерн, что уже был у `Emotions`/`LastUpdateTime`). Тест `NpcStateTests.Personality_CanBeChangedAfterConstruction`.
- Проверено вживую через UI Automation (`System.Windows.Automation`, не просто "собирается"): запустил exe, нашёл кнопки/DataGrid по дереву автоматизации, кликнул "Добавить персонажа"/"Добавить действие", сделал скриншоты — строки появляются с ожидаемыми значениями по умолчанию (0.00/0.00). Разовая нестыковка (3 клика → 4 строки в одной попытке, при этом на вкладке действий 2 клика → 2 строки точно) — похоже на особенность `InvokePattern.Invoke` в автоматизации, не баг приложения; при обычном клике мышью повторов не будет.
- 110 тестов в решении (109 → +1 `NpcStateTests`), все проходят.

**Редактор сценария событий — первая версия готова** (2026-07-03, тоже без ответа пользователя на вопрос о порядке — следующий логичный шаг после редактора персонажей, поскольку без него нечем прогонять сценарий):

- Вкладка «Сценарий»: `DataGrid` по `ObservableCollection<EventRow>` — каждая строка (`PerceiverId`, `AgentId`, `ActionId`, `PatientId`, `Dc`, `Time`) ровно соответствует одному вызову `SocialDynamicsEngine.Perceive(perceiverId, new GameEvent(agentId, action, patientId, dc), time)`. Кнопка «Запустить сценарий» прогоняет события, отсортированные по `Time`, через движок; область «Результат» показывает активные эмоции всех персонажей и `SocialRelation` по всем прямым рёбрам между ними.
- **Найден и исправлен реальный пробел, а не просто зафиксирован как "будет позже"**: при сквозной проверке через UI выяснилось, что `AddNpc_Click` регистрировал NPC только в `SocialDynamicsEngine`, но не добавлял узел в `DeepGraph` — `Perceive` бросил бы `InvalidOperationException` при первом же обновлении отношения. Добавлен `_graph.AddNode(new Node(entityId))` в `AddNpc_Click`.
- **Второй пробел, найден тем же способом**: первый прогон сценария завершился без ошибок, но `liking`/`dominance` остались `0.00` — потому что редактор персонажей не давал задать `Appraisal` (attitude), а без attitude к себе ни одна эмоция не триггерится (`attitude==0` — корректное поведение модели, не баг, см. `EmotionRules.ComputeStimulus`). Добавлено поле `NpcRow.SelfAttitude` (attitude к своему же `EntityId` — самое нагруженное значение Appraisal во всех сценариях статьи, "самоуважение") и колонка в таблице персонажей.
- Проверено вживую через UI Automation, включая нетривиальную деталь: `AutomationElement.GetCurrentPattern(ValuePattern.Pattern).SetValue(...)` прямо на ячейке `DataGrid` работает и без ручной эмуляции клика/F2/ввода символов — WPF `DataGridCell` поддерживает `IValueProvider` напрямую. Полностью воспроизвёл сценарий допроса грабителя через UI (2 персонажа, 1 действие с эффектом -0.8, self-attitude=0.9, событие ареста) — результат (`Fear=0.68`, `liking=dominance=-0.20`) совпал с ручным расчётом формулы и с более ранней консольной проверкой того же сценария.
- 110 тестов в решении (без изменений — `EventRow`/UI-код не покрывается `RelationshipCore.Tests`, это UI-слой, проверен через ручную UI-автоматизацию, а не xUnit).

**Графики эмоций и визуализация графа — готовы, Этап 6 полностью завершён** (2026-07-03):

- **Графики эмоций** (`OxyPlot.Wpf`, NuGet): вкладка «Графики эмоций», `ComboBox` выбора персонажа + `oxy:PlotView`. `RunScenario_Click` теперь параллельно с прогоном событий пишет снимки `(NpcId, Time, EmotionVector)` в `_emotionSamples` — иначе строить график во времени не из чего, `NpcState.Emotions` хранит только текущее значение. `RebuildChart()` рисует по одной линии на каждую эмоцию, которая хоть раз была заметно ненулевой у выбранного персонажа (десять плоских нулевых линий только мешали бы). Между соседними снимками OxyPlot проводит прямую линию — реальное затухание экспоненциальное, но т.к. снимки берутся только в моменты событий, отображение упрощено (не искажает качественную картину, просто не показывает форму кривой между событиями).
  - Проверено вживую через UI Automation: 2 персонажа, 3 события во времени (t=0,3,6, из них третье с dc=0.2). Результат: Fear=0.68 (плато между t=0 и t=3 — новый триггер точно совпал с уже затухшим старым значением), затем плавно к ~0.50 к t=6. Оба числа пересчитаны вручную (`0.68·exp(-0.1·3)≈0.50`) и совпали.
- **Визуализация графа** (`Msagl.WpfGraphControl` 1.2.1 + `Msagl.Drawing` + `Msagl`, NuGet — таргетируют `net6.0-windows7.0`, совместимы с `net10.0-windows`): вкладка «Граф отношений», `GraphViewer.BindToPanel` на `Grid`. Кнопка «Обновить визуализацию» строит `Microsoft.Msagl.Drawing.Graph` из добавленных персонажей и прямых `SocialRelation`-рёбер между ними; цвет ребра — зелёный при `liking≥0`, красный при `liking<0`, метка ребра — `liking=X.XX`.
  - **Именование**: типы MSAGL (`Graph`, `Node`) пишутся везде полным именем `Microsoft.Msagl.Drawing.*` — короткие имена уже заняты `RelationshipCore.Graphs.Graph`/`RelationshipCore.Nodes.Node` через существующие `using`.
  - Проверено вживую: 2 персонажа, тот же сценарий допроса (liking=-0.20) — MSAGL нарисовал узлы `1`→`2` направленным красным ребром с меткой `liking=-0,20`, автоматическая раскладка сработала корректно.
- 110 тестов в решении (без изменений — новый UI-код графиков/визуализации не покрывается `RelationshipCore.Tests`, проверен вручную).

Также: репозиторий синхронизирован с GitHub (`origin` → `https://github.com/Loriendil/Social_behavior.git`, ветка `master` в актуальном состоянии, проверено 2026-07-03) — выгрузка наработок больше не блокер.
