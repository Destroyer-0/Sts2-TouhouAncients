using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using Godot;
using TouhouAncients.Scripts.Enchantment;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 不语之物：鬼人正邪「妖器再塑」的衍生物。外观为被抽走的那张牌，意图与生命值由该牌类型决定。
/// 打击型：造成该牌面板伤害（吃自身力量）；防御型：为正邪提供该牌面板格挡（吃自身敏捷）。
/// 被代表的牌若有重放次数，则按实际结算次数连击 / 多次给予格挡；并按附魔逐项附加特殊行为。
/// </summary>
public sealed class SilentObjectMonster : TouhouAncientMonsterBase
{
    private CardModel? _embodiedCard;
    private EnchantmentModel? _enchant;
    private bool _fromPlayer;
    private bool _isAttack;
    private int _panelDamage;
    private int _panelBlock;

    /// <summary>当前结算次数；华彩的一次性重放首次后回落。</summary>
    private int _hits = 1;

    /// <summary>一次性重放消耗后的结算次数。</summary>
    private int _hitsAfterFirst = 1;

    private bool _oneTimeReplayConsumed;

    // --- 逐附魔行为 ---
    private int _vigorousAmount;    // 活力：生成时获得的活力层数
    private int _adroitBlock;       // 伶俐：生成时为自身提供的格挡
    private int _inkyWeak;          // 墨影：攻击额外给予的虚弱
    private int _momentumStrength;  // 动量：行动后获得的力量
    private int _corruptedLife;     // 腐化：行动后失去的生命（固定 2）
    private int _bloodshedHeal;     // 喋血：每次攻击恢复的生命
    private bool _diesAfterAction;  // 黏糊/奇迹：行动后死亡
    private bool _returnToExhaust;  // 死亡时归入玩家消耗堆
    private bool _isGoopy;          // 黏糊：额外增加计数

    /// <summary>本机创建的卡面节点（仅展示用），死亡时统一清除。</summary>
    private readonly List<NCard> _cardNodes = new();

    protected override bool HasAnimation => false;

    public override bool IsPrimaryMonster => false;

