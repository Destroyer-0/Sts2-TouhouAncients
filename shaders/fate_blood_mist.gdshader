shader_type canvas_item;
render_mode blend_premul_alpha;

// 命运吊坠专用卡面叠加层，包含两层图形：
//   1. 从卡牌边缘向内漫延的血色雾气
//   2. 卡牌中央一只微微振翅的血色蝙蝠剪影
//
// 设计参考原版侵蚀 overlay（shaders/afflictions/devoured.gdshader 与
// shaders/vfx/ui/card/afflictions/smog 系列）的做法：本 shader 挂在独立叠加层
// （ColorRect）上，不读取卡牌本体材质，因此不影响卡牌原有颜色与原版高亮。
//
// 全部图形均程序化生成（哈希噪声 + SDF），不依赖任何贴图。
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

// 蝙蝠剪影的不透明度（0.5 = 半透明）
uniform float bat_alpha : hint_range(0.0, 1.0) = 0.5;

// 蝙蝠颜色（血色）
uniform vec4 bat_color : source_color = vec4(0.72, 0.03, 0.06, 1.0);

// 整体缩放（1.0 为默认大小）
uniform float bat_scale : hint_range(0.2, 3.0) = 1.0;

// 位置微调（等比例坐标，1 单位 = 卡宽；y 正方向向下）
uniform vec2 bat_offset = vec2(0.0, 0.0);

// 振翅速度（0 表示不振动）
uniform float flap_speed : hint_range(0.0, 8.0) = 2.6;

// 振翅幅度（弧度；0.12 约 7 度，属"微微振翅"）
uniform float flap_amount : hint_range(0.0, 0.5) = 0.12;

// 剪影外缘的淡血色辉光强度（0 则只保留纯剪影）
uniform float bat_glow : hint_range(0.0, 1.0) = 0.30;

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

// 椭圆近似 SDF：边界上为 0，内部为负；梯度长度接近 1，
// 因此可以直接用 fwidth 做像素级抗锯齿。
float ellipse_sdf(vec2 p, vec2 r) {
    float k1 = length(p / r);
    float k2 = length(p / (r * r));
    if (k2 < 1e-6) {
        // 圆心处退化为「到边界的内向距离」
        return -min(r.x, r.y);
    }
    return k1 * (k1 - 1.0) / k2;
}

vec2 rotate2(vec2 p, float a) {
    float c = cos(a);
    float s = sin(a);
    return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

// 蝙蝠剪影的近似 SDF。
// p    —— 等比例局部坐标（1 单位 = 卡宽），原点在卡牌中心，y 向下为正
// flap —— 当前振翅角度（弧度）
//
// 与"蛾子"的形状区分（蛾翅是圆钝的整片，蝙蝠翅是分指的薄翼）：
//   · 后缘由若干细长"指骨"椭圆自肩部扇形展开，相邻指的并集在指尖之间
//     留下凹缺口 → 蝙蝠标志性的扇贝状后缘
//   · 前缘分「臂骨 / 掌骨」两段，腕部有轻微折角
//   · 耳朵是尖细椭圆，长轴朝上外侧（明显上竖，而非圆钝贴头）
float bat_silhouette(vec2 p, float flap) {
    // 躯干（细长）
    float d = ellipse_sdf(p - vec2(0.0, 0.018), vec2(0.028, 0.056));

    // 头
    d = min(d, ellipse_sdf(p - vec2(0.0, -0.038), vec2(0.029, 0.029)));

    // 双耳：镜像后评估同一个倾斜细椭圆，保证左右完全对称。
    // rotate2 的 +1.16 是把采样点转入椭圆局部坐标系，使长轴指向"上外侧"。
    vec2 ear = vec2(abs(p.x), p.y) - vec2(0.021, -0.068);
    d = min(d, ellipse_sdf(rotate2(ear, 1.16), vec2(0.032, 0.010)));

    // 双翅：镜像后只算右侧；振翅 = 绕肩部旋转采样点。
    // 左右共用同一份采样坐标，翅膀同步扇动（对称振翅）。
    vec2 q = vec2(abs(p.x), p.y);
    vec2 shoulder = vec2(0.030, -0.005);
    vec2 w = rotate2(q - shoulder, flap) + shoulder;

    // ---- 前缘（上边界）：臂骨 + 掌骨两段，腕部有轻微折角 ----
    float wing = ellipse_sdf(rotate2(w - shoulder, 0.11) - vec2(0.043, 0.0), vec2(0.045, 0.013));
    wing = min(wing, ellipse_sdf(rotate2(w - shoulder, 0.31) - vec2(0.120, 0.0), vec2(0.086, 0.012)));

    // ---- 指骨（后缘）：自肩部向下扇形展开，相邻指的并集形成扇贝状凹缺口 ----
    wing = min(wing, ellipse_sdf(rotate2(w - shoulder, -0.10) - vec2(0.100, 0.0), vec2(0.102, 0.013)));
    wing = min(wing, ellipse_sdf(rotate2(w - shoulder, -0.30) - vec2(0.092, 0.0), vec2(0.094, 0.012)));
    wing = min(wing, ellipse_sdf(rotate2(w - shoulder, -0.48) - vec2(0.078, 0.0), vec2(0.080, 0.011)));

    return min(d, wing);
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

    // ---------- 卡牌中央的血色蝙蝠剪影 ----------
    // 换算到等比例局部坐标（1 单位 = 卡宽），避免图形被卡牌宽高比拉长
    vec2 bp = vec2(uv.x - 0.5, (uv.y - 0.5) * card_aspect);
    bp = (bp - bat_offset) / max(bat_scale, 0.001);

    float flap = sin(TIME * flap_speed) * flap_amount;
    float bat_d = bat_silhouette(bp, flap);

    // 用 fwidth 得到像素尺度，做抗锯齿
    float aa = max(fwidth(bat_d), 1e-5) * 0.9;
    float bat_mask = 1.0 - smoothstep(-aa, aa, bat_d);
    float bat_a = bat_mask * bat_alpha * card_mask;

    // 剪影外缘的淡血色辉光（距形状边缘越远越淡）
    float glow_a = exp(-max(bat_d, 0.0) * 45.0) * (1.0 - bat_mask) * bat_glow * card_mask;

    // ---------- 合成（premultiplied alpha，蝙蝠叠在血雾之上） ----------
    float bat_total = clamp(bat_a + glow_a, 0.0, 1.0);
    float out_a = bat_total + mist_a * (1.0 - bat_total);
    vec3 out_rgb = bat_color.rgb * bat_total + mist_rgb * mist_a * (1.0 - bat_total);

    COLOR = vec4(out_rgb, out_a);
}
