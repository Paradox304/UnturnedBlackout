using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Managers
{
    public class LoadoutManager
    {
        public DatabaseManager DB { get; set; }

        public LoadoutManager()
        {
            DB = Plugin.Instance.DB;
        }

        public void EquipGun(UnturnedPlayer player, int loadoutID, ushort newGun, bool isPrimary)
        {
            Logging.Debug($"{player.CharacterName} is trying to switch {(isPrimary ? "Primary" : "Secondary")} to {newGun} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Guns.TryGetValue(newGun, out LoadoutGun gun) && newGun != 0)
            {
                Logging.Debug($"Error finding loadout gun with id {newGun} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            if (isPrimary)
            {
                playerLoadout.Primary = gun;
                playerLoadout.PrimaryAttachments.Clear();
                playerLoadout.PrimarySkin = null;
                if (gun == null)
                {
                    playerLoadout.PrimaryGunCharm = null;
                }
                if (gun != null)
                {
                    foreach (GunAttachment defaultAttachment in gun.Gun.DefaultAttachments)
                    {
                        if (gun.Attachments.TryGetValue(defaultAttachment.AttachmentID, out LoadoutAttachment attachment))
                        {
                            if (!playerLoadout.PrimaryAttachments.ContainsKey(attachment.Attachment.AttachmentType))
                            {
                                playerLoadout.PrimaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                            }
                        }
                    }
                }
            }
            else
            {
                playerLoadout.Secondary = gun;
                playerLoadout.SecondaryAttachments.Clear();
                playerLoadout.SecondarySkin = null;
                if (gun == null)
                {
                    playerLoadout.SecondaryGunCharm = null;
                }
                if (gun != null)
                {
                    foreach (GunAttachment defaultAttachment in gun.Gun.DefaultAttachments)
                    {
                        if (gun.Attachments.TryGetValue(defaultAttachment.AttachmentID, out LoadoutAttachment attachment))
                        {
                            if (!playerLoadout.SecondaryAttachments.ContainsKey(attachment.Attachment.AttachmentType))
                            {
                                playerLoadout.SecondaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                            }
                        }
                    }
                }
            }

            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void EquipAttachment(UnturnedPlayer player, ushort attachmentID, int loadoutID, bool isPrimary)
        {
            Logging.Debug($"{player.CharacterName} is trying to equip attachment for {(isPrimary ? "Primary" : "Secondary")} with id {attachmentID} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            LoadoutGun gun = isPrimary ? playerLoadout.Primary : playerLoadout.Secondary;
            if (gun == null)
            {
                Logging.Debug($"The gun which {player.CharacterName} is trying to equip an attachment on is null");
                return;
            }

            if (!gun.Attachments.TryGetValue(attachmentID, out LoadoutAttachment attachment))
            {
                Logging.Debug($"Attachment with id {attachmentID} is not found on the gun that {player.CharacterName} is putting it on");
                return;
            }

            Dictionary<EAttachment, LoadoutAttachment> attachments = isPrimary ? playerLoadout.PrimaryAttachments : playerLoadout.SecondaryAttachments;
            if (attachments.ContainsKey(attachment.Attachment.AttachmentType))
            {
                attachments[attachment.Attachment.AttachmentType] = attachment;
            }
            else
            {
                attachments.Add(attachment.Attachment.AttachmentType, attachment);
            }

            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void DequipAttachment(UnturnedPlayer player, ushort attachmentID, int loadoutID, bool isPrimary)
        {
            Logging.Debug($"{player.CharacterName} is trying to dequip attachment for {(isPrimary ? "Primary" : "Secondary")} with id {attachmentID} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            LoadoutGun gun = isPrimary ? playerLoadout.Primary : playerLoadout.Secondary;
            if (gun == null)
            {
                Logging.Debug($"The gun which {player.CharacterName} is trying to dequip an attachment on is null");
                return;
            }

            if (!gun.Attachments.TryGetValue(attachmentID, out LoadoutAttachment attachment))
            {
                Logging.Debug($"Attachment with id {attachmentID} is not found on the gun that {player.CharacterName} is dequipping on");
                return;
            }

            Dictionary<EAttachment, LoadoutAttachment> attachments = isPrimary ? playerLoadout.PrimaryAttachments : playerLoadout.SecondaryAttachments;
            if (attachments.ContainsValue(attachment))
            {
                attachments.Remove(attachment.Attachment.AttachmentType);

                Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
                Task.Run(async () =>
                {
                    Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                    await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                    Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
                });
            }
            else
            {
                Logging.Debug($"Attachment was not found equipped on the gun");
            }
        }

        public void EquipGunCharm(UnturnedPlayer player, int loadoutID, ushort newGunCharm, bool isPrimary)
        {
            Logging.Debug($"{player.CharacterName} is trying to switch {(isPrimary ? "Primary Gun Charm" : "Secondary Gun Charm")} to {newGunCharm} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.GunCharms.TryGetValue(newGunCharm, out LoadoutGunCharm gunCharm) && newGunCharm != 0)
            {
                Logging.Debug($"Error finding loadout gun charm with id {newGunCharm} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            if (isPrimary)
            {
                playerLoadout.PrimaryGunCharm = gunCharm;
            }
            else
            {
                playerLoadout.SecondaryGunCharm = gunCharm;
            }


            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void EquipKnife(UnturnedPlayer player, int loadoutID, ushort newKnife)
        {
            Logging.Debug($"{player.CharacterName} is trying to switch knife to {newKnife} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Knives.TryGetValue(newKnife, out LoadoutKnife knife) && newKnife != 0)
            {
                Logging.Debug($"Error finding loadout knife with id {newKnife} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            playerLoadout.Knife = knife;
            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void EquipTactical(UnturnedPlayer player, int loadoutID, ushort newTactical)
        {
            Logging.Debug($"{player.CharacterName} is trying to switch tactical to {newTactical} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Gadgets.TryGetValue(newTactical, out LoadoutGadget tactical) && newTactical != 0)
            {
                Logging.Debug($"Error finding loadout gadget with id {newTactical} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            playerLoadout.Tactical = tactical;
            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void EquipLethal(UnturnedPlayer player, int loadoutID, ushort newLethal)
        {
            Logging.Debug($"{player.CharacterName} is trying to switch lethal to {newLethal} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Gadgets.TryGetValue(newLethal, out LoadoutGadget lethal) && newLethal != 0)
            {
                Logging.Debug($"Error finding loadout gadget with id {newLethal} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            playerLoadout.Lethal = lethal;
            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void EquipGlove(UnturnedPlayer player, int loadoutID, int newGlove)
        {
            Logging.Debug($"{player.CharacterName} is trying to switch glove to {newGlove} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Gloves.TryGetValue(newGlove, out LoadoutGlove glove) && newGlove != 0)
            {
                Logging.Debug($"Error finding loadout glove with id {newGlove} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            playerLoadout.Glove = glove;
            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void EquipCard(UnturnedPlayer player, int loadoutID, int newCard)
        {
            Logging.Debug($"{player.CharacterName} is trying to switch card to {newCard} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Cards.TryGetValue(newCard, out LoadoutCard card) && newCard != 0)
            {
                Logging.Debug($"Error finding loadout card with id {newCard} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            playerLoadout.Card = card;
            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void EquipPerk(UnturnedPlayer player, int loadoutID, int newPerkID)
        {
            Logging.Debug($"{player.CharacterName} is trying to equip perk with id {newPerkID} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Perks.TryGetValue(newPerkID, out LoadoutPerk newPerk))
            {
                Logging.Debug($"Error finding loadout perk with id {newPerkID} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            if (playerLoadout.Perks.TryGetValue(newPerk.Perk.PerkType, out LoadoutPerk currentPerk))
            {
                playerLoadout.PerksSearchByType.Remove(currentPerk.Perk.SkillType);
                playerLoadout.Perks[newPerk.Perk.PerkType] = newPerk;
            }
            else
            {
                playerLoadout.Perks.Add(newPerk.Perk.PerkType, newPerk);
            }

            playerLoadout.PerksSearchByType.Add(newPerk.Perk.SkillType, newPerk);
            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void DequipPerk(UnturnedPlayer player, int loadoutID, int oldPerk)
        {
            Logging.Debug($"{player.CharacterName} is trying to dequip perk with id {oldPerk} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Perks.TryGetValue(oldPerk, out LoadoutPerk perk))
            {
                Logging.Debug($"Error finding loadout perk with id {oldPerk} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            if (!playerLoadout.Perks.Remove(perk.Perk.PerkType) && !playerLoadout.PerksSearchByType.Remove(perk.Perk.SkillType))
            {
                Logging.Debug($"Perk with id {oldPerk} was not equipped for {player.CharacterName} for loadout with id {loadoutID}");
                return;
            }

            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void EquipKillstreak(UnturnedPlayer player, int loadoutID, int newKillstreak)
        {
            Logging.Debug($"{player.CharacterName} is trying to equip killstreak with id {newKillstreak} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Killstreaks.TryGetValue(newKillstreak, out LoadoutKillstreak killstreak))
            {
                Logging.Debug($"Error finding loadout killstreak with id {newKillstreak} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            playerLoadout.Killstreaks.RemoveAll(k => k.Killstreak.KillstreakRequired == killstreak.Killstreak.KillstreakRequired);

            if (playerLoadout.Killstreaks.Count == 3)
            {
                playerLoadout.Killstreaks.RemoveAt(0);
                playerLoadout.Killstreaks.Add(killstreak);
            }
            else
            {
                playerLoadout.Killstreaks.Add(killstreak);
            }

            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void DequipKillstreak(UnturnedPlayer player, int loadoutID, int oldKillstreak)
        {
            Logging.Debug($"{player.CharacterName} is trying to dequip killstreak with id {oldKillstreak} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Killstreaks.TryGetValue(oldKillstreak, out LoadoutKillstreak killstreak))
            {
                Logging.Debug($"Error finding loadout killstreak with id {oldKillstreak} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            if (!playerLoadout.Killstreaks.Remove(killstreak))
            {
                Logging.Debug($"Killstreak with id {oldKillstreak} was not equipped for {player.CharacterName} for loadout with id {loadoutID}");
                return;
            }

            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void EquipGunSkin(UnturnedPlayer player, int loadoutID, int id, bool isPrimary)
        {
            Logging.Debug($"{player.CharacterName} trying to equip gun skin with id {id}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.GunSkinsSearchByID.TryGetValue(id, out GunSkin gunSkin))
            {
                Logging.Debug($"Error finding gun skin with id {id} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            if (isPrimary)
            {
                if (playerLoadout.Primary.Gun.GunID != gunSkin.Gun.GunID)
                {
                    Logging.Debug($"{player.CharacterName} is trying to set skin with id {gunSkin.ID} which is not available for the primary that he has equipped with id {playerLoadout.Primary.Gun.GunID}");
                    return;
                }
                playerLoadout.PrimarySkin = gunSkin;
            }
            else
            {
                if (playerLoadout.Secondary.Gun.GunID != gunSkin.Gun.GunID)
                {
                    Logging.Debug($"{player.CharacterName} is trying to set skin with id {gunSkin.ID} which is not available for the secondary that he has equipped with id {playerLoadout.Primary.Gun.GunID}");
                    return;
                }
                playerLoadout.SecondarySkin = gunSkin;
            }

            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void DequipGunSkin(UnturnedPlayer player, int loadoutID, bool isPrimary)
        {
            Logging.Debug($"{player.CharacterName} trying to dequip gun skin for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout id with {loadoutID} for {player.CharacterName}");
                return;
            }

            if (isPrimary)
            {
                playerLoadout.PrimarySkin = null;
            }
            else
            {
                playerLoadout.SecondarySkin = null;
            }

            Logging.Debug($"PRE LOADOUT CHECK {player.CharacterName}");
            Task.Run(async () =>
            {
                Logging.Debug($"THREAD ENTERED LOADOUT CHECK {player.CharacterName}");
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                Logging.Debug($"SQL QUERY EXECUTED LOADOUT CHECK {player.CharacterName}");
            });
        }

        public void GiveLoadout(GamePlayer player, Kit kit, List<TeamGlove> gloves)
        {
            PlayerInventory inv = player.Player.Player.inventory;
            inv.ClearInventory();
            // Adding clothes
            foreach (ushort id in kit.ItemIDs)
            {
                inv.forceAddItem(new Item(id, true), true);
            }
            // Getting active loadout
            if (!DB.PlayerLoadouts.TryGetValue(player.SteamID, out PlayerLoadout loadout))
            {
                return;
            }

            Loadout activeLoadout = loadout.Loadouts.Values.FirstOrDefault(k => k.IsActive);
            if (activeLoadout == null)
            {
                player.SetActiveLoadout(null, 0, 0, 0);
                return;
            }
            // Giving glove to player
            if (activeLoadout.Glove != null)
            {
                TeamGlove glove = gloves.FirstOrDefault(k => k.GloveID == activeLoadout.Glove.Glove.GloveID);
                player.Player.Player.clothing.thirdClothes.shirt = 0;
                player.Player.Player.clothing.askWearShirt(0, 0, new byte[0], true);
                inv.forceAddItem(new Item(glove.ItemID, true), true);
            }
            // Giving primary to player
            if (activeLoadout.Primary != null)
            {
                Item item = new(activeLoadout.PrimarySkin == null ? activeLoadout.Primary.Gun.GunID : activeLoadout.PrimarySkin.SkinID, false);

                // Setting up attachments
                for (int i = 0; i <= 3; i++)
                {
                    EAttachment attachmentType = (EAttachment)i;
                    int startingPos = Utility.GetStartingPos(attachmentType);
                    if (activeLoadout.PrimaryAttachments.TryGetValue(attachmentType, out LoadoutAttachment attachment))
                    {
                        byte[] bytes = BitConverter.GetBytes(attachment.Attachment.AttachmentID);
                        item.state[startingPos] = bytes[0];
                        item.state[startingPos + 1] = bytes[1];

                        if (attachmentType == EAttachment.Magazine)
                        {
                            ItemMagazineAsset asset = Assets.find(EAssetType.ITEM, attachment.Attachment.AttachmentID) as ItemMagazineAsset;
                            item.state[10] = asset.amount;

                            for (int i2 = 1; i2 <= activeLoadout.Primary.Gun.MagAmount; i2++)
                            {
                                inv.forceAddItem(new Item(attachment.Attachment.AttachmentID, true), false);
                            }
                        }
                    }
                }

                if (activeLoadout.PrimaryGunCharm != null)
                {
                    byte[] bytes = BitConverter.GetBytes(activeLoadout.PrimaryGunCharm.GunCharm.CharmID);
                    item.state[2] = bytes[0];
                    item.state[3] = bytes[1];
                }

                inv.items[0].tryAddItem(item);
                player.Player.Player.equipment.ServerEquip(0, 0, 0);
            }
            // Giving secondary to player
            if (activeLoadout.Secondary != null)
            {
                Item item = new(activeLoadout.SecondarySkin == null ? activeLoadout.Secondary.Gun.GunID : activeLoadout.SecondarySkin.SkinID, true);

                // Setting up attachments
                for (int i = 0; i <= 3; i++)
                {
                    EAttachment attachmentType = (EAttachment)i;
                    int startingPos = Utility.GetStartingPos(attachmentType);
                    if (activeLoadout.SecondaryAttachments.TryGetValue(attachmentType, out LoadoutAttachment attachment))
                    {
                        byte[] bytes = BitConverter.GetBytes(attachment.Attachment.AttachmentID);
                        item.state[startingPos] = bytes[0];
                        item.state[startingPos + 1] = bytes[1];
                        if (attachmentType == EAttachment.Magazine)
                        {
                            ItemMagazineAsset asset = Assets.find(EAssetType.ITEM, attachment.Attachment.AttachmentID) as ItemMagazineAsset;
                            item.state[10] = asset.amount;
                            for (int i2 = 1; i2 <= activeLoadout.Secondary.Gun.MagAmount; i2++)
                            {
                                inv.forceAddItem(new Item(attachment.Attachment.AttachmentID, true), false);
                            }
                        }
                    }
                }

                if (activeLoadout.SecondaryGunCharm != null)
                {
                    byte[] bytes = BitConverter.GetBytes(activeLoadout.SecondaryGunCharm.GunCharm.CharmID);
                    item.state[2] = bytes[0];
                    item.state[3] = bytes[1];
                }

                inv.items[1].tryAddItem(item);
                if (activeLoadout.Primary == null)
                {
                    player.Player.Player.equipment.ServerEquip(1, 0, 0);
                }
            }
            // Giving knife to player
            byte knifePage = 0;
            byte knifeX = 0;
            byte knifeY = 0;

            if (activeLoadout.Knife != null)
            {
                inv.forceAddItem(new Item(activeLoadout.Knife.Knife.KnifeID, true), activeLoadout.Primary == null && activeLoadout.Secondary == null);
                for (byte page = 0; page < PlayerInventory.PAGES - 2; page++)
                {
                    bool shouldBreak = false;
                    for (int index = inv.getItemCount(page) - 1; index >= 0; index--)
                    {
                        ItemJar item = inv.getItem(page, (byte)index);
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
            PlayerSkills skill = player.Player.Player.skills;
            Dictionary<(int, int), int> skills = new();
            foreach (DefaultSkill defaultSkill in Plugin.Instance.Config.DefaultSkills.FileData.DefaultSkills)
            {
                if (PlayerSkills.TryParseIndices(defaultSkill.SkillName, out int specialtyIndex, out int skillIndex))
                {
                    byte max = skill.skills[specialtyIndex][skillIndex].max;
                    if (skills.ContainsKey((specialtyIndex, skillIndex)))
                    {
                        skills[(specialtyIndex, skillIndex)] = defaultSkill.SkillLevel < max ? defaultSkill.SkillLevel : max;
                    }
                    else
                    {
                        skills.Add((specialtyIndex, skillIndex), defaultSkill.SkillLevel < max ? defaultSkill.SkillLevel : max);
                    }
                }
            }
            foreach (KeyValuePair<int, LoadoutPerk> perk in activeLoadout.Perks)
            {
                if (PlayerSkills.TryParseIndices(perk.Value.Perk.SkillType, out int specialtyIndex, out int skillIndex))
                {
                    byte max = skill.skills[specialtyIndex][skillIndex].max;
                    if (skills.ContainsKey((specialtyIndex, skillIndex)))
                    {
                        if (skills[(specialtyIndex, skillIndex)] + perk.Value.Perk.SkillLevel > max)
                        {
                            skills[(specialtyIndex, skillIndex)] = max;
                        }
                        else
                        {
                            skills[(specialtyIndex, skillIndex)] += perk.Value.Perk.SkillLevel;
                        }
                    }
                    else
                    {
                        skills.Add((specialtyIndex, skillIndex), perk.Value.Perk.SkillLevel < max ? perk.Value.Perk.SkillLevel : max);
                    }
                }
            }
            for (int specialtyIndex = 0; specialtyIndex < skill.skills.Length; specialtyIndex++)
            {
                for (int skillIndex = 0; skillIndex < skill.skills[specialtyIndex].Length; skillIndex++)
                {
                    skill.ServerSetSkillLevel(specialtyIndex, skillIndex, skills.TryGetValue((specialtyIndex, skillIndex), out int level) ? level : 0);
                }
            }
            // Giving tactical and lethal to player
            if (activeLoadout.Lethal != null)
            {
                inv.forceAddItem(new Item(activeLoadout.Lethal.Gadget.GadgetID, false), false);
            }
            if (activeLoadout.Tactical != null)
            {
                inv.forceAddItem(new Item(activeLoadout.Tactical.Gadget.GadgetID, false), false);
            }
            // Giving killstreaks to player
            foreach (LoadoutKillstreak killstreak in activeLoadout.Killstreaks)
            {
                inv.forceAddItem(new Item(killstreak.Killstreak.KillstreakInfo.TriggerItemID, true), false);
            }
            player.SetActiveLoadout(activeLoadout, knifePage, knifeX, knifeY);
        }
    }
}
