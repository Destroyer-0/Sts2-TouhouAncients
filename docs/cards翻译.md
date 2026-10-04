# cards.json 翻译记录

> 翻译依据：`zhs/cards.json` → `eng/cards.json`（`jpn/cards.json` 用英语填充，与 eng 一致）
> 规则参考：`.agents/skills/translate/SKILL.md`、`docs/翻译术语表.md`
> 怪物相关卡牌（状态牌 / 招式牌）的记录见 `docs/monster翻译.md`

---

## 幻梦呢喃 / 深梦无觉 / 噩梦无终（梦境三牌）

三张牌可经休息时的「梦醒时」奖励互相变化（见 `DreamAwakeningReward`）。

### 中文原文
```json
"TOUHOUANCIENTS-ILLUSORY_DREAM_WHISPER.title": "幻梦呢喃",
"TOUHOUANCIENTS-ILLUSORY_DREAM_WHISPER.description": "将[blue]{Cards:diff()}[/blue]张随机[gold]攻击牌[/gold]或[gold]技能牌[/gold]加入你的[gold]抽牌堆[/gold]，这些牌拥有[gold]消耗[/gold]与[gold]虚无[/gold]。\n每当你休息时，你可以将其变化为[gold]深梦无觉[/gold]或[gold]噩梦无终[/gold]。",

"TOUHOUANCIENTS-DEEP_DREAM_SLUMBER.title": "深梦无觉",
"TOUHOUANCIENTS-DEEP_DREAM_SLUMBER.description": "获得{Block:diff()}点[gold]格挡[/gold]。\n将[blue]{Cards:diff()}[/blue]张随机[gold]技能牌[/gold]加入你的[gold]抽牌堆[/gold]，这些牌拥有[gold]消耗[/gold]与[gold]虚无[/gold]。\n每当你休息时，你可以将其变化为[gold]幻梦呢喃[/gold]或[gold]噩梦无终[/gold]。",

"TOUHOUANCIENTS-ENDLESS_NIGHTMARE.title": "噩梦无终",
"TOUHOUANCIENTS-ENDLESS_NIGHTMARE.description": "造成{Damage:diff()}点伤害。\n将[blue]{Cards:diff()}[/blue]张随机[gold]攻击牌[/gold]加入你的[gold]抽牌堆[/gold]，这些牌拥有[gold]消耗[/gold]与[gold]虚无[/gold]。\n每当你休息时，你可以将其变化为[gold]幻梦呢喃[/gold]或[gold]深梦无觉[/gold]。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Illusory Dream Whisper` / `Deep Dream Oblivion` / `Ominous Dream Eternal` |
| `...ILLUSORY_DREAM_WHISPER.description` | `Add {Cards:diff()} random [gold]Attack[/gold] or [gold]Skill[/gold] cards to your [gold]Draw Pile[/gold]. They have [gold]Exhaust[/gold] and [gold]Ethereal[/gold].\nWhenever you rest, you may transform it into [gold]Deep Dream Oblivion[/gold] or [gold]Ominous Dream Eternal[/gold].` |
| `...DEEP_DREAM_SLUMBER.description` | `Gain {Block:diff()} [gold]Block[/gold].\nAdd {Cards:diff()} random [gold]Skill[/gold] cards to your [gold]Draw Pile[/gold]. They have [gold]Exhaust[/gold] and [gold]Ethereal[/gold].\nWhenever you rest, you may transform it into [gold]Illusory Dream Whisper[/gold] or [gold]Ominous Dream Eternal[/gold].` |
| `...ENDLESS_NIGHTMARE.description` | `Deal {Damage:diff()} damage.\nAdd {Cards:diff()} random [gold]Attack[/gold] cards to your [gold]Draw Pile[/gold]. They have [gold]Exhaust[/gold] and [gold]Ethereal[/gold].\nWhenever you rest, you may transform it into [gold]Illusory Dream Whisper[/gold] or [gold]Deep Dream Oblivion[/gold].` |

### 备注
- 标题取「幻梦（Illusory Dream）+ 呢喃（Whisper）」直译；未使用 `Fantasy Dream`，避免与遗物「幻想之梦」`FANTASY_DREAM` 混淆。
- **三张标题统一为「X Dream YY」三段式**（对应中文「[X梦][XX]」结构）：`Illusory Dream Whisper` / `Deep Dream Oblivion` / `Ominous Dream Eternal`。`Whisper` 对「呢喃」；`Oblivion`（遗忘/幽冥）对「无觉」，不用 `Slumber` 以免与「沉睡」混淆；`Ominous` 对「噩」、`Eternal` 对「无终」，不用 `Nightmare`（那会把「噩+梦」合成一个词，破坏三段式）。
- 「每当你休息时，你可以将其变化为 A 或 B」逐字对应 `Whenever you rest, you may transform it into A or B`；牌名沿用各卡 `title` 译文并用 `[gold]` 包裹。
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
