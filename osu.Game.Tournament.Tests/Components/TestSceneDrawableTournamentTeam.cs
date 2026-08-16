// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Tests.Visual;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Drawings.Components;
using osu.Game.Tournament.Screens.Gameplay.Components;
using osu.Game.Tournament.Screens.Ladder.Components;
using osuTK;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestSceneDrawableTournamentTeam : OsuGridTestScene
    {
        [Cached]
        protected LadderInfo Ladder { get; private set; } = new LadderInfo();

        private readonly DrawableTeamFlag singlePlayerFlag;
        private readonly DrawableTeamFlag invalidRosterFlag;
        private readonly DrawableTeamFlag invalidPlayerIdFlag;

        public TestSceneDrawableTournamentTeam()
            : base(4, 3)
        {
            AddToggleStep("toggle seed view", v => Ladder.DisplayTeamSeeds.Value = v);
            AddToggleStep("toggle 1v1 mode", v => Ladder.OneVsOneMode.Value = v);

            var team = new TournamentTeam
            {
                FlagName = { Value = "AU" },
                FullName = { Value = "Australia" },
                Seed = { Value = "#5" },
                Players =
                {
                    new TournamentUser { Username = "ASecretBox" },
                    new TournamentUser { Username = "Dereban" },
                    new TournamentUser { Username = "mReKk" },
                    new TournamentUser { Username = "uyghti" },
                    new TournamentUser { Username = "Parkes" },
                    new TournamentUser { Username = "Shiroha" },
                    new TournamentUser { Username = "Jordan The Bear" },
                },
            };

            var singlePlayerTeam = new TournamentTeam
            {
                FlagName = { Value = "US" },
                FullName = { Value = "Single Player" },
                Players =
                {
                    new TournamentUser
                    {
                        OnlineID = 8210988,
                        Username = "flapczek",
                    },
                },
            };

            var match = new TournamentMatch { Team1 = { Value = team } };

            var invalidPlayerIdTeam = new TournamentTeam
            {
                FlagName = { Value = "JP" },
                Players =
                {
                    new TournamentUser { Username = "Missing ID" },
                },
            };

            int i = 0;

            Cell(i++).AddRange(new Drawable[]
            {
                new TournamentSpriteText { Text = "DrawableTeamFlag" },
                invalidRosterFlag = new DrawableTeamFlag(team)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });

            Cell(i++).AddRange(new Drawable[]
            {
                new TournamentSpriteText { Text = "DrawableTeamTitle" },
                new DrawableTeamTitle(team)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });

            Cell(i++).AddRange(new Drawable[]
            {
                new TournamentSpriteText { Text = "DrawableTeamTitleWithHeader" },
                new DrawableTeamTitleWithHeader(team, TeamColour.Red)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });

            Cell(i++).AddRange(new Drawable[]
            {
                new TournamentSpriteText { Text = "DrawableMatchTeam" },
                new DrawableMatchTeam(team, match, false)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });

            Cell(i++).AddRange(new Drawable[]
            {
                new TournamentSpriteText { Text = "TeamWithPlayers" },
                new DrawableTeamWithPlayers(team, TeamColour.Blue)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });

            Cell(i++).AddRange(new Drawable[]
            {
                new TournamentSpriteText { Text = "GroupTeam" },
                new GroupTeam(team)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });

            Cell(i).AddRange(new Drawable[]
            {
                new TournamentSpriteText { Text = "TeamDisplay" },
                new TeamDisplay(team, TeamColour.Red, new Bindable<int?>(2), 6)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });

            Cell(++i).AddRange(new Drawable[]
            {
                new TournamentSpriteText { Text = "1v1 Player Avatar" },
                singlePlayerFlag = new DrawableTeamFlag(singlePlayerTeam)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });

            Cell(++i).AddRange(new Drawable[]
            {
                new TournamentSpriteText { Text = "Invalid Player ID" },
                invalidPlayerIdFlag = new DrawableTeamFlag(invalidPlayerIdTeam)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });
        }

        [Test]
        public void TestOneVsOneDimensions()
        {
            AddStep("disable 1v1 mode", () => Ladder.OneVsOneMode.Value = false);
            AddUntilStep("single player uses flag dimensions", () => singlePlayerFlag.Size, () => Is.EqualTo(new Vector2(75, 54)));
            AddUntilStep("invalid roster uses flag dimensions", () => invalidRosterFlag.Size, () => Is.EqualTo(new Vector2(75, 54)));
            AddUntilStep("invalid player ID uses flag dimensions", () => invalidPlayerIdFlag.Size, () => Is.EqualTo(new Vector2(75, 54)));

            AddStep("enable 1v1 mode", () => Ladder.OneVsOneMode.Value = true);
            AddUntilStep("single player uses square dimensions", () => singlePlayerFlag.Size, () => Is.EqualTo(new Vector2(54)));
            AddUntilStep("invalid roster keeps flag dimensions", () => invalidRosterFlag.Size, () => Is.EqualTo(new Vector2(75, 54)));
            AddUntilStep("invalid player ID keeps flag dimensions", () => invalidPlayerIdFlag.Size, () => Is.EqualTo(new Vector2(75, 54)));

            AddStep("disable 1v1 mode", () => Ladder.OneVsOneMode.Value = false);
            AddUntilStep("single player restores flag dimensions", () => singlePlayerFlag.Size, () => Is.EqualTo(new Vector2(75, 54)));
        }
    }
}
