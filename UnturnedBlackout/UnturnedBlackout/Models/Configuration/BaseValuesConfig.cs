using System;
using System.Collections.Generic;
using UnityEngine;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class BaseValuesConfig
{
    public int MaxPlayerNameCharacters { get; set; }
    public string PlayerColorHexCode { get; set; }
    public string HardcoreColor { get; set; }
    public string WebhookURL { get; set; }
    public int MaxKillFeed { get; set; }
    public int KillFeedFont { get; set; }
    public int KillFeedSeconds { get; set; }
    public int VoiceChatFont { get; set; }
    public bool EnableDebugLogs { get; set; }
    public List<LobbySpawn> LobbySpawns { get; set; }
    public int LastDamageAfterHealSeconds { get; set; }
    public float HealSeconds { get; set; }
    public float HealAmount { get; set; }
    public float MovementStepsDelay { get; set; }
    public int SpawnUnavailableSeconds { get; set; }
    public int EndingLeaderboardSeconds { get; set; }
    public int SendingTipSeconds { get; set; }

    public int SpawnSwitchThreshold { get; set; }
    public int SpawnSwitchCountSeconds { get; set; }
    public int SpawnSwitchTimeFrame { get; set; }
    public int SpawnSwitchSeconds { get; set; }

    public int BattlepassTierSkipCost { get; set; }

    public byte HandSlotWidth { get; set; }
    public byte HandSlotHeight { get; set; }
    
    public int DefaultLoadoutAmount { get; set; }
    public int PrimeLoadoutAmount { get; set; }

    public int MinGamesCount { get; set; }
    public int MaxGamesCount { get; set; }
    public int GameThreshold { get; set; }

    public List<ScrollableImage> ScrollableImages { get; set; }
    public int ScrollableImageTimer { get; set; }
    
    public BaseValuesConfig()
    {
        MaxPlayerNameCharacters = 20;

        PlayerColorHexCode = "#FFFF00";
        HardcoreColor = "red";
        WebhookURL = "https://discord.com/api/webhooks/979000847197409280/e7Pbmjj_8bALCCDCbEDMCEVLX2ZSuIG3ymxbd-yb-IxkQ-sToxCkLJRmneeqB6LYVwgC";
        MaxKillFeed = 6;
        KillFeedFont = 40;
        KillFeedSeconds = 10;

        VoiceChatFont = 30;

        EnableDebugLogs = true;

        LobbySpawns = new();

        LastDamageAfterHealSeconds = 3;
        HealSeconds = 0.5f;
        HealAmount = 10;

        MovementStepsDelay = 0;

        SpawnUnavailableSeconds = 5;

        EndingLeaderboardSeconds = 15;

        SpawnSwitchThreshold = 5;
        SpawnSwitchCountSeconds = 11;
        SpawnSwitchTimeFrame = 27;
        SpawnSwitchSeconds = 120;

        BattlepassTierSkipCost = 100;

        HandSlotWidth = 10;
        HandSlotHeight = 10;

        DefaultLoadoutAmount = 5;
        PrimeLoadoutAmount = 10;

        MinGamesCount = 4;
        MaxGamesCount = 10;
        GameThreshold = 70;
        
        ScrollableImages = new();
        ScrollableImageTimer = 5;
    }
}