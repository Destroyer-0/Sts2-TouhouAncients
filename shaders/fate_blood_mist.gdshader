shader_type canvas_item;
render_mode blend_premul_alpha;

// 命运吊坠专用卡面叠加层，包含两层图形：
//   1. 从卡牌边缘向内漫延的血色雾气（程序化生成：哈希噪声 + 圆角矩形 SDF）
//   2. 卡牌中央一只微微振翅的血色蝙蝠剪影（贴图 res://shaders/bat.png 的 alpha 通道）
//
// 设计参考原版侵蚀 overlay（shaders/afflictions/devoured.gdshader 与
// shaders/vfx/ui/card/afflictions/smog 系列）的做法：本 shader 挂在独立叠加层
// （ColorRect）上，不读取卡牌本体材质，因此不影响卡牌原有颜色与原版高亮。
//
// 蝙蝠只用贴图的 alpha 当剪影遮罩，颜色一律由 bat_color 决定，
// 因此 bat.png 本身画成黑剪影还是白剪影都无所谓（当前素材是「黑剪影 + 透明背景」）。
// 贴图由 scenes/cards/overlays/fate_mark.tscn 的 ShaderMaterial 绑定。
//
// 输出为 premultiplied alpha（配合 blend_premul_alpha）。

// ============================ 血雾参数 ============================

// 血雾整体强度（0 完全不显示，1 最浓）
uniform float intensity : hint_range(0.0, 1.0) = 0.85;

// 血雾呼吸/蠕动速度
uniform float pulse_speed : hint_range(0.0, 4.0) = 0.55;

// 血雾向内漫延的最大宽度（相对卡牌短边，0.22 约为卡宽的 22%）
uniform float mist_width : hint_range(0.0, 0.6) = 0.22;

// 噪声密度：数值越大血雾越细碎
uniform float noise_scale : hint_range(1.0, 20.0) = 6.0;

// 沿边缘方向的拉伸系数：越大则雾气越呈长条"手指"状
uniform float edge_stretch : hint_range(1.0, 8.0) = 3.5;

// 指状分裂强度：越大则边缘越破碎不齐
uniform float finger_amount : hint_range(0.0, 1.0) = 0.55;

// 内侧最暗处（贴近卡牌边缘）的血色
uniform vec4 mist_color : source_color = vec4(0.30, 0.01, 0.03, 1.0);

// 外侧最亮处（雾气尖端）的血色
uniform vec4 mist_edge_color : source_color = vec4(0.85, 0.10, 0.07, 1.0);

// 在雾气内漂浮的暗红血点密度
uniform float speck_amount : hint_range(0.0, 1.0) = 0.35;

// ============================ 蝙蝠参数 ============================

// 蝙蝠剪影贴图：只取它的 alpha 通道当剪影遮罩（黑图、白图都能用）。
// repeat_disable：贴图内容四周留有一点空白，出界采样由 sample_bat() 显式截断。
uniform sampler2D bat_texture : filter_linear, repeat_disable;

// 贴图在卡面上的显示尺寸（等比例坐标，1 单位 = 卡宽；x 为翼展方向、y 为上下方向）。
// bat.png 的内容约占整张贴图的 0.98 × 0.41，所以 0.44 对应约 0.43 卡宽的翼展。
uniform vec2 bat_size = vec2(0.44, 0.44);

// 蝙蝠剪影的不透明度（0.5 = 半透明）
uniform float bat_alpha : hint_range(0.0, 1.0) = 0.5;

// 蝙蝠颜色（血色）
uniform vec4 bat_color : source_color = vec4(0.72, 0.03, 0.06, 1.0);

// 整体缩放（1.0 为默认大小；乘在 bat_size 之上）
uniform float bat_scale : hint_range(0.2, 3.0) = 1.0;

// 位置微调（等比例坐标，1 单位 = 卡宽；y 正方向向下）。
// 默认值不是 0：bat.png 里的蝙蝠画得比贴图中心略低一点点，
// 这里先把这点偏移补掉，使「贴图中心 = 蝙蝠视觉中心」。
uniform vec2 bat_offset = vec2(0.002, -0.024);

// 振翅铰链位置（等比例坐标，相对贴图中心）。
// x 恒为 0，左右翅膀各绕自己那侧的铰链反向旋转，于是扇动始终左右对称。
uniform vec2 bat_pivot = vec2(0.0, -0.023);

