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
