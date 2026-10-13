shader_type canvas_item;
render_mode unshaded;

// 「离开梦境」的淡出：threshold 一个参数同时管 朦胧 / 模糊 / 溶解。
// threshold = 1 完全原样，= 0 彻底化开。
//
// 材质挂在 CanvasGroup 上（C# 把哆来咪房间搬进去），画面只能靠 screen_texture +
// SCREEN_UV 从 backbuffer 读回来；CanvasGroup 材质里的 TEXTURE 是空白默认纹理。
// 模糊是偏移采样，半径一大就会采到画面外的白、整屏泛白，所以半径上限要小。

uniform sampler2D screen_src : hint_screen_texture, repeat_disable, filter_linear;
uniform sampler2D ink_noise : repeat_enable;
uniform float threshold : hint_range(0.0, 1.0) = 1.0;

// 溶解前沿的毛边幅度。
uniform float edge_softness : hint_range(0.01, 0.4) = 0.1;

// 模糊半径上限（UV）。想更糊就调大，但要盯着会不会泛白。
const float BLUR_MAX_RADIUS_UV = 0.010;

// 雾色：亮一些的紫白，泛雾时偏亮紫。太亮会盖掉暗部，峰值要压着点。
const vec3 HAZE_TONE = vec3(0.74, 0.70, 0.92);
// 朦胧峰值。
const float HAZE_PEAK = 0.7;

// 12 点双环采样，权重和为 1。采样器不能当函数参数，所以直接用 uniform screen_src。
// UV / SCREEN_UV / TEXTURE 是 fragment 独有内建量，函数里用不了，所以 uv 必须当参数传进来。
vec4 blur_screen(vec2 uv, float radius) {
    vec4 acc = textureLod(screen_src, uv, 0.0) * 0.10;
    for (int i = 0; i < 12; i++) {
        bool outer = i < 6;
        float weight = outer ? 0.09 : 0.06;
        float ring = outer ? 1.0 : 0.5;
        // 内环错开半格，避免两层采样落在同一方向、留下环状痕迹
        float angle = (float(i) + (outer ? 0.0 : 0.5)) * 1.04719755;
        vec2 offset = vec2(cos(angle), sin(angle)) * ring * radius;
        acc += textureLod(screen_src, uv + offset, 0.0) * weight;
    }
    return acc;
}

void fragment() {
    // 进度：0 原样 → 1 化开。开根号让细节先糊掉，溶解随后跟上。
    float fade = sqrt(1.0 - clamp(threshold, 0.0, 1.0));

    vec4 src = fade > 0.0001
        ? blur_screen(SCREEN_UV, fade * BLUR_MAX_RADIUS_UV)
        : textureLod(screen_src, SCREEN_UV, 0.0);

    // backbuffer 是预乘 alpha。模糊后边缘会出现极小的 a，直接除会把颜色炸成白，
    // 所以 a 太小的地方直接丢掉颜色。
    src.rgb = src.a > 0.02 ? clamp(src.rgb / src.a, 0.0, 1.0) : vec3(0.0);

    // 朦胧：去饱和 + 掺紫白雾，跟模糊共用同一条进度。
    float k = fade * HAZE_PEAK * src.a;
    float lum = dot(src.rgb, vec3(0.299, 0.587, 0.114));
    src.rgb = mix(src.rgb, vec3(lum), k);
    src.rgb = mix(src.rgb, HAZE_TONE, k * 0.5);

    // 溶解：噪声低的地方先化开，于是全画面多点同时洇开，而不是整张一起淡。
    float broad = texture(ink_noise, UV * 0.9).r;
    float detail = texture(ink_noise, UV * 6.0).r;
    float field = broad * 0.82 + detail * 0.18;
    // 两端留余量：threshold=1 时一像素不溶，threshold=0 时一像素不剩
    float start = (1.0 - threshold) * 1.2 - 0.1;
    float keep = smoothstep(start - edge_softness, start + edge_softness, field);

    COLOR = vec4(src.rgb, src.a * keep);
}
