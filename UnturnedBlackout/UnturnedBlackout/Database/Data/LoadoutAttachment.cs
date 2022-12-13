using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data;

public class LoadoutAttachment
{
    public GunAttachment Attachment { get; set; }
    public int LevelRequirement { get; set; }
    public bool IsBought { get; set; }
    public bool IsUnlocked { get; set; }

    public LoadoutAttachment(GunAttachment attachment, int levelRequirement, bool isBought, bool isUnlocked)
    {
        Attachment = attachment;
        LevelRequirement = levelRequirement;
        IsBought = isBought;
        IsUnlocked = isUnlocked;
    }

    public int GetCoins(int currentLevel) => Attachment.Coins;
}