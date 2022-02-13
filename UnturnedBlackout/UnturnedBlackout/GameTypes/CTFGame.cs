﻿using Rocket.Core;
using Rocket.Core.Utils;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnturnedBlackout.Database;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.Level;

namespace UnturnedBlackout.GameTypes
{
    public class CTFGame : Game
    {
        public Dictionary<int, List<CTFSpawnPoint>> SpawnPoints { get; set; }

        public List<CTFPlayer> Players { get; set; }
        public Dictionary<CSteamID, CTFPlayer> PlayersLookup { get; set; }

        public CTFTeam BlueTeam { get; set; }
        public CTFTeam RedTeam { get; set; }

        public Coroutine GameStarter { get; set; }
        public Coroutine GameEnder { get; set; }

        public uint Frequency { get; set; }

        public CTFGame(ArenaLocation location) : base(EGameType.CTF, location)
        {
            Utility.Debug($"Initializing CTF game for location {location.LocationName}");
            SpawnPoints = new Dictionary<int, List<CTFSpawnPoint>>();
            var blueFlag = Vector3.zero;
            var redFlag = Vector3.zero;

            foreach (var spawnPoint in Plugin.Instance.DataManager.Data.CTFSpawnPoints.Where(k => k.LocationID == location.LocationID))
            {
                if (spawnPoint.IsFlagSP)
                {
                    if ((ETeam)spawnPoint.GroupID == ETeam.Blue)
                    {
                        blueFlag = spawnPoint.GetSpawnPoint();
                    } else if ((ETeam)spawnPoint.GroupID == ETeam.Red)
                    {
                        redFlag = spawnPoint.GetSpawnPoint();
                    }
                    continue;
                }

                if (SpawnPoints.TryGetValue(spawnPoint.GroupID, out List<CTFSpawnPoint> spawnPoints))
                {
                    spawnPoints.Add(spawnPoint);
                }
                else
                {
                    SpawnPoints.Add(spawnPoint.GroupID, new List<CTFSpawnPoint> { spawnPoint });
                }
            }

            Utility.Debug($"Found {SpawnPoints.Count} spawnpoints registered");
            foreach (var key in SpawnPoints.Keys)
            {
                Utility.Debug(key.ToString());
            }
            Players = new List<CTFPlayer>();
            PlayersLookup = new Dictionary<CSteamID, CTFPlayer>();

            var blueTeamInfo = Config.TeamsInfo.FirstOrDefault(k => k.TeamID == location.BlueTeamID);
            var redTeamInfo = Config.TeamsInfo.FirstOrDefault(k => k.TeamID == location.RedTeamID);

            BlueTeam = new CTFTeam((byte)ETeam.Blue, false, blueTeamInfo, Config.CTF.BlueFlagID, blueFlag);
            RedTeam = new CTFTeam((byte)ETeam.Red, false, redTeamInfo, Config.CTF.RedFlagID, redFlag);
            Frequency = Utility.GetFreeFrequency();

            GameStarter = Plugin.Instance.StartCoroutine(StartGame());
        }

        public IEnumerator StartGame()
        {
            TaskDispatcher.QueueOnMainThread(() => WipeItems());

            for (int seconds = Config.CTF.StartSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                foreach (var player in Players)
                {
                    Plugin.Instance.UIManager.SendCountdownSeconds(player.GamePlayer, seconds);
                }
            }
            GamePhase = EGamePhase.Started;

            foreach (var player in Players)
            {
                player.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);

                Plugin.Instance.UIManager.ClearCountdownUI(player.GamePlayer);
            }

            Plugin.Instance.UIManager.SendCTFHUD(BlueTeam, RedTeam, Players);

            ItemManager.dropItem(new Item(RedTeam.FlagID, true), RedTeam.FlagSP, true, true, true);
            ItemManager.dropItem(new Item(BlueTeam.FlagID, true), BlueTeam.FlagSP, true, true, true);
            GameEnder = Plugin.Instance.StartCoroutine(EndGame());
        }

