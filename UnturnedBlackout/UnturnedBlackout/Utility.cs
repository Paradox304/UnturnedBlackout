using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;
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
                if (c == '[' || c == '<')
                {
                    omit = true;
                    continue;
                }
                else if (c == ']' || c == '>')
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

            return newString;
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

        public static string ToColor(this object value, bool isPlayer)
        {
            return isPlayer ? $"<color={Plugin.Instance.Configuration.Instance.PlayerColorHexCode}>{value}</color>" : value.ToString();
        }

        public static List<uint> UsedFrequencies = new List<uint>();
    }
}
