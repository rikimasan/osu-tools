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
        public Func<OsuDifficultyTuning, double> Getter { get; }
        public Func<OsuDifficultyTuning, double, OsuDifficultyTuning> Setter { get; }

        private OsuDifficultyTuningParameter(string uiLabel, string autobalanceLabel, bool isInteger, double autobalanceMinValue, bool defaultEnabled,
                                             Func<OsuDifficultyTuning, double> getter, Func<OsuDifficultyTuning, double, OsuDifficultyTuning> setter)
        {
            UiLabel = uiLabel;
            AutobalanceLabel = autobalanceLabel;
            IsInteger = isInteger;
            AutobalanceMinValue = autobalanceMinValue;
            DefaultEnabled = defaultEnabled;
            Getter = getter;
            Setter = setter;
        }

        public static OsuDifficultyTuningParameter ForDouble(string uiLabel, string autobalanceLabel, Func<OsuDifficultyTuning, double> getter,
                                                             Func<OsuDifficultyTuning, double, OsuDifficultyTuning> setter, bool defaultEnabled,
                                                             double autobalanceMinValue = 0.01)
            => new OsuDifficultyTuningParameter(uiLabel, autobalanceLabel, false, autobalanceMinValue, defaultEnabled, getter, setter);

        public static OsuDifficultyTuningParameter ForInt(string uiLabel, string autobalanceLabel, Func<OsuDifficultyTuning, int> getter,
                                                          Func<OsuDifficultyTuning, int, OsuDifficultyTuning> setter, bool defaultEnabled,
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
                OsuDifficultyTuningParameter.ForDouble("Total perf scale", "Total perf", t => t.TotalPerformanceScale, (t, v) => t with { TotalPerformanceScale = v }, false)
            ),
            new OsuDifficultyTuningSection("Skill strain scales",
                OsuDifficultyTuningParameter.ForDouble("Aim strain scale", "Aim strain", t => t.AimSkillStrainScale, (t, v) => t with { AimSkillStrainScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed strain scale", "Speed strain", t => t.SpeedSkillStrainScale, (t, v) => t with { SpeedSkillStrainScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flashlight strain scale", "Flashlight strain", t => t.FlashlightSkillStrainScale, (t, v) => t with { FlashlightSkillStrainScale = v }, false)
            ),
            new OsuDifficultyTuningSection("Aim bonuses",
                OsuDifficultyTuningParameter.ForDouble("Aim wide angle", "Aim wide angle", t => t.AimWideAngleBonusScale, (t, v) => t with { AimWideAngleBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim acute angle", "Aim acute angle", t => t.AimAcuteAngleScale, (t, v) => t with { AimAcuteAngleScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim slider bonus", "Aim slider bonus", t => t.AimSliderBonusScale, (t, v) => t with { AimSliderBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim velocity bonus", "Aim velocity bonus", t => t.AimVelocityChangeBonusScale, (t, v) => t with { AimVelocityChangeBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim wiggle bonus", "Aim wiggle bonus", t => t.AimWiggleBonusScale, (t, v) => t with { AimWiggleBonusScale = v }, false)
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
            new OsuDifficultyTuningSection("Speed tuning",
                OsuDifficultyTuningParameter.ForDouble("Speed single spacing", "Speed spacing", t => t.SpeedSingleSpacingThreshold, (t, v) => t with { SpeedSingleSpacingThreshold = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed min bonus BPM", "Speed min bpm", t => t.SpeedMinBonusBpm, (t, v) => t with { SpeedMinBonusBpm = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed balancing factor", "Speed balance", t => t.SpeedBalancingFactor, (t, v) => t with { SpeedBalancingFactor = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed distance scale", "Speed distance", t => t.SpeedDistanceScale, (t, v) => t with { SpeedDistanceScale = v }, true)
            )
        };

        public static readonly IReadOnlyList<OsuDifficultyTuningParameter> All =
            Sections.SelectMany(section => section.Parameters).ToArray();
    }
}
