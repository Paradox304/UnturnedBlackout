using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Timers;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.TDM
{
    public class TDMTeam
    {
        public Config Config { get; set; }
        public TDMGame Game { get; set; }
        public TeamInfo Info { get; set; }

        public int TeamID { get; set; }
        public Dictionary<CSteamID, DateTime> Players { get; set; }

        public int Score { get; set; }
        public int SpawnPoint { get; set; }

        public int SpawnThreshold { get; set; }
        public GroupInfo IngameGroup { get; set; }
        public uint Frequency { get; set; }

        public Timer m_CheckSpawnSwitch { get; set; }

        public TDMTeam(TDMGame game, int teamID, bool isDummy, TeamInfo info)
        {
            Config = Plugin.Instance.Configuration.Instance;
            TeamID = teamID;
            if (!isDummy)
            {
                Info = info;
                Game = game;
                Players = new Dictionary<CSteamID, DateTime>();
                Score = 0;
                SpawnPoint = teamID;
                SpawnThreshold = 0;
                Frequency = Utility.GetFreeFrequency();
                IngameGroup = GroupManager.addGroup(GroupManager.generateUniqueGroupID(), Info.TeamName);

                m_CheckSpawnSwitch = new Timer(Plugin.Instance.Configuration.Instance.SpawnSwitchTimeFrame * 1000);
                m_CheckSpawnSwitch.Elapsed += SpawnSwitch;
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
                    if (m_CheckSpawnSwitch.Enabled)
                    {
                        m_CheckSpawnSwitch.Stop();
                    }
                    Game.SwitchSpawn();
                    SpawnThreshold = 0;
                }
                else if (!m_CheckSpawnSwitch.Enabled)
                {
                    m_CheckSpawnSwitch.Start();
                }
            }
            Players[steamID] = DateTime.UtcNow;
        }

        private void SpawnSwitch(object sender, ElapsedEventArgs e)
        {
            if (SpawnThreshold > Config.SpawnSwitchThreshold)
            {
                Game.SwitchSpawn();
            }
            SpawnThreshold = 0;
            m_CheckSpawnSwitch.Stop();
        }

        public void Destroy()
        {
            if (m_CheckSpawnSwitch.Enabled)
            {
                m_CheckSpawnSwitch.Stop();
            }
            Game = null;
            GroupManager.deleteGroup(IngameGroup.groupID);
            Players.Clear();
        }
    }
}
