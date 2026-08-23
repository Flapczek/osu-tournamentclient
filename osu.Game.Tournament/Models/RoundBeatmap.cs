// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Tournament.Models
{
    public class RoundBeatmap
    {
        public int ID;
        public string Mods = string.Empty;

        /// <summary>
        /// Correction factor applied to scores achieved with Easy when tosu score processing is enabled.
        /// A null value opts this beatmap out of tosu score processing entirely.
        /// </summary>
        public double? EZMultiplier;

        [JsonProperty("BeatmapInfo")]
        public TournamentBeatmap? Beatmap;
    }
}
