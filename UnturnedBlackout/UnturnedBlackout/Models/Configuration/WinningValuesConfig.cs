using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.Configuration
{
    public class WinningValuesConfig
    {
        public int PointsDivisible { get; set; }
        public int PointsPerMinutePlayed { get; set; }

        public int BonusXPVictoryDivisible { get; set; }
        public int BonusXPDefeatDivisible { get; set; }
        public int BonusXPPerMinutePlayed { get; set; }

        public float PrimeBooster { get; set; }

        public WinningValuesConfig()
        {
            PointsDivisible = 25;
            PointsPerMinutePlayed = 10;

            BonusXPVictoryDivisible = 2;
            BonusXPDefeatDivisible = 3;
            BonusXPPerMinutePlayed = 100;

            PrimeBooster = 0.2f;
        }
    }
}
