// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Graphics.UserInterfaceV2;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public partial class CollectionPickerButton : RoundedButton, IHasPopover
    {
        public Func<IEnumerable<Collection>>? LoadCollections { get; set; }
        public Func<string, Collection?>? CreateCollection { get; set; }
        public Func<Collection, bool>? OnCollectionSelected { get; set; }

        public Popover GetPopover() => new CollectionPickerPopover(LoadCollections, CreateCollection, OnCollectionSelected);

        protected override bool OnClick(ClickEvent e)
        {
            this.ShowPopover();
            return base.OnClick(e);
        }
    }
}
