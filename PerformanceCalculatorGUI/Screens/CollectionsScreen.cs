// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;
using Newtonsoft.Json;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Scoring;
using osuTK;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;
using PerformanceCalculatorGUI.Screens.Collections;

namespace PerformanceCalculatorGUI.Screens
{
    public partial class CollectionsScreen : PerformanceCalculatorScreen
    {
        public override bool ShouldShowConfirmationDialogOnSwitch => false;

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        [Resolved]
        private ScoreCache scoreCache { get; set; } = null!;

        [Resolved]
        private SettingsManager configManager { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        private DialogOverlay dialogOverlay { get; set; } = null!;

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; } = null!;

        [Resolved]
        private OsuDifficultyTuningManager tuningManager { get; set; } = null!;

        private FillFlowContainer collectionList = null!;
        private CreateCollectionButton createCollectionButton = null!;

        private OsuSpriteText collectionNameText = null!;
        private FillFlowContainer collectionContainer = null!;
        private FillFlowContainer<ScoreContainer> scoresList = null!;
        private AddScoreButton addScoreButton = null!;
        private readonly Bindable<CollectionSortCriteria> sorting = new Bindable<CollectionSortCriteria>(CollectionSortCriteria.None);

        private Container autobalanceContainer = null!;
        private FillFlowContainer autobalanceParametersContainer = null!;
        private OsuSpriteText autobalanceStatusText = null!;
        private RoundedButton autobalanceRunButton = null!;
        private readonly Bindable<AutobalanceTarget> autobalanceTarget = new Bindable<AutobalanceTarget>(AutobalanceTarget.Total);
        private readonly Dictionary<AutobalanceParameter, BindableBool> autobalanceParameterStates = new Dictionary<AutobalanceParameter, BindableBool>();
        private bool autobalanceRunning;

        private VerboseLoadingLayer loadingLayer = null!;

        private readonly Bindable<Collection?> currentCollection = new Bindable<Collection?>();

        private const string collections_directory = "collections";
        private const int autobalance_max_iterations = 10000;
        private const double autobalance_tolerance = 0.01;

