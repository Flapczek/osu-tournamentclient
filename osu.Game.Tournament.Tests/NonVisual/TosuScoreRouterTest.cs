// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Tournament.IPC;

namespace osu.Game.Tournament.Tests.NonVisual
{
    [TestFixture]
    public class TosuScoreRouterTest
    {
        private static readonly TosuTeamScores file_scores = new TosuTeamScores(100, 200);
        private static readonly TosuTeamScores tosu_scores = new TosuTeamScores(300, 400);

        [TestCase(false, true, 123, 1.25, 123, 1.25, false, TestName = "Disabled uses file IPC")]
        [TestCase(true, true, 123, null, 123, 1.25, false, TestName = "Map without multiplier uses file IPC")]
        [TestCase(true, false, 123, 1.25, 123, 1.25, false, TestName = "Unavailable tosu uses file IPC")]
        [TestCase(true, true, 123, 1.25, 999, 1.25, false, TestName = "Wrong map uses file IPC")]
        [TestCase(true, true, 123, 1.25, 123, 1.5, false, TestName = "Stale multiplier uses file IPC")]
        [TestCase(true, true, 123, 1.25, 123, 1.25, true, TestName = "Valid current map uses tosu")]
        public void TestScoreSelection(bool enabled, bool valid, int currentBeatmap, double? currentMultiplier, int tosuBeatmap, double tosuMultiplier, bool expectTosu)
        {
            TosuTeamScores selected = TosuScoreRouter.SelectScores(
                enabled,
                file_scores,
                valid,
                tosu_scores,
                currentBeatmap,
                currentMultiplier,
                tosuBeatmap,
                tosuMultiplier);

            Assert.That(selected, Is.EqualTo(expectTosu ? tosu_scores : file_scores));
        }
    }
}
