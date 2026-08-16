// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Components
{
    public partial class DrawableTeamFlag : Container
    {
        private static readonly Vector2 flag_size = new Vector2(75, 54);

        private readonly TournamentTeam? team;
        private readonly Vector2 avatarSize;

        [UsedImplicitly]
        private Bindable<string>? flag;

        private Sprite? flagSprite;
        private PlayerAvatar? avatar;

        private readonly BindableBool oneVsOneMode = new BindableBool();
        private readonly BindableList<TournamentUser> players = new BindableList<TournamentUser>();

        private int avatarLoadVersion;

        public DrawableTeamFlag(TournamentTeam? team, float avatarSize = 54)
        {
            this.team = team;
            this.avatarSize = new Vector2(avatarSize);
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures, LadderInfo ladderInfo)
        {
            if (team == null) return;

            Size = flag_size;
            Masking = true;
            CornerRadius = 5;
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.FromHex("333"),
                },
                flagSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    FillMode = FillMode.Fit
                },
            };

            (flag = team.FlagName.GetBoundCopy()).BindValueChanged(_ => flagSprite.Texture = textures.Get($@"Flags/{team.FlagName}"), true);

            players.BindTo(team.Players);
            players.CollectionChanged += (_, _) => Scheduler.AddOnce(updateDisplay);

            oneVsOneMode.BindTo(ladderInfo.OneVsOneMode);
            oneVsOneMode.BindValueChanged(_ => Scheduler.AddOnce(updateDisplay), true);
        }

        private void updateDisplay()
        {
            int loadVersion = ++avatarLoadVersion;

            avatar?.Expire();
            avatar = null;

            Size = flag_size;
            flagSprite?.Show();

            if (!oneVsOneMode.Value || players.Count != 1 || players[0].OnlineID <= 1)
                return;

            int userId = players[0].OnlineID;
            Size = avatarSize;

            LoadComponentAsync(new PlayerAvatar(userId), loadedAvatar =>
            {
                if (loadVersion != avatarLoadVersion
                    || !oneVsOneMode.Value
                    || players.Count != 1
                    || players[0].OnlineID != userId
                    || !loadedAvatar.HasTexture)
                {
                    loadedAvatar.Dispose();
                    return;
                }

                Size = avatarSize;
                flagSprite?.Hide();
                Add(avatar = loadedAvatar);
            });
        }

        [LongRunningLoad]
        private partial class PlayerAvatar : Sprite
        {
            private readonly int userId;

            public bool HasTexture => Texture != null;

            public PlayerAvatar(int userId)
            {
                this.userId = userId;

                RelativeSizeAxes = Axes.Both;
                FillMode = FillMode.Fit;
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
            }

            [BackgroundDependencyLoader]
            private void load(LargeTextureStore textures)
            {
                Texture = textures.Get($@"https://a.ppy.sh/{userId}");
            }
        }
    }
}
