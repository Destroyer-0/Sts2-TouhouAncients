using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.RichTextTags;

namespace TouhouAncients.Scripts.Ui;

/// <summary>[rainbow]：原版没有这个标签，逐字按时间做 HSV 色相循环。</summary>
public partial class RainbowRichTextEffect : AbstractMegaRichTextEffect
{
    /// <summary>每秒推进的色相量，可被 [rainbow freq=] 覆盖。</summary>
    private const float DefaultFrequency = 0.25f;

    /// <summary>相邻字的相位差，否则整段同色只会一起变。</summary>
    private const float IndexPhaseStep = 0.04f;

    // Godot 靠名为 bbcode 的成员识别标签，照原版 RichTextJitter 的字段写法
    public new string bbcode = "rainbow";

    protected override string Bbcode => bbcode;

    public override bool _ProcessCustomFX(CharFXTransform charFx)
    {
        if (!ShouldTransformText()) return false;

        Dictionary env = charFx.Env;
        float hue = Mathf.PosMod(
            (float)charFx.ElapsedTime * ReadFloat(env, "freq", DefaultFrequency)
            + (float)charFx.RelativeIndex * IndexPhaseStep,
            1f);
        charFx.Color = Color.FromHsv(hue, ReadFloat(env, "sat", 0.85f), ReadFloat(env, "val", 1f));

        // [color] 显式染色优先，原版其它效果同样这么处理
        if (env.TryGetValue(RichTextUtil.colorKey, out Variant color)) charFx.Color = (Color)color;
        charFx.Visible = !env.ContainsKey(RichTextUtil.visibleKey) || (bool)env[RichTextUtil.visibleKey];
        return true;
    }

    private static float ReadFloat(Dictionary env, Variant key, float fallback) =>
        env.TryGetValue(key, out Variant value) ? (float)value : fallback;
}
