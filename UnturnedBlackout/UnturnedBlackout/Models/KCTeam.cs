using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnturnedBlackout.GameTypes;

namespace UnturnedBlackout.Models
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
                IngameGroup = GroupManager.addGroup(GroupManager.generateUniqueGroupID(), TeamID == 0 ? Plugin.Instance.Translate("Blue_Team_Name").ToUnrich() : Plugin.Instance.Translate("Red_Team_Name").ToUnrich());
                Utility.Debug($"Game: {Game.Location.LocationName}, Team: {IngameGroup.name}, ID: {IngameGroup.groupID}, FREQ: {Frequency}");
            }
        }

        public void AddPlayer(CSteamID steamID)
        {
            Utility.Debug($"Adding player to team {TeamID}");
            var player = PlayerTool.getPlayer(steamID);
            Players.Add(steamID, DateTime.UtcNow);
            Utility.Debug("Assigning the group");
            player.quests.ServerAssignToGroup(IngameGroup.groupID, EPlayerGroupRank.MEMBER, true);
            Utility.Debug($"Setting the frequency: {Frequency}");
            player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            Utility.Debug($"Setted the frequency, {player.quests.radioFrequency}");
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
