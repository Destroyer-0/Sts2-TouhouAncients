shader_type canvas_item;
render_mode blend_premul_alpha;

// 地狱火：火焰贴图（stage05a）向上滚动，噪声图（stage05b）既能打碎边缘轮廓又能扭至火焰 UV，
// 透明度按「到卡边的距离」衰减 → 卡边一圈在烧、卡面中间透出；底部另叠一张低透明度的 orin 图，被上升气流吹得扭动。
// 只做 alpha 混合（不加法提亮），不会把亮色卡面冲白。

uniform sampler2D fire_texture : source_color, filter_linear, repeat_enable;
uniform sampler2D noise_texture : filter_linear, repeat_enable;
uniform sampler2D orin_texture : source_color, filter_linear, repeat_disable;
// 回读已经画好的卡面，才能在火焰区域把它拉扭（否则 overlay 只能往上叠东西）。
uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear;

// 卡面像素尺寸（NCard 默认 300x422）。
uniform vec2 card_px = vec2(300.0, 422.0);
uniform float corner_ratio : hint_range(0.0, 0.15) = 0.055;

// —— 卡面淡染色（0 = 关）——
uniform vec4 tint_color : source_color = vec4(0.55, 0.16, 0.05, 1.0);
uniform float tint_strength : hint_range(0.0, 1.0) = 0.08;

// —— 边缘火力衰减 ——
// 从卡边往内多少像素内还有火。
uniform float edge_px : hint_range(1.0, 200.0) = 60.0;
// 越大火越贴边（中心更快消失）。
uniform float edge_power : hint_range(0.2, 6.0) = 1.6;
// 用噪声打碎火力带边界的幅度，0 = 均匀一圈。
uniform float ragged : hint_range(0.0, 1.5) = 0.6;

// —— 火焰外观 ——
uniform vec4 fire_tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);
// 火焰贴图在卡面上的铺开次数；x/y 不等可拉扁拉长。
uniform vec2 fire_scale = vec2(1.4, 1.6);
uniform float flow_speed : hint_range(0.0, 3.0) = 0.35;
// 第二层火焰（不同速度/尺度）的混合量，0 = 只一层。
uniform float layer2_mix : hint_range(0.0, 1.0) = 0.45;
uniform float brightness : hint_range(0.0, 3.0) = 1.0;
uniform float opacity : hint_range(0.0, 1.0) = 1.0;

// —— 噪声 ——
uniform vec2 noise_scale = vec2(1.6, 1.1);
uniform float noise_speed : hint_range(0.0, 2.0) = 0.12;
// 噪声横向扭曲火焰 UV 的幅度。
uniform float warp : hint_range(0.0, 0.5) = 0.10;

// —— 底部的 orin 图 ——
uniform vec4 orin_tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float orin_opacity : hint_range(0.0, 1.0) = 0.25;
// 中心位置（卡面 uv）与尺寸（以卡高为 1，x/y 同尺度 → 不会被 300:422 拉扁）。
uniform vec2 orin_center = vec2(0.5, 0.88);
uniform float orin_scale : hint_range(0.05, 1.5) = 0.3;
// 热气流扭曲幅度（贴图 uv 单位）与速度。
uniform float shimmer : hint_range(0.0, 0.15) = 0.04;
uniform float shimmer_speed : hint_range(0.0, 8.0) = 2.0;

// —— 卡图扰动（读屏幕）——
// 最大位移量（像素）；只在火焰带内生效，且离卡边 5px 内归零。
uniform float card_warp_px : hint_range(0.0, 20.0) = 3.5;
uniform float card_warp_speed : hint_range(0.0, 8.0) = 2.0;

float rounded_rect_sdf(vec2 p, vec2 half_size, float radius) {
	vec2 d = abs(p) - half_size + vec2(radius);
	return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0) - radius;
}

