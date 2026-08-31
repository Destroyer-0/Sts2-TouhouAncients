using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Afflictions;

/// <summary>
/// TouhouAncients 侵蚀基类。
/// 实现 <see cref="ICustomModel"/> 以让 BaseLib 的 PrefixIdPatch 自动
/// 为 ModelDb Entry 加上 mod ID 前缀，避免与其他 mod 的 affliction 发生
/// DuplicateModelException 冲突。
/// </summary>
public abstract class TouhouAncientAfflictionModel : AfflictionModel, ICustomModel
{
}