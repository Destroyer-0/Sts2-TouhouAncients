using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.badges;

/// <summary>储君牌组中同时存在三个侍仆。</summary>
public class ServantFamilyPhoto : TouhouAncientBadge
{
    public ServantFamilyPhoto() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Gold;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) =>
        player.CharacterId == ModelDb.Character<Regent>().Id &&
        HasCard<ServantSakuya>(player) &&
        HasCard<ServantPatchouli>(player) &&
        HasCard<ServantHongmeiling>(player);
}
