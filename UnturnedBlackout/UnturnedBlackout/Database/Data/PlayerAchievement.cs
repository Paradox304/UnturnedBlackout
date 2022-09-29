using Steamworks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class PlayerAchievement
{
    public CSteamID SteamID { get; set; }
    public Achievement Achievement { get; set; }
    public int CurrentTier { get; set; }
    public int Amount { get; set; }

    public PlayerAchievement(CSteamID steamID, Achievement achievement, int currentTier, int amount)
    {
        SteamID = steamID;
        Achievement = achievement;
        CurrentTier = currentTier;
        Amount = amount;
    }

    public bool TryGetNextTier(out AchievementTier nextTier)
    {
        nextTier = null;
        if (Achievement.TiersLookup.TryGetValue(CurrentTier + 1, out var tier))
        {
            nextTier = tier;
            return true;
        }

        return false;
    }

    public AchievementTier GetCurrentTier() => Achievement.TiersLookup.TryGetValue(CurrentTier, out var tier) ? tier : null;
}