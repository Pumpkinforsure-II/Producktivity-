using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Producktivity
{
    /// <summary>
    /// 个性化设置窗口。
    /// Tab 0：鸭子名称　Tab 1：表情包管理
    /// </summary>
    public class PersonalizationForm : Form
    {
        // ── 依赖 ──────────────────────────────────────────────
        private readonly Form1          _duck;
        private readonly StickerManager _sm;

        // ── 布局常量 ──────────────────────────────────────────
        private const int WinW      = 680;
        private const int WinH      = 580;
        private const int HeaderH   = 48;
        private const int TabBarH   = 40;
        private const int FooterH   = 52;
        private const int CellW     = 120;
        private const int CellH     = 140;
        private const int ThumbH    = 100;
        private const int ThinBarH  = 22;
        private const int CellPad   = 12;
        private const int GridLeft  = 16;

        // ── 颜色 ─────────────────────────────────────────────
        private readonly Color ColBg       = Color.FromArgb(245, 245, 247);
        private readonly Color ColHeader   = Color.FromArgb(255, 255, 255);
        private readonly Color ColBorder   = Color.FromArgb(210, 210, 215);
        private readonly Color ColTab      = Color.FromArgb(255, 255, 255);
        private readonly Color ColTabSel   = Color.FromArgb(230, 240, 255);
        private readonly Color ColTabText  = Color.FromArgb( 80,  80,  90);
        private readonly Color ColAccent   = Color.FromArgb( 70, 130, 230);
        private readonly Color ColEnabled  = Color.FromArgb( 52, 199,  89);
        private readonly Color ColDisabled = Color.FromArgb(215,  60,  60);
        private readonly Color ColStar     = Color.FromArgb(255, 200,  40);
        private readonly Color ColStarOff  = Color.FromArgb(210, 210, 215);
        private readonly Color ColClose    = Color.FromArgb(180, 180, 185);
        private readonly Color ColCloseHov = Color.FromArgb(215,  60,  60);

        // ── 状态 ─────────────────────────────────────────────
        private int  _tab          = 0;
        private bool _closeHovered = false;

        private TextBox _nameBox;
        private Button  _saveNameBtn;

        private Panel _gridPanel;
        private int   _gridScroll  = 0;
        private int   _gridCols    = 1;
        private int   _totalGridH  = 0;
        private int   _hoveredCell = -1;

        private Point _formDragOffset;
        private bool  _formDragging = false;

        // ── 构造 ─────────────────────────────────────────────
        public PersonalizationForm(Form1 duck, StickerManager sm)
        {
            _duck = duck;
            _sm   = sm;
            InitWindow();
            BuildNameTab();
            BuildStickerGrid();
            ShowTab(0);
        }

        private void InitWindow()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost         = true;
            this.BackColor       = ColBg;
            this.Size            = new Size(WinW, WinH);
            this.StartPosition   = FormStartPosition.CenterScreen;

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            this.MouseMove  += (s, e) =>
            {
                bool h = CloseBtnRect.Contains(e.Location);
                if (h != _closeHovered) { _closeHovered = h; this.Invalidate(); }
            };
            this.MouseDown += OnFormMouseDown;
            this.MouseWheel += OnFormMouseWheel;
        }

        // ── Tab 0 ─────────────────────────────────────────────
        private void BuildNameTab()
        {
            _nameBox = new TextBox
            {
                Font        = new Font("微软雅黑", 14f),
                Text        = _duck.DuckName,
                MaxLength   = 20,
                BorderStyle = BorderStyle.FixedSingle,
                Size        = new Size(300, 40),
                Location    = new Point((WinW - 300) / 2, HeaderH + TabBarH + 80),
                Visible     = false
            };

            _saveNameBtn = new Button
            {
                Text      = "保存",
                Font      = new Font("微软雅黑", 11f),
                Size      = new Size(100, 36),
                Location  = new Point((WinW - 100) / 2, _nameBox.Bottom + 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColAccent,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand,
                Visible   = false
            };
            _saveNameBtn.FlatAppearance.BorderSize = 0;
            _saveNameBtn.Click += (s, e) =>
            {
                string name = _nameBox.Text.Trim();
                if (string.IsNullOrEmpty(name)) name = "DUCK";
                _duck.DuckName = name;
                ShowToast("已保存！");
            };

            this.Controls.Add(_nameBox);
            this.Controls.Add(_saveNameBtn);
        }

        // ── Tab 1 ─────────────────────────────────────────────
        private void BuildStickerGrid()
        {
            int gridTop = HeaderH + TabBarH;
            int gridH   = WinH - gridTop - FooterH;

            _gridPanel = new Panel
            {
                Location  = new Point(0, gridTop),
                Size      = new Size(WinW, gridH),
                BackColor = ColBg,
                Visible   = false
            };
            _gridPanel.Paint       += OnGridPaint;
            _gridPanel.MouseDown   += OnGridMouseDown;
            _gridPanel.MouseMove   += OnGridMouseMove;
            _gridPanel.MouseLeave  += (s, e) => { _hoveredCell = -1; _gridPanel.Invalidate(); };
            _gridPanel.MouseWheel  += OnGridMouseWheel;
            _gridPanel.DoubleClick += OnGridDoubleClick;

            this.Controls.Add(_gridPanel);
        }

        private void RefreshGrid()
        {
            _gridCols     = Math.Max(1, (_gridPanel.Width - GridLeft * 2 + CellPad) / (CellW + CellPad));
            int rows      = (int)Math.Ceiling(_sm.Entries.Count / (double)_gridCols);
            _totalGridH   = rows * (CellH + CellPad) + CellPad;
            int maxScroll = Math.Max(0, _totalGridH - _gridPanel.Height);
            _gridScroll   = Math.Clamp(_gridScroll, 0, maxScroll);
            _gridPanel.Invalidate();
        }

        private void ShowTab(int t)
        {
            _tab = t;
            _nameBox.Visible     = (t == 0);
            _saveNameBtn.Visible = (t == 0);
            _gridPanel.Visible   = (t == 1);
            if (t == 1) RefreshGrid();
            this.Invalidate();
        }

        // ── 绘制主窗口 ────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            g.Clear(ColBg);
            DrawHeader(g);
            DrawTabBar(g);
            if (_tab == 0) DrawNameTabHint(g);
            DrawFooter(g);
            DrawBorder(g);
        }

        private void DrawHeader(Graphics g)
        {
            g.FillRectangle(new SolidBrush(ColHeader), 0, 0, WinW, HeaderH);
            g.DrawLine(new Pen(ColBorder), 0, HeaderH, WinW, HeaderH);

            using var f = new Font("微软雅黑", 13f);
            var sz = g.MeasureString("个性化设置", f);
            g.DrawString("个性化设置", f, new SolidBrush(Color.FromArgb(30, 30, 35)),
                16, (HeaderH - sz.Height) / 2f);

            var r   = CloseBtnRect;
            int pad = 9;
            if (_closeHovered)
            {
                g.FillEllipse(new SolidBrush(ColCloseHov), r);
                using var pen = new Pen(Color.White, 2.5f)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(pen, r.X + pad, r.Y + pad, r.Right - pad, r.Bottom - pad);
                g.DrawLine(pen, r.Right - pad, r.Y + pad, r.X + pad, r.Bottom - pad);
            }
            else
            {
                using var pen = new Pen(ColClose, 2.5f)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(pen, r.X + pad, r.Y + pad, r.Right - pad, r.Bottom - pad);
                g.DrawLine(pen, r.Right - pad, r.Y + pad, r.X + pad, r.Bottom - pad);
            }
        }

        private void DrawTabBar(Graphics g)
        {
            int y = HeaderH;
            g.FillRectangle(new SolidBrush(ColTab), 0, y, WinW, TabBarH);
            g.DrawLine(new Pen(ColBorder), 0, y + TabBarH, WinW, y + TabBarH);

            string[] tabs = { "鸭子名称", "表情包管理" };
            int tabW = 140;
            for (int i = 0; i < tabs.Length; i++)
            {
                int tx  = 16 + i * (tabW + 8);
                bool sel = (i == _tab);
                g.FillRoundedRect(new SolidBrush(sel ? ColTabSel : ColTab),
                    new Rectangle(tx, y + 6, tabW, TabBarH - 12), 6);

                using var f = new Font("微软雅黑", 10f, sel ? FontStyle.Bold : FontStyle.Regular);
                using var b = new SolidBrush(sel ? ColAccent : ColTabText);
                var sz = g.MeasureString(tabs[i], f);
                g.DrawString(tabs[i], f, b,
                    tx + (tabW - sz.Width) / 2f,
                    y + (TabBarH - sz.Height) / 2f);
            }
        }

        private void DrawNameTabHint(Graphics g)
        {
            using var f = new Font("微软雅黑", 10f);
            using var b = new SolidBrush(Color.FromArgb(130, 130, 140));
            const string hint = "名字将显示在鸭子上方，最多 20 个字符";
            var sz = g.MeasureString(hint, f);
            g.DrawString(hint, f, b, (WinW - sz.Width) / 2f, HeaderH + TabBarH + 30);
        }

        private void DrawFooter(Graphics g)
        {
            int y = WinH - FooterH;
            g.FillRectangle(new SolidBrush(ColHeader), 0, y, WinW, FooterH);
            g.DrawLine(new Pen(ColBorder), 0, y, WinW, y);

            if (_tab == 1)
            {
                g.FillRoundedRect(new SolidBrush(ColAccent),
                    new Rectangle(WinW - 220, y + 10, 100, 32), 6);
                using var f  = new Font("微软雅黑", 10f);
                var sz = g.MeasureString("导入表情包", f);
                g.DrawString("导入表情包", f, Brushes.White,
                    WinW - 220 + (100 - sz.Width) / 2f, y + 10 + (32 - sz.Height) / 2f);

                using var hint  = new Font("微软雅黑", 9f);
                using var hintB = new SolidBrush(Color.FromArgb(140, 140, 150));
                g.DrawString("支持 PNG / JPG / GIF　　单击 bar 切换启用　　双击预览",
                    hint, hintB, 16, y + 18);
            }
        }

        private void DrawBorder(Graphics g)
        {
            using var pen = new Pen(ColBorder, 1f);
            g.DrawRectangle(pen, 0, 0, WinW - 1, WinH - 1);
        }

        // ── 表情包网格绘制 ────────────────────────────────────
        private void OnGridPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(ColBg);

            if (_sm.Entries.Count == 0)
            {
                using var f = new Font("微软雅黑", 11f);
                using var b = new SolidBrush(Color.FromArgb(170, 170, 175));
                const string hint = "还没有表情包，点击「导入表情包」添加";
                var sz = g.MeasureString(hint, f);
                g.DrawString(hint, f, b,
                    (_gridPanel.Width - sz.Width) / 2f,
                    (_gridPanel.Height - sz.Height) / 2f);
                return;
            }

            for (int i = 0; i < _sm.Entries.Count; i++)
            {
                var (cx, cy) = CellPos(i);
                cy -= _gridScroll;
                if (cy + CellH < 0 || cy > _gridPanel.Height) continue;
                DrawStickerCell(g, _sm.Entries[i], cx, cy, i == _hoveredCell);
            }
        }

        private void DrawStickerCell(Graphics g, StickerEntry entry,
            int x, int y, bool hovered)
        {
            var cellRect = new Rectangle(x, y, CellW, CellH);
            g.FillRoundedRect(
                new SolidBrush(hovered ? Color.FromArgb(235, 235, 240) : Color.White),
                cellRect, 8);
            g.DrawRoundedRect(new Pen(ColBorder, 0.8f), cellRect, 8);

            // Thin bar
            g.FillRoundedRect(
                new SolidBrush(entry.Enabled ? ColEnabled : ColDisabled),
                new Rectangle(x, y, CellW, ThinBarH), 6);

            DrawStars(g, entry.Stars, x + CellW / 2, y + ThinBarH / 2);

            // 缩略图
            int thumbY    = y + ThinBarH + 4;
            var thumbRect = new Rectangle(x + 4, thumbY, CellW - 8, ThumbH - 8);
            var img       = entry.GetImage();
            if (img != null)
            {
                float scale = Math.Min((float)thumbRect.Width  / img.Width,
                                       (float)thumbRect.Height / img.Height);
                int dw = (int)(img.Width  * scale);
                int dh = (int)(img.Height * scale);
                g.DrawImage(img,
                    thumbRect.X + (thumbRect.Width  - dw) / 2,
                    thumbRect.Y + (thumbRect.Height - dh) / 2,
                    dw, dh);
            }
            else
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(220, 220, 225)), thumbRect);
                using var f = new Font("微软雅黑", 9f);
                g.DrawString("无法加载", f, Brushes.Gray, thumbRect.X + 4, thumbRect.Y + 20);
            }

            // 文件名
            using var nf = new Font("微软雅黑", 7.5f);
            string name = TruncateName(entry.FileName, nf, g, CellW - 8);
            g.DrawString(name, nf, new SolidBrush(Color.FromArgb(110, 110, 120)),
                x + 4, thumbY + ThumbH - 4);
        }

        private void DrawStars(Graphics g, int filled, int cx, int cy)
        {
            const int ss  = 10;
            const int gap = 2;
            int sx = cx - (3 * ss + 2 * gap) / 2;
            for (int i = 0; i < 3; i++)
            {
                int px = sx + i * (ss + gap);
                DrawStar(g, px, cy - ss / 2, ss, i < filled ? ColStar : ColStarOff);
            }
        }

        private static void DrawStar(Graphics g, int x, int y, int size, Color col)
        {
            var pts  = new PointF[10];
            float cx = x + size / 2f, cy = y + size / 2f;
            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? size / 2f : size / 4.5f;
                double ang = i * Math.PI / 5 - Math.PI / 2;
                pts[i] = new PointF(cx + r * (float)Math.Cos(ang),
                                    cy + r * (float)Math.Sin(ang));
            }
            g.FillPolygon(new SolidBrush(col), pts);
        }

        private string TruncateName(string name, Font font, Graphics g, int maxW)
        {
            if (g.MeasureString(name, font).Width <= maxW) return name;
            while (name.Length > 1 && g.MeasureString(name + "…", font).Width > maxW)
                name = name[..^1];
            return name + "…";
        }

        // ── 格子坐标 ──────────────────────────────────────────
        private (int x, int y) CellPos(int i)
        {
            int col = i % _gridCols;
            int row = i / _gridCols;
            return (GridLeft + col * (CellW + CellPad),
                    CellPad  + row * (CellH + CellPad));
        }

        private int CellAtPoint(Point p)
        {
            int sy = p.Y + _gridScroll;
            for (int i = 0; i < _sm.Entries.Count; i++)
            {
                var (cx, cy) = CellPos(i);
                if (new Rectangle(cx, cy, CellW, CellH).Contains(new Point(p.X, sy)))
                    return i;
            }
            return -1;
        }

        private bool PointInThinBar(Point p, int idx)
        {
            var (cx, cy) = CellPos(idx);
            return new Rectangle(cx, cy - _gridScroll, CellW, ThinBarH).Contains(p);
        }

        private int StarAtPoint(Point p, int idx)
        {
            var (cx, cy) = CellPos(idx);
            int barY  = cy - _gridScroll;
            const int ss = 10, gap = 2;
            int sx = cx + CellW / 2 - (3 * ss + 2 * gap) / 2;
            int sy = barY + ThinBarH / 2 - ss / 2;
            for (int i = 0; i < 3; i++)
                if (new Rectangle(sx + i * (ss + gap), sy, ss, ss).Contains(p))
                    return i + 1;
            return -1;
        }

        // ── 表情包网格鼠标 ────────────────────────────────────
        private void OnGridMouseMove(object sender, MouseEventArgs e)
        {
            int idx = CellAtPoint(e.Location);
            if (idx != _hoveredCell) { _hoveredCell = idx; _gridPanel.Invalidate(); }
        }

        private void OnGridMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            int idx = CellAtPoint(e.Location);
            if (idx < 0) return;

            int star = StarAtPoint(e.Location, idx);
            if (star >= 1)
            {
                var entry  = _sm.Entries[idx];
                entry.Stars = (entry.Stars == star) ? star - 1 : star;
                _sm.Save();
                _gridPanel.Invalidate();
                return;
            }
            if (PointInThinBar(e.Location, idx))
            {
                _sm.Entries[idx].Enabled = !_sm.Entries[idx].Enabled;
                _sm.Save();
                _gridPanel.Invalidate();
            }
        }

        private void OnGridDoubleClick(object sender, EventArgs e)
        {
            var me = (MouseEventArgs)e;
            int idx = CellAtPoint(me.Location);
            if (idx < 0 || PointInThinBar(me.Location, idx)) return;
            ShowStickerPreview(_sm.Entries[idx]);
        }

        private void OnGridMouseWheel(object sender, MouseEventArgs e)
        {
            int maxScroll = Math.Max(0, _totalGridH - _gridPanel.Height);
            _gridScroll   = Math.Clamp(_gridScroll - e.Delta / 3, 0, maxScroll);
            _gridPanel.Invalidate();
        }

        private void ShowStickerPreview(StickerEntry entry)
        {
            var img = entry.GetImage();
            if (img == null) return;

            const int maxSide = 400;
            float scale = Math.Min((float)maxSide / img.Width, (float)maxSide / img.Height);
            var preview = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                TopMost         = true,
                BackColor       = Color.White,
                StartPosition   = FormStartPosition.CenterScreen,
                Size            = new Size((int)(img.Width * scale) + 2,
                                           (int)(img.Height * scale) + 2)
            };
            var pb = new PictureBox { Image = img, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
            preview.Controls.Add(pb);
            preview.Click += (s, _) => preview.Close();
            pb.Click      += (s, _) => preview.Close();
            preview.ShowDialog(this);
        }

        // ── 主窗口鼠标 ────────────────────────────────────────
        private void OnFormMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (CloseBtnRect.Contains(e.Location)) { this.Close(); return; }

            int tabIdx = TabAtPoint(e.Location);
            if (tabIdx >= 0) { ShowTab(tabIdx); return; }

            if (_tab == 1 && ImportBtnRect.Contains(e.Location)) { ImportSticker(); return; }

            if (e.Y < HeaderH) { _formDragging = true; _formDragOffset = e.Location; }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_formDragging)
                this.Location = new Point(this.Left + e.X - _formDragOffset.X,
                                          this.Top  + e.Y - _formDragOffset.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _formDragging = false;
        }

        private void OnFormMouseWheel(object sender, MouseEventArgs e)
        {
            if (_tab != 1) return;
            int maxScroll = Math.Max(0, _totalGridH - _gridPanel.Height);
            _gridScroll   = Math.Clamp(_gridScroll - e.Delta / 3, 0, maxScroll);
            _gridPanel.Invalidate();
        }

        // ── 区域辅助 ──────────────────────────────────────────
        private Rectangle CloseBtnRect
        {
            get { const int s = 28; return new Rectangle(WinW - s - 12, (HeaderH - s) / 2, s, s); }
        }

        private Rectangle ImportBtnRect =>
            new Rectangle(WinW - 220, WinH - FooterH + 10, 100, 32);

        private int TabAtPoint(Point p)
        {
            if (p.Y < HeaderH || p.Y > HeaderH + TabBarH) return -1;
            int tabW = 140;
            for (int i = 0; i < 2; i++)
                if (new Rectangle(16 + i * (tabW + 8), HeaderH + 6, tabW, TabBarH - 12).Contains(p))
                    return i;
            return -1;
        }

        private void ImportSticker()
        {
            using var dlg = new OpenFileDialog
            {
                Title       = "选择表情包",
                Filter      = "图片文件|*.png;*.jpg;*.jpeg;*.gif",
                Multiselect = true
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            foreach (var file in dlg.FileNames) _sm.Import(file);
            RefreshGrid();
        }

        private void ShowToast(string msg)
        {
            var t = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                TopMost         = true,
                BackColor       = Color.FromArgb(50, 50, 55),
                Opacity         = 0.92,
                Size            = new Size(160, 40),
                StartPosition   = FormStartPosition.Manual,
                Location        = new Point(this.Left + (WinW - 160) / 2, this.Top + WinH - 80)
            };
            t.Controls.Add(new Label
            {
                Text      = msg, ForeColor = Color.White,
                Font      = new Font("微软雅黑", 10f),
                AutoSize  = false, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });
            t.Show(this);
            var timer = new System.Windows.Forms.Timer { Interval = 1200 };
            timer.Tick += (s, _) => { timer.Stop(); t.Close(); };
            timer.Start();
        }
    }
}