        public IEnumerator EndGame()
        {
            for (int seconds = Config.CTF.EndSeconds; seconds >= 0; seconds--)
            {
                yield return new WaitForSeconds(1);
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                foreach (var player in Players)
                {
                    Plugin.Instance.UIManager.UpdateCTFTimer(player.GamePlayer, timeSpan.ToString(@"m\:ss"));
                }
            }

            CTFTeam wonTeam;
            if (BlueTeam.Score > RedTeam.Score)
            {
                wonTeam = BlueTeam;
            }
            else if (RedTeam.Score > BlueTeam.Score)
            {
                wonTeam = RedTeam;
            }
            else
            {
                wonTeam = new CTFTeam(-1, true, new TeamInfo(), 0, Vector3.zero);
            }
            Plugin.Instance.StartCoroutine(GameEnd(wonTeam));
        }

        public IEnumerator GameEnd(CTFTeam wonTeam)
        {
            if (GameEnder != null)
            {
                Plugin.Instance.StopCoroutine(GameEnder);
            }

            GamePhase = EGamePhase.Ending;
            Plugin.Instance.UIManager.OnGameUpdated(this);

            for (int index = 0; index < Players.Count; index++)
            {
                var player = Players[index];
                Plugin.Instance.UIManager.ClearCTFHUD(player.GamePlayer);
                if (player.GamePlayer.HasScoreboard)
                {
                    player.GamePlayer.HasScoreboard = false;
                    Plugin.Instance.UIManager.HideCTFLeaderboard(player.GamePlayer);
                }
                if (player.Team == wonTeam)
                {
                    var xp = player.XP * Config.CTF.WinMultipler;
                    ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(player.GamePlayer.SteamID, (uint)xp));
                }
                Plugin.Instance.UIManager.SetupPreEndingUI(player.GamePlayer, EGameType.CTF, player.Team.TeamID == wonTeam.TeamID, BlueTeam.Score, RedTeam.Score, BlueTeam.Info.TeamName, RedTeam.Info.TeamName);
                player.GamePlayer.Player.Player.quests.askSetRadioFrequency(CSteamID.Nil, Frequency);
            }
            TaskDispatcher.QueueOnMainThread(() =>
            {
                Plugin.Instance.UIManager.SetupCTFLeaderboard(Players, Location, wonTeam, BlueTeam, RedTeam, false);
                WipeItems();
            });
            yield return new WaitForSeconds(5);
            foreach (var player in Players)
            {
                Plugin.Instance.UIManager.ShowCTFLeaderboard(player.GamePlayer);
            }
            yield return new WaitForSeconds(Config.EndingLeaderboardSeconds);
            foreach (var player in Players.ToList())
            {
                RemovePlayerFromGame(player.GamePlayer);
                Plugin.Instance.GameManager.SendPlayerToLobby(player.GamePlayer.Player);
            }

            Players = new List<CTFPlayer>();
            BlueTeam.Destroy();
            RedTeam.Destroy();

