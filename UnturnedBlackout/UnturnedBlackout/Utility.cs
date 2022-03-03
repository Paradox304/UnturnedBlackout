using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using Logger = Rocket.Core.Logging.Logger;

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
            string newString = "";
            bool omit = false;

            foreach (var c in value)
            {
                if (c == '<')
                {
                    omit = true;
                    continue;
                }
                else if (c == '>')
                {
                    omit = false;
                    continue;
                }

                if (omit)
                {
                    continue;
                }

                newString += c;
            }

            return newString.Trim();
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

        public static void Debug(string message)
        {
            if (Plugin.Instance.Configuration.Instance.EnableDebugLogs == true)
            {
                Logger.Log($"[DEBUG] {message}");
            }
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

            Plugin.Instance.HUDManager.ClearGunUI(inv.player);
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
                return new List<int>();

            return readerText.Split(',').Select(k => int.TryParse(k, out var id) ? id : -1).Where(k => k != -1).ToList();
        }

        public static Dictionary<ushort, LoadoutAttachment> GetAttachmentsFromString(string text)
        {
            var attachments = new Dictionary<ushort, LoadoutAttachment>();
            var attachmentsText = text.Split(',');
            foreach (var attachmentText in attachmentsText)
            {
                var isBought = false;
                ushort attachmentID = 0;
                if (attachmentText.Contains("B."))
                {
                    isBought = true;
                    if (!ushort.TryParse(attachmentText.Replace("B.", ""), out attachmentID)) continue;
                }
                else if (attachmentsText.Contains("UB."))
                {
                    isBought = false;
                    if (!ushort.TryParse(attachmentText.Replace("UB.", ""), out attachmentID)) continue;
                }
                if (!Plugin.Instance.DBManager.GunAttachments.TryGetValue(attachmentID, out GunAttachment gunAttachment))
                {
                    if (!attachments.ContainsKey(gunAttachment.AttachmentID))
                    {
                        attachments.Add(gunAttachment.AttachmentID, new LoadoutAttachment(gunAttachment, isBought));
                    }
                }
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
            return text.Remove(text.Length - 1, 1);
        }

        public static string CreateStringFromDefaultAttachments(List<GunAttachment> gunAttachments)
        {
            var text = "";
            foreach (var attachment in gunAttachments)
            {
                text += $"B.{attachment.AttachmentID},";
            }
            return text.Remove(text.Length - 1, 1);
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

        public static string ToColor(this object value, bool isPlayer)
        {
            return isPlayer ? $"<color={Plugin.Instance.Configuration.Instance.PlayerColorHexCode}>{value}</color>" : value.ToString();
        }

        public static List<uint> UsedFrequencies = new List<uint>();
    }
}
