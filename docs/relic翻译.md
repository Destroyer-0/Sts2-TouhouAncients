# relics.json 翻译记录

> 翻译依据：`zhs/relics.json` → `eng/relics.json`
> 规则参考：`.agents/skills/translate/SKILL.md`、`docs/翻译术语表.md`

---

## 展望未来的X药

**键名**: `TOUHOUANCIENTS-ZHANGEWEILAIBA`

### 中文原文
```json
"TOUHOUANCIENTS-ZHANGEWEILAIBA.title": "展望未来的X药",
"TOUHOUANCIENTS-ZHANGEWEILAIBA.description": "拾起时，从你的角色牌中选择一张耗能为[blue]X[/blue]的牌加入牌组。耗能为[blue]X[/blue]的牌的效果数值增加[blue]{Increase}[/blue]点。",
"TOUHOUANCIENTS-ZHANGEWEILAIBA.eventDescription": "从你的角色牌中选择一张耗能为[blue]X[/blue]的牌加入牌组。耗能为[blue]X[/blue]的牌的效果数值增加[blue]{Increase}[/blue]点。",
"TOUHOUANCIENTS-ZHANGEWEILAIBA.flavor": "战什么未来？现在就要！",
"TOUHOUANCIENTS-ZHANGEWEILAIBA.selectionScreenPrompt": "选择一张牌"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Foresight X` |
| `.description` | `Upon pickup, choose an [blue]X[/blue]-cost card from your character pool and add it to your [gold]Deck[/gold]. The effects of your cost [blue]X[/blue] cards are increased by [blue]{Increase}[/blue].` |
| `.eventDescription` | `Choose an [blue]X[/blue]-cost card from your character pool and add it to your [gold]Deck[/gold]. The effects of your cost [blue]X[/blue] cards are increased by [blue]{Increase}[/blue].` |
| `.flavor` | `Who needs foresight? I want it now!` |
| `.selectionScreenPrompt` | `Choose a card` |

### Neta / 文化梗说明
- **「展望未来」** — 来源于中国某位杀戮尖塔主播 A。他在直播前已提前玩过某个种子，知道后续能拿到"旋风斩"，于是在第一个商店就购买了"化学物 X"并声称"战个未来吧"
- **flavor「战什么未来？现在就要！」** — 反向调侃该主播的"未来"理论
- 英文标题 `Foresight X` 保留了"未来"（Foresight）和"X 费用"的双关

---

## 重峦叠嶂的壁垒

**键名**: `TOUHOUANCIENTS-BILEIHAOPAIDUOZHUA`

### 中文原文
```json
"TOUHOUANCIENTS-BILEIHAOPAIDUOZHUA.title": "重峦叠嶂的壁垒",
"TOUHOUANCIENTS-BILEIHAOPAIDUOZHUA.description": "拾起时，将[blue]3[/blue]张带有[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]的[gold]壁垒+[/gold]加入你的牌组。每当你将一张[gold]已有同名牌存在于牌组中[/gold]的卡牌加入牌组时，为那张牌[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-BILEIHAOPAIDUOZHUA.eventDescription": "将[blue]3[/blue]张带有[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]的[gold]壁垒+[/gold]加入你的牌组。每当你将一张[gold]已有同名牌存在于牌组中[/gold]的卡牌加入牌组时，为那张牌[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-BILEIHAOPAIDUOZHUA.flavor": "壁垒好牌多抓"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Towering Barricade` |
| `.description` | `Upon pickup, add [blue]3[/blue] [gold]Barricade+[/gold] cards [gold]enchanted[/gold] with [purple]{EnchantmentName}[/purple] to your [gold]Deck[/gold]. Whenever you add a card to your [gold]Deck[/gold] that already shares a name with a card in it, [gold]enchant[/gold] that card with [purple]{EnchantmentName}[/purple].` |
| `.eventDescription` | `Add [blue]3[/blue] [gold]Barricade+[/gold] cards [gold]enchanted[/gold] with [purple]{EnchantmentName}[/purple] to your [gold]Deck[/gold]. Whenever you add a card to your [gold]Deck[/gold] that already shares a name with a card in it, [gold]enchant[/gold] that card with [purple]{EnchantmentName}[/purple].` |
| `.flavor` | `Barricade is a great card. Pick some more!` |

### Neta / 文化梗说明
- **「壁垒好牌多抓」** — 来源于中国某位杀戮尖塔主播 B，以节目效果著称。他喜欢同名卡拿很多张，包括壁垒这种重复打出没有任何效果的牌
- 英文 flavor `"Barricade is a great card. Pick some more!"` 模仿该主播的语气

---

## 白白胖胖的海兽

**键名**: `TOUHOUANCIENTS-BAIBAIXIANGXIANGRUANRUAN`

### 中文原文
```json
"TOUHOUANCIENTS-BAIBAIXIANGXIANGRUANRUAN.title": "白白胖胖的海兽",
"TOUHOUANCIENTS-BAIBAIXIANGXIANGRUANRUAN.description": "拾起时，删除[blue]4[/blue]张牌，然后复制你牌组中的防御并升级。",
"TOUHOUANCIENTS-BAIBAIXIANGXIANGRUANRUAN.eventDescription": "删除[blue]4[/blue]张牌，然后复制你牌组中的防御并升级。",
"TOUHOUANCIENTS-BAIBAIXIANGXIANGRUANRUAN.flavor": "删牌大于一切啊！"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Chubby White Seal` |
| `.description` | `Upon pickup, remove [blue]4[/blue] cards from your [gold]Deck[/gold], then copy all Defends in your [gold]Deck[/gold] and [green]Upgrade[/green] them.` |
| `.eventDescription` | `Remove [blue]4[/blue] cards from your [gold]Deck[/gold], then copy all Defends in your [gold]Deck[/gold] and [green]Upgrade[/green] them.` |
| `.flavor` | `Card removal above all!` |

### Neta / 文化梗说明
- **「删牌大于一切啊！」** — 来源于中国某位杀戮尖塔主播 C。某次他本想删除一张防御，却不小心把"复制"看成了"删除"，结果复制了一张本应被删掉的防御
- 英文 flavor `"Card removal above all!"` 强调删牌的重要性

---

## 萎靡心灵的魔咒 → 枯萎的 Snake Plant

**键名**: `TOUHOUANCIENTS-GEISHEHUAXIAOJIE`

### 中文原文
```json
"TOUHOUANCIENTS-GEISHEHUAXIAOJIE.title": "萎靡心灵的魔咒",
"TOUHOUANCIENTS-GEISHEHUAXIAOJIE.description": "在每场战斗开始时，所有敌人减少[blue]{StrengthLose}[/blue]点[gold]力量[/gold]，你每打出一张牌，所有敌人增加[blue]1[/blue]点[gold]力量[/gold]，每个敌人最多增加[blue]{StrengthLose}[/blue]次。",
"TOUHOUANCIENTS-GEISHEHUAXIAOJIE.flavor": "给XX小姐减少Y点力量"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Withered Snake Plant` |
| `.description` | `At the start of each combat, ALL enemies lose [blue]{StrengthLose}[/blue] [gold]Strength[/gold]. Whenever you play a card, ALL enemies gain [blue]1[/blue] [gold]Strength[/gold], up to [blue]{StrengthLose}[/blue] times per enemy.` |
| `.flavor` | `Let's reduce Miss Snake Plant's Strength a bit, shall we?` |

### Neta / 文化梗说明
- **Snake Plant（蛇花）** — 尖塔中一种敌人。如果能减少它 8 点力量，它将完全无法造成任何伤害
- **flavor「给 Snake Plant 小姐减点力量」** — 调侃这种针对性的战术
- 英文 flavor 保留了"Miss Snake Plant"的拟人化语气

---

## 涤荡腐坏的炽焰

**键名**: `TOUHOUANCIENTS-HUOYANTUXI`

### 中文原文
```json
"TOUHOUANCIENTS-HUOYANTUXI.title": "涤荡腐坏的炽焰",
"TOUHOUANCIENTS-HUOYANTUXI.description": "受到攻击时，对攻击者造成[blue]{FlameBarrierPower}[/blue]点伤害。",
"TOUHOUANCIENTS-HUOYANTUXI.flavor": "噶人们，火焰屏障伤害特别高"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Purifying Flame` |
| `.description` | `When attacked, deal [blue]{FlameBarrierPower}[/blue] damage to the attacker.` |
| `.flavor` | `Guys, that Flame Barrier damage is insane!` |

### Neta / 文化梗说明
- **火焰屏障（Flame Barrier）** — 来源于中国杀戮尖塔主播 B，某次他只靠火焰屏障这张牌的伤害就击杀了心脏（Heart）
- **flavor「噶人们，火焰屏障伤害特别高」** — 引用该主播的直播台词

---

## 吞天噬地的毒牙

**键名**: `TOUHOUANCIENTS-SHEYAOTEBIEQIANG`

### 中文原文
```json
"TOUHOUANCIENTS-SHEYAOTEBIEQIANG.title": "吞天噬地的毒牙",
"TOUHOUANCIENTS-SHEYAOTEBIEQIANG.description": "拾起时，将[blue]1[/blue]张[gold]蛇咬[/gold]加入牌组。为你所有牌组中与之后获得的名字中带有"蛇"的牌[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-SHEYAOTEBIEQIANG.eventDescription": "将[blue]1[/blue]张[gold]蛇咬[/gold]加入牌组。为你所有牌组中与之后获得的名字中带有"蛇"的牌[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-SHEYAOTEBIEQIANG.flavor": "蛇咬不愧是尖塔二第一神卡",
"TOUHOUANCIENTS-SHEYAOTEBIEQIANG.filterKeywords": "蛇"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Sky-Devouring Snecko Fang` |
| `.description` | `Upon pickup, add [blue]1[/blue] [gold]SnakeBite[/gold] to your [gold]Deck[/gold]. [gold]Enchant[/gold] all cards in your [gold]Deck[/gold] and future cards with "Snecko" in their name with [purple]{EnchantmentName}[/purple].` |
| `.eventDescription` | `Add [blue]1[/blue] [gold]SnakeBite[/gold] to your [gold]Deck[/gold]. [gold]Enchant[/gold] all cards in your [gold]Deck[/gold] and future cards with "Snecko" in their name with [purple]{EnchantmentName}[/purple].` |
| `.flavor` | `SnakeBite — the greatest card in Spire 2, no contest!` |
| `.filterKeywords` | `Snecko` |

### Neta / 文化梗说明
- **SnakeBite meme** — 来源于 SnakeBite（蛇咬）这张卡牌的强度梗，玩家社区公认其为 STS2 最强卡之一
- **「吞天噬地」** — 夸张描述 SnakeBite 的强度
- 英文 flavor `"SnakeBite — the greatest card in Sts2, no contest!"` 直接引用社区 meme

### beizhu
- **Snecko**: STS 官方术语，异蛇种族
- **SnakeBite**: 卡牌"蛇咬"的英文名，保留原卡名

---

## 异眼顶真的蛇瞳

**键名**: `TOUHOUANCIENTS-YIYANDINGZHEN`

### 中文原文
```json
"TOUHOUANCIENTS-YIYANDINGZHEN.title": "异眼顶真的蛇瞳",
"TOUHOUANCIENTS-YIYANDINGZHEN.description": "每场战斗开始时获得[red]混乱[/red]效果。在你的回合开始时，选择一张手牌，其在本回合可以免费打出。",
"TOUHOUANCIENTS-YIYANDINGZHEN.flavor": "异眼顶真，鉴定为大费物。",
"TOUHOUANCIENTS-YIYANDINGZHEN.selectionScreenPrompt": "选择一张牌使其在本回合可以免费打出。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Snecko's True Gaze` |
| `.description` | `Start each combat [red]Confused[/red]. At the start of your turn, choose a card in your [gold]Hand[/gold]. It is free to play this turn.` |
| `.flavor` | `One True Gaze confirms it: way too costly.` |
| `.selectionScreenPrompt` | `Choose a card to make it free this turn.` |

### Neta / 文化梗说明
- **「异眼顶真」** — 来源于中文互联网 meme「一眼丁真」（谐音"一眼真"），结合原版遗物"异蛇之眼"(Snecko Eye)的取名风格
- **「鉴定为大费物」** — 「大费用」（高费用）的谐音梗，讽刺异蛇之眼随机改变费用后经常出现高费牌的尴尬
- 英文 title `Snecko's True Gaze` 保留了 Snecko + "True Gaze"（顶真/真视）的双关
- 英文 flavor `"One True Gaze confirms it: way too costly."` 保留了"鉴定为费用太高"的幽默

---

## 一时兴起的消费

**键名**: `TOUHOUANCIENTS-YISHIXINGQILE`

### 中文原文
```json
"TOUHOUANCIENTS-YISHIXINGQILE.title": "一时兴起的消费",
"TOUHOUANCIENTS-YISHIXINGQILE.description": "拾起时，获得[blue]{Gold}[/blue][gold]金币[/gold]。在商店消费后，随机1~2件商店出售的物品价格将会被降至0。",
"TOUHOUANCIENTS-YISHIXINGQILE.eventDescription": "获得[blue]{Gold}[/blue][gold]金币[/gold]。在商店消费后，随机1~2件商店出售的物品价格将会被降至0。",
"TOUHOUANCIENTS-YISHIXINGQILE.flavor": "我让你别带尼尔龙马？"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Impulse Purchase` |
| `.description` | `Upon pickup, gain [blue]{Gold}[/blue] [gold]Gold[/gold]. After spending at the Merchant, [blue]1~2[/blue] random items for sale have their price reduced to [blue]0[/blue].` |
| `.eventDescription` | `Upon pickup, gain [blue]{Gold}[/blue] [gold]Gold[/gold]. After spending at the Merchant, [blue]1~2[/blue] random items for sale have their price reduced to [blue]0[/blue].` |
| `.flavor` | `Spend money to make... more spending money!` |

### Neta / 文化梗说明
- **「一时兴起的消费」** — 来源于玩家一进商店就本能反应购买优秀卡牌的行为，讽刺无计划消费
- 英文 flavor `"Spend money to make... more spending money!"` 调侃花钱赚钱的循环

---

## 固若金汤的圣铠

**键名**: `TOUHOUANCIENTS-YONGHENGKAIJIAWANGCHAOLE`

### 中文原文
```json
"TOUHOUANCIENTS-YONGHENGKAIJIAWANGCHAOLE.title": "固若金汤的圣铠",
"TOUHOUANCIENTS-YONGHENGKAIJIAWANGCHAOLE.description": "在每场战斗开始时，获得{PlatingPower}[gold]覆甲[/gold]。当你获得[gold]覆甲[/gold]时，获得{Energy:energyIcons()}。",
"TOUHOUANCIENTS-YONGHENGKAIJIAWANGCHAOLE.flavor": "永恒铠甲王朝了"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `The Ethernal Armor` |
| `.description` | `At the start of each combat, gain {PlatingPower} [gold]Plating[/gold]. Whenever you gain [gold]Plating[/gold], gain {Energy:energyIcons()}.` |
| `.flavor` | `Who wouldn't love Eternal Armor?` |

### Neta / 文化梗说明
- **反讽「永恒铠甲」(Eternal Armor)** — 这张卡牌本身强度并不突出，该遗物反讽"永恒铠甲"这张卡牌，用夸张的方式调侃其受欢迎程度
- **「Ethernal」** — 自造词，将 Ether（以太/虚无）与 Eternal 结合，暗示该遗物是"虚无的永恒铠甲"
- 英文 title `The Ethernal Armor` 保留了"虚无"的意味，flavor 则直接调侃 STS2 原版卡

