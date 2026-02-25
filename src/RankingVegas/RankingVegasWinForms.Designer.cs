#if !Sony
using ScriptPortal.Vegas;
using Region = ScriptPortal.Vegas.Region;
#else
using Sony.Vegas;
using Region = Sony.Vegas.Region;
#endif

using System;
using System.Drawing;
using System.Windows.Forms;

namespace RankingVegas
{
    sealed partial class RankingVegasWinForms
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel mainLayout;

        // Stats panel
        private Panel panelStats;
        private Label lblTimeTitle;
        private Label lblTimeValue;
        private Label lblStatusIcon;
        private Label lblStatusValue;
        private PictureBox picVegasIcon;
        private Label lblVegasVersion;

        // User panel
        private Panel panelUser;
        private PictureBox picUserAvatar;
        private Label lblUserNickname;
        private Label lblUserDuration;
        private Label lblUserRank;
        private Button btnBind;
        private Button btnLogout;

        // Buttons panel
        private Panel panelButtons;
        private Button btnRefreshLeaderboard;
        private Button btnSettings;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            Color darkBg = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.03f);
            Color lightBg = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.02f);

            this.SuspendLayout();

            this.mainLayout = new TableLayoutPanel();
            this.picVegasIcon = new PictureBox();
            this.lblVegasVersion = new Label();
            this.panelStats = new Panel();
            this.lblTimeTitle = new Label();
            this.lblTimeValue = new Label();
            this.lblStatusIcon = new Label();
            this.lblStatusValue = new Label();

            this.panelUser = new Panel();
            this.picUserAvatar = new PictureBox();
            this.lblUserNickname = new Label();
            this.lblUserDuration = new Label();
            this.lblUserRank = new Label();
            this.btnBind = new Button();
            this.btnLogout = new Button();

            this.panelButtons = new Panel();
            this.btnRefreshLeaderboard = new Button();
            this.btnSettings = new Button();

            // Main layout
            this.mainLayout.Dock = DockStyle.Fill;
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.mainLayout.RowCount = 3;
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 114F));
            this.mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            this.mainLayout.Padding = new Padding(8);
            this.mainLayout.BackColor = RankingVegasCommand.UIBackColor;

            // Vegas info panel (icon + version)
            Panel vegasInfoPanel = new Panel();
            vegasInfoPanel.Dock = DockStyle.Top;
            vegasInfoPanel.Height = 24;
            vegasInfoPanel.Padding = new Padding(0, 5, 0, 0);

            this.picVegasIcon.Location = new Point(0, 2);
            this.picVegasIcon.Size = new Size(18, 18);
            this.picVegasIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            this.picVegasIcon.BackColor = Color.Transparent;

            this.lblVegasVersion.Text = "";
            this.lblVegasVersion.Location = new Point(22, 3);
            this.lblVegasVersion.AutoSize = true;
            this.lblVegasVersion.Font = new Font(Localization.FontFamily, 8);
            this.lblVegasVersion.ForeColor = ControlPaint.Light(RankingVegasCommand.UIForeColor, 0.2f);

            vegasInfoPanel.Controls.Add(this.picVegasIcon);
            vegasInfoPanel.Controls.Add(this.lblVegasVersion);

            // Add to panelStats - order matters for DockStyle.Top (last added = topmost)
            this.panelStats.Controls.Add(vegasInfoPanel);

            // Stats Panel
            this.panelStats.Dock = DockStyle.Fill;
            this.panelStats.BackColor = darkBg;
            this.panelStats.BorderStyle = BorderStyle.FixedSingle;
            this.panelStats.Margin = new Padding(0, 0, 0, 6);
            this.panelStats.Padding = new Padding(12, 10, 12, 8);
            this.panelStats.MinimumSize = new Size(0, 80);
            this.panelStats.AutoSize = true;

            this.lblTimeTitle.Text = Localization.Text("累计时长", "Total Duration", "累計時間");
            this.lblTimeTitle.Dock = DockStyle.Top;
            this.lblTimeTitle.Height = 18;
            this.lblTimeTitle.Font = new Font(Localization.FontFamily, 8, FontStyle.Bold);
            this.lblTimeTitle.ForeColor = RankingVegasCommand.UIForeColor;

            this.lblTimeValue.Text = "00:00:00";
            this.lblTimeValue.Dock = DockStyle.Top;
            this.lblTimeValue.Height = 30;
            this.lblTimeValue.Font = new Font("Consolas", 18, FontStyle.Bold);
            this.lblTimeValue.ForeColor = RankingVegasCommand.UIForeColor;
            this.lblTimeValue.TextAlign = ContentAlignment.MiddleLeft;

            Panel statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 20;
            statusPanel.Padding = new Padding(0, 5, 0, 0);

            this.lblStatusIcon.Text = "●";
            this.lblStatusIcon.Location = new Point(0, 0);
            this.lblStatusIcon.Size = new Size(18, 18);
            this.lblStatusIcon.Font = new Font(Localization.FontFamily, 9);
            this.lblStatusIcon.ForeColor = Color.LimeGreen;

            this.lblStatusValue.Text = Localization.Text("等待同步", "Waiting for sync", "同期待ち");
            this.lblStatusValue.Location = new Point(18, 0);
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.Font = new Font(Localization.FontFamily, 8);
            this.lblStatusValue.ForeColor = ControlPaint.Light(RankingVegasCommand.UIForeColor, 0.2f);

            statusPanel.Controls.Add(this.lblStatusIcon);
            statusPanel.Controls.Add(this.lblStatusValue);

            this.panelStats.Controls.Add(statusPanel);
            this.panelStats.Controls.Add(this.lblTimeValue);
            this.panelStats.Controls.Add(this.lblTimeTitle);
            this.panelStats.Controls.Add(vegasInfoPanel);

            // User Panel
            this.panelUser.Dock = DockStyle.Fill;
            this.panelUser.BackColor = lightBg;
            this.panelUser.BorderStyle = BorderStyle.FixedSingle;
            this.panelUser.Margin = new Padding(0, 0, 0, 6);
            this.panelUser.Padding = new Padding(12);

            this.picUserAvatar.Location = new Point(12, 12);
            this.picUserAvatar.Size = new Size(56, 56);
            this.picUserAvatar.SizeMode = PictureBoxSizeMode.StretchImage;
            this.picUserAvatar.BackColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.08f);
            this.picUserAvatar.Click += PicUserAvatar_Click;

            this.lblUserNickname.Text = Localization.Text("未绑定账号", "Account not bound", "アカウント未連携");
            this.lblUserNickname.Location = new Point(78, 12);
            this.lblUserNickname.Size = new Size(200, 22);
            this.lblUserNickname.Font = new Font(Localization.FontFamily, 9, FontStyle.Bold);
            this.lblUserNickname.ForeColor = RankingVegasCommand.UIForeColor;
            this.lblUserNickname.Click += LblUserNickname_Click;

            this.lblUserDuration.Text = Localization.Text("总时长: --", "Total Duration: --", "合計時間: --");
            this.lblUserDuration.Location = new Point(78, 34);
            this.lblUserDuration.Size = new Size(200, 18);
            this.lblUserDuration.Font = new Font(Localization.FontFamily, 8);
            this.lblUserDuration.ForeColor = ControlPaint.Light(RankingVegasCommand.UIForeColor, 0.2f);

            this.lblUserRank.Text = Localization.Text("排名: --", "Rank: --", "ランキング: --");
            this.lblUserRank.Location = new Point(78, 52);
            this.lblUserRank.Size = new Size(200, 18);
            this.lblUserRank.Font = new Font(Localization.FontFamily, 8);
            this.lblUserRank.ForeColor = ControlPaint.Light(RankingVegasCommand.UIForeColor, 0.2f);

            this.btnBind.Text = Localization.Text("绑定账号", "Bind Account", "アカウント連携");
            this.btnBind.Location = new Point(78, 72);
            this.btnBind.Size = new Size(200, 28);
            this.btnBind.Anchor = AnchorStyles.Left;
            this.btnBind.Font = new Font(Localization.FontFamily, 8, FontStyle.Bold);
            this.btnBind.ForeColor = RankingVegasCommand.UIForeColor;
            this.btnBind.BackColor = lightBg;
            this.btnBind.FlatStyle = FlatStyle.Flat;
            this.btnBind.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            this.btnBind.Cursor = Cursors.Hand;
            this.btnBind.Click += BtnBind_Click;

            this.btnLogout.Text = Localization.Text("退出登录", "Logout", "ログアウト");
            this.btnLogout.Location = new Point(78, 72);
            this.btnLogout.Size = new Size(200, 28);
            this.btnLogout.Anchor = AnchorStyles.Left;
            this.btnLogout.Font = new Font(Localization.FontFamily, 8, FontStyle.Bold);
            this.btnLogout.ForeColor = RankingVegasCommand.UIForeColor;
            this.btnLogout.BackColor = lightBg;
            this.btnLogout.FlatStyle = FlatStyle.Flat;
            this.btnLogout.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            this.btnLogout.Cursor = Cursors.Hand;
            this.btnLogout.Visible = false;
            this.btnLogout.Click += BtnLogout_Click;

            this.panelUser.Controls.Add(this.picUserAvatar);
            this.panelUser.Controls.Add(this.lblUserNickname);
            this.panelUser.Controls.Add(this.lblUserDuration);
            this.panelUser.Controls.Add(this.lblUserRank);
            this.panelUser.Controls.Add(this.btnBind);
            this.panelUser.Controls.Add(this.btnLogout);

            // Buttons Panel
            this.panelButtons.Dock = DockStyle.Fill;
            this.panelButtons.BackColor = lightBg;
            this.panelButtons.BorderStyle = BorderStyle.FixedSingle;
            this.panelButtons.Margin = new Padding(0, 0, 0, 0);
            this.panelButtons.Padding = new Padding(12, 8, 12, 8);

            TableLayoutPanel buttonLayout = new TableLayoutPanel();
            buttonLayout.Dock = DockStyle.Fill;
            buttonLayout.ColumnCount = 2;
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            buttonLayout.RowCount = 1;
            buttonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            buttonLayout.Margin = new Padding(0);
            buttonLayout.Padding = new Padding(0);

            this.btnRefreshLeaderboard.Text = Localization.Text("排行榜", "Leaderboard", "ランキング");
            this.btnRefreshLeaderboard.Dock = DockStyle.Fill;
            this.btnRefreshLeaderboard.Margin = new Padding(0, 0, 4, 0);
            this.btnRefreshLeaderboard.Font = new Font(Localization.FontFamily, 8, FontStyle.Bold);
            this.btnRefreshLeaderboard.ForeColor = RankingVegasCommand.UIForeColor;
            this.btnRefreshLeaderboard.BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f);
            this.btnRefreshLeaderboard.FlatStyle = FlatStyle.Flat;
            this.btnRefreshLeaderboard.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            this.btnRefreshLeaderboard.Cursor = Cursors.Hand;
            this.btnRefreshLeaderboard.UseVisualStyleBackColor = false;
            this.btnRefreshLeaderboard.Click += BtnRefreshLeaderboard_Click;

            this.btnSettings.Text = "⚙";
            this.btnSettings.Dock = DockStyle.Fill;
            this.btnSettings.Margin = new Padding(4, 0, 0, 0);
            this.btnSettings.Font = new Font(Localization.EmojiFontFamily, 10);
            this.btnSettings.ForeColor = RankingVegasCommand.UIForeColor;
            this.btnSettings.BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f);
            this.btnSettings.FlatStyle = FlatStyle.Flat;
            this.btnSettings.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            this.btnSettings.Cursor = Cursors.Hand;
            this.btnSettings.UseVisualStyleBackColor = false;
            this.btnSettings.Click += BtnSettings_Click;

            buttonLayout.Controls.Add(this.btnRefreshLeaderboard, 0, 0);
            buttonLayout.Controls.Add(this.btnSettings, 1, 0);

            this.panelButtons.Controls.Add(buttonLayout);

            // Add to main layout
            this.mainLayout.Controls.Add(this.panelStats, 0, 0);
            this.mainLayout.Controls.Add(this.panelUser, 0, 1);
            this.mainLayout.Controls.Add(this.panelButtons, 0, 2);

            this.AutoScaleMode = AutoScaleMode.Font;
            this.MinimumSize = new Size(400, 400);
            this.BackColor = RankingVegasCommand.UIBackColor;
            this.ForeColor = RankingVegasCommand.UIForeColor;
            this.DisplayName = Localization.Text("排行榜", "Ranking", "ランキング");
            this.Font = new Font(Localization.FontFamily, 9);

            this.Controls.Add(this.mainLayout);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
