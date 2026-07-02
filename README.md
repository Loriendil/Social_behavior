# RelationshipCore

Библиотека на C# (.NET 10), моделирующая отношения и эмоции NPC в играх. Объединяет идеи
двух научных работ:

1. **O'Connor, "A Relationship Model for Believable Social Dynamics of Characters in Games"**
   (диссертация, Trinity College Dublin, 2015) — модель **RelationshipGraph**: направленный
   граф отношений между сущностями (персонажи, группы, предметы, локации).
2. **Ochs, Sabouret, Corruble, "Simulation of the Dynamics of Nonplayer Characters' Emotions
   and Social Relations in Games"** (IEEE TCIAIG, 2009) — динамика: события порождают эмоции
   (упрощённая OCC-модель), личность масштабирует их интенсивность, эмоции обновляют
   социальные отношения.

**Идея объединения:** граф O'Connor — это хранилище и API; модель Ochs — правила обновления
значений на рёбрах этого графа. Атрибут `Relationship` на ребре — не enum вида `"FRIEND"`,
а объект `SocialRelation` с четырьмя числовыми измерениями (liking, dominance, familiarity,
solidarity) из статьи Ochs.

Подробности архитектуры, обоснование решений и roadmap — в [`CLAUDE.md`](CLAUDE.md).

## Структура кода

```
RelationshipCore/
├── Interfaces/        — INode, IEdge, IRelationship, IMessage (ядро контрактов)
├── Nodes/              — Node : INode (минимальная реализация)
├── Edges/              — Edge : IEdge (минимальная реализация)
├── Graphs/             — Graph (хранилище по EntityId), DeepGraph (прямые/косвенные рёбра)
├── Dynamics/           — типы модели Ochs: EmotionVector, Personality, GameEvent,
│                         ActionDictionary, Appraisal, SocialRelation (IRelationship),
│                         SocialRelationPattern — не зависят от графа
└── Dynamics/Rules/     — чистые формулы: EmotionRules, PersonalityRules, DecayRules,
                          SocialRelationRules

RelationshipCore.Tests/ — юнит-тесты (xUnit), зеркалит структуру выше
```

Слой `Dynamics`/`Dynamics.Rules` намеренно не знает про граф (`Graphs`/`INode`) — это чистая
математика, которую можно тестировать и переиспользовать отдельно (в том числе в будущем
Unity-порте). Связывающий слой (`Simulation`, читает/пишет граф на основе формул) — следующий
этап работы, пока не реализован.

## Текущий статус

- ✅ **Этап 1** — граф: интерфейсы + `Graph`/`DeepGraph`.
- ✅ **Этап 2** — модель Ochs: типы-значения + формулы (стимул → личность → триггер →
  затухание → обновление отношений), протестированы изолированно от графа.
- ⏳ **Этап 3** (следующий) — `NpcState` + `SocialDynamicsEngine`, связывающие формулы Ochs с
  `DeepGraph`.

Полный roadmap и список принятых архитектурных решений — в `CLAUDE.md`.

## Сборка и тесты

```powershell
dotnet build
dotnet test
```

На момент последнего коммита: 64 теста, все проходят.

## Справочные материалы

PDF обеих статей и архив с оригинальным кодом O'Connor лежат локально в `docs/` — в
репозиторий не закоммичены (авторские права), см. `.gitignore`.
