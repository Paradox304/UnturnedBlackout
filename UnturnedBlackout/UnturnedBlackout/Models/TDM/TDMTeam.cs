using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.TDM;

public class TDMTeam : IDisposable
{
    private static ConfigManager Config => Plugin.Instance.Config;

    public TDMGame Game { get; set; }
    public TeamInfo Info { get; set; }

    public int TeamID { get; set; }
    public Dictionary<CSteamID, DateTime> Players { get; set; }

    public int Score { get; set; }
    public int SpawnPoint { get; set; }

    public int SpawnThreshold { get; set; }
    public GroupInfo IngameGroup { get; set; }
    public uint Frequency { get; set; }

    public Coroutine CheckSpawnSwitcher { get; set; }

    public TDMTeam(TDMGame game, int teamID, bool isDummy, TeamInfo info)
    {
        TeamID = teamID;
        if (isDummy)
            return;

        Info = info;
        Game = game;
        Players = new();
        Score = 0;
        SpawnPoint = teamID;
        SpawnThreshold = 0;
        Frequency = Utility.GetFreeFrequency();
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

    public void OnDeath(CSteamID steamID)
    {
        if (!Players.TryGetValue(steamID, out var lastDeath))
        {
            Logging.Debug($"Could'nt find player registered to the team, return");
            return;
        }

        if ((DateTime.UtcNow - lastDeath).TotalSeconds < Config.Base.FileData.SpawnSwitchCountSeconds)
        {
            SpawnThreshold++;
            if (SpawnThreshold > Config.Base.FileData.SpawnSwitchThreshold)
            {
                CheckSpawnSwitcher.Stop();
                Game.SwitchSpawn();
                SpawnThreshold = 0;
            }
            else if (CheckSpawnSwitcher == null)
                CheckSpawnSwitcher = Plugin.Instance.StartCoroutine(SpawnSwitch());
        }

        Players[steamID] = DateTime.UtcNow;
    }

    public IEnumerator SpawnSwitch()
    {
        yield return new WaitForSeconds(Config.Base.FileData.SpawnSwitchTimeFrame);

        Logging.Debug($"Spawn switch time frame reached, setting threshold back to 0 and waiting for kills");
        if (SpawnThreshold > Config.Base.FileData.SpawnSwitchThreshold)
            Game.SwitchSpawn();

        SpawnThreshold = 0;
    }

    public void Dispose()
    {
        Logging.Debug($"TDMTeam for {Info.TeamName} is being disposed");
        CheckSpawnSwitcher.Stop();
        CheckSpawnSwitcher = null;
        GroupManager.deleteGroup(IngameGroup.groupID);
        Utility.ClearFrequency(Frequency);
        Game = null;
        IngameGroup = null;
        Players = null;
    }
    ~TDMTeam()
    {
        Logging.Debug("TDMTeam is being destroyed/finalised", ConsoleColor.Magenta);
    }
    
    /*public void Destroy()
    {
        CheckSpawnSwitcher.Stop();
        GroupManager.deleteGroup(IngameGroup.groupID);
        Utility.ClearFrequency(Frequency);
        Game = null;
        IngameGroup = null;
        Players.Clear();
    }*/
}