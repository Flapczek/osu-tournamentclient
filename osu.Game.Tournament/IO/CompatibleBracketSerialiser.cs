// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        };

        public static string CreateCompatibleBracket(string serialisedLadder)
        {
            var bracket = JObject.Parse(serialisedLadder);

            foreach (string property in enhanced_properties)
                bracket.Remove(property);

            return bracket.ToString(Formatting.Indented);
        }
    }
}
