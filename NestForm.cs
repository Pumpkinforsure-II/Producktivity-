using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Producktivity
{
    public class NestForm : Form
    {
        private Image nestImage;

        private int nestX;
        private int nestY;
        private const int nestW = 130;
        private const int nestH = 80;
        private const int duckH = 200;  // 与 Form1 的 ClientSize.Height 一致

        private bool isDragging = false;
        private int  dragStartX, dragStartY;

        private Form1          duckForm;
        private StickerManager stickerManager;
        private StatsManager   statsManager;

        public NestForm(Form1 duck, StickerManager sm, StatsManager stats)
        {
            duckForm       = duck;
            stickerManager = sm;
            statsManager   = stats;
            LoadImage();
            SetupWindow();
        }

        private void LoadImage()
        {
            try   { nestImage = Image.FromFile("images/nest.png"); }
            catch { nestImage = null; }
        }

        private void SetupWindow()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost         = true;
            this.BackColor       = Color.Black;
            this.TransparencyKey = Color.Black;
            this.ClientSize      = new Size(nestW, nestH);

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            int screenW = Screen.PrimaryScreen.Bounds.Width;
            nestX = screenW / 2 - nestW / 2;
            nestY = 300;
            this.Location = new Point(nestX, nestY);

            this.MouseDown  += OnMouseDown;
            this.MouseMove  += OnMouseMove;
            this.MouseUp    += OnMouseUp;
            this.MouseClick += OnMouseClick;
        }

        public int GetNestCenterX() => this.Location.X + nestW / 2;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode   = PixelOffsetMode.Half;
            g.SmoothingMode     = SmoothingMode.None;
            g.CompositingMode   = CompositingMode.SourceOver;

            if (nestImage != null)
            {
                g.DrawImage(nestImage, 0, 0, nestW, nestH);
            }
            else
            {
                using var brush    = new SolidBrush(Color.SandyBrown);
                using var pen      = new Pen(Color.Peru, 3);
                using var strawPen = new Pen(Color.Goldenrod, 2);
                g.FillEllipse(brush, 0, 10, nestW, nestH - 10);
                g.DrawEllipse(pen,   0, 10, nestW, nestH - 10);
                g.DrawLine(strawPen,  20,  30,  50,  50);
                g.DrawLine(strawPen,  60,  20,  80,  55);
                g.DrawLine(strawPen, 100,  25, 120,  52);
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartX = e.X;
                dragStartY = e.Y;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            int screenW = Screen.PrimaryScreen.Bounds.Width;
            int screenH = Screen.PrimaryScreen.Bounds.Height;

            nestX = Math.Clamp(this.Location.X + e.X - dragStartX, 0, screenW - nestW);
            nestY = Math.Clamp(this.Location.Y + e.Y - dragStartY, 0, screenH - nestH);

            this.Location = new Point(nestX, nestY);
            duckForm.SetDuckY(nestY + nestH - duckH);

            if (duckForm.IsResting)
                duckForm.SetDuckX(nestX + nestW / 2 - 75);
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) isDragging = false;
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var menu = new ContextMenuStrip();

            // 休息 / 出巢
            if (duckForm.IsResting)
            {
                var item = new ToolStripMenuItem("鸭子出巢");
                item.Click += (s, _) => duckForm.WakeUp();
                menu.Items.Add(item);
            }
            else
            {
                var item = new ToolStripMenuItem("鸭子休息");
                item.Click += (s, _) => duckForm.RequestRest();
                menu.Items.Add(item);
            }

            menu.Items.Add(new ToolStripSeparator());

            // 聊天框
            var chatItem = new ToolStripMenuItem("开启聊天框");
            chatItem.Click += (s, _) => duckForm.OpenChat();
            menu.Items.Add(chatItem);

            // 设置（原"个性化"）
            var settingsItem = new ToolStripMenuItem("设置");
            settingsItem.Click += (s, _) =>
            {
                var sf = new SettingsForm(duckForm, stickerManager, statsManager);
                sf.Show();
            };
            menu.Items.Add(settingsItem);

            menu.Show(this, e.Location);
        }
    }
}