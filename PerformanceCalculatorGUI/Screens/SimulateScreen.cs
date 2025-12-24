// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play.HUD;
using osu.Game.Utils;
using osuTK;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;
using PerformanceCalculatorGUI.Screens.ObjectInspection;
using PerformanceCalculatorGUI.Screens.Simulate;

namespace PerformanceCalculatorGUI.Screens
{
    public partial class SimulateScreen : PerformanceCalculatorScreen
    {
        private ProcessorWorkingBeatmap? working;

        private ExtendedUserModSelectOverlay userModsSelectOverlay = null!;

        private GridContainer beatmapImportContainer = null!;
        private LabelledTextBox beatmapFileTextBox = null!;
        private LabelledTextBox beatmapIdTextBox = null!;
        private SwitchButton beatmapImportTypeSwitch = null!;

        private GridContainer missesContainer = null!;
        private LimitedLabelledNumberBox missesTextBox = null!;
        private LimitedLabelledNumberBox largeTickMissesTextBox = null!;
        private LimitedLabelledNumberBox sliderTailMissesTextBox = null!;
        private LimitedLabelledNumberBox comboTextBox = null!;
        private LimitedLabelledNumberBox scoreTextBox = null!;

        private LabelledNumberBox scoreIdTextBox = null!;
        private StatefulButton scoreIdPopulateButton = null!;

        private GridContainer accuracyContainer = null!;
        private LimitedLabelledFractionalNumberBox accuracyTextBox = null!;
        private LimitedLabelledNumberBox goodsTextBox = null!;
        private LimitedLabelledNumberBox mehsTextBox = null!;
        private SwitchButton fullScoreDataSwitch = null!;

        private FillFlowContainer osuTuningContainer = null!;
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

        private DifficultyAttributes? difficultyAttributes;
        private AttributesTable difficultyAttributesContainer = null!;

        private PerformanceCalculator? performanceCalculator;
        private AttributesTable performanceAttributesContainer = null!;

        [Cached]
        private Bindable<DifficultyCalculator?> difficultyCalculator = new Bindable<DifficultyCalculator?>();

        private FillFlowContainer beatmapDataContainer = null!;
        private Container beatmapTitle = null!;

        private ModDisplay modDisplay = null!;

        private StrainVisualizer strainVisualizer = null!;

        private ObjectInspector? objectInspector;

        private BufferedContainer? background;

        private OsuDifficultyTuning osuDifficultyTuning = OsuDifficultyTuning.Default;

        private ScheduledDelegate? debouncedPerformanceUpdate;
        private ScheduledDelegate? debouncedTuningUpdate;

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; } = null!;

        [Resolved]
        private AudioManager audio { get; set; } = null!;

        [Resolved]
        private Bindable<IReadOnlyList<Mod>> appliedMods { get; set; } = null!;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        private LargeTextureStore textures { get; set; } = null!;

        [Resolved]
        private SettingsManager configManager { get; set; } = null!;

        [Resolved]
        private APIManager apiManager { get; set; } = null!;

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        public override bool ShouldShowConfirmationDialogOnSwitch => working != null;

