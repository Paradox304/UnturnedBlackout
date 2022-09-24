﻿using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Managers
{
    public class UnboxManager
    {
        public DatabaseManager DB
        {
            get
            {
                return Plugin.Instance.DB;
            }
        }

        public bool TryCalculateReward(Case @case, UnturnedPlayer player, out Reward reward, out string rewardImage, out string rewardName, out string rewardDesc, out ERarity rewardRarity, out bool isDuplicate, out int duplicateScrapAmount)
        {
            reward = null;
            rewardImage = "";
            rewardName = "";
            rewardDesc = "";
            rewardRarity = ERarity.NONE;
            isDuplicate = false;
            duplicateScrapAmount = 0;

            Logging.Debug($"Calculating reward for case with id {@case.CaseID} for {player.CharacterName}");
            ECaseRarity caseRarity = CalculateRewardRarity(@case.Weights);
            Logging.Debug($"Rarity found: {caseRarity}");

            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error getting loadout for player with steam id {player.CSteamID}");
                return false;
            }

            switch (caseRarity)
            {
                case ECaseRarity.KNIFE or ECaseRarity.LIMITED_KNIFE:
                    {
                        bool isLimited = caseRarity == ECaseRarity.LIMITED_KNIFE;
                        Logging.Debug($"{player.CharacterName} reward rarity is KNIFE, IsLimited {isLimited}");
                        List<Knife> knivesAvailable = new();
                        if (isLimited)
                        {
                            Logging.Debug($"{player.CharacterName} has gotten a limited knife rarity, checking if there are any limited knives available");
                            knivesAvailable = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount > 0 && k.MaxAmount > k.UnboxedAmount && (!loadout.Knives.TryGetValue(k.KnifeID, out LoadoutKnife knife) || !knife.IsBought)).ToList();
                            if (knivesAvailable.Count == 0)
                            {
                                Logging.Debug("There are no limited knives available, getting knives that are not owned by the player");
                                knivesAvailable = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount == 0 && (!loadout.Knives.TryGetValue(k.KnifeID, out LoadoutKnife knife) || !knife.IsBought)).ToList();
                            }
                        }
                        else
                        {
                            knivesAvailable = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount == 0 && (!loadout.Knives.TryGetValue(k.KnifeID, out LoadoutKnife knife) || !knife.IsBought)).ToList();
                        }

                        Logging.Debug($"Found {knivesAvailable.Count} knives available to be unboxed by the player");
                        Knife knife = null;
                        if (knivesAvailable.Count == 0)
                        {
                            Logging.Debug($"There are no knives available to calculate with, sending a random one to player");
                            List<Knife> newKnives = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount == 0).ToList();
                            knife = newKnives[UnityEngine.Random.Range(0, newKnives.Count)];

                            isDuplicate = true;
                            duplicateScrapAmount = knife.ScrapAmount;
                        }
                        else
                        {
                            knife = CalculateKnife(knivesAvailable);

                            // Send unboxed amount +1
                            Task.Run(async () => await DB.UpdatePlayerKnifeUnboxedAmountAsync(knife.KnifeID, 1));
                        }

                        rewardName = knife.KnifeName;
                        rewardImage = knife.IconLink;
                        rewardDesc = knife.KnifeDesc;
                        rewardRarity = knife.KnifeRarity;
                        reward = new Reward(ERewardType.Knife, knife.KnifeID);

                        return true;
                    }
                case ECaseRarity.GLOVE or ECaseRarity.LIMITED_GLOVE:
                    {
                        bool isLimited = caseRarity == ECaseRarity.LIMITED_GLOVE;
                        Logging.Debug($"{player.CharacterName} reward rarity is GLOVE, IsLimited {isLimited}");
                        List<Glove> glovesAvailable = new();
                        if (isLimited)
                        {
                            Logging.Debug($"{player.CharacterName} has gotten a limited glove rarity, checking if there are any limited gloves available");
                            glovesAvailable = DB.Gloves.Values.Where(k => k.GloveWeight > 0 && k.MaxAmount > 0 && k.MaxAmount > k.UnboxedAmount && (!loadout.Gloves.TryGetValue(k.GloveID, out LoadoutGlove glove) || !glove.IsBought)).ToList();
                            if (glovesAvailable.Count == 0)
                            {
                                Logging.Debug("There are no limited gloves available, getting gloves that are not owned by the player");
                                glovesAvailable = DB.Gloves.Values.Where(k => k.GloveWeight > 0 && k.MaxAmount == 0 && (!loadout.Gloves.TryGetValue(k.GloveID, out LoadoutGlove glove) || !glove.IsBought)).ToList();
                            }
                        }
                        else
                        {
                            glovesAvailable = DB.Gloves.Values.Where(k => k.GloveWeight > 0 && k.MaxAmount == 0 && (!loadout.Gloves.TryGetValue(k.GloveID, out LoadoutGlove glove) || !glove.IsBought)).ToList();
                        }

                        Logging.Debug($"Found {glovesAvailable.Count} gloves available to be unboxed by the player");
                        Glove glove = null;
                        if (glovesAvailable.Count == 0)
                        {
                            Logging.Debug($"There are no gloves available to calculate with, sending a random one to player");
                            List<Glove> newGloves = DB.Gloves.Values.Where(k => k.GloveWeight > 0 && k.MaxAmount == 0).ToList();
                            glove = newGloves[UnityEngine.Random.Range(0, newGloves.Count)];

                            isDuplicate = true;
                            duplicateScrapAmount = glove.ScrapAmount;
                        }
                        else
                        {
                            glove = CalculateGlove(glovesAvailable);

                            // Send updated unbox amount +1
                            Task.Run(async () => await DB.UpdatePlayerGloveUnboxedAmountAsync(glove.GloveID, 1));
                        }

                        rewardName = glove.GloveName;
                        rewardImage = glove.IconLink;
                        rewardDesc = glove.GloveDesc;
                        rewardRarity = glove.GloveRarity;
                        reward = new Reward(ERewardType.Glove, glove.GloveID);

                        return true;
                    }
                default:
                    {
                        Logging.Debug($"{player.CharacterName} reward rarity is GUN SKIN with rarity {caseRarity}");
                        if (!Enum.TryParse(caseRarity.ToString(), true, out ERarity skinRarity))
                        {
                            Logging.Debug($"Error parsing {caseRarity} to a specified skin rarity");
                            return false;
                        }

                        List<GunSkin> skinsAvailable = @case.AvailableSkinsSearchByRarity[skinRarity].Where(k => k.MaxAmount == 0 || k.MaxAmount > k.UnboxedAmount).ToList();
                        Logging.Debug($"Found {skinsAvailable.Count} skins available to got by {player.CharacterName} for rarity {skinRarity}");
                        if (skinsAvailable.Count == 0)
                        {
                            Logging.Debug($"Found no skins available for the {player.CharacterName} to get for rarity {skinRarity}");
                            return false;
                        }

                        GunSkin skin = skinsAvailable[UnityEngine.Random.Range(0, skinsAvailable.Count)];
                        if (loadout.GunSkinsSearchByID.ContainsKey(skin.ID))
                        {
                            isDuplicate = true;
                            duplicateScrapAmount = skin.ScrapAmount;
                        }
                        else
                        {
                            // Send unboxed amount by +1
                            Task.Run(async () => await DB.UpdatePlayerGunSkinUnboxedAmountAsync(skin.ID, 1));
                        }

                        rewardName = skin.Gun.GunName + " | " + skin.SkinName;
                        rewardImage = skin.IconLink;
                        rewardDesc = skin.SkinDesc;
                        rewardRarity = skin.SkinRarity;
                        reward = new Reward(ERewardType.GunSkin, skin.ID);

                        return true;
                    }
            }
        }

        private Glove CalculateGlove(List<Glove> gloves)
        {
            Logging.Debug($"Calculating random glove, found {gloves.Count} weights to look from");
            int poolSize = 0;
            foreach (Glove glove in gloves) poolSize += glove.GloveWeight;
            int randInt = UnityEngine.Random.Range(0, poolSize) + 1;

            Logging.Debug($"Total Poolsize: {poolSize}, random int: {randInt}");
            int accumulatedProbability = 0;
            for (int i = 0; i < gloves.Count; i++)
            {
                Glove glove = gloves[i];
                Logging.Debug($"i: {i}, glove: {glove.GloveID}, weight: {glove.GloveWeight}");
                accumulatedProbability += glove.GloveWeight;
                Logging.Debug($"accumulated probability: {accumulatedProbability}, rand int: {randInt}");
                if (randInt <= accumulatedProbability)
                    return glove;
            }
            Logging.Debug($"Random rarity not found, sending a random rarity");
            return gloves[UnityEngine.Random.Range(0, gloves.Count)];
        }

        private Knife CalculateKnife(List<Knife> knives)
        {
            Logging.Debug($"Calculating random knife, found {knives.Count} weights to look from");
            int poolSize = 0;
            foreach (Knife knife in knives) poolSize += knife.KnifeWeight;
            int randInt = UnityEngine.Random.Range(0, poolSize) + 1;

            Logging.Debug($"Total Poolsize: {poolSize}, random int: {randInt}");
            int accumulatedProbability = 0;
            for (int i = 0; i < knives.Count; i++)
            {
                Knife knife = knives[i];
                Logging.Debug($"i: {i}, knife: {knife.KnifeID}, weight: {knife.KnifeWeight}");
                accumulatedProbability += knife.KnifeWeight;
                Logging.Debug($"accumulated probability: {accumulatedProbability}, rand int: {randInt}");
                if (randInt <= accumulatedProbability)
                    return knife;
            }
            Logging.Debug($"Random rarity not found, sending a random rarity");
            return knives[UnityEngine.Random.Range(0, knives.Count)];
        }

        private ECaseRarity CalculateRewardRarity(List<(ECaseRarity, int)> weights)
        {
            Logging.Debug($"Calculating reward rarities, found {weights.Count} weights to look from");
            int poolSize = 0;
            foreach ((ECaseRarity, int) weight in weights) poolSize += weight.Item2;
            int randInt = UnityEngine.Random.Range(0, poolSize) + 1;

            Logging.Debug($"Total Poolsize: {poolSize}, random int: {randInt}");
            int accumulatedProbability = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                (ECaseRarity, int) weight = weights[i];
                Logging.Debug($"i: {i}, rarity: {weight.Item1}, weight: {weight.Item2}");
                accumulatedProbability += weight.Item2;
                Logging.Debug($"accumulated probability: {accumulatedProbability}, rand int: {randInt}");
                if (randInt <= accumulatedProbability)
                    return weight.Item1;
            }
            Logging.Debug($"Random rarity not found, sending a random rarity");
            return weights[UnityEngine.Random.Range(0, weights.Count)].Item1;
        }
    }
}
