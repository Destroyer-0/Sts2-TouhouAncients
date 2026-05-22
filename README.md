# Sts2-TouhouAncients
杀戮尖塔2，东方先古之民
依赖于BaseLib3.1.0(如有更新会说明)

# 控制台使用
使用`觉醒键进行一些快捷操作

输入 unlock all解锁所有角色、遗物进度、卡牌进度

## 定位先古之民
设置里可以设置某个东方角色必定不生成，或是选择一个角色使其必定在二层或三层生成。<br>
如果不勾必定生成，和原版的先古之民会共享一个池子，还是有小概率遇到老资历先古之民<br>
输入ancient+空格+先古之民代号 可以直接跳跃到对应先古之民处，问心无愧。<br>
使用示例：ancient TOUHOUANCIENTS-HAKUREI_REIMU_ANCIENT<br>
### 角色代号：
| 角色 | 代号 |
|:----|:----|
| 东风谷早苗（二层/三层） | `TOUHOUANCIENTS-KOTIYA_SANAE_ANCIENT` |
| 博丽灵梦（二层） | `TOUHOUANCIENTS-HAKUREI_REIMU_ANCIENT` |
| 古明地觉（二层） | `TOUHOUANCIENTS-KOMEJI_SATORI_ANCIENT` |
| 渡里贝子（二层） | `TOUHOUANCIENTS-WATARI_NINA_ANCIENT` |
| 因幡帝（二层） | `TOUHOUANCIENTS-INABA_TEWI_ANCIENT` |
| 蕾米莉亚·斯卡雷特（三层） | `TOUHOUANCIENTS-REMILIA_SCARLET_ANCIENT` |
| 比那名居天子（三层） | `TOUHOUANCIENTS-HINANAWI_TENSHI_ANCIENT` |
| 鬼人正邪（二层） | `TOUHOUANCIENTS-KIJIN_SEIJA_ANCIENT` |
| 梅蒂欣·梅兰可莉（二层） | `TOUHOUANCIENTS-MEDICINE_MELANCHOLY_ANCIENT` |

## 获得对应遗物
控制台也可以直接获得对应的遗物。

输入relic+空格+遗物代号 即可直接获得该遗物

使用示例：relic TOUHOUANCIENTS-THE_THIRD_EYE
### 遗物代号：

#### 东风谷早苗（二层/三层）
| 遗物 | 代号 | 效果 | 衍生 |
|:----|:----|:----|:----|
| 守矢御币 | `TOUHOUANCIENTS-MORIYA_GOHEI` | 拾起时，从牌组中选择一张牌，为其附魔：**奇迹** | 奇迹：获得消耗。打出后获得1能量，将一张带有奇迹附魔的随机颜色牌加入抽牌堆。 |
| 白昼新星 | `TOUHOUANCIENTS-DAY_KAKUSEI` | 每场战斗开始时，获得一瓶能力药水。升级你生成的能力牌。 | — |
| 蛇之护符 | `TOUHOUANCIENTS-SNAKE_AMULET` | 如果你在回合结束时手牌数不大于1，获得8点格挡与1敏捷，下回合额外获得1能量。 | — |
| 蛙之护符 | `TOUHOUANCIENTS-FROG_AMULET` | 通过卡牌造成伤害后，下回合结束时对所有敌人造成25%造成伤害值的伤害。 | — |
| 水手服 | `TOUHOUANCIENTS-SAILOR_SUIT` | 战斗开始时，获得1人工制品。进入脆弱、虚弱与易伤状态时，免疫之并对所有敌人附加等量回合的对应效果。 | — |
| 神山的谢礼 | `TOUHOUANCIENTS-GIFT_FROM_MOUNTAIN` | 拾起时，获得300金币、一组普通、一组罕见和一组稀有卡牌奖励。 | — |

#### 博丽灵梦（二层）
| 遗物 | 代号 | 效果 | 衍生 |
|:----|:----|:----|:----|
| 博丽御币 | `TOUHOUANCIENTS-HAKUREI_GOHEI` | 拾起时，将一张梦想封印加入牌组。 | 梦想封印：1费攻击牌，对所有敌人造成2点伤害6次。如果敌人的意图是攻击，则给予1层易伤并使其在本回合失去6点力量。 |
| 亚空穴 | `TOUHOUANCIENTS-SUBSPACE_HOLE` | 每场战斗你首次失去生命值时，获得2层无实体。 | — |
| 迷你神龛 | `TOUHOUANCIENTS-MINI_SHRINE` | 在每个回合开始时获得1能量。拾起时，将一张供奉加入你的牌组。你持有的金币会被自动转化为供奉的进度。 | 供奉：永恒，不可打出。累计供奉250金币后自动从牌组移除。 |
| 阴阳玉 | `TOUHOUANCIENTS-YIN_YANG_ORB` | 每当你打出一张技能牌时，获得1临时力量。每当你打出一张攻击牌时，获得1临时敏捷。 | — |
| 封魔针 | `TOUHOUANCIENTS-SEALING_NEEDLE` | 使用攻击牌后额外给予目标1层虚弱。你对处于虚弱状态的敌人造成伤害时，增加等同于其当前虚弱层数的伤害。 | — |

#### 古明地觉（二层）
| 遗物 | 代号 | 效果 | 衍生 |
|:----|:----|:----|:----|
| 第三只眼 | `TOUHOUANCIENTS-THE_THIRD_EYE` | 在每场战斗开始时，给予自身2层虚弱，预见5，将1张觉之瞳加入你的手牌。 | 觉之瞳：0费技能。预见5。将此牌返回你的手牌，本回合耗能增加2。保留。 |
| 地狱猫车 | `TOUHOUANCIENTS-HELL_ORIN` | 有敌人死亡时，恢复5点生命。每当有敌人死亡或洗牌时，若恢复生命的数值不为0，抽一张牌并使该效果减少2。 | — |
| 地狱鸦羽 | `TOUHOUANCIENTS-HELL_OKUU` | 在每个回合开始时获得1能量。如果你结束回合时能量为0，将一张灼伤置入抽牌堆，并获得2力量。 | — |
| 心灵探测仪 | `TOUHOUANCIENTS-MIND_PROBE` | 若敌人造成的伤害与你格挡值的绝对值差≤2，击晕该敌人。 | — |
| 记忆烧瓶 | `TOUHOUANCIENTS-MEMORY_FLASK` | 拾起时，从牌组中选择一张牌，为其附魔：**回忆**。 | 回忆：首次进入弃牌堆时自动打出。 |
| 缸中之脑 | `TOUHOUANCIENTS-BRAIN_IN_A_VAT` | 拾起时，从25张当前角色牌中指定任意张，使其不出现在后续卡牌奖励与商店中。 | — |
| 遗忘残片 | `TOUHOUANCIENTS-OBLIVION_FRAGMENT` | 拾起时，获得随机3个涅奥的初始遗物。 | — |

#### 渡里贝子（二层）
| 遗物 | 代号 | 效果 | 衍生 |
|:----|:----|:----|:----|
| 展望未来的X药 | `TOUHOUANCIENTS-ZHANGEWEILAIBA` | 拾起时从角色牌中选择一张X费牌加入牌组。X费牌的效果数值增加2。 | — |
| 异眼顶真的蛇瞳 | `TOUHOUANCIENTS-YIYANDINGZHEN` | 战斗开始时获得混乱。回合开始时选择一张手牌，本回合免费打出。 | — |
| 涤荡腐坏的炽焰 | `TOUHOUANCIENTS-HUOYANTUXI` | 受到攻击时，对攻击者造成8点伤害。 | — |
| 重峦叠嶂的壁垒 | `TOUHOUANCIENTS-BILEIHAOPAIDUOZHUA` | 拾起时，将4张壁垒加入牌组，具有附魔：**优质**。获得已有同名牌时自动附魔优质。 | 优质：牌组中每有一张同名牌，此牌费用减少1。 |
| 白白胖胖的海兽 | `TOUHOUANCIENTS-BAIBAIXIANGXIANGRUANRUAN` | 拾起时删除4张牌，复制牌组中所有防御牌并升级。 | — |
| 萎靡心灵的魔咒 | `TOUHOUANCIENTS-GEISHEHUAXIAOJIE` | 战斗开始时敌人减少9力量。你每打出一张牌，敌人增加1力量（最多9次）。 | — |
| 吞天噬地的毒牙 | `TOUHOUANCIENTS-SHEYAOTEBIEQIANG` | 将一张蛇咬加入牌组。为牌组中名称带"蛇"的牌附魔：**蛇毒**。 | 蛇毒：手牌中每回合减1费。打出后回手，本回合随机化耗能。 |
| 一时兴起的消费 | `TOUHOUANCIENTS-YISHIXINGQILE` | 拾起时获得112金币。消费后随机一件商品价格降至0。 | — |
| 固若金汤的圣铠 | `TOUHOUANCIENTS-YONGHENGKAIJIAWANGCHAOLE` | 战斗开始时获得7层覆甲。获得覆甲时获得1能量。 | — |
| 激寒大地的重冰 | `TOUHOUANCIENTS-ZHIHUIJIZHONGBING` | 战斗开始时获得3个额外充能球栏位。未打出任何牌结束时，获得3临时集中，生成2个冰霜充能球。 | — |

#### 蕾米莉亚·斯卡雷特（三层）
| 遗物 | 代号 | 效果 | 衍生 |
|:----|:----|:----|:----|
| 德古拉之遗 | `TOUHOUANCIENTS-DRACULA_LEGACY` | 拾起时随机获得6个遗物。不以此方式获得遗物时失去13生命。 | — |
| 恶魔胸针 | `TOUHOUANCIENTS-NOBLE_BROOCH` | 回合开始时将至多2张牌变化，然后可以再次变化以此法变化的牌。 | — |
| 猩红酒樽 | `TOUHOUANCIENTS-CRIMSON_CHALICE` | 拾起时最大生命翻倍，将4张腐朽加入牌组。 | — |
| 殷红之牙 | `TOUHOUANCIENTS-BLOOD_FANG` | 拾起时失去最大生命的一半，从牌组中选择一张牌为其附魔：**喋血**。 | 喋血：每次打出恢复4生命。首伤时额外恢复等于造成伤害的生命。 |
| 腌制红雾 | `TOUHOUANCIENTS-PRESERVED_RED_FOG` | 拾起时从牌组中复制3张牌。将一张愚行加入牌组。 | 愚行：不可打出，永恒，固有。 |
| 极夜侍仆 | `TOUHOUANCIENTS-NIGHT_SERVANT` | 卡牌奖励额外包含时间仆从、魔法仆从、罡气仆从中的一张。 | 时间仆从：1费，生成2把小刀。回合结束时每消耗1张牌造成5伤害。<br>魔法仆从：1费，为手牌添加虚无。获得2能量，本回合不可再打出。<br>罡气仆从：2费，获得18格挡。本回合将格挡掉的攻击伤害转化为活力。 |
| 领主防晒霜 | `TOUHOUANCIENTS-LORDS_SUNSCREEN_CREAM` | 卡牌奖励掉落的牌一定为稀有牌，且你可以选择全都要。 | — |
| 冈格尼尔 | `TOUHOUANCIENTS-SPEAR_GUNGNIR` | 待设计 | — |

#### 比那名居天子（三层）
| 遗物 | 代号 | 效果 | 衍生 |
|:----|:----|:----|:----|
| 绯想之剑 | `TOUHOUANCIENTS-HISOU_SWORD` | 拾起时，将一张全人类的绯想天加入牌组。 | 全人类的绯想天：3费能力。保留。打出并消耗所有当前手牌与消耗堆里的牌。 |
| 天穹裙带 | `TOUHOUANCIENTS-FIRMAMENT_SASH` | 受到非自身伤害时减少至多10点并记录。回合结束时对自身造成等量伤害。 | — |
| 天衍苦厄 | `TOUHOUANCIENTS-CURSE_BREAKER_QI` | 拾起时获得9张随机诅咒。可打出诅咒，打出诅咒时获得1力量、1能量并抽2张牌。 | — |
| 天罚石柱 | `TOUHOUANCIENTS-KEYSTONE_FLOATING_CANNON` | 技能牌插下要石，攻击牌拔出要石对所有敌人造成8伤害，本场战斗伤害+2。 | — |
| 天馔仙桃 | `TOUHOUANCIENTS-MYSTIC_FORTUNE_PEACH` | 每打出攻击、技能、能力各一张时，清除负面效果并恢复1能量。 | — |
| 天赐甲胄 | `TOUHOUANCIENTS-HOLY_ARMOR` | 每回合开始时获得1能量。每回合首次从卡牌获得的格挡减半。 | — |
| 天界冷漠 | `TOUHOUANCIENTS-CELESTIAL_INDIFFERENCE` | 回合结束时，升级本回合获得过的牌与抽牌堆顶的牌。 | — |
| 天宇诏令 | `TOUHOUANCIENTS-COSMIC_DECREE` | 拾起时获得天赋君权。查看15张储君的升级牌，选择任意数量加入牌组。 | — |
| 至尊金箍 | `TOUHOUANCIENTS-SUPREME_HEAVEN_SEAL` | 拾起时选择一张升级后的先古之民卡牌加入牌组。不再掉落卡牌奖励。 | — |

#### 鬼人正邪（二层）
| 遗物 | 代号 | 效果 | 衍生 |
|:----|:----|:----|:----|
| 反叛号角 | `TOUHOUANCIENTS-REBELLION_HORN` | 拾起时，升级你牌组中所有初始牌与普通牌。 | — |
| 隐身布 | `TOUHOUANCIENTS-INVISIBILITY_CLOTH` | 战斗开始时获得壁垒与15点格挡。首次受伤时失去壁垒并进入1回合昏眩(只能打出一张牌)。 | — |
| 沥血阴阳玉 | `TOUHOUANCIENTS-BLOOD_YIN_YANG_ORB` | 每回合开始时获得1能量。休息处恢复生命减少25点。 | — |
| 哔哩电池 | `TOUHOUANCIENTS-BATTERY_BILI` | 每场战斗第一回合开始时，从抽牌堆中选择3张能力牌置入手牌，这些牌本回合免费打出，并被侵蚀为流电(打出这张牌时受到6点伤害)。 | — |
| 亡灵提灯 | `TOUHOUANCIENTS-GHOST_LANTERN` | 拾起时，从15张无色牌中选择至多5张加入牌组，拥有附魔：**付丧之力**。 | 付丧之力：获得虚无。抽牌阶段结束后，如果你的手牌中没有带有付丧之力的卡牌，将随机一张带有此附魔的卡牌移动到手牌中。 |
| 万宝槌 | `TOUHOUANCIENTS-MAGIC_MALLET` | 拾起时，将一张小槌的魔力加入牌组。 | 小槌的魔力：1费技能。你的下1张其它牌免费打出。每次打出后本场战斗费用+1，免费次数+1。 |
| 折叠伞 | `TOUHOUANCIENTS-FOLDING_UMBRELLA` | 战斗开始时，获得5层倒映。 | — |
| 饥饿背包 | `TOUHOUANCIENTS-HUNGRY_BACKPACK` | 每回合开始时额外抽3张牌，使随机3张抽牌阶段抽到的牌获得吞噬(获得消耗)，然后数值减少1。 | — |
| 伪灵异珠 | `TOUHOUANCIENTS-FAKE_SPIRIT_ORB` | 每回合开始时获得1能量。随机2张抽牌阶段抽到的牌获得沉重。(打出这张牌时失去1能量) | — |

#### 因幡帝（二层）
| 遗物 | 代号 | 效果 | 衍生 |
|:----|:----|:----|:----|
| 因幡御守 | `TOUHOUANCIENTS-WHITE_RABBIT_AMULET` | 拾起时移除牌组中所有非永恒诅咒。阻止之后获得诅咒。 | — |
| 萝卜项链 | `TOUHOUANCIENTS-CARROT_NECKLACE` | 拾起时从牌组中移除至多3张牌。商人移除卡牌服务价格翻倍。 | — |
| 兔角契约 | `TOUHOUANCIENTS-RABBIT_HORN_CONTRACT` | 拾起时获得200金币，将一张债务加入牌组。每场战斗结束获得当前金币20%。 | — |
| 幸运宝盒 | `TOUHOUANCIENTS-LUCKY_TREASURE_CHEST` | 拾起时获得3个药水栏位，用幸运补剂填满药水栏。 | — |
| 四十叶草 | `TOUHOUANCIENTS-FOUR_LEAF_CLOVER` | 每2场战斗卡牌奖励额外包含一张稀有卡牌。第二次以此法出现时升级。 | — |
| 兔脚 | `TOUHOUANCIENTS-RABBITS_FOOT` | 可以以70金币的价格出售卡牌奖励。 | — |
| 兔笼 | `TOUHOUANCIENTS-RABBITS_CAGE` | 拾起时，将一张幸运白兔加入你的牌组。 | 幸运白兔：0费技能。消耗。抽2张牌，在本回合随机化你手牌中所有牌的耗能，并为其中费用为0的牌在本场战斗添加重放。 |

#### 梅蒂欣·梅兰可莉（二层）
| 遗物 | 代号 | 效果 | 衍生 |
|:----|:----|:----|:----|
| 童忆书包 | `TOUHOUANCIENTS-CHILDHOOD_BAG` | 用污浊药水填满空药水栏位。战斗结束时的金币奖励替换为1瓶污浊药水。自身免疫污浊药水伤害。 | — |
| 舞台装置 | `TOUHOUANCIENTS-STAGE_DEVICE` | 回合开始时给予所有敌人1层易伤（已有易伤则失去1生命）。回合结束时给予所有敌人1层虚弱（已有虚弱则失去1生命）。 | — |
| 蛊毒魔盒 | `TOUHOUANCIENTS-MEDICINE_POISON_BOX` | 拾起时，将所有打击替换为蛇咬并附魔：**蛊毒**。 | 蛊毒：回合结束时若在手牌，费用减1直至打出。本局游戏中毒+5。 |
| 铃色的日记本 | `TOUHOUANCIENTS-LILY_BELL_DIARY` | 每场战斗开始时，将1张铃兰加入手牌。 | 铃兰：0费保留消耗。打出时获得1(2)能量抽2张。被保留时自身+2中毒，能量+1。 |
| 幸福的秘药 | `TOUHOUANCIENTS-HAPPINESS_ELIXIR` | Boss战开始时用随机药水填满药水栏、回复所有生命、升级所有卡牌、移除所有诅咒。 | — |
| 丝带蝴蝶结 | `TOUHOUANCIENTS-RIBBON_BOW` | 拾起时选2张非能力牌附魔：**誓约**。 | 誓约：打出后同时打出其他誓约牌。被消耗时消耗其他誓约牌。 |
| 蔷薇皇冠 | `TOUHOUANCIENTS-ROSE_CROWN` | 战斗开始时获得5层荆棘与5层覆甲。 | — |
| 恶毒的童话书 | `TOUHOUANCIENTS-MALICIOUS_FAIRY_TALE` | 回合开始时获得3力量。所有敌人初始获得1层荆棘。 | — |

