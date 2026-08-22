// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using osu.Game.Tournament.IO;
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

        [Test]
        public void TestUseIpcForMapPoolProgressionSerialisation()
        {
            var defaultLadder = JsonConvert.DeserializeObject<LadderInfo>("{}", new JsonPointConverter());
            Assert.That(defaultLadder, Is.Not.Null);
            Assert.That(defaultLadder!.UseIPCForMapPoolProgression.Value, Is.False);

            var ladder = createSampleLadder();
            ladder.UseIPCForMapPoolProgression.Value = true;

            string serialised = JsonConvert.SerializeObject(ladder);
            var deserialised = JsonConvert.DeserializeObject<LadderInfo>(serialised, new JsonPointConverter());

            Assert.That(deserialised, Is.Not.Null);
            Assert.That(deserialised!.UseIPCForMapPoolProgression.Value, Is.True);
        }

        [Test]
        public void TestCompatibleSerialisationRemovesEnhancedProperties()
        {
            var ladder = createSampleLadder();
            ladder.OneVsOneMode.Value = true;
            ladder.WipeChromaArea.Value = true;
            ladder.UseIPCForMapPoolProgression.Value = true;

            string serialised = JsonConvert.SerializeObject(ladder);
            var compatibleBracket = JObject.Parse(CompatibleBracketSerialiser.CreateCompatibleBracket(serialised));

            Assert.Multiple(() =>
            {
                Assert.That(compatibleBracket.Property(nameof(LadderInfo.OneVsOneMode)), Is.Null);
                Assert.That(compatibleBracket.Property(nameof(LadderInfo.WipeChromaArea)), Is.Null);
                Assert.That(compatibleBracket.Property(nameof(LadderInfo.UseIPCForMapPoolProgression)), Is.Null);
            });
        }

        [Test]
        public void TestCompatibleSerialisationPreservesStandardProperties()
        {
            var ladder = createSampleLadder();
            ladder.AutoProgressScreens.Value = true;
            ladder.SplitMapPoolByMods.Value = false;
            ladder.DisplayTeamSeeds.Value = true;

            string serialised = JsonConvert.SerializeObject(ladder);
            var compatibleBracket = JObject.Parse(CompatibleBracketSerialiser.CreateCompatibleBracket(serialised));
            var originalBracket = JObject.Parse(serialised);

            Assert.Multiple(() =>
            {
                Assert.That(compatibleBracket[nameof(LadderInfo.Teams)], Is.EqualTo(originalBracket[nameof(LadderInfo.Teams)]));
                Assert.That(compatibleBracket[nameof(LadderInfo.Rounds)], Is.EqualTo(originalBracket[nameof(LadderInfo.Rounds)]));
                Assert.That(compatibleBracket[nameof(LadderInfo.Matches)], Is.EqualTo(originalBracket[nameof(LadderInfo.Matches)]));
                Assert.That(compatibleBracket[nameof(LadderInfo.AutoProgressScreens)], Is.EqualTo(originalBracket[nameof(LadderInfo.AutoProgressScreens)]));
                Assert.That(compatibleBracket[nameof(LadderInfo.SplitMapPoolByMods)], Is.EqualTo(originalBracket[nameof(LadderInfo.SplitMapPoolByMods)]));
                Assert.That(compatibleBracket[nameof(LadderInfo.DisplayTeamSeeds)], Is.EqualTo(originalBracket[nameof(LadderInfo.DisplayTeamSeeds)]));
            });
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
