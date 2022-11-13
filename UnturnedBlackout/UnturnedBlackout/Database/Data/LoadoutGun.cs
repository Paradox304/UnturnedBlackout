using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Database.Data;

public class LoadoutGun
{
    public Gun Gun { get; set; }
    public int Level { get; set; }
    public int XP { get; set; }
    public int GunKills { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }
    public Dictionary<ushort, LoadoutAttachment> Attachments { get; set; }

    public LoadoutGun(Gun gun, int level, int xP, int gunKills, bool isBought, bool isUnlocked, Dictionary<ushort, LoadoutAttachment> attachments)
    {
        Gun = gun;
        Level = level;
        XP = xP;
        GunKills = gunKills;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
        Attachments = attachments;
    }

    public bool TryGetNeededXP(out int neededXP)
    {
        neededXP = 0;
        if (Gun.LevelXPNeeded.Count <= Level - 1)
            return false;

        neededXP = Gun.LevelXPNeeded[Level - 1];
        return true;

    }

    public Dictionary<EStat, int> GetDefaultStats()
    {
        Dictionary<EStat, int> defaultStats = new();
        for (var i = 0; i <= 7; i++)
        {
            var stat = (EStat)i;
            switch (stat)
            {
                case EStat.RELOAD_SPEED:
                {
                    var defaultMagazine = Gun.DefaultAttachments.FirstOrDefault(k => k.AttachmentType == EAttachment.MAGAZINE);
                    if (defaultMagazine == null)
                    {
                        Logging.Debug($"No default magazine found to get default stats from for gun {Gun.GunName}");
                        break;
                    }

                    if (!defaultMagazine.StatMultipliers.TryGetValue(stat, out var reloadMultiplier))
                    {
                        Logging.Debug($"No reload multiplier found to get default stats from for gun {Gun.GunName}");
                        break;
                    }

                    var reloadValue = Gun.Stats[stat];
                    defaultStats.Add(stat, Mathf.RoundToInt(reloadValue + reloadValue * reloadMultiplier));
                    break;
                }
                case EStat.AMMO:
                {
                    var defaultMagazine = Gun.DefaultAttachments.FirstOrDefault(k => k.AttachmentType == EAttachment.MAGAZINE);
                    if (defaultMagazine == null)
                    {
                        Logging.Debug($"No default magazine found to get default stats from for gun {Gun.GunName}");
                        break;
                    }

                    if (!defaultMagazine.StatMultipliers.TryGetValue(stat, out var amount))
                    {
                        Logging.Debug($"No ammo found to get default stats from for gun {Gun.GunName}");
                        break;
                    }
                    
                    defaultStats.Add(stat, (int)amount);
                    break;
                }
                default:
                {
                    if (!Gun.Stats.TryGetValue(stat, out var statValue))
                    {
                        Logging.Debug($"No default {stat} found to get default stats from for gun {Gun.GunName}");
                        break;
                    }
                    
                    defaultStats.Add(stat, statValue);
                    break;
                }
            }
        }

        return defaultStats;
    }
    
    public void GetCurrentStats(Loadout loadout, out Dictionary<EStat, int> defaultStats, out Dictionary<EStat, int> finalStats, out Dictionary<EStat, int> attachmentsCompare, out Dictionary<EStat, int> perksCompare)
    {
        Logging.Debug($"Getting current stats for gun with name {Gun.GunName}");
        // Get default stats with the current mag in account
        defaultStats = new();
        for (var i = 0; i <= 7; i++)
        {
            var stat = (EStat)i;
            switch (stat)
            {
                case EStat.AMMO:
                {
                    var defaultMagazine = Attachments.Values.FirstOrDefault(k => k.Attachment.AttachmentType == EAttachment.MAGAZINE)?.Attachment;
                    if (defaultMagazine == null)
                    {
                        Logging.Debug($"No equipped magazine found to get current stats from for gun {Gun.GunName}");
                        break;
                    }

                    if (!defaultMagazine.StatMultipliers.TryGetValue(stat, out var amount))
                    {
                        Logging.Debug($"No ammo found to get current stats from for gun {Gun.GunName}");
                        break;
                    }
                    
                    defaultStats.Add(stat, (int)amount);
                    break;
                }
                default:
                {
                    if (!Gun.Stats.TryGetValue(stat, out var statValue))
                    {
                        Logging.Debug($"No default {stat} found to get default stats from for gun {Gun.GunName}");
                        break;
                    }
                    
                    defaultStats.Add(stat, statValue);
                    break;
                }
            }
        }

        Logging.Debug($"Default Stats:");
        foreach (var stat in defaultStats)
            Logging.Debug($"Stat: {stat.Key}, Amount: {stat.Value}");

        var attachments = loadout.Primary == this ? loadout.PrimaryAttachments : loadout.SecondaryAttachments;
        attachmentsCompare = new();
        perksCompare = new();
        finalStats = new();
        Logging.Debug($"Getting all the attachment mulipliers, perk multipliers and computing the compare and final stat value");
        for (var i = 0; i <= 6; i++)
        {
            var stat = (EStat)i;
            var startingStat = defaultStats[stat];
            Logging.Debug($"Stat: {stat}, Starting Value: {startingStat}");
            // First get all the attachment multipliers for this stat and sum them up (so the + and - equalise themselves)
            var attachmentMultiplier = attachments.Values.Sum(k => k.Attachment.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier != 1f ? multiplier : 0f : 0f);
            Logging.Debug($"Combined Attachment Multiplier: {attachmentMultiplier}");
            var afterAttachmentStat = attachmentMultiplier != 0f && attachmentMultiplier != 1f ? Mathf.RoundToInt(startingStat + startingStat * attachmentMultiplier) : startingStat;
            Logging.Debug($"After attachment stat: {afterAttachmentStat}");
            if (startingStat != afterAttachmentStat)
            {
                var attachmentCompare = afterAttachmentStat - startingStat;
                Logging.Debug($"Attachment compare: {attachmentCompare}");
                attachmentsCompare.Add(stat, attachmentCompare);
            }
            
            // Get all the perk multipliers for this stat and sum them up (so the + and - equalise themselves)
            var perkMultiplier = loadout.Perks.Values.Sum(k => k.Perk.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
            Logging.Debug($"Combined perk multiplier: {perkMultiplier}");
            var afterPerkStat = perkMultiplier != 0f && perkMultiplier != 1f ? Mathf.RoundToInt(afterAttachmentStat + afterAttachmentStat * perkMultiplier) : afterAttachmentStat;
            Logging.Debug($"After perk stat: {afterPerkStat}");
            if (afterAttachmentStat != afterPerkStat)
            {
                var perkCompare = afterPerkStat - afterAttachmentStat;
                Logging.Debug($"Perk compare: {perkCompare}");
                perksCompare.Add(stat, perkCompare);
            }
            
            Logging.Debug($"Final stat: {afterPerkStat}");
            finalStats.Add(stat, afterPerkStat);
        }
    }
}