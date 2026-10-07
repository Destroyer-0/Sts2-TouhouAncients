using System.Collections.Generic;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.badges;

/// <summary>徽章的局内布尔标志：值挂在 live Player 上，经 BaseLib 扩展存档随本局保存。</summary>
internal static class BadgeFlags
{
    private static readonly SpireField<Player, HashSet<string>> Flags = new(() => new HashSet<string>());

    /// <summary>须在 Entry.Init 调用：晚于序列化上下文初始化就不会进存档。</summary>
    public static void Register(string id) =>
        ExtendedSaveHandlers<Player, SerializablePlayer>.RegisterSave<bool>(
            id,
            player => Flags[player]!.Contains(id),
            (player, value) =>
            {
                if (value) Flags[player]!.Add(id);
                else Flags[player]!.Remove(id);
            },
            (value, writer) => writer.WriteBool(value),
            reader => reader.ReadBool());

    public static void Set(Player player, string id) => Flags[player]!.Add(id);

    public static bool Get(SerializablePlayer player, string id) =>
        ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[player]
            .DictForType<bool>().GetValueOrDefault(id);
}
