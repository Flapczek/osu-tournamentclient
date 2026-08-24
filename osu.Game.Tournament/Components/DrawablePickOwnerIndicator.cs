// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Components
{
    internal partial class DrawablePickOwnerIndicator : TournamentSpriteTextWithBackground
    {
        public DrawablePickOwnerIndicator(Anchor anchor)
        {
            Anchor = anchor;
            Origin = anchor;
            Margin = new MarginPadding(6);
            Depth = -1;
            Alpha = 0;

            Text.Colour = TournamentGame.TEXT_COLOUR;
            Text.Font = OsuFont.Torus.With(weight: FontWeight.SemiBold, size: 14);
            Text.Padding = new MarginPadding { Horizontal = 8, Vertical = 3 };
        }

        public void ShowForPick(TeamColour team, string username)
        {
            Background.Colour = TournamentGame.GetTeamColour(team);
            Text.Text = $"PICKED BY {username}".ToUpperInvariant();
            Alpha = 1;
        }

        public void HideIndicator() => Alpha = 0;
    }
}
