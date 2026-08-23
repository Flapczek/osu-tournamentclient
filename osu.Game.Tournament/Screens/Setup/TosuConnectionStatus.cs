// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Tournament.IPC;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Setup
{
    internal partial class TosuConnectionStatus : LabelledDrawable<Drawable>
    {
        private TournamentSpriteText statusText = null!;
        private OsuColour colours = null!;

        public TosuConnectionStatus()
            : base(true)
        {
            Label = "tosu status";
            Description = "Connection used to read live tournament scores and apply per-map Easy correction factors.";
        }

        protected override Drawable CreateComponent() => statusText = new TournamentSpriteText
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
        };

        [BackgroundDependencyLoader]
        private void load(OsuColour osuColour, MatchIPCInfo ipc)
        {
            colours = osuColour;
            ipc.TosuConnectionState.BindValueChanged(state => updateState(state.NewValue), true);
        }

        private void updateState(TosuConnectionState state)
        {
            (string text, Color4 colour) = state switch
            {
                TosuConnectionState.Disabled => ("tosu integration disabled", Color4.White),
                TosuConnectionState.Searching => ("Searching for tosu...", colours.Yellow),
                TosuConnectionState.ConnectedWaitingForClients => ("Connected to tosu — waiting for tournament clients", colours.Yellow),
                TosuConnectionState.Connected => ("Connected to tosu", colours.Green),
                TosuConnectionState.Failed => ("Could not connect to tosu — using standard tournament scores", colours.Red),
                _ => ("tosu integration disabled", Color4.White),
            };

            statusText.Text = text;
            statusText.Colour = colour;
        }
    }
}