### 备注
- **Eternal Armor**: STS2 原版卡牌名

---

## 童忆书包

**键名**: `TOUHOUANCIENTS-CHILDHOOD_BAG`

### 中文原文
```json
"TOUHOUANCIENTS-CHILDHOOD_BAG.title": "童忆书包",
"TOUHOUANCIENTS-CHILDHOOD_BAG.description": "拾起时，用[purple]污浊药水[/purple]填满你的药水栏。将战斗结束时的[gold]金币奖励[/gold]替换为[blue]{PotionCount}[/blue]瓶[purple]污浊药水[/purple]。你投掷的[purple]污浊药水[/purple]只会对敌人造成伤害。",
"TOUHOUANCIENTS-CHILDHOOD_BAG.eventDescription": "用[purple]污浊药水[/purple]填满你的药水栏。将战斗结束时的[gold]金币奖励[/gold]替换为[blue]{PotionCount}[/blue]瓶[purple]污浊药水[/purple]。你投掷的[purple]污浊药水[/purple]只会对敌人造成伤害。",
"TOUHOUANCIENTS-CHILDHOOD_BAG.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Childhood Bag` |
| `.description` | `Upon pickup, fill all empty [gold]Potion Slots[/gold] with [purple]Foul Potion[/purple]. Replace combat [gold]Gold[/gold] rewards with [blue]{PotionCount}[/blue] [purple]Foul Potion[/purple]. Thrown [purple]Foul Potion[/purple] only damages enemies.` |
| `.eventDescription` | `Fill all empty [gold]Potion Slots[/gold] with [purple]Foul Potion[/purple]. Replace combat [gold]Gold[/gold] rewards with [blue]{PotionCount}[/blue] [purple]Foul Potion[/purple]. Thrown [purple]Foul Potion[/purple] only damages enemies.` |
| `.flavor` | `""`（留空） |

---

## 舞台装置

**键名**: `TOUHOUANCIENTS-STAGE_DEVICE`

### 中文原文
```json
"TOUHOUANCIENTS-STAGE_DEVICE.title": "舞台装置",
"TOUHOUANCIENTS-STAGE_DEVICE.description": "在奇数回合开始时，给予所有敌人[blue]{VulnAmount}[/blue]层[gold]易伤[/gold]并在本回合内获得[blue]{TempStr}[/blue]点[gold]力量[/gold]。在偶数回合开始时，给予所有敌人[blue]{WeakAmount}[/blue]层[gold]虚弱[/gold]并在本回合内获得[blue]{TempDex}[/blue]点[gold]敏捷[/gold]。",
"TOUHOUANCIENTS-STAGE_DEVICE.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Stage Device` |
| `.description` | `At the start of odd-numbered turns, apply [blue]{VulnAmount}[/blue] [gold]Vulnerable[/gold] to ALL enemies and gain [blue]{TempStr}[/blue] [gold]Strength[/gold] this turn. At the start of even-numbered turns, apply [blue]{WeakAmount}[/blue] [gold]Weak[/gold] to ALL enemies and gain [blue]{TempDex}[/blue] [gold]Dexterity[/gold] this turn.` |
| `.flavor` | `""`（留空） |

---

## 蛊毒魔盒

**键名**: `TOUHOUANCIENTS-MEDICINE_POISON_BOX`

### 中文原文
```json
"TOUHOUANCIENTS-MEDICINE_POISON_BOX.title": "蛊毒魔盒",
"TOUHOUANCIENTS-MEDICINE_POISON_BOX.description": "拾起时，将你[gold]牌组[/gold]中的所有[gold]打击[/gold]变化为[gold]蛇咬[/gold]并[gold]附魔[/gold]：[purple]蛊毒3[/purple]。",
"TOUHOUANCIENTS-MEDICINE_POISON_BOX.eventDescription": "将你[gold]牌组[/gold]中的所有[gold]打击[/gold]变化为[gold]蛇咬[/gold]并[gold]附魔[/gold]：[purple]蛊毒3[/purple]。",
"TOUHOUANCIENTS-MEDICINE_POISON_BOX.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Toxic Chemical Box` |
| `.description` | `Upon pickup, Transform all [gold]Strikes[/gold] in your [gold]Deck[/gold] into [gold]Snakebite[/gold] and [gold]enchant[/gold] them with [purple]Toxic 3[/purple].` |
| `.eventDescription` | `Transform all [gold]Strikes[/gold] in your [gold]Deck[/gold] into [gold]Snakebite[/gold] and [gold]enchant[/gold] them with [purple]Toxic 3[/purple].` |
| `.flavor` | `""`（留空） |

### 备注
- **蛊毒 → Toxic**: 附魔名 `MEDICINE_POISON` 英文为 `Toxic`。`蛊毒3` 即 `Toxic 3`。

---

## 铃色的日记本

**键名**: `TOUHOUANCIENTS-LILY_BELL_DIARY`

### 中文原文
```json
"TOUHOUANCIENTS-LILY_BELL_DIARY.title": "铃色的日记本",
"TOUHOUANCIENTS-LILY_BELL_DIARY.description": "拾起时，将[blue]1[/blue]张[gold]铃兰[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-LILY_BELL_DIARY.eventDescription": "将[blue]1[/blue]张[gold]铃兰[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-LILY_BELL_DIARY.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Lily's Diary` |
| `.description` | `Upon pickup, add [blue]1[/blue] [gold]Lily Bell[/gold] to your [gold]Deck[/gold].` |
| `.eventDescription` | `Add [blue]1[/blue] [gold]Lily Bell[/gold] to your [gold]Deck[/gold].` |
| `.flavor` | `""`（留空） |

---

## 幸福的秘药

**键名**: `TOUHOUANCIENTS-HAPPINESS_ELIXIR`

### 中文原文
```json
"TOUHOUANCIENTS-HAPPINESS_ELIXIR.title": "幸福的秘药",
"TOUHOUANCIENTS-HAPPINESS_ELIXIR.description": "拾起时，获得一瓶[green]卡米莉亚[/green]。",
"TOUHOUANCIENTS-HAPPINESS_ELIXIR.eventDescription": "获得一瓶[green]卡米莉亚[/green]。",
"TOUHOUANCIENTS-HAPPINESS_ELIXIR.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Happiness Elixir` |
| `.description` | `Upon pickup, obtain a [green]Camellia[/green]. ` |
| `.eventDescription` | `Obtain a [green]Camellia[/green]. ` |
| `.flavor` | `""`（留空） |

---

## 丝带蝴蝶结

**键名**: `TOUHOUANCIENTS-RIBBON_BOW`

### 中文原文
```json
"TOUHOUANCIENTS-RIBBON_BOW.title": "丝带蝴蝶结",
"TOUHOUANCIENTS-RIBBON_BOW.description": "拾起时，将一张[gold]遗恨[/gold]加入你的[gold]牌组[/gold]。从你的[gold]牌组[/gold]中选择[blue]{SelectCount}[/blue]张攻击牌或技能牌，[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-RIBBON_BOW.eventDescription": "将一张[gold]遗恨[/gold]加入你的[gold]牌组[/gold]。从你的[gold]牌组[/gold]中选择[blue]{SelectCount}[/blue]张攻击牌或技能牌，[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-RIBBON_BOW.selectionScreenPrompt": "选择[blue]{SelectCount}[/blue]张牌进行[gold]附魔[/gold]",
"TOUHOUANCIENTS-RIBBON_BOW.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Ribbon Bow` |
| `.description` | `Upon pickup, add a [gold]Grudge[/gold] to your [gold]Deck[/gold]. Choose [blue]{SelectCount}[/blue] Attacks or Skills from your [gold]Deck[/gold] and [gold]enchant[/gold] them with [purple]{EnchantmentName}[/purple].` |
| `.eventDescription` | `Add a [gold]Grudge[/gold] to your [gold]Deck[/gold]. Choose [blue]{SelectCount}[/blue] Attacks or Skills from your [gold]Deck[/gold] and [gold]enchant[/gold] them with [purple]{EnchantmentName}[/purple].` |
| `.selectionScreenPrompt` | `Choose [blue]{SelectCount}[/blue] cards to [gold]enchant[/gold]` |
| `.flavor` | `""`（留空） |

### 重做说明（2026-08-10）
- 拾起效果新增：将一张[遗恨](`TOUHOUANCIENTS-YUAN_HEN`)加入牌组
- 附魔数量改为动态变量 `{SelectCount}`（默认 2），从硬编码 2 调整

### 关联卡牌：遗恨

**键名**: `TOUHOUANCIENTS-YUAN_HEN`（`scripts/cards/YuanHen.cs`）

| 语言 | `.title` | `.description` |
|------|----------|----------------|
| zhs | `遗恨` | `如果这张牌在你的[gold]手牌[/gold]中，你从非手牌区打出牌时，将一张[gold]毒素[/gold]加入你的[gold]手牌[/gold]。` |
| eng/jpn | `Grudge` | `While this card is in your [gold]Hand[/gold], whenever you play a card from a pile other than your [gold]Hand[/gold], add a [gold]Toxic[/gold] to your [gold]Hand[/gold].` |

---

## 蔷薇皇冠

**键名**: `TOUHOUANCIENTS-ROSE_CROWN`

### 中文原文
```json
"TOUHOUANCIENTS-ROSE_CROWN.title": "蔷薇皇冠",
"TOUHOUANCIENTS-ROSE_CROWN.description": "在每场战斗开始时，获得[blue]{ThornsAmount}[/blue]层[gold]荆棘[/gold]与[blue]{PlatingAmount}[/blue]层[gold]覆甲[/gold]。",
"TOUHOUANCIENTS-ROSE_CROWN.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Rose Crown` |
| `.description` | `At the start of each combat, gain [blue]{ThornsAmount}[/blue] [gold]Thorns[/gold] and [blue]{PlatingAmount}[/blue] [gold]Plating[/gold].` |
| `.flavor` | `""`（留空） |

---

## 恶毒的童话书

**键名**: `TOUHOUANCIENTS-MALICIOUS_FAIRY_TALE`

### 中文原文
```json
"TOUHOUANCIENTS-MALICIOUS_FAIRY_TALE.title": "恶毒的童话书",
"TOUHOUANCIENTS-MALICIOUS_FAIRY_TALE.description": "在每场战斗开始时，所有敌人获得[blue]{ThornsAmount}[/blue]层[gold]荆棘[/gold]。在你的回合开始时，获得[blue]{Power}[/blue]点[gold]力量[/gold]。",
"TOUHOUANCIENTS-MALICIOUS_FAIRY_TALE.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Malicious Fairy Tale` |
| `.description` | `At the start of each combat, ALL enemies gain [blue]{ThornsAmount}[/blue] [gold]Thorns[/gold]. At the start of your turn, gain [blue]{Power}[/blue] [gold]Strength[/gold].` |
| `.flavor` | `""`（留空） |

---

## 荒疫特调

**键名**: `TOUHOUANCIENTS-PLAGUE_BLEND`

### 中文原文
```json
"TOUHOUANCIENTS-PLAGUE_BLEND.title": "荒疫特调",
"TOUHOUANCIENTS-PLAGUE_BLEND.description": "在每个回合开始时获得{Energy:energyIcons()}。第[blue]{TriggerTurn}[/blue]回合开始时，给予自身[blue]{PoisonAmount}[/blue]层[gold]中毒[/gold]。",
"TOUHOUANCIENTS-PLAGUE_BLEND.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Blight Blend` |
| `.description` | `Gain {Energy:energyIcons()} at the start of each turn.  At the start of turn [blue]{TriggerTurn}[/blue], apply [blue]{PoisonAmount}[/blue] [gold]Poison[/gold] to yourself.` |
| `.flavor` | `""`（留空） |

---

## 缄默魔偶

**键名**: `TOUHOUANCIENTS-SILENCE_DOLL`

### 中文原文
```json
"TOUHOUANCIENTS-SILENCE_DOLL.title": "缄默魔偶",
"TOUHOUANCIENTS-SILENCE_DOLL.description": "每回合开始时，选择一张上回合进入过[gold]弃牌堆[/gold]的卡牌放入[gold]手牌[/gold]，这张牌直到打出前获得[gold]保留[/gold]。",
"TOUHOUANCIENTS-SILENCE_DOLL.selectionScreenPrompt": "选择一张牌加入你的手牌。",
"TOUHOUANCIENTS-SILENCE_DOLL.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Silence Doll` |
| `.description` | `At the start of each turn, choose a card that entered [gold]Discard Pile[/gold] last turn and put it into your [gold]Hand[/gold]. It gains [gold]Retain[/gold] until played.` |
| `.selectionScreenPrompt` | `Choose a card to add to your Hand.` |
| `.flavor` | `""`（留空） |

---

## 弘川之骨

**键名**: `TOUHOUANCIENTS-REPOSITORY_OF_HIROKAWA`

### 中文原文
```json
"TOUHOUANCIENTS-REPOSITORY_OF_HIROKAWA.title": "弘川之骨",
"TOUHOUANCIENTS-REPOSITORY_OF_HIROKAWA.description": "回合结束时，如果你本回合只打出过[gold]能力牌[/gold]，获得[blue]{BufferAmount}[/blue]层[gold]缓冲[/gold]。",
"TOUHOUANCIENTS-REPOSITORY_OF_HIROKAWA.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Repository of Hirokawa` |
| `.description` | `At the end of your turn, if you only played [gold]Powers[/gold] this turn, gain [blue]{BufferAmount}[/blue] [gold]Buffer[/gold].` |
| `.flavor` | `""`（留空） |

---

## 天冠

**键名**: `TOUHOUANCIENTS-SKY_HAT`

### 中文原文
```json
"TOUHOUANCIENTS-SKY_HAT.title": "天冠",
"TOUHOUANCIENTS-SKY_HAT.description": "你每获得[blue]{BlockPerIntangible}[/blue][gold]格挡[/gold]，将[blue]{Cards}[/blue]张[gold]灵魂+[/gold]加入[gold]抽牌堆[/gold]。",
"TOUHOUANCIENTS-SKY_HAT.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Sky Hat` |
| `.description` | `Whenever you gain [blue]{BlockPerIntangible}[/blue] [gold]Block[/gold], add [blue]{Cards}[/blue] [gold]Soul+[/gold] into your [gold]Draw Pile[/gold].` |
| `.flavor` | `""`（留空） |

---

## 反魂蝶

**键名**: `TOUHOUANCIENTS-SOUL_BUTTERFLY`

### 中文原文
```json
"TOUHOUANCIENTS-SOUL_BUTTERFLY.title": "反魂蝶",
"TOUHOUANCIENTS-SOUL_BUTTERFLY.description": "当你的生命值将要降低至[blue]0[/blue]或以下时，回复到[blue]{ReviveHp}[/blue]血，获得[blue]{IntangibleAmount}[/blue]层[gold]无实体[/gold]。然后休眠[blue]{DormantTurns}[/blue]个回合。",
"TOUHOUANCIENTS-SOUL_BUTTERFLY.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Soul Butterfly` |
| `.description` | `When your HP would be reduced to [blue]0[/blue], heal to [blue]{ReviveHp}[/blue] HP, gain [blue]{IntangibleAmount}[/blue] [gold]Intangible[/gold], then become dormant for [blue]{DormantTurns}[/blue] turns.` |
| `.flavor` | `""`（留空） |

---

## 弹幕的亡灵

**键名**: `TOUHOUANCIENTS-DANMUKU_GHOST`

