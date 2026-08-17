---
name: translate
description: Translate TouhouAncients mod localization files from Chinese (zhs/) to English (eng/). Use when the user wants to translate enchantments, cards, relics, powers, potions, ancients, or other localization text.
---

# Translate — TouhouAncients 本地化英译

将 `TouhouAncients/localization/zhs/` 中的中文文本翻译为英文，输出到 `TouhouAncients/localization/eng/` 和 `TouhouAncients/localization/jpn/`（jpn 用英语填充）。

## 增量翻译规则

> **翻译基准线**：`4e46dfcf86c9909dd015d1fdf54d864a17ddd748`（2026-08-17 记录，`i18n: 更新本地化文本`）
> 此后调用 `/translate` 时，**只翻译自该基准提交之后新增或变更的本地化内容**。
>
> 操作步骤：
> 1. 运行 `git diff 4e46dfc -- TouhouAncients/localization/zhs/` 查看新增/变更的条目
> 2. 只处理这些新增或变更条目的翻译
> 3. 已有译文的条目不动

## 前置参考

0. **查阅术语表**：`docs/翻译术语表.md` — 所有已收录的术语、句式模板、专有名词英译，每次翻译前先查阅。
1. 先读取对应的 `zhs/` 源文件获取完整上下文
2. 参考 STS2 原版英文术语表：`d:\STS2Code\localization\eng\card_keywords.json`（关键字对照）、`d:\STS2Code\localization\eng\relics.json`（遗物描述范式）、`d:\STS2Code\localization\eng\enchantments.json`（附魔描述范式）、`d:\STS2Code\localization\eng\cards.json`（卡牌描述范式）、`d:\STS2Code\localization\eng\potions.json`（药水描述范式）、`d:\STS2Code\localization\eng\events.json`（事件描述范式）
3. 参考 `d:\STS2Code\localization\zhs\card_keywords.json` 与 `eng/` 对照确认术语映射

## 核心翻译规则

### 0. 黄金法则 — 始终使用 STS2 原版精确表述
**禁止自创句式。** 任何中文描述对应的英文，必须先在 `d:\STS2Code\localization\eng\` 中找到 STS2 原版中完全相同的功能/句式，直接复制其英文文本。
- 例如："恢复所有生命" → 查找原版事件中的 `[green]Heal to full HP.[/green]`，不可自创 `"Restore all HP."` 等变体
- "用随机药水填满药水栏位" → 查找原版药水 `ENTROPIC_BREW`，不可自创 `"Fill your empty potion slots with random potions."`
- "这张牌在本回合免费打出" → 查找原版药水 `ATTACK_POTION` / `LIQUID_MEMORIES` 中的 `It's free to play this turn.`，不可自创 `"This card costs 0 this turn."` 等变体

### 1. 富文本标记与动态变量 — 原样保留
所有 STS2 标记和变量**不得翻译、不得修改、不得遗漏**：
- 颜色标记：`[gold]`、`[purple]`、`[blue]`、`[red]`、`[green]`、`[pink]`、`[b]`
- 动画标记：`[jitter]`、`[sine]`、`[fade_in]`
- 动态变量：`{Damage}`、`{Block}`、`{Energy:energyIcons()}`、`{Amount:diff()}`、`{IfUpgraded:show:...|...}`、`{InCombat:...|}` 等
- 图标：`{singleStarIcon}`、`{energyPrefix:energyIcons(1)}` 等

### 2. 术语对照表（以 STS2 原版英文为准）

#### 2.1 卡牌 Keywords（官方 — `d:\STS2Code\localization\eng\card_keywords.json`）

| 中文（zhs） | English（eng） | 说明 |
|-------------|----------------|------|
| 消耗 | **Exhaust** | `[gold]Exhaust[/gold]` |
| 保留 | **Retain** | `[gold]Retain[/gold]` |
| 虚无 | **Ethereal** | `[gold]Ethereal[/gold]` |
| 永恒 | **Eternal** | `[gold]Eternal[/gold]` — 不能从牌组移除/变化 |
| 固有 | **Innate** | `[gold]Innate[/gold]` — 战斗开始到手牌 |
| 奇巧 | **Sly** | `[gold]Sly[/gold]` — 回合结束前丢弃则免费打出 |
| 不能被打出 | **Unplayable** | `[gold]Unplayable[/gold]` |
| 重放 | **Replay** | `[gold]Replay[/gold]` — 自定义关键字，参考 Glam/Spiral 附魔 |

