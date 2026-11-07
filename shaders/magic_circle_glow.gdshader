shader_type canvas_item;
render_mode blend_add, unshaded;

// 八方鬼缚阵脚下魔法阵的发光层。
// 注意 shield_p_00x.png 是**全不透明**的方图（无 alpha 通道），
// 所以遮罩必须取亮度而不是 alpha，否则整块方形都会发光。
// 原版没开 2D Glow 拿不到 bloom，这里用「亮度遮罩提亮 + 8 方向采样外扩」伪造。

// 法阵发光主色
uniform vec4 glow_color : source_color = vec4(1.0, 0.12, 0.35, 1.0);

// 法阵线条本体亮度
uniform float core_strength : hint_range(0.0, 8.0) = 2.6;

// 外圈光晕亮度，0 = 关闭
uniform float halo_strength : hint_range(0.0, 4.0) = 1.2;

// 外圈采样半径（贴图像素）
uniform float halo_radius : hint_range(0.0, 24.0) = 5.0;

// 亮度阈值：低于它的当背景丢掉，高于它的当线条
uniform float cutoff : hint_range(0.0, 1.0) = 0.12;

// 亮度曲线：越大线条越锐、越亮
uniform float lum_power : hint_range(0.5, 4.0) = 1.4;

// 呼吸脉冲
uniform float pulse_amount : hint_range(0.0, 1.0) = 0.18;
uniform float pulse_speed : hint_range(0.0, 10.0) = 2.5;

// 取三通道最大值当亮度遮罩，并抹掉近黑背景
float mask_of(vec3 rgb) {
    float lum = max(rgb.r, max(rgb.g, rgb.b));
    return pow(smoothstep(cutoff, 1.0, lum), lum_power);
}

void fragment() {
    // COLOR 入参是节点 modulate，先存下来（后面会被覆盖）
    float fade = COLOR.a;

    vec2 px = TEXTURE_PIXEL_SIZE * halo_radius;
    vec3 base = texture(TEXTURE, UV).rgb;

    float halo = 0.0;
    halo += mask_of(texture(TEXTURE, UV + vec2(px.x, 0.0)).rgb);
    halo += mask_of(texture(TEXTURE, UV - vec2(px.x, 0.0)).rgb);
    halo += mask_of(texture(TEXTURE, UV + vec2(0.0, px.y)).rgb);
    halo += mask_of(texture(TEXTURE, UV - vec2(0.0, px.y)).rgb);
    halo += mask_of(texture(TEXTURE, UV + px * 0.7071).rgb);
    halo += mask_of(texture(TEXTURE, UV - px * 0.7071).rgb);
    halo += mask_of(texture(TEXTURE, UV + vec2(px.x, -px.y) * 0.7071).rgb);
    halo += mask_of(texture(TEXTURE, UV + vec2(-px.x, px.y) * 0.7071).rgb);
    halo /= 8.0;

    float pulse = 1.0 + sin(TIME * pulse_speed) * pulse_amount;
    vec3 rgb = glow_color.rgb * (mask_of(base) * core_strength + halo * halo_strength) * pulse;

    // blend_add 下只加不减，渐隐靠 modulate:a 缩放整体亮度
    COLOR = vec4(rgb * fade, 1.0);
}
