using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public int ScrapAmount { get; set; }
        public int BuyPrice { get; set; }
        public bool IsDefault { get; set; }
        public bool IsPrimary { get; set; }
        public List<GunAttachment> DefaultAttachments { get; set; }
        public int MaxLevel { get; set; }
        public List<int> LevelXPNeeded { get; set; }
        public List<int> LevelRewards { get; set; }

        public Gun(ushort gunID, string gunName, string gunDesc, EGun gunType, string iconLink, int magAmount, int scrapAmount, int buyPrice, bool isDefault, bool isPrimary, List<GunAttachment> defaultAttachments, int maxLevel, List<int> levelXPNeeded, List<int> levelRewards)
        {
            GunID = gunID;
            GunName = gunName;
            GunDesc = gunDesc;
            GunType = gunType;
            IconLink = iconLink;
            MagAmount = magAmount;
            ScrapAmount = scrapAmount;
            BuyPrice = buyPrice;
            IsDefault = isDefault;
            IsPrimary = isPrimary;
            DefaultAttachments = defaultAttachments;
            MaxLevel = maxLevel;
            LevelXPNeeded = levelXPNeeded;
            LevelRewards = levelRewards;
        }
    }
}
