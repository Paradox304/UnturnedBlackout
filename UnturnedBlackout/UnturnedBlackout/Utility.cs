using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Calculates the average center of an <see cref="IEnumerable{Vector3}"/>.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{Vector3}"/> to get the average center from.</param>
        /// <returns>
        /// A <see cref="Vector3"/> that is the average center of <paramref name="source"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="source"/> is null, then this exception is thrown, as <paramref name="source"/> should never be null.
        /// </exception>
        public static Vector3 AverageCenter(this IEnumerable<Vector3> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var list = source.ToList();

            var sum = Vector3.zero;

            checked
            {
                sum = list.Aggregate(sum, (current, element) => current + element);
            }

            if (list.Count > 0)
            {
                return sum / list.Count;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Calculates the average center of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{TSource}"/> to get the average center from.</param>
        /// <param name="selector">The way that the <paramref name="source"/> should be selected to convert it to an <see cref="IEnumerable{Vector3}"/>.</param>
        /// <typeparam name="TSource">A type that can select a <see cref="Vector3"/>.</typeparam>
        /// <returns>
        /// A <see cref="Vector3"/> that is the average center of <paramref name="source"/> after applying a <paramref name="selector"/>.
        /// </returns>
        /// <remarks>
        /// This method calls <see cref="Extensions.AverageCenter"/>, which only takes an <see cref="IEnumerable{Vector3}"/>.
        /// <br/>
        /// Therefore any input for this average center should support a selector to a <see cref="Vector3"/>.
        /// </remarks>
        public static Vector3 AverageCenter<TSource>(this IEnumerable<TSource> source, Func<TSource, Vector3> selector)
        {
            return source.Select(selector).AverageCenter();
        }

        public static List<uint> UsedFrequencies = new List<uint>();
    }
}
