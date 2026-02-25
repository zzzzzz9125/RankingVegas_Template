using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RankingVegas
{
    public enum UIStyleOption
    {
        WebView,
        WinForms
    }

    public class RankingConfig
    {
        private const string ConfigFileName = "Ranking.config";
        
        // Minimum values for intervals
        public const int MinOfflineSaveIntervalSeconds = 1;
        public const int MinOnlineReportIntervalSeconds = 60;
        
        // Default values
        public const int DefaultOfflineSaveIntervalSeconds = 30;
        public const int DefaultOnlineReportIntervalSeconds = 60;

        private static string ConfigFolderPath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folderPath = Path.Combine(appDataPath, "RankingVegas");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                return folderPath;
            }
        }

        private static string ConfigFilePath
        {
            get { return Path.Combine(ConfigFolderPath, ConfigFileName); }
        }

        public string AppId { get; private set; }
        public string AppSecret { get; private set; }
        public string SessionCode { get; set; }
        public int OfflineTotalSeconds { get; set; }
        public bool IsOfflineAccount { get; set; }
        public string OfflineAvatarPath { get; set; }
        public string OfflineNickname { get; set; }
        public int OfflineSaveIntervalSeconds { get; set; }
        public int OnlineReportIntervalSeconds { get; set; }
        public bool ShowTimer { get; set; }
        public bool ShowStatus { get; set; }
        public bool ShowAccount { get; set; }
        public bool ShowVegasInfo { get; set; }
        public SupportedLanguage Language { get; set; }
        public bool LanguageInitialized { get; set; }
        public UIStyleOption UIStyle { get; set; }

        public RankingConfig()
        {
            AppId = RankingAppProfile.AppId;
            AppSecret = RankingAppProfile.AppSecret;
            OfflineSaveIntervalSeconds = DefaultOfflineSaveIntervalSeconds;
            OnlineReportIntervalSeconds = DefaultOnlineReportIntervalSeconds;
            Language = SupportedLanguage.English;
            LanguageInitialized = false;
            ShowTimer = true;
            ShowStatus = true;
            ShowAccount = true;
            ShowVegasInfo = true;
            UIStyle = UIStyleOption.WebView;
        }

        public static RankingConfig Load()
        {
            var config = new RankingConfig();

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string content = ReadConfigFile(ConfigFilePath);
                    if (content == null)
                    {
                        // Decryption failed, discard old config
                        try { File.Delete(ConfigFilePath); } catch { }
                        return FinalizeConfig(config);
                    }

                    string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        string[] parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            if (key == "SessionCode")
                            {
                                config.SessionCode = value;
                            }
                            else if (key == "OfflineTotalSeconds")
                            {
                                int totalSeconds;
                                if (int.TryParse(value, out totalSeconds))
                                {
                                    config.OfflineTotalSeconds = totalSeconds;
                                }
                            }
                            else if (key == "IsOfflineAccount")
                            {
                                bool isOffline;
                                if (bool.TryParse(value, out isOffline))
                                {
                                    config.IsOfflineAccount = isOffline;
                                }
                            }
                            else if (key == "OfflineAvatarPath")
                            {
                                config.OfflineAvatarPath = value;
                            }
                            else if (key == "OfflineNickname")
                            {
                                config.OfflineNickname = value;
                            }
                            else if (key == "OfflineSaveIntervalSeconds")
                            {
                                int interval;
                                if (int.TryParse(value, out interval))
                                {
                                    config.OfflineSaveIntervalSeconds = Math.Max(interval, MinOfflineSaveIntervalSeconds);
                                }
                            }
                            else if (key == "OnlineReportIntervalSeconds")
                            {
                                int interval;
                                if (int.TryParse(value, out interval))
                                {
                                    config.OnlineReportIntervalSeconds = Math.Max(interval, MinOnlineReportIntervalSeconds);
                                }
                            }
                            else if (key == "ShowTimer")
                            {
                                bool show;
                                if (bool.TryParse(value, out show))
                                {
                                    config.ShowTimer = show;
                                }
                            }
                            else if (key == "ShowStatus")
                            {
                                bool show;
                                if (bool.TryParse(value, out show))
                                {
                                    config.ShowStatus = show;
                                }
                            }
                            else if (key == "ShowAccount")
                            {
                                bool show;
                                if (bool.TryParse(value, out show))
                                {
                                    config.ShowAccount = show;
                                }
                            }
                            else if (key == "ShowVegasInfo")
                            {
                                bool show;
                                if (bool.TryParse(value, out show))
                                {
                                    config.ShowVegasInfo = show;
                                }
                            }
                            else if (key == "Language")
                            {
                                SupportedLanguage lang;
                                if (Enum.TryParse(value, out lang))
                                {
                                    config.Language = lang;
                                }
                            }
                            else if (key == "LanguageInitialized")
                            {
                                bool initialized;
                                if (bool.TryParse(value, out initialized))
                                {
                                    config.LanguageInitialized = initialized;
                                }
                            }
                            else if (key == "UIStyle")
                            {
                                UIStyleOption style;
                                if (Enum.TryParse(value, out style))
                                {
                                    config.UIStyle = style;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // If anything fails during load, discard old config
                try { File.Delete(ConfigFilePath); } catch { }
            }

            return FinalizeConfig(config);
        }

        private static RankingConfig FinalizeConfig(RankingConfig config)
        {
            // Initialize language from system culture on first run
            if (!config.LanguageInitialized)
            {
                config.Language = Localization.Language;
                config.LanguageInitialized = true;
            }
            else
            {
                // Apply saved language to Localization
                Localization.Language = config.Language;
            }

            return config;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"SessionCode={SessionCode ?? ""}");
                sb.AppendLine($"OfflineTotalSeconds={OfflineTotalSeconds}");
                sb.AppendLine($"IsOfflineAccount={IsOfflineAccount}");
                sb.AppendLine($"OfflineAvatarPath={OfflineAvatarPath ?? ""}");
                sb.AppendLine($"OfflineNickname={OfflineNickname ?? ""}");
                sb.AppendLine($"OfflineSaveIntervalSeconds={OfflineSaveIntervalSeconds}");
                sb.AppendLine($"OnlineReportIntervalSeconds={OnlineReportIntervalSeconds}");
                sb.AppendLine($"ShowTimer={ShowTimer}");
                sb.AppendLine($"ShowStatus={ShowStatus}");
                sb.AppendLine($"ShowAccount={ShowAccount}");
                sb.AppendLine($"ShowVegasInfo={ShowVegasInfo}");
                sb.AppendLine($"Language={Language}");
                sb.AppendLine($"LanguageInitialized={LanguageInitialized}");
                sb.AppendLine($"UIStyle={UIStyle}");

                WriteConfigFile(ConfigFilePath, sb.ToString());
            }
            catch
            {
            }
        }

        public static string GenerateSessionCode()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] data = new byte[32];
                rng.GetBytes(data);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in data)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(AppId) && !string.IsNullOrEmpty(AppSecret)
                && AppId != "YOUR_APP_ID_HERE" && AppSecret != "YOUR_APP_SECRET_HERE";
        }

        public static string GetOfflineAvatarFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appDataPath, "RankingVegas");
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            return Path.Combine(folderPath, "offline_avatar.png");
        }

        public string GetOfflineDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(OfflineNickname))
            {
                return OfflineNickname;
            }
            return Localization.Text("离线账号", "Offline Account", "オフラインアカウント");
        }

        #region Config Encryption

        private static string GetMachineFingerprint()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("MachineGuid");
                        if (value != null)
                            return value.ToString();
                    }
                }
            }
            catch
            {
            }
            return Environment.MachineName;
        }

        private static byte[] DeriveEncryptionKey()
        {
            string encryptionKey = RankingAppProfile.ConfigEncryptionKey;
            if (string.IsNullOrEmpty(encryptionKey))
                return null;

            string combined = encryptionKey + GetMachineFingerprint();
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            }
        }

        private static string ReadConfigFile(string filePath)
        {
            byte[] keyBytes = DeriveEncryptionKey();
            if (keyBytes == null)
            {
                return File.ReadAllText(filePath);
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                string decrypted = AesDecrypt(fileData, keyBytes);
                return decrypted;
            }
            catch
            {
                return null;
            }
        }

        private static void WriteConfigFile(string filePath, string content)
        {
            byte[] keyBytes = DeriveEncryptionKey();
            if (keyBytes == null)
            {
                File.WriteAllText(filePath, content);
                return;
            }

            byte[] encrypted = AesEncrypt(content, keyBytes);
            File.WriteAllBytes(filePath, encrypted);
        }

        private static byte[] AesEncrypt(string plainText, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    // Prepend IV to cipher text
                    byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
                    Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
                    Array.Copy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
                    return result;
                }
            }
        }

        private static string AesDecrypt(byte[] cipherData, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                int ivLength = aes.BlockSize / 8;
                if (cipherData.Length < ivLength)
                    throw new CryptographicException("Invalid encrypted data.");

                byte[] iv = new byte[ivLength];
                Array.Copy(cipherData, 0, iv, 0, ivLength);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] plainBytes = decryptor.TransformFinalBlock(cipherData, ivLength, cipherData.Length - ivLength);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }

        #endregion
    }
}
