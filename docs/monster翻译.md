# monsters.json / encounters.json / cards.json / powers.json 翻译记录（怪物相关）

> 翻译依据：`zhs/*.json` → `eng/*.json`（jpn 用英语填充，与 eng 一致）
> 规则参考：`.agents/skills/translate/SKILL.md`、`docs/翻译术语表.md`

---

## 博丽灵梦（怪物 + 挑战遭遇 + 状态卡牌 + 固有能力）

### 怪物：`TOUHOUANCIENTS-HAKUREI_REIMU_MONSTER`

#### 中文原文
```json
"TOUHOUANCIENTS-HAKUREI_REIMU_MONSTER.name": "博丽灵梦",
"TOUHOUANCIENTS-HAKUREI_REIMU_MONSTER.moves.DREAM_SEAL.title": "梦想封印",
"TOUHOUANCIENTS-HAKUREI_REIMU_MONSTER.moves.OCTAGONAL_BINDING_ARRAY.title": "八方鬼缚阵",
"TOUHOUANCIENTS-HAKUREI_REIMU_MONSTER.moves.SEALING_NEEDLE.title": "封魔针",
"TOUHOUANCIENTS-HAKUREI_REIMU_MONSTER.moves.TENBU_HURRICANE_KICK.title": "阴阳玉",
"TOUHOUANCIENTS-HAKUREI_REIMU_MONSTER.moves.SUBSPACE_ACUPRESSURE.title": "亚空点穴",
"TOUHOUANCIENTS-HAKUREI_REIMU_MONSTER.moves.FANTASY_NATURE.title": "梦想天生"
```

#### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.name` | `Reimu Hakurei` |
| `.moves.DREAM_SEAL.title` | `Dream Seal` |
| `.moves.OCTAGONAL_BINDING_ARRAY.title` | `Octagonal Binding Array` |
| `.moves.SEALING_NEEDLE.title` | `Sealing Needle` |
| `.moves.TENBU_HURRICANE_KICK.title` | `Yin-Yang Orb` |
| `.moves.SUBSPACE_ACUPRESSURE.title` | `Subspace Acupressure` |
| `.moves.FANTASY_NATURE.title` | `Fantasy Nature` |

### 遭遇：`TOUHOUANCIENTS-HAKUREI_REIMU_ENCOUNTER`

#### 中文原文
```json
"TOUHOUANCIENTS-HAKUREI_REIMU_ENCOUNTER.title": "博丽灵梦",
"TOUHOUANCIENTS-HAKUREI_REIMU_ENCOUNTER.loss": "即便没有符卡规则，[gold]{encounter}[/gold]的实力依然不容置喙。"
```

#### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Reimu Hakurei` |
| `.loss` | `Even without the spell card rules, the strength of [gold]{encounter}[/gold] is beyond question.` |

### 状态卡牌：`TOUHOUANCIENTS-DREAM_SEAL_WABI` / `TOUHOUANCIENTS-DREAM_SEAL_SABI`

#### 中文原文
```json
"TOUHOUANCIENTS-DREAM_SEAL_WABI.title": "梦想封印·侘",
"TOUHOUANCIENTS-DREAM_SEAL_WABI.description": "获得[blue]1[/blue]层[gold]易伤[/gold]。如果这张牌在你的手中，你不能打出技能牌。",
"TOUHOUANCIENTS-DREAM_SEAL_WABI.descriptionUpgraded": "获得[blue]{Amount:diff()}[/blue]层[gold]易伤[/gold]。如果这张牌在你的手中，你不能打出技能牌。",
"TOUHOUANCIENTS-DREAM_SEAL_SABI.title": "梦想封印·寂",
"TOUHOUANCIENTS-DREAM_SEAL_SABI.description": "获得[blue]1[/blue]层[gold]虚弱[/gold]。如果这张牌在你的手中，你不能打出攻击牌。",
"TOUHOUANCIENTS-DREAM_SEAL_SABI.descriptionUpgraded": "获得[blue]{Amount:diff()}[/blue]层[gold]虚弱[/gold]。如果这张牌在你的手中，你不能打出攻击牌。"
```

#### 英文翻译
| 字段 | 翻译 |
|------|------|
| `DREAM_SEAL_WABI.title` | `Dream Seal: Wabi` |
| `DREAM_SEAL_WABI.description` | `Apply [blue]1[/blue] [gold]Vulnerable[/gold]. While this card is in your [gold]Hand[/gold], you cannot play Skill cards.` |
| `DREAM_SEAL_WABI.descriptionUpgraded` | `Apply [blue]{Amount:diff()}[/blue] [gold]Vulnerable[/gold]. While this card is in your [gold]Hand[/gold], you cannot play Skill cards.` |
| `DREAM_SEAL_SABI.title` | `Dream Seal: Sabi` |
| `DREAM_SEAL_SABI.description` | `Apply [blue]1[/blue] [gold]Weak[/gold]. While this card is in your [gold]Hand[/gold], you cannot play Attack cards.` |
| `DREAM_SEAL_SABI.descriptionUpgraded` | `Apply [blue]{Amount:diff()}[/blue] [gold]Weak[/gold]. While this card is in your [gold]Hand[/gold], you cannot play Attack cards.` |

