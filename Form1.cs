using System;
using System.Drawing;
using System.Windows.Forms;
using System.Timers;
using System.IO; // --- MODIFIED

namespace TetrisEmulator
{
    public partial class TetrisForm : Form
    {
        private const int Rows = 20;
        private const int Cols = 10;
        private const int CellSize = 25;

        private readonly System.Timers.Timer timer;
        private int[,] board = new int[Rows, Cols];
        private Tetromino? currentPiece;
        private Point currentPos;
        private readonly Random rand = new();

        private static readonly Color[] colors = {
            Color.Cyan, Color.Yellow, Color.Purple,
            Color.Green, Color.Red, Color.Blue, Color.Orange
        };

        private static readonly int[][,] shapes = {
            new int[,] { {1,1,1,1} }, // I
            new int[,] { {1,1},{1,1} }, // O
            new int[,] { {0,1,0},{1,1,1} }, // T
            new int[,] { {0,1,1},{1,1,0} }, // S
            new int[,] { {1,1,0},{0,1,1} }, // Z
            new int[,] { {1,0,0},{1,1,1} }, // J
            new int[,] { {0,0,1},{1,1,1} }  // L
        };

        // Score and level fields
        private int score = 0;
        private int level = 1;
        private int linesCleared = 0;

        // --- MODIFIED: High score
        private int highScore = 0;
        private readonly string highScoreFile = "highscore.txt";
        private bool levelAnnounced = false; // --- MODIFIED

        public TetrisForm()
        {
            InitializeComponent();
            ClientSize = new Size(Cols * CellSize + 1, Rows * CellSize + 41); // Extra space for score/level
            Text = "Tetris";
            DoubleBuffered = true;

            LoadHighScore(); // --- MODIFIED

            NewPiece();

            timer = new System.Timers.Timer(500);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            KeyDown += TetrisForm_KeyDown;
            Paint += TetrisForm_Paint;
        }

        private static void InitializeComponent() { }

        // --- MODIFIED: Load high score from file
        private void LoadHighScore()
        {
            if (File.Exists(highScoreFile))
            {
                int.TryParse(File.ReadAllText(highScoreFile), out highScore);
            }
        }

        // --- MODIFIED: Save high score to file
        private void SaveHighScore()
        {
            try
            {
                File.WriteAllText(highScoreFile, highScore.ToString());
            }
            catch { }
        }

        private void NewPiece()
        {
            int shapeIndex = rand.Next(shapes.Length);
            currentPiece = new Tetromino(shapes[shapeIndex], shapeIndex);
            currentPos = new Point(Cols / 2 - currentPiece.Width / 2, 0);
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Invoke((MethodInvoker)(() =>
            {
                if (!levelAnnounced) // --- MODIFIED: Pause for level announcement
                {
                    MoveDown();
                }
            }));
        }

        private void MoveDown()
        {
            if (currentPiece == null) return;
            if (CanMove(currentPiece, currentPos.X, currentPos.Y + 1))
            {
                currentPos.Y++;
            }
            else
            {
                LockPiece();
                int cleared = ClearLines();
                if (cleared > 0)
                {
                    // Scoring: 1 line=100, 2=300, 3=500, 4=800 (classic Tetris)
                    int[] lineScores = { 0, 100, 300, 500, 800 };
                    score += lineScores[Math.Min(cleared, 4)] * level;
                    linesCleared += cleared;
                    // Level up every 10 lines
                    int newLevel = 1 + linesCleared / 10;
                    if (newLevel > level)
                    {
                        level = newLevel;
                        // Increase speed, but don't go below 50ms
                        timer.Interval = Math.Max(500 - (level - 1) * 40, 50);

                        // --- MODIFIED: Announce new level and pause
                        levelAnnounced = true;
                        timer.Stop();
                        BeginInvoke((MethodInvoker)(() =>
                        {
                            MessageBox.Show($"Level {level}!", "Level Up");
                            levelAnnounced = false;
                            timer.Start();
                        }));
                    }
                }
                NewPiece();
                if (!CanMove(currentPiece, currentPos.X, currentPos.Y))
                {
                    timer.Stop();
                    // --- MODIFIED: Update high score if needed
                    if (score > highScore)
                    {
                        highScore = score;
                        SaveHighScore();
                        MessageBox.Show($"Game Over!\nNEW HIGH SCORE: {score}\nLevel: {level}", "Tetris");
                    }
                    else
                    {
                        MessageBox.Show($"Game Over!\nScore: {score}\nLevel: {level}\nHigh Score: {highScore}", "Tetris");
                    }
                    board = new int[Rows, Cols];
                    score = 0;
                    level = 1;
                    linesCleared = 0;
                    timer.Interval = 500;
                    NewPiece();
                    timer.Start();
                }
            }
            Invalidate();
        }

