#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace RankingVegas
{
    public sealed partial class RankingVegasWebView : DockableControl
    {
        public Vegas MyVegas;
        
        public TimeTracker GlobalTimeTracker { get; set; }
        public RankingConfig GlobalConfig { get; set; }
        public RankingApiClient GlobalApiClient { get; set; }
        
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private UserInfo currentUserInfo;
        private int currentUserRank = 0;
        private bool isInitialized = false;
        private bool isWebViewReady = false;
        private bool hasWebViewReady = false;
        private Timer delayedInitTimer;
        private LeaderboardGroupManager groupManager;

        public RankingVegasWebView()
            : base("RankingVegas")
        {
            InitializeComponent();
            PersistDockWindowState = true;
            
            groupManager = LeaderboardGroupManager.Load();
            
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

        protected override void OnLoaded(EventArgs args)
        {
            base.OnLoaded(args);
            
            if (RankingVegasCommand.Instance != null)
            {
                RankingVegasCommand.Instance.RegisterDockView(this);
            }
            
            InitializeWebView();
            InitializeFromGlobalState();
            
            if (!isInitialized)
            {
                delayedInitTimer.Start();
            }
        }
        
        private async void InitializeWebView()
        {
            try
            {
                if (!WebView2Detector.IsWebView2Available())
                {
                    throw new InvalidOperationException(Localization.Text("WebView2 运行时不可用", "WebView2 runtime is not available", "WebView2 ランタイムが利用できません"));
                }

                webView = new Microsoft.Web.WebView2.WinForms.WebView2();
                webView.Dock = DockStyle.Fill;
                this.Controls.Add(webView);

                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RankingVegas",
                    "WebView2");
                Directory.CreateDirectory(userDataFolder);

                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                if (environment == null)
                {
                    throw new InvalidOperationException(Localization.Text("无法创建 WebView2 环境", "Unable to create WebView2 environment", "WebView2 環境を作成できません"));
                }

                await webView.EnsureCoreWebView2Async(environment);
                
                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                string html = GetEmbeddedHtml();
                webView.NavigateToString(html);

                isWebViewReady = false;
                hasWebViewReady = false;
            }
            catch (Exception ex)
            {
                isWebViewReady = false;
                Label errorLabel = new Label
                {
                    Text = Localization.Format("WebView2 初始化失败:\n{0}\n\n请安装 WebView2 运行时", "WebView2 initialization failed:\n{0}\n\nPlease install the WebView2 runtime.", "WebView2 初期化失敗:\n{0}\n\nWebView2 ランタイムをインストールしてください。", ex.Message),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Red
                };
                this.Controls.Add(errorLabel);
            }
        }
        
        private string GetEmbeddedHtml()
        {
            try
            {
                byte[] htmlBytes = EmbeddedResourceHelper.ReadEmbeddedResource("index.html");
                return System.Text.Encoding.UTF8.GetString(htmlBytes);
            }
            catch
            {
            }
            
            return Localization.Text(
                "<!DOCTYPE html><html><body style='background: #667eea; color: white; text-align: center; padding: 40px; font-family: sans-serif;'><h1>无法加载前端页面</h1><p>请确保 index.html 已嵌入为资源</p></body></html>",
                "<!DOCTYPE html><html><body style='background: #667eea; color: white; text-align: center; padding: 40px; font-family: sans-serif;'><h1>Unable to load the UI</h1><p>Please ensure index.html is embedded as a resource.</p></body></html>",
                "<!DOCTYPE html><html><body style='background: #667eea; color: white; text-align: center; padding: 40px; font-family: sans-serif;'><h1>UIをロードできません</h1><p>index.htmlがリソースとして埋め込まれていることを確認してください。</p></body></html>");
        }
        
        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.WebMessageAsJson;
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = e.TryGetWebMessageAsString();
                }

                WebMessage message = null;
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        message = JsonConvert.DeserializeObject<WebMessage>(json);
                    }
                    catch
                    {
                        message = new WebMessage { Action = json.Trim('"') };
                    }
                }

                if (message == null)
                {
                    return;
                }

                if (InvokeRequired)
                {
                    Invoke(new Action(() => HandleWebMessage(message)));
                }
                else
                {
                    HandleWebMessage(message);
                }
            }
            catch
            {
            }
        }
        
        private void HandleWebMessage(WebMessage message)
        {
            switch (message.Action)
            {
                case "ready":
                    if (!isWebViewReady)
                    {
                        isWebViewReady = true;
                    }
                    OnWebViewReady();
                    break;
                case "bind":
                    HandleBind();
                    break;
                case "refresh":
                    HandleRefresh();
                    break;
                case "showLeaderboard":
                    HandleShowLeaderboard();
                    break;
                case "logout":
                    HandleLogout();
                    break;
                case "selectAvatar":
                    HandleSelectAvatar();
                    break;
                case "editNickname":
                    HandleEditNickname();
                    break;
                case "getSettings":
                    HandleGetSettings();
                    break;
                case "saveSettings":
                    HandleSaveSettings(message.Data);
                    break;
                case "changeLanguage":
                    HandleChangeLanguage(message.Data);
                    break;
                case "updateShowTimer":
                    HandleUpdateShowTimer(message.Data);
                    break;
                case "updateShowStatus":
                    HandleUpdateShowStatus(message.Data);
                    break;

                case "getGroups":
                    HandleGetGroups();
                    break;
                case "selectGroup":
                    HandleSelectGroup(message.Data);
                    break;
                case "addGroup":
                    HandleAddGroup(message.Data);
                    break;
                case "editGroup":
                    HandleEditGroup(message.Data);
                    break;
                case "deleteGroup":
                    HandleDeleteGroup(message.Data);
                    break;
                case "addUsersToGroup":
                    HandleAddUsersToGroup(message.Data);
                    break;
            }
        }
        
        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Reset ready flags so F5 refresh re-initializes everything
            isWebViewReady = false;
            hasWebViewReady = false;
        }
        
        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                return;
            }

            isWebViewReady = true;

            // Re-send all state on navigation completion
            if (GlobalConfig != null && GlobalConfig.IsConfigured() && !string.IsNullOrEmpty(GlobalConfig.SessionCode))
            {
                Task.Run(() =>
                {
                    try
                    {
                        System.Threading.Thread.Sleep(200);
                        
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() =>
                            {
                                LoadUserInfo();
                            }));
                        }
                        else
                        {
                            LoadUserInfo();
                        }
                    }
                    catch
                    {
                    }
                });
            }
            
            SendThemeColors();
            SendLocalization();
            SendAppProfile();
            SendConfiguredState();
            SendTimeUpdate();
            SendStatusUpdate();
            SendShowSettings();
            SendVegasInfo();
            UpdateOfflineState();
            SendGroups();
            LoadLeaderboard();
        }
        
        private void OnWebViewReady()
        {
            if (hasWebViewReady)
            {
                return;
            }

            hasWebViewReady = true;
            SendThemeColors();
            SendLocalization();
            SendAppProfile();
            SendConfiguredState();
            SendTimeUpdate();
            SendStatusUpdate();
            SendShowSettings();
            SendVegasInfo();
            UpdateOfflineState();
            SendGroups();
            LoadLeaderboard();
            
            if (GlobalConfig != null && GlobalConfig.IsConfigured() && !string.IsNullOrEmpty(GlobalConfig.SessionCode))
            {
                Task.Run(() =>
                {
                    try
                    {
                        System.Threading.Thread.Sleep(200);
                        
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() =>
                            {
                                LoadUserInfo();
                            }));
                        }
                        else
                        {
                            LoadUserInfo();
                        }
                    }
                    catch
                    {
                    }
                });
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
            
            SendThemeColors();
            SendConfiguredState();
            SendAppProfile();
            UpdateOfflineState();
            
            if (GlobalConfig != null && GlobalConfig.IsConfigured() && !string.IsNullOrEmpty(GlobalConfig.SessionCode))
            {
                Task.Run(() =>
                {
                    try
                    {
                        System.Threading.Thread.Sleep(100);
                        
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() =>
                            {
                                LoadUserInfo();
                            }));
                        }
                        else
                        {
                            LoadUserInfo();
                        }
                    }
                    catch
                    {
                    }
                });
            }
            else if (GlobalConfig != null && !GlobalConfig.IsConfigured())
            {
                SendStatus(Localization.Text("配置未完成", "Configuration incomplete", "設定未完了"), StatusKind.Error);
            }
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
                            if (InvokeRequired)
                            {
                                Invoke(new Action(() => LoadUserInfo()));
                            }
                            else
                            {
                                LoadUserInfo();
                            }
                        }
                        catch
                        {
                        }
                    });
                }
            }

            base.OnVisibleChanged(e);
        }
        
        protected override void InitLayout()
        {
            base.InitLayout();
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
            
            if (webView != null)
            {
                try
                {
                    webView.Dispose();
                }
                catch
                {
                }
                webView = null;
            }
            
            base.OnClosed(e);
        }
        
        private void HandleBind()
        {
            if (GlobalConfig == null || !GlobalConfig.IsConfigured())
            {
                MessageBox.Show(Localization.Text("配置未初始化，请检查 App ID 和 App Secret 是否正确配置", "Configuration is not initialized. Please check App ID and App Secret.", "設定が初期化されていません。App ID と App Secret を確認してください。"),
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
                        "浏览器已打开绑定页面\n\n请登录完成绑定\n点击【确定】后将自动刷新账号状态\n\n提示：若绑定页面返回\"签名验证失败\"，则说明在线排行榜的签名可能已经更换，您当前使用的 RankVegas 扩展的版本无法绑定到当前的在线排行榜。请尝试重新到您获得该分发版本的地方，下载并更新到最新版本。",
                        "Your browser has opened the binding page.\n\nPlease sign in to complete binding.\nClick OK to refresh the account status.\n\nNote: If the binding page returns 'Signature verification failed', the online leaderboard signature may have changed. Please download and update to the latest version.",
                        "ブラウザで連携ページが開きました。\n\nサインインして連携を完了してください。\nOKをクリックするとアカウント状態が更新されます。\n\n注：連携ページで「署名検証失敗」と表示された場合は、オンラインランキングの署名が変更された可能性があります。最新版をダウンロードして更新してください。"),
                    Localization.Text("绑定账号", "Bind Account", "アカウント連携"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                HandleRefresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localization.Format("无法打开浏览器: {0}\n\n请手动访问:\n{1}", "Unable to open the browser: {0}\n\nPlease open manually:\n{1}", "ブラウザを開けません: {0}\n\n手動で開いてください:\n{1}", ex.Message, bindUrl),
                    Localization.Text("错误", "Error", "エラー"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void HandleRefresh()
        {
            if (GlobalConfig == null || !GlobalConfig.IsConfigured())
            {
                return;
            }
            
            LoadUserInfo();
        }
        
        private void HandleShowLeaderboard()
        {
            LoadLeaderboard();
        }
        
        private void HandleLogout()
        {
            if (GlobalConfig == null)
                return;
            
            DialogResult result = MessageBox.Show(
                Localization.Text("确定要退出登录吗？退出后将切换为离线账号。", "Are you sure you want to log out? You will be switched to offline mode.", "ログアウトしますか？オフラインモードに切り替わります。"),
                Localization.Text("退出登录", "Logout", "ログアウト"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                GlobalConfig.SessionCode = RankingConfig.GenerateSessionCode();
                GlobalConfig.IsOfflineAccount = true;
                GlobalConfig.Save();
                
                currentUserInfo = null;
                currentUserRank = 0;
                
                if (GlobalTimeTracker != null)
                {
                    GlobalTimeTracker.SetOfflineMode(true);
                }
                
                UpdateOfflineState();
                SendUserInfo();
            }
        }
        
        private void HandleSelectAvatar()
        {
            if (GlobalConfig == null || !GlobalConfig.IsOfflineAccount)
                return;
            
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = Localization.Text("选择头像图片", "Select Avatar Image", "アバター画像を選択");
                openFileDialog.Filter = Localization.Text("图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|所有文件|*.*", "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All Files|*.*", "画像ファイル|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|すべてのファイル|*.*");
                openFileDialog.FilterIndex = 1;
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string targetPath = RankingConfig.GetOfflineAvatarFilePath();
                        
                        // Load using SkiaSharp to support WebP and other formats
                        using (Image sourceImage = ImageHelper.LoadImageFromFile(openFileDialog.FileName))
                        {
                            if (sourceImage == null)
                            {
                                MessageBox.Show(
                                    Localization.Text("无法加载该图片文件", "Unable to load the image file", "画像ファイルを読み込めません"),
                                    Localization.Text("错误", "Error", "エラー"),
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                                return;
                            }

                            using (Image resized = ResizeImageForAvatar(sourceImage, 120, 120))
                            {
                                resized.Save(targetPath, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                        
                        GlobalConfig.OfflineAvatarPath = targetPath;
                        GlobalConfig.Save();
                        
                        // Send avatar to WebView
                        SendOfflineAvatar();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            Localization.Format("保存头像失败: {0}", "Failed to save avatar: {0}", "アバターの保存に失敗しました: {0}", ex.Message),
                            Localization.Text("错误", "Error", "エラー"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void HandleEditNickname()
        {
            if (GlobalConfig == null || !GlobalConfig.IsOfflineAccount)
                return;
            
            string currentNickname = GlobalConfig.OfflineNickname ?? "";
            string input = ShowInputDialog(
                Localization.Text("输入昵称", "Enter Nickname", "ニックネームを入力"),
                Localization.Text("请输入您的离线账号昵称:", "Please enter your offline account nickname:", "オフラインアカウントのニックネームを入力してください:"),
                currentNickname);
            
            if (input != null)
            {
                GlobalConfig.OfflineNickname = input.Trim();
                GlobalConfig.Save();
                SendUserInfo();
            }
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
        
        private void HandleGetSettings()
        {
            if (GlobalConfig == null)
                return;

            string languageCode = "en";
            switch (GlobalConfig.Language)
            {
                case SupportedLanguage.Chinese:
                    languageCode = "zh";
                    break;
                case SupportedLanguage.Japanese:
                    languageCode = "ja";
                    break;
            }

            var settings = new
            {
                offlineNickname = GlobalConfig.OfflineNickname ?? "",
                offlineSaveInterval = GlobalConfig.OfflineSaveIntervalSeconds,
                onlineReportInterval = GlobalConfig.OnlineReportIntervalSeconds,
                showTimer = GlobalConfig.ShowTimer,
                showStatus = GlobalConfig.ShowStatus,
                showAccount = GlobalConfig.ShowAccount,
                showVegasInfo = GlobalConfig.ShowVegasInfo,
                language = languageCode,
                uiStyle = GlobalConfig.UIStyle == UIStyleOption.WinForms ? "winforms" : "webview"
            };

            string json = JsonConvert.SerializeObject(settings);
            ExecuteScript($"loadSettings({json})");
        }
        
        private void HandleSaveSettings(object data)
        {
            if (GlobalConfig == null || data == null)
                return;
            
            try
            {
                var settings = JsonConvert.DeserializeObject<SettingsData>(data.ToString());
                if (settings != null)
                {
                    bool languageChanged = false;
                    bool uiStyleChanged = false;
                    UIStyleOption newUIStyle = UIStyleOption.WebView;
                    
                    GlobalConfig.OfflineNickname = settings.OfflineNickname;
                    GlobalConfig.OfflineSaveIntervalSeconds = Math.Max(settings.OfflineSaveInterval, RankingConfig.MinOfflineSaveIntervalSeconds);
                    GlobalConfig.OnlineReportIntervalSeconds = Math.Max(settings.OnlineReportInterval, RankingConfig.MinOnlineReportIntervalSeconds);
                    GlobalConfig.ShowTimer = settings.ShowTimer;
                    GlobalConfig.ShowStatus = settings.ShowStatus;
                    GlobalConfig.ShowAccount = settings.ShowAccount;
                    GlobalConfig.ShowVegasInfo = settings.ShowVegasInfo;

                    if (!string.IsNullOrEmpty(settings.Language))
                    {
                        SupportedLanguage newLanguage = SupportedLanguage.English;
                        switch (settings.Language)
                        {
                            case "zh":
                                newLanguage = SupportedLanguage.Chinese;
                                break;
                            case "ja":
                                newLanguage = SupportedLanguage.Japanese;
                                break;
                            default:
                                newLanguage = SupportedLanguage.English;
                                break;
                        }
                        
                        if (GlobalConfig.Language != newLanguage)
                        {
                            languageChanged = true;
                            GlobalConfig.Language = newLanguage;
                            Localization.Language = newLanguage;
                        }
                    }

                    if (!string.IsNullOrEmpty(settings.UIStyle))
                    {
                        newUIStyle = settings.UIStyle == "winforms" ? UIStyleOption.WinForms : UIStyleOption.WebView;
                        if (GlobalConfig.UIStyle != newUIStyle)
                        {
                            uiStyleChanged = true;
                        }
                    }

                    GlobalConfig.Save();

                    if (GlobalTimeTracker != null)
                    {
                        GlobalTimeTracker.UpdateReportInterval();
                    }
                    
                    if (uiStyleChanged && RankingVegasCommand.Instance != null)
                    {
                        RankingVegasCommand.Instance.SwitchUIStyle(newUIStyle);
                        return;
                    }
                    
                    if (languageChanged)
                    {
                        if (GlobalTimeTracker != null)
                        {
                            GlobalTimeTracker.RefreshStatus();
                        }
                        
                        // Refresh the entire page to apply new language
                        if (webView != null && webView.CoreWebView2 != null)
                        {
                            webView.CoreWebView2.Reload();
                        }
                    }
                    else
                    {
                        SendUserInfo();
                        SendShowSettings();
                    }
                }
            }
            catch
            {
            }
        }
        
        private void HandleChangeLanguage(object data)
        {
            if (GlobalConfig == null || data == null)
                return;
            
            try
            {
                string languageCode = data.ToString().Trim('"').Trim().ToLowerInvariant();
                SupportedLanguage newLanguage = SupportedLanguage.English;
                
                switch (languageCode)
                {
                    case "zh":
                        newLanguage = SupportedLanguage.Chinese;
                        break;
                    case "ja":
                        newLanguage = SupportedLanguage.Japanese;
                        break;
                    case "en":
                    default:
                        newLanguage = SupportedLanguage.English;
                        break;
                }
                
                if (GlobalConfig.Language != newLanguage)
                {
                    GlobalConfig.Language = newLanguage;
                    Localization.Language = newLanguage;
                    GlobalConfig.Save();
                    
                    if (GlobalTimeTracker != null)
                    {
                        GlobalTimeTracker.RefreshStatus();
                    }
                    
                    // Refresh the entire page to apply new language
                    if (webView != null && webView.CoreWebView2 != null)
                    {
                        webView.CoreWebView2.Reload();
                    }
                }
            }
            catch
            {
            }
        }
        
        private void HandleUpdateShowTimer(object data)
        {
            if (GlobalConfig == null) return;
            bool show = true;
            if (data is bool) show = (bool)data;
            else if (data != null) bool.TryParse(data.ToString(), out show);
            GlobalConfig.ShowTimer = show;
            GlobalConfig.Save();
        }

        private void HandleUpdateShowStatus(object data)
        {
            if (GlobalConfig == null) return;
            bool show = true;
            if (data is bool) show = (bool)data;
            else if (data != null) bool.TryParse(data.ToString(), out show);
            GlobalConfig.ShowStatus = show;
            GlobalConfig.Save();
        }

        private void SendShowSettings()
        {
            if (!isWebViewReady || GlobalConfig == null) return;
            ExecuteScript($"updateShowSettings({GlobalConfig.ShowTimer.ToString().ToLower()}, {GlobalConfig.ShowStatus.ToString().ToLower()}, {GlobalConfig.ShowAccount.ToString().ToLower()}, {GlobalConfig.ShowVegasInfo.ToString().ToLower()})");
        }

        private void SendLocalization()
        {
            if (!isWebViewReady)
                return;

            string languageCode = "en";
            SupportedLanguage lang = Localization.Language;
            if (GlobalConfig != null) lang = GlobalConfig.Language;
            switch (lang)
            {
                case SupportedLanguage.Chinese:
                    languageCode = "zh";
                    break;
                case SupportedLanguage.Japanese:
                    languageCode = "ja";
                    break;
            }

            bool rendering = GlobalTimeTracker != null && GlobalTimeTracker.IsRendering();

            var strings = new
            {
                title = !string.IsNullOrEmpty(RankingAppProfile.AppDisplayName) ? RankingAppProfile.AppDisplayName : Localization.Text("排行", "Ranking", "ランキング"),
                timeLabel = Localization.GetTimerLabel(rendering),
                waitingSync = Localization.Text("等待同步", "Waiting for sync", "同期待ち"),
                notBound = Localization.Text("未绑定账号", "Account not bound", "アカウント未連携"),
                totalDurationPrefix = Localization.Text("总时长: ", "Total Duration: ", "合計時間: "),
                rankPrefix = Localization.Text("排名: ", "Rank: ", "ランキング: "),
                bindAccount = Localization.Text("绑定", "Bind", "連携"),
                rebindAccount = Localization.Text("重绑", "Rebind", "再連携"),
                leaderboardTitle = Localization.Text("📊 排行榜", "📊 Leaderboard", "📊 ランキング"),
                leaderboardButton = Localization.Text("📊 排行榜", "📊 Leaderboard", "📊 ランキング"),
                leaderboardBack = Localization.Text("返回", "Back", "戻る"),
                refresh = Localization.Text("刷新", "Refresh", "更新"),
                loading = Localization.Text("加载中...", "Loading...", "読み込み中..."),
                noData = Localization.Text("暂无数据", "No data", "データなし"),
                unknownUser = Localization.Text("未知用户", "Unknown user", "不明なユーザー"),
                notConfigured = Localization.Text("配置未完成", "Configuration incomplete", "設定未完了"),
                offlineAccount = Localization.Text("离线账号", "Offline Account", "オフラインアカウント"),
                hoursUnit = Localization.Text("小时", "hrs", "時間"),
                minutesUnit = Localization.Text("分钟", "mins", "分"),
                secondsUnit = Localization.Text("秒", "secs", "秒"),
                logout = Localization.Text("退出", "Logout", "ログアウト"),
                settings = Localization.Text("设置", "Settings", "設定"),
                offlineNickname = Localization.Text("离线账号昵称:", "Offline Nickname:", "オフラインニックネーム:"),
                offlineNicknamePlaceholder = Localization.Text("输入昵称", "Enter nickname", "ニックネームを入力"),
                offlineSaveInterval = Localization.Text("离线保存间隔 (秒):", "Offline Save Interval (seconds):", "オフライン保存間隔 (秒):"),
                onlineReportInterval = Localization.Text("在线上报间隔 (秒):", "Online Report Interval (seconds):", "オンライン報告間隔 (秒):"),
                showVegasInfo = Localization.Text("显示 Vegas 信息", "Show Vegas Info", "Vegas 情報を表示"),
                showTimer = Localization.Text("显示计时器", "Show Timer", "タイマーを表示"),
                showStatusBar = Localization.Text("显示状态栏", "Show Status Bar", "ステータスバーを表示"),
                showAccount = Localization.Text("显示账号", "Show Account", "アカウントを表示"),
                save = Localization.Text("保存", "Save", "保存"),
                cancel = Localization.Text("取消", "Cancel", "キャンセル"),
                language = Localization.Text("语言:", "Language:", "言語:"),
                uiStyle = Localization.Text("UI 风格:", "UI Style:", "UI スタイル:"),
                manageGroups = Localization.Text("管理分组", "Manage Groups", "グループ管理"),
                total = Localization.Text("总榜", "Total", "総合"),
                whitelist = Localization.Text("白名单", "Whitelist", "ホワイトリスト"),
                blacklist = Localization.Text("黑名单", "Blacklist", "ブラックリスト"),
                groupName = Localization.Text("分组名称:", "Group Name:", "グループ名:"),
                groupUserIds = Localization.Text("用户 ID (逗号分隔):", "User IDs (comma separated):", "ユーザーID (カンマ区切り):"),
                newGroup = Localization.Text("新建分组", "New Group", "新規グループ"),
                editGroup = Localization.Text("编辑分组", "Edit Group", "グループ編集"),
                addToGroup = Localization.Text("添加到分组", "Add to Group", "グループに追加"),
                deleteGroup = Localization.Text("删除", "Delete", "削除"),
                whitelistPrefix = Localization.Text("[白]", "[W]", "[白]"),
                blacklistPrefix = Localization.Text("[黑]", "[B]", "[黒]"),
                bindButtonText = Localization.Text("绑定", "Bind", "連携"),
                languageCode = languageCode,
                fontFamily = Localization.CssFontFamily
            };

            string json = JsonConvert.SerializeObject(strings);
            ExecuteScript($"applyLocalization({json})");
            ExecuteScript($"if(document.getElementById('mainLangSelect')){{document.getElementById('mainLangSelect').value='{EscapeJs(languageCode)}';}}");
            ExecuteScript($"if(document.getElementById('bindButtonText')){{document.getElementById('bindButtonText').textContent='{EscapeJs(Localization.Text("绑定", "Bind", "連携"))}';}}");
            ExecuteScript($"if(document.getElementById('logoutButtonText')){{document.getElementById('logoutButtonText').textContent='{EscapeJs(Localization.Text("退出", "Logout", "ログアウト"))}';}}");
        }
        
        private void SendLeaderboard(LeaderboardData leaderboardData)
        {
            if (!isWebViewReady || leaderboardData == null)
                return;

            try
            {
                var leaderboardList = new System.Collections.Generic.List<object>();

                foreach (var entry in leaderboardData.Leaderboard)
                {
                    string avatarSrc;
                    if (!string.IsNullOrEmpty(entry.Avatar) && entry.Avatar.StartsWith("data:"))
                    {
                        // Already a data URL (e.g. from demo mode), use directly
                        avatarSrc = entry.Avatar;
                    }
                    else
                    {
                        string avatarUrl = ImageHelper.NormalizeAvatarUrl(entry.Avatar);
                        string cachedDataUrl = ImageHelper.GetCachedAvatarAsDataUrl(entry.Avatar);
                        avatarSrc = cachedDataUrl ?? avatarUrl;
                    }

                    leaderboardList.Add(new
                    {
                        userId = entry.UserId,
                        nickname = entry.Nickname,
                        avatar = avatarSrc,
                        totalDuration = entry.TotalDuration,
                        rank = entry.Rank
                    });

                    if (currentUserInfo != null && entry.UserId == currentUserInfo.UserId)
                    {
                        currentUserRank = entry.Rank;
                    }
                }

                string json = JsonConvert.SerializeObject(leaderboardList);
                ExecuteScript($"updateLeaderboard({json})");

                // Also send groups so the frontend can filter
                SendGroups();

                if (currentUserRank > 0)
                {
                    SendUserInfo();
                }
            }
            catch
            {
            }
        }
        
        private void SendGroups()
        {
            if (!isWebViewReady)
                return;

            var groupList = new System.Collections.Generic.List<object>();
            if (groupManager.Groups != null)
            {
                foreach (var g in groupManager.Groups)
                {
                    groupList.Add(new
                    {
                        id = g.Id,
                        name = g.Name,
                        isWhitelist = g.IsWhitelist,
                        userIds = g.UserIds ?? new System.Collections.Generic.List<int>()
                    });
                }
            }

            var data = new
            {
                groups = groupList,
                selectedGroupId = groupManager.SelectedGroupId ?? LeaderboardGroupManager.TotalGroupId
            };

            string json = JsonConvert.SerializeObject(data);
            ExecuteScript($"updateGroups({json})");
            ExecuteScript("if(typeof renderGroupList==='function')renderGroupList()");
        }

        private Image ResizeImageForAvatar(Image image, int maxWidth, int maxHeight)
        {
            double ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
            Bitmap resized = new Bitmap((int)(image.Width * ratio), (int)(image.Height * ratio));
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(image, 0, 0, resized.Width, resized.Height);
            }
            return resized;
        }

        private void SendOfflineAvatar()
        {
            if (!isWebViewReady) return;
            string avatarPath = RankingConfig.GetOfflineAvatarFilePath();
            if (File.Exists(avatarPath))
            {
                try { ExecuteScript($"setOfflineAvatar('data:image/png;base64,{EscapeJs(Convert.ToBase64String(File.ReadAllBytes(avatarPath)))}')"); }
                catch { SendDefaultAvatar(); }
            }
            else SendDefaultAvatar();
        }

        private void SendDefaultAvatar()
        {
            try { ExecuteScript($"setOfflineAvatar('data:image/jpeg;base64,{EscapeJs(Convert.ToBase64String(EmbeddedResourceHelper.ReadEmbeddedResource("default_avatar.jpg")))}')"); }
            catch { ExecuteScript("setOfflineAvatar('')"); }
        }

        private void SetOfflineMode(bool isOffline)
        {
            if (GlobalConfig == null) return;
            if (GlobalConfig.IsOfflineAccount != isOffline) { GlobalConfig.IsOfflineAccount = isOffline; GlobalConfig.Save(); }
            if (GlobalTimeTracker != null) GlobalTimeTracker.SetOfflineMode(isOffline);
            UpdateOfflineState();
            if (isOffline) SendOfflineAvatar();
        }

        private void TimeTracker_TimeUpdated(object sender, TimeSpan time)
        {
            if (InvokeRequired) { try { Invoke(new Action(() => TimeTracker_TimeUpdated(sender, time))); } catch { } return; }
            SendTimeUpdate();
            if (GlobalConfig != null && GlobalConfig.IsOfflineAccount) SendUserInfo();
        }

        private void TimeTracker_StatusChanged(object sender, string status)
        {
            if (InvokeRequired) { try { Invoke(new Action(() => TimeTracker_StatusChanged(sender, status))); } catch { } return; }
            SendStatusUpdate();
        }

        private void LoadUserInfo()
        {
            if (GlobalConfig == null || GlobalApiClient == null || string.IsNullOrEmpty(GlobalConfig.SessionCode))
            { SetOfflineMode(true); currentUserInfo = null; SendUserInfo(); LoadLeaderboard(); return; }
            Task.Run(() => {
                try {
                    var response = GlobalApiClient.GetUserInfo(GlobalConfig.SessionCode);
                    Action handler = () => {
                        if (response.Success && response.Data != null) { currentUserInfo = response.Data; SetOfflineMode(false); }
                        else { currentUserInfo = null; SetOfflineMode(true); }
                        SendUserInfo(); LoadLeaderboard();
                    };
                    if (InvokeRequired) Invoke(handler); else handler();
                } catch {
                    Action errHandler = () => { currentUserInfo = null; SetOfflineMode(true); SendUserInfo(); LoadLeaderboard(); };
                    if (InvokeRequired) { try { Invoke(errHandler); } catch { } } else errHandler();
                }
            });
        }

        private void LoadLeaderboard()
        {
            if (RankingAppProfile.IsDemo && GlobalApiClient == null)
            {
                SendLeaderboard(CreateDemoLeaderboardData(10));
                return;
            }

            Task.Run(() => {
                try {
                    var response = GlobalApiClient.GetLeaderboard(50, 0);
                    Action handler = () => {
                        if (response.Success && response.Data != null)
                        {
                            SendLeaderboard(response.Data);
                        }
                        else
                        {
                            if (RankingAppProfile.IsDemo)
                            {
                                SendLeaderboard(CreateDemoLeaderboardData(10));
                            }
                            else
                            {
                                ExecuteScript($"showError('{EscapeJs(Localization.Format("获取排行榜失败: {0}", "Failed to get leaderboard: {0}", "ランキングの取得に失敗しました: {0}", response.Message))}')");
                            }
                        }
                    };
                    if (InvokeRequired) Invoke(handler); else handler();
                }
                catch (Exception ex) {
                    Action errHandler;
                    if (RankingAppProfile.IsDemo)
                    {
                        errHandler = () => SendLeaderboard(CreateDemoLeaderboardData(10));
                    }
                    else
                    {
                        errHandler = () => ExecuteScript($"showError('{EscapeJs(Localization.Format("加载排行榜异常: {0}", "Leaderboard load error: {0}", "ランキングの読み込みエラー: {0}", ex.Message))}')");
                    }
                    if (InvokeRequired) { try { Invoke(errHandler); } catch { } } else errHandler();
                }
            });
        }

        private LeaderboardData CreateDemoLeaderboardData(int count)
        {
            var random = new Random();
            int[] demoIds = new[] { 1111, 2222, 3333, 4444, 5555, 6666, 7777, 8888, 9999, 114514 };
            var entries = new LeaderboardEntry[count];
            
            string defaultAvatarDataUrl = "";
            try
            {
                byte[] avatarBytes = EmbeddedResourceHelper.ReadEmbeddedResource("default_avatar.jpg");
                defaultAvatarDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(avatarBytes)}";
            }
            catch { }
            
            for (int i = 0; i < count; i++)
            {
                entries[i] = new LeaderboardEntry
                {
                    UserId = demoIds[i % demoIds.Length],
                    Nickname = $"DemoUser{i + 1}",
                    Avatar = defaultAvatarDataUrl,
                    TotalDuration = random.Next(300, 200000),
                    Rank = i + 1
                };
            }

            var sorted = entries.OrderByDescending(e => e.TotalDuration).ToArray();
            for (int i = 0; i < sorted.Length; i++)
            {
                sorted[i].Rank = i + 1;
            }

            return new LeaderboardData
            {
                AppId = 0,
                AppName = RankingAppProfile.LeaderboardName,
                IsVerified = false,
                Leaderboard = sorted
            };
        }

        private void SendTimeUpdate()
        {
            if (!isWebViewReady || GlobalTimeTracker == null) return;
            bool rendering = GlobalTimeTracker.IsRendering();
            TimeSpan time = rendering ? GlobalTimeTracker.GetRenderTime() : GlobalTimeTracker.GetTotalTime();
            ExecuteScript($"updateTime({(int)time.TotalHours}, {time.Minutes}, {time.Seconds})");
            ExecuteScript($"updateTimeLabel('{EscapeJs(Localization.GetTimerLabel(rendering))}')");
        }

        private void SendStatusUpdate()
        {
            if (!isWebViewReady) return;
            if (GlobalTimeTracker == null) { SendStatus(Localization.Text("等待同步", "Waiting for sync", "同期待ち"), StatusKind.Active); return; }
            SendStatus(GlobalTimeTracker.GetCurrentStatus(), GlobalTimeTracker.GetCurrentStatusKind());
        }

        private void SendConfiguredState()
        {
            if (!isWebViewReady) return;
            ExecuteScript($"updateConfiguredState({(RankingAppProfile.IsDemo || (GlobalConfig != null && GlobalConfig.IsConfigured())).ToString().ToLower()})");
        }

        private void SendThemeColors()
        {
            if (!isWebViewReady) return;
            Color bg = RankingVegasCommand.UIBackColor, txt = RankingVegasCommand.UIForeColor;
            ExecuteScript($"applyTheme('" +
                EscapeJs(ColorTranslator.ToHtml(bg)) + "','" +
                EscapeJs(ColorTranslator.ToHtml(txt)) + "','" +
                EscapeJs(ColorTranslator.ToHtml(ControlPaint.Dark(bg, 0.1f))) + "','" +
                EscapeJs(ColorTranslator.ToHtml(ControlPaint.Light(bg, 0.02f))) + "')");
        }

        private void SendUserInfo()
        {
            if (!isWebViewReady) return;
            if (GlobalConfig != null && GlobalConfig.IsOfflineAccount)
            {
                var info = new { userId = 0, nickname = GlobalConfig.GetOfflineDisplayName(), avatar = string.Empty, totalDuration = GetOfflineDurationSeconds(), rank = 0, isOffline = true };
                ExecuteScript($"updateUserInfo({JsonConvert.SerializeObject(info)})");
                ExecuteScript($"if(document.getElementById('logoutButtonText')){{document.getElementById('logoutButtonText').textContent='{EscapeJs(Localization.Text("退出", "Logout", "ログアウト"))}';}}");
                UpdateAppTitle();
                SendOfflineAvatar();
                return;
            }
            if (currentUserInfo == null)
            {
                ExecuteScript("updateUserInfo(null)");
                UpdateAppTitle();
                return;
            }
            string avatarUrl = ImageHelper.NormalizeAvatarUrl(currentUserInfo.Avatar);
            // Use cached avatar as data URL if available
            string cachedDataUrl = ImageHelper.GetCachedAvatarAsDataUrl(currentUserInfo.Avatar);
            string avatarSrc = cachedDataUrl ?? avatarUrl;
            var ui = new { userId = currentUserInfo.UserId, nickname = currentUserInfo.Nickname, avatar = avatarSrc, totalDuration = currentUserInfo.TotalDuration, rank = currentUserRank, isOffline = false };
            ExecuteScript($"updateUserInfo({JsonConvert.SerializeObject(ui)})");
            ExecuteScript($"if(document.getElementById('logoutButtonText')){{document.getElementById('logoutButtonText').textContent='{EscapeJs(Localization.Text("退出", "Logout", "ログアウト"))}';}}");
            UpdateAppTitle();
        }

        private void ExecuteScript(string script)
        {
            if (webView == null || !isWebViewReady) return;
            try { if (InvokeRequired) Invoke(new Action(() => { try { webView.ExecuteScriptAsync(script); } catch { } })); else webView.ExecuteScriptAsync(script); } catch { }
        }

        private string EscapeJs(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private void SendStatus(string status, StatusKind kind) { ExecuteScript($"updateStatus('{EscapeJs(status)}', '{GetStatusType(kind)}')"); }

        private string GetStatusType(StatusKind k) { switch (k) { case StatusKind.Idle: return "idle"; case StatusKind.Rendering: return "rendering"; case StatusKind.Error: return "error"; default: return "active"; } }

        private void SendAppProfile()
        {
            if (!isWebViewReady) return;
            ExecuteScript($"applyAppProfile({JsonConvert.SerializeObject(new { avatarOrigin = RankingAppProfile.ApiOrigin, leaderboardName = RankingAppProfile.LeaderboardName })})"); UpdateAppTitle();
        }

        private void SendVegasInfo()
        {
            if (!isWebViewReady) return;
            string version = RankingVegasCommand.VegasVersion ?? "";
            string iconBase64 = RankingVegasCommand.VegasIconBase64 ?? "";
            string iconDataUrl = !string.IsNullOrEmpty(iconBase64) ? $"data:image/png;base64,{iconBase64}" : "";
            ExecuteScript($"updateVegasInfo('{EscapeJs(version)}', '{EscapeJs(iconDataUrl)}')");
        }

        private void UpdateAppTitle()
        {
            if (!isWebViewReady) return;
            bool offline = GlobalConfig != null && GlobalConfig.IsOfflineAccount;
            string title = RankingAppProfile.GetDisplayName(offline);
            DisplayName = title; ExecuteScript($"updateAppTitle('{EscapeJs(title)}')");
        }

        private void UpdateOfflineState()
        {
            if (!isWebViewReady) return;
            bool offline = GlobalConfig != null && GlobalConfig.IsOfflineAccount;
            ExecuteScript($"updateOfflineState({offline.ToString().ToLower()})"); UpdateAppTitle();
        }

        private int GetOfflineDurationSeconds() { return GlobalTimeTracker != null ? GlobalTimeTracker.GetOfflineTotalDurationSeconds() : 0; }

        #region Group Management
        private void HandleGetGroups() { SendGroups(); }

        private void HandleSelectGroup(object data)
        {
            if (data == null) return;
            string gid = data.ToString().Trim('"');
            groupManager.SelectedGroupId = gid; groupManager.Save();
            ExecuteScript($"onGroupSelected('{EscapeJs(gid)}')");
        }

        private void HandleAddGroup(object data)
        {
            if (data == null) return;
            try {
                var gd = JsonConvert.DeserializeObject<GroupData>(data.ToString());
                if (gd != null && !string.IsNullOrWhiteSpace(gd.Name))
                {
                    var ng = new LeaderboardGroup(gd.Name.Trim(), gd.IsWhitelist);
                    if (!string.IsNullOrWhiteSpace(gd.UserIdsString)) ng.SetUserIdsFromString(gd.UserIdsString);
                    groupManager.AddGroup(ng); groupManager.Save(); SendGroups();
                }
            } catch { }
        }

        private void HandleEditGroup(object data)
        {
            if (data == null) return;
            try {
                var gd = JsonConvert.DeserializeObject<GroupData>(data.ToString());
                if (gd != null && !string.IsNullOrEmpty(gd.Id)) {
                    var g = groupManager.GetGroup(gd.Id);
                    if (g != null) {
                        if (!string.IsNullOrWhiteSpace(gd.Name)) g.Name = gd.Name.Trim();
                        g.IsWhitelist = gd.IsWhitelist;
                        if (gd.UserIdsString != null) g.SetUserIdsFromString(gd.UserIdsString);
                        groupManager.Save(); SendGroups();
                    }
                }
            } catch { }
        }

        private void HandleDeleteGroup(object data)
        {
            if (data == null) return;
            groupManager.RemoveGroup(data.ToString().Trim('"')); groupManager.Save(); SendGroups();
        }

        private void HandleAddUsersToGroup(object data)
        {
            if (data == null) return;
            try {
                var ad = JsonConvert.DeserializeObject<AddUsersToGroupData>(data.ToString());
                if (ad != null && !string.IsNullOrEmpty(ad.GroupId) && ad.UserIds != null) {
                    var g = groupManager.GetGroup(ad.GroupId);
                    if (g != null) {
                        int cnt = 0;
                        foreach (int uid in ad.UserIds) { if (!g.ContainsUser(uid)) { g.AddUser(uid); cnt++; } }
                        if (cnt > 0) { groupManager.Save(); SendGroups(); }
                    }
                }
            } catch { }
        }
        #endregion
    }

    public class WebMessage
    {
        [JsonProperty("action")] public string Action { get; set; }
        [JsonProperty("data")] public object Data { get; set; }
    }

    public class SettingsData
    {
        [JsonProperty("offlineNickname")] public string OfflineNickname { get; set; }
        [JsonProperty("offlineSaveInterval")] public int OfflineSaveInterval { get; set; }
        [JsonProperty("onlineReportInterval")] public int OnlineReportInterval { get; set; }
        [JsonProperty("showTimer")] public bool ShowTimer { get; set; }
        [JsonProperty("showStatus")] public bool ShowStatus { get; set; }
        [JsonProperty("showAccount")] public bool ShowAccount { get; set; }
        [JsonProperty("showVegasInfo")] public bool ShowVegasInfo { get; set; }
        [JsonProperty("language")] public string Language { get; set; }
        [JsonProperty("uiStyle")] public string UIStyle { get; set; }
    }

    public class GroupData
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("isWhitelist")] public bool IsWhitelist { get; set; }
        [JsonProperty("userIdsString")] public string UserIdsString { get; set; }
    }

    public class AddUsersToGroupData
    {
        [JsonProperty("groupId")] public string GroupId { get; set; }
        [JsonProperty("userIds")] public int[] UserIds { get; set; }
    }
}
