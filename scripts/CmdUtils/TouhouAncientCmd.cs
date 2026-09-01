using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts;

public static class TouhouAncientCmd
{
    public static string? CheckPathExists(string path) => ResourceLoader.Exists(path) ? path : null;

    public static string CheckPathExistsWithFallback(string path, string alternative) =>
        ResourceLoader.Exists(path) ? path : alternative;
    public static string? CheckPathExistsWithFallback2(string path, string? alternative) =>
        ResourceLoader.Exists(path) ? path : alternative;
    
    public static bool IsScry(CardModel card)
    {
        return card.Keywords.Contains(TouhouAncientKeywords.TouhouAncientSatoriScry);
    }
    public static bool IsKoishi(CardModel card)
    {
        return card.Keywords.Contains(TouhouAncientKeywords.TouhouAncientKoishiUnplayable);
    }

    public static bool IsPlayerDamageIncludePet(Player player, Creature? dealer)
    {
        if (dealer == player.Creature) return true;
        return dealer is { IsPet: true } && dealer.PetOwner?.Creature == player.Creature;
    }

    public static bool IsPlayerDamageIncludePet(Player player, ref Creature? dealer)
    {
        if (dealer == player.Creature) return true;
        if (dealer is { IsPet: true } && dealer.PetOwner?.Creature == player.Creature)
        {
            dealer = dealer.PetOwner.Creature;
            return true;
        }
        return false;
    }
    public static bool IsPlayerDamageIncludePet(Creature? player, Creature? dealer)
    {
        if (dealer == null) return false;
        if (dealer == player) return true;
        return dealer is { IsPet: true } && dealer.PetOwner?.Creature == player;
    }
}