            StartVoting();
        }

        public override void AddPlayerToGame(GamePlayer player)
        {
            Utility.Debug($"Adding {player.Player.CharacterName} to CTF game");
            if (Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                Utility.Debug("Player is already in the game, returning");
                return;
            }

            var team = BlueTeam.Players.Count > RedTeam.Players.Count ? RedTeam : BlueTeam;

            CTFPlayer cPlayer = new CTFPlayer(player, team);
            team.AddPlayer(player.SteamID);
            Players.Add(cPlayer);
            if (PlayersLookup.ContainsKey(player.SteamID))
            {
                PlayersLookup.Remove(player.SteamID);
            }
            PlayersLookup.Add(player.SteamID, cPlayer);
            GiveLoadout(cPlayer);

            if (GamePhase == EGamePhase.Starting)
            {
                player.Player.Player.movement.sendPluginSpeedMultiplier(0);
                Plugin.Instance.UIManager.ShowCountdownUI(player);
                SpawnPlayer(cPlayer);
            }
            else
            {
                Plugin.Instance.UIManager.SendCTFHUD(cPlayer, BlueTeam, RedTeam, Players);
                SpawnPlayer(cPlayer);
            }
            Plugin.Instance.UIManager.SendPreEndingUI(cPlayer.GamePlayer);

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void RemovePlayerFromGame(GamePlayer player)
        {
            Utility.Debug($"Removing {player.Player.CharacterName} from CTF game");
            if (!Players.Exists(k => k.GamePlayer.SteamID == player.SteamID))
            {
                Utility.Debug("Player is already not in the game, returning");
                return;
            }

            var cPlayer = GetCTFPlayer(player.Player);

            Plugin.Instance.UIManager.ClearCTFHUD(player);
            Plugin.Instance.UIManager.ClearPreEndingUI(player);

            if (GamePhase == EGamePhase.Starting)
            {
                Plugin.Instance.UIManager.ClearCountdownUI(player);
                cPlayer.GamePlayer.Player.Player.movement.sendPluginSpeedMultiplier(1);
            }

            if (cPlayer != null)
            {
                cPlayer.Team.RemovePlayer(cPlayer.GamePlayer.SteamID);
                cPlayer.GamePlayer.OnGameLeft();
                Players.Remove(cPlayer);
                PlayersLookup.Remove(cPlayer.GamePlayer.SteamID);
            }

            Plugin.Instance.UIManager.OnGameCountUpdated(this);
        }

        public override void OnPlayerDead(Player player, CSteamID killer, ELimb limb, EDeathCause cause)
        {
            Utility.Debug("Player died, getting the ctf player");
            var cPlayer = GetCTFPlayer(player);
            if (cPlayer == null)
            {
                Utility.Debug("Could'nt find the ctf player, returning");
                return;
            }

            if (cPlayer.GamePlayer.HasScoreboard)
            {
                cPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideCTFLeaderboard(cPlayer.GamePlayer);
            }

            var victimKS = cPlayer.KillStreak;

            Utility.Debug($"Game player found, player name: {cPlayer.GamePlayer.Player.CharacterName}");
            cPlayer.OnDeath(killer);
            cPlayer.GamePlayer.OnDeath(killer, Config.CTF.RespawnSeconds);

            var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;
            if (cPlayer.IsCarryingFlag)
            {
                if (player.clothing.backpack == otherTeam.FlagID)
                {
                    ItemManager.dropItem(new Item(otherTeam.FlagID, true), player.transform.position, true, true, true);
                }
                cPlayer.IsCarryingFlag = false;
                Plugin.Instance.UIManager.UpdateCTFHUD(Players, otherTeam);

                TaskDispatcher.QueueOnMainThread(() =>
                {
                    var otherPlayers = Players.Where(k => k.Team.TeamID == otherTeam.TeamID);
                    foreach (var ply in otherPlayers)
                    {
                        ply.GamePlayer.Player.Player.quests.sendSetMarker(true, cPlayer.GamePlayer.Player.Player.transform.position);
                    }
                });
            }

            ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerDeathsAsync(cPlayer.GamePlayer.SteamID, 1));

            TaskDispatcher.QueueOnMainThread(() =>
            {
                var kPlayer = GetCTFPlayer(killer);
                if (kPlayer == null)
                {
                    Utility.Debug("Could'nt find the killer, returning");
                    return;
                }

                if (kPlayer.GamePlayer.SteamID == cPlayer.GamePlayer.SteamID)
                {
                    Utility.Debug("Player killed themselves, returning");
                    return;
                }

                Utility.Debug($"Killer found, killer name: {kPlayer.GamePlayer.Player.CharacterName}");
                if (cPlayer.GamePlayer.LastDamager.Peek() == kPlayer.GamePlayer.SteamID)
                {
                    cPlayer.GamePlayer.LastDamager.Pop();
                }

                if (cPlayer.GamePlayer.LastDamager.Count > 0)
                {
                    var assister = GetCTFPlayer(cPlayer.GamePlayer.LastDamager.Pop());
                    if (assister != null && assister != kPlayer)
                    {
                        Utility.Debug($"Last damage done to the player by {assister.GamePlayer.Player.CharacterName}");
                        assister.Assists++;
                        assister.Score += Config.AssistPoints;
                        if (!assister.GamePlayer.Player.Player.life.isDead)
                        {
                            Plugin.Instance.UIManager.ShowXPUI(assister.GamePlayer, Config.CTF.XPPerAssist, Plugin.Instance.Translate("Assist_Kill", cPlayer.GamePlayer.Player.CharacterName.ToUnrich()));
                        }
                        ThreadPool.QueueUserWorkItem(async (o) => await Plugin.Instance.DBManager.IncreasePlayerXPAsync(assister.GamePlayer.SteamID, (uint)Config.CTF.XPPerAssist));
                    }
                    cPlayer.GamePlayer.LastDamager.Clear();
                }

                kPlayer.Kills++;
                kPlayer.Score += Config.KillPoints;

                int xpGained = 0;
                string xpText = "";
                if (cause == EDeathCause.MELEE)
                {
                    xpGained += Config.CTF.XPPerMeleeKill;
                    xpText += Plugin.Instance.Translate("Melee_Kill").ToRich();

                }
                else if (limb == ELimb.SKULL)
                {
                    xpGained += Config.CTF.XPPerKillHeadshot;
                    xpText += Plugin.Instance.Translate("Headshot_Kill").ToRich();
                }
                else
                {
                    xpGained += Config.CTF.XPPerKill;
                    xpText += Plugin.Instance.Translate("Normal_Kill").ToRich();
                }
                xpText += "\n";

                kPlayer.KillStreak++;

                if (kPlayer.MultipleKills == 0)
                {
                    kPlayer.MultipleKills++;
                }
                else if ((DateTime.UtcNow - kPlayer.LastKill).TotalSeconds <= 10)
                {
                    xpGained += Config.CTF.BaseXPMK + (++kPlayer.MultipleKills * Config.CTF.IncreaseXPPerMK);
                    var multiKillText = Plugin.Instance.Translate($"Multiple_Kills_Show_{kPlayer.MultipleKills}").ToRich();
                    xpText += (multiKillText == $"Multiple_Kills_Show_{kPlayer.MultipleKills}" ? Plugin.Instance.Translate("Multiple_Kills_Show", kPlayer.MultipleKills).ToRich() : multiKillText) + "\n";
                }
                else
                {
                    kPlayer.MultipleKills = 1;
                }

                if (victimKS > Config.ShutdownKillStreak)
                {
                    xpGained += Config.CTF.ShutdownXP;
                    xpText += Plugin.Instance.Translate("Shutdown_Kill").ToRich() + "\n";
                }

                if (kPlayer.PlayersKilled.ContainsKey(cPlayer.GamePlayer.SteamID))
                {
                    kPlayer.PlayersKilled[cPlayer.GamePlayer.SteamID] += 1;
                    if (kPlayer.PlayersKilled[cPlayer.GamePlayer.SteamID] > Config.DominationKills)
                    {
                        xpGained += Config.CTF.DominationXP;
                        xpText += Plugin.Instance.Translate("Domination_Kill").ToRich() + "\n";
                    }
                }
                else
                {
                    kPlayer.PlayersKilled.Add(cPlayer.GamePlayer.SteamID, 1);
                }

                kPlayer.LastKill = DateTime.UtcNow;
                kPlayer.XP += xpGained;
                Players.Sort((x, y) => y.Kills.CompareTo(x.Kills));

                Plugin.Instance.UIManager.ShowXPUI(kPlayer.GamePlayer, xpGained, xpText);
                Plugin.Instance.UIManager.SendMultiKillSound(kPlayer.GamePlayer, kPlayer.MultipleKills);
                kPlayer.CheckKills();

                OnKill(kPlayer.GamePlayer, cPlayer.GamePlayer, kPlayer.GamePlayer.Player.Player.equipment.itemID, kPlayer.Team.Info.KillFeedHexCode, cPlayer.Team.Info.KillFeedHexCode);

                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    if (limb == ELimb.SKULL)
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerHeadshotKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }
                    else
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerKillsAsync(kPlayer.GamePlayer.SteamID, 1);
                    }
                    await Plugin.Instance.DBManager.IncreasePlayerXPAsync(kPlayer.GamePlayer.SteamID, (uint)xpGained);
                });
            });
        }

        public override void OnPlayerDamage(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            Utility.Debug($"{parameters.player.channel.owner.playerID.characterName} got damaged, checking if the player is in game");
            var player = GetCTFPlayer(parameters.player);
            if (player == null)
            {
                Utility.Debug("Player isn't ingame, returning");
                return;
            }

            if (GamePhase == EGamePhase.Starting || GamePhase == EGamePhase.Ending)
            {
                shouldAllow = false;
                return;
            }

            if (player.GamePlayer.HasSpawnProtection)
            {
                shouldAllow = false;
                return;
            }

            if (parameters.cause == EDeathCause.MELEE)
            {
                parameters.damage = 200;
            }

            player.GamePlayer.OnDamaged(parameters.killer);

            var kPlayer = GetCTFPlayer(parameters.killer);
            if (kPlayer == null)
            {
                Utility.Debug("Killer not found, returning");
                return;
            }

            if (kPlayer.GamePlayer.HasSpawnProtection)
            {
                kPlayer.GamePlayer.HasSpawnProtection = false;
            }
        }

        public override void OnPlayerRevived(UnturnedPlayer player)
        {
            Utility.Debug("Player revived, getting the ctf player");
            var cPlayer = GetCTFPlayer(player);
            if (cPlayer == null)
            {
                Utility.Debug("Could'nt find the ctf player, returning");
                return;
            }

            Utility.Debug($"Game player found, player name: {cPlayer.GamePlayer.Player.CharacterName}");
            Utility.Debug("Reviving the player");

            var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;
            if (otherTeam.FlagID == player.Player.clothing.backpack)
            {
                player.Player.clothing.thirdClothes.backpack = 0;
                player.Player.clothing.askWearBackpack(0, 0, new byte[0], true);
            }

            cPlayer.GamePlayer.OnRevived();
            SpawnPlayer(cPlayer);
        }

        public override void OnTakingItem(GamePlayer player, ItemData itemData, ref bool shouldAllow)
        {
            var cPlayer = GetCTFPlayer(player.Player);
            if (cPlayer == null)
            {
                return;
            }

            Utility.Debug($"{player.Player.CharacterName} is trying to pick up item {itemData.item.id}");
            var otherTeam = cPlayer.Team == BlueTeam ? RedTeam : BlueTeam;

            if (cPlayer.Team.FlagID == itemData.item.id)
            {
                Utility.Debug($"{player.Player.CharacterName} is trying to pick up their own flag, checking if they are saving the flag");
                shouldAllow = false;

                if (!cPlayer.Team.HasFlag)
                {
                    Utility.Debug($"{player.Player.CharacterName} is saving their flag, clearing the flag and putting it back into position");
                    ItemManager.ServerClearItemsInSphere(itemData.point, 1);
                    ItemManager.dropItem(new Item(cPlayer.Team.FlagID, true), cPlayer.Team.FlagSP, true, true, true);
                    cPlayer.Team.HasFlag = true;
                    cPlayer.Score += Config.FlagSavedPoints;
                    cPlayer.XP += Config.CTF.XPPerFlagSaved;
                    cPlayer.FlagsSaved++;
                    Plugin.Instance.UIManager.ShowXPUI(cPlayer.GamePlayer, Config.CTF.XPPerFlagSaved, Plugin.Instance.Translate("Flag_Saved").ToRich());

                    Plugin.Instance.UIManager.UpdateCTFHUD(Players, cPlayer.Team);
                    ThreadPool.QueueUserWorkItem(async (o) =>
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerFlagsSavedAsync(cPlayer.GamePlayer.SteamID, 1);
                        await Plugin.Instance.DBManager.IncreasePlayerXPAsync(cPlayer.GamePlayer.SteamID, (uint)Config.CTF.XPPerFlagSaved);
                    });
                    return;
                }

                if (!cPlayer.IsCarryingFlag)
                {
                    Utility.Debug($"{player.Player.CharacterName} is not carrying an enemy's flag");
                    return;
                }

                Utility.Debug($"{player.Player.CharacterName} is carrying the enemy's flag, getting the flag, other team lost flag {otherTeam.HasFlag}");
                if (player.Player.Player.clothing.backpack == otherTeam.FlagID && !otherTeam.HasFlag)
                {
                    player.Player.Player.clothing.thirdClothes.backpack = 0;
                    player.Player.Player.clothing.askWearBackpack(0, 0, new byte[0], true);

                    ItemManager.dropItem(new Item(otherTeam.FlagID, true), otherTeam.FlagSP, true, true, true);
                    otherTeam.HasFlag = true;
                    cPlayer.Team.Score++;
                    cPlayer.Score += Config.FlagCapturedPoints;
                    cPlayer.XP += Config.CTF.XPPerFlagCaptured;
                    cPlayer.FlagsCaptured++;
                    Plugin.Instance.UIManager.ShowXPUI(cPlayer.GamePlayer, Config.CTF.XPPerFlagCaptured, Plugin.Instance.Translate("Flag_Captured").ToRich());

                    Plugin.Instance.UIManager.UpdateCTFHUD(Players, cPlayer.Team);
                    Plugin.Instance.UIManager.UpdateCTFHUD(Players, otherTeam);

                    if (cPlayer.Team.Score >= Config.CTF.ScoreLimit)
                    {
                        Plugin.Instance.StartCoroutine(GameEnd(cPlayer.Team));
                    }
                    ThreadPool.QueueUserWorkItem(async (o) =>
                    {
                        await Plugin.Instance.DBManager.IncreasePlayerFlagsCapturedAsync(cPlayer.GamePlayer.SteamID, 1);
                        await Plugin.Instance.DBManager.IncreasePlayerXPAsync(cPlayer.GamePlayer.SteamID, (uint)Config.CTF.XPPerFlagCaptured);
                    });
                } else
                {
                    Utility.Debug($"[ERROR] Could'nt find the other team's flag as the player's backpack");
                }

                cPlayer.IsCarryingFlag = false;
            } else if (otherTeam.FlagID == itemData.item.id)
            {
                Utility.Debug($"{player.Player.CharacterName} is trying to pick up the other team's flag, checking if they have their own flag");
                if (!cPlayer.Team.HasFlag)
                {
                    Utility.Debug($"Their own team doesn't have their flag, don't allow to pickup flag");
                    shouldAllow = false;
                    return;
                }

                otherTeam.HasFlag = false;
                cPlayer.IsCarryingFlag = true;

                Plugin.Instance.UIManager.UpdateCTFHUD(Players, otherTeam);
            }
        }

        public override void OnChatMessageSent(GamePlayer player, EChatMode chatMode, string text, ref bool isVisible)
        {
            var tPlayer = GetCTFPlayer(player.Player);
            if (tPlayer == null)
            {
                return;
            }

            if (text.Substring(0, 1) == "/")
            {
                return;
            }

            isVisible = false;
            TaskDispatcher.QueueOnMainThread(() =>
            {
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(player.SteamID, out PlayerData data))
                {
                    return;
                }

                var iconLink = Plugin.Instance.UIManager.Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Plugin.Instance.UIManager.Icons.TryGetValue(0, out icon) ? icon.IconLink28 : "");
                var updatedText = $"<color={tPlayer.Team.Info.ChatPlayerHexCode}>{player.Player.CharacterName}</color>: <color={tPlayer.Team.Info.ChatMessageHexCode}>{text.ToUnrich()}</color>";

                if (chatMode == EChatMode.GLOBAL)
                {
                    foreach (var reciever in Players)
                    {
                        ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: iconLink, useRichTextFormatting: true);
                    }
                    return;
                }

                var teamPlayers = Players.Where(k => k.Team == tPlayer.Team);
                foreach (var reciever in teamPlayers)
                {
                    ChatManager.serverSendMessage(updatedText, Color.white, toPlayer: reciever.GamePlayer.Player.SteamPlayer(), iconURL: iconLink, useRichTextFormatting: true);
                }
            });
        }

        public override void OnVoiceChatUpdated(GamePlayer player)
        {
            if (GamePhase == EGamePhase.Ending)
            {
                SendVoiceChat(Players.Select(k => k.GamePlayer).ToList(), false);
                return;
            }

            var tPlayer = GetCTFPlayer(player.Player);
            SendVoiceChat(Players.Where(k => k.Team == tPlayer.Team).Select(k => k.GamePlayer).ToList(), true);
        }

        public void GiveLoadout(CTFPlayer player)
        {
            Utility.Debug($"Giving loadout to {player.GamePlayer.Player.CharacterName}");

            player.GamePlayer.Player.Player.inventory.ClearInventory();
            R.Commands.Execute(player.GamePlayer.Player, $"/kit {player.Team.Info.KitNames[UnityEngine.Random.Range(0, player.Team.Info.KitNames.Count)]}");
        }

        public void SpawnPlayer(CTFPlayer player)
        {
            Utility.Debug($"Spawning {player.GamePlayer.Player.CharacterName}, getting a random location");
            if (!SpawnPoints.TryGetValue(player.Team.SpawnPoint, out var spawnPoints))
            {
                Utility.Debug($"Could'nt find the spawnpoints for group {player.Team.SpawnPoint}");
                return;
            }

            if (spawnPoints.Count == 0)
            {
                Utility.Debug("No spawnpoints set for CTF, returning");
                return;
            }

            var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            player.GamePlayer.Player.Player.teleportToLocationUnsafe(spawnPoint.GetSpawnPoint(), 0);
            player.GamePlayer.GiveSpawnProtection(Config.CTF.SpawnProtectionSeconds);
        }

        public override void PlayerLeaned(PlayerAnimator obj)
        {
            if (obj.lean != 1) return;
            var cPlayer = GetCTFPlayer(obj.player);
            if (cPlayer == null) return;
            if (GamePhase == EGamePhase.Ending || GamePhase == EGamePhase.Starting) return;
            Utility.Debug($"{obj.player.channel.owner.playerID.characterName} leaned, lean {obj.lean}");

            if (cPlayer.GamePlayer.HasScoreboard)
            {
                cPlayer.GamePlayer.HasScoreboard = false;
                Plugin.Instance.UIManager.HideCTFLeaderboard(cPlayer.GamePlayer);
            }
            else
            {
                cPlayer.GamePlayer.HasScoreboard = true;
                CTFTeam wonTeam;
                if (BlueTeam.Score > RedTeam.Score)
                {
                    wonTeam = BlueTeam;
                }
                else if (RedTeam.Score > BlueTeam.Score)
                {
                    wonTeam = RedTeam;
                }
                else
                {
                    wonTeam = new CTFTeam(-1, true, new TeamInfo(), 0, Vector3.zero);
                }
                Plugin.Instance.UIManager.SetupCTFLeaderboard(cPlayer, Players, Location, wonTeam, BlueTeam, RedTeam, true);
                Plugin.Instance.UIManager.ShowCTFLeaderboard(cPlayer.GamePlayer);
            }
        }

        public override void PlayerStanceChanged(PlayerStance obj)
        {
            var cPlayer = GetCTFPlayer(obj.player);
            if (cPlayer == null)
            {
                return;
            }
            Utility.Debug($"{cPlayer.GamePlayer.Player.CharacterName} changed stance to {obj.stance}");
            cPlayer.GamePlayer.OnStanceChanged(obj.stance);
        }

        public CTFPlayer GetCTFPlayer(CSteamID steamID)
        {
            return PlayersLookup.TryGetValue(steamID, out CTFPlayer cPlayer) ? cPlayer : null;
        }

        public CTFPlayer GetCTFPlayer(UnturnedPlayer player)
        {
            return PlayersLookup.TryGetValue(player.CSteamID, out CTFPlayer cPlayer) ? cPlayer : null;
        }

        public CTFPlayer GetCTFPlayer(Player player)
        {
            return PlayersLookup.TryGetValue(player.channel.owner.playerID.steamID, out CTFPlayer cPlayer) ? cPlayer : null;
        }

        public override bool IsPlayerIngame(CSteamID steamID)
        {
            return PlayersLookup.ContainsKey(steamID);
        }

        public override int GetPlayerCount()
        {
            return Players.Count;
        }

        public override void PlayerPickupItem(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {

        }

        public override List<GamePlayer> GetPlayers()
        {
            return Players.Select(k => k.GamePlayer).ToList();
        }
    }
}
