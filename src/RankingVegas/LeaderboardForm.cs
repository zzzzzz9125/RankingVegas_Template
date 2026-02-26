using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace RankingVegas
{
    public class LeaderboardForm : Form
    {
        private RankingConfig config;
        private RankingApiClient apiClient;
        private UserInfo currentUserInfo;
        private int currentUserRank;
        private LeaderboardGroupManager groupManager;

        private ListView listViewLeaderboard;
        private Button btnPreviousPage;
        private Button btnNextPage;
        private Button btnClose;
        private Label lblPageInfo;
        private Panel pnlHeader;
        private Panel pnlFooter;
        private ImageList rowHeightImageList;
        private ComboBox cboGroup;
        private Button btnManageGroups;
        private Button btnAddToGroup;
        private Label lblGroupFilter;

        private int currentPage = 0;
        private const int PAGE_SIZE = 50;
        private int totalEntries = 0;
        private LeaderboardData currentLeaderboard;
        private LeaderboardEntry[] originalEntries;

        public LeaderboardForm(RankingConfig config, RankingApiClient apiClient, UserInfo currentUserInfo, int currentUserRank)
        {
            this.config = config;
            this.apiClient = apiClient;
            this.currentUserInfo = currentUserInfo;
            this.currentUserRank = currentUserRank;
            this.groupManager = LeaderboardGroupManager.Load();

            InitializeComponents();
            LoadLeaderboard();
        }

        private void InitializeComponents()
        {
            this.Text = RankingAppProfile.LeaderboardName ?? "Leaderboard";
            this.Width = 550;
            this.Height = 650;
            this.ShowIcon = false;
            this.MinimumSize = new Size(550, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = RankingVegasCommand.UIBackColor;
            this.ForeColor = RankingVegasCommand.UIForeColor;
            this.Font = new Font(Localization.FontFamily, 9F);

            // Header panel with group filter
            pnlHeader = new Panel
            {
                Height = 45,
                Dock = DockStyle.Top,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10, 8, 10, 8)
            };

            lblGroupFilter = new Label
            {
                Text = Localization.Text("分组:", "Group:", "グループ:"),
                Location = new Point(12, 12),
                Size = new Size(50, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            pnlHeader.Controls.Add(lblGroupFilter);

            cboGroup = new ComboBox
            {
                Location = new Point(65, 9),
                Size = new Size(180, 24),
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cboGroup.SelectedIndexChanged += CboGroup_SelectedIndexChanged;
            pnlHeader.Controls.Add(cboGroup);

            btnManageGroups = new Button
            {
                Text = Localization.Text("管理", "Manage", "管理"),
                Location = new Point(255, 8),
                Size = new Size(60, 26),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnManageGroups.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            btnManageGroups.Click += BtnManageGroups_Click;
            pnlHeader.Controls.Add(btnManageGroups);

            btnAddToGroup = new Button
            {
                Text = Localization.Text("添加选中到分组", "Add Selected to Group", "選択をグループに追加"),
                Location = new Point(325, 8),
                Size = new Size(130, 26),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAddToGroup.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            btnAddToGroup.Click += BtnAddToGroup_Click;
            pnlHeader.Controls.Add(btnAddToGroup);

            // ListView
            listViewLeaderboard = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HideSelection = false,
                MultiSelect = true,
                BorderStyle = BorderStyle.None,
                BackColor = RankingVegasCommand.UIBackColor,
                ForeColor = RankingVegasCommand.UIForeColor,
                Font = new Font(Localization.FontFamily, 9F)
            };

            rowHeightImageList = new ImageList
            {
                ImageSize = new Size(1, 28)
            };
            listViewLeaderboard.SmallImageList = rowHeightImageList;

            listViewLeaderboard.Columns.Add(Localization.Text("排行", "Rank", "順位"), 70);
            listViewLeaderboard.Columns.Add(Localization.Text("昵称", "Nickname", "ニックネーム"), 220);
            listViewLeaderboard.Columns.Add(Localization.Text("时长", "Duration", "時間"), 100, HorizontalAlignment.Right);
            listViewLeaderboard.Columns.Add("ID", 60, HorizontalAlignment.Right);
            listViewLeaderboard.Resize += (s, e) => UpdateColumnWidths();
            listViewLeaderboard.SelectedIndexChanged += ListViewLeaderboard_SelectedIndexChanged;

            // Don't add listViewLeaderboard here yet - will add after footer for correct dock order
            UpdateColumnWidths();

            // Footer
            pnlFooter = new Panel
            {
                Height = 56,
                Dock = DockStyle.Bottom,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                BorderStyle = BorderStyle.FixedSingle
            };

            TableLayoutPanel footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(12, 10, 12, 10)
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            FlowLayoutPanel navigationPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnPreviousPage = new Button
            {
                Text = Localization.Text("上一页", "Previous", "前へ"),
                Width = 90,
                Height = 28
            };
            btnPreviousPage.Click += BtnPreviousPage_Click;

            btnNextPage = new Button
            {
                Text = Localization.Text("下一页", "Next", "次へ"),
                Width = 90,
                Height = 28
            };
            btnNextPage.Click += BtnNextPage_Click;

            navigationPanel.Controls.Add(btnPreviousPage);
            navigationPanel.Controls.Add(btnNextPage);

            lblPageInfo = new Label
            {
                Text = "Page 0",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = RankingVegasCommand.UIForeColor
            };

            btnClose = new Button
            {
                Text = Localization.Text("关闭", "Close", "閉じる"),
                Width = 90,
                Height = 28
            };
            btnClose.Click += (s, e) => this.Close();

            footerLayout.Controls.Add(navigationPanel, 0, 0);
            footerLayout.Controls.Add(lblPageInfo, 1, 0);
            footerLayout.Controls.Add(btnClose, 2, 0);

            pnlFooter.Controls.Add(footerLayout);

            // WinForms dock layout order: last added docks first (highest priority)
            // Add ListView (Fill) first, then edge panels, so edges dock before Fill
            this.Controls.Add(listViewLeaderboard);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);

            // Load group combo box
            RefreshGroupComboBox();
        }

        private void RefreshGroupComboBox()
        {
            cboGroup.Items.Clear();

            // Add "Total" option
            cboGroup.Items.Add(new GroupComboItem(null, Localization.Text("总榜", "Total", "総合")));

            // Add user groups
            if (groupManager.Groups != null)
            {
                foreach (var group in groupManager.Groups)
                {
                    string typePrefix = group.IsWhitelist
                        ? Localization.Text("[白]", "[W]", "[白]")
                        : Localization.Text("[黑]", "[B]", "[黒]");
                    cboGroup.Items.Add(new GroupComboItem(group, $"{typePrefix} {group.Name}"));
                }
            }

            // Select current group
            int selectedIndex = 0;
            if (groupManager.SelectedGroupId != LeaderboardGroupManager.TotalGroupId)
            {
                for (int i = 1; i < cboGroup.Items.Count; i++)
                {
                    if (cboGroup.Items[i] is GroupComboItem item && item.Group?.Id == groupManager.SelectedGroupId)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            cboGroup.SelectedIndex = selectedIndex;
        }

        private void CboGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboGroup.SelectedItem is GroupComboItem item)
            {
                groupManager.SelectedGroupId = item.Group?.Id ?? LeaderboardGroupManager.TotalGroupId;
                groupManager.Save();

                // Refresh display with current data
                if (originalEntries != null)
                {
                    UpdateLeaderboardUI();
                }
            }
        }

        private void ListViewLeaderboard_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAddToGroup.Enabled = listViewLeaderboard.SelectedItems.Count > 0 &&
                                   groupManager.Groups != null &&
                                   groupManager.Groups.Count > 0;
        }

        private void BtnManageGroups_Click(object sender, EventArgs e)
        {
            using (var form = new ManageGroupsForm(groupManager, () => RefreshGroupComboBox()))
            {
                form.ShowDialog(this);
            }
            RefreshGroupComboBox();

            // Refresh display
            if (originalEntries != null)
            {
                UpdateLeaderboardUI();
            }
        }

        private void BtnAddToGroup_Click(object sender, EventArgs e)
        {
            if (listViewLeaderboard.SelectedItems.Count == 0)
                return;

            if (groupManager.Groups == null || groupManager.Groups.Count == 0)
            {
                MessageBox.Show(
                    Localization.Text("请先创建一个分组", "Please create a group first", "まずグループを作成してください"),
                    Localization.Text("提示", "Info", "情報"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Show group selection context menu
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.BackColor = RankingVegasCommand.UIBackColor;
            menu.ForeColor = RankingVegasCommand.UIForeColor;

            foreach (var group in groupManager.Groups)
            {
                string typePrefix = group.IsWhitelist
                    ? Localization.Text("[白]", "[W]", "[白]")
                    : Localization.Text("[黑]", "[B]", "[黒]");

                ToolStripMenuItem item = new ToolStripMenuItem($"{typePrefix} {group.Name}");
                item.Tag = group;
                item.Click += AddToGroupMenuItem_Click;
                menu.Items.Add(item);
            }

            menu.Show(btnAddToGroup, new Point(0, btnAddToGroup.Height));
        }

        private void AddToGroupMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is LeaderboardGroup group)
            {
                int addedCount = 0;
                foreach (ListViewItem item in listViewLeaderboard.SelectedItems)
                {
                    if (item.Tag is int userId)
                    {
                        if (!group.ContainsUser(userId))
                        {
                            group.AddUser(userId);
                            addedCount++;
                        }
                    }
                }

                if (addedCount > 0)
                {
                    groupManager.Save();
                    MessageBox.Show(
                        Localization.Format("已添加 {0} 名用户到分组 \"{1}\"", "Added {0} users to group \"{1}\"", "{0} 人のユーザーをグループ \"{1}\" に追加しました", addedCount, group.Name),
                        Localization.Text("成功", "Success", "成功"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Refresh display if current filter is affected
                    if (groupManager.SelectedGroupId == group.Id)
                    {
                        UpdateLeaderboardUI();
                    }
                }
            }
        }

        private void BtnPreviousPage_Click(object sender, EventArgs e)
        {
            if (currentPage > 0)
            {
                currentPage--;
                LoadLeaderboard();
            }
        }

        private void BtnNextPage_Click(object sender, EventArgs e)
        {
            int maxPage = (totalEntries + PAGE_SIZE - 1) / PAGE_SIZE;
            if (currentPage < maxPage - 1)
            {
                currentPage++;
                LoadLeaderboard();
            }
        }

        private void LoadLeaderboard()
        {
            btnPreviousPage.Enabled = false;
            btnNextPage.Enabled = false;
            lblPageInfo.Text = Localization.Text("加载中...", "Loading...", "読み込み中...");

            Task.Run(() =>
            {
                try
                {
                    if (RankingAppProfile.IsDemo && apiClient == null)
                    {
                        currentPage = 0;
                        currentLeaderboard = CreateDemoLeaderboardData(10);
                        originalEntries = currentLeaderboard.Leaderboard;
                        totalEntries = originalEntries?.Length ?? 0;
                        SafeInvokeUpdateLeaderboard();
                        return;
                    }

                    int offset = currentPage * PAGE_SIZE;
                    var response = apiClient.GetLeaderboard(PAGE_SIZE, offset);

                    if (response.Success && response.Data != null)
                    {
                        currentLeaderboard = response.Data;
                        originalEntries = response.Data.Leaderboard;
                        totalEntries = response.Data.Leaderboard?.Length ?? 0;

                        if (InvokeRequired)
                        {
                            Invoke(new Action(() => UpdateLeaderboardUI()));
                        }
                        else
                        {
                            UpdateLeaderboardUI();
                        }
                    }
                    else
                    {
                        if (RankingAppProfile.IsDemo)
                        {
                            currentPage = 0;
                            currentLeaderboard = CreateDemoLeaderboardData(10);
                            originalEntries = currentLeaderboard.Leaderboard;
                            totalEntries = originalEntries?.Length ?? 0;
                            SafeInvokeUpdateLeaderboard();
                        }
                        else
                        {
                            MessageBox.Show(Localization.Format("获取排行榜失败: {0}", "Failed to get leaderboard: {0}", "ランキングの取得に失敗しました: {0}", response.Message),
                                Localization.Text("错误", "Error", "エラー"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (RankingAppProfile.IsDemo)
                    {
                        currentPage = 0;
                        currentLeaderboard = CreateDemoLeaderboardData(10);
                        originalEntries = currentLeaderboard.Leaderboard;
                        totalEntries = originalEntries?.Length ?? 0;
                        SafeInvokeUpdateLeaderboard();
                    }
                    else
                    {
                        MessageBox.Show(Localization.Format("加载排行榜异常: {0}", "Leaderboard load error: {0}", "ランキングの読み込みエラー: {0}", ex.Message),
                            Localization.Text("错误", "Error", "エラー"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            btnPreviousPage.Enabled = (currentPage > 0);
                            int maxPage = (totalEntries + PAGE_SIZE - 1) / PAGE_SIZE;
                            btnNextPage.Enabled = (currentPage < maxPage - 1);
                        }));
                    }
                    else
                    {
                        btnPreviousPage.Enabled = (currentPage > 0);
                        int maxPage = (totalEntries + PAGE_SIZE - 1) / PAGE_SIZE;
                        btnNextPage.Enabled = (currentPage < maxPage - 1);
                    }
                }
            });
        }

        private void SafeInvokeUpdateLeaderboard()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateLeaderboardUI()));
            }
            else
            {
                UpdateLeaderboardUI();
            }
        }

        private LeaderboardData CreateDemoLeaderboardData(int count)
        {
            var random = new Random();
            int[] demoIds = new[] { 1111, 2222, 3333, 4444, 5555, 6666, 7777, 8888, 9999, 114514 };
            var entries = new LeaderboardEntry[count];
            for (int i = 0; i < count; i++)
            {
                entries[i] = new LeaderboardEntry
                {
                    UserId = demoIds[i % demoIds.Length],
                    Nickname = $"DemoUser{i + 1}",
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

        private void UpdateLeaderboardUI()
        {
            listViewLeaderboard.BeginUpdate();
            listViewLeaderboard.Items.Clear();

            // Apply group filter
            var entries = groupManager.FilterLeaderboard(originalEntries);

            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    ListViewItem item = new ListViewItem(GetRankEmoji(entry.Rank) + " " + entry.Rank);
                    item.SubItems.Add(entry.Nickname);
                    item.SubItems.Add(FormatDuration(entry.TotalDuration));
                    item.SubItems.Add(entry.UserId.ToString());
                    item.Tag = entry.UserId;

                    if (currentUserInfo != null && entry.UserId == currentUserInfo.UserId)
                    {
                        item.BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.1f);
                        item.ForeColor = RankingVegasCommand.UIForeColor;
                        item.Font = new Font(listViewLeaderboard.Font, FontStyle.Bold);
                        currentUserRank = entry.Rank;
                    }

                    listViewLeaderboard.Items.Add(item);
                }
            }

            listViewLeaderboard.EndUpdate();
            UpdatePageInfo();
        }

        private void UpdatePageInfo()
        {
            var selectedGroup = groupManager.GetSelectedGroup();
            string groupName = selectedGroup != null ? selectedGroup.Name : Localization.Text("总榜", "Total", "総合");

            int displayCount = listViewLeaderboard.Items.Count;
            int maxPage = Math.Max(1, (totalEntries + PAGE_SIZE - 1) / PAGE_SIZE);

            lblPageInfo.Text = Localization.Format("{0} - 第 {1}/{2} 页 ({3})", "{0} - Page {1}/{2} ({3})", "{0} - {1}/{2} ページ ({3})",
                groupName, currentPage + 1, maxPage, displayCount);
        }

        private void UpdateColumnWidths()
        {
            if (listViewLeaderboard.Columns.Count < 4)
            {
                return;
            }

            int rankWidth = 70;
            int durationWidth = 100;
            int idWidth = 60;
            int availableWidth = listViewLeaderboard.ClientSize.Width - rankWidth - durationWidth - idWidth - 6;
            if (availableWidth < 140)
            {
                availableWidth = 140;
            }

            listViewLeaderboard.Columns[0].Width = rankWidth;
            listViewLeaderboard.Columns[1].Width = availableWidth;
            listViewLeaderboard.Columns[2].Width = durationWidth;
            listViewLeaderboard.Columns[3].Width = idWidth;
        }

        private string GetRankEmoji(int rank)
        {
            switch (rank)
            {
                case 1: return "🥇";
                case 2: return "🥈";
                case 3: return "🥉";
                default: return rank <= 10 ? "🏅" : "⭐";
            }
        }

        private string FormatDuration(int seconds)
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

        private class GroupComboItem
        {
            public LeaderboardGroup Group { get; }
            public string DisplayText { get; }

            public GroupComboItem(LeaderboardGroup group, string displayText)
            {
                Group = group;
                DisplayText = displayText;
            }

            public override string ToString()
            {
                return DisplayText;
            }
        }
    }
}
