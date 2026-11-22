shader_type canvas_item;
render_mode blend_premul_alpha;

// 叛乱之潮的卡面效果：复制自 devoured.gdshader（吞噬），保留独立副本以便单独调参。
// 边缘暗潮向内蔓延；上方流光为红，下方为灰黑，约 uv.y=0.33 处过渡。

uniform float edge_intensity : hint_range(0.0, 1.0) = 0.8;
uniform float pulse_speed : hint_range(0.1, 4.0) = 0.7;
uniform float void_width : hint_range(0.0, 0.5) = 0.13;
uniform float noise_scale : hint_range(1.0, 20.0) = 5.0;
uniform float void_stretch : hint_range(1.0, 6.0) = 3.0;
uniform vec4 top_color : source_color = vec4(0.757, 0.0, 0.0, 1.0); // #C10000
uniform vec4 bottom_color : source_color = vec4(0.102, 0.102, 0.102, 1.0);
uniform float gradient_center : hint_range(0.0, 1.0) = 0.33; // uv.y=0 为卡面顶部
uniform float gradient_softness : hint_range(0.01, 0.5) = 0.15;
uniform float corner_radius : hint_range(0.0, 0.15) = 0.055;
// 卡面像素尺寸（NCard 默认 300x422），只用于把 UV 换成等比坐标。
uniform vec2 card_px = vec2(300.0, 422.0);

// 外轮廓交给场景里的 card_mask（ui_card_mask.png + clip_children）裁，
// shader 只负责暗潮本身；顶点色不能丢，否则卡牌淡出/变灰时 overlay 原地满色。
varying vec4 vertex_color;

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

float rounded_rect_sdf(vec2 p, vec2 half_size, float radius) {
	vec2 d = abs(p) - half_size + vec2(radius);
	return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0) - radius;
}

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec2 uv = UV;
	float t = TIME * pulse_speed;

	// 像素空间求 SDF：直接拿 UV 算会把圆角拉成椭圆
	vec2 p = (uv - 0.5) * card_px;
	float sdf = rounded_rect_sdf(p, card_px * 0.5, corner_radius * card_px.y);
	// 换算回「卡高 = 1」，后面的阈值才和参数同单位
	float edge_dist = max(-sdf, 0.0) / card_px.y;

	// 0 = 最近的边在上下，1 = 在左右
	float dist_h = min(uv.y, 1.0 - uv.y);
	float dist_v = min(uv.x, 1.0 - uv.x);
	float edge_blend = smoothstep(0.0, 0.05, dist_h - dist_v);

	// 噪声沿最近的边拉长：左右边竖着拉，上下边横着拉
	vec2 stretch = mix(vec2(void_stretch, 1.0), vec2(1.0, void_stretch), edge_blend);

	// 原地摆 UV，不滚动
	vec2 noise_uv = p / card_px.y * noise_scale * stretch;
	vec2 warp = vec2(
		sin(t * 0.7 + uv.y * 4.0) * 0.4 + cos(t * 0.5 + uv.x * 3.0) * 0.3,
		cos(t * 0.6 + uv.x * 4.0) * 0.4 + sin(t * 0.4 + uv.y * 3.0) * 0.3
	) * stretch;

	// 两层不同相位的噪声叠出蠕动感
	float n1 = fbm(noise_uv + warp);
	float n2 = fbm(noise_uv * 1.4 + warp * 0.7 + 5.0);
	float combined_noise = mix(n1, n2, 0.45);

	// 反向 smoothstep（edge0 > edge1）在 GLSL 里是未定义行为，必须写成 1 - smoothstep
	float reach = void_width + combined_noise * 0.12;
	float void_mask = 1.0 - smoothstep(0.0, reach, edge_dist);

	// Sharpen for more definition
	void_mask = smoothstep(0.1, 0.6, void_mask);

	// 沿边起伏的呼吸
	float breath = 0.8 + 0.2 * sin(t * 1.3 + combined_noise * 3.0);
	void_mask *= breath;

	// 上红下灰黑
	float vertical = smoothstep(gradient_center - gradient_softness, gradient_center + gradient_softness, uv.y);
	vec3 effect_color = mix(top_color.rgb, bottom_color.rgb, vertical);

	// 虚空里的闪烁亮点，用同一套配色提亮
	float speck = value_noise(noise_uv * 3.5 + warp * 2.0);
	speck = step(0.9, speck) * void_mask * 0.35;
	effect_color += effect_color * 1.2 * speck;

	float alpha = void_mask * edge_intensity * vertex_color.a;
	// 预乘 alpha 输出：rgb 也要乘最终 alpha
	COLOR = vec4(effect_color * vertex_color.rgb * alpha, alpha);
}
