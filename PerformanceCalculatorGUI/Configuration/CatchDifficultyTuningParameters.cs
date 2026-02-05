// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Humanizer;
using osu.Game.Rulesets.Catch.Difficulty;

namespace PerformanceCalculatorGUI.Configuration
{
    public sealed class CatchDifficultyTuningParameter
    {
        public string UiLabel { get; }
        public string AutobalanceLabel { get; }
        public bool IsInteger { get; }
        public double AutobalanceMinValue { get; }
        public bool DefaultEnabled { get; }
        public Func<CatchDifficultyConstants, double> Getter { get; }
        public Func<CatchDifficultyConstants, double, CatchDifficultyConstants> Setter { get; }

        private CatchDifficultyTuningParameter(string uiLabel, string autobalanceLabel, bool isInteger, double autobalanceMinValue, bool defaultEnabled,
                                                Func<CatchDifficultyConstants, double> getter, Func<CatchDifficultyConstants, double, CatchDifficultyConstants> setter)
        {
            UiLabel = uiLabel;
            AutobalanceLabel = autobalanceLabel;
            IsInteger = isInteger;
            AutobalanceMinValue = autobalanceMinValue;
            DefaultEnabled = defaultEnabled;
            Getter = getter;
            Setter = setter;
        }

        public static CatchDifficultyTuningParameter ForDouble(string uiLabel, string autobalanceLabel, Func<CatchDifficultyConstants, double> getter,
                                                                Func<CatchDifficultyConstants, double, CatchDifficultyConstants> setter, bool defaultEnabled,
                                                                double autobalanceMinValue = 0.01)
            => new CatchDifficultyTuningParameter(uiLabel, autobalanceLabel, false, autobalanceMinValue, defaultEnabled, getter, setter);

        public static CatchDifficultyTuningParameter ForInt(string uiLabel, string autobalanceLabel, Func<CatchDifficultyConstants, int> getter,
                                                            Func<CatchDifficultyConstants, int, CatchDifficultyConstants> setter, bool defaultEnabled,
                                                            int autobalanceMinValue = 1)
            => new CatchDifficultyTuningParameter(uiLabel, autobalanceLabel, true, autobalanceMinValue, defaultEnabled,
                                                  t => getter(t), (t, v) => setter(t, (int)v));
    }

