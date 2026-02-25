using System;
using System.Drawing;
using System.Windows.Forms;

namespace RankingVegas
{
    public class SettingsForm : Form
    {
        private RankingConfig config;
        private TimeTracker timeTracker;
        private Action onSettingsChanged;
        
        private Label lblLanguage;
        private ComboBox cboLanguage;
        private Label lblUIStyle;
        private ComboBox cboUIStyle;
        private Label lblOfflineNickname;
        private TextBox txtOfflineNickname;
        private Label lblOfflineSaveInterval;
        private NumericUpDown nudOfflineSaveInterval;
        private Label lblOnlineReportInterval;
        private NumericUpDown nudOnlineReportInterval;
        private CheckBox chkShowTimer;
        private CheckBox chkShowStatus;
        private CheckBox chkShowAccount;
        private CheckBox chkShowVegasInfo;
        private Button btnSave;
        private Button btnCancel;
        
        public SettingsForm(RankingConfig config, TimeTracker timeTracker, Action onSettingsChanged)
        {
            this.config = config;
            this.timeTracker = timeTracker;
            this.onSettingsChanged = onSettingsChanged;
            
            InitializeComponents();
            LoadSettings();
        }
        
        private void InitializeComponents()
        {
            this.Text = Localization.Text("设置", "Settings", "設定");
            this.Width = 400;
            this.Height = 400;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = RankingVegasCommand.UIBackColor;
            this.ForeColor = RankingVegasCommand.UIForeColor;
            this.Font = new Font(Localization.FontFamily, 9F);
            
            int y = 20;
            int labelWidth = 160;
            int controlLeft = 180;
            int controlWidth = 180;
            
            // Language selection
            lblLanguage = new Label
            {
                Text = Localization.Text("语言:", "Language:", "言語:"),
                Location = new Point(20, y + 3),
                Size = new Size(labelWidth, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            
            cboLanguage = new ComboBox
            {
                Location = new Point(controlLeft, y),
                Size = new Size(controlWidth, 24),
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cboLanguage.Items.Add(Localization.GetLanguageDisplayName(SupportedLanguage.English));
            cboLanguage.Items.Add(Localization.GetLanguageDisplayName(SupportedLanguage.Chinese));
            cboLanguage.Items.Add(Localization.GetLanguageDisplayName(SupportedLanguage.Japanese));
            cboLanguage.SelectedIndexChanged += CboLanguage_SelectedIndexChanged;
            
            this.Controls.Add(lblLanguage);
            this.Controls.Add(cboLanguage);
            y += 35;
            
            // UI Style selection
            lblUIStyle = new Label
            {
                Text = Localization.Text("UI 风格:", "UI Style:", "UI スタイル:"),
                Location = new Point(20, y + 3),
                Size = new Size(labelWidth, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            
            cboUIStyle = new ComboBox
            {
                Location = new Point(controlLeft, y),
                Size = new Size(controlWidth, 24),
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cboUIStyle.Items.Add("WebView");
            cboUIStyle.Items.Add("WinForms");
            
            this.Controls.Add(lblUIStyle);
            this.Controls.Add(cboUIStyle);
            y += 35;
            
            // Offline Nickname (only for offline accounts)
            lblOfflineNickname = new Label
            {
                Text = Localization.Text("离线账号昵称:", "Offline Nickname:", "オフラインニックネーム:"),
                Location = new Point(20, y + 3),
                Size = new Size(labelWidth, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            
            txtOfflineNickname = new TextBox
            {
                Location = new Point(controlLeft, y),
                Size = new Size(controlWidth, 24),
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            this.Controls.Add(lblOfflineNickname);
            this.Controls.Add(txtOfflineNickname);
            y += 35;
            
            // Offline Save Interval
            lblOfflineSaveInterval = new Label
            {
                Text = Localization.Text("离线保存间隔 (秒):", "Offline Save Interval (seconds):", "オフライン保存間隔 (秒):"),
                Location = new Point(20, y + 3),
                Size = new Size(labelWidth, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            
            nudOfflineSaveInterval = new NumericUpDown
            {
                Location = new Point(controlLeft, y),
                Size = new Size(100, 24),
                Minimum = RankingConfig.MinOfflineSaveIntervalSeconds,
                Maximum = 3600,
                Value = RankingConfig.DefaultOfflineSaveIntervalSeconds,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            
            this.Controls.Add(lblOfflineSaveInterval);
            this.Controls.Add(nudOfflineSaveInterval);
            y += 35;
            
            // Online Report Interval
            lblOnlineReportInterval = new Label
            {
                Text = Localization.Text("在线上报间隔 (秒):", "Online Report Interval (seconds):", "オンライン報告間隔 (秒):"),
                Location = new Point(20, y + 3),
                Size = new Size(labelWidth, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            
            nudOnlineReportInterval = new NumericUpDown
            {
                Location = new Point(controlLeft, y),
                Size = new Size(100, 24),
                Minimum = RankingConfig.MinOnlineReportIntervalSeconds,
                Maximum = 3600,
                Value = RankingConfig.DefaultOnlineReportIntervalSeconds,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            
            this.Controls.Add(lblOnlineReportInterval);
            this.Controls.Add(nudOnlineReportInterval);
            y += 35;
            
            // Separator
            Panel separator = new Panel
            {
                Location = new Point(20, y + 5),
                Size = new Size(340, 1),
                BackColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.1f)
            };
            this.Controls.Add(separator);
            y += 20;

            // Arrange the four "show" checkboxes in a 2x2 grid to save vertical space
            int leftMargin = 20;
            int rightMargin = 20;
            int gap = 12;
            int chkRowHeight = 30;

            // compute column widths based on client area to avoid clipping
            int availableWidth = Math.Max(260, this.ClientSize.Width - leftMargin - rightMargin);
            int colWidth = Math.Max(120, (availableWidth - gap) / 2);
            int chkLeftCol1 = leftMargin;
            int chkLeftCol2 = chkLeftCol1 + colWidth + gap;

            // Show Vegas Info (top-left)
            chkShowVegasInfo = new CheckBox
            {
                Text = Localization.Text("显示 Vegas 信息", "Show Vegas Info", "Vegas 情報を表示"),
                Location = new Point(chkLeftCol1, y),
                Size = new Size(colWidth, 24),
                ForeColor = RankingVegasCommand.UIForeColor,
                AutoSize = false
            };
            this.Controls.Add(chkShowVegasInfo);

            // Show Timer (top-right)
            chkShowTimer = new CheckBox
            {
                Text = Localization.Text("显示计时器", "Show Timer", "タイマーを表示"),
                Location = new Point(chkLeftCol2, y),
                Size = new Size(colWidth, 24),
                ForeColor = RankingVegasCommand.UIForeColor,
                AutoSize = false
            };
            this.Controls.Add(chkShowTimer);

            // Show Status (bottom-left)
            chkShowStatus = new CheckBox
            {
                Text = Localization.Text("显示状态栏", "Show Status Bar", "ステータスバーを表示"),
                Location = new Point(chkLeftCol1, y + chkRowHeight),
                Size = new Size(colWidth, 24),
                ForeColor = RankingVegasCommand.UIForeColor,
                AutoSize = false
            };
            this.Controls.Add(chkShowStatus);

            // Show Account (bottom-right)
            chkShowAccount = new CheckBox
            {
                Text = Localization.Text("显示账号", "Show Account", "アカウントを表示"),
                Location = new Point(chkLeftCol2, y + chkRowHeight),
                Size = new Size(colWidth, 24),
                ForeColor = RankingVegasCommand.UIForeColor,
                AutoSize = false
            };
            this.Controls.Add(chkShowAccount);

            // Move Y position down to make space for buttons
            y += (chkRowHeight * 2) + 15; // two rows + spacing

            // Buttons
            btnSave = new Button
            {
                Text = Localization.Text("保存", "Save", "保存"),
                Location = new Point(180, y),
                Size = new Size(80, 30),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            btnSave.Click += BtnSave_Click;
            
            btnCancel = new Button
            {
                Text = Localization.Text("取消", "Cancel", "キャンセル"),
                Location = new Point(270, y),
                Size = new Size(80, 30),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            btnCancel.Click += (s, e) => this.Close();
            
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
            
            UpdateControlVisibility();
        }
        
        private void LoadSettings()
        {
            if (config == null)
                return;
            
            // Set language combo box
            switch (config.Language)
            {
                case SupportedLanguage.Chinese:
                    cboLanguage.SelectedIndex = 1;
                    break;
                case SupportedLanguage.Japanese:
                    cboLanguage.SelectedIndex = 2;
                    break;
                default:
                    cboLanguage.SelectedIndex = 0;
                    break;
            }
            
            // Set UI style combo box
            cboUIStyle.SelectedIndex = config.UIStyle == UIStyleOption.WinForms ? 1 : 0;
            
            txtOfflineNickname.Text = config.OfflineNickname ?? "";
            nudOfflineSaveInterval.Value = Math.Max(config.OfflineSaveIntervalSeconds, RankingConfig.MinOfflineSaveIntervalSeconds);
            nudOnlineReportInterval.Value = Math.Max(config.OnlineReportIntervalSeconds, RankingConfig.MinOnlineReportIntervalSeconds);
            chkShowTimer.Checked = config.ShowTimer;
            chkShowStatus.Checked = config.ShowStatus;
            chkShowAccount.Checked = config.ShowAccount;
            chkShowVegasInfo.Checked = config.ShowVegasInfo;
        }
        
        private void UpdateControlVisibility()
        {
            bool isOffline = config != null && config.IsOfflineAccount;
            
            // Offline nickname only visible for offline accounts
            lblOfflineNickname.Visible = isOffline;
            txtOfflineNickname.Visible = isOffline;
            
            // Offline save interval only visible for offline accounts
            lblOfflineSaveInterval.Visible = isOffline;
            nudOfflineSaveInterval.Visible = isOffline;
            
            // Online report interval only visible for online accounts
            lblOnlineReportInterval.Visible = !isOffline;
            nudOnlineReportInterval.Visible = !isOffline;
        }
        
        private void CboLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (config == null)
                return;
            
            SupportedLanguage selectedLanguage = GetSelectedLanguage();
            
            if (config.Language != selectedLanguage)
            {
                config.Language = selectedLanguage;
                Localization.Language = selectedLanguage;
                config.Save();
                
                // Update all UI text in this form
                UpdateLocalizedTexts();
                
                // Notify caller to update their UI
                try
                {
                    onSettingsChanged?.Invoke();
                }
                catch
                {
                }
            }
        }
        
        private SupportedLanguage GetSelectedLanguage()
        {
            switch (cboLanguage.SelectedIndex)
            {
                case 1:
                    return SupportedLanguage.Chinese;
                case 2:
                    return SupportedLanguage.Japanese;
                default:
                    return SupportedLanguage.English;
            }
        }
        
        private void UpdateLocalizedTexts()
        {
            // Update form font for current language
            this.Font = new Font(Localization.FontFamily, 9F);

            // Update form title
            this.Text = Localization.Text("设置", "Settings", "設定");
            
            // Update labels
            lblLanguage.Text = Localization.Text("语言:", "Language:", "言語:");
            lblUIStyle.Text = Localization.Text("UI 风格:", "UI Style:", "UI スタイル:");
            lblOfflineNickname.Text = Localization.Text("离线账号昵称:", "Offline Nickname:", "オフラインニックネーム:");
            lblOfflineSaveInterval.Text = Localization.Text("离线保存间隔 (秒):", "Offline Save Interval (seconds):", "オフライン保存間隔 (秒):");
            lblOnlineReportInterval.Text = Localization.Text("在线上报间隔 (秒):", "Online Report Interval (seconds):", "オンライン報告間隔 (秒):");
            chkShowTimer.Text = Localization.Text("显示计时器", "Show Timer", "タイマーを表示");
            chkShowStatus.Text = Localization.Text("显示状态栏", "Show Status Bar", "ステータスバーを表示");
            chkShowAccount.Text = Localization.Text("显示账号", "Show Account", "アカウントを表示");
            chkShowVegasInfo.Text = Localization.Text("显示 Vegas 信息", "Show Vegas Info", "Vegas 情報を表示");
            btnSave.Text = Localization.Text("保存", "Save", "保存");
            btnCancel.Text = Localization.Text("取消", "Cancel", "キャンセル");
        }
        
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (config == null)
            {
                this.Close();
                return;
            }
            
            // Language is already saved in SelectedIndexChanged
            
            // Check if UI style changed
            UIStyleOption selectedUIStyle = cboUIStyle.SelectedIndex == 1 ? UIStyleOption.WinForms : UIStyleOption.WebView;
            bool uiStyleChanged = config.UIStyle != selectedUIStyle;
            
            // Save settings
            config.OfflineNickname = txtOfflineNickname.Text.Trim();
            config.OfflineSaveIntervalSeconds = (int)nudOfflineSaveInterval.Value;
            config.OnlineReportIntervalSeconds = (int)nudOnlineReportInterval.Value;
            config.ShowTimer = chkShowTimer.Checked;
            config.ShowStatus = chkShowStatus.Checked;
            config.ShowAccount = chkShowAccount.Checked;
            config.ShowVegasInfo = chkShowVegasInfo.Checked;
            
            config.Save();

            // Update time tracker interval
            if (timeTracker != null)
            {
                timeTracker.UpdateReportInterval();
            }
            
            // Close the form first before notifying caller
            this.Close();
            
            // Notify caller (after form is closed to avoid accessing disposed controls)
            try
            {
                onSettingsChanged?.Invoke();
            }
            catch
            {
            }
            
            // Switch UI style after settings changed callback
            if (uiStyleChanged && RankingVegasCommand.Instance != null)
            {
                RankingVegasCommand.Instance.SwitchUIStyle(selectedUIStyle);
            }
        }
    }
}
