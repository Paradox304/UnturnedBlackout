using Steamworks;
using System.Collections.Generic;

namespace UnturnedBlackout.Database.Data;

public class PlayerBattlepass
{
    public CSteamID SteamID { get; set; }
    public int CurrentTier { get; set; }
    public int XP { get; set; }
    public HashSet<int> ClaimedFreeRewards { get; set; }
    public HashSet<int> ClaimedPremiumRewards { get; set; }

    public PlayerBattlepass()
    {

    }

    public PlayerBattlepass(CSteamID steamID, int currentTier, int xP, HashSet<int> claimedFreeRewards, HashSet<int> claimedPremiumRewards)
    {
        SteamID = steamID;
        CurrentTier = currentTier;
        XP = xP;
        ClaimedFreeRewards = claimedFreeRewards;
        ClaimedPremiumRewards = claimedPremiumRewards;
    }

    public bool TryGetNeededXP(out int xp)
    {
        if (Plugin.Instance.DB.BattlepassTiersSearchByID.TryGetValue(CurrentTier + 1, out var battlepassTier))
        {
            xp = battlepassTier.XP;
            return true;
        }

        xp = 0;
        return false;
    }
}
