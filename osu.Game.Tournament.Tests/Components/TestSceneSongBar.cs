// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Online.API;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Online;

namespace osu.Game.Tournament.Tests.Components
{
    [TestFixture]
    public partial class TestSceneSongBar : TournamentTestScene
    {
        private SongBar songBar = null!;
        private TournamentBeatmap ladderBeatmap = null!;
        private readonly List<GetBeatmapDifficultyAttributesRequest> difficultyAttributeRequests = new List<GetBeatmapDifficultyAttributesRequest>();
        private bool automaticallyCompleteDifficultyAttributeRequests;

        [Cached]
        private readonly BeatmapDifficultyAttributesProvider difficultyAttributesProvider = new BeatmapDifficultyAttributesProvider();

        public TestSceneSongBar()
        {
            Add(difficultyAttributesProvider);
        }

        [SetUpSteps]
        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("set up API", () =>
            {
                difficultyAttributeRequests.Clear();
                automaticallyCompleteDifficultyAttributeRequests = true;
                ((DummyAPIAccess)API).HandleRequest = request =>
                {
                    if (request is not GetBeatmapDifficultyAttributesRequest difficultyAttributesRequest)
                        return false;

                    difficultyAttributeRequests.Add(difficultyAttributesRequest);

                    if (automaticallyCompleteDifficultyAttributeRequests)
                    {
                        difficultyAttributesRequest.TriggerSuccess(new BeatmapDifficultyAttributesResponse
                        {
                            Attributes = new BeatmapDifficultyAttributes { StarRating = 5 },
                        });
                    }

                    return true;
                };
            });

            AddStep("setup picks bans", () =>
            {
                ladderBeatmap = CreateSampleBeatmap();
                Ladder.CurrentMatch.Value!.PicksBans.Add(new BeatmapChoice
                {
                    BeatmapID = ladderBeatmap.OnlineID,
                    Team = TeamColour.Red,
                    Type = ChoiceType.Pick,
                });
            });

            AddStep("create bar", () => Child = songBar = new SongBar
            {
                RelativeSizeAxes = Axes.X,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });
            AddUntilStep("wait for loaded", () => songBar.IsLoaded);
        }

        [Test]
        public void TestSongBar()
        {
            AddStep("set beatmap", () =>
            {
                var beatmap = CreateAPIBeatmap(Ruleset.Value);

                beatmap.CircleSize = 3.4f;
                beatmap.ApproachRate = 6.8f;
                beatmap.OverallDifficulty = 5.5f;
                beatmap.StarRating = 4.56f;
                beatmap.DrainRate = 1.23f;
                beatmap.Length = 123456;
                beatmap.BPM = 133;
                beatmap.OnlineID = ladderBeatmap.OnlineID;

                songBar.Beatmap = new TournamentBeatmap(beatmap);
            });

            AddStep("set mods to HR", () => songBar.Mods = LegacyMods.HardRock);
            AddStep("set mods to DT", () => songBar.Mods = LegacyMods.DoubleTime);
            AddStep("set mods to HDHRDT", () => songBar.Mods = LegacyMods.Hidden | LegacyMods.HardRock | LegacyMods.DoubleTime);

            AddStep("unset mods", () => songBar.Mods = LegacyMods.None);

            AddToggleStep("toggle expanded", expanded => songBar.Expanded = expanded);

            AddStep("set null beatmap", () => songBar.Beatmap = null);

            AddStep("set ruleset to osu", () => Ruleset.Value = new OsuRuleset().RulesetInfo);
            AddStep("set ruleset to taiko", () => Ruleset.Value = new TaikoRuleset().RulesetInfo);
            AddStep("set ruleset to catch", () => Ruleset.Value = new CatchRuleset().RulesetInfo);
            AddStep("set ruleset to mania", () => Ruleset.Value = new ManiaRuleset().RulesetInfo);
        }

        [Test]
        public void TestModdedStarRating()
        {
            TournamentBeatmap beatmap = null!;

            AddStep("set beatmap", () => songBar.Beatmap = beatmap = createBeatmap(ladderBeatmap.OnlineID, 4.5));
            AddAssert("shows base star rating", () => getDisplayedStarRating() == "4.50");
            AddAssert("no request without mods", () => difficultyAttributeRequests.Count == 0);

            AddStep("hold request", () => automaticallyCompleteDifficultyAttributeRequests = false);
            AddStep("set HR", () => songBar.Mods = LegacyMods.HardRock);
            AddUntilStep("request received", () => difficultyAttributeRequests.Count == 1);
            AddAssert("request uses current values", () => difficultyAttributeRequests[0].Key == new BeatmapDifficultyAttributesKey(
                beatmap.OnlineID, LegacyMods.HardRock, Ruleset.Value.OnlineID));
            AddAssert("shows fallback while loading", () => getDisplayedStarRating() == "4.50*");

            AddStep("complete request", () => difficultyAttributeRequests[0].TriggerSuccess(new BeatmapDifficultyAttributesResponse
            {
                Attributes = new BeatmapDifficultyAttributes { StarRating = 6.789 },
            }));
            AddUntilStep("shows modded star rating", () => getDisplayedStarRating() == "6.78");

            AddStep("unset mods", () => songBar.Mods = LegacyMods.None);
            AddUntilStep("returns to base star rating", () => getDisplayedStarRating() == "4.50");
            AddStep("set HR again", () => songBar.Mods = LegacyMods.HardRock);
            AddUntilStep("uses cached star rating", () => getDisplayedStarRating() == "6.78");
            AddAssert("does not request cached value", () => difficultyAttributeRequests.Count == 1);
        }

