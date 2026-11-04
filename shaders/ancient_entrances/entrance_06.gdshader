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
		p = rot * p * 2.0 + vec2(2.4, 5.1);
		a *= 0.5;
	}
	return v;
}

vec3 prism(float x) {
	return 0.58 + 0.38 * cos(6.28318 * (x + vec3(0.0, 0.33, 0.67)));
}

void fragment() {
	vec2 uv = UV;
	vec2 p = (uv - 0.5) * vec2(aspect_ratio, 1.0);
	float t = progress;

	float r = length(p);
	float a = atan(p.y, p.x);
	float swirl = a + r * 3.2 - t * 3.5;
	vec2 sp = vec2(cos(swirl), sin(swirl)) * r;

	float nebula = fbm(sp * 2.8 + vec2(t * 0.5, -t * 0.3));
	float spark_axis = abs(p.y - p.x * 0.32 + sin(p.x * 4.0 - t * 6.0) * 0.06);
	float field = mix(spark_axis, r * 0.75, 0.45) + (nebula - 0.5) * 0.35;

	float blast = pow(smoothstep(0.05, 0.9, t), 1.2) * 1.55;
	float mask = smoothstep(blast - 0.2, blast + 0.06, field);
	float spark_wave = smoothstep(0.24, 0.0, abs(field - blast)) * smoothstep(0.02, 0.18, t) * (1.0 - smoothstep(0.78, 0.98, t));

	float stardust = 0.0;
	vec3 star_col = vec3(0.0);
	for (int i = 0; i < 6; i++) {
		float fi = float(i);
		vec2 gp = (sp + vec2(t * (0.4 + 0.15 * fi), 0.0)) * (5.0 + fi * 1.4);
		vec2 id = floor(gp);
		vec2 lc = fract(gp) - 0.5;
		float h = hash(id + fi * 23.4);
		float ang = h * 6.28 + t * 4.0;
		mat2 rm = mat2(vec2(cos(ang), sin(ang)), vec2(-sin(ang), cos(ang)));
		vec2 rc = rm * lc;
		float star_r = length(rc) * (1.0 + 0.35 * cos(atan(rc.y, rc.x) * 5.0));
		float s = smoothstep(0.16, 0.03, star_r) * step(0.75, h);
		stardust += s;
		star_col += prism(h + fi * 0.2) * s;
	}
	stardust = clamp(stardust, 0.0, 1.0) * (1.0 - smoothstep(0.8, 0.99, t));

	vec3 witch_night = vec3(0.06, 0.04, 0.15);
	vec3 galaxy_violet = vec3(0.28, 0.14, 0.52);
	vec3 gold_spark = vec3(0.98, 0.88, 0.45);

	vec3 col = mix(witch_night, galaxy_violet, smoothstep(0.2, 0.8, nebula));
	vec3 rainbow = prism(p.x * 0.8 - p.y * 0.5 + nebula * 0.6 - t * 1.5);
	col = mix(col, rainbow, spark_wave * 0.85);
	col = mix(col, gold_spark, pow(spark_wave, 2.2) * 0.75);
	col += star_col * 0.65;

	float alpha = clamp(mask + spark_wave * 0.85 + stardust * 0.7, 0.0, 1.0);
	alpha *= smoothstep(1.0, 0.93, t);
	alpha = mix(1.0, alpha, smoothstep(0.0, 0.04, t));

	COLOR = vec4(clamp(col, 0.0, 1.0), alpha);
}