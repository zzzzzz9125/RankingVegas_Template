using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RankingVegas
{
    /// <summary>
    /// Represents a leaderboard group that can filter users as whitelist or blacklist.
    /// </summary>
    public class LeaderboardGroup
    {
        /// <summary>
        /// Unique identifier for this group.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Display name for this group.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// If true, this is a whitelist group (only show members).
        /// If false, this is a blacklist group (exclude members from total).
        /// </summary>
        [JsonProperty("isWhitelist")]
        public bool IsWhitelist { get; set; }

        /// <summary>
        /// List of user IDs in this group.
        /// </summary>
        [JsonProperty("userIds")]
        public List<int> UserIds { get; set; }

        public LeaderboardGroup()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            Name = "";
            IsWhitelist = true;
            UserIds = new List<int>();
        }

        public LeaderboardGroup(string name, bool isWhitelist) : this()
        {
            Name = name;
            IsWhitelist = isWhitelist;
        }

        /// <summary>
        /// Check if a user is in this group.
        /// </summary>
        public bool ContainsUser(int userId)
        {
            return UserIds != null && UserIds.Contains(userId);
        }

        /// <summary>
        /// Add a user to this group.
        /// </summary>
        public void AddUser(int userId)
        {
            if (UserIds == null)
            {
                UserIds = new List<int>();
            }
            if (!UserIds.Contains(userId))
            {
                UserIds.Add(userId);
            }
        }

        /// <summary>
        /// Remove a user from this group.
        /// </summary>
        public void RemoveUser(int userId)
        {
            if (UserIds != null)
            {
                UserIds.Remove(userId);
            }
        }

        /// <summary>
        /// Get display text for the group type.
        /// </summary>
        public string GetTypeDisplayText()
        {
            if (IsWhitelist)
            {
                return Localization.Text("白名单", "Whitelist", "ホワイトリスト");
            }
            else
            {
                return Localization.Text("黑名单", "Blacklist", "ブラックリスト");
            }
        }

        /// <summary>
        /// Get the user IDs as a comma-separated string for editing.
        /// </summary>
        public string GetUserIdsString()
        {
            if (UserIds == null || UserIds.Count == 0)
            {
                return "";
            }
            return string.Join(", ", UserIds);
        }

        /// <summary>
        /// Set user IDs from a comma-separated string.
        /// </summary>
        public void SetUserIdsFromString(string idsString)
        {
            UserIds = new List<int>();
            if (string.IsNullOrWhiteSpace(idsString))
            {
                return;
            }

            string[] parts = idsString.Split(new[] { ',', ';', ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (int.TryParse(part.Trim(), out int id))
                {
                    if (!UserIds.Contains(id))
                    {
                        UserIds.Add(id);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Manages leaderboard groups storage and operations.
    /// </summary>
    public class LeaderboardGroupManager
    {
        private const string GroupsFileName = "LeaderboardGroups.json";

        /// <summary>
        /// Special ID representing the total (unfiltered) leaderboard.
        /// </summary>
        public const string TotalGroupId = "__total__";

        /// <summary>
        /// List of user-defined groups.
        /// </summary>
        [JsonProperty("groups")]
        public List<LeaderboardGroup> Groups { get; set; }

        /// <summary>
        /// Currently selected group ID. Use TotalGroupId for total leaderboard.
        /// </summary>
        [JsonProperty("selectedGroupId")]
        public string SelectedGroupId { get; set; }

        private static string GroupsFilePath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folderPath = System.IO.Path.Combine(appDataPath, "RankingVegas");

                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                return System.IO.Path.Combine(folderPath, GroupsFileName);
            }
        }

        public LeaderboardGroupManager()
        {
            Groups = new List<LeaderboardGroup>();
            SelectedGroupId = TotalGroupId;
        }

        /// <summary>
        /// Load groups from file.
        /// </summary>
        public static LeaderboardGroupManager Load()
        {
            try
            {
                if (System.IO.File.Exists(GroupsFilePath))
                {
                    string json = System.IO.File.ReadAllText(GroupsFilePath);
                    var manager = JsonConvert.DeserializeObject<LeaderboardGroupManager>(json);
                    if (manager != null)
                    {
                        return manager;
                    }
                }
            }
            catch
            {
            }

            return new LeaderboardGroupManager();
        }

        /// <summary>
        /// Save groups to file.
        /// </summary>
        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                System.IO.File.WriteAllText(GroupsFilePath, json);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Get a group by ID.
        /// </summary>
        public LeaderboardGroup GetGroup(string id)
        {
            if (string.IsNullOrEmpty(id) || id == TotalGroupId)
            {
                return null;
            }

            return Groups?.Find(g => g.Id == id);
        }

        /// <summary>
        /// Get the currently selected group, or null for total leaderboard.
        /// </summary>
        public LeaderboardGroup GetSelectedGroup()
        {
            return GetGroup(SelectedGroupId);
        }

        /// <summary>
        /// Add a new group.
        /// </summary>
        public void AddGroup(LeaderboardGroup group)
        {
            if (Groups == null)
            {
                Groups = new List<LeaderboardGroup>();
            }
            Groups.Add(group);
        }

        /// <summary>
        /// Remove a group by ID.
        /// </summary>
        public void RemoveGroup(string id)
        {
            if (Groups != null)
            {
                Groups.RemoveAll(g => g.Id == id);
            }

            // Reset selection if current group was removed
            if (SelectedGroupId == id)
            {
                SelectedGroupId = TotalGroupId;
            }
        }

        /// <summary>
        /// Filter leaderboard entries based on the selected group.
        /// Returns filtered and re-ranked entries.
        /// </summary>
        public LeaderboardEntry[] FilterLeaderboard(LeaderboardEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                return entries;
            }

            var group = GetSelectedGroup();
            if (group == null)
            {
                // No group selected, return original
                return entries;
            }

            var filtered = new List<LeaderboardEntry>();

            if (group.IsWhitelist)
            {
                // Whitelist: only include members
                foreach (var entry in entries)
                {
                    if (group.ContainsUser(entry.UserId))
                    {
                        filtered.Add(entry);
                    }
                }
            }
            else
            {
                // Blacklist: exclude members
                foreach (var entry in entries)
                {
                    if (!group.ContainsUser(entry.UserId))
                    {
                        filtered.Add(entry);
                    }
                }
            }

            // Re-rank the filtered list
            var result = filtered.ToArray();
            for (int i = 0; i < result.Length; i++)
            {
                // Create a copy with new rank
                result[i] = new LeaderboardEntry
                {
                    UserId = result[i].UserId,
                    Nickname = result[i].Nickname,
                    Avatar = result[i].Avatar,
                    TotalDuration = result[i].TotalDuration,
                    Rank = i + 1
                };
            }

            return result;
        }

        /// <summary>
        /// Get display name for current selection.
        /// </summary>
        public string GetSelectedDisplayName()
        {
            var group = GetSelectedGroup();
            if (group == null)
            {
                return Localization.Text("总榜", "Total", "総合");
            }
            return group.Name;
        }
    }
}
