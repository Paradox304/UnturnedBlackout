using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.KC
{
    public class KCConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public int XPPerKill { get; set; }
        public int XPPerMeleeKill { get; set; }
        public int XPPerKillHeadshot { get; set; }
        public int XPPerAssist { get; set; }

        public int XPPerKillConfirmed { get; set; }
        public int XPPerKillDenied { get; set; }

        public int BaseXPMK { get; set; }
        public int IncreaseXPPerMK { get; set; }
        public int MKSeconds { get; set; }

        public int ShutdownXP { get; set; }
        public int DominationXP { get; set; }

        public float WinMultipler { get; set; }

        public int ScoreLimit { get; set; }
        public int SpawnProtectionSeconds { get; set; }

        public ushort RedDogTagID { get; set; }
        public ushort BlueDogTagID { get; set; }

        public KCConfig()
        {

        }

        public KCConfig(int startSeconds, int endSeconds, int xPPerKill, int xPPerMeleeKill, int xPPerKillHeadshot, int xPPerAssist, int xPPerKillConfirmed, int xPPerKillDenied, int baseXPMK, int increaseXPPerMK, int mKSeconds, int shutdownXP, int dominationXP, float winMultipler, int scoreLimit, int spawnProtectionSeconds, ushort redDogTagID, ushort blueDogTagID)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            XPPerKill = xPPerKill;
            XPPerMeleeKill = xPPerMeleeKill;
            XPPerKillHeadshot = xPPerKillHeadshot;
            XPPerAssist = xPPerAssist;
            XPPerKillConfirmed = xPPerKillConfirmed;
            XPPerKillDenied = xPPerKillDenied;
            BaseXPMK = baseXPMK;
            IncreaseXPPerMK = increaseXPPerMK;
            MKSeconds = mKSeconds;
            ShutdownXP = shutdownXP;
            DominationXP = dominationXP;
            WinMultipler = winMultipler;
            ScoreLimit = scoreLimit;
            SpawnProtectionSeconds = spawnProtectionSeconds;
            RedDogTagID = redDogTagID;
            BlueDogTagID = blueDogTagID;
        }
    }
}