### 中文原文
```json
"TOUHOUANCIENTS-DANMUKU_GHOST.title": "弹幕的亡灵",
"TOUHOUANCIENTS-DANMUKU_GHOST.description": "敌人的回合结束时，若其进行过攻击但未造成伤害，使其获得[blue]{ShrinkAmount}[/blue]回合[gold]缩小[/gold]。",
"TOUHOUANCIENTS-DANMUKU_GHOST.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Danmaku Ghost` |
| `.description` | `At the end of an enemy's turn, if it attacked but dealt no damage, apply [blue]{ShrinkAmount}[/blue] [gold]Shrink[/gold] to it.` |
| `.flavor` | `""`（留空） |

---

## 幽灵折扇

**键名**: `TOUHOUANCIENTS-GHOST_FAN`

### 中文原文
```json
"TOUHOUANCIENTS-GHOST_FAN.title": "幽灵折扇",
"TOUHOUANCIENTS-GHOST_FAN.description": "拾起时，选择至多[blue]3[/blue]张[gold]技能牌[/gold]，为这些牌[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-GHOST_FAN.eventDescription": "选择至多[blue]3[/blue]张[gold]技能牌[/gold]，为这些牌[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-GHOST_FAN.selectionScreenPrompt": "选择至多[blue]3[/blue]张技能牌进行[gold]附魔[/gold]",
"TOUHOUANCIENTS-GHOST_FAN.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Ghost Fan` |
| `.description` | `Upon pickup, choose up to [blue]3[/blue] [gold]Skills[/gold] and [gold]enchant[/gold] them with [purple]{EnchantmentName}[/purple].` |
| `.eventDescription` | `Choose up to [blue]3[/blue] [gold]Skills[/gold] and [gold]enchant[/gold] them with [purple]{EnchantmentName}[/purple].` |
| `.selectionScreenPrompt` | `Choose up to [blue]3[/blue] Skills to [gold]enchant[/gold]` |
| `.flavor` | `""`（留空） |

---

## 墨染的樱花

**键名**: `TOUHOUANCIENTS-INK_DYED_CHERRY_BLOSSOMS`

### 中文原文
```json
"TOUHOUANCIENTS-INK_DYED_CHERRY_BLOSSOMS.title": "墨染的樱花",
"TOUHOUANCIENTS-INK_DYED_CHERRY_BLOSSOMS.description": "在每个回合开始时获得{Energy:energyIcons()}并失去[blue]1[/blue]点[gold]最大生命值[/gold]。你可以在[gold]休息处[/gold][pink]赏樱[/pink]。",
"TOUHOUANCIENTS-INK_DYED_CHERRY_BLOSSOMS.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Ink-Dyed Cherry Blossoms` |
| `.description` | `At the start of each turn, gain {Energy:energyIcons()} and lose [blue]1[/blue] [gold]Max HP[/gold]. You may [pink]Hanami[/pink] at [gold]Rest Sites[/gold].` |
| `.flavor` | `""`（留空） |

---

## 幽魂酒盅

**键名**: `TOUHOUANCIENTS-SOUL_SAKE_CUP`

### 中文原文
```json
"TOUHOUANCIENTS-SOUL_SAKE_CUP.title": "幽魂酒盅",
"TOUHOUANCIENTS-SOUL_SAKE_CUP.description": "拾起时，将你的最大生命值提升[blue]{MaxHpGain}[/blue]点，然后将生命值降低至[blue]{SetHp}[/blue]。",
"TOUHOUANCIENTS-SOUL_SAKE_CUP.eventDescription": "将你的最大生命值提升[blue]{MaxHpGain}[/blue]点，然后将生命值降低至[blue]{SetHp}[/blue]。",
"TOUHOUANCIENTS-SOUL_SAKE_CUP.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Sake Cup` |
| `.description` | `Upon pickup, increase your Max HP by [blue]{MaxHpGain}[/blue], then set your HP to [blue]{SetHp}[/blue].` |
| `.eventDescription` | `Increase your Max HP by [blue]{MaxHpGain}[/blue], then set your HP to [blue]{SetHp}[/blue].` |
| `.flavor` | `""`（留空） |

---

## 人魂灯

**键名**: `TOUHOUANCIENTS-SOUL_LATTERN`

### 中文原文
```json
"TOUHOUANCIENTS-SOUL_LATTERN.title": "人魂灯",
"TOUHOUANCIENTS-SOUL_LATTERN.description": "每场战斗首次有任意单位的生命值低于[blue]50%[/blue]时，进入[gold]死神形态[/gold]。",
"TOUHOUANCIENTS-SOUL_LATTERN.flavor": "死是凉爽的夏夜，可供人无忧的安眠。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Soul Lantern` |
| `.description` | `The first time any unit's HP drops below [blue]50%[/blue] each combat, enter [gold]Reaper Form[/gold].` |
| `.flavor` | `Death is a cool summer night, where one may sleep without a care.` |

---

## 黄泉之期票

**键名**: `TOUHOUANCIENTS-TICKET_TO_NETHERWORLD`

### 中文原文
```json
"TOUHOUANCIENTS-TICKET_TO_NETHERWORLD.title": "黄泉之期票",
"TOUHOUANCIENTS-TICKET_TO_NETHERWORLD.description": "拾起时，将[blue]1[/blue]张[gold]生者必灭之理[/gold]加入牌组。",
"TOUHOUANCIENTS-TICKET_TO_NETHERWORLD.eventDescription": "将[blue]1[/blue]张[gold]生者必灭之理[/gold]加入牌组。",
"TOUHOUANCIENTS-TICKET_TO_NETHERWORLD.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Ticket to Netherworld` |
| `.description` | `Upon pickup, add [blue]1[/blue] [gold]All Living Things Must Perish[/gold] to your [gold]Deck[/gold].` |
| `.eventDescription` | `Add [blue]1[/blue] [gold]All Living Things Must Perish[/gold] to your [gold]Deck[/gold].` |
| `.flavor` | `""`（留空） |

---

## 西行妖枯枝

**键名**: `TOUHOUANCIENTS-SAIGYOUJI_BRANCH`

### 中文原文
```json
"TOUHOUANCIENTS-SAIGYOUJI_BRANCH.title": "西行妖枯枝",
"TOUHOUANCIENTS-SAIGYOUJI_BRANCH.description": "每当你[gold]消耗[/gold]一张牌，增加一张带有[gold]虚无[/gold]的随机卡牌到你的[gold]手牌[/gold]并获得[blue]{BlockAmount}[/blue][gold]格挡[/gold]。",
"TOUHOUANCIENTS-SAIGYOUJI_BRANCH.flavor": "请勿用作添柴。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Saigyouji's Withered Branch` |
| `.description` | `Whenever you [gold]Exhaust[/gold] a card, add a random card with [gold]Ethereal[/gold] to your [gold]Hand[/gold] and gain [blue]{BlockAmount}[/blue] [gold]Block[/gold].` |
| `.flavor` | `Do not use as kindling.` |

---

## 绀珠之药

**键名**: `TOUHOUANCIENTS-KONSHII_NO_KUSURI`

### 中文原文
```json
"TOUHOUANCIENTS-KONSHII_NO_KUSURI.title": "绀珠之药",
"TOUHOUANCIENTS-KONSHII_NO_KUSURI.description": "在每场战斗开始时，将[blue]1[/blue]张[gold]绀珠之药[/gold]放入你的[gold]手牌[/gold]。",
"TOUHOUANCIENTS-KONSHII_NO_KUSURI.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Ultramarine Orb Elixir` |
| `.description` | `At the start of each combat, add [blue]1[/blue] [gold]Ultramarine Orb Elixir[/gold] to your [gold]Hand[/gold].` |
| `.flavor` | `""`（留空） |

---

## 龙颈之玉

**键名**: `TOUHOUANCIENTS-RYUKEI_NO_TAMA`

### 中文原文
```json
"TOUHOUANCIENTS-RYUKEI_NO_TAMA.title": "龙颈之玉",
"TOUHOUANCIENTS-RYUKEI_NO_TAMA.description": "你每打出[blue]{Card}[/blue]张牌，将一张带有[gold]虚无[/gold]、辉星费用为[blue]0[/blue]的[gold]七星[/gold]放入你的[gold]手牌[/gold]。",
"TOUHOUANCIENTS-RYUKEI_NO_TAMA.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Jewel from the Dragon's Neck` |
| `.description` | `Every [blue]{Card}[/blue] cards you play, put a [gold]Seven Stars[/gold] with [gold]Ethereal[/gold] and [blue]0[/blue] Stars cost into your [gold]Hand[/gold].` |
| `.flavor` | `""`（留空） |

---

## 火鼠的皮衣

**键名**: `TOUHOUANCIENTS-HINEZUMI_NO_KAWAGOROMO`

### 中文原文
```json
"TOUHOUANCIENTS-HINEZUMI_NO_KAWAGOROMO.title": "火鼠的皮衣",
"TOUHOUANCIENTS-HINEZUMI_NO_KAWAGOROMO.description": "在战斗开始时，将[blue]{BurnCount}[/blue]张[gold]灼伤[/gold]加入你的[gold]抽牌堆[/gold]。受到来自[gold]灼伤[/gold]的伤害时，免疫之并获得[blue]{Block}[/blue]点[gold]格挡[/gold]与[blue]{Thorns}[/blue]层[gold]荆棘[/gold]，在下回合额外抽[blue]1[/blue]张牌。",
"TOUHOUANCIENTS-HINEZUMI_NO_KAWAGOROMO.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Robe of Fire Rat` |
| `.description` | `At the start of combat, add [blue]{BurnCount}[/blue] [gold]Burn[/gold] into your [gold]Draw Pile[/gold]. When you take damage from [gold]Burn[/gold], negate it and gain [blue]{Block}[/blue] [gold]Block[/gold] and [blue]{Thorns}[/blue] [gold]Thorns[/gold], next turn, draw [blue]1[/blue] extra card.` |
| `.flavor` | `""`（留空） |

---

## 燕之子安贝

**键名**: `TOUHOUANCIENTS-TSUBAME_NO_KOYASUGAI`

### 中文原文
```json
"TOUHOUANCIENTS-TSUBAME_NO_KOYASUGAI.title": "燕之子安贝",
"TOUHOUANCIENTS-TSUBAME_NO_KOYASUGAI.description": "拾起时，将你的最大生命值提升[blue]{MaxHpGain}[/blue]点。进入新的房间时，若你的生命值少于[blue]40%[/blue]则回复到[blue]40%[/blue]。",
"TOUHOUANCIENTS-TSUBAME_NO_KOYASUGAI.eventDescription": "将你的最大生命值提升[blue]{MaxHpGain}[/blue]点。进入新的房间时，若你的生命值少于[blue]40%[/blue]则回复到[blue]40%[/blue]。",
"TOUHOUANCIENTS-TSUBAME_NO_KOYASUGAI.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Swallow's Cowrie Shell` |
| `.description` | `Upon pickup, increase your Max HP by [blue]{MaxHpGain}[/blue]. Upon entering a new room, if your HP is below [blue]40%[/blue], heal to [blue]40%[/blue].` |
| `.eventDescription` | `Increase your Max HP by [blue]{MaxHpGain}[/blue]. Upon entering a new room, if your HP is below [blue]40%[/blue], heal to [blue]40%[/blue].` |
| `.flavor` | `""`（留空） |

---

## 佛御石之钵

**键名**: `TOUHOUANCIENTS-HOTOKE_MISHI_ISHI_NO_HACHI`

### 中文原文
```json
"TOUHOUANCIENTS-HOTOKE_MISHI_ISHI_NO_HACHI.title": "佛御石之钵",
"TOUHOUANCIENTS-HOTOKE_MISHI_ISHI_NO_HACHI.description": "拾起时，失去[blue]{MaxHpLoss}[/blue]点最大生命值。在每场战斗开始时，获得[blue]{Dexterity}[/blue]点[gold]敏捷[/gold]与[blue]{StartBlock}[/blue]点[gold]格挡[/gold]。",
"TOUHOUANCIENTS-HOTOKE_MISHI_ISHI_NO_HACHI.eventDescription": "失去[blue]{MaxHpLoss}[/blue]点最大生命值。在每场战斗开始时，获得[blue]{Dexterity}[/blue]点[gold]敏捷[/gold]与[blue]{StartBlock}[/blue]点[gold]格挡[/gold]。",
"TOUHOUANCIENTS-HOTOKE_MISHI_ISHI_NO_HACHI.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Buddha's Stone Bowl` |
| `.description` | `Upon pickup, lose [blue]{MaxHpLoss}[/blue] Max HP. At the start of each combat, gain [blue]{Dexterity}[/blue] [gold]Dexterity[/gold] and [blue]{StartBlock}[/blue] [gold]Block[/gold].` |
| `.eventDescription` | `Lose [blue]{MaxHpLoss}[/blue] Max HP. At the start of each combat, gain [blue]{Dexterity}[/blue] [gold]Dexterity[/gold] and [blue]{StartBlock}[/blue] [gold]Block[/gold].` |
| `.flavor` | `""`（留空） |

---

## 蓬莱的玉枝

**键名**: `TOUHOUANCIENTS-HOURAI_NO_TAMAE`

### 中文原文
```json
"TOUHOUANCIENTS-HOURAI_NO_TAMAE.title": "蓬莱的玉枝",
"TOUHOUANCIENTS-HOURAI_NO_TAMAE.description": "每场战斗开始时，为你的随机[blue]{EnchantCount}[/blue]张牌[gold]附魔[/gold]：[purple]梦色[/purple]。",
"TOUHOUANCIENTS-HOURAI_NO_TAMAE.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Jeweled Branch of Hourai` |
| `.description` | `At the start of each combat, [gold]enchant[/gold] [blue]{EnchantCount}[/blue] random cards in your [gold]Deck[/gold] with [purple]Dreamy[/purple].` |
| `.flavor` | `""`（留空） |

---

## 永远亭座药

**键名**: `TOUHOUANCIENTS-EIENTEI_ZAKUSHI`

### 中文原文
```json
"TOUHOUANCIENTS-EIENTEI_ZAKUSHI.title": "永远亭座药",
"TOUHOUANCIENTS-EIENTEI_ZAKUSHI.description": "拾起时，用随机[gold]药水[/gold]填满你的[gold]药水栏位[/gold]。在你的回合开始时，你每拥有一瓶[gold]药水[/gold]，对随机一个敌人造成[blue]{DamagePerPotion}[/blue]点伤害。",
"TOUHOUANCIENTS-EIENTEI_ZAKUSHI.eventDescription": "用随机[gold]药水[/gold]填满你的[gold]药水栏位[/gold]。在你的回合开始时，你每拥有一瓶[gold]药水[/gold]，对随机一个敌人造成[blue]{DamagePerPotion}[/blue]点伤害。",
"TOUHOUANCIENTS-EIENTEI_ZAKUSHI.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Eientei Zakushi` |
| `.description` | `Upon pickup, fill all empty [gold]Potion Slots[/gold] with random [gold]Potions[/gold]. At the start of your turn, for each [gold]Potion[/gold] you have, deal [blue]{DamagePerPotion}[/blue] damage to a random enemy.` |
| `.eventDescription` | `Fill all empty [gold]Potion Slots[/gold] with random [gold]Potions[/gold]. At the start of your turn, for each [gold]Potion[/gold] you have, deal [blue]{DamagePerPotion}[/blue] damage to a random enemy.` |
| `.flavor` | `""`（留空） |


---

## 辉夜姬秘宝

**键名**: `TOUHOUANCIENTS-KAGUYA_SECRET_TREASURE`