#### 2.2 状态效果 / Powers（官方 — `d:\STS2Code\localization\eng\powers.json` 中 `XXX_POWER.title` 字段）

| 中文（zhs） | English（eng） |
|-------------|----------------|
| 易伤 | **Vulnerable** |
| 虚弱 | **Weak** |
| 脆弱 | **Frail** |
| 中毒 | **Poison** |
| 灾厄 | **Doom** |
| 人工制品 | **Artifact** |
| 缓冲 | **Buffer** |
| 力量 | **Strength** |
| 敏捷 | **Dexterity** |
| 集中 | **Focus** |
| 活力 | **Vigor** |
| 荆棘 | **Thorns** |
| 覆甲 | **Plating** |
| 无实体 | **Intangible** |
| 荒疫 | **Blight** |

#### 2.3 牌堆与位置

| 中文（zhs） | English（eng） |
|-------------|----------------|
| 牌组 | **Deck** |
| 手牌 | **Hand** |
| 抽牌堆 | **Draw Pile** |
| 弃牌堆 | **Discard Pile** |
| 消耗堆 | **Exhaust Pile** |
| 抽牌堆顶 | **top of your Draw Pile** |

#### 2.4 卡牌类型与操作

| 中文（zhs） | English（eng） |
|-------------|----------------|
| 攻击牌 | **Attack** |
| 技能牌 | **Skill** |
| 能力牌 | **Power** |
| 诅咒牌 | **Curse** |
| 状态牌 | **Status** |
| 打出 | **Play** |
| 消耗（动词/名词） | **Exhaust** |
| 抽（牌） | **Draw** |
| 弃（牌） | **Discard** |
| 保留（动词/名词） | **Retain** |
| 升级 | **Upgrade** |
| 铸造 | **Smith**（休息处升级操作） |
| 召唤 | **Summon** |
| 变化 | **Transform** |
| 添加 | **Add** |
| 移出 | **Remove** |
| 复制 / 复制品 | **Copy / copies** |
| 仆从 | **Minion** |

#### 2.5 战斗与回合

| 中文（zhs） | English（eng） |
|-------------|----------------|
| 战斗 | **Combat** |
| 回合 | **Turn** |
| 本回合 | **this turn** |
| 下回合 | **next turn** |
| 每场战斗 | **each combat / per combat** |
| 回合结束时 | **at the end of your turn** |
| 回合开始 | **at the start of your turn** |
| 抽牌阶段 | **Draw Phase** |
| 能量 | **Energy** |
| 费用 / 耗能 | **Cost** |
| 意图 | **intent** |
| 格挡 | **Block** |
| 伤害 | **Damage** |
| 额外（造成/获得） | **additional** |
| 层 / 层数 | **stack(s)**（取决于上下文） |

#### 2.6 通用游戏概念

| 中文（zhs） | English（eng） |
|-------------|----------------|
| 生命 | **HP** |
| 最大生命 | **Max HP** |
| 金币 | **Gold** |
| 药水 | **Potion** |
| 药水栏位 | **Potion Slot(s)** |
| 先古之民 | **Ancient** |
| 事件 | **Event** |
| 休息处 | **Rest Site** |
| 商店 | **Merchant**（Merchant Room） |
| 精英 | **Elite** |
| Boss | **Boss** |
| 附魔 | **Enchantment** |
| 充能 | **Energy**（星数系统的星能） |
| 随机 | **random** |
| 随机化 | **randomize** |
| 同名牌 | **card with the same name** |
| 预见 | **Scry** |

#### 2.7 动作句式参考（从 STS2 原版附魔/遗物/卡牌英文描述中提取）