    protected override int InitialHp => _isAttack
        ? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 21, 20)
        : AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 24);

    /// <summary>
    /// 在 <see cref="CreatureCmd.Add"/> 之前写入被代表的牌与面板值——
    /// Creature 构造会读 InitialHp、SetUpForCombat 会读 GenerateMoveStateMachine，字段必须先就位。
    /// </summary>
    public void InitFromCard(CardModel card, bool fromPlayer)
    {
        _embodiedCard = card;
        _fromPlayer = fromPlayer;
        _enchant = card.Enchantment;

        // 取当前面板值：含升级与附魔修正（附魔加成在预览里无条件生效）
        card.UpdateDynamicVarPreview(CardPreviewMode.None, null, card.DynamicVars);

        // 重放 → 结算次数：华彩为每场战斗一次（首次 +replay，之后回落），其余恒定
        _hits = Math.Max(1, card.GetEnchantedReplayCount() + 1);
        _hitsAfterFirst = _hits;
        if (_enchant is Glam glam)
        {
            _hitsAfterFirst = Math.Max(1, _hits - glam.DynamicVars["Times"].IntValue);
        }

        if (card.DynamicVars.ContainsKey("Damage"))
        {
            _isAttack = true;
            _panelDamage = (int)card.DynamicVars["Damage"].PreviewValue;
        }
        else if (card.DynamicVars.ContainsKey("Block"))
        {
            _isAttack = false;
            _panelBlock = (int)card.DynamicVars["Block"].PreviewValue;
        }

        // 逐附魔特殊行为
        switch (_enchant)
        {
            case Vigorous vigorous when vigorous.Status == EnchantmentStatus.Normal:
                // 活力：面板已无条件含 +Amount，剔除后改为生成时给活力
                _vigorousAmount = vigorous.Amount;
                _panelDamage = Math.Max(0, _panelDamage - _vigorousAmount);
                break;
            case Inky inky:
                _inkyWeak = inky.DynamicVars.Weak.IntValue;
                break;
            case Momentum momentum:
                _momentumStrength = momentum.Amount;
                break;
            case Corrupted:
                _corruptedLife = 2;
                break;
            case Adroit adroit:
                _adroitBlock = adroit.Amount;
                break;
            case Bloodshed bloodshed:
                _bloodshedHeal = bloodshed.DynamicVars.Heal.IntValue;
                break;
        }
        if (_enchant is Goopy)
        {
            _isGoopy = true;
            _diesAfterAction = true;
            _returnToExhaust = true;
        }
        else if (_enchant is Miracle)
        {
            _diesAfterAction = true;
            _returnToExhaust = true;
        }
    }

    /// <summary>这张牌是否可被塑成不语之物（打击或防御）。</summary>
    public static bool CanEmbody(CardModel card)
        => card.DynamicVars.ContainsKey("Damage") || card.DynamicVars.ContainsKey("Block");

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<MinionPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);

        // 本体：悬停随从时预览被代表的牌（ExtraHoverTips 会被 Creature.HoverTips 聚合）
        SilentObjectCardPower? cardPower = await PowerCmd.Apply<SilentObjectCardPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        if (cardPower != null)
        {
            cardPower.EmbodiedCard = _embodiedCard;
        }

        // 活力：生成时获得 Amount 层活力
        if (_vigorousAmount > 0)
        {
            await PowerCmd.Apply<VigorPower>(new ThrowingPlayerChoiceContext(), base.Creature, _vigorousAmount, base.Creature, null);
        }
        ShowEmbodiedCard();
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();
        MoveState move;
        if (_isAttack)
        {
            List<AbstractIntent> intents = new();
            if (_diesAfterAction)
            {
                intents.Add(new DeathBlowIntent(() => _panelDamage));
            }
            else if (_hits > 1)
            {
                intents.Add(new MultiAttackIntent(_panelDamage, () => _hits));
            }
            else
            {
                intents.Add(new SingleAttackIntent(() => _panelDamage));
            }

            if (_adroitBlock > 0) intents.Add(new DefendIntent());
            if (_momentumStrength > 0) intents.Add(new BuffIntent());
            if (_inkyWeak > 0) intents.Add(new DebuffIntent());
            if (_bloodshedHeal > 0) intents.Add(new HealIntent());
            
            
            // 重放数 > 0 时用多段意图展示连击数（实时求值，供华彩回落时刷新）
            move = new MoveState("STRIKE", AttackMove, intents.ToArray());
        }
        else
        {
            move = new MoveState("GUARD", GuardMove, new DefendIntent());
        }
        move.FollowUpState = move;
        list.Add(move);
        return new MonsterMoveStateMachine(list, move);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(_panelDamage)
            .FromMonster(this)
            .WithHitCount(_hits)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await AfterAction(targets, wasAttack: true);
    }

    /// <summary>为正邪提供格挡：吃不语之物自身的敏捷，不吃正邪的敏捷。</summary>
    private async Task GuardMove(IReadOnlyList<Creature> targets)
    {
        Creature? seija = base.CombatState.Enemies
            .FirstOrDefault(c => c is { Monster: KijinSeijaMonster, IsDead: false });
        if (seija == null) return;

        int dexterity = base.Creature.GetPower<DexterityPower>()?.Amount ?? 0;
        // 手动叠加自身敏捷后以 Unpowered 授予，避免被正邪自身的敏捷二次放大
        for (int i = 0; i < _hits; i++)
        {
            await CreatureCmd.GainBlock(seija, _panelBlock + dexterity, ValueProp.Unpowered, null);
        }
        await AfterAction(targets, wasAttack: false);
    }

    /// <summary>行动后的逐附魔结算（墨影/动量/腐化/华彩/黏糊）。</summary>
    private async Task AfterAction(IReadOnlyList<Creature> targets, bool wasAttack)
    {
        var choiceContext = new ThrowingPlayerChoiceContext();

        // 墨影：攻击额外给予虚弱
        if (wasAttack && _inkyWeak > 0)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, targets, _inkyWeak, base.Creature, null);
        }
        // 喋血：每次攻击恢复生命
        if (wasAttack && _bloodshedHeal > 0)
        {
            await CreatureCmd.Heal(base.Creature, _bloodshedHeal * CombatState.Players.Count);
        }
        // 动量：行动后获得力量
        if (_momentumStrength > 0)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, base.Creature, _momentumStrength, base.Creature, null);
        }

        // 腐化：行动后失去生命（可致死）
        if (_corruptedLife > 0)
        {
            await CreatureCmd.Damage(choiceContext, base.Creature, _corruptedLife * CombatState.Players.Count, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        }

        if (_adroitBlock > 0)
        {
            await CreatureCmd.GainBlock(base.Creature, _adroitBlock, ValueProp.Move, null);
        }

        // 华彩：一次性重放首次后回落
        if (!_oneTimeReplayConsumed)
        {
            _oneTimeReplayConsumed = true;
            _hits = _hitsAfterFirst;
        }

        // 黏糊/奇迹：行动后死亡（黏糊额外增加计数）
        if (_diesAfterAction)
        {
            if (_isGoopy)
            {
                IncrementGoopy();
            }
            if (!base.Creature.IsDead)
            {
                await CreatureCmd.Kill(base.Creature);
            }
        }
    }

    /// <summary>黏糊被打出时的计数增加（本体与牌组版本）。</summary>
    private void IncrementGoopy()
    {
        if (_enchant is not Goopy goopy) return;
        goopy.Amount++;
        if (_embodiedCard?.DeckVersion?.Enchantment != null)
        {
            _embodiedCard.DeckVersion.Enchantment.Amount++;
        }
    }

    /// <summary>把被代表的牌挂到场景的 %StolenCardPos 上（仅本机显示）。</summary>
    private void ShowEmbodiedCard()
    {
        if (_embodiedCard == null) return;
        if (!LocalContext.IsMine(_embodiedCard)) return;

        NCreature? creatureNode = base.Creature.GetCreatureNode();
        Marker2D? cardPos = creatureNode?.GetSpecialNode<Marker2D>("%StolenCardPos");
        if (creatureNode == null || cardPos == null) return;

        NCard? nCard = NCard.Create(_embodiedCard);
        if (nCard == null) return;

        nCard.Position = cardPos.Position;
        nCard.Scale = Vector2.One * 0.5f;
        creatureNode.Visuals.AddChildSafely(nCard);
        nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
        _cardNodes.Add(nCard);
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature || wasRemovalPrevented) return;

        foreach (NCard node in _cardNodes)
        {
            node.QueueFreeSafely();
        }
        _cardNodes.Clear();

        await ReturnEmbodiedCard();
    }

    /// <summary>死亡时把被代表的牌归还：消耗堆牌只返回来历为玩家的牌，其余进弃牌堆；均清除侵蚀。</summary>
    private async Task ReturnEmbodiedCard()
    {
        CardModel? card = _embodiedCard;
        bool fromPlayer = _fromPlayer;
        bool toExhaust = _returnToExhaust;
        _embodiedCard = null;
        if (card == null) return;

        // 归还的牌不再受两种侵蚀影响
        if (card.Affliction != null)
        {
            CardCmd.ClearAffliction(card);
        }

        // 黏糊/奇迹：历史删除牌不归还
        if (toExhaust && !fromPlayer) return;

        Player? owner = card.Owner;
        if (owner?.Creature == null || owner.Creature.IsDead) return;
        if (owner.Creature.CombatState == null) return;

        card.HasBeenRemovedFromState = false;
        await CardPileCmd.Add(card, toExhaust ? PileType.Exhaust : PileType.Discard);
    }
}