### 中文原文
```json
"TOUHOUANCIENTS-KAGUYA_SECRET_TREASURE.title": "辉夜姬秘宝",
"TOUHOUANCIENTS-KAGUYA_SECRET_TREASURE.description": "每场战斗开始时，从[blue]{Cards}[/blue]张[green]升级过的[/green]其它角色的牌库中选择一张加入[gold]手牌[/gold]。打出这张牌后，其原始复制会额外出现在[gold]战斗奖励[/gold]中。",
"TOUHOUANCIENTS-KAGUYA_SECRET_TREASURE.flavor": "",
"TOUHOUANCIENTS-KAGUYA_SECRET_TREASURE.selectionScreenPrompt": "选择一张牌"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Kaguya's Secret Treasure` |
| `.description` | `At the start of each combat, choose one of [blue]{Cards}[/blue] [green]Upgraded[/green] cards from other characters' pools and add it to your [gold]Hand[/gold]. After playing this card, its original copy additionally appears in [gold]combat rewards[/gold].` |
| `.selectionScreenPrompt` | `Choose a card` |
| `.flavor` | `""`（留空） |

---

## 迷你八卦炉

**键名**: `TOUHOUANCIENTS-MINI_HAKKERO`

### 中文原文
```json
"TOUHOUANCIENTS-MINI_HAKKERO.title": "迷你八卦炉",
"TOUHOUANCIENTS-MINI_HAKKERO.description": "回合结束时，消耗[blue]1~3[/blue]张手牌。若消耗的牌不小于[blue]{Cards}[/blue]张，下个回合开始时获得{Energy:energyIcons()}。",
"TOUHOUANCIENTS-MINI_HAKKERO.flavor": "能将魔法的波纹集中一处朝前方发射的魔法武器。",
"TOUHOUANCIENTS-MINI_HAKKERO.selectionScreenPrompt": "选择1~3张牌消耗"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Mini Hakkero` |
| `.description` | `At end of turn, [gold]Exhaust[/gold] [blue]1~3[/blue] cards from your [gold]Hand[/gold]. If at least [blue]{Cards}[/blue] card(s) were exhausted, gain {Energy:energyIcons()} at the start of next turn.` |
| `.selectionScreenPrompt` | `Choose 1~3 cards to Exhaust` |
| `.flavor` | `A magical weapon that focuses magical waves into one point and fires them forward.` |

---

## 恋色手电筒

**键名**: `TOUHOUANCIENTS-LOVE_COLOR_FLASHLIGHT`

### 中文原文
```json
"TOUHOUANCIENTS-LOVE_COLOR_FLASHLIGHT.title": "恋色手电筒",
"TOUHOUANCIENTS-LOVE_COLOR_FLASHLIGHT.description": "拾起时，将[blue]1[/blue]张[gold]极限火花[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-LOVE_COLOR_FLASHLIGHT.eventDescription": "将[blue]1[/blue]张[gold]极限火花[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-LOVE_COLOR_FLASHLIGHT.flavor": "不华丽就不是魔法了！弹幕最重要的是火力Da☆Ze！"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Flashlight of Love` |
| `.description` | `Upon pickup, add [blue]1[/blue] [gold]Master Spark[/gold] to your [gold]Deck[/gold].` |
| `.eventDescription` | `Add [blue]1[/blue] [gold]Master Spark[/gold] to your [gold]Deck[/gold].` |
| `.flavor` | `If it's not flashy, it's not magic! The most important thing about danmaku is firepower Da☆Ze!` |

---

## 彗星加速器

**键名**: `TOUHOUANCIENTS-COMET_ACCELERATOR`

### 中文原文
```json
"TOUHOUANCIENTS-COMET_ACCELERATOR.title": "彗星加速器",
"TOUHOUANCIENTS-COMET_ACCELERATOR.description": "在每个回合开始时，额外抽[blue]2[/blue]张牌，并将[blue]{DazeNum}[/blue]张[gold]晕眩[/gold]加入弃牌堆。",
"TOUHOUANCIENTS-COMET_ACCELERATOR.flavor": "携带书本逃离红魔馆的速度是每秒11.2米。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Comet Accelerator` |
| `.description` | `At the start of each turn, draw [blue]2[/blue] additional cards and add [blue]{DazeNum}[/blue] [gold]Daze[/gold] into your [gold]Discard Pile[/gold].` |
| `.flavor` | `The speed required to escape the Scarlet Devil Mansion while carrying books is 11.2 meters per second.` |

---

## 金平糖罐

**键名**: `TOUHOUANCIENTS-KOMPEITO_POT`

### 中文原文
```json
"TOUHOUANCIENTS-KOMPEITO_POT.title": "金平糖罐",
"TOUHOUANCIENTS-KOMPEITO_POT.description": "你的[gold]卡牌奖励[/gold]中将额外出现一张[green]升级过的[/green][gold]能力牌[/gold]，选取[blue]3[/blue]张后失效。",
"TOUHOUANCIENTS-KOMPEITO_POT.flavor": "如同繁星一般闪烁的，孩子们与妖精们的挚爱。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Konpeito Pot` |
| `.description` | `Your [gold]Card Rewards[/gold] contain an additional [green]Upgraded[/green] [gold]Power[/gold]. Expires after [blue]3[/blue] picks.` |
| `.flavor` | `Like twinkling stars, beloved by children and fairies alike.` |

---

## 星尘扫帚

**键名**: `TOUHOUANCIENTS-STARDUST_BROOM`

### 中文原文
```json
"TOUHOUANCIENTS-STARDUST_BROOM.title": "星尘扫帚",
"TOUHOUANCIENTS-STARDUST_BROOM.description": "初始充能[blue]2[/blue]，最大充能[blue]3[/blue]。你可以消耗一层充能无视当前的路线选择下一层的房间。每经过[blue]3[/blue]个房间获得[blue]1[/blue]充能。",
"TOUHOUANCIENTS-STARDUST_BROOM.flavor": "世上本没有魔法扫帚，坐的人多了，也便有了。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Stardust Broom` |
| `.description` | `Start with [blue]2[/blue] charges, max [blue]3[/blue] charges. You may ignore paths when choosing the next rooms to travel to, consuming [blue]1[/blue] charge per use. Gain [blue]1[/blue] charge every [blue]3[/blue] rooms.` |
| `.flavor` | `There was never a magic broom in this world, but when enough people ride it, it becomes one.` |

---

## 魔女的炼药锅

**键名**: `TOUHOUANCIENTS-WITCHS_CAULDRON`

### 中文原文
```json
"TOUHOUANCIENTS-WITCHS_CAULDRON.title": "魔女的炼药锅",
"TOUHOUANCIENTS-WITCHS_CAULDRON.description": "拾起时，获得[blue]2[/blue]个[gold]药水栏位[/gold]。你可以在[gold]休息处[/gold]炼药。",
"TOUHOUANCIENTS-WITCHS_CAULDRON.eventDescription": "获得[blue]2[/blue]个[gold]药水栏位[/gold]。你可以在[gold]休息处[/gold]炼药。",
"TOUHOUANCIENTS-WITCHS_CAULDRON.flavor": "炼药锅里有股洗不掉的蘑菇味。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Witch's Cauldron` |
| `.description` | `Upon pickup, gain [blue]2[/blue] [gold]Potion Slots[/gold]. You may [gold]Brew[/gold] at [gold]Rest Sites[/gold].` |
| `.eventDescription` | `Gain [blue]2[/blue] [gold]Potion Slots[/gold]. You may [gold]Brew[/gold] at [gold]Rest Sites[/gold].` |
| `.flavor` | `The cauldron has a lingering smell of mushrooms that just won't wash off.` |

---

## 不稳定魔瓶

**键名**: `TOUHOUANCIENTS-UNSTABLE_BOTTLE`

### 中文原文
```json
"TOUHOUANCIENTS-UNSTABLE_BOTTLE.title": "不稳定魔瓶",
"TOUHOUANCIENTS-UNSTABLE_BOTTLE.description": "在每个回合开始时，随机为[gold]抽牌堆[/gold]与[gold]弃牌堆[/gold]中的各[blue]1[/blue]张牌[gold]附魔[/gold]：[purple]华彩[/purple]。",
"TOUHOUANCIENTS-UNSTABLE_BOTTLE.flavor": "请不要打探少女的裙中藏了什么哦？"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Unstable Bottle` |
| `.description` | `At the start of each turn, randomly [gold]enchant[/gold] [blue]1[/blue] card in your [gold]Draw Pile[/gold] and [blue]1[/blue] card in your [gold]Discard Pile[/gold] with [purple]Glam[/purple].` |
| `.flavor` | `Please don't go poking around under a maiden's skirt, okay?` |

---

## 太阳系仪

**键名**: `TOUHOUANCIENTS-GLOBE`

### 中文原文
```json
"TOUHOUANCIENTS-GLOBE.title": "太阳系仪",
"TOUHOUANCIENTS-GLOBE.description": "以保留[green]升级[/green]的状态变化[blue]{Cards}[/blue]张牌。",
"TOUHOUANCIENTS-GLOBE.flavor": "天阶夜色凉如水，坐看牵牛织女星。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Globe` |
| `.description` | `Transform [blue]{Cards}[/blue] cards, keeping their [green]Upgrades[/green].` |
| `.flavor` | `The night stretches cool as water, as I sit and watch the Weaver and the Cowherd stars.` |


---

## 蘑菇便当

**键名**: `TOUHOUANCIENTS-MUSHROOM_BENTO`

### 中文原文
```json
"TOUHOUANCIENTS-MUSHROOM_BENTO.title": "蘑菇便当",
"TOUHOUANCIENTS-MUSHROOM_BENTO.description": "在每个回合开始时获得{Energy:energyIcons()}。在你的回合结束时，如果你的手牌数不小于[blue]{Cards}[/blue]，将一张[gold]孢子心灵[/gold]加入抽牌堆。",
"TOUHOUANCIENTS-MUSHROOM_BENTO.flavor": "用八卦炉的火力烧熟米饭，并把经过数日独方调配煮熬的（超大量的）各种品类的可食用蘑菇煮成汤汁浇在上面，无论是外观还是口味都是一绝。",
"TOUHOUANCIENTS-MUSHROOM_BENTO.mushroom": "借助魔理沙的蘑菇料理技巧好好料理了，不会受到负面影响。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Mushroom Bento` |
| `.description` | `Gain {Energy:energyIcons()} at the start of each turn.  At the end of your turn, if you have at least [blue]{Cards}[/blue] cards in your [gold]Hand[/gold], add a [gold]Spore Mind[/gold] to your [gold]Draw Pile[/gold].` |
| `.flavor` | `Rice cooked with Hakkero firepower, topped with a soup made from days of secret-recipe-brewed (massive amounts of) edible mushrooms. A feast for both the eyes and the palate.` |
| `.mushroom` | `Marisa's mushroom cooking has been put to good use—no negative effects will occur.` |

---

## 纯粹的自信

**键名**: `TOUHOUANCIENTS-PURE_CONFIDENCE`

### 中文原文
```json
"TOUHOUANCIENTS-PURE_CONFIDENCE.title": "纯粹的自信",
"TOUHOUANCIENTS-PURE_CONFIDENCE.description": "拾起时，将第[blue]3[/blue][gold]阶段[/gold]的地图替换为一条[red]极为凶险[/red]的充满精英与宝藏的直道。在每个回合开始时获得{Energy:energyIcons()}。",
"TOUHOUANCIENTS-PURE_CONFIDENCE.flavor": "会赢的。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Pure Confidence` |
| `.description` | `Upon pickup, replace the Act [blue]3[/blue] map with a [red]perilous[/red] straight path filled with Elites and treasure. Gain {Energy:energyIcons()} at the start of each turn. ` |
| `.flavor` | `I will prevail.` |

---

## 不休的恚恨

**键名**: `TOUHOUANCIENTS-CEASELESS_RESENTMENT`

### 中文原文
```json
"TOUHOUANCIENTS-CEASELESS_RESENTMENT.title": "不休的恚恨",
"TOUHOUANCIENTS-CEASELESS_RESENTMENT.description": "受到来自敌人的伤害时，为该敌人施加等同于失去生命值的来源于你的[gold]怨仇[/gold]。",
"TOUHOUANCIENTS-CEASELESS_RESENTMENT.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Ceaseless Resentment` |
| `.description` | `When you take damage from an enemy, apply [gold]Resentment[/gold] to that enemy equal to the HP lost, sourced from you.` |
| `.flavor` | `""`（留空） |

### 备注
- **怨仇 → Resentment**: Power名 `YUAN_CHOU`，英文暂未翻译。此处作为描述文本引用使用 `Resentment`。待询问

---

## 杀意的百合

**键名**: `TOUHOUANCIENTS-MURDEROUS_LILY`

### 中文原文
```json
"TOUHOUANCIENTS-MURDEROUS_LILY.title": "杀意的百合",
"TOUHOUANCIENTS-MURDEROUS_LILY.description": "拾起时，将[blue]1[/blue]张[gold]杀戮灵气[/gold]加入你的[gold]牌组[/gold]。战斗结束时，如果没有任何单位死于[gold]杀戮灵气[/gold]，将[blue]{Cards}[/blue]张[gold]杀戮灵气[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-MURDEROUS_LILY.eventDescription": "将[blue]1[/blue]张[gold]杀戮灵气[/gold]加入你的[gold]牌组[/gold]。战斗结束时，如果没有任何单位死于[gold]杀戮灵气[/gold]，将{Cards}张[gold]杀戮灵气[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-MURDEROUS_LILY.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Murderous Lily` |
| `.description` | `Upon pickup, add [blue]1[/blue] [gold]Killing Aura[/gold] to your [gold]Deck[/gold]. At the end of combat, if no unit died from [gold]Killing Aura[/gold], add [blue]{Cards}[/blue] [gold]Killing Aura[/gold] to your [gold]Deck[/gold].` |
| `.eventDescription` | `Add [blue]1[/blue] [gold]Killing Aura[/gold] to your [gold]Deck[/gold]. At the end of combat, if no unit died from [gold]Killing Aura[/gold], add {Cards} [gold]Killing Aura[/gold] to your [gold]Deck[/gold].` |
| `.flavor` | `""`（留空） |

---

## 死黑的冠冕

**键名**: `TOUHOUANCIENTS-DEATH_BLACK_CROWN`

### 中文原文
```json
"TOUHOUANCIENTS-DEATH_BLACK_CROWN.title": "死黑的冠冕",
"TOUHOUANCIENTS-DEATH_BLACK_CROWN.description": "每回合你打出的前[blue]{CardThreshold}[/blue]张牌造成的伤害翻倍。",
"TOUHOUANCIENTS-DEATH_BLACK_CROWN.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Death Black Crown` |
| `.description` | `The first [blue]{CardThreshold}[/blue] cards you play each turn deal double damage.` |
| `.flavor` | `""`（留空） |

---

## 弹幕的地狱

**键名**: `TOUHOUANCIENTS-HELL_OF_BULLETS`

### 中文原文
```json
"TOUHOUANCIENTS-HELL_OF_BULLETS.title": "弹幕的地狱",
"TOUHOUANCIENTS-HELL_OF_BULLETS.description": "拾起时，将[blue]1[/blue]张[gold]纯粹的弹幕地狱[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-HELL_OF_BULLETS.eventDescription": "将[blue]1[/blue]张[gold]纯粹的弹幕地狱[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-HELL_OF_BULLETS.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Hell of Bullets` |
| `.description` | `Upon pickup, add [blue]1[/blue] [gold]Pure Hell of Bullets[/gold] to your [gold]Deck[/gold].` |
| `.eventDescription` | `Add [blue]1[/blue] [gold]Pure Hell of Bullets[/gold] to your [gold]Deck[/gold].` |
| `.flavor` | `""`（留空） |

