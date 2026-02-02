// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public partial class CollectionPickerPopover : OsuPopover
    {
        private readonly Func<IEnumerable<Collection>>? loadCollections;
        private readonly Func<string, Collection?>? createCollection;
        private readonly Func<Collection, bool>? onCollectionSelected;

        private FillFlowContainer collectionList = null!;
        private OsuSpriteText emptyText = null!;
        private OsuSpriteText loadingText = null!;
        private CreateCollectionButton createCollectionButton = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public CollectionPickerPopover(Func<IEnumerable<Collection>>? loadCollections, Func<string, Collection?>? createCollection, Func<Collection, bool>? onCollectionSelected)
            : base(false)
        {
            this.loadCollections = loadCollections;
            this.createCollection = createCollection;
            this.onCollectionSelected = onCollectionSelected;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new Container
            {
                AutoSizeAxes = Axes.Y,
                Width = 320,
                Padding = new MarginPadding(10),
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 6),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = "Add to collection",
                            Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold)
                        },
                        loadingText = new OsuSpriteText
                        {
                            Text = "Loading collections...",
                            Alpha = 0,
                            Font = OsuFont.GetFont(size: 12),
                            Colour = colourProvider.Light2
                        },
                        emptyText = new OsuSpriteText
                        {
                            Text = "No collections found",
                            Alpha = 0,
                            Font = OsuFont.GetFont(size: 12),
                            Colour = colourProvider.Light2
                        },
                        collectionList = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 4)
                        },
                        createCollectionButton = new CreateCollectionButton()
                    }
                }
            };

            createCollectionButton.OnSave += name =>
            {
                var collection = createCollection?.Invoke(name);
                if (collection == null)
                    return;

                if (!selectCollection(collection))
                    reloadCollections();
            };
        }

        protected override void PopIn()
        {
            base.PopIn();
            reloadCollections();
        }

        private void reloadCollections()
        {
            collectionList.Clear();
            emptyText.Alpha = 0;
            loadingText.Alpha = 1;

            Task.Run(() =>
            {
                try
                {
                    return loadCollections?.Invoke()?.OrderBy(x => x.Name).ToList() ?? new List<Collection>();
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString(), level: LogLevel.Error);
                    return new List<Collection>();
                }
            }).ContinueWith(task =>
            {
                var collections = task.GetResultSafely() ?? new List<Collection>();

                Schedule(() =>
                {
                    if (IsDisposed)
                        return;

                    collectionList.Clear();
                    loadingText.Alpha = 0;
                    emptyText.Alpha = collections.Count == 0 ? 1 : 0;

                    foreach (var collection in collections)
                    {
                        collectionList.Add(new RoundedButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 32,
                            Text = collection.Name,
                            BackgroundColour = colourProvider.Background1,
                            Action = () => { selectCollection(collection); }
                        });
                    }
                });
            });
        }

        private bool selectCollection(Collection collection)
        {
            bool added = onCollectionSelected?.Invoke(collection) ?? false;

            if (added)
                this.HidePopover();

            return added;
        }
    }
}
