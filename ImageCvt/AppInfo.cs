using System;
using System.Reflection;
using System.Windows;

namespace ImageCvt
{
    internal class AppInfo
    {
        private static readonly AssemblyName assembly = Application.ResourceAssembly.GetName();
        private static readonly Version v = assembly.Version;

        public const string Guid = "{29245F04-96AF-47EE-8A89-DF8878181FDC}";
        public const string Title = "ImageCvt";
        public const string Author = "hrpzcf";
        public const string Published = "www.52pojie.cn";
        public static readonly string Ver = $"{v.Major}.{v.Minor}.{v.Build}";
        public static readonly string AppName = assembly.Name;
    }
}
