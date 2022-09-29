using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers;

public class LoadoutManager
{
    public DatabaseManager DB { get; set; }

    public LoadoutManager()
    {
        DB = Plugin.Instance.DB;
    }

    public void EquipGun(UnturnedPlayer player, int loadoutID, ushort newGun, bool isPrimary)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to switch {(isPrimary ? "Primary" : "Secondary")} to {newGun} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Guns.TryGetValue(newGun, out var gun) && newGun != 0)
        {
            Logging.Debug($"Error finding loadout gun with id {newGun} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        if (isPrimary)
        {
            playerLoadout.Primary = gun;
            playerLoadout.PrimaryAttachments.Clear();
            playerLoadout.PrimarySkin = null;
            if (gun == null) playerLoadout.PrimaryGunCharm = null;

            if (gun != null)
            {
                foreach (var defaultAttachment in gun.Gun.DefaultAttachments)
                {
                    if (gun.Attachments.TryGetValue(defaultAttachment.AttachmentID, out var attachment))
                    {
                        if (!playerLoadout.PrimaryAttachments.ContainsKey(attachment.Attachment.AttachmentType))
                            playerLoadout.PrimaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                    }
                }
            }
        }
        else
        {
            playerLoadout.Secondary = gun;
            playerLoadout.SecondaryAttachments.Clear();
            playerLoadout.SecondarySkin = null;
            if (gun == null) playerLoadout.SecondaryGunCharm = null;

            if (gun != null)
            {
                foreach (var defaultAttachment in gun.Gun.DefaultAttachments)
                {
                    if (gun.Attachments.TryGetValue(defaultAttachment.AttachmentID, out var attachment))
                    {
                        if (!playerLoadout.SecondaryAttachments.ContainsKey(attachment.Attachment.AttachmentType))
                            playerLoadout.SecondaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                    }
                }
            }
        }

        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void EquipAttachment(UnturnedPlayer player, ushort attachmentID, int loadoutID, bool isPrimary)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to equip attachment for {(isPrimary ? "Primary" : "Secondary")} with id {attachmentID} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        var gun = isPrimary ? playerLoadout.Primary : playerLoadout.Secondary;
        if (gun == null)
        {
            Logging.Debug($"The gun which {player.CharacterName} is trying to equip an attachment on is null");
            return;
        }

        if (!gun.Attachments.TryGetValue(attachmentID, out var attachment))
        {
            Logging.Debug(
                $"Attachment with id {attachmentID} is not found on the gun that {player.CharacterName} is putting it on");
            return;
        }

        var attachments = isPrimary ? playerLoadout.PrimaryAttachments : playerLoadout.SecondaryAttachments;
        if (attachments.ContainsKey(attachment.Attachment.AttachmentType))
            attachments[attachment.Attachment.AttachmentType] = attachment;
        else
            attachments.Add(attachment.Attachment.AttachmentType, attachment);

        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void DequipAttachment(UnturnedPlayer player, ushort attachmentID, int loadoutID, bool isPrimary)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to dequip attachment for {(isPrimary ? "Primary" : "Secondary")} with id {attachmentID} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        var gun = isPrimary ? playerLoadout.Primary : playerLoadout.Secondary;
        if (gun == null)
        {
            Logging.Debug($"The gun which {player.CharacterName} is trying to dequip an attachment on is null");
            return;
        }

        if (!gun.Attachments.TryGetValue(attachmentID, out var attachment))
        {
            Logging.Debug(
                $"Attachment with id {attachmentID} is not found on the gun that {player.CharacterName} is dequipping on");
            return;
        }

        var attachments = isPrimary ? playerLoadout.PrimaryAttachments : playerLoadout.SecondaryAttachments;
        if (attachments.ContainsValue(attachment))
        {
            _ = attachments.Remove(attachment.Attachment.AttachmentType);

            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            _ = Task.Run(async () =>
            {
                Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }
        else
            Logging.Debug($"Attachment was not found equipped on the gun");
    }

    public void EquipGunCharm(UnturnedPlayer player, int loadoutID, ushort newGunCharm, bool isPrimary)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to switch {(isPrimary ? "Primary Gun Charm" : "Secondary Gun Charm")} to {newGunCharm} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.GunCharms.TryGetValue(newGunCharm, out var gunCharm) && newGunCharm != 0)
        {
            Logging.Debug($"Error finding loadout gun charm with id {newGunCharm} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        if (isPrimary)
            playerLoadout.PrimaryGunCharm = gunCharm;
        else
            playerLoadout.SecondaryGunCharm = gunCharm;

        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void EquipKnife(UnturnedPlayer player, int loadoutID, ushort newKnife)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to switch knife to {newKnife} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Knives.TryGetValue(newKnife, out var knife) && newKnife != 0)
        {
            Logging.Debug($"Error finding loadout knife with id {newKnife} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        playerLoadout.Knife = knife;
        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void EquipTactical(UnturnedPlayer player, int loadoutID, ushort newTactical)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to switch tactical to {newTactical} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Gadgets.TryGetValue(newTactical, out var tactical) && newTactical != 0)
        {
            Logging.Debug($"Error finding loadout gadget with id {newTactical} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        playerLoadout.Tactical = tactical;
        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void EquipLethal(UnturnedPlayer player, int loadoutID, ushort newLethal)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to switch lethal to {newLethal} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Gadgets.TryGetValue(newLethal, out var lethal) && newLethal != 0)
        {
            Logging.Debug($"Error finding loadout gadget with id {newLethal} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        playerLoadout.Lethal = lethal;
        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void EquipGlove(UnturnedPlayer player, int loadoutID, int newGlove)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to switch glove to {newGlove} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Gloves.TryGetValue(newGlove, out var glove) && newGlove != 0)
        {
            Logging.Debug($"Error finding loadout glove with id {newGlove} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        playerLoadout.Glove = glove;
        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void EquipCard(UnturnedPlayer player, int loadoutID, int newCard)
    {
        Logging.Debug($"{player.CharacterName} is trying to switch card to {newCard} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Cards.TryGetValue(newCard, out var card) && newCard != 0)
        {
            Logging.Debug($"Error finding loadout card with id {newCard} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        playerLoadout.Card = card;
        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void EquipPerk(UnturnedPlayer player, int loadoutID, int newPerkID)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to equip perk with id {newPerkID} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Perks.TryGetValue(newPerkID, out var newPerk))
        {
            Logging.Debug($"Error finding loadout perk with id {newPerkID} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        if (playerLoadout.Perks.TryGetValue(newPerk.Perk.PerkType, out var currentPerk))
        {
            _ = playerLoadout.PerksSearchByType.Remove(currentPerk.Perk.SkillType);
            playerLoadout.Perks[newPerk.Perk.PerkType] = newPerk;
        }
        else
            playerLoadout.Perks.Add(newPerk.Perk.PerkType, newPerk);

        playerLoadout.PerksSearchByType.Add(newPerk.Perk.SkillType, newPerk);
        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void DequipPerk(UnturnedPlayer player, int loadoutID, int oldPerk)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to dequip perk with id {oldPerk} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Perks.TryGetValue(oldPerk, out var perk))
        {
            Logging.Debug($"Error finding loadout perk with id {oldPerk} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        if (!playerLoadout.Perks.Remove(perk.Perk.PerkType) &&
            !playerLoadout.PerksSearchByType.Remove(perk.Perk.SkillType))
        {
            Logging.Debug(
                $"Perk with id {oldPerk} was not equipped for {player.CharacterName} for loadout with id {loadoutID}");
            return;
        }

        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void EquipKillstreak(UnturnedPlayer player, int loadoutID, int newKillstreak)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to equip killstreak with id {newKillstreak} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Killstreaks.TryGetValue(newKillstreak, out var killstreak))
        {
            Logging.Debug($"Error finding loadout killstreak with id {newKillstreak} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        _ = playerLoadout.Killstreaks.RemoveAll(k =>
            k.Killstreak.KillstreakRequired == killstreak.Killstreak.KillstreakRequired);

        if (playerLoadout.Killstreaks.Count == 3)
        {
            playerLoadout.Killstreaks.RemoveAt(0);
            playerLoadout.Killstreaks.Add(killstreak);
        }
        else
            playerLoadout.Killstreaks.Add(killstreak);

        playerLoadout.Killstreaks.Sort((x, y) =>
            x.Killstreak.KillstreakRequired.CompareTo(y.Killstreak.KillstreakRequired));

        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void DequipKillstreak(UnturnedPlayer player, int loadoutID, int oldKillstreak)
    {
        Logging.Debug(
            $"{player.CharacterName} is trying to dequip killstreak with id {oldKillstreak} for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Killstreaks.TryGetValue(oldKillstreak, out var killstreak))
        {
            Logging.Debug($"Error finding loadout killstreak with id {oldKillstreak} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        if (!playerLoadout.Killstreaks.Remove(killstreak))
        {
            Logging.Debug(
                $"Killstreak with id {oldKillstreak} was not equipped for {player.CharacterName} for loadout with id {loadoutID}");
            return;
        }

        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void EquipGunSkin(UnturnedPlayer player, int loadoutID, int id, bool isPrimary)
    {
        Logging.Debug($"{player.CharacterName} trying to equip gun skin with id {id}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.GunSkinsSearchByID.TryGetValue(id, out var gunSkin))
        {
            Logging.Debug($"Error finding gun skin with id {id} for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
            return;
        }

        if (isPrimary)
        {
            if (playerLoadout.Primary.Gun.GunID != gunSkin.Gun.GunID)
            {
                Logging.Debug(
                    $"{player.CharacterName} is trying to set skin with id {gunSkin.ID} which is not available for the primary that he has equipped with id {playerLoadout.Primary.Gun.GunID}");
                return;
            }

            playerLoadout.PrimarySkin = gunSkin;
        }
        else
        {
            if (playerLoadout.Secondary.Gun.GunID != gunSkin.Gun.GunID)
            {
                Logging.Debug(
                    $"{player.CharacterName} is trying to set skin with id {gunSkin.ID} which is not available for the secondary that he has equipped with id {playerLoadout.Primary.Gun.GunID}");
                return;
            }

            playerLoadout.SecondarySkin = gunSkin;
        }

        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void DequipGunSkin(UnturnedPlayer player, int loadoutID, bool isPrimary)
    {
        Logging.Debug($"{player.CharacterName} trying to dequip gun skin for loadout with id {loadoutID}");
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding loadout for {player.CharacterName}");
            return;
        }

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
        {
            Logging.Debug($"Error finding loadout id with {loadoutID} for {player.CharacterName}");
            return;
        }

        if (isPrimary)
            playerLoadout.PrimarySkin = null;
        else
            playerLoadout.SecondarySkin = null;

        Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
        _ = Task.Run(async () =>
        {
            Logging.Debug($"TASK ENTERED LOADOUT CHECK {player.CharacterName}");
            await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
        });
    }

    public void GiveLoadout(GamePlayer player)
    {
        var inv = player.Player.Player.inventory;
        inv.ClearInventory();

        // Getting active loadout
        if (!DB.PlayerLoadouts.TryGetValue(player.SteamID, out var loadout)) return;

        var activeLoadout = loadout.Loadouts.Values.FirstOrDefault(k => k.IsActive);
        if (activeLoadout == null)
        {
            player.SetActiveLoadout(null, 0, 0, 0);
            return;
        }

        // Getting team info of player
        var game = player.CurrentGame;
        if (game == null) return;

        var team = game.GetTeam(player);
        var gloves = team.TeamGloves;
        var kit = team.TeamKits[UnityEngine.Random.Range(0, team.TeamKits.Count)];

        // Adding clothes
        foreach (var id in kit.ItemIDs) inv.forceAddItem(new(id, true), true);

        // Giving glove to player
        if (activeLoadout.Glove != null)
        {
            var glove = gloves.FirstOrDefault(k => k.GloveID == activeLoadout.Glove.Glove.GloveID);
            player.Player.Player.clothing.thirdClothes.shirt = 0;
            player.Player.Player.clothing.askWearShirt(0, 0, new byte[0], true);
            inv.forceAddItem(new(glove.ItemID, true), true);
        }

        // Giving primary to player
        if (activeLoadout.Primary != null)
        {
            Item item = new(
                activeLoadout.PrimarySkin == null ? activeLoadout.Primary.Gun.GunID : activeLoadout.PrimarySkin.SkinID,
                false);

            // Setting up attachments
            for (var i = 0; i <= 3; i++)
            {
                var attachmentType = (EAttachment)i;
                var startingPos = Utility.GetStartingPos(attachmentType);
                if (activeLoadout.PrimaryAttachments.TryGetValue(attachmentType, out var attachment))
                {
                    var bytes = BitConverter.GetBytes(attachment.Attachment.AttachmentID);
                    item.state[startingPos] = bytes[0];
                    item.state[startingPos + 1] = bytes[1];

                    if (attachmentType == EAttachment.Magazine)
                    {
                        var asset =
                            Assets.find(EAssetType.ITEM, attachment.Attachment.AttachmentID) as ItemMagazineAsset;
                        item.state[10] = asset.amount;

                        for (var i2 = 1; i2 <= activeLoadout.Primary.Gun.MagAmount; i2++)
                            inv.forceAddItem(new(attachment.Attachment.AttachmentID, true), false);
                    }
                }
            }

            if (activeLoadout.PrimaryGunCharm != null)
            {
                var bytes = BitConverter.GetBytes(activeLoadout.PrimaryGunCharm.GunCharm.CharmID);
                item.state[2] = bytes[0];
                item.state[3] = bytes[1];
            }

            _ = inv.items[0].tryAddItem(item);
            player.Player.Player.equipment.ServerEquip(0, 0, 0);
        }

        // Giving secondary to player
        if (activeLoadout.Secondary != null)
        {
            Item item = new(
                activeLoadout.SecondarySkin == null
                    ? activeLoadout.Secondary.Gun.GunID
                    : activeLoadout.SecondarySkin.SkinID, true);

            // Setting up attachments
            for (var i = 0; i <= 3; i++)
            {
                var attachmentType = (EAttachment)i;
                var startingPos = Utility.GetStartingPos(attachmentType);
                if (activeLoadout.SecondaryAttachments.TryGetValue(attachmentType, out var attachment))
                {
                    var bytes = BitConverter.GetBytes(attachment.Attachment.AttachmentID);
                    item.state[startingPos] = bytes[0];
                    item.state[startingPos + 1] = bytes[1];
                    if (attachmentType == EAttachment.Magazine)
                    {
                        var asset =
                            Assets.find(EAssetType.ITEM, attachment.Attachment.AttachmentID) as ItemMagazineAsset;
                        item.state[10] = asset.amount;
                        for (var i2 = 1; i2 <= activeLoadout.Secondary.Gun.MagAmount; i2++)
                            inv.forceAddItem(new(attachment.Attachment.AttachmentID, true), false);
                    }
                }
            }

            if (activeLoadout.SecondaryGunCharm != null)
            {
                var bytes = BitConverter.GetBytes(activeLoadout.SecondaryGunCharm.GunCharm.CharmID);
                item.state[2] = bytes[0];
                item.state[3] = bytes[1];
            }

            _ = inv.items[1].tryAddItem(item);
            if (activeLoadout.Primary == null) player.Player.Player.equipment.ServerEquip(1, 0, 0);
        }

        // Giving knife to player
        byte knifePage = 0;
        byte knifeX = 0;
        byte knifeY = 0;

        if (activeLoadout.Knife != null)
        {
            inv.forceAddItem(new(activeLoadout.Knife.Knife.KnifeID, true),
                activeLoadout.Primary == null && activeLoadout.Secondary == null);
            for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
            {
                var shouldBreak = false;
                for (var index = inv.getItemCount(page) - 1; index >= 0; index--)
                {
                    var item = inv.getItem(page, (byte)index);
                    if (item != null && item.item.id == activeLoadout.Knife.Knife.KnifeID)
                    {
                        knifePage = page;
                        knifeX = item.x;
                        knifeY = item.y;
                        shouldBreak = true;
                        break;
                    }
                }

                if (shouldBreak) break;
            }
        }

        // Giving perks to player
        var skill = player.Player.Player.skills;
        Dictionary<(int, int), int> skills = new();

        foreach (var defaultSkill in Plugin.Instance.Config.DefaultSkills.FileData.DefaultSkills)
        {
            if (PlayerSkills.TryParseIndices(defaultSkill.SkillName, out var specialtyIndex, out var skillIndex))
            {
                var max = skill.skills[specialtyIndex][skillIndex].max;
                if (skills.ContainsKey((specialtyIndex, skillIndex)))
                    skills[(specialtyIndex, skillIndex)] =
                        defaultSkill.SkillLevel < max ? defaultSkill.SkillLevel : max;
                else
                    skills.Add((specialtyIndex, skillIndex),
                        defaultSkill.SkillLevel < max ? defaultSkill.SkillLevel : max);
            }
        }

        foreach (var perk in activeLoadout.Perks)
        {
            if (PlayerSkills.TryParseIndices(perk.Value.Perk.SkillType, out var specialtyIndex, out var skillIndex))
            {
                var max = skill.skills[specialtyIndex][skillIndex].max;
                if (skills.ContainsKey((specialtyIndex, skillIndex)))
                {
                    if (skills[(specialtyIndex, skillIndex)] + perk.Value.Perk.SkillLevel > max)
                        skills[(specialtyIndex, skillIndex)] = max;
                    else
                        skills[(specialtyIndex, skillIndex)] += perk.Value.Perk.SkillLevel;
                }
                else
                    skills.Add((specialtyIndex, skillIndex),
                        perk.Value.Perk.SkillLevel < max ? perk.Value.Perk.SkillLevel : max);
            }
        }

        for (var specialtyIndex = 0; specialtyIndex < skill.skills.Length; specialtyIndex++)
        {
            for (var skillIndex = 0; skillIndex < skill.skills[specialtyIndex].Length; skillIndex++)
                _ = skill.ServerSetSkillLevel(specialtyIndex, skillIndex,
                    skills.TryGetValue((specialtyIndex, skillIndex), out var level) ? level : 0);
        }

        // Giving tactical and lethal to player
        if (activeLoadout.Lethal != null) inv.forceAddItem(new(activeLoadout.Lethal.Gadget.GadgetID, false), false);

        if (activeLoadout.Tactical != null) inv.forceAddItem(new(activeLoadout.Tactical.Gadget.GadgetID, false), false);

        // Giving killstreaks to player
        foreach (var killstreak in activeLoadout.Killstreaks)
            inv.forceAddItem(new(killstreak.Killstreak.KillstreakInfo.TriggerItemID, true), false);

        player.SetActiveLoadout(activeLoadout, knifePage, knifeX, knifeY);
    }
}