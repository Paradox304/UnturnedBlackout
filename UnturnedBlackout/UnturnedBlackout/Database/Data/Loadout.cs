using System.Collections.Generic;
using System.Linq;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Data;

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
    public Dictionary<string, LoadoutPerk> PerksSearchByType { get; set; }
    public LoadoutGlove Glove { get; set; }
    public LoadoutCard Card { get; set; }

    public Loadout(
        int loadoutID, string loadoutName, bool isActive, LoadoutGun primary, GunSkin primarySkin, LoadoutGunCharm primaryGunCharm, Dictionary<EAttachment, LoadoutAttachment> primaryAttachments, LoadoutGun secondary, GunSkin secondarySkin, LoadoutGunCharm secondaryGunCharm,
        Dictionary<EAttachment, LoadoutAttachment> secondaryAttachments, LoadoutKnife knife, LoadoutGadget tactical, LoadoutGadget lethal, List<LoadoutKillstreak> killstreaks, Dictionary<int, LoadoutPerk> perks, Dictionary<string, LoadoutPerk> perksSearchByType, LoadoutGlove glove, LoadoutCard card)
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
        PerksSearchByType = perksSearchByType;
        Glove = glove;
        Card = card;
    }

    public void GetPrimaryMovement(out float movementChange, out float movementChangeADS)
    {
        movementChange = 0f;
        movementChangeADS = 0f;
        if (Primary == null)
            return;

        movementChange = Primary.Gun.MovementChange + PrimaryAttachments.Values.Sum(k => k.Attachment.MovementChange);
        movementChangeADS = Primary.Gun.MovementChangeADS + PrimaryAttachments.Values.Sum(k => k.Attachment.MovementChangeADS);
    }

    public void GetSecondaryMovement(out float movementChange, out float movementChangeADS)
    {
        movementChange = 0f;
        movementChangeADS = 0f;
        if (Secondary == null)
            return;

        movementChange = Secondary.Gun.MovementChange + SecondaryAttachments.Values.Sum(k => k.Attachment.MovementChange);
        movementChangeADS = Secondary.Gun.MovementChangeADS + SecondaryAttachments.Values.Sum(k => k.Attachment.MovementChangeADS);
    }

    public float GetKnifeMovement() => Knife.Knife.MovementChange;
}