### 固有能力：`TOUHOUANCIENTS-INDISCRIMINATE_SUBJUGATION_POWER`

#### 中文原文
```json
"TOUHOUANCIENTS-INDISCRIMINATE_SUBJUGATION_POWER.title": "无差别降伏",
"TOUHOUANCIENTS-INDISCRIMINATE_SUBJUGATION_POWER.description": "每使用梦想天生以外的技能造成7次未被格挡的伤害，下一次意图切换至梦想天生。",
"TOUHOUANCIENTS-INDISCRIMINATE_SUBJUGATION_POWER.smartDescription": "每使用梦想天生以外的技能造成[blue]{Amount}[/blue]次未被格挡的伤害，下一次意图切换至[gold]梦想天生[/gold]。"
```

#### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Indiscriminate Subjugation` |
| `.description` | `After dealing [blue]7[/blue] instances of unblocked damage with any skill other than Fantasy Nature, switch the next intent to Fantasy Nature.` |
| `.smartDescription` | `After dealing [blue]{Amount}[/blue] instances of unblocked damage with any skill other than Fantasy Nature, switch the next intent to [gold]Fantasy Nature[/gold].` |

### Neta / 文化梗说明
- **「梦想天生」（Fantasy Nature）** — 东方 Project 灵梦的经典符卡，此处使用官方英译。
- **「梦想封印」（Dream Seal）** — 灵梦的基础符卡，术语表中已存在（cards.json），保持一致。
- **「封魔针」（Sealing Needle）** — 与灵梦遗物「封魔针」译名一致。
- **「无差别降伏」** — 灵梦的固有能力名，取自其战斗风格"不分敌我地退治"。
- **侘/寂（Wabi/Sabi）** — 取自日本美学概念"侘寂"，保留日语罗马音作为英文标题。
- **「倒映」「残影」「翱翔」「无实体」** — 使用 STS2 原版 Power 译名（Reflect / Blur / Soar / Intangible）。

---

## 增量更新（2026-08-06，基于提交 c188fbc 之后）

### 依神姐妹怪物键名变更

> `TOUHOUANCIENTS-YORIGAMI_JOON.*` / `TOUHOUANCIENTS-YORIGAMI_SHION.*` → 加 `_MONSTER` 后缀（`YORIGAMI_JOON_MONSTER.*`、`YORIGAMI_SHION_MONSTER.*`），英文值不变。

### 雾雨魔理沙怪物：补全

| 键 | 中文 | English |
|------|------|---------|
| `KIRISAME_MARISA_MONSTER.name` | 雾雨魔理沙 | Marisa Kirisame |
| `KIRISAME_MARISA_MONSTER.moves.ESCAPE_VELOCITY.title` | 逃逸速度 | Escape Velocity |
| `KIRISAME_MARISA_MONSTER.moves.STELLAR_FANTASY.title` | 星尘幻想 | Stellar Fantasy |
| `KIRISAME_MARISA_MONSTER.moves.BLACK_HOLE_EDGE.title` | 黑洞边缘 | Black Hole Edge |
| `KIRISAME_MARISA_MONSTER.moves.FUNGUS_EXPERT.title` | 菌菇培育 | Mushroom Cultivation |
| `KIRISAME_MARISA_MONSTER.moves.FUNGUS_EXPERT.banter` | 蘑菇是怎么也吃不腻的！ | I never get tired of mushrooms! |
| `KIRISAME_MARISA_MONSTER.moves.MASTER_SPARK_CHARGE.title` | 极限火花·蓄力 | Master Spark: Charge |
| `KIRISAME_MARISA_MONSTER.moves.MASTER_SPARK_CHARGE.banter` | [gold]嘿嘿，[jitter]要来喽~[/jitter][/gold] | [gold]Hehe, [jitter]here it comes~[/jitter][/gold] |
| `KIRISAME_MARISA_MONSTER.moves.MASTER_SPARK.title` | 极限火花 | Master Spark |

### 奇幻蘑菇：键名变更 + 补全

| 键 | 中文 | English |
|------|------|---------|
| `FANTASY_MUSHROOM_MONSTER.name` | 奇幻蘑菇 | Fantasy Mushroom |
| `FANTASY_MUSHROOM_MONSTER.moves.HEAL.title` | 生长 | Grow |

### 遭遇补全（encounters.json）

