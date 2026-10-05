# cards.json 翻译记录

> 翻译依据：`zhs/cards.json` → `eng/cards.json`（`jpn/cards.json` 用英语填充，与 eng 一致）
> 规则参考：`.agents/skills/translate/SKILL.md`、`docs/翻译术语表.md`
> 怪物相关卡牌（状态牌 / 招式牌）的记录见 `docs/monster翻译.md`

---

## 幻梦无垠 / 深梦无觉 / 噩梦无终（梦境三牌）

三张牌可经休息时的「梦醒时」奖励互相变化（见 `DreamAwakeningReward`）。三张牌生成的衍生牌只拥有[gold]虚无[/gold]（无消耗）；幻梦无垠生成的衍生牌额外「首次打出费用-1」。

### 中文原文
```json
"TOUHOUANCIENTS-ILLUSORY_DREAM_WHISPER.title": "幻梦无垠",
"TOUHOUANCIENTS-ILLUSORY_DREAM_WHISPER.description": "将[blue]{Cards:diff()}[/blue]张随机[gold]攻击牌[/gold]或[gold]技能牌[/gold]加入你的[gold]抽牌堆[/gold]，这些牌拥有[gold]虚无[/gold]，且首次打出时费用减少[blue]1[/blue]点。\n每当你休息时，你可以将其变化为[gold]深梦无觉[/gold]或[gold]噩梦无终[/gold]。",

"TOUHOUANCIENTS-DEEP_DREAM_SLUMBER.title": "深梦无觉",
"TOUHOUANCIENTS-DEEP_DREAM_SLUMBER.description": "获得{Block:diff()}点[gold]格挡[/gold]。\n将[blue]{Cards:diff()}[/blue]张随机[gold]技能牌[/gold]加入你的[gold]抽牌堆[/gold]，这些牌拥有[gold]虚无[/gold]。\n每当你休息时，你可以将其变化为[gold]幻梦无垠[/gold]或[gold]噩梦无终[/gold]。",

"TOUHOUANCIENTS-ENDLESS_NIGHTMARE.title": "噩梦无终",
"TOUHOUANCIENTS-ENDLESS_NIGHTMARE.description": "造成{Damage:diff()}点伤害。\n将[blue]{Cards:diff()}[/blue]张随机[gold]攻击牌[/gold]加入你的[gold]抽牌堆[/gold]，这些牌拥有[gold]虚无[/gold]。\n每当你休息时，你可以将其变化为[gold]幻梦无垠[/gold]或[gold]深梦无觉[/gold]。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Illusory Dream Boundless` / `Deep Dream Insensate` / `Ominous Dream Eternal` |
| `...ILLUSORY_DREAM_WHISPER.description` | `Add {Cards:diff()} random [gold]Attack[/gold] or [gold]Skill[/gold] cards to your [gold]Draw Pile[/gold]. They have [gold]Ethereal[/gold] and cost [blue]1[/blue] less until played.\nWhenever you rest, you may transform it into [gold]Deep Dream Insensate[/gold] or [gold]Ominous Dream Eternal[/gold].` |
| `...DEEP_DREAM_SLUMBER.description` | `Gain {Block:diff()} [gold]Block[/gold].\nAdd {Cards:diff()} random [gold]Skill[/gold] cards to your [gold]Draw Pile[/gold]. They have [gold]Ethereal[/gold].\nWhenever you rest, you may transform it into [gold]Illusory Dream Boundless[/gold] or [gold]Ominous Dream Eternal[/gold].` |
| `...ENDLESS_NIGHTMARE.description` | `Deal {Damage:diff()} damage.\nAdd {Cards:diff()} random [gold]Attack[/gold] cards to your [gold]Draw Pile[/gold]. They have [gold]Ethereal[/gold].\nWhenever you rest, you may transform it into [gold]Illusory Dream Boundless[/gold] or [gold]Deep Dream Insensate[/gold].` |

### 备注
- 标题统一为「X Dream YY」三段式；旧名「幻梦呢喃」的 `Illusory Dream Whisper` 已弃用，「无垠」取 `Boundless`。
- **三张标题统一为「X Dream YY」三段式**（对应中文「[X梦][XX]」结构）：`Illusory Dream Boundless` / `Deep Dream Insensate` / `Ominous Dream Eternal`。`Boundless` 对「无垠」；`Insensate`（无知觉的）对「无觉」，不用 `Slumber` 以免与「沉睡」混淆；`Ominous` 对「噩」、`Eternal` 对「无终」，不用 `Nightmare`（那会把「噩+梦」合成一个词，破坏三段式）。
- 「每当你休息时，你可以将其变化为 A 或 B」逐字对应 `Whenever you rest, you may transform it into A or B`；牌名沿用各卡 `title` 译文并用 `[gold]` 包裹。
- 衍生牌：三张牌生成的牌只加 `CardKeyword.Ethereal`（已去掉 `Exhaust`）；幻梦无垠额外 `EnergyCost.AddUntilPlayed(-1)`（首次打出费用-1），英文 `cost [blue]1[/blue] less until played`。
- 「张数」的 `{Cards:diff()}` **不加** `[blue]`（用户指定），页面上按升级差值（3→4）自带颜色；`{Block:diff()}` / `{Damage:diff()}` 也同为裸变量。

