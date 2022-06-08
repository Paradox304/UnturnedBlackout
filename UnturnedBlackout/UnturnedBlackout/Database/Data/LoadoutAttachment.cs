using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutAttachment
    {
        public GunAttachment Attachment { get; set; }
        public int LevelRequirement { get; set; }
        public bool IsBought { get; set; }

        public LoadoutAttachment(GunAttachment attachment, int levelRequirement, bool isBought)
        {
            Attachment = attachment;
            LevelRequirement = levelRequirement;
            IsBought = isBought;
        }

        public int GetCoins(int currentLevel)
        {
            var levelsRequired = LevelRequirement - currentLevel;
            return Attachment.Coins * levelsRequired;
        }
    }
}
