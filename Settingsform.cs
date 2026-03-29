using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Producktivity
{
    public class SettingsForm : Form
    {
        // ── 依赖 ──────────────────────────────────────────────
        private readonly Form1          _duck;
        private readonly StickerManager _sm;
        private readonly StatsManager   _stats;

        // ── 布局常量（1.5 倍） ────────────────────────────────
        private const int WinW      = 1020;
        private const int WinH      = 870;
        private const int HeaderH   = 54;
        private const int TabBarH   = 44;
        private const int FooterH   = 56;
        private const int CellW     = 140;
        private const int CellH     = 168;
        private const int ThumbH    = 110;
        private const int ThinBarH  = 30;
        private const int CellPad   = 14;
        private const int GridLeft  = 20;

        // ── 通用按钮高度 ──────────────────────────────────────
        private const int BtnH = 46;

        // ── 星星尺寸（2 倍） ──────────────────────────────────
        private const int StarSize = 20;
        private const int StarGap  = 4;

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
        private int    _tab          = 0;
        private bool   _closeHovered = false;
        private Point  _formDragOffset;
        private bool   _formDragging = false;

        // ── Tab 0：名称 ──────────────────────────────────────
        private TextBox _nameBox;
        private Button  _saveNameBtn;

        // ── Tab 1：表情包 ────────────────────────────────────
        private Panel _gridPanel;
        private int   _gridScroll  = 0;
        private int   _gridCols    = 1;
        private int   _totalGridH  = 0;
        private int   _hoveredCell = -1;

        // ── Tab 2：控制 ──────────────────────────────────────
        private Panel   _controlPanel;
        private TextBox _lbBox;
        private TextBox _hbBox;
        private TextBox _kBox;
        private TextBox _blockAppBox;
        private ListBox _blockList;
        private Button  _addBlockBtn;
        private Button  _removeBlockBtn;
        private Button  _saveControlBtn;

        // ── Tab 3：数据 ──────────────────────────────────────
        private Panel _dataPanel;
        private System.Windows.Forms.Timer _dataRefreshTimer;

        // ── Tab 名称数组 ─────────────────────────────────────
        private readonly string[] _tabNames = { "鸭子名称", "表情包管理", "控制", "数据" };

        // ── 构造 ─────────────────────────────────────────────
        public SettingsForm(Form1 duck, StickerManager sm, StatsManager stats)
        {
            _duck  = duck;
            _sm    = sm;
            _stats = stats;
            InitWindow();
            BuildNameTab();
            BuildStickerGrid();
            BuildControlTab();
            BuildDataTab();
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

        // ══════════════════════════════════════════════════════
        //  Tab 0：鸭子名称
        // ══════════════════════════════════════════════════════
        private void BuildNameTab()
        {
            _nameBox = new TextBox
            {
                Font        = new Font("微软雅黑", 14f),
                Text        = _duck.DuckName,
                MaxLength   = 20,
                BorderStyle = BorderStyle.FixedSingle,
                Size        = new Size(360, 40),
                Location    = new Point((WinW - 360) / 2, HeaderH + TabBarH + 100),
                Visible     = false
            };

            _saveNameBtn = new Button
            {
                Text      = "保存",
                Font      = new Font("微软雅黑", 10f),
                Size      = new Size(100, BtnH),
                Location  = new Point((WinW - 100) / 2, _nameBox.Bottom + 24),
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

        // ══════════════════════════════════════════════════════
        //  Tab 1：表情包管理
        // ══════════════════════════════════════════════════════
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

        // ══════════════════════════════════════════════════════
        //  Tab 2：控制
        // ══════════════════════════════════════════════════════
        private void BuildControlTab()
        {
            int panelTop = HeaderH + TabBarH;
            int panelH   = WinH - panelTop - FooterH;

            _controlPanel = new Panel
            {
                Location   = new Point(0, panelTop),
                Size       = new Size(WinW, panelH),
                BackColor  = ColBg,
                Visible    = false,
                AutoScroll = true
            };

            int labelX  = 60;                   // 标签靠左
            int inputX  = WinW - 60 - 180;      // 输入框靠右 (WinW - 右边距 - 输入框宽)
            int inputW  = 180;
            int y       = 40;
            int rowH    = 58;

            // ── LB ──
            _controlPanel.Controls.Add(MakeLabel("随机消息间隔下限 (秒):", labelX, y));
            _lbBox = MakeInput(_duck.RandomLB.ToString(), inputX, y, inputW);
            _controlPanel.Controls.Add(_lbBox);
            y += rowH;

            // ── HB ──
            _controlPanel.Controls.Add(MakeLabel("随机消息间隔上限 (秒):", labelX, y));
            _hbBox = MakeInput(_duck.RandomHB.ToString(), inputX, y, inputW);
            _controlPanel.Controls.Add(_hbBox);
            y += rowH;

            // ── K ──
            _controlPanel.Controls.Add(MakeLabel("文字消息比例 K (表情包比例 = 1−K):", labelX, y));
            _kBox = MakeInput(_duck.TextRatioK.ToString("0.##"), inputX, y, inputW);
            _controlPanel.Controls.Add(_kBox);
            y += rowH;

            // ── 保存按钮 ──
            _saveControlBtn = new Button
            {
                Text      = "保存设置",
                Font      = new Font("微软雅黑", 10f),
                Size      = new Size(120, BtnH),
                Location  = new Point(inputX, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColAccent,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand
            };
            _saveControlBtn.FlatAppearance.BorderSize = 0;
            _saveControlBtn.Click += OnSaveControl;
            _controlPanel.Controls.Add(_saveControlBtn);
            y += rowH + 30;

            // ── 拦截应用列表标题 ──
            _controlPanel.Controls.Add(MakeLabel("拦截应用列表（进程名，不含 .exe）:", labelX, y));
            y += 36;   // 标题与列表之间留间距

            // ── 列表 ──
            _blockList = new ListBox
            {
                Font     = new Font("微软雅黑", 10.5f),
                Size     = new Size(400, 200),
                Location = new Point(labelX, y)
            };
            foreach (var app in AppBlocker.BlockedApps)
                _blockList.Items.Add(app);
            _controlPanel.Controls.Add(_blockList);

            // ── 右侧：输入框 + 添加/移除按钮 ──
            int rightX = labelX + 420;

            _blockAppBox = new TextBox
            {
                Font            = new Font("微软雅黑", 11f),
                Size            = new Size(200, 34),
                Location        = new Point(rightX, y),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "输入进程名"
            };
            _controlPanel.Controls.Add(_blockAppBox);

            _addBlockBtn = new Button
            {
                Text      = "添加",
                Font      = new Font("微软雅黑", 10f),
                Size      = new Size(90, BtnH),
                Location  = new Point(rightX, y + 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColAccent,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand
            };
            _addBlockBtn.FlatAppearance.BorderSize = 0;
            _addBlockBtn.Click += (s, _) =>
            {
                string name = _blockAppBox.Text.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(name)) return;
                if (name.EndsWith(".exe")) name = name[..^4];
                if (!AppBlocker.BlockedApps.Contains(name))
                {
                    AppBlocker.BlockedApps.Add(name);
                    AppBlocker.Save();
                    _blockList.Items.Add(name);
                }
                _blockAppBox.Text = "";
            };
            _controlPanel.Controls.Add(_addBlockBtn);

            _removeBlockBtn = new Button
            {
                Text      = "移除选中",
                Font      = new Font("微软雅黑", 10f),
                Size      = new Size(110, BtnH),
                Location  = new Point(rightX, y + 100),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColDisabled,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand
            };
            _removeBlockBtn.FlatAppearance.BorderSize = 0;
            _removeBlockBtn.Click += (s, _) =>
            {
                if (_blockList.SelectedIndex < 0) return;
                string sel = _blockList.SelectedItem.ToString();
                AppBlocker.BlockedApps.Remove(sel);
                AppBlocker.Save();
                _blockList.Items.Remove(sel);
            };
            _controlPanel.Controls.Add(_removeBlockBtn);

            this.Controls.Add(_controlPanel);
        }

        private void OnSaveControl(object sender, EventArgs e)
        {
            if (!int.TryParse(_lbBox.Text.Trim(), out int lb) || lb <= 0 || lb >= 10000)
            {
                ShowToast("设置失败，数值超出程序给定范围");
                return;
            }
            if (!int.TryParse(_hbBox.Text.Trim(), out int hb) || hb <= 0 || hb >= 10000)
            {
                ShowToast("设置失败，数值超出程序给定范围");
                return;
            }
            if (hb <= lb)
            {
                ShowToast("设置失败，上限必须大于下限");
                return;
            }
            if (!double.TryParse(_kBox.Text.Trim(), out double k) || k < 0 || k > 1)
            {
                ShowToast("设置失败，K 必须在 0~1 之间");
                return;
            }

            _duck.RandomLB   = lb;
            _duck.RandomHB   = hb;
            _duck.TextRatioK = k;
            _duck.RefreshRandomTimer();

            ShowToast("已保存，立即生效！");
        }

        private Label MakeLabel(string text, int x, int y) => new Label
        {
            Text      = text,
            Font      = new Font("微软雅黑", 10.5f),
            AutoSize  = true,
            Location  = new Point(x, y + 6),
            BackColor = ColBg
        };

        private TextBox MakeInput(string text, int x, int y, int w) => new TextBox
        {
            Text        = text,
            Font        = new Font("微软雅黑", 11f),
            Size        = new Size(w, 34),
            Location    = new Point(x, y),
            BorderStyle = BorderStyle.FixedSingle
        };

        // ══════════════════════════════════════════════════════
        //  Tab 3：数据
        // ══════════════════════════════════════════════════════
        private void BuildDataTab()
        {
            int panelTop = HeaderH + TabBarH;
            int panelH   = WinH - panelTop - FooterH;

            _dataPanel = new Panel
            {
                Location  = new Point(0, panelTop),
                Size      = new Size(WinW, panelH),
                BackColor = ColBg,
                Visible   = false
            };
            _dataPanel.Paint += OnDataPaint;

            _dataRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _dataRefreshTimer.Tick += (s, _) => { if (_tab == 3) _dataPanel.Invalidate(); };
            _dataRefreshTimer.Start();

            this.Controls.Add(_dataPanel);
        }

        private void OnDataPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(ColBg);

            using var titleFont = new Font("微软雅黑", 14f, FontStyle.Bold);
            using var valFont   = new Font("微软雅黑", 22f, FontStyle.Bold);
            using var lblFont   = new Font("微软雅黑", 10.5f);
            using var titleBr   = new SolidBrush(Color.FromArgb(40, 40, 50));
            using var valBr     = new SolidBrush(ColAccent);
            using var lblBr     = new SolidBrush(Color.FromArgb(110, 110, 120));

            g.DrawString("数据总览", titleFont, titleBr, 60, 30);

            int cardW = 200, cardH = 120, gap = 24;
            int startX = 60, startY = 80;

            var items = new (string label, string value)[]
            {
                ("上传表情包总数",   _sm.Entries.Count.ToString()),
                ("文字消息总数",     _stats.TotalTextMsgs.ToString()),
                ("表情包消息总数",   _stats.TotalStickerMsgs.ToString()),
                ("程序运行总时长",   _stats.GetFormattedTotalRuntime()),
            };

            for (int i = 0; i < items.Length; i++)
            {
                int cx = startX + i * (cardW + gap);
                int cy = startY;

                g.FillRoundedRect(new SolidBrush(Color.White),
                    new Rectangle(cx, cy, cardW, cardH), 10);
                g.DrawRoundedRect(new Pen(ColBorder, 0.8f),
                    new Rectangle(cx, cy, cardW, cardH), 10);

                var vsz = g.MeasureString(items[i].value, valFont);
                g.DrawString(items[i].value, valFont, valBr,
                    cx + (cardW - vsz.Width) / 2f, cy + 20);

                var lsz = g.MeasureString(items[i].label, lblFont);
                g.DrawString(items[i].label, lblFont, lblBr,
                    cx + (cardW - lsz.Width) / 2f, cy + cardH - 35);
            }
        }

        // ══════════════════════════════════════════════════════
        //  Tab 切换
        // ══════════════════════════════════════════════════════
        private void ShowTab(int t)
        {
            _tab = t;
            _nameBox.Visible       = (t == 0);
            _saveNameBtn.Visible   = (t == 0);
            _gridPanel.Visible     = (t == 1);
            _controlPanel.Visible  = (t == 2);
            _dataPanel.Visible     = (t == 3);
            if (t == 1) RefreshGrid();
            this.Invalidate();
        }

        // ══════════════════════════════════════════════════════
        //  主窗口绘制
        // ══════════════════════════════════════════════════════
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

            using var f = new Font("微软雅黑", 14f);
            var sz = g.MeasureString("设置", f);
            g.DrawString("设置", f, new SolidBrush(Color.FromArgb(30, 30, 35)),
                20, (HeaderH - sz.Height) / 2f);

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

            int tabW = 140;
            for (int i = 0; i < _tabNames.Length; i++)
            {
                int tx  = 20 + i * (tabW + 10);
                bool sel = (i == _tab);
                g.FillRoundedRect(new SolidBrush(sel ? ColTabSel : ColTab),
                    new Rectangle(tx, y + 6, tabW, TabBarH - 12), 6);

                using var f = new Font("微软雅黑", 10f, sel ? FontStyle.Bold : FontStyle.Regular);
                using var b = new SolidBrush(sel ? ColAccent : ColTabText);
                var sz = g.MeasureString(_tabNames[i], f);
                g.DrawString(_tabNames[i], f, b,
                    tx + (tabW - sz.Width) / 2f,
                    y + (TabBarH - sz.Height) / 2f);
            }
        }

        private void DrawNameTabHint(Graphics g)
        {
            using var f = new Font("微软雅黑", 10.5f);
            using var b = new SolidBrush(Color.FromArgb(130, 130, 140));
            const string hint = "名字将显示在鸭子上方，最多 20 个字符";
            var sz = g.MeasureString(hint, f);
            g.DrawString(hint, f, b, (WinW - sz.Width) / 2f, HeaderH + TabBarH + 40);
        }

        private void DrawFooter(Graphics g)
        {
            int y = WinH - FooterH;
            g.FillRectangle(new SolidBrush(ColHeader), 0, y, WinW, FooterH);
            g.DrawLine(new Pen(ColBorder), 0, y, WinW, y);

            if (_tab == 1)
            {
                g.FillRoundedRect(new SolidBrush(ColAccent),
                    new Rectangle(WinW - 260, y + 8, 130, BtnH), 6);
                using var f  = new Font("微软雅黑", 10f);
                var sz = g.MeasureString("导入表情包", f);
                g.DrawString("导入表情包", f, Brushes.White,
                    WinW - 260 + (130 - sz.Width) / 2f, y + 8 + (BtnH - sz.Height) / 2f);

                using var hint  = new Font("微软雅黑", 9f);
                using var hintB = new SolidBrush(Color.FromArgb(140, 140, 150));
                g.DrawString("支持 PNG / JPG / GIF　　单击 bar 切换启用　　双击预览",
                    hint, hintB, 20, y + 20);
            }
        }

        private void DrawBorder(Graphics g)
        {
            using var pen = new Pen(ColBorder, 1f);
            g.DrawRectangle(pen, 0, 0, WinW - 1, WinH - 1);
        }

        // ══════════════════════════════════════════════════════
        //  表情包网格绘制
        // ══════════════════════════════════════════════════════
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

            g.FillRoundedRect(
                new SolidBrush(entry.Enabled ? ColEnabled : ColDisabled),
                new Rectangle(x, y, CellW, ThinBarH), 6);

            DrawStars(g, entry.Stars, x + CellW / 2, y + ThinBarH / 2);

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

            using var nf = new Font("微软雅黑", 7.5f);
            string name = TruncateName(entry.FileName, nf, g, CellW - 8);
            g.DrawString(name, nf, new SolidBrush(Color.FromArgb(110, 110, 120)),
                x + 4, thumbY + ThumbH - 4);
        }

        private void DrawStars(Graphics g, int filled, int cx, int cy)
        {
            int totalW = 3 * StarSize + 2 * StarGap;
            int sx = cx - totalW / 2;
            for (int i = 0; i < 3; i++)
            {
                int px = sx + i * (StarSize + StarGap);
                DrawStar(g, px, cy - StarSize / 2, StarSize, i < filled ? ColStar : ColStarOff);
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

        private bool PointInStarArea(Point p, int idx)
        {
            var (cx, cy) = CellPos(idx);
            int barY   = cy - _gridScroll;
            int totalW = 3 * StarSize + 2 * StarGap;
            int sx     = cx + CellW / 2 - totalW / 2;
            int sy     = barY + ThinBarH / 2 - StarSize / 2;
            return new Rectangle(sx, sy, totalW, StarSize).Contains(p);
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

            // 星星区域：循环 0→1→2→3→0
            if (PointInStarArea(e.Location, idx))
            {
                var entry = _sm.Entries[idx];
                entry.Stars = (entry.Stars + 1) % 4;
                _sm.Save();
                _gridPanel.Invalidate();
                return;
            }

            // thin bar 其余区域：切换启用/禁用
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

            if (CloseBtnRect.Contains(e.Location))
            {
                _dataRefreshTimer?.Stop();
                this.Close();
                return;
            }

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
            get { const int s = 30; return new Rectangle(WinW - s - 14, (HeaderH - s) / 2, s, s); }
        }

        private Rectangle ImportBtnRect =>
            new Rectangle(WinW - 260, WinH - FooterH + 8, 130, BtnH);

        private int TabAtPoint(Point p)
        {
            if (p.Y < HeaderH || p.Y > HeaderH + TabBarH) return -1;
            int tabW = 140;
            for (int i = 0; i < _tabNames.Length; i++)
                if (new Rectangle(20 + i * (tabW + 10), HeaderH + 6, tabW, TabBarH - 12).Contains(p))
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
                Size            = new Size(260, 44),
                StartPosition   = FormStartPosition.Manual,
                Location        = new Point(this.Left + (WinW - 260) / 2, this.Top + WinH - 90)
            };
            t.Controls.Add(new Label
            {
                Text      = msg, ForeColor = Color.White,
                Font      = new Font("微软雅黑", 10f),
                AutoSize  = false, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });
            t.Show(this);
            var timer = new System.Windows.Forms.Timer { Interval = 1500 };
            timer.Tick += (s, _) => { timer.Stop(); t.Close(); };
            timer.Start();
        }
    }
}