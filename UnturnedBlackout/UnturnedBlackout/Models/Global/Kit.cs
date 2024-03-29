﻿using System;
using System.Collections.Generic;

namespace UnturnedBlackout.Models.Global;

[Serializable]
public class Kit
{
    public List<ushort> ItemIDs { get; set; }

    public Kit()
    {
    }

    public Kit(List<ushort> itemIDs)
    {
        ItemIDs = itemIDs;
    }
}