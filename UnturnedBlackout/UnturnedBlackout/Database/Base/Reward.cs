using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Reward
    {
        public ERewardType RewardType { get; set; }
        public object RewardValue { get; set; }

        public Reward(ERewardType rewardType, object rewardValue)
        {
            RewardType = rewardType;
            RewardValue = rewardValue;
        }
    }
}
