shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;
uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear;
uniform sampler2D smoke_tex : filter_linear;

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

float sampleSmoke(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	vec4 tex = texture(smoke_tex, clamp(uv, 0.0, 1.0));
	return max(tex.r, tex.a) * b.x * b.y;
}

vec2 evalBulletChain(vec2 p, vec2 origin, float angle, float scroll, float scale) {
	float c = cos(angle);
	float s = sin(angle);
	vec2 q = p - origin;
	vec2 lp = vec2(c * q.x + s * q.y, -s * q.x + c * q.y) / scale;
	lp.y += sin(lp.x * 1.4) * 0.08;
	float cell_w = 0.13;
	float u = lp.x + scroll;
	float id = round(u / cell_w);
	vec2 cell_p = vec2(u - id * cell_w, lp.y);
	float tilt = 0.25 * sin(id * 1.7);
	mat2 erot = mat2(vec2(cos(tilt), sin(tilt)), vec2(-sin(tilt), cos(tilt)));
	vec2 ep = erot * cell_p;
	float d_elli = (length(ep / vec2(0.056, 0.024)) - 1.0) * 0.032 * scale;
	return vec2(d_elli, id);
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float diag_coord = (p.x * 0.45 + p.y * 0.89);
	float band_id = floor(diag_coord * 5.0);
	float band_hash = hash(vec2(band_id, 4.7));

	float flip_start = 0.36 + band_hash * 0.14;
	float flip_end = flip_start + 0.34;
	float flip_p = smoothstep(flip_start, flip_end, t);
	float flip_ease = flip_p * flip_p * (3.0 - 2.0 * flip_p);

	float rot_theta = (1.0 - flip_ease) * PI;
	float cr = cos(rot_theta);
	float sr = sin(rot_theta);

	vec2 sc_center = SCREEN_UV - 0.5;
	float flip_pinch = sin(flip_ease * PI) * 0.12;
	vec2 sc_rot = vec2(cr * sc_center.x - sr * sc_center.y, sr * sc_center.x + cr * sc_center.y);
	sc_rot.x += sin(flip_ease * PI) * (band_hash - 0.5) * 0.08 * (1.0 - smoothstep(0.78, 0.92, t));
	sc_rot /= (1.0 - flip_pinch * abs(sc_center.x));
	vec2 sample_uv = clamp(sc_rot + 0.5, 0.002, 0.998);

	float chroma_shift = sin(flip_ease * PI) * 0.008 * (1.0 - smoothstep(0.80, 0.94, t));
	float scr_r = texture(screen_tex, clamp(sample_uv + vec2(chroma_shift, 0.0), 0.002, 0.998)).r;
	float scr_g = texture(screen_tex, sample_uv).g;
	float scr_b = texture(screen_tex, clamp(sample_uv - vec2(chroma_shift, 0.0), 0.002, 0.998)).b;
	vec3 room_col = vec3(scr_r, scr_g, scr_b);

	float flip_wave_glow = sin(flip_ease * PI);
	vec3 invert_tint = mix(room_col, vec3(1.0 - room_col.r * 0.5, room_col.g * 0.75, 1.0 - room_col.b * 0.3), flip_wave_glow * 0.35);
	room_col = mix(room_col, invert_tint, 1.0 - smoothstep(0.75, 0.90, t));

	vec2 shoji_p = vec2(p.x * 0.92 - p.y * 0.38, p.x * 0.38 + p.y * 0.92) + vec2(t * 0.4, -t * 0.3);
	vec2 grid_f = abs(fract(shoji_p * vec2(5.5, 3.8)) - 0.5);
	float shoji_frame = smoothstep(0.05, 0.015, min(grid_f.x, grid_f.y));
	float shoji_glow = exp(-length(p - vec2(-0.35, 0.12)) * 1.6);
	float smoke_val = sampleSmoke((shoji_p * 0.65) + 0.5) * 0.6 + noise(p * 3.5 - t * 0.8) * 0.4;

	vec3 col_wood_dark = vec3(0.11, 0.06, 0.07);
	vec3 col_shoji_amber = vec3(0.88, 0.44, 0.18);
	vec3 col_rebel_crimson = vec3(0.94, 0.08, 0.16);
	vec3 col_chain_core = vec3(0.14, 0.02, 0.04);

	vec3 cover_col = mix(col_shoji_amber * (0.45 + 0.55 * shoji_glow), col_wood_dark, clamp(shoji_frame + smoke_val * 0.65, 0.0, 1.0));

	float slash_open = pow(smoothstep(0.05, 0.38, t), 1.2) * 1.45;
	float slash_dist = abs(p.y * 0.78 - p.x * 0.62 + sin(p.x * 4.0) * 0.08) + (smoke_val - 0.5) * 0.22;
	float cover_mask = smoothstep(slash_open - 0.14, slash_open + 0.04, slash_dist);
	if (t < 0.05) {
		cover_mask = mix(1.0, cover_mask, smoothstep(0.0, 0.05, t));
	}
	float cover_rim = exp(-pow((slash_dist - slash_open) / 0.07, 2.0)) * (1.0 - smoothstep(0.32, 0.48, t));

	float chain_core_mask = 0.0;
	float chain_red_rim = 0.0;
	float chain_Retreat = pow(smoothstep(0.45, 0.94, t), 1.4) * 1.35;

	for (int i = 0; i < 6; i++) {
		float fi = float(i);
		float dir_s = (mod(fi, 2.0) < 0.5) ? 1.0 : -1.0;
		float c_ang = -0.65 + fi * 0.48 + sin(fi * 2.1) * 0.25;
		vec2 c_orig = vec2((fi - 2.5) * 0.24, (mod(fi, 3.0) - 1.0) * 0.18);
		vec2 ortho = vec2(-sin(c_ang), cos(c_ang));
		c_orig += ortho * dir_s * chain_Retreat;

		float c_scale = (i < 3) ? 1.15 : 0.78;
		vec2 ch = evalBulletChain(p, c_orig, c_ang, t * 1.4 * dir_s + fi * 0.7, c_scale);
		float d_e = ch.x;

		float core = smoothstep(0.003, -0.002, d_e);
		float rim = smoothstep(0.009 * c_scale, 0.004 * c_scale, abs(d_e));
		chain_core_mask = clamp(chain_core_mask + core, 0.0, 1.0);
		chain_red_rim = clamp(chain_red_rim + rim, 0.0, 1.0);
	}
	float chain_fade = 1.0 - smoothstep(0.82, 0.97, t);
	chain_core_mask *= chain_fade;
	chain_red_rim *= chain_fade;

	vec3 col = mix(room_col, cover_col, cover_mask);
	col = mix(col, col_rebel_crimson, cover_rim * 0.85);

	float band_edge = abs(fract(diag_coord * 5.0) - 0.5);
	float shear_line = smoothstep(0.49, 0.44, band_edge) * flip_wave_glow * (1.0 - cover_mask);
	col = mix(col, vec3(0.95, 0.24, 0.28), shear_line * 0.7);

	col = mix(col, col_chain_core, chain_core_mask);
	col = mix(col, col_rebel_crimson, chain_red_rim);

	float alpha = 1.0;
	if (t > 0.80) {
		float end_fade = smoothstep(1.0, 0.82, t);
		alpha = clamp(end_fade + chain_core_mask + chain_red_rim, 0.0, 1.0) * smoothstep(1.0, 0.95, t);
	}

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}