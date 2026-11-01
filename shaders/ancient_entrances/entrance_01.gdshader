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
	mat2 rot = mat2(vec2(0.866, 0.5), vec2(-0.5, 0.866));
	for (int i = 0; i < 5; i++) {
		v += a * noise(p);
		p = rot * p * 2.03 + vec2(4.1, 8.7);
		a *= 0.5;
	}
	return v;
}

vec3 aurora(float x) {
	return 0.5 + 0.42 * cos(6.28318 * (x + vec3(0.0, 0.33, 0.67)));
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float raw_progress = progress;
 float t = clamp((progress - 0.34) / 0.66, 0.0, 1.0);

	float dir = dot(p, normalize(vec2(0.75, 0.65)));
	float ortho = dot(p, normalize(vec2(-0.65, 0.75)));

	float beam_t = smoothstep(0.08, 0.88, t);
	float drift = beam_t * 0.9;
	vec2 cp = p + normalize(vec2(0.75, 0.65)) * sign(dir) * drift;

	vec2 q = vec2(fbm(cp * 2.0 + vec2(t * 0.4, -t * 0.2)), fbm(cp * 2.0 + vec2(-t * 0.3, t * 0.5)));
	float cloud = fbm(cp * 3.2 + q * 1.8);
	float strata = fbm(vec2(cp.x * 1.5, cp.y * 4.5) + q * 1.2);

	float cleave = abs(dir + (cloud - 0.5) * 0.45 + (strata - 0.5) * 0.2);
	float threshold = pow(smoothstep(0.05, 0.92, t), 1.35) * 1.65;
	float cloud_mask = smoothstep(threshold - 0.22, threshold + 0.08, cleave);

	float sword_glow = smoothstep(0.28, 0.0, abs(cleave - threshold)) * smoothstep(0.02, 0.2, t) * (1.0 - smoothstep(0.75, 0.98, t));
	float ray = exp(-abs(dir) * (4.0 + 12.0 * t)) * smoothstep(0.0, 0.35, t) * (1.0 - smoothstep(0.55, 0.95, t));

	vec3 sky_deep = vec3(0.08, 0.16, 0.36);
	vec3 sky_cyan = vec3(0.22, 0.52, 0.82);
	vec3 cloud_warm = vec3(0.95, 0.86, 0.76);
	vec3 peach_gold = vec3(0.96, 0.64, 0.42);

	vec3 col = mix(sky_deep, sky_cyan, smoothstep(-0.5, 0.5, p.y + q.x * 0.3));
	col = mix(col, peach_gold, smoothstep(0.3, 0.75, strata) * 0.6);
	col = mix(col, cloud_warm, smoothstep(0.4, 0.85, cloud));

	vec3 hisou = aurora(ortho * 1.4 + cloud * 0.8 - t * 1.2);
	col = mix(col, mix( hisou, vec3(0.98, 0.92, 0.75), 0.45), (sword_glow + ray * 0.6));

	float motes = 0.0;
	for (int i = 0; i < 5; i++) {
		float fi = float(i);
		vec2 mp = (p - vec2(0.0, t * (0.3 + 0.1 * fi))) * (7.0 + fi * 2.0);
		vec2 id = floor(mp);
		vec2 f_mp = fract(mp) - 0.5;
		float h = hash(id + fi * 31.7);
		float d = length(f_mp - 0.25 * vec2(sin(t * 4.0 + h * 6.0), cos(t * 4.0 + h * 6.0)));
		motes += smoothstep(0.12, 0.02, d) * step(0.8, h);
	}
	motes = clamp(motes, 0.0, 1.0) * (1.0 - smoothstep(0.8, 1.0, t));
	col += vec3(0.95, 0.88, 0.65) * motes * 0.4;

	float alpha = clamp(cloud_mask + sword_glow * 0.85 + ray * 0.5 + motes * 0.4, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	// Keep the original cloud reveal, preceded by a moving slash and a held incision.
 float slash_normal = dot(p, normalize(vec2(0.75, 0.65)));
 float slash_along = dot(p, normalize(vec2(-0.65, 0.75)));
 float slash_head = mix(-1.25, 1.25, smoothstep(0.14, 0.27, raw_progress));
 float slash_tail = smoothstep(slash_head - 0.65, slash_head - 0.12, slash_along);
 float slash_front = 1.0 - smoothstep(slash_head - 0.02, slash_head + 0.04, slash_along);
 float slash_flash = smoothstep(0.13, 0.16, raw_progress) * (1.0 - smoothstep(0.27, 0.30, raw_progress));
 float slash_core = exp(-pow(slash_normal / 0.008, 2.0));
 float slash_bloom = exp(-pow(slash_normal / 0.022, 2.0));
 float slash_sweep = slash_tail * slash_front * slash_flash;
 float incision_hold = smoothstep(0.25, 0.28, raw_progress) * (1.0 - smoothstep(0.34, 0.43, raw_progress));
 vec3 slash_hue = 0.65 + 0.30 * cos(vec3(0.0, 2.1, 4.2) + slash_along * 4.0);
 col = mix(col, col * 0.65, slash_bloom * slash_sweep * 0.45);
 col += (vec3(1.0, 0.93, 0.72) * slash_core * 1.8 + slash_hue * slash_bloom * 0.65) * slash_sweep;
 col += vec3(0.85, 0.43, 0.38) * slash_core * incision_hold * 0.55;
 alpha = max(alpha, clamp((slash_core + slash_bloom * 0.4) * slash_sweep + slash_core * incision_hold, 0.0, 1.0));
 COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}