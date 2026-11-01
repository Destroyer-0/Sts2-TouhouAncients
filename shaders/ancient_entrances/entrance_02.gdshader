shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;
uniform sampler2D moon_tex : filter_linear, repeat_enable;
uniform sampler2D light_tex : filter_linear;

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
		p = rot * p * 2.02 + vec2(3.7, 5.1);
		a *= 0.5;
	}
	return v;
}

float sampleLight(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	return texture(light_tex, clamp(uv, 0.0, 1.0)).r * b.x * b.y;
}

float sdBambooStalk(vec2 p, float x_pos, float tilt) {
	vec2 q = p - vec2(x_pos, 0.0);
	q.x -= q.y * tilt;
	float seg_y = fract(q.y * 3.8) - 0.5;
	float stalk_w = 0.018 + 0.005 * exp(-abs(seg_y) * 28.0);
	float d_stalk = abs(q.x) - stalk_w;
	float node_gap = abs(seg_y) - 0.01;
	d_stalk = max(d_stalk, -smoothstep(0.008, 0.0, node_gap));
	vec2 lp = vec2(abs(q.x) - 0.016, seg_y);
	mat2 lrot = mat2(vec2(0.76, -0.65), vec2(0.65, 0.76));
	vec2 rlp = lrot * lp;
	float d_leaf = abs(rlp.y) - 0.012 * (1.0 - clamp(abs(rlp.x) / 0.13, 0.0, 1.0));
	d_leaf = max(d_leaf, abs(rlp.x - 0.065) - 0.065);
	return min(d_stalk, d_leaf);
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
	float limb = pow(z, 0.42);
	return vec4(tex * limb, inside);
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	vec2 moon_c = vec2(-0.04, -0.18 - smoothstep(0.15, 0.85, t) * 0.22);
	float moon_rad = 0.31 + smoothstep(0.1, 0.7, t) * 0.05;
	vec4 moon_data = sampleSphereMoon(p, moon_c, moon_rad, t * 0.5);

	float halo_main = sampleLight((p - moon_c) / (moon_rad * 3.6) + 0.5);
	float halo_wide = sampleLight((p - moon_c) / (moon_rad * 5.8) + 0.5);

	float silk_n = fbm(p * vec2(2.2, 1.5) + vec2(0.0, -t * 0.35));
	float veil_wave = sin(p.x * 5.0 + silk_n * 3.5) * 0.04;

	float moon_r = length(p - vec2(-0.04, -0.16) + vec2(veil_wave, 0.0));
	float moon_open = 0.22 + pow(smoothstep(0.12, 0.88, t), 1.3) * 1.45;
	float d_moon_ring = abs(moon_r - moon_open);

	float shift = pow(smoothstep(0.08, 0.88, t), 1.25) * 0.98;
	float b1 = sdBambooStalk(p, -0.48 - shift, -0.12);
	float b2 = sdBambooStalk(p, -0.70 - shift * 1.12, -0.06);
	float b3 = sdBambooStalk(p, 0.50 + shift, 0.11);
	float b4 = sdBambooStalk(p, 0.74 + shift * 1.12, 0.05);
	float bamboo_silhouette = smoothstep(0.006, -0.003, min(min(b1, b2), min(b3, b4))) * (1.0 - smoothstep(0.74, 0.96, t));

	float curtain_part = abs(p.x + veil_wave * 0.8) + (silk_n - 0.5) * 0.18 - shift * 1.15;
	float mask = smoothstep(moon_open - 0.12, moon_open + 0.08, moon_r) * smoothstep(-0.18, 0.14, curtain_part);
	if (t < 0.12) {
		mask = 1.0;
	}

	vec3 fireflies = vec3(0.0);
	float ff_alpha = 0.0;
	for (int i = 0; i < 8; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 2.3));
		float h2 = hash(vec2(fi, 7.1));
		vec2 fpos = vec2((h1 - 0.5) * aspect_ratio * 1.1, 0.38 - h2 * 0.55 - t * (0.35 + 0.25 * h1));
		fpos.x += sin(t * 2.4 + fi * 1.7) * 0.12 + sign(fpos.x) * t * 0.28;
		float fg = sampleLight((p - fpos) / (0.025 + 0.012 * h2) + 0.5);
		vec3 fcol = mix(vec3(0.55, 0.88, 1.0), vec3(0.98, 0.76, 0.92), mod(fi, 2.0));
		fireflies += fcol * fg * 0.55;
		ff_alpha += fg * 0.4;
	}
	float c_fade = 1.0 - smoothstep(0.76, 0.98, t);
	fireflies *= c_fade;
	ff_alpha = clamp(ff_alpha, 0.0, 1.0) * c_fade;

	vec3 col_night_indigo = vec3(0.05, 0.03, 0.15);
	vec3 col_plum_mist = vec3(0.32, 0.11, 0.30);
	vec3 col_moon_cream = vec3(1.0, 0.96, 0.84);
	vec3 col_moon_silver = vec3(0.82, 0.88, 0.98);

	vec3 col = mix(col_night_indigo, col_plum_mist, smoothstep(0.15, 0.85, silk_n));
	float moon_vis = moon_data.a * (1.0 - smoothstep(0.16, 0.50, t)) * mask;
	vec3 kaguya_moon = mix(vec3(0.52, 0.44, 0.66), col_moon_cream, smoothstep(0.05, 0.85, moon_data.rgb));
	col = mix(col, kaguya_moon, moon_vis * 0.95);
	col += col_moon_cream * (halo_main * 0.45 + halo_wide * 0.25) * (1.0 - smoothstep(0.55, 0.92, t));

	float moon_rim = smoothstep(0.12, 0.0, d_moon_ring) * c_fade;
	col = mix(col, mix(col_moon_silver, col_moon_cream, 0.6), moon_rim * 0.75);
	col = mix(col, vec3(0.03, 0.06, 0.11), bamboo_silhouette);
	col += fireflies;

	float alpha = clamp(mask + moon_rim * 0.65 + bamboo_silhouette + ff_alpha, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}