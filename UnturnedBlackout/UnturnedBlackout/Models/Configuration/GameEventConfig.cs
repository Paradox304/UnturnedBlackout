using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class GameEventConfig
{
    public List<GameEvent> GameEvents { get; set; }
}