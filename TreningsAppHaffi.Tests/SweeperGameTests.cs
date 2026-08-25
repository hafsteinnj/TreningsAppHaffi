using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TreningsAppHaffi.Pages;

namespace TreningsAppHaffi.Tests
{
    public class SweeperGameTests
    {
        /*
         * Test som sjekker at 'on first click' logikken som har ansvar for at
         * den første ruten du klikker i sweepergame aldri er en mine.
         */
        [Fact]
        public void FirstClick_IsNeverAMine()
        {
            // Arrange
            var model = new SweeperGameModel();

            /*
             * laget fake database i SQLtestModelTests.cs
             * Her trengs det en fake context.
             */
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new SweeperGameTestSession();

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            // Act
            model.OnGet();

            var result = model.OnPostReveal(0, 0);

            // Assert
            model.GetCell(0, 0).IsMine.Should().BeFalse();
        }


        /*
         * Jeg ønsket en oppgave hvor jeg fik bruke / det var hensiktsmessig å bruke [theory]
         * Slik jeg opfatter det. betyr det bar 'kjør denne testen som en [fact]' bare over og over igjenn med forskjellig input/data.
         */
        [Theory]
        [InlineData(0, 0)]
        [InlineData(5, 5)]
        [InlineData(9, 9)]
        public void FlaggedCell_CannotBeRevealed(int row, int column)
        {
            // Arrange
            var model = new SweeperGameModel();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new SweeperGameTestSession();

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            model.OnGet();

            // Act
            model.OnPostToggleFlagMode(); //flag mode on
            model.OnGet();

            model.OnPostReveal(row, column); //click cell (Ruten blir 'flagget' i systemet)
            model.OnGet();

            model.OnPostToggleFlagMode(); //flag mode off
            model.OnGet();

            model.OnPostReveal(row, column); //click cell (Ingenting skjer fordi den allerede har et flagg)
            model.OnGet();

            // Assert
            model.GetCell(row, column).IsFlagged.Should().BeTrue();
            model.GetCell(row, column).IsRevealed.Should().BeFalse();
        }

        // Made using Claude
        // Confirms the "no game in session yet" branch of OnGet() creates a
        // fresh default board (12x10, 30 mines). If board size ever becomes
        // configurable this test's expected numbers will need updating -
        // that's expected, since the default-game behavior will have changed.
        [Fact]
        public void OnGet_NoSession_CreatesDefaultGame()
        {
            // Arrange
            var model = new SweeperGameModel();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new SweeperGameTestSession();

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            // Act
            model.OnGet();

            // Assert
            model.Width.Should().Be(12);
            model.Height.Should().Be(10);
            model.MineCount.Should().Be(30);
            model.GameOver.Should().BeFalse();
            model.GameWon.Should().BeFalse();
        }

        // Made using Claude
        // OnPostReveal has an InBounds() guard - clicking outside the grid
        // should silently do nothing rather than throw or change any cell.
        [Fact]
        public void OnPostReveal_OutOfBoundsCoordinates_IsIgnored()
        {
            // Arrange
            var model = new SweeperGameModel();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new SweeperGameTestSession();

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            model.OnGet();

            // Act
            model.OnPostReveal(-1, -1);
            model.OnGet();

            // Assert
            model.GameOver.Should().BeFalse();
            model.GameWon.Should().BeFalse();

            for (int x = 0; x < model.Width; x++)
                for (int y = 0; y < model.Height; y++)
                    model.GetCell(x, y).IsRevealed.Should().BeFalse();
        }

        // Made using Claude
        // Clicking an already-revealed cell a second time should be a no-op
        // (covers the cell.IsRevealed early-return in OnPostReveal).
        [Fact]
        public void OnPostReveal_AlreadyRevealedCell_DoesNothing()
        {
            // Arrange
            var model = new SweeperGameModel();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new SweeperGameTestSession();

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            model.OnGet();

            // Act
            model.OnPostReveal(0, 0); // first click, always safe by design
            model.OnGet();

            var adjacentMinesBefore = model.GetCell(0, 0).AdjacentMines;

            model.OnPostReveal(0, 0); // click the same cell again
            model.OnGet();

            // Assert
            model.GetCell(0, 0).IsRevealed.Should().BeTrue();
            model.GetCell(0, 0).AdjacentMines.Should().Be(adjacentMinesBefore);
            model.GameOver.Should().BeFalse();
        }

