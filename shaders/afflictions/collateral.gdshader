shader_type canvas_item;
render_mode blend_premul_alpha;

// 抵押物：照原版 Smog 的做法——噪声图原样平铺，两层异速异向滚叠加，
// 再用「到卡边的距离」代替原版 ui_card_smog_mask.png，把图案收在边缘、中心留空。
// 切勿把噪声 UV 拧成（周长, 到边距离）：那会把火焰拉成一圈环带花纹，不再是流动的火焰。
// 配色 = #022782 实边 + #12e5f3 火焰。外轮廓由场景 card_mask 裁，本 shader 只管卡内。

uniform sampler2D aura_texture : filter_linear, repeat_enable;

// 卡面像素尺寸（NCard 默认 300x422）。
uniform vec2 card_px = vec2(300.0, 422.0);
uniform float corner_radius : hint_range(0.0, 0.15) = 0.055;

// —— 边框 ——
uniform vec4 border_color : source_color = vec4(0.007843, 0.152941, 0.509804, 1.0); // #022782
uniform float border_width : hint_range(0.0, 0.1) = 0.022;
uniform float border_softness : hint_range(0.0, 0.05) = 0.004;
uniform float border_alpha : hint_range(0.0, 1.0) = 0.95;

// —— 火焰 ——
uniform vec4 flame_color : source_color = vec4(0.070588, 0.898039, 0.952941, 1.0); // #12e5f3
// 最亮火苗的提亮色，做出温度层次
uniform vec4 flame_hot_color : source_color = vec4(0.72, 0.97, 1.0, 1.0);
uniform float flame_strength : hint_range(0.0, 3.0) = 1.5;
uniform float flame_alpha : hint_range(0.0, 1.0) = 0.9;

// —— 噪声两层 ——
uniform vec2 noise_tile = vec2(1.6, 1.6);
uniform vec2 secondary_tile = vec2(2.9, 2.4);
uniform float noise_mix : hint_range(0.0, 1.0) = 0.35;

// —— 边缘收拢（替代原版 mask 贴图）——
// 火焰带宽度（卡高 = 1）
uniform float smoke_width : hint_range(0.02, 0.5) = 0.24;
uniform float smoke_falloff : hint_range(0.5, 6.0) = 1.6;
// 阈值切火苗：越高只剩最亮的火舌
uniform vec2 erosion = vec2(0.08, 0.58);

// —— 流动 ——
// 噪声整体绕卡心顺时针漂移（等比坐标，卡高 = 1）；椭圆轨道贴合竖卡，边缘处更接近切向
uniform vec2 orbit_radius = vec2(0.03, 0.07);
uniform float orbit_speed : hint_range(0.0, 2.0) = 0.6;
// 叠加线性漂移（UV/秒）：火往上飘
uniform vec2 drift = vec2(0.0, -0.035);
uniform float flicker_speed : hint_range(0.0, 20.0) = 6.0;
uniform float flicker_amount : hint_range(0.0, 1.0) = 0.3;

// 顶点色不能丢：卡牌淡出/变灰时 overlay 要跟着一起变。
varying vec4 vertex_color;

float rounded_rect_sdf(vec2 p, vec2 half_size, float radius) {
	vec2 d = abs(p) - half_size + vec2(radius);
	return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0) - radius;
}

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec2 p = (UV - 0.5) * card_px;
	float sdf = rounded_rect_sdf(p, card_px * 0.5, corner_radius * card_px.y);
	float aa = max(fwidth(sdf), 1e-4);
	float inside = 1.0 - smoothstep(-aa, aa, sdf);
	// 到轮廓的向内距离，换算成「卡高 = 1」
	float dn = max(-sdf, 0.0) / card_px.y;

	// 等比坐标：UV 的 x 被 300:422 压扁，不换掉噪声圆斑会变竖椭圆
	vec2 q = UV * card_px / card_px.y;

	float t = TIME;
	// 屏幕 Y 向下 → 相位增大即顺时针
	vec2 orbit = vec2(cos(t * orbit_speed) * orbit_radius.x, sin(t * orbit_speed) * orbit_radius.y);
	vec2 flow = orbit + drift * t;

	// 取 G 通道：这张图暗部是纯深蓝（G≈0），亮部是青/白（G 高），区分度最好
	float n1 = texture(aura_texture, q * noise_tile + flow).g;
	float n2 = texture(aura_texture, q * secondary_tile - flow * 1.35 + vec2(0.37, 0.61)).g;
	float n = mix(n1, n2, noise_mix);

	// 贴边浓、向内淡出：替代原版 smog 的 mask 贴图
	float falloff = pow(1.0 - smoothstep(0.0, smoke_width, dn), smoke_falloff);

	// 阈值切出火苗（原版 erosion_factors 的做法）
	float flame = smoothstep(erosion.x, erosion.y, n * falloff);
	// 局部明暗跳动，火才活
	flame *= mix(1.0, 0.5 + 0.5 * sin(t * flicker_speed + n * 12.566), flicker_amount);

	vec3 flame_rgb = mix(flame_color.rgb, flame_hot_color.rgb, smoothstep(0.55, 1.0, flame));

	// —— 底层：深蓝实边框 ——
	float border = 1.0 - smoothstep(border_width - border_softness, border_width + border_softness, dn);
	float a_border = border * border_alpha * inside;
	float a_flame = clamp(flame * flame_alpha * inside, 0.0, 1.0);

	vec3 col = border_color.rgb * a_border;
	float cov = a_border;
	// 火焰走加法：预乘下 rgb > alpha 即提亮背景，青火才透得出来
	col += flame_rgb * a_flame * flame_strength;
	cov = clamp(cov + a_flame * 0.9, 0.0, 1.0);

	// 预乘输出：rgb 与 alpha 一起乘顶点色
	float va = vertex_color.a;
	COLOR = vec4(col * vertex_color.rgb * va, cov * va);
}
