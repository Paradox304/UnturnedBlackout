using System;
using System.Collections.Generic;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class IconsConfig
{
    public string BlacktagsSmallIconLink { get; set; }
    public string PointsSmallIconLink { get; set; }
    public string ScrapSmallIconLink { get; set; }

    public string BlacktagsLargeIconLink { get; set; }
    public string PointsLargeIconLink { get; set; }
    public string ScrapLargeIconLink { get; set; }

    public string KnifeUnboxingIconLink { get; set; }
    public string GloveUnboxingIconLink { get; set; }
    public string LimitedSkinUnboxingIconLink { get; set; }
    public string SpecialSkinUnboxingIconLink { get; set; }
    
    public string XPIconLink { get; set; }

    public string XPBoostIconLink { get; set; }
    public string GunXPBoostIconLink { get; set; }
    public string BPXPBoostIconLink { get; set; }

    public string HiddenFlagIconLink { get; set; }
    public string FlagAPILink { get; set; }
    
    public List<string> ScrollableImages { get; set; }

    public IconsConfig()
    {
    }
}