using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using SixLabors.ImageSharp.Formats.Png;
using DrawingImage = System.Drawing.Image;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace RankingVegas
{
    public static class ImageHelper
    {
        private static readonly string AvatarCacheFolder;

        static ImageHelper()
        {
            AvatarCacheFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RankingVegas",
                "AvatarCache");
        }

        private static void EnsureCacheFolder()
        {
            if (!Directory.Exists(AvatarCacheFolder))
            {
                Directory.CreateDirectory(AvatarCacheFolder);
            }
        }

        private static string GetCacheFileName(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                Uri uri = new Uri(url);
                string fileName = Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrEmpty(fileName) && fileName != "/" && fileName.Contains("."))
                {
                    return fileName;
                }
            }
            catch
            {
            }

            // Fallback: hash the URL
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
            }
        }

        private static string GetCachedFilePath(string url)
        {
            string cacheFileName = GetCacheFileName(url);
            if (cacheFileName == null)
                return null;

            EnsureCacheFolder();
            return Path.Combine(AvatarCacheFolder, cacheFileName);
        }

        public static string NormalizeAvatarUrl(string avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl))
            {
                return avatarUrl;
            }

            if (!avatarUrl.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!avatarUrl.StartsWith("/"))
                {
                    avatarUrl = $"/{avatarUrl}";
                }
                avatarUrl = $"{RankingAppProfile.ApiOrigin}{avatarUrl}";
            }

            return avatarUrl;
        }

        // Downloads raw image bytes, using local cache when available.
        public static byte[] LoadImageBytesFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                url = NormalizeAvatarUrl(url);

                // Check cache
                string cachePath = GetCachedFilePath(url);
                if (cachePath != null && File.Exists(cachePath))
                {
                    try
                    {
                        return File.ReadAllBytes(cachePath);
                    }
                    catch
                    {
                    }
                }

                // Ensure TLS 1.2 is enabled for HTTPS connections
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                byte[] data;
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "RankingVegas/1.0");
                    data = client.DownloadData(url);
                }

                // Save to cache
                if (data != null && data.Length > 0 && cachePath != null)
                {
                    try
                    {
                        EnsureCacheFolder();
                        File.WriteAllBytes(cachePath, data);
                    }
                    catch
                    {
                    }
                }

                return data;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Loads an image from a local file path, using ImageSharp to support WebP and other formats
        /// that System.Drawing does not handle natively.
        /// </summary>
        public static DrawingImage LoadImageFromFile(string filePath, int maxWidth = 0, int maxHeight = 0)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                return DecodeImageBytes(fileData, maxWidth, maxHeight);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Decodes raw image bytes into a System.Drawing.Image using ImageSharp (supports WebP, PNG, JPEG, etc.).
        /// Falls back to System.Drawing if ImageSharp fails.
        /// </summary>
        public static DrawingImage DecodeImageBytes(byte[] imageData, int maxWidth = 0, int maxHeight = 0)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            // Try ImageSharp first (supports WebP and other formats)
            try
            {
                using (var input = new MemoryStream(imageData))
                using (var image = ImageSharpImage.Load(input))
                using (var pngMs = new MemoryStream())
                {
                    image.Save(pngMs, new PngEncoder());
                    pngMs.Position = 0;

                    using (var loadedImage = DrawingImage.FromStream(pngMs))
                    {
                        DrawingImage img = new Bitmap(loadedImage);
                        if (img != null && maxWidth > 0 && maxHeight > 0)
                        {
                            img = ResizeImage(img, maxWidth, maxHeight);
                        }

                        return img;
                    }
                }
            }
            catch
            {
            }

            // Fallback: try System.Drawing decode directly
            try
            {
                using (var ms = new MemoryStream(imageData))
                using (var loadedImage = DrawingImage.FromStream(ms))
                {
                    DrawingImage img = new Bitmap(loadedImage);
                    if (img != null && maxWidth > 0 && maxHeight > 0)
                    {
                        img = ResizeImage(img, maxWidth, maxHeight);
                    }

                    return img;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the local cache file path for a given avatar URL.
        /// If the file is already cached, returns the path without downloading.
        /// If not cached, downloads and caches the file, then returns the path.
        /// Returns null on failure.
        /// </summary>
        public static string GetCachedAvatarFilePath(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            url = NormalizeAvatarUrl(url);

            string cachePath = GetCachedFilePath(url);
            if (cachePath != null && File.Exists(cachePath))
            {
                return cachePath;
            }

            // Download and cache
            byte[] data = LoadImageBytesFromUrl(url);
            if (data != null && data.Length > 0 && cachePath != null && File.Exists(cachePath))
            {
                return cachePath;
            }

            return null;
        }

        /// <summary>
        /// Returns a data URL (base64) for a cached avatar image, suitable for use in WebView.
        /// Downloads and caches if not already cached.
        /// </summary>
        public static string GetCachedAvatarAsDataUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                string cachedPath = GetCachedAvatarFilePath(url);
                if (cachedPath != null && File.Exists(cachedPath))
                {
                    byte[] data = File.ReadAllBytes(cachedPath);
                    string mimeType = DetectMimeType(data);
                    return $"data:{mimeType};base64,{Convert.ToBase64String(data)}";
                }
            }
            catch
            {
            }

            return null;
        }

        private static string DetectMimeType(byte[] data)
        {
            if (data == null || data.Length < 4)
                return "image/png";

            // PNG
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "image/png";

            // JPEG
            if (data[0] == 0xFF && data[1] == 0xD8)
                return "image/jpeg";

            // GIF
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return "image/gif";

            // WebP (RIFF....WEBP)
            if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
                return "image/webp";

            // BMP
            if (data[0] == 0x42 && data[1] == 0x4D)
                return "image/bmp";

            return "image/png";
        }

        // Loads an Image suitable for WinForms by decoding (via ImageSharp) and converting to PNG-backed System.Drawing.Image.
        public static DrawingImage LoadImageForWinFormsFromUrl(string url, int maxWidth = 0, int maxHeight = 0)
        {
            var imageData = LoadImageBytesFromUrl(url);
            if (imageData == null || imageData.Length == 0)
            {
                return CreatePlaceholderImage(maxWidth > 0 ? maxWidth : 50, maxHeight > 0 ? maxHeight : 50);
            }

            DrawingImage result = DecodeImageBytes(imageData, maxWidth, maxHeight);
            if (result != null)
                return result;

            return CreatePlaceholderImage(maxWidth > 0 ? maxWidth : 50, maxHeight > 0 ? maxHeight : 50);
        }

        private static DrawingImage ResizeImage(DrawingImage image, int maxWidth, int maxHeight)
        {
            if (image == null)
                return null;

            int newWidth = image.Width;
            int newHeight = image.Height;

            double ratioX = (double)maxWidth / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio = Math.Min(ratioX, ratioY);

            newWidth = (int)(image.Width * ratio);
            newHeight = (int)(image.Height * ratio);

            Bitmap resized = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            image.Dispose();
            return resized;
        }

        private static DrawingImage CreatePlaceholderImage(int width, int height)
        {
            Bitmap placeholder = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(placeholder))
            {
                g.Clear(Color.FromArgb(200, 200, 200));

                using (Pen pen = new Pen(Color.FromArgb(150, 150, 150), 2))
                {
                    g.DrawRectangle(pen, 1, 1, width - 2, height - 2);
                }

                string text = "?";
                using (Font font = new Font("Arial", Math.Max(8, Math.Min(width, height) / 3), FontStyle.Bold))
                {
                    SizeF textSize = g.MeasureString(text, font);
                    g.DrawString(text, font, Brushes.Gray,
                        (width - textSize.Width) / 2,
                        (height - textSize.Height) / 2);
                }
            }

            return placeholder;
        }

        public static DrawingImage MakeRoundedImage(DrawingImage image, int cornerRadius)
        {
            if (image == null)
                return null;

            Bitmap rounded = new Bitmap(image.Width, image.Height);

            using (Graphics g = Graphics.FromImage(rounded))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, image.Width, image.Height), cornerRadius))
                {
                    g.SetClip(path);
                    g.DrawImage(image, 0, 0, image.Width, image.Height);
                }
            }

            return rounded;
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
