# TouhouAncients Mod — Copilot 工作区指令

## 附魔开发规则
- 附魔继承 `CustomEnchantmentModel`，位于 `TouhouAncients.Scripts.Enchantment`
- 附魔会被包含在战斗 Hook 监听器中 → 可以重写 `AfterDamageGiven`, `BeforeDamageReceived` 等全局 Hook
- 附魔独有 Hook: `OnPlay`, `AfterCardPlayed`, `BeforeFlush`, `AfterCardChangedPiles`, `BeforeCardRemoved`
- 无需 `[SavedProperty]` 的附魔不用在 `Entry.cs` 注册
- 仅 Attack 牌限制: `CanEnchantCardType(CardType cardType) => cardType == CardType.Attack`
- 判断进入弃牌堆: `card.Pile?.Type == PileType.Discard`，配合 `oldPileType != PileType.Play` 排除正常打出

## 遗物开发规则
- 所有遗物继承 `TouhouAncientRelics`（继承 `CustomRelicModel`），命名空间 `TouhouAncients.Scripts.relics`
- 必须加 `[Pool(typeof(SharedRelicPool))]` 属性
- 图标路径自动解析: `res://icon/relics/{类名小写}.png`
- 多人模式: 每个 Hook 检查 `player != base.Owner`
- 只有 `[SavedProperty]` 字段才需要在 `Entry.cs` 注册 `InjectTypeIntoCache`
- 新 Ancient 角色: 创建在 `scripts/ancients/`，继承 `CustomAncientModel`，池子用 `MakePool` / `AncientOption<T>`
- 新 Ancient 子目录: `scripts/relics/{角色名}/`

## 运行时动态变量
- `CanonicalVars` 定义静态默认值
- `base.DynamicVars["Key"].BaseValue = value` 在 `AfterObtained` 中运行时修改
- 常用子类: `DynamicVar`, `MaxHpVar`, `HpLossVar`, `EnergyVar`, `StringVar`

## 常用命令速查
- 丢最大HP: `CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), creature, amount, isFromCard: false);`
- 加最大HP: `CreatureCmd.GainMaxHp(creature, amount);`
- 治疗: `CreatureCmd.Heal(creature, amount);`
- 选牌附魔VFX: `CardSelectCmd.FromDeckForEnchantment` + `CardCmd.Enchant<T>` + `CardCmd.Preview`
- 建牌+附魔+入牌组: `player.RunState.CreateCard` + `CardCmd.Enchant<T>` + `CardPileCmd.Add` + `CardCmd.PreviewCardPileAdd`
- 克隆奖励牌: `player.RunState.CloneCard(card)` + `reward.ModifyCard(clonedCard, this)`
- 自动打出: `CardCmd.AutoPlay(choiceContext, card, target: null)`
- 洗牌 Hook: `AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)`
- 敌人死亡 Hook: `AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)`
- 随机打乱: `list.UnstableShuffle(rng).Take(N)`
- 处理事件遗物: `ModelDb.Event<Neow>().AllPossibleOptions` 获取事件选项

## 本地化键名格式
- `TOUHOUANCIENTS-{全大写蛇形}.title` / `.description` / `.flavor`
- 附魔: `TOUHOUANCIENTS-BLOODSHED.title`
- 遗物: `TOUHOUANCIENTS-BLOOD_FANG.title`
- Ancient: `TOUHOUANCIENTS-{角色}_ANCIENT.title` / `.epithet` / `.talk....`

## 本地化文本规范
### 1.1 遗物附魔描述格式
拾起遗物并对牌附魔时，统一使用以下格式：
```
拾起时，（其他效果），从[gold]牌组[/gold]中选择一张(指定类型的)牌，为它[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。
```
要点：
- 使用 `[gold]牌组[/gold]` 而非 "牌组"（不加标记）
- 使用 `为它[gold]附魔[/gold]` 而非 "为其附魔" 或其他变体
- 附魔名使用 `{EnchantmentName}` 动态变量（`StringVar`），而非硬编码

### 1.2 生命相关术语
- 统一使用 **"最大生命"**，禁止使用 "体力上限"