| 句式（中文） | 句式（英文） | 来源参考 |
|-------------|-------------|---------|
| 获得[N]点[gold]格挡[/gold] | Gain [blue]{N}[/blue] [gold]Block[/gold]. | Nimble / Block Potion |
| 造成[N]点（额外）伤害 | Deal [blue]{N}[/blue] (additional) damage. | Sharp / Vigorous |
| 造成[N]层[gold]中毒[/gold] | Apply [blue]{N}[/blue] [gold]Poison[/gold]. | — |
| 对所有敌人 | to ALL enemies | Inky / Bag of Marbles |
| 这张牌获得[gold]保留[/gold] | This card gains [gold]Retain[/gold]. | Steady |
| 回合结束时，如果这张牌在你的手牌中 | If this card is in your hand at the end of turn | Slumbering Essence |
| 每场战斗第一次打出 | The first time you play this card each combat | Sown / Swift |
| 费用为[N] | Costs [blue]{N}[/blue] | Tezcatara's Ember |
| 给一张随机牌添加[red]关键词[/red] | Gain [red]Keyword[/red] on a random card. | Closed Eye（禁闭之眼） — 自定义关键词句式 |
| 为一张随机攻击牌附魔[purple]附魔名[/purple] | [gold]Enchant[/gold] a random Attack with [purple]EnchantmentName[/purple]. | Closed Eye（禁闭之眼） — 引用附魔句式 |
| 获得{Amount:energyIcons()} | Gain {Amount:energyIcons()}. | Sown |
| 抽[N]张牌 | Draw [blue]{N}[/blue] {N:plural:card\|cards}. | Swift |
| 在本场战斗中 | this combat | Momentum |
| 从牌组中移除 | Remove from your [gold]Deck[/gold]. | Soul's Power |
| 被复制 | be duplicated | Clone |
| 失去[N]点生命 | lose [blue]{N}[/blue] HP | Corrupted |
| 回复[N]点生命 | Heal for [blue]{N}[/blue] HP. | Blood Potion |
| 恢复所有生命 | [green]Heal to full HP.[/green] | ROUND_TEA_PARTY / UNREST_SITE（事件） |
| 用随机药水填满药水栏位 | Fill all your empty potion slots with random potions. | ENTROPIC_BREW |
| 这张牌在本回合免费打出 | It's free to play this turn. | ATTACK_POTION / LIQUID_MEMORIES |
| 将一张随机XX卡牌加入到手牌，这张牌在本回合免费打出 | Add a random XX card into your [gold]Hand[/gold]. It's free to play this turn. | ATTACK_POTION |
| 将一张[牌堆]的牌加入到手牌 | Put a card from your [gold]Pile[/gold] into your [gold]Hand[/gold]. | LIQUID_MEMORIES / DROPLET_OF_PRECOGNITION |
| 任意张 | any of them | Scry（预见） |
| 消耗一层充能无视当前的路线选择下一层的房间 | You may ignore paths when choosing the next rooms to travel to, consuming [blue]1[/blue] charge per use. | STARDUST_BROOM（星尘扫帚） |
| 当[单位]的生命值将要降低至[blue]0[/blue] | When [unit]'s HP would be reduced to [blue]0[/blue] | LIZARD_TAIL（蜥蜴尾巴） |

#### 2.8 附魔专用字段格式（参考原版 `d:\STS2Code\localization\eng\enchantments.json`）

| 字段 | 句式惯例 |
|------|---------|
| `.description` | This card ... / Gains ... / When played, ... / Increases ... / The first time ... |
| `.title` | 单个形容词或名词短语，首字母大写 |
| `.extraCardText` | 卡面额外简短文本，祈使句不加定冠词，如 `"Gain X Block."` |
| `.dialogue` | 角色台词，如 `"Power..."` |

### 2.9 卡牌描述参考源（翻译具体条目时对照的 STS2 原版卡牌/附魔）

> 翻译时遇到类似句式，优先返回这些源文件查找对应英文描述，确保与 STS2 标准一致。