### 备注
- **纯粹的弹幕地狱 → Pure Hell of Bullets**: `TOUHOUANCIENTS-PURE_HELL_OF_BULLETS` 卡牌在 `eng/cards.json` 中尚未翻译。译名待确认。待询问

---

## 战栗的冻星

**键名**: `TOUHOUANCIENTS-TREMBLING_FROZEN_STAR`

### 中文原文
```json
"TOUHOUANCIENTS-TREMBLING_FROZEN_STAR.title": "战栗的冻星",
"TOUHOUANCIENTS-TREMBLING_FROZEN_STAR.description": "每打出[blue]{AttackThreshold}[/blue]张攻击牌，生成一个[gold]冰霜[/gold]充能球，并交替将一张带有[gold]虚无[/gold]的[gold]战栗[/gold]/[gold]主宰[/gold]添加到你的[gold]手牌[/gold]。",
"TOUHOUANCIENTS-TREMBLING_FROZEN_STAR.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Trembling Frozen Star` |
| `.description` | `Every [blue]{AttackThreshold}[/blue] Attacks played, evoke a [gold]Frost[/gold] Orb, and alternately add a [gold]Tremble[/gold] / [gold]Dominate[/gold] with [gold]Ethereal[/gold] to your [gold]Hand[/gold].` |
| `.flavor` | `""`（留空） |

---

## 迷幻的投影

**键名**: `TOUHOUANCIENTS-ILLUSORY_PROJECTION`

### 中文原文
```json
"TOUHOUANCIENTS-ILLUSORY_PROJECTION.title": "迷幻的投影",
"TOUHOUANCIENTS-ILLUSORY_PROJECTION.description": "在每场战斗开始时，获得[red]混乱[/red]效果。费用为[blue]{CostTwo}[/blue]的卡牌打出[blue]{CostTwo}[/blue]次，费用大于等于[blue]{CostThree}[/blue]的卡牌打出[blue]{CostThree}[/blue]次。",
"TOUHOUANCIENTS-ILLUSORY_PROJECTION.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Illusory Projection` |
| `.description` | `Start each combat [red]Confused[/red]. Cards costing [blue]{CostTwo}[/blue] are played [blue]{CostTwo}[/blue] times, and cards costing [blue]{CostThree}[/blue] or more are played [blue]{CostThree}[/blue] times.` |
| `.flavor` | `""`（留空） |

---

## 溢出的暇秽

**键名**: `TOUHOUANCIENTS-OVERFLOWING_DEFILEMENT`

### 中文原文
```json
"TOUHOUANCIENTS-OVERFLOWING_DEFILEMENT.title": "溢出的暇秽",
"TOUHOUANCIENTS-OVERFLOWING_DEFILEMENT.description": "在每个回合开始时，获得{Energy:energyIcons()}，额外抽[blue]{Cards}[/blue]张牌。在你的回合结束时，每有一张非状态[gold]手牌[/gold]，就向抽牌堆加入一张[gold]碎屑[/gold]，每有一点能量，就向抽牌堆加入一张[gold]虚空[/gold]。",
"TOUHOUANCIENTS-OVERFLOWING_DEFILEMENT.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Overflowing Defilement` |
| `.description` | `At the start of each turn, gain {Energy:energyIcons()} and draw [blue]{Cards}[/blue] additional cards. At the end of your turn, for each non-Status card in your [gold]Hand[/gold], add a [gold]Debris[/gold] to your [gold]Draw Pile[/gold]; for each Energy, add a [gold]Void[/gold] to your [gold]Draw Pile[/gold].` |
| `.flavor` | `""`（留空） |

---

## 原初的神灵

**键名**: `TOUHOUANCIENTS-PRIMAL_SPIRIT`

### 中文原文
```json
"TOUHOUANCIENTS-PRIMAL_SPIRIT.title": "原初的神灵",
"TOUHOUANCIENTS-PRIMAL_SPIRIT.description": "在每场战斗开始时，获得[blue]{IntangibleAmount}[/blue]层[gold]无实体[/gold]。在每回合开始时，获得等同于当前回合数的{energyPrefix:energyIcons(1)}。\n[red]第[blue]{TriggerTurn}[/blue]回合开始时，获得孤注一掷。[/red]",
"TOUHOUANCIENTS-PRIMAL_SPIRIT.eventDescription": "在每场战斗开始时，获得[blue]{IntangibleAmount}[/blue]层[gold]无实体[/gold]。在每回合开始时，获得等同于当前回合数的{energyPrefix:energyIcons(1)}。[red]第[blue]{TriggerTurn}[/blue]回合开始时，获得孤注一掷。[/red]",
"TOUHOUANCIENTS-PRIMAL_SPIRIT.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Primal Spirit` |
| `.description` | `At the start of each combat, gain [blue]{IntangibleAmount}[/blue] [gold]Intangible[/gold]. At the start of each turn, gain {energyPrefix:energyIcons(1)} equal to the current turn number.\n[red]At the start of turn [blue]{TriggerTurn}[/blue], gain The Gambit.[/red]` |
| `.eventDescription` | `At the start of each combat, gain [blue]{IntangibleAmount}[/blue] [gold]Intangible[/gold]. At the start of each turn, gain {energyPrefix:energyIcons(1)} equal to the current turn number.[red]At the start of turn [blue]{TriggerTurn}[/blue], gain The Gambit.[/red]` |
| `.flavor` | `""`（留空） |

### 备注
- **孤注一掷 → The Gambit**: `THE_GAMBIT_POWER` 的英文名为 `The Gambit`。

---

## 无底之胃

**键名**: `TOUHOUANCIENTS-BOTTOMLESS_STOMACH`

### 中文原文
```json
"TOUHOUANCIENTS-BOTTOMLESS_STOMACH.title": "无底之胃",
"TOUHOUANCIENTS-BOTTOMLESS_STOMACH.description": "吞噬你初始遗物以外的全部遗物，每个为你提供[blue]{MaxHp}[/blue][gold]最大生命[/gold]，每[blue]{StrengthTrigger}[/blue]个提供[blue]{Strength}[/blue][gold]力量[/gold]、[blue]{Dexterity}[/blue][gold]敏捷[/gold]，每[blue]{EnergyTrigger}[/blue]个提供{Energy:energyIcons()}，每[blue]{DrawTrigger}[/blue]个提供每回合额外抽[blue]{CardsPerTurn}[/blue]张牌。",
"TOUHOUANCIENTS-BOTTOMLESS_STOMACH.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Bottomless Stomach` |
| `.description` | `Devour all relics other than your Starter Relic. Each grants [blue]{MaxHp}[/blue] [gold]Max HP[/gold]. Every [blue]{StrengthTrigger}[/blue] relics grant [blue]{Strength}[/blue] [gold]Strength[/gold] and [blue]{Dexterity}[/blue] [gold]Dexterity[/gold]. Every [blue]{EnergyTrigger}[/blue] relics grant {Energy:energyIcons()}. Every [blue]{DrawTrigger}[/blue] relics grant [blue]{CardsPerTurn}[/blue] additional draw each turn.` |
| `.flavor` | `""`（留空） |

### 备注
- **初始遗物 → Starter Relic**: 标准STS2术语。

---

## 暴食之齿

**键名**: `TOUHOUANCIENTS-GLUTTONOUS_FANG`

### 中文原文
```json
"TOUHOUANCIENTS-GLUTTONOUS_FANG.title": "暴食之齿",
"TOUHOUANCIENTS-GLUTTONOUS_FANG.description": "拾起时，将[blue]1[/blue]张[gold]狂飨[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-GLUTTONOUS_FANG.eventDescription": "将[blue]1[/blue]张[gold]狂飨[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-GLUTTONOUS_FANG.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Gluttonous Jaws` |
| `.description` | `Upon pickup, add [blue]1[/blue] [gold]The Feast[/gold] to your [gold]Deck[/gold].` |
| `.eventDescription` | `Add [blue]1[/blue] [gold]The Feast[/gold] to your [gold]Deck[/gold].` |
| `.flavor` | `""`（留空） |

---

## 舐血之舌

**键名**: `TOUHOUANCIENTS-BLOODLICKING_TONGUE`

### 中文原文
```json
"TOUHOUANCIENTS-BLOODLICKING_TONGUE.title": "舐血之舌",
"TOUHOUANCIENTS-BLOODLICKING_TONGUE.description": "你每失去[blue]{Threshold}[/blue]生命，就获得[blue]{MaxHp}[/blue][gold]最大生命[/gold]与[blue]{Strength}[/blue][gold]力量[/gold]。",
"TOUHOUANCIENTS-BLOODLICKING_TONGUE.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Bloodlicking Tongue` |
| `.description` | `Whenever you lose [blue]{Threshold}[/blue] HP, gain [blue]{MaxHp}[/blue] [gold]Max HP[/gold] and [blue]{Strength}[/blue] [gold]Strength[/gold].` |
| `.flavor` | `""`（留空） |

---

## 吞天之勺

**键名**: `TOUHOUANCIENTS-SKY_SWALLOWING_SPOON`

### 中文原文
```json
"TOUHOUANCIENTS-SKY_SWALLOWING_SPOON.title": "吞天之勺",
"TOUHOUANCIENTS-SKY_SWALLOWING_SPOON.description": "每当你将一张卡牌加入牌组时，其费用与[gold]辉星[/gold]消耗降至0并获得重放1，下一场战斗结束后，吞噬之并获得[blue]{MaxHp}[/blue][gold]最大生命[/gold]，若为[red]诅咒[/red]则效果翻倍。",
"TOUHOUANCIENTS-SKY_SWALLOWING_SPOON.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Sky-Swallowing Spoon` |
| `.description` | `Whenever you add a card to your [gold]Deck[/gold], set its cost and [gold]Stars[/gold] cost to [blue]0[/blue] and give it [gold]Replay[/gold] 1. After the next combat ends, devour it and gain [blue]{MaxHp}[/blue] [gold]Max HP[/gold]. If it's a [red]Curse[/red], the effect is doubled.` |
| `.flavor` | `""`（留空） |

---

## 刚欲之证

**键名**: `TOUHOUANCIENTS-RIGID_DESIRE_PROOF`

### 中文原文
```json
"TOUHOUANCIENTS-RIGID_DESIRE_PROOF.title": "刚欲之证",
"TOUHOUANCIENTS-RIGID_DESIRE_PROOF.description": "在每个回合开始时获得{Energy:energyIcons()}。你不能连续打出[blue]{TypeLimit}[/blue]张同类型牌。",
"TOUHOUANCIENTS-RIGID_DESIRE_PROOF.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Gouyoku Proof` |
| `.description` | `Gain {Energy:energyIcons()} at the start of each turn.  You cannot play [blue]{TypeLimit}[/blue] cards of the same type in a row.` |
| `.flavor` | `""`（留空） |

---

## 依神姐妹系列遗物（Yorigami Relics）

> 翻译日期：2026-07-14

### 凭依之灵
**键名**: `TOUHOUANCIENTS-POSSESSION_SPIRIT`

| 字段 | 翻译 |
|------|------|
| `.title` | `Possession Spirit` |
| `.description` | `Upon pickup, add [blue]1[/blue] [gold]Richest Form[/gold] and [gold]Poorest Form[/gold] to your [gold]Deck[/gold].` |
| `.eventDescription` | `Add [blue]1[/blue] [gold]Richest Form[/gold] and [gold]Poorest Form[/gold] to your [gold]Deck[/gold].` |
| `.flavor` | `Tonight Stars an Easygoing Egoist` |

### 百万英镑
**键名**: `TOUHOUANCIENTS-MILLION_POUNDS`

| 字段 | 翻译 |
|------|------|
| `.title` | `Million Pounds` |
| `.description` | `At the start of each combat, gain [gold]Celebrity[/gold], and add a [gold]Plague Check[/gold] into your [gold]Hand[/gold].` |
| `.flavor` | `Oh, mate. That'll cost ya.` |

### 纯金手镯
**键名**: `TOUHOUANCIENTS-PURE_GOLD_BRACELET`

| 字段 | 翻译 |
|------|------|
| `.title` | `Pure Gold Bracelet` |
| `.description` | `At the start of each turn, draw [blue]{Cards}[/blue] additional cards, then [gold]Discard[/gold] any non-X-cost cards drawn this way that cost less than [blue]{Threshold}[/blue].` |
| `.flavor` | `Money is meant to be passed from one hand to the next. Who do you think you are, trying to save it up?` |

### 香奈儿手包
**键名**: `TOUHOUANCIENTS-CHANEL_HANDBAG`

| 字段 | 翻译 |
|------|------|
| `.title` | `Chanel Handbag` |
| `.description` | `[gold]Enchant[/gold] all cards for sale in shops with [purple]Glam[/purple].` |
| `.flavor` | `Come on, give me your gold.` |

### 朱莉安娜羽扇
**键名**: `TOUHOUANCIENTS-JYOON_FAN`

| 字段 | 翻译 |
|------|------|
| `.title` | `Juliana Fan` |
| `.description` | `At the start of non-Elite combats, gain [blue]{Gold}[/blue] [gold]Gold[/gold], [blue]{Strength} Strength[/blue], [blue]{Dexterity} Dexterity[/blue], and [blue]{Focus} Focus[/blue]. This relic expires after [blue]{Charges}[/blue] non-Elite combats. Gain [blue]{Charges}[/blue] additional charges after each Elite kill.` |
| `.flavor` | `Bad day.Bad day.Bad day!` |

### 黑猫玩偶
**键名**: `TOUHOUANCIENTS-BLACK_CAT_DOLL`

| 字段 | 翻译 |
|------|------|
| `.title` | `Black Cat Doll` |
| `.description` | `At the start of each turn, gain {Energy:energyIcons()}. At the start of each turn, [blue]{Cards}[/blue] random cards in your [gold]Hand[/gold] are [gold]Afflicted with[/gold] [purple]Collateral[/purple] [blue]{CollateralNum}[/blue].` |
| `.flavor` | `If you keep resting on your laurels, you'll be penniless in no time...` |

### 石油期货
**键名**: `TOUHOUANCIENTS-OIL_FUTURES`

| 字段 | 翻译 |
|------|------|
| `.title` | `Oil Futures` |
| `.description` | `At the start of each turn, gain {Energy:energyIcons()}. Every [blue]{Turns}[/blue] turns, choose [blue]1[/blue] of [blue]{Cards}[/blue] random [red]Curses[/red] to add to your [gold]Hand[/gold].` |
| `.flavor` | `With these resources in hand, I'll become a millionaire god in no time.` |
| `.selectionScreenPrompt` | `Choose a [red]Curse[/red] to add to your Hand` |

### 烟熏的团扇
**键名**: `TOUHOUANCIENTS-SMOKED_FAN`

| 字段 | 翻译 |
|------|------|
| `.title` | `Smoked Fan` |
| `.description` | `ALL Elite enemies have [blue]1[/blue] HP. Whenever you defeat an Elite, add a [red]Guilty[/red] to your [gold]Deck[/gold].` |
| `.flavor` | `The poverty god's blessing.` |

### 烤味噌
**键名**: `TOUHOUANCIENTS-GRILLED_MISO`

| 字段 | 翻译 |
|------|------|
| `.title` | `Grilled Miso` |
| `.description` | `Upon pickup, [gold]Upgrade[/gold] [blue]{Cards}[/blue] cards. [green]Upgraded[/green] cards will no longer appear in combat rewards or shops.` |
| `.eventDescription` | `[gold]Upgrade[/gold] [blue]{Cards}[/blue] cards. [green]Upgraded[/green] cards will no longer appear in combat rewards or shops.` |
| `.flavor` | `Oh~ real grilled miso... now that's the good stuff~` |

