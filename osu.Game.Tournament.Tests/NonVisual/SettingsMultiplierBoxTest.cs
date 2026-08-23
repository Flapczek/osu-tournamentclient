// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Tournament.Screens.Editors.Components;

namespace osu.Game.Tournament.Tests.NonVisual
{
    [TestFixture]
    public class SettingsMultiplierBoxTest
    {
        [TestCase("1.25", 1.25)]
        [TestCase("1,25", 1.25)]
        [TestCase("0.1", 0.1)]
        [TestCase("10", 10)]
        public void TestValidInput(string text, double expected)
        {
            Assert.That(SettingsMultiplierBox.TryParse(text, out double? value), Is.True);
            Assert.That(value, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase(" ")]
        public void TestEmptyInputClearsMultiplier(string text)
        {
            Assert.That(SettingsMultiplierBox.TryParse(text, out double? value), Is.True);
            Assert.That(value, Is.Null);
        }

        [TestCase("0")]
        [TestCase("10.1")]
        [TestCase("1.2.3")]
        [TestCase("abc")]
        public void TestInvalidInput(string text)
        {
            Assert.That(SettingsMultiplierBox.TryParse(text, out _), Is.False);
        }
    }
}
