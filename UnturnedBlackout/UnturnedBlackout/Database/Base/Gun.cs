﻿using System.Collections.Generic;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base;

public class Gun
{
    public ushort GunID { get; set; }
    public string GunName { get; set; }
    public string GunDesc { get; set; }
    public EGun GunType { get; set; }
    public ERarity GunRarity { get; set; }
    public float MovementChange { get; set; }
    public float MovementChangeADS { get; set; }
    public string IconLink { get; set; }
    public int MagAmount { get; set; }
    public int Coins { get; set; }
    public int BuyPrice { get; set; }
    public int ScrapAmount { get; set; }
    public int LevelRequirement { get; set; }
    public bool IsPrimary { get; set; }
    public List<GunAttachment> DefaultAttachments { get; set; }
    public Dictionary<int, GunAttachment> RewardAttachments { get; set; }
    public Dictionary<GunAttachment, int> RewardAttachmentsInverse { get; set; }
    public List<int> LevelXPNeeded { get; set; }

    // VANILLA STATS
    public float LongshotRange { get; set; }
    public Dictionary<EStat, int> Stats { get; set; }

    public Gun(ushort gunID, string gunName, string gunDesc, EGun gunType, ERarity gunRarity, float movementChange, float movementChangeADS, string iconLink, int magAmount, int coins, int buyPrice, int scrapAmount, int levelRequirement, bool isPrimary, List<GunAttachment> defaultAttachments, Dictionary<int, GunAttachment> rewardAttachments, Dictionary<GunAttachment, int> rewardAttachmentsInverse, List<int> levelXPNeeded, float longshotRange, Dictionary<EStat, int> stats)
    {
        GunID = gunID;
        GunName = gunName;
        GunDesc = gunDesc;
        GunType = gunType;
        GunRarity = gunRarity;
        MovementChange = movementChange;
        MovementChangeADS = movementChangeADS;
        IconLink = iconLink;
        MagAmount = magAmount;
        Coins = coins;
        BuyPrice = buyPrice;
        ScrapAmount = scrapAmount;
        LevelRequirement = levelRequirement;
        IsPrimary = isPrimary;
        DefaultAttachments = defaultAttachments;
        RewardAttachments = rewardAttachments;
        RewardAttachmentsInverse = rewardAttachmentsInverse;
        LevelXPNeeded = levelXPNeeded;
        LongshotRange = longshotRange;
        Stats = stats;
    }

    public int GetCoins(int currentLevel) => Coins;
}