| 参考源（英文键名） | 中文名 | 英文描述 | 用于参照 |
|-------------------|--------|---------|---------|
| `SLUMBERING_ESSENCE` | 沉眠精华 | `If this card is in your hand at the end of turn, reduce its cost by [blue]1[/blue] until it is played.` | 蛇！！前半段：回合结束手牌降费 |
| `HEAVENLY_DRILL` | 天际钻头 | `Deal {Damage:diff()} damage X times.\nDouble X if it's {Energy:diff()} or more.` | 极限火花：X费+翻倍条件句式 |
| `TRANSFIGURE` | 重构 | `Add [gold]Replay[/gold] to a card in your [gold]Hand[/gold].\nIt costs an extra {Energy:energyIcons()}.` | 添加重放句式 |
| `GUILTY` | 愧疚 | `Removed from your [gold]Deck[/gold] after {Combats:diff()} {Combats:plural:combat\|combats}.` | 从牌组移除句式 |
| `GLAM` (附魔) | 华彩 | `This card has [gold]Replay[/gold] once per combat.` | 重放关键词句式 |
| `SPIRAL` (附魔) | 涡旋 | `This card gains [gold]Replay[/gold] [blue]1[/blue].` | 重放+次数句式 |
| `SOWN` (附魔) | 播种 | `The first time you play this card each combat, gain {Amount:energyIcons()}.` | 每战首次打出句式 |
| `SWIFT` (附魔) | 迅速 | `The first time you play this card, draw [blue]{Amount}[/blue] {Amount:plural:card\|cards}.` | 首次打出+抽牌句式 |
| `NIMBLE` (附魔) | 灵巧 | `Increases [gold]Block[/gold] gained from this card by [blue]{Amount}[/blue].` | 格挡递增句式 |
| `GOOPY` (附魔) | 黏糊 | `This card gains [gold]Exhaust[/gold]. When played, permanently increase this card's [gold]Block[/gold] by [blue]1[/blue].` | 获得消耗句式 |
| `ROYALLY_APPROVED` (附魔) | 王室认证 | `This card has [gold]Innate[/gold] and [gold]Retain[/gold].` | 同时拥有多个关键字句式 |
| `SHARP` (附魔) | 锋利 | `Increases damage on this card by [blue]{Amount}[/blue].` | 伤害递增句式 |

### 2.10 自定义关键词翻译

| 中文（zhs） | English（eng） | 备注 |
|-------------|----------------|------|
| 无意识 | **Unconscious** | 古明地恋相关，禁闭之眼使用。句式：`Gain [red]Unconscious[/red] on a random card.` |
| 污秽 | **Filth** | 绀珠之药计数机制。句式：`gain [pink]Filth[/pink] count.` |
| 辉星 | **Stars** | STS2 费用系统专有名词 |

### 3. Touhou 特色台词 — 英文意译
- 保留台词风味，但不逐字直译日文梗
- 用自然英语传达角色语气（如 Marisa 的 `Da☆Ze` 保留）
- 格式标记如 `[purple][sine]...[/sine][/purple]` 原样保留

### 4. 格式规则
- 保持 JSON 结构与源文件完全一致
- 键名不变，仅翻译值
- 换行符 `\n` 保留
- 标点：中文句号 `。` → 英文句号 `.`，中文逗号 `、` → `,`

### 5. ⚠️ 跨文件术语一致 — 以中文文本为准，键名仅作参考

> **🚨 这是最常犯的错误，必须牢记！🚨**
> **翻译标题时，必须以 `zhs/` 里的实际中文文本为准，键名/类名仅可用于参考。**

- **✅ 正确做法：以 `zhs/` 中的实际中文文本为准进行翻译**。中文写的是什么，就翻译什么。键名可用于辅助理解中文含义。
- **❌ 错误做法：根据键名/类名直接翻译标题**。键名（如 `MYSTIC_FORTUNE_PEACH`）可用于参考线索，但**最终译名必须从中文字面出发**。
- **📋 示例：**
  - 中文 `"天馔仙桃"` → 译为 **"Celestial Peach"**，❌ 而非键名直译的 `"Mystic Fortune Peach"`（键名仅供参考，不能代替中文文本）
  - 中文 `"守矢御币"` → 译为 **"Moriya Gohei"**，❌ 而非键名 `MORIYA_GOHEI` 的拆字翻译
  - 中文 `"赏樱"` → 译为 **"Hanami"**，❌ 而非键名 `CHERRYBLOSSOMS` 的 `"Cherry Blossoms"`
