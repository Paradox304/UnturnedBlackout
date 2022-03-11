using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutAttachment
    {
        public GunAttachment Attachment { get; set; }
        public bool IsBought { get; set; }

        public LoadoutAttachment(GunAttachment attachment, bool isBought)
        {
            Attachment = attachment;
            IsBought = isBought;
        }
    }
}
