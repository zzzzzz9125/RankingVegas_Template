#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;

namespace RankingVegas
{
    public sealed partial class RankingVegasWinForms : DockableControl
    {
        public Vegas MyVegas;

        public TimeTracker GlobalTimeTracker { get; set; }
        public RankingConfig GlobalConfig { get; set; }
        public RankingApiClient GlobalApiClient { get; set; }

        private UserInfo currentUserInfo;
        private int currentUserRank = 0;
        private bool isInitialized = false;
        private Timer delayedInitTimer;
        private LeaderboardForm leaderboardForm;
        private string lastAvatarUrl = null;

        public RankingVegasWinForms()
            : base("RankingVegas")
        {
            InitializeComponent();
            PersistDockWindowState = true;

            delayedInitTimer = new Timer();
            delayedInitTimer.Interval = 500;
            delayedInitTimer.Tick += DelayedInitTimer_Tick;
        }

        public override DockWindowStyle DefaultDockWindowStyle
        {
            get { return DockWindowStyle.Floating; }
        }

        public override Size DefaultFloatingSize
        {
            get { return new Size(400, 630); }
        }

        #region Lifecycle

        protected override void OnLoaded(EventArgs args)
        {
            base.OnLoaded(args);

            if (RankingVegasCommand.Instance != null)
            {
                RankingVegasCommand.Instance.RegisterDockView(this);
            }

            InitializeFromGlobalState();

            if (!isInitialized)
            {
                delayedInitTimer.Start();
            }
        }

        private void DelayedInitTimer_Tick(object sender, EventArgs e)
        {
            if (isInitialized)
            {
                delayedInitTimer.Stop();
                return;
            }

            if (RankingVegasCommand.Instance != null)
            {
                RefreshFromGlobalState(
                    RankingVegasCommand.Instance.GlobalTimeTracker,
                    RankingVegasCommand.Instance.GlobalConfig,
                    RankingVegasCommand.Instance.GlobalApiClient);

                if (GlobalTimeTracker != null)
                {
                    delayedInitTimer.Stop();
                }
            }
        }

