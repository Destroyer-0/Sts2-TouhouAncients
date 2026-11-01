shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;
uniform sampler2D glyph_tex : source_color, filter_linear;
uniform sampler2D leaf_tex : source_color, filter_linear;

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

float sdBox(vec2 p, vec2 b) {
	vec2 d = abs(p) - b;
	return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float sdOctagon(vec2 p, float r) {
	const vec3 k = vec3(-0.9238795325, 0.3826834323, 0.4142135623);
	p = abs(p);
	p -= 2.0 * min(dot(vec2(k.x, k.y), p), 0.0) * vec2(k.x, k.y);
	p -= 2.0 * min(dot(vec2(-k.x, k.y), p), 0.0) * vec2(-k.x, k.y);
	p -= vec2(clamp(p.x, -k.z * r, k.z * r), r);
	return length(p) * sign(p.y);
}

vec4 evalYinYang(vec2 p, float R) {
	float d_outer = length(p) - R;
	vec2 p_top = p - vec2(0.0, 0.5 * R);
	vec2 p_bot = p - vec2(0.0, -0.5 * R);
	float d_top = length(p_top) - 0.5 * R;
	float d_bot = length(p_bot) - 0.5 * R;
	float s_div = p.x;
	if (d_top < 0.0) s_div = -1.0;
	if (d_bot < 0.0) s_div = 1.0;
	float side = smoothstep(-0.008, 0.008, s_div);
	float eye_top = smoothstep(0.006, -0.006, length(p_top) - 0.13 * R);
	float eye_bot = smoothstep(0.006, -0.006, length(p_bot) - 0.13 * R);
	float pattern = mix(side, 1.0, eye_top);
	pattern = mix(pattern, 0.0, eye_bot);
	float s_curve_dist = min(min(abs(d_top), abs(d_bot)), abs(p.x));
	return vec4(d_outer, pattern, s_curve_dist, max(eye_top, eye_bot));
}

float sdShide(vec2 p) {
	float d = 10.0;
	vec2 cur = p;
	for (int i = 0; i < 4; i++) {
		float fi = float(i);
		float s = (mod(fi, 2.0) < 0.5) ? 1.0 : -1.0;
		float ang = s * 0.45;
		mat2 rm = mat2(vec2(cos(ang), sin(ang)), vec2(-sin(ang), cos(ang)));
		vec2 seg_p = rm * (cur - vec2(s * 0.025, -fi * 0.075));
		d = min(d, sdBox(seg_p, vec2(0.022, 0.048)));
	}
	return d;
}

float sdAutumnLeaf(vec2 p) {
	p.y += p.x * p.x * 1.1;
	float r = length(p);
	float a = atan(p.y, p.x);
	float lobes = 0.042 * (0.7 + 0.3 * cos(a * 5.0)) * (0.85 + 0.25 * sin(a));
	return r - lobes;
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float rot_ang = -t * 3.8;
	mat2 rot_yy = mat2(vec2(cos(rot_ang), sin(rot_ang)), vec2(-sin(rot_ang), cos(rot_ang)));
	float yy_scale = 0.28 + 1.35 * pow(smoothstep(0.18, 0.92, t), 1.5);
	vec4 yy = evalYinYang(rot_yy * p, yy_scale);

	float oct_scale1 = 0.36 + 1.45 * pow(smoothstep(0.12, 0.86, t), 1.35);
	float oct_scale2 = 0.24 + 1.55 * pow(smoothstep(0.22, 0.92, t), 1.45);
	mat2 rot_o1 = mat2(vec2(cos(t * 1.2), sin(t * 1.2)), vec2(-sin(t * 1.2), cos(t * 1.2)));
	mat2 rot_o2 = mat2(vec2(cos(-t * 1.6 + 0.3927), sin(-t * 1.6 + 0.3927)), vec2(-sin(-t * 1.6 + 0.3927), cos(-t * 1.6 + 0.3927)));
	float d_oct1 = sdOctagon(rot_o1 * p, oct_scale1);
	float d_oct2 = sdOctagon(rot_o2 * p, oct_scale2);

	float n_warm = noise(p * 3.5 + vec2(t * 0.5, -t * 0.3)) * 0.5 + noise(p * 7.0 - vec2(t * 0.8, 0.0)) * 0.25;
	float reveal_hole = smoothstep(-0.08, 0.06, d_oct2 + (n_warm - 0.35) * 0.14);
	if (t < 0.18) {
		reveal_hole = 1.0;
	} else {
		reveal_hole = mix(1.0, reveal_hole, smoothstep(0.18, 0.35, t));
	}

	float barrier_lines = smoothstep(0.018, 0.002, abs(d_oct1)) + smoothstep(0.018, 0.002, abs(d_oct2));
	barrier_lines += smoothstep(0.015, 0.002, abs(d_oct1 - 0.06)) * 0.6;
	barrier_lines *= (1.0 - smoothstep(0.78, 0.98, t));

	float shide_shift = pow(smoothstep(0.15, 0.85, t), 1.3) * 0.85;
	float d_shide_l = sdShide(p - vec2(-0.52 - shide_shift, 0.25 + sin(t * 4.0) * 0.03));
	float d_shide_r = sdShide((p - vec2(0.52 + shide_shift, 0.25 - sin(t * 4.0) * 0.03)) * vec2(-1.0, 1.0));
	float shide_mask = smoothstep(0.008, -0.004, min(d_shide_l, d_shide_r)) * (1.0 - smoothstep(0.75, 0.95, t));

	float amulets = 0.0;
	float amulet_red = 0.0;
	float leaves = 0.0;
 vec3 leaf_color_sum = vec3(0.0);
	for (int i = 0; i < 8; i++) {
		float fi = float(i);
		float ang = fi * 0.785398 + t * 0.9;
		float rad = 0.16 + pow(smoothstep(0.08, 0.88, t), 1.15) * (0.95 + 0.15 * mod(fi, 2.0));
		vec2 dir = vec2(cos(ang), sin(ang));
		vec2 ap = p - dir * rad;
		mat2 rm = mat2(vec2(dir.x, dir.y), vec2(-dir.y, dir.x));
		vec2 lp = rm * ap;
		float db = sdBox(lp, vec2(0.075, 0.028));
		float a_body = smoothstep(0.006, -0.003, db);
		vec2 glyph_uv = vec2(lp.y / 0.056, -lp.x / 0.15) + 0.5;
 float a_seal = (1.0 - texture(glyph_tex, clamp(glyph_uv, 0.0, 1.0)).g) * a_body;
		amulets += a_body;
		amulet_red += a_seal * a_body;

		float h1 = hash(vec2(fi, 3.7));
		float h2 = hash(vec2(fi, 9.1));
		vec2 leaf_pos = vec2((h1 - 0.5) * aspect_ratio * 1.15 + t * 0.28 + sin(t * 3.2 + fi) * 0.08, (h2 - 0.5) * 1.05 + t * (0.30 + h1 * 0.28));
		float lang = h1 * 6.28 + sin(t * 3.4 + fi) * 0.75 + t * 0.6;
		mat2 lrm = mat2(vec2(cos(lang), sin(lang)), vec2(-sin(lang), cos(lang)));
		vec2 leaf_local = lrm * (p - leaf_pos);
 leaf_local.x /= max(0.24, abs(cos(t * 3.5 + fi * 1.8)));
 float leaf_size = 0.055 + 0.040 * h2;
 vec2 leaf_uv = leaf_local / leaf_size + 0.5;
 vec2 leaf_bounds = step(vec2(0.0), leaf_uv) * step(leaf_uv, vec2(1.0));
 vec4 leaf_sample = texture(leaf_tex, clamp(leaf_uv, 0.0, 1.0));
 float leaf_a = leaf_sample.a * leaf_bounds.x * leaf_bounds.y;
 vec3 leaf_shade = mix(vec3(0.82, 0.69, 0.62), vec3(1.0, 0.95, 0.85), 0.5 + 0.5 * cos(t * 3.5 + fi * 1.8));
 leaf_color_sum += leaf_sample.rgb * leaf_shade * leaf_a;
 leaves += leaf_a;
	}
	float item_fade = smoothstep(0.02, 0.15, t) * (1.0 - smoothstep(0.75, 0.97, t));
	amulets = clamp(amulets, 0.0, 1.0) * item_fade;
	amulet_red = clamp(amulet_red, 0.0, 1.0) * item_fade;
	vec3 leaf_final_color = leaf_color_sum / max(leaves, 0.001);
 leaves = clamp(leaves, 0.0, 1.0) * (1.0 - smoothstep(0.8, 0.99, t));

	vec3 col_shrine_red = vec3(0.72, 0.14, 0.12);
	vec3 col_autumn_gold = vec3(0.94, 0.56, 0.18);
	vec3 col_deep_wood = vec3(0.18, 0.05, 0.06);
	vec3 col_paper = vec3(0.97, 0.94, 0.88);

	vec3 bg = mix(col_deep_wood, col_shrine_red, smoothstep(0.1, 0.8, n_warm));
	bg = mix(bg, col_autumn_gold, smoothstep(0.35, 0.85, p.y + n_warm * 0.6) * 0.65);

	float yy_in = smoothstep(0.01, -0.01, yy.x) * (1.0 - smoothstep(0.32, 0.62, t));
	vec3 yy_col = mix(col_shrine_red, col_paper, yy.y);
	yy_col = mix(yy_col, col_autumn_gold, smoothstep(0.015, 0.0, abs(yy.x)));
	bg = mix(bg, yy_col, yy_in);

	vec3 col = bg;
	col = mix(col, vec3(0.98, 0.84, 0.48), clamp(barrier_lines, 0.0, 1.0) * 0.9);
	col = mix(col, col_paper, shide_mask * 0.95);
	col = mix(col, col_paper, amulets);
	col = mix(col, col_shrine_red, amulet_red);
	col = mix(col, leaf_final_color, leaves * 0.95);

	float alpha = clamp(reveal_hole + barrier_lines * 0.85 + shide_mask + amulets + leaves * 0.85, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}