using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;

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

        public void TryCalculateReward(Case @case, UnturnedPlayer player)
        {
            Logging.Debug($"Calculating reward for case with id {@case.CaseID} for {player.CharacterName}");
            var rewardRarity = CalculateRewardRarity(@case.Weights);
            Logging.Debug($"Rarity found: {rewardRarity}");

            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error getting loadout for player with steam id {player.CSteamID}");
                return;
            }

            switch (rewardRarity)
            {
                case ECaseRarity.KNIFE:
                    {
                        Logging.Debug($"{player.CharacterName} reward rarity is KNIFE");
                        var knivesAvailable = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount == 0 && (!loadout.Knives.TryGetValue(k.KnifeID, out LoadoutKnife knife) || !knife.IsBought)).ToList();
                        Logging.Debug($"Found {knivesAvailable.Count} knives available to be unboxed by the player");
                        Knife knife = null;
                        if (knivesAvailable.Count > 0)
                        {
                            var newKnives = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount == 0).ToList();
                            knife = newKnives[UnityEngine.Random.Range(0, newKnives.Count)];
                        }
                        else
                        {
                            knife = CalculateKnife(knivesAvailable);
                        }

                        break;
                    }
                case ECaseRarity.GLOVE:
                    {
                        Logging.Debug($"{player.CharacterName} reward rarity is GLOVE");
                        break;
                    }
                default:
                    {
                        Logging.Debug($"{player.CharacterName} reward rarity is GUN SKIN with rarity {rewardRarity}");
                        break;
                    }
            }

            return;
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

            return false;
        }

        private Knife CalculateKnife(List<Knife> knives)
        {
            Logging.Debug($"Calculating random knife, found {knives.Count} weights to look from");
            int poolSize = 0;
            foreach (var knife in knives) poolSize += knife.KnifeWeight;
            int randInt = UnityEngine.Random.Range(0, poolSize) + 1;

            Logging.Debug($"Total Poolsize: {poolSize}, random int: {randInt}");
            int accumulatedProbability = 0;
            for (int i = 0; i < knives.Count; i++)
            {
                var knife = knives[i];
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
            foreach (var weight in weights) poolSize += weight.Item2;
            int randInt = UnityEngine.Random.Range(0, poolSize) + 1;

            Logging.Debug($"Total Poolsize: {poolSize}, random int: {randInt}");
            int accumulatedProbability = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                var weight = weights[i];
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
