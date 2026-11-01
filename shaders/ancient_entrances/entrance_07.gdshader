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
	for (int i = 0; i < 4; i++) {
		v += a * noise(p);
		p = rot * p * 2.04 + vec2(3.1, 5.7);
		a *= 0.5;
	}
	return v;
}

float irisFibers(float a, float r) {
	float v = 0.0;
	float amp = 0.5;
	float freq = 18.0;
	for (int i = 0; i < 4; i++) {
		float fi = float(i);
		float wave = sin(a * freq + sin(r * (10.0 + fi * 4.0) + a * 4.0) * 1.6 + fi * 2.1);
		v += amp * (wave * 0.5 + 0.5);
		freq *= 1.85;
		amp *= 0.52;
	}
	return v;
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float tilt = -0.04 * (1.0 - smoothstep(0.55, 0.92, t));
	mat2 rot_eye = mat2(vec2(cos(tilt), sin(tilt)), vec2(-sin(tilt), cos(tilt)));
	vec2 ep = rot_eye * p;

	float zoom = 1.0 + pow(smoothstep(0.46, 0.95, t), 2.35) * 5.6;
	float half_w = 0.74 * zoom;
	float nx = ep.x / half_w;
	float ax = clamp(abs(nx), 0.0, 1.5);
	float corner_env = pow(max(0.0, 1.0 - ax * ax), 1.38);

	float closed_line = 0.024 * (1.0 - ax * ax) - 0.015 * sin(clamp(nx, -1.0, 1.0) * 3.14159);
	float stir = smoothstep(0.03, 0.18, t) * 0.008;
	float open_main = pow(smoothstep(0.14, 0.72, t), 1.32);
	float expand_vert = pow(smoothstep(0.52, 0.95, t), 2.15) * 3.2;

	float upper_open = (stir + open_main * 0.36) * zoom + expand_vert;
	float lower_open = (stir * 0.6 + open_main * 0.27) * zoom + expand_vert * 0.95;

	float upper_arch = corner_env * (1.0 + 0.14 * sin((nx + 0.12) * 2.2));
	float lower_arch = corner_env * (1.0 - 0.10 * sin((nx - 0.15) * 2.2));

	float y_top = closed_line * (1.0 - open_main) - upper_open * upper_arch;
	float y_bot = closed_line * (1.0 - open_main) + lower_open * lower_arch;

	float d_top = ep.y - y_top;
	float d_bot = y_bot - ep.y;
	float d_aperture = min(d_top, d_bot);
	if (ax >= 1.0) {
		d_aperture = -length(vec2((ax - 1.0) * half_w, ep.y - closed_line * (1.0 - open_main)));
	}

	float edge_soft = 0.010 * (1.0 + 0.35 * zoom);
	float in_eye = smoothstep(-edge_soft, edge_soft, d_aperture);

	float crease_gap = (0.055 + 0.038 * open_main) * zoom * pow(max(0.0, 1.0 - ax * ax * 0.82), 0.85);
	float y_crease = y_top - crease_gap;
	float d_crease = abs(ep.y - y_crease);
	float crease_shadow = exp(-pow(d_crease / (0.018 * zoom), 2.0)) * smoothstep(1.05, 0.25, ax) * smoothstep(0.05, 0.35, t);

	float lash_freq = 44.0;
	float lash_wave = sin(nx * lash_freq + sin(nx * 9.0) * 1.5) * 0.5 + 0.5;
	float lash_fringe = exp(-abs(d_top) / (0.012 * zoom)) * pow(lash_wave, 2.2) * smoothstep(0.98, 0.15, ax) * smoothstep(0.06, 0.28, t);

	float tear_top = exp(-pow(d_top / (0.0085 * zoom), 2.0)) * smoothstep(0.98, 0.1, ax);
	float tear_bot = exp(-pow(d_bot / (0.0095 * zoom), 2.0)) * smoothstep(0.98, 0.1, ax);
	float tear_slit = exp(-pow((ep.y - closed_line) / 0.011, 2.0)) * smoothstep(0.92, 0.1, ax) * (1.0 - smoothstep(0.18, 0.35, t));
	float tear_meniscus = clamp((tear_top * 0.7 + tear_bot * 1.1) * smoothstep(0.08, 0.25, t) + tear_slit * 0.85, 0.0, 1.0);

	float iris_rad = 0.33 * (1.0 + pow(smoothstep(0.24, 0.86, t), 1.65) * 3.8);
	vec2 ic = ep / max(0.001, iris_rad);
	float ir = length(ic);
	float ia = atan(ic.y, ic.x);

	float pupil_dilate = 0.24 + 0.04 * sin(t * 9.0) * (1.0 - smoothstep(0.3, 0.6, t)) + pow(smoothstep(0.28, 0.84, t), 1.48) * 1.55;

	float f_radial = irisFibers(ia, ir);
	float f_crypts = irisFibers(ia * 0.65 + 1.4, ir * 1.4);
	float collarette_pos = pupil_dilate + (1.0 - pupil_dilate) * 0.34 + 0.025 * sin(ia * 14.0 + f_radial * 3.0);
	float collarette_ring = exp(-pow((ir - collarette_pos) / 0.085, 2.0));
	float limbal_ring = smoothstep(0.68, 0.98, ir) * smoothstep(1.12, 0.92, ir);
	float contraction_furrows = sin(ir * 28.0 + sin(ia * 8.0) * 1.2) * 0.5 + 0.5;

	vec3 col_iris_deep = vec3(0.20, 0.04, 0.18);
	vec3 col_iris_rose = vec3(0.84, 0.16, 0.42);
	vec3 col_iris_magenta = vec3(0.96, 0.38, 0.64);
	vec3 col_iris_cyan = vec3(0.38, 0.84, 0.94);
	vec3 col_limbus = vec3(0.06, 0.01, 0.08);

	vec3 iris_col = mix(col_iris_deep, col_iris_rose, smoothstep(0.15, 0.85, f_radial));
	iris_col = mix(iris_col, col_iris_magenta, collarette_ring * 0.78 + smoothstep(0.55, 0.9, f_crypts) * 0.35);
	float cyan_irid = smoothstep(0.45, 0.95, sin(ia * 2.0 - ir * 4.0 + t * 3.5)) * smoothstep(0.3, 0.85, f_radial);
	iris_col = mix(iris_col, col_iris_cyan, cyan_irid * 0.38 + contraction_furrows * 0.12);
	iris_col = mix(iris_col, col_limbus, limbal_ring * 0.88);

	float upper_lid_cast = exp(-max(0.0, d_top) / (0.065 * zoom)) * 0.58;
	iris_col *= (1.0 - upper_lid_cast);

	vec3 col_sclera = vec3(0.24, 0.15, 0.30);
	col_sclera = mix(col_sclera, vec3(0.10, 0.04, 0.16), smoothstep(0.95, 1.45, ir));
	col_sclera *= (1.0 - upper_lid_cast * 0.7);

	float in_iris = smoothstep(1.03, 0.94, ir);
	vec3 eyeball_surface = mix(col_sclera, iris_col, in_iris);

	vec2 spec1_p = ic - vec2(-0.26, -0.22 + 0.06 * open_main);
	mat2 srot = mat2(vec2(0.86, -0.5), vec2(0.5, 0.86));
	vec2 s1 = srot * spec1_p;
	float spec_main = exp(-pow(s1.x / 0.15, 2.0) - pow(s1.y / 0.065, 2.0)) * smoothstep(0.05, 0.22, t) * (1.0 - smoothstep(0.55, 0.82, t));
	float spec_sec = exp(-pow(length(ic - vec2(0.28, 0.24)) / 0.075, 2.0)) * smoothstep(0.08, 0.25, t) * (1.0 - smoothstep(0.55, 0.82, t));
	float cornea_gloss = clamp(spec_main * 0.85 + spec_sec * 0.45, 0.0, 1.0);

	float mind_wave = sin(ir * 22.0 - t * 14.0) * 0.5 + 0.5;
	mind_wave *= exp(-abs(ir - pupil_dilate) * 4.5) * smoothstep(0.15, 0.35, t) * (1.0 - smoothstep(0.75, 0.96, t));
	eyeball_surface += mix(col_iris_magenta, col_iris_cyan, 0.45) * mind_wave * 0.45;
	eyeball_surface = mix(eyeball_surface, vec3(0.98, 0.94, 1.0), cornea_gloss);

	float pupil_reveal = smoothstep(pupil_dilate - 0.10, pupil_dilate + 0.06, ir);
	float early_veil = 1.0 - smoothstep(0.16, 0.34, t);
	float iris_fade = 1.0 - smoothstep(0.66, 0.92, t);
	float inner_eye_alpha = clamp(mix(pupil_reveal * iris_fade, 1.0, early_veil) + cornea_gloss * 0.65, 0.0, 1.0);

	float skin_tex = fbm(ep * 3.2);
	vec3 col_lid_base = vec3(0.07, 0.02, 0.11);
	vec3 col_lid_warm = vec3(0.22, 0.06, 0.18);
	vec3 col_lid_rim = vec3(0.72, 0.18, 0.38);

	float lid_grad = exp(-abs(d_aperture) / (0.16 * zoom));
	vec3 skin_col = mix(col_lid_base, col_lid_warm, lid_grad * 0.75 + skin_tex * 0.25);
	skin_col = mix(skin_col, col_lid_rim, exp(-abs(d_aperture) / (0.028 * zoom)) * 0.55);
	skin_col = mix(skin_col, vec3(0.03, 0.005, 0.05), clamp(crease_shadow * 0.85 + lash_fringe * 0.9, 0.0, 1.0));

	vec3 col = mix(skin_col, eyeball_surface, in_eye);
	col = mix(col, vec3(0.02, 0.005, 0.04), lash_fringe * in_eye * 0.75);
	col += mix(vec3(0.98, 0.52, 0.72), vec3(0.65, 0.92, 1.0), 0.35) * tear_meniscus * 0.75;

	float alpha = mix(1.0, inner_eye_alpha, in_eye);
	alpha = clamp(alpha + tear_meniscus * 0.35 * (1.0 - smoothstep(0.80, 0.96, t)), 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.92, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}