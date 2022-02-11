using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.KC
{
    public class KCTeam
    {
        public Config Config { get; set; }
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
        public Coroutine SpawnSwitcher { get; set; }

        public KCTeam(KCGame game, int teamID, bool isDummy, ushort dogTagID, TeamInfo info)
        {
            Config = Plugin.Instance.Configuration.Instance;
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

        public void OnDeath(CSteamID steamID)
        {
            if (!Players.TryGetValue(steamID, out DateTime lastDeath))
            {
                return;
            }

            if ((DateTime.UtcNow - lastDeath).TotalSeconds < Config.SpawnSwitchCountSeconds)
            {
                SpawnThreshold++;
                if (SpawnThreshold > Config.SpawnSwitchThreshold)
                {
                    if (SpawnSwitcher != null)
                    {
                        Plugin.Instance.StopCoroutine(SpawnSwitcher);
                    }
                    Game.SwitchSpawn();
                    SpawnThreshold = 0;
                }
                else if (SpawnSwitcher == null)
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
            GroupManager.deleteGroup(IngameGroup.groupID);
            Players.Clear();
        }
    }
}