        public void RefreshFromGlobalState(TimeTracker timeTracker, RankingConfig config, RankingApiClient apiClient)
        {
            if (RankingAppProfile.IsDemo)
            {
                if (timeTracker == null || config == null)
                    return;
            }
            else
            {
                if (timeTracker == null || config == null || apiClient == null)
                    return;
            }

            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => RefreshFromGlobalState(timeTracker, config, apiClient)));
                }
                catch
                {
                }
                return;
            }

            if (GlobalTimeTracker != null && isInitialized)
            {
                GlobalTimeTracker.TimeUpdated -= TimeTracker_TimeUpdated;
                GlobalTimeTracker.StatusChanged -= TimeTracker_StatusChanged;
            }

            GlobalTimeTracker = timeTracker;
            GlobalConfig = config;
            GlobalApiClient = apiClient;

            InitializeFromGlobalState();
        }

        private void InitializeFromGlobalState()
        {
            if (GlobalTimeTracker == null && RankingVegasCommand.Instance != null)
            {
                GlobalTimeTracker = RankingVegasCommand.Instance.GlobalTimeTracker;
                GlobalConfig = RankingVegasCommand.Instance.GlobalConfig;
                GlobalApiClient = RankingVegasCommand.Instance.GlobalApiClient;
            }

            if (GlobalTimeTracker != null && !isInitialized)
            {
                GlobalTimeTracker.TimeUpdated += TimeTracker_TimeUpdated;
                GlobalTimeTracker.StatusChanged += TimeTracker_StatusChanged;
                isInitialized = true;

                SendTimeUpdate();
                SendStatusUpdate();
            }

            SendConfiguredState();
            SendHideSettings();
            SendVegasInfo();
            UpdateOfflineState();
            SendUserInfo();

            // Load offline avatar on initial setup so the default avatar is shown
            if (GlobalConfig != null && GlobalConfig.IsOfflineAccount)
            {
                LoadOfflineAvatar();
            }

            if (GlobalConfig != null && GlobalConfig.IsConfigured() && !string.IsNullOrEmpty(GlobalConfig.SessionCode))
            {
                Task.Run(() =>
                {
                    try
                    {
                        System.Threading.Thread.Sleep(100);
                        SafeInvoke(() => LoadUserInfo());
                    }
                    catch
                    {
                    }
                });
            }
            else if (GlobalConfig != null && !GlobalConfig.IsConfigured() && isInitialized)
            {
                SendStatus(Localization.Text("配置未完成", "Configuration incomplete", "設定未完了"), StatusKind.Error);
            }
        }

        protected override void InitLayout()
        {
            base.InitLayout();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)
            {
                if (GlobalTimeTracker != null)
                {
                    SendTimeUpdate();
                    SendStatusUpdate();
                }

                if (GlobalConfig != null && GlobalConfig.IsConfigured() && !string.IsNullOrEmpty(GlobalConfig.SessionCode))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            SafeInvoke(() => LoadUserInfo());
                        }
                        catch
                        {
                        }
                    });
                }
            }

            base.OnVisibleChanged(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (RankingVegasCommand.Instance != null)
            {
                RankingVegasCommand.Instance.UnregisterDockView(this);
            }

            if (delayedInitTimer != null)
            {
                delayedInitTimer.Stop();
                delayedInitTimer.Dispose();
                delayedInitTimer = null;
            }

            if (GlobalTimeTracker != null && isInitialized)
            {
                GlobalTimeTracker.TimeUpdated -= TimeTracker_TimeUpdated;
                GlobalTimeTracker.StatusChanged -= TimeTracker_StatusChanged;
                isInitialized = false;
            }

            if (leaderboardForm != null && !leaderboardForm.IsDisposed)
            {
                leaderboardForm.Close();
                leaderboardForm.Dispose();
                leaderboardForm = null;
            }

            base.OnClosed(e);
        }

        #endregion

        #region TimeTracker Events

        private void TimeTracker_TimeUpdated(object sender, TimeSpan time)
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => TimeTracker_TimeUpdated(sender, time)));
                }
                catch
                {
                }
                return;
            }

            SendTimeUpdate();

            if (GlobalConfig != null && GlobalConfig.IsOfflineAccount)
            {
                SendUserInfo();
            }
        }

        private void TimeTracker_StatusChanged(object sender, string status)
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => TimeTracker_StatusChanged(sender, status)));
                }
                catch
                {
                }
                return;
            }

            SendStatusUpdate();
        }

        #endregion

        #region Button Click Handlers

        private void BtnBind_Click(object sender, EventArgs e)
        {
            HandleBind();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            HandleLogout();
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            HandleSettings();
        }

        private void BtnRefreshLeaderboard_Click(object sender, EventArgs e)
        {
            HandleShowLeaderboard();
        }

        private void PicUserAvatar_Click(object sender, EventArgs e)
        {
            HandleSelectAvatar();
        }

        private void LblUserNickname_Click(object sender, EventArgs e)
        {
            HandleEditNickname();
        }

        #endregion

        #region Action Handlers

        private void HandleBind()
        {
            if (GlobalConfig == null || !GlobalConfig.IsConfigured())
            {
                MessageBox.Show(
                    Localization.Text("配置未初始化，请检查 App ID 和 App Secret 是否正确配置", "Configuration is not initialized. Please check App ID and App Secret.", "設定が初期化されていません。App ID と App Secret を確認してください。"),
                    Localization.Text("错误", "Error", "エラー"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(GlobalConfig.SessionCode))
            {
                GlobalConfig.SessionCode = RankingConfig.GenerateSessionCode();
                GlobalConfig.Save();
            }

            string bindUrl = GlobalApiClient.GenerateBindUrl(GlobalConfig.SessionCode);

            try
            {
                System.Diagnostics.Process.Start(bindUrl);
                MessageBox.Show(
                    Localization.Text(
                        "浏览器已打开绑定页面\n\n请登录完成绑定\n点击【确定】后将自动刷新账号状态\n\n提示：\n若绑定页面返回\"签名验证失败\"，则说明在线排行榜的签名可能已经更换，您当前使用的 RankVegas 扩展的版本无法绑定到当前的在线排行榜。请尝试重新到您获得该分发版本的地方，下载并更新到最新版本。",
                        "Your browser has opened the binding page.\n\nPlease sign in to complete binding.\nClick OK to refresh the account status.\n\nNote:\nIf the binding page returns 'Signature verification failed', the online leaderboard signature may have changed. Please download and update to the latest version.",
                        "ブラウザで連携ページが開かれました。\n\nサインインして連携を完了してください。\nOKをクリックするとアカウント状態が更新されます。\n\n注：\n連携ページで「署名検証失敗」と表示された場合は、オンラインランキングの署名が変更された可能性があります。最新版をダウンロードして更新してください。"),
                    Localization.Text("绑定账号", "Bind Account", "アカウント連携"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                HandleRefresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Localization.Format("无法打开浏览器: {0}\n\n请手动访问:\n{1}", "Unable to open the browser: {0}\n\nPlease open manually:\n{1}", "ブラウザを開けません: {0}\n\n手動で開いてください:\n{1}", ex.Message, bindUrl),
                    Localization.Text("错误", "Error", "エラー"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleRefresh()
        {
            if (GlobalConfig == null || !GlobalConfig.IsConfigured())
                return;

            LoadUserInfo();
        }

        private void HandleLogout()
        {
            if (GlobalConfig == null)
                return;

            DialogResult result = MessageBox.Show(
                Localization.Text("确定要退出登录吗？退出后将切换为离线账号。", "Are you sure you want to log out? You will be switched to offline mode.", "ログアウトしますか？オフラインモードに切り替わります。"),
                Localization.Text("退出登录", "Logout", "ログアウト"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                GlobalConfig.SessionCode = RankingConfig.GenerateSessionCode();
                SetOfflineMode(true);

                currentUserInfo = null;
                currentUserRank = 0;
                lastAvatarUrl = null;

                // Refresh UI
                SendUserInfo();
            }
        }

        private void HandleSelectAvatar()
        {
            if (GlobalConfig == null || !GlobalConfig.IsOfflineAccount)
                return;

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = Localization.Text("选择头像图片", "Select Avatar Image", "アバター画像を選択");
                dlg.Filter = Localization.Text(
                    "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|所有文件|*.*",
                    "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All Files|*.*",
                    "画像ファイル|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|すべてのファイル|*.*");
                dlg.FilterIndex = 1;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string targetPath = RankingConfig.GetOfflineAvatarFilePath();

                        using (Image src = ImageHelper.LoadImageFromFile(dlg.FileName))
                        {
                            if (src == null)
                            {
                                MessageBox.Show(
                                    Localization.Text("无法加载该图片文件", "Unable to load the image file", "画像ファイルを読み込めません"),
                                    Localization.Text("错误", "Error", "エラー"),
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            using (Image resized = ResizeImageForAvatar(src, 120, 120))
                            {
                                resized.Save(targetPath, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }

                        GlobalConfig.OfflineAvatarPath = targetPath;
                        GlobalConfig.Save();

                        LoadOfflineAvatar();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            Localization.Format("保存头像失败: {0}", "Failed to save avatar: {0}", "アバターの保存に失敗しました: {0}", ex.Message),
                            Localization.Text("错误", "Error", "エラー"),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void HandleEditNickname()
        {
            if (GlobalConfig == null || !GlobalConfig.IsOfflineAccount)
                return;

            string input = ShowInputDialog(
                Localization.Text("输入昵称", "Enter Nickname", "ニックネームを入力"),
                Localization.Text("请输入您的离线账号昵称:", "Please enter your offline account nickname:", "オフラインアカウントのニックネームを入力してください:"),
                GlobalConfig.OfflineNickname ?? "");

            if (input != null)
            {
                GlobalConfig.OfflineNickname = input.Trim();
                GlobalConfig.Save();
                SendUserInfo();
            }
        }

        private void HandleSettings()
        {
            if (GlobalConfig == null)
                return;

            SettingsForm settingsForm = new SettingsForm(GlobalConfig, GlobalTimeTracker, OnSettingsChanged);
            settingsForm.ShowDialog(this);
        }

        private void HandleShowLeaderboard()
        {
            if (!RankingAppProfile.IsDemo)
            {
                if (GlobalConfig == null || !GlobalConfig.IsConfigured())
                {
                    MessageBox.Show(
                        Localization.Text("配置未初始化", "Configuration is not initialized", "設定が初期化されていません"),
                        Localization.Text("错误", "Error", "エラー"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (leaderboardForm != null && !leaderboardForm.IsDisposed)
            {
                leaderboardForm.BringToFront();
                return;
            }

            leaderboardForm = new LeaderboardForm(GlobalConfig, GlobalApiClient, currentUserInfo, currentUserRank);
            leaderboardForm.FormClosed += delegate { leaderboardForm = null; };
            leaderboardForm.Show(this);
        }

        private void OnSettingsChanged()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(OnSettingsChanged));
                }
                else
                {
                    SendHideSettings();
                    SendUserInfo();
                    UpdateLocalizedTexts();
                    SendStatusUpdate();
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Data Loading

        private void LoadUserInfo()
        {
            if (GlobalConfig == null || GlobalApiClient == null || string.IsNullOrEmpty(GlobalConfig.SessionCode))
            {
                SetOfflineMode(true);
                currentUserInfo = null;
                currentUserRank = 0;
                SendUserInfo();
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var response = GlobalApiClient.GetUserInfo(GlobalConfig.SessionCode);

                    SafeInvoke(() =>
                    {
                        if (response.Success && response.Data != null)
                        {
                            bool wasOffline = GlobalConfig.IsOfflineAccount;
                            currentUserInfo = response.Data;
                            currentUserRank = response.Data.Rank;
                            SetOfflineMode(false);

                            lastAvatarUrl = response.Data.Avatar;

                            if (wasOffline || lastAvatarUrl != null)
                            {
                                LoadUserAvatarAsync(response.Data.Avatar);
                            }
                        }
                        else
                        {
                            currentUserInfo = null;
                            currentUserRank = 0;
                            SetOfflineMode(true);
                        }
                        SendUserInfo();
                    });
                }
                catch
                {
                    SafeInvoke(() =>
                    {
                        currentUserInfo = null;
                        currentUserRank = 0;
                        SetOfflineMode(true);
                        SendUserInfo();
                    });
                }
            });
        }

        private void SetOfflineMode(bool isOffline)
        {
            if (GlobalConfig == null)
                return;

            if (GlobalConfig.IsOfflineAccount != isOffline)
            {
                GlobalConfig.IsOfflineAccount = isOffline;
                GlobalConfig.Save();
            }

            if (GlobalTimeTracker != null)
            {
                GlobalTimeTracker.SetOfflineMode(isOffline);
            }

            UpdateOfflineState();

            if (isOffline)
            {
                LoadOfflineAvatar();
            }
        }

        #endregion

        #region UI Update Methods

        private void SendTimeUpdate()
        {
            if (GlobalTimeTracker == null)
                return;

            bool rendering = GlobalTimeTracker.IsRendering();
            TimeSpan time = rendering ? GlobalTimeTracker.GetRenderTime() : GlobalTimeTracker.GetTotalTime();
            lblTimeValue.Text = $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            lblTimeTitle.Text = Localization.GetTimerLabel(rendering);
        }

        private void SendStatusUpdate()
        {
            if (GlobalTimeTracker == null)
            {
                SendStatus(Localization.Text("等待同步", "Waiting for sync", "同期待ち"), StatusKind.Active);
                return;
            }

            SendStatus(GlobalTimeTracker.GetCurrentStatus(), GlobalTimeTracker.GetCurrentStatusKind());
        }

        private void SendStatus(string status, StatusKind statusKind)
        {
            lblStatusValue.Text = status;

            switch (statusKind)
            {
                case StatusKind.Idle:
                    lblStatusIcon.ForeColor = Color.Orange;
                    break;
                case StatusKind.Rendering:
                    lblStatusIcon.ForeColor = Color.DeepSkyBlue;
                    break;
                case StatusKind.Error:
                    lblStatusIcon.ForeColor = Color.Red;
                    break;
                default:
                    lblStatusIcon.ForeColor = Color.LimeGreen;
                    break;
            }
        }

        private void SendConfiguredState()
        {
            bool configured = GlobalConfig != null && GlobalConfig.IsConfigured();
            btnBind.Enabled = configured;
            btnRefreshLeaderboard.Enabled = RankingAppProfile.IsDemo || configured;
        }

        private void SendVegasInfo()
        {
            try
            {
                string version = RankingVegasCommand.VegasVersion;
                if (!string.IsNullOrEmpty(version))
                {
                    lblVegasVersion.Text = version;
                }

                string iconBase64 = RankingVegasCommand.VegasIconBase64;
                if (!string.IsNullOrEmpty(iconBase64))
                {
                    byte[] iconBytes = Convert.FromBase64String(iconBase64);
                    using (var ms = new System.IO.MemoryStream(iconBytes))
                    {
                        Image oldImage = picVegasIcon.Image;
                        picVegasIcon.Image = Image.FromStream(ms);
                        if (oldImage != null)
                        {
                            oldImage.Dispose();
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void SendHideSettings()
        {
            if (GlobalConfig == null)
                return;

            lblTimeValue.Visible = GlobalConfig.ShowTimer;
            lblTimeTitle.Visible = GlobalConfig.ShowTimer;

            lblStatusIcon.Visible = GlobalConfig.ShowStatus;
            lblStatusValue.Visible = GlobalConfig.ShowStatus;

            picVegasIcon.Visible = GlobalConfig.ShowVegasInfo;
            lblVegasVersion.Visible = GlobalConfig.ShowVegasInfo;

            // Also collapse the inner vegas info panel (created as a local panel in designer)
            // so that when Vegas info is hidden the stats panel's height reduces and
            // controls below move up.
            try
            {
                if (panelStats != null)
                {
                    foreach (Control child in panelStats.Controls)
                    {
                        if (child != null && child.Controls != null && (child.Controls.Contains(picVegasIcon) || child.Controls.Contains(lblVegasVersion)))
                        {
                            child.Visible = GlobalConfig.ShowVegasInfo;
                            break;
                        }
                    }
                }
            }
            catch
            {
            }

            bool hideStatsPanel = !GlobalConfig.ShowTimer && !GlobalConfig.ShowStatus && !GlobalConfig.ShowVegasInfo;
            panelStats.Visible = !hideStatsPanel;

            panelUser.Visible = GlobalConfig.ShowAccount;

            bool showStatusOnly = GlobalConfig.ShowStatus && !GlobalConfig.ShowTimer && !GlobalConfig.ShowVegasInfo;
            if (showStatusOnly)
            {
                panelStats.Padding = new Padding(12, 6, 12, 6);
                panelStats.MinimumSize = new Size(0, 36);
            }
            else if (!hideStatsPanel)
            {
                panelStats.Padding = new Padding(12, 10, 12, 8);
                panelStats.MinimumSize = new Size(0, 80);
            }
            else
            {
                panelStats.MinimumSize = new Size(0, 0);
            }

            // Update layout row style based on visibility
            if (mainLayout != null && mainLayout.RowStyles.Count > 1)
            {
                if (hideStatsPanel)
                {
                    mainLayout.RowStyles[0].SizeType = SizeType.Absolute;
                    mainLayout.RowStyles[0].Height = 0;
                }
                else
                {
                    mainLayout.RowStyles[0].SizeType = SizeType.AutoSize;
                }

                if (!GlobalConfig.ShowAccount)
                {
                    mainLayout.RowStyles[1].SizeType = SizeType.Absolute;
                    mainLayout.RowStyles[1].Height = 0;
                }
                else
                {
                    mainLayout.RowStyles[1].SizeType = SizeType.Absolute;
                    mainLayout.RowStyles[1].Height = 114;
                }
            }
        }

        private void SendUserInfo()
        {
            bool isOffline = GlobalConfig != null && GlobalConfig.IsOfflineAccount;

            if (isOffline)
            {
                lblUserNickname.Text = GlobalConfig.GetOfflineDisplayName();
                int offlineSec = GlobalTimeTracker != null ? GlobalTimeTracker.GetOfflineTotalDurationSeconds() : 0;
                lblUserDuration.Text = Localization.Format("总时长: {0}", "Total Duration: {0}", "合計時間: {0}", FormatDuration(offlineSec));
                lblUserRank.Text = Localization.Text("排名: --", "Rank: --", "ランキング: --");

                btnBind.Text = Localization.Text("绑定账号", "Bind Account", "アカウント連携");
                btnBind.Visible = true;
                btnLogout.Visible = false;

                picUserAvatar.Cursor = Cursors.Hand;
                lblUserNickname.Cursor = Cursors.Hand;
            }
            else if (currentUserInfo != null)
            {
                lblUserNickname.Text = currentUserInfo.Nickname;
                lblUserDuration.Text = Localization.Format("总时长: {0}", "Total Duration: {0}", "合計時間: {0}", FormatDuration(currentUserInfo.TotalDuration));
                UpdateUserRankDisplay();

                btnBind.Visible = false;
                btnLogout.Visible = true;

                picUserAvatar.Cursor = Cursors.Default;
                lblUserNickname.Cursor = Cursors.Default;
            }
            else
            {
                lblUserNickname.Text = Localization.Text("未绑定账号", "Account not bound", "アカウント未連携");
                lblUserDuration.Text = Localization.Text("总时长: --", "Total Duration: --", "合計時間: --");
                lblUserRank.Text = Localization.Text("排名: --", "Rank: --", "ランキング: --");

                btnBind.Visible = true;
                btnLogout.Visible = false;

                picUserAvatar.Cursor = Cursors.Default;
                lblUserNickname.Cursor = Cursors.Default;
            }

            UpdateAppTitle();
        }

        private void UpdateOfflineState()
        {
            bool isOffline = GlobalConfig != null && GlobalConfig.IsOfflineAccount;
            UpdateAppTitle();
        }

        private void UpdateAppTitle()
        {
            bool isOffline = GlobalConfig != null && GlobalConfig.IsOfflineAccount;
            DisplayName = RankingAppProfile.GetDisplayName(isOffline);
        }

        private void UpdateUserRankDisplay()
        {
            if (currentUserRank > 0)
            {
                lblUserRank.Text = Localization.Format("排名: {0} {1}", "Rank: {0} {1}", "ランキング: {0} {1}", GetRankEmoji(currentUserRank), currentUserRank);
            }
            else
            {
                lblUserRank.Text = Localization.Text("排名: --", "Rank: --", "ランキング: --");
            }
        }

        private void UpdateLocalizedTexts()
        {
            bool rendering = GlobalTimeTracker != null && GlobalTimeTracker.IsRendering();
            lblTimeTitle.Text = Localization.GetTimerLabel(rendering);
            btnRefreshLeaderboard.Text = Localization.Text("排行榜", "Leaderboard", "ランキング");

            ApplyLocalizedFonts();

            if (GlobalTimeTracker != null)
            {
                GlobalTimeTracker.RefreshStatus();
            }

            SendUserInfo();
        }

        private void ApplyLocalizedFonts()
        {
            string fontFamily = Localization.FontFamily;

            this.Font = new Font(fontFamily, 9);
            lblTimeTitle.Font = new Font(fontFamily, 8, FontStyle.Bold);
            lblTimeValue.Font = new Font("Consolas", 18, FontStyle.Bold);
            lblStatusIcon.Font = new Font(fontFamily, 9);
            lblStatusValue.Font = new Font(fontFamily, 8);
            lblVegasVersion.Font = new Font(fontFamily, 8);
            lblUserNickname.Font = new Font(fontFamily, 9, FontStyle.Bold);
            lblUserDuration.Font = new Font(fontFamily, 8);
            lblUserRank.Font = new Font(fontFamily, 8);
            btnBind.Font = new Font(fontFamily, 8, FontStyle.Bold);
            btnLogout.Font = new Font(fontFamily, 8, FontStyle.Bold);
            btnRefreshLeaderboard.Font = new Font(fontFamily, 8, FontStyle.Bold);
            btnSettings.Font = new Font(Localization.EmojiFontFamily, 10);
        }

        #endregion

        #region Avatar Loading

        private void LoadOfflineAvatar()
        {
            string avatarPath = RankingConfig.GetOfflineAvatarFilePath();

            if (File.Exists(avatarPath))
            {
                try
                {
                    Image img = ImageHelper.LoadImageFromFile(avatarPath);
                    if (img != null)
                    {
                        Image avatar = ImageHelper.MakeRoundedImage(img, 8);
                        img.Dispose();
                        SetAvatarImage(avatar);
                        return;
                    }
                }
                catch
                {
                }
            }

            SetAvatarImage(CreateDefaultAvatar());
        }

        private void LoadUserAvatarAsync(string avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl))
            {
                SetAvatarImage(CreateDefaultAvatar());
                return;
            }

            if (!avatarUrl.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                avatarUrl = RankingAppProfile.ApiOrigin + avatarUrl;
            }

            string url = avatarUrl;
            Task.Run(() =>
            {
                Image avatar = ImageHelper.LoadImageForWinFormsFromUrl(url, 60, 60);

                if (avatar != null)
                {
                    avatar = ImageHelper.MakeRoundedImage(avatar, 8);
                }
                else
                {
                    avatar = CreateDefaultAvatar();
                }

                try
                {
                    SafeInvoke(() => SetAvatarImage(avatar));
                }
                catch
                {
                    avatar?.Dispose();
                }
            });
        }

        private void SetAvatarImage(Image newImage)
        {
            Image oldImage = picUserAvatar.Image;
            picUserAvatar.Image = newImage;

            if (oldImage != null && oldImage != newImage)
            {
                try
                {
                    oldImage.Dispose();
                }
                catch
                {
                }
            }
        }

        private Image CreateDefaultAvatar()
        {
            try
            {
                byte[] avatarBytes = EmbeddedResourceHelper.ReadEmbeddedResource("default_avatar.jpg");
                using (var ms = new System.IO.MemoryStream(avatarBytes))
                {
                    Image loadedAvatar = Image.FromStream(ms);
                    Image roundedAvatar = ImageHelper.MakeRoundedImage(loadedAvatar, 8);
                    loadedAvatar.Dispose();
                    return roundedAvatar;
                }
            }
            catch { }

            Bitmap placeholderAvatar = new Bitmap(60, 60);
            using (Graphics g = Graphics.FromImage(placeholderAvatar))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.05f)))
                {
                    g.FillEllipse(brush, 0, 0, 60, 60);
                }
                using (Font font = new Font(Localization.FontFamily, 18, FontStyle.Bold))
                {
                    string text = "?";
                    SizeF textSize = g.MeasureString(text, font);
                    using (SolidBrush textBrush = new SolidBrush(RankingVegasCommand.UIForeColor))
                    {
                        g.DrawString(text, font, textBrush, (60 - textSize.Width) / 2, (60 - textSize.Height) / 2);
                    }
                }
            }
            return ImageHelper.MakeRoundedImage(placeholderAvatar, 8);
        }

        #endregion

        #region Utility

        private Image ResizeImageForAvatar(Image image, int maxWidth, int maxHeight)
        {
            double ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
            int newWidth = (int)(image.Width * ratio);
            int newHeight = (int)(image.Height * ratio);

            Bitmap resized = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return resized;
        }

        private string ShowInputDialog(string title, string prompt, string defaultValue)
        {
            Form inputForm = new Form
            {
                Text = title,
                Font = new Font(Localization.FontFamily, 9F),
                Width = 350,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = RankingVegasCommand.UIBackColor,
                ForeColor = RankingVegasCommand.UIForeColor
            };

            Label lblPrompt = new Label
            {
                Text = prompt,
                Location = new Point(12, 15),
                Size = new Size(310, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };

            TextBox txtInput = new TextBox
            {
                Text = defaultValue,
                Location = new Point(12, 40),
                Size = new Size(310, 24),
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            Button btnOK = new Button
            {
                Text = Localization.Text("确定", "OK", "OK"),
                Location = new Point(166, 75),
                Size = new Size(75, 28),
                DialogResult = DialogResult.OK,
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat
            };

            Button btnCancel = new Button
            {
                Text = Localization.Text("取消", "Cancel", "キャンセル"),
                Location = new Point(247, 75),
                Size = new Size(75, 28),
                DialogResult = DialogResult.Cancel,
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat
            };

            inputForm.Controls.Add(lblPrompt);
            inputForm.Controls.Add(txtInput);
            inputForm.Controls.Add(btnOK);
            inputForm.Controls.Add(btnCancel);
            inputForm.AcceptButton = btnOK;
            inputForm.CancelButton = btnCancel;

            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                return txtInput.Text;
            }
            return null;
        }

        private static string GetRankEmoji(int rank)
        {
            switch (rank)
            {
                case 1: return "🥇";
                case 2: return "🥈";
                case 3: return "🥉";
                default: return rank <= 10 ? "🏅" : "⭐";
            }
        }

        private static string FormatDuration(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);

            if (time.TotalHours >= 1)
            {
                return Localization.Format("{0:F1} 小时", "{0:F1} hrs", "{0:F1} 時間", time.TotalHours);
            }
            else if (time.TotalMinutes >= 1)
            {
                return Localization.Format("{0} 分钟", "{0} mins", "{0} 分", (int)time.TotalMinutes);
            }
            else
            {
                return Localization.Format("{0} 秒", "{0} secs", "{0} 秒", seconds);
            }
        }

        private void SafeInvoke(Action action)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                if (InvokeRequired)
                {
                    Invoke(action);
                }
                else
                {
                    action();
                }
            }
            catch
            {
            }
        }

        #endregion
    }
}
