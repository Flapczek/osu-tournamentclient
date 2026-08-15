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
        private readonly MatchHeader standardHeader;
        private readonly MatchHeader gameplayHeader;

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
                    standardHeader = new MatchHeader(),
                    new TournamentSpriteText { Text = "without logo", Font = OsuFont.Torus.With(size: 30) },
                    gameplayHeader = new MatchHeader { ShowLogo = false },
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

        [Test]
        public void TestRoundNameAlignment()
        {
            float originalGameplayPosition = 0;
            float originalStandardPosition = 0;

            AddStep("disable alignment", () => Ladder.AlignRoundNameWithSeeds.Value = false);
            AddStep("display team seeds", () => Ladder.DisplayTeamSeeds.Value = true);
            AddUntilStep("team seeds loaded", () => gameplayHeader.ChildrenOfType<DrawableTeamSeed>().Count(), () => Is.EqualTo(2));
            AddStep("store original positions", () =>
            {
                originalGameplayPosition = getRoundCentreY(gameplayHeader);
                originalStandardPosition = getRoundCentreY(standardHeader);
            });

            AddStep("enable alignment", () => Ladder.AlignRoundNameWithSeeds.Value = true);
            AddAssert("round name moved down", () => getRoundCentreY(gameplayHeader), () => Is.GreaterThan(originalGameplayPosition));
            AddAssert("round name aligned with first seed", () => getRoundCentreY(gameplayHeader),
                () => Is.EqualTo(getSeedCentreY(gameplayHeader, 0)).Within(2));
            AddAssert("round name aligned with second seed", () => getRoundCentreY(gameplayHeader),
                () => Is.EqualTo(getSeedCentreY(gameplayHeader, 1)).Within(2));
            AddAssert("standard header unchanged", () => getRoundCentreY(standardHeader), () => Is.EqualTo(originalStandardPosition).Within(0.01f));

            float alignedPosition = 0;
            AddStep("store aligned position", () => alignedPosition = getRoundCentreY(gameplayHeader));
            AddStep("hide team seeds", () => Ladder.DisplayTeamSeeds.Value = false);
            AddAssert("alignment remains when seeds hidden", () => getRoundCentreY(gameplayHeader), () => Is.EqualTo(alignedPosition).Within(0.01f));

            AddStep("disable alignment", () => Ladder.AlignRoundNameWithSeeds.Value = false);
            AddAssert("original position restored", () => getRoundCentreY(gameplayHeader), () => Is.EqualTo(originalGameplayPosition).Within(0.01f));
        }

        private int countHeadersWithText(string text) =>
            this.ChildrenOfType<DrawableTeamHeader>().Count(header => header.Text.Text.ToString() == text);

        private static float getRoundCentreY(MatchHeader header) =>
            header.ChildrenOfType<MatchRoundDisplay>().Single().ScreenSpaceDrawQuad.Centre.Y;

        private static float getSeedCentreY(MatchHeader header, int index) =>
            header.ChildrenOfType<DrawableTeamSeed>().ElementAt(index).ScreenSpaceDrawQuad.Centre.Y;
    }
}
