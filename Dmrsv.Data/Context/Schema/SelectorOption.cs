﻿using Dmrsv.Data.Enums;

namespace Dmrsv.Data.Context.Schema
{
    public class SelectorOption
    {
        public FilterType FilterType { get; set; } = FilterType.Query;
        public int InputInterval { get; set; } = 30;
        public bool SavesExclusion { get; set; } = false;
        public List<string> OwnedDlcs { get; set; } = new();
    }
}
