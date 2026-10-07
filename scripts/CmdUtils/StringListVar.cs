using System.Collections;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TouhouAncients.Scripts;

/// <summary>
/// 列表型动态变量：文本侧写 {Name:list:{}|、}，分隔符交给各语言自己定。
/// 只实现 IEnumerable 即可（SmartFormat 的 list 格式直接枚举）；漏写 :list: 会退回 ToString 拼接。
/// </summary>
public sealed class StringListVar : DynamicVar, IEnumerable<string>
{
    private readonly List<string> _values = [];

    public StringListVar(string name) : base(name, 0m)
    {
    }

    public void Set(IEnumerable<string> values)
    {
        _values.Clear();
        _values.AddRange(values);
    }

    public IEnumerator<string> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    public override string ToString() => string.Join("、", _values);
}
