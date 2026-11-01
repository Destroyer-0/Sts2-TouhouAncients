shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

float sdSegment(vec2 p, vec2 a, vec2 b, out float h_out) {
	vec2 pa = p - a, ba = b - a;
	h_out = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
	return length(pa - ba * h_out);
}

vec2 evalBeadedPentagram(vec2 p, float R, float num_beads) {
	float min_seg = 10.0;
	float min_bead = 10.0;
	for (int i = 0; i < 5; i++) {
		float fi = float(i);
		float a1 = fi * 2.513274 - 1.570796;
		float a2 = (fi + 1.0) * 2.513274 - 1.570796;
		vec2 v1 = vec2(cos(a1), sin(a1)) * R;
		vec2 v2 = vec2(cos(a2), sin(a2)) * R;
		float h;
		float d_s = sdSegment(p, v1, v2, h);
		min_seg = min(min_seg, d_s);
		float b_idx = round(h * num_beads) / num_beads;
		vec2 b_pos = mix(v1, v2, b_idx);
		min_bead = min(min_bead, length(p - b_pos));
	}
	return vec2(min_seg, min_bead);
}

float sdMapleLeaf(vec2 p) {
	float r = length(p);
	float a = atan(p.y, p.x) + 1.570796;
	float lobes = 0.048 * (0.55 + 0.45 * abs(cos(a * 2.5))) * step(-2.2, -abs(a));
	return r - lobes;
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float r = length(p);
	float a = atan(p.y, p.x);

	float astro_rings = min(abs(r - 0.28), min(abs(r - 0.46), abs(r - 0.64)));
	float astro_chords = min(abs(p.x * 0.8 + p.y * 0.6 - 0.15), abs(p.x * 0.5 - p.y * 0.86 + 0.12));
	astro_chords = min(astro_chords, abs(sin(a * 6.0 + t * 0.5)) * r);
	float astrolabe = smoothstep(0.006, 0.001, min(astro_rings, astro_chords)) * (1.0 - smoothstep(0.65, 0.95, t));

	float star_scale = 0.25 + pow(smoothstep(0.08, 0.9, t), 1.3) * 1.35;
	mat2 rot1 = mat2(vec2(cos(t * 1.4), sin(t * 1.4)), vec2(-sin(t * 1.4), cos(t * 1.4)));
	mat2 rot2 = mat2(vec2(cos(-t * 1.1 + 0.6), sin(-t * 1.1 + 0.6)), vec2(-sin(-t * 1.1 + 0.6), cos(-t * 1.1 + 0.6)));

	vec2 p_star1 = rot1 * (p - vec2(0.22 * (1.0 + t), 0.08));
	vec2 p_star2 = rot2 * (p - vec2(-0.24 * (1.0 + t), -0.06));
	vec2 star_red = evalBeadedPentagram(p_star1, star_scale * 0.85, 11.0);
	vec2 star_blue = evalBeadedPentagram(p_star2, star_scale * 0.72, 10.0);

	float bead_r = 0.016 * (1.0 + 0.4 * t);
	float red_outline = smoothstep(bead_r + 0.006, bead_r, star_red.y);
	float red_core = smoothstep(bead_r * 0.65, bead_r * 0.2, star_red.y);
	float blue_outline = smoothstep(bead_r + 0.006, bead_r, star_blue.y);
	float blue_core = smoothstep(bead_r * 0.65, bead_r * 0.2, star_blue.y);

	float snake_ribbon = abs(p.y - 0.18 * sin(p.x * 4.5 - t * 6.0) + p.x * 0.35);
	float ribbon_glow = smoothstep(0.035, 0.004, snake_ribbon) * (1.0 - smoothstep(0.6, 0.92, t));

	float star_poly = r * (0.82 + 0.18 * cos(a * 5.0 - t * 2.5));
	float open_r = pow(smoothstep(0.12, 0.88, t), 1.25) * 1.45;
	float mask = smoothstep(open_r - 0.14, open_r + 0.06, star_poly);
	if (t < 0.12) {
		mask = 1.0;
	}

	float rings_and_leaves = 0.0;
	float ring_type = 0.0;
	for (int i = 0; i < 8; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 2.7));
		float h2 = hash(vec2(fi, 7.4));
		vec2 ipos = vec2((h1 - 0.5) * aspect_ratio * 0.9, (h2 - 0.5) * 0.8);
		ipos += normalize(ipos + vec2(0.01)) * t * (0.45 + 0.3 * h1);
		if (mod(fi, 2.0) < 0.5) {
			float dr = abs(length(p - ipos) - 0.038) - 0.006;
			float c = smoothstep(0.005, -0.002, dr);
			rings_and_leaves += c;
			ring_type += c * step(0.5, h2);
		} else {
			float lang = h1 * 6.28 + t * 2.5;
			mat2 lrm = mat2(vec2(cos(lang), sin(lang)), vec2(-sin(lang), cos(lang)));
			rings_and_leaves += smoothstep(0.005, -0.002, sdMapleLeaf(lrm * (p - ipos)));
		}
	}
	float fade_out = 1.0 - smoothstep(0.76, 0.98, t);
	rings_and_leaves = clamp(rings_and_leaves, 0.0, 1.0) * fade_out;
	red_outline *= fade_out;
	blue_outline *= fade_out;

	vec3 col_deep_navy = vec3(0.08, 0.16, 0.38);
	vec3 col_sanae_mint = vec3(0.24, 0.78, 0.58);
	vec3 col_sky_cyan = vec3(0.55, 0.86, 0.98);
	vec3 col_star_red = vec3(0.96, 0.32, 0.42);
	vec3 col_star_blue = vec3(0.28, 0.44, 0.96);

	vec3 col = mix(col_deep_navy, col_sanae_mint, smoothstep(-0.6, 0.6, p.y - p.x * 0.5));
	col = mix(col, col_sky_cyan, astrolabe * 0.75 + ribbon_glow * 0.65);
	col = mix(col, mix(vec3(0.98, 0.96, 1.0), col_star_red, clamp(ring_type, 0.0, 1.0)), rings_and_leaves);
	col = mix(col, col_star_blue, blue_outline);
	col = mix(col, vec3(0.98, 0.99, 1.0), blue_core * fade_out);
	col = mix(col, col_star_red, red_outline);
	col = mix(col, vec3(1.0, 0.98, 0.98), red_core * fade_out);

	float edge_glow = smoothstep(0.14, 0.0, abs(star_poly - open_r)) * smoothstep(0.05, 0.2, t) * fade_out;
	col += vec3(0.85, 0.98, 0.92) * edge_glow * 0.7;

	float alpha = clamp(mask + edge_glow * 0.8 + red_outline + blue_outline + rings_and_leaves * 0.9 + ribbon_glow * 0.5, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}