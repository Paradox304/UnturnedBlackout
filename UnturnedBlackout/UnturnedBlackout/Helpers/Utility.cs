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

            Plugin.Instance.HUDManager.RemoveGunUI(inv.player.channel.GetOwnerTransportConnection());
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

        public static string GetStringFromIntList(this List<int> listInt)
        {
            var text = "";
            foreach (var id in listInt)
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

                if (!Plugin.Instance.DBManager.GunAttachments.TryGetValue(attachmentID, out GunAttachment gunAttachment))
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

        public static Dictionary<int, List<Reward>> GetRankedRewardsFromString(string text)
        {
            Logging.Debug("Getting ranked rewards from string");
            var rewardsRanked = new Dictionary<int, List<Reward>>();

            int rank = 0;
            var letterRegex = new Regex("([a-zA-Z]*)");
            var numberRegex = new Regex(@"(\d+)");

            foreach (var rewardsTxt in text.Split(','))
            {
                Logging.Debug($"Getting rewards for rank {rank}");
                var rewards = new List<Reward>();
                foreach (var rewardTxt in rewardsTxt.Split(' '))
                {
                    if (string.IsNullOrEmpty(rewardTxt))
                    {
                        continue;
                    }

                    Logging.Debug($"Found reward with text {rewardTxt}");
                    if (!letterRegex.IsMatch(rewardTxt) || !numberRegex.IsMatch(rewardTxt))
                    {
                        Logging.Debug($"There isn't a text or number in the reward text");
                        continue;
                    }

                    var letterRegexMatch = letterRegex.Match(rewardTxt).Value;
                    if (!Enum.TryParse(letterRegexMatch, true, out ERewardType rewardType))
                    {
                        Logging.Debug($"Cant find reward type with the match {letterRegexMatch}");
                        continue;
                    }

                    var numberRegexMatch = numberRegex.Match(rewardTxt).Value;
                    if (!int.TryParse(numberRegexMatch, out int rewardValue))
                    {
                        Logging.Debug($"Cant find reward value with the match {numberRegexMatch}");
                        continue;
                    }

                    Logging.Debug($"Found reward with type {rewardType} and value {rewardValue}");
                    rewards.Add(new Reward(rewardType, rewardValue));
                }
                Logging.Debug($"Found total {rewards.Count} rewards for rank {rank}");
                rewardsRanked.Add(rank, rewards);
                rank++;
            }

            return rewardsRanked;
        }


        public static List<PercentileReward> GetPercentileRewardsFromString(string text)
        {
            Logging.Debug("Getting percentile rewards from string");
            var percentileRewards = new List<PercentileReward>();

            var letterRegex = new Regex("([a-zA-Z]*)");
            var numberRegex = new Regex(@"(\d+)");

            var percentRegex = new Regex("([0-9]*)%");

            int lowerPercentile = 0;

            foreach (var percRewards in text.Split(','))
            {
                Logging.Debug($"Getting percentage in {percRewards}");
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
                Logging.Debug($"Getting rewards from reward text {rewardsTxt}");
                var rewards = new List<Reward>();
                foreach (var rewardTxt in rewardsTxt.Split(' '))
                {
                    if (string.IsNullOrEmpty(rewardTxt))
                    {
                        continue;
                    }

                    Logging.Debug($"Found reward with text {rewardTxt}");
                    if (!letterRegex.IsMatch(rewardTxt) || !numberRegex.IsMatch(rewardTxt))
                    {
                        Logging.Debug($"There isn't a text or number in the reward text");
                        continue;
                    }

                    var letterRegexMatch = letterRegex.Match(rewardTxt).Value;
                    if (!Enum.TryParse(letterRegexMatch, true, out ERewardType rewardType))
                    {
                        Logging.Debug($"Cant find reward type with the match {letterRegexMatch}");
                        continue;
                    }

                    var numberRegexMatch = numberRegex.Match(rewardTxt).Value;
                    if (!int.TryParse(numberRegexMatch, out int rewardValue))
                    {
                        Logging.Debug($"Cant find reward value with the match {numberRegexMatch}");
                        continue;
                    }

                    Logging.Debug($"Found reward with type {rewardType} and value {rewardValue}");
                    rewards.Add(new Reward(rewardType, rewardValue));
                }
                Logging.Debug($"Found total {rewards.Count} rewards for lower percentile {lowerPercentile} and upper percentile {upperPercentile}");
                percentileRewards.Add(new PercentileReward(lowerPercentile, upperPercentile, rewards));
                lowerPercentile = upperPercentile;
            }

            return percentileRewards;
        }

        public static Dictionary<EQuestCondition, List<int>> GetQuestConditionsFromString(string text)
        {
            Logging.Debug("Getting quest conditions from string");
            var questConditions = new Dictionary<EQuestCondition, List<int>>();

            var letterRegex = new Regex("([a-zA-Z]*)");
            var numberRegex = new Regex(@"(\d+)");

            foreach (var conditionTxt in text.Split(','))
            {
                Logging.Debug($"Getting condition with text {conditionTxt}");
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

                Logging.Debug($"Found condition with type {condition} and value {conditionValue}");
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
            var amount = Plugin.Instance.Configuration.Instance.DefaultLoadoutAmount;
            foreach (var loadoutAmount in Plugin.Instance.Configuration.Instance.LoadoutAmounts.OrderByDescending(k => k.Amount))
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


        public static double CalculateSimilarityBetweenStrings(string str1, string str2)
        {
            var str1Array = str1.ToCharArray();
            var str2Array = str2.ToCharArray();
            var str1Length = str1Array.Length;
            var str2Length = str2Array.Length;
            var maxLength = Math.Max(str1Length, str2Length);
            var minLength = Math.Min(str1Length, str2Length);
            var similarity = 0.0;
            for (var i = 0; i < minLength; i++)
            {
                if (str1Array[i] == str2Array[i])
                {
                    similarity++;
                }
            }
            similarity /= maxLength;
            return similarity;
        }
        
        public static string ToColor(this object value, bool isPlayer)
        {
            return isPlayer ? $"<color={Plugin.Instance.Configuration.Instance.PlayerColorHexCode}>{value}</color>" : value.ToString();
        }

        public static List<uint> UsedFrequencies = new();
    }
}
