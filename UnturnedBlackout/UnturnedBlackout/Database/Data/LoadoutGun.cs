﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutGun
    {
        public Gun Gun { get; set; }
        public int Level { get; set; }
        public int XP { get; set; }
        public int GunKills { get; set; }
        public bool IsBought { get; set; }
        public Dictionary<ushort, LoadoutAttachment> Attachments { get; set; }

        public LoadoutGun(Gun gun, int level, int xP, int gunKills, bool isBought, Dictionary<ushort, LoadoutAttachment> attachments)
        {
            Gun = gun;
            Level = level;
            XP = xP;
            GunKills = gunKills;
            IsBought = isBought;
            Attachments = attachments;
        }

        public bool TryGetNeededXP(out int neededXP)
        {
            neededXP = 0;
            if (Gun.LevelXPNeeded.Count < (Level - 1))
            {
                neededXP = Gun.LevelXPNeeded[Level - 1];
                return true;
            }
            return false;
        }
    }
}