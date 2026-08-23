// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Tournament.IPC;

namespace osu.Game.Tournament.Tests.NonVisual
{
    [TestFixture]
    public class TosuScoreParserTest
    {
        [Test]
        public void TestAppliesMultiplierOnlyToEasyPlayers()
        {
            const string json = """
                                {
                                  "beatmap": { "id": 123 },
                                  "tourney": {
                                    "clients": [
                                      { "team": "left", "play": { "score": 1000, "mods": { "array": [{ "acronym": "EZ" }] } } },
                                      { "team": "left", "play": { "score": 500, "mods": { "array": [] } } },
                                      { "team": "right", "play": { "score": 2000, "mods": { "array": [{ "acronym": "EZ" }, { "acronym": "HD" }] } } }
                                    ]
                                  }
                                }
                                """;

            var result = TosuScoreParser.TryParse(json, 123, 1.25, out TosuTeamScores scores);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(TosuScoreParseResult.Valid));
                Assert.That(scores.Score1, Is.EqualTo(1750));
                Assert.That(scores.Score2, Is.EqualTo(2500));
            });
        }

        [Test]
        public void TestSupportsLongScoresAndTruncatesCorrection()
        {
            const string json = """
                                { "beatmap": { "id": 123 }, "tourney": { "clients": [
                                  { "team": "left", "play": { "score": 3000000000, "mods": { "array": [{ "acronym": "EZ" }] } } }
                                ] } }
                                """;

            var result = TosuScoreParser.TryParse(json, 123, 1.001, out TosuTeamScores scores);

            Assert.That(result, Is.EqualTo(TosuScoreParseResult.Valid));
            Assert.That(scores.Score1, Is.EqualTo(3002999999));
        }

        [Test]
        public void TestWrongBeatmapRejected()
        {
            const string json = """
                                { "beatmap": { "id": 999 }, "tourney": { "clients": [
                                  { "team": "left", "play": { "score": 1000, "mods": { "array": [] } } }
                                ] } }
                                """;

            Assert.That(TosuScoreParser.TryParse(json, 123, 1.25, out _), Is.EqualTo(TosuScoreParseResult.WrongBeatmap));
        }

        [TestCase("{}", TosuScoreParseResult.WaitingForClients)]
        [TestCase("{ \"tourney\": { \"clients\": [] } }", TosuScoreParseResult.WaitingForClients)]
        [TestCase("{", TosuScoreParseResult.Invalid)]
        [TestCase("{ \"beatmap\": { \"id\": 123 }, \"tourney\": { \"clients\": [{ \"team\": \"other\", \"play\": { \"score\": 1 } }] } }", TosuScoreParseResult.Invalid)]
        [TestCase("{ \"beatmap\": { \"id\": 123 }, \"tourney\": { \"clients\": [{ \"team\": \"left\", \"play\": {} }] } }", TosuScoreParseResult.Invalid)]
        [TestCase("{ \"beatmap\": { \"id\": \"invalid\" }, \"tourney\": { \"clients\": [{ \"team\": \"left\", \"play\": { \"score\": 1, \"mods\": { \"array\": [] } } }] } }", TosuScoreParseResult.Invalid)]
        [TestCase("{ \"beatmap\": { \"id\": 123 }, \"tourney\": { \"clients\": [{ \"team\": \"left\", \"play\": { \"score\": 1 } }] } }", TosuScoreParseResult.Invalid)]
        public void TestInvalidOrIncompletePayloadFallsBack(string json, int expected)
        {
            Assert.That((int)TosuScoreParser.TryParse(json, 123, 1.25, out _), Is.EqualTo(expected));
        }
    }
}