        public CollectionsScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension(GridSizeMode.Absolute, 250), new Dimension() },
                    RowDimensions = new[] { new Dimension() },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background6.Darken(0.2f)
                                    },
                                    new OsuScrollContainer(Direction.Vertical)
                                    {
                                        Name = "Collection List",
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            new FillFlowContainer
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
                                                        Origin = Anchor.TopCentre,
                                                        Anchor = Anchor.TopCentre,
                                                        Height = 20,
                                                        Text = "Collection list"
                                                    },
                                                    collectionList = new FillFlowContainer
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y,
                                                        Direction = FillDirection.Vertical,
                                                    },
                                                    createCollectionButton = new CreateCollectionButton()
                                                }
                                            }
                                        }
                                    },
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background6
                                    },
                                    new OsuScrollContainer(Direction.Vertical)
                                    {
                                        Name = "Scores",
                                        RelativeSizeAxes = Axes.Both,
                                        Child = collectionContainer = new FillFlowContainer
                                        {
                                            Padding = new MarginPadding { Left = 10f, Right = 15.0f, Vertical = 5f },
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 2f),
                                            Alpha = 0,
                                            Children =
                                            [
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Children = new Drawable[]
                                                    {
                                                        collectionNameText = new OsuSpriteText
                                                        {
                                                            Origin = Anchor.TopLeft,
                                                            Anchor = Anchor.TopLeft,
                                                            Height = 20
                                                        },
                                                        new OverlaySortTabControl<CollectionSortCriteria>
                                                        {
                                                            Anchor = Anchor.CentreRight,
                                                            Origin = Anchor.CentreRight,
                                                            Margin = new MarginPadding { Right = 20 },
                                                            Current = { BindTarget = sorting }
                                                        }
                                                    }
                                                },
                                                autobalanceContainer = new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Masking = true,
                                                    CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS,
                                                    Children = new Drawable[]
                                                    {
                                                        new Box
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            Colour = colourProvider.Background5,
                                                            Alpha = 0.6f
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Vertical,
                                                            Spacing = new Vector2(0, 6),
                                                            Padding = new MarginPadding { Horizontal = 10, Vertical = 8 },
                                                            Children = new Drawable[]
                                                            {
                                                                new OsuSpriteText
                                                                {
                                                                    Text = "Autobalance",
                                                                    Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold),
                                                                    Margin = new MarginPadding { Bottom = 2 }
                                                                },
                                                                new OverlaySortTabControl<AutobalanceTarget>
                                                                {
                                                                    Title = "Target",
                                                                    Current = { BindTarget = autobalanceTarget }
                                                                },
                                                                new OsuSpriteText
                                                                {
                                                                    Text = "Parameters",
                                                                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                                                    Colour = colourProvider.Light2,
                                                                    Margin = new MarginPadding { Top = 6 }
                                                                },
                                                                autobalanceParametersContainer = new FillFlowContainer
                                                                {
                                                                    RelativeSizeAxes = Axes.X,
                                                                    AutoSizeAxes = Axes.Y,
                                                                    Direction = FillDirection.Full,
                                                                    Spacing = new Vector2(10, 6),
                                                                },
                                                                new FillFlowContainer
                                                                {
                                                                    RelativeSizeAxes = Axes.X,
                                                                    AutoSizeAxes = Axes.Y,
                                                                    Direction = FillDirection.Horizontal,
                                                                    Spacing = new Vector2(10, 0),
                                                                    Children = new Drawable[]
                                                                    {
                                                                        autobalanceRunButton = new RoundedButton
                                                                        {
                                                                            Width = 160,
                                                                            Height = 40,
                                                                            Text = "Auto-balance",
                                                                            Action = runAutobalance,
                                                                            BackgroundColour = colourProvider.Background1
                                                                        },
                                                                        autobalanceStatusText = new OsuSpriteText
                                                                        {
                                                                            Anchor = Anchor.CentreLeft,
                                                                            Origin = Anchor.CentreLeft,
                                                                            Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                                                            Colour = colourProvider.Light2,
                                                                            Text = "Ready"
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                },
                                                scoresList = new FillFlowContainer<ScoreContainer>
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                },
                                                addScoreButton = new AddScoreButton()
                                            ]
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                loadingLayer = new VerboseLoadingLayer(true)
                {
                    RelativeSizeAxes = Axes.Both
                }
            };
            sorting.ValueChanged += e => { updateSorting(e.NewValue); };

            currentCollection.ValueChanged += loadCollection;
            createCollectionButton.OnSave += onCollectionAdd;
            addScoreButton.OnAdd += onScoreAdd;
            tuningManager.Current.BindValueChanged(_ =>
            {
                if (currentCollection.Value != null)
                    calculateScores();
            });

            createAutobalanceParameterControls();
            loadCollectionList();

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(calculateScores);
        }

        private void onScoreAdd(long scoreId)
        {
            if (currentCollection.Value!.Scores.Contains(scoreId))
            {
                notificationDisplay.Display(new Notification($"Score {scoreId} already exists"));
                return;
            }

            currentCollection.Value.Scores = [.. currentCollection.Value.Scores, scoreId];

            saveCurrentCollection();
        }

        private void onScoreRemove(long scoreId)
        {
            currentCollection.Value!.Scores = currentCollection.Value.Scores.Where(x => x != scoreId).ToArray();
            currentCollection.Value.ExpectedPerformance.Remove(scoreId);

            saveCurrentCollection();
        }

        private void loadCollection(ValueChangedEvent<Collection?> obj)
        {
            if (obj.NewValue == null)
            {
                collectionContainer.Hide();
                return;
            }

            obj.NewValue.ExpectedPerformance ??= new Dictionary<long, ExpectedPerformanceValues>();
            collectionNameText.Text = obj.NewValue!.Name;
            collectionContainer.Show();
            autobalanceStatusText.Text = "Ready";

            calculateScores();
        }

        private void createAutobalanceParameterControls()
        {
            autobalanceParametersContainer.Clear();
            autobalanceParameterStates.Clear();

            foreach (var parameter in autobalanceParameters)
            {
                var bindable = new BindableBool { Value = parameter.DefaultEnabled };
                autobalanceParameterStates[parameter] = bindable;

                autobalanceParametersContainer.Add(new Container
                {
                    Width = 230,
                    AutoSizeAxes = Axes.Y,
                    Child = new ExtendedOsuCheckbox
                    {
                        RelativeSizeAxes = Axes.X,
                        Padding = new MarginPadding(4),
                        Current = { BindTarget = bindable },
                        LabelText = parameter.Label,
                        TextColour = colourProvider.Light2
                    }
                });
            }
        }

        private void saveCurrentCollection()
        {
            saveCurrentCollection(true);
        }

        private void saveCurrentCollection(bool recalculateScores)
        {
            if (currentCollection.Value == null)
                return;

            saveCollection(currentCollection.Value, recalculateScores);
        }

        private void saveCollection(Collection collection, bool recalculateScores)
        {
            string path = Path.Combine(collections_directory, collection.FileName);

            File.WriteAllText(path, JsonConvert.SerializeObject(collection));

            if (recalculateScores && collection == currentCollection.Value)
                calculateScores();
        }

        private void calculateScores()
        {
            if (currentCollection.Value == null)
                return;

            scoresList.Clear();

            loadingLayer.Show();

            var collection = currentCollection.Value;

            Task.Run(async () =>
            {
                foreach (long scoreId in collection.Scores)
                {
                    var score = await scoreCache.GetScore(scoreId).ConfigureAwait(false);
                    if (score == null)
                        continue;

                    var rulesetInfo = rulesets.GetRuleset(score.RulesetID)!;
                    var rulesetInstance = rulesetInfo.ShortName == "osu"
                        ? new OsuRuleset(tuningManager.Current.Value)
                        : rulesetInfo.CreateInstance();

                    var working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapID.ToString(), cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);

                    Mod[] mods = score.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();

                    var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);

                    var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                    var difficultyCalculator = rulesetInstance.CreateDifficultyCalculator(working);
                    var difficultyAttributes = difficultyCalculator.Calculate(mods);
                    var performanceCalculator = rulesetInstance.CreatePerformanceCalculator();
                    if (performanceCalculator == null)
                        continue;

                    var perfAttributes = performanceCalculator.Calculate(parsedScore.ScoreInfo, difficultyAttributes);
                    Schedule(() =>
                    {
                        var scoreContainer = new ScoreContainer(new ExtendedScore(score, difficultyAttributes, perfAttributes), scoreId,
                            collection.ExpectedPerformance, () => saveCollection(collection, false));
                        scoreContainer.OnDelete += onScoreRemove;

                        scoresList.Add(scoreContainer);
                    });
                }
            }).ContinueWith(t =>
            {
                Logger.Log(t.Exception?.ToString(), level: LogLevel.Error);
                notificationDisplay.Display(new Notification(t.Exception?.Flatten().Message ?? "Failed to calculate collection"));
            }, TaskContinuationOptions.OnlyOnFaulted).ContinueWith(t =>
            {
                Schedule(() =>
                {
                    updateSorting(sorting.Value);
                    loadingLayer.Hide();
                });
            }, TaskContinuationOptions.None);
        }

        private void runAutobalance()
        {
            if (autobalanceRunning)
                return;

            if (currentCollection.Value == null)
            {
                notificationDisplay.Display(new Notification("Select a collection first."));
                return;
            }

            var selectedParameters = autobalanceParameterStates
                                     .Where(kv => kv.Value.Value)
                                     .Select(kv => kv.Key)
                                     .ToArray();

            if (selectedParameters.Length == 0)
            {
                notificationDisplay.Display(new Notification("Select at least one tuning parameter."));
                return;
            }

            setAutobalanceState(true, "Preparing...");

            var collection = currentCollection.Value;
            var target = autobalanceTarget.Value;

            Task.Run(async () =>
            {
                var dataset = await buildAutobalanceDataset(collection, target).ConfigureAwait(false);
                if (dataset.Count == 0)
                    return AutobalanceResult.Failure($"No expected values found for {getTargetLabel(target)}.");

                var baseTuning = tuningManager.Current.Value;
                var objective = ObjectiveFunction.Value(point => evaluateAutobalance(dataset, selectedParameters, baseTuning, target, point));
                var initialGuess = Vector<double>.Build.Dense(selectedParameters.Length, i => selectedParameters[i].Getter(baseTuning));

                var solver = new NelderMeadSimplex(autobalance_tolerance, autobalance_max_iterations);
                var result = solver.FindMinimum(objective, initialGuess);
                var balancedTuning = applyAutobalanceParameters(baseTuning, selectedParameters, result.MinimizingPoint);
                double rmse = Math.Sqrt(result.FunctionInfoAtMinimum.Value);

                return AutobalanceResult.Success(balancedTuning, rmse, dataset.Count);
            }).ContinueWith(t =>
            {
                if (t.Exception != null)
                    Logger.Log(t.Exception.ToString(), level: LogLevel.Error);

                Schedule(() =>
                {
                    loadingLayer.Hide();

                    AutobalanceResult result = t.IsFaulted ? AutobalanceResult.Failure("Autobalance failed.") : t.GetResultSafely();

                    if (t.IsFaulted || result.IsFailure)
                    {
                        string message = t.IsFaulted
                            ? t.Exception?.Flatten().Message ?? "Autobalance failed."
                            : result.ErrorMessage ?? "Autobalance failed.";

                        notificationDisplay.Display(new Notification(message));
                        setAutobalanceState(false, "Failed");
                        return;
                    }

                    tuningManager.Current.Value = result.Tuning!;
                    setAutobalanceState(false, $"RMSE {result.Rmse:0.##}pp ({result.SampleCount} scores)");
                });
            }, TaskContinuationOptions.None);
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
                    return double.PositiveInfinity;

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

                return count > 0 ? errorSum / count : double.PositiveInfinity;
            }
            catch
            {
                return double.PositiveInfinity;
            }
        }

        private OsuDifficultyTuning applyAutobalanceParameters(OsuDifficultyTuning baseTuning, AutobalanceParameter[] parameters, Vector<double> values)
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

        private void setAutobalanceState(bool running, string status)
        {
            autobalanceRunning = running;
            autobalanceRunButton.Enabled.Value = !running;
            autobalanceStatusText.Text = status;

            if (running)
                loadingLayer.Show();
        }

        private void onCollectionAdd(string name)
        {
            string fileName = RandomNumberGenerator.GetString(choices: "abcdefghijklmnopqrstuvwxyz0123456789", length: 16) + ".json";

            var collection = new Collection
            {
                Name = name,
                FileName = fileName,
                Scores = []
            };

            string path = Path.Combine(collections_directory, fileName);

            File.WriteAllText(path, JsonConvert.SerializeObject(collection));

            loadCollectionList();
        }

        private void loadCollectionList()
        {
            if (!Directory.Exists(collections_directory))
            {
                Directory.CreateDirectory(collections_directory);

                return; // nothing to load
            }

            collectionList.Clear();

            var collections = new List<Collection>();

            foreach (string collectionFile in Directory.EnumerateFiles(collections_directory))
            {
                var deserializedCollection = JsonConvert.DeserializeObject<Collection>(File.ReadAllText(collectionFile));

                if (deserializedCollection != null)
                {
                    collections.Add(deserializedCollection);
                }
            }

            foreach (var collection in collections.OrderBy(x => x.Name))
            {
                var collectionButton = new CollectionButton(collection, currentCollection);
                collectionList.Add(collectionButton);

                collectionButton.OnDelete += onCollectionDelete;
            }
        }

        private void onCollectionDelete(Collection collection)
        {
            dialogOverlay.Push(new ConfirmDialog("", () =>
            {
                if (collection == currentCollection.Value)
                    currentCollection.Value = null;

                File.Delete(Path.Combine(collections_directory, collection.FileName));

                loadCollectionList();
            })
            {
                HeaderText = DialogStrings.DeletionHeaderText,
                Icon = FontAwesome.Solid.Trash,
                BodyText = collection.Name
            });
        }

        private void updateSorting(CollectionSortCriteria sortCriteria)
        {
            if (!scoresList.Children.Any())
                return;

            if (sortCriteria == CollectionSortCriteria.None)
            {
                for (int i = 0; i < scoresList.Count; i++)
                {
                    scoresList.SetLayoutPosition(scoresList[i], Array.IndexOf(currentCollection.Value!.Scores, scoresList[i].Score.SoloScore.ID));
                }

                return;
            }

            ScoreContainer[] sortedScores;

            switch (sortCriteria)
            {
                case CollectionSortCriteria.Live:
                    sortedScores = scoresList.Children.OrderByDescending(x => x.Score.LivePP).ToArray();
                    break;

                case CollectionSortCriteria.Local:
                    sortedScores = scoresList.Children.OrderByDescending(x => x.Score.PerformanceAttributes?.Total).ToArray();
                    break;

                case CollectionSortCriteria.Difference:
                    sortedScores = scoresList.Children.OrderByDescending(x => x.Score.PerformanceAttributes?.Total - x.Score.LivePP).ToArray();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(sortCriteria), sortCriteria, null);
            }

            for (int i = 0; i < sortedScores.Length; i++)
            {
                scoresList.SetLayoutPosition(sortedScores[i], i);
            }
        }

        private static readonly AutobalanceParameter[] autobalanceParameters = createAutobalanceParameters();

        private static AutobalanceParameter[] createAutobalanceParameters() => new[]
        {
            AutobalanceParameter.ForDouble("Aim perf", t => t.AimPerformanceScale, (t, v) => t with { AimPerformanceScale = v }, true),
            AutobalanceParameter.ForDouble("Speed perf", t => t.SpeedPerformanceScale, (t, v) => t with { SpeedPerformanceScale = v }, true),
            AutobalanceParameter.ForDouble("Accuracy perf", t => t.AccuracyPerformanceScale, (t, v) => t with { AccuracyPerformanceScale = v }, true),
            AutobalanceParameter.ForDouble("Flashlight perf", t => t.FlashlightPerformanceScale, (t, v) => t with { FlashlightPerformanceScale = v }, false),
            AutobalanceParameter.ForDouble("Total perf", t => t.TotalPerformanceScale, (t, v) => t with { TotalPerformanceScale = v }, true),
            AutobalanceParameter.ForDouble("Aim strain", t => t.AimSkillStrainScale, (t, v) => t with { AimSkillStrainScale = v }, true),
            AutobalanceParameter.ForDouble("Speed strain", t => t.SpeedSkillStrainScale, (t, v) => t with { SpeedSkillStrainScale = v }, true),
            AutobalanceParameter.ForDouble("Flashlight strain", t => t.FlashlightSkillStrainScale, (t, v) => t with { FlashlightSkillStrainScale = v }, false),
            AutobalanceParameter.ForDouble("Aim wide angle", t => t.AimWideAngleBonusScale, (t, v) => t with { AimWideAngleBonusScale = v }, true),
            AutobalanceParameter.ForDouble("Aim acute angle", t => t.AimAcuteAngleScale, (t, v) => t with { AimAcuteAngleScale = v }, true),
            AutobalanceParameter.ForDouble("Aim slider bonus", t => t.AimSliderBonusScale, (t, v) => t with { AimSliderBonusScale = v }, true),
            AutobalanceParameter.ForDouble("Aim velocity bonus", t => t.AimVelocityChangeBonusScale, (t, v) => t with { AimVelocityChangeBonusScale = v }, true),
            AutobalanceParameter.ForDouble("Aim wiggle bonus", t => t.AimWiggleBonusScale, (t, v) => t with { AimWiggleBonusScale = v }, true),
            AutobalanceParameter.ForDouble("Flashlight max opacity", t => t.FlashlightMaxOpacityBonusScale, (t, v) => t with { FlashlightMaxOpacityBonusScale = v }, false),
            AutobalanceParameter.ForDouble("Flashlight hidden bonus", t => t.FlashlightHiddenBonusScale, (t, v) => t with { FlashlightHiddenBonusScale = v }, false),
            AutobalanceParameter.ForDouble("Flashlight min velocity", t => t.FlashlightMinVelocityScale, (t, v) => t with { FlashlightMinVelocityScale = v }, false),
            AutobalanceParameter.ForDouble("Flashlight slider bonus", t => t.FlashlightSliderBonusScale, (t, v) => t with { FlashlightSliderBonusScale = v }, false),
            AutobalanceParameter.ForDouble("Flashlight min angle", t => t.FlashlightMinAngleScale, (t, v) => t with { FlashlightMinAngleScale = v }, false),
            AutobalanceParameter.ForInt("Rhythm history ms", t => t.RhythmHistoryTimeMax, (t, v) => t with { RhythmHistoryTimeMax = v }, true),
            AutobalanceParameter.ForInt("Rhythm history objs", t => t.RhythmHistoryObjectsMax, (t, v) => t with { RhythmHistoryObjectsMax = v }, true),
            AutobalanceParameter.ForDouble("Rhythm overall", t => t.RhythmOverallScale, (t, v) => t with { RhythmOverallScale = v }, true),
            AutobalanceParameter.ForDouble("Rhythm ratio", t => t.RhythmRatioScale, (t, v) => t with { RhythmRatioScale = v }, true),
            AutobalanceParameter.ForDouble("Speed spacing", t => t.SpeedSingleSpacingThreshold, (t, v) => t with { SpeedSingleSpacingThreshold = v }, true),
            AutobalanceParameter.ForDouble("Speed min bpm", t => t.SpeedMinBonusBpm, (t, v) => t with { SpeedMinBonusBpm = v }, true),
            AutobalanceParameter.ForDouble("Speed balance", t => t.SpeedBalancingFactor, (t, v) => t with { SpeedBalancingFactor = v }, true),
            AutobalanceParameter.ForDouble("Speed distance", t => t.SpeedDistanceScale, (t, v) => t with { SpeedDistanceScale = v }, true),
        };

        private enum AutobalanceTarget
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

        private sealed class AutobalanceParameter
        {
            public string Label { get; }
            public Func<OsuDifficultyTuning, double> Getter { get; }
            public Func<OsuDifficultyTuning, double, OsuDifficultyTuning> Setter { get; }
            public double MinValue { get; }
            public bool IsInteger { get; }
            public bool DefaultEnabled { get; }

            private AutobalanceParameter(string label, Func<OsuDifficultyTuning, double> getter, Func<OsuDifficultyTuning, double, OsuDifficultyTuning> setter,
                                         double minValue, bool isInteger, bool defaultEnabled)
            {
                Label = label;
                Getter = getter;
                Setter = setter;
                MinValue = minValue;
                IsInteger = isInteger;
                DefaultEnabled = defaultEnabled;
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

            public static AutobalanceParameter ForDouble(string label, Func<OsuDifficultyTuning, double> getter,
                                                         Func<OsuDifficultyTuning, double, OsuDifficultyTuning> setter, bool defaultEnabled)
                => new AutobalanceParameter(label, getter, setter, 0.01, false, defaultEnabled);

            public static AutobalanceParameter ForInt(string label, Func<OsuDifficultyTuning, int> getter,
                                                      Func<OsuDifficultyTuning, int, OsuDifficultyTuning> setter, bool defaultEnabled)
                => new AutobalanceParameter(label, t => getter(t), (t, v) => setter(t, (int)v), 1, true, defaultEnabled);
        }

        private sealed class AutobalanceScoreData
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

        private readonly struct AutobalanceResult
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
}
