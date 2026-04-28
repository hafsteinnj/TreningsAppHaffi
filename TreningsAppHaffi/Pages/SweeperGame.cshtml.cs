using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TreningsAppHaffi.Data;

namespace TreningsAppHaffi.Pages
{
    public class SweeperGameModel : PageModel
    {
        //Forsøk nr 2 fra chatgpt,
        //Får se om koden krasjer denne gangen.
        private static SweeperCell[,] Grid;

        public int Width { get; set; } = 12;
        public int Height { get; set; } = 10;
        public int MineCount { get; set; }

        public bool GameOver { get; set; }
        public bool GameWon { get; set; }
        public bool FirstClick { get; set; } = true;

        public SweeperCell[,] GameGrid => Grid;

        public void OnGet()
        {
            InitializeGame();
        }

        public IActionResult OnPostNewGame()
        {
            InitializeGame();
            return Page();
        }

        private void InitializeGame()
        {
            Width = Math.Clamp(Width, 4, 20);
            Height = Math.Clamp(Height, 4, 20);

            Grid = new SweeperCell[Width, Height];

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Grid[x, y] = new SweeperCell();

            MineCount = Math.Max(1, (Width * Height) / 4);

            PlaceMines();
            CalculateAdjacents();

            GameOver = false;
            GameWon = false;
            FirstClick = true;
        }

        public IActionResult OnPostReveal(int x, int y)
        {
            if (GameOver || GameWon)
                return Page();

            var cell = Grid[x, y];

            if (cell.IsFlagged || cell.IsRevealed)
                return Page();

            if (FirstClick)
                FirstClick = false;

            if (cell.IsMine)
            {
                cell.IsRevealed = true;
                GameOver = true;
                return Page();
            }

            RevealRecursive(x, y);

            CheckWin();

            return Page();
        }

        // Plaserer miner tilfeldig. Men prøver å unngå å plassere miner i 3x3 grid / clusters.
        private void PlaceMines()
        {
            var rand = new Random();
            int placed = 0;

            while (placed < MineCount)
            {
                int x = rand.Next(Width);
                int y = rand.Next(Height);

                if (Grid[x, y].IsMine)
                    continue;

                // Prevent 3x3 full mine clusters
                if (WouldCreateDenseCluster(x, y))
                    continue;

                Grid[x, y].IsMine = true;
                placed++;
            }
        }

        private bool WouldCreateDenseCluster(int x, int y)
        {
            int mines = 0;

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (InBounds(nx, ny) && Grid[nx, ny].IsMine)
                        mines++;
                }

            return mines >= 5; // tweak threshold
        }

        // kalkulerer antall miner rundt hver celle
        private void CalculateAdjacents()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (Grid[x, y].IsMine) continue;

                    int count = 0;

                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (InBounds(nx, ny) && Grid[nx, ny].IsMine)
                                count++;
                        }

                    Grid[x, y].AdjacentMines = count;
                }
        }

        //flood fill logikk for å avsløre alle tilstøtende celler uten miner
        private void RevealRecursive(int x, int y)
        {
            if (!InBounds(x, y))
                return;

            var cell = Grid[x, y];

            if (cell.IsRevealed || cell.IsFlagged)
                return;

            cell.IsRevealed = true;

            // STOP condition:
            // If this cell has adjacent mines, do NOT continue spreading
            if (cell.AdjacentMines > 0)
                return;

            // Otherwise, expand to all neighbors
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    RevealRecursive(x + dx, y + dy);
                }
        }

        //win condition: alle ikke-miner er avslørt
        private void CheckWin()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var c = Grid[x, y];

                    if (!c.IsMine && !c.IsRevealed)
                        return;
                }

            GameWon = true;
        }

        // Ting chatgpt *host* glemte...
        private bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width &&
                   y >= 0 && y < Height;
        }

        //flagging av celler
        public IActionResult OnPostToggleFlag(int x, int y)
        {
            var cell = Grid[x, y];

            if (!cell.IsRevealed)
                cell.IsFlagged = !cell.IsFlagged;

            //return Page(); //dette skapte problemer når siden var lastet opp i azure.
            /*
             * The browser tries to reload the page
                But the last request was a POST → browser says:
                “If I reload, I’ll repeat that POST. Are you sure?”
                That’s the “resend form data” warning you’re seeing in Firefox.

            Why Azure makes it worse
            Locally, browsers sometimes behave more forgivingly.

                On Azure:
                stricter request handling
                full round-trip latency
                no cached state

                → so the POST + reload pattern becomes unreliable


            The correct pattern (Post-Redirect-Get)

                In web apps, you should never return a Page() after POST if the user might refresh.
                Instead:

                POST → Redirect → GET

            Se gjerne samtalen jeg har med chatgpt om dette her: https://docs.google.com/document/d/15MGG57hWhO_1JxQmzfQTw3E9qkvajaEqVVgkiZAWgWk/edit?usp=sharing
            (coppy paste noe av teksten over og search)
             */

            return RedirectToPage(); // ✅ converts flow to GET
        }

    }
}