        // Made using Claude
        // After revealing a cell, starting a new game should hand back a
        // completely fresh, unrevealed, unflagged board.
        [Fact]
        public void OnPostNewGame_ResetsToFreshBoard()
        {
            // Arrange
            var model = new SweeperGameModel();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new SweeperGameTestSession();

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            model.OnGet();

            model.OnPostReveal(0, 0);
            model.OnGet();

            // Act
            model.OnPostNewGame();
            model.OnGet();

            // Assert
            for (int x = 0; x < model.Width; x++)
                for (int y = 0; y < model.Height; y++)
                {
                    var cell = model.GetCell(x, y);
                    cell.IsRevealed.Should().BeFalse();
                    cell.IsFlagged.Should().BeFalse();
                }

            model.GameOver.Should().BeFalse();
            model.GameWon.Should().BeFalse();
        }

        // Made using Claude
        // A bit pedantic, as flagged during review, but cheap to keep and
        // guards against FlagMode getting stuck after an even number of toggles.
        [Fact]
        public void OnPostToggleFlagMode_TwiceReturnsToOriginalState()
        {
            // Arrange
            var model = new SweeperGameModel();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new SweeperGameTestSession();

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            model.OnGet();
            var originalFlagMode = model.FlagMode;

            // Act
            model.OnPostToggleFlagMode();
            model.OnGet();

            model.OnPostToggleFlagMode();
            model.OnGet();

            // Assert
            model.FlagMode.Should().Be(originalFlagMode);
        }

        // Made using Claude
        // Writes garbage bytes directly under the same session key
        // SweeperGameModel uses ("SweeperGameState" - mirrors the private
        // SessionKey constant, since it isn't accessible from here) to force
        // LoadState()'s JSON deserialization to fail. OnGet() should recover
        // by falling back to a brand new game instead of throwing.
        [Fact]
        public void OnGet_CorruptedSessionPayload_FallsBackToNewGame()
        {
            // Arrange
            var model = new SweeperGameModel();

            var httpContext = new DefaultHttpContext();
            var session = new SweeperGameTestSession();
            httpContext.Session = session;

            session.Set("SweeperGameState", System.Text.Encoding.UTF8.GetBytes("not valid json {{{"));

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            // Act
            model.OnGet();

            // Assert
            model.GameOver.Should().BeFalse();
            model.GameWon.Should().BeFalse();
            model.Width.Should().Be(12);
            model.Height.Should().Be(10);
        }

        // Made using Claude
        // Mines are placed randomly with no seed, so this doesn't force a
        // specific mine location - instead it clicks (0,0) to trigger mine
        // placement (first click is always safe by design), then scans the
        // board for any cell that ended up a mine and clicks that one.
        [Fact]
        public void OnPostReveal_ClickingAMine_EndsGameAsLost()
        {
            // Arrange
            var model = new SweeperGameModel();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new SweeperGameTestSession();

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            model.OnGet();

            // First click is always safe by design - this also places the mines.
            model.OnPostReveal(0, 0);
            model.OnGet();

            int mineX = -1, mineY = -1;
            for (int x = 0; x < model.Width && mineX == -1; x++)
                for (int y = 0; y < model.Height && mineX == -1; y++)
                    if (model.GetCell(x, y).IsMine)
                    {
                        mineX = x;
                        mineY = y;
                    }

            mineX.Should().NotBe(-1, "a freshly created board should always contain at least one mine");

            // Act
            model.OnPostReveal(mineX, mineY);
            model.OnGet();

            // Assert
            model.GameOver.Should().BeTrue();
            model.GameWon.Should().BeFalse();
            model.GetCell(mineX, mineY).IsRevealed.Should().BeTrue();
        }

        // Made using Claude
        // Same "don't fight the randomness" approach as the mine test above:
        // click (0,0) to place mines, then keep clicking whatever safe,
        // unrevealed cell is found until the game reports a win. This drives
        // the real reveal/recursion/win-check logic exactly as a player would,
        // regardless of the random mine layout. The foundSafeCell guard is a
        // safety net against an infinite loop if the win logic ever changes.
        [Fact]
        public void OnPostReveal_AllSafeCellsRevealed_SetsGameWon()
        {
            // Arrange
            var model = new SweeperGameModel();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new SweeperGameTestSession();

            model.PageContext = new PageContext
            {
                HttpContext = httpContext
            };

            model.OnGet();

            // First click is always safe by design - this also places the mines.
            model.OnPostReveal(0, 0);
            model.OnGet();

            // Act
            bool foundSafeCell = true;
            while (!model.GameWon && !model.GameOver && foundSafeCell)
            {
                foundSafeCell = false;

                for (int x = 0; x < model.Width && !foundSafeCell; x++)
                    for (int y = 0; y < model.Height && !foundSafeCell; y++)
                    {
                        var cell = model.GetCell(x, y);
                        if (!cell.IsMine && !cell.IsRevealed)
                        {
                            model.OnPostReveal(x, y);
                            model.OnGet();
                            foundSafeCell = true;
                        }
                    }
            }

            // Assert
            model.GameWon.Should().BeTrue();
            model.GameOver.Should().BeFalse();
        }
    }
}
