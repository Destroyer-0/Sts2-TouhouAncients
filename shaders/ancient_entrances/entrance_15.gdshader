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
	mat2 rot = mat2(vec2(0.8, -0.6), vec2(0.6, 0.8));
	for (int i = 0; i < 4; i++) {
		v += a * noise(p);
		p = rot * p * 2.03 + vec2(3.4, 6.1);
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

vec2 ukiyoSpiral(vec2 p, vec2 center, float dir, float rot_t) {
	vec2 q = p - center;
	float r = length(q);
	float a = atan(q.y, q.x) + dir * log(max(0.025, r)) * 2.8 - rot_t * dir;
	return vec2(cos(a), sin(a)) * r;
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	vec2 pipe_bowl = vec2(-0.46, 0.22);
	vec2 face_focus = vec2(0.10, -0.14);

	vec2 ember_p = p - pipe_bowl;
	float ember_glow = exp(-pow(length(ember_p) / 0.15, 2.0) * 1.8) * smoothstep(0.0, 0.12, t) * (1.0 - smoothstep(0.45, 0.76, t));
	float pipe_spark = sampleFlame((ember_p / 0.20) + 0.5) * smoothstep(0.0, 0.10, t) * (1.0 - smoothstep(0.42, 0.72, t));

	float scroll_open = pow(smoothstep(0.10, 0.88, t), 1.25);
	vec2 radial_dir = normalize(p - face_focus + vec2(0.001, 0.001));
	vec2 drift_p = p - radial_dir * scroll_open * 0.85;

	vec2 sp_bl = ukiyoSpiral(drift_p, vec2(-0.52, 0.28), 1.0, t * 2.0);
	vec2 sp_tr = ukiyoSpiral(drift_p, vec2(0.56, -0.26), -1.0, t * 1.8);
	vec2 sp_tl = ukiyoSpiral(drift_p, vec2(-0.48, -0.32), -1.0, t * 1.6);
	vec2 sp_br = ukiyoSpiral(drift_p, vec2(0.52, 0.30), 1.0, t * 1.9);

	vec2 top_sp = mix(sp_tl, sp_tr, smoothstep(-0.4, 0.4, p.x));
	vec2 bot_sp = mix(sp_bl, sp_br, smoothstep(-0.4, 0.4, p.x));
	vec2 cloud_coord = mix(top_sp, bot_sp, smoothstep(-0.3, 0.3, p.y));

	float cloud_fbm = fbm(cloud_coord * 3.0 + fbm(drift_p * 2.4 - vec2(t * 0.4, -t * 0.3)));
	float curl_ridge = sin(length(cloud_coord) * 18.0 - atan(cloud_coord.y, cloud_coord.x) * 2.0 + cloud_fbm * 5.0) * 0.5 + 0.5;

	float smoke_fg = 0.0;
	float smoke_bg = 0.0;
	float wisp_gold = 0.0;
	float wisp_leaf = 0.0;

	for (int i = 0; i < 9; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 2.3));
		float h2 = hash(vec2(fi, 7.1));

		float orbit_a = fi * 0.6981 + t * (1.1 + 0.4 * h1) * (mod(fi, 2.0) * 2.0 - 1.0);
		float orbit_r = (0.18 + 0.28 * h2) + scroll_open * (0.75 + 0.35 * h1);
		vec2 c_pos = face_focus + vec2(cos(orbit_a) * 1.25, sin(orbit_a)) * orbit_r;

		float ang = orbit_a + 1.5708;
		float ca = cos(ang);
		float sa = sin(ang);
		vec2 rel = p - c_pos;
		vec2 rot_p = vec2(ca * rel.x + sa * rel.y, -sa * rel.x + ca * rel.y);

		vec2 suv = rot_p / vec2(0.65 + 0.25 * h1, 0.46 + 0.18 * h2) + 0.5;
		vec2 fuv = rot_p / vec2(0.35 + 0.14 * h2, 0.18 + 0.08 * h1) + 0.5;

		float s_val = sampleSmoke(suv);
		float f_val = sampleFlame(fuv);

		if (i < 5) {
			smoke_fg += s_val * 0.55;
		} else {
			smoke_bg += s_val * 0.45;
		}

		float morph_phase = smoothstep(0.16, 0.62, t + (h1 - 0.5) * 0.22);
		wisp_leaf += f_val * (1.0 - morph_phase) * 0.75;
		wisp_gold += f_val * morph_phase * 0.85;
	}
	smoke_fg = clamp(smoke_fg, 0.0, 1.0);
	smoke_bg = clamp(smoke_bg, 0.0, 1.0);
	wisp_leaf = clamp(wisp_leaf, 0.0, 1.0);
	wisp_gold = clamp(wisp_gold, 0.0, 1.0);

	vec2 frame_p = (p - vec2(0.04, -0.06)) * vec2(0.78, 0.96);
	float scallop = sin(atan(frame_p.y, frame_p.x) * 6.0 + t * 2.2) * 0.08;
	float scroll_field = length(frame_p) + scallop + (cloud_fbm - 0.5) * 0.34 + (curl_ridge - 0.5) * 0.12;

	float open_thresh = pow(smoothstep(0.08, 0.88, t), 1.24) * 1.52;
	float mask = smoothstep(open_thresh - 0.16, open_thresh + 0.05, scroll_field);
	if (t < 0.08) {
		mask = mix(1.0, mask, smoothstep(0.0, 0.08, t));
	}

	float sumi_border = exp(-pow((scroll_field - open_thresh) / 0.042, 2.0));
	float sumi_swirl = exp(-pow((curl_ridge - 0.52) / 0.055, 2.0)) * mask * 0.8;
	float sumi_ink_line = clamp(sumi_border * 1.1 + sumi_swirl, 0.0, 1.0) * smoothstep(0.02, 0.14, t) * (1.0 - smoothstep(0.80, 0.98, t));
	float inner_glow_rim = exp(-pow((scroll_field - open_thresh + 0.045) / 0.085, 2.0)) * smoothstep(0.04, 0.18, t) * (1.0 - smoothstep(0.78, 0.98, t));

	vec3 col_night_plum = vec3(0.13, 0.08, 0.22);
	vec3 col_wisteria_cloud = vec3(0.48, 0.35, 0.58);
	vec3 col_pale_lavender = vec3(0.95, 0.89, 0.97);
	vec3 col_sumi_ink = vec3(0.07, 0.03, 0.11);
	vec3 col_haori_coral = vec3(0.86, 0.42, 0.32);
	vec3 col_lotus_green = vec3(0.38, 0.74, 0.36);
	vec3 col_bell_gold = vec3(0.98, 0.82, 0.38);

	float billow = smoothstep(0.18, 0.80, cloud_fbm * 0.65 + smoke_bg * 0.45);
	vec3 col = mix(col_night_plum, col_wisteria_cloud, billow);
	col = mix(col, col_haori_coral, smoothstep(0.35, 0.85, smoke_bg * curl_ridge) * 0.35);
	col = mix(col, col_pale_lavender, smoothstep(0.46, 0.86, cloud_fbm * 0.55 + smoke_fg * 0.45 + curl_ridge * 0.2));

	float fx_fade = 1.0 - smoothstep(0.78, 0.98, t);
	col = mix(col, col_lotus_green, wisp_leaf * fx_fade * 0.85);
	col = mix(col, col_bell_gold, clamp(wisp_gold * fx_fade * 0.9 + inner_glow_rim * 0.75, 0.0, 1.0));
	col = mix(col, col_sumi_ink, sumi_ink_line * 0.94);
	col += mix(col_haori_coral, col_bell_gold, 0.5) * (ember_glow * 0.85 + pipe_spark * 0.9);

	float alpha = clamp(mask + sumi_ink_line * 0.9 + inner_glow_rim * 0.7 + (wisp_leaf + wisp_gold) * 0.75 * fx_fade + ember_glow * 0.6, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}