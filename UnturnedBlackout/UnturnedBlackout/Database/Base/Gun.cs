using System.Collections.Generic;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Gun
    {
        public ushort GunID { get; set; }
        public string GunName { get; set; }
        public string GunDesc { get; set; }
        public EGun GunType { get; set; }
        public string IconLink { get; set; }
        public int MagAmount { get; set; }
        public int Coins { get; set; }
        public int BuyPrice { get; set; }
        public int LevelRequirement { get; set; }
        public bool IsPrimary { get; set; }
        public List<GunAttachment> DefaultAttachments { get; set; }
        public Dictionary<int, GunAttachment> RewardAttachments { get; set; }
        public List<int> LevelXPNeeded { get; set; }

        public Gun(ushort gunID, string gunName, string gunDesc, EGun gunType, string iconLink, int magAmount, int coins, int buyPrice, int levelRequirement, bool isPrimary, List<GunAttachment> defaultAttachments, Dictionary<int, GunAttachment> rewardAttachments, List<int> levelXPNeeded)
        {
            GunID = gunID;
            GunName = gunName;
            GunDesc = gunDesc;
            GunType = gunType;
            IconLink = iconLink;
            MagAmount = magAmount;
            Coins = coins;
            BuyPrice = buyPrice;
            LevelRequirement = levelRequirement;
            IsPrimary = isPrimary;
            DefaultAttachments = defaultAttachments;
            RewardAttachments = rewardAttachments;
            LevelXPNeeded = levelXPNeeded;
        }
    }
}