        [Test]
        public void TestCoalescesRequests()
        {
            SongBar secondSongBar = null!;
            TournamentBeatmap beatmap = null!;

            AddStep("set first bar", () =>
            {
                automaticallyCompleteDifficultyAttributeRequests = false;
                songBar.Beatmap = beatmap = createBeatmap(ladderBeatmap.OnlineID, 4.5);
                songBar.Mods = LegacyMods.DoubleTime;
            });
            AddStep("add second bar", () => Add(secondSongBar = new SongBar
            {
                Beatmap = beatmap,
                Mods = LegacyMods.DoubleTime,
            }));
            AddUntilStep("one shared request received", () => difficultyAttributeRequests.Count == 1);

            AddStep("complete shared request", () => difficultyAttributeRequests[0].TriggerSuccess(new BeatmapDifficultyAttributesResponse
            {
                Attributes = new BeatmapDifficultyAttributes { StarRating = 7.12 },
            }));
            AddUntilStep("first bar updated", () => getDisplayedStarRating(songBar) == "7.12");
            AddUntilStep("second bar updated", () => getDisplayedStarRating(secondSongBar) == "7.12");
        }

        [Test]
        public void TestIgnoresStaleResponse()
        {
            int secondBeatmapId = ladderBeatmap.OnlineID + 1;

            AddStep("set first beatmap", () =>
            {
                automaticallyCompleteDifficultyAttributeRequests = false;
                songBar.Beatmap = createBeatmap(ladderBeatmap.OnlineID, 4.5);
                songBar.Mods = LegacyMods.HardRock;
            });
            AddUntilStep("first request received", () => difficultyAttributeRequests.Count == 1);

            AddStep("set second beatmap and mods", () =>
            {
                songBar.Beatmap = createBeatmap(secondBeatmapId, 3.25);
                songBar.Mods = LegacyMods.Hidden | LegacyMods.DoubleTime;
            });
            AddUntilStep("second request received", () => difficultyAttributeRequests.Count == 2);

            AddStep("complete stale request", () => difficultyAttributeRequests[0].TriggerSuccess(new BeatmapDifficultyAttributesResponse
            {
                Attributes = new BeatmapDifficultyAttributes { StarRating = 9.99 },
            }));
            AddUntilStep("keeps current fallback", () => getDisplayedStarRating() == "3.25*");

            AddStep("complete current request", () => difficultyAttributeRequests[1].TriggerSuccess(new BeatmapDifficultyAttributesResponse
            {
                Attributes = new BeatmapDifficultyAttributes { StarRating = 5.432 },
            }));
            AddUntilStep("shows current result", () => getDisplayedStarRating() == "5.43");
        }

        [Test]
        public void TestFailedRequestKeepsFallback()
        {
            AddStep("set beatmap and mods", () =>
            {
                automaticallyCompleteDifficultyAttributeRequests = false;
                songBar.Beatmap = createBeatmap(ladderBeatmap.OnlineID, 4.5);
                songBar.Mods = LegacyMods.Easy;
            });
            AddUntilStep("request received", () => difficultyAttributeRequests.Count == 1);
            AddStep("fail request", () => difficultyAttributeRequests[0].Fail(new InvalidOperationException("Test failure")));
            AddUntilStep("keeps fallback", () => getDisplayedStarRating() == "4.50*");
        }

        [Test]
        public void TestRulesetChangeRequestsNewAttributes()
        {
            AddStep("set beatmap and mods", () =>
            {
                automaticallyCompleteDifficultyAttributeRequests = false;
                songBar.Beatmap = createBeatmap(ladderBeatmap.OnlineID, 4.5);
                songBar.Mods = LegacyMods.HardRock;
            });
            AddUntilStep("first request received", () => difficultyAttributeRequests.Count == 1);

            AddStep("change ruleset", () => Ruleset.Value = new TaikoRuleset().RulesetInfo);
            AddUntilStep("second request received", () => difficultyAttributeRequests.Count == 2);
            AddAssert("request uses new ruleset", () => difficultyAttributeRequests[1].Key.RulesetID == 1);
            AddStep("complete requests", () =>
            {
                difficultyAttributeRequests[0].TriggerSuccess(new BeatmapDifficultyAttributesResponse
                {
                    Attributes = new BeatmapDifficultyAttributes { StarRating = 6 },
                });
                difficultyAttributeRequests[1].TriggerSuccess(new BeatmapDifficultyAttributesResponse
                {
                    Attributes = new BeatmapDifficultyAttributes { StarRating = 5.5 },
                });
            });
            AddUntilStep("shows new ruleset result", () => getDisplayedStarRating() == "5.50");
        }

        private TournamentBeatmap createBeatmap(int onlineId, double starRating)
        {
            var beatmap = CreateAPIBeatmap(new OsuRuleset().RulesetInfo);
            beatmap.OnlineID = onlineId;
            beatmap.StarRating = starRating;
            return new TournamentBeatmap(beatmap);
        }

        private string getDisplayedStarRating() => getDisplayedStarRating(songBar);

        private static string getDisplayedStarRating(SongBar bar)
        {
            var starRatingPiece = bar.ChildrenOfType<SongBar.DiffPiece>().Single(piece =>
                piece.ChildrenOfType<TournamentSpriteText>().Any(text => text.Text.ToString() == "Star Rating"));

            return starRatingPiece.ChildrenOfType<TournamentSpriteText>().Last().Text.ToString();
        }
    }
}
