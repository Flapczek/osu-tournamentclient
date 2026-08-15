// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Components
{
    public partial class DrawableTeamHeader : TournamentSpriteTextWithBackground
    {
        private readonly TeamColour colour;
        private Bindable<bool>? oneVsOneMode;

        public DrawableTeamHeader(TeamColour colour)
        {
            this.colour = colour;

            Background.Colour = TournamentGame.GetTeamColour(colour);

            Text.Colour = TournamentGame.TEXT_COLOUR;
            Text.Scale = new Vector2(0.6f);

            updateText();
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladderInfo)
        {
            oneVsOneMode = ladderInfo.OneVsOneMode.GetBoundCopy();
            oneVsOneMode.BindValueChanged(_ => updateText(), true);
        }

        private void updateText()
        {
            Text.Text = oneVsOneMode?.Value == true
                ? colour == TeamColour.Red ? "PLAYER 1" : "PLAYER 2"
                : $"Team {colour}".ToUpperInvariant();
        }
    }
}
