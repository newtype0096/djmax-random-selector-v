﻿using System.Collections.Generic;

namespace DjmaxRandomSelectorV.Models
{
    public class SelectorOption
    {
        public int InputInterval { get; set; } = 30;
        public bool SavesExclusion { get; set; } = false;
        public List<string> OwnedDlcs { get; set; } = new();
    }
}
