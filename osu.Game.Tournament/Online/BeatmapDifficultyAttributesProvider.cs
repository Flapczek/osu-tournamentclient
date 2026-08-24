// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.IO.Network;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Online.API;

namespace osu.Game.Tournament.Online
{
    internal partial class BeatmapDifficultyAttributesProvider : Component
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private readonly Dictionary<BeatmapDifficultyAttributesKey, double> cache = new Dictionary<BeatmapDifficultyAttributesKey, double>();
        private readonly Dictionary<BeatmapDifficultyAttributesKey, List<Action<double?>>> pendingRequests = new Dictionary<BeatmapDifficultyAttributesKey, List<Action<double?>>>();

        public void GetStarRating(BeatmapDifficultyAttributesKey key, Action<double?> completion)
        {
            if (cache.TryGetValue(key, out double cachedStarRating))
            {
                completion(cachedStarRating);
                return;
            }

            if (pendingRequests.TryGetValue(key, out var callbacks))
            {
                callbacks.Add(completion);
                return;
            }

            pendingRequests[key] = new List<Action<double?>> { completion };

            var request = new GetBeatmapDifficultyAttributesRequest(key);
            request.Success += response => complete(key, response.Attributes?.StarRating);
            request.Failure += _ => complete(key, null);
            api.Queue(request);
        }

        private void complete(BeatmapDifficultyAttributesKey key, double? starRating)
        {
            if (!pendingRequests.Remove(key, out var callbacks))
                return;

            if (starRating is >= 0 && double.IsFinite(starRating.Value))
                cache[key] = starRating.Value;
            else
                starRating = null;

            foreach (var callback in callbacks)
                callback(starRating);
        }
    }

    internal readonly record struct BeatmapDifficultyAttributesKey(int BeatmapID, LegacyMods Mods, int RulesetID);

    internal class GetBeatmapDifficultyAttributesRequest : APIRequest<BeatmapDifficultyAttributesResponse>
    {
        public readonly BeatmapDifficultyAttributesKey Key;

        public GetBeatmapDifficultyAttributesRequest(BeatmapDifficultyAttributesKey key)
        {
            Key = key;
        }

        protected override WebRequest CreateWebRequest()
        {
            var request = base.CreateWebRequest();

            request.ContentType = "application/json";
            request.Method = HttpMethod.Post;
            request.AddRaw(JsonConvert.SerializeObject(new
            {
                mods = (int)Key.Mods,
                ruleset_id = Key.RulesetID,
            }));

            return request;
        }

        protected override string Target => $"beatmaps/{Key.BeatmapID}/attributes";
    }

    internal class BeatmapDifficultyAttributesResponse
    {
        [JsonProperty("attributes")]
        public BeatmapDifficultyAttributes? Attributes { get; set; }
    }

    internal class BeatmapDifficultyAttributes
    {
        [JsonProperty("star_rating")]
        public double StarRating { get; set; }
    }
}
