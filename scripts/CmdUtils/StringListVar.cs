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
    // DynamicVar.Clone 是 MemberwiseClone，字段不能放可变集合，否则同类型的多个实例会共用一份列表
    private const char Separator = '\u001f';

    private string _joined = "";

    public StringListVar(string name) : base(name, 0m)
    {
    }

    public void Set(IEnumerable<string> values) => _joined = string.Join(Separator, values);

    public IEnumerator<string> GetEnumerator()
    {
        string[] parts = _joined.Length == 0 ? [] : _joined.Split(Separator);
        return ((IEnumerable<string>)parts).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => _joined.Replace(Separator, '、');
}
