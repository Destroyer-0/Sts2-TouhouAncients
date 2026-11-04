shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;
uniform sampler2D butterfly_tex : filter_linear;
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
		p = rot * p * 2.02 + vec2(4.5, 2.8);
		a *= 0.5;
	}
	return v;
}

float sampleLight(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	return texture(light_tex, clamp(uv, 0.0, 1.0)).r * b.x * b.y;
}

float sampleButterfly(vec2 p, vec2 center, float size, float angle, float flap) {
	float c = cos(angle);
	float s = sin(angle);
	vec2 rel = p - center;
	vec2 local = vec2(c * rel.x + s * rel.y, -s * rel.x + c * rel.y);
	local.x /= max(0.28, flap);
	// The downloaded butterfly points upper-left, not straight up.
 vec2 source_forward = vec2(-0.574, -0.819);
 vec2 source_right = vec2(0.819, -0.574);
 vec2 source_local = source_right * local.x - source_forward * local.y;
 vec2 buv = source_local / size + 0.5;
	vec2 in_bounds = step(vec2(0.0), buv) * step(buv, vec2(1.0));
	float mask_b = in_bounds.x * in_bounds.y;
	vec4 tex = texture(butterfly_tex, clamp(buv, 0.0, 1.0));
	return tex.a * mask_b;
}

