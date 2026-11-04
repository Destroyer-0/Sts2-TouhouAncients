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

float sdSegment(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
	return length(pa - ba * h);
}

vec4 evalBroadLeaf(vec2 p, vec2 base, float angle, float len, float wid, float bend) {
	vec2 rel = p - base;
	float ca = cos(angle);
	float sa = sin(angle);
	vec2 lp = vec2(ca * rel.x + sa * rel.y, -sa * rel.x + ca * rel.y);
	float v = clamp(lp.y / len, 0.0, 1.0);
	float curve_x = bend * v * v * len;
	float lx = lp.x - curve_x;
	float env = sin(pow(v, 0.72) * 3.14159265) * wid;
	float d = abs(lx) - env;
	if (lp.y < 0.0) d = length(vec2(lx, lp.y));
	if (lp.y > len) d = length(vec2(lx, lp.y - len));
	float norm_u = lx / max(0.002, env);
	float veins = sin(abs(norm_u) * 26.0 - v * 5.0) * 0.5 + 0.5;
	return vec4(d, norm_u, v, veins);
}

vec4 evalLilyBell(vec2 p, vec2 attach, float hang_ang, float size) {
	vec2 q = (p - attach) / size;
	float ca = cos(hang_ang);
	float sa = sin(hang_ang);
	q = vec2(ca * q.x + sa * q.y, -sa * q.x + ca * q.y);
	q.y -= 0.42;
	float v = clamp((q.y + 0.42) / 0.88, 0.0, 1.0);
	float dome = sin(pow(v, 0.58) * 2.28) * 0.44;
	float lip_flare = pow(smoothstep(0.60, 1.0, v), 1.7) * 0.21;
	float r_w = max(0.01, dome + lip_flare);
	float u = q.x / r_w;
	float teeth_wave = cos(clamp(u, -1.1, 1.1) * 3.14159 * 1.5) * 0.048 * smoothstep(0.55, 1.0, v);
	float y_bottom = 0.46 + teeth_wave - 0.035 * (1.0 - min(1.0, u * u));
	float y_top = -0.42;
	float d_side = (abs(q.x) - r_w) * 0.75;
	float d_vert = max(y_top - q.y, q.y - y_bottom);
	float d_bell = max(d_side, d_vert) * size;
	float calyx_d = (max(abs(q.x) - 0.14 * (1.0 - v * 2.5), abs(q.y + 0.40) - 0.09)) * size;
	float calyx_m = smoothstep(0.005, -0.002, calyx_d);
	return vec4(d_bell, clamp(u, -1.0, 1.0), v, calyx_m);
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float tug_impulse = smoothstep(0.14, 0.25, t) * (1.0 - 0.55 * smoothstep(0.25, 0.44, t));
	float lift_open = pow(smoothstep(0.30, 0.90, t), 1.28);
	float slack = (1.0 - smoothstep(0.14, 0.24, t)) * 0.055;

	float bg_n = noise(p * 3.2 - vec2(0.0, t * 0.4)) * 0.6 + noise(p * 6.5 + vec2(t * 0.3, 0.0)) * 0.4;

	vec2 focus_c = vec2(0.12, -0.04) - vec2(0.0, tug_impulse * 0.04);
	vec2 fp = p - focus_c;
	float foliage_wave = sin(atan(fp.y, fp.x) * 5.0 + bg_n * 3.5) * 0.08;
	float garden_gap = length(fp * vec2(0.76, 0.92)) + foliage_wave + (bg_n - 0.5) * 0.22;
	float open_radius = pow(smoothstep(0.28, 0.88, t), 1.22) * 1.62 + tug_impulse * 0.08;
	float base_mask = smoothstep(open_radius - 0.18, open_radius + 0.08, garden_gap);
	if (t < 0.22) {
		base_mask = 1.0;
	} else {
		base_mask = mix(1.0, base_mask, smoothstep(0.22, 0.34, t));
	}

	vec3 col_night_garden = vec3(0.05, 0.04, 0.09);
	vec3 col_deep_moss = vec3(0.07, 0.18, 0.14);
	vec3 col_ribbon_crimson = vec3(0.46, 0.06, 0.14);
	vec3 col_venom_purple = vec3(0.54, 0.22, 0.78);
	vec3 col_venom_mint = vec3(0.46, 0.96, 0.64);
	vec3 col_bell_pearl = vec3(0.95, 0.98, 0.96);
	vec3 col_bell_shadow = vec3(0.60, 0.72, 0.78);

	vec3 col = mix(col_night_garden, col_deep_moss, smoothstep(0.15, 0.85, bg_n));
	float crimson_bow_hint = exp(-length((p - vec2(0.08, 0.26)) * vec2(0.8, 1.3)) * 2.6) * (1.0 - lift_open);
	col = mix(col, col_ribbon_crimson, crimson_bow_hint * 0.48);

	float leaves_alpha = 0.0;
	for (int i = 0; i < 4; i++) {
		float fi = float(i);
		float side = (i < 2) ? -1.0 : 1.0;
		float layer = mod(fi, 2.0);
		vec2 l_base = vec2(side * (0.20 + 0.22 * layer + lift_open * (0.88 + 0.25 * layer)), 0.56 - tug_impulse * 0.06 + lift_open * 0.18);
		float l_ang = -side * (0.28 + 0.20 * layer + lift_open * 0.42) + sin(t * 2.2 + fi) * 0.03;
		float l_len = 0.88 + 0.14 * layer;
		float l_wid = 0.22 + 0.04 * layer;
		float l_bend = -side * (0.24 + 0.08 * layer);

		vec4 lf = evalBroadLeaf(p, l_base, l_ang, l_len, l_wid, l_bend);
		float l_mask = smoothstep(0.008, -0.004, lf.x);
		float midrib = exp(-abs(lf.y) * 18.0);
		float side_light = smoothstep(-0.8, 0.8, lf.y * side);
		vec3 l_col = mix(vec3(0.05, 0.16, 0.12), vec3(0.16, 0.42, 0.28), side_light * 0.65 + lf.w * 0.35);
		l_col = mix(l_col, vec3(0.32, 0.68, 0.44), midrib * 0.55 + pow(1.0 - abs(lf.y), 3.0) * 0.15);
		float rim_v = smoothstep(0.015, 0.0, abs(lf.x)) * smoothstep(0.15, 0.55, t);
		l_col += mix(col_venom_mint, col_venom_purple, layer) * rim_v * 0.45;

		float shade_depth = 0.72 + 0.28 * layer;
		col = mix(col, l_col * shade_depth, l_mask);
		leaves_alpha = clamp(leaves_alpha + l_mask, 0.0, 1.0);
	}

	float poison_spread = smoothstep(0.18, 0.58, t);
	float smoke_acc = 0.0;
	float venom_glow_acc = 0.0;
	float bells_alpha = 0.0;
	float threads_alpha = 0.0;

	for (int i = 0; i < 4; i++) {
		float fi = float(i);
		float side = (i < 2) ? -1.0 : 1.0;
		float inner = mod(fi, 2.0);

		float retract_x = side * lift_open * (0.92 + 0.22 * (1.0 - inner));
		float retract_y = -tug_impulse * (0.09 + 0.03 * inner) - lift_open * (0.35 + 0.15 * inner);
		vec2 root = vec2(side * (0.38 + 0.24 * (1.0 - inner)) + retract_x, 0.54 + retract_y);
		vec2 tip = vec2(side * (0.04 + 0.16 * (1.0 - inner)) + retract_x * 1.08, -0.22 - 0.14 * inner + retract_y * 1.25);
		vec2 ctrl = mix(root, tip, 0.55) + vec2(-side * 0.22, -0.26 - tug_impulse * 0.06);

		vec2 prev_pt = root;
		float d_stem = 10.0;
		float stem_param = 0.0;
		for (int s = 1; s <= 4; s++) {
			float st = float(s) * 0.25;
			float omt = 1.0 - st;
			vec2 cur_pt = omt * omt * root + 2.0 * omt * st * ctrl + st * st * tip;
			float ds = sdSegment(p, prev_pt, cur_pt);
			if (ds < d_stem) {
				d_stem = ds;
				stem_param = st;
			}
			prev_pt = cur_pt;
		}

		float stem_w = 0.0095 * (1.2 - 0.45 * stem_param);
		float stem_m = smoothstep(stem_w + 0.003, stem_w - 0.002, d_stem);
		float stem_Sap = exp(-pow(d_stem / 0.022, 2.0)) * smoothstep(0.0, poison_spread, 1.0 - stem_param * 0.6) * smoothstep(0.18, 0.35, t);
		vec3 stem_col = mix(vec3(0.14, 0.38, 0.24), col_venom_mint, clamp(stem_Sap * 1.2, 0.0, 1.0));
		col = mix(col, stem_col, stem_m);
		col += mix(col_venom_mint, col_venom_purple, stem_param) * stem_Sap * 0.45;
		bells_alpha = clamp(bells_alpha + stem_m, 0.0, 1.0);
		venom_glow_acc = clamp(venom_glow_acc + stem_Sap * 0.5, 0.0, 1.0);

		float st_mid = 0.62;
		vec2 tie_pt = (1.0 - st_mid) * (1.0 - st_mid) * root + 2.0 * (1.0 - st_mid) * st_mid * ctrl + st_mid * st_mid * tip;
		vec2 top_anchor = vec2(tie_pt.x + side * 0.05, -0.62);
		float wy = clamp((p.y - top_anchor.y) / max(0.01, tie_pt.y - top_anchor.y), 0.0, 1.0);
		float bow_curve = sin(wy * 3.14159265) * slack * side * 1.4;
		float thread_x = mix(top_anchor.x, tie_pt.x, wy) + bow_curve;
		float d_thread = abs(p.x - thread_x);
		float in_thread_y = step(top_anchor.y, p.y) * step(p.y, tie_pt.y);
		float tension_gleam = 0.45 + 0.55 * smoothstep(0.14, 0.25, t);
		float thread_line = exp(-d_thread * 260.0) * in_thread_y * tension_gleam;
		float thread_halo = exp(-d_thread * 45.0) * in_thread_y * tug_impulse * 0.6;
		col = mix(col, vec3(0.96, 0.92, 1.0), clamp(thread_line, 0.0, 1.0));
		col += vec3(0.88, 0.65, 1.0) * thread_halo;
		threads_alpha = clamp(threads_alpha + thread_line + thread_halo * 0.5, 0.0, 1.0);

		for (int j = 0; j < 3; j++) {
			float fj = float(j);
			float bt = 0.42 + fj * 0.26;
			float obt = 1.0 - bt;
			vec2 node_pos = obt * obt * root + 2.0 * obt * bt * ctrl + bt * bt * tip;
			vec2 pedicel_end = node_pos + vec2(-side * (0.032 + 0.008 * fj), 0.038);

			float d_ped = sdSegment(p, node_pos, pedicel_end);
			float ped_m = smoothstep(0.005, 0.001, d_ped);
			col = mix(col, vec3(0.22, 0.52, 0.34), ped_m);
			bells_alpha = clamp(bells_alpha + ped_m, 0.0, 1.0);

			float sway_b = sin(t * 5.2 + fi * 1.9 + fj * 1.3) * 0.14 * (0.3 + 0.7 * tug_impulse) - side * 0.18;
			float b_size = (0.125 - fj * 0.016) * (0.92 + 0.08 * inner);
			vec4 bell = evalLilyBell(p, pedicel_end, sway_b, b_size);

			float b_mask = smoothstep(0.005, -0.003, bell.x);
			float cyl_nz = sqrt(max(0.05, 1.0 - bell.y * bell.y));
			float rib_flute = cos(bell.y * 3.14159 * 3.0) * 0.10;
			float sss_rim = pow(1.0 - cyl_nz, 1.8);
			float lip_glow = smoothstep(0.65, 1.0, bell.z);

			vec3 b_col = mix(col_bell_shadow, col_bell_pearl, cyl_nz * 0.75 + 0.25 + rib_flute);
			b_col += vec3(0.98, 1.0, 0.96) * sss_rim * 0.48;

			float heart_venom = smoothstep(0.35, 1.0, bell.z) * (1.0 - abs(bell.y) * 0.6) * poison_spread;
			vec3 v_tint = mix(col_venom_mint, col_venom_purple, 0.5 + 0.5 * sin(bell.y * 2.5 + fi));
			b_col = mix(b_col, v_tint, heart_venom * 0.62);
			b_col += col_venom_mint * lip_glow * poison_spread * 0.42;
			b_col = mix(b_col, vec3(0.18, 0.48, 0.28), bell.w);

			col = mix(col, b_col, b_mask);
			bells_alpha = clamp(bells_alpha + b_mask, 0.0, 1.0);

			vec2 mouth_pos = pedicel_end + vec2(-sin(sway_b), cos(sway_b)) * (b_size * 0.82);
			float d_mouth = length(p - mouth_pos);
			float heart_orb = exp(-pow(d_mouth / (b_size * 0.55), 2.0)) * poison_spread;
			col += mix(col_venom_mint, col_venom_purple, mod(fi + fj, 2.0)) * heart_orb * 0.42;
			venom_glow_acc = clamp(venom_glow_acc + heart_orb * 0.55, 0.0, 1.0);

			if (j == 1) {
				vec2 puff_c = mouth_pos + vec2(-side * (t - 0.22) * 0.25, (t - 0.22) * 0.12);
				float sang = fi * 1.5 + t * 0.8 * side;
				mat2 srot = mat2(vec2(cos(sang), sin(sang)), vec2(-sin(sang), cos(sang)));
				vec2 suv = (srot * (p - puff_c)) / vec2(0.46 + 0.15 * inner, 0.34 + 0.12 * inner) + 0.5;
				vec2 fuv = (srot * (p - mouth_pos)) / vec2(0.24, 0.16) + 0.5;
				float s_tex = sampleSmoke(suv) * smoothstep(0.18, 0.38, t);
				float f_tex = sampleFlame(fuv) * smoothstep(0.18, 0.38, t);
				smoke_acc += s_tex * 0.55;
				col += mix(col_venom_purple, col_venom_mint, 0.55) * (s_tex * 0.32 + f_tex * 0.45) * (1.0 - smoothstep(0.78, 0.98, t));
			}
		}
	}

	smoke_acc = clamp(smoke_acc, 0.0, 1.0);
	float edge_mist = exp(-pow((garden_gap - open_radius) / 0.11, 2.0)) * smoothstep(0.22, 0.38, t) * (1.0 - smoothstep(0.80, 0.98, t));
	col += mix(col_venom_purple, col_venom_mint, 0.5) * edge_mist * 0.55;

	float elem_fade = 1.0 - smoothstep(0.80, 0.98, t);
	float foreground_elements = clamp(leaves_alpha + bells_alpha + threads_alpha + (venom_glow_acc * 0.65 + smoke_acc * 0.55), 0.0, 1.0) * elem_fade;

	float alpha = clamp(base_mask + foreground_elements + edge_mist * 0.65, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}