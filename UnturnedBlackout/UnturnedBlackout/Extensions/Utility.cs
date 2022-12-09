using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedBlackout.Extensions;

public static class Utility
{
    public static string ToRich(this string value) => value.Replace('[', '<').Replace(']', '>').Replace("osqb", "[").Replace("csqb", "]");

    public static string ToUnrich(this string value) => new Regex(@"\[[^\]]*\]", RegexOptions.IgnoreCase).Replace(value, "").Trim();

    public static void Say(IRocketPlayer target, string message)
    {
        if (target is UnturnedPlayer player)
            ChatManager.serverSendMessage(message.ToRich(), Color.green, toPlayer: player.SteamPlayer(), useRichTextFormatting: true);
        else
            Logger.Log(message.ToUnrich());
    }

    public static void ClearInventory(this PlayerInventory inv)
    {
        inv.player.equipment.sendSlot(0);
        inv.player.equipment.sendSlot(1);

        for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
        for (var index = inv.getItemCount(page) - 1; index >= 0; index--)
            inv.removeItem(page, (byte)index);

        inv.player.clothing.updateClothes(0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0]);
        inv.player.equipment.sendSlot(0);
        inv.player.equipment.sendSlot(1);

        Plugin.Instance.UI.RemoveGunUI(inv.player.channel.GetOwnerTransportConnection());
    }

    public static bool TryGetItemIndex(this PlayerInventory inv, ushort itemID, out byte x, out byte y, out byte page, out ItemJar itemJar)
    {
        x = 0;
        y = 0;
        page = 0;
        itemJar = null;

        var itemCount = inv.getItemCount(PlayerInventory.SLOTS);
        for (byte i = 0; i < itemCount; i++)
        {
            itemJar = inv.getItem(PlayerInventory.SLOTS, i);
            if (itemJar != null && itemJar.item.id == itemID)
            {
                x = itemJar.x;
                y = itemJar.y;
                page = PlayerInventory.SLOTS;
                return true;
            }
        }

        return false;
    }

    public static void Stop(this Coroutine cr)
    {
        if (cr != null)
            Plugin.Instance.StopCoroutine(cr);
    }

    public static string GetOrdinal(int index) => index switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        var _ => index + "th"
    };

    public static string GetDiscordEmoji(int index) => index switch
    {
        1 => ":first_place:",
        2 => ":second_place:",
        3 => ":third_place:",
        var _ => ":military_medal:"
    };

    public static uint GetFreeFrequency()
    {
        while (true)
        {
            var freq = (uint)UnityEngine.Random.Range(300000, 900000);
            if (!UsedFrequencies.Contains(freq) && freq != 460327)
            {
                UsedFrequencies.Add(freq);
                return freq;
            }
        }
    }

    public static List<int> GetIntListFromReaderResult(this object readerResult)
    {
        var readerText = readerResult.ToString();
        return readerText == "" ? new() : readerText.Split(',').Select(k => int.TryParse(k, out var id) ? id : -1).Where(k => k != -1).ToList();
    }

    public static HashSet<int> GetHashSetIntFromReaderResult(this object readerResult)
    {
        var readerText = readerResult.ToString();
        return readerText == "" ? new() : readerText.Split(',').Select(k => int.TryParse(k, out var id) ? id : -1).Where(k => k != -1).ToHashSet();
    }

    public static string GetStringFromIntList(this List<int> listInt)
    {
        var text = "";
        foreach (var id in listInt)
            text += $"{id},";

        return text;
    }

    public static string GetStringFromHashSetInt(this HashSet<int> hashsetInt)
    {
        var text = "";
        foreach (var id in hashsetInt)
            text += $"{id},";

        return text;
    }

    public static Dictionary<ushort, LoadoutAttachment> GetAttachmentsFromString(string text, Gun gun, UnturnedPlayer player)
    {
        Dictionary<ushort, LoadoutAttachment> attachments = new();
        var attachmentsText = text.Split(',');
        Regex numberRegex = new("([0-9]+)");

        foreach (var attachmentText in attachmentsText)
        {
            if (string.IsNullOrEmpty(attachmentText))
                continue;

            var isBought = attachmentText.Contains("B.");
            var isUnlocked = attachmentText.Contains("UL.");
            var numberRegexMatch = numberRegex.Match(attachmentText).Value;

            if (!ushort.TryParse(numberRegexMatch, out var attachmentID))
            {
                Logging.Debug($"Attachment text match with {numberRegexMatch} not found ID");
                continue;
            }

            if (!Plugin.Instance.DB.GunAttachments.TryGetValue(attachmentID, out var gunAttachment))
            {
                Logging.Debug($"Gun with name {gun.GunName} has an attachment with id {attachmentID} which is not registered in attachments table for {player.CharacterName}");
                continue;
            }

            if (attachments.ContainsKey(attachmentID))
            {
                Logging.Debug($"Gun with name {gun.GunName} has a duplicate attachment with id {attachmentID} for {player.CharacterName}");
                continue;
            }

            int levelRequired;
            if (gun.DefaultAttachments.Contains(gunAttachment))
                levelRequired = 0;
            else if (!gun.RewardAttachmentsInverse.TryGetValue(gunAttachment, out levelRequired))
            {
                Logging.Debug($"Gun with name {gun.GunName} has an attachment with name {gunAttachment.AttachmentName} which is not in the default attachments list for the gun or reward attachments list for {player.CharacterName}");
                continue;
            }

            attachments.Add(gunAttachment.AttachmentID, new(gunAttachment, levelRequired, isBought, isUnlocked));
        }

        return attachments;
    }

    public static string GetStringFromAttachments(List<LoadoutAttachment> attachments)
    {
        var text = "";
        foreach (var attachment in attachments)
            text += $"{(attachment.IsBought ? "B." : "")}{(attachment.IsUnlocked ? "UL." : "")}{attachment.Attachment.AttachmentID},";

        return text;
    }

    public static string CreateStringFromDefaultAttachments(List<GunAttachment> gunAttachments)
    {
        var text = "";
        foreach (var attachment in gunAttachments)
            text += $"B.{attachment.AttachmentID},";

        return text;
    }

    public static string CreateStringFromRewardAttachments(List<GunAttachment> gunAttachments)
    {
        var text = "";
        foreach (var attachment in gunAttachments)
            text += $"{attachment.AttachmentID},";

        return text;
    }

    public static List<Reward> GetRewardsFromString(string text)
    {
        return text.Split(' ').Select(GetRewardFromString).Where(reward => reward != null).ToList();
    }

    public static Reward GetRewardFromString(string text)
    {
        Regex letterRegex = new(@"([a-zA-Z_]+)");
        Regex numberRegex = new(@"([0-9.]+)");

        if (string.IsNullOrEmpty(text))
            return null;

        if (!letterRegex.IsMatch(text) || !numberRegex.IsMatch(text))
        {
            Logging.Debug($"There isn't a text or number in the reward text ({text})");
            return null;
        }

        var letterRegexMatch = letterRegex.Match(text).Value;
        if (!Enum.TryParse(letterRegexMatch, true, out ERewardType rewardType))
        {
            Logging.Debug($"Cant find reward type with the match {letterRegexMatch} ({text})");
            return null;
        }

        var numberRegexMatch = numberRegex.Match(text).Value;
        if (string.IsNullOrEmpty(numberRegexMatch))
        {
            Logging.Debug($"Number regex match is coming empty ({text})");
            return null;
        }

        if (numberRegexMatch[0] == '.')
            numberRegexMatch = numberRegexMatch.Remove(0, 1);

        return new(rewardType, numberRegexMatch);
    }

    public static Dictionary<EStat, int> GetStatsFromString(string text)
    {
        var stats = new Dictionary<EStat, int>();
        var letterRegex = new Regex("[A-Za-z_]+");
        var numberRegex = new Regex("[0-9-]+");

        if (string.IsNullOrEmpty(text))
            return stats;
        
        foreach (var statText in text.Split(','))
        {
            var letterMatch = letterRegex.Match(statText);
            if (!letterMatch.Success || !Enum.TryParse(letterMatch.Value, true, out EStat stat))
            {
                Logging.Debug($"Letter match not success, unable to find letters in {statText}");
                continue;
            }
            
            var numberMatch = numberRegex.Match(statText);
            if (!numberMatch.Success || !int.TryParse(numberMatch.Value, out var value))
            {
                Logging.Debug($"Number match not success, unable to find numbers in {statText}");
                continue;
            }

            if (stats.ContainsKey(stat))
            {
                Logging.Debug("Stat already defined in dictionary");
                continue;
            }
            
            stats.Add(stat, value);
        }

        return stats;
    }
    
    public static Dictionary<EStat, float> GetStatMultipliersFromString(string text)
    {
        var stats = new Dictionary<EStat, float>();
        var letterRegex = new Regex("[A-Za-z_]+");
        var numberRegex = new Regex("[0-9-.]+");

        if (string.IsNullOrEmpty(text))
            return stats;
        
        foreach (var statText in text.Split(','))
        {
            var letterMatch = letterRegex.Match(statText);
            if (!letterMatch.Success || !Enum.TryParse(letterMatch.Value, true, out EStat stat))
            {
                Logging.Debug($"Letter match not success, unable to find letters in {statText}");
                continue;
            }
            
            if (stats.ContainsKey(stat))
            {
                Logging.Debug("Stat already defined in dictionary");
                continue;
            }
            
            var numberMatch = numberRegex.Match(statText);
            var numberText = numberMatch.Value;
            if (numberText[0] == '.')
                numberText = numberText.Remove(0, 1);

            if (!numberMatch.Success || !float.TryParse(numberText, out var value))
            {
                Logging.Debug($"Float match not success, unable to find numbers in {numberText}");
                continue;
            }
            
            stats.Add(stat, value);
        }

        return stats;
    }
    
    public static Dictionary<int, List<Reward>> GetRankedRewardsFromString(string text)
    {
        Dictionary<int, List<Reward>> rewardsRanked = new();
        var rank = 0;

        foreach (var rewardsTxt in text.Split(','))
        {
            var rewards = GetRewardsFromString(rewardsTxt);
            rewardsRanked.Add(rank, rewards);
            rank++;
        }

        return rewardsRanked;
    }

    public static List<PercentileReward> GetPercentileRewardsFromString(string text)
    {
        List<PercentileReward> percentileRewards = new();
        Regex percentRegex = new("([0-9]*)%");

        var lowerPercentile = 0;

        foreach (var percRewards in text.Split(','))
        {
            if (!percentRegex.IsMatch(percRewards))
            {
                Logging.Debug("Could'nt find percentage");
                continue;
            }

            var percentRegexMatch = percentRegex.Match(percRewards).Value;
            if (!int.TryParse(percentRegexMatch.Replace("%", ""), out var percentage))
            {
                Logging.Debug($"Couldnt find percentage with the match {percentRegexMatch}");
                continue;
            }

            var rewardsTxt = percRewards.Remove(0, percRewards.IndexOf('-') + 1);
            var rewards = GetRewardsFromString(rewardsTxt);
            percentileRewards.Add(new(lowerPercentile, percentage, rewards));
            lowerPercentile = percentage;
        }

        return percentileRewards;
    }

    public static Dictionary<EQuestCondition, List<int>> GetQuestConditionsFromString(string text)
    {
        Dictionary<EQuestCondition, List<int>> questConditions = new();

        Regex letterRegex = new("([a-zA-Z_]+)");
        Regex numberRegex = new("([0-9-]+)");

        foreach (var conditionTxt in text.Split(','))
        {
            if (string.IsNullOrEmpty(conditionTxt))
                continue;

            if (!letterRegex.IsMatch(conditionTxt) || !numberRegex.IsMatch(conditionTxt))
            {
                Logging.Debug($"There isn't a text or number in the condition text");
                continue;
            }

            var letterRegexMatch = letterRegex.Match(conditionTxt).Value;
            if (!Enum.TryParse(letterRegexMatch, true, out EQuestCondition condition))
            {
                Logging.Debug($"Cant find condition type with the match {letterRegexMatch}");
                continue;
            }

            var numberRegexMatch = numberRegex.Match(conditionTxt).Value;
            if (!int.TryParse(numberRegexMatch, out var conditionValue))
            {
                Logging.Debug($"Cant find condition value with the match {numberRegexMatch}");
                continue;
            }

            if (!questConditions.ContainsKey(condition))
                questConditions.Add(condition, new());

            questConditions[condition].Add(conditionValue);
        }

        return questConditions;
    }

    public static int GetLoadoutAmount(UnturnedPlayer player)
    {
        var config = Plugin.Instance.Config.Loadout.FileData;
        var amount = config.DefaultLoadoutAmount;
        foreach (var loadoutAmount in config.LoadoutAmounts.OrderByDescending(k => k.Amount))
        {
            if (player.HasPermission(loadoutAmount.Permission))
            {
                amount = loadoutAmount.Amount;
                break;
            }
        }

        return amount;
    }

    public static string GetPath(this Transform current) {
        if (current.parent == null)
            return "/" + current.name;
        return current.parent.GetPath() + "/" + current.name;
    }
    
    public static int GetStartingPos(EAttachment attachment) => attachment switch
    {
        EAttachment.SIGHTS => 0,
        EAttachment.GRIP => 4,
        EAttachment.BARREL => 6,
        EAttachment.MAGAZINE => 8,
        var _ => -1
    };

    public static string GetLevelColor(int level) => level switch
    {
        >= 1 and <= 18 => "#a87f49",
        >= 19 and <= 36 => "#f7f7f7",
        >= 37 and <= 54 => "#ffd32e",
        >= 55 and <= 72 => "#41ffe8",
        >= 73 and <= 90 => "#2cff35",
        >= 91 and <= 108 => "#fd2d2d",
        >= 109 and <= 126 => "#b04dff",
        var _ => "white"
    };

    public static string ToFriendlyName(this ELoadoutPage page) => page switch
    {
        ELoadoutPage.PRIMARY_SKIN or ELoadoutPage.SECONDARY_SKIN => "Skin",
        ELoadoutPage.PERK1 or ELoadoutPage.PERK2 or ELoadoutPage.PERK3 => "Perk",
        ELoadoutPage.ATTACHMENT_PRIMARY_GRIP => "Grip",
        ELoadoutPage.ATTACHMENT_PRIMARY_CHARM or ELoadoutPage.ATTACHMENT_SECONDARY_CHARM => "Charm",
        ELoadoutPage.ATTACHMENT_PRIMARY_BARREL or ELoadoutPage.ATTACHMENT_SECONDARY_BARREL => "Barrel",
        ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS or ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS => "Sights",
        ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE or ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE => "Magazine",
        ELoadoutPage.CARD => "Card",
        ELoadoutPage.GLOVE => "Gloves",
        ELoadoutPage.KNIFE => "Melee",
        ELoadoutPage.LETHAL => "Lethal",
        ELoadoutPage.TACTICAL => "Tactical",
        ELoadoutPage.PRIMARY => "Primary",
        ELoadoutPage.SECONDARY => "Secondary",
        ELoadoutPage.KILLSTREAK => "Killstreak",
        var _ => page.ToString()
    };

    public static string ToFriendlyName(this EGamePhase gamePhase) => gamePhase switch
    {
        EGamePhase.WAITING_FOR_PLAYERS => "Waiting",
        EGamePhase.ENDED => "Ended",
        EGamePhase.ENDING => "Ending",
        EGamePhase.STARTED => "Started",
        EGamePhase.STARTING => "Starting",
        var _ => gamePhase.ToString()
    };

    public static string ToFriendlyName(this EHotkey hotkey) => hotkey switch
    {
        EHotkey.LETHAL => "Lethal",
        EHotkey.TACTICAL => "Tactical",
        EHotkey.KILLSTREAK_1 => "Killstreak 1",
        EHotkey.KILLSTREAK_2 => "Killstreak 2",
        EHotkey.KILLSTREAK_3 => "Killstreak 3",
        var _ => throw new ArgumentOutOfRangeException(nameof(hotkey), hotkey, "Hotkey is not as expected")
    };

    public static string ToFriendlyName(this ECurrency currency) => currency switch
    {
        ECurrency.SCRAP => "Scrap",
        ECurrency.CREDIT => "Points",
        ECurrency.COIN => "Blacktags",
        var _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Currency is not as expected")
    };

    public static string ToUIName(this ECurrency currency) => currency switch
    {
        ECurrency.SCRAP => "Scrap",
        ECurrency.CREDIT => "Credits",
        ECurrency.COIN => "Coins",
        var _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, null)
    };

    public static string ToUIName(this EAttachment attachment) => attachment switch
    {
        EAttachment.GRIP => "Grip",
        EAttachment.BARREL => "Barrel",
        EAttachment.MAGAZINE => "Magazine",
        EAttachment.SIGHTS => "Sights",
        var _ => throw new ArgumentOutOfRangeException(nameof(attachment), attachment, "Attachment is not as expected")
    };

    public static string ToUIName(this ETeam team) => team switch
    {
        ETeam.RED => "Red",
        ETeam.BLUE => "Blue",
        var _ => throw new ArgumentOutOfRangeException(nameof(team), team, null)
    };

    public static string ToUIName(this EFlagState state) => state switch
    {
        EFlagState.TAKEN => "Taken",
        EFlagState.PICKED => "Picked",
        EFlagState.DROPPED => "Dropped",
        EFlagState.CAPTURED => "Captured",
        EFlagState.RECOVERED => "Recovered",
        var _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    public static string ToFriendlyName(this EChatMode chatMode) => chatMode switch
    {
        EChatMode.LOCAL or EChatMode.GROUP => "Team",
        EChatMode.GLOBAL => "All",
        var _ => throw new ArgumentOutOfRangeException(nameof(chatMode), chatMode, "ChatMode is not as expected")
    };

    public static string GetDefaultAttachmentImage(string attachmentType) => attachmentType.ToLower() switch
    {
        "sights" => "https://cdn.discordapp.com/attachments/458038940847439903/957681666875347044/sight.png",
        "grip" => "https://cdn.discordapp.com/attachments/458038940847439903/957681668494356580/grip.png",
        "barrel" => "https://cdn.discordapp.com/attachments/458038940847439903/957681668276232213/barrel.png",
        "magazine" => "https://cdn.discordapp.com/attachments/458038940847439903/957681667101835305/ammo.png",
        "charm" => "https://cdn.discordapp.com/attachments/458038940847439903/957681668053958656/charm.png",
        "skin" => "https://cdn.discordapp.com/attachments/1016553641861202001/1037457447838818304/SkinsDefault.png",
        var _ => ""
    };

    public static string ToColor(this object value, bool isPlayer) => isPlayer ? $"<color={Plugin.Instance.Config.Base.FileData.PlayerColorHexCode}>{value}</color>" : value.ToString();

    public static int ServerNameSort(Server a, Server b)
    {
        var indexA = ServerDenominations.IndexOf(a.FriendlyIP.Substring(0, 2));
        var indexB = ServerDenominations.IndexOf(b.FriendlyIP.Substring(0, 2));

        return indexA.CompareTo(indexB);
    }

    private static readonly List<string> ServerDenominations = new() { "eu", "na", "oce", "as" };
    
    public static string GetRarityColor(ERarity rarity) => rarity switch
    {
        ERarity.COMMON => "#FFFFFF",
        ERarity.UNCOMMON => "#1F871F",
        ERarity.RARE => "#4B64C8",
        ERarity.EPIC => "#964BFA",
        ERarity.LEGENDARY => "#C832FA",
        ERarity.MYTHICAL => "#FA3219",
        ERarity.YELLOW => "#FFFF00",
        ERarity.CYAN => "#31FFF9",
        ERarity.GREEN => "#00FF00",
        var _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Rarity is not as expected")
    };

    public static string GetDiscordColorCode(ERarity rarity) => rarity switch
    {
        ERarity.COMMON => "16777215",
        ERarity.UNCOMMON => "2066207",
        ERarity.RARE => "4941000",
        ERarity.EPIC => "9849850",
        ERarity.LEGENDARY => "13120250",
        ERarity.MYTHICAL => "16396825",
        ERarity.YELLOW => "16776960",
        ERarity.CYAN => "3276793",
        ERarity.GREEN => "65280",
        var _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Rarity is not as expected")
    };

    public static string ToUIName(this EStat stat) => stat switch
    {
        EStat.RANGE => "Range",
        EStat.DAMAGE => "Damage",
        EStat.FIRE_RATE => "Fire Rate",
        EStat.MOBILITY =>  "Mobility",
        EStat.RELOAD_SPEED => "Reload",
        EStat.RECOIL_CONTROL => "Recoil",
        EStat.HIPFIRE_ACCURACY => "Accuracy",
        EStat.AMMO => "Ammo",
        var _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, "EStat not as expected")
    };

    public static int GetMaxAmount(this EStat stat) => 100;

    public static string ToFriendlyName(this ERarity rarity) => rarity switch
    {
        ERarity.COMMON => "Common",
        ERarity.UNCOMMON => "Uncommon",
        ERarity.RARE => "Rare",
        ERarity.EPIC => "Epic",
        ERarity.LEGENDARY => "Legendary",
        ERarity.MYTHICAL => "Mythical",
        ERarity.YELLOW => "Special",
        ERarity.CYAN => "Limited",
        ERarity.GREEN => "Special",
        var _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Rarity is not as expected")
    };
    
    public static string GetCurrencySymbol(ECurrency currency) => currency switch
    {
        ECurrency.COIN => "",
        ECurrency.SCRAP => "",
        ECurrency.CREDIT => "",
        var _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Currency is not as expected")
    };
    
    public static string GetFlag(string country) => Plugin.Instance.Config.Icons.FileData.FlagAPILink.Replace("{country}", country.ToLower());

    private static readonly List<uint> UsedFrequencies = new();
}