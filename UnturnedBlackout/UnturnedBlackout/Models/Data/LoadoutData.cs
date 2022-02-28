using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.Data
{
    public class LoadoutData
    {
        public int LoadoutID { get; set; }
        public string LoadoutName { get; set; }
        public bool IsActive { get; set; }
        public ushort Primary { get; set; }
        public List<ushort> PrimaryAttachments { get; set; }
        public ushort Secondary { get; set; }
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
            LoadoutID = loadout.LoadoutID;
            LoadoutName = loadout.LoadoutName;
            IsActive = loadout.IsActive;
            Primary = loadout.Primary?.Gun?.GunID ?? 0;
            PrimaryAttachments = loadout.PrimaryAttachments.Values.Select(k => k.Attachment.AttachmentID).ToList();
            Secondary = loadout.Secondary?.Gun?.GunID ?? 0;
            SecondaryAttachments = loadout.SecondaryAttachments.Values.Select(k => k.Attachment.AttachmentID).ToList();
            Knife = loadout.Knife?.Knife?.KnifeID ?? 0;
            Tactical = loadout.Tactical?.Gadget?.GadgetID ?? 0;
            Lethal = loadout.Lethal?.Gadget?.GadgetID ?? 0;
            Killstreaks = loadout.Killstreaks.Values.Select(k => k.Killstreak.KillstreakID).ToList();
            Perks = loadout.Perks.Select(k => k.Perk.PerkID).ToList();
            Glove = loadout.Glove?.Glove?.GloveID ?? 0;
            Card = loadout.Card?.Card?.CardID ?? 0;
        }
    }
}
