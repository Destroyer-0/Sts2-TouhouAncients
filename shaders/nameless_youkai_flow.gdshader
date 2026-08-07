shader_type canvas_item;
render_mode blend_add;

// 未名妖魔专用红蓝流光 shader。
// 基于原版 card_ripple.gdshader（沿卡牌边缘流动的亮带），
// 但颜色在红蓝之间随时间和卡牌边缘位置流动，形成红蓝双色流光。
uniform float ease = 0.005;
uniform float modulo_width = 0.02;
uniform float width = 0.0;
uniform float ripple_speed = 0.03;

uniform vec4 color_a : source_color = vec4(0.95, 0.2, 0.35, 0.98);
uniform vec4 color_b : source_color = vec4(0.2, 0.5, 1.0, 0.98);
uniform float flow_speed = 0.5;

void fragment() {
	float sdf_alpha = mod(TIME * ripple_speed + COLOR.a, modulo_width);
    float front_ease = smoothstep(0.0, ease, sdf_alpha);
    float easeAmount = front_ease * modulo_width;
    float brightness = COLOR.a - sdf_alpha + easeAmount;
    float alpha = smoothstep(1.0 - width, width + (1.0 - width), brightness);

    // 红蓝流光：颜色沿卡牌边缘位置与时间流动
    float flow = 0.5 + 0.5 * sin(TIME * flow_speed * 6.28318 + COLOR.a * 3.0);
    vec3 col = mix(color_a.rgb, color_b.rgb, flow);
    COLOR = vec4(col, alpha);
}
