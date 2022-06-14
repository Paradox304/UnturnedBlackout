using System.Collections.Generic;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class GunAttachment
    {
        public ushort AttachmentID { get; set; }
        public string AttachmentName { get; set; }
        public string AttachmentDesc { get; set; }
        public List<string> AttachmentPros { get; set; }
        public List<string> AttachmentCons { get; set; }
        public EAttachment AttachmentType { get; set; }
        public string AttachmentRarity { get; set; }
        public float MovementChange { get; set; }
        public float MovementChangeADS { get; set; }
        public string IconLink { get; set; }
        public int BuyPrice { get; set; }
        public int Coins { get; set; }

        public GunAttachment(ushort attachmentID, string attachmentName, string attachmentDesc, List<string> attachmentPros, List<string> attachmentCons, EAttachment attachmentType, string attachmentRarity, float movementChange, float movementChangeADS, string iconLink, int buyPrice, int coins)
        {
            AttachmentID = attachmentID;
            AttachmentName = attachmentName;
            AttachmentDesc = attachmentDesc;
            AttachmentPros = attachmentPros;
            AttachmentCons = attachmentCons;
            AttachmentType = attachmentType;
            AttachmentRarity = attachmentRarity;
            MovementChange = movementChange;
            MovementChangeADS = movementChangeADS;
            IconLink = iconLink;
            BuyPrice = buyPrice;
            Coins = coins;
        }
    }
}
