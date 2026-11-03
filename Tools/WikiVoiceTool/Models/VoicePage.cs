namespace WikiVoiceTool.Models;

/// <summary>角色页解析结果。</summary>
public sealed record VoicePage(
    int UnitId,
    string CharacterName,
    IReadOnlyList<VoiceStyle> Styles,
    IReadOnlyList<VoiceLine> Lines);

/// <summary>一套声线（一个 CV）。</summary>
public sealed record VoiceStyle(
    int File,
    string Cv,
    string DisplayName);

/// <summary>一条语音。</summary>
public sealed record VoiceLine(
    int No,
    string Title,
    string Text,
    bool HasAudio);
