// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json.Linq;

namespace osu.Game.Tournament.IPC
{
    internal enum TosuScoreParseResult
    {
        Invalid,
        WaitingForClients,
        WrongBeatmap,
        Valid,
    }

    internal readonly record struct TosuTeamScores(long Score1, long Score2);

    internal static class TosuScoreParser
    {
        public static bool HasTournamentClients(string json)
        {
            try
            {
                return JObject.Parse(json)["tourney"]?["clients"] is JArray { Count: > 0 };
            }
            catch
            {
                return false;
            }
        }

        public static TosuScoreParseResult TryParse(string json, int expectedBeatmapId, double ezMultiplier, out TosuTeamScores scores)
        {
            scores = default;

            if (expectedBeatmapId <= 0 || !double.IsFinite(ezMultiplier) || ezMultiplier < 0.1 || ezMultiplier > 10)
                return TosuScoreParseResult.Invalid;

            JObject root;

            try
            {
                root = JObject.Parse(json);
            }
            catch
            {
                return TosuScoreParseResult.Invalid;
            }

            try
            {
                return parse(root, expectedBeatmapId, ezMultiplier, out scores);
            }
            catch
            {
                scores = default;
                return TosuScoreParseResult.Invalid;
            }
        }

        private static TosuScoreParseResult parse(JObject root, int expectedBeatmapId, double ezMultiplier, out TosuTeamScores scores)
        {
            scores = default;

            var clients = root["tourney"]?["clients"] as JArray;

            if (clients == null || clients.Count == 0)
                return TosuScoreParseResult.WaitingForClients;

            if (root["beatmap"]?["id"]?.Value<int?>() != expectedBeatmapId)
                return TosuScoreParseResult.WrongBeatmap;

            long score1 = 0;
            long score2 = 0;

            foreach (JToken client in clients)
            {
                string? team = client["team"]?.Value<string>();
                long? rawScore = client["play"]?["score"]?.Value<long?>();

                if (rawScore == null || rawScore < 0 || team is not ("left" or "right"))
                    return TosuScoreParseResult.Invalid;

                var mods = client["play"]?["mods"]?["array"] as JArray;

                if (mods == null)
                    return TosuScoreParseResult.Invalid;

                bool hasEasy = false;

                foreach (JToken mod in mods)
                {
                    if (string.Equals(mod["acronym"]?.Value<string>(), "EZ", StringComparison.OrdinalIgnoreCase))
                    {
                        hasEasy = true;
                        break;
                    }
                }

                long adjustedScore;

                try
                {
                    adjustedScore = hasEasy ? checked((long)(rawScore.Value * ezMultiplier)) : rawScore.Value;
                }
                catch (OverflowException)
                {
                    return TosuScoreParseResult.Invalid;
                }

                try
                {
                    if (team == "left")
                        score1 = checked(score1 + adjustedScore);
                    else
                        score2 = checked(score2 + adjustedScore);
                }
                catch (OverflowException)
                {
                    return TosuScoreParseResult.Invalid;
                }
            }

            scores = new TosuTeamScores(score1, score2);
            return TosuScoreParseResult.Valid;
        }
    }
}
