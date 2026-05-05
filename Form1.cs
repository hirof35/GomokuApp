using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Windows.Forms;
using System.Collections.Generic;

namespace GomokuApp
{
    public partial class GomokuForm : Form
    {
        // --- 設定・定数 ---
        private const int GridSize = 15;
        private const int CellSize = 40;
        private const int MarginSize = 40;
        // フィールド変数（上の方）に追加
        private Rectangle btnBackToTitle;

        
        enum GameState { Title, Playing, GameOver }
        enum Difficulty { Easy, Normal, Hard }
        enum StoneStyle { Standard, Metallic }

        // --- フィールド ---
        private int[,] state = new int[GridSize, GridSize]; // 0:空, 1:黒(人), 2:白(CPU)
        private GameState currentState = GameState.Title;
        private Difficulty currentDifficulty = Difficulty.Normal;
        private StoneStyle currentStoneStyle = StoneStyle.Standard;
        private bool isBlackTurn = true;
        private SoundPlayer stoneSound;

        // UI用ボタン領域
        private Rectangle btnEasy, btnNormal, btnHard, btnStd, btnMeta, btnStart;

        public GomokuForm()
        {
            this.Text = "本格・五目並べ 2026";
            this.ClientSize = new Size(CellSize * (GridSize - 1) + MarginSize * 2, CellSize * (GridSize - 1) + MarginSize * 2 + 50);
            this.DoubleBuffered = true;
            this.Font = new Font("Yu Gothic UI", 10);
            // コンストラクタ（GomokuForm()）の中に追加
            btnBackToTitle = new Rectangle(this.ClientSize.Width - 110, 10, 100, 30);
            // ボタン配置の計算
            int bw = 80; int bh = 30; int y = this.ClientSize.Height - 120;
            btnEasy = new Rectangle(MarginSize, y, bw, bh);
            btnNormal = new Rectangle(MarginSize + bw + 10, y, bw, bh);
            btnHard = new Rectangle(MarginSize + (bw + 10) * 2, y, bw, bh);
            btnStd = new Rectangle(MarginSize, y + 40, bw + 40, bh);
            btnMeta = new Rectangle(MarginSize + bw + 50, y + 40, bw + 40, bh);
            btnStart = new Rectangle(this.ClientSize.Width / 2 - 60, y + 80, 120, 40);

            try { stoneSound = new SoundPlayer("stone.wav"); stoneSound.Load(); } catch { }
        }

        // --- 描画ロジック ---
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (currentState == GameState.Title)
            {
                DrawTitle(g);
            }
            else
            {
                DrawBoard(g);
                if (currentState == GameState.GameOver) DrawOverlay(g, "ゲーム終了");
            }
        }

        private void DrawTitle(Graphics g)
        {
            g.Clear(Color.FromArgb(40, 44, 52));
            using (Font f = new Font(this.Font.FontFamily, 28, FontStyle.Bold))
                g.DrawString("GOMOKU 2026", f, Brushes.White, 80, 100);

            DrawBtn(g, "初級", btnEasy, currentDifficulty == Difficulty.Easy);
            DrawBtn(g, "中級", btnNormal, currentDifficulty == Difficulty.Normal);
            DrawBtn(g, "上級", btnHard, currentDifficulty == Difficulty.Hard);
            DrawBtn(g, "標準石", btnStd, currentStoneStyle == StoneStyle.Standard);
            DrawBtn(g, "メタリック", btnMeta, currentStoneStyle == StoneStyle.Metallic);
            DrawBtn(g, "対戦開始", btnStart, true, Color.OrangeRed);
        }

