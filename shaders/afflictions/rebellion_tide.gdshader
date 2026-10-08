shader_type canvas_item;

// 叛乱之潮：整张牌倒置渲染（180°）——【暂时停用，实现整段注释保留，随时可恢复】
//
// 恢复方法：把下面 <实现> 两段的注释去掉，并删掉文件末尾的停用 stub。
//
// 原理：overlay 只能画自己，要把卡面读回来，再按「绕卡面中心旋转后的局部坐标」去采样屏幕，
// 就等于把整张牌转了个方向。屏幕纹理是预乘 alpha，采样后要除回直通 alpha 再用默认混合输出。
// 场景里在采样点前面放了一个 BackBufferCopy（copy_mode = 2）节点：screen_texture
// 拿的是「最近一次 backbuffer 拷贝」的快照，不强制重拷时快照可能只包含卡牌早期绘制的
// 节点（踩过：卡牌的 CanvasGroup 立绘先触发了一次拷贝 → 只有立绘会被翻转）。

// <实现> 参数
// uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear;
//
// // 倒置角度：180 = 完整倒置（默认）；0 = 不翻转，可用来做任意角度的「歪牌」
// uniform float flip_degrees : hint_range(0.0, 360.0) = 180.0;

// <实现> 主逻辑
// void fragment() {
// 	// 局部 UV → 屏幕 UV 的线性映射（列 = dS/dUV）。用屏幕空间导数求出来，
// 	// 就不用知道画布/相机变换，手牌旋转、缩放时同样成立。
// 	mat2 jac_screen = mat2(dFdx(SCREEN_UV), dFdy(SCREEN_UV));
// 	mat2 jac_local = mat2(dFdx(UV), dFdy(UV));
//
// 	// 卡牌尺寸为 0（入场/消失动画）时导数退化，直接不出图，避免 NaN 闪白
// 	if (abs(determinant(jac_local)) < 1e-9) {
// 		COLOR = vec4(0.0);
// 	} else {
// 		mat2 local_to_screen = jac_screen * inverse(jac_local);
//
// 		float angle = radians(flip_degrees);
// 		float cs = cos(angle);
// 		float sn = sin(angle);
// 		vec2 centered = UV - 0.5;
// 		vec2 target_uv = vec2(cs * centered.x - sn * centered.y, sn * centered.x + cs * centered.y) + 0.5;
// 		vec2 src = SCREEN_UV + local_to_screen * (target_uv - UV);
//
// 		vec4 col = texture(screen_tex, src);
// 		if (col.a > 0.0001) {
// 			col.rgb /= col.a; // 预乘 → 直通
// 		}
// 		COLOR = col;
// 	}
// }

// 停用中：不出图（overlay 场景照常挂载，只是画出来全透明）
void fragment() {
	COLOR = vec4(0.0);
}
