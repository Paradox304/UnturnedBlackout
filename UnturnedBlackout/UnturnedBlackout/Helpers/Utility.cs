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

            Plugin.Instance.HUD.RemoveGunUI(inv.player.channel.GetOwnerTransportConnection());
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
            if (readerText == "")
            {
                return new List<int>();
            }

            return readerText.Split(',').Select(k => int.TryParse(k, out var id) ? id : -1).Where(k => k != -1).ToList();
        }

        public static HashSet<int> GetHashSetIntFromReaderResult(this object readerResult)
        {
            var readerText = readerResult.ToString();
            if (readerText == "")
            {
                return new HashSet<int>();
            }

            return readerText.Split(',').Select(k => int.TryParse(k, out var id) ? id : -1).Where(k => k != -1).ToHashSet();
        }

        public static string GetStringFromIntList(this List<int> listInt)
        {
            var text = "";
            foreach (var id in listInt)
            {
                text += $"{id},";
            }
            return text;
        }

        public static string GetStringFromHashSetInt(this HashSet<int> hashsetInt)
        {
            var text = "";
            foreach (var id in hashsetInt)
            {
                text += $"{id},";
            }
            return text;
        }

        public static Dictionary<ushort, LoadoutAttachment> GetAttachmentsFromString(string text, Gun gun, UnturnedPlayer player)
        {
            var attachments = new Dictionary<ushort, LoadoutAttachment>();
            var attachmentsText = text.Split(',');
            foreach (var attachmentText in attachmentsText)
            {
                if (string.IsNullOrEmpty(attachmentText))
                {
                    continue;
                }

                var isBought = false;
                ushort attachmentID = 0;
                if (attachmentText.StartsWith("B."))
                {
                    isBought = true;
                    if (!ushort.TryParse(attachmentText.Replace("B.", ""), out attachmentID))
                    {
                        continue;
                    }
                }
                else if (attachmentText.StartsWith("UB."))
                {
                    isBought = false;
                    if (!ushort.TryParse(attachmentText.Replace("UB.", ""), out attachmentID))
                    {
                        continue;
                    }
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

                var levelRequired = -1;
                if (gun.DefaultAttachments.Contains(gunAttachment))
                {
                    levelRequired = 0;
                }
                else if (!gun.RewardAttachmentsInverse.TryGetValue(gunAttachment, out levelRequired))
                {
                    Logging.Debug($"Gun with name {gun.GunName} has an attachment with name {gunAttachment.AttachmentName} which is not in the default attachments list for the gun or reward attachments list for {player.CharacterName}");
                    continue;
                }
                attachments.Add(gunAttachment.AttachmentID, new LoadoutAttachment(gunAttachment, levelRequired, isBought));
            }
            return attachments;
        }

        public static string GetStringFromAttachments(List<LoadoutAttachment> attachments)
        {
            var text = "";
            foreach (var attachment in attachments)
            {
                text += $"{(attachment.IsBought ? "B." : "UB.")}{attachment.Attachment.AttachmentID},";
            }
            return text;
        }

        public static string CreateStringFromDefaultAttachments(List<GunAttachment> gunAttachments)
        {
            var text = "";
            foreach (var attachment in gunAttachments)
            {
                text += $"B.{attachment.AttachmentID},";
            }
            return text;
        }

        public static string CreateStringFromRewardAttachments(List<GunAttachment> gunAttachments)
        {
            var text = "";
            foreach (var attachment in gunAttachments)
            {
                text += $"UB.{attachment.AttachmentID},";
            }
            return text;
        }

        public static List<Reward> GetRewardsFromString(string text)
        {
            var rewards = new List<Reward>();

            foreach (var rewardText in text.Split(' '))
            {
                var reward = GetRewardFromString(rewardText);
                if (reward != null)
                {
                    rewards.Add(reward);
                }
            }

            return rewards;
        }

        public static Reward GetRewardFromString(string text)
        {
            var letterRegex = new Regex(@"([a-zA-Z]+)");
            var numberRegex = new Regex(@"([0-9.]+)");

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            if (!letterRegex.IsMatch(text) || !numberRegex.IsMatch(text))
            {
                Logging.Debug($"There isn't a text or number in the reward text");
                return null;
            }

            var letterRegexMatch = letterRegex.Match(text).Value;
            if (!Enum.TryParse(letterRegexMatch, true, out ERewardType rewardType))
            {
                Logging.Debug($"Cant find reward type with the match {letterRegexMatch}");
                return null;
            }

            var numberRegexMatch = numberRegex.Match(text).Value;
            if (string.IsNullOrEmpty(numberRegexMatch))
            {
                Logging.Debug($"Number regex match is coming empty");
                return null;
            }

            if (numberRegexMatch[0] == '.')
            {
                numberRegexMatch = numberRegexMatch.Remove(0, 1);
            }

            Logging.Debug($"Reward Type: {rewardType}, Reward Value: {numberRegexMatch}");
            return new Reward(rewardType, numberRegexMatch);
        }

        public static Dictionary<int, List<Reward>> GetRankedRewardsFromString(string text)
        {
            var rewardsRanked = new Dictionary<int, List<Reward>>();
            int rank = 0;

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
            var percentileRewards = new List<PercentileReward>();
            var percentRegex = new Regex("([0-9]*)%");

            int lowerPercentile = 0;

            foreach (var percRewards in text.Split(','))
            {
                if (!percentRegex.IsMatch(percRewards))
                {
                    Logging.Debug("Could'nt find percentage");
                    continue;
                }

                var percentRegexMatch = percentRegex.Match(percRewards).Value;
                if (!int.TryParse(percentRegexMatch.Replace("%", ""), out int percentage))
                {
                    Logging.Debug($"Couldnt find percentage with the match {percentRegexMatch}");
                    continue;
                }

                var upperPercentile = lowerPercentile + percentage;
                var rewardsTxt = percRewards.Remove(0, percRewards.IndexOf('-') + 1);
                var rewards = GetRewardsFromString(rewardsTxt);
                percentileRewards.Add(new PercentileReward(lowerPercentile, upperPercentile, rewards));
                lowerPercentile = upperPercentile;
            }

            return percentileRewards;
        }

        public static Dictionary<EQuestCondition, List<int>> GetQuestConditionsFromString(string text)
        {
            var questConditions = new Dictionary<EQuestCondition, List<int>>();

            var letterRegex = new Regex("([a-zA-Z]+)");
            var numberRegex = new Regex("([0-9-]+)");

            foreach (var conditionTxt in text.Split(','))
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

                var letterRegexMatch = letterRegex.Match(conditionTxt).Value;
                if (!Enum.TryParse(letterRegexMatch, true, out EQuestCondition condition))
                {
                    Logging.Debug($"Cant find condition type with the match {letterRegexMatch}");
                    continue;
                }

                var numberRegexMatch = numberRegex.Match(conditionTxt).Value;
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
            var Config = Plugin.Instance.Config.Loadout.FileData;
            var amount = Config.DefaultLoadoutAmount;
            foreach (var loadoutAmount in Config.LoadoutAmounts.OrderByDescending(k => k.Amount))
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
                ERarity.YELLOW => "yellow",
                ERarity.ORANGE => "orange",
                ERarity.CYAN => "#31FFF9",
                ERarity.GREEN => "green",
                _ => throw new ArgumentOutOfRangeException("rarity", "Rarity is not as expected")
            };

        public static string GetCurrencySymbol(ECurrency currency) =>
            currency switch
            {
                ECurrency.Coin => "",
                ECurrency.Scrap => "",
                ECurrency.Credit => "",
                _ => throw new ArgumentOutOfRangeException("currency", "Currency is not as expected")
            };

        public static List<uint> UsedFrequencies = new();
    }
}
