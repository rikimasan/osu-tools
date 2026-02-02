// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using osu.Framework.Logging;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Scoring;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public class AutobalanceRunner
    {
        private const int max_iterations = 500;
        private const double dataset_progress_portion = 0.05;

        private const double gradient_tolerance = 1e-2;
        private const double parameter_tolerance = 1e-2;
        private const double function_progress_tolerance = 1e-8;

        private const double big_penalty = 1e12;

        private const double bound_lower_factor = 0.33;
        private const double bound_upper_factor = 3.0;

        private readonly ScoreCache scoreCache;
        private readonly RulesetStore rulesets;
        private readonly SettingsManager configManager;

        public AutobalanceRunner(ScoreCache scoreCache, RulesetStore rulesets, SettingsManager configManager)
        {
            this.scoreCache = scoreCache;
            this.rulesets = rulesets;
            this.configManager = configManager;
        }

        public static IReadOnlyList<AutobalanceParameter> Parameters => parameters;

        private static readonly AutobalanceParameter[] parameters = createAutobalanceParameters();

        private static AutobalanceParameter[] createAutobalanceParameters() =>
            OsuDifficultyTuningParameters.All.Select(parameter => new AutobalanceParameter(parameter)).ToArray();

        private sealed class ProgressReporter
        {
            private readonly Action<AutobalanceProgress>? callback;

            private double lastValue = -1;
            private string? lastStage;
            private long lastReportTicks;

            public ProgressReporter(Action<AutobalanceProgress>? callback)
            {
                this.callback = callback;
                lastReportTicks = Stopwatch.GetTimestamp();
            }

            public void Report(double value, string? stage = null, int? completed = null, int? total = null)
            {
                if (callback == null)
                    return;

                value = Math.Clamp(value, 0, 1);

                long now = Stopwatch.GetTimestamp();
                double msSinceLast = (now - lastReportTicks) * 1000.0 / Stopwatch.Frequency;

                bool stageChanged = stage != null && stage != lastStage;
                bool valueChanged = Math.Abs(value - lastValue) >= 0.0025;
                bool force = value >= 1 || stageChanged;

                if (!force && msSinceLast < 200 && !valueChanged)
                    return;

                lastReportTicks = now;
                lastValue = value;

                if (stage != null)
                    lastStage = stage;

                callback(new AutobalanceProgress(value, stage, completed, total));
            }
        }

        public Task<AutobalanceResult> RunAsync(Collection collection, AutobalanceTarget target, AutobalanceParameter[] selectedParameters,
                                                OsuDifficultyConstants baseTuning, Action<AutobalanceProgress>? progress = null)
        {
            return Task.Run(async () =>
            {
                var reporter = new ProgressReporter(progress);
                reporter.Report(0, stage: "Preparing...");

                var dataset = await buildAutobalanceDataset(collection, target, reporter).ConfigureAwait(false);
                if (dataset.Count == 0)
                {
                    reporter.Report(1, stage: "Failed");
                    return AutobalanceResult.Failure($"No expected values found for {getTargetLabel(target)}.");
                }

                selectedParameters = selectedParameters.Where(p => !p.IsInteger).ToArray();

                if (selectedParameters.Length == 0)
                {
                    reporter.Report(dataset_progress_portion, stage: "Evaluating...");
                    var empty = Vector<double>.Build.Dense(0);
                    double mse = evaluateAutobalance(dataset, selectedParameters, baseTuning, target, empty);
                    double rmse = Math.Sqrt(mse);
                    reporter.Report(1, stage: "Done");
                    return AutobalanceResult.Success(baseTuning, rmse, dataset.Count);
                }

                int n = selectedParameters.Length;

                var initialGuess = Vector<double>.Build.Dense(n, i => selectedParameters[i].Getter(baseTuning));

                var (lowerBound, upperBound) = buildBounds(selectedParameters, baseTuning);
                initialGuess = clamp(initialGuess, lowerBound, upperBound);

                int evalCount = 0;

                reporter.Report(dataset_progress_portion, stage: "Optimizing...");

                Func<Vector<double>, double> f = point =>
                {
                    double mse = evaluateAutobalance(dataset, selectedParameters, baseTuning, target, point);

                    evalCount++;

                    double opt = Math.Min(evalCount / (double)max_iterations, 0.727);
                    double combined = dataset_progress_portion + (1.0 - dataset_progress_portion) * opt;

                    reporter.Report(combined);
                    return mse;
                };

                var minimizingPoint = FindMinimum.OfFunctionConstrained(
                    f,
                    lowerBound,
                    upperBound,
                    initialGuess,
                    gradient_tolerance,
                    parameter_tolerance,
                    function_progress_tolerance,
                    max_iterations);

                var balancedTuning = applyAutobalanceParameters(baseTuning, selectedParameters, minimizingPoint);

                // FindMinimum returns only the point, not the function value, so we evaluate once more.
                double mseAtMin = f(minimizingPoint);
                double rmseAtMin = Math.Sqrt(mseAtMin);

                reporter.Report(1, stage: "Done");
                return AutobalanceResult.Success(balancedTuning, rmseAtMin, dataset.Count);
            });
        }

        private static (Vector<double> lower, Vector<double> upper) buildBounds(AutobalanceParameter[] parameters, OsuDifficultyConstants baseTuning)
        {
            int n = parameters.Length;

            var lower = Vector<double>.Build.Dense(n);
            var upper = Vector<double>.Build.Dense(n);

            for (int i = 0; i < n; i++)
            {
                double min = parameters[i].MinValue;

                double baseVal = parameters[i].Getter(baseTuning);
                if (double.IsNaN(baseVal) || double.IsInfinity(baseVal))
                    baseVal = 1.0;


                double lo = Math.Max(min, baseVal * bound_lower_factor);
                double hi = Math.Max(baseVal * bound_upper_factor, min * bound_upper_factor);

                if (double.IsNaN(lo) || double.IsInfinity(lo))
                    lo = min;

                if (double.IsNaN(hi) || double.IsInfinity(hi) || hi <= lo)
                    hi = lo + Math.Max(1e-6, Math.Abs(lo) * 0.1);

                lower[i] = lo;
                upper[i] = hi;
            }

            return (lower, upper);
        }

        private static Vector<double> clamp(Vector<double> x, Vector<double> lower, Vector<double> upper)
        {
            var y = x.Clone();
            for (int i = 0; i < y.Count; i++)
                y[i] = Math.Min(Math.Max(y[i], lower[i]), upper[i]);
            return y;
        }

        private async Task<List<AutobalanceScoreData>> buildAutobalanceDataset(Collection collection, AutobalanceTarget target, ProgressReporter reporter)
        {
            var dataset = new List<AutobalanceScoreData>();

            collection.EnsureEntries();
            collection.ExpectedPerformance ??= new Dictionary<long, ExpectedPerformanceValues>();

            var entries = collection.Entries ?? new List<CollectionScoreEntry>();
            int total = entries.Count;

            reporter.Report(0, stage: "Loading scores", completed: 0, total: total);

            if (total == 0)
            {
                reporter.Report(dataset_progress_portion, stage: "Loading scores", completed: 0, total: 0);
                return dataset;
            }

            for (int i = 0; i < total; i++)
            {
                var entry = entries[i];

                if (!entry.ScoreId.HasValue)
                {
                    reporter.Report(dataset_progress_portion * (i + 1) / total, stage: "Loading scores", completed: i + 1, total: total);
                    continue;
                }

                long scoreId = entry.ScoreId.Value;

                if (!collection.ExpectedPerformance.TryGetValue(scoreId, out var expectedValues))
                {
                    reporter.Report(dataset_progress_portion * (i + 1) / total, stage: "Loading scores", completed: i + 1, total: total);
                    continue;
                }

                if (!tryGetExpectedValue(expectedValues, target, out double expectedValue))
                {
                    reporter.Report(dataset_progress_portion * (i + 1) / total, stage: "Loading scores", completed: i + 1, total: total);
                    continue;
                }

                SoloScoreInfo? score = null;

                try
                {
                    score = await scoreCache.GetScore(scoreId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString(), level: LogLevel.Error);
                }

                if (score == null)
                {
                    reporter.Report(dataset_progress_portion * (i + 1) / total, stage: "Loading scores", completed: i + 1, total: total);
                    continue;
                }

                var rulesetInfo = rulesets.GetRuleset(score.RulesetID);
                if (rulesetInfo?.ShortName != "osu")
                {
                    reporter.Report(dataset_progress_portion * (i + 1) / total, stage: "Loading scores", completed: i + 1, total: total);
                    continue;
                }

                ProcessorWorkingBeatmap working;

                try
                {
                    working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapID.ToString(), cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString(), level: LogLevel.Error);
                    reporter.Report(dataset_progress_portion * (i + 1) / total, stage: "Loading scores", completed: i + 1, total: total);
                    continue;
                }

                var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);
                var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);
                var mods = score.Mods.Select(x => x.ToMod(rulesetInfo.CreateInstance())).ToArray();

                dataset.Add(new AutobalanceScoreData(working, mods, parsedScore.ScoreInfo, expectedValue));

                reporter.Report(dataset_progress_portion * (i + 1) / total, stage: "Loading scores", completed: i + 1, total: total);
            }

            reporter.Report(dataset_progress_portion, stage: $"Dataset ready ({dataset.Count} scores)");
            return dataset;
        }

        private double evaluateAutobalance(IReadOnlyList<AutobalanceScoreData> dataset, AutobalanceParameter[] parameters, OsuDifficultyConstants baseTuning,
                                           AutobalanceTarget target, Vector<double> values)
        {
            try
            {
                var tuning = applyAutobalanceParameters(baseTuning, parameters, values);
                var ruleset = new OsuRuleset(tuning);
                var performanceCalculator = ruleset.CreatePerformanceCalculator();

                if (performanceCalculator == null)
                    return big_penalty;

                double errorSum = 0;
                int count = 0;

                foreach (var entry in dataset)
                {
                    var difficultyCalculator = ruleset.CreateDifficultyCalculator(entry.Working);
                    var difficultyAttributes = difficultyCalculator.Calculate(entry.Mods);
                    var performanceAttributes = performanceCalculator.Calculate(entry.ScoreInfo, difficultyAttributes);
                    var actual = getTargetValue(performanceAttributes, target);

                    if (actual == null)
                        continue;

                    double diff = actual.Value - entry.ExpectedValue;
                    errorSum += diff * diff;
                    count++;
                }

                return count > 0 ? errorSum / count : big_penalty;
            }
            catch
            {
                return big_penalty;
            }
        }

        private static OsuDifficultyConstants applyAutobalanceParameters(OsuDifficultyConstants baseTuning, AutobalanceParameter[] parameters, Vector<double> values)
        {
            var tuning = baseTuning;

            for (int i = 0; i < parameters.Length; i++)
            {
                tuning = parameters[i].Apply(tuning, values[i]);
            }

            return tuning;
        }

        private static bool tryGetExpectedValue(ExpectedPerformanceValues expectedValues, AutobalanceTarget target, out double expectedValue)
        {
            expectedValue = default;

            if (target == AutobalanceTarget.Total)
            {
                if (expectedValues.Total.HasValue)
                {
                    expectedValue = expectedValues.Total.Value;
                    return true;
                }

                if (expectedValues.Skills.TryGetValue("pp", out expectedValue))
                    return true;

                if (expectedValues.Skills.TryGetValue("total", out expectedValue))
                    return true;

                return false;
            }

            string key = getTargetKey(target);
            return expectedValues.Skills.TryGetValue(key, out expectedValue);
        }

        private static double? getTargetValue(PerformanceAttributes? attributes, AutobalanceTarget target)
        {
            if (attributes == null)
                return null;

            return target switch
            {
                AutobalanceTarget.Total => attributes.Total,
                AutobalanceTarget.Aim => (attributes as OsuPerformanceAttributes)?.Aim,
                AutobalanceTarget.Speed => (attributes as OsuPerformanceAttributes)?.Speed,
                AutobalanceTarget.Accuracy => (attributes as OsuPerformanceAttributes)?.Accuracy,
                AutobalanceTarget.Reading => (attributes as OsuPerformanceAttributes)?.Reading,
                AutobalanceTarget.Flashlight => (attributes as OsuPerformanceAttributes)?.Flashlight,
                _ => null
            };
        }

        private static string getTargetKey(AutobalanceTarget target)
        {
            return target switch
            {
                AutobalanceTarget.Total => "total",
                AutobalanceTarget.Aim => "aim",
                AutobalanceTarget.Speed => "speed",
                AutobalanceTarget.Accuracy => "accuracy",
                AutobalanceTarget.Reading => "reading",
                AutobalanceTarget.Flashlight => "flashlight",
                _ => "total"
            };
        }

        private static string getTargetLabel(AutobalanceTarget target)
        {
            return target switch
            {
                AutobalanceTarget.Total => "total",
                AutobalanceTarget.Aim => "aim",
                AutobalanceTarget.Speed => "speed",
                AutobalanceTarget.Accuracy => "accuracy",
                AutobalanceTarget.Reading => "reading",
                AutobalanceTarget.Flashlight => "flashlight",
                _ => "total"
            };
        }
    }

    public enum AutobalanceTarget
    {
        [System.ComponentModel.Description("Total")]
        Total,
        [System.ComponentModel.Description("Aim")]
        Aim,
        [System.ComponentModel.Description("Speed")]
        Speed,
        [System.ComponentModel.Description("Accuracy")]
        Accuracy,
        [System.ComponentModel.Description("Reading")]
        Reading,
        [System.ComponentModel.Description("Flashlight")]
        Flashlight
    }

    public sealed class AutobalanceParameter
    {
        public string Label { get; }
        public Func<OsuDifficultyConstants, double> Getter { get; }
        public Func<OsuDifficultyConstants, double, OsuDifficultyConstants> Setter { get; }
        public double MinValue { get; }
        public bool IsInteger { get; }
        public bool DefaultEnabled { get; }

        public AutobalanceParameter(OsuDifficultyTuningParameter definition)
        {
            Label = definition.AutobalanceLabel;
            Getter = definition.Getter;
            Setter = definition.Setter;
            MinValue = definition.AutobalanceMinValue;
            IsInteger = definition.IsInteger;
            DefaultEnabled = definition.DefaultEnabled;
        }

        public OsuDifficultyConstants Apply(OsuDifficultyConstants tuning, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return tuning;

            if (IsInteger)
            {
                int intValue = Math.Max((int)Math.Round(value), (int)MinValue);
                return Setter(tuning, intValue);
            }

            double clamped = Math.Max(value, MinValue);
            return Setter(tuning, clamped);
        }
    }

    public sealed class AutobalanceScoreData
    {
        public ProcessorWorkingBeatmap Working { get; }
        public Mod[] Mods { get; }
        public ScoreInfo ScoreInfo { get; }
        public double ExpectedValue { get; }

        public AutobalanceScoreData(ProcessorWorkingBeatmap working, Mod[] mods, ScoreInfo scoreInfo, double expectedValue)
        {
            Working = working;
            Mods = mods;
            ScoreInfo = scoreInfo;
            ExpectedValue = expectedValue;
        }
    }

    public readonly struct AutobalanceProgress
    {
        public double Value { get; }
        public string? Stage { get; }
        public int? Completed { get; }
        public int? Total { get; }

        public AutobalanceProgress(double value, string? stage = null, int? completed = null, int? total = null)
        {
            Value = value;
            Stage = stage;
            Completed = completed;
            Total = total;
        }
    }

    public readonly struct AutobalanceResult
    {
        public bool IsFailure { get; }
        public OsuDifficultyConstants? Tuning { get; }
        public double Rmse { get; }
        public int SampleCount { get; }
        public string? ErrorMessage { get; }

        private AutobalanceResult(OsuDifficultyConstants tuning, double rmse, int sampleCount)
        {
            IsFailure = false;
            Tuning = tuning;
            Rmse = rmse;
            SampleCount = sampleCount;
            ErrorMessage = null;
        }

        private AutobalanceResult(string errorMessage)
        {
            IsFailure = true;
            Tuning = null;
            Rmse = 0;
            SampleCount = 0;
            ErrorMessage = errorMessage;
        }

        public static AutobalanceResult Success(OsuDifficultyConstants tuning, double rmse, int sampleCount) => new AutobalanceResult(tuning, rmse, sampleCount);
        public static AutobalanceResult Failure(string errorMessage) => new AutobalanceResult(errorMessage);
    }
}