    public static class CatchDifficultyTuningParameters
    {
        public static readonly IReadOnlyList<CatchDifficultyTuningParameter> All = new[]
        {
            doubleParam(nameof(CatchDifficultyConstants.DifficultyMultiplier), t => t.DifficultyMultiplier, (t, v) => t with { DifficultyMultiplier = v }, true, 0.0001),
            doubleParam(nameof(CatchDifficultyConstants.ApproachRateSecondConstant), t => t.ApproachRateSecondConstant, (t, v) => t with { ApproachRateSecondConstant = v }),
            doubleParam(nameof(CatchDifficultyConstants.BeginningTimePenaltyPower), t => t.BeginningTimePenaltyPower, (t, v) => t with { BeginningTimePenaltyPower = v }),
            doubleParam(nameof(CatchDifficultyConstants.BeginningFullPenalty), t => t.BeginningFullPenalty, (t, v) => t with { BeginningFullPenalty = v }),
            doubleParam(nameof(CatchDifficultyConstants.SrScalerY1), t => t.SrScalerY1, (t, v) => t with { SrScalerY1 = v }),
            doubleParam(nameof(CatchDifficultyConstants.SrScalerY2), t => t.SrScalerY2, (t, v) => t with { SrScalerY2 = v }),
            doubleParam(nameof(CatchDifficultyConstants.SrScalerY3), t => t.SrScalerY3, (t, v) => t with { SrScalerY3 = v }),
            doubleParam(nameof(CatchDifficultyConstants.SrScalerY4), t => t.SrScalerY4, (t, v) => t with { SrScalerY4 = v }),
            doubleParam(nameof(CatchDifficultyConstants.SrScalerY5), t => t.SrScalerY5, (t, v) => t with { SrScalerY5 = v }),
            doubleParam(nameof(CatchDifficultyConstants.SrScalerY6), t => t.SrScalerY6, (t, v) => t with { SrScalerY6 = v }),
            doubleParam(nameof(CatchDifficultyConstants.DefaultDecayWeight), t => t.DefaultDecayWeight, (t, v) => t with { DefaultDecayWeight = v }),
            doubleParam(nameof(CatchDifficultyConstants.LocalStarRatingMaxConstant), t => t.LocalStarRatingMaxConstant, (t, v) => t with { LocalStarRatingMaxConstant = v }),
            doubleParam(nameof(CatchDifficultyConstants.LocalStarRatingMinConstant), t => t.LocalStarRatingMinConstant, (t, v) => t with { LocalStarRatingMinConstant = v }),
            doubleParam(nameof(CatchDifficultyConstants.LocalStarRatingCorrelationConstant), t => t.LocalStarRatingCorrelationConstant, (t, v) => t with { LocalStarRatingCorrelationConstant = v }),
            doubleParam(nameof(CatchDifficultyConstants.PerformanceLengthLinearPace), t => t.PerformanceLengthLinearPace, (t, v) => t with { PerformanceLengthLinearPace = v }),
            intParam(nameof(CatchDifficultyConstants.PerformanceLengthCutoff), t => t.PerformanceLengthCutoff, (t, v) => t with { PerformanceLengthCutoff = v }),
            doubleParam(nameof(CatchDifficultyConstants.PerformanceLengthLogarithmicPace), t => t.PerformanceLengthLogarithmicPace, (t, v) => t with { PerformanceLengthLogarithmicPace = v }),
            doubleParam(nameof(CatchDifficultyConstants.PerformanceValueMultiplier), t => t.PerformanceValueMultiplier, (t, v) => t with { PerformanceValueMultiplier = v }, true),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionRawWeightHyperjumps), t => t.PrecisionRawWeightHyperjumps, (t, v) => t with { PrecisionRawWeightHyperjumps = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionRawWeightHyperjumpAfterJump), t => t.PrecisionRawWeightHyperjumpAfterJump, (t, v) => t with { PrecisionRawWeightHyperjumpAfterJump = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionRawWeightJumpAfterHyperjump), t => t.PrecisionRawWeightJumpAfterHyperjump, (t, v) => t with { PrecisionRawWeightJumpAfterHyperjump = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionRawWeightJumps), t => t.PrecisionRawWeightJumps, (t, v) => t with { PrecisionRawWeightJumps = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionDelayedWeight), t => t.PrecisionDelayedWeight, (t, v) => t with { PrecisionDelayedWeight = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionStrainAmplitude), t => t.PrecisionStrainAmplitude, (t, v) => t with { PrecisionStrainAmplitude = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionStrainPace), t => t.PrecisionStrainPace, (t, v) => t with { PrecisionStrainPace = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionStrainMultiplier), t => t.PrecisionStrainMultiplier, (t, v) => t with { PrecisionStrainMultiplier = v }),
            doubleParam(nameof(CatchDifficultyConstants.MaxPrecisionCorrection), t => t.MaxPrecisionCorrection, (t, v) => t with { MaxPrecisionCorrection = v }),
            doubleParam(nameof(CatchDifficultyConstants.SpeedSnapAmplitude), t => t.SpeedSnapAmplitude, (t, v) => t with { SpeedSnapAmplitude = v }),
            doubleParam(nameof(CatchDifficultyConstants.SpeedSnapPace), t => t.SpeedSnapPace, (t, v) => t with { SpeedSnapPace = v }),
            doubleParam(nameof(CatchDifficultyConstants.SpeedSnapMultiplier), t => t.SpeedSnapMultiplier, (t, v) => t with { SpeedSnapMultiplier = v }),
            doubleParam(nameof(CatchDifficultyConstants.SpeedBurstAmplitude), t => t.SpeedBurstAmplitude, (t, v) => t with { SpeedBurstAmplitude = v }),
            doubleParam(nameof(CatchDifficultyConstants.SpeedBurstPace), t => t.SpeedBurstPace, (t, v) => t with { SpeedBurstPace = v }),
            doubleParam(nameof(CatchDifficultyConstants.SpeedBurstMultiplier), t => t.SpeedBurstMultiplier, (t, v) => t with { SpeedBurstMultiplier = v }),
            doubleParam(nameof(CatchDifficultyConstants.SpeedConsistencyAmplitude), t => t.SpeedConsistencyAmplitude, (t, v) => t with { SpeedConsistencyAmplitude = v }),
            doubleParam(nameof(CatchDifficultyConstants.SpeedConsistencyPace), t => t.SpeedConsistencyPace, (t, v) => t with { SpeedConsistencyPace = v }),
            doubleParam(nameof(CatchDifficultyConstants.SpeedConsistencyMultiplier), t => t.SpeedConsistencyMultiplier, (t, v) => t with { SpeedConsistencyMultiplier = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingHighCsPower), t => t.ReadingHighCsPower, (t, v) => t with { ReadingHighCsPower = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingHighCsRate), t => t.ReadingHighCsRate, (t, v) => t with { ReadingHighCsRate = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingHighCsPenaltyHypers), t => t.ReadingHighCsPenaltyHypers, (t, v) => t with { ReadingHighCsPenaltyHypers = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingLocalRhythmPenalty), t => t.ReadingLocalRhythmPenalty, (t, v) => t with { ReadingLocalRhythmPenalty = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingExplicitRhythmPenalty), t => t.ReadingExplicitRhythmPenalty, (t, v) => t with { ReadingExplicitRhythmPenalty = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingImplicitRhythmPenalty), t => t.ReadingImplicitRhythmPenalty, (t, v) => t with { ReadingImplicitRhythmPenalty = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingSimilarDistancePenalty), t => t.ReadingSimilarDistancePenalty, (t, v) => t with { ReadingSimilarDistancePenalty = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingAlternatingDistancePenalty), t => t.ReadingAlternatingDistancePenalty, (t, v) => t with { ReadingAlternatingDistancePenalty = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingHyperchainPenalty), t => t.ReadingHyperchainPenalty, (t, v) => t with { ReadingHyperchainPenalty = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingNonHyperchainPenalty), t => t.ReadingNonHyperchainPenalty, (t, v) => t with { ReadingNonHyperchainPenalty = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingHighVelocityNerf), t => t.ReadingHighVelocityNerf, (t, v) => t with { ReadingHighVelocityNerf = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingHighDistanceBuff), t => t.ReadingHighDistanceBuff, (t, v) => t with { ReadingHighDistanceBuff = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingFakeActionBuff), t => t.ReadingFakeActionBuff, (t, v) => t with { ReadingFakeActionBuff = v }),
            doubleParam(nameof(CatchDifficultyConstants.ReadingFuturePrecisionBuff), t => t.ReadingFuturePrecisionBuff, (t, v) => t with { ReadingFuturePrecisionBuff = v }),
            doubleParam(nameof(CatchDifficultyConstants.StandingWidthAdditiveConstant), t => t.StandingWidthAdditiveConstant, (t, v) => t with { StandingWidthAdditiveConstant = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionCorrectionDistanceExponent), t => t.PrecisionCorrectionDistanceExponent, (t, v) => t with { PrecisionCorrectionDistanceExponent = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionCorrectionTimeExponent), t => t.PrecisionCorrectionTimeExponent, (t, v) => t with { PrecisionCorrectionTimeExponent = v }),
            doubleParam(nameof(CatchDifficultyConstants.PrecisionCorrectionDistanceWeight), t => t.PrecisionCorrectionDistanceWeight, (t, v) => t with { PrecisionCorrectionDistanceWeight = v })
        };

        private static CatchDifficultyTuningParameter doubleParam(string propertyName, Func<CatchDifficultyConstants, double> getter,
                                                                   Func<CatchDifficultyConstants, double, CatchDifficultyConstants> setter,
                                                                   bool defaultEnabled = false, double minValue = 0.01)
        {
            string label = propertyName.Humanize();
            return CatchDifficultyTuningParameter.ForDouble(label, label, getter, setter, defaultEnabled, minValue);
        }

        private static CatchDifficultyTuningParameter intParam(string propertyName, Func<CatchDifficultyConstants, int> getter,
                                                                Func<CatchDifficultyConstants, int, CatchDifficultyConstants> setter,
                                                                bool defaultEnabled = false, int minValue = 1)
        {
            string label = propertyName.Humanize();
            return CatchDifficultyTuningParameter.ForInt(label, label, getter, setter, defaultEnabled, minValue);
        }
    }
}
