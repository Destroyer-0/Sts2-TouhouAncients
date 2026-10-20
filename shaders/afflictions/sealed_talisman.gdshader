shader_type canvas_item;
render_mode blend_premul_alpha;

// 封印侵蚀：四角各一条符纸带横切角部（形成斜切边），符沿带子循环滚动，带子透出红光。
// 贴图是米色纸 + 红字/红框，用 1-G 提取红色笔画；贴图自带的红框正好成了带子两侧红线与符间分隔。
// 四角靠折叠坐标（取到最近竖边/横边的距离）实现，天然镜像，不必写四份逻辑。
// 所有几何都用像素量（x/y 同单位），否则 300:422 的宽高比会把斜置的符札切成长方形。

uniform sampler2D talisman_texture : filter_linear, repeat_disable;

// 卡面像素尺寸（NCard 默认 300x422）。仅尺寸本身用得到，其余长度参数仍以「卡高 = 1」为单位。
uniform vec2 card_px = vec2(300.0, 422.0);
// 带子中轴离角的距离，沿角落对角线量。越大带子越往卡内挪。
uniform float corner_distance : hint_range(0.0, 0.9) = 0.19;
// 单张符的长度（沿带子方向），带子宽度按贴图比例自动算。一张符 + 一段间隔 = 一个平铺周期。
uniform float talisman_length : hint_range(0.02, 0.6) = 0.15;
// 带子长轴的倾角（屏幕空间角度，度，0 = 水平，负数 = 右上到左下）。
// 默认 -45：与卡边成 45° 的对称斜切（同参考图）；改成 -35.4 则是垂直角落对角线。
uniform float tilt_degrees : hint_range(-180.0, 180.0) = -45.0;
// 滚动速度（卡高/秒），符沿带子循环滑动。
uniform float flow_speed : hint_range(0.0, 3.0) = 0.55;
// 正 = 沿长轴一侧滚动，负 = 反向。
uniform float roll_direction : hint_range(-1.0, 1.0) = 1.0;
// 两张符之间的间隔（相对符长），用符纸底色填充。
uniform float roll_gap : hint_range(0.0, 3.0) = 0.3;
uniform float sway_amount : hint_range(0.0, 0.1) = 0.008;

uniform float ink_opacity : hint_range(0.0, 1.0) = 0.95;
uniform vec4 ink_color : source_color = vec4(0.92, 0.10, 0.10, 1.0);
// 红光亮度与扩散半径（半径以卡高为单位）。
uniform float glow_strength : hint_range(0.0, 3.0) = 0.55;
uniform float glow_radius : hint_range(0.005, 0.3) = 0.05;
uniform vec4 glow_color : source_color = vec4(1.0, 0.16, 0.16, 1.0);
// 符纸米色底的不透明度（0 = 只留红笔画）。默认取贴图实测底色 #FDF6E7。
uniform float paper_opacity : hint_range(0.0, 1.0) = 0.85;
uniform vec4 paper_color : source_color = vec4(0.992, 0.965, 0.906, 1.0);

// 卡面圆角半径（以卡高为单位）。
uniform float card_corner_radius : hint_range(0.0, 0.15) = 0.055;

// 贴图 82x191，四边各有 2px 透明边；平铺前必须裁掉，否则每张符之间会露一条纸色缝。
const vec2 TEX_TRIM = vec2(2.0 / 82.0, 2.0 / 191.0);
const vec2 TEX_INNER = vec2(1.0 - 2.0 * TEX_TRIM.x, 1.0 - 2.0 * TEX_TRIM.y);
// 裁边后内容的宽高比（78x187），带子宽度按它算才不把符压扁。
const float TALISMAN_RATIO = 0.4171;

float rounded_rect_sdf(vec2 p, vec2 half_size, float radius) {
	vec2 d = abs(p) - half_size + vec2(radius);
	return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0) - radius;
}

// 红字/红框的 G 通道接近 0，米色纸 G 很高，相减后用 smoothstep 去纸留笔。
float ink_of(vec2 tuv) {
	vec4 s = texture(talisman_texture, clamp(tuv, vec2(0.0), vec2(1.0)));
	return smoothstep(0.22, 0.55, 1.0 - s.g);
}

