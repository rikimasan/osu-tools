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
                OsuDifficultyTuningParameter.ForDouble("Aim perf scale", "Aim perf", t => t.AimPerformanceScale, (t, v) => t with { AimPerformanceScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed perf scale", "Speed perf", t => t.SpeedPerformanceScale, (t, v) => t with { SpeedPerformanceScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Accuracy perf scale", "Accuracy perf", t => t.AccuracyPerformanceScale, (t, v) => t with { AccuracyPerformanceScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flashlight perf scale", "Flashlight perf", t => t.FlashlightPerformanceScale, (t, v) => t with { FlashlightPerformanceScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Total perf scale", "Total perf", t => t.TotalPerformanceScale, (t, v) => t with { TotalPerformanceScale = v }, true)
            ),
            new OsuDifficultyTuningSection("Aim skill",
                OsuDifficultyTuningParameter.ForDouble("Aim skill multiplier", "Aim mult", t => t.AimSkillMultiplier, (t, v) => t with { AimSkillMultiplier = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim snap diff scale", "Aim snap diff", t => t.AimSnapDifficultyScale, (t, v) => t with { AimSnapDifficultyScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim flow diff scale", "Aim flow diff", t => t.AimFlowDifficultyScale, (t, v) => t with { AimFlowDifficultyScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim agility diff scale", "Aim agility diff", t => t.AimAgilityDifficultyScale, (t, v) => t with { AimAgilityDifficultyScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim strain decay", "Aim decay", t => t.AimStrainDecayBase, (t, v) => t with { AimStrainDecayBase = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim agility decay", "Aim agility decay", t => t.AimAgilityStrainDecayBase, (t, v) => t with { AimAgilityStrainDecayBase = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim backwards strain", "Aim back strain", t => t.AimBackwardsStrainInfluence, (t, v) => t with { AimBackwardsStrainInfluence = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim flow strain mult", "Aim flow strain", t => t.AimFlowStrainMultiplier, (t, v) => t with { AimFlowStrainMultiplier = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim snap strain mult", "Aim snap strain", t => t.AimSnapStrainMultiplier, (t, v) => t with { AimSnapStrainMultiplier = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim flow transition", "Aim flow trans", t => t.AimFlowTransitionScale, (t, v) => t with { AimFlowTransitionScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Aim snap transition", "Aim snap trans", t => t.AimSnapTransitionScale, (t, v) => t with { AimSnapTransitionScale = v }, true)
            ),
            new OsuDifficultyTuningSection("Flow tuning",
                OsuDifficultyTuningParameter.ForDouble("Flow distance exponent", "Flow dist exp", t => t.FlowDistanceExponent, (t, v) => t with { FlowDistanceExponent = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow jerk scale", "Flow jerk", t => t.FlowJerkScale, (t, v) => t with { FlowJerkScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow angular velocity", "Flow ang vel", t => t.FlowAngularVelocityScale, (t, v) => t with { FlowAngularVelocityScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow velocity change", "Flow vel change", t => t.FlowVelocityChangeScale, (t, v) => t with { FlowVelocityChangeScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow slider bonus", "Flow slider", t => t.FlowSliderBonusScale, (t, v) => t with { FlowSliderBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow overall scale", "Flow overall", t => t.FlowOverallScale, (t, v) => t with { FlowOverallScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow max angle (rad)", "Flow max angle", t => t.FlowMaxAngleRadians, (t, v) => t with { FlowMaxAngleRadians = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow overlap nerf max", "Flow overlap max", t => t.FlowOverlapNerfMax, (t, v) => t with { FlowOverlapNerfMax = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow velocity bonus", "Flow vel bonus", t => t.FlowVelocityBonusScale, (t, v) => t with { FlowVelocityBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow velocity exp", "Flow vel exp", t => t.FlowVelocityBonusExponent, (t, v) => t with { FlowVelocityBonusExponent = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow jerk dist threshold", "Flow jerk dist th", t => t.FlowJerkDistanceThreshold, (t, v) => t with { FlowJerkDistanceThreshold = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Flow jerk dist scale", "Flow jerk dist", t => t.FlowJerkDistanceScale, (t, v) => t with { FlowJerkDistanceScale = v }, true)
            ),
            new OsuDifficultyTuningSection("Snap tuning",
                OsuDifficultyTuningParameter.ForDouble("Snap velocity bonus", "Snap vel bonus", t => t.SnapVelocityChangeBonusScale, (t, v) => t with { SnapVelocityChangeBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap velocity penalty exp", "Snap vel pen exp", t => t.SnapVelocityChangePenaltyExponent, (t, v) => t with { SnapVelocityChangePenaltyExponent = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap slider bonus", "Snap slider", t => t.SnapSliderBonusScale, (t, v) => t with { SnapSliderBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap wide angle bonus", "Snap wide angle", t => t.SnapWideAngleBonusScale, (t, v) => t with { SnapWideAngleBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap angle repeat base", "Snap repeat base", t => t.SnapAngleRepeatBaseScale, (t, v) => t with { SnapAngleRepeatBaseScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap angle repeat vector", "Snap repeat vec", t => t.SnapAngleRepeatVectorScale, (t, v) => t with { SnapAngleRepeatVectorScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap angle repeat exp", "Snap repeat exp", t => t.SnapAngleRepeatExponent, (t, v) => t with { SnapAngleRepeatExponent = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap wide angle time exp", "Snap wide time", t => t.SnapWideAngleTimeExponent, (t, v) => t with { SnapWideAngleTimeExponent = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap back-and-forth scale", "Snap back forth", t => t.SnapWideAngleBackAndForthScale, (t, v) => t with { SnapWideAngleBackAndForthScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap overlap velocity", "Snap overlap", t => t.SnapOverlapVelocityScale, (t, v) => t with { SnapOverlapVelocityScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap wide angle min (rad)", "Snap wide min", t => t.SnapWideAngleMinRadians, (t, v) => t with { SnapWideAngleMinRadians = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap wide angle max (rad)", "Snap wide max", t => t.SnapWideAngleMaxRadians, (t, v) => t with { SnapWideAngleMaxRadians = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap repeat high (rad)", "Snap repeat high", t => t.SnapAngleRepeatSmootherstepHighRadians, (t, v) => t with { SnapAngleRepeatSmootherstepHighRadians = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Snap repeat low (rad)", "Snap repeat low", t => t.SnapAngleRepeatSmootherstepLowRadians, (t, v) => t with { SnapAngleRepeatSmootherstepLowRadians = v }, true)
            ),
            new OsuDifficultyTuningSection("Agility tuning",
                OsuDifficultyTuningParameter.ForDouble("Agility base BPM", "Agility bpm", t => t.AgilityBaseBpm, (t, v) => t with { AgilityBaseBpm = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Agility exponent", "Agility exp", t => t.AgilityExponent, (t, v) => t with { AgilityExponent = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Agility cheesability time", "Agility cheese", t => t.AgilityCheesabilityTimeScale, (t, v) => t with { AgilityCheesabilityTimeScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Agility angle bonus", "Agility angle", t => t.AgilityAngleBonusScale, (t, v) => t with { AgilityAngleBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Agility angle repeat base", "Agility repeat base", t => t.AgilityAngleRepeatBaseScale, (t, v) => t with { AgilityAngleRepeatBaseScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Agility angle repeat vector", "Agility repeat vec", t => t.AgilityAngleRepeatVectorScale, (t, v) => t with { AgilityAngleRepeatVectorScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Agility angle repeat exp", "Agility repeat exp", t => t.AgilityAngleRepeatExponent, (t, v) => t with { AgilityAngleRepeatExponent = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Agility overall scale", "Agility overall", t => t.AgilityOverallScale, (t, v) => t with { AgilityOverallScale = v }, true)
            ),
            new OsuDifficultyTuningSection("Flashlight tuning",
                OsuDifficultyTuningParameter.ForDouble("Flashlight skill mult", "Flashlight mult", t => t.FlashlightSkillMultiplier, (t, v) => t with { FlashlightSkillMultiplier = v }, false),
                OsuDifficultyTuningParameter.ForDouble("Flashlight strain decay", "Flashlight decay", t => t.FlashlightStrainDecayBase, (t, v) => t with { FlashlightStrainDecayBase = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL max opacity bonus", "Flashlight opacity", t => t.FlashlightMaxOpacityBonusScale, (t, v) => t with { FlashlightMaxOpacityBonusScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL hidden bonus", "Flashlight hidden", t => t.FlashlightHiddenBonusScale, (t, v) => t with { FlashlightHiddenBonusScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL min velocity", "Flashlight min vel", t => t.FlashlightMinVelocityScale, (t, v) => t with { FlashlightMinVelocityScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL slider bonus", "Flashlight slider", t => t.FlashlightSliderBonusScale, (t, v) => t with { FlashlightSliderBonusScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL min angle", "Flashlight min angle", t => t.FlashlightMinAngleScale, (t, v) => t with { FlashlightMinAngleScale = v }, false),
                OsuDifficultyTuningParameter.ForInt("FL history length", "Flashlight history", t => t.FlashlightHistoryLength, (t, v) => t with { FlashlightHistoryLength = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL small distance nerf", "Flashlight small dist", t => t.FlashlightSmallDistanceNerfDistance, (t, v) => t with { FlashlightSmallDistanceNerfDistance = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL stack nerf distance", "Flashlight stack dist", t => t.FlashlightStackNerfDistance, (t, v) => t with { FlashlightStackNerfDistance = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL result exponent", "Flashlight result exp", t => t.FlashlightResultExponent, (t, v) => t with { FlashlightResultExponent = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL angle repeat threshold", "Flashlight repeat th", t => t.FlashlightAngleRepeatThreshold, (t, v) => t with { FlashlightAngleRepeatThreshold = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL angle repeat decay", "Flashlight repeat decay", t => t.FlashlightAngleRepeatDecayScale, (t, v) => t with { FlashlightAngleRepeatDecayScale = v }, false),
                OsuDifficultyTuningParameter.ForDouble("FL slider velocity exp", "Flashlight slider exp", t => t.FlashlightSliderVelocityExponent, (t, v) => t with { FlashlightSliderVelocityExponent = v }, false)
            ),
            new OsuDifficultyTuningSection("Rhythm tuning",
                OsuDifficultyTuningParameter.ForInt("Rhythm time max (ms)", "Rhythm history ms", t => t.RhythmHistoryTimeMax, (t, v) => t with { RhythmHistoryTimeMax = v }, true),
                OsuDifficultyTuningParameter.ForInt("Rhythm objects max", "Rhythm history objs", t => t.RhythmHistoryObjectsMax, (t, v) => t with { RhythmHistoryObjectsMax = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm overall scale", "Rhythm overall", t => t.RhythmOverallScale, (t, v) => t with { RhythmOverallScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm ratio scale", "Rhythm ratio", t => t.RhythmRatioScale, (t, v) => t with { RhythmRatioScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm delta epsilon", "Rhythm delta eps", t => t.RhythmDeltaDifferenceEpsilonMultiplier, (t, v) => t with { RhythmDeltaDifferenceEpsilonMultiplier = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm ratio cap", "Rhythm ratio cap", t => t.RhythmRatioCap, (t, v) => t with { RhythmRatioCap = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm diff mult base", "Rhythm diff base", t => t.RhythmDifferenceMultiplierBase, (t, v) => t with { RhythmDifferenceMultiplierBase = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm diff mult scale", "Rhythm diff scale", t => t.RhythmDifferenceMultiplierScale, (t, v) => t with { RhythmDifferenceMultiplierScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm slider change", "Rhythm slider", t => t.RhythmSliderChangeScale, (t, v) => t with { RhythmSliderChangeScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm prev slider", "Rhythm prev slider", t => t.RhythmPrevSliderChangeScale, (t, v) => t with { RhythmPrevSliderChangeScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm repeated polarity", "Rhythm polarity", t => t.RhythmRepeatedPolarityScale, (t, v) => t with { RhythmRepeatedPolarityScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm speed up/down", "Rhythm speed up", t => t.RhythmSpeedUpSlowDownScale, (t, v) => t with { RhythmSpeedUpSlowDownScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm repeated island", "Rhythm island", t => t.RhythmRepeatedIslandScale, (t, v) => t with { RhythmRepeatedIslandScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm island repeat limit", "Rhythm island limit", t => t.RhythmIslandRepeatLimit, (t, v) => t with { RhythmIslandRepeatLimit = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm island power max", "Rhythm island max", t => t.RhythmIslandPowerMaxValue, (t, v) => t with { RhythmIslandPowerMaxValue = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm island power mult", "Rhythm island mult", t => t.RhythmIslandPowerMultiplier, (t, v) => t with { RhythmIslandPowerMultiplier = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm island midpoint", "Rhythm island mid", t => t.RhythmIslandPowerMidpointOffset, (t, v) => t with { RhythmIslandPowerMidpointOffset = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm doubletap scale", "Rhythm doubletap", t => t.RhythmDoubletapnessScale, (t, v) => t with { RhythmDoubletapnessScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Rhythm speed up slider", "Rhythm speed slider", t => t.RhythmSpeedUpSliderScale, (t, v) => t with { RhythmSpeedUpSliderScale = v }, true)
            ),
            new OsuDifficultyTuningSection("Speed tuning",
                OsuDifficultyTuningParameter.ForDouble("Speed skill mult", "Speed mult", t => t.SpeedSkillMultiplier, (t, v) => t with { SpeedSkillMultiplier = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed strain decay", "Speed decay", t => t.SpeedStrainDecayBase, (t, v) => t with { SpeedStrainDecayBase = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed min bonus BPM", "Speed min bpm", t => t.SpeedMinBonusBpm, (t, v) => t with { SpeedMinBonusBpm = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed balancing factor", "Speed balance", t => t.SpeedBalancingFactor, (t, v) => t with { SpeedBalancingFactor = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed bonus scale", "Speed bonus", t => t.SpeedBonusScale, (t, v) => t with { SpeedBonusScale = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed bonus exponent", "Speed bonus exp", t => t.SpeedBonusExponent, (t, v) => t with { SpeedBonusExponent = v }, true),
                OsuDifficultyTuningParameter.ForDouble("Speed base scale", "Speed base", t => t.SpeedBaseScale, (t, v) => t with { SpeedBaseScale = v }, true)
            )
        };

        public static readonly IReadOnlyList<OsuDifficultyTuningParameter> All =
            Sections.SelectMany(section => section.Parameters).ToArray();
    }
}