---

## 「梦醒时」奖励（`gameplay_ui.json`）

**键名**: `TOUHOUANCIENTS-DREAM_AWAKENING_REWARD`

### 中文原文
```json
"TOUHOUANCIENTS-DREAM_AWAKENING_REWARD": "梦醒时，变化你的{Card}。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `TOUHOUANCIENTS-DREAM_AWAKENING_REWARD` | `Upon waking, transform your {Card}.` |

### 备注
- `{Card}` 由 `DreamAwakeningReward.Description` 传入卡牌 `Title`（嵌套 `LocString`，同原版 `SpecialCardReward`），**不要**改名为 `CardName` 之类的其他变量。
- 奖励文本只有描述栏，没有独立标题栏，所以「梦醒时」这个名称必须写在描述里。

---

## 妖怪治退（`YOUKAI_EXTERMINATION`）

**键名**: `TOUHOUANCIENTS-YOUKAI_EXTERMINATION`

### 中文原文
```json
"TOUHOUANCIENTS-YOUKAI_EXTERMINATION.title": "妖怪治退",
"TOUHOUANCIENTS-YOUKAI_EXTERMINATION.description": "造成{Damage:diff()}点伤害[blue]{Repeat}[/blue]次。\n[gold]斩杀[/gold]时，获得{energyPrefix:energyIcons(1)}，这张牌在本局游戏中的伤害永久性增加{Increase:diff()}。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Youkai Extermination` |
| `.description` | `Deal {Damage:diff()} damage [blue]{Repeat}[/blue] times.\nIf [gold]Fatal[/gold], gain {energyPrefix:energyIcons(1)} and permanently increase this card's damage by {Increase:diff()}.` |

### 备注
- 多段伤害句式取原版 `CELESTIAL_MIGHT` / `GUNK_UP`：`Deal {Damage:diff()} damage ... times.`；本卡 `RepeatVar(2)` 不随升级变化，故按 zhs 保留 `[blue]{Repeat}[/blue]`（不加 `:diff()`）。
- 「斩杀时」按项目已定映射译 `If [gold]Fatal[/gold],`（见 `relic翻译.md` 誊写卷轴条的复核表：对应原版 `FEED` / `THE_HUNT` / `HAND_OF_GREED`）。
- 「永久性增加」取原版 `THE_SCYTHE` / `GENETIC_ALGORITHM`：`Permanently increase this card's damage by {Increase:diff()}.`；`{energyPrefix:energyIcons(1)}` 直接接 `gain`（同 `POSSESSED_BY_SEIGA_POWER`）。
- 卡名须与 `relics.json` 的 `PARADISE_DREAM`（乐园之梦）描述里 `[gold]妖怪治退[/gold]` 完全一致 —— 两处均已译为 `Youkai Extermination`。
- ⚠️ 顺带发现：`SHINING_TOWER.description`（光辉宝塔）用的是 `On [gold]Fatal[/gold],`，与项目已定映射 `If [gold]Fatal[/gold],` 不一致，待确认后统一。

---

## 附魔「回响」（`ECHO`，`enchantments.json`）

> 本项目暂无独立的 `enchantments.json` 翻译记录文档，附魔条目暂记于本文件。

**键名**: `TOUHOUANCIENTS-ECHO`

### 中文原文
```json
"TOUHOUANCIENTS-ECHO.description": "打出此牌后，将一张本回合耗能增加[blue]1[/blue]的复制品加入你的[gold]手牌[/gold]。",
"TOUHOUANCIENTS-ECHO.title": "回响"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Echo` |
| `.description` | `When played, add a copy of this card to your [gold]Hand[/gold]. It costs [blue]1[/blue] more this turn.` |

### 备注
- 「打出此牌后」用 `When played,`（项目内 `EXORCISM` / `MIRACLE` 同写法）；「复制品」用原版 `ANGER` / `JUGGLING` 的 `add a copy of this card`。
- 费用 +1 由 `copy.EnergyCost.AddThisTurnOrUntilPlayed(1)` 实现，故句尾限定 `this turn`；原版 `TRANSFIGURE` 的 `It costs an extra {Energy:energyIcons()}.` 不带回合限定，本处按机制补 `this turn`。
- 复制品保留本附魔（可连锁、费用逐次递增），与 zhs 一样不在描述中额外说明。
- 本附魔无 `extraCardText`（zhs 侧只有 `description` / `title`）。

---

## 「变化」奖励（`gameplay_ui.json`）

**键名**: `TOUHOUANCIENTS-TRANSFORM_CARD_REWARD`

| 字段 | 翻译 |
|------|------|
| `TOUHOUANCIENTS-TRANSFORM_CARD_REWARD` | `Transform {cards} card(s).` |

### 备注
- 与既有 `TOUHOUANCIENTS-UPGRADE_CARD_REWARD`（`Upgrade {cards} card(s).`）保持同一句式；`{cards}` 为小写变量名，不可改。
- 本条与超ZUN啤酒（potions）的机制改动译文已在上一轮完成，此处仅作登记。

