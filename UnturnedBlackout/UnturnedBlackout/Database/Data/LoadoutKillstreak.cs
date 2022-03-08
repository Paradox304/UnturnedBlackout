﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutKillstreak
    {
        public Killstreak Killstreak { get; set; }
        public int KillstreakKills { get; set; }
        public bool IsBought { get; set; }

        public LoadoutKillstreak(Killstreak killstreak, int killstreakKills, bool isBought)
        {
            Killstreak = killstreak;
            KillstreakKills = killstreakKills;
            IsBought = isBought;
        }
    }
}