// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Game.Rulesets.Scoring;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public class CollectionScoreEntry
    {
        public string EntryId { get; set; } = Guid.NewGuid().ToString("N");
        public long? ScoreId { get; set; }
        public int BeatmapId { get; set; }
        public int RulesetId { get; set; }
        public double Accuracy { get; set; }
        public int MaxCombo { get; set; }
        public long TotalScore { get; set; }
        public long? LegacyTotalScore { get; set; }
        public Dictionary<HitResult, int> Statistics { get; set; } = new Dictionary<HitResult, int>();
        public List<CollectionModInfo> Mods { get; set; } = new List<CollectionModInfo>();
        public DateTimeOffset? EndedAt { get; set; }

        public string GetExpectedPerformanceKey()
        {
            return ScoreId?.ToString(CultureInfo.InvariantCulture) ?? EntryId;
        }
    }

    public class CollectionModInfo
    {
        public string Acronym { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    }

}
