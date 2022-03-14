using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.Data
{
    public class LoadoutData
    {
        public string LoadoutName { get; set; }
        public ushort Primary { get; set; }
        public int PrimarySkin { get; set; }
        public ushort PrimaryGunCharm { get; set; }
        public List<ushort> PrimaryAttachments { get; set; }
        public ushort Secondary { get; set; }
        public int SecondarySkin { get; set; }
        public ushort SecondaryGunCharm { get; set; }
        public List<ushort> SecondaryAttachments { get; set; }
        public ushort Knife { get; set; }
        public ushort Tactical { get; set; }
        public ushort Lethal { get; set; }
        public List<int> Killstreaks { get; set; }
        public List<int> Perks { get; set; }
        public ushort Glove { get; set; }
        public int Card { get; set; }

        public LoadoutData(Loadout loadout)
        {
            LoadoutName = loadout.LoadoutName;
            Primary = loadout.Primary?.Gun?.GunID ?? 0;
            PrimarySkin = loadout.PrimarySkin?.ID ?? 0;
            PrimaryGunCharm = loadout.PrimaryGunCharm?.GunCharm.CharmID ?? 0;
            PrimaryAttachments = loadout.PrimaryAttachments.Values.Select(k => k.Attachment.AttachmentID).ToList();
            Secondary = loadout.Secondary?.Gun?.GunID ?? 0;
            SecondarySkin = loadout.SecondarySkin?.ID ?? 0;
            SecondaryGunCharm = loadout.SecondaryGunCharm?.GunCharm.CharmID ?? 0;
            SecondaryAttachments = loadout.SecondaryAttachments.Values.Select(k => k.Attachment.AttachmentID).ToList();
            Knife = loadout.Knife?.Knife?.KnifeID ?? 0;
            Tactical = loadout.Tactical?.Gadget?.GadgetID ?? 0;
            Lethal = loadout.Lethal?.Gadget?.GadgetID ?? 0;
            Killstreaks = loadout.Killstreaks.Select(k => k.Killstreak.KillstreakID).ToList();
            Perks = loadout.Perks.Select(k => k.Perk.PerkID).ToList();
            Glove = loadout.Glove?.Glove?.GloveID ?? 0;
            Card = loadout.Card?.Card?.CardID ?? 0;
        }

        [JsonConstructor]
        public LoadoutData(string loadoutName, ushort primary, int primarySkin, ushort primaryGunCharm, List<ushort> primaryAttachments, ushort secondary, int secondarySkin, ushort secondaryGunCharm, List<ushort> secondaryAttachments, ushort knife, ushort tactical, ushort lethal, List<int> killstreaks, List<int> perks, ushort glove, int card)
        {
            LoadoutName = loadoutName;
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
