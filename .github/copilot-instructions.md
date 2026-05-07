# TouhouAncients Mod — Copilot 工作区指令

## 附魔开发规则
- 附魔继承 `CustomEnchantmentModel`，位于 `TouhouAncients.Scripts.Enchantment`
- 附魔会被包含在战斗 Hook 监听器中 → 可以重写 `AfterDamageGiven`, `BeforeDamageReceived` 等全局 Hook
- 附魔独有 Hook: `OnPlay`, `AfterCardPlayed`, `BeforeFlush`, `AfterCardChangedPiles`, `BeforeCardRemoved`
- 无需 `[SavedProperty]` 的附魔不用在 `Entry.cs` 注册
- 仅 Attack 牌限制: `CanEnchantCardType(CardType cardType) => cardType == CardType.Attack`

## 遗物开发规则
- 所有遗物继承 `TouhouAncientRelics`（继承 `CustomRelicModel`），命名空间 `TouhouAncients.Scripts.relics`
- 必须加 `[Pool(typeof(SharedRelicPool))]` 属性
- 图标路径自动解析: `res://icon/relics/{类名小写}.png`
- 多人模式: 每个 Hook 检查 `player != base.Owner`
- 只有 `[SavedProperty]` 字段才需要在 `Entry.cs` 注册 `InjectTypeIntoCache`

## 运行时动态变量
- `CanonicalVars` 定义静态默认值
- `base.DynamicVars["Key"].BaseValue = value` 在 `AfterObtained` 中运行时修改
- 常用子类: `DynamicVar`, `MaxHpVar`, `HpLossVar`, `EnergyVar`, `StringVar`

## 常用命令速查
- 丢最大HP: `CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), creature, amount, isFromCard: false);`
- 加最大HP: `CreatureCmd.GainMaxHp(creature, amount);`
- 治疗: `CreatureCmd.Heal(creature, amount);`
- 选牌附魔VFX: `CardSelectCmd.FromDeckForEnchantment` + `CardCmd.Enchant<T>` + `NCardEnchantVfx.Create`
- 建牌+附魔+入牌组: `player.RunState.CreateCard` + `CardCmd.Enchant<T>` + `CardPileCmd.Add` + `CardCmd.PreviewCardPileAdd`
- 克隆奖励牌: `player.RunState.CloneCard(card)` + `reward.ModifyCard(clonedCard, this)`

## 本地化键名格式
- `TOUHOUANCIENTS-{全大写蛇形}.title` / `.description` / `.flavor`
- 附魔: `TOUHOUANCIENTS-BLOODSHED.title`
- 遗物: `TOUHOUANCIENTS-BLOOD_FANG.title`
