// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Game.Tests;

namespace osu.Game.Tournament.Tests.NonVisual
{
    [TestFixture]
    public class TournamentAudioTest : TournamentHostTest
    {
        [Test]
        public void TestMutedOnStartupWithoutChangingConfiguration()
        {
            using (HeadlessGameHost host = new CleanRunHeadlessGameHost())
            {
                try
                {
                    var tournament = LoadTournament(host, new TournamentGame());
                    var audio = tournament.Dependencies.Get<AudioManager>();

                    WaitForOrAssert(() => audio.AggregateVolume.Value == 0, "Tournament audio was not muted.");

                    Assert.That(tournament.Dependencies.Get<FrameworkConfigManager>().Get<double>(FrameworkSetting.VolumeUniversal), Is.GreaterThan(0));
                }
                finally
                {
                    host.Exit();
                }
            }
        }
    }
}
