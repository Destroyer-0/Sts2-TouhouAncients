shader_type canvas_item;
render_mode unshaded;

// 「离开梦境」的溶解效果：像几滴墨落进水里，从多个地方同时洇开。
//
// 溶解场完全由 ink_noise 决定：噪声值低的地方先化开、高的地方后化开，
// 于是噪声里那些「低谷」就成了一个个洇开的中心，画面像被点着的纸一样到处开花。
// 低频负责给大块的峰谷（决定在哪儿化开），高频只用来把边界打散成水渍样的毛边。
//
// 想在编辑器里预览：把本 shader 拖到某个 TextureRect 上，然后
//   1) 勾上 preview_mode（改用节点自身的 TEXTURE 作为画面来源）；
//   2) 把 threshold 从 1 往下拖，观察化开的形状；
//      噪声建议用 FastNoiseLite（Frequency 0.01 左右、FractalType = Fbm）并勾上 Seamless。
// 正式运行时 preview_mode 必须保持关闭（代码里不会去设置它，默认就是关的）。
//
// 正式运行时这个材质挂在 CanvasGroup 上：CanvasGroup 会先把子树画进 backbuffer，
// 再用它的材质绘制结果，所以必须用 screen_texture + SCREEN_UV 把画面读回来。
// 在 CanvasGroup 的材质里 TEXTURE 是空的默认纹理（采样出来全白），不能用。

uniform bool preview_mode = false;

uniform sampler2D screen_src : hint_screen_texture, repeat_disable, filter_linear;
uniform sampler2D ink_noise : repeat_enable;
uniform float threshold : hint_range(0.0, 1.0) = 1.0;

// 边缘柔和程度：越大越像水渍慢慢晕开，越小越像纸被烧出的利落边。
uniform float edge_softness : hint_range(0.01, 0.4) = 0.1;

void fragment() {
    vec4 src = preview_mode
        ? texture(TEXTURE, UV)
        : textureLod(screen_src, SCREEN_UV, 0.0);

    if (!preview_mode && src.a > 0.0001) {
        src.rgb /= src.a; // backbuffer 是预乘 alpha，先还原成直通 alpha
    }

    float broad = texture(ink_noise, UV * 0.9).r;
    float detail = texture(ink_noise, UV * 6.0 + vec2(0.37, 0.11)).r;
    float field = broad * 0.82 + detail * 0.18;

    // 两端留余量：threshold=1 时一像素不溶，threshold=0 时一像素不剩
    float t = (1.0 - threshold) * 1.2 - 0.1;
    float keep = smoothstep(t - edge_softness, t + edge_softness, field);

    COLOR = vec4(src.rgb, src.a * keep);
}
