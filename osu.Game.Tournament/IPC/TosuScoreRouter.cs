// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Tournament.IPC
{
    internal static class TosuScoreRouter
    {
        public static TosuTeamScores SelectScores(
            bool enabled,
            TosuTeamScores fileScores,
            bool hasValidTosuScores,
            TosuTeamScores tosuScores,
            int currentBeatmapId,
            double? currentMultiplier,
            int tosuBeatmapId,
            double tosuMultiplier)
        {
            bool useTosu = enabled
                           && hasValidTosuScores
                           && currentMultiplier != null
                           && currentBeatmapId == tosuBeatmapId
                           && currentMultiplier.Value.Equals(tosuMultiplier);

            return useTosu ? tosuScores : fileScores;
        }
    }
}
