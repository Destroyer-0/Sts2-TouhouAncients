using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TouhouAncients.Scripts.Background;

/// <summary>
/// 专属战斗背景场景（CustomBackgroundScenePath 指向的 .tscn）的根节点脚本。
///
/// 游戏本体的 NCombatBackground.Create 会用
/// PreloadManager.Cache.GetScene(背景路径).Instantiate&lt;NCombatBackground&gt; 实例化背景场景，
/// 根节点必须是 NCombatBackground（或其派生类）。但 NCombatBackground.cs 属于游戏本体工程，
/// 不在本 mod 工程的 res:// 命名空间内，直接引用会导致 mod 编辑器/导出 pck 时解析不到该脚本资源。
/// 因此本类继承 NCombatBackground：场景根节点挂本类脚本（资源属于本 mod 工程，uid 可正常解析），
/// 运行时仍是 NCombatBackground 的派生类型，满足 Instantiate 的类型要求，且无需改动任何游戏逻辑。
///
/// 本类作为所有自定义背景场景的公共基类，提供通用的亮度过渡方法
/// <see cref="FadeTo"/>；某场战斗特殊的初始状态（如开场压暗）直接写在
/// 对应 .tscn 根节点的属性上，由技能侧在适当时机调用 FadeTo 过渡。
/// </summary>
public partial class TouhouAncientBackground : NCombatBackground
{
    /// <summary>
    /// 在 <paramref name="seconds"/> 秒内把背景的 modulate 平滑过渡到 <paramref name="target"/>。
    /// 已在目标色附近时直接返回，避免叠加多余的 Tween。
    /// </summary>
    public void FadeTo(Color target, float seconds)
    {
        if (Modulate.IsEqualApprox(target))
        {
            return;
        }

        CreateTween().TweenProperty(this, "modulate", target, seconds);
    }
}
