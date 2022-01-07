using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.GameTypes;

namespace UnturnedBlackout.Models
{
    public class TDMTeam
    {
        public Config Config { get; set; }
        public TDMGame Game { get; set; }

        public int TeamID { get; set; }
        public Dictionary<CSteamID, DateTime> Players { get; set; }

        public int Score { get; set; }
        public int SpawnPoint { get; set; }

        public int SpawnThreshold { get; set; }
        public CSteamID GroupID { get; set; }

        public Coroutine SpawnSwitcher { get; set; }

        public TDMTeam(TDMGame game, int teamID, bool isDummy)
        {
            Config = Plugin.Instance.Configuration.Instance;
            TeamID = teamID;
            if (!isDummy)
            {
                Game = game;
                Players = new Dictionary<CSteamID, DateTime>();
                Score = 0;
                SpawnPoint = teamID;
                SpawnThreshold = 0;
                GroupID = GroupManager.generateUniqueGroupID();
                GroupManager.addGroup(GroupID, TeamID == 0 ? Plugin.Instance.Translate("Blue_Team_Name").ToUnrich() : Plugin.Instance.Translate("Red_Team_Name").ToUnrich());
            }
        }

        public void AddPlayer(CSteamID steamID)
        {
            var player = PlayerTool.getPlayer(steamID);
            Players.Add(steamID, DateTime.UtcNow);
            player.quests.ServerAssignToGroup(GroupID, EPlayerGroupRank.MEMBER, true);
            player.quests.sendSetRadioFrequency((uint)GroupID);
        }

        public void RemovePlayer(CSteamID steamID)
        {
            var player = PlayerTool.getPlayer(steamID);
            Players.Remove(steamID);
            player.quests.leaveGroup(true);
            player.quests.sendSetRadioFrequency(0);
        }

        public void OnDeath(CSteamID steamID)
        {
            Utility.Debug($"Team player died, spawn threshold {SpawnThreshold}");
            if (!Players.TryGetValue(steamID, out DateTime lastDeath))
            {
                return;
            }

            if ((DateTime.UtcNow - lastDeath).TotalSeconds < Config.SpawnSwitchCountSeconds)
            {
                Utility.Debug("Last death comes within the seconds");
                SpawnThreshold++;
                Utility.Debug($"Threshold: {SpawnThreshold}");
                if (SpawnThreshold > Config.SpawnSwitchThreshold)
                {
                    Utility.Debug("Threshold limit reached, switch the spawns");
                    if (SpawnSwitcher != null)
                    {
                        Plugin.Instance.StopCoroutine(SpawnSwitcher);
                    }
                    Game.SwitchSpawn();
                    SpawnThreshold = 0;
                } else if (SpawnSwitcher == null)
                {
                    SpawnSwitcher = Plugin.Instance.StartCoroutine(SpawnSwitch());
                }
            }
            Players[steamID] = DateTime.UtcNow;
        }

        public IEnumerator SpawnSwitch()
        {
            yield return new WaitForSeconds(Plugin.Instance.Configuration.Instance.SpawnSwitchTimeFrame);
            if (SpawnThreshold > Config.SpawnSwitchThreshold)
            {
                Game.SwitchSpawn();
            }
            SpawnThreshold = 0;
        } 

        public void Destroy()
        {
            if (SpawnSwitcher != null)
            {
                Plugin.Instance.StopCoroutine(SpawnSwitcher);
            }
            Game = null;
            GroupManager.deleteGroup(GroupID);
            Players.Clear();
        }
    }
}
