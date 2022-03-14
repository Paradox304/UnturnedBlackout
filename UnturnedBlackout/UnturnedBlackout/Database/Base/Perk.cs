﻿namespace UnturnedBlackout.Database.Base
{
    public class Perk
    {
        public int PerkID { get; set; }
        public string PerkName { get; set; }
        public string PerkDesc { get; set; }
        public string IconLink { get; set; }
        public string SkillType { get; set; }
        public int SkillLevel { get; set; }
        public int Coins { get; set; }
        public int BuyPrice { get; set; }
        public int ScrapAmount { get; set; }
        public int LevelRequirement { get; set; }

        public Perk(int perkID, string perkName, string perkDesc, string iconLink, string skillType, int skillLevel, int coins, int buyPrice, int scrapAmount, int levelRequirement)
        {
            PerkID = perkID;
            PerkName = perkName;
            PerkDesc = perkDesc;
            IconLink = iconLink;
            SkillType = skillType;
            SkillLevel = skillLevel;
            Coins = coins;
            BuyPrice = buyPrice;
            ScrapAmount = scrapAmount;
            LevelRequirement = levelRequirement;
        }
    }
}
