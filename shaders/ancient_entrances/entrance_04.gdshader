shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;
uniform sampler2D flame_tex : filter_linear;
uniform sampler2D smoke_tex : filter_linear;
uniform sampler2D light_tex : filter_linear;
uniform sampler2D moon_tex : filter_linear, repeat_enable;

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

float sampleFlame(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	vec4 tex = texture(flame_tex, clamp(uv, 0.0, 1.0));
	return max(tex.r, tex.a) * b.x * b.y;
}

float sampleSmoke(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	vec4 tex = texture(smoke_tex, clamp(uv, 0.0, 1.0));
	return max(tex.r, tex.a) * b.x * b.y;
}

float sampleLight(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	vec4 tex = texture(light_tex, clamp(uv, 0.0, 1.0));
	return max(tex.r, tex.a) * b.x * b.y;
}

vec4 sampleSphereMoon(vec2 p, vec2 center, float radius, float rot_angle) {
	vec2 rel = (p - center) / max(0.001, radius);
	float r2 = dot(rel, rel);
	float inside = smoothstep(1.02, 0.96, r2);
	vec2 clamped_rel = rel / max(1.0, sqrt(r2));
	float z = sqrt(max(0.0001, 1.0 - dot(clamped_rel, clamped_rel)));
	vec3 n = vec3(clamped_rel.x, clamped_rel.y, z);
	float c = cos(rot_angle);
	float s = sin(rot_angle);
	vec3 nr = vec3(c * n.x + s * n.z, n.y, -s * n.x + c * n.z);
	vec2 m_uv = vec2(atan(nr.z, nr.x) / TAU + 0.5, asin(clamp(nr.y, -1.0, 1.0)) / PI + 0.5);
	vec3 tex = texture(moon_tex, m_uv).rgb;
	float limb = pow(z, 0.4);
	return vec4(tex * limb, inside);
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	vec2 star_pos = vec2(0.36, -0.18) + vec2(0.16, -0.14) * smoothstep(0.15, 0.75, t);
	float star_rad = 0.27;
	vec4 moon_data = sampleSphereMoon(p, star_pos, star_rad, -t * 0.45);
	float star_halo = sampleLight((p - star_pos) / (star_rad * 3.5) + 0.5) * (1.0 - smoothstep(0.6, 0.95, t));

	vec2 flow_dir = normalize(vec2(0.82, -0.57));
	vec2 ortho_dir = vec2(0.57, 0.82);
	float along = dot(p, flow_dir);
	float across = dot(p, ortho_dir);

	float wave_warp = sin(along * 4.2 - t * 5.0) * 0.12 + (noise(p * 3.2 - flow_dir * t * 1.8) - 0.5) * 0.24;

	float smoke_acc = 0.0;
	float flame_primary = 0.0;
	float flame_gold = 0.0;

	for (int i = 0; i < 10; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 1.4));
		float h2 = hash(vec2(fi, 6.8));

		float sweep_pos = -0.95 + pow(smoothstep(0.0, 0.92, t), 1.15) * 2.1 + (h1 - 0.5) * 0.55;
		float lane_offset = (h2 - 0.5) * 0.95 + sin(along * 3.0 + fi) * 0.08;
		vec2 center = flow_dir * sweep_pos + ortho_dir * lane_offset;

		float ang = -0.6 + sin(t * 3.0 + fi) * 0.22;
		float ca = cos(ang);
		float sa = sin(ang);
		vec2 rel = p - center;
		vec2 l_uv = vec2(ca * rel.x + sa * rel.y, -sa * rel.x + ca * rel.y);
		l_uv.y += sin(l_uv.x * 5.0 - t * 6.0) * 0.04;

		vec2 f_uv = l_uv / vec2(0.58 + 0.25 * h1, 0.32 + 0.15 * h2) + 0.5;
		vec2 s_uv = l_uv / vec2(0.75 + 0.3 * h2, 0.52 + 0.2 * h1) + 0.5;

		float f_val = sampleFlame(f_uv);
		float s_val = sampleSmoke(s_uv);

		smoke_acc += s_val * 0.45;
		if (mod(fi, 3.0) < 0.5) {
			flame_gold += f_val * 0.75;
		} else {
			flame_primary += f_val * 0.8;
		}
	}

	smoke_acc = clamp(smoke_acc, 0.0, 1.0);
	flame_primary = clamp(flame_primary, 0.0, 1.0);
	flame_gold = clamp(flame_gold, 0.0, 1.0);

	float right_bias = smoothstep(0.1, 0.55, p.x - p.y * 0.4) * smoothstep(0.12, 0.45, t) * 0.48;
	float purify_front = (along - right_bias) + wave_warp - (-0.85 + pow(smoothstep(0.05, 0.90, t), 1.2) * 2.15);
	float mask = smoothstep(-0.16, 0.12, purify_front + (smoke_acc - 0.4) * 0.22);
	if (t < 0.06) {
		mask = 1.0;
	}

	float front_band = smoothstep(0.28, 0.0, abs(purify_front)) * smoothstep(0.02, 0.16, t) * (1.0 - smoothstep(0.78, 0.98, t));
	float fade_fx = 1.0 - smoothstep(0.78, 0.98, t);
	flame_primary *= fade_fx;
	flame_gold *= fade_fx;

	vec3 col_abyss = vec3(0.03, 0.02, 0.06);
	vec3 col_dark_plum = vec3(0.18, 0.09, 0.28);
	vec3 col_cold_violet = vec3(0.68, 0.56, 0.96);
	vec3 col_pure_white = vec3(0.96, 0.95, 1.0);
	vec3 col_crown_gold = vec3(0.96, 0.78, 0.38);

	float bg_n = noise(p * 3.0 - flow_dir * t * 1.2);
	vec3 col = mix(col_abyss, col_dark_plum, smoothstep(0.15, 0.85, bg_n * 0.6 + smoke_acc * 0.5));

	float moon_vis = moon_data.a * (1.0 - smoothstep(0.12, 0.44, t)) * mask;
	vec3 junko_moon = mix(vec3(0.55, 0.50, 0.78), col_pure_white, smoothstep(0.04, 0.8, moon_data.rgb));
	col = mix(col, junko_moon, moon_vis * 0.92);
	col += mix(col_cold_violet, col_pure_white, 0.6) * star_halo * 0.5;

	col = mix(col, col_cold_violet, clamp(flame_primary + front_band * 0.7, 0.0, 1.0) * 0.85);
	col = mix(col, col_crown_gold, flame_gold * 0.75);
	col = mix(col, col_pure_white, pow(clamp(flame_primary * 0.8 + front_band * 0.6, 0.0, 1.0), 2.2) * 0.85);

	float alpha = clamp(mask + front_band * 0.8 + flame_primary * 0.85 + flame_gold * 0.75, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}