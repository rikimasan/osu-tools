// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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

        private readonly List<TuningControl> tuningControls = new List<TuningControl>();

        public OsuDifficultyTuningPopover()
            : base(false)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var initialTuning = tuningManager.Current.Value;
            tuningControls.Clear();

            var content = new List<Drawable>
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
                }
            };

            foreach (var section in OsuDifficultyTuningParameters.Sections)
            {
                content.Add(new OsuSpriteText
                {
                    Margin = new MarginPadding { Top = 6f },
                    Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                    Text = section.Title
                });

                content.Add(createSectionGrid(section, initialTuning));
            }

            content.Add(new FillFlowContainer
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
            });

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
                        Children = content.ToArray()
                    }
                }
            };
        }

        private GridContainer createSectionGrid(OsuDifficultyTuningSection section, OsuDifficultyTuning initialTuning)
        {
            int rows = (section.Parameters.Count + 1) / 2;
            var rowDimensions = new Dimension[rows];

            for (int i = 0; i < rows; i++)
                rowDimensions[i] = new Dimension(GridSizeMode.AutoSize);

            var content = new Drawable[rows][];

            for (int row = 0; row < rows; row++)
            {
                var rowContent = new Drawable[2];

                for (int column = 0; column < 2; column++)
                {
                    int index = row * 2 + column;

                    rowContent[column] = index < section.Parameters.Count
                        ? createControl(section.Parameters[index], initialTuning)
                        : new Container();
                }

                content[row] = rowContent;
            }

            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                RowDimensions = rowDimensions,
                Content = content
            };
        }

        private Drawable createControl(OsuDifficultyTuningParameter parameter, OsuDifficultyTuning initialTuning)
        {
            TuningControl control;

            if (parameter.IsInteger)
            {
                int value = (int)Math.Round(parameter.Getter(initialTuning));
                var box = createTuningIntBox(parameter.UiLabel, value);
                control = new TuningControl(parameter, box);
            }
            else
            {
                double value = parameter.Getter(initialTuning);
                var box = createTuningBox(parameter.UiLabel, value);
                control = new TuningControl(parameter, box);
            }

            tuningControls.Add(control);
            return control.Drawable;
        }

        private void apply()
        {
            var tuning = tuningManager.Current.Value;

            foreach (var control in tuningControls)
            {
                tuning = control.Parameter.Setter(tuning, control.Value);
            }

            tuningManager.Current.Value = tuning;

            this.HidePopover();
        }

        private void resetToDefaults()
        {
            var defaults = OsuDifficultyTuning.Default;

            foreach (var control in tuningControls)
            {
                control.Reset(control.Parameter.Getter(defaults));
            }

            tuningManager.Current.Value = defaults;
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
            box.PlaceholderText = value.ToString();
            box.Text = string.Empty;
            box.Value.Value = value;
        }

        private static void setTuningValue(LimitedLabelledNumberBox box, int value)
        {
            box.PlaceholderText = value.ToString();
            box.Text = string.Empty;
            box.Value.Value = value;
        }

        private sealed class TuningControl
        {
            public OsuDifficultyTuningParameter Parameter { get; }

            private readonly LimitedLabelledFractionalNumberBox? fractionalBox;
            private readonly LimitedLabelledNumberBox? intBox;

            public Drawable Drawable => fractionalBox ?? (Drawable)intBox!;

            public double Value => fractionalBox != null ? fractionalBox.Value.Value : intBox!.Value.Value;

            public TuningControl(OsuDifficultyTuningParameter parameter, LimitedLabelledFractionalNumberBox box)
            {
                Parameter = parameter;
                fractionalBox = box;
            }

            public TuningControl(OsuDifficultyTuningParameter parameter, LimitedLabelledNumberBox box)
            {
                Parameter = parameter;
                intBox = box;
            }

            public void Reset(double value)
            {
                if (fractionalBox != null)
                    setTuningValue(fractionalBox, value);
                else
                    setTuningValue(intBox!, (int)Math.Round(value));
            }
        }
    }
}
