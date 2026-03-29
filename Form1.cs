using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Producktivity
{
    public partial class Form1 : Form
    {
        // ── 图像 ─────────────────────────────────────────────
        private Image footClosed;
        private Image footOpen;
        private Image currentImage;

        // ── 动画 ─────────────────────────────────────────────
        private System.Windows.Forms.Timer animationTimer;
        private bool isFootOpen       = false;
        private int  walkFrameCounter = 0;

        // ── 位置 / 移动 ───────────────────────────────────────
        private int  duckX     = 0;
        private int  duckY     = 300;
        private int  speedX    = 3;
        private bool facingLeft = true;

        private bool isDragging = false;
        private int  dragStartX, dragStartY;

        // ── 随机消息计时器 ────────────────────────────────────
        private System.Windows.Forms.Timer _randomMsgTimer;
        private readonly Random rnd = new Random();

        // ── 休息状态 ──────────────────────────────────────────
        private bool     restPending = false;
        private bool     isResting   = false;
        private NestForm nestForm;

        // ── 外部依赖 ──────────────────────────────────────────
        private ChatForm       chatForm;
        private StickerManager stickerManager;
        private StatsManager   statsManager;

        // ── 控制参数 ─────────────────────────────────────────
        public int    RandomLB   { get; set; } = 15;
        public int    RandomHB   { get; set; } = 45;
        public double TextRatioK { get; set; } = 0.6;

        // ── 鸭子名称 ──────────────────────────────────────────
        private string _duckName = "DUCK";
        private static readonly string NameSavePath = "duck_name.txt";

        public string DuckName
        {
            get => _duckName;
            set
            {
                _duckName = string.IsNullOrWhiteSpace(value) ? "DUCK" : value;
                try { File.WriteAllText(NameSavePath, _duckName); } catch { }
                chatForm?.UpdateDuckName(_duckName);
                this.Invalidate();
            }
        }

        public bool IsResting => isResting;

        // ── 构造 ─────────────────────────────────────────────
        public Form1()
        {
            InitializeComponent();
            LoadImages();
            SetupWindow();
            LoadDuckName();
        }

        private void LoadImages()
        {
            try
            {
                footClosed   = Image.FromFile("images/foot_closed.png");
                footOpen     = Image.FromFile("images/foot_open.png");
                currentImage = footClosed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图片失败: {ex.Message}");
                Application.Exit();
            }
        }

        private void LoadDuckName()
        {
            if (!File.Exists(NameSavePath)) return;
            try
            {
                string saved = File.ReadAllText(NameSavePath).Trim();
                if (!string.IsNullOrEmpty(saved)) _duckName = saved;
            }
            catch { }
        }

        private void SetupWindow()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost         = true;
            this.BackColor       = Color.Black;
            this.TransparencyKey = Color.Black;
            this.ClientSize      = new Size(150, 200);

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            speedX     = (rnd.Next(2) == 0) ? 3 : -3;
            facingLeft = (speedX < 0);

            this.MouseDown  += OnMouseDown;
            this.MouseMove  += OnMouseMove;
            this.MouseUp    += OnMouseUp;
            this.MouseClick += OnMouseClick;
        }

        // ── 注入依赖 ──────────────────────────────────────────
        public void SetNestForm(NestForm nest)            => nestForm       = nest;
        public void SetChatForm(ChatForm chat)            => chatForm       = chat;
        public void SetStickerManager(StickerManager sm)  => stickerManager = sm;
        public void SetStatsManager(StatsManager stats)   => statsManager   = stats;

        // ── 休息控制 ──────────────────────────────────────────
        public void RequestRest()  { restPending = true;  isResting = false; }
        public void WakeUp()       { isResting   = false; restPending = false; }

        public void OpenChat()
        {
            if (chatForm == null) return;
            chatForm.Show();
            chatForm.BringToFront();
        }

        // ── 动画启动 ──────────────────────────────────────────
        public void StartAnimation()
        {
            animationTimer          = new System.Windows.Forms.Timer();
            animationTimer.Interval = 16;
            animationTimer.Tick    += OnAnimationTick;
            animationTimer.Start();
            StartRandomMsgTimer();
        }

        // ── 随机消息计时器 ────────────────────────────────────
        private void StartRandomMsgTimer()
        {
            _randomMsgTimer          = new System.Windows.Forms.Timer();
            _randomMsgTimer.Interval = NextRandomInterval();
            _randomMsgTimer.Tick    += OnRandomMsgTick;
            _randomMsgTimer.Start();
        }

        private int NextRandomInterval()
        {
            int lb = Math.Max(1, RandomLB);
            int hb = Math.Max(lb + 1, RandomHB);
            return (lb + rnd.Next(hb - lb + 1)) * 1000;
        }

        public void RefreshRandomTimer()
        {
            if (_randomMsgTimer != null)
                _randomMsgTimer.Interval = NextRandomInterval();
        }

        private void OnRandomMsgTick(object sender, EventArgs e)
        {
            _randomMsgTimer.Interval = NextRandomInterval();
            TriggerRandomMessage();
        }

        public void TriggerRandomMessage()
        {
            if (rnd.NextDouble() < TextRatioK)
            {
                string msg = DuckMessages.PickRandom(rnd);
                chatForm?.AddMessage(msg);
                statsManager?.AddTextMsg();
            }
            else
            {
                if (stickerManager == null || !stickerManager.HasEnabled) return;
                var entry = stickerManager.PickRandom(rnd);
                if (entry == null) return;
                var img = entry.GetImage();
                if (img == null) return;
                chatForm?.AddImageMessage(img);
                statsManager?.AddStickerMsg();
            }
        }

        // ── 动画 Tick ─────────────────────────────────────────
        private void OnAnimationTick(object sender, EventArgs e)
        {
            if (isDragging) return;
            if (isResting)  return;

            duckX += speedX;

            if (restPending && nestForm != null)
            {
                int nestCenterX = nestForm.GetNestCenterX() - this.Width / 2;
                int prevX       = duckX - speedX;
                bool passed = (speedX > 0)
                    ? prevX < nestCenterX && duckX >= nestCenterX
                    : prevX > nestCenterX && duckX <= nestCenterX;

                if (passed)
                {
                    duckX        = nestCenterX;
                    restPending  = false;
                    isResting    = true;
                    currentImage = footClosed;
                    this.Location = new Point(duckX, duckY);
                    this.Invalidate();
                    return;
                }
            }

            int screenW = Screen.PrimaryScreen.Bounds.Width;
            if (duckX <= 0 || duckX >= screenW - this.Width)
            {
                speedX = -speedX;
                duckX  = Math.Clamp(duckX, 0, screenW - this.Width);
            }

            facingLeft    = (speedX < 0);
            this.Location = new Point(duckX, duckY);

            walkFrameCounter++;
            if (walkFrameCounter > 8)
            {
                walkFrameCounter = 0;
                isFootOpen       = !isFootOpen;
                currentImage     = isFootOpen ? footOpen : footClosed;
            }

            this.Invalidate();
        }

        // ── 绘制 ─────────────────────────────────────────────
        private const int DuckDrawY = 50;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (currentImage == null) return;

            var g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode   = PixelOffsetMode.Half;
            g.SmoothingMode     = SmoothingMode.None;
            g.CompositingMode   = CompositingMode.SourceOver;

            if (facingLeft)
                g.DrawImage(currentImage, 0, DuckDrawY, 150, 150);
            else
                g.DrawImage(currentImage,
                    new Rectangle(150, DuckDrawY, -150, 150),
                    0, 0, currentImage.Width, currentImage.Height,
                    GraphicsUnit.Pixel);

            DrawNameTag(g);
        }

        private void DrawNameTag(Graphics g)
        {
            if (string.IsNullOrEmpty(_duckName)) return;

            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var font = new Font("微软雅黑", 9f, FontStyle.Bold);
            SizeF sz = g.MeasureString(_duckName, font);
            int nw = (int)sz.Width  + 14;
            int nh = (int)sz.Height + 6;
            int nx = (this.Width - nw) / 2;
            int ny = DuckDrawY - nh - 2;

            using var bg = new SolidBrush(Color.FromArgb(180, 30, 30, 30));
            g.FillRoundedRect(bg, new Rectangle(nx, ny, nw, nh), 5);

            using var textBrush = new SolidBrush(Color.FromArgb(255, 100, 180, 255));
            g.DrawString(_duckName, font, textBrush, nx + 7, ny + 3);
        }

        // ── 鼠标事件 ─────────────────────────────────────────
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (isResting) return;
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartX = e.X;
                dragStartY = e.Y;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                int newX    = this.Location.X + e.X - dragStartX;
                int screenW = Screen.PrimaryScreen.Bounds.Width;
                duckX       = Math.Clamp(newX, 0, screenW - this.Width);
                this.Location = new Point(duckX, duckY);
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) isDragging = false;
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                string msg = DuckMessages.PickRandom(rnd);
                chatForm?.AddMessage(msg);
            }
        }

        public void SayToChat(string message)
        {
            chatForm?.AddMessage(message);
        }

        // ── 位置同步 ──────────────────────────────────────────
        public void SetDuckY(int y)
        {
            duckY = y;
            if (isResting) this.Location = new Point(duckX, duckY);
        }

        public void SetDuckX(int x)
        {
            duckX = x;
            if (isResting) this.Location = new Point(duckX, duckY);
        }
    }
}