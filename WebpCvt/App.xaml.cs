using System;
using System.Threading;
using System.Windows;

namespace WebpCvt
{
    public partial class App : Application
    {
        public Mutex Mutex { get; private set; }

        private void ApplicationExit(object sender, ExitEventArgs e)
        {
            ConfigHelper.SaveConfig();
        }

        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            try
            {
                this.Mutex = new Mutex(false, AppInfo.Guid, out bool newMutex);
                if (!newMutex)
                {
                    MessageBox.Show("已经有一个运行中的 WebpCvt 实例，请查看桌面右下角托盘图标！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                    Environment.Exit(0);
                }
            }
            catch (Exception)
            {
            }
            ConfigHelper.LoadConfig();
            ConfigHelper.CheckExtractLibWebpCvt();
        }
    }
}