float sdSakuraPetal(vec2 p) {
	p.x *= 1.3;
	p.y += p.x * p.x * 1.2;
	float r = length(p);
	float a = atan(p.y, p.x);
	float notch = smoothstep(0.3, 1.0, sin(a)) * pow(abs(cos(a * 1.5)), 0.5) * 0.016;
	float base = 0.036 * (0.82 + 0.18 * cos(a * 2.0)) - notch;
	return r - base;
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	vec2 q = vec2(fbm(p * 2.2 + vec2(t * 0.25, -t * 0.3)), fbm(p * 2.2 + vec2(-t * 0.2, t * 0.35)));
	float ink = fbm(p * 3.0 + q * 1.8);

	vec2 reveal_focus = vec2(0.12, -0.04);
	vec2 rp = p - reveal_focus;
	float flow_dist = length(rp * vec2(0.75, 1.05)) + sin(rp.x * 2.5 - rp.y * 2.0) * 0.12 + (ink - 0.5) * 0.42;
	float open_t = pow(smoothstep(0.06, 0.90, t), 1.22) * 1.65;
	float mask = smoothstep(open_t - 0.24, open_t + 0.06, flow_dist);
	if (t < 0.08) {
		mask = 1.0;
	}

	float ink_rim = smoothstep(0.26, 0.0, abs(flow_dist - open_t)) * smoothstep(0.02, 0.18, t) * (1.0 - smoothstep(0.78, 0.98, t));

	float bf_lace = 0.0;
	float bf_aura = 0.0;
	vec3 bf_color_acc = vec3(0.0);

	for (int i = 0; i < 6; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 1.7));
		float h2 = hash(vec2(fi, 6.3));
		float b_size = 0.055 + 0.038 * h1;

		vec2 start_p = vec2(-0.52 + fi * 0.21 + (h2 - 0.5) * 0.12, 0.32 - mod(fi, 2.0) * 0.22);
		vec2 vel = vec2(0.18 + 0.15 * h2, -0.58 - 0.25 * h1);
		float wave = sin(t * 4.2 + fi * 1.9) * 0.08;
		vec2 bpos = start_p + vel * t + vec2(wave, cos(t * 3.5 + fi) * 0.04);

		vec2 tangent = normalize(vel + vec2(cos(t * 4.2 + fi * 1.9) * 0.336, -sin(t * 3.5 + fi) * 0.14));
		float bang = atan(tangent.y, tangent.x) + 1.5707963;
		float flap = 0.38 + 0.62 * abs(cos(t * 11.0 + fi * 2.3));

		float b_alpha = sampleButterfly(p, bpos, b_size, bang, flap);
		float g_main = sampleLight((p - bpos) / (b_size * 1.5) + 0.5);
		vec2 trail_pos1 = bpos - tangent * (b_size * 0.65);
		vec2 trail_pos2 = bpos - tangent * (b_size * 1.25);
		float g_trail = sampleLight((p - trail_pos1) / (b_size * 1.6) + 0.5) * 0.55 + sampleLight((p - trail_pos2) / (b_size * 1.1) + 0.5) * 0.3;

		vec3 b_tint = mix(vec3(1.0, 0.82, 0.94), vec3(0.80, 0.88, 1.0), mod(fi, 2.0));
		bf_lace = clamp(bf_lace + b_alpha, 0.0, 1.0);
		float local_aura = clamp(g_main * 0.18 + g_trail * 0.10, 0.0, 1.0);
		bf_aura = clamp(bf_aura + local_aura, 0.0, 1.0);
		bf_color_acc = mix(bf_color_acc, b_tint, clamp(b_alpha + local_aura * 0.6, 0.0, 1.0));
	}

	float petals = 0.0;
	for (int i = 0; i < 6; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 9.2));
		float h2 = hash(vec2(fi, 4.8));
		vec2 ppos = vec2((h1 - 0.5) * aspect_ratio * 1.1 + t * 0.32, (h2 - 0.5) * 0.9 + t * 0.22);
		float pang = h1 * 6.28 + t * 2.6;
		mat2 prm = mat2(vec2(cos(pang), sin(pang)), vec2(-sin(pang), cos(pang)));
		petals += smoothstep(0.005, -0.002, sdSakuraPetal(prm * (p - ppos)));
	}

	float elem_fade = smoothstep(0.01, 0.12, t) * (1.0 - smoothstep(0.78, 0.98, t));
	bf_lace *= elem_fade;
	bf_aura *= elem_fade;
	petals = clamp(petals, 0.0, 1.0) * elem_fade;

	vec3 col_nether_slate = vec3(0.14, 0.17, 0.28);
	vec3 col_wisteria = vec3(0.38, 0.24, 0.44);
	vec3 col_robe_azure = vec3(0.56, 0.72, 0.86);
	vec3 col_sakura_pink = vec3(0.98, 0.78, 0.88);

	vec3 col = mix(col_nether_slate, col_wisteria, smoothstep(0.15, 0.82, ink));
	col = mix(col, col_robe_azure, smoothstep(-0.35, 0.55, q.x - p.y * 0.5) * 0.5);
	col = mix(col, col_sakura_pink, ink_rim * 0.8);
	col += bf_color_acc * bf_aura * 0.75;
	col = mix(col, vec3(1.0, 0.96, 1.0), bf_lace * 0.95);
	col = mix(col, col_sakura_pink, petals * 0.9);

	float alpha = clamp(mask + ink_rim * 0.75 + bf_lace + bf_aura * 0.65 + petals * 0.85, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	
 // A pleated silk fan opens, then sweeps across and exits the viewport.
 float fan_open = smoothstep(0.04, 0.24, t);
 float fan_exit = smoothstep(0.26, 0.76, t);
 vec2 fan_pivot = vec2(mix(-0.16, 0.12, fan_exit), mix(0.55, 1.65, fan_exit));
 vec2 fan_q = p - fan_pivot;
 float fan_angle = mix(-0.12, 0.08, fan_exit);
 fan_q = mat2(vec2(cos(fan_angle),sin(fan_angle)),vec2(-sin(fan_angle),cos(fan_angle))) * fan_q;
 float fan_r = length(fan_q);
 float fan_a = atan(fan_q.x, -fan_q.y);
 float fan_half = mix(0.035, 1.05, fan_open);
 float fan_sector = (1.0-smoothstep(fan_half-0.01,fan_half+0.012,abs(fan_a))) * smoothstep(0.06,0.11,fan_r) * (1.0-smoothstep(0.77,0.785,fan_r));
 float fan_pleat = 0.5+0.5*cos(fan_a*44.0);
 float fan_ribs = pow(fan_pleat,24.0);
 vec3 fan_silk = mix(vec3(0.18,0.11,0.27),vec3(0.72,0.48,0.64),pow(fan_pleat,0.65));
 fan_silk = mix(fan_silk,vec3(0.86,0.73,0.57),fan_ribs*0.65);
 float fan_border = exp(-pow((fan_r-0.75)/0.009,2.0));
 fan_silk += vec3(0.22,0.12,0.15)*fan_border;
 float fan_fade = smoothstep(0.02,0.10,t)*(1.0-smoothstep(0.68,0.80,t));
 float fan_alpha = fan_sector*fan_fade*0.92;
 col = mix(col,fan_silk,fan_alpha);
 alpha = max(alpha,fan_alpha);

 COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}