void fragment() {
	vec2 uv = UV;
	float t = TIME * flow_speed;

	// 全部几何都在「像素空间」量：x/y 同单位，符札才是正长方形，不会被卡宽高比剪切。
	vec2 p = (uv - 0.5) * card_px;
	float card_sdf = rounded_rect_sdf(p, card_px * 0.5, card_corner_radius * card_px.y);
	float card_mask = 1.0 - smoothstep(-1.0, 0.5, card_sdf);

	// 折叠到最近的角：corner = 从角指向卡内的像素向量。
	vec2 corner = min(uv, 1.0 - uv) * card_px;

	// 呼吸/漂移的相位必须沿卡面连续：按象限取值会在卡片中线上留下一条亮度台阶。
	float phase = (uv.x + uv.y) * 2.6;

	// 带子中轴：从角沿卡面对角线外移（屏幕空间方向）。
	vec2 center = normalize(card_px * 0.5) * (corner_distance * card_px.y);
	center += vec2(sin(t * 0.9 + phase), cos(t * 0.7 + phase)) * (sway_amount * card_px.y);

	// 长轴/法线正交，矩形不会被斜切；倾角按屏幕空间理解。
	float a = radians(tilt_degrees);
	vec2 axis = vec2(cos(a), sin(a));
	vec2 normal = vec2(-sin(a), cos(a));
	vec2 lp = corner - center;
	float along = dot(lp, axis);
	float across = dot(lp, normal);

	// 带子沿长轴无限延伸，两端交给卡面 mask 裁断（就是参考图那种贴边截断）。
	float half_l = talisman_length * 0.5 * card_px.y;
	float half_w = half_l * TALISMAN_RATIO;
	float band_sdf = abs(across) - half_w;
	float aa = max(fwidth(band_sdf), 1e-4);
	float inside = 1.0 - smoothstep(-aa, aa, band_sdf);

	// 沿带子无缝平铺 + 滚动：一张符 + 一段间隔 = 一个周期。间隔 0 时首尾直接相接。
	// 不能做周期淡出：那会在每张符的边界处把红光和红框切一刀，看起来就是断档。
	float period = half_l * 2.0 * (1.0 + roll_gap);
	float fill = 1.0 / (1.0 + roll_gap);
	float u = fract((along - t * roll_direction * card_px.y) / period);
	// 间隔区只铺纸、不采样贴图：越界采样会被 clamp 拉成一条边色带。
	float ink_mask = 1.0 - smoothstep(fill - 0.002, fill + 0.002, u);
	// 裁掉贴图透明边后铺满整个周期；V 取反是因为贴图字符本身是倒的（滚动方向由 along 决定，不受影响）。
	vec2 tuv = vec2(
		(across / (half_w * 2.0) + 0.5) * TEX_INNER.x + TEX_TRIM.x,
		(1.0 - clamp(u / fill, 0.0, 1.0)) * TEX_INNER.y + TEX_TRIM.y);
	float ink = ink_of(tuv) * inside * ink_mask;

	float breathe = 0.85 + 0.15 * sin(t * 1.7 + phase);
	float ink_amount = ink * ink_opacity * breathe;

	// 红光从带子边缘向内/外对称衰减并沿带子连续；乘任何随周期变化的量都会在接缝处画一道。
	// glow_color 的 alpha 也参与调制，可当第二档亮度旋钮。
	float glow = exp(-abs(band_sdf) / max(glow_radius * card_px.y, 1e-3));
	glow *= glow_strength * breathe * glow_color.a;

	// 符纸底色铺满整条带子（含符间间隔），所以间隔看上去和符纸同色。
	float paper = paper_opacity * inside;

	// 纸在下、字在上（若改成相加，红字会被纸色冲成白粉色）；红光最后叠加。
	float base_a = max(paper, ink_amount);
	vec3 rgb = paper_color.rgb * paper * (1.0 - ink_amount) + ink_color.rgb * ink_amount;
	rgb += glow_color.rgb * glow;
	float alpha = clamp(base_a + glow, 0.0, 1.0);

	COLOR = vec4(rgb * card_mask, alpha * card_mask);
}
