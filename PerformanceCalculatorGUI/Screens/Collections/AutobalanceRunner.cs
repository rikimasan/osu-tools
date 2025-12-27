// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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

        public Task<AutobalanceResult> RunAsync(Collection collection, AutobalanceTarget target, AutobalanceParameter[] selectedParameters, OsuDifficultyTuning baseTuning)
        {
            return Task.Run(async () =>
            {
                var dataset = await buildAutobalanceDataset(collection, target).ConfigureAwait(false);
                if (dataset.Count == 0)
                    return AutobalanceResult.Failure($"No expected values found for {getTargetLabel(target)}.");

                selectedParameters = selectedParameters.Where(p => !p.IsInteger).ToArray();

                if (selectedParameters.Length == 0)
                {
                    var empty = Vector<double>.Build.Dense(0);
                    double mse = evaluateAutobalance(dataset, selectedParameters, baseTuning, target, empty);
                    double rmse = Math.Sqrt(mse);
                    return AutobalanceResult.Success(baseTuning, rmse, dataset.Count);
                }

                int n = selectedParameters.Length;

                var initialGuess = Vector<double>.Build.Dense(n, i => selectedParameters[i].Getter(baseTuning));

                var (lowerBound, upperBound) = buildBounds(selectedParameters, baseTuning);
                initialGuess = clamp(initialGuess, lowerBound, upperBound);

                Func<Vector<double>, double> f = point => evaluateAutobalance(dataset, selectedParameters, baseTuning, target, point);

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

                return AutobalanceResult.Success(balancedTuning, rmseAtMin, dataset.Count);
            });
        }

        private static (Vector<double> lower, Vector<double> upper) buildBounds(AutobalanceParameter[] parameters, OsuDifficultyTuning baseTuning)
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

        private async Task<List<AutobalanceScoreData>> buildAutobalanceDataset(Collection collection, AutobalanceTarget target)
        {
            var dataset = new List<AutobalanceScoreData>();

            foreach (long scoreId in collection.Scores)
            {
                if (!collection.ExpectedPerformance.TryGetValue(scoreId, out var expectedValues))
                    continue;

                if (!tryGetExpectedValue(expectedValues, target, out double expectedValue))
                    continue;

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
                    continue;

                var rulesetInfo = rulesets.GetRuleset(score.RulesetID);
                if (rulesetInfo?.ShortName != "osu")
                    continue;

                ProcessorWorkingBeatmap working;

                try
                {
                    working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapID.ToString(), cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString(), level: LogLevel.Error);
                    continue;
                }

                var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);
                var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);
                var mods = score.Mods.Select(x => x.ToMod(rulesetInfo.CreateInstance())).ToArray();

                dataset.Add(new AutobalanceScoreData(working, mods, parsedScore.ScoreInfo, expectedValue));
            }

            return dataset;
        }

        private double evaluateAutobalance(IReadOnlyList<AutobalanceScoreData> dataset, AutobalanceParameter[] parameters, OsuDifficultyTuning baseTuning,
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

        private static OsuDifficultyTuning applyAutobalanceParameters(OsuDifficultyTuning baseTuning, AutobalanceParameter[] parameters, Vector<double> values)
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
        [System.ComponentModel.Description("Flashlight")]
        Flashlight
    }

    public sealed class AutobalanceParameter
    {
        public string Label { get; }
        public Func<OsuDifficultyTuning, double> Getter { get; }
        public Func<OsuDifficultyTuning, double, OsuDifficultyTuning> Setter { get; }
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

        public OsuDifficultyTuning Apply(OsuDifficultyTuning tuning, double value)
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

    public readonly struct AutobalanceResult
    {
        public bool IsFailure { get; }
        public OsuDifficultyTuning? Tuning { get; }
        public double Rmse { get; }
        public int SampleCount { get; }
        public string? ErrorMessage { get; }

        private AutobalanceResult(OsuDifficultyTuning tuning, double rmse, int sampleCount)
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

        public static AutobalanceResult Success(OsuDifficultyTuning tuning, double rmse, int sampleCount) => new AutobalanceResult(tuning, rmse, sampleCount);
        public static AutobalanceResult Failure(string errorMessage) => new AutobalanceResult(errorMessage);
    }
}