// 躯干半宽：|x| 小于它的部分（头、身、尾）完全不参与振翅，免得整只蝙蝠跟着扭。
uniform float bat_body_half = 0.035;

// 躯干 → 翅膀的过渡宽度：把旋转角从 0 平滑抬到满值，避免翅膀根部出现硬折痕。
uniform float bat_wing_feather = 0.030;

// 振翅速度（0 表示不振动）
uniform float flap_speed : hint_range(0.0, 8.0) = 2.6;

// 振翅幅度（弧度；0.12 约 7 度，属"微微振翅"）
uniform float flap_amount : hint_range(0.0, 0.5) = 0.12;

// 剪影外缘的淡血色辉光强度（0 则只保留纯剪影）
uniform float bat_glow : hint_range(0.0, 1.0) = 0.30;

// 辉光向外扩散的半径（等比例坐标；0 则辉光紧贴剪影边缘）
uniform float bat_glow_width : hint_range(0.0, 0.1) = 0.025;

// ============================ 通用参数 ============================

// 卡牌圆角半径（与卡面圆角匹配）
uniform float corner_radius : hint_range(0.0, 0.2) = 0.055;

// 卡牌宽高比（高 / 宽），用于把紫外坐标换算成等比例坐标，避免图形被纵向拉伸
uniform float card_aspect = 1.4067;

// ------------------------------------------------------------------
// 噪声
// ------------------------------------------------------------------

float hash(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

float value_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 4; i++) {
        value += amplitude * value_noise(p);
        p *= 2.1;
        amplitude *= 0.5;
    }
    return value;
}

// ------------------------------------------------------------------
// 几何
// ------------------------------------------------------------------

float rounded_rect_sdf(vec2 p, vec2 half_size, float radius) {
    vec2 d = abs(p) - half_size + vec2(radius);
    return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0) - radius;
}

// 采样蝙蝠剪影贴图的 alpha。
// 贴图内容四周留了一点透明边距，但 repeat_disable 的越界采样会被钳到边缘像素，
// 而边缘像素恰好落在翅膀上（非 0），所以越界必须在这里显式截断成 0，
// 否则拉伸采样点会把翅膀在最外侧那一列上"拉丝"成一条横带。
float sample_bat(vec2 tex_uv) {
    vec2 inside = step(vec2(0.0), tex_uv) * step(tex_uv, vec2(1.0));
    return texture(bat_texture, tex_uv).a * inside.x * inside.y;
}

