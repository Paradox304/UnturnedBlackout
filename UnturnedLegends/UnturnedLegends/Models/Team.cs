using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;

namespace UnturnedLegends.Models
{
    public class Team
    {
        public int TeamID { get; set; }
        public HashSet<CSteamID> Players { get; set; }

        // Can be flags captured, kills confirmed, kills and time on hill depending upon the gamemode
        public int Score { get; set; }

        public CSteamID GroupID { get; set; }

        public Team(int teamID, bool isDummy)
        {
            TeamID = teamID;
            if (!isDummy)
            {
                Players = new HashSet<CSteamID>();
                Score = 0;
                GroupID = GroupManager.generateUniqueGroupID();
                GroupManager.addGroup(GroupID, TeamID == 0 ? Plugin.Instance.Translate("Blue_Team_Name").ToUnrich() : Plugin.Instance.Translate("Red_Team_Name").ToUnrich());
            }
        }

        public void AddPlayer(CSteamID steamID)
        {
            var player = PlayerTool.getPlayer(steamID);
            Players.Add(steamID);
            player.quests.ServerAssignToGroup(GroupID, EPlayerGroupRank.MEMBER, true);
            player.quests.sendSetRadioFrequency((uint)GroupID);
        }

        public void RemovePlayer(CSteamID steamID)
        {
            var player = PlayerTool.getPlayer(steamID);
            Players.Remove(steamID);
            player.quests.leaveGroup(true);
        }

        public void Destroy()
        {
            GroupManager.deleteGroup(GroupID);
            Players.Clear();
        }
    }
}
