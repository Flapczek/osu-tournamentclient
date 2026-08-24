// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay.Components;
using osu.Game.Tournament.Screens.MapPool;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tournament.Tests.Screens
{
    public partial class TestSceneMapPoolScreen : TournamentScreenTestScene
    {
        private MapPoolScreen screen = null!;
        private int firstPickedBeatmapId;
        private int latestPickedBeatmapId;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(screen = new TestMapPoolScreen { Width = 0.7f });
        }

        [SetUpSteps]
        public override void SetUpSteps()
        {
            AddStep("reset state", resetState);
        }

        private void resetState()
        {
            screen.Hide();
            screen.RelativeSizeAxes = Axes.Both;
            screen.Width = 0.7f;

            Ladder.AutoProgressScreens.Value = true;
            Ladder.UseIPCForMapPoolProgression.Value = true;
            Ladder.OneVsOneMode.Value = false;
            Ladder.SplitMapPoolByMods.Value = true;

            IPCInfo.State.Value = TourneyState.Idle;
            IPCInfo.BeatmapID.Value = 0;
            IPCInfo.Beatmap.Value = null;

            Ladder.CurrentMatch.Value = new TournamentMatch();
            Ladder.CurrentMatch.Value = Ladder.Matches.First();
            Ladder.CurrentMatch.Value.PicksBans.Clear();

            firstPickedBeatmapId = 0;
            latestPickedBeatmapId = 0;
            ((TestMapPoolScreen)screen).ResetGameplayTransitionCount();
            screen.Show();
        }

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
        });

        [Test]
        public void TestFewMaps()
        {
            AddStep("load few maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 8; i++)
                    addBeatmap();
            });

            AddStep("reset match", () =>
            {
                Ladder.CurrentMatch.Value = new TournamentMatch();
            });

            assertTwoWide();
        }

        [Test]
        public void TestBeatmapCardHeight()
        {
            AddStep("load beatmap", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();
                addBeatmap();
            });

            AddStep("reset state", resetState);

            AddAssert("card height is 50", () => screen.ChildrenOfType<TournamentBeatmapPanel>().Single().Height == 50);
            AddAssert("small pool starts at Y 160", () => getMapFlows().Y == 160);
        }

        [Test]
        public void TestMapPoolOnlyMovesUpToAvoidChat()
        {
            AddStep("load 15 maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 15; i++)
                    addBeatmap();
            });

            AddStep("reset state", resetState);
            setProductionWidth();
            AddAssert("15 maps stay at Y 160", () => getMapFlows().Y, () => Is.EqualTo(160));
            AddAssert("15 maps leave space above chat", () => getMapFlows().Y + getMapFlows().DrawHeight < screen.DrawHeight - TournamentMatchChatDisplay.HEIGHT);
            assertRoundDisplayGap();

            AddStep("load 21 maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 21; i++)
                    addBeatmap($"MOD{i / 4}");
            });

            AddStep("reset state", resetState);
            setProductionWidth();
            AddAssert("large pool moves above Y 160", () => getMapFlows().Y < 160);
            AddAssert("large pool ends at top of chat", () => getMapFlows().Y + getMapFlows().DrawHeight,
                () => Is.EqualTo(screen.DrawHeight - TournamentMatchChatDisplay.HEIGHT).Within(0.01f));
            assertRoundDisplayGap();
        }

        [Test]
        public void TestSingleRoundDisplay()
        {
            AddAssert("one round display", () => screen.ChildrenOfType<MatchRoundDisplay>().Count() == 1);
        }

        private FillFlowContainer<FillFlowContainer<TournamentBeatmapPanel>> getMapFlows() =>
            screen.ChildrenOfType<FillFlowContainer<FillFlowContainer<TournamentBeatmapPanel>>>().Single();

        private void setProductionWidth() => AddStep("set production stream width", () =>
        {
            screen.RelativeSizeAxes = Axes.Y;
            screen.Width = TournamentSceneManager.STREAM_AREA_WIDTH;
        });

        private void assertRoundDisplayGap() => AddAssert("round display is 10px above first card", () =>
        {
            float roundBottom = screen.ToLocalSpace(screen.ChildrenOfType<MatchRoundDisplay>().Single().ScreenSpaceDrawQuad.BottomLeft).Y;
            float firstCardTop = screen.ToLocalSpace(screen.ChildrenOfType<TournamentBeatmapPanel>().First().ScreenSpaceDrawQuad.TopLeft).Y;
            return firstCardTop - roundBottom;
        }, () => Is.EqualTo(10).Within(0.01f));

        [Test]
        public void TestJustEnoughMaps()
        {
            AddStep("load just enough maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 18; i++)
                    addBeatmap();
            });

            AddStep("reset state", resetState);

            assertTwoWide();
        }

        [Test]
        public void TestManyMaps()
        {
            AddStep("load many maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 19; i++)
                    addBeatmap();
            });

            AddStep("reset state", resetState);

            assertThreeWide();
        }

        [Test]
        public void TestJustEnoughMods()
        {
            AddStep("load many maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 11; i++)
                    addBeatmap(i > 4 ? Ruleset.Value.CreateInstance().AllMods.ElementAt(i).Acronym : "NM");
            });

            AddStep("reset state", resetState);

            assertTwoWide();
        }

        private void assertTwoWide() =>
            AddAssert("ensure layout width is 2", () => screen.ChildrenOfType<FillFlowContainer<FillFlowContainer<TournamentBeatmapPanel>>>().First().Padding.Left > 0);

        private void assertThreeWide() =>
            AddAssert("ensure layout width is 3", () => screen.ChildrenOfType<FillFlowContainer<FillFlowContainer<TournamentBeatmapPanel>>>().First().Padding.Left == 0);

        [Test]
        public void TestManyMods()
        {
            AddStep("load many maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 12; i++)
                    addBeatmap(i > 4 ? Ruleset.Value.CreateInstance().AllMods.ElementAt(i).Acronym : "NM");
            });

            AddStep("reset state", resetState);

            assertThreeWide();
        }

        [Test]
        public void TestSplitMapPoolByMods()
        {
            AddStep("load many maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 12; i++)
                    addBeatmap(i > 4 ? Ruleset.Value.CreateInstance().AllMods.ElementAt(i).Acronym : "NM");
            });

            AddStep("disable splitting map pool by mods", () => Ladder.SplitMapPoolByMods.Value = false);

            AddStep("reset state", resetState);
        }

        [Test]
        public void TestTimerSelectedManualProgression()
        {
            setProgressionSettings(useIpcProgression: false, autoProgressScreens: false, oneVsOneMode: true);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            startPlaying(() => latestPickedBeatmapId);
            waitPastTestTransitionDelay();
            assertGameplayTransitionCount(0);
        }

        [Test]
        public void TestTimerSelectedUsesExistingProgression()
        {
            setProgressionSettings(useIpcProgression: false, autoProgressScreens: true, oneVsOneMode: true);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            AddUntilStep("timer progresses to gameplay", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
        }

        [Test]
        public void TestIpcSelectedManualProgression()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: false, oneVsOneMode: false);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            startPlaying(() => latestPickedBeatmapId);
            waitPastTestTransitionDelay();
            assertGameplayTransitionCount(0);
        }

        [Test]
        public void TestIpcSelectedUsesPickedMapPlayingProgression()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: true, oneVsOneMode: false);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            AddAssert("production IPC delay is two seconds", () => ((TestMapPoolScreen)screen).ProductionIpcGameplayTransitionDelay, () => Is.EqualTo(2000));

            waitPastTestTransitionDelay();
            assertGameplayTransitionCount(0);

            startPlaying(() => latestPickedBeatmapId);
            assertGameplayTransitionCount(0);
            AddUntilStep("IPC progresses to gameplay", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));

            AddStep("send another Playing edge", () =>
            {
                IPCInfo.State.Value = TourneyState.Ranking;
                IPCInfo.State.Value = TourneyState.Playing;
            });
            assertGameplayTransitionCount(1);
        }

        [Test]
        public void TestIpcProgressionDelayCancelledWhenPlayingEnds()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: true);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            startPlaying(() => latestPickedBeatmapId);
            assertGameplayTransitionCount(0);
            AddStep("leave Playing before delay", () => IPCInfo.State.Value = TourneyState.Ranking);
            waitPastTestTransitionDelay();
            assertGameplayTransitionCount(0);

            startPlaying(() => latestPickedBeatmapId);
            AddUntilStep("fresh Playing edge progresses", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
        }

        [Test]
        public void TestIpcProgressionDelayCancelledWhenLatestPickChanges()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: true);
            prepareMapsForAutoProgression(2);
            pickMap(0);

            startPlaying(() => latestPickedBeatmapId);
            assertGameplayTransitionCount(0);
            pickMap(1);
            waitPastTestTransitionDelay();
            assertGameplayTransitionCount(0);

            startPlaying(() => latestPickedBeatmapId);
            AddUntilStep("new latest pick progresses", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
        }

        [Test]
        public void TestIpcProgressionDelayCancelledWhenMapPoolHidden()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: true);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            startPlaying(() => latestPickedBeatmapId);
            assertGameplayTransitionCount(0);
            AddStep("hide map pool before delay", () => screen.Hide());
            waitPastTestTransitionDelay();
            assertGameplayTransitionCount(0);

            AddStep("re-enter map pool", () => screen.Show());
            startPlaying(() => latestPickedBeatmapId);
            AddUntilStep("fresh Playing edge progresses", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
        }

        [Test]
        public void TestIpcProgressionRejectsWrongMapAndSamePlayingSessionMapChange()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: true);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            startPlaying(() => latestPickedBeatmapId + 1);
            assertGameplayTransitionCount(0);

            AddStep("change map during same Playing state", () => IPCInfo.BeatmapID.Value = latestPickedBeatmapId);
            assertGameplayTransitionCount(0);

            startPlaying(() => latestPickedBeatmapId);
            AddUntilStep("new correct Playing edge progresses", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
        }

        [Test]
        public void TestIpcProgressionRejectsStalePlayingOnPickAndReentry()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: true);
            prepareMapsForAutoProgression(1);

            AddStep("start map before pick", () =>
            {
                IPCInfo.BeatmapID.Value = firstPickedBeatmapId;
                IPCInfo.State.Value = TourneyState.Playing;
            });

            pickMap(0);
            assertGameplayTransitionCount(0);

            AddStep("hide and re-enter map pool", () =>
            {
                screen.Hide();
                screen.Show();
            });
            assertGameplayTransitionCount(0);

            startPlaying(() => latestPickedBeatmapId);
            AddUntilStep("fresh Playing edge progresses", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
        }

        [Test]
        public void TestIpcProgressionTracksLatestPick()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: true);
            prepareMapsForAutoProgression(2);
            pickMap(0);
            pickMap(1);

            startPlaying(() => firstPickedBeatmapId);
            assertGameplayTransitionCount(0);

            startPlaying(() => latestPickedBeatmapId);
            AddUntilStep("latest pick progresses", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
        }

        [Test]
        public void TestEnablingIpcProgressionCancelsTimer()
        {
            setProgressionSettings(useIpcProgression: false, autoProgressScreens: true);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            AddStep("enable IPC before timer", () => Ladder.UseIPCForMapPoolProgression.Value = true);
            waitPastTestTransitionDelay();
            assertGameplayTransitionCount(0);

            startPlaying(() => latestPickedBeatmapId);
            AddUntilStep("IPC progresses after mode change", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
        }

        [Test]
        public void TestDisablingIpcProgressionStartsTimer()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: true);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            AddStep("disable IPC progression", () => Ladder.UseIPCForMapPoolProgression.Value = false);
            AddUntilStep("timer progresses after mode change", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
        }

        [Test]
        public void TestDisablingAutoProgressCancelsIpcProgression()
        {
            setProgressionSettings(useIpcProgression: true, autoProgressScreens: true);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            startPlaying(() => latestPickedBeatmapId);
            assertGameplayTransitionCount(0);
            AddStep("disable auto progress during delay", () => Ladder.AutoProgressScreens.Value = false);
            waitPastTestTransitionDelay();
            assertGameplayTransitionCount(0);
        }

        [Test]
        public void TestCompletedPickDoesNotRetriggerAfterSelectorChange()
        {
            setProgressionSettings(useIpcProgression: false, autoProgressScreens: true);
            prepareMapsForAutoProgression(1);
            pickMap(0);

            AddUntilStep("timer progresses to gameplay", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(1));
            AddStep("enable IPC after progression", () => Ladder.UseIPCForMapPoolProgression.Value = true);
            startPlaying(() => latestPickedBeatmapId);
            assertGameplayTransitionCount(1);
        }

        [Test]
        public void TestBanOrderMultipleBans()
        {
            AddStep("set ban count", () => Ladder.CurrentMatch.Value!.Round.Value!.BanCount.Value = 2);

            AddStep("load some maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 5; i++)
                    addBeatmap();
            });

            AddStep("update displayed maps", () => Ladder.SplitMapPoolByMods.Value = false);

            AddStep("start bans from blue team", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Blue Ban").TriggerClick());

            AddStep("ban map", () => clickBeatmapPanel(0));
            checkTotalPickBans(1);
            checkLastPick(ChoiceType.Ban, TeamColour.Blue);

            AddStep("ban map", () => clickBeatmapPanel(1));
            checkTotalPickBans(2);
            checkLastPick(ChoiceType.Ban, TeamColour.Red);

            AddStep("ban map", () => clickBeatmapPanel(2));
            checkTotalPickBans(3);
            checkLastPick(ChoiceType.Ban, TeamColour.Red);

            AddStep("pick map", () => clickBeatmapPanel(3));
            checkTotalPickBans(4);
            checkLastPick(ChoiceType.Ban, TeamColour.Blue);

            AddStep("pick map", () => clickBeatmapPanel(4));
            checkTotalPickBans(5);
            checkLastPick(ChoiceType.Pick, TeamColour.Blue);
        }

        [Test]
        public void TestPickBanOrder()
        {
            AddStep("set ban count", () => Ladder.CurrentMatch.Value!.Round.Value!.BanCount.Value = 1);

            AddStep("load some maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 5; i++)
                    addBeatmap();
            });

            AddStep("update displayed maps", () => Ladder.SplitMapPoolByMods.Value = false);

            AddStep("start bans from blue team", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Blue Ban").TriggerClick());

            AddStep("ban map", () => clickBeatmapPanel(0));
            checkTotalPickBans(1);
            checkLastPick(ChoiceType.Ban, TeamColour.Blue);

            AddStep("ban map", () => clickBeatmapPanel(1));
            checkTotalPickBans(2);
            checkLastPick(ChoiceType.Ban, TeamColour.Red);

            AddStep("pick map", () => clickBeatmapPanel(2));
            checkTotalPickBans(3);
            checkLastPick(ChoiceType.Pick, TeamColour.Red);

            AddStep("pick map", () => clickBeatmapPanel(3));
            checkTotalPickBans(4);
            checkLastPick(ChoiceType.Pick, TeamColour.Blue);

            AddStep("pick map", () => clickBeatmapPanel(4));
            checkTotalPickBans(5);
            checkLastPick(ChoiceType.Pick, TeamColour.Red);

            AddStep("reset match", () =>
            {
                Ladder.CurrentMatch.Value = new TournamentMatch();
                Ladder.CurrentMatch.Value = Ladder.Matches.First();
                Ladder.CurrentMatch.Value.PicksBans.Clear();
            });
        }

        [Test]
        public void TestMultipleTeamBans()
        {
            AddStep("set ban count", () => Ladder.CurrentMatch.Value!.Round.Value!.BanCount.Value = 3);

            AddStep("load some maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 12; i++)
                    addBeatmap();
            });

            AddStep("update displayed maps", () => Ladder.SplitMapPoolByMods.Value = false);

            AddStep("start bans with red team", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Red Ban").TriggerClick());

            AddStep("first ban", () => clickBeatmapPanel(0));
            AddAssert("red ban registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Red),
                () => Is.EqualTo(1));

            AddStep("ban two more maps", () =>
            {
                clickBeatmapPanel(1);
                clickBeatmapPanel(2);
            });

            AddAssert("three bans registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban),
                () => Is.EqualTo(3));
            AddAssert("both new bans for blue team",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Blue),
                () => Is.EqualTo(2));

            AddStep("ban two more maps", () =>
            {
                clickBeatmapPanel(3);
                clickBeatmapPanel(4);
            });

            AddAssert("five bans registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban),
                () => Is.EqualTo(5));
            AddAssert("both new bans for red team",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Red),
                () => Is.EqualTo(3));

            AddStep("ban last map", () => clickBeatmapPanel(5));
            AddAssert("six bans registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban),
                () => Is.EqualTo(6));
            AddAssert("red banned three",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Red),
                () => Is.EqualTo(3));
            AddAssert("blue banned three",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Blue),
                () => Is.EqualTo(3));

            AddStep("pick map", () => clickBeatmapPanel(6));
            AddAssert("one pick registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Pick),
                () => Is.EqualTo(1));
            AddAssert("pick was blue's",
                () => Ladder.CurrentMatch.Value!.PicksBans.Last().Team,
                () => Is.EqualTo(TeamColour.Blue));

            AddStep("pick map", () => clickBeatmapPanel(7));
            AddAssert("two picks registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Pick),
                () => Is.EqualTo(2));
            AddAssert("pick was red's",
                () => Ladder.CurrentMatch.Value!.PicksBans.Last().Team,
                () => Is.EqualTo(TeamColour.Red));

            AddStep("pick map", () => clickBeatmapPanel(8));
            AddAssert("three picks registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Pick),
                () => Is.EqualTo(3));
            AddAssert("pick was blue's",
                () => Ladder.CurrentMatch.Value!.PicksBans.Last().Team,
                () => Is.EqualTo(TeamColour.Blue));

            AddStep("reset match", () =>
            {
                Ladder.CurrentMatch.Value = new TournamentMatch();
                Ladder.CurrentMatch.Value = Ladder.Matches.First();
                Ladder.CurrentMatch.Value.PicksBans.Clear();
            });
        }

        private void checkTotalPickBans(int expected) => AddAssert($"total pickbans is {expected}", () => Ladder.CurrentMatch.Value!.PicksBans, () => Has.Count.EqualTo(expected));

        private void checkLastPick(ChoiceType expectedChoice, TeamColour expectedColour) =>
            AddAssert($"last choice was {expectedChoice} by {expectedColour}",
                () => Ladder.CurrentMatch.Value!.PicksBans.Select(pb => (pb.Type, pb.Team)).Last(),
                () => Is.EqualTo((expectedChoice, expectedColour)));

        private void addBeatmap(string mods = "NM")
        {
            Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Add(new RoundBeatmap
            {
                Beatmap = CreateSampleBeatmap(),
                Mods = mods
            });
        }

        private void setProgressionSettings(bool useIpcProgression, bool autoProgressScreens, bool oneVsOneMode = false) => AddStep(
            $"set IPC {useIpcProgression}, auto progress {autoProgressScreens}, and 1v1 {oneVsOneMode}",
            () =>
            {
                Ladder.OneVsOneMode.Value = oneVsOneMode;
                Ladder.UseIPCForMapPoolProgression.Value = useIpcProgression;
                Ladder.AutoProgressScreens.Value = autoProgressScreens;
            });

        private void prepareMapsForAutoProgression(int count)
        {
            AddStep($"prepare {count} maps", () =>
            {
                var round = Ladder.CurrentMatch.Value!.Round.Value!;
                round.BanCount.Value = 0;
                round.Beatmaps.Clear();

                for (int i = 0; i < count; i++)
                    addBeatmap();

                firstPickedBeatmapId = round.Beatmaps[0].Beatmap!.OnlineID;
                Ladder.SplitMapPoolByMods.Value = false;
            });
            AddUntilStep("prepared maps are displayed", () =>
            {
                var expectedIds = Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Select(beatmap => beatmap.Beatmap!.OnlineID);
                var displayedIds = screen.ChildrenOfType<TournamentBeatmapPanel>().Where(panel => panel.IsAlive).Select(panel => panel.Beatmap!.OnlineID);
                return expectedIds.All(displayedIds.Contains);
            });
        }

        private void pickMap(int index)
        {
            AddStep("select red pick", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Red Pick").TriggerClick());
            AddStep($"pick map {index}", () =>
            {
                int beatmapId = Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps[index].Beatmap!.OnlineID;
                ((TestMapPoolScreen)screen).AddSelectedBeatmap(beatmapId);
                latestPickedBeatmapId = Ladder.CurrentMatch.Value!.PicksBans.Last(choice => choice.Type == ChoiceType.Pick).BeatmapID;
            });
        }

        private void startPlaying(System.Func<int> getBeatmapId) => AddStep("start playing beatmap", () =>
        {
            int beatmapId = getBeatmapId();
            IPCInfo.State.Value = TourneyState.WaitingForClients;
            IPCInfo.BeatmapID.Value = beatmapId;
            IPCInfo.State.Value = TourneyState.Playing;
        });

        private void waitPastTestTransitionDelay() => AddWaitStep("wait past timer delay", 90);

        private void assertGameplayTransitionCount(int expected) =>
            AddAssert($"gameplay transition count is {expected}", () => ((TestMapPoolScreen)screen).GameplayTransitionCount, () => Is.EqualTo(expected));

        private void clickBeatmapPanel(int index)
        {
            InputManager.MoveMouseTo(screen.ChildrenOfType<TournamentBeatmapPanel>().ElementAt(index));
            InputManager.Click(MouseButton.Left);
        }

        private partial class TestMapPoolScreen : MapPoolScreen
        {
            public int GameplayTransitionCount { get; private set; }
            public double ProductionIpcGameplayTransitionDelay => base.IpcGameplayTransitionDelay;

            protected override double GameplayTransitionDelay => 1000;
            protected override double IpcGameplayTransitionDelay => 1000;

            protected override void ProgressToGameplay() => GameplayTransitionCount++;

            public void ResetGameplayTransitionCount() => GameplayTransitionCount = 0;

            public void AddSelectedBeatmap(int beatmapId) => AddForBeatmap(beatmapId);

            // this is a bit of a test-specific workaround.
            // the way pick/ban is implemented is a bit funky; the screen itself is what handles the mouse there,
            // rather than the beatmap panels themselves.
            // in some extreme situations headless it may turn out that the panels overflow the screen,
            // and as such picking stops working anymore outside of the bounds of the screen drawable.
            // this override makes it so the screen sees all of the input at all times, making that impossible to happen.
            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;
        }
    }
}