vec2 rotate2(vec2 p, float a) {
    float c = cos(a);
    float s = sin(a);
    return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

// 蝙蝠剪影的振翅形变。
// p    —— 等比例局部坐标（1 单位 = 卡宽），原点在贴图中心，y 向下为正
// flap —— 当前振翅角度（弧度）
//
// 贴图里的蝙蝠是「左右对称的固定姿态」，所以振翅只能靠形变做：
// 把采样点绕肩部铰链旋转，等价于翅膀绕肩上下扇动。
//   · 躯干（|x| < bat_body_half）角度为 0，头和身体不会跟着扭；
//   · 躯干到翅膀之间用 smoothstep 把角度从 0 抬到 flap，翅膀根部自然弯折；
//   · 左右两侧各用 ±flap（镜像），因此扇动始终左右对称，不会一边上一边下。
vec2 bat_warp(vec2 p, float flap) {
    vec2 hinge = p - bat_pivot;
    float side = hinge.x < 0.0 ? -1.0 : 1.0;
    float wing = smoothstep(bat_body_half, bat_body_half + bat_wing_feather, abs(hinge.x));
    return rotate2(hinge, flap * wing * side) + bat_pivot;
}

void fragment() {
    vec2 uv = UV;
    float t = TIME * pulse_speed;

    // ---------- 卡牌形状遮罩：圆角矩形之外完全透明 ----------
    vec2 centered = uv - 0.5;
    float card_sdf = rounded_rect_sdf(centered, vec2(0.5), corner_radius);
    float card_mask = 1.0 - smoothstep(-0.004, 0.0, card_sdf);
    float edge_dist = max(-card_sdf, 0.0);

    // ---------- 血雾：从边缘向内漫延 ----------
    // 判断当前像素更靠近左右边还是上下边，用于沿边缘方向拉伸噪声
    float dist_h = min(uv.y, 1.0 - uv.y);
    float dist_v = min(uv.x, 1.0 - uv.x);
    float edge_blend = smoothstep(0.0, 0.05, dist_h - dist_v);

    // 左右边：压缩 X、拉伸 Y；上下边：压缩 Y、拉伸 X
    vec2 stretch = mix(vec2(edge_stretch, 1.0), vec2(1.0, edge_stretch), edge_blend);

    // 原地摆动噪声坐标（不滚动），让血雾原地蠕动而非平移
    vec2 noise_uv = uv * noise_scale * stretch;
    vec2 warp = vec2(
        sin(t * 0.7 + uv.y * 4.0) * 0.4 + cos(t * 0.5 + uv.x * 3.0) * 0.3,
        cos(t * 0.6 + uv.x * 4.0) * 0.4 + sin(t * 0.4 + uv.y * 3.0) * 0.3
    ) * stretch;

    // 两层不同相位的噪声叠加，让雾气形状持续变形
    float combined = mix(fbm(noise_uv + warp), fbm(noise_uv * 1.4 + warp * 0.7 + 5.0), 0.45);

    // 血雾向内漫延的深度 = 基础宽度 + 噪声扰动（形成指状）
    float reach = mist_width + combined * finger_amount * 0.18;
    float mist = smoothstep(reach, 0.0, edge_dist);

    // 提高对比度，让边缘处的血雾有明确边界
    mist = smoothstep(0.08, 0.65, mist);

    // 沿边缘呼吸，让雾气浓度起伏
    mist *= 0.82 + 0.18 * sin(t * 1.3 + combined * 3.0);

    // 颜色渐变：贴边处最深，雾气尖端偏亮红
    vec3 mist_rgb = mix(mist_edge_color.rgb, mist_color.rgb, smoothstep(0.10, 0.0, edge_dist));

    // 雾气内部漂浮的暗红血点
    float speck = step(0.9, value_noise(noise_uv * 3.5 + warp * 2.0)) * mist * speck_amount;
    mist_rgb += vec3(0.55, 0.05, 0.08) * speck;

    float mist_a = mist * intensity * card_mask;

    // ---------- 卡牌中央的血色蝙蝠剪影（贴图 alpha 当遮罩） ----------
    // 换算到等比例局部坐标（1 单位 = 卡宽），避免图形被卡牌宽高比拉长
    vec2 bp = vec2(uv.x - 0.5, (uv.y - 0.5) * card_aspect);
    bp = (bp - bat_offset) / max(bat_scale, 0.001);

    // 贴图在卡面上占的尺寸；同时充当「局部位移 → 贴图 uv 位移」的换算系数
    vec2 bat_extent = max(bat_size, vec2(1e-4));

    // 振翅形变 → 贴图 uv（v 轴向下，与 bp.y 同向，所以不需要翻转 y）
    float flap = sin(TIME * flap_speed) * flap_amount;
    vec2 bat_uv = bat_warp(bp, flap) / bat_extent + 0.5;

    // 线性过滤自带边缘抗锯齿（贴图 alpha 本身就是抗锯齿的黑白边）
    float bat_mask = sample_bat(bat_uv);
    float bat_a = bat_mask * bat_alpha * card_mask;

    // 剪影外缘的淡血色辉光：绕当前像素取一圈样本做平均，相当于一次环形模糊。
    // 形状外「模糊值 − 剪影值」由内向外自然衰减（形状边缘处约 0.5，越远越接近 0），
    // 所以乘 1.8 后再夹到 1，就能得到原来那种指数衰减的柔和辉光。
    float halo = 0.0;
    vec2 halo_step = bat_glow_width / bat_extent;
    for (int i = 0; i < 12; i++) {
        float ang = float(i) * 0.5235987756; // 每步 30°
        halo += sample_bat(bat_uv + vec2(cos(ang), sin(ang)) * halo_step);
    }
    float glow_a = clamp((halo / 12.0 - bat_mask) * 1.8, 0.0, 1.0) * bat_glow * card_mask;

    // ---------- 合成（premultiplied alpha，蝙蝠叠在血雾之上） ----------
    float bat_total = clamp(bat_a + glow_a, 0.0, 1.0);
    float out_a = bat_total + mist_a * (1.0 - bat_total);
    vec3 out_rgb = bat_color.rgb * bat_total + mist_rgb * mist_a * (1.0 - bat_total);

    COLOR = vec4(out_rgb, out_a);
}
