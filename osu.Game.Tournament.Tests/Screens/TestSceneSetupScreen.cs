// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Tournament.Screens.Setup;

namespace osu.Game.Tournament.Tests.Screens
{
    public partial class TestSceneSetupScreen : TournamentScreenTestScene
    {
        private SetupScreen screen = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(screen = new SetupScreen());
        }

        [Test]
        public void TestExportBracketButtonEnabledOnlyForLoadedTournament()
        {
            string? loadedTournament = null;

            AddStep("store loaded tournament", () => loadedTournament = screen.ChildrenOfType<OsuDropdown<string>>().Single().Current.Value);
            AddAssert("export button present and enabled", () => getExportButton().Enabled.Value);
            AddStep("select another tournament", () =>
            {
                var dropdown = screen.ChildrenOfType<OsuDropdown<string>>().Single();
                dropdown.Items = new[] { loadedTournament!, "another-tournament" };
                dropdown.Current.Value = "another-tournament";
            });
            AddAssert("export button disabled", () => !getExportButton().Enabled.Value);
            AddStep("restore loaded tournament", () => screen.ChildrenOfType<OsuDropdown<string>>().Single().Current.Value = loadedTournament!);
            AddAssert("export button enabled", () => getExportButton().Enabled.Value);
        }

        private OsuButton getExportButton() => screen.ChildrenOfType<OsuButton>().Single(button => button.Text.ToString() == "Export bracket");
    }
}
