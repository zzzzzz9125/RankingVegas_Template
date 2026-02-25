// How to use: Copy this file to RankingAppProfile.Local.cs, fill in real values and change "#if false" to "#if true".
// Make sure RankingAppProfile.Local.cs is NOT committed to git.
#if false
namespace RankingVegas
{
    internal static partial class RankingAppProfile
    {
        /// <summary>
        /// LoadLocalSettings is a template method to populate local configuration values.
        /// Fill in the real values when copying this file to RankingAppProfile.Local.cs and enable the file.
        /// </summary>
        /// <param name="appId">The application identifier (public client id) provided by the backend or identity provider.</param>
        /// <param name="appSecret">The application secret or key used for server-side authentication (keep this value private).</param>
        /// <param name="leaderboardName">The default name of the leaderboard to display in the app UI.</param>
        /// <param name="appDisplayName">The human-friendly display name of the application shown in the UI.</param>
        /// <param name="apiDomain">The primary API domain (hostname) used for backend API calls.</param>
        /// <param name="webDomain">The web frontend domain (hostname) for constructing public URLs.</param>
        /// <param name="apiDomains">An array of allowed domains (hostnames) for API and web requests; typically includes both apiDomain and webDomain.</param>
        /// <param name="configEncryptionKey">A key used to encrypt local configuration or secrets; keep this secure and do not commit it to source control. If it's null or empty, no encryption will be performed.</param>
        static partial void LoadLocalSettings(ref string appId, ref string appSecret, ref string leaderboardName, ref string appDisplayName, ref string apiDomain, ref string webDomain, ref string[] apiDomains, ref string configEncryptionKey)
        {
            appId = "YOUR_APP_ID_HERE";
            appSecret = "YOUR_APP_SECRET_HERE";
            leaderboardName = "Leaderboard";
            appDisplayName = "Ranking";
            apiDomain = "api.your.site";
            webDomain = "your.site";
            apiDomains = new[]
            {
                apiDomain,
                webDomain
            };
            configEncryptionKey = "YOUR_CONFIG_ENCRYPTION_KEY_HERE";
        }
    }
}
#endif