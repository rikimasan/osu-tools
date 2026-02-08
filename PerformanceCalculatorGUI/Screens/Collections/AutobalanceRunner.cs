// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Logging;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Difficulty;
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
        private const int max_iterations = 5000;
        private const double initial_temperature = 100.0;
        private const double cooling_rate = 0.999;
        private const double min_temperature = 0.001;
        private const double dataset_progress_portion = 0.05;
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

        public static IReadOnlyList<AutobalanceParameter<OsuDifficultyConstants>> OsuParameters => osuParameters;
        public static IReadOnlyList<AutobalanceParameter<CatchDifficultyConstants>> CatchParameters => catchParameters;

        public static IReadOnlyList<IAutobalanceParameter> GetParameters(AutobalanceRuleset ruleset) =>
            ruleset == AutobalanceRuleset.Catch ? catchParametersForUi : osuParametersForUi;

        private static readonly AutobalanceParameter<OsuDifficultyConstants>[] osuParameters = createOsuAutobalanceParameters();
        private static readonly AutobalanceParameter<CatchDifficultyConstants>[] catchParameters = createCatchAutobalanceParameters();
        private static readonly IAutobalanceParameter[] osuParametersForUi = osuParameters;
        private static readonly IAutobalanceParameter[] catchParametersForUi = catchParameters;

        private static AutobalanceParameter<OsuDifficultyConstants>[] createOsuAutobalanceParameters() =>
            OsuDifficultyTuningParameters.All.Select(parameter => new AutobalanceParameter<OsuDifficultyConstants>(
                parameter.AutobalanceLabel,
                parameter.IsInteger,
                parameter.AutobalanceMinValue,
                parameter.DefaultEnabled,
                parameter.Getter,
                parameter.Setter,
                parameter.AutobalanceMaxValue)).ToArray();

        private static AutobalanceParameter<CatchDifficultyConstants>[] createCatchAutobalanceParameters() =>
            CatchDifficultyTuningParameters.All.Select(parameter => new AutobalanceParameter<CatchDifficultyConstants>(
                parameter.AutobalanceLabel,
                parameter.IsInteger,
                parameter.AutobalanceMinValue,
                parameter.DefaultEnabled,
                parameter.Getter,
                parameter.Setter,
                parameter.AutobalanceMaxValue)).ToArray();

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

        public Task<AutobalanceResult<OsuDifficultyConstants>> RunAsync(Collection collection, AutobalanceTarget target,
                                                                        AutobalanceParameter<OsuDifficultyConstants>[] selectedParameters,
                                                                        OsuDifficultyConstants baseTuning, Action<AutobalanceProgress>? progress = null)
        {
            return runAutobalanceAsync(collection, target, selectedParameters, baseTuning, "osu",
                tuning => new OsuRuleset(tuning), getOsuTargetValue, progress);
        }

        public Task<AutobalanceResult<CatchDifficultyConstants>> RunCatchAsync(Collection collection, AutobalanceTarget target,
                                                                               AutobalanceParameter<CatchDifficultyConstants>[] selectedParameters,
                                                                               CatchDifficultyConstants baseTuning, Action<AutobalanceProgress>? progress = null)
        {
            if (target != AutobalanceTarget.Total)
                return Task.FromResult(AutobalanceResult<CatchDifficultyConstants>.Failure("Catch autobalance supports only Total target."));

            return runAutobalanceAsync(collection, target, selectedParameters, baseTuning, "fruits",
                tuning => new CatchRuleset(tuning), getCatchTargetValue, progress);
        }

        private Task<AutobalanceResult<TTuning>> runAutobalanceAsync<TTuning>(Collection collection, AutobalanceTarget target,
                                                                              AutobalanceParameter<TTuning>[] selectedParameters,
                                                                              TTuning baseTuning, string rulesetShortName,
                                                                              Func<TTuning, Ruleset> createRuleset,
                                                                              Func<PerformanceAttributes?, AutobalanceTarget, double?> getTargetValue,
                                                                              Action<AutobalanceProgress>? progress = null)
        {
            return Task.Run(async () =>
            {
                var reporter = new ProgressReporter(progress);
                reporter.Report(0, stage: "Preparing...");

                var dataset = await buildAutobalanceDataset(collection, target, reporter, rulesetShortName).ConfigureAwait(false);
                if (dataset.Count == 0)
                {
                    reporter.Report(1, stage: "Failed");
                    return AutobalanceResult<TTuning>.Failure($"No expected values found for {getTargetLabel(target)}.");
                }

                selectedParameters = selectedParameters.Where(p => !p.IsInteger).ToArray();

                if (selectedParameters.Length == 0)
                {
                    reporter.Report(dataset_progress_portion, stage: "Evaluating...");
                    var (_, baseRmse, baseSpearman) = evaluateAutobalance(dataset, selectedParameters, baseTuning, target, Array.Empty<double>(), createRuleset, getTargetValue);
                    reporter.Report(1, stage: "Done");
                    return AutobalanceResult<TTuning>.Success(baseTuning, baseRmse, baseSpearman, dataset.Count);
                }

                int n = selectedParameters.Length;
                double[] currentValues = new double[n];
                double[] bestValues = new double[n];
                double[] lowerBounds = new double[n];
                double[] upperBounds = new double[n];

                for (int i = 0; i < n; i++)
                {
                    double baseVal = selectedParameters[i].Getter(baseTuning);
                    if (double.IsNaN(baseVal) || double.IsInfinity(baseVal))
                        baseVal = 1.0;

                    double lo, hi;

                    if (selectedParameters[i].MaxValue is { } maxVal)
                    {
                        lo = selectedParameters[i].MinValue;
                        hi = maxVal;
                    }
                    else
                    {
                        lo = Math.Max(selectedParameters[i].MinValue, baseVal * bound_lower_factor);
                        hi = Math.Max(baseVal * bound_upper_factor, selectedParameters[i].MinValue * bound_upper_factor);
                    }

                    if (double.IsNaN(lo) || double.IsInfinity(lo))
                        lo = selectedParameters[i].MinValue;
                    if (double.IsNaN(hi) || double.IsInfinity(hi) || hi <= lo)
                        hi = lo + Math.Max(1e-6, Math.Abs(lo) * 0.1);

                    currentValues[i] = Math.Clamp(baseVal, lo, hi);
                    lowerBounds[i] = lo;
                    upperBounds[i] = hi;
                }

                Array.Copy(currentValues, bestValues, n);

                reporter.Report(dataset_progress_portion, stage: "Optimizing...");

                var (currentLoss, currentRmse, currentSpearman) = evaluateAutobalance(dataset, selectedParameters, baseTuning, target, currentValues, createRuleset, getTargetValue);
                double bestLoss = currentLoss;
                double bestRmse = currentRmse;
                double bestSpearman = currentSpearman;

                var random = new Random(42);
                double temperature = initial_temperature;

                for (int iteration = 0; iteration < max_iterations && temperature > min_temperature; iteration++)
                {
                    int paramIndex = random.Next(n);
                    double range = (upperBounds[paramIndex] - lowerBounds[paramIndex]) * temperature / initial_temperature;
                    double perturbation = (random.NextDouble() * 2 - 1) * range;

                    double[] candidateValues = (double[])currentValues.Clone();
                    candidateValues[paramIndex] = Math.Clamp(
                        candidateValues[paramIndex] + perturbation,
                        lowerBounds[paramIndex],
                        upperBounds[paramIndex]);

                    var (candidateLoss, candidateRmse, candidateSpearman) = evaluateAutobalance(dataset, selectedParameters, baseTuning, target, candidateValues, createRuleset, getTargetValue);

                    double delta = candidateLoss - currentLoss;

                    if (delta < 0 || random.NextDouble() < Math.Exp(-delta / temperature))
                    {
                        currentValues = candidateValues;
                        currentLoss = candidateLoss;

                        if (currentLoss < bestLoss)
                        {
                            bestLoss = currentLoss;
                            bestRmse = candidateRmse;
                            bestSpearman = candidateSpearman;
                            Array.Copy(currentValues, bestValues, n);
                        }
                    }

                    temperature *= cooling_rate;

                    double opt = (double)(iteration + 1) / max_iterations;
                    double combined = dataset_progress_portion + (1.0 - dataset_progress_portion) * opt;
                    reporter.Report(combined);
                }

                var balancedTuning = applyAutobalanceParameters(baseTuning, selectedParameters, bestValues);

                reporter.Report(1, stage: "Done");
                return AutobalanceResult<TTuning>.Success(balancedTuning, bestRmse, bestSpearman, dataset.Count);
            });
        }

        private async Task<List<AutobalanceScoreData>> buildAutobalanceDataset(Collection collection, AutobalanceTarget target,
                                                                               ProgressReporter reporter, string rulesetShortName)
        {
            var dataset = new List<AutobalanceScoreData>();
            var expectedPerformance = collection.ExpectedPerformance;

            if (expectedPerformance == null || expectedPerformance.Count == 0)
            {
                reporter.Report(dataset_progress_portion, stage: "Loading scores", completed: 0, total: 0);
                return dataset;
            }

            int totalScores = collection.Scores.Length + (collection.StoredScores?.Length ?? 0);
            int processed = 0;

            reporter.Report(0, stage: "Loading scores", completed: 0, total: totalScores);

            // Process online scores
            foreach (long scoreId in collection.Scores)
            {
                processed++;

                string key = scoreId.ToString();

                if (!expectedPerformance.TryGetValue(key, out var expectedValues) || !TryGetExpectedValue(expectedValues, target, out double expectedValue))
                {
                    reporter.Report(dataset_progress_portion * processed / totalScores, stage: "Loading scores", completed: processed, total: totalScores);
                    continue;
                }

                SoloScoreInfo? score;

                try
                {
                    score = await scoreCache.GetScore(scoreId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString(), level: LogLevel.Error);
                    reporter.Report(dataset_progress_portion * processed / totalScores, stage: "Loading scores", completed: processed, total: totalScores);
                    continue;
                }

                if (score == null)
                {
                    reporter.Report(dataset_progress_portion * processed / totalScores, stage: "Loading scores", completed: processed, total: totalScores);
                    continue;
                }

                var rulesetInfo = rulesets.GetRuleset(score.RulesetID);
                if (rulesetInfo?.ShortName != rulesetShortName)
                {
                    reporter.Report(dataset_progress_portion * processed / totalScores, stage: "Loading scores", completed: processed, total: totalScores);
                    continue;
                }

                try
                {
                    var working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapID.ToString(),
                        cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);
                    var rulesetInstance = rulesetInfo.CreateInstance();
                    var mods = score.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();
                    var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);
                    var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                    double weight = expectedValues.Weight ?? 1.0;
                    dataset.Add(new AutobalanceScoreData(working, mods, parsedScore.ScoreInfo, expectedValue, weight));
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString(), level: LogLevel.Error);
                }

                reporter.Report(dataset_progress_portion * processed / totalScores, stage: "Loading scores", completed: processed, total: totalScores);
            }

            // Process stored scores
            if (collection.StoredScores != null)
            {
                foreach (var storedScore in collection.StoredScores)
                {
                    processed++;

                    if (!expectedPerformance.TryGetValue(storedScore.Id, out var expectedValues) || !TryGetExpectedValue(expectedValues, target, out double expectedValue))
                    {
                        reporter.Report(dataset_progress_portion * processed / totalScores, stage: "Loading scores", completed: processed, total: totalScores);
                        continue;
                    }

                    var rulesetInfo = rulesets.GetRuleset(storedScore.RulesetID);
                    if (rulesetInfo?.ShortName != rulesetShortName)
                    {
                        reporter.Report(dataset_progress_portion * processed / totalScores, stage: "Loading scores", completed: processed, total: totalScores);
                        continue;
                    }

                    try
                    {
                        var working = ProcessorWorkingBeatmap.FromFileOrId(storedScore.BeatmapID.ToString(),
                            cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);
                        var rulesetInstance = rulesetInfo.CreateInstance();
                        var soloScore = storedScore.ToSoloScoreInfo(working);
                        var mods = soloScore.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();
                        var scoreInfo = soloScore.ToScoreInfo(rulesets, working.BeatmapInfo);
                        var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                        double weight = expectedValues.Weight ?? 1.0;
                        dataset.Add(new AutobalanceScoreData(working, mods, parsedScore.ScoreInfo, expectedValue, weight));
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.ToString(), level: LogLevel.Error);
                    }

                    reporter.Report(dataset_progress_portion * processed / totalScores, stage: "Loading scores", completed: processed, total: totalScores);
                }
            }

            reporter.Report(dataset_progress_portion, stage: $"Dataset ready ({dataset.Count} scores)");
            return dataset;
        }

        private (double loss, double rmse, double spearman) evaluateAutobalance<TTuning>(IReadOnlyList<AutobalanceScoreData> dataset,
                                                                                      AutobalanceParameter<TTuning>[] parameters,
                                                                                      TTuning baseTuning, AutobalanceTarget target, double[] values,
                                                                                      Func<TTuning, Ruleset> createRuleset,
                                                                                      Func<PerformanceAttributes?, AutobalanceTarget, double?> getTargetValue)
        {
            try
            {
                var tuning = applyAutobalanceParameters(baseTuning, parameters, values);
                var ruleset = createRuleset(tuning);
                var performanceCalculator = ruleset.CreatePerformanceCalculator();

                if (performanceCalculator == null)
                    return (big_penalty, big_penalty, 0);

                int n = dataset.Count;
                var computedActuals = new double[n];
                var valid = new bool[n];

                Parallel.For(0, n, i =>
                {
                    var entry = dataset[i];
                    var difficultyCalculator = ruleset.CreateDifficultyCalculator(entry.Working);
                    var difficultyAttributes = difficultyCalculator.Calculate(entry.Mods);
                    var performanceAttributes = performanceCalculator.Calculate(entry.ScoreInfo, difficultyAttributes);
                    double? actual = getTargetValue(performanceAttributes, target);

                    if (actual != null)
                    {
                        computedActuals[i] = actual.Value;
                        valid[i] = true;
                    }
                });

                double weightedErrorSum = 0;
                double weightSum = 0;
                int validCount = 0;
                var actuals = new double[n];
                var expecteds = new double[n];

                for (int i = 0; i < n; i++)
                {
                    if (!valid[i])
                        continue;

                    double actual = computedActuals[i];
                    double expected = dataset[i].ExpectedValue;
                    double weight = dataset[i].Weight;

                    double diff = actual - expected;
                    weightedErrorSum += weight * diff * diff;
                    weightSum += weight;

                    actuals[validCount] = actual;
                    expecteds[validCount] = expected;
                    validCount++;
                }

                if (validCount == 0 || weightSum <= 0)
                    return (big_penalty, big_penalty, 0);

                double rmse = Math.Sqrt(weightedErrorSum / weightSum);
                double spearman = validCount >= 2 ? ComputeSpearmanCorrelation(actuals, expecteds, validCount) : 0;
                double loss = rmse * (2.0 - spearman);

                return (loss, rmse, spearman);
            }
            catch
            {
                return (big_penalty, big_penalty, 0);
            }
        }

        private static TTuning applyAutobalanceParameters<TTuning>(TTuning baseTuning, AutobalanceParameter<TTuning>[] parameters, double[] values)
        {
            var tuning = baseTuning;

            for (int i = 0; i < parameters.Length; i++)
            {
                tuning = parameters[i].Apply(tuning, values[i]);
            }

            return tuning;
        }

        internal static bool TryGetExpectedValue(ExpectedPerformanceValues expectedValues, AutobalanceTarget target, out double expectedValue)
        {
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

        private static double? getOsuTargetValue(PerformanceAttributes? attributes, AutobalanceTarget target)
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

        private static double? getCatchTargetValue(PerformanceAttributes? attributes, AutobalanceTarget target)
        {
            if (attributes == null || target != AutobalanceTarget.Total)
                return null;

            return attributes.Total;
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

        internal static double ComputeSpearmanCorrelation(double[] actual, double[] expected, int count)
        {
            if (count < 2)
                return 0;

            double[] actualRanks = computeRanks(actual, count);
            double[] expectedRanks = computeRanks(expected, count);

            double sumDSq = 0;

            for (int i = 0; i < count; i++)
            {
                double d = actualRanks[i] - expectedRanks[i];
                sumDSq += d * d;
            }

            return 1.0 - 6.0 * sumDSq / (count * ((double)count * count - 1));
        }

        private static double[] computeRanks(double[] values, int count)
        {
            var indexed = new (double value, int index)[count];

            for (int i = 0; i < count; i++)
                indexed[i] = (values[i], i);

            Array.Sort(indexed, (a, b) => a.value.CompareTo(b.value));

            double[] ranks = new double[count];
            int pos = 0;

            while (pos < count)
            {
                int end = pos;

                while (end < count - 1 && Math.Abs(indexed[end + 1].value - indexed[end].value) < 1e-9)
                    end++;

                double avgRank = (pos + end) / 2.0 + 1;

                for (int k = pos; k <= end; k++)
                    ranks[indexed[k].index] = avgRank;

                pos = end + 1;
            }

            return ranks;
        }
    }

    public enum AutobalanceTarget
    {
        [Description("Total")]
        Total,

        [Description("Aim")]
        Aim,

        [Description("Speed")]
        Speed,

        [Description("Accuracy")]
        Accuracy,

        [Description("Reading")]
        Reading,

        [Description("Flashlight")]
        Flashlight
    }

    public enum AutobalanceRuleset
    {
        [Description("osu!")]
        Osu,

        [Description("catch")]
        Catch
    }

    public interface IAutobalanceParameter
    {
        public string Label { get; }
        public double MinValue { get; }
        public bool IsInteger { get; }
        public bool DefaultEnabled { get; }
    }

    public sealed class AutobalanceParameter<TTuning> : IAutobalanceParameter
    {
        public string Label { get; }
        public Func<TTuning, double> Getter { get; }
        public Func<TTuning, double, TTuning> Setter { get; }
        public double MinValue { get; }
        public double? MaxValue { get; }
        public bool IsInteger { get; }
        public bool DefaultEnabled { get; }

        public AutobalanceParameter(string label, bool isInteger, double minValue, bool defaultEnabled,
                                    Func<TTuning, double> getter, Func<TTuning, double, TTuning> setter, double? maxValue = null)
        {
            Label = label;
            IsInteger = isInteger;
            MinValue = minValue;
            MaxValue = maxValue;
            DefaultEnabled = defaultEnabled;
            Getter = getter;
            Setter = setter;
        }

        public TTuning Apply(TTuning tuning, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return tuning;

            if (IsInteger)
            {
                int intValue = (int)Math.Round(value);
                int minValue = (int)MinValue;
                int maxValue = MaxValue.HasValue ? (int)Math.Round(MaxValue.Value) : int.MaxValue;
                if (maxValue < minValue)
                    maxValue = minValue;
                intValue = Math.Clamp(intValue, minValue, maxValue);
                return Setter(tuning, intValue);
            }

            double clamped = Math.Max(value, MinValue);
            if (MaxValue.HasValue)
                clamped = Math.Min(clamped, MaxValue.Value);
            return Setter(tuning, clamped);
        }
    }

    public sealed class AutobalanceScoreData
    {
        public ProcessorWorkingBeatmap Working { get; }
        public Mod[] Mods { get; }
        public ScoreInfo ScoreInfo { get; }
        public double ExpectedValue { get; }
        public double Weight { get; }

        public AutobalanceScoreData(ProcessorWorkingBeatmap working, Mod[] mods, ScoreInfo scoreInfo, double expectedValue, double weight = 1.0)
        {
            Working = working;
            Mods = mods;
            ScoreInfo = scoreInfo;
            ExpectedValue = expectedValue;
            Weight = weight;
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

    public readonly struct AutobalanceResult<TTuning>
    {
        public bool IsFailure { get; }
        public TTuning? Tuning { get; }
        public double Rmse { get; }
        public double Spearman { get; }
        public int SampleCount { get; }
        public string? ErrorMessage { get; }

        private AutobalanceResult(TTuning tuning, double rmse, double spearman, int sampleCount)
        {
            IsFailure = false;
            Tuning = tuning;
            Rmse = rmse;
            Spearman = spearman;
            SampleCount = sampleCount;
            ErrorMessage = null;
        }

        private AutobalanceResult(string errorMessage)
        {
            IsFailure = true;
            Tuning = default;
            Rmse = 0;
            Spearman = 0;
            SampleCount = 0;
            ErrorMessage = errorMessage;
        }

        public static AutobalanceResult<TTuning> Success(TTuning tuning, double rmse, double spearman, int sampleCount) => new AutobalanceResult<TTuning>(tuning, rmse, spearman, sampleCount);
        public static AutobalanceResult<TTuning> Failure(string errorMessage) => new AutobalanceResult<TTuning>(errorMessage);
    }
}
