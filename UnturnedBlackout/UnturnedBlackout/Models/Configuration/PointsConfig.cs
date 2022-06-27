using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models.Configuration
{
    public class PointsConfig
    {
        public int KillPoints { get; set; }
        public int AssistPoints { get; set; }
        public int KillConfirmedPoints { get; set; }
        public int KillDeniedPoints { get; set; }
        public int FlagSavedPoints { get; set; }
        public int FlagCapturedPoints { get; set; }

        public PointsConfig()
        {
            KillPoints = 50;
            AssistPoints = 25;
            KillConfirmedPoints = 15;
            KillDeniedPoints = 10;
            FlagSavedPoints = 100;
            FlagCapturedPoints = 200;
        }
    }
}
