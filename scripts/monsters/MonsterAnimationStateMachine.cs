using System;
using Godot;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 单个动画状态（状态机内部数据，不可变）：动画名、是否循环、播放完成后的下一个状态解析器、
/// 进入/退出钩子、受击打断条件、是否为插播动画（播完恢复打断前状态）。
/// 解析器返回 null 表示不自动转移（由演出逻辑显式切换，保持时间驱动）。
/// </summary>
internal sealed class MonsterAnimState
{
    public string Name { get; }
    public bool IsLooping { get; }
    public Func<string>? NextStateResolver { get; }
    public Action? OnEnter { get; }
    public Action? OnExit { get; }

    /// <summary>
    /// 播放该动画期间是否允许被受击（hurt）打断。
    /// 返回 false 的动画（如蓄力、必杀演出）播放期间忽略受击触发，避免打断演出。
    /// 委托形式支持动态条件（如紫苑眩晕期间禁止打断）。
    /// </summary>
    public Func<bool> CanBeInterruptedByHit { get; }

    /// <summary>
    /// 是否为插播动画：播完后恢复到被打断前正在播的动画，而非走 <see cref="NextStateResolver"/>。
    /// 典型场景是 hurt：受击打断当前动画，播完回到打断前状态。
    /// 属性化后，未来任何需要相同"打断后恢复"语义的插播动画（如弹反演出）
    /// 注册时置为 true 即可复用同一套恢复逻辑，无需按名字特判。
    /// </summary>
    public bool RestoresPreviousOnFinish { get; }

    public MonsterAnimState(string name, bool isLooping,
        Func<string>? nextStateResolver = null, Action? onEnter = null, Action? onExit = null,
        Func<bool>? canBeInterruptedByHit = null, bool restoresPreviousOnFinish = false)
    {
        Name = name;
        IsLooping = isLooping;
        NextStateResolver = nextStateResolver;
        OnEnter = onEnter;
        OnExit = onExit;
        CanBeInterruptedByHit = canBeInterruptedByHit ?? (() => true);
        RestoresPreviousOnFinish = restoresPreviousOnFinish;
    }
}

/// <summary>
/// 怪物帧动画状态机（适配 AnimatedSprite2D）。
///
/// 设计原则：
/// - 时间驱动：不接管演出时间线，播放时长仍由调用方 await Cmd.Wait 控制；
///   状态机只负责状态注册 / 转移 / 播放完成后的系统策略。
/// - 受击恢复：记录打断前的循环动画（<see cref="_preHurtState"/>），
///   hurt 播完回到被打断前正在播的动画，而非一律回默认循环。
///   例如魔理沙蓄力（spell 循环）中被打，hurt 结束后应回到 spell 而非 idle。
/// - 死亡锁定：IsDeathLocked 委托为真时，除 die 外所有转移被忽略（尸体锁定 die）。
/// - 动态循环归属：TriggerLoop 播放 LoopResolver 的结果（默认 "idle"）。
/// </summary>
public sealed class MonsterAnimationStateMachine
{
    private readonly AnimatedSprite2D _sprite;
    private readonly Dictionary<string, MonsterAnimState> _states = new();
    private MonsterAnimState? _currentState;
    private string? _preHurtState;

    /// <summary>当前循环归属动画解析器（替代旧 CurrentLoopAnimation）。</summary>
    public Func<string> LoopResolver { get; set; } = static () => "idle";

    /// <summary>死亡锁定委托（替代旧 IsDeathAnimationLocked）。死后除 die 外所有转移被忽略。</summary>
    public Func<bool> IsDeathLocked { get; set; } = static () => false;

    /// <summary>
    /// 未显式指定 <see cref="MonsterAnimState.CanBeInterruptedByHit"/> 的状态的默认受击打断条件。
    /// 全局条件（如紫苑眩晕期间、灵梦准备演出期间禁止打断）可在此设置，个别状态再显式覆盖。
    /// </summary>
    public Func<bool> DefaultCanBeInterruptedByHit { get; set; } = static () => true;

    public MonsterAnimationStateMachine(AnimatedSprite2D sprite)
    {
        _sprite = sprite;
        _sprite.AnimationFinished += OnAnimationFinished;
    }

    /// <summary>注册一个循环动画状态（播放后保持循环，直到显式切换）。</summary>
    public void RegisterLoop(string name, Func<string>? nextStateResolver = null,
        Action? onEnter = null, Action? onExit = null, Func<bool>? canBeInterruptedByHit = null)
        => RegisterState(name, isLooping: true, nextStateResolver, onEnter, onExit, canBeInterruptedByHit);

