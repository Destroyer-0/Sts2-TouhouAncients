shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;
uniform sampler2D smoke_tex : filter_linear;
uniform sampler2D flame_tex : filter_linear;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
	           mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

float fbm(vec2 p) {
	float v = 0.0;
	float a = 0.5;
	mat2 rot = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));
	for (int i = 0; i < 4; i++) {
		v += a * noise(p);
		p = rot * p * 2.02 + vec2(4.7, 2.3);
		a *= 0.5;
	}
	return v;
}

float sampleSmoke(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	vec4 tex = texture(smoke_tex, clamp(uv, 0.0, 1.0));
	return max(tex.r, tex.a) * b.x * b.y;
}

float sampleFlame(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	vec4 tex = texture(flame_tex, clamp(uv, 0.0, 1.0));
	return max(tex.r, tex.a) * b.x * b.y;
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float pipe_angle = -0.22;
	mat2 rot_pipe = mat2(vec2(cos(pipe_angle), sin(pipe_angle)), vec2(-sin(pipe_angle), cos(pipe_angle)));
	vec2 rp = rot_pipe * p;

	vec2 bowl_pos = vec2(-0.56, 0.14);
	vec2 face_pos = vec2(0.38, -0.08);

	float paper_grain = noise(p * vec2(28.0, 22.0)) * 0.5 + noise(p * vec2(12.0, 45.0)) * 0.5;
	float sketch_hatch = sin((rp.x + rp.y * 1.8) * 65.0 + paper_grain * 4.0) * 0.5 + 0.5;
	float sun_backlight = exp(-length(p - vec2(-0.22, -0.34)) * 1.85);

	float ribbon_wisp = 0.0;
	float ink_stroke = 0.0;
	float leaf_glint = 0.0;

	for (int i = 0; i < 6; i++) {
		float fi = float(i);
		float y_base = -0.24 + fi * 0.095;
		float wave_freq = 2.6 + mod(fi, 3.0) * 0.7;
		float speed = (1.6 + 0.4 * mod(fi, 2.0)) * ((mod(fi, 2.0) < 0.5) ? 1.0 : -1.0);
		float spread = (fi - 2.5) * pow(smoothstep(0.18, 0.88, t), 1.3) * 0.36;

		float curve_y = rp.y - (y_base + spread) - (0.08 + 0.03 * sin(t * 2.0 + fi)) * sin(rp.x * wave_freq - t * speed + fi * 1.3);
		float d_rib = abs(curve_y);

		float draw_front = smoothstep(-1.1 + t * 2.8, -0.3 + t * 2.8, rp.x * ((mod(fi, 2.0) < 0.5) ? 1.0 : -1.0));
		float w = 0.032 + 0.018 * sin(rp.x * 3.5 + fi);
		ribbon_wisp += exp(-pow(d_rib / w, 2.0)) * (0.5 + 0.5 * draw_front) * 0.55;
		ink_stroke += exp(-pow((d_rib - w * 0.65) / 0.007, 2.0)) * draw_front * 0.65;
		if (i == 1 || i == 4) {
			leaf_glint += exp(-pow(d_rib / 0.014, 2.0)) * smoothstep(0.0, 0.5, p.x) * 0.7;
		}
	}
	ribbon_wisp = clamp(ribbon_wisp, 0.0, 1.0);
	ink_stroke = clamp(ink_stroke, 0.0, 1.0);
	leaf_glint = clamp(leaf_glint, 0.0, 1.0);

	float plume_tex = 0.0;
	float spark_tex = 0.0;
	for (int i = 0; i < 8; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 3.7));
		float h2 = hash(vec2(fi, 8.9));

		float slide_dir = (mod(fi, 2.0) < 0.5) ? 1.0 : -1.0;
		float slide_amt = pow(smoothstep(0.12, 0.88, t), 1.25) * 1.35 * slide_dir;
		vec2 c_pos = vec2((h1 - 0.5) * 1.3 + slide_amt, (h2 - 0.5) * 0.75 - t * 0.25);

		float ang = pipe_angle + (h1 - 0.5) * 0.8 + t * 0.7 * slide_dir;
		float ca = cos(ang);
		float sa = sin(ang);
		vec2 rel = p - c_pos;
		vec2 l_uv = vec2(ca * rel.x + sa * rel.y, -sa * rel.x + ca * rel.y);

		vec2 suv = l_uv / vec2(0.82 + 0.35 * h1, 0.38 + 0.16 * h2) + 0.5;
		vec2 fuv = l_uv / vec2(0.45 + 0.2 * h2, 0.16 + 0.08 * h1) + 0.5;

		plume_tex += sampleSmoke(suv) * 0.55;
		spark_tex += sampleFlame(fuv) * 0.6;
	}
	plume_tex = clamp(plume_tex, 0.0, 1.0);
	spark_tex = clamp(spark_tex, 0.0, 1.0);

	float flow_fbm = fbm(rp * vec2(2.0, 3.8) + vec2(-t * 0.8, t * 0.3));

	float d_face_window = length((p - face_pos) * vec2(0.88, 0.92)) - pow(smoothstep(0.06, 0.72, t), 1.2) * 1.35;
	float band_y = rp.y + 0.08 * sin(rp.x * 3.2 - t * 3.0);
	float band_id = floor(band_y * 4.2);
	float band_dir = (mod(band_id, 2.0) < 0.5) ? 1.0 : -1.0;
	float horiz_slide = (rp.x * band_dir) - (-0.75 + pow(smoothstep(0.10, 0.88, t), 1.25) * 2.1);

	float reveal_field = min(d_face_window, horiz_slide * 0.65) + (flow_fbm - 0.5) * 0.26 + (plume_tex - 0.4) * 0.22;
	float mask = smoothstep(-0.14, 0.10, reveal_field);
	if (t < 0.07) {
		mask = mix(1.0, mask, smoothstep(0.0, 0.07, t));
	}

	float sketch_rim = exp(-pow(reveal_field / 0.045, 2.0)) * smoothstep(0.02, 0.15, t) * (1.0 - smoothstep(0.80, 0.98, t));
	float warm_halo_rim = exp(-pow((reveal_field + 0.05) / 0.10, 2.0)) * smoothstep(0.04, 0.18, t) * (1.0 - smoothstep(0.78, 0.98, t));
	float bowl_ember = exp(-pow(length(p - bowl_pos) / 0.14, 2.0) * 1.8) * (1.0 - smoothstep(0.55, 0.88, t));

	vec3 col_espresso = vec3(0.18, 0.11, 0.08);
	vec3 col_warm_sepia = vec3(0.64, 0.42, 0.28);
	vec3 col_sun_amber = vec3(0.96, 0.72, 0.44);
	vec3 col_cream_smoke = vec3(0.99, 0.96, 0.90);
	vec3 col_leaf_mint = vec3(0.46, 0.84, 0.42);
	vec3 col_charcoal = vec3(0.12, 0.07, 0.05);

	vec3 bg = mix(col_espresso, col_warm_sepia, smoothstep(0.15, 0.82, flow_fbm * 0.7 + paper_grain * 0.3));
	bg = mix(bg, col_sun_amber, sun_backlight * 0.65);
	bg *= (0.92 + 0.08 * sketch_hatch);

	float fx_fade = 1.0 - smoothstep(0.78, 0.98, t);
	vec3 col = mix(bg, col_cream_smoke, clamp((plume_tex * 0.65 + ribbon_wisp * 0.75) * fx_fade, 0.0, 1.0));
	col = mix(col, col_sun_amber, clamp(warm_halo_rim * 0.8 + spark_tex * fx_fade * 0.55, 0.0, 1.0));
	col = mix(col, col_leaf_mint, leaf_glint * fx_fade * 0.75);
	col = mix(col, col_charcoal, clamp(ink_stroke * fx_fade * 0.8 + sketch_rim * 0.85, 0.0, 1.0));
	col += vec3(0.98, 0.55, 0.22) * bowl_ember * 0.8;

	float alpha = clamp(mask + sketch_rim * 0.85 + warm_halo_rim * 0.7 + (ribbon_wisp * 0.8 + ink_stroke * 0.7 + leaf_glint * 0.6) * fx_fade + bowl_ember * 0.5, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}