---

## 已有遗物更新（2026-07-14）

### 冈格尼尔（描述重写）
- 新描述: `Upon pickup, add a [gold]Spear "Gungnir"[/gold] to your [gold]Deck[/gold]. Whenever you gain [gold]Block[/gold], ALL enemies gain that much [gold]Block[/gold].`
- 移除了 eventDescription
- 新风味: `Gathers aura into a spear-like projectile and hurls it. With immense piercing power, it tears through most danmaku and impales the enemy in a single strike.`

### 天壤梦弓（描述修改）
- 旧: 对最高HP敌人造成 200% 伤害 + 2 层虚弱 → 新: 对最高HP敌人额外造成 2 次伤害 + `{Weak}` 层虚弱

### 炼狱之烬（描述修改）
- 添加了 add Ethereal 效果，修复 typo "Ethernal"→"Ethereal"

### 新增风味文本
- **无底之胃**: `Whether physical or spiritual, organic or inorganic — anything and everything goes straight down her gullet.`
- **暴食之齿**: `Refuses nothing — except golden apples.`
- **舐血之舌**: `Not enough, not enough, not enough! I need more...`
- **剜天之勺**: `Just one scoop.`
- **刚欲之证**: `Might makes right. Survival of the fittest is the law of beasts.`
- **贪婪之瞳**: `A greed like no other is quite filling, you know.`
- **诅咒之血**: `The fear, joy, hatred, and lament of life — all of it is what oil, the blood of the earth, truly is.`
- **炼狱之烬**: `"(Yawn—) Says they're gonna conquer the world, yet here they are, lazing around just like me." — Chiyari Tenkajin`
- **无疚之面**: `Most members of the Gouyoku Alliance are a bunch of self-serving rogues.`

### 备注
- **刚欲同盟 → Gouyoku 东方原作专有名词**

---

## 贪婪之瞳

**键名**: `TOUHOUANCIENTS-GREEDY_EYE`

### 中文原文
```json
"TOUHOUANCIENTS-GREEDY_EYE.title": "贪婪之瞳",
"TOUHOUANCIENTS-GREEDY_EYE.description": "拾起时，将[blue]{Cards}[/blue]张带有[gold]附魔[/gold]：[purple]贪欲[/purple]的[gold]未掘宝石[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-GREEDY_EYE.eventDescription": "将[blue]{Cards}[/blue]张带有[gold]附魔[/gold]：[purple]贪欲[/purple]的[gold]未掘宝石[/gold]加入你的[gold]牌组[/gold]。",
"TOUHOUANCIENTS-GREEDY_EYE.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Greedy Eye` |
| `.description` | `Upon pickup, add [blue]{Cards}[/blue] [gold]Hidden Gem[/gold] [gold]enchanted[/gold] with [purple]Obsession[/purple] to your [gold]Deck[/gold].` |
| `.eventDescription` | `Add [blue]{Cards}[/blue] [gold]Hidden Gem[/gold] [gold]enchanted[/gold] with [purple]Obsession[/purple] to your [gold]Deck[/gold].` |
| `.flavor` | `""`（留空） |

---

## 诅咒之血

**键名**: `TOUHOUANCIENTS-CURSED_BLOOD`

### 中文原文
```json
"TOUHOUANCIENTS-CURSED_BLOOD.title": "诅咒之血",
"TOUHOUANCIENTS-CURSED_BLOOD.description": "拾起时，将一张[gold]腐朽[/gold]加入你的[gold]牌组[/gold]。在你的每个回合开始时，炼制[blue]{PotionCount}[/blue]瓶药水并喝下。",
"TOUHOUANCIENTS-CURSED_BLOOD.eventDescription": "将一张[gold]腐朽[/gold]加入你的[gold]牌组[/gold]。在你的每个回合开始时，炼制[blue]{PotionCount}[/blue]瓶药水并喝下。",
"TOUHOUANCIENTS-CURSED_BLOOD.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Cursed Blood` |
| `.description` | `Upon pickup, add a [gold]Decay[/gold] to your [gold]Deck[/gold]. At the start of each of your turns, brew [blue]{PotionCount}[/blue] potions and drink them.` |
| `.eventDescription` | `Add a [gold]Decay[/gold] to your [gold]Deck[/gold]. At the start of each of your turns, brew [blue]{PotionCount}[/blue] potions and drink them.` |
| `.flavor` | `""`（留空） |

---

## 炼狱之烬

**键名**: `TOUHOUANCIENTS-PURGATORY_EMBERS`

### 中文原文
```json
"TOUHOUANCIENTS-PURGATORY_EMBERS.title": "炼狱之烬",
"TOUHOUANCIENTS-PURGATORY_EMBERS.description": "在你的回合开始时，从[gold]消耗堆[/gold]中选择任意张牌放入[gold]手牌[/gold]，并向[gold]抽牌堆[/gold]中加入等量张[gold]灼伤[/gold]。",
"TOUHOUANCIENTS-PURGATORY_EMBERS.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Purgatory Embers` |
| `.description` | `At the start of your turn, choose any number of cards from your [gold]Exhaust Pile[/gold] to put into your [gold]Hand[/gold]. Add that many [gold]Burn[/gold] to your [gold]Draw Pile[/gold].` |
| `.flavor` | `""`（留空） |

---

## 无疚之面

**键名**: `TOUHOUANCIENTS-GUILTLESS_FACE`

### 中文原文
```json
"TOUHOUANCIENTS-GUILTLESS_FACE.title": "无疚之面",
"TOUHOUANCIENTS-GUILTLESS_FACE.description": "拾起时，选择你[gold]牌组[/gold]中的一张牌，将你[gold]牌组[/gold]中所有同类型牌变化为其。",
"TOUHOUANCIENTS-GUILTLESS_FACE.eventDescription": "选择你[gold]牌组[/gold]中的一张牌，将你[gold]牌组[/gold]中所有同类型牌变化为其。",
"TOUHOUANCIENTS-GUILTLESS_FACE.selectionScreenPrompt": "选择一张牌，将你[gold]牌组[/gold]中所有同类型牌变化为其。",
"TOUHOUANCIENTS-GUILTLESS_FACE.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Guiltless Face` |
| `.description` | `Upon pickup, choose a card in your [gold]Deck[/gold]. Transform ALL cards of the same type in your [gold]Deck[/gold] into it.` |
| `.eventDescription` | `Choose a card in your [gold]Deck[/gold]. Transform ALL cards of the same type in your [gold]Deck[/gold] into it.` |
| `.selectionScreenPrompt` | `Choose a card to Transform all cards of the same type in your Deck into it.` |
| `.flavor` | `""`（留空） |

---

## 陌路之心

**键名**: `TOUHOUANCIENTS-ESTRANGED_HEART`

### 中文原文
```json
"TOUHOUANCIENTS-ESTRANGED_HEART.title": "陌路之心",
"TOUHOUANCIENTS-ESTRANGED_HEART.description": "抽牌阶段结束后，选择任意张[gold]抽牌堆[/gold]的牌加入[gold]手牌[/gold]。累计使用此方式获得[blue]{Threshold}[/blue]张牌后，此遗物在本场战斗结束后失效。",
"TOUHOUANCIENTS-ESTRANGED_HEART.selectionScreenPrompt": "选择任意张牌加入手牌",
"TOUHOUANCIENTS-ESTRANGED_HEART.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Estranged Heart` |
| `.description` | `After the Draw Phase, choose any number of cards from your [gold]Draw Pile[/gold] to add to your [gold]Hand[/gold]. After accumulating [blue]{Threshold}[/blue] cards obtained this way, this relic expires at the end of this combat.` |
| `.selectionScreenPrompt` | `Choose any number of cards to add to your Hand` |
| `.flavor` | `""`（留空） |

---

## 激寒大地的重冰

**键名**: `TOUHOUANCIENTS-ZHIHUIJIZHONGBING`

### 中文原文
```json
"TOUHOUANCIENTS-ZHIHUIJIZHONGBING.title": "激寒大地的重冰",
"TOUHOUANCIENTS-ZHIHUIJIZHONGBING.description": "在每场战斗开始时，获得[blue]{Repeat}[/blue]个额外[gold]充能球栏位[/gold]。当你没有打出任何牌结束回合时，当回合获得[blue]{FocusPower}[/blue]临时[gold]集中[/gold]，然后[gold]生成[/gold]{IceOrbNum}个[gold]冰霜[/gold]充能球。",
"TOUHOUANCIENTS-ZHIHUIJIZHONGBING.flavor": "只会集中冰"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Subzero Glacier` |
| `.description` | `At the start of each combat, gain [blue]{Repeat}[/blue] additional [gold]Orb Slots[/gold]. When you end your turn without playing any cards, gain [blue]{FocusPower}[/blue] temporary [gold]Focus[/gold] this turn, then [gold]Channel[/gold] {IceOrbNum} [gold]Frost[/gold] Orbs.` |
| `.flavor` | `Focused entirely on frost.` |

### Neta / 文化梗说明
- **「激寒大地的重冰」** 改自宝可梦冰系 Z 招式「激狂大地万里冰」(Subzero Slammer)
- **「重冰」** → Glacier，继承原招式 Subzero 的冰系意象
- **flavor「只会集中冰」** — 双关语：
  - **集中** = The Defect 的 **Focus** 属性
  - **冰** = **Frost Orb**
  - 意为只会堆 Focus 和 Frost Orb，自嘲只会玩冰系构筑
- 英文 `"Focused entirely on frost."` 保留 Focus/Frost 双关

---

## 封魔针

**键名**: `TOUHOUANCIENTS-SEALING_NEEDLE`

### 中文原文
```json
"TOUHOUANCIENTS-SEALING_NEEDLE.title": "封魔针",
"TOUHOUANCIENTS-SEALING_NEEDLE.description": "使用攻击牌后额外给予目标[blue]1[/blue]层[gold]虚弱[/gold]。攻击牌对处于[gold]虚弱[/gold]状态的敌人额外造成[blue]等同于其当前虚弱层数[/blue]的伤害。",
"TOUHOUANCIENTS-SEALING_NEEDLE.flavor": "据说还能用于活络经血。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Sealing Needle` |
| `.description` | `Attacks deal [blue]additional[/blue] damage to [gold]Weak[/gold] enemies equal to their [gold]Weak[/gold] stacks, and apply [blue]1[/blue] [gold]Weak[/gold] to the target.` |
| `.flavor` | `It's said to also be good for promoting blood circulation.` |

### Neta / 文化梗说明
- 描述采用原版 STS2 的标准措辞模式：`Attacks deal [blue]additional[/blue] damage` → `攻击牌额外造成[blue]XX[/blue]的伤害`
- 英文 description 使用了 `Attacks deal additional damage to [gold]Weak[/gold] enemies` 句式，遵循原版遗物中同类效果的描述范式
- `ModifyDamageAdditive` 运行时返回目标当前的虚弱层数作为额外伤害值

---

## 侦探小说集

**键名**: `TOUHOUANCIENTS-DETECTIVE_STORY`

### 中文原文
```json
"TOUHOUANCIENTS-DETECTIVE_STORY.title": "侦探小说集",
"TOUHOUANCIENTS-DETECTIVE_STORY.description": "你的攻击牌对意图不是攻击的敌人额外造成[blue]{Damage}[/blue]点伤害。回合结束时，场上每有一个意图是攻击的敌人，额外保留[blue]{RetainCard}[/blue]张牌。",
"TOUHOUANCIENTS-DETECTIVE_STORY.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Detective Story Collection` |
| `.description` | `Attacks deal [blue]{Damage}[/blue] additional damage to enemies who do not intend to [gold]Attack[/gold]. At the end of your turn, [gold]Retain[/gold] [blue]{RetainCard}[/blue] additional card for each enemy who intends to [gold]Attack[/gold].` |

### 设计说明
- 标题 `Detective Story Collection` 直接对应「侦探小说集」
- 描述使用 `do not intend to Attack` / `intends to Attack` 区分敌人意图，参考原版 Go for the Eyes 的描述风格
- 数值 `{Damage}` 和 `{RetainCard}` 使用动态变量，方便以后平衡调整

---

## 绯红晶石

**键名**: `TOUHOUANCIENTS-CRIMSON_CRYSTAL`

### 中文原文
```json
"TOUHOUANCIENTS-CRIMSON_CRYSTAL.title": "绯红晶石",
"TOUHOUANCIENTS-CRIMSON_CRYSTAL.description": "在每个回合开始时，如果你的生命值不低于[blue]{HpThreshold}%[/blue]，获得{Energy:energyIcons()}。",
"TOUHOUANCIENTS-CRIMSON_CRYSTAL.flavor": "蕾米莉亚的藏品之一，拥有比世界上任何红宝石都鲜艳的血色，不过到目前为止，还没有哪个怪盗盯上过。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Crimson Crystal` |
| `.description` | `At the start of each turn, if your HP is at least [blue]{HpThreshold}%[/blue], gain {Energy:energyIcons()}.` |
| `.flavor` | `One of Remilia's treasures, with a blood-red color more vivid than any ruby in the world — though no phantom thief has ever set their sights on it.` |

### 变更记录
- **flavor 原文修正**：`"还没有哪个盯上过"` → `"还没有哪个怪盗盯上过"`，英文同步更新 `no one` → `no phantom thief`

---

## 石油期货

**键名**: `TOUHOUANCIENTS-SHION_A`

### 中文原文
```json
"TOUHOUANCIENTS-SHION_A.title": "石油期货",
"TOUHOUANCIENTS-SHION_A.description": "在每回合开始时获得{energyPrefix:energyIcons(1)}。每[blue]4[/blue]个回合，从3张[red]诅咒牌[/red]中选择一张加入抽牌堆。",
"TOUHOUANCIENTS-SHION_A.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Oil Futures` |
| `.description` | `Gain {energyPrefix:energyIcons(1)} at the start of each turn. Every [blue]4[/blue] turns, choose 1 of [blue]3[/blue] [red]Curses[/red] to add to your [gold]Draw Pile[/gold].` |
| `.flavor` | `""`（留空） |

### 备注
- 英文翻译已完成，待同步写入 eng/jpn 文件

---

## 黑猫玩偶

**键名**: `TOUHOUANCIENTS-SHION_B`

### 中文原文
```json
"TOUHOUANCIENTS-SHION_B.title": "黑猫玩偶",
"TOUHOUANCIENTS-SHION_B.description": "在每回合开始时获得{energyPrefix:energyIcons(1)}。战斗开始时，随机[blue]8[/blue]张卡牌被侵蚀为[purple]抵押物[/purple]。",
"TOUHOUANCIENTS-SHION_B.flavor": "",
"TOUHOUANCIENTS-SHION_B.test1": "抵押物",
"TOUHOUANCIENTS-SHION_B.test2": "获得[gold]保留[/gold]与[gold]无法被打出[/gold]。保留[blue]2[/blue]回合后移除此效果。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Black Cat Doll` |
| `.description` | `Gain {energyPrefix:energyIcons(1)} at the start of each turn. At the start of combat, [blue]8[/blue] random cards are corrupted into [purple]Collateral[/purple].` |
| `.flavor` | `""`（留空） |
| `.test1` | `Collateral` |
| `.test2` | `Gain [gold]Retain[/gold] and [gold]Unplayable[/gold]. Remove this effect after [blue]2[/blue] turns of being retained.` |

