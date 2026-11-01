shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;

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
	for (int i = 0; i < 5; i++) {
		v += a * noise(p);
		p = rot * p * 2.02 + vec2(4.3, 2.9);
		a *= 0.5;
	}
	return v;
}

vec3 mirage_spectrum(float x) {
	return 0.62 + 0.33 * cos(6.28318 * (x + vec3(0.0, 0.22, 0.48)));
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float wave_y = p.y + 0.07 * sin(p.x * 2.8 + t * 2.5) + 0.04 * cos(p.x * 4.5 - t * 1.8);
	float band_coord = wave_y * 4.5;
	float band_id = floor(band_coord);
	float band_f = fract(band_coord) - 0.5;
	float h_band = hash(vec2(band_id, 7.3));

	float dir = (mod(band_id, 2.0) < 0.5) ? 1.0 : -1.0;
	float delay = h_band * 0.26;
	float slide_t = smoothstep(delay, 0.74 + delay * 0.8, t);

	float refract_ripples = sin(p.y * 28.0 + p.x * 6.0 - t * 8.0) * 0.025 * sin(t * 3.14159);
	vec2 mp = vec2(p.x + dir * pow(slide_t, 1.3) * 1.65 + refract_ripples, p.y);

	vec2 q = vec2(fbm(mp * vec2(1.8, 3.0) + vec2(dir * t * 0.6, 0.0)), fbm(mp * vec2(1.8, 3.0) + vec2(3.4, t * 0.4)));
	float mist = fbm(mp * vec2(2.4, 3.5) + q * 1.8);

	float horiz_clear = abs(mp.x - dir * 0.2) * 0.55 + abs(band_f) * 0.45 + (mist - 0.5) * 0.35;
	float threshold = (1.0 - pow(slide_t, 1.2)) * 1.35;
	float mask = smoothstep(threshold + 0.14, threshold - 0.08, horiz_clear);

	float prism_rim = smoothstep(0.22, 0.0, abs(horiz_clear - threshold)) * smoothstep(0.02, 0.18, t) * (1.0 - smoothstep(0.78, 0.98, t));
	float shimmer_line = smoothstep(0.06, 0.0, abs(band_f + (q.x - 0.5) * 0.25)) * (1.0 - slide_t * 0.8);

	vec3 abyss_blue = vec3(0.05, 0.09, 0.22);
	vec3 rumor_violet = vec3(0.22, 0.20, 0.54);
	vec3 truth_cyan = vec3(0.38, 0.78, 0.92);

	vec3 irid = mirage_spectrum(q.x * 1.8 + p.y * 1.2 - t * 1.4 + h_band);
	vec3 col = mix(abyss_blue, rumor_violet, smoothstep(0.18, 0.8, mist));
	col = mix(col, irid, smoothstep(0.35, 0.85, q.y) * 0.55);
	col = mix(col, truth_cyan, shimmer_line * 0.5);
	col = mix(col, mix(irid, vec3(0.95, 0.88, 0.96), 0.45), prism_rim * 0.9);

	float alpha = clamp(mask + prism_rim * 0.8 + shimmer_line * mask * 0.3, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}