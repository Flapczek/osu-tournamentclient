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
    public partial class TestScenePickOwnerIndicator : TournamentTestScene
    {
        private TournamentBeatmap beatmap = null!;
        private TournamentBeatmapPanel mapPoolPanel = null!;
        private TournamentBeatmapPanel gameplayPanel = null!;
        private TournamentBeatmapPanel panelWithoutIndicator = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            beatmap = CreateSampleBeatmap();

            Child = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Children = new Drawable[]
                {
                    mapPoolPanel = new TournamentBeatmapPanel(beatmap, "NM", Anchor.BottomLeft),
                    gameplayPanel = new TournamentBeatmapPanel(beatmap, pickOwnerIndicatorAnchor: Anchor.BottomRight),
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

                Ladder.OneVsOneMode.Value = false;
            });
        }

        [Test]
        public void TestPickOwnerDisplay()
        {
            AddAssert("non opted-in panel has no indicator", () => !panelWithoutIndicator.ChildrenOfType<DrawablePickOwnerIndicator>().Any());

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

            AddStep("replace pick with ban", () =>
            {
                Ladder.CurrentMatch.Value!.PicksBans.Clear();
                Ladder.CurrentMatch.Value.PicksBans.Add(new BeatmapChoice
                {
                    BeatmapID = beatmap.OnlineID,
                    Team = TeamColour.Red,
                    Type = ChoiceType.Ban,
                });
            });
            AddAssert("hidden for ban", () => getIndicator(mapPoolPanel).Alpha == 0);

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

        private static DrawablePickOwnerIndicator getIndicator(TournamentBeatmapPanel panel) => panel.ChildrenOfType<DrawablePickOwnerIndicator>().Single();
    }
}
