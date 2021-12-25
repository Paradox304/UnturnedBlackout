using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedLegends
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
                if (c == '[')
                {
                    omit = true;
                    continue;
                }
                else if (c == ']')
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
    }
}
