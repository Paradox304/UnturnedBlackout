using System.Collections.Generic;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Data
{
    public class Loadout
    {
        public int LoadoutID { get; set; }
        public string LoadoutName { get; set; }
        public bool IsActive { get; set; }
        public LoadoutGun Primary { get; set; }
        public GunSkin PrimarySkin { get; set; }
        public LoadoutGunCharm PrimaryGunCharm { get; set; }
        public Dictionary<EAttachment, LoadoutAttachment> PrimaryAttachments { get; set; }
        public LoadoutGun Secondary { get; set; }
        public GunSkin SecondarySkin { get; set; }
        public LoadoutGunCharm SecondaryGunCharm { get; set; }
        public Dictionary<EAttachment, LoadoutAttachment> SecondaryAttachments { get; set; }
        public LoadoutKnife Knife { get; set; }
        public LoadoutGadget Tactical { get; set; }
        public LoadoutGadget Lethal { get; set; }
        public List<LoadoutKillstreak> Killstreaks { get; set; }
        public Dictionary<int, LoadoutPerk> Perks { get; set; }
        public LoadoutGlove Glove { get; set; }
        public LoadoutCard Card { get; set; }

        public Loadout(int loadoutID, string loadoutName, bool isActive, LoadoutGun primary, GunSkin primarySkin, LoadoutGunCharm primaryGunCharm, Dictionary<EAttachment, LoadoutAttachment> primaryAttachments, LoadoutGun secondary, GunSkin secondarySkin, LoadoutGunCharm secondaryGunCharm, Dictionary<EAttachment, LoadoutAttachment> secondaryAttachments, LoadoutKnife knife, LoadoutGadget tactical, LoadoutGadget lethal, List<LoadoutKillstreak> killstreaks, Dictionary<int, LoadoutPerk> perks, LoadoutGlove glove, LoadoutCard card)
        {
            LoadoutID = loadoutID;
            LoadoutName = loadoutName;
            IsActive = isActive;
            Primary = primary;
            PrimarySkin = primarySkin;
            PrimaryGunCharm = primaryGunCharm;
            PrimaryAttachments = primaryAttachments;
            Secondary = secondary;
            SecondarySkin = secondarySkin;
            SecondaryGunCharm = secondaryGunCharm;
            SecondaryAttachments = secondaryAttachments;
            Knife = knife;
            Tactical = tactical;
            Lethal = lethal;
            Killstreaks = killstreaks;
            Perks = perks;
            Glove = glove;
            Card = card;
        }
    }
}
