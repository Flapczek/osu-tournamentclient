// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Graphics;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Screens.Gameplay.Components;
using osuTK;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestSceneMatchHeader : TournamentTestScene
    {
        public TestSceneMatchHeader()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(50),
                Children = new Drawable[]
                {
                    new TournamentSpriteText { Text = "with logo", Font = OsuFont.Torus.With(size: 30) },
                    new MatchHeader(),
                    new TournamentSpriteText { Text = "without logo", Font = OsuFont.Torus.With(size: 30) },
                    new MatchHeader { ShowLogo = false },
                    new TournamentSpriteText { Text = "without scores", Font = OsuFont.Torus.With(size: 30) },
                    new MatchHeader { ShowScores = false },
                }
            };
        }

        [Test]
        public void TestOneVsOneMode()
        {
            AddStep("disable 1v1 mode", () => Ladder.OneVsOneMode.Value = false);
            AddUntilStep("red team labels shown", () => countHeadersWithText("TEAM RED"), () => Is.EqualTo(3));
            AddUntilStep("blue team labels shown", () => countHeadersWithText("TEAM BLUE"), () => Is.EqualTo(3));

            AddStep("enable 1v1 mode", () => Ladder.OneVsOneMode.Value = true);
            AddUntilStep("player 1 labels shown", () => countHeadersWithText("PLAYER 1"), () => Is.EqualTo(3));
            AddUntilStep("player 2 labels shown", () => countHeadersWithText("PLAYER 2"), () => Is.EqualTo(3));
        }

        private int countHeadersWithText(string text) =>
            this.ChildrenOfType<DrawableTeamHeader>().Count(header => header.Text.Text.ToString() == text);
    }
}
