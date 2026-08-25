using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using TreningsAppHaffi.Data;

namespace TreningsAppHaffi.Pages
{
    public class SweeperGameModel : PageModel
    {
        private const string SessionKey = "SweeperGameState"; //Hvis den endres, Vil OnGet_CorruptedSessionPayload_FallsBackToNewGame faile uten lyd.

        public int Width { get; private set; } = 12;
        public int Height { get; private set; } = 10;
        public int MineCount { get; private set; }
        public bool GameOver { get; private set; }
        public bool GameWon { get; private set; }
        public bool FlagMode { get; private set; }
        public int ElapsedSeconds { get; private set; }
        public bool Locked => GameOver || GameWon;
        public string ElapsedDisplay => $"{ElapsedSeconds / 60:00}:{ElapsedSeconds % 60:00}";

        private List<SweeperCell> _cells = new();

        public SweeperCell GetCell(int x, int y) => _cells[y * Width + x];

        public void OnGet()
        {
            var state = LoadState();
            if (state == null)
            {
                state = CreateNewGame(Width, Height);
                SaveState(state);
            }
            ApplyState(state);
        }

        public IActionResult OnPostNewGame()
        {
            SaveState(CreateNewGame(Width, Height));
            return RedirectToPage();
        }

        public IActionResult OnPostToggleFlagMode()
        {
            var state = LoadState() ?? CreateNewGame(Width, Height);
            if (!state.GameOver && !state.GameWon)
                state.FlagMode = !state.FlagMode;
            SaveState(state);
            return RedirectToPage();
        }

        public IActionResult OnPostReveal(int x, int y)
        {
            var state = LoadState();
            if (state == null)
            {
                SaveState(CreateNewGame(Width, Height));
                return RedirectToPage();
            }

            if (state.GameOver || state.GameWon || !InBounds(state, x, y))
            {
                return RedirectToPage();
            }

            var cell = state.Cells[y * state.Width + x];

            // Flag mode: left click toggles a flag, never reveals.
            if (state.FlagMode)
            {
                if (!cell.IsRevealed)
                    cell.IsFlagged = !cell.IsFlagged;

                SaveState(state);
                return RedirectToPage();
            }

            // Flagged cells ignore normal clicks until un-flagged.
            if (cell.IsFlagged || cell.IsRevealed)
            {
                return RedirectToPage();
            }

            // First click of the game: place mines now, guaranteed not on (x, y).
            if (state.FirstClick)
            {
                PlaceMines(state, x, y);
                CalculateAdjacents(state);
                state.FirstClick = false;
                state.StartTimeUtc = DateTime.UtcNow;
            }

            if (cell.IsMine)
            {
                cell.IsRevealed = true;
                state.GameOver = true;
                state.FinalElapsedSeconds = GetElapsedSeconds(state);
            }
            else
            {
                RevealRecursive(state, x, y);

                if (CheckWin(state))
                {
                    state.GameWon = true;
                    state.FinalElapsedSeconds = GetElapsedSeconds(state);
                }
            }

            SaveState(state);
            return RedirectToPage();
        }

        // ----- session load/save -----

        private GameState? LoadState()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonSerializer.Deserialize<GameState>(json);
            }
            catch
            {
                return null; // corrupted/old session payload - treat as no game yet
            }
        }

        private void SaveState(GameState state)
        {
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(state));
        }

        private void ApplyState(GameState state)
        {
            Width = state.Width;
            Height = state.Height;
            MineCount = state.MineCount;
            GameOver = state.GameOver;
            GameWon = state.GameWon;
            FlagMode = state.FlagMode;
            _cells = state.Cells;
            ElapsedSeconds = (int)GetElapsedSeconds(state);
        }

        private double GetElapsedSeconds(GameState state)
        {
            if (state.FinalElapsedSeconds.HasValue)
                return state.FinalElapsedSeconds.Value;

            if (state.StartTimeUtc.HasValue)
                return (DateTime.UtcNow - state.StartTimeUtc.Value).TotalSeconds;

            return 0;
        }

        // ----- game setup / rules -----

        private GameState CreateNewGame(int width, int height)
        {
            width = Math.Clamp(width, 4, 20);
            height = Math.Clamp(height, 4, 20);

            var state = new GameState
            {
                Width = width,
                Height = height,
                MineCount = Math.Max(1, (width * height) / 4),
                Cells = new List<SweeperCell>(width * height)
            };

            for (int i = 0; i < width * height; i++)
                state.Cells.Add(new SweeperCell());

            return state;
        }

        private bool InBounds(GameState state, int x, int y)
            => x >= 0 && x < state.Width && y >= 0 && y < state.Height;

        // Places mines, skipping (safeX, safeY) so the first click is never a mine,
        // and avoiding dense 3x3 clusters like the original logic did.
        private void PlaceMines(GameState state, int safeX, int safeY)
        {
            var rand = new Random();
            int placed = 0;

            while (placed < state.MineCount)
            {
                int x = rand.Next(state.Width);
                int y = rand.Next(state.Height);

                if (x == safeX && y == safeY) continue;

                var cell = state.Cells[y * state.Width + x];
                if (cell.IsMine) continue;

                if (WouldCreateDenseCluster(state, x, y)) continue;

                cell.IsMine = true;
                placed++;
            }
        }

        private bool WouldCreateDenseCluster(GameState state, int x, int y)
        {
            int mines = 0;

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (InBounds(state, nx, ny) && state.Cells[ny * state.Width + nx].IsMine)
                        mines++;
                }

            return mines >= 5;
        }

        private void CalculateAdjacents(GameState state)
        {
            for (int x = 0; x < state.Width; x++)
                for (int y = 0; y < state.Height; y++)
                {
                    var cell = state.Cells[y * state.Width + x];
                    if (cell.IsMine) continue;

                    int count = 0;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (InBounds(state, nx, ny) && state.Cells[ny * state.Width + nx].IsMine)
                                count++;
                        }

                    cell.AdjacentMines = count;
                }
        }

        private void RevealRecursive(GameState state, int x, int y)
        {
            if (!InBounds(state, x, y)) return;

            var cell = state.Cells[y * state.Width + x];
            if (cell.IsRevealed || cell.IsFlagged) return;

            cell.IsRevealed = true;

            if (cell.AdjacentMines > 0) return;

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    RevealRecursive(state, x + dx, y + dy);
                }
        }

        private bool CheckWin(GameState state)
        {
            foreach (var c in state.Cells)
                if (!c.IsMine && !c.IsRevealed) return false;

            return true;
        }

        // Serializable per-user game state, stored as JSON in Session.
        private class GameState
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int MineCount { get; set; }
            public bool GameOver { get; set; }
            public bool GameWon { get; set; }
            public bool FlagMode { get; set; }
            public bool FirstClick { get; set; } = true;
            public DateTime? StartTimeUtc { get; set; }
            public double? FinalElapsedSeconds { get; set; }
            public List<SweeperCell> Cells { get; set; } = new();
        }
    }
}
