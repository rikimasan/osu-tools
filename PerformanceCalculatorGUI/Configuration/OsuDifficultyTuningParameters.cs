// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Osu.Difficulty;

namespace PerformanceCalculatorGUI.Configuration
{
    public sealed class OsuDifficultyTuningParameter
    {
        public string UiLabel { get; }
        public string AutobalanceLabel { get; }
        public bool IsInteger { get; }
        public double AutobalanceMinValue { get; }
        public bool DefaultEnabled { get; }
        public Func<OsuDifficultyConstants, double> Getter { get; }
        public Func<OsuDifficultyConstants, double, OsuDifficultyConstants> Setter { get; }

        private OsuDifficultyTuningParameter(string uiLabel, string autobalanceLabel, bool isInteger, double autobalanceMinValue, bool defaultEnabled,
                                             Func<OsuDifficultyConstants, double> getter, Func<OsuDifficultyConstants, double, OsuDifficultyConstants> setter)
        {
            UiLabel = uiLabel;
            AutobalanceLabel = autobalanceLabel;
            IsInteger = isInteger;
            AutobalanceMinValue = autobalanceMinValue;
            DefaultEnabled = defaultEnabled;
            Getter = getter;
            Setter = setter;
        }

        public static OsuDifficultyTuningParameter ForDouble(string uiLabel, string autobalanceLabel, Func<OsuDifficultyConstants, double> getter,
                                                             Func<OsuDifficultyConstants, double, OsuDifficultyConstants> setter, bool defaultEnabled,
                                                             double autobalanceMinValue = 0.01)
            => new OsuDifficultyTuningParameter(uiLabel, autobalanceLabel, false, autobalanceMinValue, defaultEnabled, getter, setter);

        public static OsuDifficultyTuningParameter ForInt(string uiLabel, string autobalanceLabel, Func<OsuDifficultyConstants, int> getter,
                                                          Func<OsuDifficultyConstants, int, OsuDifficultyConstants> setter, bool defaultEnabled,
                                                          int autobalanceMinValue = 1)
            => new OsuDifficultyTuningParameter(uiLabel, autobalanceLabel, true, autobalanceMinValue, defaultEnabled,
                                                t => getter(t), (t, v) => setter(t, (int)v));
    }

    public sealed class OsuDifficultyTuningSection
    {
        public string Title { get; }
        public IReadOnlyList<OsuDifficultyTuningParameter> Parameters { get; }

        public OsuDifficultyTuningSection(string title, params OsuDifficultyTuningParameter[] parameters)
        {
            Title = title;
            Parameters = parameters;
        }
    }

