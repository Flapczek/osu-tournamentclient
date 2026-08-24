// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentBeatmapPanel : CompositeDrawable
    {
        public readonly IBeatmapInfo? Beatmap;

        private readonly string mod;
        private readonly Anchor? choiceOwnerIndicatorAnchor;
        private readonly bool showBanOwnerIndicator;

        public const float HEIGHT = 50;

        private const double pick_flash_iteration_duration = 500;
        private const int pick_flash_iterations = 10;

        internal const double PICK_FLASH_TOTAL_DURATION = pick_flash_iteration_duration * pick_flash_iterations;

        private const float pending_pick_glow_max_alpha = 0.2f;
        private const double pending_pick_glow_half_cycle_duration = 2500;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        private readonly BindableBool oneVsOneMode = new BindableBool();

        private Container beatmapContent = null!;
        private Box flash = null!;
        private Box pendingPickGlow = null!;
        private ScheduledDelegate? scheduledPendingPickGlow;
        private DrawableChoiceOwnerIndicator? choiceOwnerIndicator;

        public TournamentBeatmapPanel(IBeatmapInfo? beatmap, string mod = "", Anchor? choiceOwnerIndicatorAnchor = null, bool showBanOwnerIndicator = false)
        {
            Beatmap = beatmap;
            this.mod = mod;
            this.choiceOwnerIndicatorAnchor = choiceOwnerIndicatorAnchor;
            this.showBanOwnerIndicator = showBanOwnerIndicator;

            Width = 400;
            Height = HEIGHT;
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);
            oneVsOneMode.BindTo(ladder.OneVsOneMode);
            oneVsOneMode.BindValueChanged(_ => Scheduler.AddOnce(updateState));

            Masking = true;

            AddRangeInternal(new Drawable[]
            {
                beatmapContent = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Name = "Beatmap content",
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black,
                            Depth = 2,
                        },
                        new NoUnloadBeatmapSetCover
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = OsuColour.Gray(0.5f),
                            OnlineInfo = (Beatmap as IBeatmapSetOnlineInfo),
                            Depth = 1,
                        },
                        pendingPickGlow = new Box
                        {
                            Name = "Pending pick glow",
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(244, 223, 201, 255),
                            Depth = 0.5f,
                            Alpha = 0,
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Padding = new MarginPadding(15),
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText
                                {
                                    Text = Beatmap?.GetDisplayTitleRomanisable(false, false) ?? (LocalisableString)@"unknown",
                                    Font = OsuFont.Torus.With(weight: FontWeight.Bold),
                                },
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Children = new Drawable[]
                                    {
                                        new TournamentSpriteText
                                        {
                                            Text = "mapper",
                                            Padding = new MarginPadding { Right = 5 },
                                            Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 14)
                                        },
                                        new TournamentSpriteText
                                        {
                                            Text = Beatmap?.Metadata.Author.Username ?? "unknown",
                                            Padding = new MarginPadding { Right = 20 },
                                            Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 14)
                                        },
                                        new TournamentSpriteText
                                        {
                                            Text = "difficulty",
                                            Padding = new MarginPadding { Right = 5 },
                                            Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 14)
                                        },
                                        new TournamentSpriteText
                                        {
                                            Text = Beatmap?.DifficultyName ?? "unknown",
                                            Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 14)
                                        },
                                    }
                                }
                            },
                        },
                    }
                },
                flash = new Box
                {
                    Name = "Pick flash",
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Gray,
                    Blending = BlendingParameters.Additive,
                    Depth = -2,
                    Alpha = 0,
                },
            });

            if (!string.IsNullOrEmpty(mod))
            {
                beatmapContent.Add(new TournamentModIcon(mod)
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding(10),
                    Width = 60,
                    RelativeSizeAxes = Axes.Y,
                });
            }

            if (choiceOwnerIndicatorAnchor != null)
                AddInternal(choiceOwnerIndicator = new DrawableChoiceOwnerIndicator(choiceOwnerIndicatorAnchor.Value));
        }

        internal void StartPendingPickGlow(double delay = PICK_FLASH_TOTAL_DURATION)
        {
            StopPendingPickGlow();

            scheduledPendingPickGlow = Scheduler.AddDelayed(() =>
            {
                scheduledPendingPickGlow = null;
                var easing = new CubicBezierEasingFunction(0.42, 0, 0.58, 1);

                pendingPickGlow
                    .FadeTo(pending_pick_glow_max_alpha, pending_pick_glow_half_cycle_duration, easing)
                    .Then().FadeTo(0, pending_pick_glow_half_cycle_duration, easing)
                    .Loop();
            }, delay);
        }

        internal void StopPendingPickGlow()
        {
            scheduledPendingPickGlow?.Cancel();
            scheduledPendingPickGlow = null;

            pendingPickGlow.ClearTransforms();
            pendingPickGlow.Alpha = 0;
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            if (match.OldValue != null)
                match.OldValue.PicksBans.CollectionChanged -= picksBansOnCollectionChanged;
            if (match.NewValue != null)
                match.NewValue.PicksBans.CollectionChanged += picksBansOnCollectionChanged;

            Scheduler.AddOnce(updateState);
        }

        private void picksBansOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => Scheduler.AddOnce(updateState);

        private BeatmapChoice? choice;

        private void updateState()
        {
            if (currentMatch.Value == null)
            {
                choiceOwnerIndicator?.HideIndicator();
                choice = null;
                return;
            }

            var newChoice = currentMatch.Value.PicksBans.FirstOrDefault(p => p.BeatmapID == Beatmap?.OnlineID);

            bool shouldFlash = newChoice != choice;

            if (newChoice != null)
            {
                if (shouldFlash)
                    flash.FadeOutFromOne(pick_flash_iteration_duration).Loop(0, pick_flash_iterations);

                beatmapContent.BorderThickness = 6;
                beatmapContent.BorderColour = newChoice.Type == ChoiceType.Pick && isTiebreaker()
                    ? TournamentGame.ELEMENT_BACKGROUND_COLOUR
                    : TournamentGame.GetTeamColour(newChoice.Team);

                switch (newChoice.Type)
                {
                    case ChoiceType.Pick:
                        beatmapContent.Colour = Color4.White;
                        beatmapContent.Alpha = 1;
                        break;

                    case ChoiceType.Ban:
                        beatmapContent.Colour = Color4.Gray;
                        beatmapContent.Alpha = 0.5f;
                        break;
                }
            }
            else
            {
                beatmapContent.Colour = Color4.White;
                beatmapContent.Alpha = 1;
                beatmapContent.BorderThickness = 0;
            }

            updateChoiceOwnerIndicator(newChoice);
            choice = newChoice;
        }

        private void updateChoiceOwnerIndicator(BeatmapChoice? newChoice)
        {
            if (choiceOwnerIndicator == null
                || newChoice == null)
            {
                choiceOwnerIndicator?.HideIndicator();
                return;
            }

            if (newChoice.Type == ChoiceType.Pick && isTiebreaker())
            {
                choiceOwnerIndicator.ShowTiebreaker();
                return;
            }

            if (!oneVsOneMode.Value
                || (newChoice.Type == ChoiceType.Ban && !showBanOwnerIndicator))
            {
                choiceOwnerIndicator.HideIndicator();
                return;
            }

            TournamentTeam? team = newChoice.Team == TeamColour.Red
                ? currentMatch.Value?.Team1.Value
                : currentMatch.Value?.Team2.Value;

            if (team?.Players.Count != 1 || string.IsNullOrWhiteSpace(team.Players[0].Username))
            {
                choiceOwnerIndicator.HideIndicator();
                return;
            }

            choiceOwnerIndicator.ShowForChoice(newChoice.Type, newChoice.Team, team.Players[0].Username);
        }

        private bool isTiebreaker()
        {
            if (!string.IsNullOrWhiteSpace(mod))
                return isTiebreakerMod(mod);

            if (Beatmap == null)
                return false;

            var roundBeatmap = currentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(candidate =>
                (candidate.Beatmap?.OnlineID ?? candidate.ID) == Beatmap.OnlineID);

            return isTiebreakerMod(roundBeatmap?.Mods);
        }

        private static bool isTiebreakerMod(string? mods)
            => string.Equals(mods?.Trim(), "TB", StringComparison.OrdinalIgnoreCase);

        private partial class NoUnloadBeatmapSetCover : UpdateableOnlineBeatmapSetCover
        {
            // As covers are displayed on stream, we want them to load as soon as possible.
            protected override double LoadDelay => 0;

            // Use DelayedLoadWrapper to avoid content unloading when switching away to another screen.
            protected override DelayedLoadWrapper CreateDelayedLoadWrapper(Func<Drawable> createContentFunc, double timeBeforeLoad)
                => new DelayedLoadWrapper(createContentFunc(), timeBeforeLoad);
        }
    }
}
