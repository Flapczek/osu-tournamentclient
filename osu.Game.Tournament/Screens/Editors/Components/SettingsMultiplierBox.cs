// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Game.Overlays.Settings;

namespace osu.Game.Tournament.Screens.Editors.Components
{
    public partial class SettingsMultiplierBox : SettingsItem<double?>
    {
        private const double minimum = 0.1;
        private const double maximum = 10;

        protected override Drawable CreateControl() => new MultiplierControl
        {
            RelativeSizeAxes = Axes.X,
        };

        private sealed partial class MultiplierControl : CompositeDrawable, IHasCurrentValue<double?>
        {
            private readonly BindableWithCurrent<double?> current = new BindableWithCurrent<double?>();

            public Bindable<double?> Current
            {
                get => current.Current;
                set => current.Current = value;
            }

            public MultiplierControl()
            {
                AutoSizeAxes = Axes.Y;

                MultiplierTextBox textBox;

                InternalChild = textBox = new MultiplierTextBox
                {
                    RelativeSizeAxes = Axes.X,
                    CommitOnFocusLost = true,
                };

                textBox.Current.BindValueChanged(e =>
                {
                    if (TryParse(e.NewValue, out double? value))
                    {
                        Current.Value = value;
                        return;
                    }

                    textBox.NotifyInputError();
                    Current.TriggerChange();
                });

                Current.BindValueChanged(e =>
                {
                    textBox.Current.Value = e.NewValue?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty;
                });
            }
        }

        internal static bool TryParse(string text, out double? value)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                value = null;
                return true;
            }

            string normalised = text.Replace(',', '.');

            if (double.TryParse(normalised, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double parsed)
                && double.IsFinite(parsed)
                && parsed >= minimum
                && parsed <= maximum)
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }

        private partial class MultiplierTextBox : OutlinedTextBox
        {
            public MultiplierTextBox()
            {
                InputProperties = new TextInputProperties(TextInputType.Decimal, false);
            }

            protected override bool CanAddCharacter(char character) => char.IsAsciiDigit(character) || character is '.' or ',';

            public new void NotifyInputError() => base.NotifyInputError();
        }
    }
}
