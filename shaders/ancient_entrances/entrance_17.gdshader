shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float aspect_ratio = 1.777778;
uniform sampler2D rose_tex : source_color, filter_linear;
uniform sampler2D rose_atlas_tex : source_color, filter_linear;
uniform sampler2D smoke_tex : filter_linear;
uniform sampler2D flame_tex : filter_linear;

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

float sampleSmoke(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	vec4 tex = texture(smoke_tex, clamp(uv, 0.0, 1.0));
	return max(tex.r, tex.a) * b.x * b.y;
}

float sampleFlame(vec2 uv) {
	vec2 b = step(vec2(0.0), uv) * step(uv, vec2(1.0));
	vec4 tex = texture(flame_tex, clamp(uv, 0.0, 1.0));
	return max(tex.r, tex.a) * b.x * b.y;
}

vec4 evalBlueRose(vec2 p, vec2 center, float rad, float rot) {
 vec2 q=(p-center)/max(0.001,rad);
 q=mat2(vec2(cos(rot),sin(rot)),vec2(-sin(rot),cos(rot)))*q;
 vec2 uv=q*0.5+0.5;
 vec2 bounds=step(vec2(0.0),uv)*step(uv,vec2(1.0));
 vec4 flower=texture(rose_tex,clamp(uv,0.0,1.0));
 float atlas_choice=mod(floor(rot/1.3+0.5),3.0);
 if(atlas_choice>0.5){
  vec2 tile=vec2(0.5,0.0);
  vec2 atlas_uv=tile+clamp(uv,0.01,0.99)*0.5;
  vec3 atlas_color=texture(rose_atlas_tex,atlas_uv).rgb;
  vec3 atlas_background=texture(rose_atlas_tex,tile+vec2(0.01,0.01)).rgb;
  float atlas_alpha=smoothstep(0.055,0.22,distance(atlas_color,atlas_background));
  atlas_alpha=max(atlas_alpha,1.0-smoothstep(0.34,0.42,length(uv-0.5)));
  flower=vec4(atlas_color,atlas_alpha);
 }
 flower.a*=bounds.x*bounds.y;
 return flower;
}