    /// <summary>注册一个一次性动画状态（非循环，播完停在最后一帧，由 NextStateResolver 决定去向）。</summary>
    public void RegisterOneShot(string name, Func<string>? nextStateResolver = null,
        Action? onEnter = null, Action? onExit = null, Func<bool>? canBeInterruptedByHit = null,
        bool restoresPreviousOnFinish = false)
        => RegisterState(name, isLooping: false, nextStateResolver, onEnter, onExit, canBeInterruptedByHit,
            restoresPreviousOnFinish);

    private void RegisterState(string name, bool isLooping,
        Func<string>? nextStateResolver = null, Action? onEnter = null, Action? onExit = null,
        Func<bool>? canBeInterruptedByHit = null, bool restoresPreviousOnFinish = false)
    {
        var state = new MonsterAnimState(name, isLooping, nextStateResolver, onEnter, onExit,
            canBeInterruptedByHit ?? DefaultCanBeInterruptedByHit, restoresPreviousOnFinish);
        _states[name] = state;
    }

    /// <summary>SpriteFrames 中是否存在该动画。</summary>
    public bool HasAnimation(string name) => _sprite.SpriteFrames.HasAnimation(name);

    /// <summary>
    /// 转移到指定动画（等价旧 PlayAnimation）。
    /// 死亡锁定时忽略除 die 外的所有转移；动画未注册时允许播放但记录警告。
    /// </summary>
    public void Trigger(string name)
    {
        if (IsDeathLocked() && name != "die")
            return;

        if (!_sprite.SpriteFrames.HasAnimation(name))
        {
            GD.PushWarning($"MonsterAnimationStateMachine: 动画 '{name}' 不存在于 SpriteFrames，忽略转移。");
            return;
        }

        if (!_states.TryGetValue(name, out MonsterAnimState? state))
        {
            // 未注册的动画：允许播放（兼容直调），但不参与状态表转移语义
            GD.PushWarning($"MonsterAnimationStateMachine: 动画 '{name}' 未注册，按普通动画播放。");
            state = new MonsterAnimState(name, _sprite.SpriteFrames.GetAnimationLoop(name));
        }

        if (_currentState != null && !ReferenceEquals(_currentState, state))
            _currentState.OnExit?.Invoke();

        _currentState = state;
        // 离开插播恢复流程：显式切换到非插播动画时清除被打断前状态残留；
        // 目标仍是插播动画（如连续 hurt）则保留记录，供播完恢复打断前的动画
        if (!state.RestoresPreviousOnFinish)
            _preHurtState = null;

        state.OnEnter?.Invoke();
        _sprite.Animation = name;
        _sprite.Play();
    }

    /// <summary>播放当前循环归属动画（等价旧 PlayCurrentLoopAnimation）。</summary>
    public void TriggerLoop()
    {
        string loopName = LoopResolver();
        if (string.IsNullOrEmpty(loopName))
            return;
        Trigger(loopName);
    }

    /// <summary>
    /// 受击入口（由引擎 Hit 触发器调用）。
    /// 记录打断前的循环动画，播放 hurt；hurt 播完由 <see cref="OnAnimationFinished"/> 回到打断前状态。
    /// 当前状态 <see cref="MonsterAnimState.CanBeInterruptedByHit"/> 返回 false 时忽略受击。
    /// </summary>
    public void HandleHit()
    {
        if (IsDeathLocked())
            return;
        if (!HasAnimation("hurt"))
            return;
        // 当前状态不允许被受击打断（如蓄力 / 必杀演出）时忽略
        if (_currentState is { } state && !state.CanBeInterruptedByHit())
            return;

        // 记录打断前的循环动画（当前是循环动画且不在插播恢复流程中时）；
        // 连续受击时保留第一次记录的打断前状态，避免第二次 hurt 播完错误回默认循环
        if (_currentState is { IsLooping: true, RestoresPreviousOnFinish: false })
            _preHurtState = _currentState.Name;

        Trigger("hurt");
    }

    private void OnAnimationFinished()
    {
        MonsterAnimState? finished = _currentState;
        if (finished == null)
            return;

        // 插播恢复：插播动画（hurt）播完回到被打断前的动画；
        // 无打断前状态（开场未播任何动画 / 打断一次性动画）回默认循环
        if (finished.RestoresPreviousOnFinish)
        {
            if (_preHurtState != null)
            {
                string restore = _preHurtState;
                _preHurtState = null;
                Trigger(restore);
            }
            else
            {
                TriggerLoop();
            }
            return;
        }

        if (finished.NextStateResolver?.Invoke() is string explicitNext)
        {
            Trigger(explicitNext);
        }
    }
}
