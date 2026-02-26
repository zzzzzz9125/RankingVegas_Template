namespace RankingVegas
{
    internal static partial class RankingAppProfile
    {
        internal static readonly string ApiDomain;
        internal static readonly string WebDomain;

        internal static readonly string[] ApiDomains;

        internal static readonly string ApiOrigin;
        internal static readonly string WebOrigin;
        internal static readonly string ApiBaseUrl;
        internal static readonly string BindUrl;

        internal static readonly string AppId;
        internal static readonly string AppSecret;
        internal static readonly string LeaderboardName;
        internal static readonly string AppDisplayName;
        internal static readonly string ConfigEncryptionKey;

        internal static readonly bool IsDemo;

        static RankingAppProfile()
        {
            string appId = null;
            string appSecret = null;
            string leaderboardName = null;
            string appDisplayName = null;
            string apiDomain = null;
            string webDomain = null;
            string[] apiDomains = null;
            string configEncryptionKey = null;
            bool isDemo = false;

            LoadLocalSettings(ref appId, ref appSecret, ref leaderboardName, ref appDisplayName, ref apiDomain, ref webDomain, ref apiDomains, ref configEncryptionKey, ref isDemo);

            AppId = appId ?? string.Empty;
            AppSecret = appSecret ?? string.Empty;
            LeaderboardName = string.IsNullOrWhiteSpace(leaderboardName) ? "Leaderboard" : leaderboardName;
            AppDisplayName = string.IsNullOrWhiteSpace(appDisplayName) ? "Ranking" : appDisplayName;
            ApiDomain = apiDomain;
            WebDomain = webDomain;
            ApiDomains = NormalizeDomains(apiDomains, ApiDomain, WebDomain);
            ConfigEncryptionKey = configEncryptionKey ?? string.Empty;
            IsDemo = isDemo;

            ApiOrigin = $"https://{ApiDomain}";
            WebOrigin = $"https://{WebDomain}";
            ApiBaseUrl = $"{ApiOrigin}/api/ranking";
            BindUrl = $"{WebOrigin}/ranking/bind";
        }

        internal static string GetDisplayName(bool isOffline)
        {
            if (isOffline)
            {
                return Localization.Text("离线账号", "Offline Account", "オフラインアカウント");
            }

            return AppDisplayName;
        }

        private static string[] NormalizeDomains(string[] apiDomains, string apiDomain, string webDomain)
        {
            var domains = new System.Collections.Generic.List<string>();

            if (apiDomains != null)
            {
                foreach (var domain in apiDomains)
                {
                    if (!string.IsNullOrWhiteSpace(domain) && !domains.Contains(domain))
                    {
                        domains.Add(domain);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(apiDomain) && !domains.Contains(apiDomain))
            {
                domains.Add(apiDomain);
            }

            if (!string.IsNullOrWhiteSpace(webDomain) && !domains.Contains(webDomain))
            {
                domains.Add(webDomain);
            }

            return domains.ToArray();
        }

        static partial void LoadLocalSettings(ref string appId, ref string appSecret, ref string leaderboardName, ref string appDisplayName, ref string apiDomain, ref string webDomain, ref string[] apiDomains, ref string configEncryptionKey, ref bool isDemo);
    }
}