        private void LockPiece()
        {
            if (currentPiece == null) return;
            for (int y = 0; y < currentPiece.Height; y++)
            {
                for (int x = 0; x < currentPiece.Width; x++)
                {
                    if (currentPiece.Shape[y, x] != 0)
                    {
                        board[currentPos.Y + y, currentPos.X + x] = currentPiece.Shape[y, x];
                    }
                }
            }
        }

        // Returns number of lines cleared
        private int ClearLines()
        {
            int cleared = 0;
            for (int y = Rows - 1; y >= 0; y--)
            {
                bool full = true;
                for (int x = 0; x < Cols; x++)
                {
                    if (board[y, x] == 0)
                    {
                        full = false;
                        break;
                    }
                }

                if (full)
                {
                    for (int row = y; row > 0; row--)
                        for (int col = 0; col < Cols; col++)
                            board[row, col] = board[row - 1, col];

                    for (int col = 0; col < Cols; col++)
                        board[0, col] = 0;

                    y++;
                    cleared++;
                }
            }
            return cleared;
        }

        private bool CanMove(Tetromino? piece, int x, int y)
        {
            if (piece == null) return false;
            for (int py = 0; py < piece.Height; py++)
            {
                for (int px = 0; px < piece.Width; px++)
                {
                    if (piece.Shape[py, px] != 0)
                    {
                        int boardX = x + px;
                        int boardY = y + py;
                        if (boardX < 0 || boardX >= Cols || boardY < 0 || boardY >= Rows)
                            return false;
                        if (board[boardY, boardX] != 0)
                            return false;
                    }
                }
            }
            return true;
        }

        private void RotatePiece()
        {
            if (currentPiece == null) return;
            var rotated = currentPiece.Rotate();
            if (CanMove(rotated, currentPos.X, currentPos.Y))
            {
                currentPiece = rotated;
            }
        }

        private void TetrisForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (levelAnnounced) return; // --- MODIFIED: Ignore input during announcement

            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (CanMove(currentPiece, currentPos.X - 1, currentPos.Y))
                        currentPos.X--;
                    break;
                case Keys.Right:
                    if (CanMove(currentPiece, currentPos.X + 1, currentPos.Y))
                        currentPos.X++;
                    break;
                case Keys.Down:
                    MoveDown();
                    break;
                case Keys.Up:
                    RotatePiece();
                    break;
                case Keys.Space:
                    while (CanMove(currentPiece, currentPos.X, currentPos.Y + 1))
                        currentPos.Y++;
                    MoveDown();
                    break;
            }
            Invalidate();
        }

        private void TetrisForm_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.FillRectangle(Brushes.Black, 0, 0, Cols * CellSize, Rows * CellSize);

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (board[y, x] != 0)
                    {
                        using var b = new SolidBrush(colors[board[y, x] - 1]);
                        g.FillRectangle(b, x * CellSize, y * CellSize, CellSize - 1, CellSize - 1);
                    }
                }
            }

            if (currentPiece != null)
            {
                for (int y = 0; y < currentPiece.Height; y++)
                {
                    for (int x = 0; x < currentPiece.Width; x++)
                    {
                        if (currentPiece.Shape[y, x] != 0)
                        {
                            using var b = new SolidBrush(colors[currentPiece.Shape[y, x] - 1]);
                            g.FillRectangle(b, (currentPos.X + x) * CellSize, (currentPos.Y + y) * CellSize, CellSize - 1, CellSize - 1);
                        }
                    }
                }
            }

            using var pen = new Pen(Color.Gray);
            for (int i = 0; i <= Rows; i++)
                g.DrawLine(pen, 0, i * CellSize, Cols * CellSize, i * CellSize);
            for (int i = 0; i <= Cols; i++)
                g.DrawLine(pen, i * CellSize, 0, i * CellSize, Rows * CellSize);

            // Draw score, level, and high score
            using var font = new Font("Arial", 14, FontStyle.Bold);
            g.DrawString($"Score: {score}", font, Brushes.Black, 5, Rows * CellSize + 2);
            g.DrawString($"Level: {level}", font, Brushes.Black, 150, Rows * CellSize + 2);
            g.DrawString($"High: {highScore}", font, Brushes.Yellow, 300, Rows * CellSize + 2); // --- MODIFIED
        }

        class Tetromino
        {
            public int[,] Shape { get; private set; }
            public int Width => Shape.GetLength(1);
            public int Height => Shape.GetLength(0);
            public int ColorIndex { get; }

            public Tetromino(int[,] shape, int colorIndex)
            {
                ColorIndex = colorIndex;
                Shape = new int[shape.GetLength(0), shape.GetLength(1)];
                for (int y = 0; y < shape.GetLength(0); y++)
                    for (int x = 0; x < shape.GetLength(1); x++)
                        Shape[y, x] = shape[y, x] != 0 ? ColorIndex + 1 : 0;
            }

            public Tetromino Rotate()
            {
                int[,] rotated = new int[Width, Height];
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        rotated[x, Height - y - 1] = Shape[y, x] != 0 ? ColorIndex + 1 : 0;

                return new Tetromino(rotated, ColorIndex);
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TetrisForm());
        }
    }
}
