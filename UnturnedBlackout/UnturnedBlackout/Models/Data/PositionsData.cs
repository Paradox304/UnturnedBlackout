﻿using System.Collections.Generic;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.TDM;

namespace UnturnedBlackout.Models.Data;

public class PositionsData
{
    public List<FFASpawnPoint> FFASpawnPoints { get; set; }
    public List<TDMSpawnPoint> TDMSpawnPoints { get; set; }
    public List<CTFSpawnPoint> CTFSpawnPoints { get; set; }

    public PositionsData()
    {
        FFASpawnPoints = new();
        TDMSpawnPoints = new();
        CTFSpawnPoints = new();
    }
}