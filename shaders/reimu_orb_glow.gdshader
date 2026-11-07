shader_type canvas_item;
render_mode blend_add, unshaded;

// 梦想天生光球的 bloom 外观：中心实心 + 外围柔光。
// fadeBall0000.png 是**全不透明**的径向亮度渐变（alpha 恒 255），
// 贴图本身亮度在半径 34px 以内是纯白平台，直接拿亮度当遮罩会得到一块硬边实心圆盘，
// 所以这里改成按 UV 半径程序化生成「实心核心 + 平滑光晕」，不再依赖贴图亮度。
// 颜色由节点 modulate 决定（六色光球共用一个 shader）。

// 实心核心半径（0~1，相对贴图半宽）
uniform float core_radius : hint_range(0.0, 1.0) = 0.32;

// 核心到光晕的过渡宽度
uniform float core_softness : hint_range(0.01, 0.6) = 0.16;

// 光晕衰减指数：越大光晕收得越快
uniform float glow_power : hint_range(0.5, 6.0) = 2.4;

// 光晕强度
uniform float glow_strength : hint_range(0.0, 2.0) = 0.85;

// 整体亮度
uniform float brightness : hint_range(0.2, 4.0) = 1.6;

// 从 vertex 带出节点 modulate：fragment 的 COLOR 已被乘上贴图色，不能直接用
varying vec4 v_modulate;

void vertex() {
    v_modulate = COLOR;
}

void fragment() {
    // 到贴图中心的归一化距离：中心 0，内切圆边缘 1
    float d = length(UV - vec2(0.5)) * 2.0;

    float core = 1.0 - smoothstep(core_radius, core_radius + core_softness, d);
    float glow = pow(clamp(1.0 - d, 0.0, 1.0), glow_power) * glow_strength;
    float mask = clamp(core + glow, 0.0, 1.0);

    // blend_add 只累加 rgb，显隐/淡出仍靠节点 modulate:a 缩放亮度
    COLOR = vec4(v_modulate.rgb * mask * brightness * v_modulate.a, 1.0);
}
