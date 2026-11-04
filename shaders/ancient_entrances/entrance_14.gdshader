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
		p = rot * p * 2.03 + vec2(2.9, 5.3);
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

	float s_divide = p.x + 0.24 * sin(p.y * 3.4 - t * 2.8) + 0.08 * cos(p.y * 6.0 + t * 1.5);
	float side_blend = smoothstep(-0.18, 0.18, s_divide);

	vec2 p_jyoon = p + vec2(t * 0.22, t * 0.55);
	vec2 p_shion = p + vec2(-t * 0.18, -t * 0.50);

	float n_jyoon = fbm(p_jyoon * vec2(2.4, 3.2) + fbm(p_jyoon * 2.0));
	float n_shion = fbm(p_shion * vec2(2.8, 2.2) + fbm(p_shion * 2.5));

	float tex_jyoon = 0.0;
	float tex_shion = 0.0;
	for (int i = 0; i < 8; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 1.9));
		float h2 = hash(vec2(fi, 6.4));
		if (i < 4) {
			vec2 jc = vec2(-0.42 + (h1 - 0.5) * 0.55 - t * 0.35, 0.35 - t * (0.65 + 0.3 * h2) + (h2 - 0.5) * 0.5);
			float jang = -0.4 + h1 * 1.2 + t * 1.1;
			mat2 jrot = mat2(vec2(cos(jang), sin(jang)), vec2(-sin(jang), cos(jang)));
			vec2 fuv = (jrot * (p - jc)) / vec2(0.55 + 0.25 * h1, 0.35 + 0.15 * h2) + 0.5;
			tex_jyoon += sampleFlame(fuv) * 0.65;
		} else {
			vec2 sc = vec2(0.42 + (h1 - 0.5) * 0.55 + t * 0.35, -0.35 + t * (0.60 + 0.25 * h2) + (h2 - 0.5) * 0.5);
			float sang = 0.5 + h2 * 3.14 - t * 0.8;
			mat2 srot = mat2(vec2(cos(sang), sin(sang)), vec2(-sin(sang), cos(sang)));
			vec2 suv = (srot * (p - sc)) / vec2(0.70 + 0.3 * h2, 0.50 + 0.2 * h1) + 0.5;
			tex_shion += sampleSmoke(suv) * 0.6;
		}
	}
	tex_jyoon = clamp(tex_jyoon, 0.0, 1.0);
	tex_shion = clamp(tex_shion, 0.0, 1.0);

	vec2 jyoon_face = vec2(-0.32, -0.10);
	vec2 shion_face = vec2(0.32, -0.08);

	float open_j = pow(smoothstep(0.08, 0.86, t), 1.2) * 1.45;
	float open_s = pow(smoothstep(0.12, 0.90, t), 1.25) * 1.45;

	vec2 rj = p - jyoon_face;
	float dist_j = length(rj * vec2(0.95, 0.82)) + (n_jyoon - 0.5) * 0.28 + p.y * 0.18 * smoothstep(0.1, 0.6, t);
	vec2 rs = p - shion_face;
	float dist_s = length(rs * vec2(0.95, 0.82)) + (n_shion - 0.5) * 0.28 - p.y * 0.18 * smoothstep(0.1, 0.6, t);

	float mask_j = smoothstep(open_j - 0.18, open_j + 0.06, dist_j);
	float mask_s = smoothstep(open_s - 0.18, open_s + 0.06, dist_s);
	float mask = mix(mask_j, mask_s, side_blend);
	if (t < 0.08) {
		mask = mix(1.0, mask, smoothstep(0.0, 0.08, t));
	}

	float rim_j = exp(-pow((dist_j - open_j) / 0.10, 2.0)) * (1.0 - side_blend);
	float rim_s = exp(-pow((dist_s - open_s) / 0.10, 2.0)) * side_blend;
	float center_clash = exp(-pow(s_divide / 0.08, 2.0)) * (1.0 - smoothstep(0.25, 0.78, t));

	float gold_dust = 0.0;
	float jewel_glint = 0.0;
	float ink_motes = 0.0;
	for (int i = 0; i < 8; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 4.3));
		float h2 = hash(vec2(fi, 9.1));
		if (i < 4) {
			vec2 gpos = vec2(-0.12 - h1 * 0.68 - t * 0.25, 0.42 - fract(h2 + t * (0.55 + 0.25 * h1)) * 0.95);
			gpos.x += sin(t * 3.5 + fi * 2.0) * 0.06;
			float d_g = length(p - gpos);
			gold_dust += exp(-pow(d_g / (0.045 + 0.03 * h2), 2.0)) * 0.55;
			vec2 gp = p - gpos;
			float star_cross = exp(-abs(gp.x + gp.y) * 95.0) * exp(-abs(gp.x - gp.y) * 22.0) + exp(-abs(gp.x - gp.y) * 95.0) * exp(-abs(gp.x + gp.y) * 22.0);
			jewel_glint += star_cross * (0.5 + 0.5 * sin(t * 9.0 + fi));
		} else {
			vec2 ipos = vec2(0.12 + h1 * 0.68 + t * 0.2, -0.42 + fract(h2 + t * (0.45 + 0.2 * h1)) * 0.95);
			ipos.x += cos(t * 2.5 + fi) * 0.05;
			float d_i = length((p - ipos) * vec2(1.0, 0.65));
			ink_motes += smoothstep(0.025 + 0.015 * h2, 0.004, d_i) * 0.7;
		}
	}
	float fx_fade = smoothstep(0.02, 0.16, t) * (1.0 - smoothstep(0.78, 0.98, t));
	gold_dust = clamp(gold_dust, 0.0, 1.0) * fx_fade;
	jewel_glint = clamp(jewel_glint, 0.0, 1.0) * fx_fade;
	ink_motes = clamp(ink_motes, 0.0, 1.0) * fx_fade;

	vec3 col_jyoon_wine = vec3(0.28, 0.06, 0.18);
	vec3 col_jyoon_rose = vec3(0.76, 0.22, 0.44);
	vec3 col_jyoon_gold = vec3(0.98, 0.78, 0.30);
	vec3 col_jewel_cyan = vec3(0.24, 0.88, 0.82);

	vec3 col_shion_void = vec3(0.06, 0.08, 0.16);
	vec3 col_shion_slate = vec3(0.32, 0.38, 0.52);
	vec3 col_shion_indigo = vec3(0.18, 0.32, 0.78);
	vec3 col_shion_paper = vec3(0.84, 0.86, 0.90);

	vec3 bg_j = mix(col_jyoon_wine, col_jyoon_rose, smoothstep(0.15, 0.82, n_jyoon));
	bg_j = mix(bg_j, col_jyoon_gold, clamp(tex_jyoon * 0.75 + smoothstep(0.55, 0.92, n_jyoon) * 0.5, 0.0, 1.0));

	vec3 bg_s = mix(col_shion_void, col_shion_slate, smoothstep(0.18, 0.85, n_shion));
	bg_s = mix(bg_s, col_shion_paper, smoothstep(0.45, 0.0, p.y + n_shion * 0.4) * 0.35);
	bg_s = mix(bg_s, col_shion_indigo, tex_shion * 0.65);
	bg_s = mix(bg_s, vec3(0.03, 0.04, 0.08), ink_motes);

	vec3 col = mix(bg_j, bg_s, side_blend);
	col = mix(col, col_jyoon_gold, (rim_j * 0.9 + gold_dust * 0.85) * fx_fade);
	col = mix(col, mix(col_shion_indigo, col_shion_paper, 0.55), rim_s * 0.85 * fx_fade);
	col = mix(col, mix(col_jyoon_gold, col_jewel_cyan, 0.5), center_clash * 0.85);
	col += mix(col_jyoon_gold, col_jewel_cyan, 0.4) * jewel_glint * 0.9;

	float total_rim = (rim_j + rim_s + center_clash) * fx_fade;
	float alpha = clamp(mask + total_rim * 0.85 + (gold_dust * 0.6 + jewel_glint * 0.8 + ink_motes * 0.6 + (tex_jyoon * (1.0 - side_blend) + tex_shion * side_blend) * 0.45 * fx_fade), 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}