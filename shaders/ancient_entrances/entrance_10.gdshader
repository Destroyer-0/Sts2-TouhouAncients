shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;
uniform sampler2D bat_tex : filter_linear;
uniform sampler2D moon_tex : filter_linear, repeat_enable;
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
		p = rot * p * 2.02 + vec2(3.1, 5.7);
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

vec4 sampleSphereMoon(vec2 p, vec2 center, float radius, float rot_angle) {
	vec2 rel = (p - center) / max(0.001, radius);
	float r2 = dot(rel, rel);
	float inside = smoothstep(1.02, 0.95, r2);
	vec2 clamped_rel = rel / max(1.0, sqrt(r2));
	float z = sqrt(max(0.0001, 1.0 - dot(clamped_rel, clamped_rel)));
	vec3 n = vec3(clamped_rel.x, clamped_rel.y, z);
	float c = cos(rot_angle);
	float s = sin(rot_angle);
	vec3 nr = vec3(c * n.x + s * n.z, n.y, -s * n.x + c * n.z);
	vec2 m_uv = vec2(atan(nr.z, nr.x) / TAU + 0.5, asin(clamp(nr.y, -1.0, 1.0)) / PI + 0.5);
	vec3 tex = texture(moon_tex, m_uv).rgb;
	float limb = pow(z, 0.48);
	return vec4(tex * limb, inside);
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float tilt = -0.36;
	mat2 rot_tilt = mat2(vec2(cos(tilt), sin(tilt)), vec2(-sin(tilt), cos(tilt)));
	vec2 tp = rot_tilt * p;

	vec2 moon_base = vec2(0.32, -0.14);
	vec2 moon_pos = moon_base + vec2(0.26, -0.20) * smoothstep(0.10, 0.68, t);
	float moon_rad = 0.33 * (1.0 + 0.08 * smoothstep(0.0, 0.6, t));
	vec4 moon_data = sampleSphereMoon(p, moon_pos, moon_rad, t * 0.55 - 0.4);

	float dist_m = length(p - moon_pos);
	float gauss_halo1 = exp(-pow(dist_m / (moon_rad * 1.45), 2.0) * 1.6);
	float gauss_halo2 = exp(-pow(dist_m / (moon_rad * 2.8), 2.0) * 1.8);
	float moon_fade = 1.0 - smoothstep(0.55, 0.94, t);

	float open_t = pow(smoothstep(0.08, 0.88, t), 1.25);
	float right_lead = smoothstep(-0.3, 0.5, tp.x) * 0.24 * smoothstep(0.05, 0.55, t);
	float drape_curve = sin(tp.x * 2.4 - t * 2.0) * 0.06 + cos(tp.x * 5.2 + t * 1.5) * 0.025;

	float upper_edge = -open_t * 1.18 - right_lead + drape_curve;
	float lower_edge = open_t * 1.12 + right_lead * 0.6 + drape_curve * 0.8;

	float side_sel = step(0.0, tp.y - drape_curve * 0.5);
	float pull_offset = mix(-open_t * 0.65, open_t * 0.65, side_sel);
	vec2 fabric_p = vec2(tp.x + pull_offset * 0.25, tp.y - pull_offset);

	float fold_n = fbm(fabric_p * vec2(2.2, 3.8) + vec2(0.0, t * 0.3));
	float velvet_pleats = sin(fabric_p.x * 11.0 + fabric_p.y * 5.0 + fold_n * 4.5) * 0.5 + 0.5;
	float sheen = pow(velvet_pleats, 2.6) * (0.4 + 0.6 * fold_n);

	vec2 win_p = tp - vec2(-0.32, 0.16);
	float arch_shape = smoothstep(0.32, 0.05, abs(win_p.x + win_p.y * 0.25));
	float mullion_soft = smoothstep(0.15, 0.85, sin(win_p.x * 16.0 + fold_n * 2.0) * 0.5 + 0.5);
	float rose_soft = exp(-pow(length(win_p - vec2(-0.05, -0.08)) / 0.26, 2.0) * 2.5);
	float window_caustics = (arch_shape * mullion_soft * 0.55 + rose_soft * 0.6) * smoothstep(0.4, -0.3, tp.x) * (1.0 - smoothstep(0.45, 0.82, t));

	float mist_tex = 0.0;
	float wisp_tex = 0.0;
	for (int i = 0; i < 8; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 2.3));
		float h2 = hash(vec2(fi, 7.9));
		float s_side = (i < 4) ? -1.0 : 1.0;
		float border_y = (i < 4) ? upper_edge : lower_edge;
		vec2 m_center = vec2((h1 - 0.5) * 1.6 + (t - 0.3) * 0.4 * s_side, border_y + (h2 - 0.5) * 0.16);
		float mang = h1 * 6.28 + t * 1.2 * s_side;
		mat2 mrot = mat2(vec2(cos(mang), sin(mang)), vec2(-sin(mang), cos(mang)));
		vec2 rel_m = mrot * (tp - m_center);
		vec2 suv = rel_m / vec2(0.65 + 0.25 * h1, 0.42 + 0.18 * h2) + 0.5;
		vec2 fuv = rel_m / vec2(0.55 + 0.2 * h2, 0.28 + 0.12 * h1) + 0.5;
		mist_tex += sampleSmoke(suv) * 0.55;
		wisp_tex += sampleFlame(fuv) * 0.65;
	}
	mist_tex = clamp(mist_tex, 0.0, 1.0);
	wisp_tex = clamp(wisp_tex, 0.0, 1.0);

	float organic_warp = (fold_n - 0.5) * 0.22 + (mist_tex - 0.3) * 0.18;
	float m_upper = smoothstep(upper_edge + 0.14, upper_edge - 0.08, tp.y + organic_warp);
	float m_lower = smoothstep(lower_edge - 0.14, lower_edge + 0.08, tp.y - organic_warp);
	float mask = clamp(m_upper + m_lower, 0.0, 1.0);
	if (t < 0.08) {
		mask = mix(1.0, mask, smoothstep(0.0, 0.08, t));
	}

	float rim_u = exp(-pow((tp.y + organic_warp - upper_edge) / 0.085, 2.0));
	float rim_l = exp(-pow((tp.y - organic_warp - lower_edge) / 0.085, 2.0));
	float drape_rim = clamp(rim_u + rim_l + wisp_tex * 0.7, 0.0, 1.0) * smoothstep(0.02, 0.16, t) * (1.0 - smoothstep(0.80, 0.98, t));

	float strings = 0.0;
	float string_glow = 0.0;
	for (int i = 0; i < 5; i++) {
		float fi = float(i);
		float y_off = (fi - 2.0) * 0.11 + sin(tp.x * 3.2 + fi * 1.7 + t * 5.0) * 0.045;
		float spread = (fi - 2.0) * pow(smoothstep(0.12, 0.85, t), 1.2) * 0.32;
		float dy = abs(tp.y - (y_off + spread));
		float sweep_pulse = smoothstep(-1.2 + t * 2.6, -0.2 + t * 2.6, tp.x * (mod(fi, 2.0) * 2.0 - 1.0));
		strings += exp(-dy * 140.0) * (0.6 + 0.4 * sweep_pulse);
		string_glow += exp(-dy * 24.0) * 0.35;
	}
	float str_fade = smoothstep(0.01, 0.15, t) * (1.0 - smoothstep(0.72, 0.96, t));
	strings = clamp(strings, 0.0, 1.0) * str_fade;
	string_glow = clamp(string_glow, 0.0, 1.0) * str_fade;

	vec3 col_obsidian = vec3(0.05, 0.01, 0.02);
	vec3 col_burgundy = vec3(0.32, 0.03, 0.08);
	vec3 col_velvet_ruby = vec3(0.68, 0.08, 0.16);
	vec3 col_rose_crimson = vec3(0.95, 0.22, 0.32);
	vec3 col_moon_blush = vec3(1.0, 0.88, 0.90);
	vec3 col_warm_amber = vec3(0.88, 0.46, 0.38);

	vec3 col = mix(col_obsidian, col_burgundy, smoothstep(0.1, 0.85, fold_n));
	col += col_velvet_ruby * sheen * 0.55;
	col += mix(col_velvet_ruby, col_warm_amber, 0.45) * window_caustics * 0.65;

	float moon_vis = moon_data.a * (1.0 - smoothstep(0.14, 0.46, t)) * mask;
	vec3 remi_moon = mix(vec3(0.52, 0.09, 0.16), col_moon_blush, smoothstep(0.04, 0.82, moon_data.rgb));
	col = mix(col, remi_moon, moon_vis * 0.94);
	col += mix(col_rose_crimson, col_moon_blush, 0.65) * (gauss_halo1 * 0.42 + gauss_halo2 * 0.22) * moon_fade;

	col = mix(col, mix(col_velvet_ruby, col_rose_crimson, 0.6), drape_rim * 0.85);
	col += col_rose_crimson * string_glow * 0.7;
	col = mix(col, col_moon_blush, strings * 0.92);

	float alpha = clamp(mask + drape_rim * 0.75 + strings * 0.9 + string_glow * 0.45 + gauss_halo1 * 0.25 * moon_fade, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	
 float bats = 0.0;
 for (int bat_i = 0; bat_i < 6; bat_i++) {
  float bi = float(bat_i);
  float seed = fract(sin(bi * 34.17 + 1.2) * 1543.8);
  vec2 center = vec2(0.72 - t * (1.7 + seed * 0.5), -0.22 + seed * 0.55 - t * 0.28 + sin(t * 6.0 + bi) * 0.035);
  vec2 local_bat = p - center;
  vec2 bat_forward=normalize(vec2(-(1.7+seed*0.5),-0.28+cos(t*6.0+bi)*0.21));
  vec2 bat_right=vec2(-bat_forward.y,bat_forward.x);
  local_bat=vec2(dot(local_bat,bat_right),-dot(local_bat,bat_forward));
  local_bat.x /= 0.40 + 0.60 * abs(cos(t * 16.0 + bi * 2.0));
  vec2 bat_uv = local_bat / (0.085 + seed * 0.065) + 0.5;
  vec2 inside_bat = step(vec2(0.0),bat_uv)*step(bat_uv,vec2(1.0));
  bats += texture(bat_tex,clamp(bat_uv,0.0,1.0)).a * inside_bat.x * inside_bat.y;
 }
 bats = clamp(bats,0.0,1.0)*smoothstep(0.08,0.20,t)*(1.0-smoothstep(0.75,0.94,t));
 col = mix(col,vec3(0.055,0.015,0.075),bats);
 alpha = max(alpha,bats);

 COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}