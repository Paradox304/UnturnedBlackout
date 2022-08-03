namespace UnturnedBlackout.Models.Animation
{
    public class AnimationItemUnlock
    {
        public string ItemIcon { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }

        public AnimationItemUnlock(string itemIcon, string itemType, string itemName)
        {
            ItemIcon = itemIcon;
            ItemType = itemType;
            ItemName = itemName;
        }
    }
}
