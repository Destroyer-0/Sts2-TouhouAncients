using Godot;

namespace TouhouAncients.Scripts;

/// <summary>
/// 定义 Ancient 对话中不同说话者的头像、outline 和气泡颜色。
/// </summary>
public record AncientSpeakerProfile(string IconPath, string OutlinePath, Color DialogueColor);
