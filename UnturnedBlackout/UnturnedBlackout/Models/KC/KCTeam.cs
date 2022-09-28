using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.KC;

public class KCTeam
{
    public ConfigManager Config => Plugin.Instance.Config;

    public KCGame Game { get; set; }
    public TeamInfo Info { get; set; }

    public int TeamID { get; set; }
    public Dictionary<CSteamID, DateTime> Players { get; set; }

    public ushort DogTagID { get; set; }
    public int Score { get; set; }
    public int SpawnPoint { get; set; }

    public int SpawnThreshold { get; set; }
    public GroupInfo IngameGroup { get; set; }
    public uint Frequency { get; set; }

    public Coroutine CheckSpawnSwitcher { get; set; }

    public KCTeam(KCGame game, int teamID, bool isDummy, ushort dogTagID, TeamInfo info)
    {
        TeamID = teamID;
        if (!isDummy)
        {
            Info = info;
            Game = game;
            Players = new Dictionary<CSteamID, DateTime>();
            DogTagID = dogTagID;
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
        Logging.Debug($"Player with {steamID} died, team: {Info.TeamName}");
        if (!Players.TryGetValue(steamID, out var lastDeath))
        {
            Logging.Debug($"Could'nt find player registered to the team, return");
            return;
        }

        Logging.Debug($"Last death: {lastDeath}, Current Time: {DateTime.UtcNow}, Seconds: {(DateTime.UtcNow - lastDeath).TotalSeconds}, Spawn Switch Count: {Config.Base.FileData.SpawnSwitchCountSeconds}");
        if ((DateTime.UtcNow - lastDeath).TotalSeconds < Config.Base.FileData.SpawnSwitchCountSeconds)
        {
            Logging.Debug($"Player died within spawn switch count seconds, current threshold {SpawnThreshold}, increasing it by one");
            SpawnThreshold++;
            if (SpawnThreshold > Config.Base.FileData.SpawnSwitchThreshold)
            {
                Logging.Debug($"Threshold reached limit: {Config.Base.FileData.SpawnSwitchThreshold}, switching spawns");
                CheckSpawnSwitcher.Stop();
                Game.SwitchSpawn();
                SpawnThreshold = 0;
            }
            else if (CheckSpawnSwitcher == null)
            {
                CheckSpawnSwitcher = Plugin.Instance.StartCoroutine(SpawnSwitch());
            }
        }

        Players[steamID] = DateTime.UtcNow;
    }

    public IEnumerator SpawnSwitch()
    {
        yield return new WaitForSeconds(Config.Base.FileData.SpawnSwitchTimeFrame);
        Logging.Debug($"Spawn switch time frame reached, setting threshold back to 0 and waiting for kills");
        if (SpawnThreshold > Config.Base.FileData.SpawnSwitchThreshold)
        {
            Game.SwitchSpawn();
        }

        SpawnThreshold = 0;
    }

    public void Destroy()
    {
        CheckSpawnSwitcher.Stop();
        Game = null;
        GroupManager.deleteGroup(IngameGroup.groupID);
        Players.Clear();
    }
}
