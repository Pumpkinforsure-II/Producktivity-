using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Producktivity
{
    public class ChatForm : Form
    {
        // ── 布局常量 ──────────────────────────────────────────
        private const int HeaderH      = 48;
        private const int FooterH      = 32;
        private const int ResizeEdgeH  = 10;
        private const int AvatarSize   = 48;
        private const int BubblePadX   = 14;
        private const int BubblePadY   = 10;
        private const int MsgMarginL   = 68;
        private const int MsgSpacing   = 18;
        private const int ScrollBarW   = 6;
        private const int BubbleRadius = 12;
        private const int CloseBtnSize = 32;

        // ── 颜色 ─────────────────────────────────────────────
        private readonly Color ColBg         = Color.FromArgb(242, 242, 247);
        private readonly Color ColHeader     = Color.FromArgb(255, 255, 255);
        private readonly Color ColFooter     = Color.FromArgb(250, 250, 250);
        private readonly Color ColBorder     = Color.FromArgb(210, 210, 215);
        private readonly Color ColBubble     = Color.FromArgb(255, 255, 255);
        private readonly Color ColTimestamp  = Color.FromArgb(150, 150, 155);
        private readonly Color ColSession    = Color.FromArgb(52,  199,  89);
        private readonly Color ColCloseNorm  = Color.FromArgb(200, 200, 205);
        private readonly Color ColCloseHover = Color.FromArgb(215,  60,  60);
        private readonly Color ColSubtitle   = Color.FromArgb(130, 130, 140);

        // ── 状态 ─────────────────────────────────────────────
        private bool  isDraggingHeader = false;
        private bool  isDraggingResize = false;
        private Point dragOffset;
        private int   dragStartMouseY;
        private int   dragStartHeight;
        private bool  closeHovered = false;

        private Image  duckAvatar;
        private string sessionStatus = "空闲中";
        private string duckName      = "DUCK";

        private readonly List<ChatMessage> messages = new List<ChatMessage>();
        private int scrollOffset   = 0;
        private int totalMsgHeight = 0;

        // ── 构造 ─────────────────────────────────────────────
        public ChatForm()
        {
            InitWindow();
            LoadAvatar();
        }

        private void InitWindow()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost         = true;
            this.BackColor       = ColBg;
            this.Size            = new Size(800, 600);
            this.MinimumSize     = new Size(400, 250);
            this.StartPosition   = FormStartPosition.Manual;

            var wa = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(wa.Right - this.Width - 20,
                                      wa.Top + (wa.Height - this.Height) / 2);

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }

        private void LoadAvatar()
        {
            foreach (var path in new[] { "images/user_defined_icon.png",
                                          "images/default_icon.png" })
            {
                if (!File.Exists(path)) continue;
                try { duckAvatar = Image.FromFile(path); return; }
                catch { }
            }
        }

        // ── 公开方法 ──────────────────────────────────────────
        public void SetSessionStatus(string status)
        {
            sessionStatus = status;
            this.Invalidate();
        }

        public void UpdateDuckName(string name)
        {
            duckName = string.IsNullOrWhiteSpace(name) ? "DUCK" : name;
            this.Invalidate();
        }

        public void AddMessage(string text)
        {
            messages.Add(new ChatMessage { Text = text, Time = DateTime.Now });
            AfterAdd();
        }

        public void AddImageMessage(Image image)
        {
            messages.Add(new ChatMessage { IsImage = true, ImageData = image, Time = DateTime.Now });
            AfterAdd();
        }

        private void AfterAdd()
        {
            RecalcLayout();
            PinLastMessageToBottom();
            this.Invalidate();
        }

        // ── 区域尺寸 ──────────────────────────────────────────
        private int MsgAreaTop    => HeaderH;
        private int MsgAreaBottom => this.Height - FooterH;
        private int MsgAreaH      => MsgAreaBottom - MsgAreaTop;
        private int TextMaxW      => this.Width - MsgMarginL - BubblePadX * 2 - ScrollBarW - 24;

        // ── 布局计算 ──────────────────────────────────────────
        private void RecalcLayout()
        {
            using var g = this.CreateGraphics();
            int y = MsgSpacing;
            foreach (var msg in messages)
            {
                msg.CachedY = y;
                msg.CachedH = MeasureMessageH(g, msg);
                y += msg.CachedH + MsgSpacing;
            }
            totalMsgHeight = y;
        }

        private int MeasureMessageH(Graphics g, ChatMessage msg)
        {
            const int timeH = 18;
            if (msg.IsImage && msg.ImageData != null)
            {
                var ds = GetDisplaySize(msg.ImageData);
                return BubblePadY * 2 + ds.Height + timeH;
            }
            using var font = new Font("微软雅黑", 10.5f);
            var sz = g.MeasureString(msg.Text, font, TextMaxW);
            return BubblePadY * 2 + (int)Math.Ceiling(sz.Height) + timeH;
        }

        private static Size GetDisplaySize(Image img, int minPx = 80, int maxPx = 200)
        {
            float w = img.Width, h = img.Height;
            float longer  = Math.Max(w, h);
            float shorter = Math.Min(w, h);
            float scale   = 1f;

            if (longer > maxPx) scale = maxPx / longer;
            if (shorter * scale < minPx && shorter > 0)
                scale = Math.Max(scale, minPx / shorter);
            scale = Math.Min(scale, maxPx / longer);

            return new Size(Math.Max(1, (int)(w * scale)),
                            Math.Max(1, (int)(h * scale)));
        }

        private void PinLastMessageToBottom()
        {
            if (messages.Count == 0) { scrollOffset = 0; return; }
            var last      = messages[messages.Count - 1];
            int lastBottom = last.CachedY + last.CachedH + MsgSpacing;
            scrollOffset  = Math.Max(0, lastBottom - MsgAreaH);
        }

        // ── 绘制 ──────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            g.Clear(ColBg);
            PaintMessages(g);
            PaintScrollBar(g);
            PaintHeader(g);
            PaintFooter(g);
            PaintOuterBorder(g);
        }

        private void PaintHeader(Graphics g)
        {
            g.FillRectangle(new SolidBrush(ColHeader), 0, 0, this.Width, HeaderH);
            g.DrawLine(new Pen(ColBorder), 0, HeaderH, this.Width, HeaderH);

            // 日期
            using var dateFont = new Font("微软雅黑", 13f);
            g.DrawString(DateTime.Now.ToString("yyyy年M月d日"), dateFont,
                Brushes.Black, 16, (HeaderH - dateFont.Height) / 2f);

            PaintCloseBtn(g);
        }

        private void PaintCloseBtn(Graphics g)
        {
            var r   = CloseBtnRect;
            int pad = 9;

            if (closeHovered)
            {
                g.FillEllipse(new SolidBrush(ColCloseHover), r);
                using var pen = new Pen(Color.White, 2.5f)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(pen, r.X + pad, r.Y + pad, r.Right - pad, r.Bottom - pad);
                g.DrawLine(pen, r.Right - pad, r.Y + pad, r.X + pad, r.Bottom - pad);
            }
            else
            {
                using var pen = new Pen(ColCloseNorm, 2.5f)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(pen, r.X + pad, r.Y + pad, r.Right - pad, r.Bottom - pad);
                g.DrawLine(pen, r.Right - pad, r.Y + pad, r.X + pad, r.Bottom - pad);
            }
        }

        private void PaintFooter(Graphics g)
        {
            int fy = this.Height - FooterH;
            g.FillRectangle(new SolidBrush(ColFooter), 0, fy, this.Width, FooterH);
            g.DrawLine(new Pen(ColBorder), 0, fy, this.Width, fy);

            // 左下角：与 XXX 的 chat
            using var subFont  = new Font("微软雅黑", 9f);
            using var subBrush = new SolidBrush(ColSubtitle);
            g.DrawString($"与 {duckName} 的 chat", subFont, subBrush,
                12, fy + (FooterH - subFont.Height) / 2f);

            // 右下角：session 状态
            using var stFont  = new Font("微软雅黑", 9f);
            using var stBrush = new SolidBrush(ColSession);
            string label = "● " + sessionStatus;
            var    sz    = g.MeasureString(label, stFont);
            g.DrawString(label, stFont, stBrush,
                this.Width - sz.Width - 14,
                fy + (FooterH - sz.Height) / 2f);
        }

        private void PaintMessages(Graphics g)
        {
            g.SetClip(new Rectangle(0, MsgAreaTop, this.Width, MsgAreaH));

            if (messages.Count == 0)
            {
                using var f = new Font("微软雅黑", 10.5f);
                using var b = new SolidBrush(Color.FromArgb(170, 170, 175));
                const string hint = "还没有消息～";
                var sz = g.MeasureString(hint, f);
                g.DrawString(hint, f, b,
                    (this.Width - sz.Width) / 2f,
                    MsgAreaTop + (MsgAreaH - sz.Height) / 2f);
                g.ResetClip();
                return;
            }

            using var textFont  = new Font("微软雅黑", 10.5f);
            using var timeFont  = new Font("微软雅黑", 8.5f);
            using var textBrush = new SolidBrush(Color.FromArgb(30, 30, 35));
            using var timeBrush = new SolidBrush(ColTimestamp);

            int baseY = MsgAreaTop - scrollOffset;

            foreach (var msg in messages)
            {
                int absY = baseY + msg.CachedY;
                if (absY + msg.CachedH < MsgAreaTop) continue;
                if (absY > MsgAreaBottom)             break;

                PaintAvatar(g, 10, absY);

                if (msg.IsImage && msg.ImageData != null)
                {
                    var ds      = GetDisplaySize(msg.ImageData);
                    int bubbleW = ds.Width  + BubblePadX * 2;
                    int bubbleH = ds.Height + BubblePadY * 2;

                    PaintBubble(g, MsgMarginL, absY, bubbleW, bubbleH);
                    g.DrawImage(msg.ImageData,
                        MsgMarginL + BubblePadX, absY + BubblePadY,
                        ds.Width, ds.Height);
                    g.DrawString(msg.Time.ToString("HH:mm"), timeFont, timeBrush,
                        MsgMarginL, absY + bubbleH + 3);
                }
                else
                {
                    var tsz     = g.MeasureString(msg.Text, textFont, TextMaxW);
                    int bubbleW = Math.Max(72, (int)Math.Ceiling(tsz.Width) + BubblePadX * 2);
                    int textH   = (int)Math.Ceiling(tsz.Height);
                    int bubbleH = BubblePadY * 2 + textH;

                    PaintBubble(g, MsgMarginL, absY, bubbleW, bubbleH);
                    g.DrawString(msg.Text, textFont, textBrush,
                        new RectangleF(MsgMarginL + BubblePadX, absY + BubblePadY,
                            TextMaxW, textH + 4));
                    g.DrawString(msg.Time.ToString("HH:mm"), timeFont, timeBrush,
                        MsgMarginL, absY + bubbleH + 3);
                }
            }

            g.ResetClip();
        }

        private void PaintAvatar(Graphics g, int x, int y)
        {
            var rect = new Rectangle(x, y, AvatarSize, AvatarSize);
            using var clipPath = new GraphicsPath();
            clipPath.AddEllipse(rect);
            var saved = g.Save();
            g.SetClip(clipPath, CombineMode.Intersect);

            if (duckAvatar != null)
                g.DrawImage(duckAvatar, rect);
            else
            {
                g.FillEllipse(new SolidBrush(Color.FromArgb(255, 200, 50)), rect);
                using var f = new Font("微软雅黑", 20f, FontStyle.Bold);
                var sz = g.MeasureString("D", f);
                g.DrawString("D", f, Brushes.White,
                    x + (AvatarSize - sz.Width)  / 2f,
                    y + (AvatarSize - sz.Height) / 2f);
            }

            g.Restore(saved);
        }

        private void PaintBubble(Graphics g, int x, int y, int w, int h)
        {
            using var shadow = new SolidBrush(Color.FromArgb(15, 0, 0, 0));
            FillRounded(g, shadow, new Rectangle(x + 2, y + 2, w, h), BubbleRadius);
            using var fill = new SolidBrush(ColBubble);
            FillRounded(g, fill, new Rectangle(x, y, w, h), BubbleRadius);
            using var pen = new Pen(ColBorder, 0.8f);
            DrawRounded(g, pen, new Rectangle(x, y, w, h), BubbleRadius);
        }

        private void PaintScrollBar(Graphics g)
        {
            if (totalMsgHeight <= MsgAreaH) return;
            int trackH    = MsgAreaH - 8;
            int thumbH    = Math.Max(24, trackH * MsgAreaH / totalMsgHeight);
            int maxScroll = totalMsgHeight - MsgAreaH;
            int thumbY    = MsgAreaTop + 4 +
                (maxScroll > 0 ? scrollOffset * (trackH - thumbH) / maxScroll : 0);
            int barX      = this.Width - ScrollBarW - 4;
            using var brush = new SolidBrush(Color.FromArgb(170, 170, 175));
            FillRounded(g, brush, new Rectangle(barX, thumbY, ScrollBarW, thumbH), 3);
        }

        private void PaintOuterBorder(Graphics g)
        {
            using var pen = new Pen(ColBorder, 1f);
            g.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        }

        // ── 圆角矩形 ──────────────────────────────────────────
        private static void FillRounded(Graphics g, Brush br, Rectangle r, int rad)
        {
            using var path = RoundedPath(r, rad); g.FillPath(br, path);
        }
        private static void DrawRounded(Graphics g, Pen pen, Rectangle r, int rad)
        {
            using var path = RoundedPath(r, rad); g.DrawPath(pen, path);
        }
        private static GraphicsPath RoundedPath(Rectangle r, int rad)
        {
            int d = rad * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X,         r.Y,          d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            p.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            p.CloseFigure();
            return p;
        }

        // ── 交互区域 ──────────────────────────────────────────
        private Rectangle CloseBtnRect
        {
            get
            {
                int x = this.Width - CloseBtnSize - 12;
                int y = (HeaderH - CloseBtnSize) / 2;
                return new Rectangle(x, y, CloseBtnSize, CloseBtnSize);
            }
        }

        private bool InHeader   (Point p) => p.Y >= 0 && p.Y < HeaderH && !CloseBtnRect.Contains(p);
        private bool InResizeBar(Point p) => p.Y >= this.Height - FooterH;

        // ── 鼠标事件 ──────────────────────────────────────────
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (CloseBtnRect.Contains(e.Location)) { this.Hide(); return; }

            if (InResizeBar(e.Location))
            {
                isDraggingResize = true;
                dragStartMouseY  = Cursor.Position.Y;
                dragStartHeight  = this.Height;
                this.Cursor      = Cursors.SizeNS;
            }
            else if (InHeader(e.Location))
            {
                isDraggingHeader = true;
                dragOffset       = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDraggingHeader)
            {
                var screen = Screen.PrimaryScreen.Bounds;
                int nx = Math.Clamp(this.Left + e.X - dragOffset.X, 0, screen.Width  - this.Width);
                int ny = Math.Clamp(this.Top  + e.Y - dragOffset.Y, 0, screen.Height - this.Height);
                this.Location = new Point(nx, ny);
                return;
            }
            if (isDraggingResize)
            {
                this.Height = Math.Max(this.MinimumSize.Height,
                    dragStartHeight + Cursor.Position.Y - dragStartMouseY);
                RecalcLayout();
                PinLastMessageToBottom();
                this.Invalidate();
                return;
            }

            bool nowHovered = CloseBtnRect.Contains(e.Location);
            if (nowHovered != closeHovered) { closeHovered = nowHovered; this.Invalidate(); }

            if      (InResizeBar(e.Location)) this.Cursor = Cursors.SizeNS;
            else if (InHeader   (e.Location)) this.Cursor = Cursors.SizeAll;
            else                              this.Cursor = Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isDraggingHeader = false;
            isDraggingResize = false;
            this.Cursor      = Cursors.Default;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int maxScroll = Math.Max(0, totalMsgHeight - MsgAreaH);
            scrollOffset  = Math.Clamp(scrollOffset - e.Delta / 3, 0, maxScroll);
            this.Invalidate();
        }
    }

    // ── 消息数据模型 ──────────────────────────────────────────
    public class ChatMessage
    {
        public string   Text      { get; set; }
        public bool     IsImage   { get; set; }
        public Image    ImageData { get; set; }
        public DateTime Time      { get; set; }
        public int      CachedY   { get; set; }
        public int      CachedH   { get; set; }
    }
}