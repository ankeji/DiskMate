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
            bar.Height = Theme.P(62);
            bar.BackColor = Theme.Bg;
            pageBig.Controls.Add(bar);

            btnBigScan = new FlatButton();
            btnBigScan.Kind = BtnKind.Primary;
            Txt(btnBigScan, "big.btn");
            btnBigScan.Size = new Size(Theme.P(140), Theme.P(38));
            btnBigScan.Location = new Point(Theme.P(0), Theme.P(10));
            btnBigScan.Click += delegate { StartBigScan(); };
            bar.Controls.Add(btnBigScan);

            cmbThreshold = new ComboBox();
            cmbThreshold.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbThreshold.FlatStyle = FlatStyle.Flat;
            cmbThreshold.Font = Theme.Base;
            cmbThreshold.ItemHeight = Theme.P(22);
            cmbThreshold.Size = new Size(Theme.P(140), Theme.P(30));
            cmbThreshold.Location = new Point(Theme.P(152), Theme.P(14));
            FillThresholds();   // 内部会填选项并选中默认项（200 MB）
            bar.Controls.Add(cmbThreshold);

            lblBigHint = new Label();
            lblBigHint.AutoSize = false;
            lblBigHint.AutoEllipsis = true;   // 窗口变窄时不硬挤，末尾省略
            lblBigHint.Location = new Point(Theme.P(306), Theme.P(14));
            lblBigHint.Size = new Size(Theme.P(520), Theme.P(30));
            lblBigHint.ForeColor = Theme.TextSub;
            Txt(lblBigHint, "big.idle");
            bar.Controls.Add(lblBigHint);

            btnBigReveal = new FlatButton();
            btnBigReveal.Kind = BtnKind.Secondary;
            Txt(btnBigReveal, "big.open");
            btnBigReveal.Icon = "folder";
            btnBigReveal.Enabled = false;
            btnBigReveal.Size = new Size(Theme.P(120), Theme.P(36));
            btnBigReveal.Location = new Point(Theme.P(840), Theme.P(11));
            btnBigReveal.Click += delegate { RevealBigFile(); };
            bar.Controls.Add(btnBigReveal);

            bar.Resize += delegate
            {
                btnBigReveal.Location = new Point(bar.Width - btnBigReveal.Width, Theme.P(12));
                lblBigHint.Width = Math.Max(Theme.P(120), btnBigReveal.Left - Theme.P(20) - lblBigHint.Left);
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
            lvBig.DoubleClick += delegate { RevealBigFile(); };
            lvBig.SelectedIndexChanged += delegate { UpdateBigButtons(); };

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
                bar.SetBounds(Theme.P(0), Theme.P(0), pageBig.Width, Theme.P(62));
                card.SetBounds(Theme.P(0), Theme.P(62), pageBig.Width, Math.Max(Theme.P(120), pageBig.Height - Theme.P(62)));
            };
        }

        private void StartBigScan()
        {
            if (busy) return;
            if (!RequireLogin()) return;
            busy = true;
            scanCancel = false;
            btnBigScan.Enabled = false;
            btnBigScan.Text = I18n.T("big.scanning");
            btnBigScan.Invalidate();
            lblBigHint.Text = I18n.T("big.scanning");
            if (lblBigEmpty != null) lblBigEmpty.Visible = false;
            long threshold = BigThresholds[Math.Max(0, cmbThreshold.SelectedIndex)];

            Task.Factory.StartNew(delegate
            {
                var result = new List<KeyValuePair<string, long>>();
                var roots = new List<string>();
                roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
                roots.Add("C:\\Program Files");
                roots.Add("C:\\Program Files (x86)");

                var stack = new Stack<string>();
                foreach (string r in roots) if (Directory.Exists(r)) stack.Push(r);
                long scanned = 0;
                long reportAt = 0;

                while (stack.Count > 0)
                {
                    if (scanCancel) break;
                    string dir = stack.Pop();
                    try
                    {
                        foreach (string f in Directory.EnumerateFiles(dir))
                        {
                            try
                            {
                                var fi = new FileInfo(f);
                                scanned++;
                                if (fi.Length > threshold) result.Add(new KeyValuePair<string, long>(f, fi.Length));
                            }
                            catch { }
                        }
                    }
                    catch { }
                    if (++reportAt % 40 == 0)
                    {
                        long n = scanned;
                        Ui(delegate { lblBigHint.Text = I18n.F("big.progress", n, result.Count); });
                    }
                    try
                    {
                        foreach (string d in Directory.EnumerateDirectories(dir))
                        {
                            try
                            {
                                var di = new DirectoryInfo(d);
                                if ((di.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                                if (di.Name.Equals("WinSxS", StringComparison.OrdinalIgnoreCase)) continue;
                                stack.Push(d);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                result.Sort(delegate(KeyValuePair<string, long> a, KeyValuePair<string, long> b) { return b.Value.CompareTo(a.Value); });

                Ui(delegate
                {
                    lvBig.BeginUpdate();
                    lvBig.Items.Clear();
                    foreach (KeyValuePair<string, long> kv in result)
                        lvBig.Items.Add(new ListViewItem(new[] { Theme.Fmt(kv.Value), kv.Key }));
                    lvBig.EndUpdate();
                    lblBigHint.Text = scanCancel
                        ? I18n.T("big.cancel")
                        : I18n.F("big.done", scanned, result.Count, Theme.Fmt(threshold));
                    Log(string.Format("大文件扫描：{0} 个命中", result.Count));
                    if (!scanCancel) Session.PingActivity("bigscan");   // 用了一次大文件扫描，记一次活跃
                    busy = false;
                    btnBigScan.Enabled = true;
                    Txt(btnBigScan, "big.btn");
                    btnBigScan.Invalidate();
                    if (lblBigEmpty != null)
                    {
                        bool empty = lvBig.Items.Count == 0;
                        lblBigEmpty.Visible = empty;
                        if (empty) lblBigEmpty.Text = I18n.F("big.none", Theme.Fmt(threshold));
                    }
                    if (lvBig.Items.Count > 0) { lvBig.Items[0].Selected = true; lvBig.Items[0].Focused = true; }
                    UpdateBigButtons();
                });
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>阈值下拉：跟随语言切换重建，保留选中项</summary>
        private void FillThresholds()
        {
            if (cmbThreshold == null) return;
            int keep = cmbThreshold.SelectedIndex;
            cmbThreshold.BeginUpdate();
            try
            {
                cmbThreshold.Items.Clear();
                cmbThreshold.Items.Add(I18n.T("big.th100"));
                cmbThreshold.Items.Add(I18n.T("big.th200"));
                cmbThreshold.Items.Add(I18n.T("big.th500"));
                cmbThreshold.Items.Add(I18n.T("big.th1g"));
            }
            finally { cmbThreshold.EndUpdate(); }
            cmbThreshold.SelectedIndex = keep >= 0 && keep < cmbThreshold.Items.Count ? keep : 1;
        }

        /// <summary>没选中行时「打开位置」不可点</summary>
        private void UpdateBigButtons()
        {
            if (btnBigReveal == null) return;
            bool has = lvBig != null && lvBig.SelectedItems.Count > 0;
            if (btnBigReveal.Enabled != has)
            {
                btnBigReveal.Enabled = has;
                btnBigReveal.Invalidate();
            }
        }

        private void RevealBigFile()
        {
            if (lvBig.SelectedItems.Count == 0) return;
            try { Process.Start("explorer.exe", "/select,\"" + lvBig.SelectedItems[0].SubItems[1].Text + "\""); }
            catch { }
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
