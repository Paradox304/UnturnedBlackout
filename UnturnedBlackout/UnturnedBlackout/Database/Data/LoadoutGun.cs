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
                    defaultStats.Add(stat, reloadValue + Mathf.RoundToInt(reloadValue * reloadMultiplier));
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
        defaultStats = GetDefaultStats();

        attachmentsCompare = new();
        perksCompare = new();
        finalStats = new();
        var attachments = (loadout.Primary == this ? loadout.PrimaryAttachments : loadout.SecondaryAttachments).Select(k => k.Value.Attachment).Where(k => !Gun.DefaultAttachments.Contains(k)).ToList();
        for (var i = 0; i <= 7; i++)
        {
            var stat = (EStat)i;
            switch (stat)
            {
                case EStat.AMMO:
                {
                    var startingAmmoStat = defaultStats[EStat.AMMO];
                    var magazine = attachments.FirstOrDefault(k => k.AttachmentType == EAttachment.MAGAZINE);
                    if (magazine == null)
                    {
                        finalStats.Add(EStat.AMMO, startingAmmoStat);
                        return;
                    }

                    var finalAmmoStat = magazine.StatMultipliers.TryGetValue(EStat.AMMO, out var ammoValue) ? Mathf.RoundToInt(ammoValue) : startingAmmoStat;
                    if (startingAmmoStat != finalAmmoStat)
                    {
                        var ammoCompare = finalAmmoStat - startingAmmoStat;
                        attachmentsCompare.Add(EStat.AMMO, ammoCompare);
                    }
        
                    finalStats.Add(EStat.AMMO, finalAmmoStat);
                    break;
                }
                case EStat.RELOAD_SPEED:
                {
                    var startingStat = defaultStats[stat];
                    var tempStat = Gun.Stats[EStat.RELOAD_SPEED];
                    // First get all the attachment multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var attachmentMultiplier = attachments.Sum(k => k.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterAttachmentStat = attachmentMultiplier != 0f ? tempStat + Mathf.RoundToInt(tempStat * attachmentMultiplier) : startingStat;
                    if (startingStat != afterAttachmentStat)
                    {
                        var attachmentCompare = afterAttachmentStat - startingStat;
                        attachmentsCompare.Add(stat, attachmentCompare);
                    }
            
                    // Get all the perk multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var perkMultiplier = loadout.Perks.Values.Sum(k => k.Perk.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterPerkStat = afterAttachmentStat + Mathf.RoundToInt(afterAttachmentStat * perkMultiplier);
                    if (afterAttachmentStat != afterPerkStat)
                    {
                        var perkCompare = afterPerkStat - afterAttachmentStat;
                        perksCompare.Add(stat, perkCompare);
                    }
            
                    finalStats.Add(stat, afterPerkStat);
                    break;
                }
                default:
                {
                    var startingStat = defaultStats[stat];
                    // First get all the attachment multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var attachmentMultiplier = attachments.Sum(k => k.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterAttachmentStat = startingStat + Mathf.RoundToInt(startingStat * attachmentMultiplier);
                    if (startingStat != afterAttachmentStat)
                    {
                        var attachmentCompare = afterAttachmentStat - startingStat;
                        attachmentsCompare.Add(stat, attachmentCompare);
                    }
            
                    // Get all the perk multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var perkMultiplier = loadout.Perks.Values.Sum(k => k.Perk.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterPerkStat = afterAttachmentStat + Mathf.RoundToInt(afterAttachmentStat * perkMultiplier);
                    if (afterAttachmentStat != afterPerkStat)
                    {
                        var perkCompare = afterPerkStat - afterAttachmentStat;
                        perksCompare.Add(stat, perkCompare);
                    }
            
                    finalStats.Add(stat, afterPerkStat);
                    break;
                }
            }
        }
    }

    public void GetCurrentStats(Loadout loadout, EAttachment attachmentTypeIgnore, out Dictionary<EStat, int> stats)
    {
        var defaultStats = GetDefaultStats();
        stats = new();
        
        var attachments = (loadout.Primary == this ? loadout.PrimaryAttachments : loadout.SecondaryAttachments).Select(k => k.Value.Attachment).Where(k => !Gun.DefaultAttachments.Contains(k) && k.AttachmentType != attachmentTypeIgnore).ToList();
        for (var i = 0; i <= 7; i++)
        {
            var stat = (EStat)i;
            switch (stat)
            {
                case EStat.AMMO:
                {
                    if (attachmentTypeIgnore != EAttachment.MAGAZINE)
                    {
                        var magazine = attachments.FirstOrDefault(k => k.AttachmentType == EAttachment.MAGAZINE);
                        if (magazine == null)
                        {
                            stats.Add(EStat.AMMO, defaultStats[EStat.AMMO]);
                            return;
                        }

                        var ammoCount = magazine.StatMultipliers.TryGetValue(EStat.AMMO, out var ammoCountValue) ? Mathf.RoundToInt(ammoCountValue) : defaultStats[EStat.AMMO];
                        stats.Add(EStat.AMMO, ammoCount);
                    }
                    else
                        stats.Add(EStat.AMMO, defaultStats[EStat.AMMO]);
                    break;
                }
                case EStat.RELOAD_SPEED:
                {
                    var startingStat = defaultStats[stat];
                    var tempStat = Gun.Stats[EStat.RELOAD_SPEED];
            
                    // First get all the attachment multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var attachmentMultiplier = attachments.Sum(k => k.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterAttachmentStat = attachmentMultiplier != 0f ? tempStat + Mathf.RoundToInt(tempStat * attachmentMultiplier) : startingStat;
            
                    // Get all the perk multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var perkMultiplier = loadout.Perks.Values.Sum(k => k.Perk.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterPerkStat = afterAttachmentStat + Mathf.RoundToInt(afterAttachmentStat * perkMultiplier);
                    stats.Add(stat, afterPerkStat);
                    break;
                }
                default:
                {
                    var startingStat = defaultStats[stat];
                    
                    // First get all the attachment multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var attachmentMultiplier = attachments.Sum(k => k.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterAttachmentStat = startingStat + Mathf.RoundToInt(startingStat * attachmentMultiplier);
            
                    // Get all the perk multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var perkMultiplier = loadout.Perks.Values.Sum(k => k.Perk.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterPerkStat = afterAttachmentStat + Mathf.RoundToInt(afterAttachmentStat * perkMultiplier);
                    stats.Add(stat, afterPerkStat);
                    break;
                }
            }
        }
    }
    
    public void GetCurrentStats(Loadout loadout, int perkTypeIgnore, out Dictionary<EStat, int> stats)
    {
        var defaultStats = GetDefaultStats();
        stats = new();
        
        var attachments = (loadout.Primary == this ? loadout.PrimaryAttachments : loadout.SecondaryAttachments).Select(k => k.Value.Attachment).Where(k => !Gun.DefaultAttachments.Contains(k)).ToList();
        var perks = loadout.Perks.Select(k => k.Value).Where(k => k.Perk.PerkType != perkTypeIgnore).ToList();
        for (var i = 0; i <= 7; i++)
        {
            var stat = (EStat)i;
            switch (stat)
            {
                case EStat.AMMO:
                {
                    var magazine = attachments.FirstOrDefault(k => k.AttachmentType == EAttachment.MAGAZINE);
                    if (magazine == null)
                    {
                        stats.Add(EStat.AMMO, defaultStats[EStat.AMMO]);
                        return;
                    }

                    var ammoCount = magazine.StatMultipliers.TryGetValue(EStat.AMMO, out var ammoCountValue) ? Mathf.RoundToInt(ammoCountValue) : defaultStats[EStat.AMMO];
                    stats.Add(EStat.AMMO, ammoCount);
                    break;
                }
                case EStat.RELOAD_SPEED:
                {
                    var startingStat = defaultStats[stat];
                    var tempStat = Gun.Stats[EStat.RELOAD_SPEED];
            
                    // First get all the attachment multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var attachmentMultiplier = attachments.Sum(k => k.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterAttachmentStat = attachmentMultiplier != 0f ? tempStat + Mathf.RoundToInt(tempStat * attachmentMultiplier) : startingStat;
            
                    // Get all the perk multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var perkMultiplier = perks.Sum(k => k.Perk.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterPerkStat = afterAttachmentStat + Mathf.RoundToInt(afterAttachmentStat * perkMultiplier);
                    stats.Add(stat, afterPerkStat);
                    break;
                }
                default:
                {
                    var startingStat = defaultStats[stat];
                    
                    // First get all the attachment multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var attachmentMultiplier = attachments.Sum(k => k.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterAttachmentStat = startingStat + Mathf.RoundToInt(startingStat * attachmentMultiplier);
            
                    // Get all the perk multipliers for this stat and sum them up (so the + and - equalise themselves)
                    var perkMultiplier = perks.Sum(k => k.Perk.StatMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 0f);
                    var afterPerkStat = afterAttachmentStat + Mathf.RoundToInt(afterAttachmentStat * perkMultiplier);
                    stats.Add(stat, afterPerkStat);
                    break;
                }
            }
        }
    }
}