        private void DrawBtn(Graphics g, string txt, Rectangle r, bool sel, Color? c = null)
        {
            g.FillRectangle(sel ? (c.HasValue ? new SolidBrush(c.Value) : Brushes.DodgerBlue) : Brushes.DimGray, r);
            g.DrawRectangle(Pens.White, r);
            TextRenderer.DrawText(g, txt, this.Font, r, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void DrawBoard(Graphics g)
        {
            g.Clear(Color.BurlyWood);
            Pen p = new Pen(Color.Black, 1);
            for (int i = 0; i < GridSize; i++)
            {
                g.DrawLine(p, MarginSize, MarginSize + i * CellSize, MarginSize + (GridSize - 1) * CellSize, MarginSize + i * CellSize);
                g.DrawLine(p, MarginSize + i * CellSize, MarginSize, MarginSize + i * CellSize, MarginSize + (GridSize - 1) * CellSize);
            }
            // 右上に「タイトルへ」ボタンを描画
            DrawBtn(g, "タイトルへ", btnBackToTitle, false, Color.Brown);
            for (int x = 0; x < GridSize; x++)
                for (int y = 0; y < GridSize; y++)
                    if (state[x, y] != 0) DrawStone(g, x, y, state[x, y]);
        }

        private void DrawStone(Graphics g, int x, int y, int color)
        {
            Rectangle r = new Rectangle(MarginSize + x * CellSize - 16, MarginSize + y * CellSize - 16, 32, 32);
            if (currentStoneStyle == StoneStyle.Standard)
            {
                if (color == 1) g.FillEllipse(Brushes.Black, r);
                else { g.FillEllipse(Brushes.White, r); g.DrawEllipse(Pens.Gray, r); }
            }
            else
            {
                Color c1 = (color == 1) ? Color.Gold : Color.WhiteSmoke;
                Color c2 = (color == 1) ? Color.DarkGoldenrod : Color.Gray;
                using (var b = new LinearGradientBrush(r, c1, c2, 45f)) g.FillEllipse(b, r);
                g.DrawEllipse(new Pen(c2), r);
            }
        }

        private void DrawOverlay(Graphics g, string msg)
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(150, 0, 0, 0)), this.ClientRectangle);
            TextRenderer.DrawText(g, msg + "\nクリックでタイトルへ", this.Font, this.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // --- 入力・進行 ---
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (currentState != GameState.Playing)
            {
                if (btnEasy.Contains(e.Location)) currentDifficulty = Difficulty.Easy;
                else if (btnNormal.Contains(e.Location)) currentDifficulty = Difficulty.Normal;
                else if (btnHard.Contains(e.Location)) currentDifficulty = Difficulty.Hard;
                else if (btnStd.Contains(e.Location)) currentStoneStyle = StoneStyle.Standard;
                else if (btnMeta.Contains(e.Location)) currentStoneStyle = StoneStyle.Metallic;
                else if (btnStart.Contains(e.Location) || currentState == GameState.GameOver) StartGame();
                this.Invalidate(); return;
            }
            // 対戦中に「タイトルへ」ボタンが押されたかチェック
            if (currentState == GameState.Playing && btnBackToTitle.Contains(e.Location))
            {
                currentState = GameState.Title;
                this.Invalidate();
                return;
            }
            int x = (e.X - MarginSize + CellSize / 2) / CellSize;
            int y = (e.Y - MarginSize + CellSize / 2) / CellSize;

            if (x >= 0 && x < GridSize && y >= 0 && y < GridSize && state[x, y] == 0)
            {
                if (IsForbidden(x, y, 1)) { MessageBox.Show("禁手です！"); return; }
                PlaceStone(x, y, 1);
                if (currentState == GameState.Playing) CpuTurn();
            }
        }

        private void StartGame() { Array.Clear(state, 0, state.Length); currentState = GameState.Playing; isBlackTurn = true; }

        private void PlaceStone(int x, int y, int color)
        {
            state[x, y] = color;
            try { stoneSound?.Play(); } catch { }
            this.Invalidate();
            if (CheckWin(x, y, color))
            {
                currentState = GameState.GameOver;
                MessageBox.Show((color == 1 ? "プレイヤー" : "CPU") + "の勝利！");
            }
        }

        // --- AIロジック (簡易版) ---
        private void CpuTurn()
        {
            Point best = new Point(7, 7); double max = -1;
            double wA = 1.0, wD = (currentDifficulty == Difficulty.Hard) ? 1.5 : (currentDifficulty == Difficulty.Easy ? 0.5 : 1.0);

            for (int x = 0; x < GridSize; x++)
                for (int y = 0; y < GridSize; y++)
                    if (state[x, y] == 0)
                    {
                        double score = Eval(x, y, 2) * wA + Eval(x, y, 1) * wD + (GridSize - Math.Abs(x - 7) - Math.Abs(y - 7));
                        if (score > max) { max = score; best = new Point(x, y); }
                    }
            PlaceStone(best.X, best.Y, 2);
        }

        private int Eval(int x, int y, int c)
        {
            int score = 0; int[] dx = { 1, 0, 1, 1 }, dy = { 0, 1, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int cnt = 1;
                cnt += Count(x, y, dx[i], dy[i], c, out bool a);
                cnt += Count(x, y, -dx[i], -dy[i], c, out bool b);
                if (cnt >= 5) score += 100000;
                else if (cnt == 4 && a && b) score += 10000;
                else if (cnt == 4 && (a || b)) score += 1000;
                else if (cnt == 3 && a && b) score += 800;
            }
            return score;
        }

        private int Count(int x, int y, int dx, int dy, int c, out bool o)
        {
            int n = 0; int nx = x + dx, ny = y + dy;
            while (nx >= 0 && nx < GridSize && ny >= 0 && ny < GridSize && state[nx, ny] == c) { n++; nx += dx; ny += dy; }
            o = (nx >= 0 && nx < GridSize && ny >= 0 && ny < GridSize && state[nx, ny] == 0);
            return n;
        }

        private bool CheckWin(int x, int y, int c)
        {
            int[] dx = { 1, 0, 1, 1 }, dy = { 0, 1, 1, -1 };
            for (int i = 0; i < 4; i++)
                if (1 + Count(x, y, dx[i], dy[i], c, out _) + Count(x, y, -dx[i], -dy[i], c, out _) >= 5) return true;
            return false;
        }

        private bool IsForbidden(int x, int y, int c)
        {
            if (c != 1) return false;
            int three = 0; int[] dx = { 1, 0, 1, 1 }, dy = { 0, 1, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                if (1 + Count(x, y, dx[i], dy[i], 1, out bool a) + Count(x, y, -dx[i], -dy[i], 1, out bool b) == 3 && a && b) three++;
            }
            return three >= 2; // 三三禁手のみ
        }
    }

   
}