### 备注
- **抵押物 → Collateral**: 自定义关键字，抵押物的英文译名
- 英文翻译已完成，待同步写入 eng/jpn 文件
- `test1`/`test2` 字段为 Power 名称与描述键，用于悬浮提示

---

## 至尊金箍

**键名**: `TOUHOUANCIENTS-SUPREME_HEAVEN_SEAL`

### 中文原文
```json
"TOUHOUANCIENTS-SUPREME_HEAVEN_SEAL.title": "至尊金箍",
"TOUHOUANCIENTS-SUPREME_HEAVEN_SEAL.description": "拾起时，选择一张升级后的[gold]先古之民卡牌[/gold]加入牌组。不再掉落[gold]卡牌奖励[/gold]。",
"TOUHOUANCIENTS-SUPREME_HEAVEN_SEAL.eventDescription": "选择一张升级后的[gold]先古之民卡牌[/gold]加入牌组。不再掉落[gold]卡牌奖励[/gold]。",
"TOUHOUANCIENTS-SUPREME_HEAVEN_SEAL.flavor": "",
"TOUHOUANCIENTS-SUPREME_HEAVEN_SEAL.selectionScreenPrompt": "选择一张升级后的[gold]先古之民卡牌[/gold]加入牌组"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Supreme Golden Circlet` |
| `.description` | `Upon pickup, choose an [green]Upgraded[/green] Ancient card and add it to your [gold]Deck[/gold]. [gold]Card Rewards[/gold] no longer drop.` |
| `.eventDescription` | `Choose an [green]Upgraded[/green] Ancient card and add it to your [gold]Deck[/gold]. [gold]Card Rewards[/gold] no longer drop.` |
| `.flavor` | `""`（留空） |
| `.selectionScreenPrompt` | `Choose an [green]Upgraded[/green] Ancient card to add to your [gold]Deck[/gold]` |

---

## 破财消灾

**键名**: `TOUHOUANCIENTS-LOSE_MONEY`

### 中文原文
```json
"TOUHOUANCIENTS-LOSE_MONEY.title": "破财消灾",
"TOUHOUANCIENTS-LOSE_MONEY.description": "每当你打出[blue]{CardsNeeded}[/blue]张牌，消耗[blue]{GoldCost}[/blue][gold]金币[/gold]获得{Energy:energyIcons()}。",
"TOUHOUANCIENTS-LOSE_MONEY.flavor": "紫苑女苑遗物测试1."
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Gold for Luck` |
| `.description` | `Whenever you play [blue]{CardsNeeded}[/blue] cards, spend [blue]{GoldCost}[/blue] [gold]Gold[/gold] to gain {Energy:energyIcons()}.` |
| `.flavor` | `Shion & jyoon relic test 1.` |

---

## 可疑信物

**键名**: `TOUHOUANCIENTS-SUSPICIOUS_TOKEN`

### 中文原文（已更新）
```json
"TOUHOUANCIENTS-SUSPICIOUS_TOKEN.title": "可疑信物",
"TOUHOUANCIENTS-SUSPICIOUS_TOKEN.description": "在每个商店，你可以免费刷新一次[gold]商人[/gold]出售的物品，刷新后打折[blue]{Discount}%[/blue]！",
"TOUHOUANCIENTS-SUSPICIOUS_TOKEN.flavor": "把这个交给商人就能换取特别服务——当你觉得被因幡帝骗了时，实际没有被骗，这算不算一种被骗呢？"
```

### 英文翻译（已更新）
| 字段 | 翻译 |
|------|------|
| `.title` | `Suspicious Token` |
| `.description` | `At each shop, you may refresh the [gold]Merchant's[/gold] wares once for free. Afterwards, all items are [blue]{Discount}% off[/blue]!` |
| `.flavor` | `Hand this to the merchant for a special service—when you think Tewi Inaba has tricked you, but actually hasn't, does that still count as being tricked?` |

### Neta / 文化梗说明
- **因幡帝（Tewi Inaba）** — 以狡诈、捉弄人著称的妖怪兔，这个"信物"是否真的能换取服务，或者只是她的又一个恶作剧？
- **"可疑"** — 文本玩了一个哲学悖论：如果你觉得被骗了但实际上没被骗，这算不算被骗？

---

## 发光竹子

**键名**: `TOUHOUANCIENTS-GLOWING_BAMBOO`

### 中文原文
```json
"TOUHOUANCIENTS-GLOWING_BAMBOO.title": "发光竹子",
"TOUHOUANCIENTS-GLOWING_BAMBOO.description": "每场战斗你打出的首张未被升级的牌将在战斗结束后被升级。",
"TOUHOUANCIENTS-GLOWING_BAMBOO.flavor": "自然现象？……还是月球科技？嘘！这可是商业机密。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Glowing Bamboo` |
| `.description` | `The first unupgraded card you play each combat will be upgraded after the combat ends.` |
| `.flavor` | `A natural phenomenon... or lunar technology? Shh! That's a trade secret.` |

### Neta / 文化梗说明
- **月球科技** — 捏他永夜抄的因幡竹（月兔/月球科技）背景，暗示发光竹子可能是月之都的技术产物
- **商业机密** — 俏皮地拒绝透露竹子发光的真正原因

---

## 微缩银河

**键名**: `TOUHOUANCIENTS-BOTTLED_GALAXY`

### 中文原文
```json
"TOUHOUANCIENTS-BOTTLED_GALAXY.title": "微缩银河",
"TOUHOUANCIENTS-BOTTLED_GALAXY.description": "拾起时，从你的[gold]牌组[/gold]中选择[blue]{Cards}[/blue]张牌移除。在每场战斗的前三回合，每回合开始时选择其中一张加入手牌。{CardTitles.StringValue:cond:\n\n微缩：\n{}|}",
"TOUHOUANCIENTS-BOTTLED_GALAXY.eventDescription": "从你的[gold]牌组[/gold]中选择[blue]{Cards}[/blue]张牌移除。在每场战斗的前三回合，每回合开始时选择其中一张加入手牌。{CardTitles.StringValue:cond:\n\n微缩：\n{}|}",
"TOUHOUANCIENTS-BOTTLED_GALAXY.flavor": "星之沙是如此绚丽多彩。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Miniature Milky Way` |
| `.description` | `Upon pickup, remove [blue]{Cards}[/blue] cards from your [gold]Deck[/gold]. For the first 3 turns of each combat, at the start of your turn, choose one to add to your [gold]Hand[/gold].{CardTitles.StringValue:cond:\n\nMiniaturized:\n{}|}` |
| `.eventDescription` | `Remove [blue]{Cards}[/blue] cards from your [gold]Deck[/gold]. For the first 3 turns of each combat, at the start of your turn, choose one to add to your [gold]Hand[/gold].{CardTitles.StringValue:cond:\n\nMiniaturized:\n{}|}` |
| `.flavor` | `The stardust is so colorful.` |

### 日文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Miniature Milky Way` |
| `.description` | `Upon pickup, remove [blue]{Cards}[/blue] cards from your [gold]Deck[/gold]. For the first 3 turns of each combat, at the start of your turn, choose one to add to your [gold]Hand[/gold].{CardTitles.StringValue:cond:\n\nMiniaturized:\n{}|}` |
| `.eventDescription` | `Remove [blue]{Cards}[/blue] cards from your [gold]Deck[/gold]. For the first 3 turns of each combat, at the start of your turn, choose one to add to your [gold]Hand[/gold].{CardTitles.StringValue:cond:\n\nMiniaturized:\n{}|}` |
| `.flavor` | `The stardust is so colorful.` |

### Neta / 文化梗说明
- **效果说明** — 重做前为「抽牌阶段结束后根据手牌类型从抽/弃牌堆补牌」；重做后改为「拾起时移除牌、战斗中前三回合选择一张加入手牌」，参照佩尔之牙的模式
- **星之沙** — 保留原有的 flavor 文本

---

## 天壤梦弓

**键名**: `TOUHOUANCIENTS-DREAM_HEAVEN_BOW`

### 中文原文
```json
"TOUHOUANCIENTS-DREAM_HEAVEN_BOW.title": "天壤梦弓",
"TOUHOUANCIENTS-DREAM_HEAVEN_BOW.description": "在每场战斗开始时，对所有敌人造成[blue]{Damage}[/blue]点伤害，对生命值最高的敌人额外造成[blue]200%[/blue]伤害并给予[blue]2[/blue]层[gold]虚弱[/gold]。",
"TOUHOUANCIENTS-DREAM_HEAVEN_BOW.flavor": "可惜不能附魔"无限"或者"火矢"……"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Dream Heaven Bow` |
| `.description` | `At the start of each combat, deal [blue]{Damage}[/blue] damage to ALL enemies. Deal an additional [blue]200%[/blue] damage to the enemy with the highest HP and apply [blue]2[/blue] [gold]Weak[/gold].` |
| `.flavor` | `Too bad you can't enchant it with "Infinity" or "Flame"…` |

### Neta / 文化梗说明
- **「可惜不能附魔"无限"或者"火矢"」** — 这是 Minecraft 梗。"无限"(Infinity)和"火矢"(Flame)是 Minecraft 中弓的附魔。这里调侃该遗物虽然是一把弓，但没法打上 Minecraft 的附魔。

---


---

## 龙颈之玉（2026-07-19 更新）

**键名**: `TOUHOUANCIENTS-RYUKEI_NO_TAMA`

### 中文原文（更新后）
```json
"TOUHOUANCIENTS-RYUKEI_NO_TAMA.title": "龙颈之玉",
"TOUHOUANCIENTS-RYUKEI_NO_TAMA.description": "在每场战斗开始时，将一张[gold]七星+[/gold]加入你的[gold]手牌[/gold]，其拥有[gold]保留[/gold]。在你的回合结束时，或你每打出[blue]{Card}[/blue]张牌，其在本场战斗中的[gold]辉星[/gold]费用减少[blue]1[/blue]。",
"TOUHOUANCIENTS-RYUKEI_NO_TAMA.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Jewel from the Dragon's Neck` |
| `.description` | `At the start of each combat, add a [gold]Seven Stars+[/gold] to your [gold]Hand[/gold]. It has [gold]Retain[/gold]. At the end of your turn, or every [blue]{Card}[/blue] cards you play, reduce its [gold]Stars[/gold] cost by [blue]1[/blue] this combat.` |
| `.flavor` | `""`（留空） |

---

## 百万英镑（2026-07-19 更新）

**键名**: `TOUHOUANCIENTS-MILLION_POUNDS`

### 中文原文（更新后）
```json
"TOUHOUANCIENTS-MILLION_POUNDS.title": "百万英镑",
"TOUHOUANCIENTS-MILLION_POUNDS.description": "在每场战斗开始时，将一张[gold]疫病支票[/gold]加入你的[gold]手牌[/gold]。",
"TOUHOUANCIENTS-MILLION_POUNDS.flavor": "哦~伙计，这得花不少钱。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Million Pounds` |
| `.description` | `At the start of each combat, add a [gold]Plague Check[/gold] to your [gold]Hand[/gold].` |
| `.flavor` | `Oh, mate. That'll cost ya.` |

---

## 石油期货（2026-07-19 更新）

**键名**: `TOUHOUANCIENTS-OIL_FUTURES`

### 中文原文（更新后）
```json
"TOUHOUANCIENTS-OIL_FUTURES.flavor": "只要手握这些资源，就能变成富豪神了呢（哪怕是知道那石油的真相……也无所谓吗？）"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.flavor` | `With these resources in hand, I'll become a millionaire god in no time. (Even knowing the truth about that oil... does it matter?)` |

---

## 冈格尼尔（2026-07-19 更新）

**键名**: `TOUHOUANCIENTS-SPEAR_GUNGNIR`

### 中文原文（更新后）
```json
"TOUHOUANCIENTS-SPEAR_GUNGNIR.title": "冈格尼尔",
"TOUHOUANCIENTS-SPEAR_GUNGNIR.description": "拾起时，将一张[gold]神枪「冈格尼尔」[/gold]加入你的[gold]牌组[/gold]。\n你使用[gold]神枪「冈格尼尔」[/gold]以外的卡牌对敌人造成伤害/获得格挡时，为目标/所有敌人附加等量格挡。",
"TOUHOUANCIENTS-SPEAR_GUNGNIR.eventDescription": "将一张[gold]神枪「冈格尼尔」[/gold]加入你的[gold]牌组[/gold]。你使用[gold]神枪「冈格尼尔」[/gold]以外的卡牌对敌人造成伤害/获得格挡时，为目标/所有敌人附加等量格挡。",
"TOUHOUANCIENTS-SPEAR_GUNGNIR.flavor": "将灵气收束成枪状投掷而出。贯通力很高，碰到弹幕大都都会毫无阻碍的连同敌人本体一起贯穿。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Gungnir` |
| `.description` | `Upon pickup, add a [gold]Spear "Gungnir"[/gold] to your [gold]Deck[/gold].\nWhen you deal damage or gain [gold]Block[/gold] with cards other than [gold]Spear "Gungnir"[/gold], apply equal [gold]Block[/gold] to the target/ALL enemies.` |
| `.eventDescription` | `Add a [gold]Spear "Gungnir"[/gold] to your [gold]Deck[/gold]. When you deal damage or gain [gold]Block[/gold] with cards other than [gold]Spear "Gungnir"[/gold], apply equal [gold]Block[/gold] to the target/ALL enemies.` |
| `.flavor` | `Gathers aura into a spear-like projectile and hurls it. With immense piercing power, it tears through most danmaku and impales the enemy in a single strike.` |

---

## 可靠的弟子狸（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-RELIABLE_DISCIPLE_TANUKI`

### 中文原文
```json
"TOUHOUANCIENTS-RELIABLE_DISCIPLE_TANUKI.title": "可靠的弟子狸",
"TOUHOUANCIENTS-RELIABLE_DISCIPLE_TANUKI.description": "在你的回合开始时，随机将一张[gold]仆从俯冲[/gold]、[gold]仆从打击[/gold]、[gold]仆从防御[/gold]加入你的[gold]手牌[/gold]。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Reliable Tanuki Disciple` |
| `.description` | `At the start of your turn, add a random [gold]Minion Dive[/gold], [gold]Minion Strike[/gold], or [gold]Minion Defend[/gold] to your [gold]Hand[/gold].` |

---

## 赤云指虎（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-CRIMSON_CLOUD_KNUCKLE`

### 中文原文
```json
"TOUHOUANCIENTS-CRIMSON_CLOUD_KNUCKLE.title": "赤云指虎",
"TOUHOUANCIENTS-CRIMSON_CLOUD_KNUCKLE.description": "当你不以自动打出的方式打出[gold]攻击牌[/gold]时，随机打出[gold]手牌[/gold]中另一张[gold]攻击牌[/gold]。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Crimson Cloud Knuckle` |
| `.description` | `When you play an [gold]Attack[/gold] without auto-playing it, play a random other [gold]Attack[/gold] from your [gold]Hand[/gold].` |

---

## 沉锚幽灵（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-ANCHORED_GHOST`

### 中文原文
```json
"TOUHOUANCIENTS-ANCHORED_GHOST.title": "沉锚幽灵",
"TOUHOUANCIENTS-ANCHORED_GHOST.description": "回合开始时获得[blue]1[/blue]能量。你的[gold]能力牌[/gold]在战斗开始时被置于牌堆底。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Anchored Ghost` |
| `.description` | `At the start of your turn, gain [blue]1[/blue] Energy. At the start of each combat, your [gold]Power[/gold] cards are placed at the bottom of your [gold]Draw Pile[/gold].` |

---

## 邪仙发簪（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-HERMIT_HAIRPIN`

