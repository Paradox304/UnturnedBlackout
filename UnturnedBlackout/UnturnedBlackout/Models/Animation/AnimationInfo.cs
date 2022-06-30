using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Animation
{
    public class AnimationInfo
    {
        public EAnimationType AnimationType { get; set; }
        public object Info { get; set; }

        public AnimationInfo(EAnimationType animationType, object info)
        {
            AnimationType = animationType;
            Info = info;
        }
    }
}