void fragment() {
	vec2 p = (UV - 0.5) * card_px;
	float sdf = rounded_rect_sdf(p, card_px * 0.5, corner_ratio * card_px.y);
	float inside = 1.0 - smoothstep(-1.0, 0.5, sdf);
	// 到卡边的向内距离（像素）；卡外为 0。
	float inner = max(-sdf, 0.0);
	vec2 cuv = p / card_px + 0.5;

	float t = TIME * flow_speed;
	float nz = texture(noise_texture, cuv * noise_scale + vec2(0.0, TIME * noise_speed)).r;

	// 近边 → 1，向中心衰减；再用噪声打碎边界，得到火舌形而非一圈硬边。
	float e = pow(1.0 - smoothstep(0.0, max(edge_px, 1e-3), inner), edge_power);
	float mask = smoothstep(0.2, 0.85, e + (nz - 0.5) * ragged) * inside;

	vec3 col = vec3(0.0);
	float a_out = 0.0;

	// 1) 底层：把屏幕上的卡面按热气流扭一下再贴回去。
	// 局部 UV → 屏幕 UV 的线性映射，用屏幕空间导数求 → 手牌旋转缩放也对。
	mat2 jac_screen = mat2(dFdx(SCREEN_UV), dFdy(SCREEN_UV));
	mat2 jac_local = mat2(dFdx(UV), dFdy(UV));
	if (abs(determinant(jac_local)) > 1e-9) {
		mat2 local_to_screen = jac_screen * inverse(jac_local);
		float ph = TIME * card_warp_speed;
		float j2 = texture(noise_texture, cuv * noise_scale * 2.0 + vec2(ph * 0.1, ph * 0.5)).r;
		vec2 wob = vec2(
			sin(cuv.y * 20.0 + ph) * 0.6 + (j2 - 0.5) * 1.2,
			sin(cuv.x * 15.0 - ph * 0.7) * 0.35);
		// 离卡边 5px 内幅度归零：否则扭动会采到卡外像素（背景 / 邻牌）。
		float amp = card_warp_px * mask * smoothstep(0.0, 5.0, inner);
		vec2 src = SCREEN_UV + local_to_screen * (wob * amp / card_px);
		// 屏幕纹理是预乘 alpha，直接拿来当预乘底层用。
		vec4 base = texture(screen_tex, src);
		col = base.rgb;
		a_out = base.a;
	}

	// 2) 卡面淡染色
	float tint_a = tint_strength * inside;
	a_out = tint_a + a_out * (1.0 - tint_a);
	col = tint_color.rgb * tint_a + col * (1.0 - tint_a);

	// 3) 底部 orin：越靠贴图上方扭得越厉害（热气上升），条纹用 +phase 才是向上飘
	vec2 onorm = (cuv - orin_center) * card_px / card_px.y;
	vec2 ouv = onorm / max(orin_scale, 1e-3) + 0.5;
	float ph2 = TIME * shimmer_speed;
	float rise = 1.0 - clamp(ouv.y, 0.0, 1.0);
	float jitter = texture(noise_texture, ouv * 1.5 + vec2(ph2 * 0.15, ph2 * 0.6)).r;
	ouv.x += (sin(ouv.y * 16.0 + ph2) * 0.6 + (jitter - 0.5) * 1.4) * shimmer * rise;
	ouv.y += sin(ouv.x * 12.0 - ph2 * 0.8) * shimmer * 0.35 * rise;
	vec4 orin = texture(orin_texture, ouv) * orin_tint;
	float a_orin = clamp(orin.a * orin_opacity * inside, 0.0, 1.0);
	a_out = a_orin + a_out * (1.0 - a_orin);
	col = orin.rgb * a_orin + col * (1.0 - a_orin);

	// 4) 火焰压在最上层（uv.y 加 t 即采样点下移 → 图案往屏幕上方走）
	vec2 fuv = vec2(cuv.x * fire_scale.x + (nz - 0.5) * warp, cuv.y * fire_scale.y + t);
	vec3 fire = texture(fire_texture, fuv).rgb;
	// 第二层走不同尺度与速度，避免单层贴图被一眼看穿是平铺的。
	fire = mix(fire, texture(fire_texture, fuv * 1.7 + vec2(0.37, t * 0.55)).rgb, layer2_mix);
	fire *= fire_tint.rgb * brightness;

	// 亮处更实、暗红处半透明，火焰才有“燃”的层次。
	float lum = clamp(dot(fire, vec3(0.299, 0.587, 0.114)), 0.0, 1.0);
	float a_fire = clamp(mask * opacity * (0.55 + 0.45 * lum), 0.0, 1.0);
	a_out = a_fire + a_out * (1.0 - a_fire);
	col = fire * a_fire + col * (1.0 - a_fire);

	COLOR = vec4(col, a_out);
}
