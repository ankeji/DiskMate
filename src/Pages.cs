using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskMate
{
    // 欢迎页顶部图标
    internal class HeroLogo : Control
    {
        public HeroLogo()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
            Size = new Size(Theme.P(58), Theme.P(58));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            Logo.Draw(g, new RectangleF(0, 0, Width, Height), Theme.P(17));
        }
    }

    // 微信专清页顶部的圆角图标块
    internal class WipBadge : Control
    {
        public WipBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
            Size = new Size(Theme.P(76), Theme.P(76));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            Theme.FillRound(g, new Rectangle(0, 0, Width, Height), Theme.P(20), Theme.PrimarySoft);
            int pad = Theme.P(21);
            IconPainter.Draw(g, new RectangleF(pad, pad, Width - pad * 2, Height - pad * 2), "broom", Theme.Primary);
        }
    }

    public partial class MainForm
    {
        // ============================================================ clean page
        private void BuildCleanPage()
        {
            pageWelcome = new Panel(); pageWelcome.BackColor = Theme.Bg; pageWelcome.Dock = DockStyle.Fill;
            pageScanning = new Panel(); pageScanning.BackColor = Theme.Bg; pageScanning.Dock = DockStyle.Fill; pageScanning.Visible = false;
            pageResults = new Panel(); pageResults.BackColor = Theme.Bg; pageResults.Dock = DockStyle.Fill; pageResults.Visible = false;
            pageWorking = new Panel(); pageWorking.BackColor = Theme.Bg; pageWorking.Dock = DockStyle.Fill; pageWorking.Visible = false;

            pageClean.Controls.Add(pageResults);
            pageClean.Controls.Add(pageScanning);
            pageClean.Controls.Add(pageWelcome);
            pageClean.Controls.Add(pageWorking);

            BuildWelcome();
            BuildScanning();
            BuildWorking();
            BuildResults();
        }

        private void BuildWelcome()
        {
            var hero = new CardPanel();
            hero.Size = new Size(Theme.P(700), Theme.P(372));
            hero.Radius = 18;
            pageWelcome.Controls.Add(hero);
            pageWelcome.Resize += delegate
            {
                hero.Location = new Point(Math.Max(Theme.P(0), (pageWelcome.Width - hero.Width) / 2),
                    Math.Max(Theme.P(20), (pageWelcome.Height - hero.Height) / 2));
            };

            var logo = new HeroLogo();
            logo.Location = new Point((hero.Width - Theme.P(58)) / 2, Theme.P(34));
            hero.Controls.Add(logo);

            var title = new Label();
            title.AutoSize = false;
            title.Location = new Point(Theme.P(0), Theme.P(104));
            title.Size = new Size(hero.Width, Theme.P(36));
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Font = Theme.Hero;
            title.ForeColor = Theme.Text;
            Txt(title, "welcome.title");
            hero.Controls.Add(title);

            var desc = new Label();
            desc.AutoSize = false;
            desc.Location = new Point(Theme.P(40), Theme.P(146));
            desc.Size = new Size(hero.Width - Theme.P(80), Theme.P(46));
            desc.TextAlign = ContentAlignment.TopCenter;
            desc.ForeColor = Theme.TextSub;
            Txt(desc, "welcome.desc");
            hero.Controls.Add(desc);

            var feats = new Label();
            feats.AutoSize = false;
            feats.Location = new Point(Theme.P(0), Theme.P(202));
            feats.Size = new Size(hero.Width, Theme.P(24));
            feats.TextAlign = ContentAlignment.MiddleCenter;
            feats.Font = Theme.Small;
            feats.ForeColor = Theme.Primary;
            Txt(feats, "welcome.features");
            hero.Controls.Add(feats);

            btnStart = new FlatButton();
            btnStart.Kind = BtnKind.Primary;
            Txt(btnStart, "welcome.start");
            btnStart.Size = new Size(Theme.P(184), Theme.P(46));
            btnStart.Radius = 11;
            btnStart.Font = Theme.F(11f, FontStyle.Bold);
            btnStart.Location = new Point((hero.Width - Theme.P(184)) / 2, Theme.P(246));
            btnStart.Click += delegate { BeginScan(); };
            hero.Controls.Add(btnStart);

            var hint = new Label();
            hint.AutoSize = false;
            hint.Location = new Point(Theme.P(0), Theme.P(306));
            hint.Size = new Size(hero.Width, Theme.P(22));
            hint.TextAlign = ContentAlignment.MiddleCenter;
            hint.Font = Theme.Small;
            hint.ForeColor = Theme.TextFaint;
            Txt(hint, "welcome.hint");
            hero.Controls.Add(hint);
        }

        private void BuildScanning()
        {
            var card = new CardPanel();
            card.Size = new Size(Theme.P(620), Theme.P(292));
            pageScanning.Controls.Add(card);
            pageScanning.Resize += delegate
            {
                card.Location = new Point(Math.Max(Theme.P(0), (pageScanning.Width - card.Width) / 2),
                    Math.Max(Theme.P(20), (pageScanning.Height - card.Height) / 2));
            };

            spinner = new Spinner();
            spinner.Size = new Size(Theme.P(46), Theme.P(46));
            spinner.BackColor = Theme.Surface;
            spinner.Location = new Point((card.Width - Theme.P(46)) / 2, Theme.P(40));
            card.Controls.Add(spinner);

            lblScanTitle = new Label();
            lblScanTitle.AutoSize = false;
            lblScanTitle.Location = new Point(Theme.P(0), Theme.P(100));
            lblScanTitle.Size = new Size(card.Width, Theme.P(30));
            lblScanTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblScanTitle.Font = Theme.F(14f, FontStyle.Bold);
            lblScanTitle.ForeColor = Theme.Text;
            Txt(lblScanTitle, "scan.title");
            card.Controls.Add(lblScanTitle);

            lblScanStep = new Label();
            lblScanStep.AutoSize = false;
            lblScanStep.Location = new Point(Theme.P(30), Theme.P(136));
            lblScanStep.Size = new Size(card.Width - Theme.P(60), Theme.P(24));
            lblScanStep.TextAlign = ContentAlignment.MiddleCenter;
            lblScanStep.ForeColor = Theme.TextSub;
            Txt(lblScanStep, "scan.preparing");
            card.Controls.Add(lblScanStep);

            progressScan = new ThinProgress();
            progressScan.BackColor = Theme.Surface;
            progressScan.Location = new Point(Theme.P(70), Theme.P(178));
            progressScan.Size = new Size(card.Width - Theme.P(140), Theme.P(8));
            card.Controls.Add(progressScan);

            lblScanCount = new Label();
            lblScanCount.AutoSize = false;
            lblScanCount.Location = new Point(Theme.P(70), Theme.P(192));
            lblScanCount.Size = new Size(card.Width - Theme.P(140), Theme.P(20));
            lblScanCount.TextAlign = ContentAlignment.MiddleRight;
            lblScanCount.Font = Theme.Small;
            lblScanCount.ForeColor = Theme.TextFaint;
            card.Controls.Add(lblScanCount);

            btnCancelScan = new FlatButton();
            btnCancelScan.Kind = BtnKind.Secondary;
            Txt(btnCancelScan, "scan.cancel");
            btnCancelScan.Size = new Size(Theme.P(128), Theme.P(38));
            btnCancelScan.Location = new Point((card.Width - Theme.P(128)) / 2, Theme.P(226));
            btnCancelScan.Click += delegate { CancelScan(); };
            card.Controls.Add(btnCancelScan);
        }

        private void BuildWorking()
        {
            var card = new CardPanel();
            card.Size = new Size(Theme.P(620), Theme.P(244));
            pageWorking.Controls.Add(card);
            pageWorking.Resize += delegate
            {
                card.Location = new Point(Math.Max(Theme.P(0), (pageWorking.Width - card.Width) / 2),
                    Math.Max(Theme.P(20), (pageWorking.Height - card.Height) / 2));
            };

            var t = new Label();
            t.AutoSize = false;
            t.Location = new Point(Theme.P(0), Theme.P(54));
            t.Size = new Size(card.Width, Theme.P(30));
            t.TextAlign = ContentAlignment.MiddleCenter;
            t.Font = Theme.F(14f, FontStyle.Bold);
            t.ForeColor = Theme.Text;
            Txt(t, "work.title");
            card.Controls.Add(t);

            lblWorkStep = new Label();
            lblWorkStep.AutoSize = false;
            lblWorkStep.Location = new Point(Theme.P(40), Theme.P(92));
            lblWorkStep.Size = new Size(card.Width - Theme.P(80), Theme.P(24));
            lblWorkStep.TextAlign = ContentAlignment.MiddleCenter;
            lblWorkStep.ForeColor = Theme.TextSub;
            Txt(lblWorkStep, "work.preparing");
            card.Controls.Add(lblWorkStep);

            progressWork = new ThinProgress();
            progressWork.BackColor = Theme.Surface;
            progressWork.Location = new Point(Theme.P(70), Theme.P(136));
            progressWork.Size = new Size(card.Width - Theme.P(140), Theme.P(8));
            card.Controls.Add(progressWork);

            lblWorkCount = new Label();
            lblWorkCount.AutoSize = false;
            lblWorkCount.Location = new Point(Theme.P(70), Theme.P(150));
            lblWorkCount.Size = new Size(card.Width - Theme.P(140), Theme.P(20));
            lblWorkCount.TextAlign = ContentAlignment.MiddleRight;
            lblWorkCount.Font = Theme.Small;
            lblWorkCount.ForeColor = Theme.TextFaint;
            card.Controls.Add(lblWorkCount);

            var hint = new Label();
            hint.AutoSize = false;
            hint.Location = new Point(Theme.P(0), Theme.P(190));
            hint.Size = new Size(card.Width, Theme.P(22));
            hint.TextAlign = ContentAlignment.MiddleCenter;
            hint.Font = Theme.Small;
            hint.ForeColor = Theme.TextFaint;
            Txt(hint, "work.hint");
            card.Controls.Add(hint);
        }

        /// <summary>工具条：搜索框 + 选中统计在左，按钮贴右；空间不足时先省略统计文字，再收窄搜索框。</summary>
        private void LayoutToolbar(Panel bar)
        {
            if (bar == null || search == null || btnCleanNow == null) return;

            btnCleanNow.Location = new Point(bar.Width - btnCleanNow.Width, Theme.P(11));
            btnSelectNone.Location = new Point(btnCleanNow.Left - Theme.P(10) - btnSelectNone.Width, Theme.P(12));
            btnSelectLow.Location = new Point(btnSelectNone.Left - Theme.P(8) - btnSelectLow.Width, Theme.P(12));

            int btnLeft = btnSelectLow.Left;
            int maxSearch = Math.Max(150, btnLeft - 16);
            int searchW = Math.Min(260, maxSearch);
            int labelX = searchW + 16;
            int avail = btnLeft - 16 - labelX;

            // 空间不够时依次收窄搜索框，实在放不下才隐藏统计文字
            if (avail < 140)
            {
                foreach (int w in new[] { Theme.P(210), Theme.P(170) })
                {
                    int sw = Math.Min(w, maxSearch);
                    int a = btnLeft - 16 - (sw + 16);
                    if (a >= 140) { searchW = sw; labelX = sw + 16; avail = a; break; }
                }
            }
            if (avail >= 140)
            {
                lblSelected.Visible = true;
                lblSelected.Bounds = new Rectangle(labelX, Theme.P(18), avail, Theme.P(22));
            }
            else
            {
                lblSelected.Visible = false;
                searchW = maxSearch;
            }
            search.SetBounds(Theme.P(0), Theme.P(10), searchW, Theme.P(38));
        }

        private MetricCard NewCard(string title, Color accent)        {
            var c = new MetricCard();
            c.Title = title;
            c.Accent = accent;
            c.BackColor = Theme.Bg;
            c.SetClickable(true);
            return c;
        }

        private void BuildResults()
        {
            var stats = new Panel();
            stats.Height = Theme.P(116);
            stats.BackColor = Theme.Bg;
            pageResults.Controls.Add(stats);

            cardTotal = NewCard("", Theme.Primary); Txt(cardTotal, "stat.total");
            cardLow = NewCard("", Theme.Low); Txt(cardLow, "stat.low");
            cardMid = NewCard("", Theme.Mid); Txt(cardMid, "stat.mid");
            cardHigh = NewCard("", Theme.High); Txt(cardHigh, "stat.high");
            stats.Controls.Add(cardTotal);
            stats.Controls.Add(cardLow);
            stats.Controls.Add(cardMid);
            stats.Controls.Add(cardHigh);
            stats.Resize += delegate
            {
                MetricCard[] cards = { cardTotal, cardLow, cardMid, cardHigh };
                int gap = Theme.P(12);
                int w = Math.Max(Theme.P(150), (stats.Width - gap * 3) / 4);
                for (int i = 0; i < cards.Length; i++)
                    cards[i].Bounds = new Rectangle(i * (w + gap), Theme.P(6), w, Theme.P(96));
            };

            cardTotal.Click += delegate { SetFilter(-1); };
            cardLow.Click += delegate { SetFilter(filterRisk == 0 ? -1 : 0); };
            cardMid.Click += delegate { SetFilter(filterRisk == 1 ? -1 : 1); };
            cardHigh.Click += delegate { SetFilter(filterRisk == 2 ? -1 : 2); };

            var bar = new Panel();
            bar.Height = Theme.P(60);
            bar.BackColor = Theme.Bg;
            pageResults.Controls.Add(bar);

            search = new SearchBox();
            search.Location = new Point(Theme.P(0), Theme.P(10));
            search.Size = new Size(Theme.P(300), Theme.P(38));
            Txt(search, "search.ph");
            search.TextChanged2 += delegate { if (phase == 1) BindItems(); };
            bar.Controls.Add(search);

            lblSelected = new Label();
            lblSelected.AutoSize = false;
            lblSelected.Location = new Point(Theme.P(316), Theme.P(18));
            lblSelected.Size = new Size(Theme.P(300), Theme.P(22));
            lblSelected.Font = Theme.Bold;
            lblSelected.ForeColor = Theme.Text;
            Txt(lblSelected, "sel.none");
            bar.Controls.Add(lblSelected);

            btnSelectLow = new FlatButton();
            btnSelectLow.Kind = BtnKind.Secondary;
            Txt(btnSelectLow, "btn.selectLow");
            btnSelectLow.Size = new Size(Theme.P(108), Theme.P(36));
            btnSelectLow.Click += delegate { SelectOnlyLow(); };
            bar.Controls.Add(btnSelectLow);

            btnSelectNone = new FlatButton();
            btnSelectNone.Kind = BtnKind.Secondary;
            Txt(btnSelectNone, "btn.selectNone");
            btnSelectNone.Size = new Size(Theme.P(96), Theme.P(36));
            btnSelectNone.Click += delegate { SelectNone(); };
            bar.Controls.Add(btnSelectNone);

            btnCleanNow = new FlatButton();
            btnCleanNow.Kind = BtnKind.Primary;
            Txt(btnCleanNow, "btn.clean");
            btnCleanNow.Size = new Size(Theme.P(156), Theme.P(38));
            btnCleanNow.Enabled = false;
            btnCleanNow.Click += delegate { StartClean(); };
            bar.Controls.Add(btnCleanNow);

            bar.Resize += delegate { LayoutToolbar(bar); };

            var body = new Panel();
            body.BackColor = Theme.Bg;
            pageResults.Controls.Add(body);

            var listCard = new CardPanel();
            listCard.Padding = new Padding(Theme.P(12), Theme.P(2), Theme.P(8), Theme.P(10));
            body.Controls.Add(listCard);

            listHost = new Panel();
            listHost.Dock = DockStyle.Fill;
            listHost.BackColor = Theme.Surface;
            listCard.Controls.Add(listHost);

            lvItems = new ItemList();
            lvItems.SelectedIndexChanged += delegate { UpdateDetail(); };
            lvItems.CheckedChanged += delegate { UpdateSelection(); UpdateDetail(); };
            lvItems.ColumnClick += delegate(object s, ColumnClickEventArgs e)
            {
                if (lvItems.SortColumn == e.Column) lvItems.SortDesc = !lvItems.SortDesc;
                else { lvItems.SortColumn = e.Column; lvItems.SortDesc = e.Column == 2; }
                BindItems();
            };
            lvItems.DoubleClick += delegate
            {
                if (current == null) return;
                current.Checked = !current.Checked;
                lvItems.Invalidate();
                UpdateSelection();
                UpdateDetail();
            };
            listHost.Controls.Add(lvItems);
            lvItems.AttachHost(listHost);   // 原生滚动条移到宿主边界外由裁剪隐藏

            lblEmpty = new Label();
            lblEmpty.AutoSize = false;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.ForeColor = Theme.TextFaint;
            lblEmpty.BackColor = Theme.Surface;
            lblEmpty.Visible = false;
            Txt(lblEmpty, "empty.filter");
            listCard.Controls.Add(lblEmpty);
            listCard.Resize += delegate
            {
                lblEmpty.Bounds = new Rectangle(Theme.P(12), listCard.Height / 2 - Theme.P(30), Math.Max(Theme.P(50), listCard.Width - Theme.P(40)), Theme.P(40));
            };

            var detailHost = new Panel();
            detailHost.BackColor = Theme.Bg;
            detailHost.Padding = new Padding(Theme.P(12), Theme.P(0), Theme.P(0), Theme.P(0));
            body.Controls.Add(detailHost);

            detailCard = new CardPanel();
            detailCard.Dock = DockStyle.Fill;
            detailCard.Padding = new Padding(Theme.P(22));
            detailHost.Controls.Add(detailCard);
            BuildDetailPanel();

            body.Resize += delegate
            {
                int dw = Theme.P(356);
                if (body.Width < Theme.P(720)) dw = Theme.P(320);
                detailHost.Bounds = new Rectangle(Math.Max(Theme.P(0), body.Width - dw), Theme.P(0), dw, body.Height);
                int listW = Math.Max(Theme.P(200), body.Width - dw - Theme.P(4));
                listCard.Bounds = new Rectangle(Theme.P(0), Theme.P(0), listW, body.Height);

                // 窄窗口下先收起「类别」列，再压「项目」列，避免横向滚动条
                int catW = listW >= Theme.P(640) ? Theme.P(100) : 0;
                lvItems.Columns[1].Width = catW;
                lvItems.Columns[0].Width = Math.Max(Theme.P(170), listW - catW - Theme.P(104) - Theme.P(88) - Theme.P(36));
            };

            pageResults.Resize += delegate
            {
                int w = pageResults.Width;
                int h = pageResults.Height;
                stats.SetBounds(Theme.P(0), Theme.P(0), w, Theme.P(116));
                bar.SetBounds(Theme.P(0), Theme.P(116), w, Theme.P(60));
                body.SetBounds(Theme.P(0), Theme.P(176), w, Math.Max(Theme.P(160), h - Theme.P(176)));
            };
        }

        private Label SectionLabel(string text)
        {
            var l = new Label();
            l.Text = text;
            l.Font = Theme.Small;
            l.ForeColor = Theme.TextFaint;
            l.AutoSize = true;
            return l;
        }

        private void BuildDetailPanel()
        {
            detailScrollHost = new Panel();
            detailScrollHost.BackColor = Theme.Surface;
            detailCard.Controls.Add(detailScrollHost);

            detailScroll = new ScrollPanel();   // 原生滚动条被宿主裁掉，视觉由 FlatScrollbar 接管
            detailScroll.BackColor = Theme.Surface;
            detailScroll.AutoScroll = true;
            detailScrollHost.Controls.Add(detailScroll);
            detailScrollBar = new FlatScrollbar(detailScrollHost, new PanelSource((ScrollPanel)detailScroll));
            detailScrollHost.Resize += delegate
            {
                int sb = SystemInformation.VerticalScrollBarWidth;
                detailScroll.SetBounds(0, 0, detailScrollHost.ClientSize.Width + sb, detailScrollHost.ClientSize.Height);
            };

            var cap = SectionLabel(""); Txt(cap, "det.title");
            cap.Location = new Point(Theme.P(0), Theme.P(0));
            detailScroll.Controls.Add(cap);

            lblDetName = new Label();
            lblDetName.Location = new Point(Theme.P(0), Theme.P(20));
            lblDetName.Size = new Size(Theme.P(280), Theme.P(46));
            lblDetName.Font = Theme.H2;
            lblDetName.ForeColor = Theme.Text;
            Txt(lblDetName, "det.nothing");
            detailScroll.Controls.Add(lblDetName);

            badgeDetRisk = new Badge();
            badgeDetRisk.Location = new Point(Theme.P(0), Theme.P(72));
            badgeDetRisk.Size = new Size(Theme.P(74), Theme.P(24));
            badgeDetRisk.Visible = false;
            detailScroll.Controls.Add(badgeDetRisk);

            lblDetSize = new Label();
            lblDetSize.Location = new Point(Theme.P(82), Theme.P(68));
            lblDetSize.Size = new Size(Theme.P(198), Theme.P(30));
            lblDetSize.Font = Theme.Num;
            lblDetSize.ForeColor = Theme.Text;
            lblDetSize.Text = "";
            detailScroll.Controls.Add(lblDetSize);

            var h1 = SectionLabel(""); Txt(h1, "det.impact");
            h1.Location = new Point(Theme.P(0), Theme.P(112));
            detailScroll.Controls.Add(h1);

            lblDetImpact = new Label();
            lblDetImpact.Location = new Point(Theme.P(0), Theme.P(134));
            lblDetImpact.Size = new Size(Theme.P(280), Theme.P(76));
            lblDetImpact.ForeColor = Theme.TextSub;
            Txt(lblDetImpact, "det.hint");
            detailScroll.Controls.Add(lblDetImpact);

            var h2 = SectionLabel(""); Txt(h2, "det.advice");
            h2.Location = new Point(Theme.P(0), Theme.P(216));
            detailScroll.Controls.Add(h2);

            lblDetAdvice = new Label();
            lblDetAdvice.Location = new Point(Theme.P(0), Theme.P(238));
            lblDetAdvice.Size = new Size(Theme.P(280), Theme.P(56));
            lblDetAdvice.ForeColor = Theme.TextSub;
            lblDetAdvice.Text = "";
            detailScroll.Controls.Add(lblDetAdvice);

            var h3 = SectionLabel(""); Txt(h3, "det.path");
            h3.Location = new Point(Theme.P(0), Theme.P(298));
            detailScroll.Controls.Add(h3);

            lblDetPath = new Label();
            lblDetPath.Location = new Point(Theme.P(0), Theme.P(320));
            lblDetPath.Size = new Size(Theme.P(280), Theme.P(72));
            lblDetPath.Font = Theme.Small;
            lblDetPath.ForeColor = Theme.TextFaint;
            lblDetPath.Text = "";
            detailScroll.Controls.Add(lblDetPath);

            btnDetToggle = new FlatButton();
            btnDetToggle.Kind = BtnKind.Primary;
            Txt(btnDetToggle, "det.add");
            btnDetToggle.Size = new Size(Theme.P(280), Theme.P(40));
            btnDetToggle.Visible = false;
            btnDetToggle.Click += delegate
            {
                if (current == null) return;
                current.Checked = !current.Checked;
                lvItems.Invalidate();
                UpdateSelection();
                UpdateDetail();
            };
            detailCard.Controls.Add(btnDetToggle);

            btnOpenFolder = new FlatButton();
            btnOpenFolder.Kind = BtnKind.Secondary;
            Txt(btnOpenFolder, "det.open");
            btnOpenFolder.Icon = "folder";
            btnOpenFolder.Size = new Size(Theme.P(280), Theme.P(36));
            btnOpenFolder.Visible = false;
            btnOpenFolder.Click += delegate { OpenCurrentFolder(); };
            detailCard.Controls.Add(btnOpenFolder);

            detailCard.Resize += delegate { LayoutDetail(); };
        }

        /// <summary>详情面板：正文放进可滚动区域，按钮固定在卡片底部，窗口变矮也不会互相遮挡。</summary>
        private void LayoutDetail()
        {
            if (detailCard == null || detailScroll == null) return;
            int w = Math.Max(Theme.P(120), detailCard.Width - Theme.P(44));
            btnDetToggle.Width = w;
            btnOpenFolder.Width = w;
            btnDetToggle.Location = new Point(Theme.P(22), detailCard.Height - Theme.P(96));
            btnOpenFolder.Location = new Point(Theme.P(22), detailCard.Height - Theme.P(50));

            int bottom = detailCard.Height - Theme.P(12);
            if (btnDetToggle.Visible) bottom = btnDetToggle.Top - 14;
            int h = bottom - Theme.P(14);
            if (h < Theme.P(60)) h = Theme.P(60);
            detailScrollHost.SetBounds(Theme.P(22), Theme.P(14), w, h);
            int sb = SystemInformation.VerticalScrollBarWidth;
            detailScroll.SetBounds(0, 0, w + sb, h);

            int inner = Math.Max(80, w - 18);
            lblDetName.Width = inner;
            lblDetImpact.Width = inner;
            lblDetAdvice.Width = inner;
            lblDetPath.Width = inner;
            lblDetSize.Width = Math.Max(Theme.P(60), inner - Theme.P(82));
        }

        // ============================================================ big files
        private void BuildBigPage()
        {
            var bar = new Panel();
            bar.Height = Theme.P(92);
            bar.BackColor = Theme.Bg;
            pageBig.Controls.Add(bar);

            cmbDrive = new FlatSelect();
            cmbDrive.Size = new Size(Theme.P(150), Theme.P(36));
            cmbDrive.Location = new Point(Theme.P(0), Theme.P(11));
            FillBigDrives();     // 默认选中 C 盘
            bar.Controls.Add(cmbDrive);

            btnBigScan = new FlatButton();
            btnBigScan.Kind = BtnKind.Primary;
            Txt(btnBigScan, "big.btn");
            btnBigScan.Size = new Size(Theme.P(132), Theme.P(36));
            btnBigScan.Location = new Point(Theme.P(162), Theme.P(11));
            btnBigScan.Click += delegate { StartBigScan(); };
            bar.Controls.Add(btnBigScan);

            btnBigPause = new FlatButton();
            btnBigPause.Kind = BtnKind.Secondary;
            Txt(btnBigPause, "big.pause");
            btnBigPause.Size = new Size(Theme.P(96), Theme.P(36));
            btnBigPause.Location = new Point(Theme.P(302), Theme.P(11));
            btnBigPause.Enabled = false;
            btnBigPause.Click += delegate { ToggleBigPause(); };
            bar.Controls.Add(btnBigPause);

            cmbThreshold = new FlatSelect();
            cmbThreshold.Size = new Size(Theme.P(140), Theme.P(36));
            cmbThreshold.Location = new Point(Theme.P(406), Theme.P(11));
            FillThresholds();   // 内部会填选项并选中默认项（200 MB）
            bar.Controls.Add(cmbThreshold);

            lblBigHint = new Label();
            lblBigHint.AutoSize = false;
            lblBigHint.AutoEllipsis = true;   // 窗口变窄时不硬挤，末尾省略
            lblBigHint.Location = new Point(Theme.P(2), Theme.P(56));
            lblBigHint.Size = new Size(Theme.P(600), Theme.P(28));
            lblBigHint.Font = Theme.Small;
            lblBigHint.ForeColor = Theme.TextSub;
            Txt(lblBigHint, "big.idle");
            bar.Controls.Add(lblBigHint);

            btnBigDelete = new FlatButton();
            btnBigDelete.Kind = BtnKind.Danger;
            Txt(btnBigDelete, "big.delete");
            btnBigDelete.Icon = "trash";
            btnBigDelete.Enabled = false;
            btnBigDelete.Size = new Size(Theme.P(120), Theme.P(36));
            btnBigDelete.Location = new Point(Theme.P(660), Theme.P(11));
            btnBigDelete.Click += delegate { DeleteBigSelected(); };
            bar.Controls.Add(btnBigDelete);

            bar.Resize += delegate
            {
                // 「删除选中」贴右边，进度行占满整行
                btnBigDelete.Location = new Point(bar.Width - btnBigDelete.Width, Theme.P(11));
                lblBigHint.Width = Math.Max(Theme.P(160), bar.Width - Theme.P(8) - lblBigHint.Left);
            };

            var card = new CardPanel();
            card.Padding = new Padding(Theme.P(12), Theme.P(2), Theme.P(8), Theme.P(10));
            pageBig.Controls.Add(card);

            lvBig = new ListView();
            lvBig.Dock = DockStyle.Fill;
            lvBig.View = View.Details;
            lvBig.FullRowSelect = true;
            lvBig.BorderStyle = BorderStyle.None;
            lvBig.Font = Theme.Base;
            lvBig.BackColor = Theme.Surface;
            var bigRows = new ImageList();
            bigRows.ImageSize = new Size(1, Theme.P(28));   // 统一行高，别再用系统默认的紧凑行
            bigRows.ColorDepth = ColorDepth.Depth32Bit;
            lvBig.SmallImageList = bigRows;
            lvBig.Columns.Add(I18n.T("big.colsize"), Theme.P(110), HorizontalAlignment.Right);
            lvBig.Columns.Add(I18n.T("big.colpath"), Theme.P(560));
            lvBig.CheckBoxes = true;   // 勾选后可批量移到回收站
            lvBig.DoubleClick += delegate { RevealBigFile(); };
            lvBig.SelectedIndexChanged += delegate { UpdateBigButtons(); };
            lvBig.ItemCheck += delegate { BeginInvoke((MethodInvoker)UpdateBigButtons); };

            var bigHost = new Panel();
            bigHost.Dock = DockStyle.Fill;
            bigHost.BackColor = Theme.Surface;
            card.Controls.Add(bigHost);
            lvBig.Dock = DockStyle.None;
            bigHost.Controls.Add(lvBig);

            lblBigEmpty = new Label();
            lblBigEmpty.AutoSize = false;
            lblBigEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblBigEmpty.ForeColor = Theme.TextFaint;
            lblBigEmpty.BackColor = Theme.Surface;
            Txt(lblBigEmpty, "big.idle");
            bigHost.Controls.Add(lblBigEmpty);
            lblBigEmpty.BringToFront();

            int sbw = SystemInformation.VerticalScrollBarWidth;
            bigHost.Resize += delegate
            {
                lvBig.Bounds = new Rectangle(0, 0, bigHost.ClientSize.Width + sbw, bigHost.ClientSize.Height);
                lblBigEmpty.Bounds = new Rectangle(Theme.P(16), bigHost.ClientSize.Height / 2 - Theme.P(28),
                    Math.Max(Theme.P(80), bigHost.ClientSize.Width - Theme.P(32)), Theme.P(56));
            };
            lvBig.Bounds = new Rectangle(0, 0, bigHost.Width + sbw, bigHost.Height);
            lblBigEmpty.Bounds = new Rectangle(Theme.P(16), Theme.P(120), Math.Max(Theme.P(80), bigHost.Width - Theme.P(32)), Theme.P(56));
            new FlatScrollbar(bigHost, new ListViewSource(lvBig));
            ListViewFit.Attach(lvBig, 1, 20);   // 路径列吸收剩余宽度

            pageBig.Resize += delegate
            {
                bar.SetBounds(Theme.P(0), Theme.P(0), pageBig.Width, Theme.P(92));
                card.SetBounds(Theme.P(0), Theme.P(92), pageBig.Width, Math.Max(Theme.P(120), pageBig.Height - Theme.P(92)));
            };
        }

        private void StartBigScan()
        {
            if (busy)
            {
                ShowBanner(BannerKind.Info, I18n.T("big.busy"), 4);
                return;
            }
            if (!RequireLogin(FeatureGate.Big)) return;

            busy = true;
            bigRunning = true;
            bigPaused = false;
            scanCancel = false;
            bigThreshold = BigThresholds[Math.Max(0, Math.Min(cmbThreshold.SelectedIndex, BigThresholds.Length - 1))];
            bigScanned = 0;
            bigShown = 0;
            lock (bigGate) { bigHits.Clear(); }

            lvBig.BeginUpdate();
            lvBig.Items.Clear();
            lvBig.EndUpdate();
            if (lblBigEmpty != null) lblBigEmpty.Visible = false;

            btnBigScan.Enabled = false;
            Txt(btnBigScan, "big.scanning");
            btnBigScan.Invalidate();

            btnBigPause.Enabled = true;
            btnBigPause.Text = I18n.T("big.pause");
            btnBigPause.Invalidate();

            cmbDrive.Enabled = false;
            cmbThreshold.Enabled = false;
            lblBigHint.Text = I18n.F("big.progress", 0, 0);
            UpdateBigButtons();

            StartBigFlushTimer();

            // 扫描哪个盘由下拉框决定（默认 C 盘）
            string driveRoot = null;
            int di = cmbDrive == null ? -1 : cmbDrive.SelectedIndex;
            if (di >= 0 && di < bigDrives.Count) driveRoot = bigDrives[di].RootDirectory.FullName;
            if (string.IsNullOrEmpty(driveRoot)) driveRoot = "C:////";

            Task.Factory.StartNew(delegate
            {
                var stack = new Stack<string>();
                if (Directory.Exists(driveRoot)) stack.Push(driveRoot);

                while (stack.Count > 0)
                {
                    if (scanCancel) break;

                    // 暂停：原地等着，不退出，点「继续」接着扫
                    while (bigPaused && !scanCancel)
                    {
                        try { System.Threading.Thread.Sleep(120); } catch { }
                    }

                    string dir = stack.Pop();

                    try
                    {
                        int fileTick = 0;
                        foreach (string f in Directory.EnumerateFiles(dir))
                        {
                            // 大目录里也要能及时响应「暂停」和「停止」，每 64 个文件看一下标志位
                            if ((++fileTick & 63) == 0)
                            {
                                if (scanCancel) break;
                                while (bigPaused && !scanCancel)
                                {
                                    try { System.Threading.Thread.Sleep(120); } catch { }
                                }
                            }
                            try
                            {
                                var fi = new FileInfo(f);
                                System.Threading.Interlocked.Increment(ref bigScanned);
                                if (fi.Length > bigThreshold)
                                {
                                    // 命中就塞进共享队列，UI 定时器每 200ms 搬一批到列表
                                    lock (bigGate) bigHits.Add(new KeyValuePair<string, long>(f, fi.Length));
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }

                    try
                    {
                        foreach (string d in Directory.EnumerateDirectories(dir))
                        {
                            try
                            {
                                var dirInfo = new DirectoryInfo(d);
                                if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                                if (SkipBigDir(dirInfo.Name)) continue;
                                stack.Push(d);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                Ui(delegate { FinishBigScan(); });
            }, TaskCreationOptions.LongRunning);
        }

        private const int BigMaxRows = 2000;

        private void StartBigFlushTimer()
        {
            if (bigFlushTimer == null)
            {
                bigFlushTimer = new System.Windows.Forms.Timer();
                bigFlushTimer.Interval = 200;
                bigFlushTimer.Tick += delegate { FlushBigHits(); };
            }
            bigFlushTimer.Start();
        }

        private void StopBigFlushTimer()
        {
            if (bigFlushTimer != null) bigFlushTimer.Stop();
        }

        /// <summary>把后台扫到的结果搬到列表：按大小降序插到对应位置，边扫边出。</summary>
        private void FlushBigHits()
        {
            List<KeyValuePair<string, long>> fresh = null;
            lock (bigGate)
            {
                if (bigShown < bigHits.Count)
                {
                    fresh = bigHits.GetRange(bigShown, bigHits.Count - bigShown);
                    bigShown = bigHits.Count;
                }
            }
            if (fresh != null && fresh.Count > 0) InsertBigRows(fresh);

            if (bigRunning && lblBigHint != null)
            {
                long scanned = System.Threading.Interlocked.Read(ref bigScanned);
                lblBigHint.Text = bigPaused
                    ? I18n.F("big.paused", scanned, lvBig.Items.Count)
                    : I18n.F("big.progress", scanned, lvBig.Items.Count);
            }
        }

        private void InsertBigRows(List<KeyValuePair<string, long>> rows)
        {
            lvBig.BeginUpdate();
            try
            {
                foreach (KeyValuePair<string, long> kv in rows)
                {
                    if (lvBig.Items.Count >= BigMaxRows) break;
                    var it = new ListViewItem(new[] { Theme.Fmt(kv.Value), kv.Key });
                    it.Tag = kv.Value;
                    lvBig.Items.Insert(FindBigInsertIndex(kv.Value), it);
                }
            }
            finally { lvBig.EndUpdate(); }
        }

        /// <summary>列表始终按大小降序，二分找插入点</summary>
        private int FindBigInsertIndex(long size)
        {
            int lo = 0, hi = lvBig.Items.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                object tag = lvBig.Items[mid].Tag;
                long v = tag is long ? (long)tag : 0;
                if (v >= size) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        /// <summary>暂停 / 继续</summary>
        private void ToggleBigPause()
        {
            if (!bigRunning) return;
            bigPaused = !bigPaused;
            btnBigPause.Text = I18n.T(bigPaused ? "big.resume" : "big.pause");
            btnBigPause.Invalidate();

            long scanned = System.Threading.Interlocked.Read(ref bigScanned);
            lblBigHint.Text = bigPaused
                ? I18n.F("big.paused", scanned, lvBig.Items.Count)
                : I18n.F("big.progress", scanned, lvBig.Items.Count);
        }

        /// <summary>扫描结束（正常结束或取消）：收尾按钮、提示与空状态</summary>
        private void FinishBigScan()
        {
            StopBigFlushTimer();
            FlushBigHits();

            bigRunning = false;
            bigPaused = false;
            busy = false;
            scanCancel = false;

            btnBigScan.Enabled = true;
            Txt(btnBigScan, "big.btn");
            btnBigScan.Invalidate();

            btnBigPause.Enabled = false;
            btnBigPause.Text = I18n.T("big.pause");
            btnBigPause.Invalidate();

            cmbDrive.Enabled = true;
            cmbThreshold.Enabled = true;

            long scanned = System.Threading.Interlocked.Read(ref bigScanned);
            lblBigHint.Text = I18n.F("big.done", scanned, lvBig.Items.Count, Theme.Fmt(bigThreshold));
            Log("大文件扫描：命中 " + lvBig.Items.Count + " 个，检查 " + scanned + " 个文件");
            Session.PingActivity("bigscan");

            if (lblBigEmpty != null)
            {
                bool empty = lvBig.Items.Count == 0;
                lblBigEmpty.Visible = empty;
                if (empty) lblBigEmpty.Text = I18n.F("big.none", Theme.Fmt(bigThreshold));
            }
            if (lvBig.Items.Count > 0)
            {
                lvBig.Items[0].Selected = true;
                lvBig.Items[0].Focused = true;
                lvBig.EnsureVisible(0);
            }
            UpdateBigButtons();
        }

        // ---------------------------------------------------------- 自检/调试入口
        /// <summary>调试用：切到大文件页并按 100MB 阈值开扫</summary>
        public void TestBigScanStart()
        {
            ShowPage(1);
            cmbThreshold.SelectedIndex = 0;
            StartBigScan();
        }

        /// <summary>调试用：当前扫描状态一句话</summary>
        public string TestBigState()
        {
            return "rows=" + lvBig.Items.Count
                 + " scanned=" + System.Threading.Interlocked.Read(ref bigScanned)
                 + " running=" + bigRunning
                 + " paused=" + bigPaused
                 + " hint=" + (lblBigHint == null ? "" : lblBigHint.Text)
                 + " top=" + (lvBig.Items.Count > 0 ? lvBig.Items[0].SubItems[1].Text : "-")
                 + " | drive=" + cmbDrive.Bounds + " scan=" + btnBigScan.Bounds + " pause=" + btnBigPause.Bounds
                 + " thr=" + cmbThreshold.Bounds
                 + " driveText=[" + cmbDrive.SelectedText + "] thrText=[" + cmbThreshold.SelectedText + "]" + " hint=" + lblBigHint.Bounds + " list=" + lvBig.Bounds;
        }

        /// <summary>调试用：暂停/继续</summary>
        public void TestBigTogglePause()
        {
            ToggleBigPause();
        }

        /// <summary>调试用：取消扫描</summary>
        public void TestBigStop()
        {
            scanCancel = true;
        }

        /// <summary>调试用：是否还在扫</summary>
        public bool TestBigRunning()
        {
            return bigRunning;
        }

        /// <summary>阈值下拉：跟随语言切换重建，保留选中项</summary>
        private void FillThresholds()
        {
            if (cmbThreshold == null) return;
            int keep = cmbThreshold.SelectedIndex;
            cmbThreshold.SetItems(new[]
            {
                I18n.T("big.th100"),
                I18n.T("big.th200"),
                I18n.T("big.th500"),
                I18n.T("big.th1g")
            }, keep >= 0 ? keep : 1);
        }

        /// <summary>没勾选任何文件时「删除选中」不可点</summary>
        private void UpdateBigButtons()
        {
            if (btnBigDelete == null) return;
            bool del = lvBig != null && lvBig.CheckedItems.Count > 0;
            if (btnBigDelete.Enabled != del)
            {
                btnBigDelete.Enabled = del;
                btnBigDelete.Invalidate();
            }
        }

        /// <summary>盘符下拉：默认 C 盘；扫描时按选中盘定位根目录</summary>
        private void FillBigDrives()
        {
            if (cmbDrive == null) return;
            int keep = cmbDrive.SelectedIndex;
            bigDrives.Clear();
            var labels = new List<string>();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                try
                {
                    if (d.DriveType != DriveType.Fixed || !d.IsReady) continue;
                    bigDrives.Add(d);
                    labels.Add(d.Name.Substring(0, 2) + "  可用 " + Theme.Fmt(d.AvailableFreeSpace));
                }
                catch { }
            }

            int idx = 0;   // 默认目标就是 C 盘
            for (int i = 0; i < bigDrives.Count; i++)
            {
                if (bigDrives[i].Name.StartsWith("C:", StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
            }
            cmbDrive.SetItems(labels, keep >= 0 ? keep : idx);
        }

        /// <summary>整盘扫描时跳过这些目录：系统还原点/回收站/组件库，扫进去只有噪音</summary>
        private static bool SkipBigDir(string name)
        {
            return name.Equals("WinSxS", StringComparison.OrdinalIgnoreCase)
                || name.Equals("$Recycle.Bin", StringComparison.OrdinalIgnoreCase)
                || name.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Recovery", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Config.Msi", StringComparison.OrdinalIgnoreCase)
                || name.Equals("CSC", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>删除勾选的大文件：走回收站，误删还能还原</summary>
        private void DeleteBigSelected()
        {
            if (lvBig == null) return;
            var paths = new List<string>();
            long total = 0;
            foreach (ListViewItem it in lvBig.CheckedItems)
            {
                string p = it.SubItems[1].Text;
                try
                {
                    var fi = new FileInfo(p);
                    if (!fi.Exists) continue;
                    paths.Add(p);
                    total += fi.Length;
                }
                catch { }
            }
            if (paths.Count == 0)
            {
                ShowBanner(BannerKind.Warn, I18n.T("big.delNone"), 6);
                return;
            }
            if (!MessageDialog.Ask(this, I18n.T("big.delTitle"),
                    I18n.F("big.delAsk", paths.Count, Theme.Fmt(total)), I18n.T("big.delOk"), true))
                return;

            var log = new StringBuilder();
            Task.Factory.StartNew(delegate
            {
                int gone = Engine.RecycleFiles(paths, log);
                Ui(delegate
                {
                    if (log.Length > 0) Log(log.ToString().TrimEnd());
                    Log(string.Format("大文件删除：{0}/{1} 个", gone, paths.Count));
                    var remove = new List<ListViewItem>();
                    foreach (ListViewItem it in lvBig.Items) if (it.Checked) remove.Add(it);
                    foreach (ListViewItem it in remove) lvBig.Items.Remove(it);
                    lblBigHint.Text = I18n.F("big.delDone", gone, Theme.Fmt(total));
                    if (lvBig.Items.Count == 0 && lblBigEmpty != null)
                    {
                        lblBigEmpty.Visible = true;
                        lblBigEmpty.Text = I18n.T("big.noneAfterDel");
                    }
                    UpdateBigButtons();
                    ShowBanner(BannerKind.Success, I18n.F("big.delDone", gone, Theme.Fmt(total)), 8);
                });
            }, TaskCreationOptions.LongRunning);
        }

        private void RevealBigFile()
        {
            if (lvBig.SelectedItems.Count == 0) return;
            string path = lvBig.SelectedItems[0].SubItems[1].Text;
            if (ShellReveal(path)) return;
            ShowBanner(BannerKind.Warn, I18n.T("big.revealFail"), 6);
        }

        /// <summary>
        /// 在资源管理器里定位文件。文件还在就先 /select 选中它；
        /// 文件已经被删掉（或 /select 被系统拒绝），就退一步打开它所在的目录。
        /// </summary>
        private bool ShellReveal(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                if (File.Exists(path))
                {
                    var p = Process.Start("explorer.exe", "/select,\"" + path + "\"");
                    if (p != null) return true;
                }
            }
            catch { }

            try
            {
                string dir = Path.GetDirectoryName(path);
                while (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    dir = Path.GetDirectoryName(dir);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", "\"" + dir + "\"");
                    return true;
                }
            }
            catch { }

            return false;
        }

        // ============================================================ log page
        private void BuildLogPage()
        {
            var bar = new Panel();
            bar.Height = Theme.P(52);
            bar.BackColor = Theme.Bg;
            pageLog.Controls.Add(bar);

            var b1 = new FlatButton();
            b1.Kind = BtnKind.Secondary;
            Txt(b1, "log.copy");
            b1.Size = new Size(Theme.P(104), Theme.P(36));
            b1.Location = new Point(Theme.P(0), Theme.P(4));
            b1.Click += delegate
            {
                try { Clipboard.SetText(txtLog.Text); ShowBanner(BannerKind.Success, I18n.T("log.copied"), 4); }
                catch { }
            };
            bar.Controls.Add(b1);

            var b2 = new FlatButton();
            b2.Kind = BtnKind.Secondary;
            Txt(b2, "log.open");
            b2.Icon = "folder";
            b2.Size = new Size(Theme.P(140), Theme.P(36));
            b2.Location = new Point(Theme.P(114), Theme.P(4));
            b2.Click += delegate
            {
                try { Process.Start("explorer.exe", "\"" + logDir + "\""); } catch { }
            };
            bar.Controls.Add(b2);

            var b3 = new FlatButton();
            b3.Kind = BtnKind.Secondary;
            Txt(b3, "log.clear");
            b3.Size = new Size(Theme.P(104), Theme.P(36));
            b3.Location = new Point(Theme.P(264), Theme.P(4));
            b3.Click += delegate { txtLog.Clear(); };
            bar.Controls.Add(b3);

            var card = new CardPanel();
            card.Padding = new Padding(Theme.P(1));
            card.FillColor = Color.FromArgb(26, 29, 34);
            card.BorderColor = Color.FromArgb(40, 44, 52);
            pageLog.Controls.Add(card);

            txtLog = new RichTextBox();
            txtLog.Dock = DockStyle.Fill;
            txtLog.ReadOnly = true;
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.BackColor = Color.FromArgb(26, 29, 34);
            txtLog.ForeColor = Color.FromArgb(214, 219, 226);
            txtLog.Font = Theme.Mono;
            txtLog.WordWrap = false;
            card.Controls.Add(txtLog);

            pageLog.Resize += delegate
            {
                bar.SetBounds(Theme.P(0), Theme.P(0), pageLog.Width, Theme.P(52));
                card.SetBounds(Theme.P(0), Theme.P(52), pageLog.Width, Math.Max(Theme.P(120), pageLog.Height - Theme.P(52)));
            };
        }

        // ============================================================ wechat page
        /// <summary>微信专清独立页面：功能未上线，先放“开发中…”占位 + 催更入口（复用问题反馈二维码弹窗）。</summary>
        private void BuildWeChatPage()
        {
            pageWechat.BackColor = Theme.Bg;

            var card = new CardPanel();
            card.FillColor = Theme.Surface;
            card.BorderColor = Theme.Border;
            pageWechat.Controls.Add(card);

            var badge = new WipBadge();
            card.Controls.Add(badge);

            var lblDev = new Label();
            lblDev.AutoSize = false;
            lblDev.TextAlign = ContentAlignment.MiddleCenter;
            lblDev.Font = Theme.F(19f, FontStyle.Bold);
            lblDev.ForeColor = Theme.Text;
            lblDev.Text = I18n.T("wechat.wip");
            card.Controls.Add(lblDev);

            var lblDesc = new Label();
            lblDesc.AutoSize = false;
            lblDesc.TextAlign = ContentAlignment.MiddleCenter;
            lblDesc.Font = Theme.Base;
            lblDesc.ForeColor = Theme.TextSub;
            lblDesc.Text = I18n.T("wechat.wip.sub");
            card.Controls.Add(lblDesc);

            var btnNudge = new FlatButton();
            btnNudge.Kind = BtnKind.Primary;
            btnNudge.Font = Theme.Base;
            btnNudge.Text = I18n.T("wechat.nudge");
            btnNudge.Click += delegate { ContactDialog.Show(this); };
            card.Controls.Add(btnNudge);

            Action layout = delegate
            {
                int w = pageWechat.Width, h = pageWechat.Height;
                card.SetBounds(Theme.P(0), Theme.P(0), w, h);
                int cw = card.Width;
                int bh = badge.Height;
                int total = bh + Theme.P(18) + Theme.P(34) + Theme.P(8) + Theme.P(44) + Theme.P(20) + Theme.P(40);
                int top = Math.Max(Theme.P(24), (card.Height - total) / 2);

                badge.Location = new Point((cw - badge.Width) / 2, top);
                lblDev.SetBounds(Theme.P(0), badge.Bottom + Theme.P(18), cw, Theme.P(34));
                lblDesc.SetBounds(Theme.P(0), lblDev.Bottom + Theme.P(8), cw, Theme.P(44));
                btnNudge.Size = new Size(Theme.P(132), Theme.P(40));
                btnNudge.Location = new Point((cw - btnNudge.Width) / 2, lblDesc.Bottom + Theme.P(20));
            };

            pageWechat.Resize += delegate { layout(); };
            layout();
        }

        /// <summary>自检用：切到微信专清页（占位页）</summary>
        public void TestShowWeChat()
        {
            ShowPage(3);
            Application.DoEvents();
        }

        // ============================================================ scan steps
        private List<ScanStep> BuildScanSteps()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            var steps = new List<ScanStep>();

            steps.Add(new ScanStep(I18n.T("step.temp-user"), delegate(List<CleanItem> l)
            {
                AddDir(l, "temp-user", "用户临时文件", "临时", new[] { Path.Combine(local, "Temp") }, "程序运行时产生的临时文件，可随时删除");
            }));
            steps.Add(new ScanStep(I18n.T("step.temp-win"), delegate(List<CleanItem> l)
            {
                AddDir(l, "temp-win", "系统临时文件", "临时", new[] { Path.Combine(win, "Temp") }, "Windows 临时目录，重启后会自动重建");
            }));
            steps.Add(new ScanStep(I18n.T("step.pkg"), delegate(List<CleanItem> l)
            {
                AddDir(l, "npm", "npm 缓存", "开发", new[] { Path.Combine(roaming, "npm-cache"), Path.Combine(local, "npm-cache") }, "重新安装依赖时会自动重建");
                AddDir(l, "yarn", "Yarn 缓存", "开发", new[] { Path.Combine(local, "Yarn", "Cache") }, "重新安装依赖时会自动重建");
                AddDir(l, "pnpm", "pnpm 缓存", "开发", new[] { Path.Combine(local, "pnpm-store"), Path.Combine(local, "pnpm", "store") }, "重新安装依赖时会自动重建");
            }));
            steps.Add(new ScanStep("Python / .NET / Java 缓存", delegate(List<CleanItem> l)
            {
                AddDir(l, "pip", "pip 缓存", "开发", new[] { Path.Combine(local, "pip", "Cache") }, "Python 包缓存");
                AddDir(l, "nuget", "NuGet 包缓存", "开发", new[] { Path.Combine(profile, ".nuget", "packages") }, ".NET 包缓存");
                AddDir(l, "gradle", "Gradle 缓存", "开发", new[] { Path.Combine(profile, ".gradle", "caches") }, "Java 构建缓存");
                AddDir(l, "npminstall", "npm tarball 缓存", "开发", new[] { Path.Combine(profile, ".npminstall_tarball") }, "脚手架下载的包压缩包");
            }));
            steps.Add(new ScanStep(I18n.T("step.crash"), delegate(List<CleanItem> l)
            {
                AddDir(l, "crash", "崩溃转储", "临时", new[]
                {
                    Path.Combine(local, "CrashDumps"),
                    Path.Combine(common, "Microsoft", "Windows", "WER", "ReportQueue"),
                    Path.Combine(common, "Microsoft", "Windows", "WER", "ReportArchive")
                }, "程序崩溃时生成的转储文件");
            }));
            steps.Add(new ScanStep(I18n.T("step.updaters"), delegate(List<CleanItem> l)
            {
                var updaters = new List<string>();
                try
                {
                    foreach (string d in Directory.EnumerateDirectories(local))
                    {
                        string n = Path.GetFileName(d).ToLowerInvariant();
                        if (n.IndexOf("updater") >= 0 || n.IndexOf("update") >= 0) updaters.Add(d);
                    }
                }
                catch { }
                updaters.Add(Path.Combine(local, "Ollama", "updates"));
                updaters.Add(Path.Combine(local, "electron", "Cache"));
                AddDir(l, "updaters", "更新器残留安装包", "应用", updaters.ToArray(), "各类应用下载的安装包，装完就没用了");
            }));
            steps.Add(new ScanStep("Chrome 缓存", delegate(List<CleanItem> l)
            {
                AddDir(l, "chrome", "Chrome 缓存", "浏览器", CollectBrowserCache(Path.Combine(local, "Google", "Chrome", "User Data")), "需先完全退出 Chrome，否则会被占用");
            }));
            steps.Add(new ScanStep("Edge 缓存", delegate(List<CleanItem> l)
            {
                AddDir(l, "edge", "Edge 缓存", "浏览器", CollectBrowserCache(Path.Combine(local, "Microsoft", "Edge", "User Data")), "需先完全退出 Edge");
            }));
            steps.Add(new ScanStep(I18n.T("step.gfx"), delegate(List<CleanItem> l)
            {
                AddDir(l, "thumb", "缩略图 / 图标缓存", "临时", new[] { Path.Combine(local, "Microsoft", "Windows", "Explorer") }, "资源管理器会按需重建");
                AddDir(l, "d3d", "DirectX 着色器缓存", "临时", new[] { Path.Combine(local, "D3DSCache") }, "游戏与图形程序会按需重建");
            }));
            steps.Add(new ScanStep(I18n.T("step.sys"), delegate(List<CleanItem> l)
            {
                AddDir(l, "prefetch", "预读取文件（Prefetch）", "系统", new[] { Path.Combine(win, "Prefetch") }, "首次启动程序会稍慢，之后自动恢复");
                AddDir(l, "fontcache", "字体缓存", "系统", new[] { Path.Combine(local, "FontCache") }, "系统会按需重新生成");
            }));
            steps.Add(new ScanStep(I18n.T("step.recycle"), delegate(List<CleanItem> l)
            {
                Engine.RecycleStat rs = null;
                try { rs = Engine.RecycleBinReport(); }
                catch { }
                if (rs == null || rs.Total <= 0) return;   // 只统计资源管理器可见的条目，无索引残留不计入
                l.Add(new CleanItem
                {
                    Key = "recycle",
                    CatKey = "sys",
                    Name = "回收站",
                    Category = "系统",
                    Size = rs.Total,
                    Note = "清空后无法恢复",
                    Kind = "recycle"
                });
            }));
            steps.Add(new ScanStep(I18n.T("step.hiber"), delegate(List<CleanItem> l)
            {
                try
                {
                    string hf = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "hiberfil.sys");
                    if (File.Exists(hf))
                    {
                        l.Add(new CleanItem
                        {
                            Key = "hiber",
                            CatKey = "sys",
                            Name = "休眠文件 hiberfil.sys",
                            Category = "系统",
                            Size = new FileInfo(hf).Length,
                            Note = "关闭后可释放，副作用是快速启动失效",
                            Kind = "hiber"
                        });
                    }
                }
                catch { }
            }));
            steps.Add(new ScanStep(I18n.T("step.drivers"), delegate(List<CleanItem> l)
            {
                DriverPlan plan = BuildDriverPlan();
                if (plan.Remove.Count > 0)
                {
                    l.Add(new CleanItem
                    {
                        Key = "drivers",
                        CatKey = "sys",
                        Count = plan.Remove.Count,
                        Name = "旧驱动包（" + plan.Remove.Count + " 个重复版本）",
                        Category = "系统",
                        Size = plan.EstimatedBytes,
                        Note = "只保留最新版本，正在使用的驱动不会被删",
                        Kind = "drivers"
                    });
                }
            }));
            return steps;
        }

        private static void AssignRisk(List<CleanItem> list)
        {
            foreach (CleanItem it in list)
            {
                switch (it.Key)
                {
                    case "temp-user":
                    case "temp-win":
                        it.Risk = 0;
                        it.Impact = "只删除临时文件，程序下次运行时会自动重建，不涉及任何个人数据。";
                        it.Advice = "放心删，建议每次都勾上。";
                        break;
                    case "npm":
                    case "yarn":
                    case "pnpm":
                    case "pip":
                    case "nuget":
                    case "gradle":
                        it.Risk = 0;
                        it.Impact = "删除的只是包缓存。下次安装依赖会重新下载，耗时变长，但不影响已装好的项目和功能。";
                        it.Advice = "放心删；网络不好时建议保留。";
                        break;
                    case "npminstall":
                        it.Risk = 0;
                        it.Impact = "脚手架下载的压缩包缓存，再次安装时会重新拉取。";
                        it.Advice = "放心删。";
                        break;
                    case "crash":
                        it.Risk = 0;
                        it.Impact = "程序崩溃时生成的转储文件，只在排查崩溃问题时有价值。";
                        it.Advice = "放心删，除非你正在排查某个软件的崩溃。";
                        break;
                    case "updaters":
                        it.Risk = 0;
                        it.Impact = "各类软件下载的安装包，安装完成后就不再使用。";
                        it.Advice = "放心删。";
                        break;
                    case "thumb":
                        it.Risk = 0;
                        it.Impact = "缩略图和图标缓存，资源管理器会按需重建，首次打开图片文件夹会稍慢。";
                        it.Advice = "放心删。";
                        break;
                    case "d3d":
                        it.Risk = 0;
                        it.Impact = "显卡着色器缓存，游戏或图形程序重启后会重新生成。";
                        it.Advice = "放心删；删后首次进游戏可能稍慢。";
                        break;
                    case "prefetch":
                        it.Risk = 0;
                        it.Impact = "系统预读取记录，删除后常用程序首次启动会慢一点，之后自动重建。";
                        it.Advice = "可以删，但收益通常不大。";
                        break;
                    case "fontcache":
                        it.Risk = 0;
                        it.Impact = "字体缓存，系统会按需重建，个别软件首次显示字体可能稍慢。";
                        it.Advice = "放心删。";
                        break;
                    case "chrome":
                    case "edge":
                        it.Risk = 1;
                        it.Impact = "清除网页缓存。书签、密码、登录状态不受影响；离线网页和部分图片需要重新加载。";
                        it.Advice = "先完全退出浏览器再清，否则被占用的文件会被跳过。";
                        break;
                    case "drivers":
                        it.Risk = 1;
                        it.Impact = "删除同型号驱动的历史版本，每个驱动保留最新版。删除后无法回滚到旧驱动，正在使用的驱动会被系统拒绝删除。";
                        it.Advice = "空间紧张时清。清完建议重启一次，确认显示和声音正常。";
                        break;
                    case "hiber":
                        it.Risk = 1;
                        it.Impact = "关闭休眠后不能使用「休眠」，同时「快速启动」失效，开机慢几秒。";
                        it.Advice = "不用休眠的台式机/笔记本可以关，随时能再打开。";
                        break;
                    case "recycle":
                        it.Risk = 2;
                        it.Impact = "回收站里的文件将被永久删除，无法通过任何方式恢复。";
                        it.Advice = "清空前先打开回收站，确认里面没有你还在找的文件。";
                        break;
                    default:
                        it.Risk = 1;
                        it.Impact = it.Note;
                        it.Advice = "确认无影响后再删。";
                        break;
                }
                it.Checked = it.Risk == 0;
                LocalizeItem(it);
            }
        }
    }
}
