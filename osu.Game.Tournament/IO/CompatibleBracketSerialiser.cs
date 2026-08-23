// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.IO
{
    internal static class CompatibleBracketSerialiser
    {
        private static readonly string[] enhanced_properties =
        {
            nameof(LadderInfo.OneVsOneMode),
            nameof(LadderInfo.WipeChromaArea),
            nameof(LadderInfo.UseIPCForMapPoolProgression),
            nameof(LadderInfo.UseTosuForEZMultiplier),
        };

        public static string CreateCompatibleBracket(string serialisedLadder)
        {
            var bracket = JObject.Parse(serialisedLadder);

            foreach (string property in enhanced_properties)
                bracket.Remove(property);

            foreach (var beatmap in bracket.SelectTokens($"{nameof(LadderInfo.Rounds)}[*].{nameof(TournamentRound.Beatmaps)}[*]").OfType<JObject>())
                beatmap.Remove(nameof(RoundBeatmap.EZMultiplier));

            return bracket.ToString(Formatting.Indented);
        }
    }
}
