---
name: touhou-ancients-mod
description: Work on the TouhouAncients Slay the Spire 2 mod in this repository. Use when editing or reviewing TouhouAncients C# mod code, ancients, relics, cards, enchantments, powers, potions, localization, Godot resources, STS2 hooks, or when the user mentions this STS2 Touhou mod.
---

# TouhouAncients Mod

Follow this skill when working in the TouhouAncients repository.

## First Steps

1. Read `.github/copilot-instructions.md` before making design or code decisions.
2. Prefer querying project documentation before source spelunking:
   - Read `docs/项目参考.md` for existing project patterns, paths, naming rules, and known APIs.
   - Read `docs/STS2战斗流程完整钩子生命周期.md` for combat and room hook ordering.
   - Read existing TouhouAncients implementations that are closest to the requested feature.
3. Use `E:\STS2\src\Core` and `E:\STS2\localization\zhs` only when project documentation and local examples do not answer the question, or when the user explicitly asks to compare against vanilla STS2 source.
4. If an original design/specification is missing or uncertain, tell the user before inventing mechanics.

## Collaboration Rules

- Answer only the user's explicit request.
- Before modifying code, explain the intended change and wait for user approval.
- Do not silently change the user's design.
- Do not implement a relic, card, ancient, enchantment, or other gameplay feature from your own design when the user has not provided the original requirements.

## Relics

- Put relics under `scripts/relics/{CharacterName}/` or `scripts/relics/Misc/`.
- Use namespace `TouhouAncients.Scripts.relics`.
- Inherit `TouhouAncientRelics`.
- Add `[Pool(typeof(SharedRelicPool))]`.
- The base class resolves icons from the lowercase class name under `res://images/icon/relics/`.
- In multiplayer-sensitive hooks, check ownership such as `player != base.Owner`.
- Register a type in `Entry.cs` with `SavedPropertiesTypeCache.InjectTypeIntoCache` only when it has `[SavedProperty]`.

## Ancients

- Put new Ancient models in `scripts/ancients/`.
- Inherit `CustomAncientModel`.
- Define option pools with `MakePool` and `AncientOption<T>`.
- Add new Ancient config entries in `scripts/TouhouAncientsConfig.cs`.
- Add localization in `TouhouAncients/localization/zhs/ancients.json` and settings localization when needed.

## Cards

- Put cards in `scripts/cards/`.
- Use namespace `TouhouAncients.Scripts.cards`.
- Inherit `TouhouAncientCards`.
- Add the correct pool attribute, usually `[Pool(typeof(EventCardPool))]`.
- Concrete card classes must have a public parameterless constructor that calls the base constructor with class-level constants.
- Use `CanonicalVars` for text-bound values and upgrade values with `base.DynamicVars["Key"].UpgradeValueBy(...)`.

## Enchantments

- Put enchantments in `scripts/Enchantment/`.
- Use namespace `TouhouAncients.Scripts.Enchantment`.
- Inherit `CustomEnchantmentModel`.
- Enchantments can override global combat hooks such as `AfterDamageGiven` and `BeforeDamageReceived`.
- Enchantment-specific hooks include `OnPlay`, `AfterCardPlayed`, `BeforeFlush`, `AfterCardChangedPiles`, and `BeforeCardRemoved`.
- Enchantments without `[SavedProperty]` do not need `Entry.cs` registration.
- For Attack-only enchantments, use `CanEnchantCardType(CardType cardType) => cardType == CardType.Attack`.
- To detect entering discard pile, use `card.Pile?.Type == PileType.Discard` with `oldPileType != PileType.Play` when normal play should be excluded.

## Dynamic Vars And Common Commands

- Define default runtime text values in `CanonicalVars`.
- Change runtime values with `base.DynamicVars["Key"].BaseValue = value`, often in `AfterObtained`.
- Common dynamic variable classes include `DynamicVar`, `MaxHpVar`, `HpLossVar`, `EnergyVar`, and `StringVar`.
- Lose max HP: `CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), creature, amount, isFromCard: false);`
- Gain max HP: `CreatureCmd.GainMaxHp(creature, amount);`
- Heal: `CreatureCmd.Heal(creature, amount);`
- Select and enchant a deck card: `CardSelectCmd.FromDeckForEnchantment` + `CardCmd.Enchant<T>` + `CardCmd.Preview`.
- Create, enchant, and add a card to deck: `player.RunState.CreateCard` + `CardCmd.Enchant<T>` + `CardPileCmd.Add` + `CardCmd.PreviewCardPileAdd`.
- Clone a reward card: `player.RunState.CloneCard(card)` + `reward.ModifyCard(clonedCard, this)`.
- Auto-play: `CardCmd.AutoPlay(choiceContext, card, target: null)`.

## Localization

- Use keys shaped like `TOUHOUANCIENTS-{UPPER_SNAKE_CASE}.title`, `.description`, and `.flavor`.
- Enchantment example: `TOUHOUANCIENTS-BLOODSHED.title`.
- Relic example: `TOUHOUANCIENTS-BLOOD_FANG.title`.
- Ancient example: `TOUHOUANCIENTS-{CHARACTER}_ANCIENT.title`, `.epithet`, `.talk....`.
- Omit a relic `.eventDescription` when it is identical to `.description`.
- For relics that enchant cards on pickup, use this description pattern:

```text
拾起时，（其他效果），从[gold]牌组[/gold]中选择一张(指定类型的)牌，为它[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。
```

- Use `[gold]牌组[/gold]`.
- Use `为它[gold]附魔[/gold]`.
- Use `{EnchantmentName}` through a `StringVar`; do not hard-code the enchantment name.
- Use the term `最大生命`; do not use `体力上限`.
- Do not abbreviate descriptions, comments, or localization text, except for rich-text tags.

## Documentation Maintenance

- After developing a new feature, append newly discovered patterns, methods, or paths to the appropriate section of `docs/项目参考.md`.
- Prefer documentation and repository memory before looking through vanilla STS2 source for already-recorded hooks or APIs.
- If documentation conflicts with source behavior, report the conflict before broad changes.
