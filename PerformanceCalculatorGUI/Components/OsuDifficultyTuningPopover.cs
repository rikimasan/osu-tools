// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets.Osu.Difficulty;
using osuTK;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;

namespace PerformanceCalculatorGUI.Components
{
    public partial class OsuDifficultyTuningButton : ToolbarButton, IHasPopover
    {
        protected override Anchor TooltipAnchor => Anchor.TopRight;

        public OsuDifficultyTuningButton()
        {
            TooltipMain = "Tuning";
            SetIcon(new ScreenSelectionButtonIcon());
        }

        public Popover GetPopover() => new OsuDifficultyTuningPopover();

        protected override bool OnClick(ClickEvent e)
        {
            this.ShowPopover();
            return base.OnClick(e);
        }
    }

    public partial class OsuDifficultyTuningPopover : OsuPopover
    {
        private const double tuning_min_value = 0.0;
        private const double tuning_max_value = double.MaxValue;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private OsuDifficultyTuningManager tuningManager { get; set; } = null!;

        private LimitedLabelledFractionalNumberBox aimPerformanceScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox speedPerformanceScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox accuracyPerformanceScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox flashlightPerformanceScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox totalPerformanceScaleTextBox = null!;

        private LimitedLabelledFractionalNumberBox aimSkillStrainScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox speedSkillStrainScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox flashlightSkillStrainScaleTextBox = null!;

        private LimitedLabelledFractionalNumberBox aimWideAngleBonusScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox aimAcuteAngleScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox aimSliderBonusScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox aimVelocityChangeBonusScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox aimWiggleBonusScaleTextBox = null!;

        private LimitedLabelledFractionalNumberBox flashlightMaxOpacityBonusScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox flashlightHiddenBonusScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox flashlightMinVelocityScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox flashlightSliderBonusScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox flashlightMinAngleScaleTextBox = null!;

        private LimitedLabelledNumberBox rhythmHistoryTimeMaxTextBox = null!;
        private LimitedLabelledNumberBox rhythmHistoryObjectsMaxTextBox = null!;
        private LimitedLabelledFractionalNumberBox rhythmOverallScaleTextBox = null!;
        private LimitedLabelledFractionalNumberBox rhythmRatioScaleTextBox = null!;

        private LimitedLabelledFractionalNumberBox speedSingleSpacingThresholdTextBox = null!;
        private LimitedLabelledFractionalNumberBox speedMinBonusBpmTextBox = null!;
        private LimitedLabelledFractionalNumberBox speedBalancingFactorTextBox = null!;
        private LimitedLabelledFractionalNumberBox speedDistanceScaleTextBox = null!;

        public OsuDifficultyTuningPopover()
            : base(false)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var initialTuning = tuningManager.Current.Value;

