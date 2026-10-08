shader_type canvas_item;

// 魔力躁动：整张牌像 [jitter] 富文本一样高频小幅抖动。
//
// 必须读屏幕：侵蚀 overlay 是挂在卡牌之上的独立矩形，材质只作用于它自己，
// 无法变换卡牌本体。只有把已经画好的卡面读回来、按随机偏移重新采样，
// 才能让「整张牌」动起来。偏移对整张牌一致（刚性），不做逐像素扰动。
//
// 采样点不能夹在卡面矩形内：夹紧会把位移腾出的那条边拉成最外圈像素，
// 看起来就是「卡框钉在原地、只有卡图在动」。不夹紧时腾出的一侧露出的是背景，
// 卡框、卡图、文字才会作为一整块一起位移。
//
// 场景里的 ColorRect 比卡面（-150..150 / -211..211）四周各大 16px，
// 整张牌在矩形内平移就不会被裁边；amplitude + 摆动位移请勿超过这个边距。
//
// 屏幕纹理是预乘 alpha，采样后要除回直通 alpha 再用默认混合输出。
//
// 场景里在采样点前面放了一个 BackBufferCopy（copy_mode = 2）节点：screen_texture
// 拿的是「最近一次 backbuffer 拷贝」的快照，不强制重拷时快照可能只包含卡牌早期绘制的
// 节点（踩过：卡牌的 CanvasGroup 立绘先触发了一次拷贝 → 只有立绘会被搬动，
// 卡框/描述框/标题纹丝不动）。

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear;

// 抖动幅度（屏幕像素）
uniform float amplitude : hint_range(0.0, 40.0) = 4.0;

// 抖动速度：越大换向越快，约 40~60 时基本每帧都在换
uniform float speed : hint_range(1.0, 120.0) = 42.0;

// 左右摆动幅度（弧度，绕卡面中心）：只有平移会显得很「飘」，加一点转动才像在抖
uniform float tilt : hint_range(0.0, 0.2) = 0.012;

float hash11(float n) {
	return fract(sin(n * 127.1) * 43758.5453);
}

float noise11(float t) {
	float i = floor(t);
	float f = fract(t);
	f = f * f * (3.0 - 2.0 * f);
	return mix(hash11(i), hash11(i + 1.0), f);
}

void fragment() {
	// 局部 UV → 屏幕 UV 的线性映射（列 = dS/dUV）。用屏幕空间导数求出来，
	// 就不用知道画布/相机变换，手牌旋转、缩放时同样成立。
	mat2 jac_screen = mat2(dFdx(SCREEN_UV), dFdy(SCREEN_UV));
	mat2 jac_local = mat2(dFdx(UV), dFdy(UV));

	// 卡牌尺寸为 0（入场/消失动画）时导数退化，直接不出图，避免 NaN 闪白
	if (abs(determinant(jac_local)) < 1e-9) {
		COLOR = vec4(0.0);
	} else {
		mat2 local_to_screen = jac_screen * inverse(jac_local);

		float t = TIME * speed;
		float angle = (noise11(t + 271.0) * 2.0 - 1.0) * tilt;
		vec2 offset = vec2(noise11(t) * 2.0 - 1.0, noise11(t + 137.0) * 2.0 - 1.0) * amplitude * SCREEN_PIXEL_SIZE;

		// 目标局部 UV：先绕卡面中心摆动，再整体加偏移
		vec2 centered = UV - 0.5;
		float cs = cos(angle);
		float sn = sin(angle);
		vec2 target_uv = vec2(cs * centered.x - sn * centered.y, sn * centered.x + cs * centered.y) + 0.5;
		vec2 src = SCREEN_UV + local_to_screen * (target_uv - UV) + offset;

		vec4 col = texture(screen_tex, src);
		if (col.a > 0.0001) {
			col.rgb /= col.a; // 预乘 → 直通
		}
		COLOR = col;
	}
}
