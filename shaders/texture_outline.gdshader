shader_type canvas_item;

// 贴图 alpha 向外扩 outline_width 像素后的实心剪影，用来当描边的底色层。
// 它输出的是纯色剪影、会整块盖掉贴图本身，所以别挂在要显示立绘的节点上：
// 立绘另放一层（子节点或按 z 序排好），描边层垫在它下面。
// 两个前提：
//   1. 必须靠 vertex 把 quad 向外扩 outline_width 像素 —— 描边像素落在节点矩形之外，
//      不扩边不是被裁掉，是几何上压根没有那些像素。扩边不会改 UV 属性，所以 fragment 里
//      要把 UV 映射回原贴图，否则贴图会跟着放大。
//   2. 贴图尺寸需等于节点尺寸，否则「贴图像素」与「节点像素」不等比，描边会偏粗或被切。

uniform vec4 outline_color : source_color = vec4(1.0);
uniform float outline_width : hint_range(0.0, 16.0) = 2.0;

// 外缘过渡强度，0~1（归一化）。0 = 硬边：遮罩二值，外缘按贴图网格成阶梯；
// 越大过渡带越长：渐变带从 outline_width 处铺到 outline_width * (1 - outline_softness) 处，
// 1 = 整条描边宽度都是渐变、没有实心芯。过渡带上还会乘采样点的原图 alpha（原图抗锯齿边一起参与）。
uniform float outline_softness : hint_range(0.0, 1.0) = 0.0;

// fragment 里拿到的 COLOR 已经是「调制色 × 贴图色」（透明像素还带着贴图的残留 rgb），
// 不能当调制色用，所以从 vertex 带一份：vertex 的 COLOR 才是节点自身的 modulate。
varying vec4 v_modulate;

void vertex() {
    v_modulate = COLOR;

    if (outline_width > 0.0) {
        VERTEX += (UV * 2.0 - 1.0) * outline_width;
    }
}

void fragment() {
    // 扩边把 UV 拉宽了，直接采样会把贴图放大；先映射回原贴图，越界的就是扩出来的那圈
    vec2 tex_uv = UV + outline_width * (UV * 2.0 - 1.0) * TEXTURE_PIXEL_SIZE;

    // 采样点必须锚在 texel 中心、而不是锚在当前像素：锚在像素上时「到样本的距离」只能是整数，
    // smoothstep 只在整数处取值 —— 表现为 softness 0→1 只让最外一圈消失（瞬变），
    // 之后每 +1 才多淡一圈。锚在 texel 中心后距离带小数、是连续量，softness 才能连续调。
    vec2 texel_pos = tex_uv / TEXTURE_PIXEL_SIZE;
    vec2 base = floor(texel_pos - 0.5) + 0.5;
    vec2 frac_pos = texel_pos - base;

    // 半径 outline_width 的圆形邻域（含自身）内取最大的「覆盖度」，就是实心剪影。
    // 遮罩是 max 而不是「命中即 1」，所以不能提前 break（硬边模式下也照跑整圈）。
    // 注意 fragment() 里不能用 return（Godot 着色器限制），只能靠 if 包住。
    float coverage = 0.0;
    if (outline_width > 0.0) {
        float soft = clamp(outline_softness, 0.0, 1.0);
        // 渐变带铺在 [inner, width] 上：soft 越大内边界越靠里，0 时退化成硬边
        float inner = outline_width * (1.0 - soft);
        int radius = int(ceil(outline_width));
        for (int x = -radius; x <= radius; x++) {
            for (int y = -radius; y <= radius; y++) {
                vec2 offset = vec2(float(x), float(y));
                float dist = length(offset - frac_pos);
                if (dist > outline_width) {
                    continue;
                }

                // 越界采样会被钳到边缘像素（repeat_disable），所以显式跳过
                vec2 sample_uv = (base + offset) * TEXTURE_PIXEL_SIZE;
                if (sample_uv != clamp(sample_uv, vec2(0.0), vec2(1.0))) {
                    continue;
                }

                float alpha = texture(TEXTURE, sample_uv).a;
                float weight = soft > 0.0
                    ? alpha * smoothstep(outline_width, inner, dist)
                    : (alpha > 0.0 ? 1.0 : 0.0);
                coverage = max(coverage, weight);
            }
        }
    }

    COLOR = vec4(outline_color.rgb, outline_color.a * coverage) * v_modulate;
}
