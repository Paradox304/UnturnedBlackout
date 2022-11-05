using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Managers;

public class UnboxManager
{
    public DatabaseManager DB => Plugin.Instance.DB;

    public bool TryCalculateReward(Case @case, UnturnedPlayer player, out Reward reward, out string rewardImage, out string rewardName, out string rewardDesc, out ERarity rewardRarity, out bool isDuplicate, out int duplicateScrapAmount, out ECaseRarity caseRarity, out List<(ECaseRarity, int)> updatedWeights)
    {
        reward = null;
        rewardImage = "";
        rewardName = "";
        rewardDesc = "";
        rewardRarity = ERarity.NONE;
        isDuplicate = false;
        duplicateScrapAmount = 0;
        caseRarity = ECaseRarity.EPIC;
        updatedWeights = @case.Weights.ToList();
        
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error getting loadout for player with steam id {player.CSteamID}");
            return false;
        }
        
        var specialRarities = new Dictionary<ECaseRarity, List<object>>();
        foreach (var weight in updatedWeights.ToList())
        {
            switch (weight.Item1)
            {
                case ECaseRarity.KNIFE:
                {
                    var knivesAvailable = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount == 0 && (!loadout.Knives.TryGetValue(k.KnifeID, out var knife) || !knife.IsBought)).ToList();
                    if (knivesAvailable.Count == 0)
                    {
                        updatedWeights.Remove(weight);
                        break;
                    }
                    
                    specialRarities.Add(ECaseRarity.KNIFE, knivesAvailable.Cast<object>().ToList());                                                                                            
                    break;
                }
                case ECaseRarity.LIMITED_KNIFE:
                {
                    var limitedKnivesAvailable = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount > 0 && k.MaxAmount > k.UnboxedAmount && (!loadout.Knives.TryGetValue(k.KnifeID, out var knife) || !knife.IsBought)).ToList();
                    if (limitedKnivesAvailable.Count == 0)
                    {
                        updatedWeights.Remove(weight);
                        break;
                    }
                    
                    specialRarities.Add(ECaseRarity.LIMITED_KNIFE, limitedKnivesAvailable.Cast<object>().ToList());
                    break;
                }
                case ECaseRarity.GLOVE:
                {
                    var glovesAvailable = DB.Gloves.Values.Where(g => g.GloveWeight > 0 && g.MaxAmount == 0 && (!loadout.Gloves.TryGetValue(g.GloveID, out var glove) || !glove.IsBought)).ToList();
                    if (glovesAvailable.Count == 0)
                    {
                        updatedWeights.Remove(weight);
                        break;
                    }
                    
                    specialRarities.Add(ECaseRarity.GLOVE, glovesAvailable.Cast<object>().ToList());
                    break;
                }
                case ECaseRarity.LIMITED_GLOVE:
                {
                    var limitedGlovesAvailable = DB.Gloves.Values.Where(g => g.GloveWeight > 0 && g.MaxAmount > 0 && g.MaxAmount > g.UnboxedAmount && (!loadout.Gloves.TryGetValue(g.GloveID, out var glove) || !glove.IsBought)).ToList();
                    if (limitedGlovesAvailable.Count == 0)
                    {
                        updatedWeights.Remove(weight);
                        break;
                    }
                    
                    specialRarities.Add(ECaseRarity.LIMITED_GLOVE, limitedGlovesAvailable.Cast<object>().ToList());
                    break;
                }
                case ECaseRarity.LIMITED_SKIN:
                {
                    var limitedSkinsAvailable = DB.GunSkinsSearchByID.Values.Where(k => k.MaxAmount > 0 && k.MaxAmount > k.UnboxedAmount && !loadout.GunSkinsSearchByID.ContainsKey(k.ID)).ToList();
                    if (limitedSkinsAvailable.Count == 0)
                    {
                        updatedWeights.Remove(weight);
                        break;
                    }
                    
                    specialRarities.Add(ECaseRarity.LIMITED_SKIN, limitedSkinsAvailable.Cast<object>().ToList());
                    break;
                }
                case ECaseRarity.SPECIAL_SKIN:
                {
                    var specialSkinsAvailable = DB.GunSkinsSearchByID.Values.Where(k => k.SkinRarity == ERarity.YELLOW && !loadout.GunSkinsSearchByID.ContainsKey(k.ID)).ToList();
                    if (specialSkinsAvailable.Count == 0)
                    {
                        updatedWeights.Remove(weight);
                        break;
                    }
                    
                    specialRarities.Add(ECaseRarity.SPECIAL_SKIN, specialSkinsAvailable.Cast<object>().ToList());
                    break;
                }
            }
        }
        
        caseRarity = CalculateRewardRarity(updatedWeights);
        switch (caseRarity)
        {
            case ECaseRarity.KNIFE or ECaseRarity.LIMITED_KNIFE:
            {
                if (!specialRarities.TryGetValue(caseRarity, out var knives))
                {
                    Logging.Debug("Could'nt find any special rarities with this case");
                    return false;
                }

                var knivesAvailable = knives.Cast<Knife>().ToList();
                if (knivesAvailable.Count == 0)
                {
                    Logging.Debug("No knives available for the player");
                    return false;
                }
                
                var knife = CalculateKnife(knivesAvailable);
                if (knife == null)
                {
                    Logging.Debug("Could'nt calculate any knife, returning");
                    return false;
                }
                
                Logging.Debug($"Knife calculated: {knife.KnifeName}, case: {@case.CaseName}, player: {player.CharacterName}");
                
                rewardName = knife.KnifeName;
                rewardImage = knife.IconLink;
                rewardDesc = knife.KnifeDesc;
                rewardRarity = knife.KnifeRarity;
                reward = new(ERewardType.KNIFE, knife.KnifeID);
                
                return true;
            }
            case ECaseRarity.GLOVE or ECaseRarity.LIMITED_GLOVE:
            {
                if (!specialRarities.TryGetValue(caseRarity, out var gloves))
                {
                    Logging.Debug("Couldn't find any special rarities with this case");
                    return false;
                }
                
                var glovesAvailable = gloves.Cast<Glove>().ToList();
                if (glovesAvailable.Count == 0)
                {
                    Logging.Debug("No gloves available for player");
                    return false;
                }
                
                var glove = CalculateGlove(glovesAvailable);
                if (glove == null)
                {
                    Logging.Debug("Could'nt calculate any glove, returning");
                    return false;
                }

                rewardName = glove.GloveName;
                rewardImage = glove.IconLink;
                rewardDesc = glove.GloveDesc;
                rewardRarity = glove.GloveRarity;
                reward = new(ERewardType.GLOVE, glove.GloveID);

                return true;
            }
            case ECaseRarity.LIMITED_SKIN or ECaseRarity.SPECIAL_SKIN:
            {
                if (!specialRarities.TryGetValue(caseRarity, out var skins))
                {
                    Logging.Debug("Couldn't find any special rarities with this case");
                    return false;
                }
                
                var skinsAvailable = skins.Cast<GunSkin>().ToList();
                if (skinsAvailable.Count == 0)
                {
                    Logging.Debug("No skins available for the player");
                    return false;
                }
                
                var skin = skinsAvailable[UnityEngine.Random.Range(0, skinsAvailable.Count)];

                rewardName = skin.Gun.GunName + " | " + skin.SkinName;
                rewardImage = skin.IconLink;
                rewardDesc = skin.SkinDesc;
                rewardRarity = skin.SkinRarity;
                reward = new(ERewardType.GUN_SKIN, skin.ID);

                return true;
            }
            default:
            {
                if (!Enum.TryParse(caseRarity.ToString(), true, out ERarity skinRarity))
                {
                    Logging.Debug($"Error parsing {caseRarity} to a specified skin rarity");
                    return false;
                }

                var skinsAvailable = @case.AvailableSkinsSearchByRarity[skinRarity].Where(k => k.MaxAmount == 0).ToList();
                if (skinsAvailable.Count == 0)
                {
                    Logging.Debug($"Found no skins available for the {player.CharacterName} to get for rarity {skinRarity}");
                    return false;
                }

                var skin = skinsAvailable[UnityEngine.Random.Range(0, skinsAvailable.Count)];
                if (loadout.GunSkinsSearchByID.ContainsKey(skin.ID))
                {
                    isDuplicate = true;
                    duplicateScrapAmount = skin.ScrapAmount;
                }

                rewardName = skin.Gun.GunName + " | " + skin.SkinName;
                rewardImage = skin.IconLink;
                rewardDesc = skin.SkinDesc;
                rewardRarity = skin.SkinRarity;
                reward = new(ERewardType.GUN_SKIN, skin.ID);

                return true;
            }
        }
        /*while (true)
        {
            Logging.Debug($"Calculating reward for case with id {@case.CaseID} for {player.CharacterName}");
            caseRarity = CalculateRewardRarity(@case.Weights);
            Logging.Debug($"Rarity found: {caseRarity}");

            switch (caseRarity)
            {
                case ECaseRarity.KNIFE or ECaseRarity.LIMITED_KNIFE:
                {
                    var isLimited = caseRarity == ECaseRarity.LIMITED_KNIFE;
                    Logging.Debug($"{player.CharacterName} reward rarity is KNIFE, IsLimited {isLimited}");
                    List<Knife> knivesAvailable;
                    if (isLimited)
                    {
                        Logging.Debug($"{player.CharacterName} has gotten a limited knife rarity, checking if there are any limited knives available");
                        knivesAvailable = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount > 0 && k.MaxAmount > k.UnboxedAmount && (!loadout.Knives.TryGetValue(k.KnifeID, out var knife) || !knife.IsBought)).ToList();
                        if (knivesAvailable.Count == 0)
                        {
                            Logging.Debug("There are no limited knives available, getting knives that are not owned by the player");
                            knivesAvailable = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount == 0 && (!loadout.Knives.TryGetValue(k.KnifeID, out var knife) || !knife.IsBought)).ToList();
                        }
                    }
                    else
                        knivesAvailable = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount == 0 && (!loadout.Knives.TryGetValue(k.KnifeID, out var knife) || !knife.IsBought)).ToList();

                    Logging.Debug($"Found {knivesAvailable.Count} knives available to be unboxed by the player");
                    Knife knife = null;
                    if (knivesAvailable.Count == 0)
                    {
                        Logging.Debug($"There are no knives available to calculate with, sending a random one to player");
                        var newKnives = DB.Knives.Values.Where(k => k.KnifeWeight > 0 && k.MaxAmount == 0).ToList();
                        knife = newKnives[UnityEngine.Random.Range(0, newKnives.Count)];

                        isDuplicate = true;
                        duplicateScrapAmount = knife.ScrapAmount;
                    }
                    else
                        knife = CalculateKnife(knivesAvailable);

                    rewardName = knife.KnifeName;
                    rewardImage = knife.IconLink;
                    rewardDesc = knife.KnifeDesc;
                    rewardRarity = knife.KnifeRarity;
                    reward = new(ERewardType.KNIFE, knife.KnifeID);

                    return true;
                }
                case ECaseRarity.GLOVE or ECaseRarity.LIMITED_GLOVE:
                {
                    var isLimited = caseRarity == ECaseRarity.LIMITED_GLOVE;
                    Logging.Debug($"{player.CharacterName} reward rarity is GLOVE, IsLimited {isLimited}");
                    List<Glove> glovesAvailable;
                    if (isLimited)
                    {
                        Logging.Debug($"{player.CharacterName} has gotten a limited glove rarity, checking if there are any limited gloves available");
                        glovesAvailable = DB.Gloves.Values.Where(k => k.GloveWeight > 0 && k.MaxAmount > 0 && k.MaxAmount > k.UnboxedAmount && (!loadout.Gloves.TryGetValue(k.GloveID, out var glove) || !glove.IsBought)).ToList();
                        if (glovesAvailable.Count == 0)
                        {
                            Logging.Debug("There are no limited gloves available, getting gloves that are not owned by the player");
                            glovesAvailable = DB.Gloves.Values.Where(k => k.GloveWeight > 0 && k.MaxAmount == 0 && (!loadout.Gloves.TryGetValue(k.GloveID, out var glove) || !glove.IsBought)).ToList();
                        }
                    }
                    else
                        glovesAvailable = DB.Gloves.Values.Where(k => k.GloveWeight > 0 && k.MaxAmount == 0 && (!loadout.Gloves.TryGetValue(k.GloveID, out var glove) || !glove.IsBought)).ToList();

                    Logging.Debug($"Found {glovesAvailable.Count} gloves available to be unboxed by the player");
                    Glove glove;
                    if (glovesAvailable.Count == 0)
                    {
                        Logging.Debug($"There are no gloves available to calculate with, sending a random one to player");
                        var newGloves = DB.Gloves.Values.Where(k => k.GloveWeight > 0 && k.MaxAmount == 0).ToList();
                        glove = newGloves[UnityEngine.Random.Range(0, newGloves.Count)];

                        isDuplicate = true;
                        duplicateScrapAmount = glove.ScrapAmount;
                    }
                    else
                        glove = CalculateGlove(glovesAvailable);

                    rewardName = glove.GloveName;
                    rewardImage = glove.IconLink;
                    rewardDesc = glove.GloveDesc;
                    rewardRarity = glove.GloveRarity;
                    reward = new(ERewardType.GLOVE, glove.GloveID);

                    return true;
                }
                case ECaseRarity.LIMITED_SKIN:
                {
                    Logging.Debug($"{player.CharacterName} reward rarity is LIMITED SKIN");

                    var skinsAvailable = @case.AvailableSkins.Where(k => k.MaxAmount > 0 && k.MaxAmount > k.UnboxedAmount).ToList();
                    if (skinsAvailable.Count == 0)
                    {
                        Logging.Debug("No limited skin found, re-rolling");
                        continue;
                    }

                    Logging.Debug($"Found {skinsAvailable.Count} limited skins available, checking for non duplicates");
                    var nonDuplicateSkins = skinsAvailable.Where(k => !loadout.GunSkinsSearchByID.ContainsKey(k.ID)).ToList();
                    Logging.Debug($"Found {nonDuplicateSkins.Count} non duplicate skins available");
                    if (nonDuplicateSkins.Count == 0)
                    {
                        Logging.Debug("No non duplicate skins found, re-rolling");
                        continue;
                    }

                    var skin = nonDuplicateSkins[UnityEngine.Random.Range(0, nonDuplicateSkins.Count)];

                    rewardName = skin.Gun.GunName + " | " + skin.SkinName;
                    rewardImage = skin.IconLink;
                    rewardDesc = skin.SkinDesc;
                    rewardRarity = skin.SkinRarity;
                    reward = new(ERewardType.GUN_SKIN, skin.ID);

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

                    var skinsAvailable = @case.AvailableSkinsSearchByRarity[skinRarity].Where(k => k.MaxAmount == 0).ToList();
                    Logging.Debug($"Found {skinsAvailable.Count} skins available to got by {player.CharacterName} for rarity {skinRarity}");
                    if (skinsAvailable.Count == 0)
                    {
                        Logging.Debug($"Found no skins available for the {player.CharacterName} to get for rarity {skinRarity}");
                        return false;
                    }

                    var skin = skinsAvailable[UnityEngine.Random.Range(0, skinsAvailable.Count)];
                    if (loadout.GunSkinsSearchByID.ContainsKey(skin.ID))
                    {
                        isDuplicate = true;
                        duplicateScrapAmount = skin.ScrapAmount;
                    }

                    rewardName = skin.Gun.GunName + " | " + skin.SkinName;
                    rewardImage = skin.IconLink;
                    rewardDesc = skin.SkinDesc;
                    rewardRarity = skin.SkinRarity;
                    reward = new(ERewardType.GUN_SKIN, skin.ID);

                    return true;
                }
            }
        }*/
    }

    private Glove CalculateGlove(List<Glove> gloves)
    {
        Logging.Debug($"Calculating random glove, found {gloves.Count} weights to look from");
        var poolSize = 0;
        foreach (var glove in gloves)
            poolSize += glove.GloveWeight;

        var randInt = UnityEngine.Random.Range(0, poolSize) + 1;

        Logging.Debug($"Total Poolsize: {poolSize}, random int: {randInt}");
        var accumulatedProbability = 0;
        for (var i = 0; i < gloves.Count; i++)
        {
            var glove = gloves[i];
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
        var poolSize = 0;
        foreach (var knife in knives)
            poolSize += knife.KnifeWeight;

        var randInt = UnityEngine.Random.Range(0, poolSize) + 1;

        Logging.Debug($"Total Poolsize: {poolSize}, random int: {randInt}");
        var accumulatedProbability = 0;
        for (var i = 0; i < knives.Count; i++)
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
        var poolSize = 0;
        foreach (var weight in weights)
            poolSize += weight.Item2;

        var randInt = UnityEngine.Random.Range(0, poolSize) + 1;

        Logging.Debug($"Total Poolsize: {poolSize}, random int: {randInt}");
        var accumulatedProbability = 0;
        for (var i = 0; i < weights.Count; i++)
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