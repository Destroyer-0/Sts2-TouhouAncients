using Godot;

namespace TouhouAncients.Scripts.Vfx;

public partial class NAncientEntranceTransition : ColorRect
{
    float elapsed; float duration = 3.3f;
    public static void Play(Control room, int style)
    {
        if (room.GetNodeOrNull("TouhouAncientEntrance") != null) return;
        if (style == 15) { foreach (var child in room.FindChildren("*", "TextureRect", true, false)) { if (child is TextureRect image && image.IsVisibleInTree() && image.Texture?.ResourcePath.Contains("FutatsuiwaMamizou") == true) { style = image.Texture.ResourcePath.Contains("Mamizou2") ? 16 : 15; break; } } }
        var shader = ResourceLoader.Load<Shader>($"res://shaders/ancient_entrances/entrance_{style:D2}.gdshader"); if (shader == null) return;
        var mat = new ShaderMaterial { Shader = shader }; mat.SetShaderParameter("progress", 0f);
        if (style == 17) mat.SetShaderParameter("rose_atlas_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/rose_atlas.png"));
        if (style == 17) mat.SetShaderParameter("rose_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/blue_rose.png"));
        if (style == 0) mat.SetShaderParameter("glyph_tex", ResourceLoader.Load<Texture2D>("res://shaders/博丽字体.png"));
        if (style == 10) mat.SetShaderParameter("bat_tex", ResourceLoader.Load<Texture2D>("res://shaders/bat.png"));
        if (style == 0) mat.SetShaderParameter("leaf_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/maple_leaf.png"));
        if (style is 2 or 3 or 10) { mat.SetShaderParameter("moon_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/moon.jpg")); mat.SetShaderParameter("light_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/light_01.png")); }
        if (style == 11) { mat.SetShaderParameter("butterfly_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/butterfly.svg")); mat.SetShaderParameter("light_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/light_01.png")); }
        if (style is 4 or 5 or 9 or 10 or 14 or 15 or 16 or 17) { mat.SetShaderParameter("flame_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/flame_01.png")); mat.SetShaderParameter("smoke_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/smoke_04.png")); mat.SetShaderParameter("light_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/light_01.png")); mat.SetShaderParameter("moon_tex", ResourceLoader.Load<Texture2D>("res://images/vfx/ancient_entrances/moon.jpg")); }

        var size = room.GetViewportRect().Size; mat.SetShaderParameter("aspect_ratio", size.X / Mathf.Max(1, size.Y));
        var node = new NAncientEntranceTransition { duration = style is 7 or 17 ? 3.8f : (style == 10 ? 3.6f : (style is 1 or 5 or 9 or 14 or 15 or 16 ? 3.5f : 3.3f)), Name = "TouhouAncientEntrance", Material = mat, MouseFilter = MouseFilterEnum.Ignore, ZIndex = 100 }; room.AddChild(node); node.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        if (style == 5) node.AddChild(new BackBufferCopy { Name = "SeijaRoomCapture", CopyMode = BackBufferCopy.CopyModeEnum.Viewport, ZIndex = -1 });
    }
    public override void _Process(double delta)
    {
        elapsed += (float)delta; float p = Mathf.Clamp((elapsed - .15f) / duration, 0, 1);
        var mat = (ShaderMaterial)Material; mat.SetShaderParameter("progress", p);
        var size = GetViewportRect().Size; mat.SetShaderParameter("aspect_ratio", size.X / Mathf.Max(1, size.Y));
        if (p >= 1) QueueFree();
    }
}











