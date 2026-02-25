using System;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace RankingVegas
{
    public class RankingApiClient
    {
        private readonly string appId;
        private readonly string appSecret;
        
        public RankingApiClient(string appId, string appSecret)
        {
            this.appId = appId;
            this.appSecret = appSecret;
        }
        
        public string GenerateBindUrl(string sessionCode)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string sessionCodeHash = ComputeSha256Hash(sessionCode);
            string signature = GenerateSignature(appId, sessionCodeHash, timestamp.ToString(), appSecret);
            
            return $"{RankingAppProfile.BindUrl}?app_id={appId}&session_code_hash={sessionCodeHash}&timestamp={timestamp}&signature={signature}";
        }
        
        public ApiResponse<bool> ReportDuration(string sessionCode, int duration)
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string signature = GenerateSignature(appId, sessionCode, timestamp.ToString(), appSecret);

                var requestData = new
                {
                    app_id = int.Parse(appId),
                    session_code = sessionCode,
                    duration = duration,
                    timestamp = timestamp,
                    signature = signature
                };
                
                string json = JsonConvert.SerializeObject(requestData);
                string response = PostJson($"{RankingAppProfile.ApiBaseUrl}/plugin/report", json);
                
                return JsonConvert.DeserializeObject<ApiResponse<bool>>(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"上报失败: {ex.Message}"
                };
            }
        }
        
        public ApiResponse<LeaderboardData> GetLeaderboard(int limit = 100, int offset = 0)
        {
            try
            {
                string url = $"{RankingAppProfile.ApiBaseUrl}/plugin/leaderboard/{appId}?limit={limit}&offset={offset}";
                string response = GetRequest(url);
                
                return JsonConvert.DeserializeObject<ApiResponse<LeaderboardData>>(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse<LeaderboardData>
                {
                    Success = false,
                    Message = $"获取排行榜失败: {ex.Message}"
                };
            }
        }
        
        public ApiResponse<UserInfo> GetUserInfo(string sessionCode)
        {
            try
            {
                string sessionCodeHash = ComputeSha256Hash(sessionCode);
                string url = $"{RankingAppProfile.ApiBaseUrl}/plugin/user-info?app_id={appId}&session_code_hash={sessionCodeHash}";
                string response = GetRequest(url);
                
                return JsonConvert.DeserializeObject<ApiResponse<UserInfo>>(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = $"获取用户信息失败: {ex.Message}"
                };
            }
        }
        
        public ApiResponse<bool> InvalidateSession(string sessionCode)
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string signature = GenerateSignature(appId, sessionCode, timestamp.ToString(), appSecret);

                var requestData = new
                {
                    app_id = int.Parse(appId),
                    session_code = sessionCode,
                    timestamp = timestamp,
                    signature = signature
                };

                string json = JsonConvert.SerializeObject(requestData);
                string response = PostJson($"{RankingAppProfile.ApiBaseUrl}/plugin/invalidate-session", json);

                return JsonConvert.DeserializeObject<ApiResponse<bool>>(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"失效会话失败: {ex.Message}"
                };
            }
        }
        
        private static string ComputeSha256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        private string GenerateSignature(string appId, string sessionCode, string timestamp, string appSecret)
        {
            string data = appId + sessionCode + timestamp + appSecret;
            
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        private string PostJson(string url, string json)
        {
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                return client.UploadString(url, "POST", json);
            }
        }
        
        private string GetRequest(string url)
        {
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                return client.DownloadString(url);
            }
        }
    }
    
    public class ApiResponse<T>
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("data")]
        public T Data { get; set; }
    }
    
    public class LeaderboardData
    {
        [JsonProperty("app_id")]
        public int AppId { get; set; }
        
        [JsonProperty("app_name")]
        public string AppName { get; set; }
        
        [JsonProperty("is_verified")]
        public bool IsVerified { get; set; }
        
        [JsonProperty("leaderboard")]
        public LeaderboardEntry[] Leaderboard { get; set; }
    }
    
    public class LeaderboardEntry
    {
        [JsonProperty("user_id")]
        public int UserId { get; set; }
        
        [JsonProperty("nickname")]
        public string Nickname { get; set; }
        
        [JsonProperty("avatar")]
        public string Avatar { get; set; }
        
        [JsonProperty("total_duration")]
        public int TotalDuration { get; set; }
        
        [JsonProperty("rank")]
        public int Rank { get; set; }
    }
    
    public class UserInfo
    {
        [JsonProperty("user_id")]
        public int UserId { get; set; }
        
        [JsonProperty("nickname")]
        public string Nickname { get; set; }
        
        [JsonProperty("avatar")]
        public string Avatar { get; set; }
        
        [JsonProperty("total_duration")]
        public int TotalDuration { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }
    }
}
