using System;
using System.IO;

namespace RankingVegas
{
    internal static class EmbeddedResourceHelper
    {
        public static byte[] ReadEmbeddedResource(string resourceName)
        {
            string fullResourceName = $"RankingVegas.Resources.{resourceName}";
            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Resource '{fullResourceName}' not found.");
                }
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return data;
            }
        }

        public static void SaveEmbeddedResource(string resourceName, string folder, string newFileName = null)
        {
            if (string.IsNullOrEmpty(resourceName)) throw new ArgumentException("resourceName is required", nameof(resourceName));
            if (string.IsNullOrEmpty(folder)) throw new ArgumentException("folder is required", nameof(folder));

            byte[] bytes = ReadEmbeddedResource(resourceName);

            string fileName = newFileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = resourceName;
            }

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string destinationPath = Path.Combine(folder, fileName);

            File.WriteAllBytes(destinationPath, bytes);
        }
    }
}