        [GeneratedRegex(@"osu\.ppy\.sh/(?:b|beatmapsets/\d+#\w+|beatmaps)/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private partial Regex beatmapLinkRegex();

        private int? queuedBeatmap;
        private ulong? queuedScore;

        private const int file_selection_container_height = 40;
        private const int map_title_container_height = 40;
        private const float mod_selection_container_scale = 0.7f;
        private const double tuning_min_value = 0.0;
        private const double tuning_max_value = double.MaxValue;

        public SimulateScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        public SimulateScreen(int beatmapId, ulong? scoreId = null)
        {
            RelativeSizeAxes = Axes.Both;
            queuedBeatmap = beatmapId;
            queuedScore = scoreId;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour osuColour)
        {
            var defaultTuning = OsuDifficultyTuning.Default;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background6,
                    Alpha = 0.85f
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension() },
                    RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, file_selection_container_height), new Dimension(GridSizeMode.Absolute, map_title_container_height), new Dimension() },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            beatmapImportContainer = new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.Absolute),
                                    new Dimension(),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        beatmapFileTextBox = new FileChooserLabelledTextBox(configManager.GetBindable<string>(Settings.DefaultPath), ".osu")
                                        {
                                            Label = "Beatmap File",
                                            FixedLabelWidth = 100f,
                                            PlaceholderText = "Click to select a beatmap file"
                                        },
                                        beatmapIdTextBox = new ExtendedLabelledTextBox
                                        {
                                            Label = "Beatmap ID",
                                            FixedLabelWidth = 100f,
                                            PlaceholderText = "Enter a beatmap ID or link",
                                            CommitOnFocusLoss = false,
                                            SelectAllOnFocus = true
                                        },
                                        beatmapImportTypeSwitch = new SwitchButton
                                        {
                                            Width = 80,
                                            Height = file_selection_container_height
                                        }
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            beatmapTitle = new Container
                            {
                                Name = "Beatmap title",
                                RelativeSizeAxes = Axes.Both
                            }
                        },
                        new Drawable[]
                        {
                            beatmapDataContainer = new FillFlowContainer
                            {
                                Name = "Beatmap data",
                                RelativeSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Children = new Drawable[]
                                {
                                    new OsuScrollContainer(Direction.Vertical)
                                    {
                                        Name = "Score params",
                                        RelativeSizeAxes = Axes.Both,
                                        Width = 0.5f,
                                        Child = new FillFlowContainer
                                        {
                                            Padding = new MarginPadding { Left = 10f, Right = 15.0f, Vertical = 5f },
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 2f),
                                            Children = new Drawable[]
                                            {
                                                new OsuSpriteText
                                                {
                                                    Margin = new MarginPadding { Left = 10f, Top = 5f, Bottom = 10.0f },
                                                    Origin = Anchor.TopLeft,
                                                    Height = 20,
                                                    Text = "Score params"
                                                },
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Horizontal,
                                                    Children = new Drawable[]
                                                    {
                                                        scoreIdTextBox = new LabelledNumberBox
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Width = 0.7f,
                                                            Label = "Score ID",
                                                            PlaceholderText = "0",
                                                        },
                                                        scoreIdPopulateButton = new StatefulButton("Populate from score")
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Width = 0.3f,
                                                            Action = () =>
                                                            {
                                                                if (!string.IsNullOrEmpty(scoreIdTextBox.Current.Value))
                                                                {
                                                                    populateSettingsFromScore(ulong.Parse(scoreIdTextBox.Current.Value));
                                                                }
                                                                else
                                                                {
                                                                    notificationDisplay.Display(new Notification("Incorrect score id"));
                                                                }
                                                            }
                                                        }
                                                    }
                                                },
                                                accuracyContainer = new GridContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    ColumnDimensions = new[]
                                                    {
                                                        new Dimension(),
                                                        new Dimension(GridSizeMode.Absolute),
                                                        new Dimension(GridSizeMode.Absolute),
                                                        new Dimension(GridSizeMode.AutoSize)
                                                    },
                                                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                    Content = new[]
                                                    {
                                                        new Drawable[]
                                                        {
                                                            accuracyTextBox = new LimitedLabelledFractionalNumberBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Anchor = Anchor.TopLeft,
                                                                Label = "Accuracy",
                                                                PlaceholderText = "100",
                                                                MaxValue = 100.0,
                                                                MinValue = 0.0,
                                                                Value = { Value = 100.0 }
                                                            },
                                                            goodsTextBox = new LimitedLabelledNumberBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Anchor = Anchor.TopLeft,
                                                                Label = "Goods",
                                                                PlaceholderText = "0",
                                                                MinValue = 0
                                                            },
                                                            mehsTextBox = new LimitedLabelledNumberBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Anchor = Anchor.TopLeft,
                                                                Label = "Mehs",
                                                                PlaceholderText = "0",
                                                                MinValue = 0
                                                            },
                                                            fullScoreDataSwitch = new SwitchButton
                                                            {
                                                                Width = 80,
                                                                Height = 40
                                                            }
                                                        }
                                                    }
                                                },
                                                comboTextBox = new LimitedLabelledNumberBox
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Anchor = Anchor.TopLeft,
                                                    Label = "Combo",
                                                    PlaceholderText = "0",
                                                    MinValue = 0
                                                },
                                                missesContainer = new GridContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    ColumnDimensions = new[]
                                                    {
                                                        new Dimension(),
                                                        new Dimension(),
                                                        new Dimension()
                                                    },
                                                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                    Content = new[]
                                                    {
                                                        new Drawable[]
                                                        {
                                                            missesTextBox = new LimitedLabelledNumberBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Anchor = Anchor.TopLeft,
                                                                Label = "Misses",
                                                                PlaceholderText = "0",
                                                                MinValue = 0
                                                            },
                                                            largeTickMissesTextBox = new LimitedLabelledNumberBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Anchor = Anchor.TopLeft,
                                                                Label = "Large Tick Misses",
                                                                PlaceholderText = "0",
                                                                MinValue = 0
                                                            },
                                                            sliderTailMissesTextBox = new LimitedLabelledNumberBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Anchor = Anchor.TopLeft,
                                                                Label = "Slider Tail Misses",
                                                                PlaceholderText = "0",
                                                                MinValue = 0
                                                            }
                                                        }
                                                    }
                                                },
                                                scoreTextBox = new LimitedLabelledNumberBox
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Anchor = Anchor.TopLeft,
                                                    Label = "Score",
                                                    PlaceholderText = "1000000",
                                                    MinValue = 0,
                                                    MaxValue = 1000000,
                                                    Value = { Value = 1000000 }
                                                },
                                                new OsuSpriteText
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Anchor = Anchor.TopLeft,
                                                    Font = new FontUsage(size: 14.0f),
                                                    Colour = osuColour.Yellow,
                                                    Text = "Don't forget to enable CL (classic) mod for osu!stable score simulation!"
                                                },
                                                new FillFlowContainer
                                                {
                                                    Name = "Mods container",
                                                    Height = 40,
                                                    Direction = FillDirection.Horizontal,
                                                    RelativeSizeAxes = Axes.X,
                                                    Anchor = Anchor.TopLeft,
                                                    AutoSizeAxes = Axes.Y,
                                                    Children = new Drawable[]
                                                    {
                                                        new RoundedButton
                                                        {
                                                            Width = 100,
                                                            Margin = new MarginPadding { Top = 4.0f, Right = 5.0f },
                                                            Action = () => { userModsSelectOverlay.Show(); },
                                                            BackgroundColour = colourProvider.Background1,
                                                            Text = "Mods"
                                                        },
                                                        modDisplay = new ModDisplay()
                                                    }
                                                },
                                                userModsSelectOverlay = new ExtendedUserModSelectOverlay
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Height = 460 / mod_selection_container_scale,
                                                    Width = 1f / mod_selection_container_scale,
                                                    Scale = new Vector2(mod_selection_container_scale),
                                                    IsValidMod = mod => mod.HasImplementation && ModUtils.FlattenMod(mod).All(m => m.UserPlayable),
                                                    SelectedMods = { BindTarget = appliedMods }
                                                },
                                                osuTuningContainer = new FillFlowContainer
                                                {
                                                    Name = "Osu tuning",
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(0, 4f),
                                                    Children = new Drawable[]
                                                    {
                                                        new OsuSpriteText
                                                        {
                                                            Margin = new MarginPadding { Left = 10f, Top = 10f, Bottom = 6f },
                                                            Origin = Anchor.TopLeft,
                                                            Height = 20,
                                                            Text = "osu! tuning"
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Font = new FontUsage(size: 14.0f),
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
                                                                    aimPerformanceScaleTextBox = createTuningBox("Aim perf scale", defaultTuning.AimPerformanceScale),
                                                                    speedPerformanceScaleTextBox = createTuningBox("Speed perf scale", defaultTuning.SpeedPerformanceScale)
                                                                },
                                                                new Drawable[]
                                                                {
                                                                    accuracyPerformanceScaleTextBox = createTuningBox("Accuracy perf scale", defaultTuning.AccuracyPerformanceScale),
                                                                    flashlightPerformanceScaleTextBox = createTuningBox("Flashlight perf scale", defaultTuning.FlashlightPerformanceScale)
                                                                },
                                                                new Drawable[]
                                                                {
                                                                    totalPerformanceScaleTextBox = createTuningBox("Total perf scale", defaultTuning.TotalPerformanceScale),
                                                                    new Container()
                                                                }
                                                            }
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Margin = new MarginPadding { Top = 6f },
                                                            Font = new FontUsage(size: 14.0f),
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
                                                                    aimSkillStrainScaleTextBox = createTuningBox("Aim strain scale", defaultTuning.AimSkillStrainScale),
                                                                    speedSkillStrainScaleTextBox = createTuningBox("Speed strain scale", defaultTuning.SpeedSkillStrainScale)
                                                                },
                                                                new Drawable[]
                                                                {
                                                                    flashlightSkillStrainScaleTextBox = createTuningBox("Flashlight strain scale", defaultTuning.FlashlightSkillStrainScale),
                                                                    new Container()
                                                                }
                                                            }
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Margin = new MarginPadding { Top = 6f },
                                                            Font = new FontUsage(size: 14.0f),
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
                                                                    aimWideAngleBonusScaleTextBox = createTuningBox("Aim wide angle", defaultTuning.AimWideAngleBonusScale),
                                                                    aimAcuteAngleScaleTextBox = createTuningBox("Aim acute angle", defaultTuning.AimAcuteAngleScale)
                                                                },
                                                                new Drawable[]
                                                                {
                                                                    aimSliderBonusScaleTextBox = createTuningBox("Aim slider bonus", defaultTuning.AimSliderBonusScale),
                                                                    aimVelocityChangeBonusScaleTextBox = createTuningBox("Aim velocity bonus", defaultTuning.AimVelocityChangeBonusScale)
                                                                },
                                                                new Drawable[]
                                                                {
                                                                    aimWiggleBonusScaleTextBox = createTuningBox("Aim wiggle bonus", defaultTuning.AimWiggleBonusScale),
                                                                    new Container()
                                                                }
                                                            }
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Margin = new MarginPadding { Top = 6f },
                                                            Font = new FontUsage(size: 14.0f),
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
                                                                    flashlightMaxOpacityBonusScaleTextBox = createTuningBox("FL max opacity", defaultTuning.FlashlightMaxOpacityBonusScale),
                                                                    flashlightHiddenBonusScaleTextBox = createTuningBox("FL hidden bonus", defaultTuning.FlashlightHiddenBonusScale)
                                                                },
                                                                new Drawable[]
                                                                {
                                                                    flashlightMinVelocityScaleTextBox = createTuningBox("FL min velocity", defaultTuning.FlashlightMinVelocityScale),
                                                                    flashlightSliderBonusScaleTextBox = createTuningBox("FL slider bonus", defaultTuning.FlashlightSliderBonusScale)
                                                                },
                                                                new Drawable[]
                                                                {
                                                                    flashlightMinAngleScaleTextBox = createTuningBox("FL min angle", defaultTuning.FlashlightMinAngleScale),
                                                                    new Container()
                                                                }
                                                            }
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Margin = new MarginPadding { Top = 6f },
                                                            Font = new FontUsage(size: 14.0f),
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
                                                                    rhythmHistoryTimeMaxTextBox = createTuningIntBox("Rhythm time max (ms)", defaultTuning.RhythmHistoryTimeMax),
                                                                    rhythmHistoryObjectsMaxTextBox = createTuningIntBox("Rhythm objects max", defaultTuning.RhythmHistoryObjectsMax)
                                                                },
                                                                new Drawable[]
                                                                {
                                                                    rhythmOverallScaleTextBox = createTuningBox("Rhythm overall scale", defaultTuning.RhythmOverallScale),
                                                                    rhythmRatioScaleTextBox = createTuningBox("Rhythm ratio scale", defaultTuning.RhythmRatioScale)
                                                                }
                                                            }
                                                        },
                                                        new OsuSpriteText
                                                        {
                                                            Margin = new MarginPadding { Top = 6f },
                                                            Font = new FontUsage(size: 14.0f),
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
                                                                    speedSingleSpacingThresholdTextBox = createTuningBox("Speed single spacing", defaultTuning.SpeedSingleSpacingThreshold),
                                                                    speedMinBonusBpmTextBox = createTuningBox("Speed min bonus BPM", defaultTuning.SpeedMinBonusBpm)
                                                                },
                                                                new Drawable[]
                                                                {
                                                                    speedBalancingFactorTextBox = createTuningBox("Speed balancing factor", defaultTuning.SpeedBalancingFactor),
                                                                    speedDistanceScaleTextBox = createTuningBox("Speed distance scale", defaultTuning.SpeedDistanceScale)
                                                                }
                                                            }
                                                        },
                                                        new RoundedButton
                                                        {
                                                            Anchor = Anchor.TopCentre,
                                                            Origin = Anchor.TopCentre,
                                                            Width = 170,
                                                            Height = 35,
                                                            BackgroundColour = colourProvider.Background1,
                                                            Text = "Reset tuning",
                                                            Action = resetOsuDifficultyTuning
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    new OsuScrollContainer(Direction.Vertical)
                                    {
                                        Name = "Difficulty calculation results",
                                        RelativeSizeAxes = Axes.Both,
                                        Width = 0.5f,
                                        Child = new FillFlowContainer
                                        {
                                            Padding = new MarginPadding { Left = 10f, Right = 15.0f, Vertical = 5f },
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 5f),
                                            Children = new Drawable[]
                                            {
                                                new OsuSpriteText
                                                {
                                                    Margin = new MarginPadding { Left = 10f, Vertical = 5f },
                                                    Origin = Anchor.TopLeft,
                                                    Height = 20,
                                                    Text = "Difficulty Attributes"
                                                },
                                                difficultyAttributesContainer = new AttributesTable(),
                                                new OsuSpriteText
                                                {
                                                    Margin = new MarginPadding { Left = 10f, Vertical = 5f },
                                                    Origin = Anchor.TopLeft,
                                                    Height = 20,
                                                    Text = "Performance Attributes"
                                                },
                                                performanceAttributesContainer = new AttributesTable(),
                                                new OsuSpriteText
                                                {
                                                    Margin = new MarginPadding { Left = 10f, Vertical = 5f },
                                                    Origin = Anchor.TopLeft,
                                                    Height = 20,
                                                    Text = "Strain graph (alt+scroll to zoom)"
                                                },
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Anchor = Anchor.TopLeft,
                                                    AutoSizeAxes = Axes.Y,
                                                    Child = strainVisualizer = new StrainVisualizer()
                                                },
                                                new RoundedButton
                                                {
                                                    Anchor = Anchor.TopCentre,
                                                    Origin = Anchor.TopCentre,
                                                    Width = 250,
                                                    BackgroundColour = colourProvider.Background1,
                                                    Text = "Inspect Object Difficulty Data",
                                                    Action = () =>
                                                    {
                                                        if (objectInspector is not null)
                                                            RemoveInternal(objectInspector, true);

                                                        if (working != null)
                                                        {
                                                            AddInternal(objectInspector = new ObjectInspector(working)
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                                Anchor = Anchor.Centre,
                                                                Origin = Anchor.Centre,
                                                                Size = new Vector2(0.95f)
                                                            });
                                                            objectInspector.Show();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            beatmapDataContainer.Hide();
            userModsSelectOverlay.Hide();
            osuTuningContainer.Hide();

            beatmapFileTextBox.Current.BindValueChanged(filePath => { changeBeatmap(filePath.NewValue); });
            beatmapIdTextBox.OnCommit += (_, _) => { changeBeatmap(beatmapIdTextBox.Current.Value); };

            beatmapImportTypeSwitch.Current.BindValueChanged(val =>
            {
                if (val.NewValue)
                {
                    beatmapImportContainer.ColumnDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute),
                        new Dimension(GridSizeMode.AutoSize)
                    };
                }
                else
                {
                    beatmapImportContainer.ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute),
                        new Dimension(),
                        new Dimension(GridSizeMode.AutoSize)
                    };
                }
            });

            accuracyTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            goodsTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            mehsTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            missesTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            largeTickMissesTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            sliderTailMissesTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            comboTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            scoreTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            bindTuningEvents();

            fullScoreDataSwitch.Current.BindValueChanged(val => updateAccuracyParams(val.NewValue));

            appliedMods.BindValueChanged(modsChanged);
            modDisplay.Current.BindTo(appliedMods);

            ruleset.BindValueChanged(_ =>
            {
                resetCalculations();
            });

            if (RuntimeInfo.IsDesktop)
            {
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(() =>
                {
                    calculateDifficulty();
                    calculatePerformance();
                });
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (queuedScore != null)
            {
                populateSettingsFromScore(queuedScore.Value);
                scoreIdTextBox.Text = queuedScore.Value.ToString();
            }
            else if (queuedBeatmap != null)
            {
                changeBeatmap(queuedBeatmap.Value.ToString());
                beatmapIdTextBox.Text = queuedBeatmap.Value.ToString();
            }

            queuedScore = null;
            queuedBeatmap = null;
        }

        protected override void Dispose(bool isDisposing)
        {
            modSettingChangeTracker?.Dispose();

            appliedMods.UnbindAll();
            appliedMods.Value = Array.Empty<Mod>();

            difficultyCalculator.UnbindAll();
            base.Dispose(isDisposing);
        }

        private ModSettingChangeTracker? modSettingChangeTracker;
        private ScheduledDelegate? debouncedStatisticsUpdate;

        private void modsChanged(ValueChangedEvent<IReadOnlyList<Mod>> mods)
        {
            // Hotfix for preventing a difficulty and performance calculation from being trigger twice,
            // as the mod overlay for some reason triggers a ValueChanged twice per mod change.
            if (mods.OldValue.SequenceEqual(mods.NewValue))
                return;

            modSettingChangeTracker?.Dispose();

            if (working is null)
                return;

            updateMissesTextboxes();

            // recreate calculators to update DHOs
            createCalculators();

            modSettingChangeTracker?.Dispose();
            modSettingChangeTracker = new ModSettingChangeTracker(mods.NewValue);
            modSettingChangeTracker.SettingChanged += m =>
            {
                debouncedStatisticsUpdate?.Cancel();
                debouncedStatisticsUpdate = Scheduler.AddDelayed(() =>
                {
                    createCalculators();
                    updateMissesTextboxes();
                    calculateDifficulty();
                    calculatePerformance();
                }, 300);
            };

            calculateDifficulty();
            updateCombo(false);
            calculatePerformance();
        }

        private void resetBeatmap()
        {
            working = null;
            beatmapTitle.Clear();
            resetMods();
            beatmapDataContainer.Hide();

            if (background is not null)
            {
                RemoveInternal(background, true);
            }
        }

        private void changeBeatmap(string beatmap)
        {
            beatmapDataContainer.Hide();

            if (string.IsNullOrEmpty(beatmap))
            {
                showError("Empty beatmap path!");
                resetBeatmap();
                return;
            }

            var beatmapLinkMatch = beatmapLinkRegex().Match(beatmap);

            if (beatmapLinkMatch.Success && beatmapLinkMatch.Groups.Count == 2)
            {
                beatmap = beatmapLinkMatch.Groups[1].ToString();
            }

            try
            {
                working = ProcessorWorkingBeatmap.FromFileOrId(beatmap, audio, configManager.GetBindable<string>(Settings.CachePath).Value);
            }
            catch (Exception e)
            {
                showError(e);
                resetBeatmap();
                return;
            }

            if (working is null)
                return;

            if (!working.BeatmapInfo.Ruleset.Equals(ruleset.Value))
            {
                ruleset.Value = working.BeatmapInfo.Ruleset;
            }
            else
            {
                resetCalculations();
            }

            beatmapTitle.Clear();
            beatmapTitle.Add(new BeatmapCard(working));

            loadBackground();

            beatmapDataContainer.Show();
        }

        private void createCalculators()
        {
            if (working is null)
                return;

            var rulesetInstance = ruleset.Value.ShortName == "osu"
                ? new OsuRuleset(osuDifficultyTuning)
                : ruleset.Value.CreateInstance();

            difficultyCalculator.Value = RulesetHelper.GetExtendedDifficultyCalculator(ruleset.Value, working,
                ruleset.Value.ShortName == "osu" ? osuDifficultyTuning : null);
            performanceCalculator = rulesetInstance.CreatePerformanceCalculator();
        }

        private void calculateDifficulty()
        {
            if (working == null || difficultyCalculator.Value == null)
                return;

            try
            {
                difficultyAttributes = difficultyCalculator.Value.Calculate(appliedMods.Value);
                difficultyAttributesContainer.Attributes.Value = AttributeConversion.ToDictionary(difficultyAttributes);
            }
            catch (Exception e)
            {
                showError(e);
                resetBeatmap();
                return;
            }

            if (difficultyCalculator.Value is IExtendedDifficultyCalculator extendedDifficultyCalculator)
            {
                // StrainSkill always skips the first object
                if (working.Beatmap?.HitObjects.Count > 1)
                    strainVisualizer.TimeUntilFirstStrain.Value = (int)working.Beatmap.HitObjects[1].StartTime;

                strainVisualizer.Skills.Value = extendedDifficultyCalculator.GetSkills();
            }
            else
                strainVisualizer.Skills.Value = Array.Empty<Skill>();
        }

        private void debouncedCalculatePerformance()
        {
            debouncedPerformanceUpdate?.Cancel();
            debouncedPerformanceUpdate = Scheduler.AddDelayed(calculatePerformance, 20);
        }

        private void calculatePerformance()
        {
            if (working == null || difficultyAttributes == null)
                return;

            int? countGood = null, countMeh = null;

            if (fullScoreDataSwitch.Current.Value)
            {
                countGood = goodsTextBox.Value.Value;
                countMeh = mehsTextBox.Value.Value;
            }

            int score = RulesetHelper.AdjustManiaScore(scoreTextBox.Value.Value, appliedMods.Value);

            try
            {
                var beatmap = working.GetPlayableBeatmap(ruleset.Value, appliedMods.Value);

                double accuracy = accuracyTextBox.Value.Value / 100.0;
                Dictionary<HitResult, int> statistics = new Dictionary<HitResult, int>();

                if (ruleset.Value.OnlineID != -1)
                {
                    // official rulesets can generate more precise hits from accuracy
                    if (appliedMods.Value.OfType<OsuModClassic>().Any(m => m.NoSliderHeadAccuracy.Value))
                    {
                        statistics = RulesetHelper.GenerateHitResultsForRuleset(ruleset.Value, accuracyTextBox.Value.Value / 100.0, beatmap, appliedMods.Value.ToArray(), missesTextBox.Value.Value, countMeh, countGood,
                            null, null);
                    }
                    else
                    {
                        statistics = RulesetHelper.GenerateHitResultsForRuleset(ruleset.Value, accuracyTextBox.Value.Value / 100.0, beatmap, appliedMods.Value.ToArray(), missesTextBox.Value.Value, countMeh, countGood,
                            largeTickMissesTextBox.Value.Value, sliderTailMissesTextBox.Value.Value);
                    }

                    accuracy = RulesetHelper.GetAccuracyForRuleset(ruleset.Value, beatmap, statistics, appliedMods.Value.ToArray());
                }

                var ppAttributes = performanceCalculator?.Calculate(new ScoreInfo(beatmap.BeatmapInfo, ruleset.Value)
                {
                    Accuracy = accuracy,
                    MaxCombo = comboTextBox.Value.Value,
                    Statistics = statistics,
                    Mods = appliedMods.Value.ToArray(),
                    TotalScore = score,
                    Ruleset = ruleset.Value,
                    LegacyTotalScore = legacyTotalScore,
                }, difficultyAttributes);

                performanceAttributesContainer.Attributes.Value = AttributeConversion.ToDictionary(ppAttributes);
            }
            catch (Exception e)
            {
                showError(e);
                resetBeatmap();
            }
        }

        private void populateScoreParams()
        {
            accuracyContainer.Hide();
            comboTextBox.Hide();
            missesTextBox.Hide();
            largeTickMissesTextBox.Hide();
            sliderTailMissesTextBox.Hide();
            scoreTextBox.Hide();

            if (ruleset.Value.ShortName == "osu" || ruleset.Value.ShortName == "taiko" || ruleset.Value.ShortName == "fruits")
            {
                updateAccuracyParams(fullScoreDataSwitch.Current.Value);
                accuracyContainer.Show();

                updateCombo(true);
                comboTextBox.Show();
                missesTextBox.Show();

                if (ruleset.Value.ShortName == "osu")
                {
                    largeTickMissesTextBox.Show();
                    sliderTailMissesTextBox.Show();
                }
            }
            else if (ruleset.Value.ShortName == "mania")
            {
                updateAccuracyParams(fullScoreDataSwitch.Current.Value);
                accuracyContainer.Show();

                missesTextBox.Show();

                scoreTextBox.Text = string.Empty;
                scoreTextBox.Show();
            }
            else
            {
                // show everything if it's something non-official
                updateAccuracyParams(false);
                accuracyContainer.Show();

                updateCombo(true);
                comboTextBox.Show();
                missesTextBox.Show();
                largeTickMissesTextBox.Show();
                sliderTailMissesTextBox.Show();

                scoreTextBox.Text = string.Empty;
                scoreTextBox.Show();
            }
        }

        private void updateAccuracyParams(bool useFullScoreData)
        {
            goodsTextBox.Text = string.Empty;
            goodsTextBox.Value.Value = 0;

            mehsTextBox.Text = string.Empty;
            mehsTextBox.Value.Value = 0;

            accuracyTextBox.Text = string.Empty;
            accuracyTextBox.Value.Value = 100;

            if (useFullScoreData)
            {
                goodsTextBox.Label = ruleset.Value.ShortName switch
                {
                    "osu" => "100s",
                    "taiko" => "Goods",
                    "fruits" => "Droplets",
                    _ => ""
                };

                mehsTextBox.Label = ruleset.Value.ShortName switch
                {
                    "osu" => "50s",
                    "fruits" => "Tiny Droplets",
                    _ => ""
                };

                accuracyContainer.ColumnDimensions = ruleset.Value.ShortName switch
                {
                    "osu" or "fruits" =>
                        new[]
                        {
                            new Dimension(GridSizeMode.Absolute),
                            new Dimension(),
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize)
                        },
                    "taiko" =>
                        new[]
                        {
                            new Dimension(GridSizeMode.Absolute),
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute),
                            new Dimension(GridSizeMode.AutoSize)
                        },
                    _ => new[]
                    {
                        new Dimension(GridSizeMode.Absolute),
                        new Dimension(GridSizeMode.Absolute),
                        new Dimension(GridSizeMode.Absolute),
                        new Dimension(GridSizeMode.AutoSize)
                    }
                };
            }
            else
            {
                accuracyContainer.ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute),
                    new Dimension(GridSizeMode.Absolute),
                    new Dimension(GridSizeMode.AutoSize)
                };
            }
        }

        private void bindTuningEvents()
        {
            foreach (var bindable in new[]
                     {
                         aimPerformanceScaleTextBox.Value,
                         speedPerformanceScaleTextBox.Value,
                         accuracyPerformanceScaleTextBox.Value,
                         flashlightPerformanceScaleTextBox.Value,
                         totalPerformanceScaleTextBox.Value,
                         aimSkillStrainScaleTextBox.Value,
                         speedSkillStrainScaleTextBox.Value,
                         flashlightSkillStrainScaleTextBox.Value,
                         aimWideAngleBonusScaleTextBox.Value,
                         aimAcuteAngleScaleTextBox.Value,
                         aimSliderBonusScaleTextBox.Value,
                         aimVelocityChangeBonusScaleTextBox.Value,
                         aimWiggleBonusScaleTextBox.Value,
                         flashlightMaxOpacityBonusScaleTextBox.Value,
                         flashlightHiddenBonusScaleTextBox.Value,
                         flashlightMinVelocityScaleTextBox.Value,
                         flashlightSliderBonusScaleTextBox.Value,
                         flashlightMinAngleScaleTextBox.Value,
                         rhythmOverallScaleTextBox.Value,
                         rhythmRatioScaleTextBox.Value,
                         speedSingleSpacingThresholdTextBox.Value,
                         speedMinBonusBpmTextBox.Value,
                         speedBalancingFactorTextBox.Value,
                         speedDistanceScaleTextBox.Value
                     })
            {
                bindable.BindValueChanged(_ => debouncedApplyOsuDifficultyTuning());
            }

            foreach (var bindable in new[]
                     {
                         rhythmHistoryTimeMaxTextBox.Value,
                         rhythmHistoryObjectsMaxTextBox.Value
                     })
            {
                bindable.BindValueChanged(_ => debouncedApplyOsuDifficultyTuning());
            }
        }

        private void debouncedApplyOsuDifficultyTuning()
        {
            debouncedTuningUpdate?.Cancel();
            debouncedTuningUpdate = Scheduler.AddDelayed(applyOsuDifficultyTuning, 50);
        }

        private void applyOsuDifficultyTuning()
        {
            updateOsuDifficultyTuningFromInputs();

            if (ruleset.Value.ShortName != "osu" || working == null)
                return;

            createCalculators();
            calculateDifficulty();
            calculatePerformance();
        }

        private void updateOsuDifficultyTuningFromInputs()
        {
            osuDifficultyTuning = new OsuDifficultyTuning
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
        }

        private void resetOsuDifficultyTuning()
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

            debouncedApplyOsuDifficultyTuning();
        }

        private void updateOsuTuningVisibility()
        {
            if (ruleset.Value.ShortName == "osu")
                osuTuningContainer.Show();
            else
                osuTuningContainer.Hide();
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

        private void resetMods()
        {
            // This is temporary solution to the UX problem that people would usually want to calculate classic scores, but classic and lazer scores have different max combo
            // We append classic mod automatically so that it is immediately obvious what's going on and makes max combo same as live
            /*var classicMod = ruleset.Value.CreateInstance().CreateAllMods().SingleOrDefault(m => m is ModClassic);

            if (classicMod != null)
            {
                appliedMods.Value = new[] { classicMod };
                return;
            }*/

            appliedMods.Value = Array.Empty<Mod>();
        }

        private void resetCalculations()
        {
            createCalculators();

            resetMods();
            legacyTotalScore = null;

            calculateDifficulty();
            calculatePerformance();
            populateScoreParams();
            updateOsuTuningVisibility();
        }

        // This is to make sure combo resets when classic mod is applied
        private int previousMaxCombo;

        private void updateCombo(bool reset)
        {
            if (difficultyAttributes is null)
                return;

            missesTextBox.MaxValue = difficultyAttributes.MaxCombo;

            comboTextBox.PlaceholderText = difficultyAttributes.MaxCombo.ToString();
            comboTextBox.MaxValue = difficultyAttributes.MaxCombo;

            if (comboTextBox.Value.Value > difficultyAttributes.MaxCombo ||
                missesTextBox.Value.Value > difficultyAttributes.MaxCombo ||
                previousMaxCombo != difficultyAttributes.MaxCombo)
                reset = true;

            if (reset)
            {
                comboTextBox.Text = string.Empty;
                comboTextBox.Value.Value = difficultyAttributes.MaxCombo;
                missesTextBox.Text = string.Empty;
            }

            previousMaxCombo = difficultyAttributes.MaxCombo;
        }

        private void loadBackground()
        {
            if (background is not null)
            {
                RemoveInternal(background, true);
            }

            if (working?.BeatmapInfo?.BeatmapSet?.OnlineID is not null)
            {
                LoadComponentAsync(background = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Depth = 99,
                    BlurSigma = new Vector2(6),
                    Children = new Drawable[]
                    {
                        new Sprite
                        {
                            RelativeSizeAxes = Axes.Both,
                            Texture = textures.Get($"https://assets.ppy.sh/beatmaps/{working.BeatmapInfo.BeatmapSet.OnlineID}/covers/cover.jpg"),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            FillMode = FillMode.Fill
                        },
                    }
                }).ContinueWith(_ =>
                {
                    Schedule(() =>
                    {
                        AddInternal(background);
                    });
                });
            }
        }

        private void showError(Exception? e)
        {
            Logger.Log(e?.ToString(), level: LogLevel.Error);

            string message = e is AggregateException aggregateException ? aggregateException.Flatten().Message : e?.Message ?? "Unknown error";
            showError(message, false);
        }

        private void showError(string message, bool log = true)
        {
            if (log)
                Logger.Log(message, level: LogLevel.Error);

            notificationDisplay.Display(new Notification(message));
        }

        private long? legacyTotalScore;

        private void populateSettingsFromScore(ulong scoreId)
        {
            if (scoreIdPopulateButton.State.Value == ButtonState.Loading)
                return;

            scoreIdPopulateButton.State.Value = ButtonState.Loading;

            Task.Run(async () =>
            {
                var scoreInfo = await apiManager.GetJsonFromApi<SoloScoreInfo>($"scores/{scoreId}").ConfigureAwait(false);

                Schedule(() =>
                {
                    if (scoreInfo.BeatmapID != working?.BeatmapInfo.OnlineID)
                    {
                        beatmapIdTextBox.Text = string.Empty;
                        changeBeatmap(scoreInfo.BeatmapID.ToString());
                    }

                    ruleset.Value = rulesets.GetRuleset(scoreInfo.RulesetID)!;
                    appliedMods.Value = scoreInfo.Mods.Select(x => x.ToMod(ruleset.Value.CreateInstance())).ToList();

                    legacyTotalScore = scoreInfo.LegacyTotalScore;

                    fullScoreDataSwitch.Current.Value = true;

                    // TODO: this shouldn't be done in 2 lines
                    comboTextBox.Value.Value = scoreInfo.MaxCombo;
                    comboTextBox.Text = scoreInfo.MaxCombo.ToString();

                    resetMisses();
                    updateMissesTextboxes();

                    if (scoreInfo.Statistics.TryGetValue(HitResult.Miss, out int misses))
                    {
                        missesTextBox.Value.Value = misses;
                        missesTextBox.Text = misses.ToString();
                    }

                    if (scoreInfo.Statistics.TryGetValue(HitResult.Ok, out int oks))
                    {
                        goodsTextBox.Value.Value = oks;
                        goodsTextBox.Text = oks.ToString();
                    }

                    if (scoreInfo.Statistics.TryGetValue(HitResult.Meh, out int mehs))
                    {
                        mehsTextBox.Value.Value = mehs;
                        mehsTextBox.Text = mehs.ToString();
                    }

                    if (ruleset.Value?.ShortName == "fruits")
                    {
                        if (scoreInfo.Statistics.TryGetValue(HitResult.LargeTickHit, out int largeTickHits))
                        {
                            goodsTextBox.Value.Value = largeTickHits;
                            goodsTextBox.Text = largeTickHits.ToString();
                        }

                        if (scoreInfo.Statistics.TryGetValue(HitResult.SmallTickHit, out int smallTickHits))
                        {
                            mehsTextBox.Value.Value = smallTickHits;
                            mehsTextBox.Text = smallTickHits.ToString();
                        }
                    }

                    if (scoreInfo.Statistics.TryGetValue(HitResult.LargeTickMiss, out int largeTickMisses))
                    {
                        largeTickMissesTextBox.Value.Value = largeTickMisses;
                        largeTickMissesTextBox.Text = largeTickMisses.ToString();
                    }

                    if (scoreInfo.Statistics.TryGetValue(HitResult.SliderTailHit, out int sliderTailHits))
                    {
                        int sliderTailMisses = scoreInfo.MaximumStatistics[HitResult.SliderTailHit] - sliderTailHits;
                        sliderTailMissesTextBox.Value.Value = sliderTailMisses;
                        sliderTailMissesTextBox.Text = sliderTailMisses.ToString();
                    }

                    calculateDifficulty();
                    calculatePerformance();

                    scoreIdPopulateButton.State.Value = ButtonState.Done;
                });
            }).ContinueWith(t =>
            {
                showError(t.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted).ContinueWith(t =>
            {
                Schedule(() =>
                {
                    scoreIdPopulateButton.State.Value = ButtonState.Done;
                });
            }, TaskContinuationOptions.None);
        }

        private void resetMisses()
        {
            missesTextBox.Value.Value = 0;
            missesTextBox.Text = string.Empty;

            largeTickMissesTextBox.Value.Value = 0;
            largeTickMissesTextBox.Text = string.Empty;

            sliderTailMissesTextBox.Value.Value = 0;
            sliderTailMissesTextBox.Text = string.Empty;
        }

        private void updateMissesTextboxes()
        {
            if (ruleset.Value.ShortName == "osu")
            {
                // Large tick misses and slider tail misses are only relevant in PP if slider head accuracy exists
                if (appliedMods.Value.OfType<OsuModClassic>().Any(m => m.NoSliderHeadAccuracy.Value))
                {
                    missesContainer.Content = new[] { new[] { missesTextBox } };
                    missesContainer.ColumnDimensions = [new Dimension()];
                }
                else
                {
                    missesContainer.Content = new[] { new[] { missesTextBox, largeTickMissesTextBox, sliderTailMissesTextBox } };
                    missesContainer.ColumnDimensions = [new Dimension(), new Dimension(), new Dimension()];
                }
            }
        }
    }
}
