// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Tests.NonVisual
{
    [TestFixture]
    public class LadderInfoSerialisationTest
    {
        [Test]
        public void TestDeserialise()
        {
            var ladder = createSampleLadder();
            string serialised = JsonConvert.SerializeObject(ladder);

            JsonConvert.DeserializeObject<LadderInfo>(serialised, new JsonPointConverter());
        }

        [Test]
        public void TestSerialise()
        {
            var ladder = createSampleLadder();
            JsonConvert.SerializeObject(ladder);
        }

        [Test]
        public void TestOneVsOneModeSerialisation()
        {
            var ladder = createSampleLadder();
            ladder.OneVsOneMode.Value = true;

            string serialised = JsonConvert.SerializeObject(ladder);
            var deserialised = JsonConvert.DeserializeObject<LadderInfo>(serialised, new JsonPointConverter());

            Assert.That(deserialised, Is.Not.Null);
            Assert.That(deserialised!.OneVsOneMode.Value, Is.True);
        }

        [Test]
        public void TestWipeChromaAreaSerialisation()
        {
            var ladder = createSampleLadder();
            ladder.WipeChromaArea.Value = true;

            string serialised = JsonConvert.SerializeObject(ladder);
            var deserialised = JsonConvert.DeserializeObject<LadderInfo>(serialised, new JsonPointConverter());

            Assert.That(deserialised, Is.Not.Null);
            Assert.That(deserialised!.WipeChromaArea.Value, Is.True);
        }

        private static LadderInfo createSampleLadder()
        {
            var match = TournamentTestScene.CreateSampleMatch();

            return new LadderInfo
            {
                PlayersPerTeam = { Value = 4 },
                Teams =
                {
                    match.Team1.Value!,
                    match.Team2.Value!,
                },
                Rounds =
                {
                    new TournamentRound
                    {
                        Beatmaps =
                        {
                            new RoundBeatmap { Beatmap = TournamentTestScene.CreateSampleBeatmap() },
                            new RoundBeatmap { Beatmap = TournamentTestScene.CreateSampleBeatmap() },
                        }
                    }
                },

                Matches =
                {
                    match,
                },
                Progressions =
                {
                    new TournamentProgression(1, 2),
                    new TournamentProgression(1, 3, true),
                }
            };
        }
    }
}
