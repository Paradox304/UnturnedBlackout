using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.CTF;

public class CTFTeam
{
    public Config Config { get; set; }
    public TeamInfo Info { get; set; }

    public int TeamID { get; set; }
    public Dictionary<CSteamID, DateTime> Players { get; set; }

    public ushort FlagID { get; set; }
    public int Score { get; set; }
    public int SpawnPoint { get; set; }

    public int SpawnThreshold { get; set; }
    public GroupInfo IngameGroup { get; set; }
    public uint Frequency { get; set; }

    public bool HasFlag { get; set; }
    public Vector3 FlagSP { get; set; }

    public CTFTeam(int teamID, bool isDummy, TeamInfo info, ushort flagID, Vector3 flagSP)
    {
        Config = Plugin.Instance.Configuration.Instance;
        TeamID = teamID;
        if (!isDummy)
        {
            Info = info;
            Players = new();
            Score = 0;
            SpawnPoint = teamID;
            SpawnThreshold = 0;
            Frequency = Utility.GetFreeFrequency();
            FlagSP = flagSP;
            FlagID = flagID;
            HasFlag = true;
            IngameGroup = GroupManager.addGroup(GroupManager.generateUniqueGroupID(), Info.TeamName);
        }
    }

    public void AddPlayer(CSteamID steamID)
    {
        var player = PlayerTool.getPlayer(steamID);
        Players.Add(steamID, DateTime.UtcNow);

        _ = player.quests.ServerAssignToGroup(IngameGroup.groupID, EPlayerGroupRank.MEMBER, true);
        player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
    }

    public void RemovePlayer(CSteamID steamID)
    {
        var player = PlayerTool.getPlayer(steamID);
        _ = Players.Remove(steamID);
        player.quests.leaveGroup(true);
        player.quests.askSetRadioFrequency(CSteamID.Nil, 0);
    }

    public void Destroy()
    {
        GroupManager.deleteGroup(IngameGroup.groupID);
        Players.Clear();
    }
}