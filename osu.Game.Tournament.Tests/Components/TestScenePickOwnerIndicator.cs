// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestSceneChoiceOwnerIndicator : TournamentTestScene
    {
        private TournamentBeatmap beatmap = null!;
        private TournamentBeatmap tiebreakerBeatmap = null!;
        private TournamentBeatmapPanel mapPoolPanel = null!;
        private TournamentBeatmapPanel gameplayPanel = null!;
        private TournamentBeatmapPanel tiebreakerMapPoolPanel = null!;
        private TournamentBeatmapPanel tiebreakerGameplayPanel = null!;
        private TournamentBeatmapPanel panelWithoutIndicator = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            beatmap = CreateSampleBeatmap();
            tiebreakerBeatmap = CreateSampleBeatmap();

            Child = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Children = new Drawable[]
                {
                    mapPoolPanel = new TournamentBeatmapPanel(beatmap, "NM", Anchor.BottomLeft, showBanOwnerIndicator: true),
                    gameplayPanel = new TournamentBeatmapPanel(beatmap, choiceOwnerIndicatorAnchor: Anchor.BottomRight),
                    tiebreakerMapPoolPanel = new TournamentBeatmapPanel(tiebreakerBeatmap, " tb ", Anchor.BottomLeft, showBanOwnerIndicator: true),
                    tiebreakerGameplayPanel = new TournamentBeatmapPanel(tiebreakerBeatmap, choiceOwnerIndicatorAnchor: Anchor.BottomRight),
                    panelWithoutIndicator = new TournamentBeatmapPanel(beatmap),
                }
            };
        }

        [SetUpSteps]
        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("reset match", () =>
            {
                var match = Ladder.CurrentMatch.Value!;
                match.PicksBans.Clear();

                match.Team1.Value!.Players.Clear();
                match.Team1.Value.Players.Add(new TournamentUser { Username = "flapczek" });

                match.Team2.Value!.Players.Clear();
                match.Team2.Value.Players.Add(new TournamentUser { Username = "opponent" });

                match.Round.Value!.Beatmaps.Clear();
                match.Round.Value.Beatmaps.Add(new RoundBeatmap
                {
                    ID = tiebreakerBeatmap.OnlineID,
                    Beatmap = tiebreakerBeatmap,
                    Mods = " TB ",
                });

                Ladder.OneVsOneMode.Value = false;
            });
        }

        [Test]
        public void TestChoiceOwnerDisplay()
        {
            AddAssert("non opted-in panel has no indicator", () => !panelWithoutIndicator.ChildrenOfType<DrawableChoiceOwnerIndicator>().Any());

            addPick(TeamColour.Red);
            AddAssert("hidden outside 1v1", () => getIndicator(mapPoolPanel).Alpha == 0);

            AddStep("enable 1v1", () => Ladder.OneVsOneMode.Value = true);
            assertIndicator(mapPoolPanel, Anchor.BottomLeft, "PICKED BY FLAPCZEK");
            assertIndicator(gameplayPanel, Anchor.BottomRight, "PICKED BY FLAPCZEK");
            AddAssert("pick flash covers indicator", () => mapPoolPanel.ChildrenOfType<Box>().Single(box => box.Name == "Pick flash").Depth < getIndicator(mapPoolPanel).Depth);

            AddStep("remove pick", () => Ladder.CurrentMatch.Value!.PicksBans.Clear());
            AddAssert("hidden after pick removed", () => getIndicator(mapPoolPanel).Alpha == 0);

            addPick(TeamColour.Blue);
            assertIndicator(mapPoolPanel, Anchor.BottomLeft, "PICKED BY OPPONENT");

            AddStep("replace pick with red ban", () =>
            {
                Ladder.CurrentMatch.Value!.PicksBans.Clear();
                Ladder.CurrentMatch.Value.PicksBans.Add(new BeatmapChoice
                {
                    BeatmapID = beatmap.OnlineID,
                    Team = TeamColour.Red,
                    Type = ChoiceType.Ban,
                });
            });
            assertIndicator(mapPoolPanel, Anchor.BottomLeft, "BANNED BY FLAPCZEK");
            AddAssert("ban card content remains dimmed", () => getBeatmapContent(mapPoolPanel).Alpha == 0.5f);
            AddAssert("ban indicator is fully opaque", () => getIndicator(mapPoolPanel).DrawColourInfo.Colour.AverageColour.Linear.A == 1);
            AddAssert("ban indicator keeps full team colour", () => getIndicatorBackground(mapPoolPanel).DrawColourInfo.Colour.AverageColour == TournamentGame.COLOUR_RED);
            AddAssert("ban hidden on gameplay", () => getIndicator(gameplayPanel).Alpha == 0);

            AddStep("replace with blue ban", () =>
            {
                Ladder.CurrentMatch.Value!.PicksBans.Clear();
                Ladder.CurrentMatch.Value.PicksBans.Add(new BeatmapChoice
                {
                    BeatmapID = beatmap.OnlineID,
                    Team = TeamColour.Blue,
                    Type = ChoiceType.Ban,
                });
            });
            assertIndicator(mapPoolPanel, Anchor.BottomLeft, "BANNED BY OPPONENT");

            AddStep("disable 1v1 with ban", () => Ladder.OneVsOneMode.Value = false);
            AddAssert("ban hidden outside 1v1", () => getIndicator(mapPoolPanel).Alpha == 0);

            AddStep("enable 1v1 with ban", () => Ladder.OneVsOneMode.Value = true);
            assertIndicator(mapPoolPanel, Anchor.BottomLeft, "BANNED BY OPPONENT");

            AddStep("remove ban", () => Ladder.CurrentMatch.Value!.PicksBans.Clear());
            AddAssert("hidden after ban removed", () => getIndicator(mapPoolPanel).Alpha == 0);

            AddStep("add second red player and pick", () =>
            {
                Ladder.CurrentMatch.Value!.PicksBans.Clear();
                Ladder.CurrentMatch.Value.Team1.Value!.Players.Add(new TournamentUser { Username = "another" });
                Ladder.CurrentMatch.Value.PicksBans.Add(createPick(TeamColour.Red));
            });
            AddAssert("hidden for multiple players", () => getIndicator(mapPoolPanel).Alpha == 0);

            AddStep("set empty blue username and pick", () =>
            {
                Ladder.CurrentMatch.Value!.PicksBans.Clear();
                Ladder.CurrentMatch.Value.Team2.Value!.Players[0].Username = string.Empty;
                Ladder.CurrentMatch.Value.PicksBans.Add(createPick(TeamColour.Blue));
            });
            AddAssert("hidden for empty username", () => getIndicator(mapPoolPanel).Alpha == 0);
        }

        [Test]
        public void TestTiebreakerDisplay()
        {
            AddStep("remove team players", () =>
            {
                Ladder.CurrentMatch.Value!.Team1.Value!.Players.Clear();
                Ladder.CurrentMatch.Value.Team2.Value!.Players.Clear();
            });

            AddStep("pick tiebreaker outside 1v1", () => Ladder.CurrentMatch.Value!.PicksBans.Add(new BeatmapChoice
            {
                BeatmapID = tiebreakerBeatmap.OnlineID,
                Team = TeamColour.Red,
                Type = ChoiceType.Pick,
            }));
            assertTiebreakerIndicator(tiebreakerMapPoolPanel, Anchor.BottomLeft);
            assertTiebreakerIndicator(tiebreakerGameplayPanel, Anchor.BottomRight);
            assertTiebreakerBorder(tiebreakerMapPoolPanel, Anchor.BottomLeft);
            assertTiebreakerBorder(tiebreakerGameplayPanel, Anchor.BottomRight);

            AddStep("enable 1v1", () => Ladder.OneVsOneMode.Value = true);
            assertTiebreakerIndicator(tiebreakerMapPoolPanel, Anchor.BottomLeft);
            assertTiebreakerIndicator(tiebreakerGameplayPanel, Anchor.BottomRight);

            AddStep("replace with blue tiebreaker pick", () =>
            {
                Ladder.CurrentMatch.Value!.PicksBans.Clear();
                Ladder.CurrentMatch.Value.PicksBans.Add(new BeatmapChoice
                {
                    BeatmapID = tiebreakerBeatmap.OnlineID,
                    Team = TeamColour.Blue,
                    Type = ChoiceType.Pick,
                });
            });
            assertTiebreakerIndicator(tiebreakerMapPoolPanel, Anchor.BottomLeft);
            assertTiebreakerIndicator(tiebreakerGameplayPanel, Anchor.BottomRight);
            assertTiebreakerBorder(tiebreakerMapPoolPanel, Anchor.BottomLeft);
            assertTiebreakerBorder(tiebreakerGameplayPanel, Anchor.BottomRight);

            AddStep("remove tiebreaker pick", () => Ladder.CurrentMatch.Value!.PicksBans.Clear());
            AddAssert("map pool tiebreaker hidden", () => getIndicator(tiebreakerMapPoolPanel).Alpha == 0);
            AddAssert("gameplay tiebreaker hidden", () => getIndicator(tiebreakerGameplayPanel).Alpha == 0);

            AddStep("ban tiebreaker", () =>
            {
                Ladder.CurrentMatch.Value!.Team1.Value!.Players.Add(new TournamentUser { Username = "flapczek" });
                Ladder.CurrentMatch.Value.PicksBans.Add(new BeatmapChoice
                {
                    BeatmapID = tiebreakerBeatmap.OnlineID,
                    Team = TeamColour.Red,
                    Type = ChoiceType.Ban,
                });
            });
            assertIndicator(tiebreakerMapPoolPanel, Anchor.BottomLeft, "BANNED BY FLAPCZEK");
            AddAssert("tiebreaker ban keeps team border",
                () => getBeatmapContent(tiebreakerMapPoolPanel).BorderColour.AverageColour == TournamentGame.COLOUR_RED);
            AddAssert("tiebreaker ban hidden on gameplay", () => getIndicator(tiebreakerGameplayPanel).Alpha == 0);

            AddStep("disable 1v1", () => Ladder.OneVsOneMode.Value = false);
            AddAssert("tiebreaker ban hidden outside 1v1", () => getIndicator(tiebreakerMapPoolPanel).Alpha == 0);
        }

        private void addPick(TeamColour team) => AddStep($"add {team} pick", () => Ladder.CurrentMatch.Value!.PicksBans.Add(createPick(team)));

        private BeatmapChoice createPick(TeamColour team) => new BeatmapChoice
        {
            BeatmapID = beatmap.OnlineID,
            Team = team,
            Type = ChoiceType.Pick,
        };

        private void assertIndicator(TournamentBeatmapPanel panel, Anchor anchor, string text)
        {
            AddAssert($"{anchor} indicator visible", () => getIndicator(panel).Alpha == 1);
            AddAssert($"{anchor} indicator anchored", () => getIndicator(panel).Anchor == anchor && getIndicator(panel).Origin == anchor);
            AddAssert($"{anchor} indicator text", () => getIndicator(panel).Text.Text.ToString() == text);
        }

        private void assertTiebreakerIndicator(TournamentBeatmapPanel panel, Anchor anchor)
        {
            assertIndicator(panel, anchor, "TIEBREAKER");
            AddAssert($"{anchor} tiebreaker background is white",
                () => getIndicatorBackground(panel).DrawColourInfo.Colour.AverageColour == TournamentGame.ELEMENT_BACKGROUND_COLOUR);
            AddAssert($"{anchor} tiebreaker text is black",
                () => getIndicator(panel).Text.DrawColourInfo.Colour.AverageColour == TournamentGame.ELEMENT_FOREGROUND_COLOUR);
            AddAssert($"{anchor} tiebreaker is fully opaque",
                () => getIndicator(panel).DrawColourInfo.Colour.AverageColour.Linear.A == 1);
        }

        private void assertTiebreakerBorder(TournamentBeatmapPanel panel, Anchor anchor)
        {
            AddAssert($"{anchor} tiebreaker border is visible", () => getBeatmapContent(panel).BorderThickness == 6);
            AddAssert($"{anchor} tiebreaker border is white",
                () => getBeatmapContent(panel).BorderColour.AverageColour == TournamentGame.ELEMENT_BACKGROUND_COLOUR);
        }

        private static DrawableChoiceOwnerIndicator getIndicator(TournamentBeatmapPanel panel) => panel.ChildrenOfType<DrawableChoiceOwnerIndicator>().Single();

        private static Container getBeatmapContent(TournamentBeatmapPanel panel) => panel.ChildrenOfType<Container>().Single(container => container.Name == "Beatmap content");

        private static Box getIndicatorBackground(TournamentBeatmapPanel panel) => getIndicator(panel).ChildrenOfType<Box>().Single();
    }
}
