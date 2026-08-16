// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.TeamIntro;
using osuTK;

namespace osu.Game.Tournament.Tests.Screens
{
    public partial class TestSceneTeamIntroScreen : TournamentScreenTestScene
    {
        [Cached]
        private readonly LadderInfo ladder = new LadderInfo();

        [BackgroundDependencyLoader]
        private void load()
        {
            ladder.CurrentMatch.Value = new TournamentMatch
            {
                Team1 = { Value = Ladder.Teams.FirstOrDefault(t => t.Acronym.Value == "USA") },
                Team2 = { Value = Ladder.Teams.FirstOrDefault(t => t.Acronym.Value == "JPN") },
                Round = { Value = Ladder.Rounds.FirstOrDefault(g => g.Name.Value == "Finals") }
            };

            Add(new TeamIntroScreen
            {
                FillMode = FillMode.Fit,
                FillAspectRatio = 16 / 9f
            });
        }

        [Test]
        public void TestOneVsOneAvatarSize()
        {
            AddStep("set single-player teams", () =>
            {
                var match = CreateSampleMatch();
                setSinglePlayer(match.Team1.Value!, 2);
                setSinglePlayer(match.Team2.Value!, 3);

                ladder.OneVsOneMode.Value = true;
                ladder.CurrentMatch.Value = match;
            });

            AddUntilStep("both avatars use large dimensions", () =>
                this.ChildrenOfType<DrawableTeamFlag>().Count(flag => flag.Size == new Vector2(75)), () => Is.EqualTo(2));

            AddStep("disable 1v1 mode", () => ladder.OneVsOneMode.Value = false);
            AddUntilStep("both flags restore normal dimensions", () =>
                this.ChildrenOfType<DrawableTeamFlag>().Count(flag => flag.Size == new Vector2(75, 54)), () => Is.EqualTo(2));
        }

        private static void setSinglePlayer(TournamentTeam team, int userId)
        {
            team.Players.Clear();
            team.Players.Add(new TournamentUser { OnlineID = userId });
        }
    }
}
