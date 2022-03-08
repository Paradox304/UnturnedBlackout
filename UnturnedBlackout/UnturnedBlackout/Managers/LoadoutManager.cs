﻿using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Managers
{
    public class LoadoutManager
    {
        public DatabaseManager DB { get; set; }
        
        public LoadoutManager()
        {
            DB = Plugin.Instance.DBManager;
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

            var gunSlot = isPrimary ? playerLoadout.Primary : playerLoadout.Secondary;
            var gunSlotAttachments = isPrimary ? playerLoadout.PrimaryAttachments : playerLoadout.SecondaryAttachments;

            if (isPrimary)
            {
                playerLoadout.Primary = gun;
                playerLoadout.PrimaryAttachments.Clear();
                if (gun != null)
                {
                    foreach (var defaultAttachment in gun.Gun.DefaultAttachments)
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
            } else
            {
                playerLoadout.Secondary = gun;
                playerLoadout.SecondaryAttachments.Clear();
                if (gun != null)
                {
                    foreach (var defaultAttachment in gun.Gun.DefaultAttachments)
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


            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
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

            var gun = isPrimary ? playerLoadout.Primary : playerLoadout.Secondary;
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

            var attachments = isPrimary ? playerLoadout.PrimaryAttachments : playerLoadout.SecondaryAttachments;
            if (attachments.ContainsKey(attachment.Attachment.AttachmentType))
            {
                attachments[attachment.Attachment.AttachmentType] = attachment;
            }
            else
            {
                attachments.Add(attachment.Attachment.AttachmentType, attachment);
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
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

            var gun = isPrimary ? playerLoadout.Primary : playerLoadout.Secondary;
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

            var attachments = isPrimary ? playerLoadout.PrimaryAttachments : playerLoadout.SecondaryAttachments;
            if (attachments.ContainsValue(attachment))
            {
                attachments.Remove(attachment.Attachment.AttachmentType);

                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
                });
            } else
            {
                Logging.Debug($"Attachment was not found equipped on the gun");
            }
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
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
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
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
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
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            });
        }

        public void EquipGlove(UnturnedPlayer player, int loadoutID, ushort newGlove)
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
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
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
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            });
        }

        public void EquipPerk(UnturnedPlayer player, int loadoutID, int newPerk)
        {
            Logging.Debug($"{player.CharacterName} is trying to equip perk with id {newPerk} for loadout with id {loadoutID}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.Perks.TryGetValue(newPerk, out LoadoutPerk perk))
            {
                Logging.Debug($"Error finding loadout perk with id {newPerk} for {player.CharacterName}");
                return;
            }

            if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
            {
                Logging.Debug($"Error finding loadout with id {loadoutID} for {player.CharacterName}");
                return;
            }

            if (playerLoadout.Perks.Count == 3)
            {
                playerLoadout.Perks[0] = perk;
            } else
            {
                playerLoadout.Perks.Add(perk);
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
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

            if (!playerLoadout.Perks.Remove(perk))
            {
                Logging.Debug($"Perk with id {oldPerk} was not equipped for {player.CharacterName} for loadout with id {loadoutID}");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
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

            if (playerLoadout.Killstreaks.Count == 3)
            {
                playerLoadout.Killstreaks[0] = killstreak;
            }
            else
            {
                playerLoadout.Killstreaks.Add(killstreak);
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
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

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerLoadoutAsync(player.CSteamID, loadoutID);
            });
        }

        public void EquipGunSkin(UnturnedPlayer player, int id)
        {
            Logging.Debug($"{player.CharacterName} trying to equip gun skin with id {id}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.GunSkinsSearchByID.TryGetValue(id, out LoadoutGunSkin gunSkin))
            {
                Logging.Debug($"Error finding gun skin with id {id} for {player.CharacterName}");
                return;
            }

            if (!loadout.GunSkinsSearchByGunID.TryGetValue(gunSkin.Skin.Gun.GunID, out List<LoadoutGunSkin> gunSkins))
            {
                Logging.Debug($"Error finding gun skins for gun with id {gunSkin.Skin.Gun.GunID} for {player.CharacterName}");
                return;
            }

            var otherSameSkinEquipped = gunSkins.FirstOrDefault(k => k.IsEquipped);
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerGunSkinEquipAsync(player.CSteamID, id, true);
                if  (otherSameSkinEquipped != null)
                {
                    Logging.Debug($"{player.CharacterName} has another skin equipped of the same gun with id {otherSameSkinEquipped.Skin.ID}, dequippinhg it");
                    await DB.UpdatePlayerGunSkinEquipAsync(player.CSteamID, otherSameSkinEquipped.Skin.ID, false);
                }
            });
        }

        public void DequipGunSkin(UnturnedPlayer player, int id)
        {
            Logging.Debug($"{player.CharacterName} trying to dequip gun skin with id {id}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.GunSkinsSearchByID.ContainsKey(id))
            {
                Logging.Debug($"Error finding gun skin with id {id} for {player.CharacterName}");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerGunSkinEquipAsync(player.CSteamID, id, false);
            });
        }

        public void EquipKnifeSkin(UnturnedPlayer player, int id)
        {
            Logging.Debug($"{player.CharacterName} trying to equip knife skin with id {id}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.KnifeSkinsSearchByID.TryGetValue(id, out LoadoutKnifeSkin knifeSkin))
            {
                Logging.Debug($"Error finding knife skin with id {id} for {player.CharacterName}");
                return;
            }

            if (!loadout.KnifeSkinsSearchByKnifeID.TryGetValue(knifeSkin.Skin.Knife.KnifeID, out List<LoadoutKnifeSkin> knifeSkins))
            {
                Logging.Debug($"Error finding knife skins for knife with id {knifeSkin.Skin.Knife.KnifeID} for {player.CharacterName}");
                return;
            }

            var otherSameSkinEquipped = knifeSkins.FirstOrDefault(k => k.IsEquipped);
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerKnifeSkinEquipAsync(player.CSteamID, id, true);
                if (otherSameSkinEquipped != null)
                {
                    Logging.Debug($"{player.CharacterName} has another skin equipped of the same knife with id {otherSameSkinEquipped.Skin.ID}, dequippinhg it");
                    await DB.UpdatePlayerKnifeSkinEquipAsync(player.CSteamID, otherSameSkinEquipped.Skin.ID, false);
                }
            });
        }

        public void DequipKnifeSkin(UnturnedPlayer player, int id)
        {
            Logging.Debug($"{player.CharacterName} trying to dequip knife skin with id {id}");
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding loadout for {player.CharacterName}");
                return;
            }

            if (!loadout.KnifeSkinsSearchByID.ContainsKey(id))
            {
                Logging.Debug($"Error finding gun skin with id {id} for {player.CharacterName}");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await DB.UpdatePlayerGunSkinEquipAsync(player.CSteamID, id, false);
            });
        }
    }
}