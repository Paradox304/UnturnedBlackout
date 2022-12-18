using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.CTF;

public class CTFTeam : IDisposable
{
    private static Config Config => Plugin.Instance.Configuration.Instance;
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
        TeamID = teamID;
        if (isDummy)
            return;

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
    
    public void Dispose()
    {
        Logging.Debug($"CTFTeam for team {Info.TeamName} is being disposed. Generation: {GC.GetGeneration(this)}", ConsoleColor.Blue);
        GroupManager.deleteGroup(IngameGroup.groupID);
        Utility.ClearFrequency(Frequency);
        IngameGroup = null;
        Info = null;
        Players = null;
    }
    
    ~CTFTeam()
    {
        Logging.Debug("CTFTeam is being destroyed/finalised", ConsoleColor.Magenta);
    }
    
    /*public void Destroy()
    {
        GroupManager.deleteGroup(IngameGroup.groupID);
        Utility.ClearFrequency(Frequency);
        IngameGroup = null;
        Info = null;
        Players.Clear();
    }*/
}