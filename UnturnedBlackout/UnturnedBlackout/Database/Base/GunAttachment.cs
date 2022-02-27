using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class GunAttachment
    {
        public ushort AttachmentID { get; set; }
        public string AttachmentName { get; set; }
        public string AttachmentDesc { get; set; }
        public EAttachment AttachmentType { get; set; }
        public string IconLink { get; set; }
        public int BuyPrice { get; set; }

        public GunAttachment(ushort attachmentID, string attachmentName, string attachmentDesc, EAttachment attachmentType, string iconLink, int buyPrice)
        {
            AttachmentID = attachmentID;
            AttachmentName = attachmentName;
            AttachmentDesc = attachmentDesc;
            AttachmentType = attachmentType;
            IconLink = iconLink;
            BuyPrice = buyPrice;
        }
    }
}
