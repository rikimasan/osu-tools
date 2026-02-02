// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public class Collection
    {
        public required string FileName { get; set; }
        public required string Name { get; set; }
        public List<CollectionScoreEntry>? Entries { get; set; }
        public long[]? Scores { get; set; }
        public Dictionary<long, ExpectedPerformanceValues> ExpectedPerformance { get; set; } = new Dictionary<long, ExpectedPerformanceValues>();

        public void EnsureEntries()
        {
            if (Entries == null)
            {
                Entries = Scores?.Select(id => new CollectionScoreEntry { ScoreId = id }).ToList()
                          ?? new List<CollectionScoreEntry>();
            }

            Scores = null;
        }
    }

    public class ExpectedPerformanceValues
    {
        public double? Total { get; set; }
        public Dictionary<string, double> Skills { get; set; } = new Dictionary<string, double>();
    }
}