    public static class OsuDifficultyTuningParameters
    {
        public static readonly IReadOnlyList<OsuDifficultyTuningSection> Sections = new[]
        {
            new OsuDifficultyTuningSection("Performance scales",
                OsuDifficultyTuningParameter.ForDouble("Aim perf scale", "Aim perf", t => t.AimPerformanceScale, (t, v) => t with { AimPerformanceScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Speed perf scale", "Speed perf", t => t.SpeedPerformanceScale, (t, v) => t with { SpeedPerformanceScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Accuracy perf scale", "Accuracy perf", t => t.AccuracyPerformanceScale, (t, v) => t with { AccuracyPerformanceScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Flashlight perf scale", "Flashlight perf", t => t.FlashlightPerformanceScale, (t, v) => t with { FlashlightPerformanceScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading perf scale", "Reading perf", t => t.ReadingPerformanceScale, (t, v) => t with { ReadingPerformanceScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Total perf scale", "Total perf", t => t.TotalPerformanceScale, (t, v) => t with { TotalPerformanceScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Cognition perf exponent", "Cognition perf", t => t.CognitionPerformanceExponent, (t, v) => t with { CognitionPerformanceExponent = v }, false)
            ),
            new OsuDifficultyTuningSection("Skill strain scales",
                OsuDifficultyTuningParameter.ForDouble("Aim strain scale", "Aim strain", t => t.AimSkillStrainScale, (t, v) => t with { AimSkillStrainScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed strain scale", "Speed strain", t => t.SpeedSkillStrainScale, (t, v) => t with { SpeedSkillStrainScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flashlight strain scale", "Flashlight strain", t => t.FlashlightSkillStrainScale, (t, v) => t with { FlashlightSkillStrainScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading strain scale", "Reading strain", t => t.ReadingSkillStrainScale, (t, v) => t with { ReadingSkillStrainScale = v }, true)
            ),
            new OsuDifficultyTuningSection("Aim bonuses",
                OsuDifficultyTuningParameter.ForDouble("Aim wide angle", "Aim wide angle", t => t.AimWideAngleBonusScale, (t, v) => t with { AimWideAngleBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim acute angle", "Aim acute angle", t => t.AimAcuteAngleScale, (t, v) => t with { AimAcuteAngleScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim slider bonus", "Aim slider bonus", t => t.AimSliderBonusScale, (t, v) => t with { AimSliderBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim velocity bonus", "Aim velocity bonus", t => t.AimVelocityChangeBonusScale, (t, v) => t with { AimVelocityChangeBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim wiggle bonus", "Aim wiggle bonus", t => t.AimWiggleBonusScale, (t, v) => t with { AimWiggleBonusScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Aim high BPM base", "Aim high bpm", t => t.AimHighBpmBonusBase, (t, v) => t with { AimHighBpmBonusBase = v }, true)
            ),
            new OsuDifficultyTuningSection("Flashlight bonuses",
                OsuDifficultyTuningParameter.ForDouble("FL max opacity", "Flashlight max opacity", t => t.FlashlightMaxOpacityBonusScale, (t, v) => t with { FlashlightMaxOpacityBonusScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL hidden bonus", "Flashlight hidden bonus", t => t.FlashlightHiddenBonusScale, (t, v) => t with { FlashlightHiddenBonusScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL min velocity", "Flashlight min velocity", t => t.FlashlightMinVelocityScale, (t, v) => t with { FlashlightMinVelocityScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL slider bonus", "Flashlight slider bonus", t => t.FlashlightSliderBonusScale, (t, v) => t with { FlashlightSliderBonusScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL min angle", "Flashlight min angle", t => t.FlashlightMinAngleScale, (t, v) => t with { FlashlightMinAngleScale = v }, false)
            ),
            new OsuDifficultyTuningSection("Rhythm tuning",
                OsuDifficultyTuningParameter.ForInt("Rhythm time max (ms)", "Rhythm history ms", t => t.RhythmHistoryTimeMax, (t, v) => t with { RhythmHistoryTimeMax = v }, false),
                OsuDifficultyTuningParameter.ForInt("Rhythm objects max", "Rhythm history objs", t => t.RhythmHistoryObjectsMax, (t, v) => t with { RhythmHistoryObjectsMax = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Rhythm overall scale", "Rhythm overall", t => t.RhythmOverallScale, (t, v) => t with { RhythmOverallScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm ratio scale", "Rhythm ratio", t => t.RhythmRatioScale, (t, v) => t with { RhythmRatioScale = v }, true)
            ),
            new OsuDifficultyTuningSection("Reading tuning",
                OsuDifficultyTuningParameter.ForDouble("Reading window size", "Reading window", t => t.ReadingWindowSize, (t, v) => t with { ReadingWindowSize = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading distance threshold", "Reading distance", t => t.ReadingDistanceInfluenceThreshold, (t, v) => t with { ReadingDistanceInfluenceThreshold = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading hidden multiplier", "Reading hidden", t => t.ReadingHiddenMultiplier, (t, v) => t with { ReadingHiddenMultiplier = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading density multiplier", "Reading density", t => t.ReadingDensityMultiplier, (t, v) => t with { ReadingDensityMultiplier = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading density base", "Reading density base", t => t.ReadingDensityDifficultyBase, (t, v) => t with { ReadingDensityDifficultyBase = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading preempt balance", "Reading preempt", t => t.ReadingPreemptBalancingFactor, (t, v) => t with { ReadingPreemptBalancingFactor = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading preempt start", "Reading preempt start", t => t.ReadingPreemptStartingPoint, (t, v) => t with { ReadingPreemptStartingPoint = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading min angle time", "Reading min angle", t => t.ReadingMinimumAngleRelevancyTime, (t, v) => t with { ReadingMinimumAngleRelevancyTime = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading max angle time", "Reading max angle", t => t.ReadingMaximumAngleRelevancyTime, (t, v) => t with { ReadingMaximumAngleRelevancyTime = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading reduced baseline", "Reading reduced base", t => t.ReadingReducedDifficultyBaseLine, (t, v) => t with { ReadingReducedDifficultyBaseLine = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Reading reduced duration", "Reading reduced dur", t => t.ReadingReducedDifficultyDuration, (t, v) => t with { ReadingReducedDifficultyDuration = v }, false)
            ),
            new OsuDifficultyTuningSection("Speed tuning",
                OsuDifficultyTuningParameter.ForDouble("Speed single spacing", "Speed spacing", t => t.SpeedSingleSpacingThreshold, (t, v) => t with { SpeedSingleSpacingThreshold = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed min bonus BPM", "Speed min bpm", t => t.SpeedMinBonusBpm, (t, v) => t with { SpeedMinBonusBpm = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed balancing factor", "Speed balance", t => t.SpeedBalancingFactor, (t, v) => t with { SpeedBalancingFactor = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed distance scale", "Speed distance", t => t.SpeedDistanceScale, (t, v) => t with { SpeedDistanceScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed high BPM base", "Speed high bpm", t => t.SpeedHighBpmBonusBase, (t, v) => t with { SpeedHighBpmBonusBase = v }, true)
            )
        };

        public static readonly IReadOnlyList<OsuDifficultyTuningParameter> All =
            Sections.SelectMany(section => section.Parameters).ToArray();
    }
}
