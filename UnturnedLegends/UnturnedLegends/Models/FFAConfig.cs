﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedLegends.Models
{
    public class FFAConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public int XPPerKill { get; set; }
        public int XPPerKillHeadshot { get; set; }

        public int BaseXPKS { get; set; }
        public int IncreaseXPPerKS { get; set; }

        public int BaseXPMK { get; set; }
        public int IncreaseXPPerMK { get; set; }
        public int MKSeconds { get; set; }

        public FFAConfig()
        {

        }

        public FFAConfig(int startSeconds, int endSeconds, int xPPerKill, int xPPerKillHeadshot, int baseXPKS, int increaseXPPerKS, int baseXPMK, int increaseXPPerMK, int mKSeconds)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            XPPerKill = xPPerKill;
            XPPerKillHeadshot = xPPerKillHeadshot;
            BaseXPKS = baseXPKS;
            IncreaseXPPerKS = increaseXPPerKS;
            BaseXPMK = baseXPMK;
            IncreaseXPPerMK = increaseXPPerMK;
            MKSeconds = mKSeconds;
        }
    }
}
