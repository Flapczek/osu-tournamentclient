// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Schedule;
using osuTK;

namespace osu.Game.Tournament.Tests.Screens
{
    public partial class TestSceneScheduleScreen : TournamentScreenTestScene
    {
        public override void SetUpSteps()
        {
            AddStep("clear matches", () => Ladder.Matches.Clear());

            base.SetUpSteps();
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new TourneyVideo("main") { RelativeSizeAxes = Axes.Both });
            Add(new ScheduleScreen());
        }

        [Test]
        public void TestCurrentMatchTime()
        {
            setMatchDate(TimeSpan.FromDays(-1));
            setMatchDate(TimeSpan.FromSeconds(5));
            setMatchDate(TimeSpan.FromMinutes(4));
            setMatchDate(TimeSpan.FromHours(3));
        }

        [Test]
        public void TestNoCurrentMatch()
        {
            AddStep("Set null current match", () => Ladder.CurrentMatch.Value = null);
        }

        [Test]
        public void TestUpcomingMatches()
        {
            AddStep("Add upcoming match", () =>
            {
                var tournamentMatch = CreateSampleMatch();

                tournamentMatch.Date.Value = DateTimeOffset.UtcNow.AddMinutes(5);
                tournamentMatch.Completed.Value = false;

                Ladder.Matches.Add(tournamentMatch);
            });
        }

        [Test]
        public void TestRecentMatches()
        {
            AddStep("Add recent match", () =>
            {
                var tournamentMatch = CreateSampleMatch();

                tournamentMatch.Date.Value = DateTimeOffset.UtcNow;
                tournamentMatch.Completed.Value = true;
                tournamentMatch.Team1Score.Value = tournamentMatch.PointsToWin;
                tournamentMatch.Team2Score.Value = tournamentMatch.PointsToWin / 2;

                Ladder.Matches.Add(tournamentMatch);
            });
        }

        [Test]
        public void TestOneVsOneAvatarSize()
        {
            AddStep("add single-player match", () =>
            {
                Ladder.CurrentMatch.Value = null;

                var match = CreateSampleMatch();
                setSinglePlayer(match.Team1.Value!, 2);
                setSinglePlayer(match.Team2.Value!, 3);
                match.Date.Value = DateTimeOffset.UtcNow.AddMinutes(5);

                Ladder.OneVsOneMode.Value = true;
                Ladder.Matches.Add(match);
            });

            AddUntilStep("both avatars use large dimensions", () =>
                this.ChildrenOfType<ScheduleScreen.ScheduleMatch>()
                    .SelectMany(match => match.ChildrenOfType<DrawableTeamFlag>())
                    .Count(flag => flag.Size == new Vector2(68)), () => Is.EqualTo(2));

            AddStep("disable 1v1 mode", () => Ladder.OneVsOneMode.Value = false);
            AddUntilStep("both flags restore normal dimensions", () =>
                this.ChildrenOfType<ScheduleScreen.ScheduleMatch>()
                    .SelectMany(match => match.ChildrenOfType<DrawableTeamFlag>())
                    .Count(flag => flag.Size == new Vector2(75, 54)), () => Is.EqualTo(2));
        }

        private void setMatchDate(TimeSpan relativeTime)
            // Humanizer cannot handle negative timespans.
            => AddStep($"start time is {relativeTime}", () =>
            {
                var match = CreateSampleMatch();
                match.Date.Value = DateTimeOffset.Now + relativeTime;
                Ladder.CurrentMatch.Value = match;
            });

        private static void setSinglePlayer(TournamentTeam team, int userId)
        {
            team.Players.Clear();
            team.Players.Add(new TournamentUser { OnlineID = userId });
        }
    }
}