float sdRosePetal(vec2 p) {
	p.x *= 1.15;
	p.y += p.x * p.x * 0.95;
	float r = length(p);
	float a = atan(p.y, p.x);
	float wave = 0.042 * (0.85 + 0.15 * cos(a * 2.0) + 0.08 * sin(a * 3.0));
	return r - wave;
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float stage_open = pow(smoothstep(0.16, 0.88, t), 1.28);
	vec2 koishi_focus = vec2(0.18, -0.10);

	vec2 cage_c = vec2(0.28, 0.38 - stage_open * 0.42);
	vec2 cp = p - cage_c;
	float cage_r = length(cp * vec2(0.88, 1.05));
	float cage_a = atan(cp.y, cp.x);
	float cage_dome = smoothstep(0.82, 0.78, cage_r) * step(cp.y, 0.15);
	float cage_ribs = exp(-abs(sin(cage_a * 14.0)) * cage_r * 95.0);
	float cage_rings = exp(-abs(sin(cage_r * 24.0)) * 28.0);
	float rose_cage_glow = clamp(cage_ribs + cage_rings, 0.0, 1.0) * cage_dome * (1.0 - smoothstep(0.55, 0.88, t));

	float crowd_lights = 0.0;
	vec3 crowd_col = vec3(0.0);
	for (int row = 0; row < 2; row++) {
		float fr = float(row);
		float y_line = 0.16 + fr * 0.14 + stage_open * 0.25;
		float u_x = p.x * (16.0 - fr * 3.0) + sin(t * 7.0 + fr * 2.0) * 0.6;
		float id_x = floor(u_x);
		float f_x = fract(u_x) - 0.5;
		float h_stick = hash(vec2(id_x, fr + 11.0));
		float wave_y = y_line + sin(id_x * 1.3 + t * 8.0) * 0.028;
		vec2 stick_p = vec2(f_x / 16.0, p.y - wave_y);
		float stick_ang = sin(t * 8.0 + id_x) * 0.35;
		stick_p.x += stick_p.y * sin(stick_ang);
		float d_stick = length(stick_p * vec2(1.4, 0.55));
		float glow = exp(-pow(d_stick / 0.022, 2.0)) * step(0.25, h_stick);
		crowd_lights += glow * 0.45;
		vec3 s_hue = (mod(id_x, 3.0) < 0.5) ? vec3(0.45, 0.98, 0.62) : ((mod(id_x, 3.0) < 1.5) ? vec3(0.98, 0.38, 0.78) : vec3(0.28, 0.68, 1.0));
		crowd_col = mix(crowd_col, s_hue, clamp(glow, 0.0, 1.0));
	}
	crowd_lights = clamp(crowd_lights, 0.0, 1.0) * (1.0 - smoothstep(0.58, 0.88, t));

	float spot_beams = 0.0;
	vec3 spot_col = vec3(0.0);
	float rig_flares = 0.0;
	for (int i = 0; i < 4; i++) {
		float fi = float(i);
		vec2 src = vec2(-0.52 + fi * 0.36, -0.54 - stage_open * 0.25);
		float sweep_ang = (fi - 1.5) * (0.22 + 0.48 * stage_open) + sin(t * 3.8 + fi * 1.6) * 0.14 * (1.0 - stage_open * 0.5);
		vec2 b_dir = vec2(sin(sweep_ang), cos(sweep_ang));
		vec2 b_ortho = vec2(b_dir.y, -b_dir.x);
		vec2 rel = p - src;
		float along = dot(rel, b_dir);
		float across = dot(rel, b_ortho);
		float cone_w = 0.035 + max(0.0, along) * 0.19;
		float beam = exp(-pow(across / cone_w, 2.0) * 1.8) * smoothstep(-0.05, 0.25, along) * exp(-max(0.0, along) * 0.65);
		vec3 b_hue = (mod(fi, 2.0) < 0.5) ? vec3(0.42, 0.96, 0.68) : vec3(0.32, 0.72, 1.0);
		if (i == 2) b_hue = vec3(1.0, 0.88, 0.42);
		spot_beams += beam * 0.52;
		spot_col += b_hue * beam * 0.55;

		vec2 fp_star = p - (src + vec2(0.0, 0.06));
		float star_d = exp(-abs(fp_star.x) * 42.0) * exp(-abs(fp_star.y) * 10.0) + exp(-abs(fp_star.y) * 42.0) * exp(-abs(fp_star.x) * 10.0);
		star_d += exp(-length(fp_star) * 16.0) * 0.8;
		rig_flares += star_d * (0.7 + 0.3 * sin(t * 10.0 + fi));
	}
	float beam_env = smoothstep(0.04, 0.24, t) * (1.0 - smoothstep(0.76, 0.96, t));
	spot_beams = clamp(spot_beams, 0.0, 1.0) * beam_env;
	spot_col *= beam_env;
	rig_flares = clamp(rig_flares, 0.0, 1.0) * beam_env;

	float cord_glow = 0.0;
	float cord_core = 0.0;
	for (int i = 0; i < 3; i++) {
		float fi = float(i);
		vec2 lp = p - vec2(-0.18 + fi * 0.18, 0.05 - fi * 0.06) + vec2((fi - 1.0) * stage_open * 0.65, stage_open * 0.25);
		float ca = cos(0.35 * (fi - 1.0) + t * 0.6);
		float sa = sin(0.35 * (fi - 1.0) + t * 0.6);
		lp = vec2(ca * lp.x + sa * lp.y, -sa * lp.x + ca * lp.y);
		float d_loop = abs(lp.y - 0.16 * sin(lp.x * 4.2 + fi * 2.0 - t * 4.5) - lp.x * lp.x * 0.45);
		cord_core += exp(-d_loop * 110.0);
		cord_glow += exp(-d_loop * 22.0) * 0.5;
	}
	cord_core = clamp(cord_core, 0.0, 1.0) * beam_env;
	cord_glow = clamp(cord_glow, 0.0, 1.0) * beam_env;

	float smoke_haze = 0.0;
	for (int i = 0; i < 6; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 2.9));
		float h2 = hash(vec2(fi, 7.4));
		vec2 sc = vec2((h1 - 0.5) * 1.4, (h2 - 0.5) * 0.85);
		sc += normalize(sc - koishi_focus + vec2(0.01)) * stage_open * (0.75 + 0.35 * h1);
		float sang = h1 * 6.28 + t * 0.7 * (mod(fi, 2.0) * 2.0 - 1.0);
		mat2 srot = mat2(vec2(cos(sang), sin(sang)), vec2(-sin(sang), cos(sang)));
		vec2 suv = (srot * (p - sc)) / vec2(0.75 + 0.25 * h1, 0.55 + 0.20 * h2) + 0.5;
		smoke_haze += sampleSmoke(suv) * 0.48;
	}
	smoke_haze = clamp(smoke_haze, 0.0, 1.0);

	vec3 rose_color_layer = vec3(0.0);
	float rose_alpha_layer = 0.0;
	for (int i = 0; i < 5; i++) {
		float fi = float(i);
		vec2 r_base = vec2(-0.48, -0.26);
		if (i == 1) r_base = vec2(-0.32, -0.36);
		if (i == 2) r_base = vec2(0.56, 0.28);
		if (i == 3) r_base = vec2(-0.58, 0.24);
		if (i == 4) r_base = vec2(0.54, -0.32);

		vec2 r_out = normalize(r_base - vec2(0.10, -0.05));
		vec2 r_pos = r_base + r_out * pow(smoothstep(0.18, 0.88, t), 1.25) * 0.88;
		float r_rad = (i == 0 || i == 2) ? 0.25 : 0.18;
		vec4 br = evalBlueRose(p, r_pos, r_rad, fi * 1.3 + t * 0.65 * (mod(fi, 2.0) * 2.0 - 1.0));

		rose_color_layer = mix(rose_color_layer, br.rgb, br.a);
		rose_alpha_layer = clamp(rose_alpha_layer + br.a, 0.0, 1.0);
	}

	float petals = 0.0;
	float confetti = 0.0;
	for (int i = 0; i < 8; i++) {
		float fi = float(i);
		float h1 = hash(vec2(fi, 4.1));
		float h2 = hash(vec2(fi, 8.6));
		vec2 ppos = vec2((h1 - 0.5) * aspect_ratio * 1.1, (h2 - 0.5) * 0.95);
		ppos += normalize(ppos - koishi_focus + vec2(0.02)) * t * (0.45 + 0.35 * h1);
		float pang = h1 * 6.28 + t * (2.6 + h2 * 1.5);
		mat2 prm = mat2(vec2(cos(pang), sin(pang)), vec2(-sin(pang), cos(pang)));
		vec2 lp = prm * (p - ppos);
		if (mod(fi, 2.0) < 0.5) {
			petals += smoothstep(0.005, -0.002, sdRosePetal(lp));
		} else {
			float d_conf = max(abs(lp.x) - 0.024, abs(lp.y - sin(lp.x * 35.0) * 0.004) - 0.007);
			confetti += smoothstep(0.004, -0.002, d_conf);
		}
	}
	float p_fade = smoothstep(0.04, 0.18, t) * (1.0 - smoothstep(0.78, 0.98, t));
	petals = clamp(petals, 0.0, 1.0) * p_fade;
	confetti = clamp(confetti, 0.0, 1.0) * p_fade;

	vec2 kp = p - koishi_focus;
	vec2 hp = kp * vec2(0.82, 0.95);
	hp.y -= sqrt(max(0.01, abs(hp.x))) * 0.22 - 0.06;
	float d_stage_win = length(hp) + sin(atan(kp.y, kp.x) * 5.0 - t * 3.0) * 0.04 + (smoke_haze - 0.45) * 0.22;
	float open_rad = pow(smoothstep(0.14, 0.88, t), 1.22) * 1.58;
	float base_mask = smoothstep(open_rad - 0.18, open_rad + 0.06, d_stage_win);
	if (t < 0.14) {
		base_mask = mix(1.0, base_mask, smoothstep(0.0, 0.14, t));
	}

	float stage_rim = exp(-pow((d_stage_win - open_rad) / 0.095, 2.0)) * smoothstep(0.10, 0.24, t) * (1.0 - smoothstep(0.80, 0.98, t));
	float sound_wave = sin((d_stage_win - t * 1.6) * 26.0) * 0.5 + 0.5;
	sound_wave *= exp(-abs(d_stage_win - open_rad) * 4.5) * beam_env;

	vec3 col_stage_night = vec3(0.07, 0.08, 0.18);
	vec3 col_stage_indigo = vec3(0.16, 0.20, 0.38);
	vec3 col_koishi_mint = vec3(0.56, 0.96, 0.48);
	vec3 col_idol_yellow = vec3(1.0, 0.86, 0.36);
	vec3 col_electric_violet = vec3(0.62, 0.28, 0.98);
	vec3 col_heart_pink = vec3(1.0, 0.52, 0.78);

	vec3 col = mix(col_stage_night, col_stage_indigo, smoothstep(0.1, 0.85, smoke_haze * 0.7 + (0.5 - p.y) * 0.4));
	col += vec3(0.75, 0.88, 1.0) * rose_cage_glow * 0.42;
	col = mix(col, crowd_col, crowd_lights * 0.75);
	col += spot_col * 0.85;
	col += vec3(0.92, 0.98, 1.0) * rig_flares * 0.65;

	col += col_electric_violet * cord_glow * 0.8;
	col = mix(col, vec3(0.86, 0.72, 1.0), cord_core * 0.9);

	vec3 rim_gradient = mix(col_koishi_mint, mix(col_idol_yellow, col_heart_pink, 0.5 + 0.5 * sin(kp.x * 4.0)), 0.5 + 0.5 * cos(kp.y * 4.0));
	col = mix(col, rim_gradient, clamp(stage_rim * 0.85 + sound_wave * 0.35, 0.0, 1.0));

	float rose_fade = 1.0 - smoothstep(0.80, 0.98, t);
	col = mix(col, rose_color_layer, rose_alpha_layer * rose_fade);
	col = mix(col, vec3(0.45, 0.82, 1.0), petals * 0.9);
	col = mix(col, mix(col_idol_yellow, col_heart_pink, 0.5), confetti * 0.92);

	float alpha = clamp(base_mask + stage_rim * 0.8 + rose_alpha_layer * rose_fade + petals * 0.9 + confetti * 0.9 + cord_core * 0.75 + spot_beams * 0.35, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}