| 键 | title | loss |
|------|-------|------|
| KIRISAME_MARISA_ENCOUNTER | Marisa Kirisame | {character} fell before the dazzling, overwhelming firepower of [gold]{encounter}[/gold]. |
| WATARI_NINA_ENCOUNTER | Nina Watari | {character} lost the will to fight amid the conspiracy theories touted by [gold]{encounter}[/gold]. |
| KOTIYA_SANAE_ENCOUNTER | Sanae Kochiya | {character} failed to bring about a miracle great enough to overcome [aqua]the living god[/aqua] [gold]{encounter}[/gold]. |
| REMILIA_SCARLET_ENCOUNTER | Remilia Scarlet | {character}'s fate came to an end beneath the majesty of [gold]{encounter}[/gold]. |
| KOMEJI_SATORI_ENCOUNTER | Satori Komeji | [gold]{encounter}[/gold] saw through every intention of {character}. |
| HINANAWI_TENSHI_ENCOUNTER | Tenshi Hinanawi | {character} became yet another defeated rival of [gold]{encounter}[/gold]. |
| INABA_TEWI_ENCOUNTER | Tewi Inaba | {character} stepped right into a trap long prepared by [gold]{encounter}[/gold]. |
| KIJIN_SEIJA_ENCOUNTER | Seija Kijin | [gold]{encounter}[/gold] flipped the scales tipping toward {character}'s victory. |
| MEDICINE_MELANCHOLY_ENCOUNTER | Medicine Melancholy | {character} fell into an eternal slumber within [gold]{encounter}[/gold]'s sea of lily of the valley. |
| HOURAISAN_KAGUYA_ENCOUNTER | Kaguya Houraisan | {character}'s journey came to an end beneath the allure of [gold]{encounter}[/gold]. |
| SAIGYOUJI_YUYUKO_ENCOUNTER | Yuyuko Saigyouji | [gold]{encounter}[/gold] brought [purple]death[/purple] itself to {character}. |
| JUNKO_ENCOUNTER | Junko | {character} submitted to the absolute power of [gold]{encounter}[/gold]. |
| TOUTETSU_YUUMA_ENCOUNTER | Yuuma Toutetsu | [gold]{encounter}[/gold] melted {character} into the sea of oil as just another piece of organic matter. |
| FUTATSUIWA_MAMIZOU_ENCOUNTER | Mamizou Futatsuiwa (Test) | {character} was spun dizzy by the tricks of [gold]{encounter}[/gold] and her underlings. |

### 能力更新（powers.json）

| 键 | 变更 |
|------|------|
| UNFORTUNATE_POWER | 数值 5/10 → 6/6，`{Amount2}` → `{Amount}`（zhs 同改） |
| MAGICIAN_POWER（新增） | 普通的魔法使 → Ordinary Magician：When preparing to fire [gold][jitter]Master Spark[/jitter][/gold], each mushroom on the field grants Marisa [blue]10[/blue] [gold]Vigor[/gold].\n[gold]The most important thing in danmaku is firepower [sine]Da☆Ze[/sine]![/gold] |
| I_WILL_RETURN_IT_POWER（新增） | 我会还给你的！→ I'll Return It!：When preparing to fire [gold][jitter]Master Spark[/jitter][/gold], return the stolen cards.\n"Heheh, just take them once I'm dead and gone." |
| FUNGAL_POWER（改名） | 菌类 → 共生 → Symbiosis：On death, add [blue]3[/blue] [gold]Spore Mind[/gold]s into the player's [gold]Discard Pile[/gold].（孢子心灵 → Spore Mind，与 MUSHROOM_BENTO 遗物一致） |

### 怪物图鉴"先古之民"分区标题（新增）

| 键 | 中文 | English |
|------|------|---------|
| `ANCIENT_SECTION.title` | 先古之民 | Ancients |

---

## 增量更新（2026-08-07，基于提交 6414515d 之后）

### 雾雨魔理沙怪物：新增技能台词

| 键 | 中文 | English |
|------|------|---------|
| `KIRISAME_MARISA_MONSTER.moves.ESCAPE_VELOCITY.banter` | 我会还给你的！ | I'll return it! |

### 能力更新（powers.json）

| 键 | 变更 |
|------|------|
| `I_WILL_RETURN_IT_POWER.title` | 我会还给你的！→ **魔理沙偷走了重要的东西** → Marisa Stole the Precious Thing（ZUN 名曲英文名） |
| `INDISCRIMINATE_SUBJUGATION_POWER.smartDescription` | `{Amount}` → `{HitsLeft}`（zhs 同改，仅变量名替换） |

### 状态卡牌更新（cards.json）

| 键 | 变更 |
|------|------|
| `DREAM_SEAL_WABI.description` | `[blue]1[/blue]` → `[blue]{Amount}[/blue]`；删除 `descriptionUpgraded` |
| `DREAM_SEAL_SABI.description` | `[blue]1[/blue]` → `[blue]{Amount}[/blue]`；删除 `descriptionUpgraded` |