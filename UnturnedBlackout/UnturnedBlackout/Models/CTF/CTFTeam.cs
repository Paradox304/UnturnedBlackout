﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.CTF
{
    public class CTFTeam
    {
        public Config Config { get; set; }
        public TeamInfo Info { get; set; }

        public int TeamID { get; set; }
        public Dictionary<CSteamID, DateTime> Players { get; set; }

        public int Score { get; set; }
        public int SpawnPoint { get; set; }

        public int SpawnThreshold { get; set; }
        public GroupInfo IngameGroup { get; set; }
        public uint Frequency { get; set; }

        public CTFTeam(int teamID, bool isDummy, TeamInfo info)
        {
            Config = Plugin.Instance.Configuration.Instance;
            TeamID = teamID;
            if (!isDummy)
            {
                Info = info;
                Players = new Dictionary<CSteamID, DateTime>();
                Score = 0;
                SpawnPoint = teamID;
                SpawnThreshold = 0;
                Frequency = Utility.GetFreeFrequency();
                IngameGroup = GroupManager.addGroup(GroupManager.generateUniqueGroupID(), Info.TeamName);
            }
        }

        public void AddPlayer(CSteamID steamID)
        {
            var player = PlayerTool.getPlayer(steamID);
            Players.Add(steamID, DateTime.UtcNow);

            player.quests.ServerAssignToGroup(IngameGroup.groupID, EPlayerGroupRank.MEMBER, true);
            player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
        }

        public void RemovePlayer(CSteamID steamID)
        {
            var player = PlayerTool.getPlayer(steamID);
            Players.Remove(steamID);
            player.quests.leaveGroup(true);
            player.quests.askSetRadioFrequency(CSteamID.Nil, 0);
        }

        public void Destroy()
        {
            GroupManager.deleteGroup(IngameGroup.groupID);
            Players.Clear();
        }
    }
}
