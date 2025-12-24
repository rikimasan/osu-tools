// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public class Collection
    {
        public required string FileName { get; set; }
        public required string Name { get; set; }
        public required long[] Scores { get; set; }
        public Dictionary<long, ExpectedPerformanceValues> ExpectedPerformance { get; set; } = new Dictionary<long, ExpectedPerformanceValues>();
    }

    public class ExpectedPerformanceValues
    {
        public double? Total { get; set; }
        public Dictionary<string, double> Skills { get; set; } = new Dictionary<string, double>();
    }
}
