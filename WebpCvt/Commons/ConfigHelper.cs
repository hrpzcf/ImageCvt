using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Xml.Serialization;

namespace WebpCvt
{
    public static class ConfigHelper
    {
        public const string LibWebpCvt = "webpcvt.dll";

        private static readonly string localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        private static readonly string configDir = Path.Combine(localAppData, "WebpCvt");
        private static readonly string configFile = Path.Combine(configDir, "config.xml");
        private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
        private static readonly string executableDir = Path.GetDirectoryName(assembly.Location);
        private static readonly string dwebpBinaryFullPath = Path.Combine(executableDir, LibWebpCvt);
        private const string binariesResourcePrefix = "WebpCvt.Library";

        public static string DwebpFullPath => dwebpBinaryFullPath;

        public static ProxyOfMainModel Current { get; private set; } =
            new ProxyOfMainModel();

        public static bool SaveConfig()
        {
            try
            {
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                XmlSerializer serializer = new XmlSerializer(typeof(MainWindowModel));
                using (StreamWriter streamWriter = new StreamWriter(configFile, false, Encoding.UTF8))
                {
                    serializer.Serialize(streamWriter, Current.MainModelProxy);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置保存失败：{ex.Message}", "错误", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            return false;
        }

        public static bool LoadConfig()
        {
            try
            {
                if (File.Exists(configFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(MainWindowModel));
                    using (StreamReader streamReader = new StreamReader(configFile, Encoding.UTF8, true))
                    {
                        if (serializer.Deserialize(streamReader) is MainWindowModel mainModel)
                        {
                            Current.MainModelProxy = mainModel;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置加载失败：{ex.Message}", "错误", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            return false;
        }

        public static byte[] GetManifestResource(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    using (Stream stream = assembly.GetManifestResourceStream(path))
                    {
                        if (stream != null)
                        {
                            byte[] streamBuffer = new byte[stream.Length];
                            stream.Read(streamBuffer, 0, streamBuffer.Length);
                            return streamBuffer;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return default(byte[]);
        }

        private static string ExtractFile(string prefix, string name, string extractDir, bool force)
        {
            try
            {
                string extractPath = Path.Combine(extractDir, name);
                if (force || !File.Exists(extractPath))
                {
                    if (!Directory.Exists(extractDir))
                    {
                        Directory.CreateDirectory(extractDir);
                    }
                    string resourcePath = $"{prefix}.{name}";
                    if (GetManifestResource(resourcePath) is byte[] resourceBytes)
                    {
                        using (FileStream fileStream = File.OpenWrite(extractPath))
                        {
                            fileStream.Write(resourceBytes, 0, resourceBytes.Length);
                        }
                    }
                    else
                    {
                        return $"没有找到内嵌资源： {prefix}";
                    }
                }
                return default(string);
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        public static string CheckExtractLibWebpCvt()
        {
            string message = ExtractFile(binariesResourcePrefix, LibWebpCvt, executableDir,
                Current.MainModelProxy.PreviousVer?.Equals(AppInfo.Ver, StringComparison.OrdinalIgnoreCase) != true);
            if (string.IsNullOrEmpty(message))
            {
                Current.MainModelProxy.PreviousVer = AppInfo.Ver;
            }
            return message;
        }
    }
}