### 中文原文
```json
"TOUHOUANCIENTS-HERMIT_HAIRPIN.title": "邪仙发簪",
"TOUHOUANCIENTS-HERMIT_HAIRPIN.description": "每场战斗你首次死亡时，恢复[blue]50%[/blue]生命值，之后的回合由霍青娥接管。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Hermit's Hairpin` |
| `.description` | `The first time you would die each combat, heal [blue]50%[/blue] of your Max HP. For the rest of combat, Seiga Kaku takes over.` |

---

## 鲵吞亭特调（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-GEIDONTEI_SPECIAL`

### 中文原文
```json
"TOUHOUANCIENTS-GEIDONTEI_SPECIAL.title": "鲵吞亭特调",
"TOUHOUANCIENTS-GEIDONTEI_SPECIAL.description": "获得[blue]2[/blue]个药水栏位，你获得的药水变成[gold]超ZUN啤酒[/gold]。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Geidontei Special` |
| `.description` | `Gain [blue]2[/blue] Potion Slots. Potions you obtain become [gold]Super ZUN Beer[/gold].` |

---

## 寻龙尺（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-DRAGON_DOWSING_ROD`

### 中文原文
```json
"TOUHOUANCIENTS-DRAGON_DOWSING_ROD.title": "寻龙尺",
"TOUHOUANCIENTS-DRAGON_DOWSING_ROD.description": "你没有选择的[gold]卡牌奖励[/gold]将会被记录。你可以在[gold]休息处[/gold]雇佣纳兹琳寻宝。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Dragon Dowsing Rod` |
| `.description` | `[gold]Card Rewards[/gold] you skip are recorded. You may hire Nazrin to seek treasure at [gold]Rest Sites[/gold].` |

---

## 龙脉之皿（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-DRAGON_VEIN_VESSEL`

### 中文原文
```json
"TOUHOUANCIENTS-DRAGON_VEIN_VESSEL.title": "龙脉之皿",
"TOUHOUANCIENTS-DRAGON_VEIN_VESSEL.description": "标记一条路线（不包括boss房间）。进入路线上的节点获得[blue]33[/blue][gold]金币[/gold]，如果是战斗则战斗开始时获得[blue]2[/blue][gold]力量[/gold][blue]2[/blue][gold]敏捷[/gold]，且额外掉落一组[gold]卡牌奖励[/gold]。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Dragon Vein Vessel` |
| `.description` | `Mark a path (excluding Boss rooms). When you enter a node on that path, gain [blue]33[/blue] [gold]Gold[/gold]. If it is a combat, at the start of combat gain [blue]2[/blue] [gold]Strength[/gold] and [blue]2[/blue] [gold]Dexterity[/gold], and an additional [gold]Card Reward[/gold] drops.` |

---

## 幻灭三叉戟（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-DISILLUSION_TRIDENT`

### 中文原文
```json
"TOUHOUANCIENTS-DISILLUSION_TRIDENT.title": "幻灭三叉戟",
"TOUHOUANCIENTS-DISILLUSION_TRIDENT.description": "将一张[gold]未名妖魔[/gold]加入[gold]牌组[/gold]。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Trident of Disillusion` |
| `.description` | `Add an [gold]Unnamed Demon[/gold] to your [gold]Deck[/gold].` |

---

## 独眼唐伞（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-ONE_EYED_KARAKASA`

### 中文原文
```json
"TOUHOUANCIENTS-ONE_EYED_KARAKASA.title": "独眼唐伞",
"TOUHOUANCIENTS-ONE_EYED_KARAKASA.description": "每当你斩杀一名不是[gold]爪牙[/gold]的敌人时，获得[blue]1[/blue]计数，如果此时为战斗的第一回合则改为[blue]3[/blue]。你可以在[gold]休息处[/gold]花费[blue]4[/blue]计数额外[green]升级[/green]一张牌。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `One-Eyed Karakasa` |
| `.description` | `Whenever you kill a non-[gold]Minion[/gold] enemy, gain [blue]1[/blue] Count (or [blue]3[/blue] if it is the first turn of combat). At [gold]Rest Sites[/gold], you may spend [blue]4[/blue] Count to additionally [green]Upgrade[/green] a card.` |

---

## 百鬼夜行绘卷（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-NIGHT_PARADE_SCROLL`

### 中文原文
```json
"TOUHOUANCIENTS-NIGHT_PARADE_SCROLL.title": "百鬼夜行绘卷",
"TOUHOUANCIENTS-NIGHT_PARADE_SCROLL.description": "查看来自[gold]铁甲战士[/gold]、[gold]静默猎者[/gold]、[gold]储君[/gold]、[gold]亡灵契约师[/gold]、[gold]故障机器人[/gold]的各一组[gold]卡牌奖励[/gold]。将选择的[gold]卡牌奖励[/gold]合成为卡牌：[gold]百鬼夜行[/gold]加入你的[gold]牌组[/gold]。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Night Parade Scroll` |
| `.description` | `View one [gold]Card Reward[/gold] from each of [gold]Ironclad[/gold], [gold]Silent[/gold], [gold]Heir[/gold], [gold]Necromancer[/gold], and [gold]Defect[/gold]. Combine the chosen rewards into a [gold]Night Parade[/gold] card and add it to your [gold]Deck[/gold].` |

---

## 超能念力（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-PSYCHIC_POWER`

### 中文原文
```json
"TOUHOUANCIENTS-PSYCHIC_POWER.title": "超能念力",
"TOUHOUANCIENTS-PSYCHIC_POWER.description": "每当你抽到[gold]能力牌[/gold]时，额外抽一张牌。每回合首次打出[gold]能力牌[/gold]时，将一张该牌的复制品加入[gold]弃牌堆[/gold]。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Psychic Power` |
| `.description` | `Whenever you draw a [gold]Power[/gold] card, draw [blue]1[/blue] additional card. The first time you play a [gold]Power[/gold] each turn, add a copy of it to your [gold]Discard Pile[/gold].` |

---

## 能乐假面（2026-07-21 新增）

**键名**: `TOUHOUANCIENTS-NOH_MASK`

### 中文原文
```json
"TOUHOUANCIENTS-NOH_MASK.title": "能乐假面",
"TOUHOUANCIENTS-NOH_MASK.description": "每回合开始时随机将四张[gold]手牌[/gold]在本回合添加喜、怒、哀、乐情绪。当你打出喜、怒、哀、乐情绪各一张后，恢复[blue]1[/blue]能量并抽一张牌。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Noh Mask` |
| `.description` | `At the start of each turn, random [blue]4[/blue] cards in your [gold]Hand[/gold] gain either Joy, Anger, Sorrow, or Pleasure emotion this turn. After you play one of each emotion, regain [blue]1[/blue] Energy and draw [blue]1[/blue] card.` |

---

## 2026-08-03 增量更新（53adedaf 之后）

### 烤味噌（描述修改）
- zhs 改为"战斗奖励中的卡牌不再出现升级后的"（删去"与商店"）
- `.description` / `.eventDescription` → `[green]Upgraded[/green] cards will no longer appear in combat rewards.`

### 邪仙发簪（新增 seiga 字段）
| 字段 | 翻译 |
|------|------|
| `.seigatitle` | `Seiga Kaku` |
| `.seigadescription` | `After Seiga Kaku takes over, lose [blue]6[/blue] HP and gain {energyPrefix:energyIcons(1)} at the start of your turn. She prefers higher-cost cards.` |

### 新增遗物 flavor 补译
| 遗物 | 中文 flavor | 英文 flavor |
|------|------------|------------|
| `CRIMSON_CLOUD_KNUCKLES` | 拥有精密的动作和强大的力量程度的能力。 | `The ability to have precise movements and overwhelming strength.` |
| `SUNKEN_ANCHOR_GHOST` | 无论如何都会沉船程度的能力。 | `The ability to make any ship sink no matter what.` |
| `WICKED_HERMIT_HAIRPIN` | 让僵尸出现在你草坪上程度的能力。 | `The ability to make zombies appear on your lawn.` |
| `NIDOMITEI_SPECIAL_BLEND` | 喝完酒变勇程度的能力。 | `The ability to grow braver after a drink.` |
| `DOWSING_ROD` | 不管怎样都能找回宝塔程度的能力。 | `The ability to find her pagoda no matter what.` |
| `DRAGON_VEIN_VESSEL` | 识别风水并点燃程度的能力。 | `The ability to read feng shui and ignite it.` |
| `ONE_EYED_KARAKASA` | 吓人让人人见人爱程度的能力。 | `The ability to scare people yet still be adored by everyone.` |
| `PSYCHIC_TELEKINESIS` | 将JK作为种族程度的能力。 | `The ability to treat JK as a species.` |

### 幻灭三叉戟（flavor 乱码）
- zhs 中 flavor 为故意乱码（编码损坏文本），用户确认保留乱码的错误感，仅将"鵺"替换为 "nue"：
  `è°�æ˜¯å°nueå…½éµºï¼Ÿæ˜¯ä½ ï¼Ÿè¿˜æ˜¯æˆ'ï¼Ÿ`

### 能乐假面（描述更新，zhs 大改后重译）
- zhs 改为："战斗开始时或将牌堆洗牌时，为所有卡牌随机添加喜怒哀乐情绪。每当你打出喜怒哀乐情绪各{EmotionThreshold}张后，恢复{Energy:energyIcons()}并抽{Cards}张牌。"
- `.description` → `At the start of combat or whenever you shuffle your [gold]Draw Pile[/gold], randomly add [purple]Joy[/purple], [purple]Anger[/purple], [purple]Sorrow[/purple], or [purple]Pleasure[/purple] to all cards. After playing [blue]{EmotionThreshold}[/blue] of each emotion, gain {Energy:energyIcons()} and draw [blue]{Cards}[/blue] cards.`

### 依神姐妹战斗相关（新增文件 monsters.json / encounters.json）
- 依神女苑/依神紫苑怪物名与招式：`Yorigami Joon` / `Yorigami Shion`、`Bubble Queen`、`Golden Tornado`、`Scatter Wealth Uppercut`、`Celebrity Burn`、`Doom Spread`、`Feather Plucking`、`Absolute Loser`、`Endless Poverty`、`Stunned`
- 遭遇战：`Yorigami Sisters`；失败文本 → `{character}'s wealth and fortune were squandered away by [gold]{encounter}[/gold].`

### 双生 / 讨债人（powers.json 新增）
| 键 | 翻译 |
|----|------|
| `TWIN_SOUL_POWER.title` | `Twin Soul` |
| `TWIN_SOUL_POWER.description` | `Grant [gold]Joon[/gold] [blue]8[/blue] [gold]Block[/gold] each turn.\nWhen this unit dies, it revives in [blue]2[/blue] turns with [blue]50[/blue] HP.\nWhen Joon is defeated, this unit becomes [gold]Stunned[/gold] and this power is removed.` |
| `DEBT_COLLECTOR_POWER.title` | `Debt Collector` |
| `DEBT_COLLECTOR_POWER.description` | `Time to pay up. The next hit removes [blue]50%[/blue] of the target's [gold]Royalties[/gold] and deals equal additional damage.` |

### 未名妖魔 / 百鬼夜行（cards.json 新增）
| 键 | 翻译 |
|----|------|
| `NAMELESS_YOUKAI.title` | `Nameless Demon`（与幻灭三叉戟引用一致） |
| `NAMELESS_YOUKAI.description` | `Add a copy of this card to your [gold]Draw Pile[/gold] with its cost randomized.\nTransform into the last card you played and return it to your [gold]Hand[/gold].` |
| `HYAKKI_YAGYO.title` | `Hyakki Yagyo`（与百鬼夜行绘卷引用一致） |
| `HYAKKI_YAGYO.description` | `Play [blue]2[/blue] random recorded cards, then remove them from the record this combat.\nOnce all records are removed, remove this card from combat.` |

### 狂飨（cards.json 追加台词）
- `.description` 追加 `\n[jitter][i]All shall become my food!![/i][/jitter]`

### 寻宝（rest_site_ui.json 新增）
| 字段 | 翻译 |
|------|------|
| `OPTION_TREASURE.name` | `Treasure Hunt` |
| `OPTION_TREASURE.description` | `Choose any number of recorded cards to add to your [gold]Deck[/gold]. There's also a chance to dig up some [jitter][gold]treasures[/gold][/jitter]!` |

---

## 亡灵提灯（重做：附魔付丧之力X）

**键名**: `TOUHOUANCIENTS-GHOST_LANTERN`

### 中文原文
```json
"TOUHOUANCIENTS-GHOST_LANTERN.title": "亡灵提灯",
"TOUHOUANCIENTS-GHOST_LANTERN.description": "拾起时，从[blue]{Amount}[/blue]张[gold]无色牌[/gold]中选择至多[blue]{SelectNum}[/blue]张加入牌组，为这些牌[gold]附魔[/gold]：[purple]{EnchantmentName}{EnchantAmount}[/purple]。",
"TOUHOUANCIENTS-GHOST_LANTERN.eventDescription": "从[blue]{Amount}[/blue]张[gold]无色牌[/gold]中选择至多[blue]{SelectNum}[/blue]张加入牌组，为这些牌[gold]附魔[/gold]：[purple]{EnchantmentName}{EnchantAmount}[/purple]。",
"TOUHOUANCIENTS-GHOST_LANTERN.selectionScreenPrompt": "选择至多[blue]{SelectNum}[/blue]张加入牌组",
"TOUHOUANCIENTS-GHOST_LANTERN.flavor": "寄宿其中的灵魂在[jitter]躁动[/jitter]，幽灵在各个方面都能胜过肉体，啊，我不是说某个角色。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Ghost Lantern` |
| `.description` | `Upon pickup, choose up to [blue]{SelectNum}[/blue] of [blue]{Amount}[/blue] [gold]Colorless[/gold] cards to add to your [gold]Deck[/gold], [gold]enchanted[/gold] with [purple]{EnchantmentName} {EnchantAmount}[/purple].` |
| `.eventDescription` | `Choose up to [blue]{SelectNum}[/blue] of [blue]{Amount}[/blue] [gold]Colorless[/gold] cards to add to your [gold]Deck[/gold], [gold]enchanted[/gold] with [purple]{EnchantmentName} {EnchantAmount}[/purple].` |
| `.selectionScreenPrompt` | `Choose up to [blue]{SelectNum}[/blue] cards to add to your [gold]Deck[/gold].` |
| `.flavor` | `The souls dwelling within [jitter]stir[/jitter]. In every way, a ghost surpasses the physical body... no, I'm not referring to anyone in particular.` |

### 关联附魔（重做）
**键名**: `TOUHOUANCIENTS-TSUKUMOGAMI`（付丧之力X）

### 中文原文
```json
"TOUHOUANCIENTS-TSUKUMOGAMI.description": "获得[gold]虚无[/gold]。\n[blue]{Amount}[/blue]回合后，无论何处，将这张牌打出。\n在你的回合结束时，如果这张牌在本回合没有被打出，计数-1。",
"TOUHOUANCIENTS-TSUKUMOGAMI.title": "付丧之力"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Tsukumogami` |
| `.description` | `Gains [gold]Ethereal[/gold].\nIn [blue]{Amount}[/blue] turns, play this card from anywhere.\nAt the end of your turn, if this card was not played this turn, reduce the count by 1.` |

### 说明
- 附魔重做为"付丧之力X"：`ShowAmount` 显示计数，`IsStackable` 可叠加
- 遗物附魔时使用 `EnchantAmount = 5`，即"付丧之力5"
- 附魔名与数字拼接：中文无空格（付丧之力5），英文有空格（Tsukumogami 5），参照蛊毒魔盒（Toxic 3）风格


