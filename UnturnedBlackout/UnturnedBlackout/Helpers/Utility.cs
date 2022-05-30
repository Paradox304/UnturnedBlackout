﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
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
            return new Regex("<[^>]*>", RegexOptions.IgnoreCase).Replace(value, "").Trim();
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

        public static string GetOrdinal(int index)
        {
            switch (index)
            {
                case 2:
                    return "2nd";
                case 3:
                    return "3rd";
                default:
                    return index + "th";
            }
        }

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

        public static int GetStartingPos(EAttachment attachment)
        {
            switch (attachment)
            {
                case EAttachment.Sights:
                    return 0;
                case EAttachment.Grip:
                    return 4;
                case EAttachment.Barrel:
                    return 6;
                case EAttachment.Magazine:
                    return 8;
            }
            return -1;
        }

        public static string ToFriendlyName(this ELoadoutPage page)
        {
            switch (page)
            {
                case ELoadoutPage.PrimarySkin:
                case ELoadoutPage.SecondarySkin:
                    return "Skin";
                case ELoadoutPage.Perk1:
                case ELoadoutPage.Perk2:
                case ELoadoutPage.Perk3:
                    return "Perk";
                case ELoadoutPage.AttachmentPrimaryBarrel:
                case ELoadoutPage.AttachmentPrimaryCharm:
                case ELoadoutPage.AttachmentPrimaryGrip:
                case ELoadoutPage.AttachmentPrimaryMagazine:
                case ELoadoutPage.AttachmentPrimarySights:
                    return page.ToString().Replace("AttachmentPrimary", "");
                case ELoadoutPage.AttachmentSecondarySights:
                case ELoadoutPage.AttachmentSecondaryBarrel:
                case ELoadoutPage.AttachmentSecondaryCharm:
                case ELoadoutPage.AttachmentSecondaryMagazine:
                    return page.ToString().Replace("AttachmentSecondary", "");
                default:
                    return page.ToString();
            }
        }

        public static string ToFriendlyName(this EGamePhase gamePhase)
        {
            switch (gamePhase)
            {
                case EGamePhase.WaitingForPlayers:
                    return "Waiting For Players";
                default:
                    return gamePhase.ToString();
            }
        }

        public static string GetDefaultAttachmentImage(string attachmentType)
        {
            switch (attachmentType.ToLower())
            {
                case "sights":
                    return "https://cdn.discordapp.com/attachments/458038940847439903/957681666875347044/sight.png";
                case "grip":
                    return "https://cdn.discordapp.com/attachments/458038940847439903/957681668494356580/grip.png";
                case "barrel":
                    return "https://cdn.discordapp.com/attachments/458038940847439903/957681668276232213/barrel.png";
                case "magazine":
                    return "https://cdn.discordapp.com/attachments/458038940847439903/957681667101835305/ammo.png";
                case "charm":
                    return "https://cdn.discordapp.com/attachments/458038940847439903/957681668053958656/charm.png";
                case "skin":
                    return "https://cdn.discordapp.com/attachments/458038940847439903/957681667781324810/skins.png";
                default:
                    return "";
            }
        }

        public static string ToColor(this object value, bool isPlayer)
        {
            return isPlayer ? $"<color={Plugin.Instance.Configuration.Instance.PlayerColorHexCode}>{value}</color>" : value.ToString();
        }

        public static List<uint> UsedFrequencies = new List<uint>();
    }
}
