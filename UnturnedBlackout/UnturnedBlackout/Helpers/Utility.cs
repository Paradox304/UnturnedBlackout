using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout
{
    public static class Utility
    {

        public static string ToRich(this string value)
        {
            return value.Replace('[', '<').Replace(']', '>').Replace("osqb", "[").Replace("csqb", "]");
        }

        public static string ToUnrich(this string value)
        {
            return new Regex(@"<[^>]*>", RegexOptions.IgnoreCase).Replace(value, "").Trim();
        }

        public static void Say(IRocketPlayer target, string message)
        {
            if (target is UnturnedPlayer player)
            {
                ChatManager.serverSendMessage(message, Color.green, toPlayer: player.SteamPlayer(),
                    useRichTextFormatting: true);
            }
        }

        public static void Announce(string message)
        {
            ChatManager.serverSendMessage(message, Color.green, useRichTextFormatting: true);
        }

        public static void ClearInventory(this PlayerInventory inv)
        {
            inv.player.equipment.sendSlot(0);
            inv.player.equipment.sendSlot(1);

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                for (int index = inv.getItemCount(page) - 1; index >= 0; index--)
                {
                    inv.removeItem(page, (byte)index);
                }
            }

            inv.player.clothing.updateClothes(0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0], 0, 0, new byte[0]);
            inv.player.equipment.sendSlot(0);
            inv.player.equipment.sendSlot(1);

            Plugin.Instance.UI.RemoveGunUI(inv.player.channel.GetOwnerTransportConnection());
        }

        public static void Stop(this Coroutine cr)
        {
            if (cr != null)
            {
                Plugin.Instance.StopCoroutine(cr);
            }
        }

        public static string GetOrdinal(int index) =>
             index switch
             {
                 1 => "1st",
                 2 => "2nd",
                 3 => "3rd",
                 _ => index + "th",
             };

        public static string GetDiscordEmoji(int index) =>
             index switch
             {
                 1 => ":first_place:",
                 2 => ":second_place:",
                 3 => ":third_place:",
                 _ => ":military_medal:",
             };

        public static uint GetFreeFrequency()
        {
            while (true)
            {
                uint freq = (uint)UnityEngine.Random.Range(300000, 900000);
                if (!UsedFrequencies.Contains(freq) && freq != 460327)
                {
                    UsedFrequencies.Add(freq);
                    return freq;
                }
            }
        }

        public static List<int> GetIntListFromReaderResult(this object readerResult)
        {
            string readerText = readerResult.ToString();
            if (readerText == "")
            {
                return new List<int>();
            }

            return readerText.Split(',').Select(k => int.TryParse(k, out int id) ? id : -1).Where(k => k != -1).ToList();
        }

        public static HashSet<int> GetHashSetIntFromReaderResult(this object readerResult)
        {
            string readerText = readerResult.ToString();
            if (readerText == "")
            {
                return new HashSet<int>();
            }

            return readerText.Split(',').Select(k => int.TryParse(k, out int id) ? id : -1).Where(k => k != -1).ToHashSet();
        }

        public static string GetStringFromIntList(this List<int> listInt)
        {
            string text = "";
            foreach (int id in listInt)
            {
                text += $"{id},";
            }
            return text;
        }

        public static string GetStringFromHashSetInt(this HashSet<int> hashsetInt)
        {
            string text = "";
            foreach (int id in hashsetInt)
            {
                text += $"{id},";
            }
            return text;
        }

        public static Dictionary<ushort, LoadoutAttachment> GetAttachmentsFromString(string text, Gun gun, UnturnedPlayer player)
        {
            Dictionary<ushort, LoadoutAttachment> attachments = new();
            string[] attachmentsText = text.Split(',');
            Regex numberRegex = new("([0-9]+)");

            foreach (string attachmentText in attachmentsText)
            {
                if (string.IsNullOrEmpty(attachmentText))
                {
                    continue;
                }

                bool isBought = attachmentText.Contains("B.");
                bool isUnlocked = attachmentText.Contains("UL.");
                string numberRegexMatch = numberRegex.Match(attachmentText).Value;

                if (!ushort.TryParse(numberRegexMatch, out ushort attachmentID))
                {
                    Logging.Debug($"Attachment text match with {numberRegexMatch} not found ID");
                    continue;
                }

                if (!Plugin.Instance.DB.GunAttachments.TryGetValue(attachmentID, out GunAttachment gunAttachment))
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
                {
                    levelRequired = 0;
                }
                else if (!gun.RewardAttachmentsInverse.TryGetValue(gunAttachment, out levelRequired))
                {
                    Logging.Debug($"Gun with name {gun.GunName} has an attachment with name {gunAttachment.AttachmentName} which is not in the default attachments list for the gun or reward attachments list for {player.CharacterName}");
                    continue;
                }
                attachments.Add(gunAttachment.AttachmentID, new LoadoutAttachment(gunAttachment, levelRequired, isBought, isUnlocked));
            }
            return attachments;
        }

        public static string GetStringFromAttachments(List<LoadoutAttachment> attachments)
        {
            string text = "";
            foreach (LoadoutAttachment attachment in attachments)
            {
                text += $"{(attachment.IsBought ? "B." : "")}{(attachment.IsUnlocked ? "UL." : "")}{attachment.Attachment.AttachmentID},";
            }
            return text;
        }

        public static string CreateStringFromDefaultAttachments(List<GunAttachment> gunAttachments)
        {
            string text = "";
            foreach (GunAttachment attachment in gunAttachments)
            {
                text += $"B.{attachment.AttachmentID},";
            }
            return text;
        }

        public static string CreateStringFromRewardAttachments(List<GunAttachment> gunAttachments)
        {
            string text = "";
            foreach (GunAttachment attachment in gunAttachments)
            {
                text += $"{attachment.AttachmentID},";
            }
            return text;
        }

        public static List<Reward> GetRewardsFromString(string text)
        {
            List<Reward> rewards = new();

            foreach (string rewardText in text.Split(' '))
            {
                Reward reward = GetRewardFromString(rewardText);
                if (reward != null)
                {
                    rewards.Add(reward);
                }
            }

            return rewards;
        }

        public static Reward GetRewardFromString(string text)
        {
            Regex letterRegex = new(@"([a-zA-Z]+)");
            Regex numberRegex = new(@"([0-9.]+)");

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            if (!letterRegex.IsMatch(text) || !numberRegex.IsMatch(text))
            {
                Logging.Debug($"There isn't a text or number in the reward text");
                return null;
            }

            string letterRegexMatch = letterRegex.Match(text).Value;
            if (!Enum.TryParse(letterRegexMatch, true, out ERewardType rewardType))
            {
                Logging.Debug($"Cant find reward type with the match {letterRegexMatch}");
                return null;
            }

            string numberRegexMatch = numberRegex.Match(text).Value;
            if (string.IsNullOrEmpty(numberRegexMatch))
            {
                Logging.Debug($"Number regex match is coming empty");
                return null;
            }

            if (numberRegexMatch[0] == '.')
            {
                numberRegexMatch = numberRegexMatch.Remove(0, 1);
            }

            return new Reward(rewardType, numberRegexMatch);
        }

        public static Dictionary<int, List<Reward>> GetRankedRewardsFromString(string text)
        {
            Dictionary<int, List<Reward>> rewardsRanked = new();
            int rank = 0;

            foreach (string rewardsTxt in text.Split(','))
            {
                List<Reward> rewards = GetRewardsFromString(rewardsTxt);
                rewardsRanked.Add(rank, rewards);
                rank++;
            }

            return rewardsRanked;
        }

        public static List<PercentileReward> GetPercentileRewardsFromString(string text)
        {
            List<PercentileReward> percentileRewards = new();
            Regex percentRegex = new("([0-9]*)%");

            int lowerPercentile = 0;

            foreach (string percRewards in text.Split(','))
            {
                if (!percentRegex.IsMatch(percRewards))
                {
                    Logging.Debug("Could'nt find percentage");
                    continue;
                }

                string percentRegexMatch = percentRegex.Match(percRewards).Value;
                if (!int.TryParse(percentRegexMatch.Replace("%", ""), out int percentage))
                {
                    Logging.Debug($"Couldnt find percentage with the match {percentRegexMatch}");
                    continue;
                }

                int upperPercentile = lowerPercentile + percentage;
                string rewardsTxt = percRewards.Remove(0, percRewards.IndexOf('-') + 1);
                List<Reward> rewards = GetRewardsFromString(rewardsTxt);
                percentileRewards.Add(new PercentileReward(lowerPercentile, upperPercentile, rewards));
                lowerPercentile = upperPercentile;
            }

            return percentileRewards;
        }

        public static Dictionary<EQuestCondition, List<int>> GetQuestConditionsFromString(string text)
        {
            Dictionary<EQuestCondition, List<int>> questConditions = new();

            Regex letterRegex = new("([a-zA-Z]+)");
            Regex numberRegex = new("([0-9-]+)");

            foreach (string conditionTxt in text.Split(','))
            {
                if (string.IsNullOrEmpty(conditionTxt))
                {
                    continue;
                }

                if (!letterRegex.IsMatch(conditionTxt) || !numberRegex.IsMatch(conditionTxt))
                {
                    Logging.Debug($"There isn't a text or number in the condition text");
                    continue;
                }

                string letterRegexMatch = letterRegex.Match(conditionTxt).Value;
                if (!Enum.TryParse(letterRegexMatch, true, out EQuestCondition condition))
                {
                    Logging.Debug($"Cant find condition type with the match {letterRegexMatch}");
                    continue;
                }

                string numberRegexMatch = numberRegex.Match(conditionTxt).Value;
                if (!int.TryParse(numberRegexMatch, out int conditionValue))
                {
                    Logging.Debug($"Cant find condition value with the match {numberRegexMatch}");
                    continue;
                }

                if (!questConditions.ContainsKey(condition))
                {
                    questConditions.Add(condition, new List<int>());
                }

                questConditions[condition].Add(conditionValue);
            }

            return questConditions;
        }

        public static int GetLoadoutAmount(UnturnedPlayer player)
        {
            Models.Configuration.LoadoutConfig Config = Plugin.Instance.Config.Loadout.FileData;
            int amount = Config.DefaultLoadoutAmount;
            foreach (Models.Global.LoadoutAmount loadoutAmount in Config.LoadoutAmounts.OrderByDescending(k => k.Amount))
            {
                if (player.HasPermission(loadoutAmount.Permission))
                {
                    amount = loadoutAmount.Amount;
                    break;
                }
            }
            return amount;
        }

        public static int GetStartingPos(EAttachment attachment) =>
             attachment switch
             {
                 EAttachment.Sights => 0,
                 EAttachment.Grip => 4,
                 EAttachment.Barrel => 6,
                 EAttachment.Magazine => 8,
                 _ => -1,
             };

        public static string GetLevelColor(int level) =>
             level switch
             {
                 >= 1 and <= 18 => "#a87f49",
                 >= 19 and <= 36 => "#f7f7f7",
                 >= 37 and <= 54 => "#ffd32e",
                 >= 55 and <= 72 => "#41ffe8",
                 >= 73 and <= 90 => "#2cff35",
                 >= 91 and <= 108 => "#fd2d2d",
                 >= 109 and <= 126 => "#b04dff",
                 _ => "white"
             };

        public static string ToFriendlyName(this ELoadoutPage page) =>
             page switch
             {
                 ELoadoutPage.PrimarySkin or ELoadoutPage.SecondarySkin => "Skin",
                 ELoadoutPage.Perk1 or ELoadoutPage.Perk2 or ELoadoutPage.Perk3 => "Perk",
                 ELoadoutPage.AttachmentPrimaryBarrel or ELoadoutPage.AttachmentPrimaryCharm or ELoadoutPage.AttachmentPrimaryGrip or ELoadoutPage.AttachmentPrimaryMagazine or ELoadoutPage.AttachmentPrimarySights => page.ToString().Replace("AttachmentPrimary", ""),
                 ELoadoutPage.AttachmentSecondarySights or ELoadoutPage.AttachmentSecondaryBarrel or ELoadoutPage.AttachmentSecondaryCharm or ELoadoutPage.AttachmentSecondaryMagazine => page.ToString().Replace("AttachmentSecondary", ""),
                 _ => page.ToString(),
             };

        public static string ToFriendlyName(this EGamePhase gamePhase) =>
             gamePhase switch
             {
                 EGamePhase.WaitingForPlayers => "Waiting For Players",
                 _ => gamePhase.ToString(),
             };

        public static string ToFriendlyName(this ECurrency currency) =>
             currency switch
             {
                 ECurrency.Scrap => "Scrap",
                 ECurrency.Credits => "Blackout Points",
                 ECurrency.Coins => "Blacktags",
                 _ => throw new ArgumentOutOfRangeException("currency", "Currency is not as expected")
             };

        public static string ToFriendlyName(this EChatMode chatMode) =>
             chatMode switch
             {
                 EChatMode.LOCAL or EChatMode.GROUP => "Team",
                 EChatMode.GLOBAL => "All",
                 _ => throw new ArgumentOutOfRangeException("chatMode", "ChatMode is not as expected")
             };

        public static string GetDefaultAttachmentImage(string attachmentType) =>
             attachmentType.ToLower() switch
             {
                 "sights" => "https://cdn.discordapp.com/attachments/458038940847439903/957681666875347044/sight.png",
                 "grip" => "https://cdn.discordapp.com/attachments/458038940847439903/957681668494356580/grip.png",
                 "barrel" => "https://cdn.discordapp.com/attachments/458038940847439903/957681668276232213/barrel.png",
                 "magazine" => "https://cdn.discordapp.com/attachments/458038940847439903/957681667101835305/ammo.png",
                 "charm" => "https://cdn.discordapp.com/attachments/458038940847439903/957681668053958656/charm.png",
                 "skin" => "https://cdn.discordapp.com/attachments/458038940847439903/957681667781324810/skins.png",
                 _ => "",
             };

        public static string ToColor(this object value, bool isPlayer)
        {
            return isPlayer ? $"<color={Plugin.Instance.Config.Base.FileData.PlayerColorHexCode}>{value}</color>" : value.ToString();
        }

        public static string GetRarityColor(ERarity rarity) =>
            rarity switch
            {
                ERarity.COMMON => "#FFFFFF",
                ERarity.UNCOMMON => "#1F871F",
                ERarity.RARE => "#4B64C8",
                ERarity.EPIC => "#964BFA",
                ERarity.LEGENDARY => "#C832FA",
                ERarity.MYTHICAL => "#FA3219",
                ERarity.YELLOW => "#FFFF00",
                ERarity.ORANGE => "orange",
                ERarity.CYAN => "#31FFF9",
                ERarity.GREEN => "#00FF00",
                _ => throw new ArgumentOutOfRangeException("rarity", "Rarity is not as expected")
            };

        public static string GetCurrencySymbol(ECurrency currency) =>
            currency switch
            {
                ECurrency.Coins => "",
                ECurrency.Scrap => "",
                ECurrency.Credits => "",
                _ => throw new ArgumentOutOfRangeException("currency", "Currency is not as expected")
            };

        public static string GetFlag(string country) => $"https://www.countryflagsapi.com/png/{country}";

        public static List<uint> UsedFrequencies = new();
    }
}
