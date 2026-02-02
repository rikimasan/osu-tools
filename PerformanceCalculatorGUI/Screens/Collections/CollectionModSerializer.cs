// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace PerformanceCalculatorGUI.Screens.Collections
{
    public static class CollectionModSerializer
    {
        public static List<CollectionModInfo> Serialize(IEnumerable<Mod> mods)
        {
            var items = new List<CollectionModInfo>();

            foreach (var mod in mods)
            {
                var apiMod = new APIMod(mod);
                var settings = apiMod.Settings.ToDictionary(setting => setting.Key, setting => setting.Value!);

                items.Add(new CollectionModInfo
                {
                    Acronym = apiMod.Acronym,
                    Settings = settings
                });
            }

            return items;
        }

        public static Mod[] Deserialize(IReadOnlyList<CollectionModInfo>? mods, Ruleset ruleset)
        {
            if (mods == null || mods.Count == 0)
                return Array.Empty<Mod>();

            var result = new List<Mod>();

            foreach (var modInfo in mods)
            {
                var apiMod = new APIMod { Acronym = modInfo.Acronym };

                if (modInfo.Settings != null)
                {
                    foreach (var setting in modInfo.Settings)
                    {
                        var normalizedValue = normalizeSettingValue(setting.Value);
                        if (normalizedValue != null)
                            apiMod.Settings[setting.Key] = normalizedValue;
                    }
                }

                result.Add(apiMod.ToMod(ruleset));
            }

            return result.ToArray();
        }

        private static object? normalizeSettingValue(object? value)
        {
            if (value is JToken token)
                return token.ToObject<object>() ?? token.ToString();

            return value;
        }
    }
}
