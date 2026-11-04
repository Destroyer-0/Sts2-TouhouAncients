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

float sdLeapingBunny(vec2 p) {
	float body = length((p - vec2(0.0, 0.0)) * vec2(0.75, 1.25)) - 0.032;
	float head = length(p - vec2(0.034, -0.018)) - 0.022;
	float tail = length(p - vec2(-0.044, 0.012)) - 0.012;
	vec2 ep1 = p - vec2(0.028, -0.042);
	mat2 er1 = mat2(vec2(0.86, 0.5), vec2(-0.5, 0.86));
	float ear1 = length((er1 * ep1) * vec2(2.4, 0.7)) - 0.018;
	vec2 ep2 = p - vec2(0.016, -0.040);
	mat2 er2 = mat2(vec2(0.64, 0.76), vec2(-0.76, 0.64));
	float ear2 = length((er2 * ep2) * vec2(2.4, 0.7)) - 0.017;
	return min(min(min(body, head), tail), min(ear1, ear2));
}

float sdFourLeafClover(vec2 p, float s) {
	p /= s;
	float a = atan(p.y, p.x);
	float r = length(p);
	float lobe = abs(cos(a * 2.0));
	float notch = smoothstep(0.85, 1.0, lobe) * 0.12;
	return (r - (0.35 + 0.45 * pow(lobe, 0.6) - notch)) * s;
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
	float limb = pow(z, 0.5);
	return vec4(tex * limb, inside);
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	vec2 moon_base = vec2(-0.04, -0.06);
	vec2 moon_c = moon_base + vec2(-0.12, -0.18) * smoothstep(0.12, 0.75, t);
	float moon_rad = 0.34 * (1.0 + 0.15 * smoothstep(0.1, 0.6, t));
	vec4 moon_data = sampleSphereMoon(p, moon_c, moon_rad, -t * 0.7 + 0.8);

	vec2 halo_uv = clamp((p - moon_c) / (moon_rad * 3.0) + 0.5, 0.0, 1.0);
	float moon_halo = texture(light_tex, halo_uv).r;

	float r = length(p - moon_base);
	float a = atan(p.y - moon_base.y, p.x - moon_base.x);

	float r1 = abs(r - (0.28 + t * 0.65) + 0.08 * sin(a * 3.0 - t * 5.0));
	float r2 = abs(p.y + 0.15 * sin(p.x * 3.5 + t * 4.0) - (t - 0.3) * 0.8 * sign(p.x));
	float r3 = abs(p.x - 0.22 * cos(p.y * 3.2 - t * 4.5) + (t - 0.2) * 0.9 * sign(p.y));
	float crimson_ribbons = smoothstep(0.028, 0.004, min(r1, min(r2, r3))) * (1.0 - smoothstep(0.75, 0.98, t));

	mat2 rot_c = mat2(vec2(cos(t * 1.2), sin(t * 1.2)), vec2(-sin(t * 1.2), cos(t * 1.2)));
	float open_scale = 0.22 + pow(smoothstep(0.1, 0.9, t), 1.3) * 1.55;
	float d_clover_hole = sdFourLeafClover(rot_c * (p - moon_base), open_scale);
	float mask = smoothstep(-0.06, 0.08, d_clover_hole);
	if (t < 0.12) {
		mask = 1.0;
	}
	float clover_rim = smoothstep(0.05, 0.0, abs(d_clover_hole)) * smoothstep(0.05, 0.2, t) * (1.0 - smoothstep(0.78, 0.98, t));

	float bunnies = 0.0;
	float stars = 0.0;
	for (int i = 0; i < 6; i++) {
		float fi = float(i);
		float ang = fi * 1.0472 + t * 2.2;
		float brad = 0.24 + t * 0.68 + abs(sin(t * 9.0 + fi)) * 0.06;
		vec2 bpos = moon_base + vec2(cos(ang), sin(ang)) * brad;
		float tang = ang + 1.5708;
		mat2 brm = mat2(vec2(cos(tang), sin(tang)), vec2(-sin(tang), cos(tang)));
		bunnies += smoothstep(0.005, -0.002, sdLeapingBunny(brm * (p - bpos)));

		float h1 = hash(vec2(fi, 5.1));
		float h2 = hash(vec2(fi, 9.8));
		vec2 spos = vec2((h1 - 0.5) * aspect_ratio * 1.15, (h2 - 0.5) * 0.9);
		vec2 sp = p - spos;
		vec2 s_uv = clamp(sp / 0.16 + 0.5, 0.0, 1.0);
		float s_glow = texture(light_tex, s_uv).r * 0.5;
		float star_glint = exp(-abs(sp.x) * 90.0) * exp(-abs(sp.y) * 18.0) + exp(-abs(sp.y) * 90.0) * exp(-abs(sp.x) * 18.0) + s_glow;
		stars += star_glint * (0.5 + 0.5 * sin(t * 10.0 + fi * 2.0));
	}
	float elem_fade = 1.0 - smoothstep(0.76, 0.98, t);
	bunnies = clamp(bunnies, 0.0, 1.0) * elem_fade;
	stars = clamp(stars, 0.0, 1.0) * elem_fade;

	vec3 col_night_sky = vec3(0.05, 0.04, 0.09);
	vec3 col_dark_cloud = vec3(0.18, 0.11, 0.20);
	vec3 col_blood_dark = vec3(0.28, 0.03, 0.05);
	vec3 col_blood_bright = vec3(0.95, 0.24, 0.18);
	vec3 col_ribbon_red = vec3(0.78, 0.10, 0.14);
	vec3 col_bunny_pink = vec3(0.98, 0.88, 0.92);
	vec3 col_lucky_gold = vec3(0.98, 0.84, 0.46);

	float n_cloud = noise(p * 3.5 + vec2(t * 0.4, -t * 0.2));
	vec3 col = mix(col_night_sky, col_dark_cloud, smoothstep(0.25, 0.8, n_cloud));

	float moon_vis = moon_data.a * (1.0 - smoothstep(0.14, 0.48, t)) * mask;
	vec3 tewi_moon = mix(col_blood_dark, col_blood_bright, smoothstep(0.05, 0.85, moon_data.rgb));
	col = mix(col, tewi_moon, moon_vis * 0.95);
	col += col_blood_bright * moon_halo * 0.42 * (1.0 - smoothstep(0.45, 0.88, t));

	col = mix(col, col_ribbon_red, crimson_ribbons);
	col = mix(col, col_lucky_gold, clover_rim * 0.85);
	col = mix(col, col_bunny_pink, bunnies);
	col += vec3(0.98, 0.96, 1.0) * stars * 0.75;

	float alpha = clamp(mask + crimson_ribbons * 0.9 + clover_rim * 0.85 + bunnies + stars * 0.65, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}