- **同一中文名跨文件出现时必须保持一致**。例如 `"墨染的樱花"` 同时出现在 `relics.json`、`card_keywords.json`、`rest_site_ui.json` 中，英文翻译必须统一。
- 翻译新文件前，先在已翻译的 `eng/` 文件中搜索同一中文名，确保复用已有译名。

### 6. 遗物描述中的卡牌名 — 必须与 cards.json 一致
- 遗物描述中引用的卡牌名称（`[gold]卡牌名[/gold]`），必须使用 `cards.json` 中对应键的英译名称，**不得自行翻译**。
- 翻译遗物前，先在 `zhs/cards.json` 中搜索中文卡牌名找到对应键，再查 `eng/cards.json` 获取已确定的英译名。
- 例如：遗物中引用 `[gold]开海的奇迹[/gold]`，查 `cards.json` 得 `"Miracle of the Sea Opening"`，则遗物中写 `[gold]Miracle of the Sea Opening[/gold]`。

### 7. 🌏 人名顺序 — 名前姓后（西方顺序）
- 所有角色人名在英文中使用 **西方顺序（Given Name + Family Name）**，即名前姓后。
- **✅ 正确**：`Tenshi Hinanawi`、`Tewi Inaba`、`Reimu Hakurei`、`Marisa Kirisame`
- **❌ 错误**：`Hinanawi Tenshi`、`Inaba Tewi`、`Hakurei Reimu`
- 此规则适用于所有文本中的署名（flavor、台词、签名等），无论中文原文是姓前名后还是名前姓后。

### 8. 📝 专有名词即时入表
- **用户明确指出"这是专有名词"的内容，必须立即追加到 `docs/翻译术语表.md`**，不得遗漏。
- 例如：`马萨雷斯 → Mazaleth`、`咔咔 → Caaaaw`、`我饥饿…… → I Hunger……`
- 新增条目放在术语表末节"十二、专有名词"中，注明中文原文和英文译名。
- 此规则优先级高于其他翻译规则：专有名词即使看似是普通词汇，一旦用户指定即为专有名词。

### 9. 🚫 未入翻译记录的文件须先经确认
- **`docs/relic翻译.md`、`docs/ancients翻译.md` 等翻译记录文件未收录的条目，不得擅自翻译**。
- 例如：`relic翻译.md` 中未记录的遗物条目（如渡里贝子的遗物），必须先通过 plan 模式向用户展示翻译方案，获得确认后才能实施。
- 此规则适用于所有未出现在翻译记录文件中的条目，无论其来源如何。

## 翻译流程

### 按文件优先级顺序翻译

1. `enchantments.json` — 附魔文本（条目少，术语密集）
2. `cards.json` — 卡牌文本（条目多，含台词）
3. `powers.json` — Power 文本
4. `relics.json` — 遗物文本（条目最多）
5. `potions.json` — 药水文本
6. `ancients.json` — 先古之民对话
7. `rest_site_ui.json` / `settings_ui.json` — UI 文本
8. `card_keywords.json` — 自定义关键字说明
9. `card_reward_ui.json` — 卡牌奖励 UI

### 单文件翻译操作

1. 读取 `zhs/{filename}` 获取全部待翻译条目
2. 读取 `eng/{filename}` 查看当前已翻译状态
3. 针对每个未翻译的条目（值仍为中文的），逐条生成英文翻译
4. 使用 `replace_string_in_file` 或 `multi_replace_string_in_file` 替换为英文值
5. **同步到 jpn**：将 eng 中的相同内容同步写入 `jpn/{filename}`（jpn 使用英语，与 eng 完全一致）
6. **更新翻译记录**：在对应的翻译记录文档（如 `docs/relic翻译.md`、`docs/ancients翻译.md`）中记录翻译结果
7. 翻译完成后告知用户翻译了哪些条目

### 术语一致性检查
- 同一中文术语在所有文件中必须翻译为同一个英文词
- 遇到新术语时，优先查找 STS2 原版 `eng/` 中的用法
- 若 STS2 原版无对应术语，在翻译后告知用户新增术语的翻译选择

## 常用操作

```powershell
# 对比中英文文件差异（找出未翻译条目）
code --diff TouhouAncients/localization/zhs/cards.json TouhouAncients/localization/eng/cards.json
```
