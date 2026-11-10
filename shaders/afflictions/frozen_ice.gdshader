shader_type canvas_item;
render_mode blend_premul_alpha;

// Frozen affliction overlay: semi-transparent blue ice with hairline cracks.
// Fully procedural (no textures); frost thickens toward the card border.

uniform vec4 ice_color : source_color = vec4(0.45, 0.72, 0.95, 1.0);
uniform vec4 ice_deep_color : source_color = vec4(0.10, 0.30, 0.58, 1.0);
uniform vec4 frost_color : source_color = vec4(0.88, 0.96, 1.0, 1.0);
uniform vec4 crack_color : source_color = vec4(0.72, 0.90, 1.0, 1.0);
uniform float ice_opacity : hint_range(0.0, 1.0) = 0.30;
uniform float edge_frost : hint_range(0.02, 0.6) = 0.15;
uniform float frost_opacity : hint_range(0.0, 1.0) = 0.55;
uniform float crack_scale : hint_range(1.0, 20.0) = 6.0;
uniform float crack_width : hint_range(0.002, 0.08) = 0.022;
uniform float crack_intensity : hint_range(0.0, 2.0) = 0.85;
uniform float sparkle_amount : hint_range(0.0, 1.0) = 0.5;
uniform float shimmer_speed : hint_range(0.0, 2.0) = 0.35;
uniform float frozen_pulse : hint_range(0.0, 0.5) = 0.10;
uniform float ice_noise_scale : hint_range(1.0, 12.0) = 3.0;
uniform float corner_radius : hint_range(0.0, 0.15) = 0.055;

float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));

	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
	float value = 0.0;
	float amplitude = 0.5;
	for (int i = 0; i < 4; i++) {
		value += amplitude * value_noise(p);
		p *= 2.1;
		amplitude *= 0.5;
	}
	return value;
}

float rounded_rect_sdf(vec2 p, vec2 half_size, float radius) {
	vec2 d = abs(p) - half_size + vec2(radius);
	return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0) - radius;
}

// Returns (nearest distance, second nearest, cell id) for crack generation.
vec3 voronoi(vec2 p) {
	vec2 ip = floor(p);
	vec2 fp = fract(p);
	float f1 = 8.0;
	float f2 = 8.0;
	float cell = 0.0;

	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			vec2 g = vec2(float(x), float(y));
			vec2 o = vec2(hash(ip + g), hash(ip + g + vec2(17.0, 31.0)));
			float d = length(g + o - fp);
			if (d < f1) {
				f2 = f1;
				f1 = d;
				cell = hash(ip + g + 7.3);
			} else if (d < f2) {
				f2 = d;
			}
		}
	}
	return vec3(f1, f2, cell);
}

void fragment() {
	vec2 uv = UV;
	float t = TIME * shimmer_speed;

	// Card silhouette mask
	vec2 centered = uv - 0.5;
	float sdf = rounded_rect_sdf(centered, vec2(0.5), corner_radius);
	float card_mask = 1.0 - smoothstep(-0.003, 0.0, sdf);
	float edge_dist = max(-sdf, 0.0);
	float edge_t = 1.0 - smoothstep(0.0, edge_frost, edge_dist);

	// Cloudy interior
	float n = fbm(uv * ice_noise_scale + vec2(t * 0.05, -t * 0.04));
	float pulse = 1.0 + frozen_pulse * sin(t * 0.8 + n * 3.0);

	// Cracks: y is compressed because the card is taller than wide
	vec2 cp = uv * vec2(crack_scale, crack_scale * 1.41);
	cp += vec2(sin(t * 0.3), cos(t * 0.27)) * 0.15;
	vec3 v = voronoi(cp);
	float crack_d = v.y - v.x;
	float crack = 1.0 - smoothstep(0.0, crack_width, crack_d);
	float crack_halo = (1.0 - smoothstep(0.0, crack_width * 4.5, crack_d)) * 0.28;

	// Finer secondary hairline cracks, only on some cells
	vec3 v2 = voronoi(cp * 2.7 + 11.0);
	float d2 = v2.y - v2.x;
	float crack2 = (1.0 - smoothstep(0.0, crack_width * 0.6, d2)) * step(0.45, v2.z);
	crack = clamp(crack + crack2 * 0.55, 0.0, 1.0);
	crack_halo = max(crack_halo, (1.0 - smoothstep(0.0, crack_width * 2.6, d2)) * 0.18 * step(0.45, v2.z));
	// Frost covers cracks near the border
	crack *= 1.0 - edge_t * 0.6;
	crack_halo *= 1.0 - edge_t * 0.7;

	// Border frost plus corner bloom, broken up into crystals
	vec2 cvec = centered - sign(centered) * vec2(0.47);
	float corner_frost = 1.0 - smoothstep(0.0, 0.26, length(cvec));
	float frost = clamp(edge_t * 0.95 + corner_frost * 0.6, 0.0, 1.0);
	frost *= 0.55 + 0.45 * fbm(uv * 18.0 + 3.0);

	// Twinkling ice grains
	float grains = value_noise(uv * 90.0 + floor(t * 2.0) * 7.0);
	float sparkle = step(0.965, grains) * (0.5 + 0.5 * sin(t * 6.0 + n * 10.0)) * sparkle_amount;

	// Diagonal sheen to sell the glass surface
	float sheen = smoothstep(0.86, 1.0, sin((uv.x + uv.y * 1.3) * 5.5 - t * 1.6 + n * 2.0)) * 0.12;

	// Composite: ice body -> frost -> cracks
	float ice_a = clamp(ice_opacity * (0.55 + 0.75 * edge_t + 0.30 * (1.0 - n)) * pulse, 0.0, 1.0);
	vec3 col = mix(ice_color.rgb, ice_deep_color.rgb, clamp(edge_t * 0.7 + n * 0.4, 0.0, 1.0)) * ice_a;
	float a = ice_a;

	float frost_a = frost_opacity * frost;
	col = frost_color.rgb * frost_a + col * (1.0 - frost_a);
	a = frost_a + a * (1.0 - frost_a);

	float crack_a = clamp(crack * crack_intensity * 0.85, 0.0, 1.0);
	col = crack_color.rgb * crack_halo * crack_intensity + col * (1.0 - crack_halo * 0.5);
	col = crack_color.rgb * crack_a + col * (1.0 - crack_a);
	a = crack_a + a * (1.0 - crack_a);

	col += frost_color.rgb * (sparkle + sheen);
	a = clamp(a + sparkle * 0.6, 0.0, 1.0);

	// col is already premultiplied; mask both channels together
	a = clamp(a, 0.0, 1.0) * card_mask;
	COLOR = vec4(col * card_mask, a);
}
