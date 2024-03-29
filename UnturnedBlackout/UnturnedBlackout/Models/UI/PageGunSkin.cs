﻿using System.Collections.Generic;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Models.UI;

public class PageGunSkin
{
    public int PageID { get; set; }
    public Dictionary<int, GunSkin> GunSkins { get; set; }

    public PageGunSkin(int pageID, Dictionary<int, GunSkin> gunSkins)
    {
        PageID = pageID;
        GunSkins = gunSkins;
    }
}