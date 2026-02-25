using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace RankingVegas
{
    /// <summary>
    /// Form for managing leaderboard groups.
    /// </summary>
    public class ManageGroupsForm : Form
    {
        private LeaderboardGroupManager groupManager;
        private Action onGroupsChanged;

        private ListBox lstGroups;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnClose;
        private Label lblInfo;

        public ManageGroupsForm(LeaderboardGroupManager groupManager, Action onGroupsChanged)
        {
            this.groupManager = groupManager;
            this.onGroupsChanged = onGroupsChanged;

            InitializeComponents();
            LoadGroups();
        }

        private void InitializeComponents()
        {
            this.Text = Localization.Text("管理分组", "Manage Groups", "グループ管理");
            this.Width = 400;
            this.Height = 350;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = RankingVegasCommand.UIBackColor;
            this.ForeColor = RankingVegasCommand.UIForeColor;
            this.Font = new Font("Microsoft Yahei UI", 9F);

            lblInfo = new Label
            {
                Text = Localization.Text(
                    "创建分组来筛选排行榜。白名单组只显示组内成员，黑名单组排除组内成员。",
                    "Create groups to filter the leaderboard. Whitelist groups show only members, blacklist groups exclude members.",
                    "グループを作成してランキングをフィルタリングします。ホワイトリストはメンバーのみ表示、ブラックリストはメンバーを除外します。"),
                Location = new Point(12, 12),
                Size = new Size(360, 40),
                ForeColor = ControlPaint.Light(RankingVegasCommand.UIForeColor, 0.2f)
            };
            this.Controls.Add(lblInfo);

            lstGroups = new ListBox
            {
                Location = new Point(12, 55),
                Size = new Size(270, 200),
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            lstGroups.SelectedIndexChanged += LstGroups_SelectedIndexChanged;
            lstGroups.DoubleClick += LstGroups_DoubleClick;
            this.Controls.Add(lstGroups);

            int buttonX = 295;
            int buttonWidth = 85;

            btnAdd = new Button
            {
                Text = Localization.Text("新建", "Add", "追加"),
                Location = new Point(buttonX, 55),
                Size = new Size(buttonWidth, 28),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);

            btnEdit = new Button
            {
                Text = Localization.Text("编辑", "Edit", "編集"),
                Location = new Point(buttonX, 90),
                Size = new Size(buttonWidth, 28),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnEdit.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            btnEdit.Click += BtnEdit_Click;
            this.Controls.Add(btnEdit);

            btnDelete = new Button
            {
                Text = Localization.Text("删除", "Delete", "削除"),
                Location = new Point(buttonX, 125),
                Size = new Size(buttonWidth, 28),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnDelete.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);

            btnClose = new Button
            {
                Text = Localization.Text("关闭", "Close", "閉じる"),
                Location = new Point(295, 270),
                Size = new Size(85, 30),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void LoadGroups()
        {
            lstGroups.Items.Clear();

            if (groupManager.Groups != null)
            {
                foreach (var group in groupManager.Groups)
                {
                    string typeText = group.IsWhitelist
                        ? Localization.Text("[白]", "[W]", "[白]")
                        : Localization.Text("[黑]", "[B]", "[黒]");
                    lstGroups.Items.Add(new GroupListItem(group, $"{typeText} {group.Name} ({group.UserIds?.Count ?? 0})"));
                }
            }

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = lstGroups.SelectedItem != null;
            btnEdit.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;
        }

        private void LstGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void LstGroups_DoubleClick(object sender, EventArgs e)
        {
            if (lstGroups.SelectedItem != null)
            {
                BtnEdit_Click(sender, e);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var newGroup = new LeaderboardGroup(
                Localization.Text("新分组", "New Group", "新規グループ"),
                true);

            using (var editForm = new EditGroupForm(newGroup, true))
            {
                if (editForm.ShowDialog(this) == DialogResult.OK)
                {
                    groupManager.AddGroup(newGroup);
                    groupManager.Save();
                    LoadGroups();
                    onGroupsChanged?.Invoke();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (lstGroups.SelectedItem is GroupListItem item)
            {
                using (var editForm = new EditGroupForm(item.Group, false))
                {
                    if (editForm.ShowDialog(this) == DialogResult.OK)
                    {
                        groupManager.Save();
                        LoadGroups();
                        onGroupsChanged?.Invoke();
                    }
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lstGroups.SelectedItem is GroupListItem item)
            {
                var result = MessageBox.Show(
                    Localization.Format("确定要删除分组 \"{0}\" 吗？", "Delete group \"{0}\"?", "グループ \"{0}\" を削除しますか？", item.Group.Name),
                    Localization.Text("确认删除", "Confirm Delete", "削除確認"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    groupManager.RemoveGroup(item.Group.Id);
                    groupManager.Save();
                    LoadGroups();
                    onGroupsChanged?.Invoke();
                }
            }
        }

        private class GroupListItem
        {
            public LeaderboardGroup Group { get; }
            public string DisplayText { get; }

            public GroupListItem(LeaderboardGroup group, string displayText)
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

    /// <summary>
    /// Form for editing a single group.
    /// </summary>
    public class EditGroupForm : Form
    {
        private LeaderboardGroup group;

        private Label lblName;
        private TextBox txtName;
        private Label lblType;
        private RadioButton rbWhitelist;
        private RadioButton rbBlacklist;
        private Label lblUserIds;
        private TextBox txtUserIds;
        private Label lblUserIdsHint;
        private Button btnSave;
        private Button btnCancel;

        public EditGroupForm(LeaderboardGroup group, bool isNew)
        {
            this.group = group;

            InitializeComponents(isNew);
            LoadGroupData();
        }

        private void InitializeComponents(bool isNew)
        {
            this.Text = isNew
                ? Localization.Text("新建分组", "New Group", "新規グループ")
                : Localization.Text("编辑分组", "Edit Group", "グループ編集");
            this.Width = 400;
            this.Height = 320;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = RankingVegasCommand.UIBackColor;
            this.ForeColor = RankingVegasCommand.UIForeColor;
            this.Font = new Font("Microsoft Yahei UI", 9F);

            int y = 15;
            int labelWidth = 100;
            int controlLeft = 115;
            int controlWidth = 250;

            lblName = new Label
            {
                Text = Localization.Text("分组名称:", "Group Name:", "グループ名:"),
                Location = new Point(15, y + 3),
                Size = new Size(labelWidth, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            this.Controls.Add(lblName);

            txtName = new TextBox
            {
                Location = new Point(controlLeft, y),
                Size = new Size(controlWidth, 24),
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtName);
            y += 35;

            lblType = new Label
            {
                Text = Localization.Text("分组类型:", "Group Type:", "グループタイプ:"),
                Location = new Point(15, y + 3),
                Size = new Size(labelWidth, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            this.Controls.Add(lblType);

            rbWhitelist = new RadioButton
            {
                Text = Localization.Text("白名单 (仅显示组内)", "Whitelist (show only)", "ホワイトリスト (のみ表示)"),
                Location = new Point(controlLeft, y),
                Size = new Size(controlWidth, 22),
                ForeColor = RankingVegasCommand.UIForeColor,
                Checked = true
            };
            this.Controls.Add(rbWhitelist);
            y += 25;

            rbBlacklist = new RadioButton
            {
                Text = Localization.Text("黑名单 (从总榜排除)", "Blacklist (exclude)", "ブラックリスト (除外)"),
                Location = new Point(controlLeft, y),
                Size = new Size(controlWidth, 22),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            this.Controls.Add(rbBlacklist);
            y += 35;

            lblUserIds = new Label
            {
                Text = Localization.Text("用户 ID:", "User IDs:", "ユーザーID:"),
                Location = new Point(15, y + 3),
                Size = new Size(labelWidth, 20),
                ForeColor = RankingVegasCommand.UIForeColor
            };
            this.Controls.Add(lblUserIds);

            txtUserIds = new TextBox
            {
                Location = new Point(controlLeft, y),
                Size = new Size(controlWidth, 80),
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                ForeColor = RankingVegasCommand.UIForeColor,
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            this.Controls.Add(txtUserIds);
            y += 85;

            lblUserIdsHint = new Label
            {
                Text = Localization.Text(
                    "输入用户 ID，用逗号、空格或换行分隔",
                    "Enter user IDs separated by commas, spaces, or newlines",
                    "ユーザーIDをカンマ、スペース、改行で区切って入力"),
                Location = new Point(controlLeft, y),
                Size = new Size(controlWidth, 18),
                ForeColor = ControlPaint.Light(RankingVegasCommand.UIForeColor, 0.3f),
                Font = new Font("Microsoft Yahei UI", 8F)
            };
            this.Controls.Add(lblUserIdsHint);
            y += 30;

            btnSave = new Button
            {
                Text = Localization.Text("保存", "Save", "保存"),
                Location = new Point(195, y),
                Size = new Size(80, 30),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            btnSave.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text = Localization.Text("取消", "Cancel", "キャンセル"),
                Location = new Point(285, y),
                Size = new Size(80, 30),
                ForeColor = RankingVegasCommand.UIForeColor,
                BackColor = ControlPaint.Light(RankingVegasCommand.UIBackColor, 0.05f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = ControlPaint.Dark(RankingVegasCommand.UIBackColor, 0.2f);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void LoadGroupData()
        {
            txtName.Text = group.Name ?? "";
            rbWhitelist.Checked = group.IsWhitelist;
            rbBlacklist.Checked = !group.IsWhitelist;
            txtUserIds.Text = group.GetUserIdsString();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(
                    Localization.Text("请输入分组名称", "Please enter a group name", "グループ名を入力してください"),
                    Localization.Text("错误", "Error", "エラー"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            group.Name = name;
            group.IsWhitelist = rbWhitelist.Checked;
            group.SetUserIdsFromString(txtUserIds.Text);
        }
    }
}
