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
	mat2 rot = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));
	for (int i = 0; i < 5; i++) {
		v += a * noise(p);
		p = rot * p * 2.05 + vec2(1.9, 8.3);
		a *= 0.5;
	}
	return v;
}

vec3 oil_slick(float x) {
	return 0.35 + 0.28 * cos(6.28318 * (x + vec3(0.0, 0.25, 0.55)));
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float r = length(p);
	float a = atan(p.y, p.x);
	float pull = a + (1.4 / (r + 0.18)) * pow(t, 1.1) * 1.8;
	vec2 vp = vec2(cos(pull), sin(pull)) * (r + t * 0.65);

	float oil = fbm(vp * 3.0 + fbm(vp * 2.0 - t * 0.8));
	float spiral_arm = sin(pull * 4.0 + oil * 5.0) * 0.08;

	float outer_limit = (1.0 - pow(smoothstep(0.04, 0.94, t), 1.15)) * 1.45;
	float vortex_dist = r + spiral_arm + (oil - 0.5) * 0.32;
	float mask = smoothstep(outer_limit + 0.14, outer_limit - 0.06, vortex_dist);

	float drain_rim = smoothstep(0.2, 0.0, abs(vortex_dist - outer_limit)) * smoothstep(0.02, 0.18, t) * (1.0 - smoothstep(0.82, 0.99, t));

	vec3 pitch_oil = vec3(0.04, 0.03, 0.05);
	vec3 cursed_blood = vec3(0.52, 0.04, 0.06);
	vec3 ember_orange = vec3(0.92, 0.38, 0.12);

	vec3 slick = oil_slick(oil * 2.5 + r * 2.0 - t);
	vec3 col = mix(pitch_oil, cursed_blood, smoothstep(0.3, 0.82, oil));
	col = mix(col, slick, smoothstep(0.4, 0.7, sin(oil * 10.0)) * 0.45);
	col = mix(col, ember_orange, drain_rim * 0.85);

	float alpha = clamp(mask + drain_rim * 0.75, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.94, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}