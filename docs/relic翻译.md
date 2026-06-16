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
"TOUHOUANCIENTS-YIYANDINGZHEN.description": "每场战斗开始时获得[red]混乱[/red]效果。在你的回合开始时，选择一张卡，其在本回合可以免费打出。",
"TOUHOUANCIENTS-YIYANDINGZHEN.flavor": "异眼顶真，鉴定为大费物。",
"TOUHOUANCIENTS-YIYANDINGZHEN.selectionScreenPrompt": "选择一张牌使其在本回合可以免费打出。"
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Snecko's True Gaze` |
| `.description` | `At the start of each combat, gain [red]Confusion[/red]. At the start of your turn, choose a card. It is free to play this turn.` |
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
"TOUHOUANCIENTS-HAPPINESS_ELIXIR.description": "拾起时，获得一瓶[green]卡米莉亚[/green]。第二幕的[gold]Boss[/gold]战结束后额外掉落一瓶。",
"TOUHOUANCIENTS-HAPPINESS_ELIXIR.eventDescription": "获得一瓶[green]卡米莉亚[/green]。第二幕的[gold]Boss[/gold]战结束后额外掉落一瓶。",
"TOUHOUANCIENTS-HAPPINESS_ELIXIR.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Happiness Elixir` |
| `.description` | `Upon pickup, obtain a [green]Camellia[/green]. After defeating the Act [blue]2[/blue] [gold]Boss[/gold], an extra [green]Camellia[/green] drops.` |
| `.eventDescription` | `Obtain a [green]Camellia[/green]. After defeating the Act [blue]2[/blue] [gold]Boss[/gold], an extra [green]Camellia[/green] drops.` |
| `.flavor` | `""`（留空） |

---

## 丝带蝴蝶结

**键名**: `TOUHOUANCIENTS-RIBBON_BOW`

### 中文原文
```json
"TOUHOUANCIENTS-RIBBON_BOW.title": "丝带蝴蝶结",
"TOUHOUANCIENTS-RIBBON_BOW.description": "拾起时，从你的[gold]牌组[/gold]中选择[blue]{SelectCount}[/blue]张攻击牌或技能牌，[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-RIBBON_BOW.eventDescription": "从你的[gold]牌组[/gold]中选择[blue]{SelectCount}[/blue]张攻击牌或技能牌，[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。",
"TOUHOUANCIENTS-RIBBON_BOW.selectionScreenPrompt": "选择[blue]{SelectCount}[/blue]张牌进行[gold]附魔[/gold]",
"TOUHOUANCIENTS-RIBBON_BOW.flavor": ""
```

### 英文翻译
| 字段 | 翻译 |
|------|------|
| `.title` | `Ribbon Bow` |
| `.description` | `Upon pickup, choose [blue]{SelectCount}[/blue] Attacks or Skills from your [gold]Deck[/gold] and [gold]enchant[/gold] them with [purple]{EnchantmentName}[/purple].` |
| `.eventDescription` | `Choose [blue]{SelectCount}[/blue] Attacks or Skills from your [gold]Deck[/gold] and [gold]enchant[/gold] them with [purple]{EnchantmentName}[/purple].` |
| `.selectionScreenPrompt` | `Choose [blue]{SelectCount}[/blue] cards to [gold]enchant[/gold]` |
| `.flavor` | `""`（留空） |

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
| `.description` | `Upon pickup, gain [blue]2[/blue] [gold]Potion Slots[/gold]. You may brew potions at [gold]Rest Sites[/gold].` |
| `.eventDescription` | `Gain [blue]2[/blue] [gold]Potion Slots[/gold]. You may brew potions at [gold]Rest Sites[/gold].` |
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
| `.description` | `At the start of each combat, gain [red]Confusion[/red]. Cards costing [blue]{CostTwo}[/blue] are played [blue]{CostTwo}[/blue] times, and cards costing [blue]{CostThree}[/blue] or more are played [blue]{CostThree}[/blue] times.` |
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
| `.description` | `Upon pickup, choose a card in your [gold]Deck[/gold]. Transform all cards of the same type in your [gold]Deck[/gold] into it.` |
| `.eventDescription` | `Choose a card in your [gold]Deck[/gold]. Transform all cards of the same type in your [gold]Deck[/gold] into it.` |
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