            Child = new Container
            {
                Size = new Vector2(560, 650),
                Padding = new MarginPadding { Horizontal = 16, Vertical = 10 },
                Child = new OsuScrollContainer(Direction.Vertical)
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 6f),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Font = OsuFont.Torus.With(size: 18, weight: FontWeight.SemiBold),
                                Text = "Difficulty tuning"
                            },
                            new OsuSpriteText
                            {
                                Font = new FontUsage(size: 12),
                                Text = "Applies to osu! ruleset only."
                            },
                            new OsuSpriteText
                            {
                                Margin = new MarginPadding { Top = 6f },
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                                Text = "Performance scales"
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        aimPerformanceScaleTextBox = createTuningBox("Aim perf scale", initialTuning.AimPerformanceScale),
                                        speedPerformanceScaleTextBox = createTuningBox("Speed perf scale", initialTuning.SpeedPerformanceScale)
                                    },
                                    new Drawable[]
                                    {
                                        accuracyPerformanceScaleTextBox = createTuningBox("Accuracy perf scale", initialTuning.AccuracyPerformanceScale),
                                        flashlightPerformanceScaleTextBox = createTuningBox("Flashlight perf scale", initialTuning.FlashlightPerformanceScale)
                                    },
                                    new Drawable[]
                                    {
                                        totalPerformanceScaleTextBox = createTuningBox("Total perf scale", initialTuning.TotalPerformanceScale),
                                        new Container()
                                    }
                                }
                            },
                            new OsuSpriteText
                            {
                                Margin = new MarginPadding { Top = 6f },
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                                Text = "Skill strain scales"
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        aimSkillStrainScaleTextBox = createTuningBox("Aim strain scale", initialTuning.AimSkillStrainScale),
                                        speedSkillStrainScaleTextBox = createTuningBox("Speed strain scale", initialTuning.SpeedSkillStrainScale)
                                    },
                                    new Drawable[]
                                    {
                                        flashlightSkillStrainScaleTextBox = createTuningBox("Flashlight strain scale", initialTuning.FlashlightSkillStrainScale),
                                        new Container()
                                    }
                                }
                            },
                            new OsuSpriteText
                            {
                                Margin = new MarginPadding { Top = 6f },
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                                Text = "Aim bonuses"
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        aimWideAngleBonusScaleTextBox = createTuningBox("Aim wide angle", initialTuning.AimWideAngleBonusScale),
                                        aimAcuteAngleScaleTextBox = createTuningBox("Aim acute angle", initialTuning.AimAcuteAngleScale)
                                    },
                                    new Drawable[]
                                    {
                                        aimSliderBonusScaleTextBox = createTuningBox("Aim slider bonus", initialTuning.AimSliderBonusScale),
                                        aimVelocityChangeBonusScaleTextBox = createTuningBox("Aim velocity bonus", initialTuning.AimVelocityChangeBonusScale)
                                    },
                                    new Drawable[]
                                    {
                                        aimWiggleBonusScaleTextBox = createTuningBox("Aim wiggle bonus", initialTuning.AimWiggleBonusScale),
                                        new Container()
                                    }
                                }
                            },
                            new OsuSpriteText
                            {
                                Margin = new MarginPadding { Top = 6f },
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                                Text = "Flashlight bonuses"
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        flashlightMaxOpacityBonusScaleTextBox = createTuningBox("FL max opacity", initialTuning.FlashlightMaxOpacityBonusScale),
                                        flashlightHiddenBonusScaleTextBox = createTuningBox("FL hidden bonus", initialTuning.FlashlightHiddenBonusScale)
                                    },
                                    new Drawable[]
                                    {
                                        flashlightMinVelocityScaleTextBox = createTuningBox("FL min velocity", initialTuning.FlashlightMinVelocityScale),
                                        flashlightSliderBonusScaleTextBox = createTuningBox("FL slider bonus", initialTuning.FlashlightSliderBonusScale)
                                    },
                                    new Drawable[]
                                    {
                                        flashlightMinAngleScaleTextBox = createTuningBox("FL min angle", initialTuning.FlashlightMinAngleScale),
                                        new Container()
                                    }
                                }
                            },
                            new OsuSpriteText
                            {
                                Margin = new MarginPadding { Top = 6f },
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                                Text = "Rhythm tuning"
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        rhythmHistoryTimeMaxTextBox = createTuningIntBox("Rhythm time max (ms)", initialTuning.RhythmHistoryTimeMax),
                                        rhythmHistoryObjectsMaxTextBox = createTuningIntBox("Rhythm objects max", initialTuning.RhythmHistoryObjectsMax)
                                    },
                                    new Drawable[]
                                    {
                                        rhythmOverallScaleTextBox = createTuningBox("Rhythm overall scale", initialTuning.RhythmOverallScale),
                                        rhythmRatioScaleTextBox = createTuningBox("Rhythm ratio scale", initialTuning.RhythmRatioScale)
                                    }
                                }
                            },
                            new OsuSpriteText
                            {
                                Margin = new MarginPadding { Top = 6f },
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                                Text = "Speed tuning"
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        speedSingleSpacingThresholdTextBox = createTuningBox("Speed single spacing", initialTuning.SpeedSingleSpacingThreshold),
                                        speedMinBonusBpmTextBox = createTuningBox("Speed min bonus BPM", initialTuning.SpeedMinBonusBpm)
                                    },
                                    new Drawable[]
                                    {
                                        speedBalancingFactorTextBox = createTuningBox("Speed balancing factor", initialTuning.SpeedBalancingFactor),
                                        speedDistanceScaleTextBox = createTuningBox("Speed distance scale", initialTuning.SpeedDistanceScale)
                                    }
                                }
                            },
                            new FillFlowContainer
                            {
                                Margin = new MarginPadding { Top = 10f },
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(8, 0),
                                Children = new Drawable[]
                                {
                                    new RoundedButton
                                    {
                                        Width = 120,
                                        Height = 32,
                                        BackgroundColour = colourProvider.Background3,
                                        Text = "Reset",
                                        Action = resetToDefaults
                                    },
                                    new RoundedButton
                                    {
                                        Width = 200,
                                        Height = 32,
                                        BackgroundColour = colourProvider.Background1,
                                        Text = "Apply & recalculate",
                                        Action = apply
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private void apply()
        {
            tuningManager.Current.Value = new OsuDifficultyTuning
            {
                AimPerformanceScale = aimPerformanceScaleTextBox.Value.Value,
                SpeedPerformanceScale = speedPerformanceScaleTextBox.Value.Value,
                AccuracyPerformanceScale = accuracyPerformanceScaleTextBox.Value.Value,
                FlashlightPerformanceScale = flashlightPerformanceScaleTextBox.Value.Value,
                TotalPerformanceScale = totalPerformanceScaleTextBox.Value.Value,
                AimSkillStrainScale = aimSkillStrainScaleTextBox.Value.Value,
                SpeedSkillStrainScale = speedSkillStrainScaleTextBox.Value.Value,
                FlashlightSkillStrainScale = flashlightSkillStrainScaleTextBox.Value.Value,
                AimWideAngleBonusScale = aimWideAngleBonusScaleTextBox.Value.Value,
                AimAcuteAngleScale = aimAcuteAngleScaleTextBox.Value.Value,
                AimSliderBonusScale = aimSliderBonusScaleTextBox.Value.Value,
                AimVelocityChangeBonusScale = aimVelocityChangeBonusScaleTextBox.Value.Value,
                AimWiggleBonusScale = aimWiggleBonusScaleTextBox.Value.Value,
                FlashlightMaxOpacityBonusScale = flashlightMaxOpacityBonusScaleTextBox.Value.Value,
                FlashlightHiddenBonusScale = flashlightHiddenBonusScaleTextBox.Value.Value,
                FlashlightMinVelocityScale = flashlightMinVelocityScaleTextBox.Value.Value,
                FlashlightSliderBonusScale = flashlightSliderBonusScaleTextBox.Value.Value,
                FlashlightMinAngleScale = flashlightMinAngleScaleTextBox.Value.Value,
                RhythmHistoryTimeMax = rhythmHistoryTimeMaxTextBox.Value.Value,
                RhythmHistoryObjectsMax = rhythmHistoryObjectsMaxTextBox.Value.Value,
                RhythmOverallScale = rhythmOverallScaleTextBox.Value.Value,
                RhythmRatioScale = rhythmRatioScaleTextBox.Value.Value,
                SpeedSingleSpacingThreshold = speedSingleSpacingThresholdTextBox.Value.Value,
                SpeedMinBonusBpm = speedMinBonusBpmTextBox.Value.Value,
                SpeedBalancingFactor = speedBalancingFactorTextBox.Value.Value,
                SpeedDistanceScale = speedDistanceScaleTextBox.Value.Value
            };

            this.HidePopover();
        }

        private void resetToDefaults()
        {
            var defaults = OsuDifficultyTuning.Default;

            setTuningValue(aimPerformanceScaleTextBox, defaults.AimPerformanceScale);
            setTuningValue(speedPerformanceScaleTextBox, defaults.SpeedPerformanceScale);
            setTuningValue(accuracyPerformanceScaleTextBox, defaults.AccuracyPerformanceScale);
            setTuningValue(flashlightPerformanceScaleTextBox, defaults.FlashlightPerformanceScale);
            setTuningValue(totalPerformanceScaleTextBox, defaults.TotalPerformanceScale);

            setTuningValue(aimSkillStrainScaleTextBox, defaults.AimSkillStrainScale);
            setTuningValue(speedSkillStrainScaleTextBox, defaults.SpeedSkillStrainScale);
            setTuningValue(flashlightSkillStrainScaleTextBox, defaults.FlashlightSkillStrainScale);

            setTuningValue(aimWideAngleBonusScaleTextBox, defaults.AimWideAngleBonusScale);
            setTuningValue(aimAcuteAngleScaleTextBox, defaults.AimAcuteAngleScale);
            setTuningValue(aimSliderBonusScaleTextBox, defaults.AimSliderBonusScale);
            setTuningValue(aimVelocityChangeBonusScaleTextBox, defaults.AimVelocityChangeBonusScale);
            setTuningValue(aimWiggleBonusScaleTextBox, defaults.AimWiggleBonusScale);

            setTuningValue(flashlightMaxOpacityBonusScaleTextBox, defaults.FlashlightMaxOpacityBonusScale);
            setTuningValue(flashlightHiddenBonusScaleTextBox, defaults.FlashlightHiddenBonusScale);
            setTuningValue(flashlightMinVelocityScaleTextBox, defaults.FlashlightMinVelocityScale);
            setTuningValue(flashlightSliderBonusScaleTextBox, defaults.FlashlightSliderBonusScale);
            setTuningValue(flashlightMinAngleScaleTextBox, defaults.FlashlightMinAngleScale);

            setTuningValue(rhythmHistoryTimeMaxTextBox, defaults.RhythmHistoryTimeMax);
            setTuningValue(rhythmHistoryObjectsMaxTextBox, defaults.RhythmHistoryObjectsMax);
            setTuningValue(rhythmOverallScaleTextBox, defaults.RhythmOverallScale);
            setTuningValue(rhythmRatioScaleTextBox, defaults.RhythmRatioScale);

            setTuningValue(speedSingleSpacingThresholdTextBox, defaults.SpeedSingleSpacingThreshold);
            setTuningValue(speedMinBonusBpmTextBox, defaults.SpeedMinBonusBpm);
            setTuningValue(speedBalancingFactorTextBox, defaults.SpeedBalancingFactor);
            setTuningValue(speedDistanceScaleTextBox, defaults.SpeedDistanceScale);
        }

        private LimitedLabelledFractionalNumberBox createTuningBox(string label, double defaultValue)
        {
            return new LimitedLabelledFractionalNumberBox
            {
                RelativeSizeAxes = Axes.X,
                Anchor = Anchor.TopLeft,
                Label = label,
                PlaceholderText = defaultValue.ToString(),
                MinValue = tuning_min_value,
                MaxValue = tuning_max_value,
                Value = { Value = defaultValue }
            };
        }

        private LimitedLabelledNumberBox createTuningIntBox(string label, int defaultValue)
        {
            return new LimitedLabelledNumberBox
            {
                RelativeSizeAxes = Axes.X,
                Anchor = Anchor.TopLeft,
                Label = label,
                PlaceholderText = defaultValue.ToString(),
                MinValue = 0,
                Value = { Value = defaultValue }
            };
        }

        private static void setTuningValue(LimitedLabelledFractionalNumberBox box, double value)
        {
            box.Text = string.Empty;
            box.Value.Value = value;
        }

        private static void setTuningValue(LimitedLabelledNumberBox box, int value)
        {
            box.Text = string.Empty;
            box.Value.Value = value;
        }
    }
}
